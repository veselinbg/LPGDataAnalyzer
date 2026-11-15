using LPGDataAnalyzer.Models;
using static LPGDataAnalyzer.Models.Settings;

namespace LPGDataAnalyzer.Services
{
    public class Analyzer
    {
        /// <summary>
        /// Band indexes of the selected labels, or null when the selection means "all"
        /// (nothing selected, or <see cref="Settings.ALL"/> selected).
        /// </summary>
        private static HashSet<int>? SelectedBandIndexes((int Min, int Max, string Label)[] ranges, List<string>? labels)
        {
            if (labels is null || labels.Count == 0 || labels.Contains(Settings.ALL))
                return null;

            var result = new HashSet<int>();
            for (int i = 0; i < ranges.Length; i++)
            {
                if (labels.Contains(ranges[i].Label))
                    result.Add(i);
            }
            return result;
        }

        public static DataItem[] FilterByTemp(
            DataItem[] data,
            List<string>? sLPGTempGroups,
            List<string>? sReductorTempGroups)
        {
            if (data is null)
                return [];

            var reductorBands = SelectedBandIndexes(Settings.ReductorTemperatureRanges, sReductorTempGroups);
            var gasBands = SelectedBandIndexes(Settings.GasTemperatureRanges, sLPGTempGroups);

            if (reductorBands is null && gasBands is null)
                return data;

            IEnumerable<DataItem> result = data;

            if (reductorBands is not null)
                result = result.Where(d => reductorBands.Contains(d.Temp_RID.GetTempBandIndex(Settings.ReductorTemperatureRanges)));

            if (gasBands is not null)
                result = result.Where(d => gasBands.Contains(d.Temp_GAS.GetTempBandIndex(Settings.GasTemperatureRanges)));

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

            // Buckets for values - pre-allocated for better cache locality
            var buckets = new List<double>[rpmCount, injCount];

            // Pre-allocate all bucket lists to avoid allocation during iteration
            int estimatedItemsPerBucket = Math.Max(1, data.Length / (rpmCount * injCount));
            for (int r = 0; r < rpmCount; r++)
            {
                for (int i = 0; i < injCount; i++)
                {
                    buckets[r, i] = new List<double>(capacity: estimatedItemsPerBucket);
                }
            }

            // 1️⃣ Single pass: distribute data into buckets
            foreach (var d in data)
            {
                var value = valueBankSelector(d);
                if (!value.HasValue)
                    continue;

                int rpmIndex = d.GetRpmIndex();
                int injIndex = d.GetInjectionIndex(injectionBankSelector);

                if (rpmIndex < 0 || injIndex < 0)
                    continue; // outside the map axes

                buckets[rpmIndex, injIndex].Add(value.Value);  // No null check needed - pre-allocated
            }
            Func<List<double>, double> aggregator = aggregation switch
            {
                Aggregation.Average => Extensions.AverageFast,
                Aggregation.Min => Extensions.MinFast,
                Aggregation.Max => Extensions.MaxFast,
                Aggregation.Median => Extensions.Median,
                _ => throw new ArgumentOutOfRangeException()
            };
            // 2️⃣ Aggregate buckets into final table
            for (int r = 0; r < rpmCount; r++)
            {
                for (int i = 0; i < injCount; i++)
                {
                    var values = buckets[r, i];

                    if (values is { Count: > 0 })
                        table[r, i] = aggregator(values).Round();
                }
            }

            return table;
        }
        public static double?[,] Subtract(double?[,] a, double?[,] b)
        {
            if (a == null || b == null)
                throw new ArgumentNullException("Input arrays cannot be null.");

            int rowsA = a.GetLength(0);
            int colsA = a.GetLength(1);

            int rowsB = b.GetLength(0);
            int colsB = b.GetLength(1);

            if (rowsA != rowsB || colsA != colsB)
                throw new ArgumentException("Arrays must have the same dimensions.");

            var result = new double?[rowsA, colsA];

            for (int i = 0; i < rowsA; i++)
            {
                for (int j = 0; j < colsA; j++)
                {
                    if (a[i, j] is double va && b[i, j] is double vb && va != vb)
                        result[i, j] = ((va + vb) / 2).Round();
                    else
                        result[i, j] = null; // preserve missing data
                }
            }

            return result;
        }
        public static double?[,] BuildFuelCorrectionMap(DataItem[] data)
        {
            int injs = Settings.InjectionRanges.Length;
            int rpms = Settings.RpmColumns.Length;

            double[,] sum = new double[injs, rpms];
            int[,] count = new int[injs, rpms];

            foreach (var d in data)
            {
                int row = d.GetInjectionIndex();
                int col = d.GetRpmIndex();

                if (row < 0 || col < 0)
                    continue;

                if (Math.Abs(d.BENZ_b1) < 1e-9)
                    continue;

                double diff =
                    ((d.BENZ_b2 - d.BENZ_b1) / d.BENZ_b1) * 100.0;

                sum[row, col] += diff;
                count[row, col]++;
            }

            // 🔥 SWAPPED OUTPUT DIMENSIONS
            var map = new double?[rpms, injs];

            for (int inj = 0; inj < injs; inj++)
            {
                for (int rpm = 0; rpm < rpms; rpm++)
                {
                    if (count[inj, rpm] == 0)
                        continue;

                    double avgDiff = sum[inj, rpm] / count[inj, rpm];

                    map[rpm, inj] = Math.Round(-avgDiff, 2);
                }
            }

            return map;
        }
    }
}
