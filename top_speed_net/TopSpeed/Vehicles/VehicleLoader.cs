using System;
using System.Collections.Generic;
using System.IO;
using TopSpeed.Core;
using TopSpeed.Data;
using TopSpeed.Protocol;
using TopSpeed.Tracks;

namespace TopSpeed.Vehicles
{
    internal static class VehicleLoader
    {
        private const string BuiltinPrefix = "builtin";
        private const string DefaultVehicleFolder = "default";

        public static VehicleDefinition LoadOfficial(int vehicleIndex, TrackWeather weather)
        {
            if (vehicleIndex < 0 || vehicleIndex >= VehicleCatalog.VehicleCount)
                vehicleIndex = 0;

            var parameters = VehicleCatalog.Vehicles[vehicleIndex];
            var vehiclesRoot = Path.Combine(AssetPaths.SoundsRoot, "Vehicles");
            var currentVehicleFolder = $"Vehicle{vehicleIndex + 1}";

            var def = new VehicleDefinition
            {
                CarType = (CarType)vehicleIndex,
                Name = parameters.Name,
                UserDefined = false,
                SurfaceTractionFactor = parameters.SurfaceTractionFactor,
                Deceleration = parameters.Deceleration,
                TopSpeed = parameters.TopSpeed,
                IdleFreq = parameters.IdleFreq,
                TopFreq = parameters.TopFreq,
                ShiftFreq = parameters.ShiftFreq,
                Gears = parameters.Gears,
                Steering = parameters.Steering,
                HasWipers = parameters.HasWipers == 1 && weather == TrackWeather.Rain ? 1 : 0,
                IdleRpm = parameters.IdleRpm,
                MaxRpm = parameters.MaxRpm,
                RevLimiter = parameters.RevLimiter,
                AutoShiftRpm = parameters.AutoShiftRpm > 0f ? parameters.AutoShiftRpm : parameters.RevLimiter * 0.92f,
                EngineBraking = parameters.EngineBraking,
                MassKg = parameters.MassKg,
                DrivetrainEfficiency = parameters.DrivetrainEfficiency,
                EngineBrakingTorqueNm = parameters.EngineBrakingTorqueNm,
                TireGripCoefficient = parameters.TireGripCoefficient,
                PeakTorqueNm = parameters.PeakTorqueNm,
                PeakTorqueRpm = parameters.PeakTorqueRpm,
                IdleTorqueNm = parameters.IdleTorqueNm,
                RedlineTorqueNm = parameters.RedlineTorqueNm,
                DragCoefficient = parameters.DragCoefficient,
                FrontalAreaM2 = parameters.FrontalAreaM2,
                RollingResistanceCoefficient = parameters.RollingResistanceCoefficient,
                LaunchRpm = parameters.LaunchRpm,
                FinalDriveRatio = parameters.FinalDriveRatio,
                ReverseMaxSpeedKph = parameters.ReverseMaxSpeedKph,
                ReversePowerFactor = parameters.ReversePowerFactor,
                ReverseGearRatio = parameters.ReverseGearRatio,
                TireCircumferenceM = parameters.TireCircumferenceM,
                LateralGripCoefficient = parameters.LateralGripCoefficient,
                HighSpeedStability = parameters.HighSpeedStability,
                WheelbaseM = parameters.WheelbaseM,
                MaxSteerDeg = parameters.MaxSteerDeg,
                WidthM = parameters.WidthM,
                LengthM = parameters.LengthM,
                PowerFactor = parameters.PowerFactor,
                GearRatios = parameters.GearRatios,
                BrakeStrength = parameters.BrakeStrength,
                TransmissionPolicy = parameters.TransmissionPolicy
            };

            foreach (VehicleAction action in Enum.GetValues(typeof(VehicleAction)))
            {
                var overridePath = parameters.GetSoundPath(action);
                if (!string.IsNullOrWhiteSpace(overridePath))
                {
                    def.SetSoundPath(action, Path.Combine(vehiclesRoot, overridePath!));
                }
                else
                {
                    def.SetSoundPath(action, ResolveOfficialFallback(vehiclesRoot, currentVehicleFolder, action));
                }
            }

            return def;
        }

