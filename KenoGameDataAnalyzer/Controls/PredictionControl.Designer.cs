namespace LPGDataAnalyzer.Controls
{
    partial class PredictionControl
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

        private void InitializeComponent()
        {
            checkBoxSaveSnapshot = new CheckBox();
            panelHistory = new Panel();
            historyControl1 = new HistoryControl();
            textBoxMinCount = new TextBox();
            checkBoxRound = new CheckBox();
            checkBoxOnlyChanges = new CheckBox();
            checkboxInterpolation = new CheckBox();
            checkboxEnableSmooth = new CheckBox();
            textBoxLastPredictedFuelTable = new TextBox();
            textBoxParsedData = new TextBox();
            buttonValidate = new Button();
            buttonContinue = new Button();
            buttonParceSelectedPhoto = new Button();
            checkBoxShowOnlyMiplayerChange = new CheckBox();
            textBoxMinValueOfChange = new TextBox();
            checkBoxUseHistory = new CheckBox();
            textBoxRefPress = new TextBox();
            tableLayoutPanelMain = new TableLayoutPanel();
            tableLayoutPanelRight = new TableLayoutPanel();
            tableLayoutPanelManagement = new TableLayoutPanel();
            panel1 = new Panel();
            panel3 = new Panel();
            checkBoxShowOnlyCount = new CheckBox();
            checkBoxAllwaysApplyNegativeTrim = new CheckBox();
            labelMaxBenzDiff = new Label();
            textBoxMaxBenzDiff = new TextBox();
            labelMinCount = new Label();
            labelPress = new Label();
            labelValueOfChange = new Label();
            panel4 = new Panel();
            buttonExport = new Button();
            tableLayoutPanelTopLine = new TableLayoutPanel();
            buttonConvert = new Button();
            textBoxImagePath = new TextBox();
            tableLayoutPanelLeft = new TableLayoutPanel();
            dataGridViewPrediction = new ReadOnlyDataGridView();
            dataGridViewOrig = new ReadOnlyDataGridView();
            panelLegend = new Panel();
            DataGridViewInvalidData = new DataGridView();
            panelHistory.SuspendLayout();
            tableLayoutPanelMain.SuspendLayout();
            tableLayoutPanelRight.SuspendLayout();
            tableLayoutPanelManagement.SuspendLayout();
            panel1.SuspendLayout();
            panel3.SuspendLayout();
            panel4.SuspendLayout();
            tableLayoutPanelTopLine.SuspendLayout();
            tableLayoutPanelLeft.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)DataGridViewInvalidData).BeginInit();
            SuspendLayout();
            // 
            // checkBoxSaveSnapshot
            // 
            checkBoxSaveSnapshot.AutoSize = true;
            checkBoxSaveSnapshot.Location = new Point(7, 150);
            checkBoxSaveSnapshot.Name = "checkBoxSaveSnapshot";
            checkBoxSaveSnapshot.Size = new Size(102, 19);
            checkBoxSaveSnapshot.TabIndex = 0;
            checkBoxSaveSnapshot.Text = "Save Snapshot";
            // 
            // panelHistory
            // 
            panelHistory.Controls.Add(historyControl1);
            panelHistory.Dock = DockStyle.Fill;
            panelHistory.Location = new Point(3, 399);
            panelHistory.Name = "panelHistory";
            panelHistory.Size = new Size(836, 355);
            panelHistory.TabIndex = 1;
            // 
            // historyControl1
            // 
            historyControl1.Dock = DockStyle.Fill;
            historyControl1.Location = new Point(0, 0);
            historyControl1.Name = "historyControl1";
            historyControl1.Size = new Size(836, 355);
            historyControl1.TabIndex = 1;
            // 
            // textBoxMinCount
            // 
            textBoxMinCount.Location = new Point(364, 98);
            textBoxMinCount.Name = "textBoxMinCount";
            textBoxMinCount.RightToLeft = RightToLeft.Yes;
            textBoxMinCount.Size = new Size(45, 23);
            textBoxMinCount.TabIndex = 2;
            textBoxMinCount.Text = "0";
            // 
            // checkBoxRound
            // 
            checkBoxRound.AutoSize = true;
            checkBoxRound.Checked = true;
            checkBoxRound.CheckState = CheckState.Checked;
            checkBoxRound.Location = new Point(7, 73);
            checkBoxRound.Name = "checkBoxRound";
            checkBoxRound.Size = new Size(61, 19);
            checkBoxRound.TabIndex = 4;
            checkBoxRound.Text = "Round";
            // 
            // checkBoxOnlyChanges
            // 
            checkBoxOnlyChanges.AutoSize = true;
            checkBoxOnlyChanges.Location = new Point(7, 23);
            checkBoxOnlyChanges.Name = "checkBoxOnlyChanges";
            checkBoxOnlyChanges.Size = new Size(98, 19);
            checkBoxOnlyChanges.TabIndex = 5;
            checkBoxOnlyChanges.Text = "Only changes";
            // 
            // checkboxInterpolation
            // 
            checkboxInterpolation.AutoSize = true;
            checkboxInterpolation.Location = new Point(77, 6);
            checkboxInterpolation.Name = "checkboxInterpolation";
            checkboxInterpolation.Size = new Size(94, 19);
            checkboxInterpolation.TabIndex = 6;
            checkboxInterpolation.Text = "Interpolation";
            // 
            // checkboxEnableSmooth
            // 
            checkboxEnableSmooth.AutoSize = true;
            checkboxEnableSmooth.Location = new Point(3, 6);
            checkboxEnableSmooth.Name = "checkboxEnableSmooth";
            checkboxEnableSmooth.Size = new Size(68, 19);
            checkboxEnableSmooth.TabIndex = 7;
            checkboxEnableSmooth.Text = "Smooth";
            // 
            // textBoxLastPredictedFuelTable
            // 
            textBoxLastPredictedFuelTable.Dock = DockStyle.Fill;
            textBoxLastPredictedFuelTable.Location = new Point(421, 3);
            textBoxLastPredictedFuelTable.Multiline = true;
            textBoxLastPredictedFuelTable.Name = "textBoxLastPredictedFuelTable";
            textBoxLastPredictedFuelTable.Size = new Size(412, 171);
            textBoxLastPredictedFuelTable.TabIndex = 8;
            // 
            // textBoxParsedData
            // 
            textBoxParsedData.Dock = DockStyle.Fill;
            textBoxParsedData.Location = new Point(0, 0);
            textBoxParsedData.Multiline = true;
            textBoxParsedData.Name = "textBoxParsedData";
            textBoxParsedData.Size = new Size(412, 171);
            textBoxParsedData.TabIndex = 11;
            // 
            // buttonValidate
            // 
            buttonValidate.Location = new Point(253, 3);
            buttonValidate.Name = "buttonValidate";
            buttonValidate.Size = new Size(75, 23);
            buttonValidate.TabIndex = 12;
            buttonValidate.Text = "Validate";
            buttonValidate.Click += ButtonValidate_Click;
            // 
            // buttonContinue
            // 
            buttonContinue.Location = new Point(334, 3);
            buttonContinue.Name = "buttonContinue";
            buttonContinue.Size = new Size(75, 23);
            buttonContinue.TabIndex = 13;
            buttonContinue.Text = "Predict";
            buttonContinue.Click += ButtonPredict_Click;
            // 
            // buttonParceSelectedPhoto
            // 
            buttonParceSelectedPhoto.Location = new Point(671, 3);
            buttonParceSelectedPhoto.Name = "buttonParceSelectedPhoto";
            buttonParceSelectedPhoto.Size = new Size(77, 23);
            buttonParceSelectedPhoto.TabIndex = 14;
            buttonParceSelectedPhoto.Text = "Parse";
            buttonParceSelectedPhoto.Click += ButtonParceSelectedImage_Click;
            // 
            // checkBoxShowOnlyMiplayerChange
            // 
            checkBoxShowOnlyMiplayerChange.AutoSize = true;
            checkBoxShowOnlyMiplayerChange.Location = new Point(7, 48);
            checkBoxShowOnlyMiplayerChange.Name = "checkBoxShowOnlyMiplayerChange";
            checkBoxShowOnlyMiplayerChange.Size = new Size(196, 19);
            checkBoxShowOnlyMiplayerChange.TabIndex = 19;
            checkBoxShowOnlyMiplayerChange.Text = "Show Only Multy Player Change";
            checkBoxShowOnlyMiplayerChange.UseVisualStyleBackColor = true;
            // 
            // textBoxMinValueOfChange
            // 
            textBoxMinValueOfChange.Location = new Point(364, 44);
            textBoxMinValueOfChange.Name = "textBoxMinValueOfChange";
            textBoxMinValueOfChange.Size = new Size(45, 23);
            textBoxMinValueOfChange.TabIndex = 20;
            textBoxMinValueOfChange.Text = "0";
            textBoxMinValueOfChange.TextAlign = HorizontalAlignment.Right;
            // 
            // checkBoxUseHistory
            // 
            checkBoxUseHistory.AutoSize = true;
            checkBoxUseHistory.Location = new Point(3, 150);
            checkBoxUseHistory.Name = "checkBoxUseHistory";
            checkBoxUseHistory.Size = new Size(86, 19);
            checkBoxUseHistory.TabIndex = 21;
            checkBoxUseHistory.Text = "Use History";
            checkBoxUseHistory.UseVisualStyleBackColor = true;
            // 
            // textBoxRefPress
            // 
            textBoxRefPress.Location = new Point(364, 69);
            textBoxRefPress.Name = "textBoxRefPress";
            textBoxRefPress.RightToLeft = RightToLeft.Yes;
            textBoxRefPress.Size = new Size(45, 23);
            textBoxRefPress.TabIndex = 22;
            textBoxRefPress.Text = "1.49";
            // 
            // tableLayoutPanelMain
            // 
            tableLayoutPanelMain.ColumnCount = 2;
            tableLayoutPanelMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            tableLayoutPanelMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tableLayoutPanelMain.Controls.Add(tableLayoutPanelRight, 1, 0);
            tableLayoutPanelMain.Controls.Add(tableLayoutPanelLeft, 0, 0);
            tableLayoutPanelMain.Dock = DockStyle.Fill;
            tableLayoutPanelMain.Location = new Point(0, 0);
            tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            tableLayoutPanelMain.RowCount = 1;
            tableLayoutPanelMain.RowStyles.Add(new RowStyle());
            tableLayoutPanelMain.Size = new Size(1413, 763);
            tableLayoutPanelMain.TabIndex = 24;
            // 
            // tableLayoutPanelRight
            // 
            tableLayoutPanelRight.ColumnCount = 1;
            tableLayoutPanelRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelRight.Controls.Add(panelHistory, 0, 2);
            tableLayoutPanelRight.Controls.Add(tableLayoutPanelManagement, 0, 1);
            tableLayoutPanelRight.Controls.Add(tableLayoutPanelTopLine, 0, 0);
            tableLayoutPanelRight.Dock = DockStyle.Fill;
            tableLayoutPanelRight.ForeColor = SystemColors.ActiveCaptionText;
            tableLayoutPanelRight.Location = new Point(568, 3);
            tableLayoutPanelRight.Name = "tableLayoutPanelRight";
            tableLayoutPanelRight.RowCount = 3;
            tableLayoutPanelRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            tableLayoutPanelRight.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanelRight.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanelRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanelRight.Size = new Size(842, 757);
            tableLayoutPanelRight.TabIndex = 25;
            // 
            // tableLayoutPanelManagement
            // 
            tableLayoutPanelManagement.ColumnCount = 2;
            tableLayoutPanelManagement.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanelManagement.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanelManagement.Controls.Add(panel1, 0, 0);
            tableLayoutPanelManagement.Controls.Add(textBoxLastPredictedFuelTable, 1, 0);
            tableLayoutPanelManagement.Controls.Add(panel3, 0, 1);
            tableLayoutPanelManagement.Controls.Add(panel4, 1, 1);
            tableLayoutPanelManagement.Dock = DockStyle.Fill;
            tableLayoutPanelManagement.Location = new Point(3, 38);
            tableLayoutPanelManagement.Name = "tableLayoutPanelManagement";
            tableLayoutPanelManagement.RowCount = 2;
            tableLayoutPanelManagement.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanelManagement.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanelManagement.Size = new Size(836, 355);
            tableLayoutPanelManagement.TabIndex = 11;
            // 
            // panel1
            // 
            panel1.Controls.Add(textBoxParsedData);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(3, 3);
            panel1.Name = "panel1";
            panel1.Size = new Size(412, 171);
            panel1.TabIndex = 0;
            // 
            // panel3
            // 
            panel3.Controls.Add(checkBoxShowOnlyCount);
            panel3.Controls.Add(checkBoxAllwaysApplyNegativeTrim);
            panel3.Controls.Add(labelMaxBenzDiff);
            panel3.Controls.Add(textBoxMaxBenzDiff);
            panel3.Controls.Add(labelMinCount);
            panel3.Controls.Add(checkBoxOnlyChanges);
            panel3.Controls.Add(labelPress);
            panel3.Controls.Add(labelValueOfChange);
            panel3.Controls.Add(checkBoxSaveSnapshot);
            panel3.Controls.Add(buttonContinue);
            panel3.Controls.Add(buttonValidate);
            panel3.Controls.Add(checkBoxShowOnlyMiplayerChange);
            panel3.Controls.Add(textBoxMinCount);
            panel3.Controls.Add(textBoxRefPress);
            panel3.Controls.Add(checkBoxRound);
            panel3.Controls.Add(textBoxMinValueOfChange);
            panel3.Dock = DockStyle.Fill;
            panel3.Location = new Point(3, 180);
            panel3.Name = "panel3";
            panel3.Size = new Size(412, 172);
            panel3.TabIndex = 9;
            // 
            // checkBoxShowOnlyCount
            // 
            checkBoxShowOnlyCount.AutoSize = true;
            checkBoxShowOnlyCount.Location = new Point(4, 121);
            checkBoxShowOnlyCount.Name = "checkBoxShowOnlyCount";
            checkBoxShowOnlyCount.Size = new Size(119, 19);
            checkBoxShowOnlyCount.TabIndex = 29;
            checkBoxShowOnlyCount.Text = "Show Only Count";
            checkBoxShowOnlyCount.UseVisualStyleBackColor = true;
            // 
            // checkBoxAllwaysApplyNegativeTrim
            // 
            checkBoxAllwaysApplyNegativeTrim.AutoSize = true;
            checkBoxAllwaysApplyNegativeTrim.Location = new Point(6, 97);
            checkBoxAllwaysApplyNegativeTrim.Name = "checkBoxAllwaysApplyNegativeTrim";
            checkBoxAllwaysApplyNegativeTrim.Size = new Size(176, 19);
            checkBoxAllwaysApplyNegativeTrim.TabIndex = 28;
            checkBoxAllwaysApplyNegativeTrim.Text = "Allways Apply Negative Trim";
            checkBoxAllwaysApplyNegativeTrim.UseVisualStyleBackColor = true;
            // 
            // labelMaxBenzDiff
            // 
            labelMaxBenzDiff.AutoSize = true;
            labelMaxBenzDiff.Location = new Point(255, 141);
            labelMaxBenzDiff.Name = "labelMaxBenzDiff";
            labelMaxBenzDiff.Size = new Size(106, 15);
            labelMaxBenzDiff.TabIndex = 27;
            labelMaxBenzDiff.Text = "Max Benz Diff in %";
            // 
            // textBoxMaxBenzDiff
            // 
            textBoxMaxBenzDiff.Location = new Point(365, 136);
            textBoxMaxBenzDiff.Name = "textBoxMaxBenzDiff";
            textBoxMaxBenzDiff.RightToLeft = RightToLeft.Yes;
            textBoxMaxBenzDiff.Size = new Size(44, 23);
            textBoxMaxBenzDiff.TabIndex = 26;
            textBoxMaxBenzDiff.Text = "100";
            // 
            // labelMinCount
            // 
            labelMinCount.AutoSize = true;
            labelMinCount.Location = new Point(253, 106);
            labelMinCount.Name = "labelMinCount";
            labelMinCount.Size = new Size(105, 15);
            labelMinCount.TabIndex = 25;
            labelMinCount.Text = "Min Count for Cell";
            // 
            // labelPress
            // 
            labelPress.AutoSize = true;
            labelPress.Location = new Point(273, 72);
            labelPress.Name = "labelPress";
            labelPress.Size = new Size(89, 15);
            labelPress.TabIndex = 24;
            labelPress.Text = "Reference Press";
            // 
            // labelValueOfChange
            // 
            labelValueOfChange.AutoSize = true;
            labelValueOfChange.Location = new Point(233, 52);
            labelValueOfChange.Name = "labelValueOfChange";
            labelValueOfChange.Size = new Size(125, 15);
            labelValueOfChange.TabIndex = 23;
            labelValueOfChange.Text = "Min Valid % of change";
            // 
            // panel4
            // 
            panel4.Controls.Add(buttonExport);
            panel4.Controls.Add(checkboxEnableSmooth);
            panel4.Controls.Add(checkboxInterpolation);
            panel4.Controls.Add(checkBoxUseHistory);
            panel4.Dock = DockStyle.Fill;
            panel4.Location = new Point(421, 180);
            panel4.Name = "panel4";
            panel4.Size = new Size(412, 172);
            panel4.TabIndex = 10;
            // 
            // buttonExport
            // 
            buttonExport.Location = new Point(3, 32);
            buttonExport.Name = "buttonExport";
            buttonExport.Size = new Size(152, 23);
            buttonExport.TabIndex = 22;
            buttonExport.Text = "Export Selected Cells";
            buttonExport.UseVisualStyleBackColor = true;
            buttonExport.Click += buttonExport_Click;
            // 
            // tableLayoutPanelTopLine
            // 
            tableLayoutPanelTopLine.ColumnCount = 3;
            tableLayoutPanelTopLine.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80F));
            tableLayoutPanelTopLine.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F));
            tableLayoutPanelTopLine.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F));
            tableLayoutPanelTopLine.Controls.Add(buttonConvert, 2, 0);
            tableLayoutPanelTopLine.Controls.Add(buttonParceSelectedPhoto, 1, 0);
            tableLayoutPanelTopLine.Controls.Add(textBoxImagePath, 0, 0);
            tableLayoutPanelTopLine.Dock = DockStyle.Fill;
            tableLayoutPanelTopLine.Location = new Point(3, 3);
            tableLayoutPanelTopLine.Name = "tableLayoutPanelTopLine";
            tableLayoutPanelTopLine.RowCount = 1;
            tableLayoutPanelTopLine.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelTopLine.Size = new Size(836, 29);
            tableLayoutPanelTopLine.TabIndex = 12;
            // 
            // buttonConvert
            // 
            buttonConvert.Location = new Point(754, 3);
            buttonConvert.Name = "buttonConvert";
            buttonConvert.Size = new Size(75, 23);
            buttonConvert.TabIndex = 22;
            buttonConvert.Text = "Convert";
            buttonConvert.UseVisualStyleBackColor = true;
            buttonConvert.Click += buttonConvert_Click;
            // 
            // textBoxImagePath
            // 
            textBoxImagePath.Dock = DockStyle.Fill;
            textBoxImagePath.Location = new Point(3, 3);
            textBoxImagePath.Name = "textBoxImagePath";
            textBoxImagePath.Size = new Size(662, 23);
            textBoxImagePath.TabIndex = 15;
            // 
            // tableLayoutPanelLeft
            // 
            tableLayoutPanelLeft.ColumnCount = 1;
            tableLayoutPanelLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelLeft.Controls.Add(dataGridViewPrediction, 0, 2);
            tableLayoutPanelLeft.Controls.Add(dataGridViewOrig, 0, 0);
            tableLayoutPanelLeft.Controls.Add(panelLegend, 0, 1);
            tableLayoutPanelLeft.Controls.Add(DataGridViewInvalidData, 0, 3);
            tableLayoutPanelLeft.Dock = DockStyle.Fill;
            tableLayoutPanelLeft.Location = new Point(3, 3);
            tableLayoutPanelLeft.Name = "tableLayoutPanelLeft";
            tableLayoutPanelLeft.RowCount = 4;
            tableLayoutPanelLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
            tableLayoutPanelLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanelLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));
            tableLayoutPanelLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            tableLayoutPanelLeft.Size = new Size(559, 757);
            tableLayoutPanelLeft.TabIndex = 23;
            // 
            // dataGridViewPrediction
            // 
            dataGridViewPrediction.AutoSize = true;
            dataGridViewPrediction.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            dataGridViewPrediction.Dock = DockStyle.Fill;
            dataGridViewPrediction.EnableTitle = false;
            dataGridViewPrediction.Location = new Point(3, 300);
            dataGridViewPrediction.Name = "dataGridViewPrediction";
            dataGridViewPrediction.Size = new Size(553, 241);
            dataGridViewPrediction.TabIndex = 18;
            dataGridViewPrediction.Title = "";
            // 
            // dataGridViewOrig
            // 
            dataGridViewOrig.AutoSize = true;
            dataGridViewOrig.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            dataGridViewOrig.Dock = DockStyle.Fill;
            dataGridViewOrig.EnableTitle = false;
            dataGridViewOrig.Location = new Point(3, 3);
            dataGridViewOrig.Name = "dataGridViewOrig";
            dataGridViewOrig.Size = new Size(553, 241);
            dataGridViewOrig.TabIndex = 17;
            dataGridViewOrig.Title = "";
            // 
            // panelLegend
            // 
            panelLegend.Dock = DockStyle.Fill;
            panelLegend.Location = new Point(3, 250);
            panelLegend.Name = "panelLegend";
            panelLegend.Size = new Size(553, 44);
            panelLegend.TabIndex = 9;
            // 
            // DataGridViewInvalidData
            // 
            DataGridViewInvalidData.Dock = DockStyle.Fill;
            DataGridViewInvalidData.Location = new Point(3, 547);
            DataGridViewInvalidData.Name = "DataGridViewInvalidData";
            DataGridViewInvalidData.Size = new Size(553, 207);
            DataGridViewInvalidData.TabIndex = 19;
            // 
            // PredictionControl
            // 
            Controls.Add(tableLayoutPanelMain);
            Name = "PredictionControl";
            Size = new Size(1413, 763);
            panelHistory.ResumeLayout(false);
            tableLayoutPanelMain.ResumeLayout(false);
            tableLayoutPanelRight.ResumeLayout(false);
            tableLayoutPanelManagement.ResumeLayout(false);
            tableLayoutPanelManagement.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
            panel4.ResumeLayout(false);
            panel4.PerformLayout();
            tableLayoutPanelTopLine.ResumeLayout(false);
            tableLayoutPanelTopLine.PerformLayout();
            tableLayoutPanelLeft.ResumeLayout(false);
            tableLayoutPanelLeft.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)DataGridViewInvalidData).EndInit();
            ResumeLayout(false);
        }

        private CheckBox checkBoxSaveSnapshot;
        private Panel panelHistory;
        private TextBox textBoxMinCount;
        private CheckBox checkBoxRound;
        private CheckBox checkBoxOnlyChanges;
        private CheckBox checkboxInterpolation;
        private CheckBox checkboxEnableSmooth;
        private TextBox textBoxLastPredictedFuelTable;
        private TextBox textBoxParsedData;
        private Button buttonValidate;
        private Button buttonContinue;
        private Button buttonParceSelectedPhoto;
        private CheckBox checkBoxShowOnlyMiplayerChange;
        private TextBox textBoxMinValueOfChange;
        private CheckBox checkBoxUseHistory;
        private TextBox textBoxRefPress;
        private TableLayoutPanel tableLayoutPanelMain;
        private Panel panelLegend;
        private ReadOnlyDataGridView dataGridViewOrig;
        private ReadOnlyDataGridView dataGridViewPrediction;
        private TableLayoutPanel tableLayoutPanelLeft;
        private HistoryControl historyControl1;
        private TableLayoutPanel tableLayoutPanelRight;
        private TableLayoutPanel tableLayoutPanelManagement;
        private Panel panel1;
        private Panel panel3;
        private Panel panel4;
        private Label labelMinCount;
        private Label labelPress;
        private Label labelValueOfChange;
        private TableLayoutPanel tableLayoutPanelTopLine;
        private TextBox textBoxImagePath;
        private DataGridView DataGridViewInvalidData;
        private TextBox textBoxMaxBenzDiff;
        private Label labelMaxBenzDiff;
        private CheckBox checkBoxAllwaysApplyNegativeTrim;
        private CheckBox checkBoxShowOnlyCount;
        private Button buttonConvert;
        private Button buttonExport;
    }
}