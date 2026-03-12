using System;
using System.Collections.Generic;
using System.Text;

namespace LPGDataAnalyzer.Controls
{
    internal class LegendPanelBuilder
    {
        public static void LegendPanel_Paint(object sender, PaintEventArgs e)
        {
            if (sender is not Panel panel)
                return;

            if (panel.Tag is not ValueTuple<double, double> data)
                return;

            double minSigned = data.Item1;
            double maxSigned = data.Item2;

            int width = panel.Width;
            int height = panel.Height;

            if (width <= 1 || height <= 1)
                return;

            double maxAbs = Math.Max(Math.Abs(minSigned), Math.Abs(maxSigned));
            if (maxAbs < 1e-12)
                maxAbs = 1e-12;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

            // ===== Gradient =====
            using (Pen gradientPen = new Pen(Color.Black))
            {
                for (int x = 0; x < width; x++)
                {
                    double normalized = (x / (double)(width - 1)) * 2.0 - 1.0;
                    gradientPen.Color = ColorHelper.InterpolateDiverging(normalized);
                    e.Graphics.DrawLine(gradientPen, x, 0, x, height);
                }
            }

            // ===== Ticks & Labels =====
            using Font font = new Font("Segoe UI", 8f);
            using Brush textBrush = new SolidBrush(Color.Black);
            using Pen tickPen = new Pen(Color.Black, 1f);

            double[] ticks = { -maxAbs, 0.0, maxAbs };

            foreach (double val in ticks)
            {
                double normalized = (val / maxAbs + 1.0) / 2.0;
                int x = (int)Math.Round(normalized * (width - 1));

                e.Graphics.DrawLine(tickPen, x, 0, x, 6);

                string text = val.ToString("F2");
                SizeF size = e.Graphics.MeasureString(text, font);

                e.Graphics.DrawString(
                    text,
                    font,
                    textBrush,
                    x - size.Width / 2,
                    8);
            }
        }
        public static void CreateDynamicHorizontalHeatmapLegend(
                                                Panel legendPanel,
                                                DataGridView dgv,
                                                double minSigned,
                                                double maxSigned)
        {
            if (legendPanel == null || dgv == null)
                return;

            legendPanel.Tag = (minSigned, maxSigned);

            int newWidth = dgv.ClientSize.Width;

            if (legendPanel.Width != newWidth)
                legendPanel.Width = newWidth;

            legendPanel.Invalidate();
        }
    }
}
