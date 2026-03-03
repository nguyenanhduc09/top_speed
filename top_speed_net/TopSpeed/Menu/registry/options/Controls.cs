using System.Collections.Generic;
using TopSpeed.Input;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private MenuScreen BuildOptionsControlsMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem(() => $"Select device: {DeviceLabel(_settings.DeviceMode)}", MenuAction.None, nextMenuId: "options_controls_device"),
                new CheckBox(
                    "Force feedback",
                    () => _settings.ForceFeedback,
                    value => _settingsActions.UpdateSetting(() => _settings.ForceFeedback = value),
                    hint: "Enables force feedback or vibration if your controller supports it. Press ENTER to toggle."),
                new RadioButton(
                    "Progressive keyboard input",
                    new[]
                    {
                        "Off",
                        "Fastest (0.25 seconds)",
                        "Fast (0.50 seconds)",
                        "Moderate (0.75 seconds)",
                        "Slowest (1.00 second)"
                    },
                    () => (int)_settings.KeyboardProgressiveRate,
                    value => _settingsActions.UpdateSetting(() => _settings.KeyboardProgressiveRate = (KeyboardProgressiveRate)value),
                    hint: "When enabled, throttle, brake, and steering ramp in over time instead of jumping instantly to full value. Press LEFT or RIGHT to change."),
                new MenuItem("Map keyboard keys", MenuAction.None, nextMenuId: "options_controls_keyboard"),
                new MenuItem("Map joystick keys", MenuAction.None, nextMenuId: "options_controls_joystick"),
                BackItem()
            };
            return _menu.CreateMenu("options_controls", items);
        }

        private MenuScreen BuildOptionsControlsDeviceMenu()
        {
            var items = new List<MenuItem>
            {
                new MenuItem("Keyboard", MenuAction.Back, onActivate: () => _settingsActions.SetDevice(InputDeviceMode.Keyboard)),
                new MenuItem("Joystick", MenuAction.Back, onActivate: () => _settingsActions.SetDevice(InputDeviceMode.Joystick)),
                new MenuItem("Both", MenuAction.Back, onActivate: () => _settingsActions.SetDevice(InputDeviceMode.Both)),
                BackItem()
            };
            return _menu.CreateMenu("options_controls_device", items, "Select input device");
        }

        private MenuScreen BuildOptionsControlsKeyboardMenu()
        {
            return _menu.CreateMenu("options_controls_keyboard", BuildMappingItems(InputMappingMode.Keyboard));
        }

        private MenuScreen BuildOptionsControlsJoystickMenu()
        {
            var items = new List<MenuItem>
            {
                new RadioButton(
                    "Throttle pedal direction",
                    new[] { "Auto", "Normal", "Inverted" },
                    () => (int)_settings.JoystickThrottleInvertMode,
                    value => _settingsActions.UpdateSetting(() => _settings.JoystickThrottleInvertMode = (PedalInvertMode)value),
                    hint: "Auto detects wheel pedal direction from resting position. Use LEFT or RIGHT to change."),
                new RadioButton(
                    "Brake pedal direction",
                    new[] { "Auto", "Normal", "Inverted" },
                    () => (int)_settings.JoystickBrakeInvertMode,
                    value => _settingsActions.UpdateSetting(() => _settings.JoystickBrakeInvertMode = (PedalInvertMode)value),
                    hint: "Auto detects wheel pedal direction from resting position. Use LEFT or RIGHT to change.")
            };

            items.AddRange(BuildMappingItems(InputMappingMode.Joystick, includeBack: false));
            items.Add(BackItem());
            return _menu.CreateMenu("options_controls_joystick", items);
        }

        private List<MenuItem> BuildMappingItems(InputMappingMode mode, bool includeBack = true)
        {
            var items = new List<MenuItem>();
            foreach (var action in _raceInput.KeyMap.Actions)
            {
                var definition = action;
                items.Add(new MenuItem(
                    () => $"{definition.Label}: {_mapping.FormatMappingValue(definition.Action, mode)}",
                    MenuAction.None,
                    onActivate: () => _mapping.BeginMapping(mode, definition.Action)));
            }

            if (includeBack)
                items.Add(BackItem());
            return items;
        }
    }
}
