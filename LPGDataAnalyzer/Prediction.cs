using LPGDataAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LPGDataAnalyzer
{
    internal class Prediction
    {
        private static (TLabel Low, TLabel High, double WLow, double WHigh)
SplitAxis<TValue, TLabel>(
    TValue value,
    IReadOnlyList<(TValue Min, TValue Max, TLabel Label)> bins,
    Func<TValue, TValue, TValue, double> weightFunc)
    where TValue : IComparable<TValue>
        {
            // Below minimum → collapse to first label
            if (value.CompareTo(bins[0].Min) <= 0)
            {
                var label = bins[0].Label;
                return (label, label, 1.0, 0.0);
            }

            // Above maximum → collapse to last label
            if (value.CompareTo(bins[^1].Max) >= 0)
            {
                var label = bins[^1].Label;
                return (label, label, 1.0, 0.0);
            }

            int idx = 0;
            for (; idx < bins.Count; idx++)
            {
                if (bins[idx].Min.CompareTo(value) < 0 &&
                    value.CompareTo(bins[idx].Max) <= 0)
                    break;
            }

            var low = bins[idx];
            var high = bins[Math.Min(idx + 1, bins.Count - 1)];

            double wHigh = weightFunc(value, low.Min, high.Max);
            wHigh = Math.Clamp(wHigh, 0.0, 1.0);

            return (low.Label, high.Label, 1.0 - wHigh, wHigh);
        }


        private static (int Low, int High, double WLow, double WHigh)
RpmSplit(int rpm) =>
    SplitAxis(
        rpm,
        Settings.RpmColumns,
        (v, min, max) => (double)(v - min) / (max - min)
    );

        private static (double Low, double High, double WLow, double WHigh)
        InjSplit(double inj) =>
            SplitAxis(
                inj,
                Settings.InjectionRanges,
                (v, min, max) => (v - min) / (max - min)
            );

        public List<FuelCell> AutoCorrectFuelTable(
    IEnumerable<DataItem> validLogs,
    List<FuelCell> fuelTable)
        {
            // Fast lookup
            var cellMap = fuelTable.ToDictionary(
                c => (c.RpmBin, c.InjBin),
                c => c);

            var deltaSum = new Dictionary<(int Rpm, double Inj), double>();
            var weightSum = new Dictionary<(int Rpm, double Inj), double>();

            // ========================
            // ACCUMULATION PHASE
            // ========================
            foreach (var log in validLogs)
            {
                double avgTrim =
                    ((log.FAST_b1 + log.SLOW_b1) +
                     (log.FAST_b2 + log.SLOW_b2)) / 2.0;

                double correction = Math.Clamp(1.0 + avgTrim / 100.0, 0.95, 1.08);
                double delta = (correction - 1.0) * 0.30;

                if (Math.Abs(delta) < 1e-6)
                    continue;

                var rpm = RpmSplit(log.RPM);
                var inj = InjSplit(log.BENZ_b1);

                var targets = new[]
                {
            (rpm.Low,  inj.Low,  rpm.WLow  * inj.WLow),
            (rpm.Low,  inj.High, rpm.WLow  * inj.WHigh),
            (rpm.High, inj.Low,  rpm.WHigh * inj.WLow),
            (rpm.High, inj.High, rpm.WHigh * inj.WHigh)
        };

                foreach (var t in targets)
                {
                    if (t.Item3 <= 0.0)
                        continue;

                    var key = (t.Item1, t.Item2);

                    deltaSum[key] = deltaSum.GetValueOrDefault(key) + delta * t.Item3;
                    weightSum[key] = weightSum.GetValueOrDefault(key) + t.Item3;
                }
            }

            // ========================
            // EDGE PROPAGATION (RPM)
            // ========================
            var rpmLabels = Settings.RpmColumns.Select(c => c.Label).ToList();
            var injLabels = Settings.InjectionRanges.Select(r => r.Label).ToList();

            for (int j = 0; j < injLabels.Count; j++)
            {
                var first = (rpmLabels[0], injLabels[j]);
                var next = (rpmLabels[1], injLabels[j]);

                if (!weightSum.ContainsKey(first) &&
                     weightSum.TryGetValue(next, out double w))
                {
                    deltaSum[first] = deltaSum[next] * 0.5;
                    weightSum[first] = w * 0.5;
                }
            }

            // ========================
            // APPLY PHASE
            // ========================
            var result = new List<FuelCell>();

            foreach (var kv in deltaSum)
            {
                if (!cellMap.TryGetValue(kv.Key, out var cell))
                {
                    throw new InvalidOperationException(
                        $"Fuel cell not found for RPM={kv.Key.Rpm}, Inj={kv.Key.Inj}");
                }

                double avgDelta = kv.Value / weightSum[kv.Key];
                cell.Value = (cell.Value * (1.0 + avgDelta)).Round(1);

                result.Add(cell);
            }

            return result;
        }
    }
}
