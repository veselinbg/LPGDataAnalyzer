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
            return _settings ??= Loadpublic();
        }

        public void Save(AppSettings appSettings)
        {
            if (appSettings is null)
                throw new ArgumentNullException(nameof(appSettings));

            var text = JsonSerializer.Serialize(appSettings);
            File.WriteAllText(_filePath, text);

            _settings = appSettings;
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
