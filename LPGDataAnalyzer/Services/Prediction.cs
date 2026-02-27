using LPGDataAnalyzer.Models;
using System.Runtime.InteropServices;

namespace LPGDataAnalyzer.Services
{ 
public sealed class PredictionConfig
{
    public double TransientWeight { get; set; } = 1.0;

    // Region factor smoothing
    public double RegionRpmCenter { get; set; } = 3000;
    public double RegionInjCenter { get; set; } = 5.0;
    public double RegionRpmSigma { get; set; } = 1000;
    public double RegionInjSigma { get; set; } = 2.0;
    public double RegionMinFactor { get; set; } = 0.3;
    public double RegionMaxFactor { get; set; } = 1.0;

    // K-factor tuning
    public double EconomyK { get; set; } = 0.95;
    public double EconomyRpmMax { get; set; } = 1500;
    public double EconomyInjMax { get; set; } = 3.5;
    public double HighLoadK { get; set; } = 1.02;
    public double HighLoadRpmMin { get; set; } = 1500;
    public double HighLoadInjMin { get; set; } = 8.0;

    // Edge propagation
    public double EdgeDecay { get; set; } = 0.5;

    // Adaptive smoothing
    public int SmoothingKernelSize { get; set; } = 7;
    public double SmoothingBaseSigma { get; set; } = 1.2;

    // Dynamic delta limits
    public double IdleMaxDelta { get; set; } = 0.01;
    public double CruiseMaxDelta { get; set; } = 0.03;
    public double HighLoadMaxDelta { get; set; } = 0.01;
}
internal sealed partial class Prediction
    {
        private readonly PredictionConfig _config;

        public Prediction(PredictionConfig config)
        {
            _config = config ?? new PredictionConfig();
        }

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

        // ===================== SMOOTHSTEP / LERP =====================
        private static double SmoothStep(double edge0, double edge1, double x)
        {
            if (x <= edge0) return 0;
            if (x >= edge1) return 1;
            double t = (x - edge0) / (edge1 - edge0);
            return t * t * (3 - 2 * t);
        }

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;

        // ===================== ACCUMULATOR =====================
        private sealed class CellAccumulator
        {
            public double DeltaSum;
            public double WeightSum;
            public double HitScore;

            public void Add(double delta, double weight, double trimConfidence = 1.0)
            {
                DeltaSum += delta * weight;
                WeightSum += weight;
                HitScore += trimConfidence * weight;
            }

            public double GetDelta(double targetHitCount)
            {
                if (WeightSum <= 0) return 0;
                double confidence = 1 - Math.Exp(-HitScore / targetHitCount);
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

        // ===================== TRANSIENT / TRIM =====================
        private double ComputeTransientTrim(DataItem log, DataItem? prevLog)
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

            double t = SmoothStep(MAP_slope_min, MAP_slope_max, transientMetric) * _config.TransientWeight;
            t = Math.Clamp(t, 0, 1);

            return (1 - t) * slow + t * fast;
        }

        private bool TryGetValidTrim(DataItem log, DataItem? prevLog, double mean, double stdDev, out double trim, out double transientMetric)
        {
            trim = ComputeTransientTrim(log, prevLog);
            transientMetric = prevLog != null
                ? Math.Sqrt(Math.Pow(log.MAP - prevLog.MAP, 2) + Math.Pow((log.RPM - prevLog.RPM) / 1000.0, 2))
                : 0;

            if (Math.Abs(trim) > 25) return false;
            if (stdDev > 0 && Math.Abs((trim - mean) / stdDev) > 2.5) return false;

            return true;
        }

        // ===================== REGION FACTOR =====================
        private double ComputeRegionFactor(int rpm, double inj)
        {
            double rpmWeight = Math.Exp(-Math.Pow(rpm - _config.RegionRpmCenter, 2) / (2 * _config.RegionRpmSigma * _config.RegionRpmSigma));
            double injWeight = Math.Exp(-Math.Pow(inj - _config.RegionInjCenter, 2) / (2 * _config.RegionInjSigma * _config.RegionInjSigma));
            return _config.RegionMinFactor + (_config.RegionMaxFactor - _config.RegionMinFactor) * (rpmWeight * injWeight);
        }

