using LPGDataAnalyzer.Controls;
using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Services;
using static LPGDataAnalyzer.Models.Settings;

namespace LPGDataAnalyzer
{
    public partial class MainForm : Form
    {
        private readonly Parser Parser = new();
        private readonly Analyzer Analyser = new();
        private readonly AppSettingManager _appSettingManager;
        private AppSettings AppSettings { get; set; }
        public MainForm(AppSettingManager appSettingManager)
        {
            InitializeComponent();

            _appSettingManager = appSettingManager;
            AppSettings = _appSettingManager.Load();
            
            txtFilePath.Text = AppSettings.LastSavedFilePath;

            LoadParsedData();
            predictionControl1.LoadSettings(_appSettingManager, Parser.Data);
            reducerTempCorrection1.Data = Parser.Data;
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

        private void BtnSelectFile_Click(object sender, EventArgs e)
        {
            using OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            ofd.InitialDirectory = "C:\\Users\\veselin.ivanov\\Documents\\MultipointInj\\Acquisition";
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
                                    ], ["Ratio_b1", "Ratio_b2", "Ratio_b1", "Ratio_b2"]);
        }
        private void ButtonShowTrims_Click(object sender, EventArgs e)
        {
            if (Parser?.Data is null) return;

            BuildAnalises(Analyser, Parser.Data, [item => item.BENZ_b1, item => item.BENZ_b2, item => item.BENZ_b1, item => item.BENZ_b2], [
                            item => item.Trim_b1,
                            item => item.Trim_b2,
                            item => item.Trim_b1,
                            item => item.Trim_b2,
                                    ], ["Trim_b1", "Trim_b2", "Trim_b1", "Trim_b2"]);
        }
        private void buttonShowReducerPress_Click(object sender, EventArgs e)
        {
            if (Parser?.Data is null) return;
            BuildAnalises(Analyser, Parser.Data, [item => item.BENZ, item => item.BENZ, item => item.BENZ, item => item.BENZ], [
                 item => item.AFR,
                item => item.GAS,
                item => item.PRESS,
                item => item.Trim
             ], ["AFR", "GAS", "PRESS", "TRIM"]);
        }
        private void buttonAFR_Click(object sender, EventArgs e)
        {
            BuildAnalises(Analyser, Parser.Data, [item => item.BENZ_b1, item => item.BENZ_b2, item => item.BENZ_b1, item => item.BENZ_b2],
                [item => item.AFR_b1, item => item.AFR_b2, item => item.AFR_b1, item => item.AFR_b2], ["BENZ_b1", "BENZ_b2", "BENZ_b1", "BENZ_b2"]);
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
        void BuildAnalises(Analyzer analyser, DataItem[] lpgdata, Func<DataItem, double>[] injectionBankSelectors, Func<DataItem, double?>[] valueSelectors, string[] titles)
        {
            // Get the selected value
            var aggregator = (Aggregation)comboBoxAggregation.SelectedItem;
            //temp1
            DataItem[] filteredLPGDataByTemp1 = analyser.FilterByTemp(lpgdata, comboBoxGasTemperatureb1.SelectedValue.ToString(), comboBoxReductorTempGroup1.SelectedValue.ToString());

            dataGridViewAnalyzeDataBank1t1.SetData(analyser.BuildTable(filteredLPGDataByTemp1, injectionBankSelectors[0], valueSelectors[0], aggregator), Parser.Data, titles[0]);
            dataGridViewAnalyzeDataBank2t1.SetData(analyser.BuildTable(filteredLPGDataByTemp1, injectionBankSelectors[1], valueSelectors[1], aggregator), Parser.Data, titles[1]);
            DataGridViewColorization.HighlightDifferencesHeatmapWithValues(dataGridViewAnalyzeDataBank1t1.Grid);
            DataGridViewColorization.HighlightDifferencesHeatmapWithValues(dataGridViewAnalyzeDataBank2t1.Grid);
            //temp2
            DataItem[] filteredLPGDataByTemp2 = analyser.FilterByTemp(lpgdata, comboBoxGasTemperatureb2.SelectedValue.ToString(), comboBoxReductorTempGroup2.SelectedValue.ToString());

            dataGridViewAnalyzeDataBank1t2.SetData(analyser.BuildTable(filteredLPGDataByTemp2, injectionBankSelectors[2], valueSelectors[2], aggregator), Parser.Data, titles[2]);
            dataGridViewAnalyzeDataBank2t2.SetData(analyser.BuildTable(filteredLPGDataByTemp2, injectionBankSelectors[3], valueSelectors[3], aggregator), Parser.Data, titles[3]);
            DataGridViewColorization.HighlightDifferencesHeatmapWithValues(dataGridViewAnalyzeDataBank1t2.Grid);
            DataGridViewColorization.HighlightDifferencesHeatmapWithValues(dataGridViewAnalyzeDataBank2t2.Grid);
        }
        private void buttonGroupByTemp_Click(object sender, EventArgs e)
        {
            dataGridViewGasData.DataSource = Analyser.GroupByGasTemperature(Parser.Data, BenzTimingFilterCuting, x => x.Trim_b1, y => y.Trim_b2);
            dataGridViewRIDData.DataSource = Analyser.GroupByRIDTemperature(Parser.Data, BenzTimingFilterCuting, x => x.Trim_b1, y => y.Trim_b2);
        }
        
        public static void LoadDataSource(DataGridView dataGridView, object? dataSource)
        {
            dataGridView.DataSource = dataSource;
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
        

        private void buttonExtraInjectionCalculator_Click(object sender, EventArgs e)
        {
            var res = ExtraInjectionCalculator.CalculateIdentTime(Parser.Data);

            MessageBox.Show("The result is : " + res, "Info");

            var res2 = ExtraInjectionCalculator.PrintHistogram(Parser.Data);

            MessageBox.Show(res2, "Histogram");

            var res3 = ExtraInjectionCalculator.CalculateExtraInjectionTime(Parser.Data.ToList());

            MessageBox.Show(res3.ToString(), "ExtraInjectionTime");           
        }
    }
}
