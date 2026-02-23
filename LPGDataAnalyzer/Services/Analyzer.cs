using LPGDataAnalyzer.Models;
using System.Runtime.Intrinsics.Arm;

namespace LPGDataAnalyzer.Services
{
    internal class Analyzer
    {
        private static bool GasFilter(DataItem item)
        {
            return item.GAS_b1 > 0 && item.GAS_b2 > 0;
        }
        private static bool BenzFilter(DataItem item, double val = 0)
        {
            return item.BENZ_b1 > val && item.BENZ_b2 > val;
        }
        public static double PercentageChange(double baseValue, double newValue)
        {
            return ((newValue - baseValue) / baseValue) * 100;
        }
        public ICollection<GroupByTemp> GroupByGasTemperature(
    IEnumerable<DataItem> data,
    double benzTimingFilterCuting,
    Func<DataItem, double> selector1,
    Func<DataItem, double> selector2)
        {
            if (data == null) throw new ArgumentNullException("The data object is null!");

           return [.. data.Where(x => BenzFilter(x, benzTimingFilterCuting))
                .GroupBy(x => x.Temp_GAS)
                .Select(x =>
                {
                    var bank1 = x.Select(selector1);
                    var bank2 = x.Select(selector2);
                    var Temp_RID = x.Select(y => y.Temp_RID);

                    return new GroupByTemp
                    {
                        Temp = x.Key,
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

        public ICollection<object> GroupByRIDTemperature(
            IEnumerable<DataItem> data,
            double benzTimingFilterCuting,
            Func<DataItem, double> selector1,
            Func<DataItem, double> selector2)
        {
            if (data == null) throw new ArgumentNullException("The data object is null!");

            return [.. data.Where(x => BenzFilter(x, benzTimingFilterCuting))
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
        public IEnumerable<DataItem> FilterByTemp(IEnumerable<DataItem> data, string sLPGTempGroup, string sReductorTempGroup)
        {
            var lpgRange = Settings.GasTemperatureRanges
                .FirstOrDefault(r => r.Label == sLPGTempGroup);

            if (lpgRange.Label == null)
                throw new Exception($"This Lpg Temperature group {sLPGTempGroup} is not supported.");

            var reductorRange = Settings.ReductorTemperatureRanges
               .FirstOrDefault(r => r.Label == sReductorTempGroup);

            if (reductorRange.Label == null)
                throw new Exception($"This Reductor Temperature group {sReductorTempGroup} is not supported.");

            return data.Where(d =>
                d.Temp_GAS >= lpgRange.Min && d.Temp_GAS <= lpgRange.Max &&
                d.Temp_RID >= reductorRange.Min && d.Temp_RID <= reductorRange.Max);
        }
        public IEnumerable<TableRow> BuildTable(
                                             IEnumerable<DataItem> data,
                                             Func<DataItem, double> injectionBankSelector,
                                             Func<DataItem, double?> valueBankSelector)
        {
            return Settings.InjectionRanges.Select(r => new TableRow
            {
                Key = r.Label,
                Columns = Settings.RpmColumns.ToDictionary(
                    c => c.Label,
                    c =>
                    {
                        var values = data
                            .Where(d =>
                                injectionBankSelector(d) > r.Min &&
                                injectionBankSelector(d) <= r.Max &&
                                d.RPM > c.Min &&
                                d.RPM <= c.Max &&
                                valueBankSelector(d).HasValue)
                            .Select(d => valueBankSelector(d)!.Value);

                        return values.Any()
                            ? values.Average().Round()
                            : (double?)null;
                    })
            });
        }
        public IEnumerable<object> BuildTableMap(IEnumerable<DataItem> data)
        {
            return [..Settings.MapModes.Select(r =>
            {
                var dataByMap = data.Where(d => d.MAP > r.Min && d.MAP <= r.Max);

                return new
                { 
                    r.Label,
                    MapValue = dataByMap.Average(x => x.MAP).Round(),
                    AvgFastTrimBank1 = dataByMap.Average(x => x.FAST_b1).Round(),
                    AvgFastTrimBank2 = dataByMap.Average(x => x.FAST_b2).Round(),
                    AvgSlowTrimBank1 = dataByMap.Average(x => x.SLOW_b1).Round(),
                    AvgSlowTrimBank2 = dataByMap.Average(x => x.SLOW_b2).Round(),
                };
             })];
        }
        public IEnumerable<object> BuildTableDrivingModes(IEnumerable<DataItem> data)
        {
            return [..Settings.DrivingModes.Select(r =>

            {
                var dataByb1 = data.Where(d => d.BENZ_b1 > r.Min && d.BENZ_b1 <= r.Max);
                var dataByb2 = data.Where(d => d.BENZ_b2 > r.Min && d.BENZ_b2 <= r.Max);

                return new
                {
                    r.Label,
                    BENZ_b1 = dataByb1.Average(x => x.BENZ_b1).Round(),
                    BENZ_b2 = dataByb2.Average(x => x.BENZ_b2).Round(),
                    AvgFastTrimBank1 = dataByb1.Average(x => x.FAST_b1).Round(),
                    AvgFastTrimBank2 = dataByb2.Average(x => x.FAST_b2).Round(),
                    AvgSlowTrimBank1 = dataByb1.Average(x => x.SLOW_b1).Round(),
                    AvgSlowTrimBank2 = dataByb2.Average(x => x.SLOW_b2).Round(),

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
        public IEnumerable<object> BuildBankToBankfuelBalance(IEnumerable<DataItem> data)
        {
            return [..data
                                .Where(s =>BenzFilter(s))
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
        public IList<object> ReducerThermalLag(IEnumerable<DataItem> data)
        {
            var tempLag = data.Where(s => GasFilter(s))
                .Zip(data.Skip(1), (a, b) => new
                {
                    ReducerDelta = Math.Abs((b.Temp_RID - a.Temp_RID).Round()),
                    PressureDelta = Math.Abs((b.PRESS - a.PRESS).Round())
                })
                .Where(x=>x.ReducerDelta > 0 || x.PressureDelta > 0)
                .OrderByDescending(x=>x.ReducerDelta)
                .ThenByDescending(y=>y.PressureDelta);
            return [.. tempLag];
        }
        /// <summary>
        /// LPG temp vs injector time (normalized)
        /// At same RPM + MAP: Injector_ms should increase smoothly as LPG temp drops
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public IEnumerable<object> LpgTemperatureVsInjectionTime(IEnumerable<DataItem> data)
        {
            var lpgTempEffect = data
                                .Where(s => GasFilter(s))
                                .GroupBy(s => Math.Round(s.Temp_GAS / 5) * 5)
                                .Select(g => new
                                {
                                    Temp = g.Key,
                                    AvgLpg1Ms = g.Average(x => x.GAS_b1).Round(),
                                    StdDev1 = (g.Select(x => x.GAS_b1)).StdDev().Round(),
                                    AvgLpg2Ms = g.Average(x => x.GAS_b2).Round(),
                                    StdDev2 = (g.Select(x => x.GAS_b2)).StdDev().Round()
                                })
                                .OrderBy(x => x.Temp);
            return lpgTempEffect;
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
        public IEnumerable<object> BuildABankAwareLPGBaseMap(IEnumerable<DataItem> data)
        {
            var lpgBaseMap = data.GroupBy(s => new
            {
                Rpm = (int)Math.Round(s.RPM / 500.0) * 500,
                Map = s.MAP.Round()
            }).Select(g => new
               {
                   g.Key.Rpm,
                   g.Key.Map,
                   Lpg1 = g.Average(x => x.GAS_b1).Round(),
                   Lpg2 = g.Average(x => x.GAS_b2).Round(),
                   Diff = (g.Average(x => x.GAS_b1) - g.Average(x => x.GAS_b2)).Round().ToString("P")
            }).OrderBy(x=>x.Diff);
            return lpgBaseMap;
        }
        /// <summary>
        /// LPG injector dead-time estimation
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public IEnumerable<object> LpgInjectorDeadTimeEstimation(IEnumerable<DataItem> data)
        {
            var deadTimeData = data
                                    .Where(s => GasFilter(s) )//&& s.MAP < 0.45 && s.RPM < 1500)
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
                                    }).OrderBy(x=>x.Map);
            return deadTimeData;
        }

        public IEnumerable<object> CalculateAFR(IEnumerable<DataItem> data, double stoichAfr = 15.6)
        {
            var afr = data.Select(s =>
            {
                var totalTrimB1 = (s.FAST_b1 + s.SLOW_b1).Round();
                var totalTrimB2 = (s.FAST_b2 + s.SLOW_b2).Round();

                var factorB1 = (1.0 + (totalTrimB1 / 100.0)).Round();
                var factorB2 = (1.0 + (totalTrimB2 / 100.0)).Round();

                return new
                {
                    TotalTrimB1 = totalTrimB1,
                    TotalTrimB2 = totalTrimB2,
                    fuelFactorB1 = factorB1,
                    fuelFactorB2 = factorB2,
                    AfrB1 = (stoichAfr / factorB1).Round(),
                    AfrB2 = (stoichAfr / factorB2).Round()
                };
            })
        .Distinct();

            return afr;
        }
    }
}
