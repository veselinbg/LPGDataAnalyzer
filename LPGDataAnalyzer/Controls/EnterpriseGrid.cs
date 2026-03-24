using System.ComponentModel;
using System.Linq.Expressions;

namespace LPGDataAnalyzer.Controls
{
    public interface IColumnFilter<T>
    {
        IEnumerable<T> Apply(IEnumerable<T> data);
    }
    public class CombinedColumnFilter<T> : IColumnFilter<T>
    {
        private readonly Func<T, object> getter;
        private readonly HashSet<object> values;
        private readonly double? min;
        private readonly double? max;
        private readonly bool useOr;

        public CombinedColumnFilter(
            Func<T, object> getter,
            HashSet<object> values,
            double? min,
            double? max,
            bool useOr)
        {
            this.getter = getter;
            this.values = values;
            this.min = min;
            this.max = max;
            this.useOr = useOr;
        }

        public IEnumerable<T> Apply(IEnumerable<T> data)
        {
            foreach (var item in data)
            {
                var val = getter(item);

                bool valueMatch = true;
                bool rangeMatch = true;

                if (values.Count > 0)
                    valueMatch = val != null && values.Contains(val);

                if (min.HasValue || max.HasValue)
                {
                    if (val == null || !double.TryParse(val.ToString(), out var d))
                        rangeMatch = false;
                    else
                    {
                        if (min.HasValue && d < min.Value) rangeMatch = false;
                        if (max.HasValue && d > max.Value) rangeMatch = false;
                    }
                }

                bool pass = useOr
                    ? (valueMatch || rangeMatch)
                    : (valueMatch && rangeMatch);

                if (pass)
                    yield return item;
            }
        }
    }
    public class ColumnFilterSelection
    {
        public HashSet<object> SelectedValues { get; set; } = new();
        public double? Min { get; set; }
        public double? Max { get; set; }
        public bool UseOrLogic { get; set; } = false;
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
        private bool isUpdatingUI = false;
        private string baseTitle = "";
        private string sortColumn;
        private bool sortAsc = true;

        private Panel titleBar = new();
        private Label titleLabel = new();
        private Label rowCountLabel = new();
        private Label filterIcon = new();
        private Button refreshButton = new();
        private Button clearFiltersButton = new();

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
        private CheckBox selectAllCheckBox = new CheckBox();
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
            titleBar.Dock = DockStyle.Top;
            titleBar.Height = 34;
            titleBar.Padding = new Padding(6, 4, 6, 4);
            titleBar.BackColor = Color.FromArgb(245, 245, 245);

            titleLabel.AutoSize = true;
            titleLabel.Font = new Font(titleLabel.Font, FontStyle.Bold);
            titleLabel.Location = new Point(6, 8);

            filterIcon.AutoSize = true;
            filterIcon.Text = "🔎";
            filterIcon.Visible = false;
            filterIcon.Location = new Point(120, 8);

            rowCountLabel.AutoSize = true;
            rowCountLabel.ForeColor = Color.DimGray;
            rowCountLabel.Location = new Point(140, 8);

            refreshButton.Text = "⟳";
            refreshButton.Width = 32;
            refreshButton.Height = 24;
            refreshButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            refreshButton.Location = new Point(Width - 80, 5);
            refreshButton.Click += async (_, __) => await ApplyFiltersAsync();

            clearFiltersButton.Text = "✖ Clear";
            clearFiltersButton.Height = 24;
            clearFiltersButton.Width = 80;
            clearFiltersButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            clearFiltersButton.Location = new Point(Width - 160, 5);
            clearFiltersButton.Click += async (_, __) => await ClearAllFilters();

            titleBar.Controls.Add(titleLabel);
            titleBar.Controls.Add(filterIcon);
            titleBar.Controls.Add(rowCountLabel);
            titleBar.Controls.Add(refreshButton);
            titleBar.Controls.Add(clearFiltersButton);

