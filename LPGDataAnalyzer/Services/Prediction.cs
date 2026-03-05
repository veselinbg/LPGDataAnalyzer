using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Models.Common;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LPGDataAnalyzer.Services
{
    internal sealed partial class Prediction
    { // ===================== TUNING =====================
        // Controls how aggressively the algorithm switches from slow → fast trims during transients
        // 0 = always slow (very stable), 1 = normal, >1 = more aggressive toward fast
        private const double TransientWeight = 1.0;
        private const int KernelSize = 5;
        private const double KernelSigma = 1.2;
        private const double MapSlopeMin = 0.1;
        private const double MapSlopeMax = 1.0;
        private const double Damping = 0.5;
        private const int MaxIterations = 20;

        public const double RegionMinFactor = 0.3;
        public const double RegionScale = 0.7;

        //end
        public const double SmallDeltaThreshold = 1e-9;

       
        // ===================== AXIS SPLITS =====================
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AxisSplit<TLabel> Split<T, TLabel>(T value, ReadOnlySpan<(T Min, T Max, TLabel Label)> ranges) where T : INumber<T>
        {
            int last = ranges.Length - 1;

            ref readonly var first = ref ranges[0];
            ref readonly var lastRange = ref ranges[last];

            if (value <= first.Min)
                return new(first.Label, first.Label, 1d, 0d);

            if (value >= lastRange.Max)
                return new(lastRange.Label, lastRange.Label, 1d, 0d);

            // Linear scan (fast for small N like 12 bins)
            for (int i = 0; i < last; i++)
            {
                ref readonly var r = ref ranges[i];

                if (value <= r.Max)
                {
                    ref readonly var next = ref ranges[i + 1];

                    T local = (value - r.Min) / (r.Max - r.Min);

                    double w = double.CreateChecked(local);

                    return new(
                        r.Label,
                        next.Label,
                        1d - w,
                        w);
                }
            }

            return new(lastRange.Label, lastRange.Label, 1d, 0d);
        }
        // ===================== CUSTOM KEY =====================

        public readonly struct CellKey(int rpm, double inj) : IEquatable<CellKey>
        {
            public readonly int Rpm = rpm;
            public readonly double Inj = inj;

            public bool Equals(CellKey other)
                => Rpm == other.Rpm && Inj.Equals(other.Inj);

            public override bool Equals(object? obj)
                => obj is CellKey other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(Rpm, Inj);
        }

            // ===================== SMOOTHSTEP =====================
        private static double SmoothStep(double edge0, double edge1, double x)
        {
            if (x <= edge0) return 0d;
            if (x >= edge1) return 1d;
            double t = (x - edge0) / (edge1 - edge0);
            t = Math.Clamp(t, 0d, 1d);
            return t * t * (3 - 2 * t);
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
                if (WeightSum <= 0) return 0;
                return (DeltaSum / WeightSum) * confidence;
            }
        }
        private static void Add(
            Dictionary<CellKey, CellAccumulator> dict,
            int rpm,
            double inj,
            double delta,
            double weight)
        {
            if (weight <= 0) return;

            var key = new CellKey(rpm, inj);
            ref var acc = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out _);
            acc ??= new CellAccumulator();
            acc.Add(delta, weight);
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
                deltaRPM = Math.Abs(log.RPM - prevLog.RPM) / 1000.0; // normalize
            }

            double transientMetric = Math.Sqrt(deltaMAP * deltaMAP + deltaRPM * deltaRPM);

            double blendFactor = SmoothStep(MapSlopeMin, MapSlopeMax, transientMetric);

            // Apply transient tuning factor
            blendFactor *= TransientWeight;
            blendFactor = Math.Clamp(blendFactor, 0, 1);

            return (1 - blendFactor) * slow + blendFactor * fast;
        }
        // ===================== ACCUMULATION =====================
        private static Dictionary<CellKey, CellAccumulator> AccumulateDeltas(IEnumerable<DataItem> logs, double baseRate, double mean, double stdDev)
        {
            var accumulators = new Dictionary<CellKey, CellAccumulator>(256);

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

                var trim = ComputeTransientTrim(log, prevLog);

                double correction = 1.0 + trim / 100.0;

                double rpmFactor = SmoothStep(rpmFadeStart, rpmFadeEnd, rpm);
                double injFactor = SmoothStep(injFadeStart, injFadeEnd, inj);

                double regionFactor = RegionMinFactor + RegionScale * Math.Sqrt(rpmFactor * injFactor);

                double delta = (correction - 1.0) * baseRate * regionFactor;

                if (Math.Abs(delta) < SmallDeltaThreshold) //0.000000001
                {
                    prevLog = log;
                    continue;
                }

                var r = Split(rpm, Settings.RpmColumns);
                var i = Split(inj, Settings.InjectionRanges);

                AddSplit(accumulators, r, i, delta);

                prevLog = log;
            }

            return accumulators;
        }
        private static void AddSplit(Dictionary<CellKey, CellAccumulator> accumulators, AxisSplit<int> r, AxisSplit<double> i, double delta)
        {
            Add(accumulators, r.Low, i.Low, delta, r.WLow * i.WLow);
            Add(accumulators, r.Low, i.High, delta, r.WLow * i.WHigh);
            Add(accumulators, r.High, i.Low, delta, r.WHigh * i.WLow);
            Add(accumulators, r.High, i.High, delta, r.WHigh * i.WHigh);
        }
        // ===================== EDGE PROPAGATION =====================
        private static IEnumerable<(int ni, int nj)> GetValidNeighbors(int i, int j, int maxI, int maxJ)
        {
            var offsets = new (int di, int dj)[] { (-1, 0), (1, 0), (0, -1), (0, 1) };
            foreach (var (di, dj) in offsets)
            {
                int ni = i + di;
                int nj = j + dj;
                if (ni >= 0 && ni < maxI && nj >= 0 && nj < maxJ)
                    yield return (ni, nj);
            }
        }
        private static void PropagateEdgeDeltas(Dictionary<CellKey, CellAccumulator> accumulators, IList<int> rpmLabels, IList<double> injLabels, double minWeight)
        {
            for (int iter = 0; iter < MaxIterations; iter++)
            {
                bool any = false;

                for (int i = 0; i < rpmLabels.Count; i++)
                {
                    for (int j = 0; j < injLabels.Count; j++)
                    {
                        var key = new CellKey(rpmLabels[i], injLabels[j]);

                        if (accumulators.TryGetValue(key, out var cellAcc) && cellAcc.WeightSum > minWeight)
                        {
                            continue;
                        }
                        foreach (var (ni, nj) in GetValidNeighbors(i, j, rpmLabels.Count, injLabels.Count))
                        {
                            var nKey = new CellKey(rpmLabels[ni], injLabels[nj]);

                            if (!accumulators.TryGetValue(nKey, out var nAcc) || nAcc.WeightSum < minWeight)
                            {
                                continue;
                            }

                            cellAcc ??= accumulators[key] = new CellAccumulator();

                            double propagatedWeight = nAcc.WeightSum * Damping;
                            double neighborMean = nAcc.DeltaSum / nAcc.WeightSum;

                            cellAcc.DeltaSum += neighborMean * propagatedWeight;
                            cellAcc.WeightSum += propagatedWeight;

                            any = true;
                        }
                    }
                }

                if (!any) break;
            }
        }

        // ===================== APPLY DELTAS =====================
        private static void ApplyDeltas(Dictionary<CellKey, CellAccumulator> acc, Dictionary<CellKey, FuelCell> cellMap, (double BaseLearningRate, double MinEffectiveWeight, double MaxDeltaPerCell, int TargetHitCount, double MeanTrim, double StdTrim) c)
        {
            foreach (var kv in acc)
            {
                if (kv.Value.WeightSum <= c.MinEffectiveWeight) continue;
                if (!cellMap.TryGetValue(kv.Key, out var cell)) continue;

                double confidence = Math.Min(1.0, (double)kv.Value.HitCount / c.TargetHitCount);
                double delta = kv.Value.GetDelta(confidence);
                delta = Math.Clamp(delta, -c.MaxDeltaPerCell, c.MaxDeltaPerCell);

                double k = 1;

                //if (cell.InjBin < 3.5 && cell.RpmBin < 1500)
                //    k = 0.95;  // protect economy area
                //else if (cell.InjBin > 8 && cell.RpmBin > 1500)
                //    k = 1.02;

                cell.Value *= (k + delta);
            }
        }

        // ===================== SMOOTH =====================
        private static void SmoothFuelMap(Dictionary<CellKey, FuelCell> cellMap, IList<int> rpmLabels, IList<double> injLabels, int kernelSize, double sigma)
        {
            if (kernelSize % 2 == 0) throw new ArgumentException("kernelSize must be odd.");

            int half = kernelSize / 2;
            double[,] kernel = new double[kernelSize, kernelSize];

            for (int di = -half; di <= half; di++)
                for (int dj = -half; dj <= half; dj++)
                    kernel[di + half, dj + half] = Math.Exp(-(di * di + dj * dj) / (2 * sigma * sigma));

            var newValues = new Dictionary<CellKey, double>(cellMap.Count);

            for (int i = 0; i < rpmLabels.Count; i++)
            {
                for (int j = 0; j < injLabels.Count; j++)
                {
                    var key = new CellKey(rpmLabels[i], injLabels[j]);
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

                            if (!cellMap.TryGetValue(new CellKey(rpmLabels[ni], injLabels[nj]), out var n)) continue;

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

        // ===================== ROUND =====================
        private static void RoundFuelMap(Dictionary<CellKey, FuelCell> cellMap)
        {
            foreach (var cell in cellMap.Values)
                cell.Value = cell.Value.Round(0);
        }

        // ===================== ADAPTIVE CONSTANTS =====================
        private static (double BaseLearningRate, double MinEffectiveWeight, double MaxDeltaPerCell, int TargetHitCount, double MeanTrim, double StdTrim)
            ComputeAdaptiveConstants(IEnumerable<DataItem> logs)
        {
            double sum = 0, sumSq = 0, maxAbs = 0;

            var lCount = logs.Count();

            DataItem? prevLog = null;

            foreach (var log in logs)
            {
                double trim = ComputeTransientTrim(log, prevLog);
                sum += trim;
                sumSq += trim * trim;
                maxAbs = Math.Max(maxAbs, Math.Abs(trim));
                prevLog = log;
            }

            double mean = sum / lCount;
            double std = Math.Sqrt((sumSq / lCount) - (mean * mean));

            double baseRate = Math.Clamp(std / 50.0, 0.1, 0.5);
            double minWeight = 1.0 / lCount;
            double maxDelta = Math.Clamp(maxAbs / 300.0, 0.01, 0.05);

            int targetHit = Math.Max(5, lCount / (Settings.RpmColumns.Length * Settings.InjectionRanges.Length));

            return (baseRate, minWeight, maxDelta, targetHit, mean, std);
        }

        // ===================== PUBLIC ENTRY =====================
        public List<FuelCell> AutoCorrectFuelTable(IEnumerable<DataItem> logs, IEnumerable<FuelCell> fuelTable)
        {
            var cellMap = fuelTable.ToDictionary(c => new CellKey(c.RpmBin, c.InjBin));

            var rpmLabels = Settings.RpmColumns.Select(c => c.Label).ToArray();
            var injLabels = Settings.InjectionRanges.Select(r => r.Label).ToArray();

            var constants = ComputeAdaptiveConstants(logs);

            var accumulators = AccumulateDeltas(logs, constants.BaseLearningRate, constants.MeanTrim, constants.StdTrim);

            PropagateEdgeDeltas(accumulators, rpmLabels, injLabels, constants.MinEffectiveWeight);

            ApplyDeltas(accumulators, cellMap, constants);

            SmoothFuelMap(cellMap, rpmLabels, injLabels, KernelSize, KernelSigma);

            RoundFuelMap(cellMap);

            return [.. cellMap.Values];
        }
    }
}