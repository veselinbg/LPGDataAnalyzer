using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Services;
using static LPGDataAnalyzer.Models.Settings;

namespace LPGDataAnalyzer.Controls
{
    public partial class AnalysisUC : UserControl
    {
        DataItem[] Data { get; set; }
        public AnalysisUC()
        {
            InitializeComponent();
            comboBoxAggregation.DataSource = Enum.GetValues<Aggregation>();
            comboBoxFieldsToShow.DataSource = Enum.GetValues<FieldsToShow>();
        }
        private bool _isUpdating = false;

        private void CheckedListBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_isUpdating) return;

            var list = sender as CheckedListBox;
            if (list == null || list.Items.Count == 0)
                return;

            _isUpdating = true;

            try
            {
                // ✅ Case 1: "All" clicked
                if (list.Items[e.Index].ToString() == Settings.ALL)
                {
                    bool checkAll = e.NewValue == CheckState.Checked;

                    for (int i = 0; i < list.Items.Count; i++)
                    {
                        list.SetItemChecked(i, checkAll);
                    }
                }
                else
                {
                    // ✅ Case 2: individual item changed

                    if (e.NewValue == CheckState.Unchecked)
                    {
                        // If anything is unchecked → uncheck "All"
                        list.SetItemChecked(0, false);
                    }
                    else
                    {
                        // Check if all items (except "All") are checked
                        bool allChecked = true;

                        for (int i = 1; i < list.Items.Count; i++)
                        {
                            if (i == e.Index) continue;

                            if (!list.GetItemChecked(i))
                            {
                                allChecked = false;
                                break;
                            }
                        }

                        // include current item being checked
                        if (allChecked)
                            list.SetItemChecked(0, true);
                    }
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }
        public void LoadParcedData(DataItem[] data)
        {
            Data = data;
            checkedListGasTemperatureb1.Items.AddRange(GetExistGasTemperatureRanges(data));
            checkedListGasTemperatureb2.Items.AddRange(GetExistGasTemperatureRanges(data));

            checkedListReductorTempGroup1.Items.AddRange(GetExistReductorTempGroups(data));
            checkedListReductorTempGroup2.Items.AddRange(GetExistReductorTempGroups(data));

            checkedListGasTemperatureb1.SetItemChecked(0, true);
            checkedListGasTemperatureb2.SetItemChecked(0, true);
            checkedListReductorTempGroup1.SetItemChecked(0, true);
            checkedListReductorTempGroup2.SetItemChecked(0, true);
        }
        
        private void ButtonFieldsToShow_Click(object sender, EventArgs e)
        {
            var aggregator = (FieldsToShow)comboBoxFieldsToShow.SelectedItem;


            BuildAnalises(Data, [item => item.BENZ_b1, item => item.BENZ_b2, item => item.BENZ_b1, item => item.BENZ_b2], [
                            aggregator.GetFieldValue( Banks.B1),
                            aggregator.GetFieldValue( Banks.B2),
                            aggregator.GetFieldValue( Banks.B1),
                            aggregator.GetFieldValue( Banks.B2),
                                    ], [$"{aggregator}_b1", $"{aggregator}_b2", $"{aggregator}_b1", $"{aggregator}_b2"]);
        }
        
        private void buttonShowSummary_Click(object sender, EventArgs e)
        {
            BuildAnalises(Data, [item => item.BENZ, item => item.BENZ, item => item.BENZ, item => item.BENZ], [
                 item => item.BENZ_Diff,
                item => item.MAP,
                item => item.PRESS,
                item => item.Trim
             ], ["BENZ_Diff", "Map", "PRESS", "TRIM"]);
        }
        
        void BuildAnalises(DataItem[] lpgdata, Func<DataItem, double>[] injectionBankSelectors, Func<DataItem, double?>[] valueSelectors, string[] titles)
        {
            // Get the selected value
            var aggregator = (Aggregation)comboBoxAggregation.SelectedItem;

            var gasTemps1 = Helper.GetCheckedValues(checkedListGasTemperatureb1);
            var reductors1 = Helper.GetCheckedValues(checkedListReductorTempGroup1);

            var gasTemps2 = Helper.GetCheckedValues(checkedListGasTemperatureb2);
            var reductors2 = Helper.GetCheckedValues(checkedListReductorTempGroup2);

            //temp1
            DataItem[] filteredLPGDataByTemp1 = Analyzer.FilterByTemp(lpgdata, gasTemps1, reductors1);

            dataGridViewAnalyzeDataBank1t1.SetData(Analyzer.BuildTable(filteredLPGDataByTemp1, injectionBankSelectors[0], valueSelectors[0], aggregator), Data, titles[0]);
            dataGridViewAnalyzeDataBank2t1.SetData(Analyzer.BuildTable(filteredLPGDataByTemp1, injectionBankSelectors[1], valueSelectors[1], aggregator), Data, titles[1]);
            DataGridViewColorization.HighlightDifferencesHeatmapWithValues(dataGridViewAnalyzeDataBank1t1.Grid);
            DataGridViewColorization.HighlightDifferencesHeatmapWithValues(dataGridViewAnalyzeDataBank2t1.Grid);
            //temp2
            DataItem[] filteredLPGDataByTemp2 = Analyzer.FilterByTemp(lpgdata, gasTemps2, reductors2);

            dataGridViewAnalyzeDataBank1t2.SetData(Analyzer.BuildTable(filteredLPGDataByTemp2, injectionBankSelectors[2], valueSelectors[2], aggregator), Data, titles[2]);
            dataGridViewAnalyzeDataBank2t2.SetData(Analyzer.BuildTable(filteredLPGDataByTemp2, injectionBankSelectors[3], valueSelectors[3], aggregator), Data, titles[3]);
            DataGridViewColorization.HighlightDifferencesHeatmapWithValues(dataGridViewAnalyzeDataBank1t2.Grid);
            DataGridViewColorization.HighlightDifferencesHeatmapWithValues(dataGridViewAnalyzeDataBank2t2.Grid);
        }

    }
}
