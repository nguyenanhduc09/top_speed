using System;
using System.Threading;
using SteamAudio;

namespace TS.Audio
{
    internal sealed partial class SteamAudioSpatializer
    {
        private unsafe void ApplyReflections(int frames, AudioSourceSpatialParams spatial, float* outL, float* outR)
        {
            var timeLow = Volatile.Read(ref spatial.ReverbTimeLow);
            var timeMid = Volatile.Read(ref spatial.ReverbTimeMid);
            var timeHigh = Volatile.Read(ref spatial.ReverbTimeHigh);
            var eqLow = Volatile.Read(ref spatial.ReverbEqLow);
            var eqMid = Volatile.Read(ref spatial.ReverbEqMid);
            var eqHigh = Volatile.Read(ref spatial.ReverbEqHigh);
            var delay = Volatile.Read(ref spatial.ReverbDelay);
            if (RenderReflectionsPass(
                frames,
                IPL.ReflectionEffectType.Parametric,
                delay,
                timeLow,
                timeMid,
                timeHigh,
                eqLow,
                eqMid,
                eqHigh,
                outL,
                outR))
            {
                return;
            }

            for (int i = 0; i < frames; i++)
            {
                outL[i] = 0f;
                outR[i] = 0f;
            }
        }

        private unsafe bool RenderReflectionsPass(
            int frames,
            IPL.ReflectionEffectType effectType,
            int delay,
            float timeLow,
            float timeMid,
            float timeHigh,
            float eqLow,
            float eqMid,
            float eqHigh,
            float* outL,
            float* outR)
        {
            if (effectType != IPL.ReflectionEffectType.Parametric)
                return false;

            var reflection = _reflectionParametric;
            if (reflection.Handle == IntPtr.Zero)
                return false;

            const int channelsToRender = 1;
            const int order = 0;

            var reflectionParams = new IPL.ReflectionEffectParams
            {
                Type = effectType,
                NumChannels = 1,
                IrSize = 0,
                Ir = default,
                Delay = delay
            };

            reflectionParams.ReverbTimes[0] = timeLow;
            reflectionParams.ReverbTimes[1] = timeMid;
            reflectionParams.ReverbTimes[2] = timeHigh;
            reflectionParams.Eq[0] = eqLow;
            reflectionParams.Eq[1] = eqMid;
            reflectionParams.Eq[2] = eqHigh;

            fixed (float* pIn = _mono)
            fixed (float* pAmbi = _reverbAmbi)
            fixed (float* pAmbiRot = _reverbAmbiRotated)
            {
                var rotationEffect = _ambisonicsRotationParametric;
                var binauralEffect = _ambisonicsBinauralParametric;

                for (int ch = 0; ch < channelsToRender; ch++)
                {
                    float* chPtr = pAmbi + ch * _frameSize;
                    for (int i = 0; i < frames; i++)
                        chPtr[i] = 0f;
                }
                for (int i = 0; i < frames; i++)
                {
                    outL[i] = 0f;
                    outR[i] = 0f;
                }

                var inPtr = stackalloc IntPtr[1];
                inPtr[0] = (IntPtr)pIn;
                var outPtr = stackalloc IntPtr[channelsToRender];
                for (int ch = 0; ch < channelsToRender; ch++)
                    outPtr[ch] = (IntPtr)(pAmbi + ch * _frameSize);

                var inBuffer = new IPL.AudioBuffer { NumChannels = 1, NumSamples = frames, Data = (IntPtr)inPtr };
                var outBuffer = new IPL.AudioBuffer { NumChannels = channelsToRender, NumSamples = frames, Data = (IntPtr)outPtr };

                IPL.ReflectionEffectApply(reflection, ref reflectionParams, ref inBuffer, ref outBuffer, default);

                var rotOutPtr = stackalloc IntPtr[channelsToRender];
                for (int ch = 0; ch < channelsToRender; ch++)
                    rotOutPtr[ch] = (IntPtr)(pAmbiRot + ch * _frameSize);

                var rotOutBuffer = new IPL.AudioBuffer { NumChannels = channelsToRender, NumSamples = frames, Data = (IntPtr)rotOutPtr };
                var listener = _ctx.ListenerSnapshot;
                var rotationParams = new IPL.AmbisonicsRotationEffectParams
                {
                    Orientation = new IPL.CoordinateSpace3
                    {
                        Right = listener.Right,
                        Up = listener.Up,
                        Ahead = listener.Ahead,
                        Origin = listener.Origin
                    },
                    Order = order
                };

                IPL.AmbisonicsRotationEffectApply(rotationEffect, ref rotationParams, ref outBuffer, ref rotOutBuffer);

                var ambiParams = new IPL.AmbisonicsBinauralEffectParams
                {
                    Hrtf = _ctx.Hrtf,
                    Order = order
                };

                var ambiOutPtr = stackalloc IntPtr[2];
                ambiOutPtr[0] = (IntPtr)outL;
                ambiOutPtr[1] = (IntPtr)outR;
                var ambiOutBuffer = new IPL.AudioBuffer { NumChannels = 2, NumSamples = frames, Data = (IntPtr)ambiOutPtr };

                IPL.AmbisonicsBinauralEffectApply(binauralEffect, ref ambiParams, ref rotOutBuffer, ref ambiOutBuffer);
            }

            return true;
        }

        private IPL.ReflectionEffect CreateReflectionEffect(
            in IPL.AudioSettings audioSettings,
            IPL.ReflectionEffectType type,
            int numChannels,
            int irSize,
            string label)
        {
            var settings = new IPL.ReflectionEffectSettings
            {
                Type = type,
                NumChannels = Math.Max(1, numChannels),
                IrSize = Math.Max(1, irSize)
            };

            var error = IPL.ReflectionEffectCreate(_ctx.Context, in audioSettings, in settings, out var effect);
            if (error != IPL.Error.Success)
                throw new InvalidOperationException($"Failed to create {label}: {error}");

            return effect;
        }
    }
}