        // ===================== K-FACTOR =====================
        private double ComputeKFactor(FuelCell cell)
        {
            double k = 1.0;

            if (cell.RpmBin < _config.EconomyRpmMax && cell.InjBin < _config.EconomyInjMax)
            {
                double rpmW = 1.0 - cell.RpmBin / _config.EconomyRpmMax;
                double injW = 1.0 - cell.InjBin / _config.EconomyInjMax;
                k *= _config.EconomyK + (1 - rpmW * injW) * (1 - _config.EconomyK);
            }

            if (cell.RpmBin > _config.HighLoadRpmMin && cell.InjBin > _config.HighLoadInjMin)
            {
                double rpmW = Math.Min((cell.RpmBin - _config.HighLoadRpmMin) / 2000.0, 1.0);
                double injW = Math.Min((cell.InjBin - _config.HighLoadInjMin) / 4.0, 1.0);
                k *= 1.0 + (_config.HighLoadK - 1.0) * rpmW * injW;
            }

            return k;
        }

        // ===================== ACCUMULATION =====================
        private Dictionary<(int, double), CellAccumulator> AccumulateDeltas(IList<DataItem> logs, double baseRate, double mean, double stdDev)
        {
            var acc = new Dictionary<(int, double), CellAccumulator>(256);
            int rpmMin = Settings.RpmColumns[0].Min;
            int rpmMax = Settings.RpmColumns[^1].Max;
            double injMin = Settings.InjectionRanges[0].Min;
            double injMax = Settings.InjectionRanges[^1].Max;

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
                if (correction < 1.0 - _config.HighLoadMaxDelta || correction > 1.0 + _config.HighLoadMaxDelta)
                {
                    prevLog = log;
                    continue;
                }

                double regionFactor = ComputeRegionFactor(rpm, inj);
                double delta = (correction - 1.0) * baseRate * regionFactor;
                if (Math.Abs(delta) < 1e-9) { prevLog = log; continue; }

                var r = RpmSplit(rpm);
                var i = InjSplit(inj);
                double trimConfidence = 1.0 - Math.Clamp(transientMetric, 0, 1.0);

                Add(acc, r.Low, i.Low, delta, r.WLow * i.WLow, trimConfidence);
                Add(acc, r.Low, i.High, delta, r.WLow * i.WHigh, trimConfidence);
                Add(acc, r.High, i.Low, delta, r.WHigh * i.WLow, trimConfidence);
                Add(acc, r.High, i.High, delta, r.WHigh * i.WHigh, trimConfidence);

                prevLog = log;
            }

            return acc;
        }

        // ===================== EDGE PROPAGATION =====================
        private void PropagateEdgeDeltasSmarter(
            Dictionary<(int, double), CellAccumulator> acc,
            IList<int> rpmLabels,
            IList<double> injLabels,
            double minWeight)
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

                foreach (var (di, dj) in new (int, int)[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
                {
                    int ni = i + di;
                    int nj = j + dj;
                    if (ni < 0 || ni >= rpmLabels.Count || nj < 0 || nj >= injLabels.Count) continue;

                    var neighborKey = (rpmLabels[ni], injLabels[nj]);
                    ref var neighbor = ref CollectionsMarshal.GetValueRefOrAddDefault(acc, neighborKey, out _);
                    neighbor ??= new CellAccumulator();

                    double weightFactor = _config.EdgeDecay / (1 + dist);
                    neighbor.DeltaSum += cur.DeltaSum * weightFactor;
                    neighbor.WeightSum += cur.WeightSum * weightFactor;
                    neighbor.HitScore += cur.HitScore;

                    if (neighbor.WeightSum < minWeight) queue.Enqueue((neighborKey, dist + 1));
                }
            }
        }

        private double GetRegionMaxDelta(int rpm, double inj)
        {
            if (rpm < _config.EconomyRpmMax && inj < _config.EconomyInjMax) return _config.IdleMaxDelta;
            if (rpm < _config.HighLoadRpmMin && inj < _config.HighLoadInjMin) return _config.CruiseMaxDelta;
            return _config.HighLoadMaxDelta;
        }

