using LPGDataAnalyzer.Models;
using System.Runtime.InteropServices;

namespace LPGDataAnalyzer.Services
{
    internal sealed partial class Prediction
    {
        // ===================== AXIS SPLITS =====================

        public static AxisSplit<int> RpmSplit(int rpm)
        {
            var cols = Settings.RpmColumns;
            int last = cols.Length - 1;

            if (rpm <= cols[0].Min)
                return new(cols[0].Label, cols[0].Label, 1, 0);

            if (rpm >= cols[last].Max)
                return new(cols[last].Label, cols[last].Label, 1, 0);

            for (int i = 0; i < last; i++)
            {
                var c = cols[i];
                if (rpm > c.Min && rpm <= c.Max)
                {
                    var n = cols[i + 1];
                    double w = (double)(rpm - c.Min) / (c.Max - c.Min);
                    return new(c.Label, n.Label, 1.0 - w, w);
                }
            }

            // fail-soft for imperfect bins
            return new(cols[last].Label, cols[last].Label, 1, 0);
        }

        public static AxisSplit<double> InjSplit(double inj)
        {
            var ranges = Settings.InjectionRanges;
            int last = ranges.Length - 1;

            if (inj <= ranges[0].Min)
                return new(ranges[0].Label, ranges[0].Label, 1, 0);

            if (inj >= ranges[last].Max)
                return new(ranges[last].Label, ranges[last].Label, 1, 0);

            for (int i = 0; i < last; i++)
            {
                var r = ranges[i];
                if (inj > r.Min && inj <= r.Max)
                {
                    var n = ranges[i + 1];
                    double w = (inj - r.Min) / (r.Max - r.Min);
                    return new(r.Label, n.Label, 1.0 - w, w);
                }
            }

            return new(ranges[last].Label, ranges[last].Label, 1, 0);
        }

        // ===================== TRIM =====================

        private static double GetRawTrim(DataItem log)
        {
            double slow = (log.SLOW_b1 + log.SLOW_b2) * 0.5;
            double fast = (log.FAST_b1 + log.FAST_b2) * 0.5;
            return (0.7 * slow) + (0.3 * fast);
        }

        private static bool TryGetValidTrim(
            DataItem log,
            double mean,
            double stdDev,
            out double trim)
        {
            trim = GetRawTrim(log);

            if (Math.Abs(trim) > 25)
                return false;

            if (stdDev > 0)
            {
                double z = (trim - mean) / stdDev;
                if (Math.Abs(z) > 2.5)
                    return false;
            }

            return true;
        }

        // ===================== ACCUMULATOR =====================

        private sealed class CellAccumulator
        {
            public double DeltaSum;
            public double WeightSum;
            public int HitCount;

            public void Add(double delta, double weight)
            {
                DeltaSum += delta * weight;
                WeightSum += weight;
                HitCount++;
            }

            public double GetDelta(double confidence)
            {
                if (WeightSum <= 0)
                    return 0;

                return (DeltaSum / WeightSum) * confidence;
            }
        }

        private static double SmoothStep(double edge0, double edge1, double x)
        {
            if (x <= edge0) return 0;
            if (x >= edge1) return 1;

            double t = (x - edge0) / (edge1 - edge0);
            return t * t * (3 - 2 * t);
        }

        // ===================== LEARNING =====================

        private static Dictionary<(int, double), CellAccumulator>
            AccumulateDeltas(
                IList<DataItem> logs,
                double baseRate,
                double mean,
                double stdDev)
        {
            var acc = new Dictionary<(int, double), CellAccumulator>(256);

            var rpmCols = Settings.RpmColumns;
            var injRanges = Settings.InjectionRanges;

            int rpmMin = rpmCols[0].Min;
            int rpmMax = rpmCols[^1].Max;
            double injMin = injRanges[0].Min;
            double injMax = injRanges[^1].Max;

            double rpmFadeStart = rpmCols[0].Min;
            double rpmFadeEnd = rpmCols[1].Max;
            double injFadeStart = injRanges[0].Min;
            double injFadeEnd = injRanges[1].Max;

            foreach (var log in logs)
            {
                int rpm = Math.Clamp(log.RPM, rpmMin, rpmMax);
                double inj = Math.Clamp(
                    (log.BENZ_b1 + log.BENZ_b2) * 0.5,
                    injMin,
                    injMax);

                if (!TryGetValidTrim(log, mean, stdDev, out double trim))
                    continue;

                double correction = 1.0 + trim / 100.0;
                if (correction < 0.95 || correction > 1.08)
                    continue;

                double rpmFactor = SmoothStep(rpmFadeStart, rpmFadeEnd, rpm);
                double injFactor = SmoothStep(injFadeStart, injFadeEnd, inj);

                double regionFactor = 0.1 + 0.9 * rpmFactor * injFactor;
                double delta = (correction - 1.0) * baseRate * regionFactor;

                if (Math.Abs(delta) < 1e-9)
                    continue;

                var r = RpmSplit(rpm);
                var i = InjSplit(inj);

                Add(acc, r.Low, i.Low, delta, r.WLow * i.WLow);
                Add(acc, r.Low, i.High, delta, r.WLow * i.WHigh);
                Add(acc, r.High, i.Low, delta, r.WHigh * i.WLow);
                Add(acc, r.High, i.High, delta, r.WHigh * i.WHigh);
            }

            return acc;
        }

