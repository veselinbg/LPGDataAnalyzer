# Tuning status and next steps

Read this first when a new session starts. `CLAUDE.md` describes the code and the rules; this file says
**where the tuning stands**, what was measured, what was decided and what comes next. Update it at the
end of every working session.

Last update: **2026-11-14**.

## The car and the data

- Car: **Mercedes M272 3.0 V6** (two banks), LPG ECU AEB MP6C-type, petrol trims over OBD.
- Database: `LPGDbAnalyzer/bin/Debug/net10.0-windows/Data/lpg.db` (backup before the October import:
  `lpg.db.bak-before-october-import` next to it).
- Fuel table **1 "Capture2"**: the map in the ECU since at least 12 August 2026. Its stored date is
  2026-11-13 (when it was entered); the user should set it to 2026-08-12 or earlier, so that it sorts
  before any newer map in comparisons.
- Logs under Capture2 (all recorded with this map, verified from the map's breakpoint values):

  | Id | File | Recorded | Rows |
  |---|---|---|---|
  | 2 | 04.10.2026g1.txt | 2026-10-04 10:18 | 21 042 |
  | 3 | 04.10.2026g2.txt | 2026-10-04 13:59 | 37 603 |
  | 4 | 06.10.2026g1.txt | 2026-10-06 09:47 | 2 985 |
  | 5 | 06.10.2026g2.txt | 2026-10-06 11:32 | 2 064 |
  | 6 | 08.10.2026g1.txt | 2026-10-08 14:25 | 21 912 |
  | 1 | 13.11.2026g.txt | 2026-11-13 | 16 965 |

  `12.08.2026g.txt` (same map) is not imported on purpose. The October files were renamed to their header
  dates and imported with `tools/analysis/import_logs.py` (same parsing and hash as the app).
- Temperature tables stored for all six logs: LPG `-7,-5,-4,-2,0,2,3,4,5`, reducer `-7,-4,-1,0,0,0,0,0,0`.
  These are the tables of 13.11; the real October tables are **unknown** (assumed equal).

## Map prediction: what was measured (leave one file out, the 6 Capture2 logs)

"Prediction accuracy" = share of the hidden file's FAST trim the new map would cancel (shown under the
options after every Predict). Absolute values stay small because most of the FAST trim is per-reading noise;
compare variants.

| Step (2026-11-14) | Accuracy |
|---|---|
| Interpolation Weights off, Trim Delay off (old defaults) | −2.2 % |
| Both on (now the defaults) | 4.75 % |
| + skip non-regulating (FAST 0 on both banks) and load transients (BENZ > 2×) | 6.47 % |
| + `CorrectionGain` 0.5 → **0.8** (user's OK) | **8.82 %** |

Pressure Correction on/off: ±0.2 %, no real difference in these logs (almost no long low pressure). It stays.

Tried and **rejected** (worse or no gain, do not propose again without new data): smoothing of corrections,
trimmed mean instead of median, count-dependent damping, minimum readings per cell, separate gain for lean
and rich, removing a per-file FAST offset, weighting readings by duration, one median for both banks,
"min change" threshold above 0, skipping warm-up (reducer temperature or first minutes), MAP > 0.85 filter.
Trim delay: 850 ms confirmed best (650–1050 tested). Removed options: Smooth Corrections, Fill Empty
High-Load Cells, Use History, Always Apply Negative Trim, Map Regression.

## Temperature tables

- Verified from GAS in the logs: table values are **% of gas injection time**; each value is taken at its
  band's **upper temperature** (last band: +10 °C) and the ECU **interpolates linearly** between them.
- New `TemperatureCorrectionPrediction`: trims minus the map error, both tables fitted together with
  interpolation weights and shrinkage. Leave-one-file-out: old method −3.4 %, new +1.9 %.
- Suggestion from the 6 logs: LPG 0–10 and 11–20 −1 each (borderline, −0.57/−0.54 %, not applied),
  reducer ≤20 −2, 21–25 +1, 31–35 +1.
- **User's decision (2026-11-14):** set the reducer table in the ECU to **`-9,-3,-1,1,0,0,0,0,0`**,
  LPG table unchanged (`-7,-5,-4,-2,0,2,3,4,5`), map unchanged (Capture2).

## Next steps

1. The user changes the reducer table in the ECU and records new logs with the **same map (Capture2)**.
2. Import them under fuel table **Capture2** with reducer table `-9,-3,-1,1,0,0,0,0,0` and LPG table
   `-7,-5,-4,-2,0,2,3,4,5`. Do **not** use "same as previous" for the first new log: that would copy the
   old reducer table.
3. Analyze old + new logs together ("Analyze checked" on Capture2): every log's trims are brought to the
   newest log's temperature tables (`TrimNormalizer`). Then:
   - Temperature prediction: did the reducer bands ≤20, 21–25, 31–35 move towards 0? Anything new?
   - Compare the prediction accuracy of all logs vs. only the new logs (validates the temperature normalization).
4. Only then predict the new fuel map (gain 0.8) and load it into the ECU as a new fuel table.
5. After logs with the new map exist:
   - Check that changed cells did not overshoot (sign flips) — the risk of gain 0.8. If they do, go back to 0.6–0.7.
   - "Analyze together" (old map logs brought to the new map) vs. only new-map logs: the prediction
     accuracy shows whether the map normalization helps.
   - Proposed features, not built yet: a real "predicted vs. actual" check per cell after a map change,
     and a coverage map (cells with few readings, where to drive next).

## Open points

- Pressure correction formula shrinks positive (lean) trims at low pressure (×0.86 at 1.1 bar); changing it
  needs the user's OK and logs with long low pressure.
- October temperature tables unknown (see above).

## Tools (`tools/analysis/`)

Python copies of the app's algorithms, for trying changes before they go into C# (no numpy needed):

- `map_prediction.py` — parser grouping + map prediction + leave-one-file-out score. Reproduces the app.
  `LPG_DB` / `LPG_TABLE` environment variables select the database and fuel table. Does not normalize
  temperature tables yet: extend it before using logs with different tables.
- `temperature_tables.py` — old vs. new temperature-table method, leave one file out (~1–2 minutes).
- `ecu_temperature_model.py` — shows from GAS how the ECU applies the temperature tables.
- `import_logs.py` — imports log files into the database exactly like the app (`<db> <fuelTableId> <gasJson> <reducerJson> <files...>`).

Rule: an algorithm change is kept only when the prediction accuracy (app or `map_prediction.py`) improves.
