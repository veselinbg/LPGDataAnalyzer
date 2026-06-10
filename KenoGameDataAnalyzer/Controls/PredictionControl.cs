using KenoGameDataAnalyzer.Services;
using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Services;
using System.ComponentModel;

namespace LPGDataAnalyzer.Controls
{
    public partial class PredictionControl : UserControl
    {
        private readonly TextExtractor textExtractor = new();

        private AppSettings AppSettings { get; set; }
        private AppSettingManager AppSettingManager { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        private DataItem[] Data { get; set; }
        private string[,] Markers { get; set; }

        private IReadOnlyList<HistorySnapshot> HistorySnapshots { get; set; }
        public PredictionControl()
        {
            InitializeComponent();
            panelLegend.Paint += PanelLegendBuilder.PanelLegend_Paint;
            historyControl1.HistorySelected += HistoryControl1_HistorySelected;
        }


        public void LoadSettings(AppSettingManager appSettingManager, DataItem[] data, IReadOnlyList<HistorySnapshot> historySnapshots)
        {
            HistorySnapshots = historySnapshots;
            AppSettingManager = appSettingManager;
            AppSettings = appSettingManager.Load();
            textBoxParsedData.Text = AppSettings.LastLoadedFuelTable;
            textBoxImagePath.Text = AppSettings.ImagePath;
            textBoxLastPredictedFuelTable.Text = AppSettings.LastPredictedFuelTable;
            Data = data;
            Markers = MapRpmAnalyzer.BuildMarkers(data);

            historyControl1.AppSettings = AppSettings;
        }

        private void HistoryControl1_HistorySelected(HistorySnapshot snapshot)
        {
            if (snapshot == null)
                return;

            var cellMap = ArrayConverter.To2D(snapshot.CellMap);
            var newCellMap = ArrayConverter.To2D(snapshot.NewCellMap);
            //textBoxParsedData.Text = cellMap.ToText();
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
            IReadOnlyList<HistorySnapshot> historySnapshots = null;
            if (checkBoxUseHistory.Checked && HistorySnapshots != null)
            {
                // Load all JSON files from that folder
                historySnapshots = HistorySnapshots;
                historyControl1.ClearAddSnapshots(HistorySnapshots);
            }
            var referencePressure = double.Parse(textBoxRefPress.Text.Trim());

            var tableNew = FuelMapPrediction.BuildTable(Data, table, referencePressure, historySnapshots, textBoxMinCount.Text.Trim().ToInt(),
                checkboxEnableSmooth.Checked, checkboxInterpolation.Checked, checkBoxOnlyChanges.Checked,
                checkBoxRound.Checked, checkBoxShowOnlyMiplayerChange.Checked, textBoxMinValueOfChange.Text.Trim().ToDouble(),
                textBoxMaxBenzDiff.Text.Trim().ToDouble(), checkBoxAllwaysApplyNegativeTrim.Checked, checkBoxShowOnlyCount.Checked);

            if (checkBoxSaveSnapshot.Checked)
            {
                historyControl1.AddSnapshot(Data, table, tableNew.result);
            }

            textBoxLastPredictedFuelTable.Text = tableNew.result.ToText();
            AppSettings.LastPredictedFuelTable = textBoxLastPredictedFuelTable.Text;
            AppSettingManager.Save(AppSettings);

            PreviewPrediction(table, tableNew.result);

            DataGridViewInvalidData.DataSource = null;
            DataGridViewInvalidData.DataSource = tableNew.invalidItems;
        }

        private void ButtonParceSelectedImage_Click(object sender, EventArgs e)
        {
            textBoxParsedData.Text = textExtractor.Parcer(AppSettings.ImagePath);
        }
        private void PreviewPrediction(double?[,] table, double?[,] tableNew)
        {
            dataGridViewOrig.SetData(table, Data,"Original Data", x => x.BENZ_b1, x => x.BENZ_b2);
            dataGridViewPrediction.SetData(tableNew, Data, "Prediction Data", x => x.BENZ_b1, x => x.BENZ_b2);

            if (checkBoxShowOnlyMiplayerChange.Checked)
            {
                DataGridViewColorization.HighlightDifferencesHeatmapWithValues(dataGridViewPrediction.Grid, null, Markers, tolerance: 0.01);
            }
            else
            {
                // Apply heatmap to DataGridViews
                var vals = DataGridViewColorization.HighlightDifferencesHeatmapWithValues(dataGridViewPrediction.Grid, dataGridViewOrig.Grid, Markers, tolerance: 0.01);

                // Create horizontal legend aligned with DataGridView
                PanelLegendBuilder.CreateDynamicHorizontalHeatmapLegend(panelLegend, dataGridViewPrediction.Grid, vals.WLow, vals.WHigh);
            }
        }

        private void buttonConvert_Click(object sender, EventArgs e)
        {
            var convertedText = TextExtractor.ConvertColumnsToRows(textBoxParsedData.Text.Trim());
            textBoxParsedData.Text = convertedText;
        }

        private void buttonExport_Click(object sender, EventArgs e)
        {
            var selectedLogs = dataGridViewPrediction.GetSelectedCellLogs(Data);

            if (selectedLogs.Count == 0)
            {
                MessageBox.Show("No logs found for selected cells. You must mark some cells from Prediction grid and export them in file.");
                return;
            }

            using SaveFileDialog dlg = new();

            dlg.Filter = "Log files|*.txt";
            dlg.FileName = $"export_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            ExportLogBuilder.Build(dlg.FileName, selectedLogs);

            MessageBox.Show($"Export completed.\nRows exported: {selectedLogs.Count}");
        }
    }
}