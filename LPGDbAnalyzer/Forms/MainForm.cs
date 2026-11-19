using LPGDataAnalyzer.Controls;
using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Services;
using LPGDbAnalyzer.Data;
using LPGDbAnalyzer.Services;
using System.Globalization;
using System.Text;

namespace LPGDbAnalyzer.Forms
{
    // Inside the namespace, so LPG.Core's Average/Min/Max/Median extensions are preferred over LINQ's,
    // as in LPGDataAnalyzer's own code.
    using LPGDataAnalyzer;

    /// <summary>
    /// Tabs over the whole window, as in LPGDataAnalyzer. The first tab, "Data", holds the database tree
    /// (Car -> .ml8 configuration (or a fuel table from a screenshot) -> Log files, with checkboxes) and the selected item's details: its fields,
    /// the fuel table (coloured) and the screenshot. "Analyze checked" loads the checked logs:
    /// logs of one fuel table enable the analysis tabs (the screens of LPG.Core), logs of two fuel
    /// tables enable "Compare". Tabs without loaded data are disabled.
    /// </summary>
    public sealed class MainForm : Form
    {
        private const string CarIcon = "car";
        private const string TableIcon = "table";
        private const string ConfigIcon = "config";
        private const string LogIcon = "log";

        private readonly Database _database;

        private readonly ImageList _icons = new() { ColorDepth = ColorDepth.Depth32Bit, ImageSize = new Size(16, 16) };
        private readonly TreeView _tree = new() { Dock = DockStyle.Fill, HideSelection = false };

        private readonly DataGridView _fields = CreateGrid();
        private readonly DataGridView _table = CreateGrid();
        private readonly PictureBox _picture = new() { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White };
        private readonly Label _tableTitle = new() { AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Padding = new Padding(0, 8, 0, 2) };
        private readonly Label _imageTitle = new() { AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Padding = new Padding(0, 8, 0, 2) };
        private readonly Font _boldCellFont = new("Segoe UI", 9f, FontStyle.Bold);

        private readonly ToolStripButton _editButton;
        private readonly ToolStripButton _deleteButton;
        private readonly ToolStripButton _deleteImageButton;
        private readonly ToolStripButton _downloadButton;
        private readonly ToolStripMenuItem _downloadMenu;
        private readonly ToolStripMenuItem _editMenu;
        private readonly ToolStripMenuItem _deleteMenu;
        private readonly ToolStripMenuItem _deleteImageMenu;

        // ---- Tabs: Data first, then the screens of LPGDataAnalyzer and the comparison ----
        private readonly TabControl _tabs = new()
        {
            Dock = DockStyle.Fill,
            DrawMode = TabDrawMode.OwnerDrawFixed,
            Padding = new Point(14, 5),
        };
        private readonly TabPage _dataPage = new("Data");
        private readonly TabPage _comparePage = new("Compare");
        private readonly List<TabPage> _analysisPages = [];
        private readonly HashSet<TabPage> _enabledPages = [];

        private readonly DataItemGrid _mainData = new() { Dock = DockStyle.Fill };
        private readonly AnalysisUC _analysis = new() { Dock = DockStyle.Fill };
        private readonly TemperatureAnalyzerUI _temperature = new() { Dock = DockStyle.Fill };
        private readonly MapAnalyzerUI _mapAnalysis = new() { Dock = DockStyle.Fill };
        private readonly PredictionControl _prediction = new() { Dock = DockStyle.Fill };
        private readonly ReducerTempCorrection _reducer = new() { Dock = DockStyle.Fill };
        private readonly DataItemLineChartControl _chart = new() { Dock = DockStyle.Fill };
        private readonly CompareControl _compare = new() { Dock = DockStyle.Fill };
        private readonly TextBox _extraInjection = new()
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false,
            Font = new Font("Consolas", 10f),
            BackColor = SystemColors.Window,
        };

