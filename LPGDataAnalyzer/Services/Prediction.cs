using LPGDataAnalyzer.Models;
using System.Runtime.InteropServices;

namespace LPGDataAnalyzer.Services
{
    internal sealed partial class Prediction
    {
        // ===================== TUNING =====================
        // 0 = always slow (very stable), 1 = normal, >1 = more aggressive toward fast
        private const double TransientWeight = 1.0;

        // ===================== AXIS SPLITS =====================
        public static AxisSplit<int> RpmSplit(int rpm)
        {
            var cols = Settings.RpmColumns;
            int last = cols.Length - 1;

            if (rpm <= cols[0].Min) return new(cols[0].Label, cols[0].Label, 1, 0);
            if (rpm >= cols[last].Max) return new(cols[last].Label, cols[last].Label, 1, 0);

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

            return new(cols[last].Label, cols[last].Label, 1, 0);
        }

        public static AxisSplit<double> InjSplit(double inj)
        {
            var ranges = Settings.InjectionRanges;
            int last = ranges.Length - 1;

            if (inj <= ranges[0].Min) return new(ranges[0].Label, ranges[0].Label, 1, 0);
            if (inj >= ranges[last].Max) return new(ranges[last].Label, ranges[last].Label, 1, 0);

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

        // ===================== SMOOTHSTEP =====================
        private static double SmoothStep(double edge0, double edge1, double x)
        {
            if (x <= edge0) return 0;
            if (x >= edge1) return 1;
            double t = (x - edge0) / (edge1 - edge0);
            return t * t * (3 - 2 * t);
        }

        // ===================== ACCUMULATOR =====================
        private sealed class CellAccumulator
        {
            public double DeltaSum;
            public double WeightSum;
            public double HitScore; // Weighted hit-count

            public void Add(double delta, double weight, double trimConfidence = 1.0)
            {
                DeltaSum += delta * weight;
                WeightSum += weight;
                HitScore += trimConfidence * weight; // only high-confidence trims increase HitScore
            }

            public double GetDelta(double targetHitCount)
            {
                if (WeightSum <= 0) return 0;
                double confidence = 1 - Math.Exp(-HitScore / targetHitCount); // exponential confidence
                return (DeltaSum / WeightSum) * confidence;
            }
        }

        private static void Add(
            Dictionary<(int, double), CellAccumulator> dict,
            int rpm,
            double inj,
            double delta,
            double weight,
            double trimConfidence = 1.0)
        {
            if (weight <= 0) return;

            var key = (rpm, inj);
            ref var acc = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out _);
            acc ??= new CellAccumulator();
            acc.Add(delta, weight, trimConfidence);
        }

        // ===================== TRIM / TRANSIENT LOGIC =====================
        private static double ComputeTransientTrim(DataItem log, DataItem? prevLog)
        {
            double slow = (log.SLOW_b1 + log.SLOW_b2) * 0.5;
            double fast = (log.FAST_b1 + log.FAST_b2) * 0.5;

            double deltaMAP = 0;
            double deltaRPM = 0;
            if (prevLog != null)
            {
                deltaMAP = Math.Abs(log.MAP - prevLog.MAP);
                deltaRPM = Math.Abs(log.RPM - prevLog.RPM) / 1000.0;
            }

            double transientMetric = Math.Sqrt(deltaMAP * deltaMAP + deltaRPM * deltaRPM);

            const double MAP_slope_min = 0.1;
            const double MAP_slope_max = 1.0;

            double t = SmoothStep(MAP_slope_min, MAP_slope_max, transientMetric);
            t *= TransientWeight;
            t = Math.Clamp(t, 0, 1);

            return (1 - t) * slow + t * fast;
        }

        private static bool TryGetValidTrim(
            DataItem log,
            DataItem? prevLog,
            double mean,
            double stdDev,
            out double trim,
            out double transientMetric)
        {
            trim = ComputeTransientTrim(log, prevLog);

            // recompute transientMetric for hit-score weighting
            transientMetric = prevLog != null
                ? Math.Sqrt(Math.Pow(log.MAP - prevLog.MAP, 2) + Math.Pow((log.RPM - prevLog.RPM) / 1000.0, 2))
                : 0;

            if (Math.Abs(trim) > 25) return false;
            if (stdDev > 0 && Math.Abs((trim - mean) / stdDev) > 2.5) return false;

            return true;
        }

        // ===================== REGION FACTORS =====================
        private static double ComputeRegionFactor(int rpm, double inj)
        {
            // Smooth 2D Gaussian weighting
            int rpmCenter = 3000;   // mid-RPM
            double injCenter = 5.0; // mid-load
            double sigmaRpm = 1000;
            double sigmaInj = 2.0;
            double minFactor = 0.3;
            double maxFactor = 1.0;

            double rpmWeight = Math.Exp(-Math.Pow(rpm - rpmCenter, 2) / (2 * sigmaRpm * sigmaRpm));
            double injWeight = Math.Exp(-Math.Pow(inj - injCenter, 2) / (2 * sigmaInj * sigmaInj));

            double weight = rpmWeight * injWeight;
            return minFactor + (maxFactor - minFactor) * weight;
        }

