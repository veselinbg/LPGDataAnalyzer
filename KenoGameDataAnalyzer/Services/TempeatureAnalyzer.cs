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
                    var bank1 = g.Select(x=>x.FAST_b1);
                    var bank2 = g.Select(x => x.FAST_b2);
                    var tempRid = g.Select(y => y.Temp_RID);

                    return new
                    {
                        LPG_Range = g.Key.Label,

                        AvgTrim = Enumerable.Average(g, x => x.Trim).Round(),

                        AvgB1 = bank1.Average().Round(),
                        AvgB2 = bank2.Average().Round(),

                        MinB1 = bank1.Min().Round(),
                        MinB2 = bank2.Min().Round(),

                        MaxB1 = bank1.Max().Round(),
                        MaxB2 = bank2.Max().Round(),

                        MinTempRed = tempRid.Min().Round(),
                        MaxTempRed = tempRid.Max().Round(),

                        AvgPress = Enumerable.Average(g, y => y.PRESS).Round(),
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
                    var bank1 = x.Select(x => x.FAST_b1);
                    var bank2 = x.Select(x => x.FAST_b2);
                    var Temp_GAS = x.Select(y => y.Temp_GAS);

                    return new
                    {
                        REDUCER_Temp = x.Key.Label,
                        AvgTrim = Enumerable.Average(x, x => x.Trim).Round(),
                        AvgB1 = bank1.Average().Round(),
                        AvgB2 = bank2.Average().Round(),
                        MinB1 = bank1.Min().Round(),
                        MinB2 = bank2.Min().Round(),
                        MaxB1 = bank1.Max().Round(),
                        MaxB2 = bank2.Max().Round(),
                        MinTempGas = Temp_GAS.Min().Round(),
                        MaxTempGas = Temp_GAS.Max().Round(),
                        AvgPress = Enumerable.Average(x, y => y.PRESS).Round(),
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
            return data.Zip(data.Skip(1), (a, b) => new
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
            return data.GroupBy(s => Math.Round(s.Temp_GAS / 5) * 5)
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
                    AvgPress = g.Average(x=>x.PRESS).Round(),
                    Count = g.Count()
                })
                .OrderBy(r => r.Temp_GAS) // optional but useful for analysis
                .ToList();

            return result;
        }
    }
}
