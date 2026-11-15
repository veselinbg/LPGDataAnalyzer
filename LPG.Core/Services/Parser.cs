using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer.Services
{
    public class Parser
    {
        private const int ExpectedColumns = 22;
        private const double TrimTolerance = 0.1; // 🔥 prevents float fragmentation

        public const int DefaultTrimDelayMs = 850;

        /// <summary>
        /// A delayed row is not used when it is this much older than wanted (a gap in the log).
        /// </summary>
        private const int MaxDelayGapMs = 1500;

        private readonly int _trimDelayMs;

        public Parser(int trimDelayMs = DefaultTrimDelayMs)
        {
            _trimDelayMs = Math.Max(0, trimDelayMs);
        }

        public DataItem[] Data { get; private set; } = [];

        public void Load(string path) => Load(ReadValidLines(path), Path.GetFileName(path));

        /// <summary>
        /// Groups raw rows (one per log line, in time order, see <see cref="CreateRow"/>) the same way as a log file.
        /// Rows with RPM, GAS_b1 or GAS_b2 equal to 0 are dropped, as in a file.
        /// </summary>
        public void Load(IEnumerable<DataItem> rawRows, string sourceFile)
        {
            var result = new List<(DataItem Item, int Start, int End)>();
            var buffer = new List<DataItem>();

            // Every raw row of the file, for interpolation weights and the trim delay.
            var positions = new List<EnginePosition>();
            var times = new List<int>();

            double lastTrimB1 = double.NaN;
            double lastTrimB2 = double.NaN;

            int tempoCounter = 0;

            void Flush()
            {
                var avg = AverageGroup(buffer);
                avg.TEMPO = tempoCounter++;
                avg.SourceFile = sourceFile;
                result.Add((avg, positions.Count - buffer.Count, positions.Count));
                buffer.Clear();
            }

            foreach (var item in rawRows.Where(IsOnGas))
            {
                bool isNewGroup =
                    buffer.Count == 0 ||
                    Math.Abs(item.Trim_b1 - lastTrimB1) > TrimTolerance ||
                    Math.Abs(item.Trim_b2 - lastTrimB2) > TrimTolerance;

                if (isNewGroup && buffer.Count > 0)
                    Flush();

                lastTrimB1 = item.Trim_b1;
                lastTrimB2 = item.Trim_b2;

                buffer.Add(item);
                positions.Add(new EnginePosition(item.RPM, item.BENZ_b1, item.BENZ_b2));
                times.Add(item.TEMPO);
            }

            if (buffer.Count > 0)
                Flush();

            foreach (var (item, start, end) in result)
            {
                item.Positions = positions.GetRange(start, end - start).ToArray();
                item.DelayedPositions = DelayedPositions(positions, times, start, end);
            }

            Data = result.Select(x => x.Item).Where(x => x.TEMPO > 0).ToArray();
        }

        /// <summary>
        /// For each row of a group, the last row logged at least <see cref="_trimDelayMs"/> earlier.
        /// Empty when any row has no such row (start of the file or a gap in the log).
        /// </summary>
        private EnginePosition[] DelayedPositions(List<EnginePosition> positions, List<int> times, int start, int end)
        {
            var delayed = new EnginePosition[end - start];

            for (int k = start; k < end; k++)
            {
                int target = times[k] - _trimDelayMs;

                // Rows are in time order; walk back to the last row at or before the target
                // (a few rows at ~250 ms per row).
                int j = k;
                while (j >= 0 && times[j] > target)
                    j--;

                if (j < 0 || target - times[j] > MaxDelayGapMs)
                    return [];

                delayed[k - start] = positions[j];
            }

            return delayed;
        }

        private static IEnumerable<DataItem> ReadValidLines(string path)
        {
            return File.ReadLines(path)
                .Skip(2)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(ParseLine); // streamed, no full copy of the file
        }

        private static bool IsOnGas(DataItem x) => x.RPM > 0 && x.GAS_b1 > 0 && x.GAS_b2 > 0;

        /// <summary>
        /// Share of rows in the most frequent (RPM column, injection row) cell.
        /// Rows outside the axes count as their own cell.
        /// </summary>
        private static double DominantCellShare(List<DataItem> items)
        {
            if (items.Count <= 1)
                return 1;

            Span<int> keys = items.Count <= 64 ? stackalloc int[items.Count] : new int[items.Count];

            for (int i = 0; i < items.Count; i++)
                keys[i] = (items[i].GetInjectionIndex() + 1) * 1024 + items[i].GetRpmIndex() + 1;

            keys.Sort();

            int best = 1, run = 1;
            for (int i = 1; i < keys.Length; i++)
            {
                run = keys[i] == keys[i - 1] ? run + 1 : 1;
                if (run > best)
                    best = run;
            }

            return (double)best / items.Count;
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
                TimeMs = items[0].TEMPO,
                Stability = DominantCellShare(items),
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

        private static DataItem ParseLine(string line)
        {
            Span<Range> ranges = stackalloc Range[ExpectedColumns];
            var span = line.AsSpan();

            int count = span.Split(ranges, '	');
            if (count < ExpectedColumns)
                return new DataItem();

            return CreateRow(
                tempo: span[ranges[0]].ToInt(),
                rpm: span[ranges[1]].ToInt(),
                gasB1: span[ranges[3]].ToDouble(),
                benzB1: span[ranges[4]].ToDouble(),
                press: span[ranges[5]].ToDouble(),
                map: span[ranges[6]].ToDouble(),
                tempReducer: span[ranges[7]].ToDouble(),
                tempGas: span[ranges[8]].ToDouble(),
                slowB1: span[ranges[10]].ToDouble(),
                fastB1: span[ranges[11]].ToDouble(),
                gasB2: span[ranges[14]].ToDouble(),
                benzB2: span[ranges[15]].ToDouble(),
                slowB2: span[ranges[16]].ToDouble(),
                fastB2: span[ranges[17]].ToDouble());
        }

        /// <summary>
        /// One raw log row with its derived fields (trims, AFR, ratios), as parsed from a log line.
        /// Used for rows that come from a database instead of a file.
        /// </summary>
        public static DataItem CreateRow(int tempo, int rpm,
            double gasB1, double benzB1, double press, double map, double tempReducer, double tempGas,
            double slowB1, double fastB1, double gasB2, double benzB2, double slowB2, double fastB2)
        {
            double trim_b1 = slowB1 + fastB1;
            double trim_b2 = slowB2 + fastB2;
            double trim = (trim_b1 + trim_b2) / 2;

            double ratio_b1 = gasB1 != 0 ? benzB1 / gasB1 : 0;
            double ratio_b2 = gasB2 != 0 ? benzB2 / gasB2 : 0;

            return new DataItem
            {
                TEMPO = tempo,
                RPM = rpm,

                GAS_b1 = gasB1,
                BENZ_b1 = benzB1,
                GAS_b2 = gasB2,
                BENZ_b2 = benzB2,

                PRESS = press,
                MAP = map,
                Temp_RID = tempReducer,
                Temp_GAS = tempGas,

                SLOW_b1 = slowB1,
                FAST_b1 = fastB1,
                SLOW_b2 = slowB2,
                FAST_b2 = fastB2,

                Trim_b1 = trim_b1,
                Trim_b2 = trim_b2,
                Trim = trim,
                Fast = (fastB1 + fastB2) / 2,
                Slow = (slowB1 + slowB2) / 2,
                TrimDiff = trim_b1 - trim_b2,

                AFR_b1 = 15.6 / (1 + trim_b1 / 100),
                AFR_b2 = 15.6 / (1 + trim_b2 / 100),
                AFR = 15.6 / (1 + trim / 100),

                GAS = (gasB1 + gasB2) / 2,
                BENZ = (benzB1 + benzB2) / 2,

                BENZ_Diff = benzB1.RelDiff(benzB2),

                Ratio_b1 = ratio_b1,
                Ratio_b2 = ratio_b2,
                RatioDifference = ratio_b1 - ratio_b2
            };
        }
    }
}
