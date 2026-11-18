# Backlog

## Car profiles

Support several cars, each with its own ECU information and data.

- Add a `Cars/` subfolder in the data folder, with one folder per car. Each car folder holds:
  - the car: engine, banks, modifications;
  - the petrol ECU: type (e.g. Bosch ME9.7), trim step and limits, OBD refresh rate, when it leaves closed-loop control;
  - the LPG ECU: type (e.g. AEB MP6C), map formula (what a map value means, fixed time added), calibration/reference pressure, temperature correction tables, injector brand and nozzle size, reducer and rail layout;
  - the data parameters: log column meanings (`PRESS` absolute or relative, `LAMBDA`, `OX`), map axes (RPM and injection breakpoints), trim delay;
  - its log files and fuel table image.
- At startup, select a car or add a new one.
- Settings that are now static in `Models/Settings.cs` (axes, temperature tables) and the "Reference Press" value move into the car profile. This overlaps with the planned dynamic map axes (see `CLAUDE.md`).

## Carry skipped cells' readings over to the next fuel map

Idea from the user (2026-11-17): readings in cells held back by Min Count (dark grey in the Prediction grid)
should be used again together with the logs of the next map, until the cell has enough data.

- "Analyze together" already does this in part: it brings old-map logs to the newest map. For a reading whose
  surrounding cells did not change between the maps (Min Count keeps the old value), the map ratio is exactly 1,
  so the old reading is as valid as a new one: no assumption is needed there.
- For readings around changed cells it relies on "trim follows the map change one to one", which is not verified.
- Possible option for "Analyze together": take old-map readings **only where the map did not change** (all
  surrounding cells equal in both maps), new-map readings everywhere. Then skipped cells collect data across maps
  without the unverified normalization touching the rest.
- Decide with the prediction accuracy once logs of the new map exist: new-map logs only vs. together (all) vs.
  together (old readings only in unchanged cells).
- Readings rejected by the filters (bank difference, not regulating, load transient) are not carried over: they
  would be rejected again.

## Bank balance and a petrol reference log (2026-11-18)

- On gas, bank 1's total trim (SLOW + FAST) is ~1.6 % above bank 2's (median over 72 000 regulating rows of the 11 logs;
  +2.3 at 700/1800/2200 rpm, +0.8 elsewhere). The AEB "Fuel trim bank 2" setting (`CorrSecondaBancata0..11`, all 0) could
  balance the banks, but the petrol ECU's own bank difference on petrol is unknown.
- AEB's manual tunes K until the trims on gas equal those on petrol. A log recorded **on petrol** (switch held on petrol,
  warm engine, mixed driving) would give that reference per bank: SLOW per bank on petrol vs on gas shows how much of the
  bank difference is the gas system's. Then decide on a bank-2 correction (and whether SLOW tells something about the map).
- "Adaptivity" (AEB screen Modify carburation) is **off** in all configurations (user, 2026-11-18): the prediction's
  assumption that the ECU does not adapt the gas time itself holds.

## The ECU's pressure compensation curve (2026-11-18)

- The .ml8 holds the ECU's own reducer-pressure compensation (`[PressioneDifferenziale]`, 15 points 800–1734 mbar, 0 at
  1489). Check from the logs whether FAST (after the map error) still depends on reducer pressure. If it does, the cleaner
  fix is this curve rather than the map cells or the prediction's "Pressure Correction". The curve is not shown in the
  manual, so first find out whether the AEB software shows and writes it; never write it blind.
- **Checked 2026-11-18** (`tools/analysis/pressure_bank_check.py`, leave one file out, raw FAST minus the map error at the delayed rows, both
  banks): the regulating readings lie at 1.27–1.65 bar (95 % within 1.40–1.60). Residual medians per pressure bin stay within
  ±1.7 % with no trend (Capture2 map, 9 logs: 1.30–1.40 −1.7, 1.40–1.45 +1.2, 1.45–1.50 −0.1, 1.50–1.55 +0.4, 1.55–1.60
  −0.5; overall slope −0.4 %/bar; per-log slopes −32…+26, contradicting each other; v2.94, 2 logs, the same). **No reason to
  change the curve.** Low pressure (< 1.3 bar) hardly appears in regulating readings, so the curve is untested there;
  look again when logs with long high load exist.
