using System;
using System.Globalization;
using System.Threading;

namespace TopSpeed.Localization
{
    public static class LocalizationService
    {
        private static ITextLocalizer _localizer = PassthroughLocalizer.Instance;

        public static string Mark(string? messageId)
        {
            return messageId ?? string.Empty;
        }

        internal static void SetLocalizer(ITextLocalizer? localizer)
        {
            Volatile.Write(ref _localizer, localizer ?? PassthroughLocalizer.Instance);
        }

        public static string Translate(string? messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                return string.Empty;

            var localizer = Volatile.Read(ref _localizer);
            return localizer.Translate(messageId!);
        }

        public static string Translate(string? context, string? messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                return string.Empty;

            if (string.IsNullOrWhiteSpace(context))
                return Translate(messageId);

            var localizer = Volatile.Read(ref _localizer);
            return localizer.Translate(context!, messageId!);
        }

        public static string Format(string? template, params object[]? arguments)
        {
            var localized = Translate(template);
            return ApplyFormat(localized, arguments);
        }

        public static string Format(string? context, string? template, params object[]? arguments)
        {
            var localized = Translate(context, template);
            return ApplyFormat(localized, arguments);
        }

        private static string ApplyFormat(string template, object[]? arguments)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;
            if (arguments == null || arguments.Length == 0)
                return template;

            try
            {
                return string.Format(CultureInfo.CurrentCulture, template, arguments);
            }
            catch (FormatException)
            {
                return template;
            }
        }
    }
}
