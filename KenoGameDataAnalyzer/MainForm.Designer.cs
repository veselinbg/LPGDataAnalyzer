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
            mapAnalyzerUI = new MapAnalyzerUI();
            openFileDialog1 = new OpenFileDialog();
            statusBar = new StatusStrip();
            toolStripSummary = new ToolStripStatusLabel();
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
            tabPagePredictions = new TabPage();
            predictionControl1 = new PredictionControl();
            tabPageReducerPred = new TabPage();
            reducerTempCorrection1 = new ReducerTempCorrection();
            tabPageAllData = new TabPage();
            showAllFileDataui1 = new ShowAllFileDataUI();
            tabPageChart = new TabPage();
            dataItemLineChartControl1 = new DataItemLineChartControl();
            buttonExtraInjectionCalculator = new Button();
            statusBar.SuspendLayout();
            tabControlMain.SuspendLayout();
            tabPageMainData.SuspendLayout();
            tabPageAnalyses.SuspendLayout();
            tabPageGroupByTemp.SuspendLayout();
            tabPageMapAnalysis.SuspendLayout();
            tabPagePredictions.SuspendLayout();
            tabPageReducerPred.SuspendLayout();
            tabPageAllData.SuspendLayout();
            tabPageChart.SuspendLayout();
            SuspendLayout();
            // 
            // mapAnalyzerUI
            // 
            mapAnalyzerUI.Dock = DockStyle.Fill;
            mapAnalyzerUI.Location = new Point(0, 0);
            mapAnalyzerUI.Name = "mapAnalyzerUI";
            mapAnalyzerUI.Size = new Size(1408, 775);
            mapAnalyzerUI.TabIndex = 0;
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
            tabControlMain.Controls.Add(tabPageAllData);
            tabControlMain.Controls.Add(tabPageChart);
            tabControlMain.Location = new Point(0, 40);
            tabControlMain.Name = "tabControlMain";
            tabControlMain.SelectedIndex = 0;
            tabControlMain.Size = new Size(1416, 803);
            tabControlMain.TabIndex = 5;
            // 
            // tabPageMainData
            // 
            tabPageMainData.Controls.Add(dataGridViewMainData);
            tabPageMainData.Location = new Point(4, 24);
            tabPageMainData.Name = "tabPageMainData";
            tabPageMainData.Padding = new Padding(3);
            tabPageMainData.Size = new Size(1408, 775);
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
            dataGridViewMainData.Size = new Size(1412, 775);
            dataGridViewMainData.TabIndex = 0;
            dataGridViewMainData.Title = "All logged data";
            // 
            // tabPageAnalyses
            // 
            tabPageAnalyses.Controls.Add(analysisUC);
            tabPageAnalyses.Location = new Point(4, 24);
            tabPageAnalyses.Name = "tabPageAnalyses";
            tabPageAnalyses.Padding = new Padding(3);
            tabPageAnalyses.Size = new Size(1408, 775);
            tabPageAnalyses.TabIndex = 1;
            tabPageAnalyses.Text = "Analyses";
            tabPageAnalyses.UseVisualStyleBackColor = true;
            // 
            // analysisUC
            // 
            analysisUC.Dock = DockStyle.Fill;
            analysisUC.Location = new Point(3, 3);
            analysisUC.Name = "analysisUC";
            analysisUC.Size = new Size(1402, 769);
            analysisUC.TabIndex = 0;
            // 
            // tabPageGroupByTemp
            // 
            tabPageGroupByTemp.Controls.Add(temperatureAnalyzerui1);
            tabPageGroupByTemp.Location = new Point(4, 24);
            tabPageGroupByTemp.Name = "tabPageGroupByTemp";
            tabPageGroupByTemp.Size = new Size(1408, 775);
            tabPageGroupByTemp.TabIndex = 2;
            tabPageGroupByTemp.Text = "Temperature";
            tabPageGroupByTemp.UseVisualStyleBackColor = true;
            // 
            // temperatureAnalyzerui1
            // 
            temperatureAnalyzerui1.Dock = DockStyle.Fill;
            temperatureAnalyzerui1.Location = new Point(0, 0);
            temperatureAnalyzerui1.Name = "temperatureAnalyzerui1";
            temperatureAnalyzerui1.Size = new Size(1408, 775);
            temperatureAnalyzerui1.TabIndex = 0;
            // 
            // tabPageMapAnalysis
            // 
            tabPageMapAnalysis.Controls.Add(mapAnalyzerUI);
            tabPageMapAnalysis.Location = new Point(4, 24);
            tabPageMapAnalysis.Name = "tabPageMapAnalysis";
            tabPageMapAnalysis.Size = new Size(1408, 775);
            tabPageMapAnalysis.TabIndex = 3;
            tabPageMapAnalysis.Text = "Map Analysis";
            tabPageMapAnalysis.UseVisualStyleBackColor = true;
            // 
            // tabPagePredictions
            // 
            tabPagePredictions.Controls.Add(predictionControl1);
            tabPagePredictions.Location = new Point(4, 24);
            tabPagePredictions.Name = "tabPagePredictions";
            tabPagePredictions.Size = new Size(1408, 775);
            tabPagePredictions.TabIndex = 4;
            tabPagePredictions.Text = "Prediction";
            tabPagePredictions.UseVisualStyleBackColor = true;
            // 
            // predictionControl1
            // 
            predictionControl1.Dock = DockStyle.Fill;
            predictionControl1.Location = new Point(0, 0);
            predictionControl1.Name = "predictionControl1";
            predictionControl1.Size = new Size(1408, 775);
            predictionControl1.TabIndex = 0;
            // 
            // tabPageReducerPred
            // 
            tabPageReducerPred.Controls.Add(reducerTempCorrection1);
            tabPageReducerPred.Location = new Point(4, 24);
            tabPageReducerPred.Name = "tabPageReducerPred";
            tabPageReducerPred.Size = new Size(1408, 775);
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
            // tabPageAllData
            // 
            tabPageAllData.Controls.Add(showAllFileDataui1);
            tabPageAllData.Location = new Point(4, 24);
            tabPageAllData.Name = "tabPageAllData";
            tabPageAllData.Size = new Size(1408, 775);
            tabPageAllData.TabIndex = 6;
            tabPageAllData.Text = "All Data";
            tabPageAllData.UseVisualStyleBackColor = true;
            // 
            // showAllFileDataui1
            // 
            showAllFileDataui1.Dock = DockStyle.Fill;
            showAllFileDataui1.Location = new Point(0, 0);
            showAllFileDataui1.Name = "showAllFileDataui1";
            showAllFileDataui1.Size = new Size(1408, 775);
            showAllFileDataui1.TabIndex = 0;
            // 
            // tabPageChart
            // 
            tabPageChart.Controls.Add(dataItemLineChartControl1);
            tabPageChart.Location = new Point(4, 24);
            tabPageChart.Name = "tabPageChart";
            tabPageChart.Size = new Size(1408, 775);
            tabPageChart.TabIndex = 7;
            tabPageChart.Text = "Chart";
            tabPageChart.UseVisualStyleBackColor = true;
            // 
            // dataItemLineChartControl1
            // 
            dataItemLineChartControl1.Dock = DockStyle.Fill;
            dataItemLineChartControl1.Location = new Point(0, 0);
            dataItemLineChartControl1.Name = "dataItemLineChartControl1";
            dataItemLineChartControl1.Size = new Size(1408, 775);
            dataItemLineChartControl1.TabIndex = 0;
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
            Controls.Add(txtFilePath);
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
            tabPagePredictions.ResumeLayout(false);
            tabPageReducerPred.ResumeLayout(false);
            tabPageReducerPred.PerformLayout();
            tabPageAllData.ResumeLayout(false);
            tabPageChart.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private MapAnalyzerUI mapAnalyzerUI;
        private OpenFileDialog openFileDialog1;
        private StatusStrip statusBar;
        private TextBox txtFilePath;
        private Button buttonSelectFile;
        private ToolStripStatusLabel toolStripSummary;
        private TabControl tabControlMain;
        private TabPage tabPageMainData;
        private TabPage tabPageGroupByTemp;
        private TabPage tabPageMapAnalysis;
        private TabPage tabPagePredictions;
        private TabPage tabPageReducerPred;
        private Button buttonExtraInjectionCalculator;
        private Controls.DataItemGrid dataGridViewMainData;
        private PredictionControl predictionControl1;
        private ReducerTempCorrection reducerTempCorrection1;
        private TabPage tabPageAnalyses;
        private AnalysisUC analysisUC;
        private TemperatureAnalyzerUI temperatureAnalyzerui1;
        private TabPage tabPageAllData;
        private ShowAllFileDataUI showAllFileDataui1;
        private TabPage tabPageChart;
        private DataItemLineChartControl dataItemLineChartControl1;
    }
}
