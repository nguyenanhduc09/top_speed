using Key = TopSpeed.Input.InputKey;

namespace TopSpeed.Input
{
    internal sealed partial class DriveInput
    {
        public void SetPanelInputAccess(bool allowDrivingInput, bool allowAuxiliaryInput)
        {
            _allowDrivingInput = allowDrivingInput;
            _allowAuxiliaryInput = allowAuxiliaryInput;
        }

        public void SetOverlayInputBlocked(bool blocked)
        {
            _overlayInputBlocked = blocked;
        }

        public void SetPausedHornInputAllowed(bool allowed)
        {
            _pausedHornInputAllowed = allowed;
        }

        private bool IsCtrlDown()
        {
            return _lastState.IsDown(Key.LeftControl) || _lastState.IsDown(Key.RightControl);
        }

        private bool IsShiftDown()
        {
            return _lastState.IsDown(Key.LeftShift) || _lastState.IsDown(Key.RightShift);
        }
    }
}



