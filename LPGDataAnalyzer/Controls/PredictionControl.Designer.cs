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
            panel1 = new Panel();
            historyControl1 = new HistoryControl();
            textBoxMinCount = new TextBox();
            checkBoxPreFilter = new CheckBox();
            checkBoxRound = new CheckBox();
            checkBoxOnlyChanges = new CheckBox();
            cbInterpolation = new CheckBox();
            cbEnableSmooth = new CheckBox();
            textBoxLastPredictedFuelTable = new TextBox();
            panelLegend = new Panel();
            textBoxImagePath = new TextBox();
            textBoxParsedData = new TextBox();
            buttonValidate = new Button();
            buttonContinue = new Button();
            buttonParceSelectedPhoto = new Button();
            dataGridViewOrig = new ReadOnlyDataGridView();
            dataGridViewPrediction = new ReadOnlyDataGridView();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // checkBoxSaveSnapshot
            // 
            checkBoxSaveSnapshot.AutoSize = true;
            checkBoxSaveSnapshot.Location = new Point(895, 388);
            checkBoxSaveSnapshot.Name = "checkBoxSaveSnapshot";
            checkBoxSaveSnapshot.Size = new Size(102, 19);
            checkBoxSaveSnapshot.TabIndex = 0;
            checkBoxSaveSnapshot.Text = "Save Snapshot";
            // 
            // panel1
            // 
            panel1.Controls.Add(historyControl1);
            panel1.Location = new Point(888, 434);
            panel1.Name = "panel1";
            panel1.Size = new Size(512, 282);
            panel1.TabIndex = 1;
            // 
            // historyControl1
            // 
            historyControl1.Dock = DockStyle.Fill;
            historyControl1.Location = new Point(0, 0);
            historyControl1.Name = "historyControl1";
            historyControl1.Size = new Size(512, 282);
            historyControl1.TabIndex = 0;
            // 
            // textBoxMinCount
            // 
            textBoxMinCount.Location = new Point(1239, 383);
            textBoxMinCount.Name = "textBoxMinCount";
            textBoxMinCount.RightToLeft = RightToLeft.Yes;
            textBoxMinCount.Size = new Size(45, 23);
            textBoxMinCount.TabIndex = 2;
            textBoxMinCount.Text = "3";
            // 
            // checkBoxPreFilter
            // 
            checkBoxPreFilter.AutoSize = true;
            checkBoxPreFilter.Checked = true;
            checkBoxPreFilter.CheckState = CheckState.Checked;
            checkBoxPreFilter.Location = new Point(1164, 383);
            checkBoxPreFilter.Name = "checkBoxPreFilter";
            checkBoxPreFilter.Size = new Size(72, 19);
            checkBoxPreFilter.TabIndex = 3;
            checkBoxPreFilter.Text = "Pre Filter";
            // 
            // checkBoxRound
            // 
            checkBoxRound.AutoSize = true;
            checkBoxRound.Checked = true;
            checkBoxRound.CheckState = CheckState.Checked;
            checkBoxRound.Location = new Point(1167, 354);
            checkBoxRound.Name = "checkBoxRound";
            checkBoxRound.Size = new Size(61, 19);
            checkBoxRound.TabIndex = 4;
            checkBoxRound.Text = "Round";
            // 
            // checkBoxOnlyChanges
            // 
            checkBoxOnlyChanges.AutoSize = true;
            checkBoxOnlyChanges.Location = new Point(1335, 323);
            checkBoxOnlyChanges.Name = "checkBoxOnlyChanges";
            checkBoxOnlyChanges.Size = new Size(98, 19);
            checkBoxOnlyChanges.TabIndex = 5;
            checkBoxOnlyChanges.Text = "Only changes";
            // 
            // cbInterpolation
            // 
            cbInterpolation.AutoSize = true;
            cbInterpolation.Location = new Point(1239, 324);
            cbInterpolation.Name = "cbInterpolation";
            cbInterpolation.Size = new Size(94, 19);
            cbInterpolation.TabIndex = 6;
            cbInterpolation.Text = "Interpolation";
            // 
            // cbEnableSmooth
            // 
            cbEnableSmooth.AutoSize = true;
            cbEnableSmooth.Location = new Point(1169, 322);
            cbEnableSmooth.Name = "cbEnableSmooth";
            cbEnableSmooth.Size = new Size(68, 19);
            cbEnableSmooth.TabIndex = 7;
            cbEnableSmooth.Text = "Smooth";
            // 
            // textBoxLastPredictedFuelTable
            // 
            textBoxLastPredictedFuelTable.Location = new Point(1153, 29);
            textBoxLastPredictedFuelTable.Multiline = true;
            textBoxLastPredictedFuelTable.Name = "textBoxLastPredictedFuelTable";
            textBoxLastPredictedFuelTable.Size = new Size(252, 288);
            textBoxLastPredictedFuelTable.TabIndex = 8;
            // 
            // panelLegend
            // 
            panelLegend.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            panelLegend.Location = new Point(3, 350);
            panelLegend.Name = "panelLegend";
            panelLegend.Size = new Size(818, 45);
            panelLegend.TabIndex = 9;
            // 
            // textBoxImagePath
            // 
            textBoxImagePath.Location = new Point(883, 3);
            textBoxImagePath.Name = "textBoxImagePath";
            textBoxImagePath.ReadOnly = true;
            textBoxImagePath.Size = new Size(517, 23);
            textBoxImagePath.TabIndex = 10;
            // 
            // textBoxParsedData
            // 
            textBoxParsedData.Location = new Point(883, 29);
            textBoxParsedData.Multiline = true;
            textBoxParsedData.Name = "textBoxParsedData";
            textBoxParsedData.Size = new Size(271, 288);
            textBoxParsedData.TabIndex = 11;
            // 
            // buttonValidate
            // 
            buttonValidate.Location = new Point(998, 319);
            buttonValidate.Name = "buttonValidate";
            buttonValidate.Size = new Size(75, 23);
            buttonValidate.TabIndex = 12;
            buttonValidate.Text = "Validate";
            // 
            // buttonContinue
            // 
            buttonContinue.Location = new Point(1079, 319);
            buttonContinue.Name = "buttonContinue";
            buttonContinue.Size = new Size(75, 23);
            buttonContinue.TabIndex = 13;
            buttonContinue.Text = "Predict";
            buttonContinue.Click += ButtonPredict_Click;
            // 
            // buttonParceSelectedPhoto
            // 
            buttonParceSelectedPhoto.Location = new Point(883, 317);
            buttonParceSelectedPhoto.Name = "buttonParceSelectedPhoto";
            buttonParceSelectedPhoto.Size = new Size(75, 23);
            buttonParceSelectedPhoto.TabIndex = 14;
            buttonParceSelectedPhoto.Text = "Parse";
            buttonParceSelectedPhoto.Click += ButtonParceSelectedImage_Click;
            // 
            // dataGridViewOrig
            // 
            dataGridViewOrig.EnableTitle = false;
            dataGridViewOrig.Location = new Point(3, 3);
            dataGridViewOrig.Name = "dataGridViewOrig";
            dataGridViewOrig.Size = new Size(818, 341);
            dataGridViewOrig.TabIndex = 17;
            dataGridViewOrig.Title = "My Read-Only Data Grid";
            // 
            // dataGridViewPrediction
            // 
            dataGridViewPrediction.EnableTitle = false;
            dataGridViewPrediction.Location = new Point(3, 401);
            dataGridViewPrediction.Name = "dataGridViewPrediction";
            dataGridViewPrediction.Size = new Size(818, 315);
            dataGridViewPrediction.TabIndex = 18;
            dataGridViewPrediction.Title = "My Read-Only Data Grid";
            // 
            // PredictionControl
            // 
            Controls.Add(dataGridViewPrediction);
            Controls.Add(dataGridViewOrig);
            Controls.Add(checkBoxSaveSnapshot);
            Controls.Add(panel1);
            Controls.Add(textBoxMinCount);
            Controls.Add(checkBoxPreFilter);
            Controls.Add(checkBoxRound);
            Controls.Add(checkBoxOnlyChanges);
            Controls.Add(cbInterpolation);
            Controls.Add(cbEnableSmooth);
            Controls.Add(textBoxLastPredictedFuelTable);
            Controls.Add(panelLegend);
            Controls.Add(textBoxImagePath);
            Controls.Add(textBoxParsedData);
            Controls.Add(buttonValidate);
            Controls.Add(buttonContinue);
            Controls.Add(buttonParceSelectedPhoto);
            Name = "PredictionControl";
            Size = new Size(1408, 734);
            panel1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        private CheckBox checkBoxSaveSnapshot;
        private Panel panel1;
        private HistoryControl historyControl1;
        private TextBox textBoxMinCount;
        private CheckBox checkBoxPreFilter;
        private CheckBox checkBoxRound;
        private CheckBox checkBoxOnlyChanges;
        private CheckBox cbInterpolation;
        private CheckBox cbEnableSmooth;
        private TextBox textBoxLastPredictedFuelTable;
        private Panel panelLegend;
        private TextBox textBoxImagePath;
        private TextBox textBoxParsedData;
        private Button buttonValidate;
        private Button buttonContinue;
        private Button buttonParceSelectedPhoto;
        private ReadOnlyDataGridView dataGridViewOrig;
        private ReadOnlyDataGridView dataGridViewPrediction;
    }
}