        public static VehicleDefinition LoadCustom(string vehicleFile, TrackWeather weather)
        {
            var filePath = Path.IsPathRooted(vehicleFile)
                ? vehicleFile
                : Path.Combine(AssetPaths.Root, vehicleFile);
            var builtinRoot = Path.Combine(AssetPaths.SoundsRoot, "Vehicles");
            if (!VehicleTsvParser.TryLoadFromFile(filePath, out var parsed, out var issues))
            {
                var message = issues == null || issues.Count == 0
                    ? "Unknown parse error."
                    : string.Join(" ", issues);
                throw new InvalidDataException($"Failed to load custom vehicle '{filePath}'. {message}");
            }

            var hasWipers = weather == TrackWeather.Rain ? parsed.HasWipers : 0;

            var def = new VehicleDefinition
            {
                CarType = CarType.Vehicle1,
                Name = parsed.Meta.Name,
                UserDefined = true,
                CustomFile = Path.GetFileNameWithoutExtension(filePath),
                CustomVersion = parsed.Meta.Version,
                CustomDescription = parsed.Meta.Description,
                SurfaceTractionFactor = parsed.SurfaceTractionFactor,
                Deceleration = parsed.Deceleration,
                TopSpeed = parsed.TopSpeed,
                IdleFreq = parsed.IdleFreq,
                TopFreq = parsed.TopFreq,
                ShiftFreq = parsed.ShiftFreq,
                Gears = parsed.Gears,
                Steering = parsed.Steering,
                HasWipers = hasWipers,
                IdleRpm = parsed.IdleRpm,
                MaxRpm = parsed.MaxRpm,
                RevLimiter = parsed.RevLimiter,
                AutoShiftRpm = parsed.AutoShiftRpm > 0f ? parsed.AutoShiftRpm : parsed.RevLimiter * 0.92f,
                EngineBraking = parsed.EngineBraking,
                MassKg = parsed.MassKg,
                DrivetrainEfficiency = parsed.DrivetrainEfficiency,
                EngineBrakingTorqueNm = parsed.EngineBrakingTorqueNm,
                TireGripCoefficient = parsed.TireGripCoefficient,
                PeakTorqueNm = parsed.PeakTorqueNm,
                PeakTorqueRpm = parsed.PeakTorqueRpm,
                IdleTorqueNm = parsed.IdleTorqueNm,
                RedlineTorqueNm = parsed.RedlineTorqueNm,
                DragCoefficient = parsed.DragCoefficient,
                FrontalAreaM2 = parsed.FrontalAreaM2,
                RollingResistanceCoefficient = parsed.RollingResistanceCoefficient,
                LaunchRpm = parsed.LaunchRpm,
                FinalDriveRatio = parsed.FinalDriveRatio,
                ReverseMaxSpeedKph = parsed.ReverseMaxSpeedKph,
                ReversePowerFactor = parsed.ReversePowerFactor,
                ReverseGearRatio = parsed.ReverseGearRatio,
                TireCircumferenceM = parsed.TireCircumferenceM,
                LateralGripCoefficient = parsed.LateralGripCoefficient,
                HighSpeedStability = parsed.HighSpeedStability,
                WheelbaseM = parsed.WheelbaseM,
                MaxSteerDeg = parsed.MaxSteerDeg,
                WidthM = parsed.WidthM,
                LengthM = parsed.LengthM,
                PowerFactor = parsed.PowerFactor,
                GearRatios = parsed.GearRatios,
                BrakeStrength = parsed.BrakeStrength,
                TransmissionPolicy = parsed.TransmissionPolicy
            };

            def.SetSoundPath(VehicleAction.Engine, ResolveCustomVehicleSound(parsed.Sounds.Engine, builtinRoot, parsed.SourceDirectory, VehicleAction.Engine));
            def.SetSoundPath(VehicleAction.Start, ResolveCustomVehicleSound(parsed.Sounds.Start, builtinRoot, parsed.SourceDirectory, VehicleAction.Start));
            def.SetSoundPath(VehicleAction.Horn, ResolveCustomVehicleSound(parsed.Sounds.Horn, builtinRoot, parsed.SourceDirectory, VehicleAction.Horn));
            if (!string.IsNullOrWhiteSpace(parsed.Sounds.Throttle))
                def.SetSoundPath(VehicleAction.Throttle, ResolveCustomVehicleSound(parsed.Sounds.Throttle!, builtinRoot, parsed.SourceDirectory, VehicleAction.Throttle));
            def.SetSoundPath(VehicleAction.Brake, ResolveCustomVehicleSound(parsed.Sounds.Brake, builtinRoot, parsed.SourceDirectory, VehicleAction.Brake));
            def.SetSoundPaths(VehicleAction.Crash, ResolveCustomVehicleSoundList(parsed.Sounds.CrashVariants, builtinRoot, parsed.SourceDirectory, VehicleAction.Crash));
            if (parsed.Sounds.BackfireVariants != null && parsed.Sounds.BackfireVariants.Count > 0)
                def.SetSoundPaths(VehicleAction.Backfire, ResolveCustomVehicleSoundList(parsed.Sounds.BackfireVariants, builtinRoot, parsed.SourceDirectory, VehicleAction.Backfire));

            return def;
        }

