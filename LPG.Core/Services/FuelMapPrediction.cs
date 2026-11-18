using LPGDataAnalyzer.Models;
using System.Buffers;

namespace LPGDataAnalyzer.Services
{
    public class FuelMapPrediction
    {
        private const int StackLimit = 32;

        /// <summary>
        /// Share of the bank-average fast trim applied to a cell per prediction.
        /// Was 0.5 (the former "(medianB1 + medianB2) / 4"); 0.8 since 2026-11-14 with the user's OK:
        /// Check accuracy on the 6 Capture2 logs gave 0.5: 6.5 %, 0.7: 8.3 %, 0.8: 8.8 %, 0.9: 8.9 %, 1.0: 8.6 %
        /// (every file better at 0.8). 0.8 instead of the peak leaves a margin against overshooting over several maps.
        /// </summary>
        public const double CorrectionGain = 0.8;

        /// <summary>
        /// OBD readings (parser groups) with less than this share of rows in one cell
        /// were taken while moving across cells and are not used.
        /// </summary>
        public const double MinGroupStability = 0.6;

        /// <summary>
        /// A reading is not used when the petrol injection time changes more than this many times over
        /// its rows and the rows its trim describes (tip-in, lift-off): the petrol ECU's transient fuelling,
        /// not the map, decides the trim there. Chosen with Check accuracy on the October/November logs
        /// (1.7–2.5 all gave about the same; together with <see cref="IsNotRegulating"/>: 4.75 % → 6.47 %).
        /// </summary>
        public const double MaxLoadChangeRatio = 2.0;

        /// <summary>
        /// One log reading's share in one cell for one bank. Weight is 1 when readings are
        /// put into a single cell, or the ECU's bilinear interpolation weight (0..1).
        /// </summary>
        private readonly record struct CellSample(DataItem Item, double Weight);

        /// <summary>Why a reading was not used by the prediction (the first check that failed, in this order).</summary>
        public enum RejectReason
        {
            /// <summary>|BENZ_Diff| at or above Max Benz Diff.</summary>
            BankDifference,
            /// <summary>FAST exactly 0 on both banks (<see cref="IsNotRegulating"/>).</summary>
            NotRegulating,
            /// <summary>Petrol injection time changes more than <see cref="MaxLoadChangeRatio"/> times (<see cref="IsLoadTransient"/>).</summary>
            LoadTransient,
            /// <summary>Trim Delay on, but no rows logged that much earlier (start of a file or a gap).</summary>
            NoDelayedRows,
            /// <summary>The engine position is outside the map axes.</summary>
            OutsideMap,
            /// <summary>Interpolation weights off: the reading's rows are spread over cells (<see cref="MinGroupStability"/>).</summary>
            SpreadOverCells,
        }

        /// <summary>A reading the prediction did not use, and why.</summary>
        public readonly record struct RejectedReading(DataItem Item, RejectReason Reason);

