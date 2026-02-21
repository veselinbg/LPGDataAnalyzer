namespace LPGDataAnalyzer.Models
{
    internal class TableRow
    {
        // Injection axis (row)
        public double Key { get; set; }

        // RPM axis (column)
        public Dictionary<int, double?> Columns { get; set; } = new();

        private static Dictionary<(int rpm, double inj), double>
           FlattenTable(IEnumerable<TableRow> table) =>
           table.SelectMany(r =>
               r.Columns
                .Where(c => c.Value.HasValue)
                .Select(c => ((c.Key, r.Key), c.Value!.Value)))
           .ToDictionary(x => x.Item1, x => x.Value);

        private static void ApplyToTable(
            Dictionary<(int rpm, double inj), double> values,
            List<TableRow> table)
        {
            foreach (var row in table)
            {
                foreach (var rpm in row.Columns.Keys.ToList())
                {
                    if (values.TryGetValue((rpm, row.Key), out var v))
                        row.Columns[rpm] = Math.Round(v, 0);
                }
            }
        }

    }
}
