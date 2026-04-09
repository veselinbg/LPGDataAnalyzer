using LPGDataAnalyzer.Models;
using System.Runtime.Intrinsics.Arm;

namespace LPGDataAnalyzer.Services
{
    public class TempeatureAnalyzer
    {
        public static object GasTemperatureRanges(DataItem[] data)
        {
            return data
                .GroupBy(x =>
                    Settings.GasTemperatureRanges.First(r =>
                        x.Temp_GAS >= r.Min && x.Temp_GAS <= r.Max))
                .Select(g =>
                {
                    var bank1 = g.Select(x=>x.Trim_b1);
                    var bank2 = g.Select(x => x.Trim_b2);
                    var tempRid = g.Select(y => y.Temp_RID);

                    return new
                    {
                        LPG_Range = g.Key.Label,

                        AverageTrim = g.Average(x => x.Trim).Round(),

                        AverageB1 = bank1.Average().Round(),
                        AverageB2 = bank2.Average().Round(),

                        MinB1 = bank1.Min().Round(),
                        MinB2 = bank2.Min().Round(),

                        MaxB1 = bank1.Max().Round(),
                        MaxB2 = bank2.Max().Round(),

                        MinTempRed = tempRid.Min().Round(),
                        MaxTempRed = tempRid.Max().Round(),

                        AveragePressure = g.Average(y => y.PRESS).Round(),
                        Count = g.Count()
                    };
                })
                .ToArray();
        }

        public static object ReducerTemperatureRanges(DataItem[] data)
        {
            return data.GroupBy(x =>
                    Settings.ReductorTemperatureRanges.First(r =>
                        x.Temp_RID >= r.Min && x.Temp_RID <= r.Max))
                .Select(x =>
                {
                    var bank1 = x.Select(x => x.Trim_b1);
                    var bank2 = x.Select(x => x.Trim_b2);
                    var Temp_GAS = x.Select(y => y.Temp_GAS);

                    return new
                    {
                        REDUCER_Temp = x.Key.Label,
                        AverageTrim = x.Average(x => x.Trim).Round(),
                        AverageB1 = bank1.Average().Round(),
                        AverageB2 = bank2.Average().Round(),
                        MinB1 = bank1.Min().Round(),
                        MinB2 = bank2.Min().Round(),
                        MaxB1 = bank1.Max().Round(),
                        MaxB2 = bank2.Max().Round(),
                        MinTempGas = Temp_GAS.Min().Round(),
                        MaxTempGas = Temp_GAS.Max().Round(),
                        AveragePressure = x.Average(y => y.PRESS).Round(),
                        Count = x.Count()
                    };
                }).ToArray();
        }
        /// <summary>
        /// Reducer thermal lag analysis
        /// This explains why LPG sometimes feels “off” after warmup.
        /// LPG temp rises before reducer temp
        /// Pressure changes lag reducer temp
        /// Lag = thermal inertia → tune warm-up enrichment
        /// There is filter by gas bank 1 and bank 2. 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static object ReducerThermalLag(DataItem[] data)
        {
            return data.Where(s => Filter.GasBanks(s))
                .Zip(data.Skip(1), (a, b) => new
                {
                    ReducerDelta = Math.Abs((b.Temp_RID - a.Temp_RID).Round()),
                    PressureDelta = Math.Abs((b.PRESS - a.PRESS).Round())
                })
                .Where(x => x.ReducerDelta > 0 || x.PressureDelta > 0)
                .OrderByDescending(x => x.ReducerDelta)
                .ThenByDescending(y => y.PressureDelta)
                .Distinct().ToArray();
        }
        /// <summary>
        /// LPG temp vs injector time (normalized)
        /// At same RPM + MAP: Injector_ms should increase smoothly as LPG temp drops
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static object InjectionTimeByGasTemperature(DataItem[] data)
        {
            return data
                                .Where(s => Filter.GasBanks(s))
                                .GroupBy(s => Math.Round(s.Temp_GAS / 5) * 5)
                                .Select(g => new
                                {
                                    Temp = g.Key,
                                    AvgLpg1Ms = g.Average(x => x.GAS_b1).Round(),
                                    AvgLpg2Ms = g.Average(x => x.GAS_b2).Round(),
                                    StdDev1 = (g.Select(x => x.GAS_b1)).StdDev().Round(),
                                    StdDev2 = (g.Select(x => x.GAS_b2)).StdDev().Round()
                                })
                                .OrderBy(x => x.Temp).ToArray();
        }
        public static object TemperatureExtremesBySlowTrim(DataItem[] data)
        {
            var result = data
                .GroupBy(d => new { d.SLOW_b1, d.SLOW_b2 })
                .Select(g => new
                {
                     g.Key.SLOW_b1,
                     g.Key.SLOW_b2,
                    MinTempRID = g.Min(x => x.Temp_RID),
                    MaxTempRID = g.Max(x => x.Temp_RID),
                    MinTempGAS = g.Min(x => x.Temp_GAS),
                    MaxTempGAS = g.Max(x => x.Temp_GAS),
                    Count = g.Count()
                })
                .ToList();

