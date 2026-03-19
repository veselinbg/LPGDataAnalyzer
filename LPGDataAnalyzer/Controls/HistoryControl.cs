using LPGDataAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace LPGDataAnalyzer.Controls
{
    public partial class HistoryControl : UserControl
    {
        public HistoryManager Manager { get; } = new();

        public event Action<HistorySnapshot> HistorySelected;

        ListBox listHistory = new();
        Button btnSave = new();
        Button btnLoad = new();
        Button btnClear = new();
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

            btnSave.Dock = DockStyle.Top;
            btnLoad.Dock = DockStyle.Top;
            btnClear.Dock = DockStyle.Top;

            Controls.Add(listHistory);
            Controls.Add(btnLoad);
            Controls.Add(btnSave);
            Controls.Add(btnClear);

            listHistory.SelectedIndexChanged += ListHistory_SelectedIndexChanged;
            btnSave.Click += BtnSave_Click;
            btnLoad.Click += BtnLoad_Click;
            btnClear.Click += BtnClear_Click;
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            Manager.Clear();
            listHistory.Items.Clear();
        }
        public void ClearAddSnapshots(HistorySnapshot[] snapshots)
        {
            Manager.Clear();
            listHistory.Items.Clear();

            AddSnapshots(snapshots);
        }
        public void AddSnapshots(HistorySnapshot[] snapshots)
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

        private void ListHistory_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = listHistory.SelectedIndex;

            var snapshot = Manager.Get(index);

            HistorySelected?.Invoke(snapshot);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (listHistory.SelectedIndex < 0) return;

            var snapshot = Manager.Get(listHistory.SelectedIndex);

            SaveFileDialog dlg = new();
            dlg.FileName = $"snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            dlg.Filter = "History|*.json";

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            HistoryStorage.Save(dlg.FileName, snapshot);
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new();
            dlg.InitialDirectory = "C:\\Users\\veselin.ivanov\\Downloads\\LPGDataAnalyzer\\LPGDataAnalyzer\\History";
            dlg.Filter = "History|*.json";

            if (dlg.ShowDialog() != DialogResult.OK)
                return;
            
            var snapshot = HistoryStorage.Load(dlg.FileName);

            Manager.Add(snapshot);

            listHistory.Items.Add(snapshot.Name);
        }
    }
}
