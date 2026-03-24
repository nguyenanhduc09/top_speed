using System;
using System.Collections.Generic;

namespace TopSpeed.Vehicles
{
    public enum TransmissionType
    {
        Atc = 0,
        Cvt = 1,
        Dct = 2,
        Manual = 3
    }

    public static class TransmissionTypes
    {
        public static bool IsAutomaticFamily(TransmissionType type)
        {
            return type == TransmissionType.Atc
                || type == TransmissionType.Cvt
                || type == TransmissionType.Dct;
        }

        public static bool TryParse(string raw, out TransmissionType type)
        {
            type = TransmissionType.Manual;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            switch (raw.Trim().ToLowerInvariant())
            {
                case "atc":
                    type = TransmissionType.Atc;
                    return true;
                case "cvt":
                    type = TransmissionType.Cvt;
                    return true;
                case "dct":
                    type = TransmissionType.Dct;
                    return true;
                case "manual":
                    type = TransmissionType.Manual;
                    return true;
                default:
                    return false;
            }
        }

        public static bool TryValidate(
            TransmissionType primaryType,
            IReadOnlyList<TransmissionType>? supportedTypes,
            out string error)
        {
            error = string.Empty;
            if (supportedTypes == null)
            {
                error = "Supported transmission types are required.";
                return false;
            }

            if (supportedTypes.Count == 0)
            {
                error = "At least one supported transmission type is required.";
                return false;
            }

            if (supportedTypes.Count > 2)
            {
                error = "At most two supported transmission types are allowed.";
                return false;
            }

            var seen = new HashSet<TransmissionType>();
            var manualCount = 0;
            var automaticCount = 0;

            for (var i = 0; i < supportedTypes.Count; i++)
            {
                var type = supportedTypes[i];
                if (!IsDefinedType(type))
                {
                    error = $"Unsupported transmission type '{type}'.";
                    return false;
                }

                if (!seen.Add(type))
                {
                    error = $"Duplicate supported transmission type '{type}' is not allowed.";
                    return false;
                }

                if (type == TransmissionType.Manual)
                    manualCount++;
                else if (IsAutomaticFamily(type))
                    automaticCount++;
                else
                {
                    error = $"Unsupported transmission type '{type}'.";
                    return false;
                }
            }

            if (automaticCount > 1)
            {
                error = "Only one automatic transmission family is allowed per vehicle.";
                return false;
            }

            if (supportedTypes.Count == 2 && (manualCount != 1 || automaticCount != 1))
            {
                error = "Two supported transmission types must be exactly one manual and one automatic family.";
                return false;
            }

            if (!seen.Contains(primaryType))
            {
                error = $"Primary transmission type '{primaryType}' must be included in supported transmission types.";
                return false;
            }

            return true;
        }

        private static bool IsDefinedType(TransmissionType type)
        {
            return type == TransmissionType.Atc
                || type == TransmissionType.Cvt
                || type == TransmissionType.Dct
                || type == TransmissionType.Manual;
        }
    }
}
