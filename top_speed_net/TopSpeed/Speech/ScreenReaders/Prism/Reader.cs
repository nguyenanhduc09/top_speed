using System;
using System.Collections.Generic;
using System.Linq;
using TopSpeed.Speech.Playback;
using TopSpeed.Speech.Prism;

namespace TopSpeed.Speech.ScreenReaders.Prism
{
    internal sealed class Reader : IScreenReader
    {
        private Context? _context;
        private Backend? _backend;
        private ulong? _preferredBackendId;
        private ulong? _activeBackendId;
        private int? _preferredVoiceIndex;
        private bool _trySapi;
        private bool _preferSapi;
        private IPlayer? _player;

        public IReadOnlyList<SpeechBackendInfo> AvailableBackends
        {
            get
            {
                if (_context == null)
                    return Array.Empty<SpeechBackendInfo>();

                var backends = new List<SpeechBackendInfo>();
                var source = _context.AvailableBackends;
                for (var i = 0; i < source.Count; i++)
                {
                    var info = source[i];
                    if (!TryProbeSupportedBackend(_context, info, out var backendName))
                        continue;

                    backends.Add(new SpeechBackendInfo(
                        info.Id,
                        string.IsNullOrWhiteSpace(backendName) ? info.Name : backendName,
                        info.Priority,
                        isSupported: true));
                }

                return backends;
            }
        }

        public IReadOnlyList<SpeechVoiceInfo> AvailableVoices
        {
            get
            {
                if (_backend == null)
                    return Array.Empty<SpeechVoiceInfo>();

                return _backend.Voices
                    .Select(static voice => new SpeechVoiceInfo(voice.Index, voice.Name, voice.Language))
                    .ToArray();
            }
        }

        public ulong? PreferredBackendId
        {
            get => _preferredBackendId;
            set => _preferredBackendId = value;
        }

        public ulong? ActiveBackendId => _activeBackendId;

        public int? PreferredVoiceIndex
        {
            get => _preferredVoiceIndex;
            set
            {
                _preferredVoiceIndex = value;
                ApplyVoicePreference();
            }
        }

        public SpeechCapabilities Capabilities => _backend == null ? SpeechCapabilities.None : (SpeechCapabilities)(ulong)_backend.Features;

        public string? ActiveBackendName
        {
            get
            {
                try
                {
                    return _backend?.Name;
                }
                catch
                {
                    return null;
                }
            }
        }

        public bool Initialize()
        {
            Close();

            try
            {
                _context = new Context();
                _backend = OpenBackend(_context);
                return true;
            }
            catch
            {
                Close();
                return false;
            }
        }

        public bool IsLoaded() => _context != null && _backend != null;