        private static void Add(
            Dictionary<(int, double), CellAccumulator> dict,
            int rpm,
            double inj,
            double delta,
            double weight)
        {
            if (weight <= 0)
                return;

            var key = (rpm, inj);
            ref var acc = ref CollectionsMarshal.GetValueRefOrAddDefault(
                dict, key, out _);

            acc ??= new CellAccumulator();
            acc.Add(delta, weight);
        }

        // ===================== EDGE PROPAGATION =====================

        private static readonly (int di, int dj)[] Neighbors =
        {
            (-1, 0), (1, 0), (0, -1), (0, 1)
        };

        private static void PropagateEdgeDeltas(
            Dictionary<(int, double), CellAccumulator> acc,
            IList<int> rpmLabels,
            IList<double> injLabels,
            double minWeight)
        {
            const double Damping = 0.5;
            const int MaxIterations = 20;

            for (int iter = 0; iter < MaxIterations; iter++)
            {
                bool any = false;

                for (int i = 0; i < rpmLabels.Count; i++)
                {
                    for (int j = 0; j < injLabels.Count; j++)
                    {
                        var key = (rpmLabels[i], injLabels[j]);

                        if (acc.TryGetValue(key, out var cur) &&
                            cur.WeightSum > minWeight)
                            continue;

                        foreach (var (di, dj) in Neighbors)
                        {
                            int ni = i + di;
                            int nj = j + dj;

                            if (ni < 0 || nj < 0 ||
                                ni >= rpmLabels.Count ||
                                nj >= injLabels.Count)
                                continue;

                            var nKey = (rpmLabels[ni], injLabels[nj]);
                            if (!acc.TryGetValue(nKey, out var nAcc) ||
                                nAcc.WeightSum < minWeight)
                                continue;

                            cur ??= acc[key] = new CellAccumulator();
                            cur.DeltaSum += nAcc.DeltaSum * Damping;
                            cur.WeightSum += nAcc.WeightSum * Damping;
                            cur.HitCount += nAcc.HitCount / 2;
                            any = true;
                        }
                    }
                }

                if (!any)
                    break;
            }
        }

        // ===================== APPLY =====================

        private static void ApplyDeltas(
            Dictionary<(int, double), CellAccumulator> acc,
            Dictionary<(int, double), FuelCell> cellMap,
            (double BaseLearningRate,
             double MinEffectiveWeight,
             double MaxDeltaPerCell,
             int TargetHitCount,
             double MeanTrim,
             double StdTrim) c)
        {
            foreach (var kv in acc)
            {
                if (kv.Value.WeightSum <= c.MinEffectiveWeight)
                    continue;

                if (!cellMap.TryGetValue(kv.Key, out var cell))
                    continue;

                double confidence =
                    Math.Min(1.0, (double)kv.Value.HitCount / c.TargetHitCount);

                double delta = kv.Value.GetDelta(confidence);
                delta = Math.Clamp(delta, -c.MaxDeltaPerCell, c.MaxDeltaPerCell);

                // NO rounding here
                cell.Value *= (1 + delta);
            }
        }

        // ===================== SMOOTH =====================

