using LPGDataAnalyzer.Models;

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
                                    StdDev1 = (g.Select(x => x.GAS_b1)).StdDev().Round(),
                                    AvgLpg2Ms = g.Average(x => x.GAS_b2).Round(),
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
                    Diff_Trim = Math.Abs(avgTrimB1 - avgTrimB2).ToString("0.##'%'"),

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
            return data.GroupBy(s => new
            {
                Rpm = (int)Math.Round(s.RPM / 500.0) * 500,
                Map = s.MAP.Round()
            }).Select(g =>
            {
                var GAS_b1 = g.Average(x => x.GAS_b1).Round();
                var GAS_b2 = g.Average(x => x.GAS_b2).Round();

                return new
                {
                    g.Key.Rpm,
                    g.Key.Map,
                    Lpg1 = GAS_b1,
                    Lpg2 = GAS_b2,
                    Diff = (Math.Abs(GAS_b1 - GAS_b2) / ((GAS_b1 + GAS_b2) / 2.0)).ToString("P")
                };
            }).OrderBy(x => x.Diff).ToArray();
        }
        /// <summary>
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
                                           Diff = (Math.Abs(BENZ_b1 - BENZ_b2) / ((BENZ_b1 + BENZ_b2) / 2.0)).ToString("P")
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
            return data
                                .Where(s => Filter.BenzBanks(s))
                                .GroupBy(s => new
                                {
                                    Rpm = (int)Math.Round(s.RPM / 250.0) * 250,
                                    Map = Math.Round(s.MAP, 2)
                                })
                                .Select(g => new
                                {
                                    g.Key.Rpm,
                                    g.Key.Map,
                                    Bank1Ms = g.Average(x => x.BENZ_b1).Round(),
                                    Bank2Ms = g.Average(x => x.BENZ_b2).Round(),
                                    DeltaPct =
                                        (100.0 * (g.Average(x => x.BENZ_b1) -
                                                 g.Average(x => x.BENZ_b2)) /
                                        g.Average(x => x.BENZ_b1)).Round()
                                })
                                .OrderBy(x => x.Rpm)
                                .ThenBy(y => y.Map).ToArray();
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
