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
            dataGridViewLPGData = new DataGridView();
            txtFilePath = new TextBox();
            buttonSelectFile = new Button();
            dataGridViewAnalyzeDataBank1t2 = new DataGridView();
            dataGridViewAnalyzeDataBank2t2 = new DataGridView();
            dataGridViewAnalyzeDataBank2t1 = new DataGridView();
            dataGridViewAnalyzeDataBank1t1 = new DataGridView();
            tabControlMain = new TabControl();
            tabPageMainData = new TabPage();
            tabPageAnalyses = new TabPage();
            tabPageGroupByTenp = new TabPage();
            dataGridViewGroupByTemp = new DataGridView();
            buttonAnalyzeFastTrim = new Button();
            buttonAnalyze = new Button();
            comboBoxReductorTempGroup2 = new ComboBox();
            comboBoxReductorTempGroup1 = new ComboBox();
            comboBoxTemperature2 = new ComboBox();
            comboBoxTemperature1 = new ComboBox();
            statusBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewLPGData).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewAnalyzeDataBank1t2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewAnalyzeDataBank2t2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewAnalyzeDataBank2t1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewAnalyzeDataBank1t1).BeginInit();
            tabControlMain.SuspendLayout();
            tabPageMainData.SuspendLayout();
            tabPageAnalyses.SuspendLayout();
            tabPageGroupByTenp.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewGroupByTemp).BeginInit();
            SuspendLayout();
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // statusBar
            // 
            statusBar.Items.AddRange(new ToolStripItem[] { toolStripSummary });
            statusBar.Location = new Point(0, 724);
            statusBar.Name = "statusBar";
            statusBar.Size = new Size(1232, 22);
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
            label1.Location = new Point(725, 43);
            label1.Name = "label1";
            label1.Size = new Size(133, 15);
            label1.TabIndex = 9;
            label1.Text = "Skip time group analyse";
            // 
            // tbBenzTimingFilterCuting
            // 
            tbBenzTimingFilterCuting.AccessibleDescription = "Benz Timing Cutting Filter ";
            tbBenzTimingFilterCuting.Location = new Point(864, 43);
            tbBenzTimingFilterCuting.Name = "tbBenzTimingFilterCuting";
            tbBenzTimingFilterCuting.Size = new Size(34, 23);
            tbBenzTimingFilterCuting.TabIndex = 7;
            tbBenzTimingFilterCuting.Text = "2.4";
            tbBenzTimingFilterCuting.TextAlign = HorizontalAlignment.Right;
            // 
            // dataGridViewLPGData
            // 
            dataGridViewLPGData.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridViewLPGData.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewLPGData.Location = new Point(3, 3);
            dataGridViewLPGData.Name = "dataGridViewLPGData";
            dataGridViewLPGData.Size = new Size(1218, 606);
            dataGridViewLPGData.TabIndex = 0;
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
            buttonSelectFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonSelectFile.Location = new Point(1117, 10);
            buttonSelectFile.Name = "buttonSelectFile";
            buttonSelectFile.Size = new Size(108, 23);
            buttonSelectFile.TabIndex = 1;
            buttonSelectFile.Text = "Select Txt File";
            buttonSelectFile.UseVisualStyleBackColor = true;
            buttonSelectFile.Click += btnSelectFile_Click;
            // 
            // dataGridViewAnalyzeDataBank1t2
            // 
            dataGridViewAnalyzeDataBank1t2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            dataGridViewAnalyzeDataBank1t2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewAnalyzeDataBank1t2.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewAnalyzeDataBank1t2.Location = new Point(478, 6);
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
            dataGridViewAnalyzeDataBank2t2.Location = new Point(471, 266);
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
            dataGridViewAnalyzeDataBank2t1.Location = new Point(-4, 262);
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
            tabControlMain.Controls.Add(tabPageGroupByTenp);
            tabControlMain.Location = new Point(0, 81);
            tabControlMain.Name = "tabControlMain";
            tabControlMain.SelectedIndex = 0;
            tabControlMain.Size = new Size(1232, 640);
            tabControlMain.TabIndex = 5;
            // 
            // tabPageMainData
            // 
            tabPageMainData.Controls.Add(dataGridViewLPGData);
            tabPageMainData.Location = new Point(4, 24);
            tabPageMainData.Name = "tabPageMainData";
            tabPageMainData.Padding = new Padding(3);
            tabPageMainData.Size = new Size(1224, 612);
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
            tabPageAnalyses.Size = new Size(1224, 612);
            tabPageAnalyses.TabIndex = 1;
            tabPageAnalyses.Text = "Analyses";
            tabPageAnalyses.UseVisualStyleBackColor = true;
            // 
            // tabPageGroupByTenp
            // 
            tabPageGroupByTenp.Controls.Add(dataGridViewGroupByTemp);
            tabPageGroupByTenp.Location = new Point(4, 24);
            tabPageGroupByTenp.Name = "tabPageGroupByTenp";
            tabPageGroupByTenp.Size = new Size(1224, 612);
            tabPageGroupByTenp.TabIndex = 2;
            tabPageGroupByTenp.Text = "Group By Temp";
            tabPageGroupByTenp.UseVisualStyleBackColor = true;
            // 
            // dataGridViewGroupByTemp
            // 
            dataGridViewGroupByTemp.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridViewGroupByTemp.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewGroupByTemp.Location = new Point(3, 3);
            dataGridViewGroupByTemp.Name = "dataGridViewGroupByTemp";
            dataGridViewGroupByTemp.Size = new Size(1218, 606);
            dataGridViewGroupByTemp.TabIndex = 0;
            // 
            // buttonAnalyzeFastTrim
            // 
            buttonAnalyzeFastTrim.Location = new Point(644, 39);
            buttonAnalyzeFastTrim.Name = "buttonAnalyzeFastTrim";
            buttonAnalyzeFastTrim.Size = new Size(75, 23);
            buttonAnalyzeFastTrim.TabIndex = 15;
            buttonAnalyzeFastTrim.Text = "Fast Trim";
            buttonAnalyzeFastTrim.UseVisualStyleBackColor = true;
            buttonAnalyzeFastTrim.Click += buttonAnalyze2_Click;
            // 
            // buttonAnalyze
            // 
            buttonAnalyze.Location = new Point(520, 39);
            buttonAnalyze.Name = "buttonAnalyze";
            buttonAnalyze.Size = new Size(118, 23);
            buttonAnalyze.TabIndex = 14;
            buttonAnalyze.Text = "Calculate Ration";
            buttonAnalyze.UseVisualStyleBackColor = true;
            buttonAnalyze.Click += buttonAnalyze_Click;
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
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1232, 746);
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
            ((System.ComponentModel.ISupportInitialize)dataGridViewLPGData).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewAnalyzeDataBank1t2).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewAnalyzeDataBank2t2).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewAnalyzeDataBank2t1).EndInit();
            ((System.ComponentModel.ISupportInitialize)dataGridViewAnalyzeDataBank1t1).EndInit();
            tabControlMain.ResumeLayout(false);
            tabPageMainData.ResumeLayout(false);
            tabPageAnalyses.ResumeLayout(false);
            tabPageGroupByTenp.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridViewGroupByTemp).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private OpenFileDialog openFileDialog1;
        private StatusStrip statusBar;
        private TextBox txtFilePath;
        private Button buttonSelectFile;
        private DataGridView dataGridViewLPGData;
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
        private TabPage tabPageGroupByTenp;
        private DataGridView dataGridViewGroupByTemp;
    }
}
