using System;
using MiniAudioEx.Native;
using SteamAudio;

namespace TS.Audio
{
    internal sealed partial class SteamAudioSpatializer : IDisposable
    {
        private readonly SteamAudioContext _ctx;
        private readonly bool _trueStereo;
        private readonly HrtfDownmixMode _downmixMode;
        private IPL.BinauralEffect _binauralLeft;
        private IPL.BinauralEffect _binauralRight;
        private IPL.AmbisonicsBinauralEffect _ambisonicsBinauralParametric;
        private IPL.AmbisonicsRotationEffect _ambisonicsRotationParametric;
        private IPL.DirectEffect _directLeft;
        private IPL.DirectEffect _directRight;
        private IPL.ReflectionEffect _reflectionParametric;
        private readonly float[] _mono;
        private readonly float[] _outL;
        private readonly float[] _outR;
        private readonly float[] _inLeft;
        private readonly float[] _inRight;
        private readonly float[] _directLeftSamples;
        private readonly float[] _directRightSamples;
        private readonly float[] _outLeftL;
        private readonly float[] _outLeftR;
        private readonly float[] _outRightL;
        private readonly float[] _outRightR;
        private readonly float[] _reverbAmbi;
        private readonly float[] _reverbAmbiRotated;
        private readonly float[] _reverbWetL;
        private readonly float[] _reverbWetR;
        private float _reflectionWet;
        private readonly int _frameSize;
        private readonly int _reflectionOrder;
        private readonly int _reflectionChannels;

        public SteamAudioSpatializer(SteamAudioContext context, uint frameSize, bool trueStereo, HrtfDownmixMode downmixMode)
        {
            _ctx = context;
            _trueStereo = trueStereo;
            _downmixMode = downmixMode;
            _frameSize = (int)frameSize;
            _reflectionOrder = Math.Max(0, _ctx.ReflectionOrder);
            _reflectionChannels = Math.Max(1, _ctx.ReflectionChannels);

            var audioSettings = new IPL.AudioSettings
            {
                SamplingRate = _ctx.SampleRate,
                FrameSize = _ctx.FrameSize
            };

            var binauralSettings = new IPL.BinauralEffectSettings
            {
                Hrtf = _ctx.Hrtf
            };

            var error = IPL.BinauralEffectCreate(_ctx.Context, in audioSettings, in binauralSettings, out _binauralLeft);
            if (error != IPL.Error.Success)
                throw new InvalidOperationException("Failed to create binaural effect: " + error);

            error = IPL.BinauralEffectCreate(_ctx.Context, in audioSettings, in binauralSettings, out _binauralRight);
            if (error != IPL.Error.Success)
                throw new InvalidOperationException("Failed to create binaural effect: " + error);

            var ambiSettings = new IPL.AmbisonicsBinauralEffectSettings
            {
                Hrtf = _ctx.Hrtf,
                MaxOrder = _reflectionOrder
            };

            error = IPL.AmbisonicsBinauralEffectCreate(_ctx.Context, in audioSettings, in ambiSettings, out _ambisonicsBinauralParametric);
            if (error != IPL.Error.Success)
                throw new InvalidOperationException("Failed to create parametric ambisonics binaural effect: " + error);

            var rotationSettings = new IPL.AmbisonicsRotationEffectSettings
            {
                MaxOrder = _reflectionOrder
            };

            error = IPL.AmbisonicsRotationEffectCreate(_ctx.Context, in audioSettings, in rotationSettings, out _ambisonicsRotationParametric);
            if (error != IPL.Error.Success)
                throw new InvalidOperationException("Failed to create parametric ambisonics rotation effect: " + error);

            var directSettingsMono = new IPL.DirectEffectSettings { NumChannels = 1 };
            error = IPL.DirectEffectCreate(_ctx.Context, in audioSettings, in directSettingsMono, out _directLeft);
            if (error != IPL.Error.Success)
                throw new InvalidOperationException("Failed to create direct effect: " + error);

            error = IPL.DirectEffectCreate(_ctx.Context, in audioSettings, in directSettingsMono, out _directRight);
            if (error != IPL.Error.Success)
                throw new InvalidOperationException("Failed to create direct effect: " + error);

            _reflectionParametric = CreateReflectionEffect(
                in audioSettings,
                IPL.ReflectionEffectType.Parametric,
                1,
                1,
                "parametric reflection effect");

            _mono = new float[_frameSize];
            _outL = new float[_frameSize];
            _outR = new float[_frameSize];
            _inLeft = new float[_frameSize];
            _inRight = new float[_frameSize];
            _directLeftSamples = new float[_frameSize];
            _directRightSamples = new float[_frameSize];
            _outLeftL = new float[_frameSize];
            _outLeftR = new float[_frameSize];
            _outRightL = new float[_frameSize];
            _outRightR = new float[_frameSize];
            _reverbAmbi = new float[_frameSize * _reflectionChannels];
            _reverbAmbiRotated = new float[_frameSize * _reflectionChannels];
            _reverbWetL = new float[_frameSize];
            _reverbWetR = new float[_frameSize];
        }

        public void Dispose()
        {
            if (_binauralLeft.Handle != IntPtr.Zero)
                IPL.BinauralEffectRelease(ref _binauralLeft);
            if (_binauralRight.Handle != IntPtr.Zero)
                IPL.BinauralEffectRelease(ref _binauralRight);
            if (_ambisonicsBinauralParametric.Handle != IntPtr.Zero)
                IPL.AmbisonicsBinauralEffectRelease(ref _ambisonicsBinauralParametric);
            if (_ambisonicsRotationParametric.Handle != IntPtr.Zero)
                IPL.AmbisonicsRotationEffectRelease(ref _ambisonicsRotationParametric);
            if (_directLeft.Handle != IntPtr.Zero)
                IPL.DirectEffectRelease(ref _directLeft);
            if (_directRight.Handle != IntPtr.Zero)
                IPL.DirectEffectRelease(ref _directRight);
            if (_reflectionParametric.Handle != IntPtr.Zero)
                IPL.ReflectionEffectRelease(ref _reflectionParametric);
        }
    }
}
