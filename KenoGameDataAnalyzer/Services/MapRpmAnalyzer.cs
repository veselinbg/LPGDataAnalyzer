using LPGDataAnalyzer.Models;
using System.Runtime.Intrinsics.Arm;

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

                    Press = mapArray.Avg(x => x.PRESS).Round(),
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
                                           Diff_P = ((BENZ_b1 - BENZ_b2) / ((BENZ_b1 + BENZ_b2) / 2.0)).ToString("P")
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
                        Diff_P = "0%",
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
                    Diff_P = ((avgBenzB1 - avgBenzB2) / ((avgBenzB1 + avgBenzB2) / 2.0)).ToString("P"),
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
        public static object BuildGrid(DataItem[] data)
        {
            var result = new Dictionary<(int rpm, double inj), dynamic>();

            foreach (var d in data)
            {
                // RPM bucket (columns)
                int rpmLabel =
                    Settings.RpmColumns.First(r => d.RPM > r.Min && d.RPM <= r.Max).Label;

                // Injection bucket (rows)
                double injAvg = (d.BENZ_b1 + d.BENZ_b2) / 2.0;
                double injLabel =
                    Settings.InjectionRanges.First(r => injAvg > r.Min && injAvg <= r.Max).Label;

                var key = (rpmLabel, injLabel);

                if (!result.TryGetValue(key, out var cell))
                {
                    cell = new
                    {
                        Rpm = rpmLabel,
                        Injection = injLabel,
                        Count = 0,

                        SumB1 = 0.0,
                        SumB2 = 0.0,
                        SumT1 = 0.0,
                        SumT2 = 0.0,
                        SumDiff = 0.0
                    };
                }

                result[key] = new
                {
                    cell.Rpm,
                    cell.Injection,
                    Count = cell.Count + 1,

                    SumB1 = cell.SumB1 + d.BENZ_b1,
                    SumB2 = cell.SumB2 + d.BENZ_b2,
                    SumT1 = cell.SumT1 + d.Trim_b1,
                    SumT2 = cell.SumT2 + d.Trim_b2,

                    SumDiff = cell.SumDiff +
                              (d.BENZ_b1 != 0
                                ? ((d.BENZ_b2 - d.BENZ_b1) / d.BENZ_b1) * 100.0
                                : 0)
                };
            }

            // -------------------------
            // FINAL projection
            // -------------------------
            var output = new List<object>();

            foreach (var c in result.Values)
            {
                double avgB1 = c.SumB1 / c.Count;
                double avgB2 = c.SumB2 / c.Count;
                double avgT1 = c.SumT1 / c.Count;
                double avgT2 = c.SumT2 / c.Count;
                double avgDiff = c.SumDiff / c.Count;

                // opposite trim detection
                double oppositeTrimScore =
                    (avgT1 > 0 && avgT2 < 0) || (avgT1 < 0 && avgT2 > 0)
                        ? Math.Abs(avgT1) + Math.Abs(avgT2)
                        : 0;

                double score = Math.Abs(avgDiff) * 2 + oppositeTrimScore;

                output.Add(new
                {
                    c.Rpm,
                    c.Injection,
                    c.Count,

                    AvgB1 = avgB1.Round(),
                    AvgB2 = avgB2.Round(),

                    BDes = avgB1 > avgB2 ? "B2rich" : "B2lean",
                    TrimDes = avgT1 > avgT2 ? "B2rich" : "B2lean",

                    Des = (!(avgB1 > avgB2) && (avgT1 > avgT2)) ? "-" : ((avgB1 > avgB2) && !(avgT1 > avgT2)) ? "+" : "",

                    AvgTrimB1 = avgT1.Round(),
                    AvgTrimB2 = avgT2.Round(),

                    DiffPercent = avgDiff.Round(),

                    OppositeTrimScore = oppositeTrimScore.Round(),
                    Score = score.Round(),

                    FuelCorrection = (-avgDiff / 2.0).Round()
                });
            }

            output = output
    .OrderBy(x => ((dynamic)x).Rpm)
    .ThenBy(x => ((dynamic)x).Injection)
    .ToList();

            return output;
        }
    }
}
