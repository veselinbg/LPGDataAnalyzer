using LPGDataAnalyzer.Models;
using System.Text.Json;

namespace LPGDataAnalyzer
{
    internal class AppSettingManager
    {
        private string FILE_PATH = "appSettings.json";
        public AppSettings Load()
        {
            if(File.Exists(FILE_PATH))
            {
                var text = File.ReadAllText(FILE_PATH);

                if (!string.IsNullOrEmpty(text))
                {
                    var result = JsonSerializer.Deserialize<AppSettings>(text);
                    
                    return result is null? new() : result;  
                }
            }
            return new();
        }
        public void Save(AppSettings appSettings)
        {
            if (appSettings is null)
            {
                throw new NullReferenceException("appSettings object is null.");
            }
            var text = JsonSerializer.Serialize(appSettings);

            File.WriteAllText(FILE_PATH, text);
        }
    }
}