        /// <summary>
        /// Suggests a corrected fuel map from logs recorded with <paramref name="cellMap"/> in the ECU.
        /// Positive fast trim = lean = increase the cell.
        /// </summary>
        /// <param name="referencePressure">Reducer pressure the trims are scaled to; 0 or less turns the pressure correction off.</param>
        /// <param name="useInterpolationWeights">
        /// The ECU interpolates bilinearly between map breakpoints, so a reading's fuel comes from up to
        /// 4 cells. When true, every raw row of a reading is credited to its 4 surrounding cells with
        /// those weights (averaged over the rows), instead of the whole reading going to one cell.
        /// </param>
        /// <param name="useTrimDelay">
        /// When true, readings are placed at the engine position logged <see cref="AppSettings.TrimDelayMs"/>
        /// earlier (<see cref="DataItem.DelayedPositions"/>), because the OBD trim describes that moment.
        /// </param>
        /// <returns>
        /// The new map, the readings that were not used, and the readings' weight per cell as [rpm, injection]
        /// (sum over both banks; with interpolation weights the sum of the weights). A cell whose weight is
        /// <paramref name="minCount"/> or less keeps its current value.
        /// </returns>
        public static (double?[,] result, List<RejectedReading> invalidItems, double[,] cellWeights) BuildTable(
            DataItem[] logs,
            double?[,] cellMap,
            double referencePressure,
            int minCount = 0,
            bool showOnlyChanges = false,
            bool round = true,
            bool showOnlyMultiplier = false,
            double minChangeValue = 0.5d,
            double benzDiffMax = 10d,
            bool showOnlyCount = false,
            bool useInterpolationWeights = false,
            bool useTrimDelay = false)
        {
            int rpmLength = Settings.RpmColumns.Length;
            int injLength = Settings.InjectionRanges.Length;

            if (cellMap.GetLength(0) != rpmLength || cellMap.GetLength(1) != injLength)
                throw new ArgumentException(
                    $"Fuel map is {cellMap.GetLength(0)}x{cellMap.GetLength(1)}, expected {rpmLength}x{injLength}.",
                    nameof(cellMap));

            var invalidItems = new List<RejectedReading>();

            BuildLogGrid(logs, rpmLength, injLength, benzDiffMax, useInterpolationWeights, useTrimDelay,
                invalidItems, out var gridB1, out var gridB2);

            var counts = new int[rpmLength, injLength];
            var cellWeights = new double[rpmLength, injLength];
            var multipliers = new double[rpmLength, injLength];
            // Correction factor per cell that has enough logs (null = not enough data).
            var factors = new double?[rpmLength, injLength];

            // ---------------- Per-cell correction ----------------
            for (int injIndex = 0; injIndex < injLength; injIndex++)
            {
                for (int rpmIndex = 0; rpmIndex < rpmLength; rpmIndex++)
                {
                    int idx = injIndex * rpmLength + rpmIndex;
                    var cellB1 = gridB1[idx];
                    var cellB2 = gridB2[idx];

                    // Number of readings; with interpolation weights, the sum of the weights.
                    double effectiveCount = TotalWeight(cellB1) + TotalWeight(cellB2);
                    int count = (int)Math.Round(effectiveCount);
                    counts[rpmIndex, injIndex] = count;
                    cellWeights[rpmIndex, injIndex] = effectiveCount;

                    if (effectiveCount <= 0 || effectiveCount <= minCount)
                        continue;

                    double average = BankAverage(cellB1, cellB2, referencePressure);
                    double multiplier = average * CorrectionGain;

                    multipliers[rpmIndex, injIndex] = multiplier;

                    // Threshold is checked on the undamped average, so positive and negative
                    // trims of the same size are treated the same way.
                    bool apply = Math.Abs(average) > minChangeValue;

                    factors[rpmIndex, injIndex] = apply ? 1 + multiplier / 100 : 1;
                }
            }

            // ---------------- Result ----------------
            var result = new double?[rpmLength, injLength];
            int mapDigits = round ? 0 : 2;

            for (int injIndex = 0; injIndex < injLength; injIndex++)
            {
                for (int rpmIndex = 0; rpmIndex < rpmLength; rpmIndex++)
                {
                    var currentValue = cellMap[rpmIndex, injIndex];

                    if (showOnlyCount)
                    {
                        int count = counts[rpmIndex, injIndex];
                        result[rpmIndex, injIndex] = count > 0 ? count : null;
                        continue;
                    }

                    if (factors[rpmIndex, injIndex] is double factor)
                    {
                        if (showOnlyMultiplier)
                        {
                            result[rpmIndex, injIndex] = multipliers[rpmIndex, injIndex].Round();
                            continue;
                        }

                        bool changed = factor != 1;

                        if (showOnlyChanges && !changed)
                            continue;

                        if (currentValue is not double current)
                            continue;

                        double value = current * factor;

                        value = value.Round(mapDigits);

                        if (!showOnlyChanges || value != current)
                            result[rpmIndex, injIndex] = value;
                    }
                    else if (!showOnlyChanges)
                    {
                        result[rpmIndex, injIndex] = currentValue?.Round(mapDigits);
                    }
                }
            }

            return (result, invalidItems, cellWeights);
        }

