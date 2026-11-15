using LPGDataAnalyzer.Models;
using System.Text.Json;

namespace LPGDataAnalyzer
{
    public class AppSettingManager 
    {
        private readonly string _filePath;
        private AppSettings? _settings;

        public AppSettingManager(string filePath = "appSettings.json")
        {
            _filePath = filePath;
        }

        public AppSettings Load()
        {
            if (_settings is null)
            {
                var settings = Loadpublic();
                Validate(settings);
                _settings = settings;
            }

            return _settings;
        }

        public void Save(AppSettings appSettings)
        {
            if (appSettings is null)
                throw new ArgumentNullException(nameof(appSettings));

            Validate(appSettings);

            var text = JsonSerializer.Serialize(appSettings);
            File.WriteAllText(_filePath, text);

            _settings = appSettings;
        }

        /// <summary>
        /// Checks that required settings are present.
        /// </summary>
        /// <exception cref="InvalidOperationException">When a required setting is missing.</exception>
        public void Validate(AppSettings appSettings)
        {
            if (string.IsNullOrWhiteSpace(appSettings.ImagePath))
                throw new InvalidOperationException(
                    $"'{nameof(AppSettings.ImagePath)}' is not set in '{_filePath}'.");

            if (appSettings.TrimDelayMs < 0)
                throw new InvalidOperationException(
                    $"'{nameof(AppSettings.TrimDelayMs)}' in '{_filePath}' must be 0 or more.");
        }

        private AppSettings Loadpublic()
        {
            if (File.Exists(_filePath))
            {
                var text = File.ReadAllText(_filePath);

                if (!string.IsNullOrEmpty(text))
                {
                    return JsonSerializer.Deserialize<AppSettings>(text) ?? new AppSettings();
                }
            }

            return new AppSettings();
        }
    }
}
