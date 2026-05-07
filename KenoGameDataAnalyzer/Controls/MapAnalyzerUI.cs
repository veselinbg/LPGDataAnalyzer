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
        private ReadOnlyDataGridView dataGridViewEnhancedBank;

        private TableLayoutPanel layout;

        public MapAnalyzerUI()
        {
            InitializeComponent();
        }
        private void InitializeComponent()
        {
            // MAIN layout (explicit structure)
            layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };

            // Define structure properly
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // IMPORTANT

            // LEFT layout (3 rows)
            var leftLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };

            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));

            // RIGHT layout (2 rows)
            var rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };

            rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            // Grids
            dataGridViewMapAnalysis = CreateGrid("MAP vs RPM Analysis");
            dataGridViewBankToBank = CreateGrid("Bank-to-Bank Fuel Balance");
            dataGridViewInjectionTimeAnalysis = CreateGrid("Injection Time Analysis (LPG Base Map)");
            dataGridViewDeadTime = CreateGrid("Injector Dead Time Estimation");
            dataGridViewEnhancedBank = CreateGrid("Enhanced Bank Map");

            // LEFT side
            leftLayout.Controls.Add(dataGridViewMapAnalysis, 0, 0);
            leftLayout.Controls.Add(dataGridViewBankToBank, 0, 1);
            leftLayout.Controls.Add(dataGridViewInjectionTimeAnalysis, 0, 2);

            // RIGHT side
            rightLayout.Controls.Add(dataGridViewDeadTime, 0, 0);
            rightLayout.Controls.Add(dataGridViewEnhancedBank, 0, 1);

            // THIS is the key part you emphasized:
            layout.Controls.Add(leftLayout, 0, 0);
            layout.Controls.Add(rightLayout, 1, 0);

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

            // RIGHT SIDE

            var deadTime = MapRpmAnalyzer.LpgInjectorDeadTimeEstimation(data);
            dataGridViewDeadTime.Grid.LoadData(deadTime);

            var enhancedBank = MapRpmAnalyzer.BuildGrid(data);
            dataGridViewEnhancedBank.Grid.LoadData(enhancedBank);
        }
    }
}