using LPGDbAnalyzer.Data;
using LPGDbAnalyzer.Services;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LPGDbAnalyzer.Forms
{
    /// <summary>
    /// Writes a predicted fuel map as a new AEB configuration file (.ml8), to be opened in the AEB software,
    /// checked there and written to the ECU. The template is the .ml8 stored with the fuel table, so every
    /// other setting stays exactly as it is in the ECU.
    /// </summary>
    public static class Ml8Export
    {
        /// <param name="map">The predicted map, [rpm, injection]; empty cells keep the fuel table's value.</param>
        /// <returns>True when a file was written.</returns>
        public static bool Run(IWin32Window owner, Database database, FuelTable table, double?[,] map)
        {
            // Fresh from the database: the configuration may have been imported after the logs were loaded.
            table = database.GetFuelTables(table.CarId).FirstOrDefault(t => t.Id == table.Id) ?? table;
            byte[]? template = database.GetFuelTableConfig(table.Id);
            if (template is null)
            {
                MessageBox.Show(owner,
                    $"The fuel table \"{table.Name}\" has no .ml8 configuration.\n\n" +
                    "Save the configuration in the AEB software, then use \"Import .ml8...\" on the Data tab: " +
                    "it is attached to this table when the map is the same. The export uses it as the template.",
                    "Export .ml8", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            try
            {
                var config = Ml8Config.Load(template);
                if (!SameAxes(config.RpmAxis, table.RpmAxis) || !SameAxes(config.InjectionAxis, table.InjectionAxis))
                    throw new InvalidOperationException(
                        $"The axes of \"{table.ConfigFileName}\" differ from the fuel table's axes, so the map cannot be written into it.");

                int rows = table.InjectionAxis.Length, columns = table.RpmAxis.Length;
                if (map.GetLength(0) != columns || map.GetLength(1) != rows)
                    throw new InvalidOperationException($"The prediction is {map.GetLength(0)} x {map.GetLength(1)}, the fuel table {columns} x {rows}.");

                // [injection row][rpm column], whole numbers (the ECU accepts nothing else).
                var values = new int[rows][];
                int changed = 0;
                for (int r = 0; r < rows; r++)
                {
                    values[r] = new int[columns];
                    for (int c = 0; c < columns; c++)
                    {
                        values[r][c] = (int)Math.Round(map[c, r] ?? table.Values[r][c], MidpointRounding.AwayFromZero);
                        if (values[r][c] != config.Values[r][c])
                            changed++;
                    }
                }

                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MultipointInj", "ConfigLPG");
                using var dialog = new SaveFileDialog
                {
                    Title = "New AEB configuration with the predicted map",
                    Filter = "AEB configuration (*.ml8)|*.ml8",
                    FileName = NextName(table.ConfigFileName ?? table.Name),
                    InitialDirectory = Directory.Exists(folder) ? folder : "",
                    OverwritePrompt = true,
                };
                if (dialog.ShowDialog(owner) != DialogResult.OK)
                    return false;

                if (string.Equals(Path.GetFileName(dialog.FileName), table.ConfigFileName, StringComparison.OrdinalIgnoreCase) &&
                    MessageBox.Show(owner, $"\"{table.ConfigFileName}\" is the configuration now in the ECU: keep it as the backup.\n\nOverwrite it anyway?",
                        "Export .ml8", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                    return false;

                string description = Path.GetFileNameWithoutExtension(dialog.FileName);
                File.WriteAllBytes(dialog.FileName, Ml8Config.WithMap(template, values, description));

                MessageBox.Show(owner,
                    $"Saved \"{Path.GetFileName(dialog.FileName)}\": {changed} of {rows * columns} map cells differ from " +
                    $"\"{table.ConfigFileName}\". Everything else (temperature tables, all other settings) is unchanged.\n\n" +
                    "Open it in the AEB software, compare the map with the prediction, and only then write it to the ECU. " +
                    $"Keep \"{table.ConfigFileName}\" as the backup.",
                    "Export .ml8", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            catch (Exception ex) when (ex is InvalidOperationException or InvalidDataException or IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(owner, ex.Message, "Export .ml8", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        /// <summary>"baracuda v2.94.ml8" → "baracuda v2.95.ml8": the last number of the name plus one (same digit count).</summary>
        private static string NextName(string name)
        {
            string stem = Path.GetFileNameWithoutExtension(name);
            var last = Regex.Matches(stem, @"\d+").LastOrDefault();
            if (last is null)
                return stem + " predicted" + Ml8Config.FileExtension;

            string next = (long.Parse(last.Value, CultureInfo.InvariantCulture) + 1).ToString(CultureInfo.InvariantCulture)
                .PadLeft(last.Value.Length, '0');
            return stem[..last.Index] + next + stem[(last.Index + last.Length)..] + Ml8Config.FileExtension;
        }

        private static bool SameAxes(double[] a, double[] b) =>
            a.Length == b.Length && a.Zip(b).All(p => Math.Abs(p.First - p.Second) < 0.005);
    }
}
