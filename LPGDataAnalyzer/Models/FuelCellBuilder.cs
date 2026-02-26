namespace LPGDataAnalyzer.Models
{
    internal class FuelCellBuilder
    {
        public static List<TableRow> BuildTableRow(IEnumerable<FuelCell> fuelCellTable)
        {
            var table = Settings.InjectionRanges.Select(r => new TableRow
            {
                Key = r.Label,
                Columns = Settings.RpmColumns.ToDictionary(
                   c => c.Label,
                   c =>
                   {
                       var values = fuelCellTable
                           .Where(d =>
                               d.InjBin > r.Min &&
                               d.InjBin <= r.Max &&
                               d.RpmBin > c.Min &&
                               d.RpmBin <= c.Max)
                           .Select(d => d.Value);

                       return values.Any()
                           ? values.Average().Round()
                           : (double?)null;
                   })
            });
            return [..table];
        }
        internal static IEnumerable<FuelCell> BuildTable(IList<int> values)
        {
            int i = 0;
            foreach (var inj in Settings.InjectionRanges)
            {
                foreach (var rpm in Settings.RpmColumns)
                {
                    yield return new FuelCell { InjBin = inj.Label, RpmBin = rpm.Label, Value = i < values.Count ? values[i++] : 0 };
                }
            }
        }
    }
}
