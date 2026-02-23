using LPGDataAnalyzer.Models;
using System.Text;

namespace LPGDataAnalyzer.Services
{
    internal class ExtraInjectionCalculator
    {
        public static double CalculateIdentTime(ICollection<DataItem> data)
        {
            // 1️⃣ Filter steady-state data
            var benzValues = data
                .Where(d => d.RPM > 500 && d.RPM < 3500)
                .Where(d => d.MAP > 0.2 && d.MAP < 0.7)
                .Select(d => d.BENZ_b1)
                .Where(v => v > 0.3 && v < 6.0)
                .ToList();

            if (benzValues.Count < 100)
                throw new Exception("Not enough valid data for calculation.");

            // 2️⃣ Build histogram (0.1 ms bins)
            double binSize = 0.1;
            var histogram = benzValues
                .GroupBy(v => Math.Round(v / binSize) * binSize)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());

            // 3️⃣ Detect micro-injection peak (<1.2 ms)
            var microPeak = histogram
                .Where(h => h.Key < 1.2)
                .OrderByDescending(h => h.Value)
                .First().Key;

            // 4️⃣ Detect normal injection peak (>2.0 ms)
            var normalPeak = histogram
                .Where(h => h.Key > 2.0)
                .OrderByDescending(h => h.Value)
                .First().Key;

            // 5️⃣ Find valley between peaks
            var identTime = histogram
                .Where(h => h.Key > microPeak && h.Key < normalPeak)
                .OrderBy(h => h.Value)
                .First().Key;

            return identTime.Round();
        }
        public static string PrintHistogram(ICollection<DataItem> data)
        {
            double binSize = 0.1;

            var values = data
                .Where(d => d.RPM > 500 && d.RPM < 3500)
                .Where(d => d.MAP > 0.2 && d.MAP < 0.7)
                .Select(d => d.BENZ_b1)
                .Where(v => v > 0.3 && v < 6.0)
                .ToList();

            var histogram = values
                .GroupBy(v => Math.Round(v / binSize) * binSize)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());

            int maxCount = histogram.Values.Max();
            var sResult = new StringBuilder();

            sResult.AppendLine("BENZ ms | Histogram");
            sResult.AppendLine("----------------------------");

            foreach (var bin in histogram)
            {
                int barLength = (int)(40.0 * bin.Value / maxCount);
                sResult.AppendLine(
                    $"{bin.Key,5:F1} ms | {new string('#', barLength)}"
                );
            }
            return sResult.ToString();
        }
    }
}
