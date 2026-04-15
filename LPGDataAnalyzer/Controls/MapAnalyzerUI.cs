using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Services;

namespace LPGDataAnalyzer.Controls
{
    internal class MapAnalyzerUI : UserControl
    {
        private ReadOnlyDataGridView dataGridViewMapAnalysis;
        private ReadOnlyDataGridView dataGridViewBankToBank;
        private ReadOnlyDataGridView dataGridViewInjectionTimeAnalysis;
        private ReadOnlyDataGridView dataGridViewDeadTime;

        private TableLayoutPanel layout;

        public MapAnalyzerUI()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Initialize layout
            layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            // Initialize DataGridViews with meaningful titles
            dataGridViewMapAnalysis = CreateGrid("MAP vs RPM Analysis");
            dataGridViewBankToBank = CreateGrid("Bank-to-Bank Fuel Balance");
            dataGridViewInjectionTimeAnalysis = CreateGrid("Injection Time Analysis (LPG Base Map)");
            dataGridViewDeadTime = CreateGrid("Injector Dead Time Estimation");

            // Add to layout
            layout.Controls.Add(dataGridViewMapAnalysis, 0, 0);
            layout.Controls.Add(dataGridViewBankToBank, 1, 0);
            layout.Controls.Add(dataGridViewInjectionTimeAnalysis, 0, 1);
            layout.Controls.Add(dataGridViewDeadTime, 1, 1);

            // Add layout to control
            Controls.Add(layout);
        }

        private ReadOnlyDataGridView CreateGrid(string name)
        {
            return new ReadOnlyDataGridView
            {
                Title = name,
                Dock = DockStyle.Fill,
            };
        }

        public void LoadData(DataItem[] data)
        {
            if (data == null || data.Length == 0) return;

            var mapAnalysis = MapRpmAnalyzer.BuildTableByMap(data);
            dataGridViewMapAnalysis.Grid.LoadData(mapAnalysis);

            var bankToBank = MapRpmAnalyzer.BuildBankToBankfuelBalance(data);
            dataGridViewBankToBank.Grid.LoadData(bankToBank);

            var injectionAnalysis = MapRpmAnalyzer.BuildABankAwareLPGBaseMap(data);
            dataGridViewInjectionTimeAnalysis.Grid.LoadData(injectionAnalysis);

            var deadTime = MapRpmAnalyzer.BuildEnhancedBankMap(data);// LpgInjectorDeadTimeEstimation(data);//
            dataGridViewDeadTime.Grid.LoadData(deadTime);
        }
    }
}