            return result;
        }
        public static object AverageTrimByGasTemperature(DataItem[] data)
        {
            var result = data
                .GroupBy(d => d.Temp_GAS)
                .Select(g => new
                {
                    Temp_GAS = g.Key,
                    AvgTrim = g.Average(x => x.Trim).Round(),
                    Count = g.Count()
                })
                .OrderBy(r => r.Temp_GAS) // optional but useful for analysis
                .ToList();

            return result;
        }
    }
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
            return data.Where(s => Filter.GasBanks(s))//&& s.MAP < 0.45 && s.RPM < 1500)
                                   .GroupBy(g => g.MAP)
                                   .Select(s =>
                                   {
                                       var BENZ_b1 = s.Average(x => x.BENZ_b1).Round();
                                       var BENZ_b2 = s.Average(x => x.BENZ_b2).Round();
                                       return new
                                       {
                                           Map = s.Key,
                                           Average_BENZ_b1 = BENZ_b1,
                                           Average_BENZ_b2 = BENZ_b2,
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
    public class Analyzer
    {
        public static DataItem[] FilterByTemp(
            DataItem[] data,
            List<string> sLPGTempGroups,
            List<string> sReductorTempGroups)
        {
            if (data is null)
                return [];

            // If nothing selected → treat as ALL
            bool allGas = sLPGTempGroups == null || !sLPGTempGroups.Any() || sLPGTempGroups.Contains(Settings.ALL);
            bool allRed = sReductorTempGroups == null || !sReductorTempGroups.Any() || sReductorTempGroups.Contains(Settings.ALL);

            if (allGas && allRed)
                return data;

            IEnumerable<DataItem> result = data;

            // 🔹 Reductor filtering
            if (!allRed)
            {
                var reductorRanges = Settings.ReductorTemperatureRanges
                    .Where(r => sReductorTempGroups.Contains(r.Label))
                    .ToList();

                result = result.Where(d =>
                    reductorRanges.Any(r =>
                        d.Temp_RID >= r.Min && d.Temp_RID <= r.Max));
            }

            // 🔹 LPG filtering
            if (!allGas)
            {
                var gasRanges = Settings.GasTemperatureRanges
                    .Where(r => sLPGTempGroups.Contains(r.Label))
                    .ToList();

                result = result.Where(d =>
                    gasRanges.Any(r =>
                        d.Temp_GAS >= r.Min && d.Temp_GAS <= r.Max));
            }

            return result.ToArray();
        }
        public static double?[,] BuildTable(
            DataItem[] data,
            Func<DataItem, double> injectionBankSelector,
            Func<DataItem, double?> valueBankSelector,
            Settings.Aggregation aggregation)
        {
            var rpmRanges = Settings.RpmColumns;
            var injRanges = Settings.InjectionRanges;

            int rpmCount = rpmRanges.Length;
            int injCount = injRanges.Length;

            var table = new double?[rpmCount, injCount];

            // Buckets for values
            var buckets = new List<double>[rpmCount, injCount];
            for (int r = 0; r < rpmCount; r++)
                for (int i = 0; i < injCount; i++)
                    buckets[r, i] = new List<double>();

            

            // 1️⃣ Single pass: distribute data into buckets
            foreach (var d in data)
            {
                var value = valueBankSelector(d);
                if (!value.HasValue)
                    continue;

                int rpmIndex = Helper.FindIndex(d.RPM, rpmRanges, r => (r.Min, r.Max));
                if (rpmIndex < 0)
                    throw new IndexOutOfRangeException($"Invalid rpm valie {d.RPM}.");

                double inj = injectionBankSelector(d);
                int injIndex = Helper.FindIndex(injectionBankSelector(d), injRanges, r => (r.Min, r.Max));
                if (injIndex < 0)
                    throw new IndexOutOfRangeException($"Invalid rpm valie {injectionBankSelector(d)}.");

                buckets[rpmIndex, injIndex].Add(value.Value);
            }

            // 2️⃣ Aggregate buckets into final table
            for (int r = 0; r < rpmCount; r++)
            {
                for (int i = 0; i < injCount; i++)
                {
                    var values = buckets[r, i];

                    table[r, i] = values.Count == 0
                        ? (double?)null
                        : values.AggregateValues(aggregation).Round();
                }
            }

            return table;
        }
    }
}
