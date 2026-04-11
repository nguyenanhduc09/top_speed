using System;
using System.Threading;
using MiniAudioEx.Native;
using SteamAudio;

namespace TS.Audio
{
    internal sealed partial class SteamAudioSpatializer
    {
        private static float GetReflectionWetScale(AudioSourceSpatialParams spatial)
        {
            const float defaultWetScale = 0.35f;
            var wetScale = defaultWetScale;
            var roomFlags = Volatile.Read(ref spatial.RoomFlags);
            if ((roomFlags & AudioSourceSpatialParams.RoomHasProfile) != 0)
                wetScale *= Clamp01(Volatile.Read(ref spatial.RoomReverbGain));

            return wetScale;
        }

        private static bool IsStereoWideningEnabled(AudioSourceSpatialParams spatial)
        {
            return Volatile.Read(ref spatial.StereoWidening) != 0;
        }

        private static void GetStereoWideningGains(IPL.Vector3 direction, out float leftGain, out float rightGain)
        {
            const float fullCutoffAtDirectionX = 0.90f;
            var normalizedX = Clamp(direction.X / fullCutoffAtDirectionX, -1f, 1f);

            leftGain = normalizedX > 0f ? 1f - normalizedX : 1f;
            rightGain = normalizedX < 0f ? 1f + normalizedX : 1f;
        }

        private float GetAttenuationAndDirection(AudioSourceSpatialParams spatial, out IPL.Vector3 direction)
        {
            var sourcePos = new IPL.Vector3
            {
                X = Volatile.Read(ref spatial.PosX),
                Y = Volatile.Read(ref spatial.PosY),
                Z = Volatile.Read(ref spatial.PosZ)
            };

            var listener = _ctx.ListenerSnapshot;

            var worldDir = new IPL.Vector3
            {
                X = sourcePos.X - listener.Origin.X,
                Y = sourcePos.Y - listener.Origin.Y,
                Z = sourcePos.Z - listener.Origin.Z
            };

            float distance = (float)Math.Sqrt(worldDir.X * worldDir.X + worldDir.Y * worldDir.Y + worldDir.Z * worldDir.Z);
            if (distance > 0.0001f)
            {
                float inv = 1.0f / distance;
                worldDir.X *= inv;
                worldDir.Y *= inv;
                worldDir.Z *= inv;

                direction = new IPL.Vector3
                {
                    X = worldDir.X * listener.Right.X + worldDir.Y * listener.Right.Y + worldDir.Z * listener.Right.Z,
                    Y = worldDir.X * listener.Up.X + worldDir.Y * listener.Up.Y + worldDir.Z * listener.Up.Z,
                    Z = worldDir.X * listener.Ahead.X + worldDir.Y * listener.Ahead.Y + worldDir.Z * listener.Ahead.Z
                };
            }
            else
            {
                direction = new IPL.Vector3 { X = 0, Y = 0, Z = -1 };
            }

            float refDist = Volatile.Read(ref spatial.RefDistance);
            float maxDist = Volatile.Read(ref spatial.MaxDistance);
            float rolloff = Volatile.Read(ref spatial.RollOff);
            float attenuation = distance < refDist ? 1.0f : refDist / distance;
            return ApplyDistanceModel(distance, refDist, maxDist, rolloff, attenuation, spatial.DistanceModel);
        }

        private static float ApplyDistanceModel(float distance, float refDistance, float maxDistance, float rolloff, float steamAudioAttenuation, DistanceModel model)
        {
            if (model == DistanceModel.Inverse)
            {
                if (distance < refDistance)
                    distance = refDistance;
                if (maxDistance > refDistance && maxDistance < 100000000f && distance > maxDistance)
                    distance = maxDistance;

                return Clamp(steamAudioAttenuation, 0f, 1f);
            }

            if (maxDistance <= refDistance)
                maxDistance = refDistance + 0.0001f;

            distance = Clamp(distance, refDistance, maxDistance);
            float attenuation;

            switch (model)
            {
                case DistanceModel.Linear:
                    attenuation = 1f - rolloff * (distance - refDistance) / (maxDistance - refDistance);
                    break;
                case DistanceModel.Exponential:
                    attenuation = (float)Math.Pow(distance / refDistance, -rolloff);
                    break;
                default:
                    attenuation = steamAudioAttenuation;
                    break;
            }

            return Clamp(attenuation, 0f, 1f);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
        }

        private float DownmixSample(NativeArray<float> framesIn, int offset, int channels)
        {
            if (channels <= 1)
                return framesIn[offset];

            switch (_downmixMode)
            {
                case HrtfDownmixMode.Left:
                    return framesIn[offset];
                case HrtfDownmixMode.Right:
                    return framesIn[offset + 1];
                case HrtfDownmixMode.Sum:
                {
                    float sum = 0f;
                    for (int ch = 0; ch < channels; ch++)
                        sum += framesIn[offset + ch];
                    return sum;
                }
                case HrtfDownmixMode.Average:
                default:
                {
                    float sum = 0f;
                    for (int ch = 0; ch < channels; ch++)
                        sum += framesIn[offset + ch];
                    return sum / Math.Max(1, channels);
                }
            }
        }
    }
}
