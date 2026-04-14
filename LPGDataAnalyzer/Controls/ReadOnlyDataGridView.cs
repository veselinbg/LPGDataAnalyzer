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
            ApplyLightHeaderStyle(RowHeadersDefaultCellStyle);
            ApplyLightHeaderStyle(ColumnHeadersDefaultCellStyle);
            DefaultCellStyle.SelectionBackColor = Color.DodgerBlue;
            DefaultCellStyle.SelectionForeColor = Color.White;
            DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            DefaultCellStyle.BackColor = Color.White;
            DefaultCellStyle.ForeColor = Color.Black;
            AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
            BackgroundColor = Color.White;
            GridColor = Color.LightGray;
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            AutoSize = true;
        }
        public void ApplyLightHeaderStyle(DataGridViewCellStyle style)
        {
            style.Font = ColorHelper.BoldFont;
            style.BackColor = Color.Gainsboro;
            style.ForeColor = Color.Black;
        }
        public void FormatGrid()
        {
            this.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;

            foreach (DataGridViewColumn col in this.Columns)
            {
                if (col.Name.Contains("Avg"))
                {
                    col.DefaultCellStyle.BackColor = Color.FromArgb(230, 240, 255);
                    col.DefaultCellStyle.ForeColor = Color.Black;
                }

                if (col.Name.Contains("Min") || col.Name.Contains("Max"))
                {
                    col.DefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
                    col.DefaultCellStyle.ForeColor = Color.Black;
                }

                if (col.Name.Contains("Count"))
                {
                    col.DefaultCellStyle.ForeColor = Color.DimGray;
                }

                if (col.Name.Contains("Temp"))
                {
                    col.DefaultCellStyle.ForeColor = Color.DarkOrange;
                }
            }
            this.CellFormatting += (s, e) =>
            {
                var col = this.Columns[e.ColumnIndex].Name;

                if (col.Contains("Trim") && e.Value is double val)
                {
                    if (val > 10)
                    {
                        e.CellStyle.BackColor = Color.LightCoral;
                        e.CellStyle.ForeColor = Color.Black;
                    }
                    else if (val < -10)
                    {
                        e.CellStyle.BackColor = Color.LightBlue;
                        e.CellStyle.ForeColor = Color.Black;
                    }
                    else
                    {
                        e.CellStyle.BackColor = Color.LightGreen;
                        e.CellStyle.ForeColor = Color.Black;
                    }
                }
                if (e.Value != null && e.CellStyle.ForeColor == e.CellStyle.BackColor)
                {
                    e.CellStyle.BackColor = Color.Magenta; // obvious bug marker
                    e.CellStyle.ForeColor = Color.Black;
                }
            };
        }

        public void LoadData(object data)
        {
            this.DataSource = data;
            FormatGrid();
        }
    }
    public class ReadOnlyDataGridView : UserControl
    {
        private Label titleLabel;
        private DataGridViewUC dataGridView;
        private DataItem[] data;
        private double?[,] currentTable;
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
            currentTable = table;
            data = dataItems;
            Title = title;

            CreateColumns(RpmColumns.Select(x => x.Label));

            FillRows( table);
        }
        private void InitializeComponents()
        {
            dataGridView = new DataGridViewUC();

            titleLabel = new Label
            {
                Font = ColorHelper.TitleFontBold,
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 35,
                BackColor = ColorHelper.DarkBackColor,
                ForeColor = ColorHelper.White
            };
            
            dataGridView.CellFormatting += DataGridView_CellFormatting;
            dataGridView.CellClick += DataGridView_CellClick;
            
            this.Controls.Add(dataGridView);
            this.Controls.Add(titleLabel);
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }
        private void DataGridView_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dataGridView.Columns.Contains("InjectionTime") && e.ColumnIndex == dataGridView.Columns["InjectionTime"].Index)
            {
                dataGridView.ApplyLightHeaderStyle(e.CellStyle);
            }
        }
        
        private void DataGridView_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (data is not null && e.ColumnIndex != 0 && e.RowIndex >= 0)
            {
                var range = Settings.InjectionRanges[e.RowIndex];
                var rpm = Settings.RpmColumns[e.ColumnIndex - 1];

                var dataItem = data.Where(x =>
                    x.RPM > rpm.Min && x.RPM <= rpm.Max &&
                    ((x.BENZ_b1 > range.Min && x.BENZ_b1 <= range.Max) ||
                     (x.BENZ_b2 > range.Min && x.BENZ_b2 <= range.Max)))
                    .ToList();
                var cellValue = currentTable[e.ColumnIndex - 1, e.RowIndex];

                ShowStatisticForm(this, dataItem, cellValue);
            }
        }

        public static void ShowStatisticForm(IWin32Window? owner, List<DataItem> data, double? value)
        {
            var form = new Statsistics(data, value)
            {
                Text = "Detailed Statistics"
            };
            form.ShowDialog(owner);
        }
        private void CreateColumns(IEnumerable<int> rpmColumns)
        {
            dataGridView.Columns.Clear();

            var col = new DataGridViewTextBoxColumn
            {
                Name = "InjectionTime",
                HeaderText = "Inj.Time",
                ValueType = typeof(string),
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            };
            dataGridView.Columns.Add(col);

            foreach (int rpm in rpmColumns)
            {
                var rpmCol = new DataGridViewTextBoxColumn
                {
                    Name = $"RPM_{rpm}",
                    HeaderText = rpm.ToString(),
                    ValueType = typeof(double),
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
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