using System.Text.Json;

namespace LPGDbAnalyzer
{
    /// <summary>
    /// Settings from appSettings.json.
    /// </summary>
    public sealed class AppSettings
    {
        /// <summary>
        /// SQLite database file. A relative path is resolved against the LPGDbAnalyzer project folder
        /// (the database is kept in the repository, so it travels with a clone and survives Clean/Rebuild),
        /// or against the app's folder when the app does not run from a build of the repository.
        /// </summary>
        public string DatabasePath { get; set; } = Path.Combine("Data", "lpg.db");

        public string ResolvedDatabasePath => Path.GetFullPath(DatabasePath, ProjectFolder ?? AppContext.BaseDirectory);

        /// <summary>
        /// The folder with LPGDbAnalyzer.csproj above the app's folder (bin/Debug/... or bin/x64/Debug/...),
        /// or null when there is none (a published copy of the app).
        /// </summary>
        private static string? ProjectFolder
        {
            get
            {
                for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
                {
                    if (File.Exists(Path.Combine(dir.FullName, "LPGDbAnalyzer.csproj")))
                        return dir.FullName;
                }

                return null;
            }
        }

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
