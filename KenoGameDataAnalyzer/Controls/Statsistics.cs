using KenoGameDataAnalyzer;
using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Models.Common;
using LPGDataAnalyzer.Services;

namespace LPGDataAnalyzer.Controls
{
    public partial class Statsistics : Form
    {
        private Dictionary<string, (double min, double max)> _columnRanges;
        public Statsistics(List<DataItem> data, double? value)
        {
            InitializeComponent();

            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Width = 1000;
            this.Height = 550;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            
            this.Icon = SystemIcons.Information;

            var (stats, overall) = BuildStats(data, value);

            var label = new Label
            {
                Dock = DockStyle.Top,
                Height = 50,
                Text = $"OVERALL → PRESS Min: {overall.MinPress:F2}  Avg: {overall.AvgPress:F2}  Max: {overall.MaxPress:F2} | MAP Min: {overall.MinMap:F2}  Avg: {overall.AvgMap:F2}  Max: {overall.MaxMap:F2}",
                TextAlign = ContentAlignment.MiddleCenter
            };
            var statsList = new SortableBindingList<GroupStatsistic>(stats);
            BuildColumnRanges(stats);
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoGenerateColumns = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                DataSource = statsList
            };
            grid.CellFormatting += (s, e) => ApplyHeatmap(grid, e);
            grid.DataBindingComplete += (s, e) => ConfigureGrid(grid);