        // ===================== K-FACTOR =====================
        private static double ComputeKFactor(FuelCell cell)
        {
            // Economy
            double economy = 1.0;
            if (cell.RpmBin < 1500 && cell.InjBin < 3.5)
            {
                double rpmW = 1.0 - cell.RpmBin / 1500.0;
                double injW = 1.0 - cell.InjBin / 3.5;
                economy = 0.95 + 0.05 * (1 - rpmW * injW);
            }

            // High load
            double highLoad = 1.0;
            if (cell.RpmBin > 1500 && cell.InjBin > 8)
            {
                double rpmW = Math.Min((cell.RpmBin - 1500) / 2000.0, 1.0);
                double injW = Math.Min((cell.InjBin - 8) / 4.0, 1.0);
                highLoad = 1.0 + 0.02 * rpmW * injW;
            }

            return economy * highLoad;
        }

        // ===================== ACCUMULATION =====================
        private static Dictionary<(int, double), CellAccumulator> AccumulateDeltas(
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

            DataItem? prevLog = null;

            foreach (var log in logs)
            {
                int rpm = Math.Clamp(log.RPM, rpmMin, rpmMax);
                double inj = Math.Clamp((log.BENZ_b1 + log.BENZ_b2) * 0.5, injMin, injMax);

                if (!TryGetValidTrim(log, prevLog, mean, stdDev, out double trim, out double transientMetric))
                {
                    prevLog = log;
                    continue;
                }

                double correction = 1.0 + trim / 100.0;
                if (correction < 0.95 || correction > 1.08)
                {
                    prevLog = log;
                    continue;
                }

                double regionFactor = ComputeRegionFactor(rpm, inj);
                double delta = (correction - 1.0) * baseRate * regionFactor;
                if (Math.Abs(delta) < 1e-9)
                {
                    prevLog = log;
                    continue;
                }

                var r = RpmSplit(rpm);
                var i = InjSplit(inj);

                // High confidence if transient low
                double trimConfidence = 1.0 - Math.Clamp(transientMetric, 0, 1.0);

                Add(acc, r.Low, i.Low, delta, r.WLow * i.WLow, trimConfidence);
                Add(acc, r.Low, i.High, delta, r.WLow * i.WHigh, trimConfidence);
                Add(acc, r.High, i.Low, delta, r.WHigh * i.WLow, trimConfidence);
                Add(acc, r.High, i.High, delta, r.WHigh * i.WHigh, trimConfidence);

                prevLog = log;
            }

            return acc;
        }

        // ===================== EDGE PROPAGATION WITH DECAY =====================
        private static readonly (int di, int dj)[] Neighbors = { (-1, 0), (1, 0), (0, -1), (0, 1) };

        private static void PropagateEdgeDeltasSmarter(
            Dictionary<(int, double), CellAccumulator> acc,
            IList<int> rpmLabels,
            IList<double> injLabels,
            double minWeight,
            double decay = 0.5)
        {
            var queue = new Queue<((int rpm, double inj) key, int distance)>();
            foreach (var kv in acc)
            {
                if (kv.Value.WeightSum >= minWeight)
                    queue.Enqueue((kv.Key, 0));
            }

            while (queue.Count > 0)
            {
                var (currentKey, dist) = queue.Dequeue();
                if (!acc.TryGetValue(currentKey, out var cur)) continue;

                int i = rpmLabels.IndexOf(currentKey.rpm);
                int j = injLabels.IndexOf(currentKey.inj);

                foreach (var (di, dj) in Neighbors)
                {
                    int ni = i + di;
                    int nj = j + dj;
                    if (ni < 0 || ni >= rpmLabels.Count || nj < 0 || nj >= injLabels.Count) continue;

                    var neighborKey = (rpmLabels[ni], injLabels[nj]);
                    ref var neighbor = ref CollectionsMarshal.GetValueRefOrAddDefault(acc, neighborKey, out _);
                    neighbor ??= new CellAccumulator();

                    double weightFactor = decay / (1 + dist);
                    neighbor.DeltaSum += cur.DeltaSum * weightFactor;
                    neighbor.WeightSum += cur.WeightSum * weightFactor;
                    neighbor.HitScore += cur.HitScore;

                    if (neighbor.WeightSum < minWeight)
                        queue.Enqueue((neighborKey, dist + 1));
                }
            }
        }

