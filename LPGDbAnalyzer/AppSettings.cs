using System.Text.Json;

namespace LPGDbAnalyzer
{
    /// <summary>
    /// Settings from appSettings.json. Relative paths are resolved against the app's output folder.
    /// </summary>
    public sealed class AppSettings
    {
        /// <summary>SQLite database file.</summary>
        public string DatabasePath { get; set; } = Path.Combine("Data", "lpg.db");

        public string ResolvedDatabasePath => Path.Combine(AppContext.BaseDirectory, DatabasePath);

        /// <summary>Tesseract language data, copied to the output folder by the build.</summary>
        public static string TessdataPath => Path.Combine(AppContext.BaseDirectory, "tessdata");

        public static AppSettings Load()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "appSettings.json");

            AppSettings settings;
            try
            {
                settings = File.Exists(path)
                    ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new AppSettings()
                    : new AppSettings();
            }
            catch (JsonException ex)
            {
                // In JSON a backslash must be doubled ("Data\\lpg.db"); a forward slash ("Data/lpg.db") also works.
                throw new InvalidOperationException($"'{path}' is not valid JSON: {ex.Message}", ex);
            }

            Validate(settings, path);
            return settings;
        }

        /// <summary>All settings checks are here.</summary>
        private static void Validate(AppSettings settings, string path)
        {
            if (string.IsNullOrWhiteSpace(settings.DatabasePath))
                throw new InvalidOperationException($"'{nameof(DatabasePath)}' is not set in '{path}'.");
        }
    }
}
