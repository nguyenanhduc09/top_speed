using System;

namespace TopSpeed.Physics.Tires
{
    internal readonly struct TireGripData
    {
        public TireGripData(float gripForce)
        {
            GripForce = gripForce;
        }

        public float GripForce { get; }
    }

    internal static class TireGrip
    {
        public static TireGripData Resolve(in TireModelParameters parameters, in TireModelInput input, in TireSteerData steer)
        {
            var aeroGripScale = 1f + (Math.Max(0f, parameters.DownforceGripGain) * steer.SpeedNorm * steer.SpeedNorm);
            var baseGrip = parameters.TireGripCoefficient * input.SurfaceTractionMod * parameters.LateralGripCoefficient;
            var massKg = Math.Max(100f, parameters.MassKg);
            var gripForce = Math.Max(0f, baseGrip * TireModelConstants.Gravity * massKg * aeroGripScale);
            return new TireGripData(gripForce);
        }
    }
}
