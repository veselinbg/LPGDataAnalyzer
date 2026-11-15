using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer.Services
{
    /// <summary>
    /// Suggestion for one band of an ECU temperature correction table.
    /// </summary>
    /// <param name="Label">Band label from <see cref="Settings"/>.</param>
    /// <param name="Current">Coefficient currently set in the ECU.</param>
    /// <param name="Suggested">Suggested coefficient (whole number, the ECU accepts integers only).</param>
    /// <param name="Error">How much the band's value should change in % (positive = lean = increase), before
    /// rounding; null = no readings near the band.</param>
    /// <param name="Count">Readings near the band (sum of their interpolation weights).</param>
    public sealed record TemperatureBandSuggestion(string Label, int Current, int Suggested, double? Error, int Count);

    /// <summary>Suggestions for both ECU temperature tables.</summary>
    public sealed record TemperatureTablesSuggestion(
        IReadOnlyList<TemperatureBandSuggestion> Gas,
        IReadOnlyList<TemperatureBandSuggestion> Reducer);

    /// <summary>
    /// Suggests the ECU temperature correction tables (LPG temperature and reducer temperature) from logs.
    /// <para>How the ECU uses them (verified from GAS in the logs, 2026-11-14): each value is a % of the gas
    /// injection time, it is taken at the band's upper temperature (e.g. Temp_31_40 at 40 °C, the last band 10 °C
    /// above its lower limit), and the ECU interpolates linearly between those points.</para>
    /// <para>Each reading's FAST trim minus the map error at its position (the same cell errors, interpolation
    /// weights, trim delay and filters as the map prediction) is what is left for the temperature tables. Both
    /// tables are fitted together, because LPG and reducer temperature rise together while the engine warms up:
    /// fitting them one by one would correct the same error twice. Each reading counts for its two neighbouring
    /// breakpoints with the interpolation weights; values are shrunk towards 0 where readings are few.</para>
    /// <para>Checked leave-one-file-out on the 6 Capture2 logs (hide a file, suggest from the others, see how much
    /// of the hidden file's trim left after the map error the change cancels): the former band-median method
    /// −3.4 %, this one +1.9 %.</para>
    /// </summary>
    public static class TemperatureCorrectionPrediction
    {
        /// <summary>The LPG-temperature table is kept at 0 for this band; the other bands are relative to it.</summary>
        public const string GasAnchorLabel = "Temp_31_40";

        /// <summary>
        /// Shrinkage towards 0 (in readings): a breakpoint with this much reading weight moves half way to what its
        /// readings say. Chosen with the leave-one-file-out check (50–200 about the same, 1000 too strong).
        /// </summary>
        public const double Shrinkage = 200;

        /// <summary>Trim residuals are clipped to ±this (%), so single wild readings don't pull a band.</summary>
        public const double ResidualClip = 15;

        private const int Iterations = 40;

        /// <summary>Temperature where the ECU takes the value of the last band, above its lower limit.</summary>
        private const int LastBandSpan = 10;

        public static TemperatureTablesSuggestion Predict(
            IReadOnlyList<DataItem> data,
            IReadOnlyList<int> gasCurrent,
            IReadOnlyList<int> reducerCurrent,
            double referencePressure = 1.49)
        {
            var gasRanges = Settings.GasTemperatureRanges;
            var reducerRanges = Settings.ReductorTemperatureRanges;

            if (gasCurrent.Count != gasRanges.Length)
                throw new ArgumentException($"Expected {gasRanges.Length} LPG temperature values, got {gasCurrent.Count}.", nameof(gasCurrent));
            if (reducerCurrent.Count != reducerRanges.Length)
                throw new ArgumentException($"Expected {reducerRanges.Length} reducer temperature values, got {reducerCurrent.Count}.", nameof(reducerCurrent));

            var gasPoints = Breakpoints(gasRanges);
            var reducerPoints = Breakpoints(reducerRanges);

            // 1. What is left of each trim after the map error.
            var logs = data as DataItem[] ?? data.ToArray();
            var errors = FuelMapPrediction.CellErrors(logs, referencePressure);
            var axes = FuelMapPrediction.MapAxes.FromSettings();
            var samples = new List<Sample>(logs.Length * 2);

            foreach (var d in logs)
            {
                if (d.DelayedPositions.Length == 0 || FuelMapPrediction.IsNotRegulating(d) || FuelMapPrediction.IsLoadTransient(d))
                    continue;

                var gas = Weights(d.Temp_GAS, gasPoints);
                var reducer = Weights(d.Temp_RID, reducerPoints);

                foreach (bool bank1 in (ReadOnlySpan<bool>)[true, false])
                {
                    if (FuelMapPrediction.ErrorAt(d, bank1, errors, axes) is not double mapError)
                        continue;

                    double trim = FuelMapPrediction.ApplyPressureCorrection(bank1 ? d.FAST_b1 : d.FAST_b2, d.PRESS, referencePressure);
                    samples.Add(new Sample(Math.Clamp(trim - mapError, -ResidualClip, ResidualClip), gas, reducer));
                }
            }

            // 2. Both tables together.
            int anchor = Array.FindIndex(gasRanges, r => r.Label == GasAnchorLabel);
            var (gasChange, reducerChange, gasWeight, reducerWeight) = Fit(samples, gasRanges.Length, reducerRanges.Length, anchor);

            return new TemperatureTablesSuggestion(
                Suggestions(gasRanges, gasCurrent, gasChange, gasWeight, anchor),
                Suggestions(reducerRanges, reducerCurrent, reducerChange, reducerWeight, anchor: -1));
        }

        /// <summary>One bank of one reading: trim left after the map error and its two temperature positions.</summary>
        private readonly record struct Sample(double Residual, Pair Gas, Pair Reducer);

        /// <summary>Two neighbouring breakpoints and their interpolation weights (Weight1 = 0 when only one).</summary>
        private readonly record struct Pair(int Index0, double Weight0, int Index1, double Weight1);

        /// <summary>
        /// The value the ECU applies at temperature <paramref name="t"/> for a table (% of gas time):
        /// interpolated between the band upper temperatures, as verified from the logs.
        /// </summary>
        internal static double TableValueAt(double t, (int Min, int Max, string Label)[] ranges, IReadOnlyList<int> values)
        {
            var p = Weights(t, Breakpoints(ranges));
            return p.Weight0 * values[p.Index0] + p.Weight1 * values[p.Index1];
        }

        private static double[] Breakpoints((int Min, int Max, string Label)[] ranges)
        {
            var points = new double[ranges.Length];
            for (int i = 0; i < ranges.Length; i++)
                points[i] = i < ranges.Length - 1 ? ranges[i].Max : ranges[i].Min + LastBandSpan;
            return points;
        }

        private static Pair Weights(double t, double[] points)
        {
            int last = points.Length - 1;
            if (t <= points[0])
                return new Pair(0, 1, 0, 0);
            if (t >= points[last])
                return new Pair(last, 1, last, 0);

            int j = Array.BinarySearch(points, t);
            if (j >= 0)
                return new Pair(j, 1, j, 0);

            j = ~j;
            double f = (t - points[j - 1]) / (points[j] - points[j - 1]);
            return new Pair(j - 1, 1 - f, j, f);
        }

        /// <summary>
        /// Least squares of residual = gas(t_gas) + reducer(t_reducer) with ridge shrinkage, by block updates:
        /// all gas breakpoints from the residual left by the reducer table, then all reducer breakpoints.
        /// </summary>
        private static (double[] Gas, double[] Reducer, double[] GasWeight, double[] ReducerWeight) Fit(
            List<Sample> samples, int gasCount, int reducerCount, int anchor)
        {
            var gas = new double[gasCount];
            var reducer = new double[reducerCount];
            var gasWeight = new double[gasCount];
            var reducerWeight = new double[reducerCount];

            foreach (var s in samples)
            {
                gasWeight[s.Gas.Index0] += s.Gas.Weight0;
                gasWeight[s.Gas.Index1] += s.Gas.Weight1;
                reducerWeight[s.Reducer.Index0] += s.Reducer.Weight0;
                reducerWeight[s.Reducer.Index1] += s.Reducer.Weight1;
            }

            var num = new double[Math.Max(gasCount, reducerCount)];
            var den = new double[num.Length];

            for (int iteration = 0; iteration < Iterations; iteration++)
            {
                Update(samples, gas, reducer, updateGas: true, num, den, anchor);
                Update(samples, gas, reducer, updateGas: false, num, den, anchor: -1);
            }

            return (gas, reducer, gasWeight, reducerWeight);
        }

        private static void Update(List<Sample> samples, double[] gas, double[] reducer, bool updateGas, double[] num, double[] den, int anchor)
        {
            var values = updateGas ? gas : reducer;
            Array.Clear(num);
            Array.Clear(den);

            foreach (var s in samples)
            {
                double rest = s.Residual - Value(s.Gas, gas) - Value(s.Reducer, reducer);
                var p = updateGas ? s.Gas : s.Reducer;

                Add(p.Index0, p.Weight0);
                if (p.Weight1 > 0)
                    Add(p.Index1, p.Weight1);

                void Add(int k, double w)
                {
                    num[k] += w * (rest + w * values[k]);
                    den[k] += w * w;
                }
            }

            for (int k = 0; k < values.Length; k++)
            {
                if (k == anchor)
                    continue;

                values[k] = den[k] > 0 ? num[k] / (den[k] + Shrinkage) : 0;
            }
        }

        private static double Value(Pair p, double[] values) => p.Weight0 * values[p.Index0] + p.Weight1 * values[p.Index1];

        private static List<TemperatureBandSuggestion> Suggestions(
            (int Min, int Max, string Label)[] ranges, IReadOnlyList<int> current, double[] change, double[] weight, int anchor)
        {
            var result = new List<TemperatureBandSuggestion>(ranges.Length);

            for (int b = 0; b < ranges.Length; b++)
            {
                bool hasData = weight[b] > 0;
                int step = b == anchor ? 0 : (int)Math.Round(change[b], MidpointRounding.AwayFromZero);

                result.Add(new TemperatureBandSuggestion(
                    ranges[b].Label,
                    current[b],
                    current[b] + step,
                    hasData ? Math.Round(change[b], 2) : null,
                    (int)Math.Round(weight[b])));
            }

            return result;
        }
    }
}
