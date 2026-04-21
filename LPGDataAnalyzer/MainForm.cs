using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Services;

namespace LPGDataAnalyzer
{
    public partial class MainForm : Form
    {
        private readonly Parser Parser = new();
        private readonly AppSettingManager _appSettingManager;
        private AppSettings AppSettings { get; set; }
        public MainForm(AppSettingManager appSettingManager)
        {
            InitializeComponent();

            _appSettingManager = appSettingManager;
            AppSettings = _appSettingManager.Load();

            txtFilePath.Text = AppSettings.LastSavedFilePath;

            LoadParsedData();
        }

        private void BtnSelectFile_Click(object sender, EventArgs e)
        {
            using OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            ofd.InitialDirectory = AppSettings.DataFilesFolder; 
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

            if (Parser.Data.Length != 0)
            {
                dataGridViewMainData.SetData(Parser.Data);

                predictionControl1.LoadSettings(_appSettingManager, Parser.Data);
                analysisUC.LoadParcedData(Parser.Data);
                temperatureAnalyzerui1.LoadData(Parser.Data);
                reducerTempCorrection1.Data = Parser.Data;

                mapAnalyzerUI.LoadData(Parser.Data);

                dataItemLineChartControl1.SetData(Parser.Data);

                _ = showAllFileDataui1.LoadAsync(AppSettings.DataFilesFolder);

                toolStripSummary.Text = $"Total Rows: {Parser.Data.Length} " +
                    $"LPG: Min Temp: {Parser.Data.Min(x => x.Temp_GAS)} Max Temp: {Parser.Data.Max(x => x.Temp_GAS)}" +
                    $" Min PRESS: {Parser.Data.Min(x => x.PRESS)} Max PRESS: {Parser.Data.Max(x => x.PRESS)} Avarige PRESS: {(Parser.Data.Average(x => x.PRESS)).Round()}" +
                    $" % of change Min {Helper.PercentageChange(Parser.Data.Average(x => x.PRESS), Parser.Data.Min(x => x.PRESS)).Round()} Max{Helper.PercentageChange(Parser.Data.Average(x => x.PRESS), Parser.Data.Max(x => x.PRESS)).Round()}";
            }
            else MessageBox.Show("Invalid data.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
