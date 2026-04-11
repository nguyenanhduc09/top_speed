using System;
using System.Threading;
using MiniAudioEx.Native;
using SteamAudio;

namespace TS.Audio
{
    internal sealed partial class SteamAudioSpatializer
    {
        public unsafe void Process(NativeArray<float> framesIn, UInt32 frameCountIn, NativeArray<float> framesOut, ref UInt32 frameCountOut, UInt32 channels, AudioSourceSpatialParams spatial)
        {
            if (!_trueStereo || channels < 2)
            {
                ProcessMono(framesIn, frameCountIn, framesOut, ref frameCountOut, channels, spatial);
                return;
            }

            int frames = (int)Math.Min(frameCountIn, (uint)_frameSize);

            fixed (float* pInL = _inLeft)
            fixed (float* pInR = _inRight)
            fixed (float* pDirL = _directLeftSamples)
            fixed (float* pDirR = _directRightSamples)
            fixed (float* pOutLL = _outLeftL)
            fixed (float* pOutLR = _outLeftR)
            fixed (float* pOutRL = _outRightL)
            fixed (float* pOutRR = _outRightR)
            fixed (float* pMono = _mono)
            fixed (float* pReverbL = _reverbWetL)
            fixed (float* pReverbR = _reverbWetR)
            {
                for (int i = 0; i < frames; i++)
                {
                    int idx = i * (int)channels;
                    pInL[i] = framesIn[idx];
                    pInR[i] = framesIn[idx + 1];
                    pMono[i] = 0.5f * (pInL[i] + pInR[i]);
                    pReverbL[i] = 0f;
                    pReverbR[i] = 0f;
                }

                var attenuation = GetAttenuationAndDirection(spatial, out var direction);
                var directParams = CreateDirectParams(spatial, attenuation);
                var binauralParams = CreateBinauralParams(direction);

                var inPtrL = stackalloc IntPtr[1];
                var dirPtrL = stackalloc IntPtr[1];
                var outPtrL = stackalloc IntPtr[2];
                inPtrL[0] = (IntPtr)pInL;
                dirPtrL[0] = (IntPtr)pDirL;
                outPtrL[0] = (IntPtr)pOutLL;
                outPtrL[1] = (IntPtr)pOutLR;

                var inBufferL = new IPL.AudioBuffer { NumChannels = 1, NumSamples = frames, Data = (IntPtr)inPtrL };
                var dirBufferL = new IPL.AudioBuffer { NumChannels = 1, NumSamples = frames, Data = (IntPtr)dirPtrL };
                var outBufferL = new IPL.AudioBuffer { NumChannels = 2, NumSamples = frames, Data = (IntPtr)outPtrL };

                IPL.DirectEffectApply(_directLeft, ref directParams, ref inBufferL, ref dirBufferL);
                IPL.BinauralEffectApply(_binauralLeft, ref binauralParams, ref dirBufferL, ref outBufferL);

                var inPtrR = stackalloc IntPtr[1];
                var dirPtrR = stackalloc IntPtr[1];
                var outPtrR = stackalloc IntPtr[2];
                inPtrR[0] = (IntPtr)pInR;
                dirPtrR[0] = (IntPtr)pDirR;
                outPtrR[0] = (IntPtr)pOutRL;
                outPtrR[1] = (IntPtr)pOutRR;

                var inBufferR = new IPL.AudioBuffer { NumChannels = 1, NumSamples = frames, Data = (IntPtr)inPtrR };
                var dirBufferR = new IPL.AudioBuffer { NumChannels = 1, NumSamples = frames, Data = (IntPtr)dirPtrR };
                var outBufferR = new IPL.AudioBuffer { NumChannels = 2, NumSamples = frames, Data = (IntPtr)outPtrR };

                IPL.DirectEffectApply(_directRight, ref directParams, ref inBufferR, ref dirBufferR);
                IPL.BinauralEffectApply(_binauralRight, ref binauralParams, ref dirBufferR, ref outBufferR);

                const float mixScale = 0.5f;
                if (IsStereoWideningEnabled(spatial))
                {
                    GetStereoWideningGains(direction, out var leftGain, out var rightGain);
                    for (int i = 0; i < frames; i++)
                    {
                        int idx = i * (int)channels;
                        framesOut[idx] = (pOutLL[i] + pOutRL[i]) * mixScale * leftGain;
                        framesOut[idx + 1] = (pOutLR[i] + pOutRR[i]) * mixScale * rightGain;
                        for (int ch = 2; ch < channels; ch++)
                            framesOut[idx + ch] = 0f;
                    }
                }
                else
                {
                    for (int i = 0; i < frames; i++)
                    {
                        int idx = i * (int)channels;
                        framesOut[idx] = (pOutLL[i] + pOutRL[i]) * mixScale;
                        framesOut[idx + 1] = (pOutLR[i] + pOutRR[i]) * mixScale;
                        for (int ch = 2; ch < channels; ch++)
                            framesOut[idx + ch] = 0f;
                    }
                }

                ApplyWetReflections(frames, channels, spatial, framesOut, pReverbL, pReverbR);
                frameCountOut = (UInt32)frames;
            }
        }

