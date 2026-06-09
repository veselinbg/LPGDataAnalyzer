using KenoGameDataAnalyzer;
using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Services;
using static LPGDataAnalyzer.Models.Settings;

namespace LPGDataAnalyzer.Controls
{
    public class ShowAllFileDataUI : UserControl
    {
        private const int ItemHeight = 440;

        private readonly List<FileResult> _items = new();

        private Canvas viewport;
        private VScrollBar vScroll;
        private System.Windows.Forms.Timer smoothTimer;

        private int targetScroll;
        private float currentScroll;

        public ShowAllFileDataUI()
        {
            InitUI();
        }

        // =====================================================
        // UI INIT
        // =====================================================
        private void InitUI()
        {
            viewport = new Canvas
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                TabStop = true
            };

            viewport.PaintScene = RenderScene;
            viewport.MouseEnter += (_, __) => viewport.Focus();
            viewport.MouseWheel += OnMouseWheelScroll;

            vScroll = new VScrollBar
            {
                Dock = DockStyle.Right,
                SmallChange = 20,
                LargeChange = 200
            };

            vScroll.Scroll += (_, __) =>
            {
                targetScroll = vScroll.Value;
                StartSmoothScroll();
            };

            smoothTimer = new System.Windows.Forms.Timer
            {
                Interval = 15
            };

            smoothTimer.Tick += (_, __) =>
            {
                currentScroll += (targetScroll - currentScroll) * 0.25f;

                if (Math.Abs(targetScroll - currentScroll) < 0.5f)
                    currentScroll = targetScroll;

                viewport.Invalidate();

                if (currentScroll == targetScroll)
                    smoothTimer.Stop();
            };

            Controls.Add(viewport);
            Controls.Add(vScroll);
        }

        // =====================================================
        // MOUSE WHEEL
        // =====================================================
        private void OnMouseWheelScroll(object sender, MouseEventArgs e)
        {
            int step = vScroll.SmallChange * 3;

            int newValue = vScroll.Value - Math.Sign(e.Delta) * step;

            newValue = Math.Max(
                vScroll.Minimum,
                Math.Min(vScroll.Maximum - vScroll.LargeChange + 1, newValue));

            vScroll.Value = newValue;

            targetScroll = vScroll.Value;
            StartSmoothScroll();
        }

        // =====================================================
        // LOAD DATA
        // =====================================================
        public async Task LoadAsync(string path)
        {
            var files = new DirectoryInfo(path)
                .GetFiles("*.txt", SearchOption.AllDirectories)
                .OrderByDescending(f => f.CreationTime)
                .Select(f => f.FullName)
                .ToList();

            var results = new FileResult[files.Count];

            await Task.Run(() =>
            {
                for (int i = 0; i < files.Count; i++)
                    results[i] = Process(files[i]);
            });

            _items.Clear();
            _items.AddRange(results);

            SetupScroll();
            viewport.Invalidate();
        }

        private FileResult Process(string file)
        {
            var p = new Parser();
            p.Load(file);

            var t1 = Analyzer.BuildTable(p.Data, x => x.BENZ_b1, x => x.FAST_b1, Aggregation.Median);
            var t2 = Analyzer.BuildTable(p.Data, x => x.BENZ_b2, x => x.FAST_b2, Aggregation.Median);

            return new FileResult
            {
                File = file,
                Data = p.Data,
                T1 = t1,
                T2 = t2,
                Diff = Analyzer.Subtract(t1, t2)
            };
        }

        // =====================================================
        // SCROLL SETUP
        // =====================================================
        private void SetupScroll()
        {
            vScroll.Minimum = 0;

            int max = Math.Max(0, _items.Count * ItemHeight);

            vScroll.Maximum = max;
            vScroll.LargeChange = viewport.Height;
            vScroll.Value = 0;
        }

        private void StartSmoothScroll()
        {
            if (!smoothTimer.Enabled)
                smoothTimer.Start();
        }

        // =====================================================
        // SCENE RENDER (ONLY PAINTING)
        // =====================================================
        private void RenderScene(Graphics g)
        {
            g.Clear(Color.White);

            int scroll = (int)currentScroll;

            int first = scroll / ItemHeight;
            int count = viewport.Height / ItemHeight + 3;

            for (int i = first; i < first + count && i < _items.Count; i++)
            {
                var item = _items[i];
                int y = i * ItemHeight - scroll;

                DrawItem(g, item, y, viewport.Width);
            }
        }

