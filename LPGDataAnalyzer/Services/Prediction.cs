using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer.Services
{
    internal sealed partial class Prediction
    {
        public static AxisSplit<int> RpmSplit(int rpm)
        {
            var cols = Settings.RpmColumns;
            int lastIndex = cols.Length - 1;

            var first = cols[0];
            var last = cols[lastIndex];

            if (rpm <= first.Min)
                return new AxisSplit<int>(first.Label, first.Label, 1, 0);

            if (rpm >= last.Max)
                return new AxisSplit<int>(last.Label, last.Label, 1, 0);

            for (int i = 0; i < lastIndex; i++)
            {
                var c = cols[i];
                if (rpm > c.Min && rpm <= c.Max)
                {
                    var next = cols[i + 1];
                    double wHigh = (double)(rpm - c.Min) / (c.Max - c.Min);
                    return new AxisSplit<int>(
                        c.Label,
                        next.Label,
                        1.0 - wHigh,
                        wHigh);
                }
            }

            return new AxisSplit<int>(last.Label, last.Label, 1, 0);
        }

        public static AxisSplit<double> InjSplit(double inj)
        {
            var ranges = Settings.InjectionRanges;
            int lastIndex = ranges.Length - 1;

            var first = ranges[0];
            var last = ranges[lastIndex];

            if (inj <= first.Min)
                return new AxisSplit<double>(first.Label, first.Label, 1, 0);

            if (inj >= last.Max)
                return new AxisSplit<double>(last.Label, last.Label, 1, 0);

            for (int i = 0; i < lastIndex; i++)
            {
                var r = ranges[i];
                if (inj > r.Min && inj <= r.Max)
                {
                    var next = ranges[i + 1];
                    double wHigh = (inj - r.Min) / (r.Max - r.Min);

                    return new AxisSplit<double>(
                        r.Label,
                        next.Label,
                        1.0 - wHigh,
                        wHigh);
                }
            }

            return new AxisSplit<double>(last.Label, last.Label, 1, 0);
        }


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

            public double GetWeightedDelta(double confidence)
            {
                if (WeightSum <= 0) return 0;
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

        private static double GetRawTrim(DataItem log)
        {
            double slow = (log.SLOW_b1 + log.SLOW_b2) / 2.0;
            double fast = (log.FAST_b1 + log.FAST_b2) / 2.0;

            return (0.7 * slow) + (0.3 * fast);
        }


        private static double GetFilteredTrim(
    DataItem log,
    double mean,
    double stdDev)
        {
            double trim = GetRawTrim(log);

            if (Math.Abs(trim) > 25)
                return 0;

            if (stdDev > 0)
            {
                double z = (trim - mean) / stdDev;
                if (Math.Abs(z) > 2.5)
                    return 0;
            }

            return trim;
        }

        private static double GetTrims(DataItem log)
        {
            double slow = (log.SLOW_b1 + log.SLOW_b2) / 2.0;
            double fast = (log.FAST_b1 + log.FAST_b2) / 2.0;

            return (0.7 * slow) + (0.3 * fast);
        }
        private static Dictionary<(int, double), CellAccumulator>
               AccumulateDeltas(
                   IList<DataItem> logs,
                   double baseRate,
                   double mean,
                   double stdDev)
        {
            var dict = new Dictionary<(int, double), CellAccumulator>(256);

            var rpmCols = Settings.RpmColumns;
            var injRanges = Settings.InjectionRanges;

            int rpmMin = rpmCols[0].Min;
            int rpmMax = rpmCols[^1].Max;
            double injMin = injRanges[0].Min;
            double injMax = injRanges[^1].Max;

            foreach (var log in logs)
            {
                int rpm = Math.Clamp(log.RPM, rpmMin, rpmMax);
                double inj = Math.Clamp(
                    (log.BENZ_b1 + log.BENZ_b2) / 2.0,
                    injMin,
                    injMax);

                double trim = GetFilteredTrim(log, mean, stdDev);
                if (trim == 0)
                    continue;

                double correction =
                    Math.Clamp(1.0 + trim / 100.0, 0.95, 1.08);

                double rpmFactor = SmoothStep(800, 1100, rpm);
                double injFactor = SmoothStep(1.5, 2.5, inj);

                double regionFactor =
                    0.1 + 0.9 * rpmFactor * injFactor;

                double delta =
                    (correction - 1.0) *
                    baseRate *
                    regionFactor;

                if (Math.Abs(delta) < 1e-7)
                    continue;

                var rpmSplit = RpmSplit(rpm);
                var injSplit = InjSplit(inj);

                Add(dict, rpmSplit.Low, injSplit.Low,
                    delta, rpmSplit.WLow * injSplit.WLow);

                Add(dict, rpmSplit.Low, injSplit.High,
                    delta, rpmSplit.WLow * injSplit.WHigh);

                Add(dict, rpmSplit.High, injSplit.Low,
                    delta, rpmSplit.WHigh * injSplit.WLow);

                Add(dict, rpmSplit.High, injSplit.High,
                    delta, rpmSplit.WHigh * injSplit.WHigh);
            }

            return dict;
        }
        private static void Add(
            Dictionary<(int, double), CellAccumulator> dict,
            int rpm, double inj,
            double delta, double weight)
        {
            if (weight <= 0) return;

            var key = (rpm, inj);

            if (!dict.TryGetValue(key, out var acc))
            {
                acc = new CellAccumulator();
                dict[key] = acc;
            }

            acc.Add(delta, weight);
        }

        private static void PropagateEdgeDeltas(
            Dictionary<(int, double), CellAccumulator> accumulators,
            IList<int> rpmLabels,
            IList<double> injLabels,
            double minWeight)
        {
            const double Damping = 0.5;
            const int MaxIterations = 20;

            int rpmCount = rpmLabels.Count;
            int injCount = injLabels.Count;

            for (int iteration = 0; iteration < MaxIterations; iteration++)
            {
                bool any = false;

                for (int i = 0; i < rpmCount; i++)
                {
                    for (int j = 0; j < injCount; j++)
                    {
                        var key = (rpmLabels[i], injLabels[j]);

                        if (accumulators.TryGetValue(key, out var current) &&
                            current.WeightSum > minWeight)
                            continue;

                        void TryProp(int ni, int nj)
                        {
                            var nKey = (rpmLabels[ni], injLabels[nj]);
                            if (!accumulators.TryGetValue(nKey, out var nAcc) ||
                                nAcc.WeightSum < minWeight)
                                return;

                            if (!accumulators.TryGetValue(key, out current))
                            {
                                current = new CellAccumulator();
                                accumulators[key] = current;
                            }

                            current.DeltaSum += nAcc.DeltaSum * Damping;
                            current.WeightSum += nAcc.WeightSum * Damping;
                            current.HitCount += nAcc.HitCount / 2;

                            any = true;
                        }

                        if (i > 0) TryProp(i - 1, j);
                        if (i < rpmCount - 1) TryProp(i + 1, j);
                        if (j > 0) TryProp(i, j - 1);
                        if (j < injCount - 1) TryProp(i, j + 1);
                    }
                }

                if (!any) break;
            }
        }


        // Apply deltas to fuel table
        private static FuelCorrectionResult ApplyDeltasToFuelTable(
            Dictionary<(int, double), CellAccumulator> acc,
            Dictionary<(int, double), FuelCell> cellMap,
            (double BaseLearningRate,
             double MinEffectiveWeight,
             double MaxDeltaPerCell,
             int TargetHitCount,
             double MeanTrim,
             double StdTrim) constants)
        {
            var result = new FuelCorrectionResult();

            foreach (var kv in acc)
            {
                if (kv.Value.WeightSum <= constants.MinEffectiveWeight)
                    continue;

                if (!cellMap.TryGetValue(kv.Key, out var cell))
                    continue;

                double confidence =
                    Math.Min(1.0,
                    (double)kv.Value.HitCount /
                    constants.TargetHitCount);

                double delta =
                    kv.Value.GetWeightedDelta(confidence);

                delta = Math.Clamp(
                    delta,
                    -constants.MaxDeltaPerCell,
                    constants.MaxDeltaPerCell);

                cell.Value =
                    (cell.Value * (1.0 + delta)).Round(0);

                result.UpdatedCells.Add(cell);
            }

            return result;
        }
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

            // Precompute Gaussian kernel
            double[,] kernel = new double[kernelSize, kernelSize];
            for (int di = -half; di <= half; di++)
            {
                for (int dj = -half; dj <= half; dj++)
                {
                    double distSq = di * di + dj * dj;
                    kernel[di + half, dj + half] =
                        Math.Exp(-distSq / (2 * sigma * sigma));
                }
            }

            var newValues = new Dictionary<(int, double), double>(cellMap.Count);

            for (int i = 0; i < rpmLabels.Count; i++)
            {
                for (int j = 0; j < injLabels.Count; j++)
                {
                    var key = (rpmLabels[i], injLabels[j]);
                    if (!cellMap.TryGetValue(key, out var cell))
                        continue;

                    double sumW = 0;
                    double sumV = 0;

                    for (int di = -half; di <= half; di++)
                    {
                        int ni = i + di;
                        if (ni < 0 || ni >= rpmLabels.Count) continue;

                        for (int dj = -half; dj <= half; dj++)
                        {
                            int nj = j + dj;
                            if (nj < 0 || nj >= injLabels.Count) continue;

                            var nKey = (rpmLabels[ni], injLabels[nj]);
                            if (!cellMap.TryGetValue(nKey, out var neighbor))
                                continue;

                            double weight = kernel[di + half, dj + half];

                            sumW += weight;
                            sumV += neighbor.Value * weight;
                        }
                    }

                    if (sumW > 0)
                        newValues[key] = sumV / sumW;
                }
            }

            foreach (var kv in newValues)
                cellMap[kv.Key].Value = kv.Value.Round(0);
        }
        private static (
    double BaseLearningRate,
    double MinEffectiveWeight,
    double MaxDeltaPerCell,
    int TargetHitCount,
    double MeanTrim,
    double StdTrim)
ComputeAdaptiveConstants(IList<DataItem> logs)
        {
            int count = logs.Count;

            double sum = 0;
            double sumSq = 0;
            double maxAbs = 0;

            for (int i = 0; i < count; i++)
            {
                double trim = GetRawTrim(logs[i]);

                sum += trim;
                sumSq += trim * trim;

                double abs = Math.Abs(trim);
                if (abs > maxAbs) maxAbs = abs;
            }

            double mean = sum / count;
            double stdDev = Math.Sqrt((sumSq / count) - (mean * mean));

            double baseRate = Math.Clamp(stdDev / 50.0, 0.1, 0.5);
            double minWeight = 1.0 / count;
            double maxDelta = Math.Clamp(maxAbs / 300.0, 0.01, 0.05);

            int targetHit = Math.Max(5,
                count /
                (Settings.RpmColumns.Length *
                 Settings.InjectionRanges.Length));

            return (baseRate, minWeight, maxDelta, targetHit, mean, stdDev);
        }

        public FuelCorrectionResult AutoCorrectFuelTable(
            IEnumerable<DataItem> validLogs,
            IEnumerable<FuelCell> fuelTable)
        {
            var logs = validLogs as IList<DataItem> ?? validLogs.ToList();
            if (logs.Count == 0)
                throw new InvalidOperationException("No valid logs provided.");
            // 1. Prepare lookup & caches
            var cellMap = fuelTable.ToDictionary(
                c => (c.RpmBin, c.InjBin));

            var rpmLabels = Settings.RpmColumns
                .Select(c => c.Label).ToArray();

            var injLabels = Settings.InjectionRanges
                .Select(r => r.Label).ToArray();
            // 2. Compute adaptive constants
            var constants =
                ComputeAdaptiveConstants(logs);
            // 3. Accumulate deltas from logs
            var accumulators =
                           AccumulateDeltas(
                               logs,
                               constants.BaseLearningRate,
                               constants.MeanTrim,
                               constants.StdTrim);            
            // 4. Iterative damped edge propagation
            PropagateEdgeDeltas(
                accumulators,
                rpmLabels,
                injLabels,
                constants.MinEffectiveWeight);
            // 5. Apply deltas to fuel table and create diagnostics
            var result = ApplyDeltasToFuelTable(
                accumulators,
                cellMap,
                constants);
            // 6. Apply dynamic 2D Gaussian smoothing
            SmoothFuelMap(
                cellMap,
                rpmLabels,
                injLabels,
                kernelSize: 5,
                sigma: 1.2);

            return result;
        }
    }
}
