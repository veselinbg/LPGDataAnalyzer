using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Models.Common;
using LPGDataAnalyzer.Services;

namespace LPGDataAnalyzer.Controls
{
    public partial class Statsistics : Form
    {
        private readonly Dictionary<string, (double Min, double Max)> _columnRanges = new();
        private readonly double? _valFactor;

        public Statsistics(List<DataItem> data, double? valFactor)
        {
            InitializeComponent();

            _valFactor = valFactor;

            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Width = 1000;
            Height = 550;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            Icon = SystemIcons.Information;

            var overall = BuildOverall(data);

            var label = new Label
            {
                Dock = DockStyle.Top,
                Height = 50,
                Text =
                    $"OVERALL → PRESS Min: {overall.MinPress:F2}  Avg: {overall.AvgPress:F2}  Max: {overall.MaxPress:F2} | " +
                    $"MAP Min: {overall.MinMap:F2}  Avg: {overall.AvgMap:F2}  Max: {overall.MaxMap:F2}",
                TextAlign = ContentAlignment.MiddleCenter
            };

            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoGenerateColumns = false,
                DataSource = new SortableBindingList<DataItem>(data)
            };

            grid.DataBindingComplete += (s, e) =>
            {
                CreateColumns(grid);
                AddValColumn(grid, data);
            };

            grid.CellFormatting += Grid_CellFormatting;

            Controls.Add(grid);
            Controls.Add(label);
        }
        private void CreateColumns(DataGridView grid)
        {
            if (grid.Columns.Count > 0)
                return;

            grid.Columns.Add(CreateCol(nameof(DataItem.SLOW_b1), "Slow B1"));
            grid.Columns.Add(CreateCol(nameof(DataItem.FAST_b1), "Fast B1"));
            grid.Columns.Add(CreateCol(nameof(DataItem.SLOW_b2), "Slow B2"));
            grid.Columns.Add(CreateCol(nameof(DataItem.FAST_b2), "Fast B2"));

            grid.Columns.Add(CreateCol(nameof(DataItem.Fast), "Fast"));

            grid.Columns.Add(CreateCol(nameof(DataItem.PRESS), "PRESS"));
            grid.Columns.Add(CreateCol(nameof(DataItem.MAP), "MAP"));

            grid.Columns.Add(CreateCol(nameof(DataItem.Temp_GAS), "Gas Temp"));
            grid.Columns.Add(CreateCol(nameof(DataItem.Temp_RID), "Reducer Temp"));

            grid.Columns.Add(CreateCol(nameof(DataItem.Trim), "Trim"));

            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (col.ValueType == typeof(double) ||
                    col.ValueType == typeof(double?))
                {
                    col.DefaultCellStyle.Format = "F2";
                }
            }
        }

        private DataGridViewTextBoxColumn CreateCol(string prop, string header)
        {
            return new DataGridViewTextBoxColumn
            {
                DataPropertyName = prop,
                Name = prop,
                HeaderText = header,
                ReadOnly = true
            };
        }
        private void AddValColumn(DataGridView grid, List<DataItem> data)
        {
            if (!grid.Columns.Contains("Val"))
            {
                grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "Val",
                    HeaderText = "Val",
                    ReadOnly = true
                });
            }

            var valCache = data
                .Select(x => _valFactor.SafeMultiply(
                    FuelMapPrediction.TrimCalculation(x.Trim, 0, true)
                )?.Round())
                .ToList();

            grid.CellFormatting += (s, e) =>
            {
                if (grid.Columns[e.ColumnIndex].Name != "Val")
                    return;

                e.Value = valCache[e.RowIndex];
            };

            var values = valCache
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            if (values.Count > 0)
                _columnRanges["Val"] = (values.Min(), values.Max());
        }       
        // --------------------------
        // HEATMAP
        // --------------------------
        private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (sender is not DataGridView grid)
                return;

            var name = grid.Columns[e.ColumnIndex].Name;

            if (name == "Val")
                return;

            if (!_columnRanges.TryGetValue(name, out var range))
                return;

            if (e.Value == null || !e.Value.TryGetDouble(out double value))
                return;

            if (Math.Abs(range.Max - range.Min) < 0.00001)
                return;

            double normalized = (value - range.Min) / (range.Max - range.Min);
            double diverging = normalized * 2 - 1;

            var color = ColorHelper.InterpolateDiverging(diverging);

            e.CellStyle.BackColor = color;
            e.CellStyle.ForeColor = GetContrastColor(color);

            if (name == nameof(DataItem.Fast))
                e.CellStyle.Font = ColorHelper.BoldFont;
        }        

        // --------------------------
        // OVERALL STATS
        // --------------------------
        private OverallStatistic BuildOverall(List<DataItem> data)
        {
            return new OverallStatistic
            {
                MinPress = data.Min(x => x.PRESS).Round(),
                AvgPress = data.Average(x => x.PRESS).Round(),
                MaxPress = data.Max(x => x.PRESS).Round(),

                MinMap = data.Min(x => x.MAP).Round(),
                AvgMap = data.Average(x => x.MAP).Round(),
                MaxMap = data.Max(x => x.MAP).Round()
            };
        }

        private static Color GetContrastColor(Color bg)
        {
            double lum =
                (0.299 * bg.R +
                 0.587 * bg.G +
                 0.114 * bg.B) / 255.0;

            return lum > 0.6 ? Color.Black : Color.White;
        }
    }

    public class OverallStatistic
    {
        public double MinPress { get; set; }
        public double AvgPress { get; set; }
        public double MaxPress { get; set; }

        public double MinMap { get; set; }
        public double AvgMap { get; set; }
        public double MaxMap { get; set; }
    }
}