        /// <summary>
        /// Map factor for a trim: 1 + trim/100 when the trim is above the threshold, otherwise 1.
        /// </summary>
        public static double TrimCalculation(double trim, double minChangeValue)
        {
            return 1 + (Math.Abs(trim) > minChangeValue ? trim / 100 : 0);
        }

        // ---------------- Log grid ----------------

        /// <summary>
        /// Puts every usable log into the cells of each bank (RPM column, that bank's petrol injection row).
        /// <para>Without interpolation weights each reading goes to one cell. Readings taken across cells
        /// (<see cref="MinGroupStability"/>), with too large a bank difference, or outside the axes go to
        /// <paramref name="invalidItems"/>.</para>
        /// <para>With interpolation weights each raw row is split over its 4 surrounding breakpoints, as the
        /// ECU does, and the weights are averaged over the reading's rows. The stability filter is not used,
        /// because the weights already follow the rows across cells.</para>
        /// <para>With the trim delay, the position is taken from <see cref="DataItem.DelayedPositions"/>;
        /// readings without delayed rows (start of a file) are invalid.</para>
        /// </summary>
        private static void BuildLogGrid(
            DataItem[] logs,
            int rpmLength,
            int injLength,
            double benzDiffMax,
            bool useInterpolationWeights,
            bool useTrimDelay,
            List<RejectedReading> invalidItems,
            out CellSample[][] gridB1,
            out CellSample[][] gridB2)
        {
            int cellCount = injLength * rpmLength;

            var listB1 = new List<CellSample>[cellCount];
            var listB2 = new List<CellSample>[cellCount];

            for (int i = 0; i < cellCount; i++)
            {
                listB1[i] = [];
                listB2[i] = [];
            }

            // 0 or less (e.g. an empty input box) means no bank-difference limit.
            if (benzDiffMax <= 0)
                benzDiffMax = double.PositiveInfinity;

            var axes = useInterpolationWeights ? MapAxes.FromSettings() : null;
            var cellWeights = new Dictionary<int, double>();

            foreach (var log in logs)
            {
                RejectReason? reason =
                    Math.Abs(log.BENZ_Diff) >= benzDiffMax ? RejectReason.BankDifference
                    : IsNotRegulating(log) ? RejectReason.NotRegulating
                    : IsLoadTransient(log) ? RejectReason.LoadTransient
                    : null;

                if (reason is RejectReason r)
                {
                    invalidItems.Add(new RejectedReading(log, r));
                    continue;
                }

                EnginePosition[] positions;

                if (useTrimDelay)
                {
                    positions = log.DelayedPositions;
                }
                else
                {
                    // Items not built by the parser have no rows: use the group average.
                    positions = log.Positions.Length > 0
                        ? log.Positions
                        : [new EnginePosition(log.RPM, log.BENZ_b1, log.BENZ_b2)];
                }

                if (positions.Length == 0)
                {
                    invalidItems.Add(new RejectedReading(log, RejectReason.NoDelayedRows));
                    continue;
                }

                if (useInterpolationWeights)
                {
                    bool added = AddInterpolated(log, positions, static p => p.Benz1, axes!, rpmLength, cellWeights, listB1);
                    added |= AddInterpolated(log, positions, static p => p.Benz2, axes!, rpmLength, cellWeights, listB2);

                    if (!added)
                        invalidItems.Add(new RejectedReading(log, RejectReason.OutsideMap));

                    continue;
                }

                int rpmIndex, b1, b2;
                double stability;

                if (useTrimDelay)
                {
                    // Same as the parser does for the group's own rows, on the delayed rows.
                    rpmIndex = Extensions.GetRpmIndex((int)Math.Round(Average(positions, static p => p.Rpm)));
                    b1 = Extensions.GetInjectionIndex(Average(positions, static p => p.Benz1));
                    b2 = Extensions.GetInjectionIndex(Average(positions, static p => p.Benz2));
                    stability = DominantCellShare(positions);
                }
                else
                {
                    rpmIndex = log.GetRpmIndex();
                    b1 = log.GetInjectionIndex(static x => x.BENZ_b1);
                    b2 = log.GetInjectionIndex(static x => x.BENZ_b2);
                    stability = log.Stability;
                }

                if (rpmIndex < 0 || (b1 < 0 && b2 < 0))
                {
                    invalidItems.Add(new RejectedReading(log, RejectReason.OutsideMap));
                    continue;
                }

                if (stability < MinGroupStability)
                {
                    invalidItems.Add(new RejectedReading(log, RejectReason.SpreadOverCells));
                    continue;
                }

                if (b1 >= 0)
                    listB1[b1 * rpmLength + rpmIndex].Add(new CellSample(log, 1));

                if (b2 >= 0)
                    listB2[b2 * rpmLength + rpmIndex].Add(new CellSample(log, 1));
            }

            gridB1 = new CellSample[cellCount][];
            gridB2 = new CellSample[cellCount][];

            for (int i = 0; i < cellCount; i++)
            {
                gridB1[i] = listB1[i].ToArray();
                gridB2[i] = listB2[i].ToArray();
            }
        }

