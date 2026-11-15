using LPGDataAnalyzer.Models;
using System.ComponentModel;

namespace LPGDbAnalyzer.Data
{
    /// <summary>
    /// A car and its ECU setup (see BACKLOG.md, "Car profiles"). Edited in a PropertyGrid.
    /// </summary>
    public sealed class Car
    {
        [Browsable(false)]
        public long Id { get; set; }

        [Category("1. Car"), DisplayName("Name"), Description("Required. Short name shown in the lists, e.g. \"Mercedes E350 M272\".")]
        public string Name { get; set; } = "";

        [Category("1. Car"), DisplayName("Engine"), Description("Engine code and size, e.g. \"M272 3.0 V6\".")]
        public string? Engine { get; set; }

        [Category("1. Car"), DisplayName("Banks"), Description("Number of cylinder banks (1 or 2).")]
        public int Banks { get; set; } = 2;

        [Category("1. Car"), DisplayName("Modifications"), Description("Engine or intake modifications that change fueling.")]
        public string? Modifications { get; set; }

        [Category("2. Petrol ECU"), DisplayName("Type"), Description("e.g. \"Bosch ME9.7\".")]
        public string? PetrolEcu { get; set; }

        [Category("2. Petrol ECU"), DisplayName("Fast trim step %"), Description("Smallest fast trim change reported over OBD (0.78 for this car).")]
        public double FastTrimStep { get; set; } = 0.78;

        [Category("3. LPG ECU"), DisplayName("Type"), Description("e.g. \"AEB MP6C\".")]
        public string? LpgEcu { get; set; }

        [Category("3. LPG ECU"), DisplayName("Injectors"), Description("Injector brand and nozzle size.")]
        public string? Injectors { get; set; }

        [Category("3. LPG ECU"), DisplayName("Reducer"), Description("Reducer type and rail layout.")]
        public string? Reducer { get; set; }

        [Category("3. LPG ECU"), DisplayName("Reference pressure (bar)"), Description("Calibration pressure of the reducer, used to scale trims.")]
        public double ReferencePressure { get; set; } = 1.49;

        [Category("4. Data"), DisplayName("Trim delay (ms)"), Description("Delay between the engine position and the OBD trim that describes it.")]
        public int TrimDelayMs { get; set; } = 850;

        [Category("5. Notes"), DisplayName("Notes")]
        public string? Notes { get; set; }

        [Browsable(false)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public override string ToString() => Name;
    }

    /// <summary>
    /// A fuel map (gas multiplier table) as it was in the LPG ECU. Values are [injection row][rpm column].
    /// </summary>
    public sealed class FuelTable
    {
        public long Id { get; set; }
        public long CarId { get; set; }
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>RPM breakpoints (columns).</summary>
        public double[] RpmAxis { get; set; } = [];

        /// <summary>Petrol injection time breakpoints in ms (rows).</summary>
        public double[] InjectionAxis { get; set; } = [];

        /// <summary>[injection row][rpm column].</summary>
        public double[][] Values { get; set; } = [];

        /// <summary>The screenshot the table was read from (PNG/JPG bytes), if any.</summary>
        public byte[]? Image { get; set; }
        public string? ImageFileName { get; set; }
        public string? Notes { get; set; }

        /// <summary>True when a screenshot is stored (filled by list queries; the image itself is loaded separately).</summary>
        public bool HasImage { get; set; }

        /// <summary>Number of imported log files (filled by list queries).</summary>
        public int LogCount { get; set; }

        /// <summary>Number of stored predictions made from this table (filled by list queries).</summary>
        public int PredictionCount { get; set; }

        public override string ToString() =>
            $"{CreatedAt:yyyy-MM-dd} {Name} ({RpmAxis.Length}x{InjectionAxis.Length}, {LogCount} logs)";
    }

    /// <summary>
    /// One imported real-time log file, recorded with one fuel table.
    /// </summary>
    public sealed class LogFile
    {
        public long Id { get; set; }
        public long FuelTableId { get; set; }
        public string FileName { get; set; } = "";
        /// <summary>From the file header ("real time data, Friday, November 13, 2026 12:31:19 PM").</summary>
        public DateTime RecordedAt { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.Now;
        public int RowCount { get; set; }
        /// <summary>SHA-256 of the file, so the same file is not imported twice.</summary>
        public string FileHash { get; set; } = "";

        /// <summary>
        /// LPG temperature correction table that was in the ECU while this log was recorded,
        /// one whole number per band of LPG.Core's Settings.GasTemperatureRanges.
        /// </summary>
        public int[] GasTempCoef { get; set; } = [];

        /// <summary>Reducer temperature correction table in the ECU, one per band of Settings.ReductorTemperatureRanges.</summary>
        public int[] ReducerTempCoef { get; set; } = [];

        public override string ToString() => $"{RecordedAt:yyyy-MM-dd HH:mm} {FileName} ({RowCount} rows)";
    }

    /// <summary>
    /// A stored fuel map prediction: the fuel table it started from, the logs it used, every option,
    /// and the result. Maps and counts are [rpm][injection], as LPG.Core's prediction works with them.
    /// </summary>
    public sealed class Prediction
    {
        public long Id { get; set; }
        public long FuelTableId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>The logs (LogFile ids) the prediction was made from.</summary>
        public long[] LogIds { get; set; } = [];

        /// <summary>Readings (grouped samples) used.</summary>
        public int ReadingCount { get; set; }

        public PredictionOptions Options { get; set; } = new();

        public double[] RpmAxis { get; set; } = [];
        public double[] InjectionAxis { get; set; } = [];

        /// <summary>The fuel table as it was in the prediction screen (it may have been edited there).</summary>
        public double?[][] BaseMap { get; set; } = [];
        public double?[][] NewMap { get; set; } = [];
        public int[][] Counts { get; set; } = [];

        public string? Notes { get; set; }

        // Filled by queries.
        public string FuelTableName { get; set; } = "";
        public DateTime FuelTableCreatedAt { get; set; }
    }

    /// <summary>
    /// One raw row of a log, all 22 columns as logged. Nothing is filtered or grouped on import,
    /// so the analysis can change later without importing again.
    /// </summary>
    public sealed class LogRow
    {
        public int TimeMs;      // TEMPO
        public int Rpm;         // GIRI
        public double Lambda1;  // LAMBDA
        public double Gas1;     // GAS
        public double Benz1;    // BENZ
        public double Press;    // PRESS
        public double Map;      // MAP
        public double TempReducer; // T.RID
        public double TempGas;  // T.GAS
        public double Level;    // LIV
        public double Slow1;    // SLOW
        public double Fast1;    // FAST
        public double Ox1;      // OX
        public double Lambda2;  // LAMBDA2
        public double Gas2;     // GAS2
        public double Benz2;    // BENZ2
        public double Slow2;    // SLOW2
        public double Fast2;    // FAST2
        public double Ox2;      // OX2
        public int Marker;      // MARKER
        public int AutoMarker;  // AUTOMARKER
        public int EcuMarker;   // ECUMARKER
    }
}
