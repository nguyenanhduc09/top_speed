using System.Collections.Generic;
using SharpDX.DirectInput;
using TopSpeed.Input;
using TopSpeed.Localization;

namespace TopSpeed.Core.Settings
{
    internal sealed partial class SettingsManager
    {
        private static void ApplyInput(RaceSettings settings, SettingsInputDocument input, List<SettingsIssue> issues)
        {
            if (input.ForceFeedback.HasValue)
                settings.ForceFeedback = input.ForceFeedback.Value;

            settings.KeyboardProgressiveRate = ReadEnum(input.KeyboardProgressiveRate, settings.KeyboardProgressiveRate, "input.keyboardProgressiveRate", issues);
            settings.DeviceMode = ReadEnum(input.DeviceMode, settings.DeviceMode, "input.deviceMode", issues);

            if (input.Keyboard == null)
                issues.Add(new SettingsIssue(
                    SettingsIssueSeverity.Warning,
                    "input.keyboard",
                    LocalizationService.Mark("Keyboard bindings section is missing. Defaults were used for keyboard bindings.")));
            else
                ApplyKeyboard(settings, input.Keyboard, issues);

            ApplyMenuShortcuts(settings, input.MenuShortcuts, issues);

            if (input.Joystick == null)
                issues.Add(new SettingsIssue(
                    SettingsIssueSeverity.Warning,
                    "input.joystick",
                    LocalizationService.Mark("Joystick bindings section is missing. Defaults were used for joystick bindings.")));
            else
                ApplyJoystick(settings, input.Joystick, issues);
        }

        private static void ApplyKeyboard(RaceSettings settings, SettingsKeyboardDocument keyboard, List<SettingsIssue> issues)
        {
            settings.KeyLeft = ReadKey(keyboard.Left, settings.KeyLeft, "input.keyboard.left", issues);
            settings.KeyRight = ReadKey(keyboard.Right, settings.KeyRight, "input.keyboard.right", issues);
            settings.KeyThrottle = ReadKey(keyboard.Throttle, settings.KeyThrottle, "input.keyboard.throttle", issues);
            settings.KeyBrake = ReadKey(keyboard.Brake, settings.KeyBrake, "input.keyboard.brake", issues);
            settings.KeyGearUp = ReadKey(keyboard.GearUp, settings.KeyGearUp, "input.keyboard.gearUp", issues);
            settings.KeyGearDown = ReadKey(keyboard.GearDown, settings.KeyGearDown, "input.keyboard.gearDown", issues);
            settings.KeyHorn = ReadKey(keyboard.Horn, settings.KeyHorn, "input.keyboard.horn", issues);
            settings.KeyRequestInfo = ReadKey(keyboard.RequestInfo, settings.KeyRequestInfo, "input.keyboard.requestInfo", issues);
            settings.KeyCurrentGear = ReadKey(keyboard.CurrentGear, settings.KeyCurrentGear, "input.keyboard.currentGear", issues);
            settings.KeyCurrentLapNr = ReadKey(keyboard.CurrentLapNr, settings.KeyCurrentLapNr, "input.keyboard.currentLapNr", issues);
            settings.KeyCurrentRacePerc = ReadKey(keyboard.CurrentRacePerc, settings.KeyCurrentRacePerc, "input.keyboard.currentRacePerc", issues);
            settings.KeyCurrentLapPerc = ReadKey(keyboard.CurrentLapPerc, settings.KeyCurrentLapPerc, "input.keyboard.currentLapPerc", issues);
            settings.KeyCurrentRaceTime = ReadKey(keyboard.CurrentRaceTime, settings.KeyCurrentRaceTime, "input.keyboard.currentRaceTime", issues);
            settings.KeyStartEngine = ReadKey(keyboard.StartEngine, settings.KeyStartEngine, "input.keyboard.startEngine", issues);
            settings.KeyReportDistance = ReadKey(keyboard.ReportDistance, settings.KeyReportDistance, "input.keyboard.reportDistance", issues);
            settings.KeyReportSpeed = ReadKey(keyboard.ReportSpeed, settings.KeyReportSpeed, "input.keyboard.reportSpeed", issues);
            settings.KeyTrackName = ReadKey(keyboard.TrackName, settings.KeyTrackName, "input.keyboard.trackName", issues);
            settings.KeyPause = ReadKey(keyboard.Pause, settings.KeyPause, "input.keyboard.pause", issues);
        }

