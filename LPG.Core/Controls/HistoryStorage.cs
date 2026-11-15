using LPGDataAnalyzer.Models;
using System.Text.Json;

namespace LPGDataAnalyzer.Controls
{
    public class HistorySnapshot
    {
        public required DataItem[] Logs { get; set; }

        public required double?[][] CellMap { get; set; }

        public required double?[][] NewCellMap { get; set; }

        public DateTime Created { get; set; }

        /// <summary>Database id of a stored prediction (LPGDbAnalyzer); 0 for a JSON snapshot.</summary>
        public long Id { get; set; }

        /// <summary>Text shown in the history list instead of the default one.</summary>
        public string? Title { get; set; }

        /// <summary>Options the prediction was made with; null for older JSON snapshots.</summary>
        public PredictionOptions? Options { get; set; }

        /// <summary>
        /// Readings per cell as [rpm][injection], used instead of <see cref="Logs"/> when the logs
        /// are not kept with the snapshot (stored predictions in LPGDbAnalyzer).
        /// </summary>
        public int[][]? Counts { get; set; }

        public string Name => Title ?? $"{Created:yyyy-MM-dd HH:mm:ss}  Logs:{Logs?.Length ?? 0}";
    }
    public static class ArrayConverter
    {
        /// <summary>
        /// Converts 2D rectangular array to jagged array with pre-allocation for better cache locality.
        /// </summary>
        public static double?[][] ToJagged(double?[,] array)
        {
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);

            double?[][] result = new double?[rows][];

            // Pre-allocate all rows first for better cache locality
            for (int r = 0; r < rows; r++)
            {
                result[r] = new double?[cols];
            }

            // Then copy data
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    result[r][c] = array[r, c];
                }
            }

            return result;
        }

        /// <summary>
        /// Converts jagged array to 2D rectangular array.
        /// </summary>
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
        public static HistorySnapshot? Load(string path)
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
        public void Insert(int index, HistorySnapshot snapshot)
        {
            history.Insert(index, snapshot);
        }
        public void RemoveAt(int index)
        {
            history.RemoveAt(index);
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
                throw new IndexOutOfRangeException($"Invalid index {index}.");

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

            var files = Directory.GetFiles(directoryPath, "*.json").OrderByDescending(f => File.GetCreationTime(f));

            foreach (var file in files)
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
}
