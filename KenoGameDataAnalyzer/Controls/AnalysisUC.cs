using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Services;
using static LPGDataAnalyzer.Models.Settings;

namespace LPGDataAnalyzer.Controls
{
    public partial class AnalysisUC : UserControl
    {
        private readonly double?[][,] tables = new double?[6][,];
        private DataItem[] Data;
        public AnalysisUC()
        {
            InitializeComponent();
            comboBoxAggregationBank1.DataSource = Enum.GetValues<Aggregation>();
            comboBoxAggregationBank2.DataSource = Enum.GetValues<Aggregation>();
            comboBoxFieldsToShowBank1.DataSource = Enum.GetValues<FieldsToShow>();
            comboBoxFieldsToShowBank2.DataSource = Enum.GetValues<FieldsToShow>();
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
            checkedListGasTemperatureb1.Items.Clear();
            checkedListGasTemperatureb1.Items.AddRange(data.GetExistGasTemperatureRanges());

            checkedListGasTemperatureb2.Items.Clear();
            checkedListGasTemperatureb2.Items.AddRange(data.GetExistGasTemperatureRanges());

            checkedListReductorTempGroup1.Items.Clear();
            checkedListReductorTempGroup1.Items.AddRange(data.GetExistReductorTempGroups());

            checkedListReductorTempGroup2.Items.Clear();
            checkedListReductorTempGroup2.Items.AddRange(data.GetExistReductorTempGroups());

            checkedListGasTemperatureb1.SetItemChecked(0, true);
            checkedListGasTemperatureb2.SetItemChecked(0, true);
            checkedListReductorTempGroup1.SetItemChecked(0, true);
            checkedListReductorTempGroup2.SetItemChecked(0, true);
        }
        private void ButtonFieldsToShow_Click(object sender, EventArgs e)
        {
            if (Data == null || Data.Length == 0)
                return;

            var fieldBank1 = (FieldsToShow)comboBoxFieldsToShowBank1.SelectedItem;
            var fieldBank2 = (FieldsToShow)comboBoxFieldsToShowBank2.SelectedItem;


            BuildAnalysisTables(
                Data,
                [
                    item => item.BENZ_b1,
                    item => item.BENZ_b2
                ],
                [
                    fieldBank1.GetFieldValue(Banks.B1),
                    fieldBank1.GetFieldValue(Banks.B2),
                    fieldBank2.GetFieldValue(Banks.B1),
                    fieldBank2.GetFieldValue(Banks.B2)
                ]
            );
            // diffs
            tables[4] = Analyzer.Subtract(tables[0], tables[2]);
            tables[5] = Analyzer.Subtract(tables[1], tables[3]);

            RenderAnalyses(tables,
                [
                    item => item.BENZ_b1,
                    item => item.BENZ_b2,
                    item => item.BENZ_b1,
                    item => item.BENZ_b2,
                    item => item.BENZ_b1,
                    item => item.BENZ_b2
                ],
                [
                    $"{fieldBank1}_b1",
                    $"{fieldBank1}_b2",
                    $"{fieldBank2}_b1",
                    $"{fieldBank2}_b2",
                    "Diff_b1",
                    "Diff_b2"
                ]);
        }
       
        private void buttonShowSummary_Click(object sender, EventArgs e)
        {
            if (Data == null || Data.Length == 0)
                return;

            var aggregationBank1 = (Aggregation)comboBoxAggregationBank1.SelectedItem;
            var aggregationBank2 = (Aggregation)comboBoxAggregationBank2.SelectedItem;

            BuildAnalysisTables(
                Data,
                [item => item.BENZ, item => item.BENZ],
                [
                    item => item.BENZ_Diff,
                    item => item.MAP,
                    item => item.PRESS,
                    item => item.Trim
                ]
            );
            tables[4] = Analyzer.BuildFuelCorrectionMap(Data);
            tables[5] = Analyzer.BuildTable(Data, item => item.BENZ, item => item.TrimDiff, aggregationBank2);

            RenderAnalyses(tables,
               [item => item.BENZ, item => item.BENZ, item => item.BENZ, item => item.BENZ, item => item.BENZ, item => item.BENZ],
                ["BENZ_Diff", "Map", "PRESS", "Trim", "FuelCorrection", "TrimDiff"]);
        }

        private void BuildAnalysisTables(DataItem[] lpgdata,
    Func<DataItem, double>[] injectionBankSelectors,
    Func<DataItem, double?>[] valueSelectors)
        {
            var gasTemps = new[]
            {
                Helper.GetCheckedValues(checkedListGasTemperatureb1),
                Helper.GetCheckedValues(checkedListGasTemperatureb2)
            };

            var reductors = new[]
            {
                Helper.GetCheckedValues(checkedListReductorTempGroup1),
                Helper.GetCheckedValues(checkedListReductorTempGroup2)
            };
            var aggregations = new[] {
                (Aggregation)comboBoxAggregationBank1.SelectedItem,
                (Aggregation)comboBoxAggregationBank2.SelectedItem
            };
            BuildAnalises(lpgdata, injectionBankSelectors, valueSelectors, gasTemps, reductors, aggregations);
        }
        private void BuildAnalises(
                                    DataItem[] lpgdata,
                                    Func<DataItem, double>[] injectionBankSelectors,
                                    Func<DataItem, double?>[] valueSelectors,
                                    List<string>[] gasTemps, List<string>[] reductors, Aggregation[] aggregations)
        {
            
            for (int t = 0; t < 2; t++)
            {
                var filtered = Analyzer.FilterByTemp(lpgdata, gasTemps[t], reductors[t]);

                int baseIndex = t * 2;

                for (int b = 0; b < injectionBankSelectors.Length; b++)
                {
                    tables[baseIndex + b] = Analyzer.BuildTable(
                        filtered,
                        injectionBankSelectors[b],
                        valueSelectors[baseIndex + b],
                        aggregations[t]);
                }
            }
        }

        private void RenderAnalyses(double?[][,] tables, Func<DataItem, double>[] injectionSelectors, string[] titles)
        {
            var grids = new[]
            {
                dataGridViewAnalyzeDataBank1t1,
                dataGridViewAnalyzeDataBank2t1,
                dataGridViewAnalyzeDataBank1t2,
                dataGridViewAnalyzeDataBank2t2,
                dataGridViewAnalyzeDataBank1t3,
                dataGridViewAnalyzeDataBank2t3
            };

            for (int i = 0; i < grids.Length; i++)
            {
                var grid = grids[i];

                grid.SetData(
                             tables[i],
                             Data,
                             titles[i],
                             injectionSelectors[i]);

                DataGridViewColorization.HighlightDifferencesHeatmapWithValues(grid.Grid);
            }
        }
    }
}
