namespace LPGDataAnalyzer.Models
{
    public class Settings
    {
        public const string ALL = "All";

        public static readonly (int Min, int Max, int Label)[] RpmColumns =
                                                                                    [
                                                                                        (-1, 700, 700),
                                                                                        (700, 1000, 1000),
                                                                                        (1000, 1400, 1400),
                                                                                        (1400, 1800, 1800),
                                                                                        (1800, 2200, 2200),
                                                                                        (2200, 2600, 2600),
                                                                                        (2600, 3000, 3000),
                                                                                        (3000, 3400, 3400),
                                                                                        (3400, 4000, 4000),
                                                                                        (4000, 4700, 4700),
                                                                                        (4700, 5400, 5400),
                                                                                        (5400, 6200, 6200)
                                                                                    ];

        public static readonly (double Min, double Max, double Label)[] InjectionRanges =
                                                                                        [
                                                                                            (-1.0, 1.9, 1.9),
                                                                                            (1.9, 2.4, 2.4),
                                                                                            (2.4, 2.9, 2.9),
                                                                                            (2.9, 3.4, 3.4),
                                                                                            (3.4, 4.0, 4.0),
                                                                                            (4.0, 4.8, 4.8),
                                                                                            (4.8, 5.8, 5.8),
                                                                                            (5.8, 7.0, 7.0),
                                                                                            (7.0, 8.5, 8.5),
                                                                                            (8.5, 10.0, 10.0),
                                                                                            (10.0, 11.5, 11.5),
                                                                                            (11.5, 13.5, 13.5),
                                                                                            (13.5, 20, 20)
                                                                                        ];
        public static readonly (int Min, int Max, string Label)[] GasTemperatureRanges =
        {
            (-20, 0,  "Temp_-20_0"),
            (0, 10, "Temp_0_10"),
            (10, 20, "Temp_11_20"),
            (20, 30, "Temp_21_30"),
            (30, 40, "Temp_31_40"),
            (40, 50, "Temp_41_50"),
            (50, 60, "Temp_51_60"),
            (60, 70, "Temp_61_70"),
            (70, int.MaxValue, "Temp_71_Over")
        };
        public static readonly double[] GasTemperatureCorrectionCoef = {-7d,-5d,-4d,0d,0d,2d,4d,5d,6d };
        public static readonly string[] LPGTempGroups = [ALL, .. GasTemperatureRanges.Select(t => t.Label)];

        public static string[] GetExistGasTemperatureRanges(DataItem[] data)
        {
            // Guard against null or empty
            if (data == null || data.Length == 0)
                return [];

            var usedRanges = GasTemperatureRanges
                .Where(range => data.Any(item => item.Temp_GAS >= range.Min && item.Temp_GAS <= range.Max)).Select(t => t.Label);

            return [ALL, .. usedRanges];
        }

        public static readonly (int Min, int Max, string Label)[] ReductorTemperatureRanges =
        {
            (int.MinValue, 20,  "Temp_0_20"),
            (20, 25, "Temp_21_25"),
            (25, 30, "Temp_26_30"),
            (30, 35, "Temp_31_35"),
            (35, 40, "Temp_36_40"),
            (40, 50, "Temp_41_50"),
            (50, 60, "Temp_51_60"),
            (60, 70, "Temp_61_70"),
            (70, int.MaxValue, "Temp_71_over"),
        };
        public static readonly double[] ReductorTemperatureCorrectionCoef = { -11d,-6d,-1d,0d,0d,0d,0d,0d,0d };
         
        public static readonly string[] ReductorTempGroups = [ALL, .. ReductorTemperatureRanges.Select(t => t.Label)];
        public static string[] GetExistReductorTempGroups(DataItem[] data)
        {
            // Guard against null or empty
            if (data == null || data.Length == 0)
                return [];

            var usedRanges = ReductorTemperatureRanges
                .Where(range => data.Any(item => item.Temp_RID >= range.Min && item.Temp_RID <= range.Max)).Select(t => t.Label);

            return [ALL, .. usedRanges];
        }
        public static readonly (double Min, double Max, string Label)[] DrivingRanges =
        {
            (0, 2.8,  "Idle"),
            (2.8, 6.5,  "Cruise"),
            (6.5, 8,  "Acceleration"),
            (8, int.MaxValue,  "High load"),
        };

        public static readonly (double Min, double Max, string Label)[] DrivingModes = [(int.MinValue, int.MaxValue, ALL), .. DrivingRanges];


        public static readonly (double Min, double Max, string Label)[] MapRanges =
       {
            (0, 0.33,  "Slow Down"),
            (0.33, 0.4,  "Idle"),
            (0.4, 0.6,  "Cruise"),
            (0.6, 0.8,  "Acceleration"),
            (0.8, int.MaxValue,  "High load"),
        };
        const double MIN_RPM_IDLE = 650;
        public static readonly (double Min, double Max, string Label)[] RpmRanges =
        {
            (0, MIN_RPM_IDLE,  "Idle"),
            (MIN_RPM_IDLE, int.MaxValue,  "Slow Down"),
            (MIN_RPM_IDLE, int.MaxValue,  "Cruise"),
            (MIN_RPM_IDLE, int.MaxValue,  "Acceleration"),
            (MIN_RPM_IDLE, int.MaxValue,  "High load"),
        };
        public static readonly (double Min, double Max, string Label)[] MapModes = [(int.MinValue, int.MaxValue, ALL), .. MapRanges];
        public enum Aggregation
        {
            Median,
            Min,
            Max,
            Average
        }
        public enum FieldsToShow
        {
            Trim,
            FastTrim,
            Ratio,
            GasTime,
            Press,
            AFR
        }
        public enum Banks
        {
            ALL, B1, B2
        }
    }
}