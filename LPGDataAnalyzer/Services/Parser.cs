using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer.Services
{
    public class Parser
    {
        private const int ExpectedColumns = 22;
        private const double TrimTolerance = 0.1; // 🔥 prevents float fragmentation

        public DataItem[] Data { get; private set; } = [];

        public void Load(string path)
        {
            var result = new List<DataItem>();
            var buffer = new List<DataItem>();

            double lastTrimB1 = double.NaN;
            double lastTrimB2 = double.NaN;

            int tempoCounter = 0;

            foreach (var item in ReadValidLines(path))
            {
                bool isNewGroup =
                    buffer.Count == 0 ||
                    Math.Abs(item.Trim_b1 - lastTrimB1) > TrimTolerance ||
                    Math.Abs(item.Trim_b2 - lastTrimB2) > TrimTolerance;

                if (isNewGroup && buffer.Count > 0)
                {
                    var avg = AverageGroup(buffer);
                    avg.TEMPO = tempoCounter++;
                    result.Add(avg);
                    buffer.Clear();
                }

                lastTrimB1 = item.Trim_b1;
                lastTrimB2 = item.Trim_b2;

                buffer.Add(item);
            }

            if (buffer.Count > 0)
            {
                var avg = AverageGroup(buffer);
                avg.TEMPO = tempoCounter++;
                result.Add(avg);
            }

            Data = result.Where(x=>x.TEMPO > 0).ToArray();
        }

        private IEnumerable<DataItem> ReadValidLines(string path)
        {
            return File.ReadLines(path)
                .Skip(2)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(ParseLine)
                .Where(x =>x.RPM > 0 && x.GAS_b1 > 0 && x.GAS_b2 > 0).ToList();
                //.Where(x => x.RPM > 0 && x.GAS_b1 > 0 && x.GAS_b2 > 0 && x.FAST_b1 != 0 && x.FAST_b2 != 0);
        }

        private static DataItem AverageGroup(List<DataItem> items)
        {
            double avgRPM = items.Average(x => x.RPM);

            double avgGas1 = items.Average(x => x.GAS_b1);
            double avgGas2 = items.Average(x => x.GAS_b2);
            double avgBenz1 = items.Average(x => x.BENZ_b1);
            double avgBenz2 = items.Average(x => x.BENZ_b2);

            double avgSlow1 = items.Average(x => x.SLOW_b1);
            double avgFast1 = items.Average(x => x.FAST_b1);
            double avgSlow2 = items.Average(x => x.SLOW_b2);
            double avgFast2 = items.Average(x => x.FAST_b2);

            // 🔥 unified trims per bank
            double trim_b1 = avgSlow1 + avgFast1;
            double trim_b2 = avgSlow2 + avgFast2;

            // 🔥 global trim
            double trim = (trim_b1 + trim_b2) / 2;

            return new DataItem
            {
                RPM = (int)avgRPM.Round(),

                GAS_b1 = avgGas1.Round(),
                GAS_b2 = avgGas2.Round(),
                BENZ_b1 = avgBenz1.Round(),
                BENZ_b2 = avgBenz2.Round(),

                PRESS = items.Average(x => x.PRESS).Round(),
                MAP = items.Average(x => x.MAP).Round(),
                Temp_RID = items.Average(x => x.Temp_RID).Round(),
                Temp_GAS = items.Average(x => x.Temp_GAS).Round(),

                SLOW_b1 = avgSlow1.Round(),
                FAST_b1 = avgFast1.Round(),
                SLOW_b2 = avgSlow2.Round(),
                FAST_b2 = avgFast2.Round(),

                // 🔥 trims (correct!)
                Trim_b1 = trim_b1.Round(),
                Trim_b2 = trim_b2.Round(),
                Trim = trim.Round(),
                Fast = items.Average(x=>x.Fast).Round(),
                Slow = items.Average(x=>x.Slow).Round(),
                TrimDiff = (trim_b1 - trim_b2).Round(),

                // 🔥 AFR (correct model)
                AFR_b1 = (15.6 / (1 + trim_b1 / 100)).Round(),
                AFR_b2 = (15.6 / (1 + trim_b2 / 100)).Round(),
                AFR = (15.6 / (1 + trim / 100)).Round(),

                GAS = ((avgGas1 + avgGas2) / 2).Round(),
                BENZ = ((avgBenz1 + avgBenz2) / 2).Round(),

                OX_b1 = items.Average(x => x.OX_b1).Round(),
                OX_b2 = items.Average(x => x.OX_b2).Round(),

                Ratio_b1 = items.Average(x => x.Ratio_b1).Round(),
                Ratio_b2 = items.Average(x => x.Ratio_b2).Round(),
                RatioDifference = items.Average(x => x.RatioDifference).Round(),

                BENZ_Diff = avgBenz1.RelDiff(avgBenz2).Round()
            };
        }

        private DataItem ParseLine(string line)
        {
            Span<Range> ranges = stackalloc Range[ExpectedColumns];
            var span = line.AsSpan();

            int count = span.Split(ranges, '\t');
            if (count < ExpectedColumns)
                return new DataItem();

            double gasB1 = span[ranges[3]].ToDouble();
            double benzB1 = span[ranges[4]].ToDouble();
            double gasB2 = span[ranges[14]].ToDouble();
            double benzB2 = span[ranges[15]].ToDouble();

            double slowB1 = span[ranges[10]].ToDouble();
            double fastB1 = span[ranges[11]].ToDouble();
            double slowB2 = span[ranges[16]].ToDouble();
            double fastB2 = span[ranges[17]].ToDouble();

            double trim_b1 = slowB1 + fastB1; 
            double trim_b2 = slowB2 + fastB2;
            double trim = (trim_b1 + trim_b2) / 2;

            // Cache ratio calculations to avoid recomputation
            double ratio_b1 = gasB1 != 0 ? benzB1 / gasB1 : 0;
            double ratio_b2 = gasB2 != 0 ? benzB2 / gasB2 : 0;

            return new DataItem
            {
                TEMPO = span[ranges[0]].ToInt(),
                RPM = span[ranges[1]].ToInt(),

                GAS_b1 = gasB1,
                BENZ_b1 = benzB1,
                GAS_b2 = gasB2,
                BENZ_b2 = benzB2,

                PRESS = span[ranges[5]].ToDouble(),
                MAP = span[ranges[6]].ToDouble(),
                Temp_RID = span[ranges[7]].ToDouble(),
                Temp_GAS = span[ranges[8]].ToDouble(),

                SLOW_b1 = slowB1,
                FAST_b1 = fastB1,
                SLOW_b2 = slowB2,
                FAST_b2 = fastB2,
                 
                Trim_b1 = trim_b1,
                Trim_b2 = trim_b2,
                Trim = trim,
                Fast = (fastB1 + fastB2) / 2,
                Slow = (slowB1 + slowB2) / 2,
                TrimDiff = (trim_b1 - trim_b2),

                AFR_b1 = 15.6 / (1 + trim_b1 / 100),
                AFR_b2 = 15.6 / (1 + trim_b2 / 100),
                AFR = 15.6 / (1 + trim / 100),

                GAS = (gasB1 + gasB2) / 2,
                BENZ = (benzB1 + benzB2) / 2,

                BENZ_Diff = benzB1.RelDiff(benzB2),

                Ratio_b1 = ratio_b1,  // Reuse cached value
                Ratio_b2 = ratio_b2,  // Reuse cached value
                RatioDifference = ratio_b1 - ratio_b2  // Reuse cached values
            };
        }
    }
}