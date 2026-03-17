using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Services;
using System;
using System.ComponentModel;
using System.Windows.Forms;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LPGDataAnalyzer.Controls
{
    public partial class PredictionControl : UserControl
    {
        private readonly TextExtractor textExtractor = new();
        private AppSettings _appSettings { get; set; }
        private AppSettingManager _appSettingManager { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataItem[] Data { get; set; }
        public PredictionControl()
        {
            InitializeComponent();
        }
        public void LoadSettings(AppSettingManager appSettingManager, DataItem[] data)
        {
            _appSettingManager = appSettingManager;
            _appSettings = appSettingManager.Load();
            textBoxParsedData.Text = _appSettings.LastLoadedFuelTable;
            textBoxImagePath.Text = _appSettings.ImagePath;
            textBoxLastPredictedFuelTable.Text = _appSettings.LastPredictedFuelTable;
            Data = data;
            historyControl1.HistorySelected += HistoryControl1_HistorySelected;
        }

        private void HistoryControl1_HistorySelected(HistorySnapshot snapshot)
        {
            if (snapshot == null)
                return;

            var cellMap = ArrayConverter.To2D(snapshot.CellMap);
            var newCellMap = ArrayConverter.To2D(snapshot.NewCellMap);
            var logs = snapshot.Logs;
            PreviewPrediction(cellMap, newCellMap);
            // apply to your system
        }
        private void ButtonValidate_Click(object sender, EventArgs e)
        {
            try
            {
                textExtractor.Validate(textBoxParsedData.Text);
                _appSettings.LastLoadedFuelTable = textBoxParsedData.Text;
                _appSettings.LastPredictedFuelTable = textBoxLastPredictedFuelTable.Text;
                _appSettingManager.Save(_appSettings);
                MessageBox.Show("Ok, no errors!", "Info");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Errors");
            }
        }

        private void ButtonPredict_Click(object sender, EventArgs e)
        {
            var table = textExtractor.BuildFinalTable(textBoxParsedData.Text);

            var tableNew = MyPrediction.BuildTable(Data, table, int.Parse(textBoxMinCount.Text.Trim()),
                cbEnableSmooth.Checked, cbInterpolation.Checked, checkBoxOnlyChanges.Checked, checkBoxRound.Checked, checkBoxPreFilter.Checked);

            if (checkBoxSaveSnapshot.Checked)
            {
                historyControl1.AddSnapshot(Data, table, tableNew);
            }

            textBoxLastPredictedFuelTable.Text = tableNew.ToText();
            _appSettings.LastPredictedFuelTable = textBoxLastPredictedFuelTable.Text;
            _appSettingManager.Save(_appSettings);

            PreviewPrediction(table, tableNew);
        }

        private void ButtonParceSelectedImage_Click(object sender, EventArgs e)
        {
            textBoxParsedData.Text = textExtractor.Parcer(_appSettings.ImagePath);
        }
        private void PreviewPrediction(double?[,] table, double?[,] tableNew)
        {
            dataGridViewOrig.SetData(table);
            dataGridViewPrediction.SetData(tableNew);

            // Apply heatmap to DataGridViews
            var vals = DataGridViewColorization.HighlightDifferencesHeatmapWithValues(dataGridViewPrediction.Grid, dataGridViewOrig.Grid, tolerance: 0.01);

            // Create horizontal legend aligned with DataGridView
            LegendPanelBuilder.CreateDynamicHorizontalHeatmapLegend(panelLegend, dataGridViewPrediction.Grid, vals.WLow, vals.WHigh);

        }

    }
}