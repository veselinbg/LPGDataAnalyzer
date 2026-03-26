using LPGDataAnalyzer.Models;
using System.ComponentModel;
using System.Data;
using static LPGDataAnalyzer.Models.Settings;

namespace LPGDataAnalyzer.Controls
{
    public class DataGridViewUC : DataGridView
    {
        public DataGridViewUC()
        {
            // DataGridView
            Dock = DockStyle.Fill;
            EnableHeadersVisualStyles = false;
            // Make it read-only
            ReadOnly = true;
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeRows = false;
            SelectionMode = DataGridViewSelectionMode.CellSelect;
            RowHeadersVisible = false;
            // Optional styling
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            ApplyDarkBoldStyle(RowHeadersDefaultCellStyle);
            ApplyDarkBoldStyle(ColumnHeadersDefaultCellStyle);
            DefaultCellStyle.SelectionBackColor = Color.Yellow;
            DefaultCellStyle.SelectionForeColor = Color.Black;

            DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        }
        public void ApplyDarkBoldStyle(DataGridViewCellStyle style)
        {
            style.Font = ColorHelper.BoldFont;
            style.BackColor = ColorHelper.DarkBackColor;
            style.ForeColor = ColorHelper.White;
        }
    }
    public class ReadOnlyDataGridView : UserControl
    {
        private Label titleLabel;
        private DataGridViewUC dataGridView;
        private DataItem[] data;
        public ReadOnlyDataGridView()
        {
            InitializeComponents();
        }
        public DataGridViewUC Grid {  get { return dataGridView; } }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Title
        {
            get { return titleLabel.Text; } 
            set { titleLabel.Text = value; }
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool EnableTitle
        {
            get { return titleLabel.Visible; }
            set { titleLabel.Visible = value; }
        }
        public void SetData(double?[,] table, DataItem[] dataItems, string title = "")
        {
            data = dataItems;
            Title = title;

            CreateColumns(RpmColumns.Select(x => x.Label));

            FillRows( table);
        }
        private void InitializeComponents()
        {
            // Form settings
            this.Size = new Size(800, 500);
            dataGridView = new DataGridViewUC();
            // Title Label
            titleLabel = new Label
            {
                Font = ColorHelper.TitleFontBold,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 50,
                BackColor = ColorHelper.DarkBackColor,
                ForeColor = ColorHelper.White
            };
            
            dataGridView.CellFormatting += DataGridView_CellFormatting;
            dataGridView.CellClick += DataGridView_CellClick;
            
            this.Controls.Add(dataGridView);
            this.Controls.Add(titleLabel);
        }
        private void DataGridView_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == dataGridView.Columns["InjectionTime"].Index)
            {
                dataGridView.ApplyDarkBoldStyle(e.CellStyle);
            }
        }
        
        private void DataGridView_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex != 0 && e.RowIndex >=0)
            {
                var range = Settings.InjectionRanges[e.RowIndex];
                var rpm = Settings.RpmColumns[e.ColumnIndex - 1];

                var dataItem = data.Where(x =>
                    x.RPM > rpm.Min && x.RPM <= rpm.Max &&
                    ((x.BENZ_b1 > range.Min && x.BENZ_b1 <= range.Max) ||
                     (x.BENZ_b2 > range.Min && x.BENZ_b2 <= range.Max)))
                    .ToList();

                var form = new Statsistics(dataItem)
                {
                    Text = "Detailed Statistics"
                };
                form.ShowDialog(this);
            }
        }
        private void CreateColumns(IEnumerable<int> rpmColumns)
        {
            dataGridView.Columns.Clear();

            var col = new DataGridViewTextBoxColumn
            {
                Name = "InjectionTime",
                HeaderText = "Inj.Time",
                ValueType = typeof(string)
            };
            dataGridView.Columns.Add(col);

            foreach (int rpm in rpmColumns)
            {
                var rpmCol = new DataGridViewTextBoxColumn
                {
                    Name = $"RPM_{rpm}",
                    HeaderText = rpm.ToString(),
                    ValueType = typeof(double)
                };
                //rpmCol.DefaultCellStyle.Format = "F2"; // automatic formatting
                dataGridView.Columns.Add(rpmCol);
            }
        }
        private void FillRows(double?[,] table)
        {
            dataGridView.SuspendLayout();
            try
            {
                dataGridView.Rows.Clear();

                int injCount = InjectionRanges.Length;
                int rpmCount = RpmColumns.Length;

                // Cache column indices
                int injectionColIndex = dataGridView.Columns["InjectionTime"].Index;

                int[] rpmColIndices = new int[rpmCount];
                for (int i = 0; i < rpmCount; i++)
                {
                    rpmColIndices[i] = dataGridView.Columns[$"RPM_{RpmColumns[i].Label}"].Index;
                }

                for (int injIndex = 0; injIndex < injCount; injIndex++)
                {
                    object[] cells = new object[dataGridView.Columns.Count];

                    cells[injectionColIndex] = InjectionRanges[injIndex].Label;

                    for (int rpmIndex = 0; rpmIndex < rpmCount; rpmIndex++)
                    {
                        cells[rpmColIndices[rpmIndex]] = table[rpmIndex, injIndex];
                    }

                    dataGridView.Rows.Add(cells);
                }
            }
            finally
            {
                dataGridView.ResumeLayout();
            }
        }
    }
}