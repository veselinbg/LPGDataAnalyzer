using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Services;
using static LPGDataAnalyzer.Models.Settings;

namespace LPGDataAnalyzer.Controls
{
    public partial class ShowAllFileDataUI : UserControl
    {
        public ShowAllFileDataUI()
        {
            InitializeComponent();

            var directoryPath = "C:\\Users\\veselin.ivanov\\Documents\\MultipointInj\\Acquisition";
            LoadDirectoryAsync(directoryPath);
        }

        private FlowLayoutPanel flowPanel;

        private void InitializeComponent()
        {
            flowPanel = new FlowLayoutPanel();
            flowPanel.Dock = DockStyle.Fill;
            flowPanel.FlowDirection = FlowDirection.TopDown;
            flowPanel.WrapContents = false;
            flowPanel.AutoScroll = true;

            this.Controls.Add(flowPanel);
        }
        public async Task LoadDirectoryAsync(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException(directoryPath);

            flowPanel.Controls.Clear();

            var files = new DirectoryInfo(directoryPath)
                        .GetFiles("*.txt", SearchOption.AllDirectories)
                        .OrderByDescending(f => f.CreationTime)
                        .Select(f => f.FullName)
                        .ToArray();
            var p = new Parser();
            for (int i = 0; i < files.Length; i++)
            {
                
                p.Load(files[i]);

                var table1 = Analyzer.BuildTable(p.Data, x => x.BENZ_b1, x => x.Trim_b1, Aggregation.Median);
                var table2 = Analyzer.BuildTable(p.Data, x => x.BENZ_b2, x => x.Trim_b2, Aggregation.Median);
                var diff = Analyzer.Subtract(table1, table2);

                var panel = CreatePanelForFile(files[i], p.Data, table1, table2, diff);
                flowPanel.SuspendLayout();
                flowPanel.Controls.Add(panel);
                flowPanel.ResumeLayout();
            }
        }
        private Panel CreatePanelForFile(string filePath, DataItem[] data, double?[,] table1, double?[,] table2, double?[,] difftable)
        {
            var panel = new Panel
            {
                Height = 330,
                Width = 1900,
                Margin = new Padding(10, 5, 10, 5)
            };

            var layout = CreateTwoColumnGridLayout(filePath, data, table1, table2, difftable);
            panel.SuspendLayout();
            panel.Controls.Add(layout);
            panel.ResumeLayout();

            return panel;
        }
        private Control CreateTwoColumnGridLayout(
                        string filePath,
                        DataItem[] data,
                        double?[,] table1,
                        double?[,] table2, double?[,] difftable)
        {
            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 3;
            layout.RowCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // First grid (Bank 1)
            var grid1 = new ReadOnlyDataGridView();
            grid1.Dock = DockStyle.Fill;
            grid1.SetData(table1, data, $"{new FileInfo(filePath).Name} - Bank 1");
            DataGridViewColorization.HighlightDifferencesHeatmapWithValues(grid1.Grid);

            // Second grid (Bank 2)
            var grid2 = new ReadOnlyDataGridView();
            grid2.Dock = DockStyle.Fill;
            grid2.SetData(table2, data, $"{new FileInfo(filePath).Name} - Bank 2");
            DataGridViewColorization.HighlightDifferencesHeatmapWithValues(grid2.Grid);


            var grid3 = new ReadOnlyDataGridView();
            grid3.Dock = DockStyle.Fill;
            
            grid3.SetData(difftable, data, $"Diff = Bank 1 - Bank 2");
            DataGridViewColorization.HighlightDifferencesHeatmapWithValues(grid3.Grid);

            layout.Controls.Add(grid1, 0, 0);
            layout.Controls.Add(grid2, 1, 0);
            layout.Controls.Add(grid3, 2, 0);
            

            return layout;
        }
    }
}
