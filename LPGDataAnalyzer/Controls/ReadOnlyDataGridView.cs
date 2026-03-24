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
            titleLabel = new Label
            {
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Dock = DockStyle.Top,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 50,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };
            // DataGridView
            dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,

                // Make it read-only
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                RowHeadersVisible = false,
                // Optional styling
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
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
            dataGridView.Columns.Clear();

            dataGridView.Columns.Add("InjectionTime", "Inj.Time");

            foreach (int rpm in rpmColumns)
            {
                dataGridView.Columns.Add($"RPM_{rpm}", rpm.ToString());
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