        // ---------------- Readings without a usable trim ----------------

        /// <summary>
        /// FAST exactly 0 on both banks: the petrol ECU is not regulating (open loop under full load,
        /// fuel cut on overrun, trim reset after a jump). Such readings say nothing about the map.
        /// </summary>
        internal static bool IsNotRegulating(DataItem log) => log.FAST_b1 == 0 && log.FAST_b2 == 0;

        /// <summary>
        /// The petrol injection time over the reading's rows and the rows its trim describes changes more
        /// than <see cref="MaxLoadChangeRatio"/> times (see there). Readings without rows are kept.
        /// </summary>
        internal static bool IsLoadTransient(DataItem log) => LoadChangeRatio(log) > MaxLoadChangeRatio;

        /// <summary>
        /// Largest / smallest average petrol injection time over the reading's rows and its delayed rows
        /// (0 when it has no rows); <see cref="IsLoadTransient"/> compares it with <see cref="MaxLoadChangeRatio"/>.
        /// </summary>
        public static double LoadChangeRatio(DataItem log)
        {
            double min = double.MaxValue, max = 0;

            foreach (var rows in (ReadOnlySpan<EnginePosition[]>)[log.Positions, log.DelayedPositions])
            {
                foreach (var p in rows)
                {
                    double benz = (p.Benz1 + p.Benz2) / 2;
                    min = Math.Min(min, benz);
                    max = Math.Max(max, benz);
                }
            }

            return max > 0 ? max / Math.Max(min, 0.1) : 0;
        }

        // ---------------- Interpolation weights ----------------

        /// <summary>
        /// Breakpoints of the map axes (cell labels), where the ECU takes the map values.
        /// </summary>
        internal sealed record MapAxes(double[] Rpm, double[] Injection)
        {
            public static MapAxes FromSettings() => new(
                Settings.RpmColumns.Select(static c => (double)c.Label).ToArray(),
                Settings.InjectionRanges.Select(static r => r.Label).ToArray());
        }

        /// <summary>
        /// Lower and upper breakpoint around <paramref name="value"/> and the position between them (0..1).
        /// Values outside the axis use the edge breakpoint, as the ECU does.
        /// </summary>
        internal static (int Lower, int Upper, double Fraction) Bracket(double[] labels, double value)
        {
            if (value <= labels[0])
                return (0, 0, 0);

            int last = labels.Length - 1;
            if (value >= labels[last])
                return (last, last, 0);

            int index = Array.BinarySearch(labels, value);
            if (index >= 0)
                return (index, index, 0);

            int upper = ~index;
            int lower = upper - 1;

            return (lower, upper, (value - labels[lower]) / (labels[upper] - labels[lower]));
        }

