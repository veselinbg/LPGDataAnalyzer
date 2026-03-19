using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Services;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace LPGDataAnalyzer.Controls
{
    public partial class PredictionControl : UserControl
    {
        private readonly TextExtractor textExtractor = new();
        // Create a history manager
        private readonly HistoryManager historyManager = new();

        private AppSettings AppSettings { get; set; }
        private AppSettingManager AppSettingManager { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataItem[] Data { get; set; }
        public PredictionControl()
        {
            InitializeComponent();
        }
        public void LoadSettings(AppSettingManager appSettingManager, DataItem[] data)
        {
            AppSettingManager = appSettingManager;
            AppSettings = appSettingManager.Load();
            textBoxParsedData.Text = AppSettings.LastLoadedFuelTable;
            textBoxImagePath.Text = AppSettings.ImagePath;
            textBoxLastPredictedFuelTable.Text = AppSettings.LastPredictedFuelTable;
            Data = data;
            historyControl1.HistorySelected += HistoryControl1_HistorySelected;
        }

        private void HistoryControl1_HistorySelected(HistorySnapshot snapshot)
        {
            if (snapshot == null)
                return;

            var cellMap = ArrayConverter.To2D(snapshot.CellMap);
            var newCellMap = ArrayConverter.To2D(snapshot.NewCellMap);

            textBoxLastPredictedFuelTable.Text = newCellMap.ToText();

            PreviewPrediction(cellMap, newCellMap);
        }
        private void ButtonValidate_Click(object sender, EventArgs e)
        {
            try
            {
                textExtractor.Validate(textBoxParsedData.Text);
                AppSettings.LastLoadedFuelTable = textBoxParsedData.Text;
                AppSettings.LastPredictedFuelTable = textBoxLastPredictedFuelTable.Text;
                AppSettingManager.Save(AppSettings);
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
            
            // Load all JSON files from that folder
            historyManager.ClearAndLoadFromDirectory(AppSettings.HistoryFolder);

            // Get all loaded snapshots as a list
            var historySnapshots = historyManager.Items.ToArray();
            historyControl1.ClearAddSnapshots(historySnapshots);

            historySnapshots = checkBoxUseHistory.Checked? historySnapshots: null;

            var tableNew = MyPrediction.BuildTable(Data, table, historySnapshots, textBoxMinCount.Text.Trim().ToInt(),
                cbEnableSmooth.Checked, cbInterpolation.Checked, checkBoxOnlyChanges.Checked, 
                checkBoxRound.Checked, checkBoxPreFilter.Checked, checkBoxShowOnlyMiplayerChange.Checked, textBoxMinValueOfChange.Text.Trim().ToDouble());

            if (checkBoxSaveSnapshot.Checked)
            {
                historyControl1.AddSnapshot(Data, table, tableNew);
            }

            textBoxLastPredictedFuelTable.Text = tableNew.ToText();
            AppSettings.LastPredictedFuelTable = textBoxLastPredictedFuelTable.Text;
            AppSettingManager.Save(AppSettings);

            PreviewPrediction(table, tableNew);
        }

        private void ButtonParceSelectedImage_Click(object sender, EventArgs e)
        {
            textBoxParsedData.Text = textExtractor.Parcer(AppSettings.ImagePath);
        }
        private void PreviewPrediction(double?[,] table, double?[,] tableNew)
        {
            dataGridViewOrig.SetData(table, Data);
            dataGridViewPrediction.SetData(tableNew, Data);

            if(checkBoxShowOnlyMiplayerChange.Checked)
            {
                DataGridViewColorization.HighlightDifferencesHeatmapWithValues(dataGridViewPrediction.Grid, null, tolerance: 0.01);
            }
            else
            {
                // Apply heatmap to DataGridViews
                var vals = DataGridViewColorization.HighlightDifferencesHeatmapWithValues(dataGridViewPrediction.Grid, dataGridViewOrig.Grid, tolerance: 0.01);

                // Create horizontal legend aligned with DataGridView
                LegendPanelBuilder.CreateDynamicHorizontalHeatmapLegend(panelLegend, dataGridViewPrediction.Grid, vals.WLow, vals.WHigh);
            }
        }
    }
}