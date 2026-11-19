using LPGDataAnalyzer.Models;
using System.Text;

namespace LPGDataAnalyzer.Services
{
    public class ExtraInjectionCalculator
    {
        /// <summary>
        /// Step in the gas time found by <see cref="EstimateGasStep"/>: GAS = Slope × BENZ × K × temperature factor
        /// + OffsetAboveMs, minus JumpMs when the petrol time is below StepAtMs.
        /// </summary>
        /// <param name="RmsMs">Scatter of GAS around the fit with the step; RmsWithoutStepMs without it.</param>
        public sealed record GasStepEstimate(
            double StepAtMs, double JumpMs, double Slope, double OffsetAboveMs,
            int Readings, int ReadingsBelow, double RmsMs, double RmsWithoutStepMs);

        private const double StepSearchFromMs = 2.5;
        private const double StepSearchToMs = 6.0;
        private const double MaxBenzSpreadMs = 0.1;
        private const int MinReadingsPerSide = 30;

        /// <summary>
        /// Finds the petrol time at which the gas time steps, from GAS against the gas time the map predicts.
        /// <para>On the M272 logs (2026-11-19) the gas time is ~0.4 ms shorter for petrol times below exactly 4.00 ms,
        /// on both maps. That matches the AEB "Extra-injection ident. time" (4 ms, <c>TempoMaxExtraInjBenz</c>) and a
        /// gas injector dead time (1.00 ms) replaced by the "extra-injection sensitivity" time (0.60 ms) below it:
        /// so the step position estimates the ident time, and dead time − jump the sensitivity time (a hypothesis
        /// until a config with other values confirms it).</para>
        /// <para>Uses each reading (group) per bank whose raw rows' petrol times lie within 0.1 ms, between 2.5 and
        /// 6 ms; the map value is bilinear at the rows and the temperature tables are applied as the ECU does.</para>
        /// </summary>
        /// <param name="setupOf">The ECU setup a reading was recorded with (by its log), or null to skip it.</param>
        /// <returns>Null when there are too few readings on either side of every candidate step.</returns>
        public static GasStepEstimate? EstimateGasStep(IEnumerable<DataItem> data, Func<DataItem, EcuSetup?> setupOf)
        {
            var points = new List<(double Benz, double Expected, double Gas)>();

            foreach (var d in data)
            {
                var setup = setupOf(d);
                if (setup is null || d.Positions.Length == 0)
                    continue;

                double factor = setup.TemperatureFactor(d.Temp_GAS, d.Temp_RID);
                AddBank(d.GAS_b1, static p => p.Benz1);
                AddBank(d.GAS_b2, static p => p.Benz2);

                void AddBank(double gas, Func<EnginePosition, double> benzOf)
                {
                    if (gas <= 0)
                        return;

                    double min = double.MaxValue, max = double.MinValue, benzSum = 0, mapSum = 0;
                    foreach (var p in d.Positions)
                    {
                        double benz = benzOf(p);
                        if (benz <= 0 || p.Rpm <= 0)
                            return;
                        min = Math.Min(min, benz);
                        max = Math.Max(max, benz);
                        benzSum += benz;
                        mapSum += setup.MapValueAt(p.Rpm, benz);
                    }

                    double benzAvg = benzSum / d.Positions.Length;
                    if (max - min > MaxBenzSpreadMs || benzAvg < StepSearchFromMs || benzAvg >= StepSearchToMs)
                        return;

                    points.Add((benzAvg, benzAvg * mapSum / d.Positions.Length / 100 * factor, gas));
                }
            }

            // Coarse scan in 0.05 ms, then 0.01 ms around the best step.
            Fit? best = null;
            for (double s = StepSearchFromMs + 0.05; s < StepSearchToMs; s += 0.05)
                best = Better(best, FitWithStep(points, Math.Round(s, 2)));
            if (best is null)
                return null;

            double coarse = best.Value.Step;
            for (double s = coarse - 0.04; s <= coarse + 0.04; s += 0.01)
                best = Better(best, FitWithStep(points, Math.Round(s, 2)));

            var f = best!.Value;
            var none = FitWithStep(points, double.NegativeInfinity, requireBothSides: false);

            return new GasStepEstimate(f.Step, -f.Below, f.Slope, f.Offset, f.Count, f.CountBelow, f.Rms, none?.Rms ?? double.NaN);
        }

