using System;
using TopSpeed.Protocol;
using TopSpeed.Vehicles;

namespace TopSpeed.Bots
{
    public static class BotPhysicsCatalog
    {
        public static BotPhysicsConfig Get(CarType car)
        {
            if (car == CarType.CustomVehicle)
                car = CarType.Vehicle1;

            var index = (int)car;
            var spec = OfficialVehicleCatalog.Get(index);
            return Create(spec);
        }

        private static BotPhysicsConfig Create(OfficialVehicleSpec spec)
        {
            var wheelRadiusM = Math.Max(0.01f, spec.TireCircumferenceM / (2.0f * (float)Math.PI));

            return new BotPhysicsConfig(
                spec.SurfaceTractionFactor,
                spec.Deceleration,
                spec.TopSpeed,
                spec.MassKg,
                spec.DrivetrainEfficiency,
                spec.EngineBrakingTorqueNm,
                spec.TireGripCoefficient,
                spec.BrakeStrength,
                wheelRadiusM,
                spec.EngineBraking,
                spec.IdleRpm,
                spec.RevLimiter,
                spec.FinalDriveRatio,
                spec.PowerFactor,
                spec.PeakTorqueNm,
                spec.PeakTorqueRpm,
                spec.IdleTorqueNm,
                spec.RedlineTorqueNm,
                spec.DragCoefficient,
                spec.FrontalAreaM2,
                spec.RollingResistanceCoefficient,
                spec.LaunchRpm,
                spec.LateralGripCoefficient,
                spec.HighSpeedStability,
                spec.WheelbaseM,
                spec.WidthM,
                spec.LengthM,
                spec.MaxSteerDeg,
                spec.Steering,
                spec.HighSpeedSteerGain,
                spec.HighSpeedSteerStartKph,
                spec.HighSpeedSteerFullKph,
                spec.CombinedGripPenalty,
                spec.SlipAnglePeakDeg,
                spec.SlipAngleFalloff,
                spec.TurnResponse,
                spec.MassSensitivity,
                spec.DownforceGripGain,
                spec.CornerStiffnessFront,
                spec.CornerStiffnessRear,
                spec.YawInertiaScale,
                spec.SteeringCurve,
                spec.TransientDamping,
                spec.Gears,
                spec.GearRatios,
                spec.TransmissionPolicy);
        }
    }
}
