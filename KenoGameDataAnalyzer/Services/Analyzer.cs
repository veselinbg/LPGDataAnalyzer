using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer.Services
{
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
                    if (a[i, j].HasValue && b[i, j].HasValue && a[i, j].Value != b[i, j].Value)
                        result[i, j] = (a[i, j].Value + b[i, j].Value).Round();
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
