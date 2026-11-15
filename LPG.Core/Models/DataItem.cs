using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LPGDataAnalyzer.Models
{
    public class DataItem
    {
        /// <summary>
        /// Sequence number. After parsing it is the index of the OBD reading (group) within its file, not time.
        /// </summary>
        public int TEMPO { get; set; }
        /// <summary>
        /// Real time in ms from the log's TEMPO column (first row of the group).
        /// </summary>
        public int TimeMs { get; set; }
        /// <summary>
        /// Name of the log file this item was parsed from. Consecutive items are comparable only within the same file.
        /// </summary>
        public string? SourceFile { get; set; }
        /// <summary>
        /// Share (0..1) of the group's raw rows that fall in the group's most frequent fuel-map cell.
        /// Low values mean the OBD reading was taken while moving across cells. Defaults to 1 for items not built by the parser.
        /// </summary>
        public double Stability { get; set; } = 1;
        public int RPM { get; set; }
        //public double LAMBDA_b1 { get; set; }
        public double GAS_b1 { get; set; }
        public double BENZ_b1 { get; set; }
        public double PRESS { get; set; }
        public double MAP { get; set; }
        public double Temp_RID { get; set; }
        public double Temp_GAS { get; set; }
       // public double LIV { get; set; }
        public double SLOW_b1 { get; set; }
        public double FAST_b1 { get; set; }
        public double OX_b1 { get; set; }
        //public double LAMBDA_b2 { get; set; }
        public double GAS_b2 { get; set; }
        public double BENZ_b2 { get; set; }
        public double SLOW_b2 { get; set; }
        public double FAST_b2 { get; set; }
        public double OX_b2 { get; set; }
        //public int MARKER { get; set; }
        //public int AUTOMARKER { get; set; }
        //public int ECUMARKER { get; set; }

        // Calculated fields
        public double Ratio_b1 { get; set; }
        public double Ratio_b2 { get; set; }
        public double RatioDifference { get; set; }
        /// <summary>
        /// Fast Trim sum of two banks /2
        /// </summary>
        public double Fast { get; set; }
        /// <summary>
        /// Slow Trim sum of two banks /2
        /// </summary>
        public double Slow { get; set; }
        /// <summary>
        /// Slow + fast trim for two banks /2
        /// </summary>
        public double Trim{ get; set; }
        public double Trim_b1 { get; set; }
        public double Trim_b2 { get; set; }

        public double TrimDiff { get; set; }
        /// <summary>
        /// AFR before correction if is less then 15.6 is lean if is higher is rich mixure
        /// </summary>
        public double AFR { get; set; }
        /// <summary>
        /// AFR before correction if is less then 15.6 is lean if is higher is rich mixure
        /// </summary>
        public double AFR_b1 { get;set; }        
        /// <summary>
        /// AFR before correction if is less then 15.6 is lean if is higher is rich mixure
        /// </summary>
        public double AFR_b2 { get; set; }
        /// <summary>
        /// GAS time sum of two banks /2
        /// </summary>
        public double GAS { get; set; }
        /// <summary>
        /// BENZ time sum of two banks /2
        /// </summary>
        public double BENZ { get; set; }
        /// <summary>
        /// This is difference between Benz_b1 and Benz_b2 in %
        /// </summary>
        public double BENZ_Diff { get; set; }

        /// <summary>
        /// Engine position of every raw log row in this group. Used for the ECU's
        /// interpolation weights, which are calculated per row and then averaged.
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        public EnginePosition[] Positions { get; set; } = [];

        /// <summary>
        /// Engine position of the raw rows that were logged <see cref="AppSettings.TrimDelayMs"/>
        /// before this group's rows. The OBD trim of a group describes the mixture at that earlier
        /// moment. Empty when the file has no rows that far back.
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        public EnginePosition[] DelayedPositions { get; set; } = [];
    }

    /// <summary>
    /// Engine position of one raw log row: RPM and petrol injection time of each bank.
    /// </summary>
    public readonly record struct EnginePosition(int Rpm, double Benz1, double Benz2);
}
