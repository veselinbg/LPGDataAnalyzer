using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace LPGDataAnalyzer
{
    internal class LPGData
    {
        public int TEMPO { get; set; }
        public int RPM { get; set; }
        public double LAMBDA_b1 { get; set; }
        public double GAS_b1 { get; set; }
        public double BENZ_b1 { get; set; }
        public double PRESS { get; set; }
        public double MAP { get; set; }
        public double Temp_RID { get; set; }
        public double Temp_GAS { get; set; }
        public double LIV { get; set; }
        public double SLOW_b1 { get; set; }
        public double FAST_b1 { get; set; }
        public double OX_b1 { get; set; }
        public double LAMBDA_b2 { get; set; }
        public double GAS_b2 { get; set; }
        public double BENZ_b2 { get; set; }
        public double SLOW_b2 { get; set; }
        public double FAST_b2 { get; set; }
        public double OX_b2 { get; set; }
        public int MARKER { get; set; }
        public int AUTOMARKER { get; set; }
        public int ECUMARKER { get; set; }

        // Calculated fields
        public double Ratio_b1 { get; set; }
        public double Ratio_b2 { get; set; }
        public double RatioDifference { get; set; }
    }
    internal class LPGDataParser()
    {
        public ICollection<LPGData> Data { get; protected set; } = [];

        public virtual void Load(string _datapath)
        {
            Data = [..File.ReadLines(_datapath)
            .Skip(2) // skip header row and file data info
            .Where(line => !string.IsNullOrWhiteSpace(line) )
            .Select(ParseLine)
            //remove data when the engine is workin on petrol 
            .Where(x => x.GAS_b1 > 0 && x.GAS_b2 > 0 && x.Ratio_b1 > 0 && x.Ratio_b2 > 0)];
        }
        LPGData ParseLine(string line)
        {
            string[] f = line.Split('\t', StringSplitOptions.None);
            var culture = CultureInfo.InvariantCulture;

            if (f.Count() < 22) return new LPGData();

            var lpgDataLine = new LPGData
            {
                TEMPO = ToInt(f[0]),
                RPM = ToInt(f[1]),

                LAMBDA_b1 = ToDouble(f[2], culture),
                GAS_b1 = ToDouble(f[3], culture),
                BENZ_b1 = ToDouble(f[4], culture),
                PRESS = ToDouble(f[5], culture),
                MAP = ToDouble(f[6], culture),
                Temp_RID = ToDouble(f[7], culture),
                Temp_GAS = ToDouble(f[8], culture),
                LIV = ToDouble(f[9], culture),
                SLOW_b1 = ToDouble(f[10], culture),
                FAST_b1 = ToDouble(f[11], culture),
                OX_b1 = ToDouble(f[12], culture),
                LAMBDA_b2 = ToDouble(f[13], culture),
                GAS_b2 = ToDouble(f[14], culture),
                BENZ_b2 = ToDouble(f[15], culture),
                SLOW_b2 = ToDouble(f[16], culture),
                FAST_b2 = ToDouble(f[17], culture),
                OX_b2 = ToDouble(f[18], culture),

                MARKER = ToInt(f[19]),
                AUTOMARKER = ToInt(f[20]),
                ECUMARKER = ToInt(f[21])
            };

            CalculateRatios(lpgDataLine);

            return lpgDataLine;
        }
        static int ToInt(string value)
        {
            return int.TryParse(value, out int result) ? result : 0;
        }

        static double ToDouble(string value, CultureInfo culture)
        {
            return double.TryParse(value, NumberStyles.Any, culture, out double result)
                ? result
                : 0.0;
        }
        static void CalculateRatios(LPGData d)
        {
            d.Ratio_b1 = Math.Round(d.BENZ_b1 != 0 ? d.GAS_b1 / d.BENZ_b1 : 0, 2, MidpointRounding.AwayFromZero);

            d.Ratio_b2 = Math.Round(d.BENZ_b2 != 0? d.GAS_b2 / d.BENZ_b2 : 0, 2, MidpointRounding.AwayFromZero);

            d.RatioDifference = Math.Round(d.Ratio_b1 - d.Ratio_b2, 1, MidpointRounding.AwayFromZero);
        }
    }
    internal class RatioTableRow
    {
        public double InjectionTime { get; set; }
        public Dictionary<int, double?> Columns { get; set; } = [];
    }
    internal class GroupByTemp
    {
        public double Temp { get; set; }
        public double MinB1 { get; set; }
        public double MaxB1 { get; set; }
        public double AverageB1 { get; set; }
        public double MinB2 { get; set; }
        public double MaxB2 { get; set; }
        public double AverageB2 { get; set; }
        public double MinTempRed { get; set; }
        public double MaxTempRed { get; set; }
        public double AveragePressure { get; set; }
    }

    internal class LPGDataAnalyzer 
    {
        public static readonly (double Min, double Max, double Label)[] InjectionRanges =
                                                                        {
                                                                            (0.0, 1.5, 1.5),
                                                                            (1.5, 2.5, 2.5),
                                                                            (2.5, 3.0, 3.0),
                                                                            (3.0, 3.5, 3.5),
                                                                            (3.5, 4.5, 4.5),
                                                                            (4.5, 5.5, 5.5),
                                                                            (5.5, 6.5, 6.5),
                                                                            (6.5, 7.5, 7.5),
                                                                            (7.5, 8.5, 8.5),
                                                                            (8.5, 9.5, 9.5),
                                                                            (9.5, 10.5, 10.5),
                                                                            (10.5, 15.0, 15.0)
                                                                        };
        private static readonly (int Min, int Max, string Label)[] LPGTemperatureRanges =
        {
            (int.MinValue, int.MaxValue,  "All"),
            (0, 20,  "Temp_0_20"),
            (21, 30, "Temp_21_30"),
            (31, 35, "Temp_31_35"),
            (36, 40, "Temp_36_40"),
            (41, 50, "Temp_41_50"),
            (51, 60, "Temp_51_60"),
            (61, 70, "Temp_61_70"),
            (71, int.MaxValue, "Temp_71_Over")
        };
        public static readonly string[] LPGTempGroups = LPGTemperatureRanges.Select(t => t.Label).ToArray();


        private static readonly (int Min, int Max, string Label)[] ReductorTemperatureRanges =
        {
            (int.MinValue, int.MaxValue,  "All"),
            (0, 40,  "Temp_0_40"),
            (41, 60, "Temp_41_60"),
            (61, int.MaxValue, "Temp_61_over"),
        };
        public static readonly string[] ReductorTempGroups = ReductorTemperatureRanges.Select(t => t.Label).ToArray();


        public static readonly (int Min, int Max, int Label)[] RpmColumns =
                                                                        {
                                                                            (0, 500, 500),
                                                                            (500, 1000, 1000),
                                                                            (1000, 1500, 1500),
                                                                            (1500, 2000, 2000),
                                                                            (2000, 2500, 2500),
                                                                            (2500, 3000, 3000),
                                                                            (3000, 3500, 3500),
                                                                            (3500, 4000, 4000),
                                                                            (4000, 4500, 4500),
                                                                            (4500, 5000, 5000),
                                                                            (5000, 5500, 5500),
                                                                            (5500, 6200, 6200)
                                                                        };
        public ICollection<GroupByTemp> GroupByTemperature(IEnumerable<LPGData> data, double benzTimingFilterCuting, Func<LPGData, double> selector1, Func<LPGData, double> selector2)
        {
            if (data == null) throw new ArgumentNullException("The data object is null!");

           return [.. data.Where(x => x.BENZ_b1 > benzTimingFilterCuting && x.BENZ_b2 > benzTimingFilterCuting)
                .GroupBy(x => x.Temp_GAS)
                .Select(x =>
                new GroupByTemp
                {
                    Temp = x.Key,
                    AverageB1 = x.Select(selector1).Average().Round(),
                    AverageB2 = x.Select(selector2).Average().Round(),
                    MinB1 = x.Select(selector1).Min().Round(),
                    MinB2 = x.Select(selector2).Min().Round(),
                    MaxB1 = x.Select(selector1).Max().Round(),
                    MaxB2 = x.Select(selector2).Max().Round(),
                    MinTempRed = x.Select(y => y.Temp_RID).Min().Round(),
                    MaxTempRed = x.Select(y => y.Temp_RID).Max().Round(),
                    AveragePressure = x.Select(y=>y.PRESS).Average().Round()
                })];
        }
        public IEnumerable<LPGData> FilterByTemp(IEnumerable<LPGData> data, string sLPGTempGroup, string sReductorTempGroup)
        {
            var lpgRange = LPGTemperatureRanges
                .FirstOrDefault(r => r.Label == sLPGTempGroup);

            if (lpgRange.Label == null)
                throw new Exception($"This Lpg Temperature group {sLPGTempGroup} is not supported.");

            var reductorRange = ReductorTemperatureRanges
               .FirstOrDefault(r => r.Label == sReductorTempGroup);

            if (reductorRange.Label == null)
                throw new Exception($"This Reductor Temperature group {sReductorTempGroup} is not supported.");

            return data.Where(d =>
                d.Temp_GAS >= lpgRange.Min &&d.Temp_GAS <= lpgRange.Max && 
                d.Temp_RID >= reductorRange.Min && d.Temp_RID <= reductorRange.Max);
        }
        public IEnumerable<RatioTableRow> BuildTable(
                                             IEnumerable<LPGData> data,
                                             Func<LPGData, double> injectionSelector,
                                             Func<LPGData, double?> ratioSelector)
        {
            return InjectionRanges.Select(r => new RatioTableRow
            {
                InjectionTime = r.Label,
                Columns = RpmColumns.ToDictionary(
                    c => c.Label,
                    c =>
                    {
                        var values = data
                            .Where(d =>
                                injectionSelector(d) > r.Min &&
                                injectionSelector(d) <= r.Max &&
                                d.RPM > c.Min &&
                                d.RPM <= c.Max &&
                                ratioSelector(d).HasValue)
                            .Select(d => ratioSelector(d)!.Value);

                        return values.Any()
                            ? values.Average().Round()
                            : (double?)null;
                    })
            });
        }
        public static double PercentageChange(double baseValue, double newValue)
        {
            return ((newValue - baseValue) / baseValue) * 100;
        }
    }
}
