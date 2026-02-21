using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer
{
    internal sealed class Prediction
    {
        public readonly struct AxisSplit<T> where T : IComparable<T>
        {
            public readonly T Low;
            public readonly T High;
            public readonly double WLow;
            public readonly double WHigh;

            public AxisSplit(T low, T high, double wLow, double wHigh)
            {
                Low = low;
                High = high;
                WLow = wLow;
                WHigh = wHigh;
            }
        }

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

        public class FuelCellUpdate
        {
            public int Rpm { get; set; }
            public double Inj { get; set; }
            public double AppliedDelta { get; set; }
            public double Confidence { get; set; }
            public bool Propagated { get; set; } = false;
        }

        public class FuelCorrectionResult
        {
            public List<FuelCell> UpdatedCells { get; set; } = [];
            public List<FuelCellUpdate> Diagnostics { get; set; } = [];
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

        private (double BaseLearningRate, double MinEffectiveWeight,
                 double MaxDeltaPerCell, int TargetHitCount)
            ComputeAdaptiveConstants(IList<DataItem> logs)
        {
            int count = logs.Count;

            double sum = 0;
            double sumSq = 0;
            double maxAbs = 0;

            for (int i = 0; i < count; i++)
            {
                var l = logs[i];
                double trim = (l.FAST_b1 + l.SLOW_b1 + l.FAST_b2 + l.SLOW_b2) / 2.0;

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
                (int)Math.Round((double)count /
                (Settings.RpmColumns.Length *
                 Settings.InjectionRanges.Length)));

            return (baseRate, minWeight, maxDelta, targetHit);
        }

        private Dictionary<(int, double), CellAccumulator>
            AccumulateDeltas(IList<DataItem> logs, double baseRate)
        {
            var accumulators = new Dictionary<(int, double), CellAccumulator>(256);

            var rpmCols = Settings.RpmColumns;
            var injRanges = Settings.InjectionRanges;

            int rpmMin = rpmCols[0].Min;
            int rpmMax = rpmCols[^1].Max;
            double injMin = injRanges[0].Min;
            double injMax = injRanges[^1].Max;

            for (int i = 0; i < logs.Count; i++)
            {
                var log = logs[i];

                int rpm = Math.Clamp(log.RPM, rpmMin, rpmMax);
                double inj = Math.Clamp(log.BENZ_b1, injMin, injMax);

                double avgTrim =
                    ((log.FAST_b1 + log.SLOW_b1) +
                     (log.FAST_b2 + log.SLOW_b2)) / 2.0;

                double correction = Math.Clamp(1.0 + avgTrim / 100.0, 0.95, 1.08);

                double regionFactor =
                    rpm < 900 ? 0.1 :
                    inj < 2.0 ? 0.2 :
                    1.0;

                double delta = (correction - 1.0) * baseRate * regionFactor;
                if (Math.Abs(delta) < 1e-7)
                    continue;

                var rpmSplit = RpmSplit(rpm);
                var injSplit = InjSplit(inj);

                Add(accumulators, rpmSplit.Low, injSplit.Low, delta, rpmSplit.WLow * injSplit.WLow);
                Add(accumulators, rpmSplit.Low, injSplit.High, delta, rpmSplit.WLow * injSplit.WHigh);
                Add(accumulators, rpmSplit.High, injSplit.Low, delta, rpmSplit.WHigh * injSplit.WLow);
                Add(accumulators, rpmSplit.High, injSplit.High, delta, rpmSplit.WHigh * injSplit.WHigh);
            }

            return accumulators;
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

        private void PropagateEdgeDeltas(
            Dictionary<(int, double), CellAccumulator> acc,
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

                        if (acc.TryGetValue(key, out var current) &&
                            current.WeightSum > minWeight)
                            continue;

                        void TryProp(int ni, int nj)
                        {
                            var nKey = (rpmLabels[ni], injLabels[nj]);
                            if (!acc.TryGetValue(nKey, out var nAcc) ||
                                nAcc.WeightSum < minWeight)
                                return;

                            if (!acc.TryGetValue(key, out current))
                            {
                                current = new CellAccumulator();
                                acc[key] = current;
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
        private FuelCorrectionResult ApplyDeltasToFuelTable(Dictionary<(int, double), CellAccumulator> accumulators,
            Dictionary<(int, double), FuelCell> cellMap,
            (double BaseLearningRate, double MinEffectiveWeight, double MaxDeltaPerCell, int TargetHitCount) constants)
        {
            var result = new FuelCorrectionResult();

            foreach (var kv in accumulators)
            {
                var key = kv.Key;
                var acc = kv.Value;
                if (acc.WeightSum <= constants.MinEffectiveWeight) continue;

                if (!cellMap.TryGetValue(key, out var cell))
                    throw new InvalidOperationException($"Fuel cell not found for RPM={key.Item1}, Inj={key.Item2}");

                double confidence = Math.Min(1.0, (double)acc.HitCount / constants.TargetHitCount);
                double avgDelta = acc.GetWeightedDelta(confidence);
                avgDelta = Math.Clamp(avgDelta, -constants.MaxDeltaPerCell, constants.MaxDeltaPerCell);

                cell.Value = (cell.Value * (1.0 + avgDelta)).Round(0);
                result.UpdatedCells.Add(cell);

                result.Diagnostics.Add(new FuelCellUpdate
                {
                    Rpm = key.Item1,
                    Inj = key.Item2,
                    AppliedDelta = avgDelta,
                    Confidence = confidence,
                    Propagated = acc.HitCount == 0
                });
            }

            return result;
        }

        private void SmoothFuelMap(
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

        public FuelCorrectionResult AutoCorrectFuelTable(
            IEnumerable<DataItem> validLogs,
            List<FuelCell> fuelTable)
        {
            var logs = validLogs as IList<DataItem> ?? validLogs.ToList();
            if (logs.Count == 0)
                throw new InvalidOperationException("No valid logs provided.");
            // 1. Prepare lookup & caches
            var cellMap = fuelTable.ToDictionary(
                c => (c.RpmBin, c.InjBin));

            var rpmLabels = Settings.RpmColumns
                .Select(c => c.Label).ToList();

            var injLabels = Settings.InjectionRanges
                .Select(r => r.Label).ToList();
            // 2. Compute adaptive constants
            var constants = ComputeAdaptiveConstants(logs);
            // 3. Accumulate deltas from logs
            var accumulators =
                AccumulateDeltas(logs, constants.BaseLearningRate);
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
