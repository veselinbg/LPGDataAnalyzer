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
        class ValueItem
        {
            public object Value { get; set; }
            public int Count { get; set; }

            public override string ToString()
            {
                return $"{Value} ({Count})";
            }
        }
        private bool readOnly = false;
        private string sortColumn;
        private bool sortAsc = true;
        private Label titleLabel = new();
        private TextBox searchBox = new();
        private DataGridView grid = new();

        private List<T> source = new();
        private List<T> filtered = new();

        private Dictionary<string, List<IColumnFilter<T>>> filters = new();
        private Dictionary<string, ColumnFilterSelection> columnSelections = new();
        private Dictionary<string, Func<T, object>> getters = new();
        private Dictionary<string, List<object>> columnValueCache = new();
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
        private Dictionary<string, Rectangle> headerButtonRectangles = new();
        public EnterpriseGrid()
        {
            BuildLayout();
            BuildGetterCache();
            BuildFilterPanel();
        }
        private IEnumerable<T> GetDataFilteredExcept(string excludeColumn)
        {
            IEnumerable<T> data = source;

            foreach (var kv in filters)
            {
                if (kv.Key == excludeColumn)
                    continue;

                foreach (var f in kv.Value)
                    data = f.Apply(data);
            }

            return data;
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
            grid.Scroll += (_, __) => UpdateFilterButtonsPosition();
            grid.CellPainting += Grid_CellPainting;
            Controls.Add(filterPanel);
            Controls.Add(grid);
            Controls.Add(searchBox);
            Controls.Add(titleLabel);
        }
        private void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex != -1 || e.ColumnIndex < 0 || e.ColumnIndex >= grid.Columns.Count)
                return;

            e.PaintBackground(e.CellBounds, true);

            var col = grid.Columns[e.ColumnIndex];
            var rect = e.CellBounds;

            // draw filter button
            var buttonRect = new Rectangle(rect.Left + 4, rect.Top + 4, 14, rect.Height - 8);
            ControlPaint.DrawComboButton(e.Graphics, buttonRect, ButtonState.Normal);

            // use header font (bold if filtered)
            var font = col.HeaderCell.Style.Font ?? grid.Font;

            var textRect = new Rectangle(rect.Left + 22, rect.Top, rect.Width - 22, rect.Height);

            TextRenderer.DrawText(
                e.Graphics,
                col.HeaderText,
                font,
                textRect,
                grid.ForeColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left
            );

            headerButtonRectangles[col.DataPropertyName] = buttonRect;

            e.Handled = true;
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
        private void Grid_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.ColumnIndex >= grid.Columns.Count)
                return;

            var col = grid.Columns[e.ColumnIndex];

            // Click relative to header cell
            var cellRect = grid.GetCellDisplayRectangle(e.ColumnIndex, -1, true);
            var clickPoint = new Point(e.X + cellRect.Left, e.Y + cellRect.Top);

            if (headerButtonRectangles.TryGetValue(col.DataPropertyName, out var buttonRect))
            {
                if (buttonRect.Contains(clickPoint))
                {
                    // Show filter panel under header
                    ShowFilterPanel(col.DataPropertyName,new Point(cellRect.Left, cellRect.Bottom));
                    return;
                }
            }

            // If not clicked on button, sort column
            SortColumn(col.DataPropertyName);
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
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
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

            BuildColumnCache();

            grid.DataSource = new BindingList<T>(filtered);
        }
        private void BuildColumnCache()
        {
            columnValueCache.Clear();

            foreach (var column in getters.Keys)
            {
                columnValueCache[column] = source
                    .Select(x => getters[column](x))
                    .ToList();
            }
        }
        private void AddFilter(string column, IColumnFilter<T> filter)
        {
            if (!filters.ContainsKey(column)) filters[column] = new List<IColumnFilter<T>>();
            filters[column].Add(filter);
        }

        private void ShowFilterPanel(string column, Point headerLocation)
        {
            currentColumn = column;

            if (!columnSelections.ContainsKey(column))
                columnSelections[column] = new ColumnFilterSelection();

            PopulateValueList(columnSelections[column]);

            // Convert grid coordinates → EnterpriseGrid coordinates
            var localPoint = this.PointToClient(grid.PointToScreen(headerLocation));

            filterPanel.Location = new Point(localPoint.X, localPoint.Y + 5);

            // Prevent panel from going outside the right edge
            if (filterPanel.Right > this.Width)
                filterPanel.Left = this.Width - filterPanel.Width - 5;

            filterPanel.BringToFront();
            filterPanel.Visible = true;
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

                if (filters.Count > 0)
                {
                    foreach (var kv in filters)
                    {
                        foreach (var f in kv.Value)
                            data = f.Apply(data);
                    }
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    data = data.Where(item =>
                    {
                        foreach (var g in getters.Values)
                        {
                            var v = g(item)?.ToString()?.ToLower();
                            if (v != null && v.Contains(search))
                                return true;
                        }

                        return false;
                    });
                }

                return data.ToList();
            });

            filtered = result;

            grid.Invoke(() =>
            {
                grid.DataSource = new BindingList<T>(filtered);
            });
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
            else
            {
                this.BeginInvoke(new Action(() =>
                {
                    bool allChecked = true;
                    for (int i = 1; i < valueList.Items.Count; i++)
                    {
                        if (!valueList.GetItemChecked(i)) { allChecked = false; break; }
                    }
                    valueList.ItemCheck -= ValueList_ItemCheck;
                    valueList.SetItemChecked(0, allChecked);
                    valueList.ItemCheck += ValueList_ItemCheck;
                }));
            }
        }
        private void SortColumn(string column)
        {
            if (!getters.ContainsKey(column)) return;

            // toggle sort direction if same column
            if (sortColumn == column)
                sortAsc = !sortAsc;
            else
            {
                sortColumn = column;
                sortAsc = true;
            }

            // get the getter once for speed
            var getter = getters[column];

            // fast type-aware sorting
            filtered = sortAsc
                ? filtered.OrderBy(item =>
                {
                    var val = getter(item);
                    if (val == null) return decimal.MinValue; // nulls first
                    if (val is IComparable) return val;       // numeric/date/string
                    return val.ToString();                     // fallback to string
                }).ToList()
                : filtered.OrderByDescending(item =>
                {
                    var val = getter(item);
                    if (val == null) return decimal.MinValue;
                    if (val is IComparable) return val;
                    return val.ToString();
                }).ToList();

            grid.DataSource = new BindingList<T>(filtered);

            UpdateHeaderSortArrows();
        }
        private void UpdateHeaderSortArrows()
        {
            foreach (DataGridViewColumn col in grid.Columns)
            {
                col.HeaderText = col.DataPropertyName;
                if (col.DataPropertyName == sortColumn)
                    col.HeaderText += sortAsc ? " ↑" : " ↓";
            }
        }
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            UpdateFilterButtonsPosition();
        }

        private void UpdateFilterButtonsPosition()
        {
            if (grid.Columns == null) return;

            foreach (DataGridViewColumn col in grid.Columns)
            {
                if (col.HeaderCell.Tag is Button btn)
                {
                    var rect = grid.GetCellDisplayRectangle(col.Index, -1, true);
                    btn.Location = new System.Drawing.Point(rect.Left + 2, rect.Top + 2);
                    btn.BringToFront();
                }
            }
        }
        private void FilterCheckboxList()
        {
            if (currentColumn == null) return;

            decimal? min = decimal.TryParse(minBox.Text, out var minVal) ? minVal : (decimal?)null;
            decimal? max = decimal.TryParse(maxBox.Text, out var maxVal) ? maxVal : (decimal?)null;

            IEnumerable<T> data = source;

            foreach (var kv in filters)
            {
                if (kv.Key == currentColumn) continue;

                foreach (var f in kv.Value)
                    data = f.Apply(data);
            }

            var allValues = data
                .Select(getters[currentColumn])
                .Distinct()
                .OrderBy(v => v?.ToString())
                .ToList();

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
            if (currentColumn == null) return;

            var data = GetDataFilteredExcept(currentColumn);

            var counts = new Dictionary<object, int>();

            foreach (var item in data)
            {
                var v = getters[currentColumn](item);

                if (v == null)
                    continue;

                if (counts.ContainsKey(v))
                    counts[v]++;
                else
                    counts[v] = 1;
            }

            var values = counts.Keys
                .OrderBy(v => v is IComparable ? 0 : 1)
                .ThenBy(v => v)
                .ToList();

            valueList.ItemCheck -= ValueList_ItemCheck;
            valueList.Items.Clear();

            bool selectAllChecked =
                sel.SelectedValues.Count == 0 ||
                sel.SelectedValues.Count == values.Count;

            valueList.Items.Add("Select All", selectAllChecked);

            foreach (var v in values)
            {
                var item = new ValueItem
                {
                    Value = v,
                    Count = counts[v]
                };

                bool isChecked =
                    sel.SelectedValues.Count == 0 ||
                    sel.SelectedValues.Contains(v);

                valueList.Items.Add(item, isChecked);
            }

            valueList.ItemCheck += ValueList_ItemCheck;

            minBox.Text = sel.Min?.ToString() ?? "";
            maxBox.Text = sel.Max?.ToString() ?? "";

            andButton.Checked = !sel.UseOrLogic;
            orButton.Checked = sel.UseOrLogic;
        }

        private async Task ApplyFilterForColumn()
        {
            filterPanel.Visible = false;

            var selectedValues = valueList.CheckedItems
                .Cast<object>()
                .Where(x => x is ValueItem)
                .Cast<ValueItem>()
                .Select(x => x.Value)
                .ToHashSet();

            var sel = new ColumnFilterSelection
            {
                SelectedValues = selectedValues,
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