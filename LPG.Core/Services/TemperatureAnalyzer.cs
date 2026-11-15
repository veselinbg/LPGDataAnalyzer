using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer.Services
{
    /// <summary>
    /// Provides temperature analysis functionality for engine data, including temperature range
    /// categorization, thermal lag detection, and correlation analysis between temperature
    /// and fuel injection parameters.
    /// </summary>
    public class TemperatureAnalyzer
    {
        public static object GasTemperatureRanges(DataItem[] data)
        {
            return data
                .GroupBy(x =>
                    Settings.GasTemperatureRanges[x.Temp_GAS.GetTempBandIndex(Settings.GasTemperatureRanges)])
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
                    Settings.ReductorTemperatureRanges[x.Temp_RID.GetTempBandIndex(Settings.ReductorTemperatureRanges)])
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
            return data.Zip(data.Skip(1))
                .Where(p => p.First.SourceFile == p.Second.SourceFile) // no deltas across log files
                .Select(p => new
                {
                    ReducerDelta = Math.Abs((p.Second.Temp_RID - p.First.Temp_RID).Round()),
                    PressureDelta = Math.Abs((p.Second.PRESS - p.First.PRESS).Round())
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
        public static object SlowTrimChanges(DataItem[] data)
        {
            var result = new List<object>();

            // Start index of the current "same slow trim" segment per bank.
            int lastB1 = 0;
            int lastB2 = 0;

            for (int index = 1; index < data.Length; index++)
            {
                var previous = data[index - 1];
                var current = data[index];

                // A new log file is not a slow-trim change: restart both segments.
                if (previous.SourceFile != current.SourceFile)
                {
                    lastB1 = lastB2 = index;
                    continue;
                }

                if (previous.SLOW_b1 != current.SLOW_b1)
                {
                    result.Add(BuildSlowChange("B1", data, lastB1, index, previous.SLOW_b1, current.SLOW_b1,
                        r => r.GAS_b1, r => r.BENZ_b1, r => r.FAST_b1, r => r.AFR_b1));
                    lastB1 = index;
                }

                if (previous.SLOW_b2 != current.SLOW_b2)
                {
                    result.Add(BuildSlowChange("B2", data, lastB2, index, previous.SLOW_b2, current.SLOW_b2,
                        r => r.GAS_b2, r => r.BENZ_b2, r => r.FAST_b2, r => r.AFR_b2));
                    lastB2 = index;
                }
            }

            return result;
        }

        private static object BuildSlowChange(
            string bank,
            DataItem[] data,
            int start,
            int index,
            double previousSlow,
            double currentSlow,
            Func<DataItem, double> gas,
            Func<DataItem, double> benz,
            Func<DataItem, double> fast,
            Func<DataItem, double> afr)
        {
            var range = new ArraySegment<DataItem>(data, start, index - start + 1);
            var current = data[index];

            return new
            {
                Bank = bank,

                PreviousSlow = previousSlow,
                CurrentSlow = currentSlow,
                Delta = (currentSlow - previousSlow).Round(),

                Count = range.Count,

                AvgLpgInj = range.Average(gas).Round(),
                AvgPetrolInj = range.Average(benz).Round(),
                AvgFast = range.Average(fast).Round(),
                AvgAFR = range.Average(afr).Round(),
                AvgRPM = range.Average(r => r.RPM).Round(),
                AvgMAP = range.Average(r => r.MAP).Round(),
                Pressure = range.Average(r => r.PRESS).Round(),
                Temp_RID = range.Average(r => r.Temp_RID).Round(),
                Temp_GAS = range.Average(r => r.Temp_GAS).Round(),
                IngAtChange = benz(current),
                RPMAtChange = current.RPM,
                LPGInjectionAtChange = gas(current).Round(),
                TEMPO = current.TEMPO
            };
        }
    }
}
