namespace LPGDataAnalyzer.Models
{
    internal class Settings
    {
        public const string ALL = "All";

        public static readonly (int Min, int Max, int Label)[] RpmColumns =
                                                                                    [
                                                                                        (0, 700, 700),
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
                                                                                            (0.0, 1.9, 1.9),
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
                                                                                            (11.5, 13.5, 13.5)
                                                                                        ];
        public static readonly (int Min, int Max, string Label)[] GasTemperatureRanges =
        {
            (int.MinValue, 0,  "Temp_to_0"),
            (0, 10,  "Temp_0_10"),
            (11, 20, "Temp_21_30"),
            (31, 35, "Temp_31_35"),
            (36, 40, "Temp_36_40"),
            (41, 50, "Temp_41_50"),
            (51, 60, "Temp_51_60"),
            (61, 70, "Temp_61_70"),
            (71, int.MaxValue, "Temp_71_Over")
        };
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
            (int.MinValue, 20,  "Temp_to_20"),
            (21, 25,  "Temp_21_25"),
            (26, 30, "Temp_26_30"),
            (31, 35,  "Temp_31_35"),
            (36, 40,  "Temp_36_40"),
            (41, 50, "Temp_41_50"),
            (51, 60, "Temp_51_60"),
            (61, 70, "Temp_61_70"),
            (71, int.MaxValue, "Temp_71_over"),
        };
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
            (0, 0.28,  "Slow Down"),
            (0.28, 0.4,  "Idle"),
            (0.4, 0.6,  "Cruise"),
            (0.6, 0.8,  "Acceleration"),
            (0.8, int.MaxValue,  "High load"),
        };

        public static readonly (double Min, double Max, string Label)[] MapModes = [(int.MinValue, int.MaxValue, ALL), .. MapRanges];
        public enum Aggregation
        {
            Median,
            Min,
            Max,
            Average
        }
    }
}