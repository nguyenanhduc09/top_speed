using System;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.DirectInput;
using TopSpeed.Input.Devices.Joystick;

namespace TopSpeed.Input
{
    internal sealed partial class InputManager
    {
        public void Update()
        {
            _previous.CopyFrom(_current);
            _current.Clear();

            if (_suspended || _disposed)
                return;

            if (!TryGetKeyboardState(out var state))
                return;
            foreach (var key in state.PressedKeys)
            {
                _current.Set(key, true);
            }
            ApplyModifierFallbacks(state);

            if (!_joystickEnabled)
                return;

            _gamepad.Update();
            if (!_gamepad.IsAvailable)
            {
                var joystick = GetJoystickDevice();
                if (joystick == null || !joystick.IsAvailable)
                    return;
                joystick.Update();
            }
        }

        public bool WasPressed(Key key)
        {
            if (_suspended)
                return false;

            var index = (int)key;
            if (index < 0 || index >= _keyLatch.Length)
                return false;

            if (!TryGetKeyboardState(out var state))
            {
                _keyLatch[index] = false;
                return false;
            }

            if (state.IsPressed(key))
            {
                if (_keyLatch[index])
                    return false;
                _keyLatch[index] = true;
                return true;
            }

            _keyLatch[index] = false;
            return false;
        }

        public bool TryGetJoystickState(out JoystickStateSnapshot state)
        {
            if (!_joystickEnabled)
            {
                state = default;
                return false;
            }

            var device = VibrationDevice;
            if (device != null && device.IsAvailable)
            {
                state = device.State;
                return true;
            }

            state = default;
            return false;
        }

        public void ResetState()
        {
            _current.Clear();
            _previous.Clear();
            for (var i = 0; i < _keyLatch.Length; i++)
                _keyLatch[i] = false;
        }

        private bool TryAcquire()
        {
            if (_disposed)
                return false;

            try
            {
                _keyboard.Acquire();
                return true;
            }
            catch (SharpDXException)
            {
                return false;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (NullReferenceException)
            {
                return false;
            }
        }

        private void ApplyModifierFallbacks(KeyboardState state)
        {
            // Some setups intermittently miss right-side modifiers in DirectInput state while
            // chorded with arrows. Use Win32 async key state as fallback.
            var anyShift = state.IsPressed(Key.LeftShift)
                || state.IsPressed(Key.RightShift)
                || IsVirtualKeyDown(VkShift);
            var leftShift = state.IsPressed(Key.LeftShift) || IsVirtualKeyDown(VkLeftShift);
            var rightShift = state.IsPressed(Key.RightShift) || IsVirtualKeyDown(VkRightShift);
            if (anyShift && !leftShift && !rightShift)
            {
                leftShift = true;
                rightShift = true;
            }

            var anyCtrl = state.IsPressed(Key.LeftControl)
                || state.IsPressed(Key.RightControl)
                || IsVirtualKeyDown(VkControl);
            var leftCtrl = state.IsPressed(Key.LeftControl) || IsVirtualKeyDown(VkLeftControl);
            var rightCtrl = state.IsPressed(Key.RightControl) || IsVirtualKeyDown(VkRightControl);
            if (anyCtrl && !leftCtrl && !rightCtrl)
            {
                leftCtrl = true;
                rightCtrl = true;
            }

            var anyAlt = state.IsPressed(Key.LeftAlt)
                || state.IsPressed(Key.RightAlt)
                || IsVirtualKeyDown(VkMenu);
            var leftAlt = state.IsPressed(Key.LeftAlt) || IsVirtualKeyDown(VkLeftMenu);
            var rightAlt = state.IsPressed(Key.RightAlt) || IsVirtualKeyDown(VkRightMenu);
            if (anyAlt && !leftAlt && !rightAlt)
            {
                leftAlt = true;
                rightAlt = true;
            }

            _current.Set(Key.LeftShift, leftShift);
            _current.Set(Key.RightShift, rightShift);
            _current.Set(Key.LeftControl, leftCtrl);
            _current.Set(Key.RightControl, rightCtrl);
            _current.Set(Key.LeftAlt, leftAlt);
            _current.Set(Key.RightAlt, rightAlt);
        }

        private static bool IsVirtualKeyDown(int vk)
        {
            return (GetAsyncKeyState(vk) & 0x8000) != 0;
        }

        private const int VkShift = 0x10;
        private const int VkControl = 0x11;
        private const int VkMenu = 0x12;
        private const int VkLeftShift = 0xA0;
        private const int VkRightShift = 0xA1;
        private const int VkLeftControl = 0xA2;
        private const int VkRightControl = 0xA3;
        private const int VkLeftMenu = 0xA4;
        private const int VkRightMenu = 0xA5;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
    }
}
