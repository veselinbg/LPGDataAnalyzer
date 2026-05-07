namespace LPGDataAnalyzer.Controls
{
    partial class AnalysisUC
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            buttonShowSummary = new Button();
            buttonFieldsToShow = new Button();
            checkedListReductorTempGroup2 = new CheckedListBox();
            checkedListReductorTempGroup1 = new CheckedListBox();
            checkedListGasTemperatureb2 = new CheckedListBox();
            checkedListGasTemperatureb1 = new CheckedListBox();
            tableLayoutPanelAnalyses = new TableLayoutPanel();
            dataGridViewAnalyzeDataBank2t3 = new ReadOnlyDataGridView();
            dataGridViewAnalyzeDataBank1t3 = new ReadOnlyDataGridView();
            dataGridViewAnalyzeDataBank1t1 = new ReadOnlyDataGridView();
            dataGridViewAnalyzeDataBank1t2 = new ReadOnlyDataGridView();
            dataGridViewAnalyzeDataBank2t1 = new ReadOnlyDataGridView();
            dataGridViewAnalyzeDataBank2t2 = new ReadOnlyDataGridView();
            topPanel = new Panel();
            topLayout = new TableLayoutPanel();
            labelGasTemp1 = new Label();
            labelReductor1 = new Label();
            labelGasTemp2 = new Label();
            labelReductor2 = new Label();
            panelBank1 = new Panel();
            comboBoxFieldsToShowBank1 = new ComboBox();
            comboBoxAggregationBank1 = new ComboBox();
            panelBank2 = new Panel();
            comboBoxFieldsToShowBank2 = new ComboBox();
            comboBoxAggregationBank2 = new ComboBox();
            buttonTable = new TableLayoutPanel();
            labelBank1 = new Label();
            labelBank2 = new Label();
            labelAggregation = new Label();
            tableLayoutPanelAnalyses.SuspendLayout();
            topPanel.SuspendLayout();
            topLayout.SuspendLayout();
            panelBank1.SuspendLayout();
            panelBank2.SuspendLayout();
            buttonTable.SuspendLayout();
            SuspendLayout();
            // 
            // buttonShowSummary
            // 
            buttonShowSummary.Location = new Point(3, 48);
            buttonShowSummary.Name = "buttonShowSummary";
            buttonShowSummary.Size = new Size(134, 23);
            buttonShowSummary.TabIndex = 3;
            buttonShowSummary.Text = "Press, Map, Diff ";
            buttonShowSummary.Click += buttonShowSummary_Click;
            // 
            // buttonFieldsToShow
            // 
            buttonFieldsToShow.Location = new Point(3, 3);
            buttonFieldsToShow.Name = "buttonFieldsToShow";
            buttonFieldsToShow.Size = new Size(111, 23);
            buttonFieldsToShow.TabIndex = 0;
            buttonFieldsToShow.Text = "Fields To Show";
            buttonFieldsToShow.Click += ButtonFieldsToShow_Click;
            // 
            // checkedListReductorTempGroup2
            // 
            checkedListReductorTempGroup2.CheckOnClick = true;
            checkedListReductorTempGroup2.Dock = DockStyle.Fill;
            checkedListReductorTempGroup2.Location = new Point(903, 26);
            checkedListReductorTempGroup2.Name = "checkedListReductorTempGroup2";
            checkedListReductorTempGroup2.Size = new Size(204, 91);
            checkedListReductorTempGroup2.TabIndex = 8;
            checkedListReductorTempGroup2.ItemCheck += CheckedListBox_ItemCheck;
            // 
            // checkedListReductorTempGroup1
            // 
            checkedListReductorTempGroup1.CheckOnClick = true;
            checkedListReductorTempGroup1.Dock = DockStyle.Fill;
            checkedListReductorTempGroup1.Location = new Point(213, 26);
            checkedListReductorTempGroup1.Name = "checkedListReductorTempGroup1";
            checkedListReductorTempGroup1.Size = new Size(204, 91);
            checkedListReductorTempGroup1.TabIndex = 6;
            checkedListReductorTempGroup1.ItemCheck += CheckedListBox_ItemCheck;
            // 
            // checkedListGasTemperatureb2
            // 
            checkedListGasTemperatureb2.CheckOnClick = true;
            checkedListGasTemperatureb2.Dock = DockStyle.Fill;
            checkedListGasTemperatureb2.Location = new Point(693, 26);
            checkedListGasTemperatureb2.Name = "checkedListGasTemperatureb2";
            checkedListGasTemperatureb2.Size = new Size(204, 91);
            checkedListGasTemperatureb2.TabIndex = 7;
            checkedListGasTemperatureb2.ItemCheck += CheckedListBox_ItemCheck;
            // 
            // checkedListGasTemperatureb1
            // 
            checkedListGasTemperatureb1.CheckOnClick = true;
            checkedListGasTemperatureb1.Dock = DockStyle.Fill;
            checkedListGasTemperatureb1.Location = new Point(3, 26);
            checkedListGasTemperatureb1.Name = "checkedListGasTemperatureb1";
            checkedListGasTemperatureb1.Size = new Size(204, 91);
            checkedListGasTemperatureb1.TabIndex = 5;
            checkedListGasTemperatureb1.ItemCheck += CheckedListBox_ItemCheck;
            // 
            // tableLayoutPanelAnalyses
            // 
            tableLayoutPanelAnalyses.BackColor = SystemColors.Control;
            tableLayoutPanelAnalyses.ColumnCount = 3;
            tableLayoutPanelAnalyses.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.66337F));
            tableLayoutPanelAnalyses.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.66337F));
            tableLayoutPanelAnalyses.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32.6732674F));
            tableLayoutPanelAnalyses.Controls.Add(dataGridViewAnalyzeDataBank2t3, 2, 1);
            tableLayoutPanelAnalyses.Controls.Add(dataGridViewAnalyzeDataBank1t3, 2, 0);
            tableLayoutPanelAnalyses.Controls.Add(dataGridViewAnalyzeDataBank1t1, 0, 0);
            tableLayoutPanelAnalyses.Controls.Add(dataGridViewAnalyzeDataBank1t2, 1, 0);
            tableLayoutPanelAnalyses.Controls.Add(dataGridViewAnalyzeDataBank2t1, 0, 1);
            tableLayoutPanelAnalyses.Controls.Add(dataGridViewAnalyzeDataBank2t2, 1, 1);
            tableLayoutPanelAnalyses.Dock = DockStyle.Fill;
            tableLayoutPanelAnalyses.Location = new Point(0, 120);
            tableLayoutPanelAnalyses.Name = "tableLayoutPanelAnalyses";
            tableLayoutPanelAnalyses.RowCount = 2;
            tableLayoutPanelAnalyses.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanelAnalyses.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanelAnalyses.Size = new Size(1500, 880);
            tableLayoutPanelAnalyses.TabIndex = 0;
            // 
            // dataGridViewAnalyzeDataBank2t3
            // 
            dataGridViewAnalyzeDataBank2t3.AutoSize = true;
            dataGridViewAnalyzeDataBank2t3.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            dataGridViewAnalyzeDataBank2t3.Dock = DockStyle.Fill;
            dataGridViewAnalyzeDataBank2t3.EnableTitle = true;
            dataGridViewAnalyzeDataBank2t3.Location = new Point(1011, 443);
            dataGridViewAnalyzeDataBank2t3.Name = "dataGridViewAnalyzeDataBank2t3";
            dataGridViewAnalyzeDataBank2t3.Size = new Size(486, 434);
            dataGridViewAnalyzeDataBank2t3.TabIndex = 5;
            dataGridViewAnalyzeDataBank2t3.Title = "";
            // 
            // dataGridViewAnalyzeDataBank1t3
            // 
            dataGridViewAnalyzeDataBank1t3.AutoSize = true;
            dataGridViewAnalyzeDataBank1t3.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            dataGridViewAnalyzeDataBank1t3.Dock = DockStyle.Fill;
            dataGridViewAnalyzeDataBank1t3.EnableTitle = true;
            dataGridViewAnalyzeDataBank1t3.Location = new Point(1011, 3);
            dataGridViewAnalyzeDataBank1t3.Name = "dataGridViewAnalyzeDataBank1t3";
            dataGridViewAnalyzeDataBank1t3.Size = new Size(486, 434);
            dataGridViewAnalyzeDataBank1t3.TabIndex = 4;
            dataGridViewAnalyzeDataBank1t3.Title = "";
            // 
            // dataGridViewAnalyzeDataBank1t1
            // 
            dataGridViewAnalyzeDataBank1t1.AutoSize = true;
            dataGridViewAnalyzeDataBank1t1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            dataGridViewAnalyzeDataBank1t1.Dock = DockStyle.Fill;
            dataGridViewAnalyzeDataBank1t1.EnableTitle = true;
            dataGridViewAnalyzeDataBank1t1.Location = new Point(3, 3);
            dataGridViewAnalyzeDataBank1t1.Name = "dataGridViewAnalyzeDataBank1t1";
            dataGridViewAnalyzeDataBank1t1.Size = new Size(498, 434);
            dataGridViewAnalyzeDataBank1t1.TabIndex = 0;
            dataGridViewAnalyzeDataBank1t1.Title = "";
            // 
            // dataGridViewAnalyzeDataBank1t2
            // 
            dataGridViewAnalyzeDataBank1t2.AutoSize = true;
            dataGridViewAnalyzeDataBank1t2.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            dataGridViewAnalyzeDataBank1t2.Dock = DockStyle.Fill;
            dataGridViewAnalyzeDataBank1t2.EnableTitle = true;
            dataGridViewAnalyzeDataBank1t2.Location = new Point(507, 3);
            dataGridViewAnalyzeDataBank1t2.Name = "dataGridViewAnalyzeDataBank1t2";
            dataGridViewAnalyzeDataBank1t2.Size = new Size(498, 434);
            dataGridViewAnalyzeDataBank1t2.TabIndex = 1;
            dataGridViewAnalyzeDataBank1t2.Title = "";
            // 
            // dataGridViewAnalyzeDataBank2t1
            // 
            dataGridViewAnalyzeDataBank2t1.AutoSize = true;
            dataGridViewAnalyzeDataBank2t1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            dataGridViewAnalyzeDataBank2t1.Dock = DockStyle.Fill;
            dataGridViewAnalyzeDataBank2t1.EnableTitle = true;
            dataGridViewAnalyzeDataBank2t1.Location = new Point(3, 443);
            dataGridViewAnalyzeDataBank2t1.Name = "dataGridViewAnalyzeDataBank2t1";
            dataGridViewAnalyzeDataBank2t1.Size = new Size(498, 434);
            dataGridViewAnalyzeDataBank2t1.TabIndex = 2;
            dataGridViewAnalyzeDataBank2t1.Title = "";
            // 
            // dataGridViewAnalyzeDataBank2t2
            // 
            dataGridViewAnalyzeDataBank2t2.AutoSize = true;
            dataGridViewAnalyzeDataBank2t2.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            dataGridViewAnalyzeDataBank2t2.Dock = DockStyle.Fill;
            dataGridViewAnalyzeDataBank2t2.EnableTitle = true;
            dataGridViewAnalyzeDataBank2t2.Location = new Point(507, 443);
            dataGridViewAnalyzeDataBank2t2.Name = "dataGridViewAnalyzeDataBank2t2";
            dataGridViewAnalyzeDataBank2t2.Size = new Size(498, 434);
            dataGridViewAnalyzeDataBank2t2.TabIndex = 3;
            dataGridViewAnalyzeDataBank2t2.Title = "";
            // 
            // topPanel
            // 
            topPanel.Controls.Add(topLayout);
            topPanel.Dock = DockStyle.Top;
            topPanel.Location = new Point(0, 0);
            topPanel.Name = "topPanel";
            topPanel.Size = new Size(1500, 120);
            topPanel.TabIndex = 1;
            // 
            // topLayout
            // 
            topLayout.ColumnCount = 7;
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F));
            topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F));
            topLayout.Controls.Add(labelGasTemp1, 0, 0);
            topLayout.Controls.Add(labelReductor1, 1, 0);
            topLayout.Controls.Add(labelGasTemp2, 3, 0);
            topLayout.Controls.Add(labelReductor2, 4, 0);
            topLayout.Controls.Add(checkedListGasTemperatureb1, 0, 1);
            topLayout.Controls.Add(checkedListReductorTempGroup1, 1, 1);
            topLayout.Controls.Add(panelBank1, 2, 1);
            topLayout.Controls.Add(checkedListGasTemperatureb2, 3, 1);
            topLayout.Controls.Add(checkedListReductorTempGroup2, 4, 1);
            topLayout.Controls.Add(panelBank2, 5, 1);
            topLayout.Controls.Add(buttonTable, 6, 1);
            topLayout.Controls.Add(labelBank1, 2, 0);
            topLayout.Controls.Add(labelBank2, 5, 0);
            topLayout.Dock = DockStyle.Fill;
            topLayout.Location = new Point(0, 0);
            topLayout.Name = "topLayout";
            topLayout.RowCount = 2;
            topLayout.RowStyles.Add(new RowStyle());
            topLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            topLayout.Size = new Size(1500, 120);
            topLayout.TabIndex = 0;
            // 
            // labelGasTemp1
            // 
            labelGasTemp1.Location = new Point(3, 0);
            labelGasTemp1.Name = "labelGasTemp1";
            labelGasTemp1.Size = new Size(100, 23);
            labelGasTemp1.TabIndex = 0;
            // 
            // labelReductor1
            // 
            labelReductor1.Location = new Point(213, 0);
            labelReductor1.Name = "labelReductor1";
            labelReductor1.Size = new Size(100, 23);
            labelReductor1.TabIndex = 1;
            // 
            // labelGasTemp2
            // 
            labelGasTemp2.Location = new Point(693, 0);
            labelGasTemp2.Name = "labelGasTemp2";
            labelGasTemp2.Size = new Size(100, 23);
            labelGasTemp2.TabIndex = 2;
            // 
            // labelReductor2
            // 
            labelReductor2.Location = new Point(903, 0);
            labelReductor2.Name = "labelReductor2";
            labelReductor2.Size = new Size(100, 23);
            labelReductor2.TabIndex = 3;
            // 
            // panelBank1
            // 
            panelBank1.Controls.Add(comboBoxFieldsToShowBank1);
            panelBank1.Controls.Add(comboBoxAggregationBank1);
            panelBank1.Location = new Point(423, 26);
            panelBank1.Name = "panelBank1";
            panelBank1.Size = new Size(200, 91);
            panelBank1.TabIndex = 7;
            // 
            // comboBoxFieldsToShowBank1
            // 
            comboBoxFieldsToShowBank1.Dock = DockStyle.Top;
            comboBoxFieldsToShowBank1.Location = new Point(0, 23);
            comboBoxFieldsToShowBank1.Name = "comboBoxFieldsToShowBank1";
            comboBoxFieldsToShowBank1.Size = new Size(200, 23);
            comboBoxFieldsToShowBank1.TabIndex = 0;
            // 
            // comboBoxAggregationBank1
            // 
            comboBoxAggregationBank1.Dock = DockStyle.Top;
            comboBoxAggregationBank1.Location = new Point(0, 0);
            comboBoxAggregationBank1.Name = "comboBoxAggregationBank1";
            comboBoxAggregationBank1.Size = new Size(200, 23);
            comboBoxAggregationBank1.TabIndex = 1;
            // 
            // panelBank2
            // 
            panelBank2.Controls.Add(comboBoxFieldsToShowBank2);
            panelBank2.Controls.Add(comboBoxAggregationBank2);
            panelBank2.Location = new Point(1113, 26);
            panelBank2.Name = "panelBank2";
            panelBank2.Size = new Size(200, 91);
            panelBank2.TabIndex = 9;
            // 
            // comboBoxFieldsToShowBank2
            // 
            comboBoxFieldsToShowBank2.Dock = DockStyle.Top;
            comboBoxFieldsToShowBank2.Location = new Point(0, 23);
            comboBoxFieldsToShowBank2.Name = "comboBoxFieldsToShowBank2";
            comboBoxFieldsToShowBank2.Size = new Size(200, 23);
            comboBoxFieldsToShowBank2.TabIndex = 0;
            // 
            // comboBoxAggregationBank2
            // 
            comboBoxAggregationBank2.Dock = DockStyle.Top;
            comboBoxAggregationBank2.Location = new Point(0, 0);
            comboBoxAggregationBank2.Name = "comboBoxAggregationBank2";
            comboBoxAggregationBank2.Size = new Size(200, 23);
            comboBoxAggregationBank2.TabIndex = 1;
            // 
            // buttonTable
            // 
            buttonTable.ColumnCount = 1;
            buttonTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            buttonTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            buttonTable.Controls.Add(buttonFieldsToShow, 0, 0);
            buttonTable.Controls.Add(buttonShowSummary, 0, 1);
            buttonTable.Dock = DockStyle.Fill;
            buttonTable.Location = new Point(1323, 26);
            buttonTable.Name = "buttonTable";
            buttonTable.RowCount = 2;
            buttonTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            buttonTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            buttonTable.Size = new Size(174, 91);
            buttonTable.TabIndex = 10;
            // 
            // labelBank1
            // 
            labelBank1.Location = new Point(423, 0);
            labelBank1.Name = "labelBank1";
            labelBank1.Size = new Size(100, 23);
            labelBank1.TabIndex = 11;
            // 
            // labelBank2
            // 
            labelBank2.Location = new Point(1113, 0);
            labelBank2.Name = "labelBank2";
            labelBank2.Size = new Size(100, 23);
            labelBank2.TabIndex = 12;
            // 
            // labelAggregation
            // 
            labelAggregation.Location = new Point(963, 0);
            labelAggregation.Name = "labelAggregation";
            labelAggregation.Size = new Size(100, 23);
            labelAggregation.TabIndex = 4;
            // 
            // AnalysisUC
            // 
            Controls.Add(tableLayoutPanelAnalyses);
            Controls.Add(topPanel);
            Name = "AnalysisUC";
            Size = new Size(1500, 1000);
            tableLayoutPanelAnalyses.ResumeLayout(false);
            tableLayoutPanelAnalyses.PerformLayout();
            topPanel.ResumeLayout(false);
            topLayout.ResumeLayout(false);
            panelBank1.ResumeLayout(false);
            panelBank2.ResumeLayout(false);
            buttonTable.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private Button buttonShowSummary;
        private Button buttonFieldsToShow;
        private CheckedListBox checkedListReductorTempGroup2;
        private CheckedListBox checkedListReductorTempGroup1;
        private CheckedListBox checkedListGasTemperatureb2;
        private CheckedListBox checkedListGasTemperatureb1;
        private TableLayoutPanel tableLayoutPanelAnalyses;
        private ReadOnlyDataGridView dataGridViewAnalyzeDataBank1t1;
        private ReadOnlyDataGridView dataGridViewAnalyzeDataBank1t2;
        private ReadOnlyDataGridView dataGridViewAnalyzeDataBank2t1;
        private ReadOnlyDataGridView dataGridViewAnalyzeDataBank2t2;
        private Panel topPanel;
        private TableLayoutPanel topLayout;
        private TableLayoutPanel buttonTable;
        private Label labelBank1;
        private Label labelBank2;
        private Label labelGasTemp1;
        private Label labelGasTemp2;
        private Label labelReductor1;
        private Label labelReductor2;
        private Label labelAggregation;
        private Panel panelBank1;
        private Panel panelBank2;
        private ComboBox comboBoxFieldsToShowBank1;
        private ComboBox comboBoxFieldsToShowBank2;
        private ComboBox comboBoxAggregationBank1;
        private ComboBox comboBoxAggregationBank2;
        private ReadOnlyDataGridView dataGridViewAnalyzeDataBank2t3;
        private ReadOnlyDataGridView dataGridViewAnalyzeDataBank1t3;
    }
}
