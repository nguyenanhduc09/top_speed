using TopSpeed.Input;
using TS.Sdl.Input;

namespace TopSpeed.Game
{
    internal sealed partial class Game
    {
        private void UpdateDriveTouchControls(float deltaSeconds)
        {
            if (!_isAndroidPlatform || !IsRaceState(_state))
            {
                DisableDriveTouchControls();
                return;
            }

            EnsureDriveTouchZones();

            var useMotionSteering = _settings.AndroidUseMotionSteering;
            SyncMotionSteeringMode(useMotionSteering);

            var steering = useMotionSteering ? ReadMotionSteering(deltaSeconds) : 0;
            var throttle = 0;
            var brake = 0;
            var clutch = 0;
            var horn = false;

            if (TryReadVehicleTouch(out var vehicleTouch))
            {
                var deltaX = vehicleTouch.X - vehicleTouch.StartX;
                var deltaY = vehicleTouch.Y - vehicleTouch.StartY;
                ApplyVehicleAxes(useMotionSteering, deltaX, deltaY, ref steering, ref throttle, ref brake);
            }

            var gearUp = _input.WasZoneGesturePressed(GestureIntent.TwoFingerSwipeUp, DriveTouchVehicleZoneId);
            var gearDown = _input.WasZoneGesturePressed(GestureIntent.TwoFingerSwipeDown, DriveTouchVehicleZoneId);
            var startEngine = _input.WasZoneGesturePressed(GestureIntent.DoubleTap, DriveTouchVehicleZoneId);
            ApplyTopZoneInputs(ref clutch, ref horn);

            _driveInput.SetTouchInputState(
                steering,
                throttle,
                brake,
                clutch,
                horn,
                gearUp,
                gearDown,
                startEngine);
        }

        private void EnsureDriveTouchZones()
        {
            if (_driveTouchZonesApplied)
                return;

            _input.SetTouchZones(TouchZoneLayout.Horizontal(
                DriveTouchInfoZoneId,
                DriveTouchVehicleZoneId,
                splitY: DriveTouchSplitY,
                topPriority: 20,
                bottomPriority: 20,
                topBehavior: TouchZoneBehavior.Lock,
                bottomBehavior: TouchZoneBehavior.Lock));
            _driveTouchZonesApplied = true;
        }

        private void DisableDriveTouchControls()
        {
            if (_driveTouchZonesApplied)
            {
                _input.ClearTouchZones();
                _driveTouchZonesApplied = false;
            }

            _driveGyroSteering = 0f;
            _driveGyroBias = 0f;
            _driveGyroRate = 0f;
            _driveGyroGravityX = 0f;
            _driveGyroGravityY = 0f;
            _driveGyroGravityZ = 0f;
            _driveGyroNeutralAngle = 0f;
            _driveGyroOrientationReady = false;
            _driveDisplayOrientation = TS.Sdl.Video.DisplayOrientation.Unknown;
            _driveMotionSteering = 0f;
            _driveMotionNeedsRecenter = true;
            _driveMotionEnabled = false;
            _driveTouchClutchArmed = false;
            _driveInput.ClearTouchInputState();
        }

        private void SyncMotionSteeringMode(bool useMotionSteering)
        {
            if (useMotionSteering && !_driveMotionEnabled)
            {
                _driveMotionEnabled = true;
                _driveMotionNeedsRecenter = true;
                _driveMotionSteering = 0f;
            }
            else if (!useMotionSteering && _driveMotionEnabled)
            {
                _driveMotionEnabled = false;
                _driveMotionNeedsRecenter = true;
                _driveMotionSteering = 0f;
            }
        }

        private bool TryReadVehicleTouch(out TouchZoneState touch)
        {
            return _input.TryGetTouchZoneState(DriveTouchVehicleZoneId, out touch) &&
                touch.IsActive &&
                touch.FingerCount == 1;
        }

        private void ApplyVehicleAxes(
            bool useMotionSteering,
            float deltaX,
            float deltaY,
            ref int steering,
            ref int throttle,
            ref int brake)
        {
            if (deltaX > 0f)
            {
                throttle = ScalePercent(deltaX, DriveTouchAxisTravel);
                brake = 0;
            }
            else if (deltaX < 0f)
            {
                throttle = 0;
                brake = -ScalePercent(-deltaX, DriveTouchAxisTravel);
            }

            if (useMotionSteering)
                return;

            if (deltaY < 0f)
                steering = -ScalePercent(-deltaY, DriveTouchAxisTravel);
            else if (deltaY > 0f)
                steering = ScalePercent(deltaY, DriveTouchAxisTravel);
        }

        private void ApplyTopZoneInputs(ref int clutch, ref bool horn)
        {
            var topTouchActive = _input.TryGetTouchZoneState(DriveTouchInfoZoneId, out var infoTouch) &&
                infoTouch.IsActive &&
                infoTouch.FingerCount == 1;
            var clutchGesture = _input.WasZoneGesturePressed(GestureIntent.DoubleTap, DriveTouchInfoZoneId);

            if (topTouchActive && clutchGesture)
                _driveTouchClutchArmed = true;
            if (!topTouchActive)
                _driveTouchClutchArmed = false;

            if (topTouchActive && _driveTouchClutchArmed)
                clutch = 100;
            else if (topTouchActive)
                horn = true;
        }
    }
}
