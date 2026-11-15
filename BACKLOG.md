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
