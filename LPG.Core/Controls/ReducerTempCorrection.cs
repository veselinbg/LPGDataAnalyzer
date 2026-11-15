using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Services;
using System.ComponentModel;
using System.Globalization;

namespace LPGDataAnalyzer.Controls
{
    /// <summary>
    /// Suggests the ECU temperature tables (LPG temperature and reducer temperature), both together
    /// (see <see cref="TemperatureCorrectionPrediction"/>). One button on top; two halves side by side,
    /// each: current values and the result grid that fills the rest.
    /// </summary>
    public class ReducerTempCorrection : UserControl
    {
        private readonly TextBox textBox1 = new() { Dock = DockStyle.Fill };
        private readonly TextBox textBoxReducerTempValues = new() { Dock = DockStyle.Fill };
        private readonly DataGridView gridGas = CreateGrid();
        private readonly DataGridView gridReducer = CreateGrid();
        private readonly Label resultGas = CreateResultLabel();
        private readonly Label resultReducer = CreateResultLabel();

        public ReducerTempCorrection()
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(4) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var top = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Dock = DockStyle.Fill, Margin = new Padding(4) };
            var button = new Button { Text = "Suggest temperature tables", AutoSize = true };
            button.Click += ButtonSuggest_Click;
            top.Controls.Add(button);
            top.Controls.Add(new Label
            {
                AutoSize = true,
                Margin = new Padding(9, 8, 3, 3),
                Text = "Both tables are fitted together from the trims left after the map error. " +
                       "The ECU blends each value from the band's upper temperature to the next one; values are % of gas time.",
            });
            layout.Controls.Add(top, 0, 0);
            layout.SetColumnSpan(top, 2);

            layout.Controls.Add(Section("Gas (LPG) temperature table", textBox1, gridGas, resultGas), 0, 1);
            layout.Controls.Add(Section("Reducer temperature table", textBoxReducerTempValues, gridReducer, resultReducer), 1, 1);

            Controls.Add(layout);
            Size = new Size(900, 500);

            textBox1.Text = string.Join(",", Settings.GasTemperatureCorrectionCoef);
            textBoxReducerTempValues.Text = string.Join(",", Settings.ReductorTemperatureCorrectionCoef);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataItem[]? Data { get; set; }

        /// <summary>Reducer pressure the trims are scaled to, as in the map prediction (the car's value in LPGDbAnalyzer).</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double ReferencePressure { get; set; } = 1.49;

        /// <summary>
        /// The temperature coefficients that were in the ECU while the logs were recorded
        /// (LPGDbAnalyzer stores them per log). Without this call the defaults from <see cref="Settings"/> are shown.
        /// </summary>
        public void SetCurrentCoefficients(IReadOnlyList<int> gas, IReadOnlyList<int> reducer)
        {
            textBox1.Text = string.Join(",", gas);
            textBoxReducerTempValues.Text = string.Join(",", reducer);
            gridGas.Rows.Clear();
            gridReducer.Rows.Clear();
            resultGas.Text = resultReducer.Text = "";
        }

        private static Control Section(string title, TextBox values, DataGridView grid, Label result)
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Margin = new Padding(4) };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var caption = new Label
            {
                AutoSize = true,
                Text = title + "  -  current values in the ECU (,)",
                Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold),
                Margin = new Padding(3, 6, 3, 3),
            };
            panel.Controls.Add(caption, 0, 0);
            panel.Controls.Add(values, 0, 1);
            panel.Controls.Add(grid, 0, 2);
            panel.Controls.Add(result, 0, 3);
            return panel;
        }

        private static DataGridView CreateGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = SystemColors.Window,
            };

            foreach (var header in new[] { "Band", "Current", "Suggested", "Change %", "Readings" })
                grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = header, SortMode = DataGridViewColumnSortMode.NotSortable });
            return grid;
        }

        private static Label CreateResultLabel() => new() { AutoSize = true, Margin = new Padding(3, 6, 3, 3) };

        private void ButtonSuggest_Click(object? sender, EventArgs e)
        {
            if (Data is null)
                return;

            if (!TryParseCoefficients(textBox1.Text, Settings.GasTemperatureRanges.Length, out var gas) ||
                !TryParseCoefficients(textBoxReducerTempValues.Text, Settings.ReductorTemperatureRanges.Length, out var reducer))
                return;

            var result = TemperatureCorrectionPrediction.Predict(Data, gas, reducer, ReferencePressure);
            ShowResult(gridGas, resultGas, result.Gas);
            ShowResult(gridReducer, resultReducer, result.Reducer);
        }

        private static bool TryParseCoefficients(string text, int expected, out int[] values)
        {
            var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            values = new int[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out values[i]))
                {
                    MessageBox.Show($"'{parts[i]}' is not a whole number.", "Invalid values");
                    return false;
                }
            }

            if (values.Length != expected)
            {
                MessageBox.Show($"Expected {expected} comma-separated values, got {values.Length}.", "Invalid values");
                return false;
            }

            return true;
        }

        private static void ShowResult(DataGridView grid, Label summary, IReadOnlyList<TemperatureBandSuggestion> result)
        {
            grid.Rows.Clear();

            foreach (var band in result)
            {
                int index = grid.Rows.Add(
                    band.Label,
                    band.Current.ToString("+0;-0;0", CultureInfo.InvariantCulture),
                    band.Suggested.ToString("+0;-0;0", CultureInfo.InvariantCulture),
                    band.Error is double e ? e.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture) : "no readings",
                    band.Count);

                // A suggested change stands out.
                if (band.Suggested != band.Current)
                    grid.Rows[index].Cells[2].Style.BackColor = Color.FromArgb(255, 240, 150);
            }

            grid.ClearSelection();
            summary.Text = "ECU values: " + string.Join(",", result.Select(x => x.Suggested)) +
                           "    (whole numbers: a band changes when its change rounds to at least ±1; few readings shrink it towards 0)";
        }
    }
}
