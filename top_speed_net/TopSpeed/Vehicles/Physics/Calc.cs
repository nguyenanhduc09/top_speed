using System;
using TopSpeed.Common;
using TopSpeed.Physics.Surface;

namespace TopSpeed.Vehicles
{
    internal partial class Car
    {
        private void GuardDynamicInputs()
        {
            if (!IsFinite(_speed))
                _speed = 0f;
            if (!IsFinite(_positionX))
                _positionX = 0f;
            if (!IsFinite(_positionY))
                _positionY = 0f;
            if (_positionY < 0f)
                _positionY = 0f;
        }

        private void ApplySurfaceModifiers()
        {
            var modifiers = SurfaceModel.Resolve(_surface, _surfaceTractionFactor, _deceleration);
            _currentSurfaceTractionFactor = modifiers.Traction;
            _currentDeceleration = modifiers.Deceleration;
            _currentSurfaceLateralMultiplier = modifiers.LateralSpeedMultiplier;
            _speedDiff = 0f;
        }

        private int ResolveThrust()
        {
            if (_currentThrottle == 0)
                return _currentBrake;
            if (_currentBrake == 0)
                return _currentThrottle;
            return -_currentBrake > _currentThrottle ? _currentBrake : _currentThrottle;
        }

        private void ApplyThrottleDrive(
            float elapsed,
            float speedMpsCurrent,
            float throttle,
            bool inReverse,
            bool reverseBlockedAtLapStart,
            float surfaceTractionMod,
            ref float longitudinalGripFactor)
        {
            if (reverseBlockedAtLapStart)
            {
                _speedDiff = 0f;
                _lastDriveRpm = 0f;
                return;
            }

            var tireOutput = SolveTireModel(elapsed, speedMpsCurrent, _currentSteering, surfaceTractionMod, 1f, commitState: false);
            longitudinalGripFactor = tireOutput.LongitudinalGripFactor;

            var driveRpm = CalculateDriveRpm(speedMpsCurrent, throttle);
            var engineTorque = CalculateEngineTorqueNm(driveRpm) * throttle * _powerFactor;
            var gearRatio = inReverse ? _reverseGearRatio : _engine.GetGearRatio(GetDriveGear());
            var wheelTorque = engineTorque * gearRatio * _finalDriveRatio * _drivetrainEfficiency;
            var wheelForce = wheelTorque / _wheelRadiusM;
            var tractionLimit = _tireGripCoefficient * surfaceTractionMod * _massKg * 9.80665f;
            if (wheelForce > tractionLimit)
                wheelForce = tractionLimit;
            wheelForce *= longitudinalGripFactor;
            wheelForce *= (_factor1 / 100f);
            if (inReverse)
                wheelForce *= _reversePowerFactor;

            var dragForce = 0.5f * 1.225f * _dragCoefficient * _frontalAreaM2 * speedMpsCurrent * speedMpsCurrent;
            var rollingForce = _rollingResistanceCoefficient * _massKg * 9.80665f;
            var netForce = wheelForce - dragForce - rollingForce;
            var accelMps2 = netForce / _massKg;
            var newSpeedMps = speedMpsCurrent + (accelMps2 * elapsed);
            if (newSpeedMps < 0f)
                newSpeedMps = 0f;

            _speedDiff = (newSpeedMps - speedMpsCurrent) * 3.6f;
            _lastDriveRpm = CalculateDriveRpm(newSpeedMps, throttle);
            if (_backfirePlayed)
                _backfirePlayed = false;
        }

        private void ApplyCoastDecel(float elapsed)
        {
            var surfaceDecelMod = _deceleration > 0f ? _currentDeceleration / _deceleration : 1.0f;
            var brakeInput = Math.Max(0f, Math.Min(100f, -_currentBrake)) / 100f;
            var brakeDecel = CalculateBrakeDecel(brakeInput, surfaceDecelMod);
            var engineBrakeDecel = CalculateEngineBrakingDecel(surfaceDecelMod);
            var totalDecel = _thrust < -10 ? (brakeDecel + engineBrakeDecel) : engineBrakeDecel;
            _speedDiff = -totalDecel * elapsed;
            _lastDriveRpm = 0f;
        }

        private void ClampSpeedAndTransmission(
            float elapsed,
            float throttle,
            bool inReverse,
            bool reverseBlockedAtLapStart,
            float surfaceTractionMod,
            float longitudinalGripFactor)
        {
            _speed += _speedDiff;
            if (_speed > _topSpeed)
                _speed = _topSpeed;
            if (_speed < 0f)
                _speed = 0f;
            if (!IsFinite(_speed))
            {
                _speed = 0f;
                _speedDiff = 0f;
            }

            if (!IsFinite(_lastDriveRpm))
                _lastDriveRpm = _idleRpm;

            if (reverseBlockedAtLapStart && _thrust > 10f)
            {
                _speed = 0f;
                _speedDiff = 0f;
                _lastDriveRpm = 0f;
            }

            if (inReverse)
            {
                var reverseMax = Math.Max(5.0f, _reverseMaxSpeedKph);
                if (_speed > reverseMax)
                    _speed = reverseMax;
                return;
            }

            if (_manualTransmission)
            {
                var gearMax = _engine.GetGearMaxSpeedKmh(_gear);
                if (_speed > gearMax)
                    _speed = gearMax;
            }
            else
            {
                UpdateAutomaticGear(elapsed, _speed / 3.6f, throttle, surfaceTractionMod, longitudinalGripFactor);
            }
        }

        private void SyncEngineFromSpeed(float elapsed)
        {
            _engine.SyncFromSpeed(_speed, GetDriveGear(), elapsed, _currentThrottle);
            if (_lastDriveRpm > 0f && _lastDriveRpm > _engine.Rpm)
                _engine.OverrideRpm(_lastDriveRpm);
        }

        private void UpdateBackfireStateAfterDrive()
        {
            if (_thrust > 0)
                return;

            if (!AnyBackfirePlaying() && !_backfirePlayed && Algorithm.RandomInt(5) == 1)
                PlayRandomBackfire();
            _backfirePlayed = true;
        }

        private void IntegrateVehiclePosition(float elapsed, float currentLapStart)
        {
            var speedMps = _speed / 3.6f;
            var longitudinalDelta = speedMps * elapsed;
            if (_gear == ReverseGear)
            {
                var nextPositionY = _positionY - longitudinalDelta;
                if (nextPositionY < currentLapStart)
                    nextPositionY = currentLapStart;
                if (nextPositionY < 0f)
                    nextPositionY = 0f;
                _positionY = nextPositionY;
            }
            else
            {
                _positionY += longitudinalDelta;
            }

            var surfaceTractionModLat = _surfaceTractionFactor > 0f ? _currentSurfaceTractionFactor / _surfaceTractionFactor : 1.0f;
            var tireOutput = SolveTireModel(elapsed, speedMps, _currentSteering, surfaceTractionModLat, _currentSurfaceLateralMultiplier);
            _positionX += tireOutput.LateralSpeedMps * elapsed;
        }
    }
}
