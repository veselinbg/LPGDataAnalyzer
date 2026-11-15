using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Services;
using LPGDbAnalyzer.Data;
using System.Globalization;

namespace LPGDbAnalyzer.Forms
{
    // Inside the namespace, so LPG.Core's Average/Min/Max/Median extensions are preferred over LINQ's,
    // as in LPGDataAnalyzer's own code.
    using LPGDataAnalyzer;

    /// <summary>
    /// Compares the reports of two fuel tables: A = the older table (before), B = the newer one (after).
    /// Each side uses only the logs checked under its table.
    /// </summary>
    public sealed class CompareControl : UserControl
    {
        /// <summary>
        /// One side of the comparison: a fuel table, the readings of its checked logs, and the ECU
        /// temperature tables of its newest checked log.
        /// </summary>
        public sealed record Side(FuelTable Table, DataItem[] Data, int LogCount, int[] GasCoef, int[] ReducerCoef);

        /// <summary>Cells with fewer readings than this are left empty in the trim grids.</summary>
        public const int MinCellReadings = 3;

        /// <summary>|FAST| up to this (two 0.78% steps) counts as "on target".</summary>
        private const double OnTargetTrim = 1.56;

        private readonly Label _title = new() { Dock = DockStyle.Top, Height = 44, Font = new Font("Segoe UI", 10f, FontStyle.Bold), Padding = new Padding(6) };
        private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
        private readonly Font _boldFont = new("Segoe UI", 9f, FontStyle.Bold);

        public CompareControl()
        {
            Controls.Add(_tabs);
            Controls.Add(_title);
            _title.Text = "Check the logs of two fuel tables and press \"Analyze checked\" to compare them.";
        }

        public void Show(Side first, Side second)
        {
            // A = before, B = after.
            var (a, b) = first.Table.CreatedAt <= second.Table.CreatedAt ? (first, second) : (second, first);

            _title.Text = $"A (before): {a.Table.Name}, {a.Table.CreatedAt:yyyy-MM-dd}, {a.LogCount} log(s)      " +
                          $"B (after): {b.Table.Name}, {b.Table.CreatedAt:yyyy-MM-dd}, {b.LogCount} log(s)";

            foreach (TabPage page in _tabs.TabPages)
                page.Dispose();
            _tabs.TabPages.Clear();

            // Each side is placed in the cells of its own fuel table (axes in LPG.Core's Settings),
            // and its temperature reports use the tables its logs were recorded with.
            var cellsA = WithAxes(a.Table, () => CellTrims(a.Data));
            var tempA = WithAxes(a.Table, () => TemperatureCorrectionPrediction.Predict(a.Data, a.GasCoef, a.ReducerCoef));

            // B last, so the axes stay those of the newer table.
            var cellsB = WithAxes(b.Table, () => CellTrims(b.Data));
            var tempB = WithAxes(b.Table, () => TemperatureCorrectionPrediction.Predict(b.Data, b.GasCoef, b.ReducerCoef));

            AddPage("Summary", SummaryGrid(a, b, cellsA, cellsB));
            AddPage("Cell trims", CellTrimsPanel(a, b, cellsA, cellsB));
            AddPage("Fuel map change", MapChangePanel(a.Table, b.Table));
            AddPage("Temperature", Stack(
                ("LPG temperature bands: ECU table and the change each band still needs (%, trims after the map error, both tables fitted together)", BandGrid(tempA.Gas, tempB.Gas)),
                ("Reducer temperature bands: ECU table and the change each band still needs (%, trims after the map error, both tables fitted together)", BandGrid(tempA.Reducer, tempB.Reducer))));
        }

        private static T WithAxes<T>(FuelTable table, Func<T> calculate)
        {
            Settings.SetMapAxes(table.RpmAxis, table.InjectionAxis);
            return calculate();
        }

        private void AddPage(string title, Control content)
        {
            var page = new TabPage(title) { AutoScroll = true };
            content.Dock = DockStyle.Fill;
            page.Controls.Add(content);
            _tabs.TabPages.Add(page);
        }

        // ---------------- Summary ----------------

        private enum Better { None, Lower, LowerAbs, Higher }

