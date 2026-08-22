# Changelog

## 0.2.0

- Adds a native flight-HUD indicator below the throttle gauge while either detent is locked or counting its hold.
- Adds a 0.25x to 4x relative-throttle sensitivity multiplier for aircraft with a supported detent; 1x preserves the game rate.
- Coexists with PauelsRandomFixes' `ThrottleRelativeVelocity`: Detents follows its signed throttle mapping and leaves sensitivity control to that fix while it is active.
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
