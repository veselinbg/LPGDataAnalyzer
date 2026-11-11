# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

A Windows tool for tuning LPG (autogas) multipoint-injection ECUs. It reads the tab-separated real-time logs that the gas ECU software exports, analyzes fuel trims, AFR, pressures and temperatures, and suggests a corrected gas fuel map (multiplier table).

Solution `LPG.slnx` has two projects:
- **LPGDataAnalyzer**: WinForms app (`net10.0-windows`, the main project).
- **FTDIDataReceiver**: small console app (`net10.0`, `FTD2XX.Net`). It opens an FTDI USB-serial device at 115200 8N1 and writes the raw stream to `ftdi_log_<timestamp>.txt`. It is independent of the analyzer.

## Build / run

```
dotnet build LPG.slnx
dotnet run --project LPGDataAnalyzer
dotnet run --project FTDIDataReceiver
```

- In the solution, LPGDataAnalyzer maps to the `x64` platform.
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
- Derived fields are computed in the parser: `Trim`, `AFR = 15.6 / (1 + trim/100)`, `Ratio = BENZ/GAS`, `BENZ_Diff`, and others. See `Models/DataItem.cs`.

**Fuel map model**:
- `Models/Settings.cs` holds all static breakpoints: `RpmColumns` (12), `InjectionRanges` (12, petrol injection time in ms), gas and reducer temperature bands with their correction coefficients, and driving/MAP mode ranges. Map dimensions everywhere come from these arrays.
- Fuel maps are `double?[rpmIndex, injIndex]`. `DataItemExtensions`/`Extensions.GetRpmIndex/GetInjectionIndex` map a sample to a cell.
- The current map is entered in `PredictionControl`. You can paste it as text, or OCR it from a screenshot with `Services/TextExtractor`, which uses Tesseract with the `tessdata/` folder copied to output and a digits-only whitelist. In the text form each row is a concatenation of 3-digit values. `SplitToThreeDigitInts` applies OCR fix-ups (values over 300 or over 200 are reduced).
- `Services/FuelMapPrediction.BuildTable` is the core algorithm. It takes the logs, the current map and the reference pressure, plus options for smoothing (`FuelMapSmoother`, Gaussian kernel), high-RPM/load interpolation, history weighting, minimum count, and others. It returns the new map and the rejected samples.

**History**: `Controls/HistoryStorage.cs` (`HistorySnapshot`, `HistoryManager`) stores JSON snapshots (logs, old map, new map) in `AppSettings.HistoryFolder`. Maps are stored as jagged arrays (`ArrayConverter`). Prediction can use past snapshots. `/LPGDataAnalyzer/History` is gitignored.

**Analysis services** (`Services/Analyzer`, `MapRpmAnalyzer`, `TemperatureAnalyzer`, `ReducerPrediction`, `ExtraInjectionCalculator`) are mostly static functions over `DataItem[]`. Many return `object` (anonymous-type lists) that bind directly to `DataGridView`s.

**Performance conventions**: the hot paths use `Span`, `stackalloc`/`ArrayPool`, and custom `Median`/`Average` helpers in `Extensions.cs`. Recent commits focus on this, so keep it that way.

## Domain data

- **`DataFiles/`** (repo root and `LPGDataAnalyzer/DataFiles/`, copied to output; `DataFilesFolder` setting): real-time engine fuel logs (`*.txt`) recorded from the LPG ECU while driving. These are the input for every analysis. `12.08.2026g.txt` is a real log for manual testing.
- **`LPGDataAnalyzer/FuelTable/`** (`ImagePath` setting): `Capture.PNG` is a screenshot of the **current** fuel table (gas multiplier map) from the ECU software. It is OCR'd into the map that `FuelMapPrediction` corrects.
- **Fuel map axes** are currently hard-coded in `Models/Settings.cs` to match the ECU's table:
  - `RpmColumns`: RPM breakpoints (currently 12, 700 to 6200). Each column has a `(Min, Max]` range.
  - `InjectionRanges`: petrol injection-time breakpoints in ms (currently 12, 1.9 to 15).
  - **Planned change**: both axes will become dynamic and be read from the fuel-table image together with the cell values, so the count and the values can differ per car or map. In new code, take map dimensions from the axis arrays or from the map itself (`GetLength`), never from literal `12` or fixed breakpoint values. Don't spread new direct dependencies on the static `Settings.RpmColumns`/`InjectionRanges`.
  - A sample is mapped to a cell by `Extensions.GetRpmIndex` (`RPM`) and `GetInjectionIndex` (`BENZ`, the average petrol injection time, unless a selector is passed). These use a binary search over `(Min, Max]` ranges and throw `ArgumentOutOfRangeException` when a value is outside every range.
  - Until the axes are dynamic, change these arrays if the ECU table layout changes. OCR column count, map sizes and cell lookup all come from them.
