using LPGDataAnalyzer.Models;

namespace LPGDataAnalyzer
{
    internal partial class MainForm : Form
    {
        private readonly Parser Parser = new();
        private readonly Analyzer Analyser = new();
        private readonly TextExtractor textExtractor = new();
        private readonly AppSettingManager _appSettingManager;
        private AppSettings AppSettings { get; set; }
        public MainForm(AppSettingManager appSettingManager)
        {
            InitializeComponent();

            _appSettingManager = appSettingManager;
            AppSettings = _appSettingManager.Load();
            txtFilePath.Text = AppSettings.LastSavedFilePath;
            textBoxParsedData.Text = AppSettings.LastLoadedFuelTable;
            textBoxImagePath.Text = AppSettings.ImagePath;
            LoadParsedData();

            comboBoxTemperature1.DataSource = Settings.LPGTempGroups.Clone();
            comboBoxTemperature1.SelectedIndex = 0;

            comboBoxTemperature2.DataSource = Settings.LPGTempGroups.Clone();
            comboBoxTemperature2.SelectedIndex = 1;

            comboBoxReductorTempGroup1.DataSource = Settings.ReductorTempGroups.Clone();
            comboBoxReductorTempGroup1.SelectedIndex = 0;

            comboBoxReductorTempGroup2.DataSource = Settings.ReductorTempGroups.Clone();
            comboBoxReductorTempGroup2.SelectedIndex = 1;

        }
        private void BtnSelectFile_Click(object sender, EventArgs e)
        {
            using OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";

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
                dataGridViewLPGData.DataSource = Parser.Data;
                buttonAnalyze.Enabled = true;
                buttonAnalyzeFastTrim.Enabled = true;

                toolStripSummary.Text = $"Total Rows: {Parser.Data.Count} " +
                    $"LPG: Min Temp: {Parser.Data.Min(x => x.Temp_GAS)} Max Temp: {Parser.Data.Max(x => x.Temp_GAS)}" +
                    $" Min PRESS: {Parser.Data.Min(x => x.PRESS)} Max PRESS: {Parser.Data.Max(x => x.PRESS)} Avarige PRESS: {(Parser.Data.Average(x => x.PRESS)).Round()}" +
                    $" % of change Min {Analyzer.PercentageChange(Parser.Data.Average(x => x.PRESS), Parser.Data.Min(x => x.PRESS)).Round()} Max{Analyzer.PercentageChange(Parser.Data.Average(x => x.PRESS), Parser.Data.Max(x => x.PRESS)).Round()}";
            }
            else MessageBox.Show("Invalid data.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        private void ButtonAnalyze_Click(object sender, EventArgs e)
        {
            if (Parser?.Data is null) return;

            dataGridViewGroupByTemp.DataSource = Analyser.GroupByTemperature(Parser.Data, BenzTimingFilterCuting, x => x.Ratio_b1, y => y.Ratio_b2);

            BuildAnalises(Analyser, Parser.Data, x => x.Ratio_b1, x => x.Ratio_b2);
        }
        private void ButtonAnalyze2_Click(object sender, EventArgs e)
        {
            if (Parser?.Data is null) return;

            dataGridViewGroupByTemp.DataSource = Analyser.GroupByTemperature(Parser.Data, BenzTimingFilterCuting, x => x.FAST_b1, y => y.FAST_b2);

            BuildAnalises(Analyser, Parser.Data, x => x.FAST_b1, x => x.FAST_b2);
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
        void BuildAnalises(Analyzer analyser, IEnumerable<DataItem> lpgdata, Func<DataItem, double?> selector1, Func<DataItem, double?> selector2)
        {
            //temp1
            IEnumerable<DataItem> filteredLPGDataByTemp1 = analyser.FilterByTemp(lpgdata, comboBoxTemperature1.SelectedValue.ToString(), comboBoxReductorTempGroup1.SelectedValue.ToString());

            GridBuilder(dataGridViewAnalyzeDataBank1t1, analyser.BuildTable(filteredLPGDataByTemp1, x => x.BENZ_b1, selector1));
            GridBuilder(dataGridViewAnalyzeDataBank2t1, analyser.BuildTable(filteredLPGDataByTemp1, x => x.BENZ_b2, selector2));

            //temp2
            IEnumerable<DataItem> filteredLPGDataByTemp2 = analyser.FilterByTemp(lpgdata, comboBoxTemperature2.SelectedValue.ToString(), comboBoxReductorTempGroup2.SelectedValue.ToString());

            GridBuilder(dataGridViewAnalyzeDataBank1t2, analyser.BuildTable(filteredLPGDataByTemp2, x => x.BENZ_b1, selector1));
            GridBuilder(dataGridViewAnalyzeDataBank2t2, analyser.BuildTable(filteredLPGDataByTemp2, x => x.BENZ_b2, selector2));
        }

        void GridBuilder(DataGridView dgv, IEnumerable<TableRow> data)
        {
            PrepareGrid(dgv);

            CreateColumns(dgv, Settings.RpmColumns.Select(x => x.Label));

            FillRows(dgv, data);
        }
        static void PrepareGrid(DataGridView dgv)
        {
            dgv.DataSource = null;
            dgv.Columns.Clear();
            dgv.Rows.Clear();
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgv.SelectionMode = DataGridViewSelectionMode.CellSelect;
        }
        static void LoadDataSource(DataGridView dgv, object? dataSource)
        {
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.DataSource = dataSource;
        }
        void CreateColumns(DataGridView dgv, IEnumerable<int> rpmColumns)
        {
            dgv.Columns.Add("InjectionTime", "Inj.Time");

            foreach (int rpm in rpmColumns)
            {
                dgv.Columns.Add($"RPM_{rpm}", rpm.ToString());
            }
        }
        void FillRows(DataGridView dgv, IEnumerable<TableRow> table)
        {
            foreach (var row in table)
            {
                int idx = dgv.Rows.Add();

                dgv.Rows[idx].Cells["InjectionTime"].Value = row.Key;

                foreach (var col in row.Columns)
                {
                    dgv.Rows[idx].Cells[$"RPM_{col.Key}"].Value =
                        col.Value.HasValue
                            ? Math.Round(col.Value.Value, 2)
                            : null;
                }
            }
        }
        private void buttonAFR_Click(object sender, EventArgs e)
        {
            BuildAnalises(Analyser, Parser.Data, s => (15.6 / (1.0 + ((s.FAST_b1 + s.SLOW_b1) / 100.0))).Round(), s => (15.6 / (1.0 + ((s.FAST_b2 + s.SLOW_b2) / 100.0))).Round());
        }

        private void dataGridViewAnalyzeDataBank1t1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0)
            {
                return;
            }

            var range = Settings.InjectionRanges[e.RowIndex];

            var rpm = Settings.RpmColumns[e.ColumnIndex - 1];

            var data = Parser.Data.Where(x => x.RPM > rpm.Min && x.RPM <= rpm.Max && x.BENZ_b1 > range.Min && x.BENZ_b1 <= range.Max);

            var message = string.Join("\r \n", data.GroupBy(x => x.Ratio_b1).Select(x => $"Ratio_b1:{x.Key} Count: {x.Count()} PRESS: {Math.Round(data.Where(y => y.Ratio_b1 == x.Key).Select(y => y.PRESS).Average(), 2)}"));

            MessageBox.Show(message, "Info");
        }

