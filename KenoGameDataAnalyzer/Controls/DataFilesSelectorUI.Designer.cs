namespace LPGDataAnalyzer.Controls
{
    partial class DataFilesSelectorUI
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private CheckedListBox checkedListBoxFiles;
        private Button buttonLoad;
        private Label labelTitle;
        private Panel panelBottom;
        private FileSystemWatcher? _watcher;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _watcher?.Dispose();
                components?.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support.
        /// </summary>
        private void InitializeComponent()
        {
            checkedListBoxFiles = new CheckedListBox();
            buttonLoad = new Button();
            labelTitle = new Label();
            panelBottom = new Panel();

            panelBottom.SuspendLayout();

            SuspendLayout();

            // 
            // labelTitle
            // 
            labelTitle.Dock = DockStyle.Top;
            labelTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            labelTitle.Height = 45;
            labelTitle.Text = "Select LPG Log Files";
            labelTitle.TextAlign = ContentAlignment.MiddleCenter;
            labelTitle.BackColor = Color.FromArgb(45, 45, 48);
            labelTitle.ForeColor = Color.White;

            // 
            // checkedListBoxFiles
            // 
            checkedListBoxFiles.CheckOnClick = true;
            checkedListBoxFiles.Dock = DockStyle.Fill;
            checkedListBoxFiles.Font = new Font("Segoe UI", 10F);
            checkedListBoxFiles.FormattingEnabled = true;
            checkedListBoxFiles.HorizontalScrollbar = true;
            checkedListBoxFiles.IntegralHeight = false;
            checkedListBoxFiles.BorderStyle = BorderStyle.FixedSingle;

            // 
            // panelBottom
            // 
            panelBottom.Dock = DockStyle.Bottom;
            panelBottom.Height = 60;
            panelBottom.Padding = new Padding(10);
            panelBottom.BackColor = Color.WhiteSmoke;

            // 
            // buttonLoad
            // 
            buttonLoad.Dock = DockStyle.Right;
            buttonLoad.Width = 180;
            buttonLoad.Text = "Load Selected Files";
            buttonLoad.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            buttonLoad.Cursor = Cursors.Hand;
            buttonLoad.UseVisualStyleBackColor = true;
            buttonLoad.Click += buttonLoad_Click;

            // 
            // panelBottom Controls
            // 
            panelBottom.Controls.Add(buttonLoad);

            // 
            // DataFilesSelectorUI
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;

            Controls.Add(checkedListBoxFiles);
            Controls.Add(panelBottom);
            Controls.Add(labelTitle);

            Name = "DataFilesSelectorUI";
            Size = new Size(900, 600);

            panelBottom.ResumeLayout(false);

            ResumeLayout(false);
        }

        #endregion
    }
}