        // =====================================================
        // ITEM DRAW
        // =====================================================
        private void DrawItem(Graphics g, FileResult item, int y, int width)
        {
            DrawTitle(g, item.File, y, width);

            DrawMatrix(g, item.T1, 0, "Bank 1", y, width);
            DrawMatrix(g, item.T2, 1, "Bank 2", y, width);
            DrawMatrix(g, item.Diff, 2, "Diff", y, width);
        }

        private void DrawTitle(Graphics g, string file, int y, int width)
        {
            g.DrawString(
                Path.GetFileName(file),
                ColorHelper.TitleFontBold,
                Brushes.Black,
                new Rectangle(10, y + 4, width - 20, 22));
        }

        // =====================================================
        // MATRIX DRAW (NO CONTROLS)
        // =====================================================
        private void DrawMatrix(Graphics g, double?[,] table, int block, string title, int baseY, int width)
        {
            if (table == null) return;

            int rows = InjectionRanges.Length;
            int cols = RpmColumns.Length;

            int labelWidth = 40;
            int labelGap = 2;
            int blockGap = 10;

            int usableWidth = Math.Max(200, (width - 3 * (labelWidth + blockGap)) / 3);

            int cellW = Math.Max(8, usableWidth / cols);
            int cellH = Math.Max(8, (ItemHeight - 90) / rows);

            int blockX = block * (usableWidth + labelWidth + blockGap);

            int labelX = blockX;
            int gridX = blockX + labelWidth + labelGap;

            int startY = baseY + 75;

            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            using var font = new Font("Segoe UI", 7f);

            // HEADER
            var headerRect = new Rectangle(gridX, startY - 40, usableWidth, 22);

            using (var bg = new SolidBrush(Color.FromArgb(240, 240, 240)))
                g.FillRectangle(bg, headerRect);

            g.DrawRectangle(Pens.LightGray, headerRect);
            g.DrawString(title, ColorHelper.BoldFont, Brushes.Black, headerRect, sf);

            // COLUMN HEADERS
            for (int c = 0; c < cols; c++)
            {
                var rect = new Rectangle(gridX + c * cellW, startY - 20, cellW, 20);
                g.DrawString(RpmColumns[c].Label.ToString(), ColorHelper.BoldFont, Brushes.Black, rect, sf);
            }

            // ROW LABELS
            for (int r = 0; r < rows; r++)
            {
                var rect = new Rectangle(labelX, startY + r * cellH, labelWidth, cellH);

                g.DrawString(
                    InjectionRanges[r].Label.ToString(),
                    font,
                    Brushes.Black,
                    rect,
                    sf);
            }

            // CELLS
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var val = table[c, r];

                    var rect = new Rectangle(
                        gridX + c * cellW,
                        startY + r * cellH,
                        cellW,
                        cellH);

                    if (rect.Width <= 0 || rect.Height <= 0)
                        continue;

                    using var brush = new SolidBrush(ColorHelper.InterpolateDiverging(Normalize(val)));
                    g.FillRectangle(brush, rect);
                    g.DrawRectangle(Pens.LightGray, rect);

                    if (val.HasValue)
                        g.DrawString(val.Value.ToString("0.##"), font, Brushes.Black, rect, sf);
                }
            }
        }

        private double Normalize(double? v)
        {
            if (!v.HasValue) return 0;

            double x = v.Value;
            if (double.IsNaN(x) || double.IsInfinity(x)) return 0;

            return Math.Max(-1, Math.Min(1, x / 20.0));
        }

        // =====================================================
        // CANVAS CLASS (SINGLE DRAW SURFACE)
        // =====================================================
        private class Canvas : Panel
        {
            public Action<Graphics> PaintScene;

            public Canvas()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                PaintScene?.Invoke(e.Graphics);
            }
        }

        // =====================================================
        // DATA MODEL
        // =====================================================
        public class FileResult
        {
            public string File;
            public DataItem[] Data;
            public double?[,] T1;
            public double?[,] T2;
            public double?[,] Diff;
        }
    }
}