        /// <summary>
        /// Adds one reading of one bank to the cells its raw rows used, weighted by the ECU's
        /// bilinear interpolation and averaged over the rows. Returns false when no row had a petrol time.
        /// </summary>
        private static bool AddInterpolated(
            DataItem log,
            EnginePosition[] positions,
            Func<EnginePosition, double> benzSelector,
            MapAxes axes,
            int rpmLength,
            Dictionary<int, double> cellWeights,
            List<CellSample>[] cells)
        {
            cellWeights.Clear();
            int rows = 0;

            foreach (var p in positions)
            {
                double benz = benzSelector(p);
                if (benz <= 0 || p.Rpm <= 0)
                    continue;

                var (r0, r1, fr) = Bracket(axes.Rpm, p.Rpm);
                var (i0, i1, fi) = Bracket(axes.Injection, benz);

                AddWeight(cellWeights, i0 * rpmLength + r0, (1 - fr) * (1 - fi));
                AddWeight(cellWeights, i0 * rpmLength + r1, fr * (1 - fi));
                AddWeight(cellWeights, i1 * rpmLength + r0, (1 - fr) * fi);
                AddWeight(cellWeights, i1 * rpmLength + r1, fr * fi);
                rows++;
            }

            if (rows == 0)
                return false;

            foreach (var (cell, weight) in cellWeights)
                cells[cell].Add(new CellSample(log, weight / rows));

            return true;
        }

        private static void AddWeight(Dictionary<int, double> cellWeights, int cell, double weight)
        {
            if (weight <= 1e-9)
                return;

            cellWeights[cell] = cellWeights.GetValueOrDefault(cell) + weight;
        }

        private static double Average(EnginePosition[] positions, Func<EnginePosition, double> selector)
        {
            double sum = 0;
            foreach (var p in positions)
                sum += selector(p);

            return sum / positions.Length;
        }

        /// <summary>
        /// Share of rows in the most frequent cell (by the average petrol time of both banks), like the parser's Stability.
        /// </summary>
        private static double DominantCellShare(EnginePosition[] positions)
        {
            if (positions.Length <= 1)
                return 1;

            var counts = new Dictionary<(int, int), int>();
            int best = 0;

            foreach (var p in positions)
            {
                var key = (Extensions.GetRpmIndex(p.Rpm), Extensions.GetInjectionIndex((p.Benz1 + p.Benz2) / 2));
                int n = counts.GetValueOrDefault(key) + 1;
                counts[key] = n;
                best = Math.Max(best, n);
            }

            return (double)best / positions.Length;
        }

        private static double TotalWeight(CellSample[] samples)
        {
            double sum = 0;
            foreach (var s in samples)
                sum += s.Weight;

            return sum;
        }

        // ---------------- Trims ----------------

        /// <summary>
        /// Average of the bank medians, using only the banks that have logs in the cell.
        /// </summary>
        private static double BankAverage(CellSample[] cellB1, CellSample[] cellB2, double referencePressure)
        {
            double sum = 0;
            int banks = 0;

            if (cellB1.Length > 0)
            {
                sum += BankMedian(cellB1, static d => d.FAST_b1, referencePressure);
                banks++;
            }

            if (cellB2.Length > 0)
            {
                sum += BankMedian(cellB2, static d => d.FAST_b2, referencePressure);
                banks++;
            }

            return banks == 0 ? 0 : sum / banks;
        }

        /// <summary>
        /// Median fast trim of one bank in one cell, after the reducer pressure correction.
        /// With interpolation weights it is the weighted median; with all weights 1 it is the plain median.
        /// </summary>
        private static double BankMedian(CellSample[] items, Func<DataItem, double> trimSelector, double referencePressure)
        {
            bool weighted = false;
            foreach (var s in items)
            {
                if (s.Weight != 1)
                {
                    weighted = true;
                    break;
                }
            }

            int n = items.Length;
            double[]? rented = null;
            double[]? rentedWeights = null;

            Span<double> buffer = n <= StackLimit
                ? stackalloc double[StackLimit]
                : (rented = ArrayPool<double>.Shared.Rent(n));

            Span<double> weights = !weighted
                ? Span<double>.Empty
                : n <= StackLimit
                    ? stackalloc double[StackLimit]
                    : (rentedWeights = ArrayPool<double>.Shared.Rent(n));

            try
            {
                for (int i = 0; i < n; i++)
                {
                    var d = items[i].Item;
                    buffer[i] = ApplyPressureCorrection(trimSelector(d), d.PRESS, referencePressure);

                    if (weighted)
                        weights[i] = items[i].Weight;
                }

                return weighted
                    ? buffer[..n].WeightedMedian(weights[..n])
                    : buffer[..n].MedianCore();
            }
            finally
            {
                if (rented != null)
                    ArrayPool<double>.Shared.Return(rented);
                if (rentedWeights != null)
                    ArrayPool<double>.Shared.Return(rentedWeights);
            }
        }

