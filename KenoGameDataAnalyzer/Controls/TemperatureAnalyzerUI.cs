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
        private ReadOnlyDataGridView dataGridViewSlowTrimChanges;
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
            dataGridViewSlowTrimChanges = CreateGrid("SlowTrimChanges");

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
            tabDiag.Controls.Add(CreateSplit(
                Wrap("Temperature Extremes by SLOW (Min/Max Analysis)", dataGridViewSlowAndGetMinMax),
                Wrap("Slow Trim Changes", dataGridViewSlowTrimChanges),
                Orientation.Horizontal
            ));
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
            dataGridViewGasData.Grid.LoadData(TemperatureAnalyzer.GasTemperatureRanges(data));

            // Reductor Temperature Analysis
            dataGridViewRIDData.Grid.LoadData(TemperatureAnalyzer.ReducerTemperatureRanges(data));

            dataGridViewReducerLag.Grid.LoadData(TemperatureAnalyzer.ReducerThermalLag(data));

            dataGridViewInjectionVsTemp.Grid.LoadData(TemperatureAnalyzer.InjectionTimeByGasTemperature(data));

            dataGridViewSlowAndGetMinMax.Grid.LoadData(TemperatureAnalyzer.TemperatureExtremesBySlowTrim(data));

            dataGridViewAverageTrimByTempGas.Grid.LoadData(TemperatureAnalyzer.AverageTrimByGasTemperature(data));

            dataGridViewSlowTrimChanges.Grid.LoadData(TemperatureAnalyzer.SlowTrimChanges(data));
        }
    }
}