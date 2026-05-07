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
        private Func<DataItem, double> _InjectionSelector;

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
        public void SetData(double?[,] table, DataItem[] dataItems, Func<DataItem, double> injectionSelector, string title = "")
        {
            _InjectionSelector = injectionSelector;
            currentTable = table;
            data = dataItems;
            Title = title;
            CreateColumns();
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
            if (data is null || e.ColumnIndex == 0 || e.RowIndex < 0)
                return;

            var range = Settings.InjectionRanges[e.RowIndex];
            var rpm = Settings.RpmColumns[e.ColumnIndex - 1];

            var dataItem = data.Where(x => _InjectionSelector(x) > range.Min && _InjectionSelector(x) <= range.Max &&
                 x.RPM > rpm.Min && x.RPM <= rpm.Max).ToList();

            var cellValue = currentTable[e.ColumnIndex - 1, e.RowIndex];

            ShowStatisticForm(this, dataItem, cellValue);
        }

        public static void ShowStatisticForm(IWin32Window? owner, List<DataItem> data, double? value)
        {
            var form = new Statsistics(data, value)
            {
                Text = "Detailed Statistics"
            };
            form.ShowDialog(owner);
        }
        private void CreateColumns()
        {
            if (dataGridView.Columns.Count != RpmColumns.Length + 1)
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

                foreach (var rpm in RpmColumns)
                {
                    var rpmCol = new DataGridViewTextBoxColumn
                    {
                        Name = $"RPM_{rpm.Label}",
                        HeaderText = rpm.Label.ToString(),
                        ValueType = typeof(double),
                        AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                    };
                    dataGridView.Columns.Add(rpmCol);
                }
            }
        }
        private void FillRows(double?[,] table)
        {
            int injCount = InjectionRanges.Length;
            int rpmCount = RpmColumns.Length;

            var grid = dataGridView;

            grid.SuspendLayout();

            try
            {
                // Ensure row count
                if (grid.Rows.Count != injCount)
                {
                    grid.Rows.Clear();
                    grid.RowCount = injCount;
                }

                var rows = grid.Rows;

                for (int i = 0; i < injCount; i++)
                {
                    var cells = rows[i].Cells;

                    // Column 0 = InjectionTime
                    if (!Equals(cells[0].Value, InjectionRanges[i].Label))
                        cells[0].Value = InjectionRanges[i].Label;

                    // Columns 1..N = RPM values
                    for (int j = 0; j < rpmCount; j++)
                    {
                        if (j >= 0 && j < table.GetLength(0) &&
                            i >= 0 && i < table.GetLength(1))
                        {
                            var newVal = table[j, i];

                            if (!Equals(cells[j + 1].Value, newVal))
                            {
                                cells[j + 1].Value = newVal;
                            }
                        }
                    }
                }
            }
            finally
            {
                grid.ResumeLayout();
            }
        }
    }
}