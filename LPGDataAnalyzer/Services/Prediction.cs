using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Models.Common;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;

namespace LPGDataAnalyzer.Services
{
    public static class FuelMapSmoother
    {
        /// <summary>
        /// Smooths a 2D fuel map using a Gaussian kernel.
        /// Optimized for performance: precomputed kernel, optional parallelization, in-place smoothing.
        /// </summary>
        /// <param name="cellMap">Fuel map to smooth (double[,])</param>
        /// <param name="kernelSize">Odd size of Gaussian kernel (e.g., 3, 5, 7)</param>
        /// <param name="sigma">Standard deviation of Gaussian</param>
        /// <param name="useParallel">Whether to use multi-threading for large maps</param>
        /// <param name="inPlace">If true, modifies cellMap directly; otherwise uses temporary buffer</param>
        public static void Smooth(double[,] cellMap, int kernelSize, double sigma, bool useParallel = true, bool inPlace = false)
        {
            if (kernelSize % 2 == 0)
                throw new ArgumentException("Kernel size must be odd.");

            int rpmLength = cellMap.GetLength(0);
            int injLength = cellMap.GetLength(1);
            int half = kernelSize / 2;

            // Precompute normalized Gaussian kernel
            double[,] kernel = PrecomputeKernel(kernelSize, sigma);

            // Temporary buffer if not in-place
            double[,] buffer = inPlace ? cellMap : new double[rpmLength, injLength];

            Action<int> processRow = rpmIndex =>
            {
                for (int injIndex = 0; injIndex < injLength; injIndex++)
                {
                    double sum = 0.0;
                    double weightSum = 0.0;

                    for (int di = -half; di <= half; di++)
                    {
                        int ni = rpmIndex + di;
                        if (ni < 0 || ni >= rpmLength) continue;

                        for (int dj = -half; dj <= half; dj++)
                        {
                            int nj = injIndex + dj;
                            if (nj < 0 || nj >= injLength) continue;

                            double w = kernel[di + half, dj + half];
                            sum += cellMap[ni, nj] * w;
                            weightSum += w;
                        }
                    }

                    buffer[rpmIndex, injIndex] = weightSum > 0 ? sum / weightSum : cellMap[rpmIndex, injIndex];
                }
            };

            if (useParallel && rpmLength * injLength > 256) // threshold to avoid thread overhead
            {
                Parallel.For(0, rpmLength, processRow);
            }
            else
            {
                for (int i = 0; i < rpmLength; i++)
                    processRow(i);
            }

            // If not in-place, copy buffer back
            if (!inPlace)
            {
                for (int i = 0; i < rpmLength; i++)
                    for (int j = 0; j < injLength; j++)
                        cellMap[i, j] = buffer[i, j];
            }
        }

