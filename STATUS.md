# Tuning status and next steps

Read this first when a new session starts. `CLAUDE.md` describes the code and the rules; this file says
**where the tuning stands**, what was measured, what was decided and what comes next. Update it at the
end of every working session.

Last update: **2026-11-18**.

## The car and the data

- Car: **Mercedes M272 3.0 V6** (two banks), LPG ECU AEB MP6C-type, petrol trims over OBD.
- Database: `LPGDbAnalyzer/Data/lpg.db`, **committed to git** (since 2026-11-15; commit it after every import
  session, with the app closed). The old copy in `LPGDbAnalyzer/bin/Debug/net10.0-windows/Data/` is no longer
  used; the backup from before the October import, `lpg.db.bak-before-october-import`, is still there (not in git).
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
  | 7 | 16.11.2026g1.txt | 2026-11-16 07:03 | 18 194 |

  `12.08.2026g.txt` (same map) is not imported on purpose. The October files were renamed to their header
  dates and imported with `tools/analysis/import_logs.py` (same parsing and hash as the app).
- Temperature tables stored for all six logs: LPG `-7,-5,-4,-2,0,2,3,4,5`, reducer `-7,-4,-1,0,0,0,0,0,0`.
  These are the tables of 13.11; the real October tables are **unknown** (assumed equal).
  Log 7 (16.11) was recorded with the **new reducer table `-9,-3,-1,1,0,0,0,0,0`** (LPG table unchanged).

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

## Check of the new reducer table (2026-11-16, 7 logs, `tools/analysis`)

- **Temperature normalization works**: the 16.11 log (not changed by the normalization) is predicted at
  9.46 % with the old logs brought to its tables vs. 8.28 % without. All files: 9.51 % vs. 8.15 %.
- Residual FAST (after the map error) per reducer band, old logs (normalized) | new log:
  ≤20 −4.3 (n 50) | no data; 21–25 −0.2 | −2.6 (n 54); 26–30 −0.2 | −0.1; 31–35 +0.6 | −1.7 (n 128);
  36–40 0.0 | +0.2; 41–50 +0.2 | −0.1. The new log hints that 21–25 and 31–35 are now slightly rich,
  but it is one cold-morning drive with few readings there.
- Temperature fit (shrinkage 200): new log alone suggests no change; all 7 suggest reducer ≤20 −1, 21–25 +1
  (driven by the old logs), LPG 11–20 −1 (borderline, −0.69, again). Leave-one-file-out of the temperature
  method is now −0.05 %, i.e. no reliable signal. **Recommendation: leave both temperature tables as they are.**
- Map prediction from all 7 logs: 92 of 144 cells change, median |change| 2 %, largest −17 % (high load,
  8.3–12 ms at 700–1000 and 4000–4700 rpm).

## Check with 3 logs of the new reducer table (2026-11-17)

- Imported: log 8 `16.11.2026g2.txt` (16.11 16:32, 5 301 rows), log 9 `17.11.2026g1.txt` (17.11 07:23,
  7 290 rows), both Capture2 with LPG `-7,-5,-4,-2,0,2,3,4,5`, reducer `-9,-3,-1,1,0,0,0,0,0`.
- Map prediction accuracy, 9 logs (`map_prediction.py`): **9.28 %** (per file 6.2–11.9 %; the new logs
  10.23 / 6.24 / 7.66 %).
- Residual FAST per reducer band, old logs | 3 new logs: ≤20 −4.4 (n 50) | **+2.1** (n 140, median +2.9);
  21–25 −0.2 | +0.5; 26–30 −0.2 | +0.4; 31–35 +0.6 | −0.4; 36–40 0.0 | +0.3; 41–50 +0.2 | 0.0.
  21–25 and 31–35 now sit near 0 (per log 21–25 is −2.6 / +1.7 / +0.8: noise). **≤20 is now ~2 % lean** in
  both logs that reach it (16.11g2 +2.2 n 98, 17.11 +1.9 n 42): the −2 step overshot.
- Temperature fit: new logs suggest reducer ≤20 +1 (fit +1.09), nothing else; all 9 suggest reducer 26–30 +1 and 51–60 +1
  (fits +0.65 and +0.51, borderline; 51–60 has no new data). LOO score of the temperature method −0.50 %: no reliable signal overall.
