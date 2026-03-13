using System;

namespace TopSpeed.Physics.Tires
{
    internal readonly struct TireYawData
    {
        public TireYawData(float rateTarget, float trackGain, float sourceGain, float speedSharpness)
        {
            RateTarget = rateTarget;
            TrackGain = trackGain;
            SourceGain = sourceGain;
            SpeedSharpness = speedSharpness;
        }

        public float RateTarget { get; }
        public float TrackGain { get; }
        public float SourceGain { get; }
        public float SpeedSharpness { get; }
    }

    internal static class TireYaw
    {
        public static TireYawData Resolve(in TireModelParameters parameters, in TireSteerData steer, in TireAxleData axle, float massKg)
        {
            var wheelbase = Math.Max(0.5f, axle.Wheelbase);
            var trackWidth = Math.Max(0.45f, axle.TrackWidth);
            var sharpSpeedT = TireModelMath.Clamp01((steer.SpeedKph - 160f) / 80f);
            var massSharpness = TireModelMath.Clamp((float)Math.Sqrt(1300f / Math.Max(100f, massKg)), 0.55f, 1.8f);
            var wheelSharpness = TireModelMath.Clamp(2.2f / Math.Max(0.8f, wheelbase + (trackWidth * 0.35f)), 0.7f, 1.6f);
            var sharpBoost = TireModelMath.Lerp(1f, 1f + (0.32f * massSharpness * wheelSharpness), sharpSpeedT);

            var yawRateTarget = steer.ForwardSpeed / wheelbase * (float)Math.Tan(steer.SteerRad);
            yawRateTarget *= sharpBoost;

            var trackGain = TireModelMath.Lerp(1.8f, 4.6f, sharpSpeedT) * Math.Max(0.2f, parameters.TurnResponse);
            var steerNorm = TireModelMath.Clamp01(Math.Abs(steer.SteerRad) / TireModelMath.DegToRad(Math.Max(1f, parameters.MaxSteerDeg)));
            var sourceGain = TireModelMath.Lerp(0.24f, 0.74f, sharpSpeedT) * TireModelMath.Lerp(0.35f, 1.0f, steerNorm);

            return new TireYawData(yawRateTarget, trackGain, sourceGain, sharpSpeedT);
        }
    }
}