            searchBox.Dock = DockStyle.Top;
            searchBox.PlaceholderText = "Search...";
            searchBox.TextChanged += async (_, __) => await ApplyFiltersAsync();

            grid.Dock = DockStyle.Fill;
            grid.AllowUserToResizeColumns = true;
            grid.AllowUserToResizeRows = false;
            grid.AllowUserToOrderColumns = true; // enable drag reorder
            grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick;
            grid.CellMouseDown += Grid_CellMouseDown;
            grid.CellPainting += Grid_CellPainting;
            grid.ColumnDisplayIndexChanged += Grid_ColumnDisplayIndexChanged;

            Controls.Add(filterPanel);
            Controls.Add(grid);
            Controls.Add(searchBox);
            Controls.Add(titleBar);
        }
        public void SetColumnOrder(IEnumerable<string> order)
        {
            int index = 0;

            foreach (var colName in order)
            {
                if (grid.Columns.Contains(colName))
                {
                    grid.Columns[colName].DisplayIndex = index++;
                }
            }
        }
        private void Grid_ColumnDisplayIndexChanged(object sender, DataGridViewColumnEventArgs e)
        {
            // Example: store column order
            var order = grid.Columns
                .Cast<DataGridViewColumn>()
                .OrderBy(c => c.DisplayIndex)
                .Select(c => c.DataPropertyName)
                .ToList();

            headerButtonRectangles.Clear(); // fix filter button hit detection
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

            // Close button
            closeButton.Width = 25;
            closeButton.Height = 25;
            closeButton.Top = 0;
            closeButton.Left = filterPanel.Width - closeButton.Width - 2;
            closeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            closeButton.Click += (_, __) => filterPanel.Visible = false;
            filterPanel.Controls.Add(closeButton);

            // Select All checkbox
            selectAllCheckBox.Text = "Select All";
            selectAllCheckBox.Dock = DockStyle.Top;
            selectAllCheckBox.CheckedChanged += SelectAllCheckBox_CheckedChanged;
            filterPanel.Controls.Add(selectAllCheckBox);

            // Value list
            valueList.Dock = DockStyle.Top;
            valueList.Height = 130;
            valueList.ItemCheck += ValueList_ItemCheck;
            filterPanel.Controls.Add(valueList);

            // Min/Max boxes
            minBox.PlaceholderText = "Min";
            minBox.Dock = DockStyle.Top;
            minBox.TextChanged += MinMaxBox_TextChanged;
            filterPanel.Controls.Add(minBox);

            maxBox.PlaceholderText = "Max";
            maxBox.Dock = DockStyle.Top;
            maxBox.TextChanged += MinMaxBox_TextChanged;
            filterPanel.Controls.Add(maxBox);

            // AND/OR toggle
            var togglePanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 30, FlowDirection = FlowDirection.LeftToRight };
            togglePanel.Controls.Add(andButton);
            togglePanel.Controls.Add(orButton);
            andButton.CheckedChanged += LogicModeChanged;
            orButton.CheckedChanged += LogicModeChanged;
            filterPanel.Controls.Add(togglePanel);

            // Apply / Reset buttons
            applyButton.Dock = DockStyle.Bottom;
            applyButton.Click += async (_, __) => await ApplyFilterForColumn();
            filterPanel.Controls.Add(applyButton);

