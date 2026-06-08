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
                var driving = Settings.DrivingModes
                    .FirstOrDefault(x => x.Label == map.Label);

                var rpm = Settings.RpmRanges
                    .FirstOrDefault(x => x.Label == map.Label);

                var filtered = data.Where(d =>
                    d.MAP > map.Min &&
                    d.MAP <= map.Max &&
                    (string.IsNullOrEmpty(driving.Label) ||
                        (d.BENZ_b1 > driving.Min && d.BENZ_b1 <= driving.Max)) &&
                    (string.IsNullOrEmpty(rpm.Label) ||
                        (d.RPM > rpm.Min && d.RPM <= rpm.Max)));

                var count = filtered.Count();

                if (count == 0)
                {
                    return new
                    {
                        map.Label,
                        Bank1Ms = 0.0,
                        Bank2Ms = 0.0,
                        Diff = 0.0,
                        Diff_P = "0%",
                        DeltaPct = "0%"
                    };
                }

                var avgB1 = filtered.Average(x => x.BENZ_b1);
                var avgB2 = filtered.Average(x => x.BENZ_b2);

                var diff = avgB1 - avgB2;
                var avg = (avgB1 + avgB2) / 2.0;

                return new
                {
                    map.Label,
                    Bank1Ms = avgB1.Round(),
                    Bank2Ms = avgB2.Round(),
                    Diff = diff.Round(),
                    Diff_P = (avg != 0 ? diff / avg : 0).ToString("P"),
                    DeltaPct = avgB1 != 0
                        ? (100.0 * diff / avgB1).Round() + "%"
                        : "0%"
                };
            }).ToArray();
        }
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
            int rows = Settings.InjectionRanges.Length;
            int cols = Settings.RpmColumns.Length;

            var sumB1 = new double[rows, cols];
            var sumB2 = new double[rows, cols];
            var sumT1 = new double[rows, cols];
            var sumT2 = new double[rows, cols];
            var sumDiff = new double[rows, cols];
            var count = new int[rows, cols];

            foreach (var d in data)
            {
                int row = d.GetInjectionIndex();
                int col = d.GetRpmIndex();

                if (row < 0 || col < 0)
                    continue;

                if (Math.Abs(d.BENZ_b1) < 1e-9)
                    continue;

                double diff = ((d.BENZ_b2 - d.BENZ_b1) / d.BENZ_b1) * 100.0;

                sumB1[row, col] += d.BENZ_b1;
                sumB2[row, col] += d.BENZ_b2;
                sumT1[row, col] += d.Trim_b1;
                sumT2[row, col] += d.Trim_b2;
                sumDiff[row, col] += diff;
                count[row, col]++;
            }

            var output = new List<object>();

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (count[r, c] == 0)
                        continue;

                    double avgB1 = sumB1[r, c] / count[r, c];
                    double avgB2 = sumB2[r, c] / count[r, c];
                    double avgT1 = sumT1[r, c] / count[r, c];
                    double avgT2 = sumT2[r, c] / count[r, c];
                    double avgDiff = sumDiff[r, c] / count[r, c];

                    double oppositeTrimScore =
                        (avgT1 > 0 && avgT2 < 0) || (avgT1 < 0 && avgT2 > 0)
                            ? Math.Abs(avgT1) + Math.Abs(avgT2)
                            : 0;

                    double score = Math.Abs(avgDiff) * 2 + oppositeTrimScore;

                    output.Add(new
                    {
                        Rpm = Settings.RpmColumns[c].Label,
                        Injection = Settings.InjectionRanges[r].Label,
                        Count = count[r, c],

                        AvgB1 = avgB1.Round(),
                        AvgB2 = avgB2.Round(),

                        BDes = avgB1 > avgB2 ? "B2rich" : "B2lean",
                        TrimDes = avgT1 > avgT2 ? "B2rich" : "B2lean",

                        Des =
                            (!(avgB1 > avgB2) && (avgT1 > avgT2)) ? "-" :
                            ((avgB1 > avgB2) && !(avgT1 > avgT2)) ? "+" :
                            "",

                        AvgTrimB1 = avgT1.Round(),
                        AvgTrimB2 = avgT2.Round(),

                        DiffPercent = avgDiff.Round(),

                        OppositeTrimScore = oppositeTrimScore.Round(),
                        Score = score.Round(),

                        FuelCorrection = (-avgDiff / 2.0).Round()
                    });
                }
            }

            return output
                .OrderBy(x => ((dynamic)x).Rpm)
                .ThenBy(x => ((dynamic)x).Injection)
                .ToList();
        }
        public static string[,] BuildMarkers(DataItem[] data)
        {
            int injs = Settings.InjectionRanges.Length;
            int rpms = Settings.RpmColumns.Length;

            var sumB1 = new double[injs, rpms];
            var sumB2 = new double[injs, rpms];
            var sumT1 = new double[injs, rpms];
            var sumT2 = new double[injs, rpms];
            var count = new int[injs, rpms];

            foreach (var d in data)
            {
                int inj = d.GetInjectionIndex();
                int rpm = d.GetRpmIndex();

                if (inj < 0 || rpm < 0)
                    continue;

                sumB1[inj, rpm] += d.BENZ_b1;
                sumB2[inj, rpm] += d.BENZ_b2;
                sumT1[inj, rpm] += d.Trim_b1;
                sumT2[inj, rpm] += d.Trim_b2;
                count[inj, rpm]++;
            }

            // 🔥 transposed output intentionally
            var markers = new string[rpms, injs];

            for (int rpm = 0; rpm < rpms; rpm++)
            {
                for (int inj = 0; inj < injs; inj++)
                {
                    if (count[inj, rpm] == 0)
                    {
                        markers[rpm, inj] = "";
                        continue;
                    }

                    double avgB1 = sumB1[inj, rpm] / count[inj, rpm];
                    double avgB2 = sumB2[inj, rpm] / count[inj, rpm];
                    double avgT1 = sumT1[inj, rpm] / count[inj, rpm];
                    double avgT2 = sumT2[inj, rpm] / count[inj, rpm];

                    markers[rpm, inj] =
                        (!(avgB1 > avgB2) && (avgT1 > avgT2)) ? "-" :
                        ((avgB1 > avgB2) && !(avgT1 > avgT2)) ? "+" :
                        "";
                }
            }

            return markers;
        }
    }
}
