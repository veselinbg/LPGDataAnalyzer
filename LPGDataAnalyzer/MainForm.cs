using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Models.Common;
using LPGDataAnalyzer.Services;
using static LPGDataAnalyzer.Models.Settings;

namespace LPGDataAnalyzer
{
    internal partial class MainForm : Form
    {
        private readonly Parser Parser = new();
        private readonly Analyzer Analyser = new();
        private readonly TextExtractor textExtractor = new();
        private readonly AppSettingManager _appSettingManager;
        private AppSettings AppSettings { get; set; }
        public MainForm(AppSettingManager appSettingManager)
        {
            //this.ControlAdded += (s, e) =>
            //{
            //    if (e.Control is DataGridView dg)
            //        PrepareGrid(dg);
            //};

            InitializeComponent();

            //SearchAndPrepareGrid(this.Controls);

            _appSettingManager = appSettingManager;

            AppSettings = _appSettingManager.Load();

            txtFilePath.Text = AppSettings.LastSavedFilePath;
            textBoxParsedData.Text = AppSettings.LastLoadedFuelTable;
            textBoxImagePath.Text = AppSettings.ImagePath;
            textBoxLastPredictedFuelTable.Text = AppSettings.LastPredictedFuelTable;

            LoadParsedData();

            comboBoxGasTemperatureb1.DataSource = GetExistGasTemperatureRanges(Parser.Data);
            comboBoxGasTemperatureb1.SelectedIndex = 0;

            comboBoxGasTemperatureb2.DataSource = GetExistGasTemperatureRanges(Parser.Data);
            comboBoxGasTemperatureb2.SelectedIndex = 0;

            comboBoxReductorTempGroup1.DataSource = GetExistReductorTempGroups(Parser.Data);
            comboBoxReductorTempGroup1.SelectedIndex = 0;

            comboBoxReductorTempGroup2.DataSource = GetExistReductorTempGroups(Parser.Data);
            comboBoxReductorTempGroup2.SelectedIndex = 0;

            comboBoxAggregation.DataSource = Enum.GetValues<Aggregation>();
        }
        private static void SearchAndPrepareGrid(Control.ControlCollection parentControl)
        {
            foreach (Control control in parentControl)
            {
                if (control is DataGridView dg)
                {
                    PrepareGrid(dg);
                }
                else
                {
                    // Recursively search child controls
                    SearchAndPrepareGrid(control.Controls);
                }
            }
        }
        private static void GridBuilder(DataGridView dgv, double?[,] table)
        {
            PrepareGrid(dgv);

            CreateColumns(dgv, RpmColumns.Select(x => x.Label));

            FillRows(dgv, table);
        }
        private static void PrepareGrid(DataGridView dgv)
        {
            dgv.DataSource = null;
            dgv.Columns.Clear();
            dgv.Rows.Clear();
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgv.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgv.RowHeadersVisible = false;
        }
        private static void LoadDataSource(DataGridView dgv, object? dataSource)
        {
            dgv.DataSource = dataSource;
        }
        private static void CreateColumns(DataGridView dgv, IEnumerable<int> rpmColumns)
        {
            dgv.Columns.Add("InjectionTime", "Inj.Time");

            foreach (int rpm in rpmColumns)
            {
                dgv.Columns.Add($"RPM_{rpm}", rpm.ToString());
            }
        }
        private static void FillRows(DataGridView dgv, double?[,] table)
        {
            dgv.SuspendLayout();
            try
            {
                dgv.Rows.Clear();

                for (var injIndex = 0; injIndex < InjectionRanges.Length; injIndex++)
                {
                    var row = new DataGridViewRow();
                    row.CreateCells(dgv);

                    row.Cells[dgv.Columns["InjectionTime"].Index].Value = InjectionRanges[injIndex].Label;

                    for (var rpmIndex = 0; rpmIndex < RpmColumns.Length; rpmIndex++)
                    {


                        row.Cells[dgv.Columns[$"RPM_{RpmColumns[rpmIndex].Label}"].Index].Value = table[rpmIndex, injIndex];

                    }

                    dgv.Rows.Add(row);
                }
            }
            finally
            {
                dgv.ResumeLayout();
            }
        }
        public static AxisSplit<int> HighlightDifferencesHeatmapWithValues(
    DataGridView dgv1,
    DataGridView dgv2 = null,
    double tolerance = 0.01)
        {
            int rows = dgv1.RowCount;
            int cols = dgv1.ColumnCount;

            if (dgv2 != null && (rows != dgv2.RowCount || cols != dgv2.ColumnCount))
                throw new ArgumentException("DataGridViews must have same dimensions.");

            double?[,] values = ExtractValues(dgv1, dgv2);

            return ApplyHeatmap(dgv1, dgv2, values, tolerance);
        }
        private static double?[,] ExtractValues(DataGridView dgv1, DataGridView dgv2)
        {
            int rows = dgv1.RowCount;
            int cols = dgv1.ColumnCount;

            double?[,] result = new double?[rows, cols];

            for (int r = 0; r < rows; r++)
            {
                for (int c = 1; c < cols; c++)
                {
                    double? v1 = GetCellDoubleNullable(dgv1, r, c);

                    if (v1 == null)
                    {
                        result[r, c] = null;
                        continue;
                    }

                    if (dgv2 == null)
                    {
                        result[r, c] = v1;
                    }
                    else
                    {
                        double? v2 = GetCellDoubleNullable(dgv2, r, c);

                        if (v2 == null)
                            result[r, c] = null;
                        else
                            result[r, c] = v1 - v2;
                    }
                }
            }

            return result;
        }
        private static double? GetCellDoubleNullable(DataGridView dgv, int r, int c)
        {
            var val = dgv.Rows[r].Cells[c].Value;

            if (val == null || val == DBNull.Value)
                return null;

            if (double.TryParse(val.ToString(), out double result))
                return result;

            return null;
        }
        private static void SetCellColor(DataGridView dgv1, DataGridView dgv2, int r, int c, Color color)
        {
            dgv1.Rows[r].Cells[c].Style.BackColor = color;

            if (dgv2 != null)
                dgv2.Rows[r].Cells[c].Style.BackColor = color;
        }
        private static AxisSplit<int> ApplyHeatmap(
            DataGridView dgv1,
            DataGridView dgv2,
            double?[,] diffs,
            double tolerance)
        {
            int rows = diffs.GetLength(0);
            int cols = diffs.GetLength(1);

            double minSigned = double.MaxValue;
            double maxSigned = double.MinValue;

            int minIndex = -1;
            int maxIndex = -1;

            // ---- Find extremes ----
            for (int r = 0; r < rows; r++)
            {
                for (int c = 1; c < cols; c++) // skip first column
                {
                    double? diffNullable = diffs[r, c];

                    if (!diffNullable.HasValue)
                        continue;

                    double diff = diffNullable.Value;

                    if (diff < minSigned)
                    {
                        minSigned = diff;
                        minIndex = r * cols + c;
                    }

                    if (diff > maxSigned)
                    {
                        maxSigned = diff;
                        maxIndex = r * cols + c;
                    }
                }
            }

            if (minSigned == double.MaxValue)
            {
                minSigned = -1e-6;
                maxSigned = 1e-6;
            }

            double maxAbs = Math.Max(Math.Abs(minSigned), Math.Abs(maxSigned));
            if (maxAbs < 1e-12)
                maxAbs = 1e-12;

            // ---- Apply colors ----
            double maxPositive = Math.Max(maxSigned, 1e-12);
            double maxNegative = Math.Max(Math.Abs(minSigned), 1e-12);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 1; c < cols; c++) // skip first column
                {
                    double? diffNullable = diffs[r, c];

                    if (!diffNullable.HasValue)
                    {
                        SetCellColor(dgv1, dgv2, r, c, Color.LightGray);
                        continue;
                    }

                    double diff = diffNullable.Value;

                    double normalized;

                    if (diff > 0)
                        normalized = diff / maxPositive;     // 0 → 1
                    else
                        normalized = diff / maxNegative;     // -1 → 0

                    normalized = Math.Max(-1, Math.Min(1, normalized));

                    Color color = InterpolateDiverging(normalized);

                    SetCellColor(dgv1, dgv2, r, c, color);
                }
            }

