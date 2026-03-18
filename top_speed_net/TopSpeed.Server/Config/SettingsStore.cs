using System;
using System.IO;
using System.Text.Json;
using TopSpeed.Localization;
using TopSpeed.Server.Logging;

namespace TopSpeed.Server.Config
{
    internal sealed class ServerSettingsStore
    {
        private readonly string _path;

        public ServerSettingsStore(string path)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public ServerSettings LoadOrCreate(Logger logger)
        {
            if (!File.Exists(_path))
            {
                var settings = new ServerSettings();
                Save(settings, logger);
                return settings;
            }

            try
            {
                var json = File.ReadAllText(_path);
                var settings = JsonSerializer.Deserialize(
                    json,
                    ServerSettingsJsonContext.Default.ServerSettings);
                return NormalizeSettings(settings ?? new ServerSettings());
            }
            catch (Exception ex)
            {
                logger.Warning(LocalizationService.Format(
                    LocalizationService.Mark("Failed to read server settings, using defaults: {0}"),
                    ex.Message));
                return NormalizeSettings(new ServerSettings());
            }
        }

        public void Save(ServerSettings settings, Logger logger)
        {
            try
            {
                var directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);
                var json = JsonSerializer.Serialize(
                    settings,
                    ServerSettingsJsonContext.Default.ServerSettings);
                File.WriteAllText(_path, json);
            }
            catch (Exception ex)
            {
                logger.Warning(LocalizationService.Format(
                    LocalizationService.Mark("Failed to save server settings: {0}"),
                    ex.Message));
            }
        }

        private static ServerSettings NormalizeSettings(ServerSettings settings)
        {
            settings.Language = string.IsNullOrWhiteSpace(settings.Language)
                ? "en"
                : settings.Language.Trim();
            return settings;
        }
    }
}
