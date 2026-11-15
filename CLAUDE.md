# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

A Windows tool for tuning LPG (autogas) multipoint-injection ECUs. It reads the tab-separated real-time logs that the gas ECU software exports, analyzes fuel trims, AFR, pressures and temperatures, and suggests a corrected gas fuel map (multiplier table).

Solution `LPG.slnx` has four projects:
- **LPG.Core**: WinForms class library (`net10.0-windows`, `Tesseract`, `WinForms.DataVisualization`) with all analysis code and screens: `Controls/`, `Services/`, `Models/`, `Extensions.cs`, `AppSettingManager.cs` and others. Only LPGDbAnalyzer references it. Namespaces are `LPGDataAnalyzer.*` (`RootNamespace` = `LPGDataAnalyzer`, which also keeps the `.resx` resource names). Paths like `Services/Parser.cs` in the sections below are in `LPG.Core/`, and the features described there (prediction accuracy, interpolation weights, trim delay, temperature prediction, gain 0.8, …) are LPG.Core/LPGDbAnalyzer only.
- **LPGDataAnalyzer**: WinForms app (`net10.0-windows`), the log-file app. **Rolled back on 2026-11-15 at the user's request** to its last committed state: it is standalone again, with its own copy of the old code (`Controls/`, `Services/`, `Models/`, …), and does **not** reference LPG.Core. Keep it as it is; don't port LPG.Core changes into it unless asked. Since the rollback only its build warnings (nullable annotations, one stackalloc in a loop) and paths were fixed: `HistoryFolder`, `DataFilesFolder` and `appSettings.json` resolve against the output folder (defaults `History`, `DataFiles`), and every `DataFiles\**\*` log is copied to output.
- **FTDIDataReceiver**: small console app (`net10.0`, `FTD2XX.Net`). It opens an FTDI USB-serial device at 115200 8N1 and writes the raw stream to `ftdi_log_<timestamp>.txt`. It is meant to become an own ECU data provider, but it is **frozen** for now. It is independent of the analyzer.
- **LPGDbAnalyzer**: WinForms app (`net10.0-windows`, `Microsoft.Data.Sqlite`, `Tesseract`, references LPG.Core). It collects logs over time in a SQLite database, shows the same analysis screens, and compares fuel tables to see progress. See "LPGDbAnalyzer" below.

Main workflow: save many log files over time, load them together, and use all the data to predict the fuel map. Only files recorded with the same (current) map are loaded together, because trims are relative to the map that was active during recording.

## Current state

**Read `STATUS.md` first**: where the tuning stands (car, logs, measured accuracy, decisions, next steps). Update it at the end of every session. Python copies of the algorithms for quick tests are in `tools/analysis/`.

## Instructions from the user (must follow)

**Working rules**
- Commit only when asked. Propose changes one at a time, so each can be compared against the current results.
- Don't run throwaway C# console projects to check code without asking first. Verify with `dotnet build LPG.slnx` and with Python scripts on the logs.
- Add deferred ideas to `BACKLOG.md`.
- Settings validation belongs in `AppSettingManager.Validate` (LPGDataAnalyzer) or `AppSettings.Validate` (LPGDbAnalyzer), never in property getters or UI controls.
- Never take map dimensions from a literal `12` or fixed breakpoints; use the axis arrays or `GetLength`.

