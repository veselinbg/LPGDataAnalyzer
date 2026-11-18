using LPGDataAnalyzer.Services;
using System.Reflection;
using static LPGDataAnalyzer.Services.FuelMapPrediction;

namespace LPGDataAnalyzer.Controls
{
    /// <summary>
    /// The readings a prediction did not use (Prediction screen, bottom left): a "Reason" column, a filter by
    /// reason with counts, and only the columns that explain a skip.
    /// <para>Kept fast for thousands of rows: the reason is an enum from <see cref="FuelMapPrediction.BuildTable"/>,
    /// rows are a plain bound list (no per-cell writes) and no column is auto-sized to its contents.</para>
    /// </summary>
    public sealed class SkippedReadingsView : UserControl
    {
        private readonly ComboBox _filter;
        private readonly DataGridView _grid;
        private List<SkippedRow> _rows = [];

        public SkippedReadingsView()
        {
            _filter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 360,
                Margin = new Padding(3, 3, 3, 3),
            };
            _filter.SelectedIndexChanged += (_, _) => Bind();

            var label = new Label
            {
                Text = "Skipped readings:",
                AutoSize = true,
                Margin = new Padding(3, 7, 3, 0),
            };

            var top = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0),
            };
            top.Controls.Add(label);
            top.Controls.Add(_filter);

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            };
            // Not public, but the usual way to stop a large grid from flickering while scrolling.
            typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(_grid, true);
            _grid.DataBindingComplete += (_, _) => SetColumnWidths();

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.Controls.Add(top, 0, 0);
            layout.Controls.Add(_grid, 0, 1);

            Controls.Add(layout);
        }

        /// <summary>Shows the skipped readings of a prediction; <paramref name="maxBenzDiff"/> is only used in the reason text.</summary>
        public void ShowReadings(IReadOnlyList<RejectedReading> rejected, double maxBenzDiff)
        {
            _rows = new List<SkippedRow>(rejected.Count);
            foreach (var r in rejected)
                _rows.Add(new SkippedRow(r, ReasonText(r.Reason, maxBenzDiff)));

            var items = new List<FilterItem> { new(null, $"All reasons ({_rows.Count})") };
            foreach (var group in rejected.GroupBy(static r => r.Reason).OrderByDescending(static g => g.Count()))
                items.Add(new FilterItem(group.Key, $"{ReasonText(group.Key, maxBenzDiff)} ({group.Count()})"));

            _filter.BeginUpdate();
            _filter.Items.Clear();
            _filter.Items.AddRange([.. items]);
            _filter.EndUpdate();
            _filter.SelectedIndex = 0; // binds
        }

        public void Clear()
        {
            _rows = [];
            _filter.Items.Clear();
            _grid.DataSource = null;
        }

        private void Bind()
        {
            var reason = (_filter.SelectedItem as FilterItem)?.Reason;
            _grid.DataSource = null;
            _grid.DataSource = reason is null ? _rows : _rows.Where(r => r.ReasonCode == reason).ToList();
        }

        private void SetColumnWidths()
        {
            foreach (DataGridViewColumn column in _grid.Columns)
                column.Width = column.DataPropertyName switch
                {
                    nameof(SkippedRow.Reason) => 230,
                    nameof(SkippedRow.File) => 120,
                    _ => 62,
                };

            if (_grid.Columns.Count > 0)
                _grid.Columns[0].Frozen = true;
        }

        private static string ReasonText(RejectReason reason, double maxBenzDiff) => reason switch
        {
            RejectReason.BankDifference => $"Bank difference |BENZ_Diff| ≥ {maxBenzDiff:0.#} %",
            RejectReason.NotRegulating => "Not regulating (FAST 0 on both banks)",
            RejectReason.LoadTransient => $"Load transient (BENZ changes > {MaxLoadChangeRatio:0.#}×)",
            RejectReason.NoDelayedRows => "No rows a trim delay earlier (file start or gap)",
            RejectReason.OutsideMap => "Outside the map axes",
            RejectReason.SpreadOverCells => $"Spread over cells (Stability < {MinGroupStability:0.##})",
            _ => reason.ToString(),
        };

        private sealed record FilterItem(RejectReason? Reason, string Text)
        {
            public override string ToString() => Text;
        }

        /// <summary>One grid row: the reason and the reading's values that explain it.</summary>
        private sealed class SkippedRow(RejectedReading rejected, string reason)
        {
            internal RejectReason ReasonCode { get; } = rejected.Reason;

            public string Reason { get; } = reason;
            public string? File { get; } = rejected.Item.SourceFile;
            public int TEMPO { get; } = rejected.Item.TEMPO;
            public int TimeMs { get; } = rejected.Item.TimeMs;
            public int RPM { get; } = rejected.Item.RPM;
            public double BENZ_b1 { get; } = rejected.Item.BENZ_b1;
            public double BENZ_b2 { get; } = rejected.Item.BENZ_b2;
            public double BENZ_Diff { get; } = rejected.Item.BENZ_Diff;
            /// <summary>Largest / smallest BENZ over the reading's rows and delayed rows.</summary>
            public double LoadRatio { get; } = Math.Round(LoadChangeRatio(rejected.Item), 2);
            public double FAST_b1 { get; } = rejected.Item.FAST_b1;
            public double FAST_b2 { get; } = rejected.Item.FAST_b2;
            public double SLOW_b1 { get; } = rejected.Item.SLOW_b1;
            public double SLOW_b2 { get; } = rejected.Item.SLOW_b2;
            public double PRESS { get; } = rejected.Item.PRESS;
            public double Temp_RID { get; } = rejected.Item.Temp_RID;
            public double Temp_GAS { get; } = rejected.Item.Temp_GAS;
            public double Stability { get; } = rejected.Item.Stability;
        }
    }
}
