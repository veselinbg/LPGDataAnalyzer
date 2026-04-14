using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer.Services
{
    public class MapRpmAnalyzer
    {
        public static object BuildTableByMap(DataItem[] data)
        {

            return Settings.MapModes.Select(map =>
            {
                // Filter by MAP range
                var mapData = data.Where(d => d.MAP > map.Min && d.MAP <= map.Max);

                // Apply Driving range if available
                var drivingRangeItem = Settings.DrivingModes.FirstOrDefault(dr => dr.Label == map.Label);

                if (!drivingRangeItem.Equals(default))
                {
                    mapData = mapData.Where(x => x.BENZ_b1 > drivingRangeItem.Min && x.BENZ_b1 <= drivingRangeItem.Max);
                }

                var rpmRangeItem = Settings.RpmRanges.FirstOrDefault(dr => dr.Label == map.Label);

                if (!rpmRangeItem.Equals(default))
                {
                    mapData = mapData.Where(x => x.RPM > rpmRangeItem.Min && x.RPM <= rpmRangeItem.Max);
                }


                var mapArray = mapData.ToArray(); // Only enumerate once

                // Compute averages
                var avgBenzB1 = mapArray.Avg(x => x.BENZ_b1);
                var avgBenzB2 = mapArray.Avg(x => x.BENZ_b2);

                var avgGasB1 = mapArray.Avg(x => x.GAS_b1);
                var avgGasB2 = mapArray.Avg(x => x.GAS_b2);

                var avgTrimB1 = mapArray.Avg(x => x.Trim_b1);
                var avgTrimB2 = mapArray.Avg(x => x.Trim_b2);

                return new
                {
                    map.Label,

                    BENZ_b1 = avgBenzB1.Round(),
                    BENZ_b2 = avgBenzB2.Round(),

                    Diff_Benz = avgBenzB1.RelDiff(avgBenzB2),
                    Diff_Gas = avgGasB1.RelDiff(avgGasB2),
                    Diff_Trim = (avgTrimB1 - avgTrimB2).ToString("0.##'%'"),

                    PRESS = mapArray.Avg(x => x.PRESS).Round(),
                    Press_Min = mapArray.Min(x => x.PRESS).Round(),
                    Press_Max = mapArray.Max(x => x.PRESS).Round(),

                    AvgTrim = mapArray.Avg(x => x.Trim).Round(),
                    MedianTrim = mapArray.Select(x => x.Trim).Median().Round()
                };
            }).ToArray();
        }
        /// <summary>
        /// 7. Safe economy zones (pre-lambda)
        /*
        Mark cells that are:
        MAP< 0.55 bar
        RPM 1500–3000
        Low injector variance
        Stable pressure
        These are your future lean-cruise zones.
        */
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static object BuildABankAwareLPGBaseMap(DataItem[] data)
        {
            return Settings.MapModes.Select(map =>
            {
                // Filter by MAP range
                var mapData = data.Where(d => d.MAP > map.Min && d.MAP <= map.Max);

                // Apply Driving range if available
                var drivingRangeItem = Settings.DrivingModes.FirstOrDefault(dr => dr.Label == map.Label);
                if (!drivingRangeItem.Equals(default))
                {
                    mapData = mapData.Where(x => x.BENZ_b1 > drivingRangeItem.Min && x.BENZ_b1 <= drivingRangeItem.Max);
                }

                // Apply RPM range if available
                var rpmRangeItem = Settings.RpmRanges.FirstOrDefault(dr => dr.Label == map.Label);
                if (!rpmRangeItem.Equals(default))
                {
                    mapData = mapData.Where(x => x.RPM > rpmRangeItem.Min && x.RPM <= rpmRangeItem.Max);
                }

                var mapArray = mapData.ToArray(); // Enumerate once

                if (!mapArray.Any())
                    return new
                    {
                        map.Label,
                        Lpg1 = 0.0,
                        Lpg2 = 0.0,
                        Diff = 0.0,
                        Diff_P = "0%",
                        DeltaPct = "0%"
                    };

                // Compute averages
                var avgGAS_b1 = mapArray.Average(x => x.GAS_b1);
                var avgGAS_b2 = mapArray.Average(x => x.GAS_b2);

                return new
                {
                    map.Label,
                    Lpg1 = avgGAS_b1.Round(),
                    Lpg2 = avgGAS_b2.Round(),
                    Diff = (avgGAS_b1 - avgGAS_b2).Round(),
                    Diff_P = (Math.Abs(avgGAS_b1 - avgGAS_b2) / ((avgGAS_b1 + avgGAS_b2) / 2.0)).ToString("P"),
                    DeltaPct = (100.0 * (avgGAS_b1 - avgGAS_b2) / avgGAS_b1).Round() + "%"
                };
            }).ToArray();
        }        /// <summary>
                 /// LPG injector dead-time estimation
                 /// </summary>
                 /// <param name="data"></param>
                 /// <returns></returns>
        public static object LpgInjectorDeadTimeEstimation(DataItem[] data)
        {
            return data.GroupBy(g => g.MAP)
                                   .Select(s =>
                                   {
                                       var BENZ_b1 = s.Average(x => x.BENZ_b1).Round();
                                       var BENZ_b2 = s.Average(x => x.BENZ_b2).Round();
                                       return new
                                       {
                                           Map = s.Key,
                                           Avg_BENZ_b1 = BENZ_b1,
                                           Avg_BENZ_b2 = BENZ_b2,
                                           Diff = ((BENZ_b1 - BENZ_b2) / ((BENZ_b1 + BENZ_b2) / 2.0)).ToString("P")
                                       };
                                   }).OrderBy(x => x.Map).ToArray();
        }
        /// <summary>
        /// Bank-to-bank fuel balance analysis
        /// At same RPM + MAP:
        /// Injector times should match(within ~3–5%)
        /// </summary>
        /// <param name="baseValue"></param>
        /// <param name="newValue"></param>
        /// <returns>
        /// |Delta| > 5% → injector flow mismatch, vacuum leak, manifold imbalance
        /// </returns>
        public static object BuildBankToBankfuelBalance(DataItem[] data)
        {
            return Settings.MapModes.Select(map =>
            {
                // Filter by MAP range
                var mapData = data.Where(d => d.MAP > map.Min && d.MAP <= map.Max);

                // Apply Driving range if available
                var drivingRangeItem = Settings.DrivingModes.FirstOrDefault(dr => dr.Label == map.Label);
                if (!drivingRangeItem.Equals(default))
                {
                    mapData = mapData.Where(x => x.BENZ_b1 > drivingRangeItem.Min && x.BENZ_b1 <= drivingRangeItem.Max);
                }

                // Apply RPM range if available
                var rpmRangeItem = Settings.RpmRanges.FirstOrDefault(dr => dr.Label == map.Label);
                if (!rpmRangeItem.Equals(default))
                {
                    mapData = mapData.Where(x => x.RPM > rpmRangeItem.Min && x.RPM <= rpmRangeItem.Max);
                }

                var mapArray = mapData.ToArray(); // Enumerate once

                if (!mapArray.Any())
                    return new
                    {
                        map.Label,
                        Bank1Ms = 0.0,
                        Bank2Ms = 0.0,
                        Diff = 0.0,
                        P_Diff = "0%",
                        DeltaPct = "0%"
                    };

                // Compute averages
                var avgBenzB1 = mapArray.Average(x => x.BENZ_b1);
                var avgBenzB2 = mapArray.Average(x => x.BENZ_b2);

                return new
                {
                    map.Label,
                    Bank1Ms = avgBenzB1.Round(),
                    Bank2Ms = avgBenzB2.Round(),
                    Diff = (avgBenzB1 - avgBenzB2).Round(),
                    P_Diff = ((avgBenzB1 - avgBenzB2) / ((avgBenzB1 + avgBenzB2) / 2.0)).ToString("P"),
                    DeltaPct = (100.0 * (avgBenzB1 - avgBenzB2) / avgBenzB1).Round() + "%"
                };
            }).ToArray();
        }
        /// <summary>
//         Uses map filters, driving, and RPM ranges.
//        Keeps RPM bins grouping(250 RPM steps, configurable).
//Calculates fuel and LPG bank averages and differences.
//Includes Trim and Pressure stats like in BuildTableByMap.
//Handles empty filtered sets safely.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static object BuildEnhancedBankMap(DataItem[] data)
        {
            return Settings.MapModes.SelectMany(map =>
            {
                // Filter by MAP range
                var mapData = data.Where(d => d.MAP > map.Min && d.MAP <= map.Max);

                // Apply Driving range if available
                var drivingRangeItem = Settings.DrivingModes.FirstOrDefault(dr => dr.Label == map.Label);
                if (!drivingRangeItem.Equals(default))
                {
                    mapData = mapData.Where(x => x.BENZ_b1 > drivingRangeItem.Min && x.BENZ_b1 <= drivingRangeItem.Max);
                }

                // Apply RPM range if available
                var rpmRangeItem = Settings.RpmRanges.FirstOrDefault(dr => dr.Label == map.Label);
                if (!rpmRangeItem.Equals(default))
                {
                    mapData = mapData.Where(x => x.RPM > rpmRangeItem.Min && x.RPM <= rpmRangeItem.Max);
                }

                // Group by RPM bins (e.g., 250 rpm steps)
                var groupedByRpm = mapData
                    .GroupBy(s => (int)Math.Round(s.RPM / 250.0) * 250);

                return groupedByRpm.Select(g =>
                {
                    var mapArray = g.ToArray();

                    if (!mapArray.Any())
                        return new
                        {
                            map.Label,
                            Rpm = g.Key,
                            Bank1Ms = 0.0,
                            Bank2Ms = 0.0,
                            DeltaBenzPct = "0%",
                            Lpg1 = 0.0,
                            Lpg2 = 0.0,
                            DeltaLpgPct = "0%",
                            AvgTrim = 0.0,
                            MedianTrim = 0.0,
                            PRESS = 0.0,
                            Press_Min = 0.0,
                            Press_Max = 0.0
                        };

                    // Fuel (BENZ) averages
                    var avgBenzB1 = mapArray.Average(x => x.BENZ_b1);
                    var avgBenzB2 = mapArray.Average(x => x.BENZ_b2);

                    // LPG (GAS) averages
                    var avgGasB1 = mapArray.Average(x => x.GAS_b1);
                    var avgGasB2 = mapArray.Average(x => x.GAS_b2);

                    // Trim and pressure
                    var avgTrim = mapArray.Average(x => x.Trim);
                    var medianTrim = mapArray.Select(x => x.Trim).Median();
                    var avgPress = mapArray.Average(x => x.PRESS);
                    var minPress = mapArray.Min(x => x.PRESS);
                    var maxPress = mapArray.Max(x => x.PRESS);

                    return new
                    {
                        map.Label,
                        Rpm = g.Key,

                        // BENZ
                        Bank1Ms = avgBenzB1.Round(),
                        Bank2Ms = avgBenzB2.Round(),
                        DeltaBenzPct = (100.0 * (avgBenzB1 - avgBenzB2) / avgBenzB1).Round() + "%",

                        // LPG
                        Lpg1 = avgGasB1.Round(),
                        Lpg2 = avgGasB2.Round(),
                        DeltaLpgPct = ((avgGasB1 - avgGasB2) / ((avgGasB1 + avgGasB2) / 2.0)).ToString("P"),

                        // Trim / Pressure
                        AvgTrim = avgTrim.Round(),
                        MedianTrim = medianTrim.Round(),
                        PRESS = avgPress.Round(),
                        Press_Min = minPress.Round(),
                        Press_Max = maxPress.Round()
                    };
                });
            })
            .OrderBy(x => x.Rpm)
            .ThenBy(x => x.Label)
            .ToArray();
        }
    }
}
