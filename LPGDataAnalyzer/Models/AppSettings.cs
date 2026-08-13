namespace LPGDataAnalyzer.Models
{
    public class AppSettings
    {
        public string LastSavedFilePath { get; set; } = string.Empty;
        public string LastLoadedFuelTable { get; set; } = string.Empty;

        public string ImagePath { get; set; } =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "Untitled.png");

        /// <summary>
        /// This is last suggested new fuel table based on last analysed data.
        /// </summary>
        public string LastPredictedFuelTable { get; set; } = string.Empty;

        /// <summary>
        /// Point to the folder where your JSON snapshots are stored.
        /// </summary>
        public string HistoryFolder { get; set; } =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "LPGDataAnalyzer",
                "LPGDataAnalyzer",
                "History");

        public string DataFilesFolder { get; set; } =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "MultipointInj",
                "Acquisition");
    }
}