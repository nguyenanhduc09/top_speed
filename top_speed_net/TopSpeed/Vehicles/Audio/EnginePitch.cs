using System;

namespace TopSpeed.Vehicles
{
    internal static class EnginePitch
    {
        public static int FromRpm(
            float rpm,
            float idleRpm,
            float revLimiter,
            int idleFreq,
            int topFreq,
            float pitchCurveExponent)
        {
            var safeIdleRpm = Math.Max(1f, idleRpm);
            var safeRevLimiter = Math.Max(safeIdleRpm + 1f, revLimiter);
            var rpmNormalized = (rpm - safeIdleRpm) / (safeRevLimiter - safeIdleRpm);
            if (rpmNormalized < 0f)
                rpmNormalized = 0f;
            else if (rpmNormalized > 1f)
                rpmNormalized = 1f;

            var exponent = VehicleDefinition.ClampPitchCurveExponent(pitchCurveExponent);
            var curved = (float)Math.Pow(rpmNormalized, exponent);
            var minFrequency = Math.Min(idleFreq, topFreq);
            var maxFrequency = Math.Max(idleFreq, topFreq);
            if (maxFrequency <= minFrequency)
                return minFrequency;

            var frequency = minFrequency + (int)Math.Round(curved * (maxFrequency - minFrequency));
            if (frequency < minFrequency)
                return minFrequency;
            return frequency > maxFrequency ? maxFrequency : frequency;
        }
    }
}
