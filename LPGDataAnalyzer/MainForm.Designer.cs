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
            openFileDialog1 = new OpenFileDialog();
            statusBar = new StatusStrip();
            toolStripSummary = new ToolStripStatusLabel();
            label1 = new Label();
            tbBenzTimingFilterCuting = new TextBox();
            dataGridViewMainData = new DataGridView();
            txtFilePath = new TextBox();
            buttonSelectFile = new Button();
            dataGridViewAnalyzeDataBank1t2 = new DataGridView();
            dataGridViewAnalyzeDataBank2t2 = new DataGridView();
            dataGridViewAnalyzeDataBank2t1 = new DataGridView();
            dataGridViewAnalyzeDataBank1t1 = new DataGridView();
            tabControlMain = new TabControl();
            tabPageMainData = new TabPage();
            tabPageAnalyses = new TabPage();
            tabPageGroupByTemp = new TabPage();
            dataGridViewRIDData = new DataGridView();
            dataGridViewGasData = new DataGridView();
            tabPageMapAnalysis = new TabPage();
            dataGridView1 = new DataGridView();
            dataGridViewInjectionTimeAnalisys = new DataGridView();
            dataGridViewMapAnalysis = new DataGridView();
            tabPagePredictions = new TabPage();
            textBoxLastPredictedFuelTable = new TextBox();
            panelLegend = new Panel();
            dataGridViewDiagnostics = new DataGridView();
            textBoxImagePath = new TextBox();
            textBoxParsedData = new TextBox();
            buttonValidate = new Button();
            buttonContinue = new Button();
            buttonParceSelectedPhoto = new Button();
            dataGridViewPrediction = new DataGridView();
            dataGridViewOrig = new DataGridView();
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
            comboBoxTemperature2 = new ComboBox();
            comboBoxTemperature1 = new ComboBox();
            buttonAnalysisByMap = new Button();
            button1 = new Button();
            button2 = new Button();
            buttonAFR = new Button();
            buttonExtraInjectionCalculator = new Button();
            statusBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewMainData).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewAnalyzeDataBank1t2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewAnalyzeDataBank2t2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewAnalyzeDataBank2t1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewAnalyzeDataBank1t1).BeginInit();
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
            ((System.ComponentModel.ISupportInitialize)dataGridViewDiagnostics).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewPrediction).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewOrig).BeginInit();
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
            label1.Location = new Point(717, 46);
            label1.Name = "label1";
            label1.Size = new Size(133, 15);
            label1.TabIndex = 9;
            label1.Text = "Skip time group analyse";
            // 
            // tbBenzTimingFilterCuting
            // 
            tbBenzTimingFilterCuting.AccessibleDescription = "Benz Timing Cutting Filter ";
            tbBenzTimingFilterCuting.Location = new Point(847, 44);
            tbBenzTimingFilterCuting.Name = "tbBenzTimingFilterCuting";
            tbBenzTimingFilterCuting.Size = new Size(34, 23);
            tbBenzTimingFilterCuting.TabIndex = 7;
            tbBenzTimingFilterCuting.Text = "2.4";
            tbBenzTimingFilterCuting.TextAlign = HorizontalAlignment.Right;
            // 
            // dataGridViewMainData
            // 
            dataGridViewMainData.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridViewMainData.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewMainData.Location = new Point(3, 3);
            dataGridViewMainData.Name = "dataGridViewMainData";
            dataGridViewMainData.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridViewMainData.Size = new Size(1402, 725);
            dataGridViewMainData.TabIndex = 0;
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
            // dataGridViewAnalyzeDataBank1t2
            // 
            dataGridViewAnalyzeDataBank1t2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            dataGridViewAnalyzeDataBank1t2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewAnalyzeDataBank1t2.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewAnalyzeDataBank1t2.Location = new Point(658, 3);
            dataGridViewAnalyzeDataBank1t2.Name = "dataGridViewAnalyzeDataBank1t2";
            dataGridViewAnalyzeDataBank1t2.ReadOnly = true;
            dataGridViewAnalyzeDataBank1t2.RowHeadersVisible = false;
            dataGridViewAnalyzeDataBank1t2.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridViewAnalyzeDataBank1t2.Size = new Size(750, 350);
            dataGridViewAnalyzeDataBank1t2.TabIndex = 3;
            // 
            // dataGridViewAnalyzeDataBank2t2
            // 
            dataGridViewAnalyzeDataBank2t2.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            dataGridViewAnalyzeDataBank2t2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewAnalyzeDataBank2t2.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewAnalyzeDataBank2t2.Location = new Point(652, 378);
            dataGridViewAnalyzeDataBank2t2.Name = "dataGridViewAnalyzeDataBank2t2";
            dataGridViewAnalyzeDataBank2t2.ReadOnly = true;
            dataGridViewAnalyzeDataBank2t2.RowHeadersVisible = false;
            dataGridViewAnalyzeDataBank2t2.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridViewAnalyzeDataBank2t2.Size = new Size(750, 350);
            dataGridViewAnalyzeDataBank2t2.TabIndex = 2;
            // 
            // dataGridViewAnalyzeDataBank2t1
            // 
            dataGridViewAnalyzeDataBank2t1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            dataGridViewAnalyzeDataBank2t1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewAnalyzeDataBank2t1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewAnalyzeDataBank2t1.Location = new Point(-4, 378);
            dataGridViewAnalyzeDataBank2t1.Name = "dataGridViewAnalyzeDataBank2t1";
            dataGridViewAnalyzeDataBank2t1.ReadOnly = true;
            dataGridViewAnalyzeDataBank2t1.RowHeadersVisible = false;
            dataGridViewAnalyzeDataBank2t1.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridViewAnalyzeDataBank2t1.Size = new Size(750, 350);
            dataGridViewAnalyzeDataBank2t1.TabIndex = 1;
            // 
            // dataGridViewAnalyzeDataBank1t1
            // 
            dataGridViewAnalyzeDataBank1t1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewAnalyzeDataBank1t1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewAnalyzeDataBank1t1.Location = new Point(0, 6);
            dataGridViewAnalyzeDataBank1t1.Name = "dataGridViewAnalyzeDataBank1t1";
            dataGridViewAnalyzeDataBank1t1.ReadOnly = true;
            dataGridViewAnalyzeDataBank1t1.RowHeadersVisible = false;
            dataGridViewAnalyzeDataBank1t1.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridViewAnalyzeDataBank1t1.Size = new Size(751, 350);
            dataGridViewAnalyzeDataBank1t1.TabIndex = 0;
            dataGridViewAnalyzeDataBank1t1.CellClick += dataGridViewAnalyzeDataBank1t1_CellClick;
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
            // tabPageAnalyses
            // 
            tabPageAnalyses.Controls.Add(dataGridViewAnalyzeDataBank2t2);
            tabPageAnalyses.Controls.Add(dataGridViewAnalyzeDataBank1t2);
            tabPageAnalyses.Controls.Add(dataGridViewAnalyzeDataBank1t1);
            tabPageAnalyses.Controls.Add(dataGridViewAnalyzeDataBank2t1);
            tabPageAnalyses.Location = new Point(4, 24);
            tabPageAnalyses.Name = "tabPageAnalyses";
            tabPageAnalyses.Padding = new Padding(3);
            tabPageAnalyses.Size = new Size(1408, 734);
            tabPageAnalyses.TabIndex = 1;
            tabPageAnalyses.Text = "Analyses";
            tabPageAnalyses.UseVisualStyleBackColor = true;
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
            dataGridView1.Location = new Point(975, 3);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.Size = new Size(430, 728);
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
            tabPagePredictions.Controls.Add(textBoxLastPredictedFuelTable);
            tabPagePredictions.Controls.Add(panelLegend);
            tabPagePredictions.Controls.Add(dataGridViewDiagnostics);
            tabPagePredictions.Controls.Add(textBoxImagePath);
            tabPagePredictions.Controls.Add(textBoxParsedData);
            tabPagePredictions.Controls.Add(buttonValidate);
            tabPagePredictions.Controls.Add(buttonContinue);
            tabPagePredictions.Controls.Add(buttonParceSelectedPhoto);
            tabPagePredictions.Controls.Add(dataGridViewPrediction);
            tabPagePredictions.Controls.Add(dataGridViewOrig);
            tabPagePredictions.Location = new Point(4, 24);
            tabPagePredictions.Name = "tabPagePredictions";
            tabPagePredictions.Size = new Size(1408, 734);
            tabPagePredictions.TabIndex = 4;
            tabPagePredictions.Text = "Prediction";
            tabPagePredictions.UseVisualStyleBackColor = true;
            // 
            // textBoxLastPredictedFuelTable
            // 
            textBoxLastPredictedFuelTable.Location = new Point(1153, 29);
            textBoxLastPredictedFuelTable.Multiline = true;
            textBoxLastPredictedFuelTable.Name = "textBoxLastPredictedFuelTable";
            textBoxLastPredictedFuelTable.Size = new Size(252, 288);
            textBoxLastPredictedFuelTable.TabIndex = 25;
            // 
            // panelLegend
            // 
            panelLegend.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            panelLegend.Location = new Point(3, 350);
            panelLegend.Name = "panelLegend";
            panelLegend.Size = new Size(818, 45);
            panelLegend.TabIndex = 24;
            panelLegend.Paint += LegendPanel_Paint;
            // 
            // dataGridViewDiagnostics
            // 
            dataGridViewDiagnostics.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridViewDiagnostics.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewDiagnostics.Location = new Point(883, 359);
            dataGridViewDiagnostics.Name = "dataGridViewDiagnostics";
            dataGridViewDiagnostics.ReadOnly = true;
            dataGridViewDiagnostics.Size = new Size(522, 375);
            dataGridViewDiagnostics.TabIndex = 23;
            // 
            // textBoxImagePath
            // 
            textBoxImagePath.Location = new Point(883, 3);
            textBoxImagePath.Name = "textBoxImagePath";
            textBoxImagePath.ReadOnly = true;
            textBoxImagePath.Size = new Size(517, 23);
            textBoxImagePath.TabIndex = 22;
            // 
            // textBoxParsedData
            // 
            textBoxParsedData.Location = new Point(883, 29);
            textBoxParsedData.Multiline = true;
            textBoxParsedData.Name = "textBoxParsedData";
            textBoxParsedData.Size = new Size(271, 288);
            textBoxParsedData.TabIndex = 21;
            // 
            // buttonValidate
            // 
            buttonValidate.Location = new Point(998, 319);
            buttonValidate.Name = "buttonValidate";
            buttonValidate.Size = new Size(75, 23);
            buttonValidate.TabIndex = 20;
            buttonValidate.Text = "Validate";
            buttonValidate.UseVisualStyleBackColor = true;
            buttonValidate.Click += ButtonValidate_Click;
            // 
            // buttonContinue
            // 
            buttonContinue.Location = new Point(1079, 319);
            buttonContinue.Name = "buttonContinue";
            buttonContinue.Size = new Size(75, 23);
            buttonContinue.TabIndex = 3;
            buttonContinue.Text = "Predict";
            buttonContinue.UseVisualStyleBackColor = true;
            buttonContinue.Click += ButtonPredict_Click;
            // 
            // buttonParceSelectedPhoto
            // 
            buttonParceSelectedPhoto.Location = new Point(883, 317);
            buttonParceSelectedPhoto.Name = "buttonParceSelectedPhoto";
            buttonParceSelectedPhoto.Size = new Size(75, 23);
            buttonParceSelectedPhoto.TabIndex = 19;
            buttonParceSelectedPhoto.Text = "Parce";
            buttonParceSelectedPhoto.UseVisualStyleBackColor = true;
            buttonParceSelectedPhoto.Click += buttonParceSelectedImage_Click;
            // 
            // dataGridViewPrediction
            // 
            dataGridViewPrediction.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            dataGridViewPrediction.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewPrediction.Location = new Point(-4, 399);
            dataGridViewPrediction.Name = "dataGridViewPrediction";
            dataGridViewPrediction.ReadOnly = true;
            dataGridViewPrediction.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            dataGridViewPrediction.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridViewPrediction.Size = new Size(825, 332);
            dataGridViewPrediction.TabIndex = 1;
            // 
            // dataGridViewOrig
            // 
            dataGridViewOrig.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewOrig.Location = new Point(3, 3);
            dataGridViewOrig.Name = "dataGridViewOrig";
            dataGridViewOrig.ReadOnly = true;
            dataGridViewOrig.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            dataGridViewOrig.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridViewOrig.Size = new Size(818, 339);
            dataGridViewOrig.TabIndex = 0;
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
            buttonAnalyzeFastTrim.Location = new Point(631, 40);
            buttonAnalyzeFastTrim.Name = "buttonAnalyzeFastTrim";
            buttonAnalyzeFastTrim.Size = new Size(75, 23);
            buttonAnalyzeFastTrim.TabIndex = 15;
            buttonAnalyzeFastTrim.Text = "Fast Trim";
            buttonAnalyzeFastTrim.UseVisualStyleBackColor = true;
            buttonAnalyzeFastTrim.Click += ButtonAnalyze2_Click;
            // 
            // buttonAnalyze
            // 
            buttonAnalyze.Location = new Point(520, 39);
            buttonAnalyze.Name = "buttonAnalyze";
            buttonAnalyze.Size = new Size(53, 23);
            buttonAnalyze.TabIndex = 14;
            buttonAnalyze.Text = "Ration";
            buttonAnalyze.UseVisualStyleBackColor = true;
            buttonAnalyze.Click += ButtonAnalyze_Click;
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
            // comboBoxTemperature2
            // 
            comboBoxTemperature2.FormattingEnabled = true;
            comboBoxTemperature2.Location = new Point(266, 39);
            comboBoxTemperature2.Name = "comboBoxTemperature2";
            comboBoxTemperature2.Size = new Size(121, 23);
            comboBoxTemperature2.TabIndex = 11;
            // 
            // comboBoxTemperature1
            // 
            comboBoxTemperature1.FormattingEnabled = true;
            comboBoxTemperature1.Location = new Point(7, 39);
            comboBoxTemperature1.Name = "comboBoxTemperature1";
            comboBoxTemperature1.Size = new Size(121, 23);
            comboBoxTemperature1.TabIndex = 10;
            // 
            // buttonAnalysisByMap
            // 
            buttonAnalysisByMap.Location = new Point(887, 46);
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
            // button2
            // 
            button2.Location = new Point(1014, 42);
            button2.Name = "button2";
            button2.Size = new Size(75, 23);
            button2.TabIndex = 18;
            button2.Text = "button2";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // buttonAFR
            // 
            buttonAFR.Location = new Point(579, 40);
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
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1416, 868);
            Controls.Add(buttonExtraInjectionCalculator);
            Controls.Add(buttonAFR);
            Controls.Add(button2);
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
            Controls.Add(comboBoxTemperature2);
            Controls.Add(comboBoxTemperature1);
            Name = "MainForm";
            Text = "LPG Analyser";
            WindowState = FormWindowState.Maximized;
            statusBar.ResumeLayout(false);
            statusBar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewMainData).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewAnalyzeDataBank1t2).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewAnalyzeDataBank2t2).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewAnalyzeDataBank2t1).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewAnalyzeDataBank1t1).EndInit();
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
            tabPagePredictions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewDiagnostics).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewPrediction).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewOrig).EndInit();
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
        private DataGridView dataGridViewMainData;
        private DataGridView dataGridViewAnalyzeDataBank1t1;
        private DataGridView dataGridViewAnalyzeDataBank2t1;
        private DataGridView dataGridViewAnalyzeDataBank1t2;
        private DataGridView dataGridViewAnalyzeDataBank2t2;
        private TextBox tbBenzTimingFilterCuting;
        private Label label1;
        private ToolStripStatusLabel toolStripSummary;
        private TabControl tabControlMain;
        private TabPage tabPageMainData;
        private TabPage tabPageAnalyses;
        private ComboBox comboBoxTemperature2;
        private ComboBox comboBoxTemperature1;
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
        private Button button2;
        private TabPage tabPagePredictions;
        private DataGridView dataGridViewPrediction;
        private DataGridView dataGridViewOrig;
        private Button buttonParceSelectedPhoto;
        private Button buttonAFR;
        private Button buttonContinue;
        private Button buttonValidate;
        private TextBox textBoxParsedData;
        private TextBox textBoxImagePath;
        private DataGridView dataGridViewDiagnostics;
        private Panel panelLegend;
        private TextBox textBoxLastPredictedFuelTable;
        private Button buttonReducerPrediction;
        private TabPage tabPageReducerPred;
        private Button buttonExtraInjectionCalculator;
        private DataGridView dataGridViewRIDData;
        private TextBox textBoxReducerTempValues;
        private Label labelCurrent;
        private TextBox textBoxReferencePressure;
        private Label labelRefPressure;
    }
}
