using LPGDataAnalyzer.Controls;

namespace LPGDataAnalyzer
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle5 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle6 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle7 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle8 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle9 = new DataGridViewCellStyle();
            openFileDialog1 = new OpenFileDialog();
            statusBar = new StatusStrip();
            toolStripSummary = new ToolStripStatusLabel();
            label1 = new Label();
            tbBenzTimingFilterCuting = new TextBox();
            txtFilePath = new TextBox();
            buttonSelectFile = new Button();
            tabControlMain = new TabControl();
            tabPageMainData = new TabPage();
            dataGridViewMainData = new DataItemGrid();
            tabPageAnalyses = new TabPage();
            analysisUC = new AnalysisUC();
            tabPageGroupByTemp = new TabPage();
            temperatureAnalyzerui1 = new TemperatureAnalyzerUI();
            tabPageMapAnalysis = new TabPage();
            dataGridView1 = new DataGridViewUC();
            dataGridViewInjectionTimeAnalisys = new DataGridViewUC();
            dataGridViewMapAnalysis = new DataGridViewUC();
            tabPagePredictions = new TabPage();
            predictionControl1 = new PredictionControl();
            tabPageReducerPred = new TabPage();
            reducerTempCorrection1 = new ReducerTempCorrection();
            buttonAnalysisByMap = new Button();
            button1 = new Button();
            buttonExtraInjectionCalculator = new Button();
            statusBar.SuspendLayout();
            tabControlMain.SuspendLayout();
            tabPageMainData.SuspendLayout();
            tabPageAnalyses.SuspendLayout();
            tabPageGroupByTemp.SuspendLayout();
            tabPageMapAnalysis.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewInjectionTimeAnalisys).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewMapAnalysis).BeginInit();
            tabPagePredictions.SuspendLayout();
            tabPageReducerPred.SuspendLayout();
            SuspendLayout();
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // statusBar
            // 
            statusBar.Items.AddRange(new ToolStripItem[] { toolStripSummary });
            statusBar.Location = new Point(0, 846);
            statusBar.Name = "statusBar";
            statusBar.Size = new Size(1416, 22);
            statusBar.TabIndex = 3;
            statusBar.Text = "statusStrip1";
            // 
            // toolStripSummary
            // 
            toolStripSummary.Name = "toolStripSummary";
            toolStripSummary.Size = new Size(118, 17);
            toolStripSummary.Text = "toolStripStatusLabel1";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(831, 36);
            label1.Name = "label1";
            label1.Size = new Size(133, 15);
            label1.TabIndex = 9;
            label1.Text = "Skip time group analyse";
            // 
            // tbBenzTimingFilterCuting
            // 
            tbBenzTimingFilterCuting.AccessibleDescription = "Benz Timing Cutting Filter ";
            tbBenzTimingFilterCuting.Location = new Point(970, 37);
            tbBenzTimingFilterCuting.Name = "tbBenzTimingFilterCuting";
            tbBenzTimingFilterCuting.Size = new Size(34, 23);
            tbBenzTimingFilterCuting.TabIndex = 7;
            tbBenzTimingFilterCuting.Text = "2.4";
            tbBenzTimingFilterCuting.TextAlign = HorizontalAlignment.Right;
            // 
            // txtFilePath
            // 
            txtFilePath.Location = new Point(7, 11);
            txtFilePath.Name = "txtFilePath";
            txtFilePath.ReadOnly = true;
            txtFilePath.Size = new Size(843, 23);
            txtFilePath.TabIndex = 0;
            // 
            // buttonSelectFile
            // 
            buttonSelectFile.Location = new Point(856, 10);
            buttonSelectFile.Name = "buttonSelectFile";
            buttonSelectFile.Size = new Size(108, 23);
            buttonSelectFile.TabIndex = 1;
            buttonSelectFile.Text = "Select Txt File";
            buttonSelectFile.UseVisualStyleBackColor = true;
            buttonSelectFile.Click += BtnSelectFile_Click;
            // 
            // tabControlMain
            // 
            tabControlMain.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControlMain.Controls.Add(tabPageMainData);
            tabControlMain.Controls.Add(tabPageAnalyses);
            tabControlMain.Controls.Add(tabPageGroupByTemp);
            tabControlMain.Controls.Add(tabPageMapAnalysis);
            tabControlMain.Controls.Add(tabPagePredictions);
            tabControlMain.Controls.Add(tabPageReducerPred);
            tabControlMain.Location = new Point(0, 66);
            tabControlMain.Name = "tabControlMain";
            tabControlMain.SelectedIndex = 0;
            tabControlMain.Size = new Size(1416, 777);
            tabControlMain.TabIndex = 5;
            // 
            // tabPageMainData
            // 
            tabPageMainData.Controls.Add(dataGridViewMainData);
            tabPageMainData.Location = new Point(4, 24);
            tabPageMainData.Name = "tabPageMainData";
            tabPageMainData.Padding = new Padding(3);
            tabPageMainData.Size = new Size(1408, 749);
            tabPageMainData.TabIndex = 0;
            tabPageMainData.Text = "Main Data";
            tabPageMainData.UseVisualStyleBackColor = true;
            // 
            // dataGridViewMainData
            // 
            dataGridViewMainData.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridViewMainData.Location = new Point(-4, 0);
            dataGridViewMainData.Name = "dataGridViewMainData";
            dataGridViewMainData.ReadOnly = true;
            dataGridViewMainData.Size = new Size(1412, 749);
            dataGridViewMainData.TabIndex = 0;
            dataGridViewMainData.Title = "All logged data";
            // 
            // tabPageAnalyses
            // 
            tabPageAnalyses.Controls.Add(analysisUC);
            tabPageAnalyses.Location = new Point(4, 24);
            tabPageAnalyses.Name = "tabPageAnalyses";
            tabPageAnalyses.Padding = new Padding(3);
            tabPageAnalyses.Size = new Size(192, 72);
            tabPageAnalyses.TabIndex = 1;
            tabPageAnalyses.Text = "Analyses";
            tabPageAnalyses.UseVisualStyleBackColor = true;
            // 
            // analysisUC
            // 
            analysisUC.Location = new Point(0, 0);
            analysisUC.Name = "analysisUC";
            analysisUC.Size = new Size(1516, 848);
            analysisUC.TabIndex = 0;
            // 
            // tabPageGroupByTemp
            // 
            tabPageGroupByTemp.Controls.Add(temperatureAnalyzerui1);
            tabPageGroupByTemp.Location = new Point(4, 24);
            tabPageGroupByTemp.Name = "tabPageGroupByTemp";
            tabPageGroupByTemp.Size = new Size(1408, 749);
            tabPageGroupByTemp.TabIndex = 2;
            tabPageGroupByTemp.Text = "Temperature";
            tabPageGroupByTemp.UseVisualStyleBackColor = true;
            // 
            // temperatureAnalyzerui1
            // 
            temperatureAnalyzerui1.Dock = DockStyle.Fill;
            temperatureAnalyzerui1.Location = new Point(0, 0);
            temperatureAnalyzerui1.Name = "temperatureAnalyzerui1";
            temperatureAnalyzerui1.Size = new Size(1408, 749);
            temperatureAnalyzerui1.TabIndex = 0;
            // 
            // tabPageMapAnalysis
            // 
            tabPageMapAnalysis.Controls.Add(dataGridView1);
            tabPageMapAnalysis.Controls.Add(dataGridViewInjectionTimeAnalisys);
            tabPageMapAnalysis.Controls.Add(dataGridViewMapAnalysis);
            tabPageMapAnalysis.Location = new Point(4, 24);
            tabPageMapAnalysis.Name = "tabPageMapAnalysis";
            tabPageMapAnalysis.Size = new Size(192, 72);
            tabPageMapAnalysis.TabIndex = 3;
            tabPageMapAnalysis.Text = "Map Analysis";
            tabPageMapAnalysis.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.AllowUserToResizeRows = false;
            dataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(45, 45, 48);
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dataGridViewCellStyle1.ForeColor = Color.White;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            dataGridView1.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = SystemColors.Window;
            dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle2.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = Color.Yellow;
            dataGridViewCellStyle2.SelectionForeColor = Color.Black;
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            dataGridView1.DefaultCellStyle = dataGridViewCellStyle2;
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.Location = new Point(-427, 3);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.ReadOnly = true;
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = Color.FromArgb(45, 45, 48);
            dataGridViewCellStyle3.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dataGridViewCellStyle3.ForeColor = Color.White;
            dataGridViewCellStyle3.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = DataGridViewTriState.True;
            dataGridView1.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView1.Size = new Size(616, 66);
            dataGridView1.TabIndex = 3;
            // 
            // dataGridViewInjectionTimeAnalisys
            // 
            dataGridViewInjectionTimeAnalisys.AllowUserToAddRows = false;
            dataGridViewInjectionTimeAnalisys.AllowUserToDeleteRows = false;
            dataGridViewInjectionTimeAnalisys.AllowUserToResizeRows = false;
            dataGridViewInjectionTimeAnalisys.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridViewInjectionTimeAnalisys.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle4.BackColor = Color.FromArgb(45, 45, 48);
            dataGridViewCellStyle4.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dataGridViewCellStyle4.ForeColor = Color.White;
            dataGridViewCellStyle4.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle4.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle4.WrapMode = DataGridViewTriState.True;
            dataGridViewInjectionTimeAnalisys.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle4;
            dataGridViewInjectionTimeAnalisys.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle5.BackColor = SystemColors.Window;
            dataGridViewCellStyle5.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle5.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle5.SelectionBackColor = Color.Yellow;
            dataGridViewCellStyle5.SelectionForeColor = Color.Black;
            dataGridViewCellStyle5.WrapMode = DataGridViewTriState.False;
            dataGridViewInjectionTimeAnalisys.DefaultCellStyle = dataGridViewCellStyle5;
            dataGridViewInjectionTimeAnalisys.EnableHeadersVisualStyles = false;
            dataGridViewInjectionTimeAnalisys.Location = new Point(3, -279);
            dataGridViewInjectionTimeAnalisys.Name = "dataGridViewInjectionTimeAnalisys";
            dataGridViewInjectionTimeAnalisys.ReadOnly = true;
            dataGridViewCellStyle6.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle6.BackColor = Color.FromArgb(45, 45, 48);
            dataGridViewCellStyle6.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dataGridViewCellStyle6.ForeColor = Color.White;
            dataGridViewCellStyle6.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle6.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle6.WrapMode = DataGridViewTriState.True;
            dataGridViewInjectionTimeAnalisys.RowHeadersDefaultCellStyle = dataGridViewCellStyle6;
            dataGridViewInjectionTimeAnalisys.RowHeadersVisible = false;
            dataGridViewInjectionTimeAnalisys.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridViewInjectionTimeAnalisys.Size = new Size(0, 348);
            dataGridViewInjectionTimeAnalisys.TabIndex = 2;
            // 
            // dataGridViewMapAnalysis
            // 
            dataGridViewMapAnalysis.AllowUserToAddRows = false;
            dataGridViewMapAnalysis.AllowUserToDeleteRows = false;
            dataGridViewMapAnalysis.AllowUserToResizeRows = false;
            dataGridViewMapAnalysis.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridViewMapAnalysis.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewCellStyle7.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle7.BackColor = Color.FromArgb(45, 45, 48);
            dataGridViewCellStyle7.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dataGridViewCellStyle7.ForeColor = Color.White;
            dataGridViewCellStyle7.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle7.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle7.WrapMode = DataGridViewTriState.True;
            dataGridViewMapAnalysis.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle7;
            dataGridViewMapAnalysis.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle8.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle8.BackColor = SystemColors.Window;
            dataGridViewCellStyle8.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle8.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle8.SelectionBackColor = Color.Yellow;
            dataGridViewCellStyle8.SelectionForeColor = Color.Black;
            dataGridViewCellStyle8.WrapMode = DataGridViewTriState.False;
            dataGridViewMapAnalysis.DefaultCellStyle = dataGridViewCellStyle8;
            dataGridViewMapAnalysis.EnableHeadersVisualStyles = false;
            dataGridViewMapAnalysis.Location = new Point(3, 3);
            dataGridViewMapAnalysis.Name = "dataGridViewMapAnalysis";
            dataGridViewMapAnalysis.ReadOnly = true;
            dataGridViewCellStyle9.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle9.BackColor = Color.FromArgb(45, 45, 48);
            dataGridViewCellStyle9.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dataGridViewCellStyle9.ForeColor = Color.White;
            dataGridViewCellStyle9.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle9.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle9.WrapMode = DataGridViewTriState.True;
            dataGridViewMapAnalysis.RowHeadersDefaultCellStyle = dataGridViewCellStyle9;
            dataGridViewMapAnalysis.RowHeadersVisible = false;
            dataGridViewMapAnalysis.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridViewMapAnalysis.Size = new Size(0, 0);
            dataGridViewMapAnalysis.TabIndex = 0;
            // 
            // tabPagePredictions
            // 
            tabPagePredictions.Controls.Add(predictionControl1);
            tabPagePredictions.Location = new Point(4, 24);
            tabPagePredictions.Name = "tabPagePredictions";
            tabPagePredictions.Size = new Size(192, 72);
            tabPagePredictions.TabIndex = 4;
            tabPagePredictions.Text = "Prediction";
            tabPagePredictions.UseVisualStyleBackColor = true;
            // 
            // predictionControl1
            // 
            predictionControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            predictionControl1.Location = new Point(0, 3);
            predictionControl1.Name = "predictionControl1";
            predictionControl1.Size = new Size(192, 72);
            predictionControl1.TabIndex = 0;
            // 
            // tabPageReducerPred
            // 
            tabPageReducerPred.Controls.Add(reducerTempCorrection1);
            tabPageReducerPred.Location = new Point(4, 24);
            tabPageReducerPred.Name = "tabPageReducerPred";
            tabPageReducerPred.Size = new Size(192, 72);
            tabPageReducerPred.TabIndex = 5;
            tabPageReducerPred.Text = "Reducer prediction";
            tabPageReducerPred.UseVisualStyleBackColor = true;
            // 
            // reducerTempCorrection1
            // 
            reducerTempCorrection1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            reducerTempCorrection1.AutoSize = true;
            reducerTempCorrection1.Location = new Point(191, 145);
            reducerTempCorrection1.Name = "reducerTempCorrection1";
            reducerTempCorrection1.Size = new Size(631, 240);
            reducerTempCorrection1.TabIndex = 0;
            // 
            // buttonAnalysisByMap
            // 
            buttonAnalysisByMap.Location = new Point(1014, 36);
            buttonAnalysisByMap.Name = "buttonAnalysisByMap";
            buttonAnalysisByMap.Size = new Size(68, 23);
            buttonAnalysisByMap.TabIndex = 16;
            buttonAnalysisByMap.Text = "By Map";
            buttonAnalysisByMap.UseVisualStyleBackColor = true;
            buttonAnalysisByMap.Click += buttonAnalysisByMap_Click;
            // 
            // button1
            // 
            button1.Location = new Point(1014, 10);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 17;
            button1.Text = "button1";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // buttonExtraInjectionCalculator
            // 
            buttonExtraInjectionCalculator.Location = new Point(1216, 11);
            buttonExtraInjectionCalculator.Name = "buttonExtraInjectionCalculator";
            buttonExtraInjectionCalculator.Size = new Size(152, 23);
            buttonExtraInjectionCalculator.TabIndex = 21;
            buttonExtraInjectionCalculator.Text = "Extra Injection Calculator";
            buttonExtraInjectionCalculator.UseVisualStyleBackColor = true;
            buttonExtraInjectionCalculator.Click += buttonExtraInjectionCalculator_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1416, 868);
            Controls.Add(buttonExtraInjectionCalculator);
            Controls.Add(button1);
            Controls.Add(buttonAnalysisByMap);
            Controls.Add(txtFilePath);
            Controls.Add(tbBenzTimingFilterCuting);
            Controls.Add(label1);
            Controls.Add(tabControlMain);
            Controls.Add(buttonSelectFile);
            Controls.Add(statusBar);
            Name = "MainForm";
            Text = "LPG Analyser";
            WindowState = FormWindowState.Maximized;
            statusBar.ResumeLayout(false);
            statusBar.PerformLayout();
            tabControlMain.ResumeLayout(false);
            tabPageMainData.ResumeLayout(false);
            tabPageAnalyses.ResumeLayout(false);
            tabPageGroupByTemp.ResumeLayout(false);
            tabPageMapAnalysis.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewInjectionTimeAnalisys).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewMapAnalysis).EndInit();
            tabPagePredictions.ResumeLayout(false);
            tabPageReducerPred.ResumeLayout(false);
            tabPageReducerPred.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private OpenFileDialog openFileDialog1;
        private StatusStrip statusBar;
        private TextBox txtFilePath;
        private Button buttonSelectFile;
        private TextBox tbBenzTimingFilterCuting;
        private Label label1;
        private ToolStripStatusLabel toolStripSummary;
        private TabControl tabControlMain;
        private TabPage tabPageMainData;
        private TabPage tabPageGroupByTemp;
        private Button buttonAnalysisByMap;
        private TabPage tabPageMapAnalysis;
        private DataGridViewUC dataGridViewMapAnalysis;
        private DataGridViewUC dataGridViewInjectionTimeAnalisys;
        private DataGridViewUC dataGridView1;
        private Button button1;
        private TabPage tabPagePredictions;
        private TabPage tabPageReducerPred;
        private Button buttonExtraInjectionCalculator;
        private Controls.DataItemGrid dataGridViewMainData;
        private PredictionControl predictionControl1;
        private ReducerTempCorrection reducerTempCorrection1;
        private TabPage tabPageAnalyses;
        private AnalysisUC analysisUC;
        private TemperatureAnalyzerUI temperatureAnalyzerui1;
    }
}
