using LPGDataAnalyzer.Services;

namespace LPGDataAnalyzer.Controls
{
    /// <summary>
    /// Result of "Check accuracy" inside the Prediction screen: a headline with the accuracy of the current
    /// options, each option switched the other way with a verdict, and the accuracy per hidden file.
    /// </summary>
    public sealed class PredictionScoreView : UserControl
    {
        /// <summary>Differences smaller than this (in accuracy points) count as "no real difference".</summary>
        private const double SameThreshold = 0.002;

        private readonly Label _headline;
        private readonly Label _details;
        private readonly DataGridView _variantsGrid;
        private readonly DataGridView _filesGrid;
        private readonly ToolTip _toolTip = new() { AutoPopDelay = 30000 };

        private IReadOnlyList<(string Name, PredictionScore.Result Result)> _variants = [];

        public PredictionScoreView()
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _headline = new Label
            {
                AutoSize = true,
                Margin = new Padding(3, 3, 3, 2),
                Font = new Font(Font.FontFamily, Font.Size * 1.2f, FontStyle.Bold),
            };
            _details = new Label { AutoSize = true, Margin = new Padding(3, 0, 3, 4) };

            const string help =
                "How it is measured: one log file is hidden, the map is predicted from the other files, and the hidden file\n" +
                "shows whether the new map would have fixed its FAST trims. This is repeated for every file.\n" +
                "0 % = no better than the current map, higher = better, below 0 = worse.\n" +
                "Left: each option switched the other way (green = better than now, red = worse). Right: each hidden file.";
            _toolTip.SetToolTip(_headline, help);
            _toolTip.SetToolTip(_details, help);

            _variantsGrid = CreateGrid();
            _variantsGrid.DataBindingComplete += (_, _) => FormatVariants();
            _filesGrid = CreateGrid();
            _filesGrid.DataBindingComplete += (_, _) =>
            {
                SetHeader(_filesGrid, "File", "Hidden file", 120);
                SetHeader(_filesGrid, "TrimNow", "Trim now", 60);
                SetHeader(_filesGrid, "TrimAfter", "Trim new map", 70);
            };

            layout.Controls.Add(_headline, 0, 0);
            layout.SetColumnSpan(_headline, 2);
            layout.Controls.Add(_details, 0, 1);
            layout.SetColumnSpan(_details, 2);
            layout.Controls.Add(_variantsGrid, 0, 2);
            layout.Controls.Add(_filesGrid, 1, 2);
            Controls.Add(layout);

            Disposed += (_, _) => _toolTip.Dispose();
            Clear();
        }

        /// <summary>No result yet (new data loaded).</summary>
        public void Clear() =>
            ShowMessage("Press \"Predict\": the accuracy of the prediction with these logs is calculated after it (needs 2 or more files).");

        /// <summary>Shows only a message (no result, calculating, or an error).</summary>
        public void ShowMessage(string message)
        {
            _variants = [];
            _headline.Text = "Prediction accuracy";
            _details.Text = message;
            _variantsGrid.DataSource = null;
            _filesGrid.DataSource = null;
        }

        /// <param name="variants">First the current options, then each option switched the other way.</param>
        public void ShowResults(IReadOnlyList<(string Name, PredictionScore.Result Result)> variants)
        {
            _variants = variants;
            var current = variants[0].Result;

            _headline.Text = $"Prediction accuracy with the current options: {Percent(current.Score)}";
            _details.Text =
                $"Average FAST trim in the hidden files: {current.TrimNow:0.00} % now, {current.TrimAfter:0.00} % with the new map. " +
                $"Max possible {1 - Math.Pow(1 - FuelMapPrediction.CorrectionGain, 2):P0} (only {FuelMapPrediction.CorrectionGain:P0} of each correction is applied).";

            _variantsGrid.DataSource = variants.Skip(1).Select(v => new
            {
                Change = v.Name,
                Accuracy = Percent(v.Result.Score),
                Result = Verdict(v.Result.Score - current.Score),
            }).ToList();

            _filesGrid.DataSource = current.Files.Select(f => new
            {
                f.File,
                Accuracy = Percent(f.Score),
                TrimNow = f.TrimNow.ToString("0.00") + " %",
                TrimAfter = f.TrimAfter.ToString("0.00") + " %",
                f.Readings,
            }).ToList();
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            var max = new Size(Math.Max(100, ClientSize.Width - 10), 0);
            _headline.MaximumSize = max;
            _details.MaximumSize = max;
            base.OnLayout(e);
        }

        private void FormatVariants()
        {
            SetHeader(_variantsGrid, "Change", "If you switch", 110);
            SetHeader(_variantsGrid, "Accuracy", "Accuracy", 50);
            SetHeader(_variantsGrid, "Result", "Result", 130);

            if (_variants.Count == 0)
                return;

            double current = _variants[0].Result.Score;
            for (int i = 0; i < _variantsGrid.Rows.Count && i + 1 < _variants.Count; i++)
            {
                double diff = _variants[i + 1].Result.Score - current;
                _variantsGrid.Rows[i].DefaultCellStyle.BackColor = Math.Abs(diff) < SameThreshold
                    ? SystemColors.Window
                    : diff > 0 ? Color.FromArgb(200, 240, 200) : Color.FromArgb(250, 215, 215);
            }
        }

        private static string Verdict(double diff)
        {
            string points = Math.Abs(diff * 100).ToString("0.0");
            if (Math.Abs(diff) < SameThreshold)
                return "no real difference";
            return diff > 0
                ? $"better by {points} points: worth switching"
                : $"worse by {points} points: keep it as it is";
        }

        private static string Percent(double score) => (score * 100).ToString("0.0") + " %";

        private static void SetHeader(DataGridView grid, string column, string header, float weight)
        {
            if (grid.Columns[column] is DataGridViewColumn c)
            {
                c.HeaderText = header;
                c.FillWeight = weight;
            }
        }

        private static DataGridView CreateGrid() => new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.FixedSingle,
        };
    }
}