        private static void SmoothFuelMap(
            Dictionary<(int, double), FuelCell> cellMap,
            IList<int> rpmLabels,
            IList<double> injLabels,
            int kernelSize,
            double sigma)
        {
            if (kernelSize % 2 == 0)
                throw new ArgumentException("kernelSize must be odd.");

            int half = kernelSize / 2;
            double[,] kernel = new double[kernelSize, kernelSize];

            for (int di = -half; di <= half; di++)
                for (int dj = -half; dj <= half; dj++)
                    kernel[di + half, dj + half] =
                        Math.Exp(-(di * di + dj * dj) / (2 * sigma * sigma));

            var newValues = new Dictionary<(int, double), double>(cellMap.Count);

            for (int i = 0; i < rpmLabels.Count; i++)
            {
                for (int j = 0; j < injLabels.Count; j++)
                {
                    var key = (rpmLabels[i], injLabels[j]);
                    if (!cellMap.TryGetValue(key, out var cell))
                        continue;

                    double sumW = 0, sumV = 0;

                    for (int di = -half; di <= half; di++)
                    {
                        int ni = i + di;
                        if (ni < 0 || ni >= rpmLabels.Count) continue;

                        for (int dj = -half; dj <= half; dj++)
                        {
                            int nj = j + dj;
                            if (nj < 0 || nj >= injLabels.Count) continue;

                            if (!cellMap.TryGetValue(
                                (rpmLabels[ni], injLabels[nj]), out var n))
                                continue;

                            double w = kernel[di + half, dj + half];
                            sumW += w;
                            sumV += n.Value * w;
                        }
                    }

                    if (sumW > 0)
                        newValues[key] = sumV / sumW;
                }
            }

            foreach (var kv in newValues)
                cellMap[kv.Key].Value = kv.Value;
        }

        // ===================== FINAL ROUND =====================

        private static void RoundFuelMap(
            Dictionary<(int, double), FuelCell> cellMap)
        {
            foreach (var cell in cellMap.Values)
                cell.Value = cell.Value.Round(0);
        }

        // ===================== CONSTANTS =====================

        private static (
            double BaseLearningRate,
            double MinEffectiveWeight,
            double MaxDeltaPerCell,
            int TargetHitCount,
            double MeanTrim,
            double StdTrim)
            ComputeAdaptiveConstants(IList<DataItem> logs)
        {
            double sum = 0, sumSq = 0, maxAbs = 0;

            foreach (var log in logs)
            {
                double t = GetRawTrim(log);
                sum += t;
                sumSq += t * t;
                maxAbs = Math.Max(maxAbs, Math.Abs(t));
            }

            double mean = sum / logs.Count;
            double std = Math.Sqrt((sumSq / logs.Count) - (mean * mean));

            double baseRate = Math.Clamp(std / 50.0, 0.1, 0.5);
            double minWeight = 1.0 / logs.Count;
            double maxDelta = Math.Clamp(maxAbs / 300.0, 0.01, 0.05);

            int targetHit = Math.Max(
                5,
                logs.Count /
                (Settings.RpmColumns.Length *
                 Settings.InjectionRanges.Length));

            return (baseRate, minWeight, maxDelta, targetHit, mean, std);
        }

        // ===================== PUBLIC ENTRY =====================

        public FuelCorrectionResult AutoCorrectFuelTable(
            IEnumerable<DataItem> validLogs,
            IEnumerable<FuelCell> fuelTable)
        {
            var logs = validLogs as IList<DataItem> ?? validLogs.ToList();
            if (logs.Count == 0)
                throw new InvalidOperationException("No valid logs provided.");

            var cellMap = fuelTable.ToDictionary(c => (c.RpmBin, c.InjBin));

            var rpmLabels = Settings.RpmColumns.Select(c => c.Label).ToArray();
            var injLabels = Settings.InjectionRanges.Select(r => r.Label).ToArray();

            var constants = ComputeAdaptiveConstants(logs);

            var acc = AccumulateDeltas(
                logs,
                constants.BaseLearningRate,
                constants.MeanTrim,
                constants.StdTrim);

            PropagateEdgeDeltas(
                acc,
                rpmLabels,
                injLabels,
                constants.MinEffectiveWeight);

            ApplyDeltas(acc, cellMap, constants);

            SmoothFuelMap(
                cellMap,
                rpmLabels,
                injLabels,
                kernelSize: 5,
                sigma: 1.2);

            RoundFuelMap(cellMap);

            return new FuelCorrectionResult
            {
                UpdatedCells = cellMap.Values.ToList()
            };
        }
    }
}