        private DataGridView SummaryGrid(Side a, Side b, CellTrimMap cellsA, CellTrimMap cellsB)
        {
            var grid = NewGrid();
            grid.ColumnCount = 5;
            string[] headers = ["Report", "A (before)", "B (after)", "B - A", "Result"];
            for (int i = 0; i < headers.Length; i++)
                grid.Columns[i].HeaderText = headers[i];
            grid.Columns[0].DefaultCellStyle.Font = _boldFont;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            void Row(string name, Func<DataItem[], double> metric, Better better, string format = "0.00") =>
                Values(name,
                    a.Data.Length > 0 ? metric(a.Data) : double.NaN,
                    b.Data.Length > 0 ? metric(b.Data) : double.NaN,
                    better, format);

            void Values(string name, double va, double vb, Better better, string format = "0.00")
            {
                double diff = vb - va;

                string result = double.IsNaN(diff) || Math.Abs(diff) < 1e-9 || better == Better.None ? ""
                    : better switch
                    {
                        Better.Lower => diff < 0,
                        Better.Higher => diff > 0,
                        _ => Math.Abs(vb) < Math.Abs(va),
                    } ? "better" : "worse";

                int index = grid.Rows.Add(name, Format(va, format), Format(vb, format), Format(diff, "+" + format + ";-" + format), result);
                if (result != "")
                    Heatmap.Apply(grid.Rows[index].Cells[4], result == "better" ? Color.FromArgb(200, 235, 200) : Color.FromArgb(255, 200, 200));
            }

            Values("Log files", a.LogCount, b.LogCount, Better.None, "0");
            Row("OBD readings (groups)", d => d.Length, Better.None, "0");
            Row("Raw rows on LPG", d => d.Sum(x => x.Positions.Length), Better.None, "0");

            Row("FAST b1 median %", d => d.Select(x => x.FAST_b1).Median(), Better.LowerAbs);
            Row("FAST b2 median %", d => d.Select(x => x.FAST_b2).Median(), Better.LowerAbs);
            Row("FAST mean |trim| %", d => d.Average(x => (Math.Abs(x.FAST_b1) + Math.Abs(x.FAST_b2)) / 2), Better.Lower);
            Row($"Readings with |FAST| <= {OnTargetTrim}% (%)",
                d => 100.0 * d.Count(x => Math.Abs(x.FAST_b1) <= OnTargetTrim && Math.Abs(x.FAST_b2) <= OnTargetTrim) / d.Length, Better.Higher, "0.0");
            Row("SLOW b1 median %", d => d.Select(x => x.SLOW_b1).Median(), Better.LowerAbs);
            Row("SLOW b2 median %", d => d.Select(x => x.SLOW_b2).Median(), Better.LowerAbs);
            Row("Total trim b1 mean %", d => d.Average(x => x.Trim_b1), Better.LowerAbs);
            Row("Total trim b2 mean %", d => d.Average(x => x.Trim_b2), Better.LowerAbs);
            Row("Bank difference mean %", d => d.Average(x => x.TrimDiff), Better.LowerAbs);
            Row("AFR mean", d => d.Average(x => x.AFR), Better.None);

            Row("Reducer pressure min (bar)", d => d.Min(x => x.PRESS), Better.None);
            Row("Reducer pressure mean (bar)", d => d.Average(x => x.PRESS), Better.None);
            Row("LPG temperature min (°C)", d => d.Min(x => x.Temp_GAS), Better.None, "0");
            Row("LPG temperature max (°C)", d => d.Max(x => x.Temp_GAS), Better.None, "0");
            Row("Reducer temperature median (°C)", d => d.Select(x => x.Temp_RID).Median(), Better.None, "0");

            Values($"Cells with >= {MinCellReadings} readings", cellsA.Count(), cellsB.Count(), Better.None, "0");
            Values("Cell median |FAST| mean %", cellsA.MeanAbs(), cellsB.MeanAbs(), Better.Lower);

            grid.ClearSelection();
            return grid;
        }

        private static string Format(double v, string format) =>
            double.IsNaN(v) || double.IsInfinity(v) ? "" : v.ToString(format, CultureInfo.InvariantCulture);

        // ---------------- Cell trims ----------------

        /// <summary>Median FAST trim per map cell (average of the bank medians) and the number of readings.</summary>
        private sealed class CellTrimMap(double?[,] median, int[,] count)
        {
            public double?[,] Median { get; } = median;
            public int[,] Readings { get; } = count;

            public int Count() => Median.Cast<double?>().Count(v => v is not null);

            public double MeanAbs()
            {
                var values = Median.Cast<double?>().OfType<double>().ToArray();
                return values.Length == 0 ? double.NaN : values.Average(Math.Abs);
            }
        }