        /// <summary>
        /// Scales a trim by the reducer pressure at the time of the reading, relative to
        /// <paramref name="referencePressure"/> (same formula as before the 2026-11-11 rewrite).
        /// Under high load the reducer pressure drops and the mixture risks going lean.
        /// Negative trim: × sqrt(ref / p). Positive trim: × sqrt(p / ref). 0 pressure = not logged;
        /// reference 0 or less = correction off.
        /// </summary>
        internal static double ApplyPressureCorrection(double trim, double pressure, double referencePressure)
        {
            if (referencePressure <= 0)
                return trim;

            if (pressure <= 0)
                pressure = referencePressure;

            double pressCoef = trim < 0
                ? Math.Sqrt(referencePressure / pressure)
                : Math.Sqrt(pressure / referencePressure);

            return trim * pressCoef;
        }

        // ---------------- Map error (for the temperature tables) ----------------

        /// <summary>
        /// The map error of every cell, [rpm, injection], in % of trim: the bank average of the weighted median
        /// pressure-corrected FAST trim, as the prediction finds it before <see cref="CorrectionGain"/>, with ECU
        /// interpolation weights and the trim delay. 0 where there are no readings.
        /// </summary>
        public static double[,] CellErrors(DataItem[] logs, double referencePressure, double benzDiffMax = 6)
        {
            int rpmLength = Settings.RpmColumns.Length;
            int injLength = Settings.InjectionRanges.Length;

            BuildLogGrid(logs, rpmLength, injLength, benzDiffMax, useInterpolationWeights: true, useTrimDelay: true,
                [], out var gridB1, out var gridB2);

            var errors = new double[rpmLength, injLength];
            for (int injIndex = 0; injIndex < injLength; injIndex++)
            {
                for (int rpmIndex = 0; rpmIndex < rpmLength; rpmIndex++)
                {
                    int idx = injIndex * rpmLength + rpmIndex;
                    if (TotalWeight(gridB1[idx]) + TotalWeight(gridB2[idx]) > 0)
                        errors[rpmIndex, injIndex] = BankAverage(gridB1[idx], gridB2[idx], referencePressure);
                }
            }

            return errors;
        }

        /// <summary>
        /// Map error at the rows a reading's trim describes (<see cref="DataItem.DelayedPositions"/>) for one bank,
        /// bilinear between the cells as the ECU interpolates, averaged over the rows. Null when no row has a petrol time.
        /// </summary>
        internal static double? ErrorAt(DataItem log, bool bank1, double[,] errors, MapAxes axes)
        {
            double sum = 0;
            int rows = 0;

            foreach (var p in log.DelayedPositions)
            {
                double benz = bank1 ? p.Benz1 : p.Benz2;
                if (benz <= 0 || p.Rpm <= 0)
                    continue;

                var (r0, r1, fr) = Bracket(axes.Rpm, p.Rpm);
                var (i0, i1, fi) = Bracket(axes.Injection, benz);

                sum += errors[r0, i0] * (1 - fr) * (1 - fi)
                     + errors[r1, i0] * fr * (1 - fi)
                     + errors[r0, i1] * (1 - fr) * fi
                     + errors[r1, i1] * fr * fi;
                rows++;
            }

            return rows == 0 ? null : sum / rows;
        }

        // ---------------- Counts ----------------

        /// <summary>Readings per map cell as [rpm, injection], with the axes currently in <see cref="Settings"/>.</summary>
        public static int[,] CountReadings(DataItem[] logs)
        {
            var counts = new int[Settings.RpmColumns.Length, Settings.InjectionRanges.Length];

            foreach (var d in logs)
            {
                int r = d.GetRpmIndex();
                int i = d.GetInjectionIndex();

                if (r >= 0 && i >= 0)
                    counts[r, i]++;
            }

            return counts;
        }
    }
}
