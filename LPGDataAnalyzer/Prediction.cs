using LPGDataAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LPGDataAnalyzer
{
    internal class Prediction
    {
        public struct AxisSplit<T> where T : IComparable<T>
        {
            public T Low;      // Lower axis label
            public T High;     // Higher axis label
            public double WLow;  // Weight for lower label
            public double WHigh; // Weight for higher label
        }

        public static AxisSplit<int> RpmSplit(int rpm)
        {
            var cols = Settings.RpmColumns;

            // Clamp at edges
            if (rpm <= cols.First().Min)
                return new AxisSplit<int> { Low = cols.First().Label, High = cols.First().Label, WLow = 1.0, WHigh = 0.0 };
            if (rpm >= cols.Last().Max)
                return new AxisSplit<int> { Low = cols.Last().Label, High = cols.Last().Label, WLow = 1.0, WHigh = 0.0 };

            // Find enclosing interval
            for (int i = 0; i < cols.Length; i++)
            {
                var c = cols[i];
                if (rpm > c.Min && rpm <= c.Max)
                {
                    int lowLabel = c.Label;
                    int highLabel;
                    if (i + 1 < cols.Length)
                        highLabel = cols[i + 1].Label;
                    else
                        highLabel = lowLabel; // last interval

                    double wHigh = (double)(rpm - c.Min) / (c.Max - c.Min);
                    double wLow = 1.0 - wHigh;

                    return new AxisSplit<int> { Low = lowLabel, High = highLabel, WLow = wLow, WHigh = wHigh };
                }
            }

            // fallback
            return new AxisSplit<int> { Low = cols.Last().Label, High = cols.Last().Label, WLow = 1.0, WHigh = 0.0 };
        }

        public static AxisSplit<double> InjSplit(double inj)
        {
            var ranges = Settings.InjectionRanges;

            // Clamp at edges
            if (inj <= ranges.First().Min)
                return new AxisSplit<double> { Low = ranges.First().Label, High = ranges.First().Label, WLow = 1.0, WHigh = 0.0 };
            if (inj >= ranges.Last().Max)
                return new AxisSplit<double> { Low = ranges.Last().Label, High = ranges.Last().Label, WLow = 1.0, WHigh = 0.0 };

            // Find enclosing interval
            for (int i = 0; i < ranges.Length; i++)
            {
                var r = ranges[i];
                if (inj > r.Min && inj <= r.Max)
                {
                    double lowLabel = r.Label;
                    double highLabel;
                    if (i + 1 < ranges.Length)
                        highLabel = ranges[i + 1].Label;
                    else
                        highLabel = lowLabel; // last interval

                    double wHigh = (inj - r.Min) / (r.Max - r.Min);
                    double wLow = 1.0 - wHigh;

                    return new AxisSplit<double> { Low = lowLabel, High = highLabel, WLow = wLow, WHigh = wHigh };
                }
            }

            // fallback
            return new AxisSplit<double> { Low = ranges.Last().Label, High = ranges.Last().Label, WLow = 1.0, WHigh = 0.0 };
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

        public static class MathHelpers
        {
            public static double StdDev(IEnumerable<double> values)
            {
                var list = values.ToList();
                if (!list.Any()) return 0.0;

                double mean = list.Average();
                double sumSq = list.Sum(v => (v - mean) * (v - mean));
                return Math.Sqrt(sumSq / list.Count);
            }
        }

        // Generic accumulator for each fuel cell
        public class CellAccumulator
        {
            public double DeltaSum { get; set; } = 0.0;
            public double WeightSum { get; set; } = 0.0;
            public int HitCount { get; set; } = 0;

            public void Add(double delta, double weight)
            {
                DeltaSum += delta * weight;
                WeightSum += weight;
                HitCount++;
            }

            public double GetWeightedDelta(double confidence)
            {
                if (WeightSum <= 0) return 0.0;
                return (DeltaSum / WeightSum) * confidence;
            }
        }

        // Compute adaptive constants
        private (double BaseLearningRate, double MinEffectiveWeight, double MaxDeltaPerCell, int TargetHitCount) ComputeAdaptiveConstants(IEnumerable<DataItem> logs)
        {
            var trims = logs.Select(l => (l.FAST_b1 + l.SLOW_b1 + l.FAST_b2 + l.SLOW_b2) / 2.0).ToList();
            double trimStdDev = MathHelpers.StdDev(trims);
            double baseRate = Math.Clamp(trimStdDev / 50.0, 0.1, 0.5);
            double minWeight = 1.0 / trims.Count;
            double maxDelta = Math.Clamp(trims.Max(t => Math.Abs(t)) / 300.0, 0.01, 0.05);
            int targetHit = Math.Max(5, (int)Math.Round((double)trims.Count / (Settings.RpmColumns.Length * Settings.InjectionRanges.Length))
);
            return (baseRate, minWeight, maxDelta, targetHit);
        }

        // Accumulate deltas from logs
        private Dictionary<(int, double), CellAccumulator> AccumulateDeltas(IEnumerable<DataItem> logs, double baseLearningRate)
        {
            var accumulators = new Dictionary<(int, double), CellAccumulator>();

            foreach (var log in logs)
            {
                int rpmClamped = Math.Clamp(log.RPM, Settings.RpmColumns.First().Min, Settings.RpmColumns.Last().Max);
                double injClamped = Math.Clamp(log.BENZ_b1, Settings.InjectionRanges.First().Min, Settings.InjectionRanges.Last().Max);

                double avgTrim = ((log.FAST_b1 + log.SLOW_b1) + (log.FAST_b2 + log.SLOW_b2)) / 2.0;
                double correction = Math.Clamp(1.0 + avgTrim / 100.0, 0.95, 1.08);

                double regionFactor = rpmClamped < 900 ? 0.1 :
                                      injClamped < 2.0 ? 0.2 :
                                      1.0;

                double delta = (correction - 1.0) * baseLearningRate * regionFactor;
                if (Math.Abs(delta) < 1e-6) continue;

                var rpmSplit = RpmSplit(rpmClamped);
                var injSplit = InjSplit(injClamped);

                var targets = new[]
                {
                    (rpmSplit.Low,  injSplit.Low,  rpmSplit.WLow  * injSplit.WLow),
                    (rpmSplit.Low,  injSplit.High, rpmSplit.WLow  * injSplit.WHigh),
                    (rpmSplit.High, injSplit.Low,  rpmSplit.WHigh * injSplit.WLow),
                    (rpmSplit.High, injSplit.High, rpmSplit.WHigh * injSplit.WHigh)
                };

                foreach (var t in targets)
                {
                    if (t.Item3 <= 0.0) continue;

                    if (!accumulators.TryGetValue((t.Item1, t.Item2), out var acc))
                    {
                        acc = new CellAccumulator();
                        accumulators[(t.Item1, t.Item2)] = acc;
                    }
                    acc.Add(delta, t.Item3);
                }
            }

            return accumulators;
        }

        // Iterative edge propagation
        private void PropagateEdgeDeltas(Dictionary<(int, double), CellAccumulator> accumulators, List<int> rpmLabels, List<double> injLabels, double minEffectiveWeight)
        {
            const double PropagationDamping = 0.5;
            const int MaxPropagationIterations = 20;

            for (int iteration = 0; iteration < MaxPropagationIterations; iteration++)
            {
                bool anyUpdate = false;
                for (int i = 0; i < rpmLabels.Count; i++)
                {
                    for (int j = 0; j < injLabels.Count; j++)
                    {
                        var key = (rpmLabels[i], injLabels[j]);
                        if (accumulators.TryGetValue(key, out var acc) && acc.WeightSum > minEffectiveWeight)
                            continue;

                        var neighbors = new List<(int, double)>
                {
                    (rpmLabels[Math.Max(0, i-1)], injLabels[j]),
                    (rpmLabels[Math.Min(rpmLabels.Count-1, i+1)], injLabels[j]),
                    (rpmLabels[i], injLabels[Math.Max(0, j-1)]),
                    (rpmLabels[i], injLabels[Math.Min(injLabels.Count-1, j+1)])
                };

                        foreach (var n in neighbors)
                        {
                            if (!accumulators.TryGetValue(n, out var nAcc) || nAcc.WeightSum < minEffectiveWeight) continue;

                            if (!accumulators.TryGetValue(key, out acc))
                            {
                                acc = new CellAccumulator();
                                accumulators[key] = acc;
                            }

                            acc.DeltaSum += nAcc.DeltaSum * PropagationDamping;
                            acc.WeightSum += nAcc.WeightSum * PropagationDamping;
                            acc.HitCount += nAcc.HitCount / 2;
                            anyUpdate = true;
                        }
                    }
                }
                if (!anyUpdate) break;
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

        // Dynamic Gaussian 2D smoothing
        private void SmoothFuelMap(Dictionary<(int, double), FuelCell> cellMap, List<int> rpmLabels, List<double> injLabels, int kernelSize = 5, double sigma = 1.2)
        {
            if (kernelSize % 2 == 0)
                throw new ArgumentException("kernelSize must be odd.");

            int rpmCount = rpmLabels.Count;
            int injCount = injLabels.Count;
            int half = kernelSize / 2;

            var smoothedValues = new Dictionary<(int, double), double>();

            for (int i = 0; i < rpmCount; i++)
            {
                for (int j = 0; j < injCount; j++)
                {
                    var key = (rpmLabels[i], injLabels[j]);
                    if (!cellMap.TryGetValue(key, out var cell)) continue;

                    double sumWeighted = 0.0;
                    double sumWeights = 0.0;

                    for (int di = -half; di <= half; di++)
                    {
                        int ni = i + di;
                        if (ni < 0 || ni >= rpmCount) continue;

                        for (int dj = -half; dj <= half; dj++)
                        {
                            int nj = j + dj;
                            if (nj < 0 || nj >= injCount) continue;

                            var nKey = (rpmLabels[ni], injLabels[nj]);
                            if (!cellMap.TryGetValue(nKey, out var neighbor)) continue;

                            double distance = Math.Sqrt(di * di + dj * dj);
                            double weight = Math.Exp(-(distance * distance) / (2 * sigma * sigma));

                            sumWeighted += neighbor.Value * weight;
                            sumWeights += weight;
                        }
                    }

                    if (sumWeights > 0)
                        smoothedValues[key] = sumWeighted / sumWeights;
                }
            }

            foreach (var kv in smoothedValues)
                cellMap[kv.Key].Value = kv.Value.Round(0);
        }
        public FuelCorrectionResult AutoCorrectFuelTable(IEnumerable<DataItem> validLogs, List<FuelCell> fuelTable)
        {
            if (!validLogs.Any())
                throw new InvalidOperationException("No valid logs provided.");

            // 1. Prepare lookup & caches
            var cellMap = fuelTable.ToDictionary(c => (c.RpmBin, c.InjBin), c => c);
            var rpmLabels = Settings.RpmColumns.Select(c => c.Label).ToList();
            var injLabels = Settings.InjectionRanges.Select(r => r.Label).ToList();

            var rpmIndexMap = rpmLabels.Select((v, i) => (v, i)).ToDictionary(x => x.v, x => x.i);
            var injIndexMap = injLabels.Select((v, i) => (v, i)).ToDictionary(x => x.v, x => x.i);

            // 2. Compute adaptive constants
            var constants = ComputeAdaptiveConstants(validLogs);

            // 3. Accumulate deltas from logs
            var accumulators = AccumulateDeltas(validLogs, constants.BaseLearningRate);

            // 4. Iterative damped edge propagation
            PropagateEdgeDeltas(accumulators, rpmLabels, injLabels, constants.MinEffectiveWeight);

            // 5. Apply deltas to fuel table and create diagnostics
            var result = ApplyDeltasToFuelTable(accumulators, cellMap, constants);

            // 6. Apply dynamic 2D Gaussian smoothing
            SmoothFuelMap(cellMap, rpmLabels, injLabels, kernelSize: 5, sigma: 1.2);

            return result;
        }
    }
}
