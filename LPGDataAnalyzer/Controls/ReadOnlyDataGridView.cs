using LPGDataAnalyzer.Models;
using System.ComponentModel;
using System.Data;
using static LPGDataAnalyzer.Models.Settings;

namespace LPGDataAnalyzer.Controls
{
    public class ReadOnlyDataGridView : UserControl
    {
        private Label titleLabel;
        private DataGridView dataGridView;
        private DataItem[] data;
        public ReadOnlyDataGridView()
        {
            InitializeComponents();
        }
        public DataGridView Grid {  get { return dataGridView; } }
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

            PrepareGrid();

            CreateColumns(RpmColumns.Select(x => x.Label));

            FillRows( table);
        }
        public void LoadDataSource(object? dataSource)
        {
            dataGridView.DataSource = dataSource;
        }
        private void InitializeComponents()
        {
            // Form settings
            this.Size = new Size(800, 500);

            // Title Label
            titleLabel = new Label();
            titleLabel.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            titleLabel.Dock = DockStyle.Top;
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            titleLabel.Height = 50;
            titleLabel.BackColor = Color.FromArgb(45, 45, 48);
            titleLabel.ForeColor = Color.White;
            // DataGridView
            dataGridView = new DataGridView();
            dataGridView.Dock = DockStyle.Fill;

            // Make it read-only
            dataGridView.ReadOnly = true;
            dataGridView.AllowUserToAddRows = false;
            dataGridView.AllowUserToDeleteRows = false;
            dataGridView.AllowUserToResizeRows = false;
            dataGridView.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dataGridView.RowHeadersVisible = false;
            // Optional styling
            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView.CellClick += DataGridView_CellClick;
            dataGridView.DefaultCellStyle.SelectionBackColor = Color.Yellow;
            dataGridView.DefaultCellStyle.SelectionForeColor = Color.Black;
            // Add controls to form
            this.Controls.Add(dataGridView);
            this.Controls.Add(titleLabel);
        }


        private void DataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
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

                var form = new StatsForm(dataItem)
                {
                    Text = "Detailed Statistics"
                };
                form.ShowDialog(this);
            }
        }
        private void CreateColumns(IEnumerable<int> rpmColumns)
        {
            dataGridView.Columns.Add("InjectionTime", "Inj.Time");

            foreach (int rpm in rpmColumns)
            {
                dataGridView.Columns.Add($"RPM_{rpm}", rpm.ToString());
            }
        }
        private void PrepareGrid()
        {
            dataGridView.DataSource = null;
            dataGridView.Columns.Clear();
            dataGridView.Rows.Clear();
        }
        private void FillRows(double?[,] table)
        {
            dataGridView.SuspendLayout();
            try
            {
                dataGridView.Rows.Clear();

                for (var injIndex = 0; injIndex < InjectionRanges.Length; injIndex++)
                {
                    var row = new DataGridViewRow();
                    row.CreateCells(dataGridView);

                    row.Cells[dataGridView.Columns["InjectionTime"].Index].Value = InjectionRanges[injIndex].Label;

                    for (var rpmIndex = 0; rpmIndex < RpmColumns.Length; rpmIndex++)
                    {


                        row.Cells[dataGridView.Columns[$"RPM_{RpmColumns[rpmIndex].Label}"].Index].Value = table[rpmIndex, injIndex];

                    }

                    dataGridView.Rows.Add(row);
                }
            }
            finally
            {
                dataGridView.ResumeLayout();
            }
        }
    }
}