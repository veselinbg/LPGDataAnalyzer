using LPGDataAnalyzer.Models;
using System.ComponentModel;
using System.Text.Json;

namespace LPGDataAnalyzer.Controls
{
    public class HistorySnapshot
    {
        public DataItem[] Logs { get; set; }

        public double?[][] CellMap { get; set; }

        public double?[][] NewCellMap { get; set; }

        public DateTime Created { get; set; }

        public string Name => $"{Created:yyyy-MM-dd HH:mm:ss}  Logs:{Logs?.Length ?? 0}";
    }
    public static class ArrayConverter
    {
        public static double?[][] ToJagged(double?[,] array)
        {
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);

            double?[][] result = new double?[rows][];

            for (int r = 0; r < rows; r++)
            {
                result[r] = new double?[cols];
                for (int c = 0; c < cols; c++)
                    result[r][c] = array[r, c];
            }

            return result;
        }

        public static double?[,] To2D(double?[][] jagged)
        {
            int rows = jagged.Length;
            int cols = jagged[0].Length;

            var arr = new double?[rows, cols];

            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    arr[r, c] = jagged[r][c];

            return arr;
        }
    }
    public static class HistoryStorage
    {
        public static void Save(string path, HistorySnapshot snapshot)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(snapshot, options);
            File.WriteAllText(path, json);
        }
        public static HistorySnapshot Load(string path)
        {
            string json = File.ReadAllText(path);

            var snapshot = JsonSerializer.Deserialize<HistorySnapshot>(json);

            return snapshot;
        }
    }
    public class HistoryManager
    {
        private readonly List<HistorySnapshot> history = new();

        public IReadOnlyList<HistorySnapshot> Items => history;
        public void AddRange(IEnumerable<HistorySnapshot> snapshots)
        {
            history.AddRange(snapshots);
        }
        public void Add(HistorySnapshot snapshot)
        {
            history.Add(snapshot); 
        }
        public void Add(DataItem[] logs, double?[,] cellMap, double?[,] newCellMap)
        {
            history.Add(new HistorySnapshot
            {
                Logs = logs.ToArray(),
                CellMap = ArrayConverter.ToJagged(cellMap),
                NewCellMap = ArrayConverter.ToJagged(newCellMap),
                Created = DateTime.Now
            });
        }

        public HistorySnapshot Get(int index)
        {
            if (index < 0 || index >= history.Count)
                return null;

            return history[index];
        }
        public void Clear()
        {
            history.Clear();
        }
        public void ClearAndLoadFromDirectory(string directoryPath)
        {
            Clear();
            LoadFromDirectory(directoryPath);
        }
        public void LoadFromDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return;

            foreach (var file in Directory.GetFiles(directoryPath, "*.json"))
            {
                try
                {
                    var snapshot = HistoryStorage.Load(file);
                    if (snapshot != null)
                        history.Add(snapshot);
                }
                catch
                {
                    // Optionally log or skip invalid JSON files
                }
            }
        }
    }
    public static class HistoryHelper
    {
        public static List<double> GetCellHistoryValues(
            IEnumerable<HistorySnapshot> history,
            int rpmIndex,
            int injIndex)
        {
            var values = new List<double>();

            foreach (var snapshot in history)
            {
                if (snapshot.CellMap == null || snapshot.NewCellMap == null)
                    continue;

                var oldValue = snapshot.CellMap[rpmIndex][injIndex];
                var newValue = snapshot.NewCellMap[rpmIndex][injIndex];

                // Only take changed cells
                if (oldValue != newValue && newValue.HasValue)
                {
                    values.Add(newValue.Value);
                }
            }

            return values;
        }
    }

}
