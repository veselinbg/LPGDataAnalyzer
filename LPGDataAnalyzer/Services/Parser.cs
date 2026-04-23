using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer.Services
{
    public class Parser
    {
        private const int ExpectedColumns = 22;

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
                    item.Trim_b1 != lastTrimB1 ||
                    item.Trim_b2 != lastTrimB2;

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

            Data = result.ToArray();
        }

        private IEnumerable<DataItem> ReadValidLines(string path)
        {
            return File.ReadLines(path)
                .Skip(2)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(ParseLine)
                .Where(x => x.RPM > 0 && x.GAS_b1 > 0 && x.GAS_b2 > 0 && x.Temp_RID > 25);
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

            double fast = (avgFast1 + avgFast2) / 2;
            double slow = (avgSlow1 + avgSlow2) / 2;

            return new DataItem
            {
                Trim_b1 = items[0].Trim_b1,
                Trim_b2 = items[0].Trim_b2,

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

                OX_b1 = items.Average(x => x.OX_b1).Round(),
                OX_b2 = items.Average(x => x.OX_b2).Round(),

                Fast = fast.Round(),
                Slow = slow.Round(),
                Trim = ((fast + slow) / 2).Round(),

                AFR_b1 = (15.6 / ((1 + avgFast1 / 100) * (1 + avgSlow1 / 100))).Round(),
                AFR_b2 = (15.6 / ((1 + avgFast2 / 100) * (1 + avgSlow2 / 100))).Round(),
                AFR = (15.6 / ((1 + fast / 100) * (1 + slow / 100))).Round(),

                GAS = ((avgGas1 + avgGas2) / 2).Round(),
                BENZ = ((avgBenz1 + avgBenz2) / 2).Round(),

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

            double gas1 = span[ranges[3]].ToDouble();
            double benz1 = span[ranges[4]].ToDouble();
            double gas2 = span[ranges[14]].ToDouble();
            double benz2 = span[ranges[15]].ToDouble();

            double slow1 = span[ranges[10]].ToDouble();
            double fast1 = span[ranges[11]].ToDouble();
            double slow2 = span[ranges[16]].ToDouble();
            double fast2 = span[ranges[17]].ToDouble();

            double fast = (fast1 + fast2) / 2;
            double slow = (slow1 + slow2) / 2;

            return new DataItem
            {
                TEMPO = span[ranges[0]].ToInt(),
                RPM = span[ranges[1]].ToInt(),

                GAS_b1 = gas1,
                BENZ_b1 = benz1,
                GAS_b2 = gas2,
                BENZ_b2 = benz2,

                PRESS = span[ranges[5]].ToDouble(),
                MAP = span[ranges[6]].ToDouble(),
                Temp_RID = span[ranges[7]].ToDouble(),
                Temp_GAS = span[ranges[8]].ToDouble(),

                SLOW_b1 = slow1,
                FAST_b1 = fast1,
                SLOW_b2 = slow2,
                FAST_b2 = fast2,

                OX_b1 = span[ranges[12]].ToDouble(),
                OX_b2 = span[ranges[18]].ToDouble(),

                Fast = fast,
                Slow = slow,
                Trim = (fast + slow) / 2,

                Trim_b1 = (slow1 + fast1) / 2,
                Trim_b2 = (slow2 + fast2) / 2,

                AFR_b1 = 15.6 / ((1 + fast1 / 100) * (1 + slow1 / 100)),
                AFR_b2 = 15.6 / ((1 + fast2 / 100) * (1 + slow2 / 100)),
                AFR = 15.6 / ((1 + fast / 100) * (1 + slow / 100)),

                GAS = (gas1 + gas2) / 2,
                BENZ = (benz1 + benz2) / 2,

                BENZ_Diff = benz1.RelDiff(benz2),

                Ratio_b1 = benz1 != 0 ? (gas1 / benz1).Round() : 0,
                Ratio_b2 = benz2 != 0 ? (gas2 / benz2).Round() : 0,
                RatioDifference = (benz1 != 0 && benz2 != 0)
                    ? ((gas1 / benz1) - (gas2 / benz2)).Round(1)
                    : 0
            };
        }
    }
}