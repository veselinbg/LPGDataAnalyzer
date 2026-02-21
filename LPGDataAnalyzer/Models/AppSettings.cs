
namespace LPGDataAnalyzer.Models
{
    internal class AppSettings
    {
        public string LastSavedFilePath { get; set; } = string.Empty;
        public string LastLoadedFuelTable { get; set; } = string.Empty;
        public string ImagePath { get; set; } = @"C:\Users\veselin.ivanov\Desktop\Untitled.png";

        /// <summary>
        /// This is last suggested new fuel table based on last analysed data.
        /// </summary>
        public string LastPredictedFuelTable { get; set; } = string.Empty;
    }
}
