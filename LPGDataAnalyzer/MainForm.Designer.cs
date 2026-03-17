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
            tableLayoutPanelAnalyses = new TableLayoutPanel();
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
            dataGridViewAnalyzeDataBank2t2 = new ReadOnlyDataGridView();
            dataGridViewAnalyzeDataBank1t2 = new ReadOnlyDataGridView();
            dataGridViewAnalyzeDataBank2t1 = new ReadOnlyDataGridView();
            dataGridViewAnalyzeDataBank1t1 = new ReadOnlyDataGridView();
            tabPageGroupByTemp = new TabPage();
            dataGridViewRIDData = new DataGridView();
            dataGridViewGasData = new DataGridView();
            tabPageMapAnalysis = new TabPage();
            dataGridView1 = new DataGridView();
            dataGridViewInjectionTimeAnalisys = new DataGridView();
            dataGridViewMapAnalysis = new DataGridView();
            tabPagePredictions = new TabPage();
            tabPageReducerPred = new TabPage();
            labelRefPressure = new Label();
            textBoxReferencePressure = new TextBox();
            labelCurrent = new Label();
            textBoxReducerTempValues = new TextBox();
            buttonReducerPrediction = new Button();
            buttonAnalyzeFastTrim = new Button();
            buttonAnalyze = new Button();
            comboBoxReductorTempGroup2 = new ComboBox();
            comboBoxReductorTempGroup1 = new ComboBox();
            comboBoxGasTemperatureb2 = new ComboBox();
            comboBoxGasTemperatureb1 = new ComboBox();
            buttonAnalysisByMap = new Button();
            button1 = new Button();
            buttonReducerThermalLag = new Button();
            buttonAFR = new Button();
            buttonExtraInjectionCalculator = new Button();
            buttonShowReducerPress = new Button();
            comboBoxAggregation = new ComboBox();
            buttonGroupByTemp = new Button();
            predictionControl1 = new PredictionControl();
            statusBar.SuspendLayout();
            tabControlMain.SuspendLayout();
            tabPageMainData.SuspendLayout();
            tabPageAnalyses.SuspendLayout();
            tabPageGroupByTemp.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewRIDData).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewGasData).BeginInit();
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
            tbBenzTimingFilterCuting.Location = new Point(891, 52);
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
            tabControlMain.Location = new Point(0, 81);
            tabControlMain.Name = "tabControlMain";
            tabControlMain.SelectedIndex = 0;
            tabControlMain.Size = new Size(1416, 762);
            tabControlMain.TabIndex = 5;
            // 
            // tabPageMainData
            // 
            tabPageMainData.Controls.Add(dataGridViewMainData);
            tabPageMainData.Location = new Point(4, 24);
            tabPageMainData.Name = "tabPageMainData";
            tabPageMainData.Padding = new Padding(3);
            tabPageMainData.Size = new Size(1408, 734);
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
            dataGridViewMainData.Size = new Size(1412, 734);
            dataGridViewMainData.TabIndex = 0;
            dataGridViewMainData.Title = "All logged data";
            // 
            // tabPageAnalyses
            // 
            tabPageAnalyses.Location = new Point(4, 24);
            tabPageAnalyses.Name = "tabPageAnalyses";
            tabPageAnalyses.Padding = new Padding(3);
            tabPageAnalyses.Size = new Size(1408, 734);
            tabPageAnalyses.TabIndex = 1;
            tabPageAnalyses.Text = "Analyses";
            tabPageAnalyses.UseVisualStyleBackColor = true;
            // 
            // dataGridViewAnalyzeDataBank2t2
            // 
            dataGridViewAnalyzeDataBank2t2.Location = new Point(608, 378);
            dataGridViewAnalyzeDataBank2t2.Name = "dataGridViewAnalyzeDataBank2t2";
            dataGridViewAnalyzeDataBank2t2.Size = new Size(797, 350);
            dataGridViewAnalyzeDataBank2t2.TabIndex = 7;
            dataGridViewAnalyzeDataBank2t2.Title = "My Read-Only Data Grid";
            // 
            // dataGridViewAnalyzeDataBank1t2
            // 
            dataGridViewAnalyzeDataBank1t2.Location = new Point(608, 3);
            dataGridViewAnalyzeDataBank1t2.Name = "dataGridViewAnalyzeDataBank1t2";
            dataGridViewAnalyzeDataBank1t2.Size = new Size(800, 369);
            dataGridViewAnalyzeDataBank1t2.TabIndex = 6;
            dataGridViewAnalyzeDataBank1t2.Title = "My Read-Only Data Grid";
            // 
            // dataGridViewAnalyzeDataBank2t1
            // 
            dataGridViewAnalyzeDataBank2t1.Location = new Point(0, 349);
            dataGridViewAnalyzeDataBank2t1.Name = "dataGridViewAnalyzeDataBank2t1";
            dataGridViewAnalyzeDataBank2t1.Size = new Size(706, 379);
            dataGridViewAnalyzeDataBank2t1.TabIndex = 5;
            dataGridViewAnalyzeDataBank2t1.Title = "My Read-Only Data Grid";
            // 
            // dataGridViewAnalyzeDataBank1t1
            // 
            dataGridViewAnalyzeDataBank1t1.Location = new Point(0, 3);
            dataGridViewAnalyzeDataBank1t1.Name = "dataGridViewAnalyzeDataBank1t1";
            dataGridViewAnalyzeDataBank1t1.Size = new Size(706, 350);
            dataGridViewAnalyzeDataBank1t1.TabIndex = 4;
            dataGridViewAnalyzeDataBank1t1.Title = "My Read-Only Data Grid";
            // 
            // tabPageGroupByTemp
            // 
            tabPageGroupByTemp.Controls.Add(dataGridViewRIDData);
            tabPageGroupByTemp.Controls.Add(dataGridViewGasData);
            tabPageGroupByTemp.Location = new Point(4, 24);
            tabPageGroupByTemp.Name = "tabPageGroupByTemp";
            tabPageGroupByTemp.Size = new Size(1408, 734);
            tabPageGroupByTemp.TabIndex = 2;
            tabPageGroupByTemp.Text = "Group By Temp";
            tabPageGroupByTemp.UseVisualStyleBackColor = true;
            // 
            // dataGridViewRIDData
            // 
            dataGridViewRIDData.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            dataGridViewRIDData.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewRIDData.Location = new Point(502, 3);
            dataGridViewRIDData.Name = "dataGridViewRIDData";
            dataGridViewRIDData.Size = new Size(898, 728);
            dataGridViewRIDData.TabIndex = 2;
            // 
            // dataGridViewGasData
            // 
            dataGridViewGasData.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            dataGridViewGasData.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewGasData.Location = new Point(3, 3);
            dataGridViewGasData.Name = "dataGridViewGasData";
            dataGridViewGasData.Size = new Size(843, 731);
            dataGridViewGasData.TabIndex = 0;
            // 
            // tabPageMapAnalysis
            // 
            tabPageMapAnalysis.Controls.Add(dataGridView1);
            tabPageMapAnalysis.Controls.Add(dataGridViewInjectionTimeAnalisys);
            tabPageMapAnalysis.Controls.Add(dataGridViewMapAnalysis);
            tabPageMapAnalysis.Location = new Point(4, 24);
            tabPageMapAnalysis.Name = "tabPageMapAnalysis";
            tabPageMapAnalysis.Size = new Size(1408, 734);
            tabPageMapAnalysis.TabIndex = 3;
            tabPageMapAnalysis.Text = "Map Analysis";
            tabPageMapAnalysis.UseVisualStyleBackColor = true;
            // 
            // dataGridView1
            // 
            dataGridView1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Location = new Point(789, 3);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.Size = new Size(616, 728);
            dataGridView1.TabIndex = 3;
            // 
            // dataGridViewInjectionTimeAnalisys
            // 
            dataGridViewInjectionTimeAnalisys.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridViewInjectionTimeAnalisys.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewInjectionTimeAnalisys.Location = new Point(3, 383);
            dataGridViewInjectionTimeAnalisys.Name = "dataGridViewInjectionTimeAnalisys";
            dataGridViewInjectionTimeAnalisys.ReadOnly = true;
            dataGridViewInjectionTimeAnalisys.Size = new Size(780, 348);
            dataGridViewInjectionTimeAnalisys.TabIndex = 2;
            // 
            // dataGridViewMapAnalysis
            // 
            dataGridViewMapAnalysis.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridViewMapAnalysis.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewMapAnalysis.Location = new Point(3, 3);
            dataGridViewMapAnalysis.Name = "dataGridViewMapAnalysis";
            dataGridViewMapAnalysis.ReadOnly = true;
            dataGridViewMapAnalysis.Size = new Size(777, 346);
            dataGridViewMapAnalysis.TabIndex = 0;
            // 
            // tabPagePredictions
            // 
            tabPagePredictions.Controls.Add(predictionControl1);
            tabPagePredictions.Location = new Point(4, 24);
            tabPagePredictions.Name = "tabPagePredictions";
            tabPagePredictions.Size = new Size(1408, 734);
            tabPagePredictions.TabIndex = 4;
            tabPagePredictions.Text = "Prediction";
            tabPagePredictions.UseVisualStyleBackColor = true;
            // 
            // tabPageReducerPred
            // 
            tabPageReducerPred.Controls.Add(labelRefPressure);
            tabPageReducerPred.Controls.Add(textBoxReferencePressure);
            tabPageReducerPred.Controls.Add(labelCurrent);
            tabPageReducerPred.Controls.Add(textBoxReducerTempValues);
            tabPageReducerPred.Controls.Add(buttonReducerPrediction);
            tabPageReducerPred.Location = new Point(4, 24);
            tabPageReducerPred.Name = "tabPageReducerPred";
            tabPageReducerPred.Size = new Size(1408, 734);
            tabPageReducerPred.TabIndex = 5;
            tabPageReducerPred.Text = "Reducer prediction";
            tabPageReducerPred.UseVisualStyleBackColor = true;
            // 
            // labelRefPressure
            // 
            labelRefPressure.AutoSize = true;
            labelRefPressure.Location = new Point(234, 101);
            labelRefPressure.Name = "labelRefPressure";
            labelRefPressure.Size = new Size(106, 15);
            labelRefPressure.TabIndex = 25;
            labelRefPressure.Text = "Reference Pressure";
            // 
            // textBoxReferencePressure
            // 
            textBoxReferencePressure.Location = new Point(372, 98);
            textBoxReferencePressure.Name = "textBoxReferencePressure";
            textBoxReferencePressure.RightToLeft = RightToLeft.Yes;
            textBoxReferencePressure.Size = new Size(62, 23);
            textBoxReferencePressure.TabIndex = 24;
            textBoxReferencePressure.Text = "1.5";
            // 
            // labelCurrent
            // 
            labelCurrent.AutoSize = true;
            labelCurrent.Location = new Point(231, 22);
            labelCurrent.Name = "labelCurrent";
            labelCurrent.Size = new Size(97, 15);
            labelCurrent.TabIndex = 23;
            labelCurrent.Text = "Current Values (,)";
            // 
            // textBoxReducerTempValues
            // 
            textBoxReducerTempValues.Location = new Point(227, 43);
            textBoxReducerTempValues.Name = "textBoxReducerTempValues";
            textBoxReducerTempValues.Size = new Size(207, 23);
            textBoxReducerTempValues.TabIndex = 22;
            textBoxReducerTempValues.Text = "-2,-2,-1,0,0,0,0,1,2";
            // 
            // buttonReducerPrediction
            // 
            buttonReducerPrediction.Location = new Point(453, 43);
            buttonReducerPrediction.Name = "buttonReducerPrediction";
            buttonReducerPrediction.Size = new Size(133, 23);
            buttonReducerPrediction.TabIndex = 21;
            buttonReducerPrediction.Text = "Reducer Prediction";
            buttonReducerPrediction.UseVisualStyleBackColor = true;
            buttonReducerPrediction.Click += buttonReducerPrediction_Click;
            // 
            // buttonAnalyzeFastTrim
            // 
            buttonAnalyzeFastTrim.Location = new Point(707, 42);
            buttonAnalyzeFastTrim.Name = "buttonAnalyzeFastTrim";
            buttonAnalyzeFastTrim.Size = new Size(58, 23);
            buttonAnalyzeFastTrim.TabIndex = 15;
            buttonAnalyzeFastTrim.Text = "Trim";
            buttonAnalyzeFastTrim.UseVisualStyleBackColor = true;
            buttonAnalyzeFastTrim.Click += ButtonShowTrims_Click;
            // 
            // buttonAnalyze
            // 
            buttonAnalyze.Location = new Point(596, 42);
            buttonAnalyze.Name = "buttonAnalyze";
            buttonAnalyze.Size = new Size(53, 23);
            buttonAnalyze.TabIndex = 14;
            buttonAnalyze.Text = "Ration";
            buttonAnalyze.UseVisualStyleBackColor = true;
            buttonAnalyze.Click += ButtonShowRatio_Click;
            // 
            // comboBoxReductorTempGroup2
            // 
            comboBoxReductorTempGroup2.FormattingEnabled = true;
            comboBoxReductorTempGroup2.Location = new Point(393, 40);
            comboBoxReductorTempGroup2.Name = "comboBoxReductorTempGroup2";
            comboBoxReductorTempGroup2.Size = new Size(121, 23);
            comboBoxReductorTempGroup2.TabIndex = 13;
            // 
            // comboBoxReductorTempGroup1
            // 
            comboBoxReductorTempGroup1.FormattingEnabled = true;
            comboBoxReductorTempGroup1.Location = new Point(134, 39);
            comboBoxReductorTempGroup1.Name = "comboBoxReductorTempGroup1";
            comboBoxReductorTempGroup1.Size = new Size(121, 23);
            comboBoxReductorTempGroup1.TabIndex = 12;
            // 
            // comboBoxGasTemperatureb2
            // 
            comboBoxGasTemperatureb2.FormattingEnabled = true;
            comboBoxGasTemperatureb2.Location = new Point(266, 39);
            comboBoxGasTemperatureb2.Name = "comboBoxGasTemperatureb2";
            comboBoxGasTemperatureb2.Size = new Size(121, 23);
            comboBoxGasTemperatureb2.TabIndex = 11;
            // 
            // comboBoxGasTemperatureb1
            // 
            comboBoxGasTemperatureb1.FormattingEnabled = true;
            comboBoxGasTemperatureb1.Location = new Point(7, 39);
            comboBoxGasTemperatureb1.Name = "comboBoxGasTemperatureb1";
            comboBoxGasTemperatureb1.Size = new Size(121, 23);
            comboBoxGasTemperatureb1.TabIndex = 10;
            // 
            // buttonAnalysisByMap
            // 
            buttonAnalysisByMap.Location = new Point(931, 52);
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
            // buttonReducerThermalLag
            // 
            buttonReducerThermalLag.Location = new Point(1014, 42);
            buttonReducerThermalLag.Name = "buttonReducerThermalLag";
            buttonReducerThermalLag.Size = new Size(130, 23);
            buttonReducerThermalLag.TabIndex = 18;
            buttonReducerThermalLag.Text = "Reducer Thermal Lag";
            buttonReducerThermalLag.UseVisualStyleBackColor = true;
            buttonReducerThermalLag.Click += buttonReducerThermalLag_Click;
            // 
            // buttonAFR
            // 
            buttonAFR.Location = new Point(655, 42);
            buttonAFR.Name = "buttonAFR";
            buttonAFR.Size = new Size(46, 23);
            buttonAFR.TabIndex = 20;
            buttonAFR.Text = "AFR";
            buttonAFR.UseVisualStyleBackColor = true;
            buttonAFR.Click += buttonAFR_Click;
            // 
            // buttonExtraInjectionCalculator
            // 
            buttonExtraInjectionCalculator.Location = new Point(1150, 56);
            buttonExtraInjectionCalculator.Name = "buttonExtraInjectionCalculator";
            buttonExtraInjectionCalculator.Size = new Size(152, 23);
            buttonExtraInjectionCalculator.TabIndex = 21;
            buttonExtraInjectionCalculator.Text = "Extra Injection Calculator";
            buttonExtraInjectionCalculator.UseVisualStyleBackColor = true;
            buttonExtraInjectionCalculator.Click += buttonExtraInjectionCalculator_Click;
            // 
            // buttonShowReducerPress
            // 
            buttonShowReducerPress.Location = new Point(771, 42);
            buttonShowReducerPress.Name = "buttonShowReducerPress";
            buttonShowReducerPress.Size = new Size(54, 23);
            buttonShowReducerPress.TabIndex = 22;
            buttonShowReducerPress.Text = "Press";
            buttonShowReducerPress.UseVisualStyleBackColor = true;
            buttonShowReducerPress.Click += buttonShowReducerPress_Click;
            // 
            // comboBoxAggregation
            // 
            comboBoxAggregation.FormattingEnabled = true;
            comboBoxAggregation.Location = new Point(519, 40);
            comboBoxAggregation.Name = "comboBoxAggregation";
            comboBoxAggregation.Size = new Size(71, 23);
            comboBoxAggregation.TabIndex = 23;
            // 
            // buttonGroupByTemp
            // 
            buttonGroupByTemp.Location = new Point(1105, 12);
            buttonGroupByTemp.Name = "buttonGroupByTemp";
            buttonGroupByTemp.Size = new Size(105, 23);
            buttonGroupByTemp.TabIndex = 24;
            buttonGroupByTemp.Text = "Goup By Temp";
            buttonGroupByTemp.UseVisualStyleBackColor = true;
            buttonGroupByTemp.Click += buttonGroupByTemp_Click;
            // 
            // predictionControl1
            // 
            predictionControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            predictionControl1.Location = new Point(0, 3);
            predictionControl1.Name = "predictionControl1";
            predictionControl1.Size = new Size(1408, 734);
            predictionControl1.TabIndex = 0;

            tableLayoutPanelAnalyses.Dock = DockStyle.Fill;
            tableLayoutPanelAnalyses.RowCount = 2;
            tableLayoutPanelAnalyses.ColumnCount = 2;
            tableLayoutPanelAnalyses.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanelAnalyses.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanelAnalyses.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanelAnalyses.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            tableLayoutPanelAnalyses.Controls.Add(dataGridViewAnalyzeDataBank1t1, 0, 0);
            tableLayoutPanelAnalyses.Controls.Add(dataGridViewAnalyzeDataBank1t2, 1, 0);
            tableLayoutPanelAnalyses.Controls.Add(dataGridViewAnalyzeDataBank2t1, 0, 1);
            tableLayoutPanelAnalyses.Controls.Add(dataGridViewAnalyzeDataBank2t2, 1, 1);

            dataGridViewAnalyzeDataBank1t1.Dock = DockStyle.Fill;
            dataGridViewAnalyzeDataBank1t2.Dock = DockStyle.Fill;
            dataGridViewAnalyzeDataBank2t1.Dock = DockStyle.Fill;
            dataGridViewAnalyzeDataBank2t2.Dock = DockStyle.Fill;

            tabPageAnalyses.Controls.Add(tableLayoutPanelAnalyses);
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1416, 868);
            Controls.Add(buttonGroupByTemp);
            Controls.Add(comboBoxAggregation);
            Controls.Add(buttonShowReducerPress);
            Controls.Add(buttonExtraInjectionCalculator);
            Controls.Add(buttonAFR);
            Controls.Add(buttonReducerThermalLag);
            Controls.Add(button1);
            Controls.Add(buttonAnalysisByMap);
            Controls.Add(buttonAnalyzeFastTrim);
            Controls.Add(txtFilePath);
            Controls.Add(buttonAnalyze);
            Controls.Add(tbBenzTimingFilterCuting);
            Controls.Add(label1);
            Controls.Add(tabControlMain);
            Controls.Add(comboBoxReductorTempGroup2);
            Controls.Add(buttonSelectFile);
            Controls.Add(comboBoxReductorTempGroup1);
            Controls.Add(statusBar);
            Controls.Add(comboBoxGasTemperatureb2);
            Controls.Add(comboBoxGasTemperatureb1);
            Name = "MainForm";
            Text = "LPG Analyser";
            WindowState = FormWindowState.Maximized;
            statusBar.ResumeLayout(false);
            statusBar.PerformLayout();
            tabControlMain.ResumeLayout(false);
            tabPageMainData.ResumeLayout(false);
            tabPageAnalyses.ResumeLayout(false);
            tabPageGroupByTemp.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridViewRIDData).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewGasData).EndInit();
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
        private TableLayoutPanel tableLayoutPanelAnalyses;
        private OpenFileDialog openFileDialog1;
        private StatusStrip statusBar;
        private TextBox txtFilePath;
        private Button buttonSelectFile;
        private TextBox tbBenzTimingFilterCuting;
        private Label label1;
        private ToolStripStatusLabel toolStripSummary;
        private TabControl tabControlMain;
        private TabPage tabPageMainData;
        private TabPage tabPageAnalyses;
        private ComboBox comboBoxGasTemperatureb2;
        private ComboBox comboBoxGasTemperatureb1;
        private ComboBox comboBoxReductorTempGroup2;
        private ComboBox comboBoxReductorTempGroup1;
        private Button buttonAnalyze;
        private Button buttonAnalyzeFastTrim;
        private TabPage tabPageGroupByTemp;
        private DataGridView dataGridViewGasData;
        private Button buttonAnalysisByMap;
        private TabPage tabPageMapAnalysis;
        private DataGridView dataGridViewMapAnalysis;
        private DataGridView dataGridViewInjectionTimeAnalisys;
        private DataGridView dataGridView1;
        private Button button1;
        private Button buttonReducerThermalLag;
        private TabPage tabPagePredictions;
        private Button buttonAFR;
        private Button buttonReducerPrediction;
        private TabPage tabPageReducerPred;
        private Button buttonExtraInjectionCalculator;
        private DataGridView dataGridViewRIDData;
        private TextBox textBoxReducerTempValues;
        private Label labelCurrent;
        private TextBox textBoxReferencePressure;
        private Label labelRefPressure;
        private Button buttonShowReducerPress;
        private ComboBox comboBoxAggregation;
        private Controls.DataItemGrid dataGridViewMainData;
        private Button buttonGroupByTemp;
        private ReadOnlyDataGridView dataGridViewAnalyzeDataBank2t2;
        private ReadOnlyDataGridView dataGridViewAnalyzeDataBank1t2;
        private ReadOnlyDataGridView dataGridViewAnalyzeDataBank2t1;
        private ReadOnlyDataGridView dataGridViewAnalyzeDataBank1t1;
        private PredictionControl predictionControl1;
    }
}
