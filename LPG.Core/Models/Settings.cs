namespace LPGDataAnalyzer.Models
{
    public class Settings
    {
        public const string ALL = "All";
        private const int MIN_RPM_IDLE = 650;
        /// <summary>
        /// RPM columns of the fuel map. Defaults to the table below (LPGDataAnalyzer); LPGDbAnalyzer sets
        /// the axes of the fuel table being analyzed with <see cref="SetMapAxes"/>. Every caller reads this
        /// when it runs, so set the axes before parsing or analyzing data.
        /// </summary>
        public static (int Min, int Max, int Label)[] RpmColumns { get; private set; }

        /// <summary>Petrol injection-time rows (ms) of the fuel map. See <see cref="RpmColumns"/>.</summary>
        public static (double Min, double Max, double Label)[] InjectionRanges { get; private set; }

        // Runs after all static field initializers, so the default arrays below exist.
        static Settings()
        {
            RpmColumns = DefaultRpmColumns;
            InjectionRanges = DefaultInjectionRanges;
        }

        /// <summary>
        /// Uses the breakpoints of a fuel table as the map axes. Each cell covers (previous breakpoint,
        /// its breakpoint]; values up to the first breakpoint go to the first cell and values above the
        /// last one to the last cell, as the ECU uses its edge breakpoints there.
        /// </summary>
        public static void SetMapAxes(IReadOnlyList<double> rpmBreakpoints, IReadOnlyList<double> injectionBreakpoints)
        {
            Validate(rpmBreakpoints, "RPM");
            Validate(injectionBreakpoints, "Injection");

            var rpm = new (int Min, int Max, int Label)[rpmBreakpoints.Count];
            for (int i = 0; i < rpm.Length; i++)
            {
                int label = (int)Math.Round(rpmBreakpoints[i]);
                rpm[i] = (i == 0 ? -1 : (int)Math.Round(rpmBreakpoints[i - 1]), i == rpm.Length - 1 ? int.MaxValue : label, label);
            }

            var inj = new (double Min, double Max, double Label)[injectionBreakpoints.Count];
            for (int i = 0; i < inj.Length; i++)
            {
                double label = injectionBreakpoints[i];
                inj[i] = (i == 0 ? -1.0 : injectionBreakpoints[i - 1], i == inj.Length - 1 ? double.MaxValue : label, label);
            }

            RpmColumns = rpm;
            InjectionRanges = inj;
        }

        /// <summary>Back to the built-in axes.</summary>
        public static void ResetMapAxes()
        {
            RpmColumns = DefaultRpmColumns;
            InjectionRanges = DefaultInjectionRanges;
        }

        private static void Validate(IReadOnlyList<double> breakpoints, string name)
        {
            if (breakpoints.Count < 2)
                throw new ArgumentException($"{name} axis needs at least 2 breakpoints.");

            for (int i = 1; i < breakpoints.Count; i++)
                if (breakpoints[i] <= breakpoints[i - 1])
                    throw new ArgumentException($"{name} breakpoints must increase.");
        }

        private static readonly (int Min, int Max, int Label)[] DefaultRpmColumns =
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

        private static readonly (double Min, double Max, double Label)[] DefaultInjectionRanges =
                                                                                    [
                                                                                        (-1.0, 1.9, 1.9),
                                                                                        (1.9, 2.4, 2.4),
                                                                                        (2.4, 2.9, 2.9),
                                                                                        (2.9, 3.4, 3.4),
                                                                                        (3.4, 4.0, 4.0),
                                                                                        (4.0, 4.8, 4.8),
                                                                                        (4.8, 5.8, 5.8),
                                                                                        (5.8, 7.0, 7.0),
                                                                                        (7.0, 8.3, 8.3),
                                                                                        (8.3, 9.6, 9.6),
                                                                                        (9.6, 10.8, 10.8),
                                                                                        (10.8, 15, 15)
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
        public static readonly double[] GasTemperatureCorrectionCoef = {-7d,-5d,-4d,-2d,0d,2d,3d,4d,5d };

        public static readonly string[] LPGTempGroups = [ALL, .. GasTemperatureRanges.Select(t => t.Label)];

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
        public static readonly double[] ReductorTemperatureCorrectionCoef = { -7d,-4d,-1d,0d,0d,0d,0d,0d,0d };
         
        public static readonly string[] ReductorTempGroups = [ALL, .. ReductorTemperatureRanges.Select(t => t.Label)];
        
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
            FastTrim, 
            Trim,
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