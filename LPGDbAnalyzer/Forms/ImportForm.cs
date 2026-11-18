using LPGDataAnalyzer.Models;
using LPGDbAnalyzer.Data;
using LPGDbAnalyzer.Services;
using System.Globalization;

namespace LPGDbAnalyzer.Forms
{
    /// <summary>
    /// Import of real-time logs: 1. select or add the car, 2. select or add the fuel table that was in
    /// the ECU while the logs were recorded, 3. add the log files, give each its ECU temperature
    /// tables (or mark them "same as previous") and import them.
    /// </summary>
    public sealed class ImportForm : Form
    {
        /// <summary>One file in the list.</summary>
        private sealed class ImportItem(string path)
        {
            public string Path { get; } = path;
            public LogFileReader.Result? Result { get; set; }
            public bool CanImport { get; set; }

            /// <summary>Take the tables of the log recorded just before this one (same car).</summary>
            public bool SameAsPrevious { get; set; } = true;
            public int[]? OwnGas { get; set; }
            public int[]? OwnReducer { get; set; }

            /// <summary>Tables that will be stored (own values, or the previous log's); null = unknown.</summary>
            public int[]? Gas { get; set; }
            public int[]? Reducer { get; set; }
        }

        private readonly Database _database;

        private readonly ComboBox _cars = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 380 };
        private readonly ComboBox _tables = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 380 };
        private readonly Label _tableInfo = new() { AutoSize = true, ForeColor = Color.DimGray };
        private readonly ListView _files = new()
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            HideSelection = false,
        };

        // Temperature tables of the selected file(s).
        private readonly CheckBox _sameAsPrevious = new() { Text = "Same as previous log (the log of this car recorded just before)", AutoSize = true };
        private readonly TextBox _gas = new() { Width = 260 };
        private readonly TextBox _reducer = new() { Width = 260 };
        private readonly Button _applyCoefficients = new() { Text = "Apply to selected", AutoSize = true };
        private readonly GroupBox _coefficientsBox = new() { Text = "ECU temperature tables of the selected file(s)", Dock = DockStyle.Bottom, Height = 132 };

        private readonly Button _import = new() { Text = "Import", Width = 110, Enabled = false };
        private readonly Label _status = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };

        private string? _lastFolder;
        private bool _showingSelection;

        /// <summary>True when at least one file was imported (the caller refreshes its lists).</summary>
        public bool Imported { get; private set; }

        protected override void OnLoad(EventArgs e)
        {
            // Buttons grow to fit their text (Windows scaling above 100%).
            LPGDataAnalyzer.Controls.ControlSizing.FitButtons(this);
            base.OnLoad(e);
        }

        public ImportForm(Database database, long? carId = null, long? fuelTableId = null)
        {
            _database = database;

            Text = "Import real-time logs";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1150, 720);
            MinimumSize = new Size(800, 560);

            BuildLayout();

            _cars.SelectedIndexChanged += (_, _) => { LoadTables(null); ResolveCoefficients(); };
            _tables.SelectedIndexChanged += (_, _) => { ShowTableInfo(); UpdateImportButton(); };

            LoadCars(carId);
            if (fuelTableId is long id)
                SelectTable(id);

            ShowSelectedCoefficients();
        }

        private Car? SelectedCar => _cars.SelectedItem as Car;
        private FuelTable? SelectedTable => _tables.SelectedItem as FuelTable;

        private IEnumerable<ImportItem> Items => _files.Items.Cast<ListViewItem>().Select(i => (ImportItem)i.Tag!);

        // ---------------- Layout ----------------

        private void BuildLayout()
        {
            var carBox = new GroupBox { Text = "1. Car", Dock = DockStyle.Top, Height = 60 };
            var carRow = new FlowLayoutPanel { Dock = DockStyle.Fill };
            carRow.Controls.Add(_cars);
            carRow.Controls.Add(MakeButton("New car...", NewCar_Click));
            carRow.Controls.Add(MakeButton("Edit car...", EditCar_Click));
            carBox.Controls.Add(carRow);

            var tableBox = new GroupBox { Text = "2. Fuel table that was in the ECU while the logs were recorded", Dock = DockStyle.Top, Height = 84 };
            var tableRow = new FlowLayoutPanel { Dock = DockStyle.Fill };
            tableRow.Controls.Add(_tables);
            tableRow.Controls.Add(MakeButton("New from image...", NewTable_Click));
            tableRow.Controls.Add(MakeButton("Check / edit...", EditTable_Click));
            tableRow.SetFlowBreak(tableRow.Controls[^1], true);
            tableRow.Controls.Add(_tableInfo);
            tableBox.Controls.Add(tableRow);

            var fileBox = new GroupBox { Text = "3. Log files (*.txt from the LPG ECU software) and their ECU temperature tables", Dock = DockStyle.Fill };
            _files.Columns.Add("File", 170);
            _files.Columns.Add("Recorded", 115);
            _files.Columns.Add("Rows", 60, HorizontalAlignment.Right);
            _files.Columns.Add("On LPG", 60, HorizontalAlignment.Right);
            _files.Columns.Add("LPG temp table", 150);
            _files.Columns.Add("Reducer temp table", 150);
            _files.Columns.Add("Status", 330);
            _files.SelectedIndexChanged += (_, _) => ShowSelectedCoefficients();

            var fileButtons = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34 };
            fileButtons.Controls.Add(MakeButton("Add files...", AddFiles_Click));
            fileButtons.Controls.Add(MakeButton("Remove selected", (_, _) =>
            {
                foreach (ListViewItem item in _files.SelectedItems)
                    _files.Items.Remove(item);
                ResolveCoefficients();
            }));

            // ---- Temperature tables editor ----
            var editor = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(4) };
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            editor.Controls.Add(_sameAsPrevious, 0, 0);
            editor.SetColumnSpan(_sameAsPrevious, 2);
            editor.Controls.Add(_applyCoefficients, 2, 0);
            editor.Controls.Add(new Label { Text = "LPG temperature", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            editor.Controls.Add(_gas, 1, 1);
            editor.Controls.Add(new Label { Text = BandHint(Settings.GasTemperatureRanges), AutoSize = true, ForeColor = Color.DimGray, Anchor = AnchorStyles.Left }, 2, 1);
            editor.Controls.Add(new Label { Text = "Reducer temperature", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
            editor.Controls.Add(_reducer, 1, 2);
            editor.Controls.Add(new Label { Text = BandHint(Settings.ReductorTemperatureRanges), AutoSize = true, ForeColor = Color.DimGray, Anchor = AnchorStyles.Left }, 2, 2);
            _coefficientsBox.Controls.Add(editor);

            _sameAsPrevious.CheckedChanged += (_, _) =>
            {
                _gas.Enabled = _reducer.Enabled = !_sameAsPrevious.Checked;
                if (!_showingSelection)
                    ApplyCoefficients(showErrors: false);
            };
            _applyCoefficients.Click += (_, _) => ApplyCoefficients(showErrors: true);

            fileBox.Controls.Add(_files);
            fileBox.Controls.Add(fileButtons);
            fileBox.Controls.Add(_coefficientsBox);

            var bottom = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 42, ColumnCount = 3, Padding = new Padding(4) };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var close = new Button { Text = "Close", Width = 90, DialogResult = DialogResult.Cancel };
            bottom.Controls.Add(_status, 0, 0);
            bottom.Controls.Add(_import, 1, 0);
            bottom.Controls.Add(close, 2, 0);
            _import.Click += Import_Click;
            CancelButton = close;

            Controls.Add(fileBox);
            Controls.Add(tableBox);
            Controls.Add(carBox);
            Controls.Add(bottom);
        }

        private static string BandHint((int Min, int Max, string Label)[] ranges) =>
            $"{ranges.Length} whole numbers separated by commas, bands: " +
            string.Join(" ", ranges.Select(r => r.Label.Replace("Temp_", "")));

        private static Button MakeButton(string text, EventHandler click)
        {
            var b = new Button { Text = text, AutoSize = true };
            b.Click += click;
            return b;
        }

        // ---------------- Cars ----------------

        private void LoadCars(long? selectId)
        {
            var cars = _database.GetCars();
            _cars.Items.Clear();
            _cars.Items.AddRange(cars.ToArray());

            int index = cars.FindIndex(c => c.Id == selectId);
            _cars.SelectedIndex = index >= 0 ? index : cars.Count > 0 ? 0 : -1;

            if (cars.Count == 0)
                LoadTables(null);
        }

        private void NewCar_Click(object? sender, EventArgs e)
        {
            using var form = new CarForm(_database);
            if (form.ShowDialog(this) == DialogResult.OK)
                LoadCars(form.Car.Id);
        }

        private void EditCar_Click(object? sender, EventArgs e)
        {
            if (SelectedCar is not Car car)
                return;

            using var form = new CarForm(_database, car);
            if (form.ShowDialog(this) == DialogResult.OK)
                LoadCars(form.Car.Id);
        }

        // ---------------- Fuel tables ----------------

        private void LoadTables(long? selectId)
        {
            _tables.Items.Clear();

            if (SelectedCar is Car car)
            {
                var tables = _database.GetFuelTables(car.Id);
                _tables.Items.AddRange(tables.ToArray());

                int index = tables.FindIndex(t => t.Id == selectId);
                // Newest first: the default is the table currently in the ECU.
                _tables.SelectedIndex = index >= 0 ? index : tables.Count > 0 ? 0 : -1;
            }

            ShowTableInfo();
            UpdateImportButton();
        }

        private void SelectTable(long id)
        {
            for (int i = 0; i < _tables.Items.Count; i++)
                if (_tables.Items[i] is FuelTable t && t.Id == id)
                    _tables.SelectedIndex = i;
        }

        private void ShowTableInfo()
        {
            _tableInfo.Text = SelectedCar is null ? "Add a car first."
                : SelectedTable is not FuelTable t ? "No fuel table for this car yet: add one from the ECU screenshot."
                : $"RPM {t.RpmAxis.FirstOrDefault()}–{t.RpmAxis.LastOrDefault()}, injection {t.InjectionAxis.FirstOrDefault()}–{t.InjectionAxis.LastOrDefault()} ms, " +
                  $"values {t.Values.SelectMany(r => r).DefaultIfEmpty().Min()}–{t.Values.SelectMany(r => r).DefaultIfEmpty().Max()}, {t.LogCount} log(s)";
        }

        private void NewTable_Click(object? sender, EventArgs e)
        {
            if (SelectedCar is not Car car)
            {
                MessageBox.Show(this, "Select or add a car first.", "Fuel table");
                return;
            }

            using var dialog = new OpenFileDialog
            {
                Title = "Screenshot of the fuel table",
                Filter = "Images (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*",
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            // The car's latest table gives the axes when the screenshot has none.
            var template = _tables.Items.Count > 0 ? _tables.Items[0] as FuelTable : null;

            using var form = FuelTableForm.FromImage(_database, car, dialog.FileName, template);
            if (form.ShowDialog(this) == DialogResult.OK && form.Saved is FuelTable saved)
                LoadTables(saved.Id);
        }

        private void EditTable_Click(object? sender, EventArgs e)
        {
            if (SelectedCar is not Car car || SelectedTable is not FuelTable table)
                return;

            using var form = FuelTableForm.Edit(_database, car, table);
            if (form.ShowDialog(this) == DialogResult.OK)
                LoadTables(table.Id);
        }

        // ---------------- Files ----------------

        private async void AddFiles_Click(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Real-time logs",
                Filter = "Logs (*.txt)|*.txt|All files (*.*)|*.*",
                Multiselect = true,
                InitialDirectory = _lastFolder ?? "",
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            _lastFolder = Path.GetDirectoryName(dialog.FileNames[0]);

            foreach (var path in dialog.FileNames)
            {
                if (Items.Any(i => string.Equals(i.Path, path, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var import = new ImportItem(path);
                var item = new ListViewItem([Path.GetFileName(path), "", "", "", "", "", "Reading..."]) { Tag = import };
                _files.Items.Add(item);

                try
                {
                    var result = await Task.Run(() => LogFileReader.Read(path));
                    var where = _database.FindImported(result.Log.FileHash);

                    import.Result = result;
                    import.CanImport = where is null && result.Rows.Count > 0;

                    item.SubItems[1].Text = result.Log.RecordedAt.ToString("yyyy-MM-dd HH:mm");
                    item.SubItems[2].Text = result.Rows.Count.ToString();
                    item.SubItems[3].Text = result.LpgRows.ToString();
                    item.SubItems[6].Text = where is not null ? "Already imported: " + where
                        : result.Rows.Count == 0 ? "No data rows"
                        : result.SkippedLines > 0 ? $"Ready ({result.SkippedLines} unreadable line(s) skipped)"
                        : "Ready";

                    if (!import.CanImport)
                        item.ForeColor = Color.Gray;
                }
                catch (Exception ex)
                {
                    item.SubItems[6].Text = "Error: " + ex.Message;
                    item.ForeColor = Color.DarkRed;
                }
            }

            ResolveCoefficients();

            // New files are selected, so their tables can be checked right away.
            if (_files.SelectedItems.Count == 0 && _files.Items.Count > 0)
                _files.Items[^1].Selected = true;
        }

        // ---------------- Temperature tables ----------------

        /// <summary>
        /// Works out the tables of every file. "Same as previous" takes the tables of the log of the
        /// selected car recorded just before it: from the database or from a file above it in this list.
        /// </summary>
        private void ResolveCoefficients()
        {
            var history = SelectedCar is Car car
                ? _database.GetCarLogCoefficients(car.Id).Select(x => (x.RecordedAt, Gas: (int[]?)x.Gas, Reducer: (int[]?)x.Reducer)).ToList()
                : [];

            foreach (var import in Items.Where(i => i.Result is not null).OrderBy(i => i.Result!.Log.RecordedAt))
            {
                var recordedAt = import.Result!.Log.RecordedAt;

                if (import.SameAsPrevious)
                {
                    var previous = history.Where(h => h.RecordedAt < recordedAt).OrderBy(h => h.RecordedAt).LastOrDefault();
                    import.Gas = previous.Gas;
                    import.Reducer = previous.Reducer;
                }
                else
                {
                    import.Gas = import.OwnGas;
                    import.Reducer = import.OwnReducer;
                }

                // A file to import is the "previous log" of the files recorded after it.
                if (import.CanImport && import.Gas is not null && import.Reducer is not null)
                    history.Add((recordedAt, import.Gas, import.Reducer));
            }

            foreach (ListViewItem item in _files.Items)
            {
                var import = (ImportItem)item.Tag!;
                string prefix = import.SameAsPrevious ? "= " : "";
                item.SubItems[4].Text = import.Gas is null ? (import.SameAsPrevious ? "no previous log" : "") : prefix + string.Join(",", import.Gas);
                item.SubItems[5].Text = import.Reducer is null ? (import.SameAsPrevious ? "no previous log" : "") : prefix + string.Join(",", import.Reducer);
                item.SubItems[4].ForeColor = item.SubItems[5].ForeColor = import.Gas is null && import.CanImport ? Color.DarkRed : item.ForeColor;
                item.UseItemStyleForSubItems = false;
            }

            ShowSelectedCoefficients();
            UpdateImportButton();
        }

        /// <summary>Shows the tables of the first selected file in the editor.</summary>
        private void ShowSelectedCoefficients()
        {
            var selected = _files.SelectedItems.Cast<ListViewItem>().Select(i => (ImportItem)i.Tag!).ToList();
            _coefficientsBox.Enabled = selected.Count > 0;

            _showingSelection = true;
            try
            {
                var first = selected.FirstOrDefault();
                // A file without its own tables starts from the car's newest imported log's tables
                // (Settings defaults only when the car has no logs yet).
                var latest = SelectedCar is Car car ? _database.GetCarLogCoefficients(car.Id).LastOrDefault() : default;
                _sameAsPrevious.Checked = first?.SameAsPrevious ?? true;
                _gas.Text = Join(first?.Gas ?? latest.Gas ?? Database.DefaultGasCoefficients);
                _reducer.Text = Join(first?.Reducer ?? latest.Reducer ?? Database.DefaultReducerCoefficients);
                _gas.Enabled = _reducer.Enabled = !_sameAsPrevious.Checked;
            }
            finally
            {
                _showingSelection = false;
            }
        }

        private static string Join(int[] values) => string.Join(",", values);

        /// <summary>Stores the editor's choice in the selected files.</summary>
        private void ApplyCoefficients(bool showErrors)
        {
            var selected = _files.SelectedItems.Cast<ListViewItem>().Select(i => (ImportItem)i.Tag!).ToList();
            if (selected.Count == 0)
                return;

            int[]? gas = null, reducer = null;

            if (!_sameAsPrevious.Checked)
            {
                if (!TryParse(_gas.Text, Settings.GasTemperatureRanges.Length, out gas, out var gasError) ||
                    !TryParse(_reducer.Text, Settings.ReductorTemperatureRanges.Length, out reducer, out gasError))
                {
                    if (showErrors)
                        MessageBox.Show(this, gasError, "Temperature tables", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            foreach (var import in selected)
            {
                import.SameAsPrevious = _sameAsPrevious.Checked;
                if (!_sameAsPrevious.Checked)
                {
                    import.OwnGas = gas;
                    import.OwnReducer = reducer;
                }
            }

            ResolveCoefficients();
        }

        private static bool TryParse(string text, int expected, out int[] values, out string error)
        {
            var parts = text.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            values = new int[parts.Length];
            error = "";

            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out values[i]))
                {
                    error = $"'{parts[i]}' is not a whole number.";
                    return false;
                }
            }

            if (values.Length != expected)
            {
                error = $"Expected {expected} values (one per temperature band), got {values.Length}.";
                return false;
            }

            return true;
        }

        private void UpdateImportButton()
        {
            var ready = Items.Where(i => i.CanImport).ToList();
            int missing = ready.Count(i => i.Gas is null || i.Reducer is null);

            _import.Enabled = SelectedTable is not null && ready.Count > 0 && missing == 0;
            _status.Text = missing > 0
                ? $"{missing} file(s) have no temperature tables: there is no previous log, so enter them (uncheck \"Same as previous\")."
                : ready.Count > 0 ? $"{ready.Count} file(s) ready." : "";
            _status.ForeColor = missing > 0 ? Color.DarkRed : Color.Black;
        }

        // ---------------- Import ----------------

        private async void Import_Click(object? sender, EventArgs e)
        {
            if (SelectedCar is not Car car || SelectedTable is not FuelTable table)
                return;

            // In recording order, so "same as previous" means the same as in the list.
            var ready = _files.Items.Cast<ListViewItem>()
                .Where(i => ((ImportItem)i.Tag!).CanImport)
                .OrderBy(i => ((ImportItem)i.Tag!).Result!.Log.RecordedAt)
                .ToList();

            if (MessageBox.Show(this,
                    $"Import {ready.Count} file(s) for\n\n  car: {car.Name}\n  fuel table: {table.Name} ({table.CreatedAt:yyyy-MM-dd})\n\n" +
                    "The logs must have been recorded with this fuel table and the temperature tables shown in the list.",
                    "Import", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
                return;

            _import.Enabled = false;
            UseWaitCursor = true;
            int done = 0;

            try
            {
                foreach (var item in ready)
                {
                    var import = (ImportItem)item.Tag!;
                    var result = import.Result!;
                    _status.Text = $"Importing {result.Log.FileName}...";
                    item.SubItems[6].Text = "Importing...";

                    try
                    {
                        result.Log.FuelTableId = table.Id;
                        result.Log.GasTempCoef = import.Gas!;
                        result.Log.ReducerTempCoef = import.Reducer!;
                        await Task.Run(() => _database.ImportLog(result.Log, result.Rows));

                        import.CanImport = false;
                        item.ForeColor = Color.DarkGreen;
                        item.SubItems[6].Text = "Imported";
                        done++;
                        Imported = true;
                    }
                    catch (Exception ex)
                    {
                        item.SubItems[6].Text = "Error: " + ex.Message;
                        item.ForeColor = Color.DarkRed;
                    }
                }
            }
            finally
            {
                UseWaitCursor = false;
                LoadTables(table.Id);
                ResolveCoefficients();
                _status.Text = $"{done} of {ready.Count} file(s) imported.";
                _status.ForeColor = Color.Black;
            }
        }
    }
}
