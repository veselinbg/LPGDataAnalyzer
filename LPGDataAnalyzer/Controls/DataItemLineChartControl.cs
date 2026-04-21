using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;
using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer.Controls
{
    public class DataItemLineChartControl : UserControl
    {
        private readonly ComboBox cmbX = new();
        private readonly FlowLayoutPanel pnlY = new();
        private readonly Chart chart = new();

        private readonly Button btnBuild = new() { Text = "Build" };
        private readonly Button btnZoomIn = new() { Text = "+" };
        private readonly Button btnZoomOut = new() { Text = "-" };
        private readonly Button btnReset = new() { Text = "Reset" };

        private readonly Dictionary<string, Func<DataItem, double>> _cache = new();
        private DataItem[] _data = Array.Empty<DataItem>();

        // cached transformed dataset (IMPORTANT optimization)
        private (double x, double[] y)[] _cacheData = Array.Empty<(double, double[])>();
        private string[] _yFields = Array.Empty<string>();

        public DataItemLineChartControl()
        {
            BuildLayout();
            InitChart();
            LoadFields();
        }

        public void SetData(DataItem[] data)
        {
            _data = data ?? Array.Empty<DataItem>();
            _cacheData = Array.Empty<(double, double[])>();
        }

        // ---------------- LAYOUT ----------------

        private void BuildLayout()
        {
            Dock = DockStyle.Fill;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2
            };

            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Controls.Add(root);

            var top = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3
            };

            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));

            cmbX.Dock = DockStyle.Fill;

            pnlY.Dock = DockStyle.Fill;
            pnlY.WrapContents = true;
            pnlY.AutoScroll = true;

            var btnPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill
            };

            btnPanel.Controls.AddRange(new Control[]
            {
                btnBuild, btnZoomIn, btnZoomOut, btnReset
            });

            btnBuild.Click += (_, _) => BuildChart();
            btnZoomIn.Click += (_, _) => Zoom(0.7);
            btnZoomOut.Click += (_, _) => Zoom(1.3);
            btnReset.Click += (_, _) => ResetZoom();

            top.Controls.Add(cmbX, 0, 0);
            top.Controls.Add(pnlY, 1, 0);
            top.Controls.Add(btnPanel, 2, 0);

            chart.Dock = DockStyle.Fill;

            root.Controls.Add(top, 0, 0);
            root.Controls.Add(chart, 0, 1);
        }

        // ---------------- CHART ----------------

        private void InitChart()
        {
            var area = new ChartArea("Main");

            area.AxisX.ScaleView.Zoomable = true;
            area.AxisY.ScaleView.Zoomable = true;

            area.CursorX.IsUserEnabled = true;
            area.CursorX.IsUserSelectionEnabled = true;

            chart.ChartAreas.Add(area);

            chart.Legends.Add(new Legend("Legend")
            {
                Docking = Docking.Right
            });

            chart.AxisViewChanged += (_, __) => RecalculateYScale();
        }

        // ---------------- FIELD UI ----------------

        private void LoadFields()
        {
            var fields = typeof(DataItem)
                .GetProperties()
                .Where(p => p.PropertyType == typeof(int) || p.PropertyType == typeof(double))
                .Select(p => p.Name)
                .ToArray();

            cmbX.Items.AddRange(fields);

            if (fields.Length > 0)
                cmbX.SelectedIndex = 0;

            pnlY.Controls.Clear();

            foreach (var f in fields)
            {
                var b = new CheckBox
                {
                    Text = f,
                    Appearance = Appearance.Button,
                    AutoSize = true,
                    Checked = false,
                    Margin = new Padding(3)
                };

                b.CheckedChanged += (_, _) =>
                {
                    b.BackColor = b.Checked
                        ? Color.LightSteelBlue
                        : SystemColors.Control;
                };

                pnlY.Controls.Add(b);
            }
        }

        // ---------------- REFLECTION ----------------

        private Func<DataItem, double> Get(string name)
        {
            if (_cache.TryGetValue(name, out var fn))
                return fn;

            var p = Expression.Parameter(typeof(DataItem));
            var body = Expression.Convert(Expression.Property(p, name), typeof(double));

            fn = Expression.Lambda<Func<DataItem, double>>(body, p).Compile();
            _cache[name] = fn;

            return fn;
        }

        // ---------------- BUILD ----------------

        private void BuildChart()
        {
            if (_data.Length == 0 || cmbX.SelectedItem == null)
                return;

            _yFields = pnlY.Controls
                .OfType<CheckBox>()
                .Where(b => b.Checked)
                .Select(b => b.Text)
                .ToArray();

            if (_yFields.Length == 0)
                return;

            string xName = cmbX.SelectedItem.ToString()!;
            var getX = Get(xName);

            var getYs = _yFields.Select(Get).ToArray();

            chart.Series.Clear();

            _cacheData = new (double, double[])[_data.Length];

            double minY = double.MaxValue;
            double maxY = double.MinValue;

            for (int i = 0; i < _data.Length; i++)
            {
                var d = _data[i];
                double x = getX(d);

                double[] yvals = new double[_yFields.Length];

                for (int j = 0; j < getYs.Length; j++)
                {
                    double y = getYs[j](d);
                    yvals[j] = y;

                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }

                _cacheData[i] = (x, yvals);
            }

            var colors = new[]
            {
                Color.Blue, Color.Red, Color.Green,
                Color.Orange, Color.Purple, Color.Brown
            };

            for (int i = 0; i < _yFields.Length; i++)
            {
                var s = new Series(_yFields[i])
                {
                    ChartType = SeriesChartType.Line,
                    BorderWidth = 2,
                    Color = colors[i % colors.Length],
                    Legend = "Legend"
                };

                foreach (var p in _cacheData)
                    s.Points.AddXY(p.x, p.y[i]);

                chart.Series.Add(s);
            }

            var area = chart.ChartAreas[0];

            ApplyYScale(minY, maxY);

            area.AxisX.Title = xName;
            area.AxisY.Title = "Value";

            area.RecalculateAxesScale();

            area.AxisX.ScaleView.Zoom(
                _cacheData.First().x,
                _cacheData.Last().x
            );
        }

        // ---------------- Y SCALE ----------------

        private void RecalculateYScale()
        {
            if (_cacheData.Length == 0)
                return;

            var area = chart.ChartAreas[0];

            double minX = area.AxisX.ScaleView.ViewMinimum;
            double maxX = area.AxisX.ScaleView.ViewMaximum;

            if (double.IsNaN(minX) || double.IsNaN(maxX))
                return;

            double minY = double.MaxValue;
            double maxY = double.MinValue;

            foreach (var p in _cacheData)
            {
                if (p.x < minX || p.x > maxX)
                    continue;

                foreach (var y in p.y)
                {
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            ApplyYScale(minY, maxY);
        }

        private void ApplyYScale(double minY, double maxY)
        {
            var area = chart.ChartAreas[0];

            double range = maxY - minY;
            double pad = range * 0.005;

            if (pad == 0)
                pad = 0.1;

            area.AxisY.Minimum = minY - pad;
            area.AxisY.Maximum = maxY + pad;

            area.AxisY.Interval = range / 25;
            area.AxisY.LabelStyle.Format = "0.#####";
        }

        // ---------------- ZOOM ----------------

        private void Zoom(double factor)
        {
            var axis = chart.ChartAreas[0].AxisX;

            double min = axis.ScaleView.ViewMinimum;
            double max = axis.ScaleView.ViewMaximum;

            if (double.IsNaN(min) || double.IsNaN(max))
                return;

            double center = (min + max) / 2;
            double size = (max - min) * factor;

            axis.ScaleView.Zoom(center - size / 2, center + size / 2);

            RecalculateYScale();
        }

        private void ResetZoom()
        {
            var area = chart.ChartAreas[0];

            area.AxisX.ScaleView.ZoomReset();
            area.AxisY.ScaleView.ZoomReset();

            RecalculateYScale();
        }
    }
}