            resetButton.Dock = DockStyle.Bottom;
            resetButton.Click += async (_, __) => await ResetFilterPanel();
            filterPanel.Controls.Add(resetButton);
        }
        private void LogicModeChanged(object sender, EventArgs e)
        {
            if (isUpdatingUI)
                return;

            if (!((RadioButton)sender).Checked)
                return;

            if (currentColumn == null)
                return;

            if (!columnSelections.TryGetValue(currentColumn, out var sel))
                sel = new ColumnFilterSelection();

            PopulateValueList(sel);
        }
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Category("Behavior")]
        [Description("If true, prevents editing cells but still allows filtering and searching.")]
        public bool ReadOnly
        {
            get => grid.ReadOnly;
            set
            {
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
            get => baseTitle;
            set
            {
                baseTitle = value;
                titleLabel.Text = value;
                UpdateTitleInfo();
            }
        }
        private void UpdateTitleInfo()
        {
            int total = source?.Count ?? 0;
            int visible = filtered?.Count ?? 0;

            if (total == visible)
            {
                rowCountLabel.Text = $"{total:N0} rows";
                filterIcon.Visible = false;
                clearFiltersButton.Visible = false;
            }
            else
            {
                rowCountLabel.Text = $"{visible:N0} rows (filtered from {total:N0})";
                filterIcon.Visible = true;
                clearFiltersButton.Visible = true;
            }

            UpdateTitleStyle();
        }
        private async Task ClearAllFilters()
        {
            searchBox.Clear();
            filters.Clear();
            columnSelections.Clear();
            minBox.Clear();
            maxBox.Clear();
            UpdateColumnHeaderStyles();
            UpdateTitleStyle();

            await ApplyFiltersAsync();
        }
        public void SetData(IEnumerable<T> data)
        {
            source = data.ToList();
            filtered = source;

            BuildColumnCache();

            grid.DataSource = new BindingList<T>(filtered);

            UpdateTitleInfo();
        }
        private void MinMaxBox_TextChanged(object sender, EventArgs e)
        {
            if (isUpdatingUI)
                return;
            // Update checkbox list filtered items
            FilterCheckboxList();
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
        private void UpdateUI(Action action)
        {
            if (InvokeRequired)
            {
                Invoke(() => UpdateUI(action));
                return;
            }

            isUpdatingUI = true;
            try
            {
                action();
            }
            finally
            {
                isUpdatingUI = false;
            }
        }
        private void ShowFilterPanel(string column, Point headerLocation)
        {
            bool enable = filters.Count > 0;
            andButton.Enabled = orButton.Enabled = enable;
            andButton.ForeColor = enable ? Color.Black : Color.Gray;
            orButton.ForeColor = enable ? Color.Black : Color.Gray;

            currentColumn = column;

            if (!columnSelections.ContainsKey(column))
                columnSelections[column] = new ColumnFilterSelection();

            UpdateUI(() =>
            {
                minBox.Text = "";
                maxBox.Text = "";
            });

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
        private bool EvaluateColumn(object val, ColumnFilterSelection sel)
        {
            bool hasValueFilter = sel.SelectedValues.Count > 0;
            bool hasRangeFilter = sel.Min.HasValue || sel.Max.HasValue;

            bool valueMatch = true;
            bool rangeMatch = true;

            if (hasValueFilter)
                valueMatch = val != null && sel.SelectedValues.Contains(val);

            if (hasRangeFilter)
            {
                if (val == null || !double.TryParse(val.ToString(), out var d))
                    rangeMatch = false;
                else
                {
                    if (sel.Min.HasValue && d < sel.Min.Value)
                        rangeMatch = false;

                    if (sel.Max.HasValue && d > sel.Max.Value)
                        rangeMatch = false;
                }
            }

            if (hasValueFilter && hasRangeFilter)
                return sel.UseOrLogic ? (valueMatch || rangeMatch) : (valueMatch && rangeMatch);

            if (hasValueFilter)
                return valueMatch;

            if (hasRangeFilter)
                return rangeMatch;

            return true;
        }
        private async Task ApplyFiltersAsync()
        {
            string search = searchBox.Text.ToLower();

            var result = await Task.Run(() =>
            {
                var andColumns = columnSelections
                    .Where(x => !x.Value.UseOrLogic)
                    .ToList();

                var orColumns = columnSelections
                    .Where(x => x.Value.UseOrLogic)
                    .ToList();

                var andRows = source.Where(row =>
                {
                    foreach (var kv in andColumns)
                    {
                        var val = getters[kv.Key](row);

                        if (!EvaluateColumn(val, kv.Value))
                            return false;
                    }

                    return true;
                });

                IEnumerable<T> finalRows = andRows;

                if (orColumns.Count > 0)
                {
                    var orRows = source.Where(row =>
                    {
                        foreach (var kv in orColumns)
                        {
                            var val = getters[kv.Key](row);

                            if (EvaluateColumn(val, kv.Value))
                                return true;
                        }

                        return false;
                    });

                    finalRows = andRows.Concat(orRows).Distinct();
                }

                // GLOBAL SEARCH
                if (!string.IsNullOrWhiteSpace(search))
                {
                    finalRows = finalRows.Where(item =>
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

                return finalRows.ToList();
            });

            filtered = result;

            grid.Invoke(() =>
            {
                grid.DataSource = new BindingList<T>(filtered);
                UpdateTitleInfo();
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
            if (isUpdatingUI)
                return;

            BeginInvoke(new Action(() =>
            {
                bool allChecked = true;

                for (int i = 0; i < valueList.Items.Count; i++)
                {
                    if (!valueList.GetItemChecked(i))
                    {
                        allChecked = false;
                        break;
                    }
                }

                UpdateUI(() =>
                {
                    selectAllCheckBox.Checked = allChecked;
                });

                if (!columnSelections.ContainsKey(currentColumn))
                    columnSelections[currentColumn] = new ColumnFilterSelection();

                var sel = columnSelections[currentColumn];

                sel.SelectedValues = valueList.CheckedItems
                    .Cast<object>()
                    .Where(x => x is ValueItem)
                    .Cast<ValueItem>()
                    .Select(x => x.Value)
                    .ToHashSet();
            }));
        }
        private void SelectAllCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (isUpdatingUI)
                return;

            bool check = selectAllCheckBox.Checked;

            UpdateUI(() =>
            {
                for (int i = 0; i < valueList.Items.Count; i++)
                    valueList.SetItemChecked(i, check);
            });

            if (!columnSelections.ContainsKey(currentColumn))
                columnSelections[currentColumn] = new ColumnFilterSelection();

            var sel = columnSelections[currentColumn];

            if (check)
                sel.SelectedValues.Clear();
            else
                sel.SelectedValues = valueList.Items
                    .Cast<ValueItem>()
                    .Select(x => x.Value)
                    .ToHashSet();
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
        private void FilterCheckboxList()
        {
            if (currentColumn == null) return;

            if (!columnSelections.TryGetValue(currentColumn, out var sel))
                sel = new ColumnFilterSelection();

            IEnumerable<T> data = source;

            foreach (var kv in filters)
            {
                if (kv.Key == currentColumn) continue;
                foreach (var f in kv.Value)
                    data = f.Apply(data);
            }

            var counts = new Dictionary<object, int>();

            foreach (var item in data)
            {
                var v = getters[currentColumn](item);
                if (v == null) continue;

                if (double.TryParse(v.ToString(), out var d))
                {
                    double? min = double.TryParse(minBox.Text, out var minVal) ? minVal : null;
                    double? max = double.TryParse(maxBox.Text, out var maxVal) ? maxVal : null;

                    if (min.HasValue && d < min.Value) continue;
                    if (max.HasValue && d > max.Value) continue;
                }

                if (counts.ContainsKey(v))
                    counts[v]++;
                else
                    counts[v] = 1;
            }

            var values = counts.Keys.OrderBy(v => v?.ToString()).ToList();

            UpdateUI(() =>
            {
                valueList.Items.Clear();

                selectAllCheckBox.Checked =
                    sel.SelectedValues.Count == 0 ||
                    sel.SelectedValues.Count == values.Count;

                foreach (var v in values)
                {
                    var item = new ValueItem
                    {
                        Value = v,
                        Count = counts[v]
                    };

                    bool isChecked =
                        sel.SelectedValues.Count == 0 || sel.SelectedValues.Contains(v);

                    valueList.Items.Add(item, isChecked);
                }
            });
        }
        private void PopulateValueList(ColumnFilterSelection sel)
        {
            if (currentColumn == null) return;

            IEnumerable<T> data;

            if (orButton.Checked)
            {
                var andFiltered = GetDataFilteredExcept(currentColumn);

                IEnumerable<T> orFiltered = Enumerable.Empty<T>();

                if (columnSelections.TryGetValue(currentColumn, out var currentSel))
                {
                    orFiltered = source.Where(row =>
                    {
                        var val = getters[currentColumn](row);

                        bool valueMatch = true;
                        bool rangeMatch = true;

                        if (currentSel.SelectedValues.Count > 0)
                            valueMatch = val != null && currentSel.SelectedValues.Contains(val);

                        if (currentSel.Min.HasValue || currentSel.Max.HasValue)
                        {
                            if (val == null || !double.TryParse(val.ToString(), out var d))
                                rangeMatch = false;
                            else
                            {
                                if (currentSel.Min.HasValue && d < currentSel.Min.Value) rangeMatch = false;
                                if (currentSel.Max.HasValue && d > currentSel.Max.Value) rangeMatch = false;
                            }
                        }

                        bool hasValueFilter = currentSel.SelectedValues.Count > 0;
                        bool hasRangeFilter = currentSel.Min.HasValue || currentSel.Max.HasValue;

                        if (hasValueFilter && hasRangeFilter)
                            return valueMatch || rangeMatch;

                        if (hasValueFilter) return valueMatch;
                        if (hasRangeFilter) return rangeMatch;

                        return true;
                    });
                }

                data = andFiltered.Concat(orFiltered).Distinct();
            }
            else
            {
                data = GetDataFilteredExcept(currentColumn);
            }

            var counts = new Dictionary<object, int>();

            foreach (var item in data)
            {
                var v = getters[currentColumn](item);
                if (v == null) continue;

                if (decimal.TryParse(v.ToString(), out var d))
                {
                    if (sel.Min.HasValue && d < (decimal)sel.Min.Value) continue;
                    if (sel.Max.HasValue && d > (decimal)sel.Max.Value) continue;
                }

                if (counts.TryGetValue(v, out var c))
                    counts[v] = c + 1;
                else
                    counts[v] = 1;
            }

            var values = counts.Keys
                .OrderBy(v => v is IComparable ? 0 : 1)
                .ThenBy(v => v)
                .ToList();

            UpdateUI(() =>
            {
                valueList.Items.Clear();

                selectAllCheckBox.Checked =
                    sel.SelectedValues.Count == 0 ||
                    sel.SelectedValues.Count == values.Count;

                foreach (var v in values)
                {
                    var item = new ValueItem
                    {
                        Value = v,
                        Count = counts[v]
                    };

                    bool isChecked =
                        sel.SelectedValues.Count == 0 || sel.SelectedValues.Contains(v);

                    valueList.Items.Add(item, isChecked);
                }

                minBox.Text = sel.Min?.ToString() ?? "";
                maxBox.Text = sel.Max?.ToString() ?? "";
            });
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
                Min = double.TryParse(minBox.Text, out var min) ? min : (double?)null,
                Max = double.TryParse(maxBox.Text, out var max) ? max : (double?)null,
                UseOrLogic = orButton.Checked
            };

            columnSelections[currentColumn] = sel;

            ClearFilters(currentColumn);

            if (sel.SelectedValues.Count > 0 || sel.Min.HasValue || sel.Max.HasValue)
            {
                AddFilter(currentColumn,
                    new CombinedColumnFilter<T>(
                        getters[currentColumn],
                        sel.SelectedValues,
                        sel.Min,
                        sel.Max,
                        sel.UseOrLogic));
            }

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
            selectAllCheckBox.Checked = true;

            columnSelections.Remove(currentColumn);
            ClearFilters(currentColumn);
            UpdateColumnHeaderStyles();
            UpdateTitleStyle();
            await ApplyFiltersAsync();
        }

    }
}