using LPGDataAnalyzer.Models.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace LPGDataAnalyzer.Controls
{
    internal class DataGridViewColorization
    {
        public static AxisSplit<int> HighlightDifferencesHeatmapWithValues(
                                        DataGridView dgv1,
                                        DataGridView dgv2 = null,
                                        double tolerance = 0.01)
        {
            int rows = dgv1.RowCount;
            int cols = dgv1.ColumnCount;

            if (dgv2 != null && (rows != dgv2.RowCount || cols != dgv2.ColumnCount))
                throw new ArgumentException("DataGridViews must have same dimensions.");

            double?[,] values = ExtractValues(dgv1, dgv2);

            return ApplyHeatmap(dgv1, dgv2, values, tolerance);
        }
        private static double?[,] ExtractValues(DataGridView dgv1, DataGridView dgv2)
        {
            int rows = dgv1.RowCount;
            int cols = dgv1.ColumnCount;

            double?[,] result = new double?[rows, cols];

            for (int r = 0; r < rows; r++)
            {
                for (int c = 1; c < cols; c++)
                {
                    double? v1 = GetCellDoubleNullable(dgv1, r, c);

                    if (v1 == null)
                    {
                        result[r, c] = null;
                        continue;
                    }

                    if (dgv2 == null)
                    {
                        result[r, c] = v1;
                    }
                    else
                    {
                        double? v2 = GetCellDoubleNullable(dgv2, r, c);

                        if (v2 == null)
                            result[r, c] = null;
                        else
                            result[r, c] = v1 - v2;
                    }
                }
            }

            return result;
        }
        private static double? GetCellDoubleNullable(DataGridView dgv, int r, int c)
        {
            var val = dgv.Rows[r].Cells[c].Value;

            if (val == null || val == DBNull.Value)
                return null;

            if (double.TryParse(val.ToString(), out double result))
                return result;

            return null;
        }
        private static void SetCellColor(DataGridView dgv1, DataGridView dgv2, int r, int c, Color color)
        {
            dgv1.Rows[r].Cells[c].Style.BackColor = color;

            if (dgv2 != null)
                dgv2.Rows[r].Cells[c].Style.BackColor = color;
        }
        private static AxisSplit<int> ApplyHeatmap(
            DataGridView dgv1,
            DataGridView dgv2,
            double?[,] diffs,
            double tolerance)
        {
            int rows = diffs.GetLength(0);
            int cols = diffs.GetLength(1);

            double minSigned = double.MaxValue;
            double maxSigned = double.MinValue;

            int minIndex = -1;
            int maxIndex = -1;

            // ---- Find extremes ----
            for (int r = 0; r < rows; r++)
            {
                for (int c = 1; c < cols; c++) // skip first column
                {
                    double? diffNullable = diffs[r, c];

                    if (!diffNullable.HasValue)
                        continue;

                    double diff = diffNullable.Value;

                    if (diff < minSigned)
                    {
                        minSigned = diff;
                        minIndex = r * cols + c;
                    }

                    if (diff > maxSigned)
                    {
                        maxSigned = diff;
                        maxIndex = r * cols + c;
                    }
                }
            }

            if (minSigned == double.MaxValue)
            {
                minSigned = -1e-6;
                maxSigned = 1e-6;
            }

            double maxAbs = Math.Max(Math.Abs(minSigned), Math.Abs(maxSigned));

            if (maxAbs < 1e-12)
                maxAbs = 1e-12;

            Font boldFont = new(dgv1.Font, FontStyle.Bold);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 1; c < cols; c++) // skip first column
                {
                    double? diffNullable = diffs[r, c];

                    if (!diffNullable.HasValue)
                    {
                        SetCellColor(dgv1, dgv2, r, c, Color.LightGray);
                        continue;
                    }

                    double diff = diffNullable.Value;

                    // --- tolerance dead zone ---
                    if (Math.Abs(diff) <= tolerance)
                    {
                        SetCellColor(dgv1, dgv2, r, c, Color.White);
                        continue;
                    }

                    // --- normalize ---
                    double normalized = diff / maxAbs;
                    // amplify small differences
                    normalized = Math.Sign(normalized) * Math.Sqrt(Math.Abs(normalized));

                    // clamp
                    normalized = Math.Max(-1, Math.Min(1, normalized));

                    Color color = ColorHelper.InterpolateDiverging(normalized);

                    SetCellColor(dgv1, dgv2, r, c, color);

                    // --- highlight extreme values ---
                    int index = r * cols + c;

                    if (index == minIndex || index == maxIndex)
                    {
                        dgv1.Rows[r].Cells[c].Style.Font = boldFont;

                        if (dgv2 != null)
                            dgv2.Rows[r].Cells[c].Style.Font = boldFont;
                    }
                }
            }

            return new AxisSplit<int>(minIndex, maxIndex, minSigned, maxSigned);
        }

        

    }
}
