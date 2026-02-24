namespace LPGDataAnalyzer.Models
{
    internal class Settings
    {
        public const string ALL = "All";

        public static readonly (double Min, double Max, double Label)[] InjectionRanges =
                                                                {
                                                                            (0.0, 1.5, 1.5),
                                                                            (1.5, 2.5, 2.5),
                                                                            (2.5, 3.0, 3.0),
                                                                            (3.0, 3.5, 3.5),
                                                                            (3.5, 4.5, 4.5),
                                                                            (4.5, 5.5, 5.5),
                                                                            (5.5, 6.5, 6.5),
                                                                            (6.5, 7.5, 7.5),
                                                                            (7.5, 8.5, 8.5),
                                                                            (8.5, 9.5, 9.5),
                                                                            (9.5, 10.5, 10.5),
                                                                            (10.5, 15.0, 15.0)
                                                                        };

        public static readonly (int Min, int Max, int Label)[] RpmColumns =
                                                                {
                                                                            (0, 500, 500),
                                                                            (500, 1000, 1000),
                                                                            (1000, 1500, 1500),
                                                                            (1500, 2000, 2000),
                                                                            (2000, 2500, 2500),
                                                                            (2500, 3000, 3000),
                                                                            (3000, 3500, 3500),
                                                                            (3500, 4000, 4000),
                                                                            (4000, 4500, 4500),
                                                                            (4500, 5000, 5000),
                                                                            (5000, 5500, 5500),
                                                                            (5500, 6200, 6200)
                                                                        };

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
        public static readonly string[] LPGTempGroups = [ALL,.. GasTemperatureRanges.Select(t => t.Label)];


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
        public static readonly string[] ReductorTempGroups = [ALL, ..ReductorTemperatureRanges.Select(t => t.Label)];

        public static readonly (double Min, double Max, string Label)[] DrivingModes =
        {
            (0, 2.8,  "Idle"),
            (2.8, 6.5,  "Cruise"),
            (6.5, 8,  "Acceleration"),
            (8, int.MaxValue,  "High load"),
            (int.MinValue, int.MaxValue,  ALL)
        };

        public static readonly (double Min, double Max, string Label)[] MapModes =
        {
            (0, 0.3,  "Slow Down"),
            (0.3, 0.4,  "Idle"),
            (0.4, 0.6,  "Cruise"),
            (0.6, 0.8,  "Acceleration"),
            (0.8, int.MaxValue,  "High load"),
            (int.MinValue, int.MaxValue,  ALL)
        };

    }
}