            return new AxisSplit<int>(minIndex, maxIndex, minSigned, maxSigned);
        }
        
        private static Color InterpolateDiverging(double value)
        {
            value = Math.Max(-1, Math.Min(1, value));

            Color blue = Color.FromArgb(180, 180, 255);
            Color white = Color.White;
            Color red = Color.FromArgb(255, 180, 180);

            if (value < 0)
                return Blend(blue, white, value + 1);
            else
                return Blend(white, red, value);
        }

        private static Color Blend(Color c1, Color c2, double t)
        {
            t = Math.Max(0, Math.Min(1, t));

            int r = (int)(c1.R + (c2.R - c1.R) * t);
            int g = (int)(c1.G + (c2.G - c1.G) * t);
            int b = (int)(c1.B + (c2.B - c1.B) * t);

            return Color.FromArgb(r, g, b);
        }
        private void LegendPanel_Paint(object sender, PaintEventArgs e)
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
                    gradientPen.Color = InterpolateDiverging(normalized);
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
        void CreateDynamicHorizontalHeatmapLegend(
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


        private void BtnSelectFile_Click(object sender, EventArgs e)
        {
            using OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtFilePath.Text = ofd.FileName;
                AppSettings.LastSavedFilePath = ofd.FileName;
                _appSettingManager.Save(AppSettings);

                LoadParsedData();
            }
        }
        void LoadParsedData()
        {
            if (string.IsNullOrEmpty(AppSettings.LastSavedFilePath))
                return;

            Parser.Load(AppSettings.LastSavedFilePath);

            if (Parser.Data.Any())
            {
                dataGridViewMainData.SetData(Parser.Data);
                buttonAnalyze.Enabled = true;
                buttonAnalyzeFastTrim.Enabled = true;

                toolStripSummary.Text = $"Total Rows: {Parser.Data.Length} " +
                    $"LPG: Min Temp: {Parser.Data.Min(x => x.Temp_GAS)} Max Temp: {Parser.Data.Max(x => x.Temp_GAS)}" +
                    $" Min PRESS: {Parser.Data.Min(x => x.PRESS)} Max PRESS: {Parser.Data.Max(x => x.PRESS)} Avarige PRESS: {(Parser.Data.Average(x => x.PRESS)).Round()}" +
                    $" % of change Min {Filter.PercentageChange(Parser.Data.Average(x => x.PRESS), Parser.Data.Min(x => x.PRESS)).Round()} Max{Filter.PercentageChange(Parser.Data.Average(x => x.PRESS), Parser.Data.Max(x => x.PRESS)).Round()}";
            }
            else MessageBox.Show("Invalid data.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        private void ButtonShowRatio_Click(object sender, EventArgs e)
        {
            if (Parser?.Data is null) return;

            dataGridViewGasData.DataSource = Analyser.GroupByGasTemperature(Parser.Data, BenzTimingFilterCuting, x => x.Ratio_b1, y => y.Ratio_b2);
            dataGridViewRIDData.DataSource = Analyser.GroupByRIDTemperature(Parser.Data, BenzTimingFilterCuting, x => x.Ratio_b1, y => y.Ratio_b2);

            BuildAnalises(Analyser, Parser.Data, [item => item.BENZ_b1, item => item.BENZ_b2, item => item.BENZ_b1, item => item.BENZ_b2], [
                            item => item.Ratio_b1,
                            item => item.Ratio_b2,
                            item => item.Ratio_b1,
                            item => item.Ratio_b2,
                                    ]);
        }
        private void ButtonShowTrims_Click(object sender, EventArgs e)
        {
            if (Parser?.Data is null) return;
            dataGridViewGasData.DataSource = Analyser.GroupByGasTemperature(Parser.Data, BenzTimingFilterCuting, x => x.Trim_b1, y => y.Trim_b2);
            dataGridViewRIDData.DataSource = Analyser.GroupByRIDTemperature(Parser.Data, BenzTimingFilterCuting, x => x.Trim_b1, y => y.Trim_b2);

            BuildAnalises(Analyser, Parser.Data, [item => item.BENZ_b1, item => item.BENZ_b2, item => item.BENZ_b1, item => item.BENZ_b2], [
                            item => item.Trim_b1,
                            item => item.Trim_b2,
                            item => item.Trim_b1,
                            item => item.Trim_b2,
                                    ]);
        }
        private void buttonShowReducerPress_Click(object sender, EventArgs e)
        {
            if (Parser?.Data is null) return;
            BuildAnalises(Analyser, Parser.Data, [item => item.BENZ, item => item.BENZ, item => item.BENZ, item => item.BENZ], [
                 item => item.AFR,
                item => item.GAS,
                item => item.PRESS,
                item => item.Trim
             ]);
        }
        private void buttonAFR_Click(object sender, EventArgs e)
        {
            BuildAnalises(Analyser, Parser.Data, [item => item.BENZ_b1, item => item.BENZ_b2, item => item.BENZ_b1, item => item.BENZ_b2],
                [item => item.AFR_b1, item => item.AFR_b2, item => item.AFR_b1, item => item.AFR_b2]);
        }

        double BenzTimingFilterCuting
        {
            get
            {
                if (!double.TryParse(tbBenzTimingFilterCuting.Text.Trim(), out var benzTimingFilterCuting))
                {
                    benzTimingFilterCuting = 0;
                    tbBenzTimingFilterCuting.Text = "0";
                }
                return benzTimingFilterCuting;
            }
        }
        void BuildAnalises(Analyzer analyser, DataItem[] lpgdata, Func<DataItem, double>[] injectionBankSelectors, Func<DataItem, double?>[] valueSelectors)
        {

            // Get the selected value
            var aggregator = (Aggregation)comboBoxAggregation.SelectedItem;
            //temp1
            DataItem[] filteredLPGDataByTemp1 = analyser.FilterByTemp(lpgdata, comboBoxGasTemperatureb1.SelectedValue.ToString(), comboBoxReductorTempGroup1.SelectedValue.ToString());

            GridBuilder(dataGridViewAnalyzeDataBank1t1, analyser.BuildTable(filteredLPGDataByTemp1, injectionBankSelectors[0], valueSelectors[0], aggregator));
            GridBuilder(dataGridViewAnalyzeDataBank2t1, analyser.BuildTable(filteredLPGDataByTemp1, injectionBankSelectors[1], valueSelectors[1], aggregator));
            HighlightDifferencesHeatmapWithValues(dataGridViewAnalyzeDataBank1t1);
            HighlightDifferencesHeatmapWithValues(dataGridViewAnalyzeDataBank2t1);
            //temp2
            DataItem[] filteredLPGDataByTemp2 = analyser.FilterByTemp(lpgdata, comboBoxGasTemperatureb2.SelectedValue.ToString(), comboBoxReductorTempGroup2.SelectedValue.ToString());

            GridBuilder(dataGridViewAnalyzeDataBank1t2, analyser.BuildTable(filteredLPGDataByTemp2, injectionBankSelectors[2], valueSelectors[2], aggregator));
            GridBuilder(dataGridViewAnalyzeDataBank2t2, analyser.BuildTable(filteredLPGDataByTemp2, injectionBankSelectors[3], valueSelectors[3], aggregator));
            HighlightDifferencesHeatmapWithValues(dataGridViewAnalyzeDataBank1t2);
            HighlightDifferencesHeatmapWithValues(dataGridViewAnalyzeDataBank2t2);
        }

        private void dataGridViewAnalyzeDataBank1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != 0)
            {
                var range = InjectionRanges[e.RowIndex];

                var rpm = RpmColumns[e.ColumnIndex - 1];

                var data = Parser.Data.Where(x => x.RPM > rpm.Min && x.RPM <= rpm.Max && x.BENZ_b1 > range.Min && x.BENZ_b1 <= range.Max);

                var message = string.Join("\r \n", data.GroupBy(x => $"{x.SLOW_b1.Round()}_{x.FAST_b1.Round()}").Select(x => $"S_F Trim:{x.Key} Count: {x.Count()} PRESS: {data.Where(y => $"{y.SLOW_b1.Round()}_{y.FAST_b1.Round()}" == x.Key).Average(y => y.PRESS).Round()}"));

                MessageBox.Show(message, "Info");
            }
        }

        private void buttonAnalysisByMap_Click(object sender, EventArgs e)
        {
            var mapAnalysis = Analyser.BuildTableMap(Parser.Data);

            LoadDataSource(dataGridViewMapAnalysis, mapAnalysis.ToList());

            var drivingModeAnalysis = Analyser.BuildTableDrivingModes(Parser.Data);

            LoadDataSource(dataGridViewInjectionTimeAnalisys, drivingModeAnalysis.ToList());

            var bankTobank = Analyser.BuildBankToBankfuelBalance(Parser.Data);
            LoadDataSource(dataGridView1, bankTobank.ToList());

        }

        private void button1_Click(object sender, EventArgs e)
        {
            var a1 = Analyser.LpgTemperatureVsInjectionTime(Parser.Data);

            LoadDataSource(dataGridViewMapAnalysis, a1.ToList());

            var a2 = Analyser.BuildABankAwareLPGBaseMap(Parser.Data);

            LoadDataSource(dataGridViewInjectionTimeAnalisys, a2.ToList());

            var a3 = Analyser.LpgInjectorDeadTimeEstimation(Parser.Data);
            LoadDataSource(dataGridView1, a3.ToList());
        }

        private void buttonReducerThermalLag_Click(object sender, EventArgs e)
        {
            var a3 = Analyser.ReducerThermalLag(Parser.Data);
            LoadDataSource(dataGridView1, a3.ToList());
        }
        private void buttonParceSelectedImage_Click(object sender, EventArgs e)
        {
            textBoxParsedData.Text = textExtractor.Parcer(AppSettings.ImagePath);
        }

        private void ButtonPredict_Click(object sender, EventArgs e)
        {
            var table = textExtractor.BuildFinalTable(textBoxParsedData.Text);

            GridBuilder(dataGridViewOrig, table);
            table = Prediction.BuildTable(Parser.Data, table, cbEnableSmooth.Checked, cbInterpolation.Checked, checkBoxOnlyChanges.Checked);
            //Auto-correction algorithm
            //new Prediction().AutoCorrectFuelTable(data, table, cbEnableSmooth.Checked);

            GridBuilder(dataGridViewPrediction, table);

            // Apply heatmap to DataGridViews
            var vals = HighlightDifferencesHeatmapWithValues(dataGridViewPrediction, dataGridViewOrig, tolerance: 0.1);

            // Create horizontal legend aligned with DataGridView
            CreateDynamicHorizontalHeatmapLegend(panelLegend, dataGridViewPrediction, vals.WLow, vals.WHigh);

            textBoxLastPredictedFuelTable.Text = table.ToText();
            AppSettings.LastPredictedFuelTable = textBoxLastPredictedFuelTable.Text;
            _appSettingManager.Save(AppSettings);
        }

        private void ButtonValidate_Click(object sender, EventArgs e)
        {
            try
            {
                textExtractor.Validate(textBoxParsedData.Text);
                AppSettings.LastLoadedFuelTable = textBoxParsedData.Text;
                AppSettings.LastPredictedFuelTable = textBoxLastPredictedFuelTable.Text;
                _appSettingManager.Save(AppSettings);
                MessageBox.Show("Ok, no errors!", "Info");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Errors");
            }
        }

        private void buttonReducerPrediction_Click(object sender, EventArgs e)
        {
            Dictionary<string, int> currentCorrections = [];

            var values = textBoxReducerTempValues.Text.Split(',', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < ReductorTemperatureRanges.Length; i++)
            {
                currentCorrections.Add(ReductorTemperatureRanges[i].Label, int.Parse(values[i]));
            }

            var result = new ReducerPrediction().PredictNewReducerTempCorrections(Parser.Data, currentCorrections, double.Parse(textBoxReferencePressure.Text.Trim()));

            MessageBox.Show(string.Join(",", result.Select(x => x.Value)), "LPG Reducer correction");
        }

        private void buttonExtraInjectionCalculator_Click(object sender, EventArgs e)
        {
            var res = ExtraInjectionCalculator.CalculateIdentTime(Parser.Data);

            MessageBox.Show("The result is : " + res, "Info");

            var res2 = ExtraInjectionCalculator.PrintHistogram(Parser.Data);

            MessageBox.Show(res2, "Histogram");

            var res3 = ExtraInjectionCalculator.CalculateExtraInjectionTime(Parser.Data.ToList());

            MessageBox.Show(res3.ToString(), "ExtraInjectionTime");
            ///////////////////////////////////////////////////////////////////
            return;
            //open all saved files and parse the and use the data. 
            List<string> txtFiles = new List<string>();
            var directoryPath = "C:\\Users\\veselin.ivanov\\Documents\\MultipointInj\\Acquisition";
            try
            {
                // Check if the directory exists
                if (Directory.Exists(directoryPath))
                {
                    // Get all .txt files in the directory (including subdirectories)
                    string[] files = Directory.GetFiles(directoryPath, "*.txt", SearchOption.AllDirectories);

                    foreach (var file in files)
                    {
                        txtFiles.Add(file); // Add file path to the list
                    }
                }
                else
                {
                    Console.WriteLine("The directory does not exist.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            (string, double)[] result = new (string, double)[txtFiles.Count()];
            int i = 0;
            foreach (var file in txtFiles)
            {
                var p = new Parser();
                p.Load(file);

                var res1 = ExtraInjectionCalculator.CalculateExtraInjectionTime(p.Data.ToList());
                result[i].Item1 = file;
                result[i++].Item2 = res1;
            }

        }
    }
}
