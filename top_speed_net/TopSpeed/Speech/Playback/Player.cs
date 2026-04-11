using System;
using TopSpeed.Audio;
using TS.Audio;

namespace TopSpeed.Speech.Playback
{
    internal sealed class Player : IPlayer, IDisposable
    {
        private const string BusName = AudioEngineOptions.SpeechBusName;

        private readonly AudioManager _audio;
        private readonly object _sync = new object();
        private readonly Ring _ring = new Ring();
        private Source? _source;
        private int _channels;
        private int _sampleRate;
        private long _holdUntilMs;

        public Player(AudioManager audio)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        }

        public bool IsSpeaking
        {
            get
            {
                lock (_sync)
                {
                    return _ring.Count > 0 || NowMs() < _holdUntilMs;
                }
            }
        }

        public void Write(float[] samples, int channels, int sampleRate, bool interrupt)
        {
            if (samples == null)
                throw new ArgumentNullException(nameof(samples));

            if (samples.Length == 0 || channels <= 0 || sampleRate <= 0)
                return;

            lock (_sync)
            {
                var formatChanged = channels != _channels || sampleRate != _sampleRate;
                if (formatChanged)
                {
                    ResetSource();
                    interrupt = true;
                }

                if (_source == null)
                {
                    _channels = channels;
                    _sampleRate = sampleRate;
                    _source = _audio.CreateProceduralSource(
                        Render,
                        (uint)channels,
                        (uint)sampleRate,
                        busName: BusName,
                        spatialize: false,
                        useHrtf: false);
                }

                if (interrupt)
                    _ring.Clear();

                _ring.Write(samples, 0, samples.Length);
                ExtendPlaybackWindow(samples.Length, channels, sampleRate);

                if (!_source.IsPlaying)
                    _source.Play(loop: true);
            }
        }

        public void Stop()
        {
            lock (_sync)
            {
                _ring.Clear();
                _holdUntilMs = 0;
                _source?.Stop();
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                _ring.Clear();
                _holdUntilMs = 0;
                ResetSource();
            }
        }

        private void Render(float[] buffer, int frames, int channels, ref ulong frameIndex)
        {
            lock (_sync)
            {
                var requestedSamples = frames * channels;
                var copied = _ring.Read(buffer, 0, requestedSamples);
                if (copied > 0 && copied < requestedSamples)
                    SetHoldWindow(150);
                else if (copied == 0)
                    _holdUntilMs = 0;
                else if (_ring.Count == 0)
                    SetHoldWindow(150);
            }
        }

        private void ExtendPlaybackWindow(int sampleCount, int channels, int sampleRate)
        {
            var durationMs = (int)Math.Ceiling((sampleCount * 1000d) / (channels * sampleRate));
            SetHoldWindow(Math.Max(150, durationMs + 50));
        }

        private void SetHoldWindow(int durationMs)
        {
            _holdUntilMs = Math.Max(_holdUntilMs, NowMs() + durationMs);
        }

        private void ResetSource()
        {
            if (_source == null)
                return;

            try
            {
                _source.Stop();
            }
            catch
            {
            }

            _source.Dispose();
            _source = null;
            _channels = 0;
            _sampleRate = 0;
        }

        private static long NowMs()
        {
            return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        }
    }
}