        private unsafe void ProcessMono(NativeArray<float> framesIn, UInt32 frameCountIn, NativeArray<float> framesOut, ref UInt32 frameCountOut, UInt32 channels, AudioSourceSpatialParams spatial)
        {
            int frames = (int)Math.Min(frameCountIn, (uint)_frameSize);

            fixed (float* pMono = _mono)
            fixed (float* pOutL = _outL)
            fixed (float* pOutR = _outR)
            fixed (float* pReverbL = _reverbWetL)
            fixed (float* pReverbR = _reverbWetR)
            {
                int chCount = (int)channels;
                for (int i = 0; i < frames; i++)
                {
                    int idx = i * chCount;
                    pMono[i] = DownmixSample(framesIn, idx, chCount);
                    pReverbL[i] = 0f;
                    pReverbR[i] = 0f;
                }

                var attenuation = GetAttenuationAndDirection(spatial, out var direction);
                var directParams = CreateDirectParams(spatial, attenuation);
                var binauralParams = CreateBinauralParams(direction);

                var inputPtr = stackalloc IntPtr[1];
                var outputPtr = stackalloc IntPtr[2];
                inputPtr[0] = (IntPtr)pMono;
                outputPtr[0] = (IntPtr)pOutL;
                outputPtr[1] = (IntPtr)pOutR;

                var inputBuffer = new IPL.AudioBuffer
                {
                    NumChannels = 1,
                    NumSamples = frames,
                    Data = (IntPtr)inputPtr
                };

                var outputBuffer = new IPL.AudioBuffer
                {
                    NumChannels = 2,
                    NumSamples = frames,
                    Data = (IntPtr)outputPtr
                };

                IPL.DirectEffectApply(_directLeft, ref directParams, ref inputBuffer, ref inputBuffer);
                IPL.BinauralEffectApply(_binauralLeft, ref binauralParams, ref inputBuffer, ref outputBuffer);

                if (IsStereoWideningEnabled(spatial))
                {
                    GetStereoWideningGains(direction, out var leftGain, out var rightGain);
                    for (int i = 0; i < frames; i++)
                    {
                        int idx = i * (int)channels;
                        framesOut[idx] = pOutL[i] * leftGain;
                        if (channels > 1)
                            framesOut[idx + 1] = pOutR[i] * rightGain;
                        for (int ch = 2; ch < channels; ch++)
                            framesOut[idx + ch] = 0f;
                    }
                }
                else
                {
                    for (int i = 0; i < frames; i++)
                    {
                        int idx = i * (int)channels;
                        framesOut[idx] = pOutL[i];
                        if (channels > 1)
                            framesOut[idx + 1] = pOutR[i];
                        for (int ch = 2; ch < channels; ch++)
                            framesOut[idx + ch] = 0f;
                    }
                }

                ApplyWetReflections(frames, channels, spatial, framesOut, pReverbL, pReverbR);
                frameCountOut = (UInt32)frames;
            }
        }

        private unsafe void ApplyWetReflections(int frames, UInt32 channels, AudioSourceSpatialParams spatial, NativeArray<float> framesOut, float* pReverbL, float* pReverbR)
        {
            var simFlags = Volatile.Read(ref spatial.SimulationFlags);
            if ((simFlags & AudioSourceSpatialParams.SimReflections) != 0)
            {
                ApplyReflections(frames, spatial, pReverbL, pReverbR);
                var wetScale = GetReflectionWetScale(spatial);
                for (int i = 0; i < frames; i++)
                {
                    int idx = i * (int)channels;
                    _reflectionWet += (wetScale - _reflectionWet) * 0.05f;
                    framesOut[idx] += pReverbL[i] * _reflectionWet;
                    if (channels > 1)
                        framesOut[idx + 1] += pReverbR[i] * _reflectionWet;
                }
            }
            else
            {
                _reflectionWet *= 0.90f;
            }
        }

        private unsafe IPL.DirectEffectParams CreateDirectParams(AudioSourceSpatialParams spatial, float attenuation)
        {
            var directParams = new IPL.DirectEffectParams
            {
                Flags = IPL.DirectEffectFlags.ApplyDistanceAttenuation,
                TransmissionType = IPL.TransmissionType.FrequencyDependent,
                DistanceAttenuation = attenuation,
                Directivity = 1.0f
            };

            var simFlags = Volatile.Read(ref spatial.SimulationFlags);
            if ((simFlags & AudioSourceSpatialParams.SimAirAbsorption) != 0)
            {
                directParams.Flags |= IPL.DirectEffectFlags.ApplyAirAbsorption;
                directParams.AirAbsorption[0] = Volatile.Read(ref spatial.AirAbsLow);
                directParams.AirAbsorption[1] = Volatile.Read(ref spatial.AirAbsMid);
                directParams.AirAbsorption[2] = Volatile.Read(ref spatial.AirAbsHigh);
            }
            else
            {
                directParams.AirAbsorption[0] = 1.0f;
                directParams.AirAbsorption[1] = 1.0f;
                directParams.AirAbsorption[2] = 1.0f;
            }

            if ((simFlags & AudioSourceSpatialParams.SimOcclusion) != 0)
            {
                directParams.Flags |= IPL.DirectEffectFlags.ApplyOcclusion;
                directParams.Occlusion = Volatile.Read(ref spatial.Occlusion);
            }

            if ((simFlags & AudioSourceSpatialParams.SimTransmission) != 0)
            {
                directParams.Flags |= IPL.DirectEffectFlags.ApplyTransmission;
                directParams.Transmission[0] = Volatile.Read(ref spatial.TransLow);
                directParams.Transmission[1] = Volatile.Read(ref spatial.TransMid);
                directParams.Transmission[2] = Volatile.Read(ref spatial.TransHigh);
            }
            else
            {
                directParams.Transmission[0] = 0.0f;
                directParams.Transmission[1] = 0.0f;
                directParams.Transmission[2] = 0.0f;
            }

            return directParams;
        }

        private IPL.BinauralEffectParams CreateBinauralParams(IPL.Vector3 direction)
        {
            return new IPL.BinauralEffectParams
            {
                Direction = direction,
                Interpolation = IPL.HrtfInterpolation.Bilinear,
                SpatialBlend = 1.0f,
                Hrtf = _ctx.Hrtf,
                PeakDelays = IntPtr.Zero
            };
        }
    }
}
