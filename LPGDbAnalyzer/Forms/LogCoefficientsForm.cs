using LPGDataAnalyzer.Models;
using LPGDbAnalyzer.Data;
using System.Globalization;

namespace LPGDbAnalyzer.Forms
{
    /// <summary>
    /// Edits the ECU temperature tables stored for one imported log (one grid row per table,
    /// one column per temperature band of LPG.Core's Settings).
    /// </summary>
    public sealed class LogCoefficientsForm : Form
    {
        private readonly Database _database;
        private readonly LogFile _log;
        private readonly DataGridView _grid = new()
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        };

        protected override void OnLoad(EventArgs e)
        {
            // Buttons grow to fit their text (Windows scaling above 100%).
            LPGDataAnalyzer.Controls.ControlSizing.FitButtons(this);
            base.OnLoad(e);
        }

        public LogCoefficientsForm(Database database, LogFile log)
        {
            _database = database;
            _log = log;

            Text = $"Temperature tables - {log.FileName}";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(900, 230);
            MinimumSize = new Size(600, 200);

            int bands = Math.Max(Settings.GasTemperatureRanges.Length, Settings.ReductorTemperatureRanges.Length);
            for (int i = 0; i < bands; i++)
                _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = $"Band {i + 1}", SortMode = DataGridViewColumnSortMode.NotSortable });

            AddRow("LPG temperature", Settings.GasTemperatureRanges, log.GasTempCoef);
            AddRow("Reducer temperature", Settings.ReductorTemperatureRanges, log.ReducerTempCoef);

            var info = new Label
            {
                Dock = DockStyle.Top,
                Height = 34,
                Padding = new Padding(4),
                Text = "The ECU temperature tables that were set while this log was recorded. Hover a cell to see its band.",
            };

            var save = new Button { Text = "Save", Width = 90 };
            var cancel = new Button { Text = "Cancel", Width = 90, DialogResult = DialogResult.Cancel };
            save.Click += Save_Click;
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 40, Padding = new Padding(6) };
            buttons.Controls.AddRange([cancel, save]);

            Controls.Add(_grid);
            Controls.Add(info);
            Controls.Add(buttons);
            AcceptButton = save;
            CancelButton = cancel;
        }

        private void AddRow(string name, (int Min, int Max, string Label)[] ranges, int[] values)
        {
            int index = _grid.Rows.Add();
            var row = _grid.Rows[index];
            row.HeaderCell.Value = name;

            for (int c = 0; c < _grid.ColumnCount; c++)
            {
                var cell = row.Cells[c];
                if (c < ranges.Length)
                {
                    cell.Value = c < values.Length ? values[c].ToString(CultureInfo.InvariantCulture) : "";
                    cell.ToolTipText = ranges[c].Label;
                }
                else
                {
                    cell.ReadOnly = true;
                    cell.Style.BackColor = Color.Gainsboro;
                }
            }
        }

        private int[] ReadRow(int row, int bands)
        {
            var values = new int[bands];
            for (int c = 0; c < bands; c++)
            {
                var text = _grid.Rows[row].Cells[c].Value?.ToString()?.Trim();
                if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out values[c]))
                    throw new InvalidOperationException($"{_grid.Rows[row].HeaderCell.Value}, band {c + 1}: '{text}' is not a whole number.");
            }
            return values;
        }

        private void Save_Click(object? sender, EventArgs e)
        {
            try
            {
                _grid.EndEdit();
                var gas = ReadRow(0, Settings.GasTemperatureRanges.Length);
                var reducer = ReadRow(1, Settings.ReductorTemperatureRanges.Length);

                _database.UpdateLogCoefficients(_log.Id, gas, reducer);
                _log.GasTempCoef = gas;
                _log.ReducerTempCoef = reducer;
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Cannot save", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