        private readonly ToolStripStatusLabel _summary = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };

        private bool _checking;

        protected override void OnLoad(EventArgs e)
        {
            // Buttons grow to fit their text (Windows scaling above 100%).
            LPGDataAnalyzer.Controls.ControlSizing.FitButtons(this);
            base.OnLoad(e);
        }

        public MainForm(Database database)
        {
            _database = database;

            Text = "LPG DB Analyzer";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1300, 850);
            KeyPreview = true;

            _icons.Images.Add(CarIcon, Stock(StockIconId.Folder));
            _icons.Images.Add(TableIcon, Stock(StockIconId.ImageFiles));
            _icons.Images.Add(ConfigIcon, Stock(StockIconId.DocumentWithAssociation));
            _icons.Images.Add(LogIcon, Stock(StockIconId.DocumentNoAssociation));
            _tree.ImageList = _icons;
            _tree.CheckBoxes = true;
            _tree.AfterCheck += Tree_AfterCheck;

            // ---- Toolbar and context menu ----
            var toolbar = new ToolStrip { ImageScalingSize = new Size(16, 16) };
            toolbar.Items.Add(Button("Import logs...", StockIconId.FolderOpen, (_, _) => OpenImport()));
            toolbar.Items.Add(Button("Import .ml8...", StockIconId.DocumentWithAssociation, (_, _) => ImportConfig()));
            toolbar.Items.Add(Button("New car...", StockIconId.Folder, (_, _) => NewCar()));
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(_editButton = Button("Edit...", StockIconId.Rename, (_, _) => EditSelected()));
            toolbar.Items.Add(_deleteButton = Button("Delete", StockIconId.Delete, (_, _) => DeleteSelected()));
            toolbar.Items.Add(_deleteImageButton = Button("Delete image", StockIconId.Recycler, (_, _) => DeleteImage()));
            toolbar.Items.Add(_downloadButton = Button("Download...", StockIconId.DriveFixed, (_, _) => DownloadSelected()));
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(Button("Refresh", StockIconId.Stack, (_, _) => LoadTree()));
            toolbar.Items.Add(new ToolStripSeparator());
            toolbar.Items.Add(Button("Analyze checked", StockIconId.Find, async (_, _) => await AnalyzeCheckedAsync()));
            var together = Button("Analyze together", StockIconId.MixedFiles, async (_, _) => await AnalyzeCheckedAsync(together: true));
            together.ToolTipText = "All checked logs as one, even from several fuel tables: their trims are brought to the map\n" +
                                   "and temperature tables of the newest checked log.";
            toolbar.Items.Add(together);

            var menu = new ContextMenuStrip();
            var analyzeMenu = MenuItem("Analyze checked", StockIconId.Find, async (_, _) => await AnalyzeCheckedAsync());
            analyzeMenu.Font = new Font(analyzeMenu.Font, FontStyle.Bold);
            menu.Items.Add(analyzeMenu);
            var togetherMenu = MenuItem("Analyze together (newest map)", StockIconId.MixedFiles, async (_, _) => await AnalyzeCheckedAsync(together: true));
            menu.Items.Add(togetherMenu);
            menu.Items.Add(new ToolStripSeparator());
            // Enabled only when some logs are checked.
            menu.Opening += (_, _) => analyzeMenu.Enabled = togetherMenu.Enabled = CheckedLogNodes().Any();
            menu.Items.Add(_editMenu = MenuItem("Edit...", StockIconId.Rename, (_, _) => EditSelected()));
            menu.Items.Add(_deleteMenu = MenuItem("Delete", StockIconId.Delete, (_, _) => DeleteSelected()));
            menu.Items.Add(_deleteImageMenu = MenuItem("Delete image", StockIconId.Recycler, (_, _) => DeleteImage()));
            menu.Items.Add(_downloadMenu = MenuItem("Download...", StockIconId.DriveFixed, (_, _) => DownloadSelected()));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(MenuItem("Import logs here...", StockIconId.FolderOpen, (_, _) => OpenImport()));
            menu.Items.Add(MenuItem("Import .ml8 configuration...", StockIconId.DocumentWithAssociation, (_, _) => ImportConfig()));
            _tree.ContextMenuStrip = menu;

            // Right click selects the node first, so the menu acts on it.
            _tree.NodeMouseClick += (_, e) => { if (e.Button == MouseButtons.Right) _tree.SelectedNode = e.Node; };
            _tree.AfterSelect += (_, e) => ShowDetails(e.Node);
            _tree.NodeMouseDoubleClick += (_, e) => { if (e.Node is not null) EditSelected(); };

            // ---- Right side: fields, fuel table, image (top to bottom) ----
            _fields.ColumnCount = 2;
            _fields.Columns[0].HeaderText = "Field";
            _fields.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            _fields.Columns[0].DefaultCellStyle.Font = _boldCellFont;
            _fields.Columns[0].DefaultCellStyle.BackColor = Color.WhiteSmoke;
            _fields.Columns[1].HeaderText = "Value";
            _fields.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            // Long values (notes) wrap onto more lines; the grid's height follows the rows.
            _fields.Columns[1].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            _fields.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            _fields.ColumnHeadersVisible = false;

            _table.RowHeadersVisible = true;
            _table.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            _table.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _table.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _table.ColumnHeadersDefaultCellStyle.Font = _boldCellFont;
            _table.RowHeadersDefaultCellStyle.Font = _boldCellFont;

            var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(6), AutoScroll = true };
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // fields
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // table title
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // table
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // image title
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // image
            _fields.Dock = DockStyle.Top;
            _table.Dock = DockStyle.Top;
            right.Controls.Add(_fields, 0, 0);
            right.Controls.Add(_tableTitle, 0, 1);
            right.Controls.Add(_table, 0, 2);
            right.Controls.Add(_imageTitle, 0, 3);
            right.Controls.Add(_picture, 0, 4);

            var treeHint = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                Padding = new Padding(4),
                ForeColor = Color.DimGray,
                Text = "Check the logs to analyze (a fuel table checks all its logs), then \"Analyze checked\": " +
                       "one fuel table = analysis, two = comparison. \"Analyze together\" = all checked logs as one, brought to the newest map.",
            };

            var split = new SplitContainer { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel1 };
            split.Panel1.Controls.Add(_tree);
            split.Panel1.Controls.Add(treeHint);
            split.Panel2.Controls.Add(right);

            // ---- Tabs: Data (tree, details, toolbar), then the analysis screens over the full window ----
            _dataPage.Controls.Add(split);
            _dataPage.Controls.Add(toolbar);
            _tabs.TabPages.Add(_dataPage);
            _enabledPages.Add(_dataPage);
            SetTabIcon(_dataPage, StockIconId.DriveFixed);

            AddAnalysisPage("Main Data", _mainData, StockIconId.AutoList);
            AddAnalysisPage("Analyses", _analysis, StockIconId.Find);
            AddAnalysisPage("Temperature", _temperature, StockIconId.Warning);
            AddAnalysisPage("Map Analysis", _mapAnalysis, StockIconId.World);
            AddAnalysisPage("Prediction", _prediction, StockIconId.Settings);
            AddAnalysisPage("Temperature prediction", _reducer, StockIconId.Shield);
            AddAnalysisPage("Chart", _chart, StockIconId.ImageFiles);
            AddAnalysisPage("Extra injection", _extraInjection, StockIconId.Info);
            _comparePage.Controls.Add(_compare);
            _tabs.TabPages.Add(_comparePage);
            SetTabIcon(_comparePage, StockIconId.MixedFiles);

            _tabs.Selecting += (_, e) =>
            {
                if (e.TabPage is not null && !_enabledPages.Contains(e.TabPage))
                    e.Cancel = true;
            };
            _tabs.DrawItem += Tabs_DrawItem;

            // Each tab is as wide as its own title: the padding leaves room for the icon and the bold (selected) title.
            // (LPG.Core's Max extension returns double.)
            int boldExtra = (int)_tabs.TabPages.Cast<TabPage>()
                .Max(p => TextRenderer.MeasureText(p.Text, _boldCellFont).Width - TextRenderer.MeasureText(p.Text, _tabs.Font).Width);
            _tabs.SizeMode = TabSizeMode.Normal;
            _tabs.Padding = new Point((TabIconSize + TabIconGap + boldExtra) / 2 + 8, 6);

            var statusBar = new StatusStrip();
            statusBar.Items.Add(_summary);
            statusBar.Items.Add(new ToolStripStatusLabel("Database: " + database.FilePath));

            Controls.Add(_tabs);
            Controls.Add(statusBar);

            WindowState = FormWindowState.Maximized;
            Load += (_, _) => split.SplitterDistance = 380;

            LoadTree();
        }

        /// <summary>Disabled tabs (no data loaded for them) are drawn grey; selecting them is cancelled.</summary>
        private void Tabs_DrawItem(object? sender, DrawItemEventArgs e)
        {
            var page = _tabs.TabPages[e.Index];
            bool enabled = _enabledPages.Contains(page);
            bool selected = e.Index == _tabs.SelectedIndex;

            var bounds = _tabs.GetTabRect(e.Index);
            using (var back = new SolidBrush(selected ? SystemColors.Window : SystemColors.Control))
                e.Graphics.FillRectangle(back, bounds);

            // Accent line on the selected tab.
            if (selected)
                using (var accent = new SolidBrush(SystemColors.Highlight))
                    e.Graphics.FillRectangle(accent, bounds.Left, bounds.Top, bounds.Width, 3);

            // Icon and title centred together; disabled tabs get the greyed icon.
            var font = selected ? _boldCellFont : _tabs.Font;
            int textWidth = TextRenderer.MeasureText(page.Text, font).Width;
            bool hasIcon = _tabIcons.TryGetValue(page, out var icon);
            int contentWidth = textWidth + (hasIcon ? TabIconSize + TabIconGap : 0);
            int x = bounds.Left + (bounds.Width - contentWidth) / 2;

            if (hasIcon)
            {
                e.Graphics.DrawImage(enabled ? icon.Normal : icon.Disabled,
                    new Rectangle(x, bounds.Top + (bounds.Height - TabIconSize) / 2 + 1, TabIconSize, TabIconSize));
                x += TabIconSize + TabIconGap;
            }

            TextRenderer.DrawText(e.Graphics, page.Text, font, new Rectangle(x, bounds.Top, bounds.Right - x, bounds.Height),
                enabled ? SystemColors.ControlText : SystemColors.GrayText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }

        private const int TabIconSize = 16;
        private const int TabIconGap = 5;
        private readonly Dictionary<TabPage, (Bitmap Normal, Image Disabled)> _tabIcons = [];

        private void SetTabIcon(TabPage page, StockIconId id)
        {
            var normal = Stock(id);
            _tabIcons[page] = (normal, ToolStripRenderer.CreateDisabledImage(normal));
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.F5)
                LoadTree();
            else if (e.KeyCode == Keys.Delete && _tree.Focused && _tabs.SelectedTab == _dataPage)
                DeleteSelected();
        }

        // ---------------- Icons ----------------

        private static Bitmap Stock(StockIconId id)
        {
            using var icon = SystemIcons.GetStockIcon(id, StockIconOptions.SmallIcon);
            return icon.ToBitmap();
        }

        private static ToolStripButton Button(string text, StockIconId icon, EventHandler click) =>
            new(text, Stock(icon), click) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText };

        private static ToolStripMenuItem MenuItem(string text, StockIconId icon, EventHandler click) =>
            new(text, Stock(icon), click);

        private static DataGridView CreateGrid() => new()
        {
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.FixedSingle,
            ScrollBars = ScrollBars.None,
            Margin = new Padding(0),
        };

        // ---------------- Tree ----------------

        private void LoadTree()
        {
            var selected = _tree.SelectedNode?.Tag;
            var checkedLogs = CheckedLogNodes().Select(n => ((LogFile)n.Tag!).Id).ToHashSet();

            _tree.BeginUpdate();
            _checking = true; // restoring checks must not cascade
            _tree.Nodes.Clear();

            foreach (var car in _database.GetCars())
            {
                var carNode = AddNode(_tree.Nodes, car.Name, car, CarIcon);

                foreach (var table in _database.GetFuelTables(car.Id))
                {
                    var tableNode = AddNode(carNode.Nodes, table.ToString(), table, table.HasConfig ? ConfigIcon : TableIcon);

                    foreach (var log in _database.GetLogFiles(table.Id))
                        AddNode(tableNode.Nodes, log.ToString(), log, LogIcon).Checked = checkedLogs.Contains(log.Id);

                    tableNode.Checked = tableNode.Nodes.Count > 0 && tableNode.Nodes.Cast<TreeNode>().All(n => n.Checked);
                }

                carNode.Expand();
            }

            _tree.EndUpdate();
            _checking = false;

            var node = selected is null ? null : FindNode(_tree.Nodes, selected);
            if (node is not null)
                _tree.SelectedNode = node;
            else
                ShowDetails(_tree.SelectedNode);
        }

        private static TreeNode AddNode(TreeNodeCollection nodes, string text, object tag, string icon)
        {
            var node = nodes.Add(text);
            node.Tag = tag;
            node.ImageKey = node.SelectedImageKey = icon;
            return node;
        }

        private static TreeNode? FindNode(TreeNodeCollection nodes, object tag)
        {
            foreach (TreeNode node in nodes)
            {
                if (SameItem(node.Tag, tag))
                    return node;
                if (FindNode(node.Nodes, tag) is TreeNode found)
                    return found;
            }
            return null;
        }

        private static bool SameItem(object? a, object b) => (a, b) switch
        {
            (Car x, Car y) => x.Id == y.Id,
            (FuelTable x, FuelTable y) => x.Id == y.Id,
            (LogFile x, LogFile y) => x.Id == y.Id,
            _ => false,
        };

        // ---------------- Details (right side) ----------------

        private void ShowDetails(TreeNode? node)
        {
            var item = node?.Tag;

            _editButton.Enabled = _editMenu.Enabled = item is Car or FuelTable or LogFile;
            _deleteButton.Enabled = _deleteMenu.Enabled = item is not null;
            _deleteImageButton.Enabled = _deleteImageMenu.Enabled = item is FuelTable { HasImage: true };
            _downloadButton.Enabled = _downloadMenu.Enabled = item is FuelTable { HasConfig: true } or FuelTable { HasImage: true } or LogFile { HasContent: true };

            var fields = item switch
            {
                Car c => CarFields(c, node!),
                FuelTable t => TableFields(t, node!),
                LogFile l => LogFields(l),
                _ => [("", _tree.Nodes.Count == 0
                    ? "The database is empty. Use \"Import logs...\" to add a car, its fuel table and logs."
                    : "Select a car, fuel table or log.")],
            };

            _fields.Rows.Clear();
            foreach (var (name, value) in fields)
                _fields.Rows.Add(name, value);
            FitHeight(_fields);

            var table = item as FuelTable ?? (item is LogFile && node!.Parent?.Tag is FuelTable owner ? owner : null);
            ShowTable(table);
            ShowImage(table);
        }

        private static List<(string, string)> CarFields(Car c, TreeNode node) =>
        [
            ("Car", c.Name),
            ("Engine", c.Engine ?? ""),
            ("Banks", c.Banks.ToString()),
            ("Modifications", c.Modifications ?? ""),
            ("Petrol ECU", c.PetrolEcu ?? ""),
            ("Fast trim step", $"{c.FastTrimStep} %"),
            ("LPG ECU", c.LpgEcu ?? ""),
            ("Injectors", c.Injectors ?? ""),
            ("Reducer", c.Reducer ?? ""),
            ("Reference pressure", $"{c.ReferencePressure} bar"),
            ("Trim delay", $"{c.TrimDelayMs} ms"),
            ("Notes", c.Notes ?? ""),
            ("Fuel tables", node.Nodes.Count.ToString()),
            ("Logs", node.Nodes.Cast<TreeNode>().Sum(n => n.Nodes.Count).ToString()),
            ("Added", c.CreatedAt.ToString("yyyy-MM-dd HH:mm")),
        ];

        private List<(string, string)> TableFields(FuelTable t, TreeNode node)
        {
            var all = t.Values.SelectMany(r => r).ToArray();
            var offsets = ReadOffsets(t);
            string Ms(double? v) => v is double x ? x.ToString("0.00", CultureInfo.InvariantCulture) + " ms" : (t.HasConfig ? "not in the file" : "");
            return
            [
                ("Fuel table", t.Name),
                ("Car", node.Parent?.Text ?? ""),
                ("Set in ECU", t.CreatedAt.ToString("yyyy-MM-dd HH:mm")),
                ("Size", $"{t.InjectionAxis.Length} rows x {t.RpmAxis.Length} columns"),
                ("RPM", $"{Num(t.RpmAxis.FirstOrDefault())} - {Num(t.RpmAxis.LastOrDefault())}"),
                ("Injection", $"{Num(t.InjectionAxis.FirstOrDefault())} - {Num(t.InjectionAxis.LastOrDefault())} ms"),
                ("Values", all.Length > 0 ? $"{Num(all.Min())} - {Num(all.Max())}, average {all.Average():0.0}" : ""),
                ("Logs", t.LogCount.ToString()),
                ("Image", t.HasImage ? t.ImageFileName ?? "yes" : "none"),
                ("Configuration", t.HasConfig ? $"{t.ConfigFileName}, saved {t.ConfigSavedAt:yyyy-MM-dd HH:mm}" : "none"),
                ("Config LPG temp", t.ConfigGasTempCoef is null ? "" : string.Join(", ", t.ConfigGasTempCoef)),
                ("Config reducer temp", t.ConfigReducerTempCoef is null ? "" : string.Join(", ", t.ConfigReducerTempCoef)),
                ("Gas dead time", Ms(offsets?.DeadTimeMs)),
                ("Extrainj. sensitivity", Ms(offsets?.SensitivityMs)),
                ("Extra-inj. ident. time", Ms(offsets?.IdentTimeMs)),
                ("Notes", t.Notes ?? ""),
            ];
        }

        private static List<(string, string)> LogFields(LogFile l)
        {
            List<(string, string)> fields =
            [
                ("Log file", l.FileName),
                ("Recorded", l.RecordedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                ("Imported", l.ImportedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                ("Rows", l.RowCount.ToString()),
                ("Original file", l.HasContent ? "stored (Download...)" : "not stored (imported before it was kept)"),
                ("SHA-256", l.FileHash),
            ];

            // ECU temperature tables while recording, one row per band (double-click the log to edit).
            AddBands(fields, "LPG temp", Settings.GasTemperatureRanges, l.GasTempCoef);
            AddBands(fields, "Reducer temp", Settings.ReductorTemperatureRanges, l.ReducerTempCoef);
            return fields;
        }

        private static void AddBands(List<(string, string)> fields, string name, (int Min, int Max, string Label)[] ranges, int[] values)
        {
            for (int i = 0; i < ranges.Length; i++)
                fields.Add(($"{name} {ranges[i].Label.Replace("Temp_", "").Replace('_', '-')} °C",
                            i < values.Length ? values[i].ToString("+0;-0;0", CultureInfo.InvariantCulture) : ""));
        }

        private static string Num(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

        private void ShowTable(FuelTable? t)
        {
            _table.Visible = _tableTitle.Visible = t is not null;
            if (t is null)
                return;

            _tableTitle.Text = $"Fuel table: {t.Name}  (rows = injection time ms, columns = RPM)";

            _table.Rows.Clear();
            _table.Columns.Clear();

            foreach (var rpm in t.RpmAxis)
                _table.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Num(rpm), SortMode = DataGridViewColumnSortMode.NotSortable });

            for (int r = 0; r < t.InjectionAxis.Length; r++)
            {
                int index = _table.Rows.Add(t.Values[r].Select(v => (object)Num(v)).ToArray());
                _table.Rows[index].HeaderCell.Value = t.InjectionAxis[r].ToString("0.00", CultureInfo.InvariantCulture);
            }

            Heatmap.ColorizeValues(_table, t.Values.Select(r => r.Select(v => (double?)v).ToArray()).ToArray(), boldFont: _boldCellFont);
            _table.ClearSelection();
            FitHeight(_table);
        }

        private void ShowImage(FuelTable? t)
        {
            var old = _picture.Image;
            _picture.Image = null;
            old?.Dispose();

            byte[]? bytes = t is { HasImage: true } ? _database.GetFuelTableImage(t.Id) : null;
            _picture.Visible = _imageTitle.Visible = bytes is not null;

            if (bytes is not null)
            {
                using var ms = new MemoryStream(bytes);
                _picture.Image = new Bitmap(Image.FromStream(ms));
                _imageTitle.Text = "Screenshot: " + (t!.ImageFileName ?? "");
            }
        }

        /// <summary>Makes a grid exactly as tall as its rows, so the panel shows everything top to bottom.</summary>
        private static void FitHeight(DataGridView grid)
        {
            int height = grid.Rows.GetRowsHeight(DataGridViewElementStates.Visible) + 2;
            if (grid.ColumnHeadersVisible)
                height += grid.ColumnHeadersHeight;
            grid.Height = height;
        }

        // ---------------- Checkboxes ----------------

        /// <summary>
        /// A car or fuel table checks or unchecks everything under it; a fuel table is shown checked
        /// when all its logs are checked.
        /// </summary>
        private void Tree_AfterCheck(object? sender, TreeViewEventArgs e)
        {
            if (_checking || e.Node is null)
                return;

            _checking = true;
            try
            {
                SetChildren(e.Node, e.Node.Checked);

                if (e.Node.Tag is LogFile && e.Node.Parent is TreeNode table)
                    table.Checked = table.Nodes.Cast<TreeNode>().All(n => n.Checked);
            }
            finally
            {
                _checking = false;
            }
        }

        private static void SetChildren(TreeNode node, bool isChecked)
        {
            foreach (TreeNode child in node.Nodes)
            {
                child.Checked = isChecked;
                SetChildren(child, isChecked);
            }
        }

        private IEnumerable<TreeNode> CheckedLogNodes() =>
            _tree.Nodes.Cast<TreeNode>()
                .SelectMany(car => car.Nodes.Cast<TreeNode>())
                .SelectMany(table => table.Nodes.Cast<TreeNode>())
                .Where(log => log.Checked && log.Tag is LogFile);

        // ---------------- Analysis ----------------

        private void AddAnalysisPage(string title, Control content, StockIconId icon)
        {
            var page = new TabPage(title);
            page.Controls.Add(content);
            _analysisPages.Add(page);
            _tabs.TabPages.Add(page);
            SetTabIcon(page, icon);
        }

        /// <summary>Enables either the analysis tabs or the comparison tab (Data stays enabled) and opens the first.</summary>
        private void ShowPages(bool compare)
        {
            _enabledPages.Clear();
            _enabledPages.Add(_dataPage);

            if (compare)
                _enabledPages.Add(_comparePage);
            else
                _enabledPages.UnionWith(_analysisPages);

            _tabs.Invalidate();
            _tabs.SelectedTab = compare ? _comparePage : _analysisPages[0];
        }

        /// <param name="together">All checked logs as one analysis, even from several fuel tables, brought to the
        /// setup of the newest checked log. Otherwise one fuel table = analysis, two = comparison.</param>
        private async Task AnalyzeCheckedAsync(bool together = false)
        {
            var nodes = CheckedLogNodes().ToList();
            var tables = nodes.Select(n => (FuelTable)n.Parent!.Tag!).DistinctBy(t => t.Id).ToDictionary(t => t.Id);
            var cars = nodes.Select(n => (Car)n.Parent!.Parent!.Tag!).DistinctBy(c => c.Id).ToList();
            // The fixed times each configuration adds to the gas time (gas dead time, extra injection), from its .ml8.
            var offsets = tables.Values.ToDictionary(t => t.Id, ReadOffsets);
            EcuSetup Setup(FuelTable table, LogFile log) => MainForm.Setup(table, log, offsets.GetValueOrDefault(table.Id));

            List<(FuelTable Table, Car Car, List<LogFile> Logs)> groups;

            if (together)
            {
                if (nodes.Count == 0 || cars.Count > 1)
                {
                    MessageBox.Show(this,
                        nodes.Count == 0 ? "Check the logs to analyze first (checking a fuel table checks all its logs)."
                                         : "\"Analyze together\" works with the logs of one car.",
                        "Analyze together", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // The frame is the fuel table of the newest checked log: the map that was in the ECU last.
                var logs = nodes.Select(n => (LogFile)n.Tag!).ToList();
                var newest = logs.OrderBy(l => l.RecordedAt).ThenBy(l => l.Id).Last();
                groups = [(tables[newest.FuelTableId], cars[0], logs)];
            }
            else
            {
                // Configurations with the same map (e.g. only a temperature table changed) are analyzed as one map:
                // the frame is the newest of them, each log keeps its own temperature tables.
                groups = nodes
                    .GroupBy(n => MapKey((FuelTable)n.Parent!.Tag!))
                    .Select(g =>
                    {
                        var table = g.Select(n => (FuelTable)n.Parent!.Tag!).OrderBy(t => t.CreatedAt).ThenBy(t => t.Id).Last();
                        return (Table: table, Car: (Car)g.First().Parent!.Parent!.Tag!, Logs: g.Select(n => (LogFile)n.Tag!).ToList());
                    })
                    .OrderBy(g => g.Table.CreatedAt)
                    .ToList();

                if (groups.Count is 0 or > 2)
                {
                    MessageBox.Show(this,
                        groups.Count == 0
                            ? "Check the logs to analyze first (checking a fuel table checks all its logs)."
                            : $"Logs of {groups.Count} different fuel maps are checked. Check the logs of one map to analyze it, or of two " +
                              "to compare them, or use \"Analyze together\" to analyze them all as one.",
                        "Analyze", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            UseWaitCursor = true;
            _summary.Text = "Loading...";

            try
            {
                var loaded = new List<Loaded>();
                foreach (var g in groups)
                {
                    // Every log's trims are brought to the map of the group's table and the temperature tables of its
                    // newest log (nothing changes when they are the same).
                    var newestLog = g.Logs.OrderBy(l => l.RecordedAt).ThenBy(l => l.Id).Last();
                    var target = Setup(g.Table, newestLog);

                    // The parser places readings in map cells, so the table's axes must be set first.
                    Settings.SetMapAxes(g.Table.RpmAxis, g.Table.InjectionAxis);
                    var (data, normalized) = await Task.Run(() =>
                    {
                        var d = AnalysisLoader.Load(_database, g.Logs, g.Car.TrimDelayMs, log => Setup(tables[log.FuelTableId], log), target, out int n);
                        return (d, n);
                    });

                    int tableCount = g.Logs.Select(l => MapKey(tables[l.FuelTableId])).Distinct().Count();
                    var setups = g.Logs.GroupBy(l => l.FileName)
                        .ToDictionary(x => x.Key, x => Setup(tables[x.First().FuelTableId], x.First()));
                    loaded.Add(new Loaded(g.Table, g.Car, data, g.Logs, normalized, tableCount, setups, tables[newestLog.FuelTableId]));
                }

                if (loaded.Any(l => l.Data.Length == 0))
                {
                    _summary.Text = "";
                    MessageBox.Show(this, "The checked logs have no readings on LPG.", "Analyze", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (loaded.Count == 1)
                    ShowAnalysis(loaded[0]);
                else
                    ShowComparison(loaded[0], loaded[1]);
            }
            catch (Exception ex)
            {
                _summary.Text = "";
                MessageBox.Show(this, ex.Message, "Analyze failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        /// <summary>Same key = same map (axes and values), whatever the configuration's temperature tables.</summary>
        private static string MapKey(FuelTable t) =>
            string.Join(";", t.RpmAxis) + "|" + string.Join(";", t.InjectionAxis) + "|" +
            string.Join("/", t.Values.Select(r => string.Join(";", r)));

        private static EcuSetup Setup(FuelTable table, LogFile log, GasTimeOffsets? offsets) =>
            new(table.RpmAxis, table.InjectionAxis, table.Values, log.GasTempCoef, log.ReducerTempCoef, offsets);

        /// <summary>The configuration's fixed gas times, or null (no .ml8, or one without these keys).</summary>
        private GasTimeOffsets? ReadOffsets(FuelTable table)
        {
            try
            {
                return table.HasConfig && _database.GetFuelTableConfig(table.Id) is { } file ? Ml8Config.Load(file).Offsets : null;
            }
            catch (InvalidDataException)
            {
                return null;
            }
        }

        /// <summary>Checked logs loaded for analysis, with their trims brought to <see cref="Table"/> and the newest log's temperature tables.</summary>
        /// <param name="NormalizedLogs">Logs whose trims were changed (other temperature tables or another map).</param>
        /// <param name="TableCount">Different maps the logs were recorded with (more than 1 with "Analyze together").</param>
        /// <param name="Setups">The ECU setup each log was recorded with, by file name (<see cref="DataItem.SourceFile"/>).</param>
        /// <param name="NewestTable">Fuel table (configuration) of the newest checked log.</param>
        private sealed record Loaded(FuelTable Table, Car Car, DataItem[] Data, List<LogFile> Logs, int NormalizedLogs, int TableCount,
            IReadOnlyDictionary<string, EcuSetup> Setups, FuelTable NewestTable)
        {
            public int LogCount => Logs.Count;

            /// <summary>Temperature tables of the newest checked log (the ones to change next).</summary>
            public LogFile Newest => Logs.OrderBy(l => l.RecordedAt).ThenBy(l => l.Id).Last();

            /// <summary>Short note for the status bar when trims were brought to another setup.</summary>
            public string NormalizedNote => NormalizedLogs == 0 ? "" :
                $", trims of {NormalizedLogs} log(s) brought to {(TableCount > 1 ? $"the map \"{Table.Name}\" and " : "")}the newest temperature tables";
        }

        /// <summary>One fuel table: the screens of LPGDataAnalyzer, fed as its MainForm.LoadParsedData does.</summary>
        private void ShowAnalysis(Loaded l)
        {

            // All screens read the map axes from Settings: those of this fuel table.
            Settings.SetMapAxes(l.Table.RpmAxis, l.Table.InjectionAxis);

            _mainData.SetData(l.Data);
            _analysis.LoadParcedData(l.Data);
            _temperature.LoadData(l.Data);
            _reducer.Data = l.Data;
            _reducer.ReferencePressure = l.Car.ReferencePressure;
            _reducer.SetCurrentCoefficients(l.Newest.GasTempCoef, l.Newest.ReducerTempCoef);
            _mapAnalysis.LoadData(l.Data);
            _chart.SetData(l.Data);
            _extraInjection.Text = ExtraInjectionReport(l, NewestConfig(l));

            LoadPrediction(l);

            ShowPages(compare: false);

            string tableNote = l.TableCount > 1 ? $" from {l.TableCount} fuel maps" : "";
            UpdateSummary($"{l.Car.Name} / {l.Table.Name}, {l.LogCount} log(s){tableNote}{l.NormalizedNote}", l.Data);
        }

        private void ShowComparison(Loaded first, Loaded second)
        {
            _compare.Show(
                new CompareControl.Side(first.Table, first.Data, first.LogCount, first.Newest.GasTempCoef, first.Newest.ReducerTempCoef),
                new CompareControl.Side(second.Table, second.Data, second.LogCount, second.Newest.GasTempCoef, second.Newest.ReducerTempCoef));
            ShowPages(compare: true);

            _summary.Text = $"Comparing {first.Table.Name} ({first.Data.Length} readings) with {second.Table.Name} ({second.Data.Length} readings)";

            if (first.Car.Id != second.Car.Id)
                MessageBox.Show(this, "The two fuel tables belong to different cars.", "Compare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>Same summary as LPGDataAnalyzer's status bar.</summary>
        private void UpdateSummary(string source, DataItem[] data)
        {
            double avgPress = data.Average(x => x.PRESS);
            double minPress = data.Min(x => x.PRESS);
            double maxPress = data.Max(x => x.PRESS);

            var sb = new StringBuilder();
            sb.Append($"{source}.  Rows: {data.Length}  ")
              .Append($"LPG temp: {data.Min(x => x.Temp_GAS)} - {data.Max(x => x.Temp_GAS)}  ")
              .Append($"PRESS: {minPress} - {maxPress}, average {avgPress.Round()}  ")
              .Append($"% change min: {Helper.PercentageChange(avgPress, minPress).Round()}  ")
              .Append($"max: {Helper.PercentageChange(avgPress, maxPress).Round()}");

            _summary.Text = sb.ToString();
        }

        /// <summary>The newest checked log's .ml8 configuration, or null (a table read from a screenshot, or unreadable).</summary>
        private Ml8Config? NewestConfig(Loaded l)
        {
            try
            {
                return l.NewestTable.HasConfig && _database.GetFuelTableConfig(l.NewestTable.Id) is { } file ? Ml8Config.Load(file) : null;
            }
            catch (InvalidDataException)
            {
                return null;
            }
        }

        /// <summary>
        /// The extra-injection settings estimated from the gas time step (<see cref="ExtraInjectionCalculator.EstimateGasStep"/>)
        /// next to the configuration's values, then the three results of LPGDataAnalyzer's "Extra Injection Calculator".
        /// </summary>
        /// <summary>A gas time step smaller than this is reported as no step (the scatter of GAS is ~0.06–0.1 ms).</summary>
        private const double NoStepMs = 0.1;

        private static string ExtraInjectionReport(Loaded l, Ml8Config? config)
        {
            var data = l.Data;
            var inv = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();

            sb.AppendLine("ESTIMATE FROM THE GAS TIME STEP");
            sb.AppendLine("Below the AEB \"Extra-injection ident. time\" the gas time is shorter: the ECU seems to use the");
            sb.AppendLine("\"Extrainj. sensitivity\" time there instead of the gas injector dead time (hypothesis, 2026-11-19).");
            sb.AppendLine();
            try
            {
                var est = ExtraInjectionCalculator.EstimateGasStep(data,
                    d => d.SourceFile is { } f && l.Setups.TryGetValue(f, out var s) ? s : null);
                double? dead = config?.Offsets?.DeadTimeMs;
                double? ident = config?.Offsets?.IdentTimeMs;
                double? sensitivity = config?.Offsets?.SensitivityMs;
                string Cfg(double? v) => v is null ? "" : string.Format(inv, "   (configuration: {0:F2} ms)", v);

                if (est is null)
                {
                    sb.AppendLine("Not enough readings with steady petrol times between 2.5 and 6 ms to find a step.");
                }
                else
                {
                    if (Math.Abs(est.JumpMs) < NoStepMs)
                        sb.AppendLine(string.Format(inv, "No step in the gas time (best fit {0:+0.00;-0.00} ms at {1:F2} ms): the dead time and the sensitivity act alike.",
                            -est.JumpMs, est.StepAtMs));
                    sb.AppendLine(string.Format(inv, "Extra-injection ident. time:  {0:F2} ms{1}", est.StepAtMs, Cfg(ident)));
                    sb.AppendLine(dead is { } dt
                        ? string.Format(inv, "Extrainj. sensitivity time:   {0:F2} ms = dead time {1:F2} - step {2:F2}{3}", dt - est.JumpMs, dt, est.JumpMs, Cfg(sensitivity))
                        : string.Format(inv, "Extrainj. sensitivity time:   gas injector dead time - {0:F2} ms (no .ml8 with the dead time)", est.JumpMs));
                    sb.AppendLine();
                    sb.AppendLine(string.Format(inv, "Fit: GAS = {0:F3} x BENZ x K x temperature factor + {1:F3} ms, {2:F3} ms less below {3:F2} ms petrol time",
                        est.Slope, est.OffsetAboveMs, est.JumpMs, est.StepAtMs));
                    sb.AppendLine(string.Format(inv, "Readings per bank: {0} ({1} below the step). Scatter {2:F3} ms with the step, {3:F3} ms without.",
                        est.Readings, est.ReadingsBelow, est.RmsMs, est.RmsWithoutStepMs));
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("The estimate failed: " + ex.Message);
            }

            sb.AppendLine();
            sb.AppendLine("OLD CALCULATOR (LPGDataAnalyzer)");
            try
            {
                sb.AppendLine("Ident time: " + ExtraInjectionCalculator.CalculateIdentTime(data));
                sb.AppendLine();
                sb.AppendLine("Extra injection time: " + ExtraInjectionCalculator.CalculateExtraInjectionTime(data));
                sb.AppendLine();
                sb.AppendLine("Histogram:");
                sb.AppendLine(ExtraInjectionCalculator.PrintHistogram(data));
            }
            catch (Exception ex)
            {
                sb.AppendLine("The old calculation failed: " + ex.Message);
            }

            return sb.ToString().Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        }

        // ---------------- Prediction ----------------

        /// <summary>Feeds the prediction screen with the fuel table; a predicted map can be exported as a .ml8.</summary>
        private void LoadPrediction(Loaded l)
        {
            // Written into a copy of the fuel table's stored .ml8 (Ml8Export explains when there is none).
            _prediction.ExportPrediction = map => Ml8Export.Run(this, _database, l.Table, map);
            _prediction.LoadFuelTable(l.Data, l.Table.Values, l.Car.ReferencePressure, l.Car.TrimDelayMs);
        }

        // ---------------- Commands ----------------

        private void EditSelected()
        {
            switch (_tree.SelectedNode?.Tag)
            {
                case Car car:
                    using (var form = new CarForm(_database, car))
                        if (form.ShowDialog(this) == DialogResult.OK)
                            LoadTree();
                    break;

                case LogFile log:
                    using (var form = new LogCoefficientsForm(_database, log))
                        if (form.ShowDialog(this) == DialogResult.OK)
                            LoadTree();
                    break;

                case FuelTable table when _tree.SelectedNode.Parent?.Tag is Car owner:
                    using (var form = FuelTableForm.Edit(_database, owner, table))
                        if (form.ShowDialog(this) == DialogResult.OK)
                            LoadTree();
                    break;
            }
        }

        private void DeleteSelected()
        {
            var node = _tree.SelectedNode;
            if (node?.Tag is null)
                return;

            string question = node.Tag switch
            {
                Car c => $"Delete the car \"{c.Name}\" with its {node.Nodes.Count} fuel table(s) and " +
                         $"{node.Nodes.Cast<TreeNode>().Sum(n => n.Nodes.Count)} log(s)?",
                FuelTable t => $"Delete the fuel table \"{t.Name}\" with its image and {t.LogCount} log(s)?",
                LogFile l => $"Delete the log \"{l.FileName}\" ({l.RowCount} rows)?",
                _ => "",
            };

            if (MessageBox.Show(this, question + "\n\nThis cannot be undone.", "Delete",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            try
            {
                UseWaitCursor = true;
                switch (node.Tag)
                {
                    case Car c: _database.DeleteCar(c.Id); break;
                    case FuelTable t: _database.DeleteFuelTable(t.Id); break;
                    case LogFile l: _database.DeleteLogFile(l.Id); break;
                }

                // Select the parent, so the view stays where it was.
                _tree.SelectedNode = node.Parent;
                LoadTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Delete failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private void DeleteImage()
        {
            if (_tree.SelectedNode?.Tag is not FuelTable { HasImage: true } t)
                return;

            if (MessageBox.Show(this,
                    $"Delete the screenshot of \"{t.Name}\"?\nThe fuel table values stay. Without the image they can no longer be checked against it.",
                    "Delete image", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            try
            {
                _database.DeleteFuelTableImage(t.Id);
                LoadTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Delete failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OpenImport()
        {
            // Start from the selected car / fuel table.
            long? carId = null, tableId = null;
            for (var n = _tree.SelectedNode; n is not null; n = n.Parent)
            {
                if (n.Tag is FuelTable t) tableId ??= t.Id;
                if (n.Tag is Car c) carId ??= c.Id;
            }

            using var form = new ImportForm(_database, carId, tableId);
            form.ShowDialog(this);
            LoadTree();
        }

        /// <summary>Imports an AEB .ml8 configuration for the selected car (or the only car).</summary>
        private void ImportConfig()
        {
            Car? car = null;
            for (var n = _tree.SelectedNode; n is not null && car is null; n = n.Parent)
                car = n.Tag as Car;

            if (car is null)
            {
                var cars = _database.GetCars();
                if (cars.Count != 1)
                {
                    MessageBox.Show(this, cars.Count == 0 ? "Add a car first." : "Select the car (or one of its fuel tables) in the tree first.",
                        "Import .ml8", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                car = cars[0];
            }

            if (Ml8Import.Run(this, _database, car) is not null)
                LoadTree();
        }

        /// <summary>
        /// Saves the stored original of the selected item: a configuration's .ml8 (or the screenshot of a table
        /// without one) or a log's .txt.
        /// </summary>
        private void DownloadSelected()
        {
            byte[]? content;
            string fileName, filter;

            switch (_tree.SelectedNode?.Tag)
            {
                case FuelTable { HasConfig: true } t:
                    content = _database.GetFuelTableConfig(t.Id);
                    fileName = t.ConfigFileName!;
                    filter = "AEB configuration (*.ml8)|*.ml8";
                    break;
                case FuelTable { HasImage: true } t:
                    content = _database.GetFuelTableImage(t.Id);
                    fileName = t.ImageFileName ?? t.Name + ".png";
                    filter = "Image|*" + Path.GetExtension(fileName);
                    break;
                case LogFile { HasContent: true } l:
                    content = _database.GetLogFileContent(l.Id);
                    fileName = l.FileName;
                    filter = "Log (*.txt)|*.txt";
                    break;
                default:
                    return;
            }

            if (content is null)
                return;

            using var dialog = new SaveFileDialog { Title = "Download", FileName = fileName, Filter = filter + "|All files (*.*)|*.*", OverwritePrompt = true };
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                File.WriteAllBytes(dialog.FileName, content);
                _summary.Text = $"Saved {dialog.FileName}";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(this, ex.Message, "Download failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void NewCar()
        {
            using var form = new CarForm(_database);
            if (form.ShowDialog(this) == DialogResult.OK)
                LoadTree();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _picture.Image?.Dispose();
                _extraInjection.Font.Dispose();
                _boldCellFont.Dispose();
                _icons.Dispose();
                foreach (var (normal, disabled) in _tabIcons.Values)
                {
                    normal.Dispose();
                    disabled.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}
