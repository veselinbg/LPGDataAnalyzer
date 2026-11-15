namespace LPGDbAnalyzer.Forms
{
    /// <summary>
    /// Cell colours as in LPGDataAnalyzer (ColorHelper / DataGridViewColorization): blue - white - red,
    /// square-root scaled so small values still show; empty cells light grey; the smallest and largest
    /// value bold; dark cells get white text; the selected cell is yellow.
    /// </summary>
    public static class Heatmap
    {
        public static readonly Color Blue = Color.FromArgb(100, 140, 255);
        public static readonly Color Red = Color.FromArgb(255, 120, 120);
        public static readonly Color Empty = Color.LightGray;

        /// <summary>-1 = blue, 0 = white, +1 = red.</summary>
        public static Color Diverging(double value)
        {
            value = Math.Clamp(value, -1, 1);
            return value < 0 ? Blend(Blue, Color.White, value + 1) : Blend(Color.White, Red, value);
        }

        /// <summary>
        /// Colours a table of absolute values (e.g. a fuel map): lowest value white, highest red.
        /// <paramref name="values"/> is [row][column] and matches the grid cells at
        /// (<paramref name="firstColumn"/> + column, <paramref name="firstRow"/> + row).
        /// </summary>
        public static void ColorizeValues(DataGridView grid, double?[][] values, int firstColumn = 0, int firstRow = 0, Font? boldFont = null)
        {
            var all = values.SelectMany(r => r).OfType<double>().ToArray();
            double min = all.Length > 0 ? all.Min() : 0;
            double max = all.Length > 0 ? all.Max() : 0;
            double range = Math.Max(max - min, 1e-9);

            bool minMarked = false, maxMarked = false;

            for (int r = 0; r < values.Length; r++)
            {
                for (int c = 0; c < values[r].Length; c++)
                {
                    var cell = grid[firstColumn + c, firstRow + r];

                    if (values[r][c] is not double v)
                    {
                        Apply(cell, Empty);
                        continue;
                    }

                    Apply(cell, Diverging(Math.Sqrt((v - min) / range)));

                    // Extremes in bold (first occurrence of each).
                    bool extreme = (!minMarked && v == min) || (!maxMarked && v == max);
                    minMarked |= v == min;
                    maxMarked |= v == max;
                    cell.Style.Font = extreme ? boldFont : null;
                }
            }
        }

        public static void Apply(DataGridViewCell cell, Color back)
        {
            cell.Style.BackColor = back;
            cell.Style.ForeColor = back.GetBrightness() < 0.45f ? Color.White : Color.Black;
            cell.Style.SelectionBackColor = Color.Yellow;
            cell.Style.SelectionForeColor = Color.Black;
        }

        private static Color Blend(Color a, Color b, double t)
        {
            t = Math.Clamp(t, 0, 1);
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }
    }
}