        // ===================== APPLY DELTAS =====================
        private static void ApplyDeltas(
            Dictionary<(int, double), CellAccumulator> acc,
            Dictionary<(int, double), FuelCell> cellMap,
            (double BaseLearningRate, double MinEffectiveWeight, double MaxDeltaPerCell, int TargetHitCount, double MeanTrim, double StdTrim) c)
        {
            foreach (var kv in acc)
            {
                if (kv.Value.WeightSum <= c.MinEffectiveWeight) continue;
                if (!cellMap.TryGetValue(kv.Key, out var cell)) continue;

                double delta = kv.Value.GetDelta(c.TargetHitCount);
                delta = Math.Clamp(delta, -c.MaxDeltaPerCell, c.MaxDeltaPerCell);

                double k = ComputeKFactor(cell);

                cell.Value *= (k + delta);
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
            if (kernelSize % 2 == 0) throw new ArgumentException("kernelSize must be odd.");

            int half = kernelSize / 2;
            double[,] kernel = new double[kernelSize, kernelSize];

            for (int di = -half; di <= half; di++)
                for (int dj = -half; dj <= half; dj++)
                    kernel[di + half, dj + half] = Math.Exp(-(di * di + dj * dj) / (2 * sigma * sigma));

            var newValues = new Dictionary<(int, double), double>(cellMap.Count);

            for (int i = 0; i < rpmLabels.Count; i++)
            {
                for (int j = 0; j < injLabels.Count; j++)
                {
                    var key = (rpmLabels[i], injLabels[j]);
                    if (!cellMap.TryGetValue(key, out var cell)) continue;

                    double sumW = 0, sumV = 0;

                    for (int di = -half; di <= half; di++)
                    {
                        int ni = i + di;
                        if (ni < 0 || ni >= rpmLabels.Count) continue;

                        for (int dj = -half; dj <= half; dj++)
                        {
                            int nj = j + dj;
                            if (nj < 0 || nj >= injLabels.Count) continue;

                            if (!cellMap.TryGetValue((rpmLabels[ni], injLabels[nj]), out var n)) continue;

                            double w = kernel[di + half, dj + half];
                            sumW += w;
                            sumV += n.Value * w;
                        }
                    }

                    if (sumW > 0) newValues[key] = sumV / sumW;
                }
            }

            foreach (var kv in newValues)
                cellMap[kv.Key].Value = kv.Value;
        }

        // ===================== ROUND =====================
        private static void RoundFuelMap(Dictionary<(int, double), FuelCell> cellMap)
        {
            foreach (var cell in cellMap.Values)
                cell.Value = cell.Value.Round(0);
        }

        // ===================== ADAPTIVE CONSTANTS =====================
        private static (double BaseLearningRate, double MinEffectiveWeight, double MaxDeltaPerCell, int TargetHitCount, double MeanTrim, double StdTrim)
            ComputeAdaptiveConstants(IList<DataItem> logs)
        {
            double sum = 0, sumSq = 0, maxAbs = 0;
            DataItem? prevLog = null;

            foreach (var log in logs)
            {
                double trim = ComputeTransientTrim(log, prevLog);
                sum += trim;
                sumSq += trim * trim;
                maxAbs = Math.Max(maxAbs, Math.Abs(trim));
                prevLog = log;
            }

            double mean = sum / logs.Count;
            double std = Math.Sqrt((sumSq / logs.Count) - (mean * mean));

            double baseRate = Math.Clamp(std / 50.0, 0.1, 0.5);
            double minWeight = 1.0 / logs.Count;
            double maxDelta = Math.Clamp(maxAbs / 300.0, 0.01, 0.05);

            int targetHit = Math.Max(5, logs.Count / (Settings.RpmColumns.Length * Settings.InjectionRanges.Length));

            return (baseRate, minWeight, maxDelta, targetHit, mean, std);
        }

        // ===================== PUBLIC ENTRY =====================
        public FuelCorrectionResult AutoCorrectFuelTable(IEnumerable<DataItem> validLogs, IEnumerable<FuelCell> fuelTable)
        {
            var logs = validLogs as IList<DataItem> ?? validLogs.ToList();
            if (logs.Count == 0) throw new InvalidOperationException("No valid logs provided.");

            var cellMap = fuelTable.ToDictionary(c => (c.RpmBin, c.InjBin));

            var rpmLabels = Settings.RpmColumns.Select(c => c.Label).ToArray();
            var injLabels = Settings.InjectionRanges.Select(r => r.Label).ToArray();

            var constants = ComputeAdaptiveConstants(logs);

            var acc = AccumulateDeltas(logs, constants.BaseLearningRate, constants.MeanTrim, constants.StdTrim);

            PropagateEdgeDeltasSmarter(acc, rpmLabels, injLabels, constants.MinEffectiveWeight, decay: 0.5);

            ApplyDeltas(acc, cellMap, constants);

            SmoothFuelMap(cellMap, rpmLabels, injLabels, kernelSize: 5, sigma: 1.2);

            RoundFuelMap(cellMap);

            return new FuelCorrectionResult { UpdatedCells = cellMap.Values.ToList() };
        }
    }
}