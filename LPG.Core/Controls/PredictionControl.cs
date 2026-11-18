using LPGDataAnalyzer.Models;
using LPGDataAnalyzer.Services;
using System.ComponentModel;
using System.Globalization;

namespace LPGDataAnalyzer.Controls
{
    public partial class PredictionControl : UserControl
    {
        private readonly TextExtractor textExtractor = new();

        // Set by LoadSettings; null until the first data load.
        private AppSettings? AppSettings { get; set; }
        private AppSettingManager? AppSettingManager { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        private DataItem[] Data { get; set; } = [];
        private string[,] Markers { get; set; } = MapRpmAnalyzer.BuildMarkers([]);

        // Database mode (LPGDbAnalyzer): the table comes from the database.
        private int? _trimDelayMs;
        // Database mode: the fuel table the logs were recorded with, [rpm, injection].
        private double?[,]? _databaseTable;
        // LPGDataAnalyzer: the fuel table as text (3-digit values per injection row), from OCR or the settings file.
        private string _fuelTableText = "";


        /// <summary>
        /// Database mode: writes the last predicted map (complete, display options ignored, [rpm, injection])
        /// as an ECU configuration file. The "Export .ml8..." button is shown only when this is set.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<double?[,], bool>? ExportPrediction
        {
            get => _exportPrediction;
            set
            {
                _exportPrediction = value;
                UpdateExportButton();
            }
        }

        private Func<double?[,], bool>? _exportPrediction;
        // The last prediction as a complete map, for ExportPrediction; null before the first Predict.
        private double?[,]? _exportMap;
        private readonly Button _buttonExportMl8 = new() { Text = "Export .ml8...", AutoSize = true, Anchor = AnchorStyles.Left, Visible = false };

        private void UpdateExportButton()
        {
            _buttonExportMl8.Visible = _exportPrediction is not null;
            _buttonExportMl8.Enabled = _exportPrediction is not null && _exportMap is not null;
        }


        public PredictionControl()
        {
            InitializeComponent();
            panelLegend.Paint += PanelLegendBuilder.PanelLegend_Paint;
            SetOptionToolTips();

            flowActions.Controls.Add(_buttonExportMl8);
            _buttonExportMl8.Click += (_, _) =>
            {
                if (_exportMap is not null)
                    _exportPrediction?.Invoke(_exportMap);
            };

            // The reference pressure is only used while the pressure correction is on.
            checkBoxPressureCorrection.CheckedChanged += (_, _) =>
                labelPress.Enabled = textBoxRefPress.Enabled = checkBoxPressureCorrection.Checked;
        }

        /// <summary>All prediction options as they are set in the controls.</summary>
        public PredictionOptions ReadOptions(double referencePressure) => new()
        {
            OnlyChanges = checkBoxOnlyChanges.Checked,
            ShowOnlyMultiplier = checkBoxShowOnlyMiplayerChange.Checked,
            Round = checkBoxRound.Checked,
            ShowOnlyCount = checkBoxShowOnlyCount.Checked,
            PressureCorrection = checkBoxPressureCorrection.Checked,
            InterpolationWeights = checkBoxInterpolationWeights.Checked,
            TrimDelay = checkBoxTrimDelay.Checked,
            MinChangeValue = textBoxMinValueOfChange.Text.Trim().ToDouble(),
            ReferencePressure = referencePressure,
            MinCount = textBoxMinCount.Text.Trim().ToInt(),
            MaxBenzDiff = textBoxMaxBenzDiff.Text.Trim().ToDouble(),
            TrimDelayMs = _trimDelayMs,
        };

        /// <summary>Sets the controls to stored options.</summary>
        public void ApplyOptions(PredictionOptions o, bool includeReferencePressure = true)
        {
            checkBoxOnlyChanges.Checked = o.OnlyChanges;
            checkBoxShowOnlyMiplayerChange.Checked = o.ShowOnlyMultiplier;
            checkBoxRound.Checked = o.Round;
            checkBoxShowOnlyCount.Checked = o.ShowOnlyCount;
            checkBoxPressureCorrection.Checked = o.PressureCorrection;
            checkBoxInterpolationWeights.Checked = o.InterpolationWeights;
            checkBoxTrimDelay.Checked = o.TrimDelay;
            textBoxMinValueOfChange.Text = o.MinChangeValue.ToString(CultureInfo.InvariantCulture);
            textBoxMinCount.Text = o.MinCount.ToString(CultureInfo.InvariantCulture);
            textBoxMaxBenzDiff.Text = o.MaxBenzDiff.ToString(CultureInfo.InvariantCulture);
            if (includeReferencePressure)
                textBoxRefPress.Text = o.ReferencePressure.ToString(CultureInfo.InvariantCulture);
        }

        private readonly ToolTip optionToolTips = new() { AutoPopDelay = 20000 };

        /// <summary>
        /// Hover help for the prediction options.
        /// </summary>
        private void SetOptionToolTips()
        {
            Disposed += (_, _) => optionToolTips.Dispose();

            optionToolTips.SetToolTip(checkBoxPressureCorrection,
                "Scales trims by reducer pressure against the Reference Press value.\n" +
                "Negative trim x sqrt(ref/p), positive trim x sqrt(p/ref).");
            optionToolTips.SetToolTip(checkBoxInterpolationWeights,
                "The ECU blends the 4 map breakpoints around the engine position.\n" +
                "Credits each logged row to those 4 cells with the same weights, instead of one whole cell.");
            optionToolTips.SetToolTip(checkBoxTrimDelay,
                "The OBD trim describes the mixture about TrimDelayMs (appSettings.json, default 850 ms) earlier.\n" +
                "Places each trim at the engine position logged that much earlier.");
        }


        public void LoadSettings(AppSettingManager appSettingManager, DataItem[] data)
        {
            AppSettingManager = appSettingManager;
            AppSettings = appSettingManager.Load();
            _fuelTableText = AppSettings.LastLoadedFuelTable ?? "";
            textBoxImagePath.Text = AppSettings.ImagePath;
            Data = data;
            Markers = MapRpmAnalyzer.BuildMarkers(data);
            scoreView.Clear();
            ShowFuelTable();

        }

        /// <summary>
        /// Loads logs and the fuel table they were recorded with from a database (LPGDbAnalyzer).
        /// There is no settings file and no image: the image line is hidden. Predictions are not stored;
        /// the predicted map can be written as an ECU configuration through <see cref="ExportPrediction"/>.
        /// </summary>
        /// <param name="values">Fuel table values as [injection row][rpm column].</param>
        /// <param name="stored">Stored predictions to list, newest first. The controls keep their defaults; selecting one shows its options.</param>
        public void LoadFuelTable(DataItem[] data, double[][] values, double referencePressure, int trimDelayMs)
        {
            _trimDelayMs = trimDelayMs;
            AppSettingManager = null;
            AppSettings = null;


            // [injection row][rpm column] -> [rpm, injection]
            _databaseTable = new double?[values[0].Length, values.Length];
            for (int inj = 0; inj < values.Length; inj++)
                for (int rpm = 0; rpm < values[inj].Length; rpm++)
                    _databaseTable[rpm, inj] = values[inj][rpm];
            textBoxRefPress.Text = referencePressure.ToString(CultureInfo.InvariantCulture);

            // No image to read: the line with the image path is not needed.
            tableLayoutPanelTopLine.Visible = false;
            tableLayoutPanelRight.RowStyles[0].Height = 0;


            Data = data;
            Markers = MapRpmAnalyzer.BuildMarkers(data);
            _exportMap = null;
            UpdateExportButton();
            skippedReadingsView.Clear();
            scoreView.Clear();
            ShowFuelTable();
        }

        /// <summary>
        /// Shows the current fuel table in the Original grid (before any prediction). Nothing is shown
        /// when LPGDataAnalyzer has no valid table text yet.
        /// </summary>
        private void ShowFuelTable()
        {
            try
            {
                var table = _databaseTable ?? textExtractor.BuildFinalTable(_fuelTableText);
                dataGridViewOrig.SetData(table, Data, "Original Data", x => x.BENZ_b1, x => x.BENZ_b2);
            }
            catch
            {
                // No table yet (e.g. nothing OCR'd): leave the grid as it is.
            }
        }

        /// <summary>
        /// Reads the fuel table and the options from the controls; shows a message and returns false when they are invalid.
        /// </summary>
        private bool TryReadInputs(out double?[,] table, out PredictionOptions options)
        {
            table = null!;
            options = null!;

            if (Data.Length == 0)
            {
                MessageBox.Show("Load log files first.", "Info");
                return false;
            }

            try
            {
                // Database mode has the table; LPGDataAnalyzer reads it from the text box.
                table = _databaseTable is not null
                    ? (double?[,])_databaseTable.Clone()
                    : textExtractor.BuildFinalTable(_fuelTableText);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Invalid fuel table");
                return false;
            }

            if (!double.TryParse(
                    textBoxRefPress.Text.Trim().Replace(',', '.'),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var referencePressure))
            {
                MessageBox.Show("Invalid reference pressure.");
                return false;
            }

            options = ReadOptions(referencePressure);
            return true;
        }

        /// <summary>
        /// Runs the prediction with <paramref name="options"/>. <paramref name="fullMap"/> ignores the display
        /// options (only changes / multipliers / counts) so the result is always a complete fuel map.
        /// </summary>
        private static (double?[,] result, List<FuelMapPrediction.RejectedReading> invalidItems, double[,] cellWeights) Predict(
            DataItem[] data, double?[,] table, PredictionOptions options, bool fullMap = false)
        {
            // 0 turns the pressure correction off.
            double pressure = options.PressureCorrection ? options.ReferencePressure : 0;

            return FuelMapPrediction.BuildTable(data, table, pressure, options.MinCount,
                showOnlyChanges: !fullMap && options.OnlyChanges,
                round: options.Round,
                showOnlyMultiplier: !fullMap && options.ShowOnlyMultiplier,
                minChangeValue: options.MinChangeValue,
                benzDiffMax: options.MaxBenzDiff,
                showOnlyCount: !fullMap && options.ShowOnlyCount,
                useInterpolationWeights: options.InterpolationWeights,
                useTrimDelay: options.TrimDelay);
        }

        // Each Predict starts a new accuracy check; results of an older one are dropped.
        private int _accuracyRun;

        /// <summary>
        /// Leave-one-file-out accuracy of the options just used for Predict and of each option switched the
        /// other way, computed in the background and shown in <see cref="scoreView"/>.
        /// </summary>
        private async Task UpdateAccuracyAsync(double?[,] table, PredictionOptions options)
        {
            int run = ++_accuracyRun;

            var variants = new List<(string Name, PredictionOptions Options)>
            {
                ("Current options", options),
                ($"Pressure Correction {OnOff(!options.PressureCorrection)}", options with { PressureCorrection = !options.PressureCorrection }),
                ($"ECU Interpolation Weights {OnOff(!options.InterpolationWeights)}", options with { InterpolationWeights = !options.InterpolationWeights }),
                ($"Trim Delay {OnOff(!options.TrimDelay)}", options with { TrimDelay = !options.TrimDelay }),
            };

            var data = Data;
            scoreView.ShowMessage("Calculating the prediction accuracy...");
            try
            {
                var results = await Task.Run(() => variants
                    .Select(v => (v.Name, Result: PredictionScore.LeaveOneFileOut(data, table, train => Predict(train, table, v.Options, fullMap: true).result)))
                    .ToList());

                if (run != _accuracyRun || IsDisposed)
                    return;

                if (results.Any(r => r.Result is null))
                    scoreView.ShowMessage("The accuracy needs readings from at least 2 log files recorded with this map.");
                else
                    scoreView.ShowResults(results.Select(r => (r.Name, r.Result!)).ToList());
            }
            catch (Exception ex)
            {
                if (run == _accuracyRun && !IsDisposed)
                    scoreView.ShowMessage("The accuracy could not be calculated: " + ex.Message);
            }

            static string OnOff(bool on) => on ? "on" : "off";
        }

        private void ButtonPredict_Click(object sender, EventArgs e)
        {
            if (!TryReadInputs(out var table, out var options))
                return;

            var tableNew = Predict(Data, table, options);

            if (AppSettings is not null && AppSettingManager is not null)
            {
                AppSettings.LastPredictedFuelTable = tableNew.result.ToText();
                AppSettingManager.Save(AppSettings);
            }

            // The export always gets the complete map, whatever the "Show" options display.
            if (_exportPrediction is not null)
            {
                _exportMap = Predict(Data, table, options, fullMap: true).result;
                UpdateExportButton();
            }

            PreviewPrediction(table, tableNew.result);
            MarkCellsWithoutEnoughData(tableNew.cellWeights, options.MinCount);

            skippedReadingsView.ShowReadings(tableNew.invalidItems, options.MaxBenzDiff);

            _ = UpdateAccuracyAsync(table, options);
        }

        private void ButtonParceSelectedImage_Click(object sender, EventArgs e)
        {
            if (AppSettings is null || AppSettingManager is null)
                return;

            try
            {
                // The path box is editable: use and remember what is typed there.
                var path = textBoxImagePath.Text.Trim();
                if (path != AppSettings.ImagePath)
                {
                    var previousPath = AppSettings.ImagePath;
                    AppSettings.ImagePath = path;
                    try
                    {
                        AppSettingManager.Save(AppSettings); // validates (throws if empty)
                    }
                    catch
                    {
                        AppSettings.ImagePath = previousPath;
                        textBoxImagePath.Text = previousPath;
                        throw;
                    }
                }

                var text = textExtractor.Parcer(AppSettings.ResolvedImagePath);
                textExtractor.Validate(text); // throws with the errors found
                SetFuelTableText(text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Image parsing failed");
            }
        }
        private void PreviewPrediction(double?[,] table, double?[,] tableNew)
        {
            dataGridViewOrig.SetData(table, Data,"Original Data", x => x.BENZ_b1, x => x.BENZ_b2);
            dataGridViewPrediction.SetData(tableNew, Data, "Prediction Data", x => x.BENZ_b1, x => x.BENZ_b2);

            if (checkBoxShowOnlyMiplayerChange.Checked)
            {
                DataGridViewColorization.HighlightDifferencesHeatmapWithValues(dataGridViewPrediction.Grid, null, Markers, tolerance: 0.01);
            }
            else
            {
                // Apply heatmap to DataGridViews
                var vals = DataGridViewColorization.HighlightDifferencesHeatmapWithValues(dataGridViewPrediction.Grid, dataGridViewOrig.Grid, Markers, tolerance: 0.01);

                // Create horizontal legend aligned with DataGridView
                PanelLegendBuilder.CreateDynamicHorizontalHeatmapLegend(panelLegend, dataGridViewPrediction.Grid, vals.WLow, vals.WHigh);
            }
        }

        /// <summary>Cells without readings: the current value is kept.</summary>
        private static readonly Color NoDataColor = Color.Gainsboro;

        /// <summary>Cells with readings, but not more than Min Count: the current value is kept until more data is collected.</summary>
        private static readonly Color NotEnoughDataColor = Color.DarkGray;

        /// <summary>
        /// Greys out the Prediction grid cells that kept their current value for lack of data
        /// (<paramref name="cellWeights"/> as [rpm, injection], see <see cref="FuelMapPrediction.BuildTable"/>),
        /// with the readings' weight in the tooltip. Grid rows are injection breakpoints, column 0 is the axis.
        /// </summary>
        private void MarkCellsWithoutEnoughData(double[,] cellWeights, int minCount)
        {
            var grid = dataGridViewPrediction.Grid;
            grid.ShowCellToolTips = true;

            int rpmLength = cellWeights.GetLength(0);
            int injLength = cellWeights.GetLength(1);

            for (int injIndex = 0; injIndex < injLength && injIndex < grid.RowCount; injIndex++)
            {
                for (int rpmIndex = 0; rpmIndex < rpmLength && rpmIndex + 1 < grid.ColumnCount; rpmIndex++)
                {
                    var cell = grid.Rows[injIndex].Cells[rpmIndex + 1];
                    double weight = cellWeights[rpmIndex, injIndex];
                    string weightText = weight.ToString("0.##", CultureInfo.InvariantCulture);

                    if (weight <= 0)
                    {
                        cell.Style.BackColor = NoDataColor;
                        cell.Style.ForeColor = Color.Black;
                        cell.ToolTipText = "No readings: current value kept.";
                    }
                    else if (weight <= minCount)
                    {
                        cell.Style.BackColor = NotEnoughDataColor;
                        cell.Style.ForeColor = Color.Black;
                        cell.ToolTipText = $"Not enough data: weight {weightText} ≤ Min Count {minCount} (both banks, ~2 per reading). Current value kept until more is collected.";
                    }
                    else
                    {
                        cell.ToolTipText = $"Readings weight {weightText} (both banks, ~2 per reading).";
                    }
                }
            }

            dataGridViewPrediction.Title = "Prediction Data (light grey = no readings, dark grey = not enough data, kept)";
        }

        private void buttonConvert_Click(object sender, EventArgs e)
        {
            try
            {
                var text = TextExtractor.ConvertColumnsToRows(_fuelTableText.Trim());
                textExtractor.Validate(text);
                SetFuelTableText(text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Convert failed");
            }
        }

        /// <summary>LPGDataAnalyzer: uses a new fuel table text, remembers it in the settings file and shows it.</summary>
        private void SetFuelTableText(string text)
        {
            _fuelTableText = text;

            if (AppSettings is not null && AppSettingManager is not null)
            {
                AppSettings.LastLoadedFuelTable = text;
                AppSettingManager.Save(AppSettings);
            }

            ShowFuelTable();
        }

    }

    /// <summary>
}