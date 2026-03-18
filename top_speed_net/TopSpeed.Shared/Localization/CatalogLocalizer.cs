using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using GetText;

namespace TopSpeed.Localization
{
    internal sealed class CatalogLocalizer : ITextLocalizer
    {
        private readonly Catalog _primaryCatalog;
        private readonly Catalog? _fallbackCatalog;

        private CatalogLocalizer(Catalog primaryCatalog, Catalog? fallbackCatalog)
        {
            _primaryCatalog = primaryCatalog;
            _fallbackCatalog = fallbackCatalog;
        }

        public static ITextLocalizer Create(string? languageCode, string languagesRoot)
        {
            if (string.IsNullOrWhiteSpace(languagesRoot))
                return PassthroughLocalizer.Instance;

            var primaryCatalog = TryLoadCatalog(languageCode, languagesRoot);
            var fallbackCatalog = TryLoadCatalog("en", languagesRoot);

            if (primaryCatalog == null && fallbackCatalog == null)
                return PassthroughLocalizer.Instance;

            if (primaryCatalog == null && fallbackCatalog != null)
                return new CatalogLocalizer(fallbackCatalog, null);

            if (primaryCatalog == null)
                return PassthroughLocalizer.Instance;

            if (fallbackCatalog == null)
                return new CatalogLocalizer(primaryCatalog, null);

            if (string.Equals(GetCultureCode(primaryCatalog.CultureInfo), "en", StringComparison.OrdinalIgnoreCase))
                return new CatalogLocalizer(primaryCatalog, null);

            return new CatalogLocalizer(primaryCatalog, fallbackCatalog);
        }

        public string Translate(string messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                return string.Empty;

            var resolved = _primaryCatalog.GetStringDefault(messageId, messageId);
            if (!string.Equals(resolved, messageId, StringComparison.Ordinal))
                return resolved;

            if (_fallbackCatalog != null)
                return _fallbackCatalog.GetStringDefault(messageId, messageId);

            return messageId;
        }

        public string Translate(string context, string messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                return string.Empty;

            if (TryGetContextTranslation(_primaryCatalog, context, messageId, out var translated))
                return translated;

            if (_fallbackCatalog != null && TryGetContextTranslation(_fallbackCatalog, context, messageId, out translated))
                return translated;

            return Translate(messageId);
        }

        private static bool TryGetContextTranslation(Catalog catalog, string context, string messageId, out string translation)
        {
            translation = string.Empty;
            if (catalog == null || string.IsNullOrWhiteSpace(context) || string.IsNullOrWhiteSpace(messageId))
                return false;

            var key = context + Catalog.CONTEXTGLUE + messageId;
            if (!catalog.Translations.TryGetValue(key, out var forms) || forms == null || forms.Length == 0)
                return false;

            var form = forms[0];
            if (string.IsNullOrWhiteSpace(form))
                return false;

            translation = form;
            return true;
        }

        private static Catalog? TryLoadCatalog(string? languageCode, string languagesRoot)
        {
            foreach (var candidate in BuildLanguageCandidates(languageCode))
            {
                var moFile = Path.Combine(languagesRoot, candidate, "messages.mo");
                if (!File.Exists(moFile))
                    continue;

                try
                {
                    using var stream = File.OpenRead(moFile);
                    return new Catalog(stream, ResolveCulture(candidate));
                }
                catch
                {
                    continue;
                }
            }

            return null;
        }

        private static IEnumerable<string> BuildLanguageCandidates(string? languageCode)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(languageCode))
            {
                var normalized = NormalizeCode(languageCode!);
                if (normalized.Length > 0 && seen.Add(normalized))
                    yield return normalized;

                var splitDash = normalized.IndexOf('-');
                if (splitDash > 0)
                {
                    var parentDash = normalized.Substring(0, splitDash);
                    if (seen.Add(parentDash))
                        yield return parentDash;
                }

                var splitUnderscore = normalized.IndexOf('_');
                if (splitUnderscore > 0)
                {
                    var parentUnderscore = normalized.Substring(0, splitUnderscore);
                    if (seen.Add(parentUnderscore))
                        yield return parentUnderscore;
                }
            }

            if (seen.Add("en"))
                yield return "en";
        }

        private static string NormalizeCode(string code)
        {
            return code.Trim().Replace('\\', '-').Replace('/', '-');
        }

        private static CultureInfo ResolveCulture(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                return CultureInfo.InvariantCulture;

            try
            {
                return CultureInfo.GetCultureInfo(languageCode);
            }
            catch (CultureNotFoundException)
            {
                var normalized = languageCode.Replace('_', '-');
                var splitIndex = normalized.IndexOf('-');
                if (splitIndex > 0)
                {
                    var parent = normalized.Substring(0, splitIndex);
                    try
                    {
                        return CultureInfo.GetCultureInfo(parent);
                    }
                    catch (CultureNotFoundException)
                    {
                        return CultureInfo.InvariantCulture;
                    }
                }

                return CultureInfo.InvariantCulture;
            }
        }

        private static string GetCultureCode(CultureInfo culture)
        {
            if (culture == null)
                return string.Empty;

            return culture.Name ?? string.Empty;
        }
    }
}
