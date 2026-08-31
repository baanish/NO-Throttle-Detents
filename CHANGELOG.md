# Changelog

## 0.4.0

- Adds explicit presets for Aryx's MC-260 Chimera, F-16M King Viper, F-99 Shrike, FS-41 Eclipse, and OA-27 Cavalier, plus the FS-3 Ternion.
- Pins each add-on aircraft's serialized `jsonKey`, airbrake path, afterburner range, and nozzle count. Unexpected or missing runtime components still leave that capability vanilla.
- Adds an Aircraft Profile selector populated from the installed aircraft catalog, with seat entry as a fallback. Every aircraft keeps independent custom settings, while the runtime always selects by the local aircraft ID.
- Adds Reset Profile to restore only the selected aircraft's custom settings.
- Adds up to eight custom interior detents per aircraft profile. Positions use idle-to-MIL percentages, work in both directions, share that profile's hold time, and appear on the flight HUD.
- Keeps the add-on mods optional: Nuclear Option Detents has no Blueprinter or aircraft-mod assembly dependency.
- A manual pass loaded all six aircraft and confirmed the expected live components without a Detents exception. An MC-260 ground-roll check also confirmed its separate Brake-plus-throttle reverser still works.

## 0.3.0

- Moves detent enforcement to the local pilot's throttle path and removes the per-aircraft airbrake, control-surface, and nozzle patches.
- Requires `GameManager.IsLocalAircraft(Aircraft)` before changing throttle state. AI and remote aircraft no longer run detent logic.
- Confirms supported airbrakes and afterburners through read-only owner-field scans, with a bounded retry while an expected component is still loading.
- Bypasses detents and sensitivity while Auto Hover is on. Turning Auto Hover off resumes from the current throttle state.
- Yields while another Harmony patch actively owns the local throttle output, then resumes from a fresh lock when vanilla ownership returns.
- Refreshes throttle-patch ownership on each seat entry and accounts for inverted collective output when comparing another mod's throttle value.
- Adds opt-in Network Validation logs and a PowerShell analyzer for two-client checks. The observer always includes the local aircraft, accepts one remote owner, and caches components by aircraft identity. Both diagnostic switches remain off in release packages.
- Parks active holds `0.0001` inside each changeover so the value survives the game's half-precision throttle transport.
- Live QA on Nuclear Option 0.34.2, Steam build 24724372, covered all 13 selectable airframes. Vortex and Ifrit exercised both detents, Auto Hover bypassed the mod, and an active external throttle writer received control without detent interference. Collective and unsupported aircraft remained unaffected. Remote-owner two-client validation remains outstanding.

## 0.2.0

- Adds a native flight-HUD indicator below the throttle gauge while either detent is locked or counting its hold.
- Adds a 0.25x to 4x relative-throttle sensitivity multiplier for aircraft with a supported detent; 1x preserves the game rate.
- Coexists with PauelsRandomFixes' `ThrottleRelativeVelocity`: Detents follows its signed throttle mapping and leaves sensitivity control to that fix while it is active.
- Falls back to vanilla when throttle prefixes are ambiguous, and disables the HUD indicator after a render failure.
- Keeps absolute/HOTAS, collective, unsupported aircraft, and missing systems vanilla.
- Adds focused mapping, sensitivity-ownership, aircraft-bypass, and indicator-policy tests.
- Manual testing of the final DLL on Nuclear Option 0.34.2, Steam build 24724372, covered Cricket, FS-20 Vortex, KR-67 Ifrit, and Tarantula.

## 0.1.0

- Initial prototype. Recorded testing covered identity and readiness across all 13 selectable airframes; FS-12, FS-20, KR-67, and AB-4 also passed reduced-dwell upper and lower control-path checks.
- Adds configurable 200 ms relative-throttle detents for automatic airbrake and afterburner changeovers.
- Pins compatibility to the 13 selectable airframes captured from the installed game; unknown aircraft remain vanilla until explicitly added.
- Handles component and split-surface airbrakes, and filters only an existing vanilla afterburner decision.
- Requires the captured afterburner nozzle count and range to match before enabling the upper detent, including all four AB-4 engines.
- Keeps collective and absolute/HOTAS throttle modes vanilla.
- Includes a pinned BepInEx 5.4.23.5 standalone package, a configuration-preserving plugin-only package, and a flat NOMM package.
- Includes a generated NOMNOM manifest draft for the public registry submission.
- Includes focused state-machine tests and package validation.
