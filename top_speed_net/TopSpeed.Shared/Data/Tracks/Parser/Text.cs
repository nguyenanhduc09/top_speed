using TopSpeed.Localization;

namespace TopSpeed.Data
{
    public static partial class TrackTsmParser
    {
        private static string Localized(string text)
        {
            return LocalizationService.Mark(text);
        }

        private static string Localized(string format, params object[] args)
        {
            return LocalizationService.Format(LocalizationService.Mark(format), args);
        }
    }
}
