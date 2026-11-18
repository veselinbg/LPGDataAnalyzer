using LPGDbAnalyzer.Data;
using LPGDbAnalyzer.Services;
using System.Globalization;

namespace LPGDbAnalyzer.Forms
{
    /// <summary>
    /// Check screen for a fuel table: the screenshot on the left, the parsed values on the right.
    /// <para>
    /// Row 0 of the grid holds the RPM breakpoints and column 0 the injection-time breakpoints, so the
    /// axes are edited like any other cell. Values use the LPGDataAnalyzer heatmap; black = empty or invalid, violet frame = far from its neighbours
    /// (often an OCR error, sometimes a real dip). Saving needs no black cells and the user's confirmation.
    /// </para>
    /// </summary>
    public sealed class FuelTableForm : Form
    {
        // Used only when neither the image nor an earlier table of the car gives the axes.
        private static readonly double[] DefaultRpmAxis = [700, 1000, 1400, 1800, 2200, 2600, 3000, 3400, 4000, 4700, 5400, 6200];
        private static readonly double[] DefaultInjectionAxis = [1.9, 2.4, 2.9, 3.4, 4.0, 4.8, 5.8, 7.0, 8.3, 9.6, 10.8, 12.0];

        /// <summary>A value this far (share) from the median of its neighbours gets a violet frame.</summary>
        private const double NeighbourTolerance = 0.20;

        // The values use the blue-white-red heatmap, so errors and warnings use colours outside it.
        private static readonly Color AxisBack = Color.Gainsboro;
        private static readonly Color ErrorBack = Color.Black;
        private static readonly Color WarningFrame = Color.DarkViolet;

        /// <summary>Cells far from their neighbours, framed by <see cref="Grid_CellPainting"/>.</summary>
        private readonly HashSet<(int Column, int Row)> _warningCells = [];

        private readonly Database _database;
        private readonly Car _car;
        private readonly FuelTable? _existing;
        private readonly FuelTable? _template;
        private readonly byte[]? _image;
        private readonly string? _imageFileName;

        private readonly PictureBox _picture = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White };
        private readonly DataGridView _grid = new();
        private readonly TextBox _name = new() { Dock = DockStyle.Fill };
        private readonly DateTimePicker _date = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm", Width = 150 };
        private readonly TextBox _notes = new() { Dock = DockStyle.Fill, Multiline = true, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical };
        private SplitContainer? _split;
        private readonly Label _status = new() { Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft };
        private readonly CheckBox _confirm = new() { Text = "I compared every value and both axes with the image", AutoSize = true };
        private readonly Button _save = new() { Text = "Save", Width = 90, Enabled = false };

        private readonly Font _axisFont = new("Consolas", 11f, FontStyle.Bold);

        private int _errorCount;
        private bool _filling;

        public FuelTable? Saved { get; private set; }

        /// <summary>New fuel table read from a screenshot.</summary>
        public static FuelTableForm FromImage(Database database, Car car, string imagePath, FuelTable? template) =>
            new(database, car, null, template, File.ReadAllBytes(imagePath), Path.GetFileName(imagePath));

        /// <summary>Check or correct a saved fuel table.</summary>
        public static FuelTableForm Edit(Database database, Car car, FuelTable table) =>
            new(database, car, table, null, database.GetFuelTableImage(table.Id), table.ImageFileName);

        protected override void OnLoad(EventArgs e)
        {
            // Buttons grow to fit their text (Windows scaling above 100%).
            LPGDataAnalyzer.Controls.ControlSizing.FitButtons(this);
            base.OnLoad(e);
            if (_split is not null)
                _split.SplitterDistance = Math.Max(_split.Panel1MinSize, _split.Height * 2 / 5);
        }

        private FuelTableForm(Database database, Car car, FuelTable? existing, FuelTable? template, byte[]? image, string? imageFileName)
        {
            _database = database;
            _car = car;
            _existing = existing;
            _template = template;
            _image = image;
            _imageFileName = imageFileName;

            Text = existing is null ? $"New fuel table - {car.Name}" : $"Fuel table - {car.Name} - {existing.Name}";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1300, 980);
            MinimumSize = new Size(800, 700);

