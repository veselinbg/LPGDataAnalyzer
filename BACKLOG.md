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
