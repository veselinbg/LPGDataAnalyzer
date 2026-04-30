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

        private readonly Label lblInfo = new()
        {
            AutoSize = true,
            BackColor = Color.FromArgb(230, 255, 255, 255),
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9),
            Padding = new Padding(6),
            Visible = false
        };

        private readonly Dictionary<string, Func<DataItem, double>> _cache = new();

        private DataItem[] _data = Array.Empty<DataItem>();
        private (double x, double[] y)[] _cacheData = Array.Empty<(double, double[])>();
        private double[] _xValues = Array.Empty<double>();
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
            _xValues = Array.Empty<double>();
        }

        // ---------------- UI ----------------

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

            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));

            cmbX.Dock = DockStyle.Fill;

            pnlY.Dock = DockStyle.Fill;
            pnlY.WrapContents = true;
            pnlY.AutoScroll = true;

            var btnPanel = new FlowLayoutPanel { Dock = DockStyle.Fill };

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
            chart.Controls.Add(lblInfo);
            lblInfo.Location = new Point(10, 10);

            root.Controls.Add(top, 0, 0);
            root.Controls.Add(chart, 0, 1);
        }

        // ---------------- CHART ----------------

        private void InitChart()
        {
            chart.AntiAliasing = AntiAliasingStyles.All;

            var area = new ChartArea("Main");

            area.AxisX.ScaleView.Zoomable = true;
            area.AxisY.ScaleView.Zoomable = true;

            area.CursorX.IsUserEnabled = true;
            area.CursorX.IsUserSelectionEnabled = true;
            area.CursorX.LineColor = Color.Gray;
            area.CursorX.LineDashStyle = ChartDashStyle.Dash;

            chart.ChartAreas.Add(area);

            chart.Legends.Add(new Legend("Legend")
            {
                Docking = Docking.Right
            });

            chart.MouseMove += Chart_MouseMove;

            chart.MouseLeave += (_, _) =>
            {
                lblInfo.Visible = false;
            };
        }

        // ---------------- FIELD SELECTION ----------------

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
                var cb = new CheckBox
                {
                    Text = f,
                    Appearance = Appearance.Button,
                    AutoSize = true,
                    Margin = new Padding(3)
                };

                cb.CheckedChanged += (_, _) =>
                {
                    cb.BackColor = cb.Checked
                        ? Color.LightSteelBlue
                        : SystemColors.Control;
                };

                pnlY.Controls.Add(cb);
            }
        }

        // ---------------- BUILD ----------------

        private void BuildChart()
        {
            if (_data.Length == 0 || cmbX.SelectedItem == null)
                return;

            chart.Series.Clear();

            var area = chart.ChartAreas[0];
            area.AxisX.ScaleView.ZoomReset();

            _yFields = pnlY.Controls
                .OfType<CheckBox>()
                .Where(c => c.Checked)
                .Select(c => c.Text)
                .ToArray();

            if (_yFields.Length == 0)
                return;

            string xName = cmbX.SelectedItem.ToString()!;
            var getX = Get(xName);
            var getYs = _yFields.Select(Get).ToArray();

            int n = _data.Length;

            _cacheData = new (double, double[])[n];
            _xValues = new double[n];

            double minY = double.MaxValue;
            double maxY = double.MinValue;

            for (int i = 0; i < n; i++)
            {
                double x = getX(_data[i]);
                _xValues[i] = x;

                double[] ys = new double[_yFields.Length];

                for (int j = 0; j < getYs.Length; j++)
                {
                    double y = getYs[j](_data[i]);
                    ys[j] = y;

                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }

                _cacheData[i] = (x, ys);
            }

            Array.Sort(_xValues, _cacheData);

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
                    Legend = "Legend",
                    MarkerStyle = MarkerStyle.Circle,
                    MarkerSize = 5
                };

                for (int k = 0; k < n; k++)
                    s.Points.AddXY(_cacheData[k].x, _cacheData[k].y[i]);

                chart.Series.Add(s);
            }

            ApplyYScale(minY, maxY);
            ApplyXScale();
        }

        // ---------------- CROSSHAIR + TOOLTIP ----------------

        private void Chart_MouseMove(object sender, MouseEventArgs e)
        {
            if (_xValues.Length == 0)
                return;

            var area = chart.ChartAreas[0];

            try
            {
                double xVal = area.AxisX.PixelPositionToValue(e.X);
                int idx = FindNearestIndex(xVal);

                double x = _xValues[idx];

                area.CursorX.Position = x;

                var dataItem = _data[idx];

                var props = dataItem.GetType().GetProperties();

                string text = $"X: {x:0.#####}\n";

                foreach (var p in props)
                {
                    text += $"{p.Name}: {p.GetValue(dataItem)}\n";
                }

                lblInfo.Text = text;
                lblInfo.Visible = true;
            }
            catch
            {
                lblInfo.Visible = false;
            }
        }

        private int FindNearestIndex(double value)
        {
            int idx = Array.BinarySearch(_xValues, value);

            if (idx >= 0)
                return idx;

            idx = ~idx;

            if (idx <= 0) return 0;
            if (idx >= _xValues.Length) return _xValues.Length - 1;

            return Math.Abs(value - _xValues[idx - 1]) < Math.Abs(value - _xValues[idx])
                ? idx - 1
                : idx;
        }

        // ---------------- SCALE (SAFE - DOES NOT BREAK ZOOM) ----------------

        private void ApplyXScale()
        {
            if (_xValues.Length == 0) return;

            var axis = chart.ChartAreas[0].AxisX;
            axis.IntervalAutoMode = IntervalAutoMode.VariableCount;
            axis.Minimum = _xValues[0];
            axis.Maximum = _xValues[^1];
        }

        private void ApplyYScale(double minY, double maxY)
        {
            var axis = chart.ChartAreas[0].AxisY;

            double pad = (maxY - minY) * 0.01;
            if (pad == 0) pad = 1;
            axis.IntervalAutoMode = IntervalAutoMode.VariableCount;
            axis.Minimum = minY - pad;
            axis.Maximum = maxY + pad;
        }

        // ---------------- ZOOM ----------------

        private void Zoom(double factor)
        {
            var axis = chart.ChartAreas[0].AxisX;

            double min = axis.ScaleView.ViewMinimum;
            double max = axis.ScaleView.ViewMaximum;

            if (double.IsNaN(min) || double.IsNaN(max))
            {
                min = _xValues[0];
                max = _xValues[^1];
            }

            double center = (min + max) / 2;
            double size = (max - min) * factor;

            axis.ScaleView.Zoom(center - size / 2, center + size / 2);
        }

        private void ResetZoom()
        {
            var area = chart.ChartAreas[0];

            area.AxisX.ScaleView.ZoomReset();
            area.AxisY.ScaleView.ZoomReset();

            ApplyXScale();
        }

        // ---------------- CACHE ----------------

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
    }
}