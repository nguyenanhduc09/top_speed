using System;

namespace TopSpeed.Input
{
    internal sealed partial class DriveInput
    {
        public void SetTouchInputState(
            int steering,
            int throttle,
            int brake,
            int clutch,
            bool horn,
            bool gearUp,
            bool gearDown,
            bool startEngine)
        {
            _touchSteering = ClampRange(steering, -100, 100);
            _touchThrottle = ClampRange(throttle, 0, 100);
            _touchBrake = ClampRange(brake, -100, 0);
            _touchClutch = ClampRange(clutch, 0, 100);
            _touchHorn = horn;
            _touchGearUp = gearUp;
            _touchGearDown = gearDown;
            _touchStartEngine = startEngine;
        }

        public void ClearTouchInputState()
        {
            _touchSteering = 0;
            _touchThrottle = 0;
            _touchBrake = 0;
            _touchClutch = 0;
            _touchHorn = false;
            _touchGearUp = false;
            _touchGearDown = false;
            _touchStartEngine = false;
        }

        private static int ClampRange(int value, int min, int max)
        {
            if (value < min)
                return min;
            return value > max ? max : value;
        }
    }
}
