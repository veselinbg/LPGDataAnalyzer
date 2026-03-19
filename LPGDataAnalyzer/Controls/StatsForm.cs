using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Models.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LPGDataAnalyzer.Controls
{
    public partial class StatsForm : Form
    {
        public StatsForm(List<DataItem> data)
        {
            InitializeComponent();

            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Width = 900;
            this.Height = 550;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            
            this.Icon = SystemIcons.Information;

            var (stats, overall) = BuildStats(data);

            var label = new Label
            {
                Dock = DockStyle.Top,
                Height = 50,
                Text = $"OVERALL → PRESS Min: {overall.MinPress:F2}  Avg: {overall.AvgPress:F2}  Max: {overall.MaxPress:F2} | MAP Min: {overall.MinMap:F2}  Avg: {overall.AvgMap:F2}  Max: {overall.MaxMap:F2}",
                TextAlign = ContentAlignment.MiddleCenter
            };
            var statsList = new SortableBindingList<GroupStat>(stats);

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

            grid.DataBindingComplete += (s, e) => ConfigureGrid(grid);

            Controls.Add(grid);
            Controls.Add(label);
        }
        private void ConfigureGrid(DataGridView grid)
        {
            void Set(string name, string header)
            {
                if (grid.Columns.Contains(name))
                    grid.Columns[name].HeaderText = header;
            }

            Set(nameof(GroupStat.Slow_b1), "Slow B1");
            Set(nameof(GroupStat.Fast_b1), "Fast B1");
            Set(nameof(GroupStat.Slow_b2), "Slow B2");
            Set(nameof(GroupStat.Fast_b2), "Fast B2");

            Set(nameof(GroupStat.Trim), "Trim (Avg)");
            Set(nameof(GroupStat.Count), "Count");

            Set(nameof(GroupStat.MinPress), "Min PRESS");
            Set(nameof(GroupStat.AvgPress), "Avg PRESS");
            Set(nameof(GroupStat.MaxPress), "Max PRESS");

            Set(nameof(GroupStat.MinMap), "Min MAP");
            Set(nameof(GroupStat.AvgMap), "Avg MAP");
            Set(nameof(GroupStat.MaxMap), "Max MAP");

            // format numbers
            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (col.ValueType == typeof(double))
                    col.DefaultCellStyle.Format = "F2";
            }
        }
        private (List<GroupStat>, OverallStat) BuildStats(List<DataItem> data)
        {
            var stats = data
                .GroupBy(x => new
                {
                    Slow_b1 = x.SLOW_b1.Round(),
                    Fast_b1 = x.FAST_b1.Round(),
                    Slow_b2 = x.SLOW_b2.Round(),
                    Fast_b2 = x.FAST_b2.Round()
                })
                .Select(g => new GroupStat
                {
                    Slow_b1 = g.Key.Slow_b1,
                    Fast_b1 = g.Key.Fast_b1,
                    Slow_b2 = g.Key.Slow_b2,
                    Fast_b2 = g.Key.Fast_b2,

                    Trim = g.Avg(x => x.Trim).Round(),

                    Count = g.Count(),

                    MinPress = g.Min(x => x.PRESS).Round(),
                    AvgPress = g.Avg(x => x.PRESS).Round(),
                    MaxPress = g.Max(x => x.PRESS).Round(),

                    MinMap = g.Min(x => x.MAP).Round(),
                    AvgMap = g.Avg(x => x.MAP).Round(),
                    MaxMap = g.Max(x => x.MAP).Round()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            var overall = new OverallStat
            {
                MinPress = data.Min(x => x.PRESS).Round(),
                AvgPress = data.Avg(x => x.PRESS).Round(),
                MaxPress = data.Max(x => x.PRESS).Round(),

                MinMap = data.Min(x => x.MAP).Round(),
                AvgMap = data.Avg(x => x.MAP).Round(),
                MaxMap = data.Max(x => x.MAP).Round()
            };

            return (stats, overall);
        }
    }

    public class GroupStat
    {
        public int Count { get; set; }

        public double Slow_b1 { get; set; }
        public double Fast_b1 { get; set; }
        public double Slow_b2 { get; set; }
        public double Fast_b2 { get; set; }

        public double Trim { get; set; }

        public double MinPress { get; set; }
        public double AvgPress { get; set; }
        public double MaxPress { get; set; }

        public double MinMap { get; set; }
        public double AvgMap { get; set; }
        public double MaxMap { get; set; }
    }
    public class OverallStat
    {
        public double MinPress { get; set; }
        public double AvgPress { get; set; }
        public double MaxPress { get; set; }

        public double MinMap { get; set; }
        public double AvgMap { get; set; }
        public double MaxMap { get; set; }
    }
}