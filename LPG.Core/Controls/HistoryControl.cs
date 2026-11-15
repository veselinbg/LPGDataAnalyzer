using LPGDataAnalyzer.Models;
using System.ComponentModel;

namespace LPGDataAnalyzer.Controls
{
    /// <summary>
    /// List of prediction snapshots. In LPGDataAnalyzer they are JSON files (Save / Load / Clear).
    /// In database mode (LPGDbAnalyzer) the host fills the list with stored predictions and
    /// deletes them through <see cref="DeleteSnapshot"/>.
    /// </summary>
    public partial class HistoryControl : UserControl
    {
        public HistoryManager Manager { get; } = new();

        public event Action<HistorySnapshot>? HistorySelected;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public AppSettings? AppSettings { get; set; }

        /// <summary>Deletes a stored snapshot; returns false when it was not deleted. Database mode only.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<HistorySnapshot, bool>? DeleteSnapshot { get; set; }

        ListBox listHistory = new() { IntegralHeight = false, HorizontalScrollbar = true };
        Button btnSave = new();
        Button btnLoad = new();
        Button btnClear = new();
        Button btnDelete = new() { Visible = false };
        Label title = new() { Visible = false, AutoSize = false, Height = 22, TextAlign = ContentAlignment.MiddleLeft };

        public HistoryControl()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            Dock = DockStyle.Fill;

            listHistory.Dock = DockStyle.Fill;

            btnSave.Text = "Save";
            btnLoad.Text = "Load";
            btnClear.Text = "Clear";
            btnDelete.Text = "Delete selected prediction";

            btnSave.Dock = DockStyle.Top;
            btnLoad.Dock = DockStyle.Top;
            btnClear.Dock = DockStyle.Top;
            btnDelete.Dock = DockStyle.Top;
            title.Dock = DockStyle.Top;

            Controls.Add(listHistory);
            Controls.Add(btnDelete);
            Controls.Add(btnLoad);
            Controls.Add(btnSave);
            Controls.Add(btnClear);
            Controls.Add(title);

            listHistory.SelectedIndexChanged += ListHistory_SelectedIndexChanged;
            btnSave.Click += BtnSave_Click;
            btnLoad.Click += BtnLoad_Click;
            btnClear.Click += BtnClear_Click;
            btnDelete.Click += BtnDelete_Click;
        }

        /// <summary>
        /// Database mode: no JSON files; the list holds stored predictions and can delete them.
        /// </summary>
        public void UseDatabase(string caption)
        {
            btnSave.Visible = btnLoad.Visible = btnClear.Visible = false;
            btnDelete.Visible = true;
            title.Visible = true;
            title.Text = caption;
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            Manager.Clear();
            listHistory.Items.Clear();
        }
        public void ClearAddSnapshots(IReadOnlyList<HistorySnapshot> snapshots)
        {
            Manager.Clear();
            listHistory.Items.Clear();

            AddSnapshots(snapshots);
        }
        public void AddSnapshots(IReadOnlyList<HistorySnapshot> snapshots)
        {
            Manager.AddRange(snapshots);

            foreach (var snapshot in snapshots)
            {
                listHistory.Items.Add(snapshot.Name);
            }
        }
        public void AddSnapshot(DataItem[] logs, double?[,] cellMap, double?[,] newCellMap)
        {
            Manager.Add(logs, cellMap, newCellMap);

            listHistory.Items.Add(Manager.Items.Last().Name);
        }

        /// <summary>Puts a new snapshot at the top of the list (newest first) without selecting it.</summary>
        public void InsertFirst(HistorySnapshot snapshot)
        {
            Manager.Insert(0, snapshot);
            listHistory.Items.Insert(0, snapshot.Name);
        }

        private void ListHistory_SelectedIndexChanged(object? sender, EventArgs e)
        {
            int index = listHistory.SelectedIndex;
            if (index < 0 || index >= Manager.Items.Count)
                return;

            var snapshot = Manager.Get(index);

            HistorySelected?.Invoke(snapshot);
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            int index = listHistory.SelectedIndex;
            if (index < 0 || DeleteSnapshot is null)
                return;

            var snapshot = Manager.Get(index);
            if (MessageBox.Show(this, $"Delete the stored prediction \"{snapshot.Name}\"?", "Delete prediction",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            if (!DeleteSnapshot(snapshot))
                return;

            Manager.RemoveAt(index);
            listHistory.Items.RemoveAt(index);
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (listHistory.SelectedIndex < 0) return;

            var snapshot = Manager.Get(listHistory.SelectedIndex);

            using SaveFileDialog dlg = new();
            dlg.InitialDirectory = AppSettings?.HistoryFolder ?? string.Empty;
            dlg.FileName = $"snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            dlg.Filter = "History|*.json";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                HistoryStorage.Save(dlg.FileName, snapshot);
            }
        }

        private void BtnLoad_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog dlg = new();
            dlg.InitialDirectory = AppSettings?.HistoryFolder ?? string.Empty;
            dlg.Filter = "History|*.json";

            if (dlg.ShowDialog() == DialogResult.OK)
            {

                var snapshot = HistoryStorage.Load(dlg.FileName);
                if (snapshot == null)
                    return;

                Manager.Add(snapshot);

                listHistory.Items.Add(snapshot.Name);
            }
        }
    }
}