            BuildLayout();
            SetUpGrid();

            if (image is not null)
            {
                using var ms = new MemoryStream(image);
                _picture.Image = new Bitmap(Image.FromStream(ms));
            }

            if (existing is not null)
            {
                _name.Text = existing.Name;
                _date.Value = existing.CreatedAt;
                _notes.Text = existing.Notes;
                Fill(existing.RpmAxis.Select(v => (double?)v).ToArray(),
                     existing.InjectionAxis.Select(v => (double?)v).ToArray(),
                     existing.Values.Select(r => r.Select(v => (double?)v).ToArray()).ToArray());
            }
            else
            {
                _name.Text = Path.GetFileNameWithoutExtension(imageFileName ?? "Fuel table");
                _date.Value = DateTime.Now;
                Shown += async (_, _) => await ReadImageAsync();
            }
        }

        // ---------------- Layout ----------------

        private void BuildLayout()
        {
            // Image on top, grid below (the screenshot is wide). The splitter is placed in OnLoad, when the size is known.
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            _split = split;
            split.Panel1.Controls.Add(_picture);

            var top = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 4, RowCount = 2, Padding = new Padding(4) };
            top.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            // Notes: two text lines.
            top.RowStyles.Add(new RowStyle(SizeType.Absolute, _notes.Font.Height * 2 + 14));
            top.Height = top.Padding.Vertical + _name.PreferredHeight + 6 + _notes.Font.Height * 2 + 14;
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            top.Controls.Add(new Label { Text = "Name", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            top.Controls.Add(_name, 1, 0);
            top.Controls.Add(new Label { Text = "Set in ECU", AutoSize = true, Anchor = AnchorStyles.Left }, 2, 0);
            top.Controls.Add(_date, 3, 0);
            top.Controls.Add(new Label { Text = "Notes", AutoSize = true, Anchor = AnchorStyles.Left | AnchorStyles.Top, Margin = new Padding(3, 6, 3, 3) }, 0, 1);
            top.Controls.Add(_notes, 1, 1);
            top.SetColumnSpan(_notes, 3);

            var edit = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(2) };
            edit.Controls.Add(MakeButton("Add row", (_, _) => { _grid.Rows.Add(); Revalidate(); }));
            edit.Controls.Add(MakeButton("Remove last row", (_, _) => { if (_grid.RowCount > 3) _grid.Rows.RemoveAt(_grid.RowCount - 1); Revalidate(); }));
            edit.Controls.Add(MakeButton("Add column", (_, _) => { AddColumn(); Revalidate(); }));
            edit.Controls.Add(MakeButton("Remove last column", (_, _) => { if (_grid.ColumnCount > 3) _grid.Columns.RemoveAt(_grid.ColumnCount - 1); Revalidate(); }));
            if (_image is not null)
                edit.Controls.Add(MakeButton("Read image again", async (_, _) => await ReadImageAsync()));

            var legend = new Label
            {
                Dock = DockStyle.Top,
                Height = 36,
                Text = "Row 0 = RPM breakpoints, column 0 = injection time (ms). Black = empty/invalid (must be fixed), violet frame = far from " +
                       "its neighbours (check it, it may be a real dip). Ctrl+V pastes text from the clipboard at the selected cell.",
            };

            var bottom = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 64, ColumnCount = 3, Padding = new Padding(4) };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var cancel = new Button { Text = "Cancel", Width = 90, DialogResult = DialogResult.Cancel };
            bottom.Controls.Add(_status, 0, 0);
            bottom.SetColumnSpan(_status, 3);
            bottom.Controls.Add(_confirm, 0, 1);
            bottom.Controls.Add(_save, 1, 1);
            bottom.Controls.Add(cancel, 2, 1);

            _confirm.CheckedChanged += (_, _) => UpdateSaveButton();
            _save.Click += Save_Click;
            CancelButton = cancel;

            _grid.Dock = DockStyle.Fill;
            split.Panel2.Controls.Add(_grid);
            split.Panel2.Controls.Add(legend);
            split.Panel2.Controls.Add(edit);
            split.Panel2.Controls.Add(top);
            split.Panel2.Controls.Add(bottom);

            Controls.Add(split);
        }

        private static Button MakeButton(string text, EventHandler click)
        {
            var b = new Button { Text = text, AutoSize = true };
            b.Click += click;
            return b;
        }

        private void SetUpGrid()
        {
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.ColumnHeadersVisible = false;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.DefaultCellStyle.Font = new Font("Consolas", 11f);
            _grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _grid.RowTemplate.Height = 28;

            _grid.CellValueChanged += (_, _) => { if (!_filling) Revalidate(); };
            _grid.KeyDown += Grid_KeyDown;
            _grid.CellPainting += Grid_CellPainting;
        }

        private void AddColumn()
        {
            var column = new DataGridViewTextBoxColumn { SortMode = DataGridViewColumnSortMode.NotSortable };
            _grid.Columns.Add(column);
        }

        // ---------------- Fill / read the grid ----------------

        private void Fill(double?[]? rpmAxis, double?[]? injectionAxis, double?[][] values)
        {
            _filling = true;
            try
            {
                int columns = Math.Max(rpmAxis?.Length ?? 0, values.Length == 0 ? 0 : values.Max(r => r.Length));
                int rows = Math.Max(injectionAxis?.Length ?? 0, values.Length);

                if (columns == 0) columns = DefaultRpmAxis.Length;
                if (rows == 0) rows = DefaultInjectionAxis.Length;

                // Axes the image did not have: from the car's latest table when it has the same size, else the defaults.
                rpmAxis ??= DefaultAxis(_template?.RpmAxis, DefaultRpmAxis, columns) ?? [];
                injectionAxis ??= DefaultAxis(_template?.InjectionAxis, DefaultInjectionAxis, rows) ?? [];

                _grid.Rows.Clear();
                _grid.Columns.Clear();

                for (int c = 0; c <= columns; c++)
                    AddColumn();
                _grid.Rows.Add(rows + 1);

                _grid[0, 0].Value = "ms \\ rpm";
                _grid[0, 0].ReadOnly = true;

                for (int c = 0; c < columns; c++)
                    _grid[c + 1, 0].Value = Format(c < rpmAxis.Length ? rpmAxis[c] : null);

                for (int r = 0; r < rows; r++)
                {
                    _grid[0, r + 1].Value = Format(r < injectionAxis.Length ? injectionAxis[r] : null);

                    for (int c = 0; c < columns; c++)
                        _grid[c + 1, r + 1].Value = Format(r < values.Length && c < values[r].Length ? values[r][c] : null);
                }
            }
            finally
            {
                _filling = false;
            }

            Revalidate();
        }

        private static double?[]? DefaultAxis(double[]? template, double[] fallback, int length)
        {
            var source = template is { Length: > 0 } && template.Length == length ? template
                : fallback.Length == length ? fallback
                : null;

            return source?.Select(v => (double?)v).ToArray();
        }

        private static string Format(double? value) =>
            value?.ToString("0.##", CultureInfo.InvariantCulture) ?? "";

        private double? CellNumber(int column, int row)
        {
            var text = _grid[column, row].Value?.ToString()?.Trim().Replace(',', '.');
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : null;
        }

        /// <summary>Draws a frame around values to check, on top of their heatmap colour.</summary>
        private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || !_warningCells.Contains((e.ColumnIndex, e.RowIndex)))
                return;

            e.Paint(e.CellBounds, DataGridViewPaintParts.All);

            using var pen = new Pen(WarningFrame, 3);
            var r = e.CellBounds;
            e.Graphics!.DrawRectangle(pen, r.X + 2, r.Y + 2, r.Width - 5, r.Height - 5);
            e.Handled = true;
        }

        // ---------------- Validation ----------------

        private void Revalidate()
        {
            int columns = _grid.ColumnCount;
            int rows = _grid.RowCount;
            if (columns < 2 || rows < 2)
                return;

            int errors = 0;
            _warningCells.Clear();

            var numbers = new double?[columns, rows];
            for (int c = 0; c < columns; c++)
                for (int r = 0; r < rows; r++)
                    numbers[c, r] = c == 0 && r == 0 ? null : CellNumber(c, r);

            // Values coloured as in LPGDataAnalyzer; invalid values are left out of the scale.
            var heat = Enumerable.Range(1, rows - 1)
                .Select(r => Enumerable.Range(1, columns - 1)
                    .Select(c => ValueProblem(numbers, c, r) is null ? numbers[c, r] : null)
                    .ToArray())
                .ToArray();
            Heatmap.ColorizeValues(_grid, heat, firstColumn: 1, firstRow: 1);

            for (int c = 0; c < columns; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    var cell = _grid[c, r];
                    bool isAxis = c == 0 || r == 0;

                    if (isAxis)
                    {
                        Heatmap.Apply(cell, AxisBack);
                        cell.Style.Font = _axisFont;
                    }

                    if (c == 0 && r == 0)
                        continue;

                    string? problem = isAxis ? AxisProblem(numbers, c, r) : ValueProblem(numbers, c, r);

                    if (problem is not null)
                    {
                        errors++;
                        Heatmap.Apply(cell, ErrorBack);
                        cell.ToolTipText = problem;
                    }
                    else if (!isAxis && ValueWarning(numbers, c, r) is string warning)
                    {
                        _warningCells.Add((c, r));
                        cell.ToolTipText = warning;
                    }
                    else
                    {
                        cell.ToolTipText = "";
                    }
                }
            }

            int warnings = _warningCells.Count;
            _grid.Invalidate();

            _errorCount = errors;
            _status.Text = $"{rows - 1} rows x {columns - 1} columns.  " +
                           (errors > 0 ? $"{errors} black cell(s) must be fixed.  " : "No invalid cells.  ") +
                           (warnings > 0 ? $"{warnings} framed cell(s) to check against the image." : "");
            _status.ForeColor = errors > 0 ? Color.DarkRed : Color.Black;
            UpdateSaveButton();
        }

        private static string? AxisProblem(double?[,] n, int c, int r)
        {
            if (n[c, r] is not double v)
                return "Empty or not a number.";
            if (v <= 0)
                return "Must be above 0.";

            // Breakpoints must increase along the axis.
            if (r == 0 && c > 1 && n[c - 1, 0] is double left && v <= left)
                return "Must be larger than the breakpoint before it.";
            if (c == 0 && r > 1 && n[0, r - 1] is double up && v <= up)
                return "Must be larger than the breakpoint before it.";

            return null;
        }

        private static string? ValueProblem(double?[,] n, int c, int r)
        {
            if (n[c, r] is not double v)
                return "Empty or not a number.";
            if (v <= 0 || v > 999)
                return "Must be between 1 and 999.";
            return null;
        }

        private static string? ValueWarning(double?[,] n, int c, int r)
        {
            double v = n[c, r]!.Value;

            var neighbours = new List<double>(4);
            if (c > 1 && n[c - 1, r] is double a) neighbours.Add(a);
            if (c + 1 < n.GetLength(0) && n[c + 1, r] is double b) neighbours.Add(b);
            if (r > 1 && n[c, r - 1] is double d) neighbours.Add(d);
            if (r + 1 < n.GetLength(1) && n[c, r + 1] is double e) neighbours.Add(e);

            if (neighbours.Count < 2)
                return null;

            neighbours.Sort();
            double median = neighbours.Count % 2 == 1
                ? neighbours[neighbours.Count / 2]
                : (neighbours[neighbours.Count / 2 - 1] + neighbours[neighbours.Count / 2]) / 2;

            double share = (v - median) / median;
            return Math.Abs(share) > NeighbourTolerance
                ? $"{share:+0%;-0%} from the neighbours' median ({median:0}). Check it against the image."
                : null;
        }

        private void UpdateSaveButton() => _save.Enabled = _errorCount == 0 && _confirm.Checked;

        // ---------------- OCR ----------------

        private async Task ReadImageAsync()
        {
            if (_image is null)
                return;

            _status.Text = "Reading the image...";
            _status.ForeColor = Color.Black;
            UseWaitCursor = true;
            _confirm.Checked = false;

            try
            {
                var image = _image;
                var result = await Task.Run(() => FuelTableOcr.Read(image, AppSettings.TessdataPath));
                Fill(result.RpmAxis, result.InjectionAxis, result.Values);

                if (result.Values.Length == 0)
                    MessageBox.Show(this, "No numbers were found in the image. Enter or paste the values.", "OCR",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Fill(null, null, []);
                MessageBox.Show(this, "The image could not be read: " + ex.Message + "\nEnter or paste the values.", "OCR",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        // ---------------- Keyboard: paste / delete ----------------

        private void Grid_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V)
            {
                PasteFromClipboard();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Delete)
            {
                foreach (DataGridViewCell cell in _grid.SelectedCells)
                    if (!cell.ReadOnly)
                        cell.Value = "";
                e.Handled = true;
            }
        }

        /// <summary>Pastes rows of numbers separated by tabs, spaces or semicolons, starting at the current cell.</summary>
        private void PasteFromClipboard()
        {
            if (!Clipboard.ContainsText() || _grid.CurrentCell is null)
                return;

            var lines = Clipboard.GetText()
                .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Split(['\t', ' ', ';'], StringSplitOptions.RemoveEmptyEntries))
                .Where(l => l.Length > 0)
                .ToArray();

            int startColumn = _grid.CurrentCell.ColumnIndex;
            int startRow = _grid.CurrentCell.RowIndex;

            _filling = true;
            try
            {
                for (int r = 0; r < lines.Length && startRow + r < _grid.RowCount; r++)
                    for (int c = 0; c < lines[r].Length && startColumn + c < _grid.ColumnCount; c++)
                        if (!_grid[startColumn + c, startRow + r].ReadOnly)
                            _grid[startColumn + c, startRow + r].Value = lines[r][c];
            }
            finally
            {
                _filling = false;
            }

            Revalidate();
        }

        // ---------------- Save ----------------

        private void Save_Click(object? sender, EventArgs e)
        {
            try
            {
                int columns = _grid.ColumnCount - 1;
                int rows = _grid.RowCount - 1;

                var table = new FuelTable
                {
                    Id = _existing?.Id ?? 0,
                    CarId = _car.Id,
                    Name = _name.Text.Trim(),
                    CreatedAt = _date.Value,
                    Notes = string.IsNullOrWhiteSpace(_notes.Text) ? null : _notes.Text.Trim(),
                    RpmAxis = Enumerable.Range(1, columns).Select(c => CellNumber(c, 0)!.Value).ToArray(),
                    InjectionAxis = Enumerable.Range(1, rows).Select(r => CellNumber(0, r)!.Value).ToArray(),
                    Values = Enumerable.Range(1, rows)
                        .Select(r => Enumerable.Range(1, columns).Select(c => CellNumber(c, r)!.Value).ToArray())
                        .ToArray(),
                    // On update the stored image is kept (null = unchanged).
                    Image = _existing is null ? _image : null,
                    ImageFileName = _existing is null ? _imageFileName : null,
                };

                if (_existing is { LogCount: > 0 } &&
                    MessageBox.Show(this,
                        $"{_existing.LogCount} log file(s) were recorded with this table. Change it only to fix reading errors.\n\nSave the changes?",
                        "Fuel table has logs", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                _database.SaveFuelTable(table);
                Saved = table;
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Cannot save the fuel table", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _picture.Image?.Dispose();
                _axisFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