        /// <summary>
        /// Same cell placement as the prediction without its options: each bank in its own injection row,
        /// readings taken across cells (<see cref="FuelMapPrediction.MinGroupStability"/>) skipped.
        /// Uses the axes currently in <see cref="Settings"/>. Result is [injection row, rpm column].
        /// </summary>
        private static CellTrimMap CellTrims(DataItem[] data)
        {
            int rows = Settings.InjectionRanges.Length;
            int cols = Settings.RpmColumns.Length;

            var bank1 = new List<double>[rows, cols];
            var bank2 = new List<double>[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    bank1[r, c] = [];
                    bank2[r, c] = [];
                }

            foreach (var d in data)
            {
                if (d.Stability < FuelMapPrediction.MinGroupStability)
                    continue;

                int c = d.GetRpmIndex();
                if (c < 0)
                    continue;

                int r1 = d.GetInjectionIndex(static x => x.BENZ_b1);
                int r2 = d.GetInjectionIndex(static x => x.BENZ_b2);
                if (r1 >= 0) bank1[r1, c].Add(d.FAST_b1);
                if (r2 >= 0) bank2[r2, c].Add(d.FAST_b2);
            }

            var median = new double?[rows, cols];
            var count = new int[rows, cols];

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    count[r, c] = bank1[r, c].Count + bank2[r, c].Count;
                    if (count[r, c] < MinCellReadings)
                        continue;

                    var medians = new[] { bank1[r, c], bank2[r, c] }.Where(l => l.Count > 0).Select(l => l.Median()).ToArray();
                    median[r, c] = medians.Average();
                }
            }

