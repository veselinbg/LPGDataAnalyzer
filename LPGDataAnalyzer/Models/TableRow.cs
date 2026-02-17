namespace LPGDataAnalyzer.Models
{
    internal class TableRow
    {
        public double Key { get; set; }
        public Dictionary<int, double?> Columns { get; set; } = [];
    }
}
