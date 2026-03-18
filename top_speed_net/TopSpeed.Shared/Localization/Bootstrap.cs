using System;
using System.IO;

namespace TopSpeed.Localization
{
    public static class LocalizationBootstrap
    {
        public const string ClientCatalogGroup = "client";
        public const string ServerCatalogGroup = "server";

        public static void Configure(string? languageCode, string? catalogGroup = null)
        {
            var group = catalogGroup?.Trim() ?? string.Empty;
            var languagesRoot = string.IsNullOrWhiteSpace(group)
                ? Path.Combine(AppContext.BaseDirectory, "languages")
                : Path.Combine(AppContext.BaseDirectory, "languages", group);
            var localizer = CatalogLocalizer.Create(languageCode, languagesRoot);
            LocalizationService.SetLocalizer(localizer);
        }
    }
}
