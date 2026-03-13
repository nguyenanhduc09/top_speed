using System;

namespace TopSpeed.Physics.Tires
{
    internal static class TireStep
    {
        public static TireModelOutput Solve(
            in TireModelParameters parameters,
            in TireModelInput input,
            in TireModelState state,
            in TireSteerData steer,
            in TireAxleData axle)
        {
            var dt = Math.Max(0.0001f, input.ElapsedSeconds);
            var massKg = Math.Max(100f, parameters.MassKg);
            var wheelbase = Math.Max(0.5f, axle.Wheelbase);
            var trackWidth = Math.Max(0.45f, axle.TrackWidth);
            var yawInertia = massKg * ((wheelbase * wheelbase) + (trackWidth * trackWidth)) * 0.18f * Math.Max(0.5f, parameters.YawInertiaScale);
            var damping = Math.Max(0f, parameters.TransientDamping);
            var yaw = TireYaw.Resolve(parameters, steer, axle, massKg);
            var steerSign = TireModelMath.Sign(steer.SteerRad);
            var steerMag = TireModelMath.Clamp01(Math.Abs(input.SteeringInput) / 100f);
            // Recenter damping should only dominate when steering is truly near neutral.
            var neutralSteer = TireModelMath.Clamp01(1f - (steerMag * 4.0f));

            var vyDot = (axle.TotalForce / massKg) - (steer.ForwardSpeed * state.YawRateRad);
            var rDot = ((axle.A * axle.FrontForce) - (axle.B * axle.RearForce)) / yawInertia;
            rDot += (yaw.RateTarget - state.YawRateRad) * yaw.TrackGain;
            if (steerSign != 0f)
            {
                var yawSource = steerSign * Math.Abs(yaw.RateTarget) * yaw.SourceGain;
                rDot += yawSource;

                // Preserve classic "tap steer then release" feel by steering lateral velocity toward input while active.
                var lateralCommandMps = steerSign * steer.ForwardSpeed * steerMag * (0.14f + (0.26f * yaw.SpeedSharpness));
                var lateralTrackGain = 2.2f + (3.2f * steerMag);
                vyDot += (lateralCommandMps - state.LateralVelocityMps) * lateralTrackGain;
            }

            vyDot -= state.LateralVelocityMps * ((damping * 0.75f) + (0.85f * neutralSteer));
            rDot -= state.YawRateRad * ((damping * 0.55f) + (0.14f * yaw.SpeedSharpness) + (1.00f * neutralSteer));

            var nextVy = state.LateralVelocityMps + (vyDot * dt);
            var nextYawRate = state.YawRateRad + (rDot * dt);

            var neutralInput = Math.Abs(input.SteeringInput) <= 4;
            if (neutralInput)
            {
                // Fast recenter for legacy "release stops steering" feel.
                var lateralDecay = Math.Max(0f, 1f - (24f * dt));
                var yawDecay = Math.Max(0f, 1f - (28f * dt));
                nextVy *= lateralDecay;
                nextYawRate *= yawDecay;
                if (Math.Abs(nextVy) < 0.03f)
                    nextVy = 0f;
                if (Math.Abs(nextYawRate) < 0.02f)
                    nextYawRate = 0f;
            }

            nextVy = TireModelMath.Clamp(nextVy, -steer.ForwardSpeed * 1.6f, steer.ForwardSpeed * 1.6f);
            nextYawRate = TireModelMath.Clamp(nextYawRate, -5f, 5f);

            // Ensure steering direction is stable across the full speed range.
            var desiredDirection = TireModelMath.Sign(input.SteeringInput);
            if (desiredDirection != 0f && steer.SpeedMps > 1f)
            {
                if (TireModelMath.Sign(nextVy) != desiredDirection)
                    nextVy = desiredDirection * Math.Abs(nextVy);
                if (TireModelMath.Sign(nextYawRate) != desiredDirection)
                    nextYawRate = desiredDirection * Math.Abs(nextYawRate);
            }

            var combinedPenalty = TireModelMath.Clamp(parameters.CombinedGripPenalty, 0f, 1f);
            var lateralLoad = axle.LateralForceRatio * combinedPenalty;
            var longitudinalGripFactor = TireModelMath.Clamp(1f - ((lateralLoad * lateralLoad) * 0.6f), 0.35f, 1f);

            var massRatio = (float)Math.Sqrt(1500f / massKg);
            var agilityMassScale = TireModelMath.Lerp(1f, TireModelMath.Clamp(massRatio, 0.5f, 2.5f), TireModelMath.Clamp01(parameters.MassSensitivity));
            var stabilityPenalty = parameters.HighSpeedStability * steer.SpeedNorm;
            stabilityPenalty *= TireModelMath.Lerp(1f, 1.35f, TireModelMath.Clamp01(1f / Math.Max(0.5f, agilityMassScale) - 0.5f));
            var stabilityScale = TireModelMath.Clamp(1f - stabilityPenalty, 0.5f, 1f);

            var responseScale = Math.Max(0.2f, parameters.TurnResponse) * stabilityScale;
            var directSteer = 0f;
            if (steerSign != 0f)
            {
                // Immediate steering authority term so normal-speed turning is responsive.
                var directSteerGain = 0.16f + (0.24f * yaw.SpeedSharpness);
                directSteer = steerSign * steer.ForwardSpeed * steerMag * directSteerGain * Math.Max(0.45f, parameters.TurnResponse);
            }

            var lateralSpeedMps = (nextVy * responseScale + directSteer) * input.SurfaceLateralMultiplier;
            var nextState = new TireModelState(nextVy, nextYawRate);
            return new TireModelOutput(longitudinalGripFactor, lateralSpeedMps, nextState);
        }
    }
}
