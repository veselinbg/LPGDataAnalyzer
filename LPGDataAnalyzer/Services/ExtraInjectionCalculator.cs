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

        /// <summary>
        /// Extra-Injection Ident. Time calculator base version
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static double CalculateExtraInjectionTime(IList<DataItem> data)
        {
            if (data == null || data.Count < 2)
                return 0;

            double steadyRatio = -1; // Start with -1 to ensure first ratio initialization happens properly
            double learnedExtra = 0;

            double alphaRatio = 0.05;
            double alphaExtra = 0.15;

            for (int i = 1; i < data.Count; i++)
            {
                var prev = data[i - 1];
                var curr = data[i];

                double deltaTimeMs = curr.TEMPO - prev.TEMPO;
                if (deltaTimeMs <= 0) continue; // Skip invalid delta times

                double deltaTime = deltaTimeMs / 1000.0; // Convert to seconds

                // Skip invalid BENZ_b1 or GAS_b1 values
                if (curr.BENZ_b1 <= 0 || curr.GAS_b1 <= 0)
                    continue;

                double deltaRpm = curr.RPM - prev.RPM;
                double deltaMap = curr.MAP - prev.MAP;
                double deltaBenz = curr.BENZ_b1 - prev.BENZ_b1;

                double dBenz_dt = deltaBenz / deltaTime;
                double dMap_dt = deltaMap / deltaTime;

                // -------------------------
                // STABLE ZONE DETECTION
                // -------------------------
                bool isStable =
                    Math.Abs(deltaRpm) < 100 &&  // Small RPM changes
                    Math.Abs(deltaMap) < 0.05 && // Small MAP changes
                    curr.BENZ_b1 > 1.0;          // High enough benzene level

                if (isStable)
                {
                    double ratio = curr.GAS_b1 / curr.BENZ_b1;

                    if (steadyRatio == -1) // Initialize steadyRatio only when first valid ratio is encountered
                        steadyRatio = ratio;
                    else
                        steadyRatio = (1 - alphaRatio) * steadyRatio + alphaRatio * ratio; // Smooth the ratio
                }

                // -------------------------
                // TRANSIENT DETECTION
                // -------------------------
                bool isTransient =
                    dBenz_dt > 1.5 ||    // Benzene change is too rapid
                    dMap_dt > 0.5;       // MAP change is too rapid

                if (isTransient && steadyRatio > 0)
                {
                    double expectedGas = curr.BENZ_b1 * steadyRatio;
                    double rawExtra = curr.GAS_b1 - expectedGas;

                    if (rawExtra > 0)
                    {
                        double extra = Math.Clamp(rawExtra, 0, 20); // Limit the "extra" value to a reasonable range
                        learnedExtra = (1 - alphaExtra) * learnedExtra + alphaExtra * extra; // Adaptive learning
                    }
                }
            }

            return Math.Round(Math.Clamp(learnedExtra, 0, 20), 2); // Return the learned extra injection time
        }
    }
}
