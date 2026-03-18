using System;
using TopSpeed.Localization;

namespace TopSpeed.Server.Logging
{
    internal static class ConsoleSink
    {
        public static bool WriteLine(string text)
        {
            try
            {
                Console.WriteLine(LocalizationService.Translate(text));
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (System.IO.IOException)
            {
                return false;
            }
        }

        public static bool WriteLineFormat(string template, params object[] args)
        {
            return WriteLine(LocalizationService.Format(template, args));
        }
    }
}
