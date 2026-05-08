using LPGDataAnalyzer.Models.Common;

namespace LPGDataAnalyzer.Controls
{
    public class DataGridViewColorization
    {
        public static AxisSplit<int> HighlightDifferencesHeatmapWithValues(
            DataGridView dgv1,
            DataGridView dgv2 = null,
            string[,] markers = null,
            double tolerance = 0.01)
        {
            if (dgv2 != null && (dgv1.RowCount != dgv2.RowCount || dgv1.ColumnCount != dgv2.ColumnCount))
                throw new ArgumentException("DataGridViews must have same dimensions.");

            double?[,] values = ExtractValues(dgv1, dgv2);

            return ApplyHeatmap(dgv1, dgv2, values, markers, tolerance);
        }

        // -------------------------
        // Extract values (skip column 0 = InjectionTime)
        // -------------------------
        private static double?[,] ExtractValues(DataGridView dgv1, DataGridView dgv2)
        {
            int rows = dgv1.RowCount;
            int cols = dgv1.ColumnCount;

            int colOffset = 1;

            double?[,] result = new double?[rows, cols - colOffset];

            for (int r = 0; r < rows; r++)
            {
                for (int c = colOffset; c < cols; c++)
                {
                    double? v1 = GetCellDoubleNullable(dgv1, r, c);
                    int dataCol = c - colOffset;

                    if (v1 == null)
                    {
                        result[r, dataCol] = null;
                        continue;
                    }

                    if (dgv2 == null)
                    {
                        result[r, dataCol] = v1;
                    }
                    else
                    {
                        double? v2 = GetCellDoubleNullable(dgv2, r, c);
                        result[r, dataCol] = v2 == null ? null : v1 - v2;
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

            if (val.TryGetDouble(out double result))
                return result;

            return null;
        }

        private static void SetCellColor(DataGridView dgv1, DataGridView dgv2, int r, int c, Color color)
        {
            dgv1.Rows[r].Cells[c].Style.BackColor = color;

            if (dgv2 != null)
                dgv2.Rows[r].Cells[c].Style.BackColor = color;
        }

        // -------------------------
        // MAIN HEATMAP
        // -------------------------
        private static AxisSplit<int> ApplyHeatmap(
            DataGridView dgv1,
            DataGridView dgv2,
            double?[,] diffs,
            string[,] markers,
            double tolerance)
        {
            int colOffset = 1;

            int rows = diffs.GetLength(0);
            int cols = diffs.GetLength(1);

            double minSigned = double.MaxValue;
            double maxSigned = double.MinValue;

            (int r, int c) minCell = (-1, -1);
            (int r, int c) maxCell = (-1, -1);

            // -------------------------
            // FIND EXTREMES
            // -------------------------
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    double? diffNullable = diffs[r, c];
                    if (!diffNullable.HasValue)
                        continue;

                    double diff = diffNullable.Value;

                    if (diff < minSigned)
                    {
                        minSigned = diff;
                        minCell = (r, c + colOffset);
                    }

                    if (diff > maxSigned)
                    {
                        maxSigned = diff;
                        maxCell = (r, c + colOffset);
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
            Font italicFont = new(ColorHelper.FONT_NAME, 10, FontStyle.Italic);
            // -------------------------
            // APPLY HEATMAP
            // -------------------------
            for (int r = 0; r < rows; r++)
            {
                for (int c = colOffset; c < dgv1.ColumnCount; c++)
                {
                    int dataCol = c - colOffset;

                    double? diffNullable = diffs[r, dataCol];

                    if (!diffNullable.HasValue)
                    {
                        SetCellColor(dgv1, dgv2, r, c, Color.LightGray);
                        continue;
                    }

                    double diff = diffNullable.Value;

                    if (Math.Abs(diff) <= tolerance)
                    {
                        SetCellColor(dgv1, dgv2, r, c, Color.White);
                    }
                    else
                    {
                        double normalized = diff / maxAbs;
                        normalized = Math.Sign(normalized) * Math.Sqrt(Math.Abs(normalized));
                        normalized = Math.Clamp(normalized, -1, 1);

                        Color color = ColorHelper.InterpolateDiverging(normalized);
                        SetCellColor(dgv1, dgv2, r, c, color);
                    }

                    // -------------------------
                    // MARKERS (+ / -)
                    // -------------------------
                    if (markers != null)
                    {
                        // markers is [rpm, inj]
                        string marker = markers[dataCol, r];

                        if (marker == "+")
                        {
                            dgv1.Rows[r].Cells[c].Style.ForeColor = Color.DarkGreen;
                            dgv1.Rows[r].Cells[c].Style.Font = italicFont;
                            if (dgv2 != null)
                            {
                                dgv2.Rows[r].Cells[c].Style.ForeColor = Color.DarkGreen;
                                dgv2.Rows[r].Cells[c].Style.Font = italicFont;
                            }
                        }
                        else if (marker == "-")
                        {
                            dgv1.Rows[r].Cells[c].Style.ForeColor = Color.DarkRed;
                            dgv1.Rows[r].Cells[c].Style.Font = italicFont;
                            if (dgv2 != null)
                            {
                                dgv2.Rows[r].Cells[c].Style.ForeColor = Color.DarkRed;
                                dgv2.Rows[r].Cells[c].Style.Font = italicFont;
                            }
                        }
                    }

                    // -------------------------
                    // EXTREMES HIGHLIGHT
                    // -------------------------
                    if ((r, c) == minCell || (r, c) == maxCell)
                    {
                        dgv1.Rows[r].Cells[c].Style.Font = boldFont;

                        if (dgv2 != null)
                            dgv2.Rows[r].Cells[c].Style.Font = boldFont;
                    }
                }
            }

            return new AxisSplit<int>(
                minCell.r * cols + minCell.c,
                maxCell.r * cols + maxCell.c,
                minSigned,
                maxSigned);
        }
    }
}