- Bank 2 detail (same script): the bank difference is almost all **SLOW** (SLOW1 − SLOW2 = +3.1 below 2250 rpm, +0.8 above;
  FAST1 − FAST2 −0.8…0). GAS/BENZ is the same on both banks (ratio 0.998–1.005 up to 3000 rpm), so the gas ECU treats the
  banks alike and `CorrSecondaBancata` = 0 really does nothing. SLOW is learned over petrol and gas, so only a petrol log
  (with SLOW given time to re-learn) tells whether the difference is the gas system's.
- `CorrSecondaBancata` tested 2026-11-18: one value on the AEB screen, written to all 12 keys as shown (−2 → `-2`).
  Units still to confirm: with −2 in the ECU, GAS2/BENZ2 ÷ GAS1/BENZ1 should drop from ~1.000 to ~0.980 if it is %.
- Per cell (`tools/analysis/bank_cells.py`, 2026-11-18, Capture2 map, 9 logs, per OBD reading): TrimDiff is +2.3…+3.9 at
  1400–2200 rpm × 2.9–5.8 ms and at 10.8 ms, about 0…+1.5 at 7–9.6 ms and at 1.9–2.4 ms. The pattern repeats between
  the two halves of the logs (correlation 0.63 over 44 cells), so it is real, and it follows load more than rpm. RMS of
  TrimDiff: 3.86 as is, 3.44 after one constant (2.34), 3.39 per rpm column, 3.21 per cell: one value gets most of what a
  bank-2 correction can get; the rest needs a per-cell bank-2 map, which AEB does not have. The app's "FuelCorrection"
  report (BENZ difference) is not the measure for this: the gas ECU already follows each bank's BENZ, and at 700–1000 rpm
  BENZ2 > BENZ1 although TrimDiff is positive (the petrol ECU's own bank differences). A petrol log per cell tells which part
  of the pattern is the petrol side.
- If "Fuel trim bank 2" is ever used: store it with the configuration (like the temperature tables) and let the analysis
  know, since logs recorded with it have a different bank balance.


## Gas time step at 4 ms petrol time (2026-11-18)

- `tools/analysis/gas_step_4ms.py` (Capture2 map, 9 logs, fixed temperature window): GAS / (BENZ × interpolated K) is
  0.89–0.90 for BENZ 3.5–3.99 ms and jumps to 0.97 at exactly 4.00 ms, in every log and every rpm band. Fitted as
  GAS = a·BENZ·K + c: below 4 ms a = 0.79, c = 0.55 ms; 4–8 ms a = 0.80, c = 0.94 ms, so below 4 ms the ECU gives
  ~0.39 ms (~7–8 % there) less gas for the same map value. FAST (at the delayed position, 1000–2200 rpm) shows it too:
  +0.8 at 3.7–3.9 ms, −0.8 at 4.0–4.3 ms.
- AEB has an extra-injection time field showing 4 ms (user); in the file only `Iniezione.TempoMaxExtraInjBenz` = 1562 ticks
  = 4.00 ms fits. Confirm with a test save (change the field, diff; don't write to the ECU). `TempiExtraIniettate0..2`
  = 0, 234, 39 are probably the extra-injection sensitivity settings.
- The map cannot show a step: with bilinear interpolation, row 4.0 serves both sides. Options, each needing the user's OK:
  (1) leave it (costs ~±0.8 % trim near 4 ms); (2) two injection breakpoints close together around 4 ms (costs a row
  elsewhere); (3) lower the 4 ms field below the shortest normal petrol time (~1.4 ms) and reduce the rows ≤ 4.0 to keep
  the gas time (≈ −0.39 ms / (0.8·BENZ) of K) — but what else the ECU does with "extra injections" is unknown;
  (4) let the prediction know the step (the score shows whether it helps).
