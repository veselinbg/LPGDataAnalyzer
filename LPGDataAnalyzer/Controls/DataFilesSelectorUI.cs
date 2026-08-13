using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Services;
using System.Diagnostics;

namespace LPGDataAnalyzer.Controls
{
    public partial class DataFilesSelectorUI : UserControl
    {
        private AppSettings _settings { get; set; }
        public event Action<DataItem[]>? DataLoaded;
        private ContextMenuStrip? _contextMenu;
        private int _clickedIndex = -1;
        public DataFilesSelectorUI()
        {
            InitializeComponent();
        }

        public void Initialize(AppSettings appSettings)
        {
            _settings = appSettings;

            InitializeContextMenu();

            LoadFiles();

            StartWatcher();
        }
        private void InitializeContextMenu()
        {
            _contextMenu = new ContextMenuStrip();

            _contextMenu.Items.Add("Open File", null, OpenFile_Click);
            _contextMenu.Items.Add("Open Folder", null, OpenFolder_Click);

            _contextMenu.Items.Add(new ToolStripSeparator());

            _contextMenu.Items.Add("Load Selected Files", null, buttonLoad_Click);

            _contextMenu.Items.Add(new ToolStripSeparator());

            _contextMenu.Items.Add("Select All", null, SelectAll_Click);
            _contextMenu.Items.Add("Deselect All", null, DeselectAll_Click);
            _contextMenu.Items.Add("Invert Selection", null, InvertSelection_Click);

            checkedListBoxFiles.MouseDown += CheckedListBoxFiles_MouseDown;
        }
        private void InvertSelection_Click(object? sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBoxFiles.Items.Count; i++)
            {
                checkedListBoxFiles.SetItemChecked(
                    i,
                    !checkedListBoxFiles.GetItemChecked(i));
            }
        }
        private void SelectAll_Click(object? sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBoxFiles.Items.Count; i++)
            {
                checkedListBoxFiles.SetItemChecked(i, true);
            }
        }

        private void DeselectAll_Click(object? sender, EventArgs e)
        {
            for (int i = 0; i < checkedListBoxFiles.Items.Count; i++)
            {
                checkedListBoxFiles.SetItemChecked(i, false);
            }
        }
        private void CheckedListBoxFiles_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            _clickedIndex = checkedListBoxFiles.IndexFromPoint(e.Location);

            if (_clickedIndex == ListBox.NoMatches)
                return;

            checkedListBoxFiles.SelectedIndex = _clickedIndex;

            _contextMenu?.Show(checkedListBoxFiles, e.Location);
        }
        private void OpenFile_Click(object? sender, EventArgs e)
        {
            if (_clickedIndex < 0 || _clickedIndex >= _fullPaths.Count)
                return;

            string filePath = _fullPaths[_clickedIndex];

            if (File.Exists(filePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
        }
        private void OpenFolder_Click(object? sender, EventArgs e)
        {
            if (_clickedIndex < 0 || _clickedIndex >= _fullPaths.Count)
                return;

            string filePath = _fullPaths[_clickedIndex];

            if (File.Exists(filePath))
            {
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            }
        }
        private void StartWatcher()
        {
            if (!Directory.Exists(_settings.DataFilesFolder))
                return;

            _watcher?.Dispose();

            _watcher = new FileSystemWatcher(_settings.DataFilesFolder, "*.txt")
            {
                NotifyFilter =
                    NotifyFilters.FileName |
                    NotifyFilters.CreationTime |
                    NotifyFilters.LastWrite,

                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };

            _watcher.Created += OnFilesChanged;
            _watcher.Deleted += OnFilesChanged;
            _watcher.Renamed += OnFilesChanged;
        }
        private void OnFilesChanged(object sender, FileSystemEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(LoadFiles);
                return;
            }

            LoadFiles();
        }
        private readonly List<string> _fullPaths = [];

        private void LoadFiles()
        {
            var checkedFiles = checkedListBoxFiles.CheckedItems
                .Cast<string>()
                .ToHashSet();

            checkedListBoxFiles.Items.Clear();

            _fullPaths.Clear();

            if (!Directory.Exists(_settings.DataFilesFolder))
                return;

            var files = Directory
                .GetFiles(_settings.DataFilesFolder, "*.txt")
                .OrderByDescending(File.GetCreationTime)
                .ToArray();

            _fullPaths.AddRange(files);

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);

                int index = checkedListBoxFiles.Items.Add(fileName);

                if (checkedFiles.Contains(fileName))
                {
                    checkedListBoxFiles.SetItemChecked(index, true);
                }
            }
        }

        private void buttonLoad_Click(object? sender, EventArgs e)
        {
            List<DataItem> allData = [];

            foreach (int index in checkedListBoxFiles.CheckedIndices)
            {
                try
                {
                    string fullPath = _fullPaths[index];

                    var parser = new Parser();

                    parser.Load(fullPath);

                    allData.AddRange(parser.Data);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to load file:\n{_fullPaths[index]}\n\n{ex.Message}",
                        "Load Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }

            DataLoaded?.Invoke(allData.ToArray());
        }
    }
}