        private void buttonAnalysisByMap_Click(object sender, EventArgs e)
        {
            var mapAnalysis = Analyser.BuildTableMap(Parser.Data);

            LoadDataSource(dataGridViewMapAnalysis, mapAnalysis.ToList());

            var drivingModeAnalysis = Analyser.BuildTableDrivingModes(Parser.Data);

            LoadDataSource(dataGridViewInjectionTimeAnalisys, drivingModeAnalysis.ToList());

            var bankTobank = Analyser.BuildBankToBankfuelBalance(Parser.Data);
            LoadDataSource(dataGridView1, bankTobank.ToList());

        }

        private void button1_Click(object sender, EventArgs e)
        {
            var a1 = Analyser.LpgTemperatureVsInjectionTime(Parser.Data);

            LoadDataSource(dataGridViewMapAnalysis, a1.ToList());

            var a2 = Analyser.BuildABankAwareLPGBaseMap(Parser.Data);

            LoadDataSource(dataGridViewInjectionTimeAnalisys, a2.ToList());

            var a3 = Analyser.LpgInjectorDeadTimeEstimation(Parser.Data);
            LoadDataSource(dataGridView1, a3.ToList());

        }

        private void button2_Click(object sender, EventArgs e)
        {
            var a3 = Analyser.ReducerThermalLag(Parser.Data);
            LoadDataSource(dataGridView1, a3.ToList());

            var a1 = Analyser.CalculateAFR(Parser.Data);

            LoadDataSource(dataGridViewMapAnalysis, a1.ToList());

        }
        private void buttonPrediction_Click(object sender, EventArgs e)
        {
            textBoxParsedData.Text = textExtractor.Parcer(AppSettings.ImagePath); 
        }

        private void ButtonContinue_Click(object sender, EventArgs e)
        {
            var fuelCellTable = textExtractor.BuildFinalTable(textBoxParsedData.Text);
           
            GridBuilder(dataGridViewOrig, FuelCellBuilder.BuildTableRow(fuelCellTable));

            //Auto-correction algorithm
            var newfuelTable = new Prediction().AutoCorrectFuelTable(Parser.Data, fuelCellTable);

            GridBuilder(dataGridViewPrediction, FuelCellBuilder.BuildTableRow(newfuelTable));
        }

        private void ButtonValidate_Click(object sender, EventArgs e)
        {
            try
            {
                textExtractor.Validate(textBoxParsedData.Text);
                AppSettings.LastLoadedFuelTable = textBoxParsedData.Text;
                _appSettingManager.Save(AppSettings);
                MessageBox.Show("Ok, no errors!", "Info");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Errors");
            }
        }
    }
}