        private static string? ResolveOfficialFallback(string root, string vehicleFolder, VehicleAction action)
        {
            var fileName = GetDefaultFileName(action);
            var primaryPath = Path.GetFullPath(Path.Combine(root, vehicleFolder, fileName));
            if (File.Exists(primaryPath))
                return primaryPath;

            // Only fallback to 'default' folder for non-optional sounds
            // Throttle and Backfire are vehicle-specific features
            if (action == VehicleAction.Backfire || action == VehicleAction.Throttle)
                return null;

            var fallbackPath = Path.GetFullPath(Path.Combine(root, DefaultVehicleFolder, fileName));
            if (File.Exists(fallbackPath))
                return fallbackPath;

            return null;
        }

        private static string GetDefaultFileName(VehicleAction action)
        {
            switch (action)
            {
                case VehicleAction.Engine: return "engine.wav";
                case VehicleAction.Start: return "start.wav";
                case VehicleAction.Horn: return "horn.wav";
                case VehicleAction.Throttle: return "throttle.wav";
                case VehicleAction.Crash: return "crash.wav";
                case VehicleAction.Brake: return "brake.wav";
                case VehicleAction.Backfire: return "backfire.wav";
                default: throw new ArgumentOutOfRangeException(nameof(action));
            }
        }

        private static string[] ResolveCustomVehicleSoundList(
            IReadOnlyList<string> values,
            string builtinRoot,
            string vehicleRoot,
            VehicleAction builtinAction)
        {
            var result = new List<string>();
            for (var i = 0; i < values.Count; i++)
            {
                var resolved = ResolveCustomVehicleSound(values[i], builtinRoot, vehicleRoot, builtinAction);
                if (!string.IsNullOrWhiteSpace(resolved))
                    result.Add(resolved!);
            }

            if (result.Count == 0)
                throw new InvalidDataException($"No valid sound paths resolved for {builtinAction}.");

            return result.ToArray();
        }

        private static string ResolveCustomVehicleSound(
            string value,
            string builtinRoot,
            string vehicleRoot,
            VehicleAction builtinAction)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException($"Missing required sound value for {builtinAction}.");

            var trimmed = value.Trim();
            if (trimmed.StartsWith(BuiltinPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var fromBuiltin = ResolveCustomBuiltinSound(trimmed, builtinRoot, builtinAction);
                if (!string.IsNullOrWhiteSpace(fromBuiltin))
                    return fromBuiltin!;
                throw new InvalidDataException($"Builtin sound reference '{trimmed}' for {builtinAction} could not be resolved.");
            }

            if (Path.IsPathRooted(trimmed))
                throw new InvalidDataException($"Absolute sound paths are not allowed for custom vehicles: {trimmed}");

            var normalized = trimmed
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            if (normalized.IndexOf(':') >= 0 || ContainsTraversal(normalized))
                throw new InvalidDataException($"Invalid custom sound path '{trimmed}'. Paths must stay inside the vehicle folder.");

            var rootFull = Path.GetFullPath(vehicleRoot);
            var candidate = Path.GetFullPath(Path.Combine(rootFull, normalized));
            if (!IsInsideRoot(rootFull, candidate))
                throw new InvalidDataException($"Custom sound path '{trimmed}' escapes the vehicle folder.");
            if (!File.Exists(candidate))
                throw new FileNotFoundException($"Custom vehicle sound file not found: {candidate}", candidate);
            return candidate;
        }

        private static bool ContainsTraversal(string path)
        {
            var parts = path.Split(Path.DirectorySeparatorChar);
            for (var i = 0; i < parts.Length; i++)
            {
                var segment = parts[i].Trim();
                if (segment == "." || segment == "..")
                    return true;
            }
            return false;
        }

        private static bool IsInsideRoot(string rootFull, string candidate)
        {
            if (string.Equals(rootFull, candidate, StringComparison.OrdinalIgnoreCase))
                return true;
            var rootWithSeparator = rootFull.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase);
        }

        private static string? ResolveCustomBuiltinSound(string token, string builtinRoot, VehicleAction action)
        {
            if (!int.TryParse(token.Substring(BuiltinPrefix.Length), out var index))
                return null;
            index -= 1;
            if (index < 0 || index >= VehicleCatalog.VehicleCount)
                return null;

            var vehiclesRoot = builtinRoot;
            var parameters = VehicleCatalog.Vehicles[index];
            var file = parameters.GetSoundPath(action);
            if (!string.IsNullOrWhiteSpace(file))
                return Path.Combine(vehiclesRoot, file!);

            return ResolveOfficialFallback(vehiclesRoot, $"Vehicle{index + 1}", action);
        }
    }
}


