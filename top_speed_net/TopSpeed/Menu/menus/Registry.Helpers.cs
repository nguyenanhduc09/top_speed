using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Core;
using TopSpeed.Input;
using TopSpeed.Network;

namespace TopSpeed.Menu
{
    internal sealed partial class MenuRegistry
    {
        private static IReadOnlyList<string> LoadMenuSoundPresets()
        {
            var root = Path.Combine(AssetPaths.SoundsRoot, "menu");
            if (!Directory.Exists(root))
                return Array.Empty<string>();

            var presets = new List<string>();
            foreach (var directory in Directory.GetDirectories(root))
            {
                var name = Path.GetFileName(directory);
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                presets.Add(name.Trim());
            }

            presets.Sort(StringComparer.OrdinalIgnoreCase);
            return presets;
        }

        private string MainMenuTitle()
        {
            const string keyboard = "Main Menu. Use your arrow keys to navigate the options. Press ENTER to select. Press ESCAPE to back out of any menu. Pressing HOME or END will move you to the top or bottom of a menu.";
            const string joystick = "Main Menu. Use the view finder to move through the options. Press up or down to navigate. Press right or button 1 to select. Press left to back out of any menu.";
            const string both = "Main Menu. Use your arrow keys or the view finder to move through the options. Press ENTER or right or button 1 to select. Press ESCAPE or left to back out of any menu. Pressing HOME or END will move you to the top or bottom of a menu.";

            return _settings.DeviceMode switch
            {
                InputDeviceMode.Keyboard => keyboard,
                InputDeviceMode.Joystick => joystick,
                _ => both
            };
        }

        private static string FormatServerPort(int port)
        {
            return port > 0 ? port.ToString() : $"default ({ClientProtocol.DefaultServerPort})";
        }

        private static string DeviceLabel(InputDeviceMode mode)
        {
            return mode switch
            {
                InputDeviceMode.Keyboard => "keyboard",
                InputDeviceMode.Joystick => "joystick",
                InputDeviceMode.Both => "both",
                _ => "keyboard"
            };
        }

    }
}
