using System.Threading;
using SteamAudio;

namespace TS.Audio
{
    public sealed partial class SteamAudioContext
    {
        private static unsafe void ApplyRoomOnlyOutputs(AudioSourceHandle handle)
        {
            var direct = new IPL.DirectEffectParams
            {
                Occlusion = 1f
            };
            direct.AirAbsorption[0] = 1f;
            direct.AirAbsorption[1] = 1f;
            direct.AirAbsorption[2] = 1f;
            direct.Transmission[0] = 1f;
            direct.Transmission[1] = 1f;
            direct.Transmission[2] = 1f;
            ApplyDirectOutputs(handle, in direct);

            var roomFlags = Volatile.Read(ref handle.SpatialParams.RoomFlags);
            var hasRoom = (roomFlags & AudioSourceSpatialParams.RoomHasProfile) != 0;
            if (!hasRoom)
                return;

            var reflections = new IPL.ReflectionEffectParams();
            ApplyReverbOutputs(handle, in reflections);
        }

        private static unsafe void ApplyDirectOutputs(AudioSourceHandle handle, in IPL.DirectEffectParams direct)
        {
            var spatial = handle.SpatialParams;
            var roomFlags = Volatile.Read(ref spatial.RoomFlags);
            var hasRoom = (roomFlags & AudioSourceSpatialParams.RoomHasProfile) != 0;

            float airLow = direct.AirAbsorption[0];
            float airMid = direct.AirAbsorption[1];
            float airHigh = direct.AirAbsorption[2];
            float transLow = direct.Transmission[0];
            float transMid = direct.Transmission[1];
            float transHigh = direct.Transmission[2];

            var occlusion = direct.Occlusion;
            var occlusionOverride = Volatile.Read(ref spatial.RoomOcclusionOverride);
            if (!float.IsNaN(occlusionOverride))
            {
                occlusion = Clamp01(occlusionOverride);
            }
            else if (hasRoom)
            {
                var scale = Clamp01(Volatile.Read(ref spatial.RoomOcclusionScale));
                occlusion = Lerp(1f, occlusion, scale);
            }

            var transOverrideLow = Volatile.Read(ref spatial.RoomTransmissionOverrideLow);
            var transOverrideMid = Volatile.Read(ref spatial.RoomTransmissionOverrideMid);
            var transOverrideHigh = Volatile.Read(ref spatial.RoomTransmissionOverrideHigh);
            if (!float.IsNaN(transOverrideLow) || !float.IsNaN(transOverrideMid) || !float.IsNaN(transOverrideHigh))
            {
                if (!float.IsNaN(transOverrideLow)) transLow = Clamp01(transOverrideLow);
                if (!float.IsNaN(transOverrideMid)) transMid = Clamp01(transOverrideMid);
                if (!float.IsNaN(transOverrideHigh)) transHigh = Clamp01(transOverrideHigh);
            }
            else if (hasRoom)
            {
                var scale = Clamp01(Volatile.Read(ref spatial.RoomTransmissionScale));
                transLow = Lerp(1f, transLow, scale);
                transMid = Lerp(1f, transMid, scale);
                transHigh = Lerp(1f, transHigh, scale);
            }

            var airOverrideLow = Volatile.Read(ref spatial.RoomAirAbsorptionOverrideLow);
            var airOverrideMid = Volatile.Read(ref spatial.RoomAirAbsorptionOverrideMid);
            var airOverrideHigh = Volatile.Read(ref spatial.RoomAirAbsorptionOverrideHigh);
            if (!float.IsNaN(airOverrideLow) || !float.IsNaN(airOverrideMid) || !float.IsNaN(airOverrideHigh))
            {
                if (!float.IsNaN(airOverrideLow)) airLow = Clamp01(airOverrideLow);
                if (!float.IsNaN(airOverrideMid)) airMid = Clamp01(airOverrideMid);
                if (!float.IsNaN(airOverrideHigh)) airHigh = Clamp01(airOverrideHigh);
            }
            else if (hasRoom)
            {
                var scale = Clamp01(Volatile.Read(ref spatial.RoomAirAbsorptionScale));
                airLow = Lerp(1f, airLow, scale);
                airMid = Lerp(1f, airMid, scale);
                airHigh = Lerp(1f, airHigh, scale);
            }

            handle.ApplyDirectSimulation(occlusion, airLow, airMid, airHigh, transLow, transMid, transHigh);
        }

        private static unsafe void ApplyReverbOutputs(AudioSourceHandle handle, in IPL.ReflectionEffectParams reflections)
        {
            var spatial = handle.SpatialParams;
            var roomFlags = Volatile.Read(ref spatial.RoomFlags);
            var hasRoom = (roomFlags & AudioSourceSpatialParams.RoomHasProfile) != 0;

            if (!hasRoom)
            {
                var timeLow = reflections.ReverbTimes[0];
                var timeMid = reflections.ReverbTimes[1];
                var timeHigh = reflections.ReverbTimes[2];
                var eqLow = reflections.Eq[0];
                var eqMid = reflections.Eq[1];
                var eqHigh = reflections.Eq[2];
                var delay = reflections.Delay;
                handle.ApplyReverbSimulation(timeLow, timeMid, timeHigh, eqLow, eqMid, eqHigh, delay);
                return;
            }

            var timeMidRoom = System.Math.Max(0f, Volatile.Read(ref spatial.RoomReverbTimeSeconds));
            var hfRatio = Clamp01(Volatile.Read(ref spatial.RoomHfDecayRatio));
            var roomTimeLow = timeMidRoom;
            var roomTimeMid = timeMidRoom;
            var roomTimeHigh = timeMidRoom * hfRatio;

            handle.ApplyReverbSimulation(roomTimeLow, roomTimeMid, roomTimeHigh, 1f, 1f, 1f, 0);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        private static float Lerp(float from, float to, float t)
        {
            return from + ((to - from) * t);
        }
    }
}
