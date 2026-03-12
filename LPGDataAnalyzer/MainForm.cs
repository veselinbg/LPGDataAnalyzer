using LPGDataAnalyzer.Controls;
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
            DataGridViewColorization.HighlightDifferencesHeatmapWithValues(dataGridViewAnalyzeDataBank1t1);
            DataGridViewColorization.HighlightDifferencesHeatmapWithValues(dataGridViewAnalyzeDataBank2t1);
            //temp2
            DataItem[] filteredLPGDataByTemp2 = analyser.FilterByTemp(lpgdata, comboBoxGasTemperatureb2.SelectedValue.ToString(), comboBoxReductorTempGroup2.SelectedValue.ToString());

            GridBuilder(dataGridViewAnalyzeDataBank1t2, analyser.BuildTable(filteredLPGDataByTemp2, injectionBankSelectors[2], valueSelectors[2], aggregator));
            GridBuilder(dataGridViewAnalyzeDataBank2t2, analyser.BuildTable(filteredLPGDataByTemp2, injectionBankSelectors[3], valueSelectors[3], aggregator));
            DataGridViewColorization.HighlightDifferencesHeatmapWithValues(dataGridViewAnalyzeDataBank1t2);
            DataGridViewColorization.HighlightDifferencesHeatmapWithValues(dataGridViewAnalyzeDataBank2t2);
        }
        private void buttonGroupByTemp_Click(object sender, EventArgs e)
        {
            dataGridViewGasData.DataSource = Analyser.GroupByGasTemperature(Parser.Data, BenzTimingFilterCuting, x => x.Trim_b1, y => y.Trim_b2);
            dataGridViewRIDData.DataSource = Analyser.GroupByRIDTemperature(Parser.Data, BenzTimingFilterCuting, x => x.BENZ_b1, y => y.BENZ_b2);
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
            var mapAnalysis = Analyser.BuildTableByMap(Parser.Data);

            LoadDataSource(dataGridViewMapAnalysis, mapAnalysis);

            //var drivingModeAnalysis = Analyser.BuildTableByDrivingRange(Parser.Data);

            //LoadDataSource(dataGridViewInjectionTimeAnalisys, drivingModeAnalysis.ToList());

            var bankTobank = Analyser.BuildBankToBankfuelBalance(Parser.Data);
            LoadDataSource(dataGridView1, bankTobank);

        }

        private void button1_Click(object sender, EventArgs e)
        {
            var a1 = Analyser.LpgTemperatureVsInjectionTime(Parser.Data);

            LoadDataSource(dataGridViewMapAnalysis, a1);

            var a2 = Analyser.BuildABankAwareLPGBaseMap(Parser.Data);

            LoadDataSource(dataGridViewInjectionTimeAnalisys, a2);

            var a3 = Analyser.LpgInjectorDeadTimeEstimation(Parser.Data);
            LoadDataSource(dataGridView1, a3);
        }

        private void buttonReducerThermalLag_Click(object sender, EventArgs e)
        {
            var a3 = Analyser.ReducerThermalLag(Parser.Data);
            LoadDataSource(dataGridView1, a3);
        }
        private void buttonParceSelectedImage_Click(object sender, EventArgs e)
        {
            textBoxParsedData.Text = textExtractor.Parcer(AppSettings.ImagePath);
        }

        private void ButtonPredict_Click(object sender, EventArgs e)
        {
            var table = textExtractor.BuildFinalTable(textBoxParsedData.Text);

            GridBuilder(dataGridViewOrig, table);
            table = MyPrediction.BuildTable(Parser.Data, table,
                cbEnableSmooth.Checked, cbInterpolation.Checked, checkBoxOnlyChanges.Checked, checkBoxRound.Checked);
            //Auto-correction algorithm
            //new Prediction().AutoCorrectFuelTable(data, table, cbEnableSmooth.Checked);

            GridBuilder(dataGridViewPrediction, table);

            // Apply heatmap to DataGridViews
            var vals = DataGridViewColorization.HighlightDifferencesHeatmapWithValues(dataGridViewPrediction, dataGridViewOrig, tolerance: 0.01);

            // Create horizontal legend aligned with DataGridView
            LegendPanelBuilder.CreateDynamicHorizontalHeatmapLegend(panelLegend, dataGridViewPrediction, vals.WLow, vals.WHigh);

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
