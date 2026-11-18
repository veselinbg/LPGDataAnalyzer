using LPGDbAnalyzer.Data;
using LPGDbAnalyzer.Services;

namespace LPGDbAnalyzer.Forms
{
    /// <summary>
    /// Imports an AEB configuration file (.ml8) for a car. Every configuration is its own node (Car → .ml8 → logs):
    /// it is shown on the check screen (map, axes, both temperature tables) and saved after the user confirms.
    /// A file already stored is not imported again; a fuel table read from a screenshot with the same map and no
    /// configuration gets the file attached instead.
    /// </summary>
    public static class Ml8Import
    {
        /// <summary>Where the AEB software saves its configurations, when it exists.</summary>
        private static string? DefaultFolder()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MultipointInj", "ConfigLPG");
            return Directory.Exists(folder) ? folder : null;
        }

        /// <summary>Returns the fuel table the configuration now belongs to, or null when cancelled.</summary>
        public static FuelTable? Run(IWin32Window owner, Database database, Car car)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "AEB configuration file",
                Filter = "AEB configuration (*.ml8)|*.ml8|All files (*.*)|*.*",
                InitialDirectory = DefaultFolder() ?? "",
            };
            if (dialog.ShowDialog(owner) != DialogResult.OK)
                return null;

            FuelTableForm.ConfigSource source;
            try
            {
                byte[] file = File.ReadAllBytes(dialog.FileName);
                source = new FuelTableForm.ConfigSource(file, Path.GetFileName(dialog.FileName),
                    File.GetLastWriteTime(dialog.FileName), Ml8Config.Load(file));
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(owner, ex.Message, "Cannot read the configuration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            var config = source.Parsed;
            var tables = database.GetFuelTables(car.Id);

            var stored = tables.FirstOrDefault(t => t.HasConfig && database.GetFuelTableConfig(t.Id) is byte[] b && b.AsSpan().SequenceEqual(source.File));
            if (stored is not null)
            {
                MessageBox.Show(owner, $"\"{source.FileName}\" is already stored as \"{stored.ConfigFileName}\".",
                    "Import .ml8", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return stored;
            }

            var same = tables.FirstOrDefault(t => !t.HasConfig && SameMap(t, config));

            if (same is not null)
            {
                if (MessageBox.Show(owner,
                        $"The map of \"{source.FileName}\" is the same as the fuel table \"{same.Name}\" (read from a screenshot).\n\n" +
                        $"Store the configuration with that table? Its logs then belong to this configuration.\n\n" +
                        $"Its temperature tables (LPG {Join(config.GasTempCoef)}, reducer {Join(config.ReducerTempCoef)}) " +
                        $"are then offered for logs recorded after {source.SavedAt:yyyy-MM-dd HH:mm}.",
                        "Import .ml8", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) is var answer && answer == DialogResult.Cancel)
                    return null;

                if (answer == DialogResult.Yes)
                {
                    try
                    {
                        database.AttachConfig(same.Id, source.File, source.FileName, source.SavedAt, config.GasTempCoef, config.ReducerTempCoef);
                        return same;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(owner, ex.Message, "Cannot store the configuration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return null;
                    }
                }
            }

            using var form = FuelTableForm.FromConfig(database, car, source);
            return form.ShowDialog(owner) == DialogResult.OK ? form.Saved : null;
        }

        private static bool SameMap(FuelTable t, Ml8Config c) =>
            SameValues(t.RpmAxis, c.RpmAxis) &&
            SameValues(t.InjectionAxis, c.InjectionAxis) &&
            t.Values.Length == c.Values.Length &&
            t.Values.Zip(c.Values).All(rows => SameValues(rows.First, rows.Second));

        private static bool SameValues(double[] a, double[] b) =>
            a.Length == b.Length && a.Zip(b).All(p => Math.Abs(p.First - p.Second) < 0.005);

        private static string Join(int[] values) => string.Join(",", values);
    }
}
