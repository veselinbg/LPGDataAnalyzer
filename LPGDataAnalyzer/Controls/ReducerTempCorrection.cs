using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Services;
using System.ComponentModel;
using static LPGDataAnalyzer.Models.Settings;

namespace LPGDataAnalyzer.Controls
{
    internal class ReducerTempCorrection : UserControl
    {
        public ReducerTempCorrection()
        {
            InitializeComponent();
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataItem[] Data { get; set; }
        private void buttonReducerPrediction_Click(object sender, EventArgs e)
        {
            if (Data is null) return;

            Dictionary<string, int> currentCorrections = [];

            var values = textBoxReducerTempValues.Text.Split(',', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < ReductorTemperatureRanges.Length; i++)
            {
                currentCorrections.Add(ReductorTemperatureRanges[i].Label, int.Parse(values[i]));
            }

            var result = ReducerPrediction.PredictNewReducerTempCorrections(Data,
                currentCorrections, 
                double.Parse(textBoxReferencePressure.Text.Trim()), checkBoxEnableSmooth.Checked);

            MessageBox.Show(string.Join(",", result.Select(x => x.Value)), "LPG Reducer correction");
        }
        private void InitializeComponent()
        {
            labelRefPressure = new Label();
            textBoxReferencePressure = new TextBox();
            labelCurrent = new Label();
            textBoxReducerTempValues = new TextBox();
            buttonReducerPrediction = new Button();
            checkBoxEnableSmooth = new CheckBox();
            SuspendLayout();
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
            textBoxReducerTempValues.Text = "-4,-3,-1,0,0,0,0,1,2";
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
            // checkBoxEnableSmooth
            // 
            checkBoxEnableSmooth.AutoSize = true;
            checkBoxEnableSmooth.Location = new Point(243, 135);
            checkBoxEnableSmooth.Name = "checkBoxEnableSmooth";
            checkBoxEnableSmooth.Size = new Size(68, 19);
            checkBoxEnableSmooth.TabIndex = 26;
            checkBoxEnableSmooth.Text = "Smooth";
            checkBoxEnableSmooth.UseVisualStyleBackColor = true;
            // 
            // ReducerTempCorrection
            // 
            AutoSize = true;
            Controls.Add(checkBoxEnableSmooth);
            Controls.Add(labelRefPressure);
            Controls.Add(textBoxReferencePressure);
            Controls.Add(labelCurrent);
            Controls.Add(textBoxReducerTempValues);
            Controls.Add(buttonReducerPrediction);
            Name = "ReducerTempCorrection";
            Size = new Size(652, 355);
            ResumeLayout(false);
            PerformLayout();
        }
        private TextBox textBoxReducerTempValues;
        private Label labelCurrent;
        private TextBox textBoxReferencePressure;
        private Label labelRefPressure;
        private CheckBox checkBoxEnableSmooth;
        private Button buttonReducerPrediction;
    }
}