        /// <summary>
        /// Precompute a normalized Gaussian kernel.
        /// </summary>
        private static double[,] PrecomputeKernel(int size, double sigma)
        {
            int half = size / 2;
            double[,] kernel = new double[size, size];
            double sum = 0.0;

            for (int i = -half; i <= half; i++)
            {
                for (int j = -half; j <= half; j++)
                {
                    double value = Math.Exp(-(i * i + j * j) / (2 * sigma * sigma));
                    kernel[i + half, j + half] = value;
                    sum += value;
                }
            }

            // Normalize kernel
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    kernel[i, j] /= sum;

            return kernel;
        }
    }
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
        public static AxisSplit<int> Split<T, TLabel>(T value, ReadOnlySpan<(T Min, T Max, TLabel Label)> ranges) where T : INumber<T>
        {
            int last = ranges.Length - 1;

            ref readonly var first = ref ranges[0];
            ref readonly var lastRange = ref ranges[last];

            if (value <= first.Min)
                return new(0, 0, 1d, 0d);

            if (value >= lastRange.Max)
                return new(last, last, 1d, 0d);

            for (int i = 0; i < last; i++)
            {
                ref readonly var r = ref ranges[i];

                if (value <= r.Max)
                {
                    ref readonly var next = ref ranges[i + 1];

                    T local = (value - r.Min) / (r.Max - r.Min);
                    double w = double.CreateChecked(local);

                    return new(
                        i,
                        i + 1,
                        1d - w,
                        w);
                }
            }

            return new(last, last, 1d, 0d);
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
            int inj,
            double delta,
            double weight)
        {
            if (weight <= 0) return;

            var key = new CellKey(rpm, inj);
            ref var accumulator = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, key, out _);
            accumulator ??= new CellAccumulator();
            accumulator.Add(delta, weight);
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
        private static Dictionary<CellKey, CellAccumulator> AccumulateDeltas(DataItem[] logs, double baseRate, double mean, double stdDev)
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
            List<double> trims = [];
            foreach (var log in logs)
            {
                int rpm = Math.Clamp(log.RPM, rpmMin, rpmMax);

                double inj = Math.Clamp((log.BENZ_b1 + log.BENZ_b2) * 0.5, injMin, injMax);

                var trim = ComputeTransientTrim(log, prevLog);
                trims.Add(trim);
                double correction = trim;// 1.0 + trim / 100.0;

                double rpmFactor = SmoothStep(rpmFadeStart, rpmFadeEnd, rpm);
                double injFactor = SmoothStep(injFadeStart, injFadeEnd, inj);

                double regionFactor = RegionMinFactor + RegionScale * Math.Sqrt(rpmFactor * injFactor);

                double delta = correction * baseRate * regionFactor;//(correction - 1.0) * baseRate * regionFactor;

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
        private static void AddSplit(Dictionary<CellKey, CellAccumulator> accumulators, AxisSplit<int> rpm, AxisSplit<int> inj, double delta)
        {
            Add(accumulators, rpm.Low, inj.Low, delta, rpm.WLow * inj.WLow);
            Add(accumulators, rpm.Low, inj.High, delta, rpm.WLow * inj.WHigh);
            Add(accumulators, rpm.High, inj.Low, delta, rpm.WHigh * inj.WLow);
            Add(accumulators, rpm.High, inj.High, delta, rpm.WHigh * inj.WHigh);
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
        private static void PropagateEdgeDeltas(Dictionary<CellKey, CellAccumulator> accumulators, int rpmLenght, int injLenght, double minWeight)
        {
            for (int iter = 0; iter < MaxIterations; iter++)
            {
                bool any = false;

                for (int i = 0; i < rpmLenght; i++)
                {
                    for (int j = 0; j < injLenght; j++)
                    {
                        var key = new CellKey(i, j);

                        if (accumulators.TryGetValue(key, out var cellAcc) && cellAcc.WeightSum > minWeight)
                        {
                            continue;
                        }
                        foreach (var (ni, nj) in GetValidNeighbors(i, j, rpmLenght, injLenght))
                        {
                            var nKey = new CellKey(ni, nj);

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
        private static void ApplyDeltas(Dictionary<CellKey, CellAccumulator> accumulators, double[,] cellMap, (double BaseLearningRate, double MinEffectiveWeight, double MaxDeltaPerCell, int TargetHitCount, double MeanTrim, double StdTrim) c)
        {
            foreach (var kv in accumulators)
            {
                if (kv.Value.WeightSum > c.MinEffectiveWeight)
                {
                    double confidence = Math.Min(1.0, (double)kv.Value.HitCount / c.TargetHitCount);

                    double delta = kv.Value.GetDelta(confidence);

                    delta = Math.Clamp(delta, -c.MaxDeltaPerCell, c.MaxDeltaPerCell);

                    double k = 1;

                    //cellMap[kv.Key.Rpm, kv.Key.Inj] = delta.Round(1);

                    //cellMap[kv.Key.Rpm, kv.Key.Inj] = cellMap[kv.Key.Rpm, kv.Key.Inj] * (k + delta / 100) - cellMap[kv.Key.Rpm, kv.Key.Inj];
                    cellMap[kv.Key.Rpm, kv.Key.Inj] *= (k + delta/100);
                }
            }
        }
        // ===================== ROUND =====================
        private static void RoundFuelMap(double[,] cellMap)
        {
            int rows = cellMap.GetLength(0);
            int cols = cellMap.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    cellMap[i, j] = cellMap[i, j].Round(0);
                }
            }
        }

        // ===================== ADAPTIVE CONSTANTS =====================
        private static (double BaseLearningRate, double MinEffectiveWeight, double MaxDeltaPerCell, int TargetHitCount, double MeanTrim, double StdTrim)
            ComputeAdaptiveConstants(DataItem[] logs)
        {
            double sum = 0, sumSq = 0, maxAbs = 0;

            var logLength = logs.Length;

            DataItem? prevLog = null;

            foreach (var log in logs)
            {
                double trim = ComputeTransientTrim(log, prevLog);
                sum += trim;
                sumSq += trim * trim;
                maxAbs = Math.Max(maxAbs, Math.Abs(trim));
                prevLog = log;
            }

            double mean = sum / logLength;
            double std = Math.Sqrt((sumSq / logLength) - (mean * mean));

            double baseRate = Math.Clamp(std / 5.0, 1, 50);//Math.Clamp(std / 50.0, 0.1, 0.5);
            double minWeight = 1.0 / logLength;
            double maxDelta = Math.Clamp(maxAbs / 3, 1, 50);//Math.Clamp(maxAbs / 300.0, 0.01, 0.05)

            int targetHit = Math.Max(5, logLength / (Settings.RpmColumns.Length * Settings.InjectionRanges.Length));

            return (baseRate, minWeight, maxDelta, targetHit, mean, std);
        }

        // ===================== PUBLIC ENTRY =====================
        public void AutoCorrectFuelTable(DataItem[] logs, double[,] cellMap)
        {
            var rpmLength = Settings.RpmColumns.Length;
            var injLength = Settings.InjectionRanges.Length;

            var constants = ComputeAdaptiveConstants(logs);

            var accumulators = AccumulateDeltas(logs, constants.BaseLearningRate, constants.MeanTrim, constants.StdTrim);

            PropagateEdgeDeltas(accumulators, rpmLength, injLength, constants.MinEffectiveWeight);

            ApplyDeltas(accumulators, cellMap, constants);

            FuelMapSmoother.Smooth(cellMap, KernelSize, KernelSigma);

            RoundFuelMap(cellMap);
        }
        public static double[,] BuildTable(DataItem[] logs, double[,] cellMap, bool enableSmooth, bool enableInterpolation)
        {
            int rpmLength = cellMap.GetLength(0);
            int injLength = cellMap.GetLength(1);

            double[,] result = new double[rpmLength, injLength];

            for (int injIndex = 0; injIndex < injLength; injIndex++)
            {
                var inj = Settings.InjectionRanges[injIndex];
                for (int rpmIndex = 0; rpmIndex < rpmLength; rpmIndex++)
                {
                    var rpm = Settings.RpmColumns[rpmIndex];

                    var values = logs
                        .Where(d =>
                            (d.BENZ_b1 > inj.Min && d.BENZ_b2 > inj.Min) &&
                            (d.BENZ_b1 <= inj.Max && d.BENZ_b2 <= inj.Max) &&
                            d.RPM > rpm.Min && d.RPM <= rpm.Max)
                        .Select(d => d.Trim).ToArray();

                    double trim = 1.0d;
                    bool found = values.Any();

                    if (found)
                    {
                        trim = 1 + values.Median() / 100;
                        result[rpmIndex, injIndex] = cellMap[rpmIndex, injIndex] * trim;
                    }
                    else if (enableInterpolation && rpm.Label > 3500 && inj.Label >= 5.5) // keep your condition
                    {
                        var t = 1.0d;
                        var rpmSave = 0;
                        for (int lowerRpm = rpmIndex - 1; lowerRpm >= 0; lowerRpm--)
                        {
                            values = logs
                                     .Where(d =>
                                         (d.BENZ_b1 > inj.Min && d.BENZ_b2 > inj.Min) &&
                                         (d.BENZ_b1 <= inj.Max && d.BENZ_b2 <= inj.Max) &&
                                         d.RPM > Settings.RpmColumns[lowerRpm].Min && d.RPM <= Settings.RpmColumns[lowerRpm].Max)
                                     .Select(d => d.Trim).ToArray();
                            if(rpm.Label == 4000 && inj.Label ==6.5)
                            {

                            }

                            found = values.Any();

                            if (found)
                            {
                                var tNew = 1 + values.Median() / 100;

                                if (t < tNew)
                                {
                                    t = tNew;
                                    rpmSave = lowerRpm;
                                    found = true;
                                }
                                else
                                {
                                    if(t == 1.0d)
                                        t = tNew;
                                    rpmSave = rpmIndex;
                                   
                                    break;
                                }
                                if (inj.Label <= 5.5)
                                    break;
                            }
                        }

                        var newValue = (cellMap[rpmIndex, injIndex] * t);
                        if(inj.Label > 4.5)
                            newValue += rpmIndex - rpmSave;

                        result[rpmIndex, injIndex] = newValue;
                       
                    }
                    if(!found)
                        result[rpmIndex, injIndex] = cellMap[rpmIndex, injIndex] * trim;

                }
            }
            if(enableSmooth)
             FuelMapSmoother.Smooth(result, KernelSize, KernelSigma);

            RoundFuelMap(result);
            
            return result;
            
        }
    }
}