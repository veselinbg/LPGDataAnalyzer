using System.Text.Json.Serialization;

namespace LPGDataAnalyzer.Models
{
    public class AppSettings
    {
        public string LastSavedFilePath { get; set; } = string.Empty;
        public string LastLoadedFuelTable { get; set; } = string.Empty;

        /// <summary>
        /// Screenshot of the ECU fuel map to OCR. Relative paths resolve against the app's output folder.
        /// </summary>
        public string ImagePath { get; set; } = string.Empty;

        /// <summary>
        /// <see cref="ImagePath"/> as an absolute path.
        /// </summary>
        [JsonIgnore]
        public string ResolvedImagePath => Path.GetFullPath(ImagePath, AppContext.BaseDirectory);

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

        /// <summary>
        /// Delay (ms) between the engine position and the OBD fuel trim that describes it
        /// (oxygen sensor + petrol ECU + ~1 s OBD refresh). Measured on the logs: 600–1100 ms.
        /// Used by the "Trim Delay" option of the prediction.
        /// </summary>
        public int TrimDelayMs { get; set; } = 850;

        public string DataFilesFolder { get; set; } =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "MultipointInj",
                "Acquisition");
    }
}