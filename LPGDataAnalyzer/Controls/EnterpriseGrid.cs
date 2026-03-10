using LPGDataAnalyzer.Models;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace LPGDataAnalyzer.Controls
{
    public interface IColumnFilter<T>
    {
        IEnumerable<T> Apply(IEnumerable<T> data);
    }

    public class ColumnFilterSelection
    {
        public HashSet<object> SelectedValues { get; set; } = new();
        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
        public bool UseOrLogic { get; set; } = false;
    }

    public class ValueFilter<T> : IColumnFilter<T>
    {
        private readonly Func<T, object> getter;
        private readonly HashSet<object> values;

        public ValueFilter(Func<T, object> getter, IEnumerable<object> vals)
        {
            this.getter = getter;
            values = new HashSet<object>(vals);
        }

        public IEnumerable<T> Apply(IEnumerable<T> data)
        {
            return data.Where(x => values.Contains(getter(x)));
        }
    }

    public class RangeFilter<T> : IColumnFilter<T>
    {
        private readonly Func<T, object> getter;
        private readonly decimal? min;
        private readonly decimal? max;

        public RangeFilter(Func<T, object> getter, decimal? min, decimal? max)
        {
            this.getter = getter;
            this.min = min;
            this.max = max;
        }

        public IEnumerable<T> Apply(IEnumerable<T> data)
        {
            foreach (var item in data)
            {
                var val = getter(item);
                if (val == null) continue;
                if (!decimal.TryParse(val.ToString(), out var d)) continue;
                if (min.HasValue && d < min.Value) continue;
                if (max.HasValue && d > max.Value) continue;
                yield return item;
            }
        }
    }

    public class EnterpriseGrid<T> : UserControl
    {
        private bool readOnly = false;

        private Label titleLabel = new();
        private TextBox searchBox = new();
        private DataGridView grid = new();

        private List<T> source = new();
        private List<T> filtered = new();

        private Dictionary<string, List<IColumnFilter<T>>> filters = new();
        private Dictionary<string, ColumnFilterSelection> columnSelections = new();
        private Dictionary<string, Func<T, object>> getters = new();

        // Floating filter panel
        private Panel filterPanel = new() { Visible = false, BorderStyle = BorderStyle.FixedSingle };
        private CheckedListBox valueList = new();
        private TextBox minBox = new();
        private TextBox maxBox = new();
        private RadioButton andButton = new() { Text = "AND", Checked = true };
        private RadioButton orButton = new() { Text = "OR" };
        private Button applyButton = new() { Text = "Apply" };
        private Button resetButton = new() { Text = "Reset" };
        private Button closeButton = new() { Text = "X" };
        private string currentColumn;

        public EnterpriseGrid()
        {
            BuildLayout();
            BuildGetterCache();
            BuildFilterPanel();
        }

        private void BuildLayout()
        {
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Height = 28;
            titleLabel.Font = new System.Drawing.Font(titleLabel.Font, System.Drawing.FontStyle.Regular);

            searchBox.Dock = DockStyle.Top;
            searchBox.PlaceholderText = "Search...";
            searchBox.TextChanged += async (_, __) => await ApplyFiltersAsync();

            grid.Dock = DockStyle.Fill;
            grid.AutoGenerateColumns = true;
            grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick;
            grid.CellMouseDown += Grid_CellMouseDown;

            Controls.Add(filterPanel);
            Controls.Add(grid);
            Controls.Add(searchBox);
            Controls.Add(titleLabel);
        }

        private void BuildGetterCache()
        {
            foreach (var prop in typeof(T).GetProperties())
            {
                var param = Expression.Parameter(typeof(T));
                var body = Expression.Convert(Expression.Property(param, prop), typeof(object));
                getters[prop.Name] = Expression.Lambda<Func<T, object>>(body, param).Compile();
            }
        }

        private void BuildFilterPanel()
        {
            filterPanel.Controls.Clear();
            filterPanel.Width = 250;
            filterPanel.Height = 280;

            // Close X in top-right corner
            closeButton.Width = 25;
            closeButton.Height = 25;
            closeButton.Top = 0;
            closeButton.Left = filterPanel.Width - closeButton.Width - 2;
            closeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            closeButton.Click += (_, __) => filterPanel.Visible = false;
            filterPanel.Controls.Add(closeButton);

            valueList.Dock = DockStyle.Top;
            valueList.Height = 150;
            valueList.ItemCheck += ValueList_ItemCheck;
            filterPanel.Controls.Add(valueList);

            minBox.PlaceholderText = "Min";
            minBox.Dock = DockStyle.Top;
            minBox.TextChanged += (_, __) => FilterCheckboxList();
            filterPanel.Controls.Add(minBox);

            maxBox.PlaceholderText = "Max";
            maxBox.Dock = DockStyle.Top;
            maxBox.TextChanged += (_, __) => FilterCheckboxList();
            filterPanel.Controls.Add(maxBox);

            var togglePanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 30, FlowDirection = FlowDirection.LeftToRight };
            togglePanel.Controls.Add(andButton);
            togglePanel.Controls.Add(orButton);
            filterPanel.Controls.Add(togglePanel);

            applyButton.Dock = DockStyle.Bottom;
            applyButton.Click += async (_, __) => await ApplyFilterForColumn();
            filterPanel.Controls.Add(applyButton);

            resetButton.Dock = DockStyle.Bottom;
            resetButton.Click += async (_, __) => await ResetFilterPanel();
            filterPanel.Controls.Add(resetButton);
        }

        [Browsable(true)]
        [Category("Behavior")]
        [Description("If true, prevents editing cells but still allows filtering and searching.")]
        public bool ReadOnly
        {
            get => readOnly;
            set
            {
                readOnly = value;
                grid.ReadOnly = value;              // prevent editing cells
                grid.AllowUserToAddRows = !value;   // prevent adding rows
                grid.AllowUserToDeleteRows = !value; // prevent deleting rows
            }
        }
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Category("Appearance")]
        [Description("Title text displayed above the grid.")]
        public string Title
        {
            get => titleLabel.Text;
            set => titleLabel.Text = value;
        }

        public void SetData(IEnumerable<T> data)
        {
            source = data.ToList();
            filtered = source;
            grid.DataSource = new BindingList<T>(filtered);
        }

        private void AddFilter(string column, IColumnFilter<T> filter)
        {
            if (!filters.ContainsKey(column)) filters[column] = new List<IColumnFilter<T>>();
            filters[column].Add(filter);
        }

        private void ClearFilters(string column = null)
        {
            if (column == null)
            {
                filters.Clear();
                columnSelections.Clear();
            }
            else
            {
                filters.Remove(column);
            }
            UpdateColumnHeaderStyles();
            UpdateTitleStyle();
        }

        private void UpdateTitleStyle()
        {
            titleLabel.Font = new System.Drawing.Font(titleLabel.Font, filters.Count > 0 ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular);
        }

        private void UpdateColumnHeaderStyles()
        {
            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (filters.ContainsKey(col.DataPropertyName))
                    col.HeaderCell.Style.Font = new System.Drawing.Font(col.HeaderCell.Style.Font ?? grid.Font, System.Drawing.FontStyle.Bold);
                else
                    col.HeaderCell.Style.Font = new System.Drawing.Font(col.HeaderCell.Style.Font ?? grid.Font, System.Drawing.FontStyle.Regular);
            }
        }

        private async Task ApplyFiltersAsync()
        {
            string search = searchBox.Text.ToLower();
            var result = await Task.Run(() =>
            {
                IEnumerable<T> data = source;

                foreach (var colFilters in filters.Values)
                    foreach (var f in colFilters)
                        data = f.Apply(data);

                if (!string.IsNullOrWhiteSpace(search))
                    data = data.Where(item => getters.Values.Any(g =>
                    {
                        var v = g(item)?.ToString()?.ToLower();
                        return v != null && v.Contains(search);
                    }));

                return data.ToList();
            });

            filtered = result;
            grid.Invoke(() => grid.DataSource = new BindingList<T>(filtered));
        }

        private void Grid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            var column = grid.Columns[e.ColumnIndex];
            currentColumn = column.DataPropertyName;
            if (!getters.ContainsKey(currentColumn)) return;

            ColumnFilterSelection sel = columnSelections.ContainsKey(currentColumn) ? columnSelections[currentColumn] : new ColumnFilterSelection();
            PopulateValueList(sel);

            var rect = grid.GetCellDisplayRectangle(e.ColumnIndex, -1, true);
            filterPanel.Location = new System.Drawing.Point(rect.Left, rect.Bottom);
            filterPanel.BringToFront();
            filterPanel.Visible = true;
        }

        private void Grid_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex != -1 || e.Button != MouseButtons.Right) return;

            ContextMenuStrip menu = new();
            foreach (DataGridViewColumn col in grid.Columns)
            {
                var item = new ToolStripMenuItem(col.HeaderText)
                {
                    Checked = col.Visible,
                    CheckOnClick = true
                };
                item.CheckedChanged += (_, __) => col.Visible = item.Checked;
                menu.Items.Add(item);
            }
            menu.Show(Cursor.Position);
        }

        private void ValueList_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.Index == 0)
            {
                bool newState = e.NewValue == CheckState.Checked;
                this.BeginInvoke(new Action(() =>
                {
                    for (int i = 1; i < valueList.Items.Count; i++)
                        valueList.SetItemChecked(i, newState);
                }));
            }
        }

        private void FilterCheckboxList()
        {
            if (currentColumn == null) return;

            decimal? min = decimal.TryParse(minBox.Text, out var minVal) ? minVal : (decimal?)null;
            decimal? max = decimal.TryParse(maxBox.Text, out var maxVal) ? maxVal : (decimal?)null;

            var allValues = source.Select(getters[currentColumn]).Distinct().OrderBy(v => v?.ToString()).ToList();
            var filteredValues = allValues.Where(v =>
            {
                if (v == null) return false;
                if (decimal.TryParse(v.ToString(), out var d))
                {
                    if (min.HasValue && d < min.Value) return false;
                    if (max.HasValue && d > max.Value) return false;
                }
                return true;
            }).ToList();

            valueList.ItemCheck -= ValueList_ItemCheck;
            valueList.Items.Clear();
            valueList.Items.Add("Select All", columnSelections.ContainsKey(currentColumn) && (columnSelections[currentColumn].SelectedValues.Count == 0 || columnSelections[currentColumn].SelectedValues.Count == filteredValues.Count));
            foreach (var v in filteredValues)
                valueList.Items.Add(v, !columnSelections.ContainsKey(currentColumn) || columnSelections[currentColumn].SelectedValues.Contains(v));
            valueList.ItemCheck += ValueList_ItemCheck;
        }

        private void PopulateValueList(ColumnFilterSelection sel)
        {
            var allValues = source.Select(getters[currentColumn]).Distinct().OrderBy(v => v?.ToString()).ToList();
            valueList.ItemCheck -= ValueList_ItemCheck;
            valueList.Items.Clear();
            valueList.Items.Add("Select All", sel.SelectedValues.Count == 0 || sel.SelectedValues.Count == allValues.Count);
            foreach (var v in allValues)
                valueList.Items.Add(v, sel.SelectedValues.Count == 0 || sel.SelectedValues.Contains(v));
            valueList.ItemCheck += ValueList_ItemCheck;

            minBox.Text = sel.Min?.ToString() ?? "";
            maxBox.Text = sel.Max?.ToString() ?? "";
            andButton.Checked = !sel.UseOrLogic;
            orButton.Checked = sel.UseOrLogic;
        }

        private async Task ApplyFilterForColumn()
        {
            filterPanel.Visible = false;

            var sel = new ColumnFilterSelection
            {
                SelectedValues = valueList.CheckedItems.Cast<object>().Where(x => x.ToString() != "Select All").ToHashSet(),
                Min = decimal.TryParse(minBox.Text, out var min) ? min : (decimal?)null,
                Max = decimal.TryParse(maxBox.Text, out var max) ? max : (decimal?)null,
                UseOrLogic = orButton.Checked
            };

            columnSelections[currentColumn] = sel;
            ClearFilters(currentColumn);

            if (sel.SelectedValues.Count > 0)
                AddFilter(currentColumn, new ValueFilter<T>(getters[currentColumn], sel.SelectedValues));
            if (sel.Min.HasValue || sel.Max.HasValue)
                AddFilter(currentColumn, new RangeFilter<T>(getters[currentColumn], sel.Min, sel.Max));

            UpdateColumnHeaderStyles();
            UpdateTitleStyle();
            await ApplyFiltersAsync();
        }

        private async Task ResetFilterPanel()
        {
            valueList.ItemCheck -= ValueList_ItemCheck;
            for (int i = 0; i < valueList.Items.Count; i++)
                valueList.SetItemChecked(i, true);
            valueList.ItemCheck += ValueList_ItemCheck;

            minBox.Clear();
            maxBox.Clear();
            andButton.Checked = true;
            orButton.Checked = false;

            columnSelections.Remove(currentColumn);
            ClearFilters(currentColumn);
            UpdateColumnHeaderStyles();
            UpdateTitleStyle();
            await ApplyFiltersAsync();
        }

    }
}