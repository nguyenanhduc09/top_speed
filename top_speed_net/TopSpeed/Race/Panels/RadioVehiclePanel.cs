using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using TopSpeed.Audio;
using TopSpeed.Core;
using TopSpeed.Input;
using TopSpeed.Vehicles;
using TS.Audio;

namespace TopSpeed.Race.Panels
{
    internal sealed class RadioVehiclePanel : IVehicleRacePanel
    {
        private const int VolumeStepPercent = 10;

        private readonly RaceInput _input;
        private readonly AudioManager _audio;
        private readonly RaceSettings _settings;
        private readonly VehicleRadioController _radio;
        private readonly Func<uint> _nextMediaId;
        private readonly Action<string> _announce;
        private readonly Action<uint, string>? _mediaLoaded;
        private readonly Action<bool, bool, uint>? _playbackChanged;
        private readonly object _pendingPathLock = new object();
        private volatile bool _pickerInProgress;
        private string? _pendingSelectedPath;
        private AudioSourceHandle? _volumeUpSound;
        private AudioSourceHandle? _volumeDownSound;

        public RadioVehiclePanel(
            RaceInput input,
            AudioManager audio,
            RaceSettings settings,
            VehicleRadioController radio,
            Func<uint> nextMediaId,
            Action<string> announce,
            Action<uint, string>? mediaLoaded = null,
            Action<bool, bool, uint>? playbackChanged = null)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _radio = radio ?? throw new ArgumentNullException(nameof(radio));
            _nextMediaId = nextMediaId ?? throw new ArgumentNullException(nameof(nextMediaId));
            _announce = announce ?? throw new ArgumentNullException(nameof(announce));
            _mediaLoaded = mediaLoaded;
            _playbackChanged = playbackChanged;
        }

        public string Name => "Radio";
        public bool AllowsDrivingInput => false;
        public bool AllowsAuxiliaryInput => false;

        public void Update(float elapsed)
        {
            ProcessPendingSelection();

            if (_input.GetOpenRadioMediaRequest())
                OpenRadioMedia();

            if (_input.GetToggleRadioPlaybackRequest())
                TogglePlayback();

            if (_input.GetRadioVolumeUpRequest())
                AdjustVolume(VolumeStepPercent, "volume_up.ogg");
            else if (_input.GetRadioVolumeDownRequest())
                AdjustVolume(-VolumeStepPercent, "volume_down.ogg");
        }

        public void Pause()
        {
            _radio.PauseForGame();
        }

        public void Resume()
        {
            _radio.ResumeFromGame();
        }

        public void Dispose()
        {
            _audio.ReleaseCachedSource(_volumeUpSound);
            _audio.ReleaseCachedSource(_volumeDownSound);
            _volumeUpSound = null;
            _volumeDownSound = null;
        }

        private void OpenRadioMedia()
        {
            if (_pickerInProgress)
                return;

            _pickerInProgress = true;
            BeginShowMediaPickerDialog(selectedPath =>
            {
                lock (_pendingPathLock)
                    _pendingSelectedPath = selectedPath;

                _pickerInProgress = false;
            });
        }

        private void ProcessPendingSelection()
        {
            string? selectedPath;
            lock (_pendingPathLock)
            {
                selectedPath = _pendingSelectedPath;
                _pendingSelectedPath = null;
            }

            if (string.IsNullOrWhiteSpace(selectedPath))
                return;
            var mediaPath = selectedPath!;

            var mediaId = _nextMediaId();
            if (!_radio.TryLoadFromFile(mediaPath, mediaId, preservePlaybackState: true, out var error))
            {
                _announce($"Failed to load radio media. {error}");
                return;
            }

            _announce($"Radio loaded {Path.GetFileName(mediaPath)}.");
            _mediaLoaded?.Invoke(mediaId, mediaPath);
            _playbackChanged?.Invoke(_radio.HasMedia, _radio.DesiredPlaying, _radio.MediaId);
        }

        private static void BeginShowMediaPickerDialog(Action<string?> onCompleted)
        {
            void ShowDialog()
            {
                string? selectedPath = null;
                using (var dialog = new OpenFileDialog())
                {
                    dialog.CheckFileExists = true;
                    dialog.CheckPathExists = true;
                    dialog.Multiselect = false;
                    dialog.Title = "Select radio media file";
                    dialog.Filter = "Audio files|*.wav;*.ogg;*.mp3;*.flac;*.aac;*.m4a|All files|*.*";

                    var owner = GetDialogOwner();
                    var result = owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog();
                    if (result == DialogResult.OK)
                        selectedPath = dialog.FileName;
                }

                onCompleted(selectedPath);
            }

            var ownerWindow = GetDialogOwnerForm();
            if (ownerWindow != null && ownerWindow.IsHandleCreated && !ownerWindow.IsDisposed)
            {
                ownerWindow.BeginInvoke((Action)ShowDialog);
                return;
            }

            // Fallback for rare startup/shutdown timing where no form owner is available.
            var thread = new Thread(() => ShowDialog())
            {
                IsBackground = true,
                Name = "RadioMediaPicker"
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private static Form? GetDialogOwnerForm()
        {
            if (Application.OpenForms.Count == 0)
                return null;

            return Application.OpenForms[0];
        }

        private static IWin32Window? GetDialogOwner() => GetDialogOwnerForm();

        private void TogglePlayback()
        {
            if (!_radio.HasMedia)
            {
                _announce("No radio media loaded.");
                return;
            }

            _radio.TogglePlayback();
            _announce(_radio.DesiredPlaying ? "Radio playing." : "Radio paused.");
            _playbackChanged?.Invoke(_radio.HasMedia, _radio.DesiredPlaying, _radio.MediaId);
        }

        private void AdjustVolume(int deltaPercent, string feedbackSound)
        {
            var previous = _radio.VolumePercent;
            var target = previous + deltaPercent;
            if (target < 0)
                target = 0;
            else if (target > 100)
                target = 100;

            _radio.SetVolumePercent(target);
            if (target != previous)
                _announce($"{target}%");
            PlayFeedback(feedbackSound);
        }

        private void PlayFeedback(string fileName)
        {
            var sound = GetFeedbackSound(fileName);
            if (sound == null)
                return;

            try
            {
                sound.SetVolumePercent(_settings, AudioVolumeCategory.OnlineServerEvents, 100);
                sound.Restart(loop: false);
            }
            catch
            {
            }
        }

        private AudioSourceHandle? GetFeedbackSound(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            ref var cache = ref _volumeDownSound;
            if (string.Equals(fileName, "volume_up.ogg", StringComparison.OrdinalIgnoreCase))
                cache = ref _volumeUpSound;

            if (cache != null)
                return cache;

            var path = Path.Combine(AssetPaths.SoundsRoot, "network", fileName);
            if (!_audio.TryResolvePath(path, out var fullPath))
                return null;

            try
            {
                cache = _audio.AcquireCachedSource(fullPath, streamFromDisk: false);
                cache.SetVolumePercent(_settings, AudioVolumeCategory.OnlineServerEvents, 100);
                return cache;
            }
            catch
            {
                return null;
            }
        }
    }
}