            Controls.Add(grid);
            Controls.Add(label);

        }
        private void ApplyHeatmap(DataGridView grid, DataGridViewCellFormattingEventArgs e)
        {
            var column = grid.Columns[e.ColumnIndex];
            var name = column.DataPropertyName;

            if (name == nameof(GroupStatsistic.Count))
                return;

            if (!_columnRanges.ContainsKey(name))
                return;

            if (e.Value == null || !e.Value.TryGetDouble( out double val))
                return;

            var (min, max) = _columnRanges[name];

            if (Math.Abs(max - min) < 0.0001)
                return;

            // Normalize to 0..1
            double normalized = (val - min) / (max - min);

            // Convert to -1..1 for diverging
            double diverging = normalized * 2 - 1;

            var color = ColorHelper.InterpolateDiverging(diverging);

            e.CellStyle.BackColor = color;

            // Optional: improve readability
            e.CellStyle.ForeColor = GetContrastColor(color);

            if (name == nameof(GroupStatsistic.Fast) || name == nameof(GroupStatsistic.Val))
            {
                e.CellStyle.Font = ColorHelper.BoldFont;
            }
        }
        private Color GetContrastColor(Color bg)
        {
            double luminance = (0.299 * bg.R + 0.587 * bg.G + 0.114 * bg.B) / 255;

            return luminance > 0.6 ? Color.Black : Color.White;
        }
        private void BuildColumnRanges(List<GroupStatsistic> stats)
        {
            _columnRanges = new Dictionary<string, (double, double)>();

            void Add(string name, Func<GroupStatsistic, double?> selector)
            {
                var values = stats.Select(selector)
                                  .Where(v => v.HasValue)
                                  .Select(v => v.Value)
                                  .ToList();

                if (values.Any())
                    _columnRanges[name] = (values.Min(), values.Max());
            }

            Add(nameof(GroupStatsistic.Fast), x => x.Fast);
            Add(nameof(GroupStatsistic.Count), x => x.Count);

            Add(nameof(GroupStatsistic.MinPress), x => x.MinPress);
            Add(nameof(GroupStatsistic.AvgPress), x => x.AvgPress);
            Add(nameof(GroupStatsistic.MaxPress), x => x.MaxPress);

            Add(nameof(GroupStatsistic.MinMap), x => x.MinMap);
            Add(nameof(GroupStatsistic.AvgMap), x => x.AvgMap);
            Add(nameof(GroupStatsistic.MaxMap), x => x.MaxMap);

            Add(nameof(GroupStatsistic.Temp_GAS), x => x.Temp_GAS);
            Add(nameof(GroupStatsistic.Temp_RID), x => x.Temp_RID);

            Add(nameof(GroupStatsistic.Val), x => x.Val);
        }
        private void ConfigureGrid(DataGridView grid)
        {
            void Set(string name, string header)
            {
                if (grid.Columns.Contains(name))
                    grid.Columns[name].HeaderText = header;
            }

            Set(nameof(GroupStatsistic.Slow_b1), "Slow B1");
            Set(nameof(GroupStatsistic.Fast_b1), "Fast B1");
            Set(nameof(GroupStatsistic.Slow_b2), "Slow B2");
            Set(nameof(GroupStatsistic.Fast_b2), "Fast B2");

            Set(nameof(GroupStatsistic.Fast), "Fast (Avg)");
            Set(nameof(GroupStatsistic.Count), "Count");

            Set(nameof(GroupStatsistic.MinPress), "Min PRESS");
            Set(nameof(GroupStatsistic.AvgPress), "Avg PRESS");
            Set(nameof(GroupStatsistic.MaxPress), "Max PRESS");

            Set(nameof(GroupStatsistic.MinMap), "Min MAP");
            Set(nameof(GroupStatsistic.AvgMap), "Avg MAP");
            Set(nameof(GroupStatsistic.MaxMap), "Max MAP");

            // format numbers
            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (col.ValueType == typeof(double))
                    col.DefaultCellStyle.Format = "F2";
            }
        }
        private (List<GroupStatsistic>, OverallStatsistic) BuildStats(List<DataItem> data, double? value)
        {
            var stats = data
                .GroupBy(x => new
                {
                    Slow_b1 = x.SLOW_b1.Round(),
                    Fast_b1 = x.FAST_b1.Round(),
                    Slow_b2 = x.SLOW_b2.Round(),
                    Fast_b2 = x.FAST_b2.Round()
                })
                .Select(g => new GroupStatsistic
                {
                    Slow_b1 = g.Key.Slow_b1,
                    Fast_b1 = g.Key.Fast_b1,
                    Slow_b2 = g.Key.Slow_b2,
                    Fast_b2 = g.Key.Fast_b2,

                    Fast = g.Average(x => x.Fast).Round(),

                    Count = g.Count(),

                    MinPress = g.Min(x => x.PRESS).Round(),
                    AvgPress = g.Average(x => x.PRESS).Round(),
                    MaxPress = g.Max(x => x.PRESS).Round(),

                    MinMap = g.Min(x => x.MAP).Round(),
                    AvgMap = g.Average(x => x.MAP).Round(),
                    MaxMap = g.Max(x => x.MAP).Round(),
                    Temp_GAS = g.Average(x => x.Temp_GAS).Round(),
                    Temp_RID = g.Average(x => x.Temp_RID).Round(), 
                    Val = value.SafeMultiply(FuelMapPrediction.TrimCalculation(g.Average(x => x.Trim), 0, true))?.Round()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            var overall = new OverallStatsistic
            {
                MinPress = data.Min(x => x.PRESS).Round(),
                AvgPress = data.Average(x => x.PRESS).Round(),
                MaxPress = data.Max(x => x.PRESS).Round(),

                MinMap = data.Min(x => x.MAP).Round(),
                AvgMap = data.Average(x => x.MAP).Round(),
                MaxMap = data.Max(x => x.MAP).Round()
            };

            return (stats, overall);
        }
    }

    public class GroupStatsistic
    {
        public int Count { get; set; }

        public double Slow_b1 { get; set; }
        public double Fast_b1 { get; set; }
        public double Slow_b2 { get; set; }
        public double Fast_b2 { get; set; }

        public double Fast { get; set; }

        public double MinPress { get; set; }
        public double AvgPress { get; set; }
        public double MaxPress { get; set; }

        public double MinMap { get; set; }
        public double AvgMap { get; set; }
        public double MaxMap { get; set; }
        public double Temp_GAS { get; set; }
        public double Temp_RID { get; set; }
        public double? Val { get; set; }
    }
    public class OverallStatsistic
    {
        public double MinPress { get; set; }
        public double AvgPress { get; set; }
        public double MaxPress { get; set; }

        public double MinMap { get; set; }
        public double AvgMap { get; set; }
        public double MaxMap { get; set; }
    }
}