        public bool Speak(string text, bool interrupt = true)
        {
            if (string.IsNullOrWhiteSpace(text) || _backend == null)
                return false;

            try
            {
                if (ShouldUseMemorySpeech())
                    return SpeakToPlayer(text, interrupt);

                if (_backend.Supports(Features.Speak))
                {
                    _backend.Speak(text, interrupt);
                    return true;
                }

                if (_backend.Supports(Features.Output))
                {
                    _backend.Output(text, interrupt);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool IsSpeaking()
        {
            try
            {
                if (_player?.IsSpeaking == true)
                    return true;

                return _backend != null && _backend.IsSpeaking;
            }
            catch
            {
                return false;
            }
        }

        public void Close()
        {
            _player?.Stop();
            _backend?.Dispose();
            _backend = null;
            _activeBackendId = null;
            _context?.Dispose();
            _context = null;
        }

        public float GetVolume()
        {
            try
            {
                return _backend?.Volume ?? 0f;
            }
            catch
            {
                return 0f;
            }
        }

        public void SetVolume(float volume)
        {
            try
            {
                if (_backend != null)
                    _backend.Volume = volume;
            }
            catch
            {
            }
        }

        public float GetRate()
        {
            try
            {
                return _backend?.Rate ?? 0f;
            }
            catch
            {
                return 0f;
            }
        }

        public void SetRate(float rate)
        {
            try
            {
                if (_backend != null)
                    _backend.Rate = rate;
            }
            catch
            {
            }
        }

        public bool HasSpeech()
        {
            return _backend != null && (_backend.Supports(Features.Speak) || _backend.Supports(Features.Output));
        }

        public void TrySAPI(bool trySapi)
        {
            _trySapi = trySapi;
        }

        public void PreferSAPI(bool preferSapi)
        {
            _preferSapi = preferSapi;
        }

        public string? DetectScreenReader()
        {
            try
            {
                return _backend?.Name;
            }
            catch
            {
                return null;
            }
        }

        public bool Output(string text, bool interrupt = true)
        {
            if (string.IsNullOrWhiteSpace(text) || _backend == null)
                return false;

            try
            {
                if (ShouldUseMemorySpeech())
                    return SpeakToPlayer(text, interrupt);

                if (_backend.Supports(Features.Output))
                {
                    _backend.Output(text, interrupt);
                    return true;
                }

                return Speak(text, interrupt);
            }
            catch
            {
                return false;
            }
        }

        public bool HasBraille()
        {
            return _backend != null && _backend.Supports(Features.Braille);
        }

        public bool Braille(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || _backend == null || !_backend.Supports(Features.Braille))
                return false;

            try
            {
                _backend.Braille(text);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Silence()
        {
            var silenced = false;
            if (_player != null)
            {
                _player.Stop();
                silenced = true;
            }

            if (_backend == null || !_backend.Supports(Features.Stop))
                return silenced;

            try
            {
                _backend.Stop();
                return true;
            }
            catch
            {
                return silenced;
            }
        }

        private Backend OpenBackend(Context context)
        {
            if (_preferredBackendId.HasValue)
            {
                var preferred = TryOpen(context, new BackendInfo(_preferredBackendId.Value, string.Empty, 0, true));
                if (preferred != null)
                {
                    _activeBackendId = ResolveActiveBackendId(context, preferred, _preferredBackendId.Value);
                    ApplyVoicePreference();
                    return preferred;
                }
            }

            var candidates = context.AvailableBackends
                .Where(static backend => backend.IsSupported)
                .OrderByDescending(static backend => backend.Priority)
                .ToArray();

            if (_preferSapi)
            {
                var sapi = TryOpen(context, candidates.FirstOrDefault(static backend => backend.Id == Ids.Sapi));
                if (sapi != null)
                {
                    _activeBackendId = ResolveActiveBackendId(context, sapi, Ids.Sapi);
                    ApplyVoicePreference();
                    return sapi;
                }
            }

            for (var i = 0; i < candidates.Length; i++)
            {
                if (!_trySapi && candidates[i].Id == Ids.Sapi)
                    continue;

                var backend = TryOpen(context, candidates[i]);
                if (backend != null)
                {
                    _activeBackendId = ResolveActiveBackendId(context, backend, candidates[i].Id);
                    ApplyVoicePreference();
                    return backend;
                }
            }

            if (_trySapi)
            {
                var sapi = TryOpen(context, candidates.FirstOrDefault(static backend => backend.Id == Ids.Sapi));
                if (sapi != null)
                {
                    _activeBackendId = ResolveActiveBackendId(context, sapi, Ids.Sapi);
                    ApplyVoicePreference();
                    return sapi;
                }
            }

            var best = context.AcquireBest();
            _activeBackendId = ResolveActiveBackendId(context, best, null);
            ApplyVoicePreference();
            return best;
        }

        private static Backend? TryOpen(Context context, BackendInfo info)
        {
            if (info.Id == Ids.Invalid)
                return null;

            try
            {
                var backend = context.Acquire(info.Id);
                if (!backend.IsSupportedAtRuntime)
                {
                    backend.Dispose();
                    return null;
                }

                return backend;
            }
            catch
            {
                try
                {
                    var backend = context.Create(info.Id);
                    if (!backend.IsSupportedAtRuntime)
                    {
                        backend.Dispose();
                        return null;
                    }

                    return backend;
                }
                catch
                {
                    return null;
                }
            }
        }

        public void BindPlayer(IPlayer? player)
        {
            _player = player;
        }

        private static bool TryProbeSupportedBackend(Context context, BackendInfo info, out string backendName)
        {
            backendName = info.Name;
            if (info.Id == Ids.Invalid)
                return false;

            try
            {
                using var backend = context.Create(info.Id);
                if (!backend.IsSupportedAtRuntime)
                    return false;

                backendName = backend.Name;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ApplyVoicePreference()
        {
            if (_backend == null || !_preferredVoiceIndex.HasValue)
                return;

            try
            {
                _backend.CurrentVoiceIndex = _preferredVoiceIndex.Value;
            }
            catch
            {
            }
        }

        private static ulong? ResolveActiveBackendId(Context context, Backend backend, ulong? requestedId)
        {
            if (requestedId.HasValue && requestedId.Value != Ids.Invalid)
                return requestedId;

            var name = backend.Name;
            var match = context.AvailableBackends.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));

            return match.Id == Ids.Invalid ? null : match.Id;
        }

        private bool ShouldUseMemorySpeech()
        {
            return _player != null
                && _backend != null
                && _activeBackendId == Ids.Sapi
                && _backend.Supports(Features.SpeakToMemory);
        }

        private bool SpeakToPlayer(string text, bool interrupt)
        {
            if (_backend == null || _player == null)
                return false;

            var firstChunk = true;
            _backend.SpeakToMemory(
                text,
                (samples, channels, sampleRate) =>
                {
                    _player.Write(samples, channels, sampleRate, interrupt && firstChunk);
                    firstChunk = false;
                });
            return true;
        }
    }
}
