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
            scoreView = new PredictionScoreView();
            historyControl1 = new HistoryControl();
            textBoxMinCount = new TextBox();
            checkBoxRound = new CheckBox();
            checkBoxOnlyChanges = new CheckBox();
            buttonContinue = new Button();
            buttonParceSelectedPhoto = new Button();
            checkBoxShowOnlyMiplayerChange = new CheckBox();
            textBoxMinValueOfChange = new TextBox();
            checkBoxPressureCorrection = new CheckBox();
            checkBoxInterpolationWeights = new CheckBox();
            checkBoxTrimDelay = new CheckBox();
            textBoxRefPress = new TextBox();
            tableLayoutPanelMain = new TableLayoutPanel();
            tableLayoutPanelRight = new TableLayoutPanel();
            tableLayoutPanelManagement = new TableLayoutPanel();
            panel3 = new FlowLayoutPanel();
            checkBoxShowOnlyCount = new CheckBox();
            labelMaxBenzDiff = new Label();
            textBoxMaxBenzDiff = new TextBox();
            labelMinCount = new Label();
            labelPress = new Label();
            labelValueOfChange = new Label();
            panel4 = new FlowLayoutPanel();
            flowActions = new FlowLayoutPanel();
            flowChecks = new FlowLayoutPanel();
            tableNumbers = new TableLayoutPanel();
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
            checkBoxSaveSnapshot.Size = new Size(105, 20);
            checkBoxSaveSnapshot.TabIndex = 0;
            checkBoxSaveSnapshot.Text = "Save Snapshot";
            // 
            // panelHistory
            // 
            panelHistory.Controls.Add(historyControl1);
            panelHistory.Dock = DockStyle.Fill;
            panelHistory.Location = new Point(3, 399);
            panelHistory.Name = "panelHistory";
            panelHistory.Size = new Size(805, 355);
            panelHistory.TabIndex = 1;
            // 
            // historyControl1
            // 
            historyControl1.Dock = DockStyle.Fill;
            historyControl1.Location = new Point(0, 0);
            historyControl1.Name = "historyControl1";
            historyControl1.Size = new Size(805, 355);
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
            checkBoxRound.Size = new Size(61, 20);
            checkBoxRound.TabIndex = 4;
            checkBoxRound.Text = "Round";
            // 
            // checkBoxOnlyChanges
            // 
            checkBoxOnlyChanges.AutoSize = true;
            checkBoxOnlyChanges.Location = new Point(7, 23);
            checkBoxOnlyChanges.Name = "checkBoxOnlyChanges";
            checkBoxOnlyChanges.Size = new Size(99, 20);
            checkBoxOnlyChanges.TabIndex = 5;
            checkBoxOnlyChanges.Text = "Only changes";
            // 
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
            buttonParceSelectedPhoto.Location = new Point(647, 3);
            buttonParceSelectedPhoto.Name = "buttonParceSelectedPhoto";
            buttonParceSelectedPhoto.Size = new Size(74, 23);
            buttonParceSelectedPhoto.TabIndex = 14;
            buttonParceSelectedPhoto.Text = "Parse";
            buttonParceSelectedPhoto.Click += ButtonParceSelectedImage_Click;
            // 
            // checkBoxShowOnlyMiplayerChange
            // 
            checkBoxShowOnlyMiplayerChange.AutoSize = true;
            checkBoxShowOnlyMiplayerChange.Location = new Point(7, 48);
            checkBoxShowOnlyMiplayerChange.Name = "checkBoxShowOnlyMiplayerChange";
            checkBoxShowOnlyMiplayerChange.Size = new Size(199, 20);
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
            // checkBoxPressureCorrection
            // 
            checkBoxPressureCorrection.AutoSize = true;
            checkBoxPressureCorrection.Checked = true;
            checkBoxPressureCorrection.CheckState = CheckState.Checked;
            checkBoxPressureCorrection.Location = new Point(3, 61);
            checkBoxPressureCorrection.Name = "checkBoxPressureCorrection";
            checkBoxPressureCorrection.Size = new Size(135, 20);
            checkBoxPressureCorrection.TabIndex = 23;
            checkBoxPressureCorrection.Text = "Pressure Correction";
            checkBoxPressureCorrection.UseVisualStyleBackColor = true;
            // 
            // checkBoxInterpolationWeights
            // 
            checkBoxInterpolationWeights.AutoSize = true;
            checkBoxInterpolationWeights.Checked = true;
            checkBoxInterpolationWeights.CheckState = CheckState.Checked;
            checkBoxInterpolationWeights.Location = new Point(3, 86);
            checkBoxInterpolationWeights.Name = "checkBoxInterpolationWeights";
            checkBoxInterpolationWeights.Size = new Size(170, 20);
            checkBoxInterpolationWeights.TabIndex = 24;
            checkBoxInterpolationWeights.Text = "ECU Interpolation Weights";
            checkBoxInterpolationWeights.UseVisualStyleBackColor = true;
            // 
            // checkBoxTrimDelay
            // 
            checkBoxTrimDelay.AutoSize = true;
            checkBoxTrimDelay.Checked = true;
            checkBoxTrimDelay.CheckState = CheckState.Checked;
            checkBoxTrimDelay.Location = new Point(3, 111);
            checkBoxTrimDelay.Name = "checkBoxTrimDelay";
            checkBoxTrimDelay.Size = new Size(83, 20);
            checkBoxTrimDelay.TabIndex = 25;
            checkBoxTrimDelay.Text = "Trim Delay";
            checkBoxTrimDelay.UseVisualStyleBackColor = true;
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
            tableLayoutPanelMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42.17976F));
            tableLayoutPanelMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57.82024F));
            tableLayoutPanelMain.Controls.Add(tableLayoutPanelRight, 1, 0);
            tableLayoutPanelMain.Controls.Add(tableLayoutPanelLeft, 0, 0);
            tableLayoutPanelMain.Dock = DockStyle.Fill;
            tableLayoutPanelMain.Location = new Point(0, 0);
            tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            tableLayoutPanelMain.RowCount = 1;
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelMain.Size = new Size(1413, 763);
            tableLayoutPanelMain.TabIndex = 24;
            // 
            // tableLayoutPanelRight
            // 
            tableLayoutPanelRight.ColumnCount = 1;
            tableLayoutPanelRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            scoreView.Dock = DockStyle.Fill;
            scoreView.Name = "scoreView";
            tableLayoutPanelRight.Controls.Add(scoreView, 0, 2);
            tableLayoutPanelRight.Controls.Add(panelHistory, 0, 3);
            tableLayoutPanelRight.Controls.Add(tableLayoutPanelManagement, 0, 1);
            tableLayoutPanelRight.Controls.Add(tableLayoutPanelTopLine, 0, 0);
            tableLayoutPanelRight.Dock = DockStyle.Fill;
            tableLayoutPanelRight.ForeColor = SystemColors.ActiveCaptionText;
            tableLayoutPanelRight.Location = new Point(599, 3);
            tableLayoutPanelRight.Name = "tableLayoutPanelRight";
            // Image line, options, accuracy result, stored predictions (the list is the smaller part).
            tableLayoutPanelRight.RowCount = 4;
            tableLayoutPanelRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            tableLayoutPanelRight.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableLayoutPanelRight.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            tableLayoutPanelRight.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            tableLayoutPanelRight.Size = new Size(811, 757);
            tableLayoutPanelRight.TabIndex = 25;
            // 
            // tableLayoutPanelManagement
            // 
            tableLayoutPanelManagement.ColumnCount = 2;
            tableLayoutPanelManagement.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanelManagement.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanelManagement.Controls.Add(panel3, 0, 0);
            tableLayoutPanelManagement.Controls.Add(panel4, 1, 0);
            tableLayoutPanelManagement.AutoSize = true;
            tableLayoutPanelManagement.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tableLayoutPanelManagement.Dock = DockStyle.Fill;
            tableLayoutPanelManagement.Location = new Point(3, 38);
            tableLayoutPanelManagement.Name = "tableLayoutPanelManagement";
            tableLayoutPanelManagement.RowCount = 1;
            // The options take the height they need; the history list below gets the rest.
            tableLayoutPanelManagement.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableLayoutPanelManagement.Size = new Size(805, 355);
            tableLayoutPanelManagement.TabIndex = 11;
            // 
            // panel3
            // 
            // Three groups that wrap as a whole: the actions line on its own row, then the
            // checkboxes column and the number fields side by side (stacked when narrow).
            flowActions.AutoSize = true;
            flowActions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowActions.WrapContents = false;
            flowActions.Margin = new Padding(0);
            flowActions.Controls.Add(buttonContinue);
            flowActions.Controls.Add(checkBoxSaveSnapshot);
            checkBoxSaveSnapshot.Anchor = AnchorStyles.Left;
            checkBoxSaveSnapshot.Margin = new Padding(9, 3, 3, 3);

            flowChecks.AutoSize = true;
            flowChecks.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowChecks.FlowDirection = FlowDirection.TopDown;
            flowChecks.WrapContents = false;
            flowChecks.Margin = new Padding(0, 0, 12, 0);
            flowChecks.Controls.Add(checkBoxOnlyChanges);
            flowChecks.Controls.Add(checkBoxShowOnlyMiplayerChange);
            flowChecks.Controls.Add(checkBoxRound);
            flowChecks.Controls.Add(checkBoxShowOnlyCount);

            tableNumbers.AutoSize = true;
            tableNumbers.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tableNumbers.ColumnCount = 2;
            tableNumbers.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tableNumbers.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tableNumbers.RowCount = 4;
            for (int r = 0; r < 4; r++)
                tableNumbers.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tableNumbers.Margin = new Padding(0);
            tableNumbers.Controls.Add(labelValueOfChange, 0, 0);
            tableNumbers.Controls.Add(textBoxMinValueOfChange, 1, 0);
            tableNumbers.Controls.Add(labelPress, 0, 1);
            tableNumbers.Controls.Add(textBoxRefPress, 1, 1);
            tableNumbers.Controls.Add(labelMinCount, 0, 2);
            tableNumbers.Controls.Add(textBoxMinCount, 1, 2);
            tableNumbers.Controls.Add(labelMaxBenzDiff, 0, 3);
            tableNumbers.Controls.Add(textBoxMaxBenzDiff, 1, 3);
            foreach (var label in new[] { labelValueOfChange, labelPress, labelMinCount, labelMaxBenzDiff })
            {
                label.Anchor = AnchorStyles.Left;
                label.Margin = new Padding(3);
            }
            foreach (var box in new[] { textBoxMinValueOfChange, textBoxRefPress, textBoxMinCount, textBoxMaxBenzDiff })
            {
                box.RightToLeft = RightToLeft.No;
                box.TextAlign = HorizontalAlignment.Right;
                box.Size = new Size(55, 23);
            }

            panel3.Controls.Add(flowActions);
            panel3.SetFlowBreak(flowActions, true);
            panel3.Controls.Add(flowChecks);
            panel3.Controls.Add(tableNumbers);
            panel3.AutoSize = true;
            panel3.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel3.AutoScroll = true;
            panel3.Dock = DockStyle.Fill;
            panel3.Location = new Point(3, 180);
            panel3.Name = "panel3";
            panel3.Size = new Size(396, 172);
            panel3.TabIndex = 9;
            // 
            // checkBoxShowOnlyCount
            // 
            checkBoxShowOnlyCount.AutoSize = true;
            checkBoxShowOnlyCount.Location = new Point(4, 121);
            checkBoxShowOnlyCount.Name = "checkBoxShowOnlyCount";
            checkBoxShowOnlyCount.Size = new Size(120, 20);
            checkBoxShowOnlyCount.TabIndex = 29;
            checkBoxShowOnlyCount.Text = "Show Only Count";
            checkBoxShowOnlyCount.UseVisualStyleBackColor = true;
            // 
            // labelMaxBenzDiff
            // 
            labelMaxBenzDiff.AutoSize = true;
            labelMaxBenzDiff.Margin = new Padding(9, 7, 0, 0);
            labelMaxBenzDiff.Location = new Point(255, 141);
            labelMaxBenzDiff.Name = "labelMaxBenzDiff";
            labelMaxBenzDiff.Size = new Size(109, 16);
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
            textBoxMaxBenzDiff.Text = "6";
            // 
            // labelMinCount
            // 
            labelMinCount.AutoSize = true;
            labelMinCount.Margin = new Padding(9, 7, 0, 0);
            labelMinCount.Location = new Point(253, 106);
            labelMinCount.Name = "labelMinCount";
            labelMinCount.Size = new Size(106, 16);
            labelMinCount.TabIndex = 25;
            labelMinCount.Text = "Min Count for Cell";
            // 
            // labelPress
            // 
            labelPress.AutoSize = true;
            labelPress.Margin = new Padding(9, 7, 0, 0);
            labelPress.Location = new Point(273, 72);
            labelPress.Name = "labelPress";
            labelPress.Size = new Size(94, 16);
            labelPress.TabIndex = 24;
            labelPress.Text = "Reference Press";
            // 
            // labelValueOfChange
            // 
            labelValueOfChange.AutoSize = true;
            labelValueOfChange.Margin = new Padding(9, 7, 0, 0);
            labelValueOfChange.Location = new Point(233, 52);
            labelValueOfChange.Name = "labelValueOfChange";
            labelValueOfChange.Size = new Size(127, 16);
            labelValueOfChange.TabIndex = 23;
            labelValueOfChange.Text = "Min Valid % of change";
            // 
            // panel4
            // 
            panel4.Controls.Add(checkBoxPressureCorrection);
            panel4.Controls.Add(checkBoxInterpolationWeights);
            panel4.Controls.Add(checkBoxTrimDelay);
            panel4.Controls.Add(buttonExport);
            panel4.AutoSize = true;
            panel4.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel4.AutoScroll = true;
            panel4.FlowDirection = FlowDirection.TopDown;
            panel4.WrapContents = false;
            panel4.Dock = DockStyle.Fill;
            panel4.Location = new Point(405, 180);
            panel4.Name = "panel4";
            panel4.Size = new Size(397, 172);
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
            tableLayoutPanelTopLine.Size = new Size(805, 29);
            tableLayoutPanelTopLine.TabIndex = 12;
            // 
            // buttonConvert
            // 
            buttonConvert.Location = new Point(727, 3);
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
            textBoxImagePath.Size = new Size(638, 23);
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
            tableLayoutPanelLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            tableLayoutPanelLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanelLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            tableLayoutPanelLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tableLayoutPanelLeft.Size = new Size(590, 757);
            tableLayoutPanelLeft.TabIndex = 23;
            // 
            // dataGridViewPrediction
            // 
            dataGridViewPrediction.AutoSize = true;
            dataGridViewPrediction.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            dataGridViewPrediction.Dock = DockStyle.Fill;
            dataGridViewPrediction.EnableTitle = false;
            dataGridViewPrediction.Location = new Point(3, 335);
            dataGridViewPrediction.Name = "dataGridViewPrediction";
            dataGridViewPrediction.Size = new Size(584, 276);
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
            dataGridViewOrig.Size = new Size(584, 276);
            dataGridViewOrig.TabIndex = 17;
            dataGridViewOrig.Title = "";
            // 
            // panelLegend
            // 
            panelLegend.Dock = DockStyle.Fill;
            panelLegend.Location = new Point(3, 285);
            panelLegend.Name = "panelLegend";
            panelLegend.Size = new Size(584, 44);
            panelLegend.TabIndex = 9;
            // 
            // DataGridViewInvalidData
            // 
            DataGridViewInvalidData.Dock = DockStyle.Fill;
            DataGridViewInvalidData.Location = new Point(3, 617);
            DataGridViewInvalidData.Name = "DataGridViewInvalidData";
            DataGridViewInvalidData.Size = new Size(584, 137);
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
        private PredictionScoreView scoreView;
        private TextBox textBoxMinCount;
        private CheckBox checkBoxRound;
        private CheckBox checkBoxOnlyChanges;
        private Button buttonContinue;
        private Button buttonParceSelectedPhoto;
        private CheckBox checkBoxShowOnlyMiplayerChange;
        private TextBox textBoxMinValueOfChange;
        private TextBox textBoxRefPress;
        private CheckBox checkBoxPressureCorrection;
        private CheckBox checkBoxInterpolationWeights;
        private CheckBox checkBoxTrimDelay;
        private TableLayoutPanel tableLayoutPanelMain;
        private Panel panelLegend;
        private ReadOnlyDataGridView dataGridViewOrig;
        private ReadOnlyDataGridView dataGridViewPrediction;
        private TableLayoutPanel tableLayoutPanelLeft;
        private HistoryControl historyControl1;
        private TableLayoutPanel tableLayoutPanelRight;
        private TableLayoutPanel tableLayoutPanelManagement;
        private FlowLayoutPanel panel3;
        private FlowLayoutPanel panel4;
        private FlowLayoutPanel flowActions;
        private FlowLayoutPanel flowChecks;
        private TableLayoutPanel tableNumbers;
        private Label labelMinCount;
        private Label labelPress;
        private Label labelValueOfChange;
        private TableLayoutPanel tableLayoutPanelTopLine;
        private TextBox textBoxImagePath;
        private DataGridView DataGridViewInvalidData;
        private TextBox textBoxMaxBenzDiff;
        private Label labelMaxBenzDiff;
        private CheckBox checkBoxShowOnlyCount;
        private Button buttonConvert;
        private Button buttonExport;
    }
}