        private static void ApplyJoystick(RaceSettings settings, SettingsJoystickDocument joystick, List<SettingsIssue> issues)
        {
            settings.JoystickLeft = ReadJoystick(joystick.Left, settings.JoystickLeft, "input.joystick.left", issues);
            settings.JoystickRight = ReadJoystick(joystick.Right, settings.JoystickRight, "input.joystick.right", issues);
            settings.JoystickThrottle = ReadJoystick(joystick.Throttle, settings.JoystickThrottle, "input.joystick.throttle", issues);
            settings.JoystickBrake = ReadJoystick(joystick.Brake, settings.JoystickBrake, "input.joystick.brake", issues);
            settings.JoystickGearUp = ReadJoystick(joystick.GearUp, settings.JoystickGearUp, "input.joystick.gearUp", issues);
            settings.JoystickGearDown = ReadJoystick(joystick.GearDown, settings.JoystickGearDown, "input.joystick.gearDown", issues);
            settings.JoystickHorn = ReadJoystick(joystick.Horn, settings.JoystickHorn, "input.joystick.horn", issues);
            settings.JoystickRequestInfo = ReadJoystick(joystick.RequestInfo, settings.JoystickRequestInfo, "input.joystick.requestInfo", issues);
            settings.JoystickCurrentGear = ReadJoystick(joystick.CurrentGear, settings.JoystickCurrentGear, "input.joystick.currentGear", issues);
            settings.JoystickCurrentLapNr = ReadJoystick(joystick.CurrentLapNr, settings.JoystickCurrentLapNr, "input.joystick.currentLapNr", issues);
            settings.JoystickCurrentRacePerc = ReadJoystick(joystick.CurrentRacePerc, settings.JoystickCurrentRacePerc, "input.joystick.currentRacePerc", issues);
            settings.JoystickCurrentLapPerc = ReadJoystick(joystick.CurrentLapPerc, settings.JoystickCurrentLapPerc, "input.joystick.currentLapPerc", issues);
            settings.JoystickCurrentRaceTime = ReadJoystick(joystick.CurrentRaceTime, settings.JoystickCurrentRaceTime, "input.joystick.currentRaceTime", issues);
            settings.JoystickStartEngine = ReadJoystick(joystick.StartEngine, settings.JoystickStartEngine, "input.joystick.startEngine", issues);
            settings.JoystickReportDistance = ReadJoystick(joystick.ReportDistance, settings.JoystickReportDistance, "input.joystick.reportDistance", issues);
            settings.JoystickReportSpeed = ReadJoystick(joystick.ReportSpeed, settings.JoystickReportSpeed, "input.joystick.reportSpeed", issues);
            settings.JoystickTrackName = ReadJoystick(joystick.TrackName, settings.JoystickTrackName, "input.joystick.trackName", issues);
            settings.JoystickPause = ReadJoystick(joystick.Pause, settings.JoystickPause, "input.joystick.pause", issues);
            settings.JoystickThrottleInvertMode = ReadEnum(joystick.ThrottleInvertMode, settings.JoystickThrottleInvertMode, "input.joystick.throttleInvertMode", issues);
            settings.JoystickBrakeInvertMode = ReadEnum(joystick.BrakeInvertMode, settings.JoystickBrakeInvertMode, "input.joystick.brakeInvertMode", issues);
            settings.JoystickSteeringDeadZone = ClampInt(joystick.SteeringDeadZone, settings.JoystickSteeringDeadZone, 1, 5, "input.joystick.steeringDeadZone", issues);

            if (joystick.Center == null)
                return;

            var center = settings.JoystickCenter;
            if (joystick.Center.X.HasValue) center.X = joystick.Center.X.Value;
            if (joystick.Center.Y.HasValue) center.Y = joystick.Center.Y.Value;
            if (joystick.Center.Z.HasValue) center.Z = joystick.Center.Z.Value;
            if (joystick.Center.Rx.HasValue) center.Rx = joystick.Center.Rx.Value;
            if (joystick.Center.Ry.HasValue) center.Ry = joystick.Center.Ry.Value;
            if (joystick.Center.Rz.HasValue) center.Rz = joystick.Center.Rz.Value;
            if (joystick.Center.Slider1.HasValue) center.Slider1 = joystick.Center.Slider1.Value;
            if (joystick.Center.Slider2.HasValue) center.Slider2 = joystick.Center.Slider2.Value;
            settings.JoystickCenter = center;
        }

        private static void ApplyMenuShortcuts(RaceSettings settings, SettingsMenuShortcutsDocument? menuShortcuts, List<SettingsIssue> issues)
        {
            settings.ShortcutKeyBindings = new Dictionary<string, Key>(System.StringComparer.Ordinal);
            if (menuShortcuts?.Bindings == null)
                return;

            for (var i = 0; i < menuShortcuts.Bindings.Count; i++)
            {
                var binding = menuShortcuts.Bindings[i];
                if (binding == null || string.IsNullOrWhiteSpace(binding.Id))
                {
                    issues.Add(new SettingsIssue(
                        SettingsIssueSeverity.Warning,
                        $"input.menuShortcuts.bindings[{i}].id",
                        LocalizationService.Mark("Shortcut binding id is missing and was ignored.")));
                    continue;
                }

                if (!binding.Key.HasValue)
                    continue;

                var keyValue = binding.Key.Value;
                if (!System.Enum.IsDefined(typeof(Key), keyValue) || keyValue < 0)
                {
                    issues.Add(new SettingsIssue(
                        SettingsIssueSeverity.Warning,
                        $"input.menuShortcuts.bindings[{i}].key",
                        LocalizationService.Format(
                            LocalizationService.Mark("The key input.menuShortcuts.bindings[{0}].key has invalid value {1} and was ignored."),
                            i,
                            keyValue)));
                    continue;
                }

                var actionId = binding.Id!.Trim();
                settings.ShortcutKeyBindings[actionId] = (Key)keyValue;
            }
        }
    }
}
