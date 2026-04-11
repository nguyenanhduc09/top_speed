using TopSpeed.Input;

namespace TopSpeed.Vehicles.Control
{
    internal sealed class FinishLockInputController : ICarController
    {
        private readonly DriveInput _input;

        public FinishLockInputController(DriveInput input)
        {
            _input = input;
        }

        public CarControlIntent ReadIntent(in CarControlContext context)
        {
            return new CarControlIntent(
                _input.GetSteering(),
                throttle: 0,
                brake: _input.GetBrake(),
                clutch: _input.GetClutch(),
                horn: _input.GetHorn(),
                gearUp: false,
                gearDown: false);
        }
    }
}

