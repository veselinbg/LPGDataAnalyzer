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
        internal static List<FuelCell> BuildTable()
        {

            return
            [
            // 1.50 ms
            new FuelCell { InjBin = 1.50, RpmBin = 500,  Value = 113 },
            new FuelCell { InjBin = 1.50, RpmBin = 1000, Value = 113 },
            new FuelCell { InjBin = 1.50, RpmBin = 1500, Value = 100 },
            new FuelCell { InjBin = 1.50, RpmBin = 2000, Value = 100 },
            new FuelCell { InjBin = 1.50, RpmBin = 2500, Value = 100 },
            new FuelCell { InjBin = 1.50, RpmBin = 3000, Value = 100 },
            new FuelCell { InjBin = 1.50, RpmBin = 3500, Value = 100 },
            new FuelCell { InjBin = 1.50, RpmBin = 4000, Value = 100 },
            new FuelCell { InjBin = 1.50, RpmBin = 4500, Value = 100 },
            new FuelCell { InjBin = 1.50, RpmBin = 5000, Value = 100 },
            new FuelCell { InjBin = 1.50, RpmBin = 5500, Value = 100 },
            new FuelCell { InjBin = 1.50, RpmBin = 6200, Value = 100 },

            // 2.50 ms
            new FuelCell { InjBin = 2.50, RpmBin = 500,  Value = 115 },
            new FuelCell { InjBin = 2.50, RpmBin = 1000, Value = 115 },
            new FuelCell { InjBin = 2.50, RpmBin = 1500, Value = 115 },
            new FuelCell { InjBin = 2.50, RpmBin = 2000, Value = 116 },
            new FuelCell { InjBin = 2.50, RpmBin = 2500, Value = 117 },
            new FuelCell { InjBin = 2.50, RpmBin = 3000, Value = 118 },
            new FuelCell { InjBin = 2.50, RpmBin = 3500, Value = 118 },
            new FuelCell { InjBin = 2.50, RpmBin = 4000, Value = 118 },
            new FuelCell { InjBin = 2.50, RpmBin = 4500, Value = 118 },
            new FuelCell { InjBin = 2.50, RpmBin = 5000, Value = 118 },
            new FuelCell { InjBin = 2.50, RpmBin = 5500, Value = 118 },
            new FuelCell { InjBin = 2.50, RpmBin = 6200, Value = 118 },

            // 3.00 ms
            new FuelCell { InjBin = 3.00, RpmBin = 500,  Value = 120 },
            new FuelCell { InjBin = 3.00, RpmBin = 1000, Value = 122 },
            new FuelCell { InjBin = 3.00, RpmBin = 1500, Value = 127 },
            new FuelCell { InjBin = 3.00, RpmBin = 2000, Value = 131 },
            new FuelCell { InjBin = 3.00, RpmBin = 2500, Value = 132 },
            new FuelCell { InjBin = 3.00, RpmBin = 3000, Value = 132 },
            new FuelCell { InjBin = 3.00, RpmBin = 3500, Value = 133 },
            new FuelCell { InjBin = 3.00, RpmBin = 4000, Value = 133 },
            new FuelCell { InjBin = 3.00, RpmBin = 4500, Value = 134 },
            new FuelCell { InjBin = 3.00, RpmBin = 5000, Value = 134 },
            new FuelCell { InjBin = 3.00, RpmBin = 5500, Value = 134 },
            new FuelCell { InjBin = 3.00, RpmBin = 6200, Value = 134 },

            // 3.50 ms
            new FuelCell { InjBin = 3.50, RpmBin = 500,  Value = 123 },
            new FuelCell { InjBin = 3.50, RpmBin = 1000, Value = 131 },
            new FuelCell { InjBin = 3.50, RpmBin = 1500, Value = 132 },
            new FuelCell { InjBin = 3.50, RpmBin = 2000, Value = 132 },
            new FuelCell { InjBin = 3.50, RpmBin = 2500, Value = 133 },
            new FuelCell { InjBin = 3.50, RpmBin = 3000, Value = 133 },
            new FuelCell { InjBin = 3.50, RpmBin = 3500, Value = 134 },
            new FuelCell { InjBin = 3.50, RpmBin = 4000, Value = 134 },
            new FuelCell { InjBin = 3.50, RpmBin = 4500, Value = 135 },
            new FuelCell { InjBin = 3.50, RpmBin = 5000, Value = 135 },
            new FuelCell { InjBin = 3.50, RpmBin = 5500, Value = 135 },
            new FuelCell { InjBin = 3.50, RpmBin = 6200, Value = 135 },

            // 4.50 ms
            new FuelCell { InjBin = 4.50, RpmBin = 500,  Value = 124 },
            new FuelCell { InjBin = 4.50, RpmBin = 1000, Value = 132 },
            new FuelCell { InjBin = 4.50, RpmBin = 1500, Value = 133 },
            new FuelCell { InjBin = 4.50, RpmBin = 2000, Value = 133 },
            new FuelCell { InjBin = 4.50, RpmBin = 2500, Value = 134 },
            new FuelCell { InjBin = 4.50, RpmBin = 3000, Value = 134 },
            new FuelCell { InjBin = 4.50, RpmBin = 3500, Value = 135 },
            new FuelCell { InjBin = 4.50, RpmBin = 4000, Value = 135 },
            new FuelCell { InjBin = 4.50, RpmBin = 4500, Value = 136 },
            new FuelCell { InjBin = 4.50, RpmBin = 5000, Value = 136 },
            new FuelCell { InjBin = 4.50, RpmBin = 5500, Value = 137 },
            new FuelCell { InjBin = 4.50, RpmBin = 6200, Value = 137 },

            // 5.50 ms
            new FuelCell { InjBin = 5.50, RpmBin = 500,  Value = 127 },
            new FuelCell { InjBin = 5.50, RpmBin = 1000, Value = 132 },
            new FuelCell { InjBin = 5.50, RpmBin = 1500, Value = 133 },
            new FuelCell { InjBin = 5.50, RpmBin = 2000, Value = 133 },
            new FuelCell { InjBin = 5.50, RpmBin = 2500, Value = 134 },
            new FuelCell { InjBin = 5.50, RpmBin = 3000, Value = 134 },
            new FuelCell { InjBin = 5.50, RpmBin = 3500, Value = 135 },
            new FuelCell { InjBin = 5.50, RpmBin = 4000, Value = 135 },
            new FuelCell { InjBin = 5.50, RpmBin = 4500, Value = 136 },
            new FuelCell { InjBin = 5.50, RpmBin = 5000, Value = 136 },
            new FuelCell { InjBin = 5.50, RpmBin = 5500, Value = 137 },
            new FuelCell { InjBin = 5.50, RpmBin = 6200, Value = 137 },

             // 6.50 ms
            new FuelCell { InjBin = 6.50, RpmBin = 500,  Value = 128 },
            new FuelCell { InjBin = 6.50, RpmBin = 1000, Value = 133 },
            new FuelCell { InjBin = 6.50, RpmBin = 1500, Value = 134 },
            new FuelCell { InjBin = 6.50, RpmBin = 2000, Value = 134 },
            new FuelCell { InjBin = 6.50, RpmBin = 2500, Value = 135 },
            new FuelCell { InjBin = 6.50, RpmBin = 3000, Value = 135 },
            new FuelCell { InjBin = 6.50, RpmBin = 3500, Value = 136 },
            new FuelCell { InjBin = 6.50, RpmBin = 4000, Value = 136 },
            new FuelCell { InjBin = 6.50, RpmBin = 4500, Value = 137 },
            new FuelCell { InjBin = 6.50, RpmBin = 5000, Value = 137 },
            new FuelCell { InjBin = 6.50, RpmBin = 5500, Value = 138 },
            new FuelCell { InjBin = 6.50, RpmBin = 6200, Value = 138 },

            // 7.50 ms
            new FuelCell { InjBin = 7.50, RpmBin = 500,  Value = 128 },
            new FuelCell { InjBin = 7.50, RpmBin = 1000, Value = 133 },
            new FuelCell { InjBin = 7.50, RpmBin = 1500, Value = 134 },
            new FuelCell { InjBin = 7.50, RpmBin = 2000, Value = 134 },
            new FuelCell { InjBin = 7.50, RpmBin = 2500, Value = 135 },
            new FuelCell { InjBin = 7.50, RpmBin = 3000, Value = 135 },
            new FuelCell { InjBin = 7.50, RpmBin = 3500, Value = 136 },
            new FuelCell { InjBin = 7.50, RpmBin = 4000, Value = 136 },
            new FuelCell { InjBin = 7.50, RpmBin = 4500, Value = 137 },
            new FuelCell { InjBin = 7.50, RpmBin = 5000, Value = 137 },
            new FuelCell { InjBin = 7.50, RpmBin = 5500, Value = 138 },
            new FuelCell { InjBin = 7.50, RpmBin = 6200, Value = 138 },

            // 8.50 ms
            new FuelCell { InjBin = 8.50, RpmBin = 500,  Value = 128 },
            new FuelCell { InjBin = 8.50, RpmBin = 1000, Value = 133 },
            new FuelCell { InjBin = 8.50, RpmBin = 1500, Value = 134 },
            new FuelCell { InjBin = 8.50, RpmBin = 2000, Value = 134 },
            new FuelCell { InjBin = 8.50, RpmBin = 2500, Value = 135 },
            new FuelCell { InjBin = 8.50, RpmBin = 3000, Value = 135 },
            new FuelCell { InjBin = 8.50, RpmBin = 3500, Value = 136 },
            new FuelCell { InjBin = 8.50, RpmBin = 4000, Value = 136 },
            new FuelCell { InjBin = 8.50, RpmBin = 4500, Value = 137 },
            new FuelCell { InjBin = 8.50, RpmBin = 5000, Value = 137 },
            new FuelCell { InjBin = 8.50, RpmBin = 5500, Value = 138 },
            new FuelCell { InjBin = 8.50, RpmBin = 6200, Value = 138 },

            // 9.50 ms
            new FuelCell { InjBin = 9.50, RpmBin = 500,  Value = 129 },
            new FuelCell { InjBin = 9.50, RpmBin = 1000, Value = 134 },
            new FuelCell { InjBin = 9.50, RpmBin = 1500, Value = 135 },
            new FuelCell { InjBin = 9.50, RpmBin = 2000, Value = 135 },
            new FuelCell { InjBin = 9.50, RpmBin = 2500, Value = 136 },
            new FuelCell { InjBin = 9.50, RpmBin = 3000, Value = 136 },
            new FuelCell { InjBin = 9.50, RpmBin = 3500, Value = 137 },
            new FuelCell { InjBin = 9.50, RpmBin = 4000, Value = 137 },
            new FuelCell { InjBin = 9.50, RpmBin = 4500, Value = 138 },
            new FuelCell { InjBin = 9.50, RpmBin = 5000, Value = 139 },
            new FuelCell { InjBin = 9.50, RpmBin = 5500, Value = 140 },
            new FuelCell { InjBin = 9.50, RpmBin = 6200, Value = 141 },

             // 10.50 ms
            new FuelCell { InjBin = 10.50, RpmBin = 500,  Value = 129 },
            new FuelCell { InjBin = 10.50, RpmBin = 1000, Value = 134 },
            new FuelCell { InjBin = 10.50, RpmBin = 1500, Value = 135 },
            new FuelCell { InjBin = 10.50, RpmBin = 2000, Value = 135 },
            new FuelCell { InjBin = 10.50, RpmBin = 2500, Value = 136 },
            new FuelCell { InjBin = 10.50, RpmBin = 3000, Value = 136 },
            new FuelCell { InjBin = 10.50, RpmBin = 3500, Value = 137 },
            new FuelCell { InjBin = 10.50, RpmBin = 4000, Value = 137 },
            new FuelCell { InjBin = 10.50, RpmBin = 4500, Value = 139 },
            new FuelCell { InjBin = 10.50, RpmBin = 5000, Value = 140 },
            new FuelCell { InjBin = 10.50, RpmBin = 5500, Value = 141 },
            new FuelCell { InjBin = 10.50, RpmBin = 6200, Value = 141 },

             // 15 ms
            new FuelCell { InjBin = 15, RpmBin = 500,  Value = 130 },
            new FuelCell { InjBin = 15, RpmBin = 1000, Value = 135 },
            new FuelCell { InjBin = 15, RpmBin = 1500, Value = 136 },
            new FuelCell { InjBin = 15, RpmBin = 2000, Value = 137 },
            new FuelCell { InjBin = 15, RpmBin = 2500, Value = 138 },
            new FuelCell { InjBin = 15, RpmBin = 3000, Value = 139 },
            new FuelCell { InjBin = 15, RpmBin = 3500, Value = 140 },
            new FuelCell { InjBin = 15, RpmBin = 4000, Value = 140 },
            new FuelCell { InjBin = 15, RpmBin = 4500, Value = 141 },
            new FuelCell { InjBin = 15, RpmBin = 5000, Value = 141 },
            new FuelCell { InjBin = 15, RpmBin = 5500, Value = 142 },
            new FuelCell { InjBin = 15, RpmBin = 6200, Value = 143 },
            ];
        }
    }
}
