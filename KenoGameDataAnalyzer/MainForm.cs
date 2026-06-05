using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Services;

namespace LPGDataAnalyzer
{
    public partial class MainForm : Form
    {
        private readonly AppSettingManager _appSettingManager;

        private readonly AppSettings _settings;

        private DataItem[] CurrentData = [];

        public MainForm(AppSettingManager appSettingManager)
        {
            InitializeComponent();

            _appSettingManager = appSettingManager;

            _settings = _appSettingManager.Load();

            dataFilesSelectorUI1.Initialize(_settings);

            dataFilesSelectorUI1.DataLoaded += DataFilesSelectorui1_DataLoaded;
        }

        private void DataFilesSelectorui1_DataLoaded(DataItem[] data)
        {
            CurrentData = data;

            LoadParsedData(data);
        }

        private void LoadParsedData(DataItem[] data)
        {
            if (data == null || data.Length == 0)
            {
                MessageBox.Show(
                    "Invalid data.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            dataGridViewMainData.SetData(data);

            predictionControl1.LoadSettings(_appSettingManager, data);

            analysisUC.LoadParcedData(data);

            temperatureAnalyzerui1.LoadData(data);

            reducerTempCorrection1.Data = data;

            mapAnalyzerUI.LoadData(data);

            dataItemLineChartControl1.SetData(data);

            _ = showAllFileDataui1.LoadAsync(_settings.DataFilesFolder);

            UpdateSummary(data);
        }

        private void UpdateSummary(DataItem[] data)
        {
            toolStripSummary.Text =
                $"Total Rows: {data.Length} " +
                $"LPG: Min Temp: {data.Min(x => x.Temp_GAS)} " +
                $"Max Temp: {data.Max(x => x.Temp_GAS)} " +
                $"Min PRESS: {data.Min(x => x.PRESS)} " +
                $"Max PRESS: {data.Max(x => x.PRESS)} " +
                $"Average PRESS: {data.Average(x => x.PRESS).Round()} " +
                $"% Change Min: {Helper.PercentageChange(data.Average(x => x.PRESS), data.Min(x => x.PRESS)).Round()} " +
                $"Max: {Helper.PercentageChange(data.Average(x => x.PRESS), data.Max(x => x.PRESS)).Round()}";
        }

        private void buttonExtraInjectionCalculator_Click(object sender, EventArgs e)
        {
            if (CurrentData.Length == 0)
                return;

            var res = ExtraInjectionCalculator.CalculateIdentTime(CurrentData);

            MessageBox.Show("The result is : " + res, "Info");

            var res2 = ExtraInjectionCalculator.PrintHistogram(CurrentData);

            MessageBox.Show(res2, "Histogram");

            var res3 = ExtraInjectionCalculator.CalculateExtraInjectionTime(CurrentData.ToList());

            MessageBox.Show(res3.ToString(), "ExtraInjectionTime");
        }
    }
}