**Intentional design: don't "fix" or remove**
- Parser trim grouping (it deduplicates OBD trim readings: ~1 s refresh, 250 ms log rate).
- The damping `CorrectionGain` = 0.8 (was 0.5; changed 2026-11-14 with the user's OK after Check accuracy: 0.5 → 6.5 %, 0.8 → 8.8 %, peak 0.9). Change it only with the user's OK; watch for cells overshooting over several maps.
- **Reducer pressure correction** of trims ("Reference Press", default 1.49, "Pressure Correction" checkbox on by default). It was removed once and had to be restored: never remove it; changing its formula needs the user's OK. Open point, not changed: the formula shrinks positive (lean) trims at low pressure (×0.86 at 1.1 bar). Don't judge it from averages over logs that hardly contain low pressure.
- **SLOW trims must not be used in the map prediction.** It uses FAST trims only; SLOW is tracked by a separate analysis.
- The map prediction applies no temperature scaling. The ECU applies its own temperature tables, and those tables are tuned separately (`TemperatureCorrectionPrediction`, Temperature prediction tab).
- Removed at the user's request on 2026-11-14, don't bring them back: "Smooth Corrections" (tested worse in every mode: it blurs real local dips of the map), "Fill Empty High-Load Cells", "Use History", "Always Apply Negative Trim" (positive and negative trims now use the same threshold), "Map Regression" (no difference in Check accuracy), and the Validate button and the two fuel-table text boxes of the Prediction screen.
- "ECU Interpolation Weights" and "Trim Delay" are checkboxes, ON by default since 2026-11-14 (Check accuracy on the 6 Capture2 logs: both on +4.7 %, interpolation off −0.6 %, delay off +2.0 %). Both were verified from the logs: the MP6C interpolates bilinearly between breakpoints, and the OBD trim describes the engine position ~0.6–1.1 s earlier.
- History snapshots / stored predictions are kept to look at and compare; they no longer feed into a new prediction.
- Judge prediction changes with the prediction accuracy shown after Predict (`Services/PredictionScore`, leave one file out), not by feel. Every algorithm change is kept only if it raises it.

**Tuning workflow**
- Tuning is sequential. Look at the temperature reports, change the ECU temperature tables by ±1 (the ECU accepts whole numbers only), record new logs, and only then correct the map with those logs. The reducer table rarely changes; the LPG temperature table is anchored at 0 for 30–40 °C.
- Logs are normally analyzed per fuel table. In LPGDbAnalyzer every log's trims are brought to the newest checked log's temperature tables (`TrimNormalizer`), and "Analyze together" brings logs of older maps to the newest map as well (the map part assumes trims follow a map change one to one: check it with the prediction accuracy once logs of two maps exist).

**LPGDbAnalyzer requirements**
- Data structure: car → fuel table → logs. Car and fuel table are selected from the database or defined new before import.
- An OCR'd fuel table is never saved without the user checking it against the image on the check screen and confirming.
- Fuel map axes come from each fuel table in the database. The ECU temperature tables (`GasTemperatureCorrectionCoef`, `ReductorTemperatureCorrectionCoef`) are set **per imported .txt file**, or marked "same as previous". Don't fall back to `Settings.cs` values silently; they are LPGDataAnalyzer defaults only.
- Checking logs of one fuel table works like LPGDataAnalyzer; checking two fuel tables shows the comparison, to see progress over time.
- Delete is possible for a car, fuel table, log, or only the image. Fuel tables use LPGDataAnalyzer's heatmap colours.
- Tabs like LPGDataAnalyzer: Data (import/loading) first, the analysis tabs disabled until data is loaded, using the full window and as responsive as possible.
- Predictions are stored in the database with all selected options; the history list works from the database, not JSON files.

## Build / run

```
dotnet build LPG.slnx
dotnet run --project LPGDataAnalyzer
dotnet run --project FTDIDataReceiver
dotnet run --project LPGDbAnalyzer
```

- In the solution, every project except FTDIDataReceiver maps to the `x64` platform.
- Each app that uses OCR needs its own `Tesseract` PackageReference: the package's build targets, which copy the native DLLs, don't flow through the LPG.Core reference.
- In LPGDbAnalyzer files, put `using LPGDataAnalyzer;` **inside** the namespace block. Otherwise LPG.Core's `Average`/`Min`/`Max` extensions clash with LINQ's (CS0121). The LPGDataAnalyzer code itself sits in that namespace, so it isn't affected.
- The Tesseract native DLLs (`leptonica-1.82.0.dll`, `tesseract50.dll`) are copied to `x64\` and `x86\` in the output by the Tesseract package's own `build/Tesseract.targets`. Don't add manual references to them in the csproj.
- There is no test project and no lint configuration.

## Architecture (LPGDataAnalyzer)

**Startup**: `Program.cs` builds a minimal `Microsoft.Extensions.DependencyInjection` container (`AppSettingManager` singleton, `MainForm`). `MainForm` loads `AppSettings` through `AppSettingManager`, then loads history snapshots and wires up the user controls.

**Settings** (`appSettings.json` → `Models/AppSettings`, managed by `AppSettingManager`):
- `AppSettingManager` reads `appSettings.json` when the app starts. All settings validation belongs in `AppSettingManager.Validate`, which runs in `Load()` and `Save()` and throws `InvalidOperationException` for missing required values. Currently `ImagePath` is required.
- Don't hard-code machine- or user-specific paths in `appSettings.json` or in `AppSettings` defaults. Use paths relative to the app's output folder (`HistoryFolder`, `DataFilesFolder`, `ImagePath`).
- `ImagePath` points to `FuelTable/Capture.PNG`, a screenshot of the fuel map taken from the ECU software. It is copied to output and OCR'd by `PredictionControl`. `AppSettings.ResolvedImagePath` resolves it against `AppContext.BaseDirectory`.

**Data flow**:
1. `Controls/DataFilesSelectorUI` lists `*.txt` logs in `AppSettings.DataFilesFolder`, with a `FileSystemWatcher` for live updates. For each checked file it runs `Services/Parser` and raises `DataLoaded(DataItem[])`.
2. `MainForm.LoadParsedData` passes the same `DataItem[]` to every analysis control: main grid, `PredictionControl`, `AnalysisUC`, `TemperatureAnalyzerUI`, `ReducerTempCorrection`, `MapAnalyzerUI`, `DataItemLineChartControl`. To add a new view, add a control and feed it here.

**Log format and parsing** (`Services/Parser.cs`):
- The first 2 lines are a header and are skipped. Columns are tab-separated, 22 or more per row, in this order: `TEMPO GIRI(RPM) LAMBDA GAS BENZ PRESS MAP T.RID T.GAS LIV SLOW FAST OX LAMBDA2 GAS2 BENZ2 SLOW2 FAST2 OX2 MARKER AUTOMARKER ECUMARKER`. Column indices are hard-coded in `ParseLine`.
- `_b1`/`_b2` mean bank 1 and bank 2. GAS is LPG injection time and BENZ is petrol injection time.
- Rows with RPM, GAS_b1 or GAS_b2 equal to 0 are dropped. Consecutive rows whose trims (`Trim_bN = SLOW + FAST`) differ by ≤ 0.1 are **averaged into one `DataItem`**. So `Parser.Data` holds grouped samples, not raw rows. `TEMPO` is renumbered and group 0 is discarded.
- This grouping is intentional. The LPG ECU gets the petrol ECU's trims over OBD about once per second, while the log samples every ~250 ms. Repeated rows therefore carry the same trim reading, and one group is roughly one OBD reading. Fast trims come in steps of 0.78%.
- Each group also keeps its raw rows' engine positions (`Positions`, as RPM plus `BENZ` for each bank) and the positions of the rows logged `TrimDelayMs` earlier (`DelayedPositions`). Both are `[JsonIgnore]`/`[Browsable(false)]`.
- Derived fields are computed in the parser: `Trim`, `AFR = 15.6 / (1 + trim/100)`, `Ratio = BENZ/GAS`, `BENZ_Diff`, and others. See `Models/DataItem.cs`.
- After parsing, `TEMPO` is only a per-file reading index and restarts in every file. Real time is in `TimeMs`, and the file is in `SourceFile`. Several files are concatenated in load order, so **never sort by `TEMPO`**, and compare a reading with the previous one only when `SourceFile` matches. `Stability` is the share of a group's rows in its most frequent map cell.

**Fuel map model**:
- `Models/Settings.cs` holds all static breakpoints: `RpmColumns` (12), `InjectionRanges` (12, petrol injection time in ms), gas and reducer temperature bands with their correction coefficients, and driving/MAP mode ranges. Map dimensions everywhere come from these arrays.
- Fuel maps are `double?[rpmIndex, injIndex]`. `Extensions.GetRpmIndex/GetInjectionIndex` map a sample to a cell and return `-1` outside the axes, so callers must skip those. Temperature bands go through `Extensions.GetTempBandIndex` only: bands are `(Min, Max]` (40 °C → `Temp_31_40`), and values outside are clamped to the edge bands.
- The current map: LPGDbAnalyzer passes it from the database (`LoadFuelTable`). LPGDataAnalyzer OCRs it from a screenshot ("Parse"; the text is validated, kept in `AppSettings.LastLoadedFuelTable` and shown in the Original grid; there is no text box to paste or edit it) with `Services/TextExtractor`, which uses Tesseract with the `tessdata/` folder copied to output and a digits-only whitelist. In the text form each row is a concatenation of 3-digit values. `SplitToThreeDigitInts` applies OCR fix-ups (values over 300 or over 200 are reduced).
- `GasTemperatureRanges`/`GasTemperatureCorrectionCoef` and `ReductorTemperatureRanges`/`ReductorTemperatureCorrectionCoef` in `Settings.cs` mirror the temperature correction tables configured in the LPG ECU. The ECU applies them itself. The coefficient arrays are only LPGDataAnalyzer's defaults: LPGDbAnalyzer stores the tables per log and passes them in (`ReducerTempCorrection.SetCurrentCoefficients`, `TemperatureCorrectionPrediction.Predict*(data, current)`).
- `Services/FuelMapPrediction.BuildTable` is the core algorithm. It takes the logs, the current map and options, and returns the new map plus the rejected samples. Per cell it works like this:
  - It skips groups with `Stability < MinGroupStability` (binning only), groups with a bank difference at or above `benzDiffMax`, groups where the petrol ECU is not regulating (FAST exactly 0 on both banks: open loop, overrun, reset) and load transients (average BENZ over the group's rows and its delayed rows changes more than `MaxLoadChangeRatio` = 2×). The last two were added 2026-11-14 and raised Check accuracy from 4.75 % to 6.47 % (every file better).
  - It takes the median `FAST` trim of each bank that has data and averages those medians. `SLOW` trims are intentionally not used: they apply to the whole map, the two banks differ, and they are tracked by a separate analysis. The prediction only finds which cells to change.
  - It applies `CorrectionGain` (0.8, damping chosen with Check accuracy). The `minChangeValue` threshold is checked on the undamped average.
  - Trims get no temperature scaling: the ECU applies its own temperature tables, and logs are recorded after the tables are set.
  - Trims **are** scaled by reducer pressure (`ApplyPressureCorrection`, "Reference Press" box in the UI, default 1.49, "Pressure Correction" checkbox on by default; a reference of 0 turns it off). Under high load the reducer pressure drops (down to ~1.1 bar on long high load) and the mixture risks going lean, so this correction is intentional and must stay.
  - **Prediction accuracy** (no button: calculated in the background after each Predict with the options just used; `Services/PredictionScore` + `Controls/PredictionScoreView`, shown in the Prediction screen between the options and the stored predictions, 60/40 of the height; cleared when new data is loaded): leave one file out. Each file is hidden in turn, the map is predicted from the others (`fullMap`: display options ignored), and for each hidden reading the change the ECU would apply is the bilinear blend of the 4 surrounding cells' % changes at its `DelayedPositions`, averaged over the rows. Score = 1 − Σ(FAST − change)² / Σ FAST², both banks (0 = no better than the current map, max 1 − (1 − gain)², 96 % with gain 0.8). It shows the current options, each of Pressure Correction, ECU Interpolation Weights and Trim Delay switched the other way (with a better/worse verdict), and the score per hidden file. The yardstick is the same for every option set.
  - **ECU Interpolation Weights** (checkbox, on by default): the MP6C interpolates bilinearly between map breakpoints (the axis `Label`s), which was verified from GAS/BENZ in the logs. When this is on, each raw row of a reading is credited to its 4 surrounding cells with those weights, averaged over the rows. Cells then use the weighted median, the count is the sum of weights, and the `Stability` filter is skipped.
  - **Trim Delay** (checkbox, on by default): the OBD trim describes the engine position about `AppSettings.TrimDelayMs` (default 850 ms, measured 600–1100 ms) earlier. When this is on, readings are placed at `DataItem.DelayedPositions` instead of their own rows.
  - With both checkboxes off, results are the same as before these options existed.
  - There is no smoothing of corrections: each cell is corrected from its own readings only (the Gaussian smoothing was removed on 2026-11-14).
- `Services/TemperatureCorrectionPrediction` suggests both ECU temperature tables at once ("Temperature prediction" tab, one button). Verified from GAS in the logs (2026-11-14): table values are % of gas injection time, each taken at its band's upper temperature (last band: lower limit + 10 °C) and interpolated linearly between those points by the ECU. The method: trim residual = pressure-corrected FAST minus the map error at the reading's delayed rows (`FuelMapPrediction.CellErrors`/`ErrorAt`: same interpolation weights, trim delay and filters as the map), clipped to ±15; then gas(t) + reducer(t) are fitted together by least squares with interpolation weights and ridge `Shrinkage` 200, the LPG table anchored at `Temp_31_40` = 0; changes are rounded to whole numbers. Leave-one-file-out on the 6 Capture2 logs: the former per-band median method −3.4 %, this one +1.9 % (steps instead of interpolation +0.2 %).

**History**: `Controls/HistoryStorage.cs` (`HistorySnapshot`, `HistoryManager`) stores JSON snapshots (logs, old map, new map) in `AppSettings.HistoryFolder`. Maps are stored as jagged arrays (`ArrayConverter`). They are listed and previewed only ("Use History", which mixed them into a new prediction, was removed on 2026-11-14). `/LPGDataAnalyzer/History` is gitignored.

**Analysis services** (`Services/Analyzer`, `MapRpmAnalyzer`, `TemperatureAnalyzer`, `TemperatureCorrectionPrediction`, `ExtraInjectionCalculator`) are mostly static functions over `DataItem[]`. Many return `object` (anonymous-type lists) that bind directly to `DataGridView`s.

**Performance conventions**: the hot paths use `Span`, `stackalloc`/`ArrayPool`, and custom `Median`/`Average` helpers in `Extensions.cs`. Recent commits focus on this, so keep it that way.

## Domain data

- **`DataFiles/`** (repo root and `LPGDataAnalyzer/DataFiles/`, copied to output; `DataFilesFolder` setting): real-time engine fuel logs (`*.txt`) recorded from the LPG ECU while driving. These are the input for every analysis. `12.08.2026g.txt` is a real log for manual testing.
- **`LPGDataAnalyzer/FuelTable/`** (`ImagePath` setting): `Capture.PNG` is a screenshot of the **current** fuel table (gas multiplier map) from the ECU software. It is OCR'd into the map that `FuelMapPrediction` corrects.
- **Fuel map axes**: `Settings.RpmColumns`/`InjectionRanges` are static properties. Their defaults are the table in `Models/Settings.cs` (used by LPGDataAnalyzer). `Settings.SetMapAxes(rpm, injection)` replaces them with a fuel table's breakpoints; LPGDbAnalyzer calls it with the table being analyzed **before** parsing (the parser computes `Stability` from the axes) and before each screen or comparison side. Built from breakpoints, each cell is (previous, own], and values above the last breakpoint go to the last cell. `ResetMapAxes()` restores the defaults.
  - `RpmColumns`: RPM breakpoints (currently 12, 700 to 6200). Each column has a `(Min, Max]` range.
  - `InjectionRanges`: petrol injection-time breakpoints in ms (currently 12, 1.9 to 15).
  - All callers read the axes when they run, and none caches them. In new code, take map dimensions from the axis arrays or from the map itself (`GetLength`), never from literal `12` or fixed breakpoint values.
  - A sample is mapped to a cell by `Extensions.GetRpmIndex` (`RPM`) and `GetInjectionIndex` (`BENZ`, the average petrol injection time, unless a selector is passed). These use a binary search over `(Min, Max]` ranges and throw `ArgumentOutOfRangeException` when a value is outside every range.
  - LPGDataAnalyzer still uses the defaults; its OCR column count, map sizes and cell lookup all come from them.

## LPGDbAnalyzer

- **Database** (`Data/Database.cs`, file from `appSettings.json` `DatabasePath`, default `Data/lpg.db` in the output folder; in JSON a backslash must be written `\\`): `Car` → `FuelTable` → `LogFile` → `LogRow`. The schema is created on startup, and `PRAGMA user_version` holds the schema version, so a schema change bumps `SchemaVersion` and adds a migration step. Dates are stored as `yyyy-MM-dd HH:mm:ss` text. Fuel table axes and values are JSON text columns (`CellValues` is `[injection row][rpm column]`), and the screenshot is stored as a BLOB.
- `LogRow` keeps **every raw row with all 22 columns**, unfiltered and ungrouped, so analysis can change without reimporting. `LogFile.FileHash` (SHA-256, unique) blocks importing the same file twice. `RecordedAt` comes from the first header line. Each `LogFile` also stores the ECU temperature tables used while it was recorded (`GasTempCoef`, `ReducerTempCoef`: JSON int arrays, one value per `Settings` band; schema version 2). On import each file gets its own tables or "same as previous", meaning the car's log recorded just before it, from the database or from earlier in the import list. Edit them later with `Forms/LogCoefficientsForm` (double-click a log).
- **Car fields** follow the car profile in `BACKLOG.md` and are edited in a `PropertyGrid` (`Forms/CarForm.cs`).
- **Import** (`Forms/ImportForm.cs`): select or add a car, select or add the fuel table that was in the ECU, add `*.txt` logs, then import.
- **Fuel table check** (`Forms/FuelTableForm.cs`): shows the image beside an editable grid. Row 0 is the RPM axis and column 0 is the injection axis. Values use the same blue-white-red heatmap as LPGDataAnalyzer (`Forms/Heatmap.cs`). Black cells are invalid, and cells with a violet frame are >20% from their neighbours' median. Saving needs no black cells and the "I compared every value" checkbox.
- **OCR** (`Services/FuelTableOcr.cs`), tested on `Capture2.PNG` (144/144 values and both axes correct). Cell values are read from the image upscaled 3× with each pixel set to its **brightest channel** (max of R, G, B). This makes the coloured cells, the blue selected cell and the red borders white, while black digits stay black; on the plain colour image the OCR missed the blue cell and read 131 as 1893. Each number is placed by its position, so a missed value leaves an empty cell. The white-on-red axis labels are read **one by one from their own crop**, inverted (255 − min of R, G, B). RPM crops must stay at 75% of the column width, or the border is read as an extra "1". Labels without a dot are divided by 100 (`190` → 1.90).
- **Main window** (`Forms/MainForm.cs`): tabs over the whole window (maximized), like LPGDataAnalyzer. The first tab, **Data**, has the toolbar, the tree with Windows stock icons on the left, and on the right, top to bottom: the selected item's fields, its fuel table with the heatmap, and the screenshot. Delete removes a car, fuel table or log (cascade, then `VACUUM`). "Delete image" removes only the screenshot. The other tabs (Main Data … Extra injection, Compare) are **disabled** (owner-drawn grey, `Selecting` cancelled); each tab is as wide as its own title (`SizeMode.Normal`, padding for the icon and the bold selected title) until "Analyze checked" loads data for them.
- **Stored predictions** (schema version 3, table `Prediction`, cascades with its fuel table): each prediction made in the Prediction tab is saved while "Save to database" is checked (off by default), with the fuel table, the checked log ids, every option (`LPG.Core/Models/PredictionOptions`), the axes, the base map, the result and the readings per cell (maps and counts `[rpm][injection]`). The history list in the Prediction tab shows the car's stored predictions with the same axes; selecting one shows its maps and restores its options, and it can be deleted there. Loading keeps the default options (stored ones are not restored automatically). The JSON snapshot files are LPGDataAnalyzer only.
- **Analysis** (`Forms/MainForm.cs`): tree nodes have checkboxes. Checking a car or fuel table checks all logs under it, and single logs can be unchecked. "Analyze checked" loads the rows through `Services/AnalysisLoader` (each log grouped by `Parser.Load(rows, fileName)` with the car's `TrimDelayMs`, joined in recording order) and then:
  - **one fuel table**: the LPG.Core screens as in LPGDataAnalyzer (Main Data, Analyses, Temperature, Map Analysis, Prediction, Temperature prediction, Chart) plus an "Extra injection" tab with the calculator's results. The prediction gets the table's values, the car's reference pressure and trim delay, and the stored predictions through `PredictionControl.LoadFuelTable` (no settings file, no image line, `SavePrediction`/`DeletePrediction` callbacks instead of JSON snapshots). "Temperature prediction" starts from the temperature tables of the newest checked log. Each log's trims are first brought to the newest checked log's temperature tables (`LPG.Core/Services/TrimNormalizer`, via `AnalysisLoader.Load(..., recordedWith, target, out normalized)`): the ECU gas time scales with (1 + table value/100) interpolated by temperature, so FAST −= (R − 1)·100 with R = target/recorded factor; readings with FAST 0 on both banks are left alone. Nothing changes when the tables are equal. The status bar says how many logs were adjusted.
  - **two fuel tables**: `Forms/CompareControl.cs`, where A = older table and B = newer. Each side is placed in its own table's cells and uses its newest log's temperature tables. The temperature tab shows both ECU tables, and values that changed are highlighted. It shows a summary (trims, SLOW, pressure, temperatures, cell trim stats with better/worse), cell median FAST for A, B and |B|−|A|, the fuel map change B−A (absolute and %), and temperature band errors from `TemperatureCorrectionPrediction`.
  - more than two: a message, pointing to "Analyze together".
  - **Analyze together** (toolbar and tree menu): all checked logs of one car as one analysis, even from several fuel tables. The frame is the fuel table of the newest checked log; every log's trims are brought to that map (ratio of the two maps' bilinear values at the reading's delayed rows, per bank, each map with its own axes) and to the newest log's temperature tables. Predictions made there are stored with that fuel table.
- Forms are built in code (no `.Designer.cs`).
- Screens should fill their tab and resize: use `Dock`, `TableLayoutPanel` with percent rows/columns, or `FlowLayoutPanel` with `AutoScroll` for option groups, not fixed `Location`s. `PredictionControl`'s option panels are `FlowLayoutPanel`s; `ReducerTempCorrection` shows its results in grids instead of message boxes. Every form calls `ControlSizing.FitButtons(this)` in `OnLoad`, so fixed-size buttons grow to fit their text at Windows scaling above 100%; new forms should do the same. The chart's hover info panel sits beside the mouse, stays inside the chart, and splits into columns when it's taller than the chart.
