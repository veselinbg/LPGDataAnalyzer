using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer.Services
{
    /// <summary>
    /// Measures how well the map prediction works, using only logs that are already recorded
    /// ("leave one file out"): each file in turn is hidden, the prediction is made from the other
    /// files, and the hidden file shows whether the predicted changes would have cancelled its trims.
    /// <para>For every reading of the hidden file, the change the ECU would apply at that engine position
    /// is the bilinear blend of the 4 surrounding cells' changes (as the MP6C interpolates), taken at the
    /// rows the trim describes (<see cref="DataItem.DelayedPositions"/>, ~0.85 s earlier) and averaged
    /// over them. The trim left after the change is FAST − change (positive trim = lean = the cell grows).
    /// The same yardstick is used for every option set, so the scores can be compared.</para>
    /// <para>Score = 1 − Σ(trim left)² / Σ(trim now)², over both banks: 0 % = no better than the
    /// current map, 100 % = all FAST trims cancelled, below 0 = worse. Because only
    /// <see cref="FuelMapPrediction.CorrectionGain"/> of each correction is applied, the most it can reach is
    /// 1 − (1 − gain)² (96 % at 0.8).</para>
    /// </summary>
    public static class PredictionScore
    {
        /// <param name="Readings">Readings of the hidden file that could be checked.</param>
        /// <param name="TrimNow">RMS FAST trim of those readings with the current map (%).</param>
        /// <param name="TrimAfter">RMS FAST trim expected after the predicted changes (%).</param>
        /// <param name="Score">Share of the trim error removed (0..1, negative = worse).</param>
        public sealed record FileScore(string File, int Readings, double TrimNow, double TrimAfter, double Score);

        public sealed record Result(int Readings, double TrimNow, double TrimAfter, double Score, IReadOnlyList<FileScore> Files);

        /// <summary>
        /// Runs the leave-one-file-out check. Returns null when the data has fewer than 2 files.
        /// </summary>
        /// <param name="data">Readings of all files, recorded with <paramref name="cellMap"/> in the ECU.</param>
        /// <param name="cellMap">The fuel map the files were recorded with, [rpm, injection].</param>
        /// <param name="predict">Makes the new map from the given readings, with the options under test.</param>
        public static Result? LeaveOneFileOut(DataItem[] data, double?[,] cellMap, Func<DataItem[], double?[,]> predict)
        {
            var files = data.Select(static d => d.SourceFile ?? "").Distinct().ToList();
            if (files.Count < 2)
                return null;

            var axes = FuelMapPrediction.MapAxes.FromSettings();
            var perFile = new List<FileScore>(files.Count);
            double sumNow = 0, sumAfter = 0;
            int samples = 0, readings = 0;

            foreach (var file in files)
            {
                var train = data.Where(d => (d.SourceFile ?? "") != file).ToArray();
                var change = ChangePercent(cellMap, predict(train));

                double fileNow = 0, fileAfter = 0;
                int fileSamples = 0, fileReadings = 0;

                foreach (var d in data)
                {
                    if ((d.SourceFile ?? "") != file || d.DelayedPositions.Length == 0)
                        continue;

                    bool used = false;
                    used |= Add(d.FAST_b1, ChangeAt(d.DelayedPositions, static p => p.Benz1, axes, change), ref fileNow, ref fileAfter, ref fileSamples);
                    used |= Add(d.FAST_b2, ChangeAt(d.DelayedPositions, static p => p.Benz2, axes, change), ref fileNow, ref fileAfter, ref fileSamples);

                    if (used)
                        fileReadings++;
                }

                if (fileSamples == 0)
                    continue;

                perFile.Add(new FileScore(file, fileReadings,
                    Math.Sqrt(fileNow / fileSamples), Math.Sqrt(fileAfter / fileSamples), Score(fileNow, fileAfter)));

                sumNow += fileNow;
                sumAfter += fileAfter;
                samples += fileSamples;
                readings += fileReadings;
            }

            if (samples == 0)
                return null;

            return new Result(readings, Math.Sqrt(sumNow / samples), Math.Sqrt(sumAfter / samples), Score(sumNow, sumAfter), perFile);
        }

        private static double Score(double now, double after) => now > 0 ? 1 - after / now : 0;

        private static bool Add(double trim, double? change, ref double now, ref double after, ref int samples)
        {
            if (change is not double c)
                return false;

            now += trim * trim;
            after += (trim - c) * (trim - c);
            samples++;
            return true;
        }

        /// <summary>Change of every cell in %, [rpm, injection]; 0 where either map has no value.</summary>
        private static double[,] ChangePercent(double?[,] oldMap, double?[,] newMap)
        {
            int rows = oldMap.GetLength(0), cols = oldMap.GetLength(1);
            var change = new double[rows, cols];

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (oldMap[r, c] is double o && o > 0 && newMap[r, c] is double n)
                        change[r, c] = (n / o - 1) * 100;

            return change;
        }

        /// <summary>
        /// Change the ECU would apply over the reading's rows for one bank: bilinear between the
        /// breakpoints, averaged over the rows. Null when no row has a petrol time.
        /// </summary>
        private static double? ChangeAt(EnginePosition[] positions, Func<EnginePosition, double> benzSelector,
            FuelMapPrediction.MapAxes axes, double[,] change)
        {
            double sum = 0;
            int rows = 0;

            foreach (var p in positions)
            {
                double benz = benzSelector(p);
                if (benz <= 0 || p.Rpm <= 0)
                    continue;

                var (r0, r1, fr) = FuelMapPrediction.Bracket(axes.Rpm, p.Rpm);
                var (i0, i1, fi) = FuelMapPrediction.Bracket(axes.Injection, benz);

                sum += change[r0, i0] * (1 - fr) * (1 - fi)
                     + change[r1, i0] * fr * (1 - fi)
                     + change[r0, i1] * (1 - fr) * fi
                     + change[r1, i1] * fr * fi;
                rows++;
            }

            return rows == 0 ? null : sum / rows;
        }
    }
}
