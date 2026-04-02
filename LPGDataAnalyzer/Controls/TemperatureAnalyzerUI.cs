using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Services;
using System.ComponentModel;

namespace LPGDataAnalyzer.Controls
{
    public partial class TemperatureAnalyzerUI : UserControl
    {
        // External data to analyze
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]

        private DataGridViewUC dataGridViewGasData;
        private DataGridViewUC dataGridViewRIDData;
        private DataGridViewUC dataGridViewReducerLag;
        private DataGridViewUC dataGridViewInjectionVsTemp;
        private DataGridViewUC dataGridViewSlowAndGetMinMax;
        private DataGridViewUC dataGridViewAverageTrimByTempGas;

        public TemperatureAnalyzerUI()
        {
            InitializeComponent();
        }

        private TabControl tabControl;

        private void InitializeComponent()
        {
            dataGridViewGasData = CreateGrid("GasData");
            dataGridViewRIDData = CreateGrid("RIDData");
            dataGridViewReducerLag = CreateGrid("ReducerLag");
            dataGridViewInjectionVsTemp = CreateGrid("InjectionVsTemp");
            dataGridViewSlowAndGetMinMax = CreateGrid("SlowMinMax");
            dataGridViewAverageTrimByTempGas = CreateGrid("AvgTrimGas");

            tabControl = new TabControl { Dock = DockStyle.Fill };

            // Tabs
            var tabGas = new TabPage("Gas Analysis");
            var tabReducer = new TabPage("Reducer Analysis");
            var tabInjection = new TabPage("Injection");
            var tabDiag = new TabPage("Diagnostics");

            tabControl.TabPages.AddRange(new[] { tabGas, tabReducer, tabInjection, tabDiag });

            // GAS TAB
            tabGas.Controls.Add(CreateVerticalSplit(
                Wrap("Gas Temperature Summary", dataGridViewGasData),
                Wrap("Average Trim by Gas Temperature", dataGridViewAverageTrimByTempGas)
            ));

            // REDUCER TAB
            tabReducer.Controls.Add(CreateVerticalSplit(
                Wrap("Reducer Temperature Summary", dataGridViewRIDData),
                Wrap("Reducer Thermal Lag Analysis", dataGridViewReducerLag)
            ));

            // INJECTION TAB
            tabInjection.Controls.Add(
                Wrap("Gas Temperature vs Injection Time", dataGridViewInjectionVsTemp)
            );

            // DIAGNOSTICS TAB
            tabDiag.Controls.Add(
                Wrap("Temperature Extremes by SLOW (Min/Max Analysis)", dataGridViewSlowAndGetMinMax)
            );
            Controls.Add(tabControl);
        }
        private DataGridViewUC CreateGrid(string name)
        {
            return new DataGridViewUC
            {
                Name = name,
                Dock = DockStyle.Fill,
                Tag = name // useful for logging/debugging
            };
        }
        private Control CreateVerticalSplit(Control top, Control bottom)
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal
            };

            split.Panel1.Controls.Add(top);
            split.Panel2.Controls.Add(bottom);

            // ✅ Make it equal AFTER layout is calculated
            split.Resize += (s, e) =>
            {
                split.SplitterDistance = (int)(split.Height * 0.65);
            };

            return split;
        }
        private void FormatGrid(DataGridView grid)
        {
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;

            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (col.Name.Contains("Average"))
                {
                    col.DefaultCellStyle.BackColor = Color.FromArgb(230, 240, 255);
                    col.DefaultCellStyle.ForeColor = Color.Black;
                }

                if (col.Name.Contains("Min") || col.Name.Contains("Max"))
                {
                    col.DefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
                    col.DefaultCellStyle.ForeColor = Color.Black;
                }

                if (col.Name.Contains("Count"))
                {
                    col.DefaultCellStyle.ForeColor = Color.DimGray;
                }

                if (col.Name.Contains("Temp"))
                {
                    col.DefaultCellStyle.ForeColor = Color.DarkOrange;
                }
            }
            grid.CellFormatting += (s, e) =>
            {
                var col = grid.Columns[e.ColumnIndex].Name;

                if (col.Contains("Trim") && e.Value is double val)
                {
                    if (val > 10)
                    {
                        e.CellStyle.BackColor = Color.LightCoral;    
                        e.CellStyle.ForeColor = Color.Black;
                    }
                    else if (val < -10)
                    {
                        e.CellStyle.BackColor = Color.LightBlue;     
                        e.CellStyle.ForeColor = Color.Black;
                    }
                    else
                    {
                        e.CellStyle.BackColor = Color.LightGreen;    
                        e.CellStyle.ForeColor = Color.Black;
                    }
                }
                if (e.Value != null && e.CellStyle.ForeColor == e.CellStyle.BackColor)
                {
                    e.CellStyle.BackColor = Color.Magenta; // obvious bug marker
                    e.CellStyle.ForeColor = Color.Black;
                }
            };
        }
        private Control Wrap(string title, Control grid)
        {
            return new GroupBox
            {
                Text = title,
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Controls = { grid }
            };
        }
        public void LoadData(DataItem[] data)
        {
            if (data == null || data.Length == 0) return;

            // Gas Temperature Analysis
            dataGridViewGasData.DataSource = TempeatureAnalyzer.GasTemperatureRanges(data);
            FormatGrid(dataGridViewGasData);
            // Reductor Temperature Analysis
            dataGridViewRIDData.DataSource = TempeatureAnalyzer.ReducerTemperatureRanges(data);
            FormatGrid(dataGridViewRIDData);
            dataGridViewReducerLag.DataSource = TempeatureAnalyzer.ReducerThermalLag(data);
            FormatGrid(dataGridViewReducerLag);

            dataGridViewInjectionVsTemp.DataSource = TempeatureAnalyzer.InjectionTimeByGasTemperature(data);
            FormatGrid(dataGridViewInjectionVsTemp);
            dataGridViewSlowAndGetMinMax.DataSource = TempeatureAnalyzer.TemperatureExtremesBySlowTrim(data);
            FormatGrid(dataGridViewSlowAndGetMinMax);
            dataGridViewAverageTrimByTempGas.DataSource = TempeatureAnalyzer.AverageTrimByGasTemperature(data);
            FormatGrid(dataGridViewAverageTrimByTempGas);
        }
    }
}