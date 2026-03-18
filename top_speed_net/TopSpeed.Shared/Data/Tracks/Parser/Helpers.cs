using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace TopSpeed.Data
{
    public static partial class TrackTsmParser
    {
        private static string StripInlineComment(string raw)
        {
            var text = raw ?? string.Empty;
            var index = text.IndexOf('#');
            return index >= 0 ? text.Substring(0, index) : text;
        }

        private static bool TryParseSectionHeader(
            string line,
            out string sectionKind,
            out string sectionId,
            out TrackSoundSourceType? impliedSoundType)
        {
            sectionKind = string.Empty;
            sectionId = string.Empty;
            impliedSoundType = null;

            if (string.IsNullOrWhiteSpace(line))
                return false;

            var trimmed = line.Trim();
            if (!trimmed.StartsWith("[", StringComparison.Ordinal) ||
                !trimmed.EndsWith("]", StringComparison.Ordinal))
                return false;

            var inner = trimmed.Substring(1, trimmed.Length - 2).Trim();
            if (inner.Length == 0)
                return false;

            var separatorIndex = inner.IndexOf(':');
            var rawKind = separatorIndex >= 0
                ? inner.Substring(0, separatorIndex)
                : inner;
            var rawId = separatorIndex >= 0
                ? inner.Substring(separatorIndex + 1)
                : string.Empty;

            var kind = NormalizeIdentifier(rawKind);
            if (kind.StartsWith("sound_", StringComparison.OrdinalIgnoreCase))
            {
                var typeToken = kind.Substring("sound_".Length);
                if (TryParseSoundType(typeToken, out var soundType))
                {
                    sectionKind = "sound";
                    sectionId = rawId.Trim();
                    impliedSoundType = soundType;
                    return true;
                }
            }

            sectionKind = kind;
            sectionId = rawId.Trim();
            return true;
        }

        private static bool TryParseKeyValue(string line, out string key, out string value)
        {
            key = string.Empty;
            value = string.Empty;

            if (string.IsNullOrWhiteSpace(line))
                return false;

            var index = line.IndexOf('=');
            if (index <= 0 || index >= line.Length - 1)
                return false;

            key = line.Substring(0, index).Trim();
            value = line.Substring(index + 1).Trim();
            return key.Length > 0;
        }

        private static string NormalizeIdentifier(string value)
        {
            var text = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (text.Length == 0)
                return string.Empty;

            return text
                .Replace('-', '_')
                .Replace(' ', '_');
        }

        private static string NormalizeLookupToken(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var text = raw.Trim().ToLowerInvariant();
            var chars = new List<char>(text.Length);
            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (char.IsLetterOrDigit(ch))
                    chars.Add(ch);
            }

            return chars.Count == 0 ? string.Empty : new string(chars.ToArray());
        }

        private static string? NormalizeNullable(string value)
        {
            var text = (value ?? string.Empty).Trim();
            return text.Length == 0 ? null : text;
        }

        private static IReadOnlyList<string> ParseCsvList(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<string>();

            var tokens = value.Split(',');
            var list = new List<string>(tokens.Length);
            for (var i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i].Trim();
                if (token.Length == 0)
                    continue;
                list.Add(token);
            }

            return list;
        }

        private static bool TryParseInt(string raw, out int value)
        {
            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseFloat(string raw, out float value)
        {
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseBool(string raw, out bool value)
        {
            value = false;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var token = NormalizeLookupToken(raw);
            switch (token)
            {
                case "1":
                case "true":
                case "yes":
                case "on":
                    value = true;
                    return true;
                case "0":
                case "false":
                case "no":
                case "off":
                    value = false;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryParseVector(string raw, out Vector3 value)
        {
            value = default;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var parts = raw.Split(',');
            if (parts.Length != 3)
                return false;
            if (!TryParseFloat(parts[0].Trim(), out var x))
                return false;
            if (!TryParseFloat(parts[1].Trim(), out var y))
                return false;
            if (!TryParseFloat(parts[2].Trim(), out var z))
                return false;

            value = new Vector3(x, y, z);
            return true;
        }
    }
}
