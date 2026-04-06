using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Services;
using System.ComponentModel;

namespace LPGDataAnalyzer.Controls
{
    public partial class TemperatureAnalyzerUI : UserControl
    {
        // External data to analyze
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]

        private ReadOnlyDataGridView dataGridViewGasData;
        private ReadOnlyDataGridView dataGridViewRIDData;
        private ReadOnlyDataGridView dataGridViewReducerLag;
        private ReadOnlyDataGridView dataGridViewInjectionVsTemp;
        private ReadOnlyDataGridView dataGridViewSlowAndGetMinMax;
        private ReadOnlyDataGridView dataGridViewAverageTrimByTempGas;

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

            tabControl.TabPages.Add(tabGas);
            tabControl.TabPages.Add(tabReducer);
            tabControl.TabPages.Add(tabInjection);
            tabControl.TabPages.Add(tabDiag);

            // GAS TAB
            tabGas.Controls.Add(CreateSplit(
                Wrap("Gas Temperature Summary", dataGridViewGasData),
                Wrap("Average Trim by Gas Temperature", dataGridViewAverageTrimByTempGas),
                Orientation.Vertical
            ));

            // REDUCER TAB
            tabReducer.Controls.Add(CreateSplit(
                Wrap("Reducer Temperature Summary", dataGridViewRIDData),
                Wrap("Reducer Thermal Lag Analysis", dataGridViewReducerLag),
                Orientation.Vertical
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
        private ReadOnlyDataGridView CreateGrid(string name)
        {
            return new ReadOnlyDataGridView
            {
                Title = name,
                Dock = DockStyle.Fill,
                
                Tag = name // useful for logging/debugging
            };
        }
        private Control CreateSplit(Control top, Control bottom, Orientation orientation = Orientation.Horizontal)
        {
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = orientation
            };

            split.Panel1.Controls.Add(top);
            split.Panel2.Controls.Add(bottom);

            // Set once when control is first shown
            split.HandleCreated += (s, e) =>
            {
                if (orientation == Orientation.Horizontal)
                    split.SplitterDistance = (int)(split.Height * 0.35);
                else
                    split.SplitterDistance = (int)(split.Width * 0.75);
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
            dataGridViewGasData.Grid.DataSource = TempeatureAnalyzer.GasTemperatureRanges(data);
            FormatGrid(dataGridViewGasData.Grid);
            // Reductor Temperature Analysis
            dataGridViewRIDData.Grid.DataSource = TempeatureAnalyzer.ReducerTemperatureRanges(data);
            FormatGrid(dataGridViewRIDData.Grid);
            dataGridViewReducerLag.Grid.DataSource = TempeatureAnalyzer.ReducerThermalLag(data);
            FormatGrid(dataGridViewReducerLag.Grid);

            dataGridViewInjectionVsTemp.Grid.DataSource = TempeatureAnalyzer.InjectionTimeByGasTemperature(data);
            FormatGrid(dataGridViewInjectionVsTemp.Grid);
            dataGridViewSlowAndGetMinMax.Grid.DataSource = TempeatureAnalyzer.TemperatureExtremesBySlowTrim(data);
            FormatGrid(dataGridViewSlowAndGetMinMax.Grid);
            dataGridViewAverageTrimByTempGas.Grid.DataSource = TempeatureAnalyzer.AverageTrimByGasTemperature(data);
            FormatGrid(dataGridViewAverageTrimByTempGas.Grid);
        }
    }
}