        private readonly record struct Fit(double Step, double Slope, double Offset, double Below, int Count, int CountBelow, double Rms);

        private static Fit? Better(Fit? a, Fit? b) => b is null ? a : a is null || b.Value.Rms < a.Value.Rms ? b : a;

        /// <summary>Least squares GAS = Slope × expected + Offset + Below × (benz &lt; step).</summary>
        private static Fit? FitWithStep(List<(double Benz, double Expected, double Gas)> points, double step, bool requireBothSides = true)
        {
            // Normal equations for the unknowns (Slope, Offset, Below).
            var m = new double[3, 4];
            int below = 0;
            foreach (var (benz, x, y) in points)
            {
                double z = benz < step ? 1 : 0;
                below += (int)z;
                double[] row = [x, 1, z];
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                        m[i, j] += row[i] * row[j];
                    m[i, 3] += row[i] * y;
                }
            }

            if (requireBothSides && (below < MinReadingsPerSide || points.Count - below < MinReadingsPerSide))
                return null;
            if (!requireBothSides && points.Count < MinReadingsPerSide)
                return null;

            // Without a step the third unknown is unused: solve the 2 × 2 system.
            int n = below == 0 || below == points.Count ? 2 : 3;
            for (int i = 0; i < n; i++)
            {
                double pivot = m[i, i];
                if (Math.Abs(pivot) < 1e-12)
                    return null;
                for (int j = i; j < 4; j++)
                    m[i, j] /= pivot;
                for (int r = 0; r < n; r++)
                {
                    if (r == i)
                        continue;
                    double factor = m[r, i];
                    for (int j = i; j < 4; j++)
                        m[r, j] -= factor * m[i, j];
                }
            }

            double slope = m[0, 3], offset = m[1, 3], step0 = n == 3 ? m[2, 3] : 0;
            double sum = 0;
            foreach (var (benz, x, y) in points)
            {
                double e = y - slope * x - offset - (benz < step ? step0 : 0);
                sum += e * e;
            }

            return new Fit(step, slope, offset, step0, points.Count, below, Math.Sqrt(sum / points.Count));
        }

        public static double CalculateIdentTime(ICollection<DataItem>? data)
        {
            if (data is null) 
                return 0;
            // 1️⃣ Filter steady-state data
            var benzValues = data
                .Where(d => d.RPM > 0 && d.RPM < 3500)
                .Where(d => d.MAP > 0 && d.MAP < 0.35)
                .Where(d => d.BENZ_b1< 3.0 || d.BENZ_b2 < 3.0)
                .Select(d=>d.BENZ)
                .ToList();

            if (benzValues.Count < 100)
                throw new Exception("Not enough valid data for calculation.");

            // 2️⃣ Build histogram (0.1 ms bins)
            double binSize = 0.1;
            var histogram = benzValues
                .GroupBy(v => Math.Round(v / binSize) * binSize)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());

            // 3️⃣ Detect micro-injection peak (<2 ms)
            var microPeak = histogram
                .Where(h => h.Key < 2)
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
        public static string PrintHistogram(ICollection<DataItem>? data)
        {
            if(data is null)
                return "No data available for histogram.";

            double binSize = 0.1;

            var values = data
                .Where(d => d.RPM > 0 && d.RPM < 3500)
                .Where(d => d.MAP > 0 && d.MAP < 0.7)
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
        public static double CalculateExtraInjectionTime(DataItem[]? data)
        {
            if (data is null || data.Length < 2)
                return 0;

            double steadyRatio = -1; // Start with -1 to ensure first ratio initialization happens properly
            double learnedExtra = 0;

            double alphaRatio = 0.05;
            double alphaExtra = 0.15;

            for (int i = 1; i < data.Length; i++)
            {
                var prev = data[i - 1];
                var curr = data[i];

                if (curr.SourceFile != prev.SourceFile)
                    continue; // Different log files: no time relation

                // TimeMs is the real log time; TEMPO is only a reading index after parsing.
                double deltaTimeMs = curr.TimeMs - prev.TimeMs;
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

            return Math.Clamp(learnedExtra, 0, 20).Round(); // Return the learned extra injection time
        }
    }
}