        // ===================== APPLY DELTAS =====================
        private void ApplyDeltas(
            Dictionary<(int, double), CellAccumulator> acc,
            Dictionary<(int, double), FuelCell> cellMap,
            (double BaseLearningRate, double MinEffectiveWeight, double MaxDeltaPerCell, int TargetHitCount, double MeanTrim, double StdTrim) c)
        {
            foreach (var kv in acc)
            {
                if (kv.Value.WeightSum <= c.MinEffectiveWeight) continue;
                if (!cellMap.TryGetValue(kv.Key, out var cell)) continue;

                double delta = kv.Value.GetDelta(c.TargetHitCount);
                double stabilityFactor = Math.Min(1.0, kv.Value.WeightSum / c.TargetHitCount);
                double maxDelta = GetRegionMaxDelta(cell.RpmBin, cell.InjBin) * stabilityFactor;
                delta = Math.Clamp(delta, -maxDelta, maxDelta);

                double k = ComputeKFactor(cell);
                double confidence = 1 - Math.Exp(-kv.Value.HitScore / c.TargetHitCount);

                cell.Value = Lerp(cell.Value, cell.Value * (1 + delta), confidence);
                cell.Value *= k;
            }
        }

        // ===================== ADAPTIVE SMOOTHING =====================
        private void SmoothFuelMap(
            Dictionary<(int, double), FuelCell> cellMap,
            IList<int> rpmLabels,
            IList<double> injLabels)
        {
            int half = _config.SmoothingKernelSize / 2;
            var newValues = new Dictionary<(int, double), double>(cellMap.Count);

            for (int i = 0; i < rpmLabels.Count; i++)
            {
                for (int j = 0; j < injLabels.Count; j++)
                {
                    var key = (rpmLabels[i], injLabels[j]);
                    if (!cellMap.TryGetValue(key, out var cell)) continue;

                    double sum = 0, sumSq = 0, count = 0;
                    for (int di = -1; di <= 1; di++)
                        for (int dj = -1; dj <= 1; dj++)
                        {
                            int ni = i + di;
                            int nj = j + dj;
                            if (ni < 0 || ni >= rpmLabels.Count || nj < 0 || nj >= injLabels.Count) continue;
                            if (!cellMap.TryGetValue((rpmLabels[ni], injLabels[nj]), out var n)) continue;
                            sum += n.Value; sumSq += n.Value * n.Value; count++;
                        }

                    double mean = sum / count;
                    double variance = Math.Sqrt(sumSq / count - mean * mean);
                    double sigma = _config.SmoothingBaseSigma / (1 + variance * 5);

                    double sumW = 0, sumV = 0;
                    for (int di = -half; di <= half; di++)
                        for (int dj = -half; dj <= half; dj++)
                        {
                            int ni = i + di;
                            int nj = j + dj;
                            if (ni < 0 || ni >= rpmLabels.Count || nj < 0 || nj >= injLabels.Count) continue;
                            if (!cellMap.TryGetValue((rpmLabels[ni], injLabels[nj]), out var n)) continue;
                            if (Math.Abs(n.Value - cell.Value) > 0.1) continue;
                            double w = Math.Exp(-(di * di + dj * dj) / (2 * sigma * sigma));
                            sumW += w;
                            sumV += n.Value * w;
                        }
                    if (sumW > 0) newValues[key] = sumV / sumW;
                }
            }

            foreach (var kv in newValues) cellMap[kv.Key].Value = kv.Value;
        }

        private static void RoundFuelMap(Dictionary<(int, double), FuelCell> cellMap)
        {
            foreach (var cell in cellMap.Values)
                cell.Value = cell.Value.Round(0);
        }

        // ===================== ADAPTIVE CONSTANTS =====================
        private (double BaseLearningRate, double MinEffectiveWeight, double MaxDeltaPerCell, int TargetHitCount, double MeanTrim, double StdTrim)
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
            double std = Math.Sqrt(sumSq / logs.Count - mean * mean);

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

            PropagateEdgeDeltasSmarter(acc, rpmLabels, injLabels, constants.MinEffectiveWeight);

            ApplyDeltas(acc, cellMap, constants);

            SmoothFuelMap(cellMap, rpmLabels, injLabels);

            RoundFuelMap(cellMap);

            return new FuelCorrectionResult { UpdatedCells = cellMap.Values.ToList() };
        }
    }
}