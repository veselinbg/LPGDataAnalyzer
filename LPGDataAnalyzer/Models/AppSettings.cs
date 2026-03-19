
namespace LPGDataAnalyzer.Models
{
    public class AppSettings
    {
        public string LastSavedFilePath { get; set; } = string.Empty;
        public string LastLoadedFuelTable { get; set; } = string.Empty;
        public string ImagePath { get; set; } = @"C:\Users\veselin.ivanov\Desktop\Untitled.png";

        /// <summary>
        /// This is last suggested new fuel table based on last analysed data.
        /// </summary>
        public string LastPredictedFuelTable { get; set; } = string.Empty;

        /// <summary>
        ///  Point to the folder where your JSON snapshots are stored.
        /// </summary>
        public string HistoryFolder { get; set; } = @"C:\Users\veselin.ivanov\Downloads\LPGDataAnalyzer\LPGDataAnalyzer\History";

    }
}
