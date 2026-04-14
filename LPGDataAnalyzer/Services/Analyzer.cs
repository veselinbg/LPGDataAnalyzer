using LPGDataAnalyzer.Models;
using System.Runtime.Intrinsics.Arm;

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
    }
}