- Gas table: all bands within ±0.6 in the new logs; leave it.

## Next steps

**To do next session (2026-11-19, user checks in AEB / the app):**
- **4 ms extra-injection field:** in AEB, change the "extra injection time" (now 4 ms) to e.g. 3.5 ms, save as a test file
  (don't write to the ECU), and note whether the "extra injection sensitivity" check/slider is on. Diff the decoded file
  to confirm the key is `TempoMaxExtraInjBenz`. Then choose one of the four options in BACKLOG "Gas time step at 4 ms"
  (gas time is ~0.39 ms shorter below 4.00 ms petrol time; trims +0.8 just below, −0.8 just above).
- **Bank 2:** the test file `baracuda v2.94test.ml8` (v2.94 with "Fuel trim bank 2" = −2) was deleted at the user's
  request; recreate it in AEB if needed. Decide: petrol reference log first, or a −2 config and one gas log
  (GAS2/BENZ2 ÷ GAS1/BENZ1 should go ~1.000 → ~0.980 if the unit is %). Optional: a file with different values per rpm
  to see whether the ECU uses the 12 values separately.
- Try the restructured Data tab in the running app (tree, Download, check screen, analysis of v2.92 + v2.93, Export .ml8),
  (committed untried in 85bd554 at the user's request, with the migrated database).

1. **User's decision (2026-11-17):** reducer table → **`-8,-3,-1,1,0,0,0,0,0`** (≤20 back by 1), LPG table
   unchanged, and deploy a new fuel map predicted from all 9 Capture2 logs.
2. **New map to load (2026-11-17)**, `map_prediction.py` with the app defaults (gain 0.8, **Min Count 1**),
   trims brought to the tables the ECU will run (`load_all(target=(LPG, reducer -8…))`; vs. the −9 target only
   1800/2.9 differs by 1). Accuracy 9.35 %. 82/144 cells change. Rows = injection ms, columns = rpm
   (700 1000 1400 1800 2200 2600 3000 3400 4000 4700 5400 6200):

   ```
    1.90   125 122 130 114 139 128 137 128 151 147 146 145
    2.40   119 123 148 130 147 123 147 146 154 154 154 142
    2.90   125 150 132 135 146 137 163 159 167 166 159 140
    3.40   138 150 150 146 156 157 153 162 162 161 157 156
    4.00   136 141 136 139 141 153 145 157 172 175 174 173
    4.80   122 129 122 139 135 144 136 159 193 194 186 185
    5.80   129 139 144 135 133 146 124 155 159 182 189 188
    7.00   147 137 130 115 137 148 141 148 168 177 178 177
    8.30   139 154 115 137 137 136 138 117 149 169 169 168
    9.60   142 151 136 122 134 135 141 137 153 173 175 174
   10.80   143 149 136 130 149 133 165 152 161 181 180 179
   12.00   142 147 134 124 144 136 176 169 180 181 179 178
   ```

   Largest cuts: 4000 rpm × 8.3–10.8 ms (−11…−12 %; 4000/9.6 two logs −15 %), 3400 × 9.6–10.8 (four logs
   −6…−12 %), 700/1000 × 8.3–9.6 (−5…−7 %).
   **Option sweep (2026-11-17, 9 logs, target −8):** Min Count (weight sum of both banks, ≈ 2 per reading):
   0 → 9.31 %, **1 → 9.35 %** (holds back 12 cells with weight ≤ 1: 700/10.8–12, 1000/10.8–12, 3400/12,
   4000/2.4, 2.9, 4.8, 5.8, 7.0, 4700/8.3, 4700/9.6; with 0, 4700/9.6 got −17 % from spill-over weight only),
   2 → 8.86 %, 4 → 8.52 %, 6 → 7.85 %, 20 → 6.74 %. Min change 0.5 → 9.31 % (2 cells), 1 → 8.06 %,
   2 → 6.40 %. Max Benz Diff 3 → 9.05 %, 4 → 9.03 %, 6 (default) → 9.31 %, 10 → 9.23 %.
   Finer, with Min Count 1 (newest-log target): 4 → 9.09, 5 → 9.15, **6 → 9.33**, 7 → 9.36, 8 → 9.21, 10 → 9.26,
   15 → 9.28, off → 9.26 %. 6 and 7 are equal within noise (7 is worse on 17.11 by 0.8): **6 stays**.
   **User's decision:** Min Count 1 is the default (app and `map_prediction.py`), and the Prediction grid greys
   out the cells kept for lack of data (see "Done in the code").
3. Next: the user loads the map and the reducer table into the ECU, records logs, and imports them under a
   **new fuel table** (screenshot → OCR → check screen) with reducer `-8,-3,-1,1,0,0,0,0,0`.
   Then check the app's prediction on the old logs agrees with the Python copy (9.33 % with the newest
   log's tables as target, which is what the app uses).
4. After logs with the new map exist:
   - Check that changed cells did not overshoot (sign flips) — the risk of gain 0.8. If they do, go back to 0.6–0.7.
   - "Analyze together" (old map logs brought to the new map) vs. only new-map logs: the prediction
     accuracy shows whether the map normalization helps.
     Third variant (user's idea, BACKLOG "Carry skipped cells' readings over"): old-map readings only where the
     map did not change, so the cells kept by Min Count keep collecting data across maps.
   - Proposed feature, not built yet: a real "predicted vs. actual" check per cell after a map change.
     (The grey cells of the Prediction grid now work as a coverage map.)

## First check of the new map (2026-11-18, 2 logs)

- The user loaded the new map and reducer `-8,-3,-1,1,0,0,0,0,0`. Fuel table **2** "Screenshot 2026-11-18 0758521"
  matches the predicted map. One difference: 1800/2.9 is 136, not 135, because it is the variant predicted for the
  −9 reducer table. Logs 10 `17.11.2026g2.txt` (17.11 16:40, 13 464 rows) and 11 `18.11.2026g1.txt`
  (18.11 07:06, 12 272 rows) are imported under it.
- **No overshoot.** 50 changed cells have weight ≥ 4 in the new logs. 3 of them changed sign by more than 1 %:
  2600 × 2.4 / 10.8 / 12.0, each with little data (weight 5–11). Gain 0.8 stays.
- **The changed cells improved less than expected.** Per-cell median FAST on the new map ≈ 0.56 × old − 0.4.
  If trims followed the map one to one, it would be ≈ 0.2 × old. The weighted RMS of the changed cells went
  only from 1.60 to 1.56.
- **The new logs run about 0.8 % richer overall.** Unchanged cells moved from −0.1 to −0.8…−0.95. SLOW on
  bank 1 dropped from 5.5 to 3.9 on 18.11.
- With only two logs this is weak evidence. Leave-one-out accuracy using only the new-map logs is 6.96 %
  (17.11g2 5.35 %, 18.11 9.75 %).
- The GAS check of which map the ECU used was inconclusive: too few rows where the maps differ by ≥ 3 %.
- Next: collect more new-map logs. Then measure the trim response to a map change, i.e. the slope above. It
  matters for "Analyze together", whose map normalization assumes a slope of 1.

## AEB configuration files (2026-11-18)

- Saves in `Documents\MultipointInj\ConfigLPG`: v2.91 (4.10 11:56), v2.92 (4.10 17:33) = Capture2, v2.93 (14.11) = Capture2
  with reducer −9, v2.94 (17.11 16:19) = fuel table 2 with reducer −8. The OCR'd maps are exact.
- **October temperature tables confirmed** from v2.91/v2.92: LPG `-7,-5,-4,-2,0,2,3,4,5`, reducer `-7,-4,-1,0,0,0,0,0,0`
  (as assumed).
- v2.91 is a different map (49 cells). A GAS fit says **04.10g2 (13:59) was most likely recorded with v2.91**, not Capture2
  (scatter 3.32 % vs 3.45 %; 04.10g1 undecided; every other log matches its table). The user chose to leave it as it is for now.
- The AEB software opened an edited .ml8 (map all 100, both temperature tables changed) correctly: writing .ml8 works.
- Built: import of .ml8 into LPGDbAnalyzer and "Export .ml8..." of a predicted map (see CLAUDE.md). The Ml8Tool folder and the
  test file were deleted at the user's request.
- **Data tab restructured (2026-11-18, user's request):** tree = car → .ml8 → logs; the original logs are stored (download);
  stored predictions, the history list, "Save to database" and "Export Selected Cells" removed (the 7 stored predictions of
  14.11 were dropped). Database migrated (backup taken before): fuel table 1 = **v2.92** (Capture2 map, reducer −7; logs
  04.10–13.11), new **v2.93** (same map, reducer −9; logs 16.11g1, 16.11g2, 17.11g1 moved there), fuel table 2 = **v2.94**,
  new **v2.91** (no logs). Every log's temperature tables equal its configuration's. `map_prediction.py` now takes the logs of
  every configuration with the same map: 9.33 % on the 9 Capture2-map logs, unchanged. Not yet tried in the running app.
- AEB manual read; Adaptivity is **off** in all configs (user). `tools/analysis/pressure_bank_check.py`: the ECU's own
  pressure compensation curve leaves no pressure trend in the trims at 1.3–1.6 bar (no change needed; < 1.3 bar untested).
  The bank difference (~1.6 %) is almost all SLOW, the gas ECU feeds both banks alike; "Fuel trim bank 2" waits for a
  petrol reference log (see BACKLOG). Per cell (`tools/analysis/bank_cells.py`) the bank difference is real and follows
  load (+3 at 2.9–5.8 ms, ~0 at 7–9.6 ms); one bank-2 value gets most of what is fixable. AEB has one map for both banks
  (as far as the file shows). Gas time steps at 4.00 ms petrol time (`tools/analysis/gas_step_4ms.py`).

## Done in the code on 2026-11-17

- `FuelMapPrediction.BuildTable` also returns the readings' weight per cell. The Prediction grid greys out
  cells that keep their value: light grey = no readings, dark grey = weight ≤ Min Count; every cell's tooltip
  shows its weight. Min Count default 0 → 1 (`PredictionOptions`, the text box). Built, 0 warnings; not yet
  looked at in the running app.
- Skipped readings list (Prediction screen, bottom left) is now `SkippedReadingsView`: "Reason" column, filter by
  reason with counts, explaining columns only (BENZ_Diff, LoadRatio, FAST, …). A first version (unbound auto-sized
  column) froze the app and was replaced. Expected counts on the 9 logs: 2 180 load transient, 1 768 bank
  difference ≥ 6 %, 978 not regulating, 8 no delayed rows; 10 754 of 15 688 used. Built; not yet seen in the app
  (it was running, so LPGDbAnalyzer's copy of LPG.Core.dll was locked: rebuild after closing it).
- `map_prediction.py`: `load_all(target=...)`, `build(min_count=1)` default.

## Done in the code on 2026-11-16

- Prediction screen (LPG.Core, so LPGDbAnalyzer only) regrouped: actions row (Predict, Save to database,
  Export Selected Cells), "Prediction options" box (Pressure Correction + Reference Press, disabled while off;
  Interpolation Weights; Trim Delay; Round; Min % change; Min Count; Max Benz Diff) and "Show" box (display
  filters). Built, not yet looked at by the user in the running app.
- Committed as 59702e1 (with the database and the 16.11 log).

## Open points

- Pressure correction formula shrinks positive (lean) trims at low pressure (×0.86 at 1.1 bar); changing it
  needs the user's OK and logs with long low pressure.
- October temperature tables unknown (see above).

## Tools (`tools/analysis/`)

Python copies of the app's algorithms, for trying changes before they go into C# (no numpy needed):

- `map_prediction.py` — parser grouping + map prediction + leave-one-file-out score. Reproduces the app.
  `LPG_DB` / `LPG_TABLE` environment variables select the database (default `LPGDbAnalyzer/Data/lpg.db`) and
  fuel table. Brings every log to the newest log's temperature tables like `TrimNormalizer` (since 2026-11-16;
  `load_all(normalize_tables=False)` to skip; `load_all(target=(gas, reducer))` to predict for tables set in
  the ECU after the last log). Map normalization (logs of several maps) is not in it yet.
- `temperature_tables.py` — old vs. new temperature-table method, leave one file out (~1–2 minutes).
- `ecu_temperature_model.py` — shows from GAS how the ECU applies the temperature tables.
- `temperature_check.py` — checks a temperature-table change: residuals per band, old vs. new-table logs
  (new = logs with the newest log's tables, or the file names given as arguments), and the fits.
- `import_logs.py` — imports log files into the database exactly like the app (`<db> <fuelTableId> <gasJson> <reducerJson> <files...>`).

Rule: an algorithm change is kept only when the prediction accuracy (app or `map_prediction.py`) improves.
