using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer.Services
{
    internal class Analyzer
    {
        public object[] GroupByGasTemperature(
                                                DataItem[] data,
                                                double benzTimingFilterCuting,
                                                Func<DataItem, double> selector1,
                                                Func<DataItem, double> selector2)
        {
            return [.. data.Where(x => Filter.BenzBanks(x, benzTimingFilterCuting))
                .GroupBy(x => x.Temp_GAS)
                .Select(x =>
                {
                    var bank1 = x.Select(selector1);
                    var bank2 = x.Select(selector2);
                    var Temp_RID = x.Select(y => y.Temp_RID);

                    return new
                    {
                        GasTemp = x.Key,
                        AverageB1 = bank1.Average().Round(),
                        AverageB2 = bank2.Average().Round(),
                        MinB1 = bank1.Min().Round(),
                        MinB2 = bank2.Min().Round(),
                        MaxB1 = bank1.Max().Round(),
                        MaxB2 = bank2.Max().Round(),
                        MinTempRed = Temp_RID.Min().Round(),
                        MaxTempRed = Temp_RID.Max().Round(),
                        AveragePressure = x.Average(y=>y.PRESS).Round()
                    };
                })];
        }

        public object[] GroupByRIDTemperature(
            DataItem[] data,
            double benzTimingFilterCuting,
            Func<DataItem, double> selector1,
            Func<DataItem, double> selector2)
        {
            return [.. data.Where(x => Filter.BenzBanks(x, benzTimingFilterCuting))
                .GroupBy(x => x.Temp_RID)
                .Select(x =>
                {
                    var bank1 = x.Select(selector1);
                    var bank2 = x.Select(selector2);
                    var Temp_GAS = x.Select(y => y.Temp_GAS);

                    return new 
                    {
                        Temp = x.Key,
                        AverageB1 = bank1.Average().Round(),
                        AverageB2 = bank2.Average().Round(),
                        MinB1 = bank1.Min().Round(),
                        MinB2 = bank2.Min().Round(),
                        MaxB1 = bank1.Max().Round(),
                        MaxB2 = bank2.Max().Round(),
                        MinTempGas = Temp_GAS.Min().Round(),
                        MaxTempGas = Temp_GAS.Max().Round(),
                        AveragePressure = x.Average(y=>y.PRESS).Round()
                    };
                })];
        }
        public DataItem[] FilterByTemp(DataItem[] data, string sLPGTempGroup, string sReductorTempGroup)
        {
            if (sReductorTempGroup == Settings.ALL)
                return data;
            var lpgRange = Settings.GasTemperatureRanges
                .FirstOrDefault(r => r.Label == sLPGTempGroup);

            if (lpgRange.Label == null)
                throw new Exception($"This Lpg Temperature group {sLPGTempGroup} is not supported.");

            var reductorRange = Settings.ReductorTemperatureRanges
               .FirstOrDefault(r => r.Label == sReductorTempGroup);

            if (reductorRange.Label == null)
                throw new Exception($"This Reductor Temperature group {sReductorTempGroup} is not supported.");

            return [.. data.Where(d =>
                d.Temp_GAS >= lpgRange.Min && d.Temp_GAS <= lpgRange.Max &&
                d.Temp_RID >= reductorRange.Min && d.Temp_RID <= reductorRange.Max)];
        }



        public double?[,] BuildTable(
    DataItem[] data,
    Func<DataItem, double> injectionBankSelector,
    Func<DataItem, double?> valueBankSelector,
    Settings.Aggregation aggregation)
        {
            int rpmCount = Settings.RpmColumns.Length;
            int injCount = Settings.InjectionRanges.Length;

            var table = new double?[rpmCount, injCount];

            // Generate all RPM x Injection combinations with aggregated values
            var cellValues =
                from rpmIndex in Enumerable.Range(0, rpmCount)
                let rpmCol = Settings.RpmColumns[rpmIndex]
                from injIndex in Enumerable.Range(0, injCount)
                let injRange = Settings.InjectionRanges[injIndex]
                let filteredValues = data
                    .Where(d =>
                        injectionBankSelector(d) > injRange.Min &&
                        injectionBankSelector(d) <= injRange.Max &&
                        d.RPM > rpmCol.Min &&
                        d.RPM <= rpmCol.Max &&
                        valueBankSelector(d).HasValue)
                    .Select(d => valueBankSelector(d)!.Value)
                    .DefaultIfEmpty(double.NaN) // sentinel for “no value”
                let aggregated = double.IsNaN(filteredValues.First())
                    ? (double?)null
                    : AggregateValues(filteredValues, aggregation).Round()
                select new { rpmIndex, injIndex, aggregated };

            // Fill the table
            foreach (var cell in cellValues)
                table[cell.rpmIndex, cell.injIndex] = cell.aggregated;

            return table;
        }
        public double AggregateValues(IEnumerable<double> values, Settings.Aggregation aggregation)
        {
            return aggregation switch
            {
                Settings.Aggregation.Median => values.Median(),
                Settings.Aggregation.Min => values.Min(),
                Settings.Aggregation.Max => values.Max(),
                Settings.Aggregation.Average => values.Average(),
                _ => 0
            };
        }
        public object[] BuildTableMap(DataItem[] data)
        {
            return [..Settings.MapModes.Select(r =>
                    {
                        var items = data
                            .Where(d => d.MAP > r.Min && d.MAP <= r.Max)
                            .ToArray();

                        return new
                        {
                            r.Label,
                            MapValue = items.Average(x => x.MAP).Round(),
                            AvgFastTrimBank1 = items.Average(x => x.FAST_b1).Round(),
                            AvgFastTrimBank2 = items.Average(x => x.FAST_b2).Round(),
                            AvgSlowTrimBank1 = items.Average(x => x.SLOW_b1).Round(),
                            AvgSlowTrimBank2 = items.Average(x => x.SLOW_b2).Round(),
                            PRESS = items.Average(x => x.PRESS).Round(),
                            AvgTrim = items.Average(x => x.Trim).Round(),
                            MedianTrim = items.Select(x => x.Trim).ToArray().Median().Round(),
                        };
                    })];
        }
        public object[] BuildTableDrivingModes(DataItem[] data)
        {
            return [..Settings.DrivingModes.Select(r =>

            {
                var dataByb1 = data.Where(d => d.BENZ_b1 > r.Min && d.BENZ_b1 <= r.Max).ToArray();
                var dataByb2 = data.Where(d => d.BENZ_b2 > r.Min && d.BENZ_b2 <= r.Max).ToArray();
                var dataByMode = data.Where(d=>d.BENZ > r.Min && d.BENZ <= r.Max).ToArray();
                return new
                {
                    r.Label,
                    BENZ_b1 = dataByb1.Average(x => x.BENZ_b1).Round(),
                    BENZ_b2 = dataByb2.Average(x => x.BENZ_b2).Round(),
                    AvgFastTrimBank1 = dataByb1.Average(x => x.FAST_b1).Round(),
                    AvgFastTrimBank2 = dataByb2.Average(x => x.FAST_b2).Round(),
                    AvgSlowTrimBank1 = dataByb1.Average(x => x.SLOW_b1).Round(),
                    AvgSlowTrimBank2 = dataByb2.Average(x => x.SLOW_b2).Round(),
                    PRESS = dataByMode.Average(x => x.PRESS).Round(),
                    AvgTrim = dataByMode.Average(x => x.Trim).Round(),
                    MedianTrim = dataByMode.Select(x => x.Trim).ToArray().Median().Round(),
                };
             })];
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
        public object[] BuildBankToBankfuelBalance(DataItem[] data)
        {
            return [..data
                                .Where(s =>Filter.BenzBanks(s))
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
                                .OrderBy(x=>x.Rpm)
                                .ThenBy(y=>y.Map)];
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
        public object[] ReducerThermalLag(DataItem[] data)
        {
            var tempLag = data.Where(s => Filter.GasBanks(s))
                .Zip(data.Skip(1), (a, b) => new
                {
                    ReducerDelta = Math.Abs((b.Temp_RID - a.Temp_RID).Round()),
                    PressureDelta = Math.Abs((b.PRESS - a.PRESS).Round())
                })
                .Where(x=>x.ReducerDelta > 0 || x.PressureDelta > 0)
                .OrderByDescending(x=>x.ReducerDelta)
                .ThenByDescending(y=>y.PressureDelta)
                .Distinct().ToArray();

            return tempLag;
        }
        /// <summary>
        /// LPG temp vs injector time (normalized)
        /// At same RPM + MAP: Injector_ms should increase smoothly as LPG temp drops
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public object[] LpgTemperatureVsInjectionTime(DataItem[] data)
        {
            return [.. data
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
                                .OrderBy(x => x.Temp)];
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
        public object[] BuildABankAwareLPGBaseMap(DataItem[] data)
        {
            return [.. data.GroupBy(s => new
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
                       Diff = (Math.Abs(GAS_b1 - GAS_b2)/((GAS_b1 + GAS_b2)/2.0)).ToString("P")
                    }; 
                }).OrderBy(x=>x.Diff)];
        }
        /// <summary>
        /// LPG injector dead-time estimation
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public object[] LpgInjectorDeadTimeEstimation(DataItem[] data)
        {
            return [.. data.Where(s => Filter.GasBanks(s) )//&& s.MAP < 0.45 && s.RPM < 1500)
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
                                            Diff = (Math.Abs(BENZ_b1 - BENZ_b2)/((BENZ_b1 + BENZ_b2)/2.0)).ToString("P")
                                        };
                                   }).OrderBy(x=>x.Map)];
        }
    }
}
