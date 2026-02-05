using System.Windows.Forms;

namespace LPGDataAnalyzer
{
    public partial class MainForm : Form
    {
        private LPGDataParser lPGDataParser = new();
        private LPGDataAnalyzer analyser = new();

        public MainForm()
        {
            InitializeComponent();

            comboBoxTemperature1.DataSource = LPGDataAnalyzer.LPGTempGroups.Clone();
            comboBoxTemperature1.SelectedIndex = 0;

            comboBoxTemperature2.DataSource = LPGDataAnalyzer.LPGTempGroups.Clone();
            comboBoxTemperature2.SelectedIndex = 1;

            comboBoxReductorTempGroup1.DataSource = LPGDataAnalyzer.ReductorTempGroups.Clone();
            comboBoxReductorTempGroup1.SelectedIndex = 0;

            comboBoxReductorTempGroup2.DataSource = LPGDataAnalyzer.ReductorTempGroups.Clone();
            comboBoxReductorTempGroup2.SelectedIndex = 1;

        }
        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            using OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtFilePath.Text = ofd.FileName;
                lPGDataParser.Load(ofd.FileName);

                if (lPGDataParser.Data.Any())
                {
                    dataGridViewLPGData.DataSource = lPGDataParser.Data;
                    buttonAnalyze.Enabled = true;
                    buttonAnalyzeFastTrim.Enabled = true;
                    toolStripSummary.Text = $"Total Rows: {lPGDataParser.Data.Count} " +
                        $"LPG: Min Temp: {lPGDataParser.Data.Select(x => x.Temp_GAS).Min()} Max Temp: {lPGDataParser.Data.Select(x => x.Temp_GAS).Max()}" +
                        $" Min PRESS: {lPGDataParser.Data.Select(x => x.PRESS).Min()} Max PRESS: {lPGDataParser.Data.Select(x => x.PRESS).Max()} Avarige PRESS: {Math.Round(lPGDataParser.Data.Select(x => x.PRESS).Average(), 2)}" +
                        $" % of change Min {Math.Round(LPGDataAnalyzer.PercentageChange(lPGDataParser.Data.Select(x => x.PRESS).Average(), lPGDataParser.Data.Select(x => x.PRESS).Min()),2)} Max{Math.Round(LPGDataAnalyzer.PercentageChange(lPGDataParser.Data.Select(x => x.PRESS).Average(), lPGDataParser.Data.Select(x => x.PRESS).Max()), 2)}";
                }
                else MessageBox.Show("Invalid data.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonAnalyze_Click(object sender, EventArgs e)
        {
            if (lPGDataParser?.Data is null) return;

            dataGridViewGroupByTemp.DataSource = analyser.GroupByTemperature(lPGDataParser.Data, BenzTimingFilterCuting, x => x.Ratio_b1, y => y.Ratio_b2);

            BuildAnalises(analyser, lPGDataParser.Data, x => x.Ratio_b1, x => x.Ratio_b2);
        }
        private void buttonAnalyze2_Click(object sender, EventArgs e)
        {
            if (lPGDataParser?.Data is null) return;

            dataGridViewGroupByTemp.DataSource = analyser.GroupByTemperature(lPGDataParser.Data, BenzTimingFilterCuting, x => x.FAST_b1, y => y.FAST_b2);

            BuildAnalises(analyser, lPGDataParser.Data, x => x.FAST_b1, x => x.FAST_b2);
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
        void BuildAnalises(LPGDataAnalyzer analyser, IEnumerable<LPGData> lpgdata, Func<LPGData, double?> selector1, Func<LPGData, double?> selector2)
        {
            //temp1
            IEnumerable<LPGData> filteredLPGDataByTemp1 = analyser.FilterByTemp(lpgdata, comboBoxTemperature1.SelectedValue.ToString(), comboBoxReductorTempGroup1.SelectedValue.ToString());

            GridBuilder(dataGridViewAnalyzeDataBank1t1, analyser.BuildTable(filteredLPGDataByTemp1, x => x.BENZ_b1, selector1));
            GridBuilder(dataGridViewAnalyzeDataBank2t1, analyser.BuildTable(filteredLPGDataByTemp1, x => x.BENZ_b2, selector2));

            //temp2
            IEnumerable<LPGData> filteredLPGDataByTemp2 = analyser.FilterByTemp(lpgdata, comboBoxTemperature2.SelectedValue.ToString(), comboBoxReductorTempGroup2.SelectedValue.ToString());

            GridBuilder(dataGridViewAnalyzeDataBank1t2, analyser.BuildTable(filteredLPGDataByTemp2, x => x.BENZ_b1, selector1));
            GridBuilder(dataGridViewAnalyzeDataBank2t2, analyser.BuildTable(filteredLPGDataByTemp2, x => x.BENZ_b2, selector2));
        }

        void GridBuilder(DataGridView dgv, IEnumerable<RatioTableRow> data)
        {
            PrepareGrid(dgv);

            CreateColumns(dgv, LPGDataAnalyzer.RpmColumns.Select(x => x.Label));

            FillRows(dgv, data);
        }
        void PrepareGrid(DataGridView dgv)
        {
            dgv.Columns.Clear();
            dgv.Rows.Clear();
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        }
        void CreateColumns(DataGridView dgv, IEnumerable<int> rpmColumns)
        {
            dgv.Columns.Add("InjectionTime", "Inj.Time");

            foreach (int rpm in rpmColumns)
            {
                dgv.Columns.Add($"RPM_{rpm}", rpm.ToString());
            }
        }
        void FillRows(DataGridView dgv, IEnumerable<RatioTableRow> table)
        {
            foreach (var row in table)
            {
                int idx = dgv.Rows.Add();

                dgv.Rows[idx].Cells["InjectionTime"].Value = row.InjectionTime;

                foreach (var col in row.Columns)
                {
                    dgv.Rows[idx].Cells[$"RPM_{col.Key}"].Value =
                        col.Value.HasValue
                            ? Math.Round(col.Value.Value, 2)
                            : (object)null;
                }
            }
        }

        private void dataGridViewAnalyzeDataBank1t1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0)
            {
                return;
            }

            var range = LPGDataAnalyzer.InjectionRanges[e.RowIndex];

            var rpm = LPGDataAnalyzer.RpmColumns[e.ColumnIndex - 1];

            var data = lPGDataParser.Data.Where(x => x.RPM > rpm.Min && x.RPM <= rpm.Max && x.BENZ_b1 > range.Min && x.BENZ_b1 <= range.Max);

            var message = string.Join("\r \n", data.GroupBy(x=>x.Ratio_b1).Select(x=> $"Ratio_b1:{x.Key} Count: {x.Count()} PRESS: {Math.Round(data.Where(y=>y.Ratio_b1 ==x.Key).Select(y=>y.PRESS).Average(), 2)}"));

            MessageBox.Show(message,"Info");
        }
    }
}