            return new CellTrimMap(median, count);
        }

        private Control CellTrimsPanel(Side a, Side b, CellTrimMap ta, CellTrimMap tb)
        {
            var gridA = ("A (before): median FAST % per cell (blue = rich, red = lean); tooltip = readings",
                (Control)MatrixGrid(ta.Median, a.Table.RpmAxis, a.Table.InjectionAxis, ta.Readings));
            var gridB = ("B (after): median FAST % per cell (blue = rich, red = lean); tooltip = readings",
                (Control)MatrixGrid(tb.Median, b.Table.RpmAxis, b.Table.InjectionAxis, tb.Readings));

            int rows = ta.Median.GetLength(0);
            int cols = ta.Median.GetLength(1);

            if (rows != tb.Median.GetLength(0) || cols != tb.Median.GetLength(1))
                return Stack(("The two fuel tables have different sizes: no cell-by-cell change.", new Panel { Height = 4 }), gridA, gridB);

            // |B| - |A|: negative = the cell got closer to 0 (better).
            var change = new double?[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    if (ta.Median[r, c] is double va && tb.Median[r, c] is double vb)
                        change[r, c] = Math.Abs(vb) - Math.Abs(va);

            string axesNote = a.Table.RpmAxis.SequenceEqual(b.Table.RpmAxis) && a.Table.InjectionAxis.SequenceEqual(b.Table.InjectionAxis)
                ? "" : "  (the axes differ; cells are matched by position, the axes of B are shown)";

            return Stack(
                ("Change of |median FAST| per cell, |B| - |A| (blue = closer to 0 = better, red = worse)" + axesNote,
                    MatrixGrid(change, b.Table.RpmAxis, b.Table.InjectionAxis, null)),
                gridA,
                gridB);
        }

        // ---------------- Fuel map change ----------------

        private Control MapChangePanel(FuelTable a, FuelTable b)
        {
            if (a.RpmAxis.Length != b.RpmAxis.Length || a.InjectionAxis.Length != b.InjectionAxis.Length)
                return new Label { Text = "The two fuel tables have different sizes and cannot be compared cell by cell.", Padding = new Padding(10) };

            int rows = b.InjectionAxis.Length;
            int cols = b.RpmAxis.Length;

            var diff = new double?[rows, cols];
            var percent = new double?[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    diff[r, c] = b.Values[r][c] - a.Values[r][c];
                    percent[r, c] = a.Values[r][c] != 0 ? (b.Values[r][c] / a.Values[r][c] - 1) * 100 : null;
                }

            string axesNote = a.RpmAxis.SequenceEqual(b.RpmAxis) && a.InjectionAxis.SequenceEqual(b.InjectionAxis)
                ? "" : "  (the axes differ; the axes of B are shown)";

            return Stack(
                ("Fuel map B - A (red = more gas, blue = less)" + axesNote, MatrixGrid(diff, b.RpmAxis, b.InjectionAxis, null, "+0;-0;0")),
                ("Fuel map change in % of A", MatrixGrid(percent, b.RpmAxis, b.InjectionAxis, null, "+0.0;-0.0;0")));
        }

        // ---------------- Temperature ----------------

        private DataGridView BandGrid(IReadOnlyList<TemperatureBandSuggestion> a, IReadOnlyList<TemperatureBandSuggestion> b)
        {
            var grid = NewGrid();
            grid.ColumnCount = 9;
            string[] headers = ["Band", "A table", "A change %", "A readings", "B table", "B change %", "B readings", "|B| - |A|", "Result"];
            for (int i = 0; i < headers.Length; i++)
                grid.Columns[i].HeaderText = headers[i];
            grid.Columns[0].DefaultCellStyle.Font = _boldFont;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            for (int i = 0; i < a.Count; i++)
            {
                double? change = a[i].Error is double ea && b[i].Error is double eb ? Math.Abs(eb) - Math.Abs(ea) : null;
                string result = change is double ch && Math.Abs(ch) > 1e-9 ? (ch < 0 ? "better" : "worse") : "";

                int index = grid.Rows.Add(a[i].Label,
                    a[i].Current.ToString("+0;-0;0", CultureInfo.InvariantCulture),
                    a[i].Error?.ToString("+0.00;-0.00;0", CultureInfo.InvariantCulture) ?? "", a[i].Count,
                    b[i].Current.ToString("+0;-0;0", CultureInfo.InvariantCulture),
                    b[i].Error?.ToString("+0.00;-0.00;0", CultureInfo.InvariantCulture) ?? "", b[i].Count,
                    change?.ToString("+0.00;-0.00;0", CultureInfo.InvariantCulture) ?? "", result);

                // The ECU table value changed between A and B.
                if (a[i].Current != b[i].Current)
                    Heatmap.Apply(grid.Rows[index].Cells[4], Color.FromArgb(255, 240, 150));

                if (result != "")
                    Heatmap.Apply(grid.Rows[index].Cells[8], result == "better" ? Color.FromArgb(200, 235, 200) : Color.FromArgb(255, 200, 200));
            }

            grid.ClearSelection();
            FitHeight(grid);
            grid.Dock = DockStyle.Top;
            return grid;
        }

        // ---------------- Grids ----------------

        private DataGridView NewGrid() => new()
        {
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            BackgroundColor = SystemColors.Window,
            ColumnHeadersDefaultCellStyle = { Font = _boldFont },
        };

        /// <summary>
        /// Grid of a [row, column] matrix with the axes as headers, coloured blue-white-red around 0
        /// as in LPGDataAnalyzer (square-root scaled by the largest absolute value).
        /// </summary>
        private DataGridView MatrixGrid(double?[,] values, double[] rpmAxis, double[] injAxis, int[,]? readings, string format = "+0.00;-0.00;0")
        {
            var grid = NewGrid();
            grid.RowHeadersVisible = true;
            grid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            grid.RowHeadersDefaultCellStyle.Font = _boldFont;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.ScrollBars = ScrollBars.None;

            int rows = values.GetLength(0);
            int cols = values.GetLength(1);

            for (int c = 0; c < cols; c++)
                grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = c < rpmAxis.Length ? rpmAxis[c].ToString("0", CultureInfo.InvariantCulture) : "",
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                });

            double maxAbs = values.Cast<double?>().OfType<double>().Select(Math.Abs).DefaultIfEmpty(0).Max();
            if (maxAbs < 1e-12)
                maxAbs = 1e-12;

            for (int r = 0; r < rows; r++)
            {
                int index = grid.Rows.Add();
                var row = grid.Rows[index];
                row.HeaderCell.Value = r < injAxis.Length ? injAxis[r].ToString("0.00", CultureInfo.InvariantCulture) : "";

                for (int c = 0; c < cols; c++)
                {
                    var cell = row.Cells[c];

                    if (values[r, c] is double v)
                    {
                        cell.Value = v.ToString(format, CultureInfo.InvariantCulture);
                        double n = v / maxAbs;
                        Heatmap.Apply(cell, Heatmap.Diverging(Math.Sign(n) * Math.Sqrt(Math.Abs(n))));
                    }
                    else
                    {
                        Heatmap.Apply(cell, Heatmap.Empty);
                    }

                    if (readings is not null)
                        cell.ToolTipText = $"{readings[r, c]} reading(s)";
                }
            }

            grid.ClearSelection();
            FitHeight(grid);
            grid.Dock = DockStyle.Top;
            return grid;
        }

        /// <summary>Titled grids one under another, scrolling as a whole.</summary>
        private Control Stack(params (string Title, Control Content)[] parts)
        {
            var panel = new TableLayoutPanel { ColumnCount = 1, AutoScroll = true, Padding = new Padding(6) };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            foreach (var (title, content) in parts)
            {
                panel.Controls.Add(new Label { Text = title, AutoSize = true, Font = _boldFont, Padding = new Padding(0, 10, 0, 2) });
                content.Dock = DockStyle.Top;
                panel.Controls.Add(content);
            }

            return panel;
        }

        private static void FitHeight(DataGridView grid)
        {
            int height = grid.Rows.GetRowsHeight(DataGridViewElementStates.Visible) + grid.ColumnHeadersHeight + 4;
            grid.Height = height;
            grid.MinimumSize = new Size(0, height);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _boldFont.Dispose();
            base.Dispose(disposing);
        }
    }
}
