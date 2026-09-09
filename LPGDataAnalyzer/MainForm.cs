using LPGDataAnalyzer.Controls;
using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Services;
using System.Text;

namespace LPGDataAnalyzer
{
    public partial class MainForm : Form
    {
        private readonly AppSettingManager _appSettingManager;

        private readonly AppSettings _settings;

        private DataItem[]? CurrentData;
        // Create a history manager
        private readonly HistoryManager historyManager = new();
        public MainForm(AppSettingManager appSettingManager)
        {
            InitializeComponent();

            _appSettingManager = appSettingManager;

            _settings = _appSettingManager.Load();

            dataFilesSelectorUI1.Initialize(_settings);

            historyManager.ClearAndLoadFromDirectory(_settings.HistoryFolder);

            showAllStoredData.LoadSnapshots(historyManager.Items);
            
            _ = showAllFileDataui1.LoadAsync(_settings.DataFilesFolder);

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

            predictionControl1.LoadSettings(_appSettingManager, data, historyManager.Items);

            analysisUC.LoadParcedData(data);

            temperatureAnalyzerui1.LoadData(data);

            reducerTempCorrection1.Data = data;

            mapAnalyzerUI.LoadData(data);

            dataItemLineChartControl1.SetData(data);

            UpdateSummary(data);
        }

        private void UpdateSummary(DataItem[] data)
        {
            var avgPress = data.Average(x => x.PRESS);
            var minPress = data.Min(x => x.PRESS);
            var maxPress = data.Max(x => x.PRESS);
            var minTempGas = data.Min(x => x.Temp_GAS);
            var maxTempGas = data.Max(x => x.Temp_GAS);

            var sb = new StringBuilder();

            sb.Append($"Total Rows: {data.Length} ")
              .Append($"LPG: Min Temp: {minTempGas} ")
              .Append($"Max Temp: {maxTempGas} ")
              .Append($"Min PRESS: {minPress} ")
              .Append($"Max PRESS: {maxPress} ")
              .Append($"Average PRESS: {avgPress.Round()} ")
              .Append($"% Change Min: {Helper.PercentageChange(avgPress, minPress).Round()} ")
              .Append($"Max: {Helper.PercentageChange(avgPress, maxPress).Round()}");

            toolStripSummary.Text = sb.ToString();
        }

        private void buttonExtraInjectionCalculator_Click(object sender, EventArgs e)
        {
            if (CurrentData?.Length == 0)
                return;

            var res = ExtraInjectionCalculator.CalculateIdentTime(CurrentData);

            MessageBox.Show("The result is : " + res, "Info");

            var res2 = ExtraInjectionCalculator.PrintHistogram(CurrentData);

            MessageBox.Show(res2, "Histogram");

            var res3 = ExtraInjectionCalculator.CalculateExtraInjectionTime(CurrentData);

            MessageBox.Show(res3.ToString(), "ExtraInjectionTime");
        }
    }
}