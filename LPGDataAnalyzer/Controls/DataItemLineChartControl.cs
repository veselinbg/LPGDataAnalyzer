using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer.Controls
{
    public class DataItemLineChartControl : UserControl
    {
        private readonly ComboBox cmbX = new();
        private readonly ComboBox cmbY1 = new();
        private readonly ComboBox cmbY2 = new();

        private readonly Button btnBuild = new();
        private readonly Button btnZoomIn = new();
        private readonly Button btnZoomOut = new();
        private readonly Button btnResetZoom = new();

        private readonly Chart chart = new();

        private DataItem[] _data = [];

        private readonly Dictionary<string, Func<DataItem, double>> _cache = new();

        public DataItemLineChartControl()
        {
            InitUI();
            LoadFields();
            InitChart();
        }

        public void SetData(DataItem[] data)
        {
            _data = data ?? [];
        }

        // ---------------- UI ----------------

        private void InitUI()
        {
            cmbX.SetBounds(10, 10, 160, 25);
            cmbY1.SetBounds(180, 10, 160, 25);
            cmbY2.SetBounds(350, 10, 160, 25);

            btnBuild.SetBounds(520, 8, 100, 28);
            btnBuild.Text = "Build";
            btnBuild.Click += (_, _) => BuildChart();

            btnZoomIn.SetBounds(630, 8, 80, 28);
            btnZoomOut.SetBounds(720, 8, 80, 28);
            btnResetZoom.SetBounds(810, 8, 110, 28);

            btnZoomIn.Text = "+";
            btnZoomOut.Text = "-";
            btnResetZoom.Text = "Reset";

            btnZoomIn.Click += (_, _) => Zoom(0.7);
            btnZoomOut.Click += (_, _) => Zoom(1.3);
            btnResetZoom.Click += (_, _) => ResetZoom();

            chart.SetBounds(10, 50, 1900, 500);

            Controls.AddRange([
                cmbX, cmbY1, cmbY2,
                btnBuild, btnZoomIn, btnZoomOut, btnResetZoom,
                chart
            ]);
        }

        private void InitChart()
        {
            var area = new ChartArea("Main");

            area.AxisX.ScaleView.Zoomable = true;
            area.AxisY.ScaleView.Zoomable = true;

            area.CursorX.IsUserEnabled = true;
            area.CursorX.IsUserSelectionEnabled = true;

            area.CursorY.IsUserEnabled = true;
            area.CursorY.IsUserSelectionEnabled = true;

            chart.ChartAreas.Add(area);
        }

        private void LoadFields()
        {
            var fields = typeof(DataItem)
                .GetProperties()
                .Where(p => p.PropertyType == typeof(int) || p.PropertyType == typeof(double))
                .Select(p => p.Name)
                .ToArray();

            cmbX.Items.AddRange(fields);
            cmbY1.Items.AddRange(fields);
            cmbY2.Items.AddRange(fields);

            if (fields.Length > 0)
            {
                cmbX.SelectedIndex = 0;
                cmbY1.SelectedIndex = Math.Min(1, fields.Length - 1);
                cmbY2.SelectedIndex = Math.Min(2, fields.Length - 1);
            }
        }

        // ---------------- Reflection cache ----------------

        private Func<DataItem, double> Get(string name)
        {
            if (_cache.TryGetValue(name, out var fn))
                return fn;

            var p = Expression.Parameter(typeof(DataItem), "x");
            var body = Expression.Convert(Expression.Property(p, name), typeof(double));

            fn = Expression.Lambda<Func<DataItem, double>>(body, p).Compile();

            _cache[name] = fn;
            return fn;
        }

        // ---------------- Chart ----------------

        private void BuildChart()
        {
            if (_data.Length == 0)
                return;

            if (cmbX.SelectedItem == null || cmbY1.SelectedItem == null)
                return;

            string xName = cmbX.SelectedItem.ToString()!;
            string y1Name = cmbY1.SelectedItem.ToString()!;
            string? y2Name = cmbY2.SelectedItem?.ToString();

            var getX = Get(xName);
            var getY1 = Get(y1Name);

            chart.Series.Clear();

            // prepare base dataset once
            var baseData = _data
                .Select(d => new { X = getX(d), Y1 = getY1(d) })
                .OrderBy(p => p.X)
                .ToArray();

            chart.Series.Add(CreateSeries(y1Name, baseData.Select(p => (p.X, p.Y1)), System.Drawing.Color.Blue));

            // optional second line
            if (!string.IsNullOrWhiteSpace(y2Name) && y2Name != y1Name)
            {
                var getY2 = Get(y2Name);

                var data2 = _data
                    .Select(d => new { X = getX(d), Y = getY2(d) })
                    .OrderBy(p => p.X)
                    .ToArray();

                chart.Series.Add(CreateSeries(y2Name, data2.Select(p => (p.X, p.Y)), System.Drawing.Color.Red));
            }

            var area = chart.ChartAreas[0];
            area.AxisX.Title = xName;
            area.AxisY.Title = "Value";
            area.RecalculateAxesScale();
        }

        private static Series CreateSeries(string name, IEnumerable<(double x, double y)> data, System.Drawing.Color color)
        {
            var s = new Series(name)
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 2,
                Color = color
            };

            foreach (var (x, y) in data)
                s.Points.AddXY(x, y);

            return s;
        }

        // ---------------- Zoom ----------------

        private void Zoom(double factor)
        {
            var area = chart.ChartAreas[0];

            try
            {
                double min = area.AxisX.ScaleView.ViewMinimum;
                double max = area.AxisX.ScaleView.ViewMaximum;

                double center = (min + max) / 2;
                double size = (max - min) * factor;

                area.AxisX.ScaleView.Zoom(center - size / 2, center + size / 2);
            }
            catch
            {
                // ignore if not zoomed yet
            }
        }

        private void ResetZoom()
        {
            var area = chart.ChartAreas[0];
            area.AxisX.ScaleView.ZoomReset();
            area.AxisY.ScaleView.ZoomReset();
        }
    }
}