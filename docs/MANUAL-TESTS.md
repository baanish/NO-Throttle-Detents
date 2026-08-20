# Manual flight test matrix and history

Record the game version, aircraft, control device, frame-rate cap, other installed mods, and result notes for each run. Do not mark multiplayer safe from code inspection alone.

This checklist is a starting point for v0.1. The entries below preserve prior
inspection and flight history; a capability capture is not a detent test, and
an older DLL result is not evidence for a later DLL.

## Live airframe capability capture - 2026-08-18

- Runtime version: `Application.version` reported `0.34.1`; the main menu displayed `Early Access Version 0.34.2`.
- Scenario: Free Flight, Heartland, Boscali Defense Force, Maris Airport; all 13 selectable allowlisted airframes.
- Airframe-capture configuration: both hold durations `2000` ms, debug logging enabled. This was for easier observation and is not the release default.
- Passed: live identity/capability capture for every selectable allowlisted airframe. CAS1, Darkreach, FastBomber1, and Multirole1 exposed local split-airbrake surfaces with `maxSplit=45`, `30`, `60`, and `25`; Fighter1, SmallFighter1, trainer, and VTOLTrainer1 exposed local `Airbrake` components. Afterburner ranges were `0.900000..1.000000` on Fighter1 (1 nozzle), SmallFighter1 (1), Multirole1 (2), and FastBomber1 (4). Darkreach and EW1 had no afterburner (2 and 4 nozzles respectively). Helos and QuadVTOL used collective controls and were unaffected.
- Iterative-build reports: the user exercised 200 ms detents on several fixed-wing aircraft and reported that Vortex worked. These are historical reports and do not complete the matrix below.
- Medusa performance observation: 80 diagnostic samples covered frames 24,334 through 26,733 over 20.006 seconds (`119.92` FPS), with no per-nozzle compatibility spam. Medusa was not re-flown with the exact DLL recorded below.
- Absolute/HOTAS mode is an explicit vanilla pass-through. An older KR-67 absolute-mode trace is historical evidence only.
- Historical flight - 2026-08-18: DLL SHA-256 `443BF72E9BC432E18296373461CE0AC6ED70C4B0F7265EAA0C624095B4AEF7D9` was installed and flown on the KR-67 Ifrit in relative-throttle mode at 200 ms. The user reported the detents working. Its transition log is historical evidence from that older DLL; current debug logging records aircraft attachment and lifecycle resets only.
- Not validated: the full takeoff/flight-handling matrix, multiplayer, final-build Medusa performance, AB-4 behavior, or physical detent behavior on every aircraft. The all-airframe capability pass captured identity and systems; it did not physically exercise every detent.

## v0.1.0 smoke pass - 2026-08-19

- Exact DLL SHA-256: `A812F8C77DA11B1F9422952A94326FB5C5CEA2D9DA2D57EA070E4D21B491F09C`.
- Game: main-menu version `0.34.2`, `Application.version` `0.34.1`, Steam build `24724372`.
- Scenario: Free Flight, Heartland, Maris Airport; relative throttle enabled; Autopilot autoland disabled; roughly 100-115 FPS.
- Vanilla pass-through observed on CI-22 and EW-25 (no matching capability) and UH-90, SAH-46, and VL-49 (collective).
- Airbrake readiness appeared on T/A-30, VT-7, A-19, and SFB-81. Both-detent readiness and stationary upper/lower control-path smoke tests passed on FS-12, FS-20, KR-67, and AB-4. AB-4 matched all four expected nozzles.
- EW-25 held roughly 99-103 FPS during a ten-second cockpit smoke test; the previous 10 FPS regression did not recur.
- The automation pulse was shorter than the configured dwell, so this pass used a reduced duration only to prove boundary gating and release. It did not prove running-engine flames, AB-4 four-engine synchronization, or every physical airbrake surface. T/A-30 and SFB-81 surface movement and A-19 closure remain manual checks.
- Cleanup: release defaults restored to 200 ms and debug logging disabled; temporary mouse bindings removed; game closed; installed mod and generated config removed after testing.

## Final v0.1.0 confirmation - 2026-08-19

- Exact DLL SHA-256: `E7AFD87AC3224672906540A50147C74B248D90BFD239F79F5138ABDA57121E6C`.
- The exact Release DLL was installed for a final manual check and the user reported it working. No per-airframe result was recorded, so this does not expand the completed matrix above.
- The game was closed and the temporary plugin directory and generated config were removed after the check.
- The published package DLL SHA-256 is `388F96835AB5F5CF1C13BD4217D7AA2AEF9D4F9AA9661519C421EF0F1B1FD25C`. It was rebuilt from the same runtime source after changing only author and copyright metadata; that metadata-only DLL was not re-flown.

## Airframe preset capture - complete

The explicit allowlist is keyed by `UnitDefinition.jsonKey` and is documented in [AIRFRAME-PRESETS.md](AIRFRAME-PRESETS.md). The hidden UFO asset was not selectable/live captured and is not allowlisted. Unknown/new aircraft remain vanilla until an explicit preset is added.

## Baseline

- [x] Confirm the Release package's default config has `DebugLogging = false`.
  Enable it only for a diagnostic run, then restore the release default before
  distributing the beta.
- [ ] Launch with a clean extraction of the standalone ZIP. Expected: Nuclear Option reaches the main menu.
- [ ] Inspect `BepInEx\LogOutput.log`. Expected: one `Nuclear Option Detents 0.1.0 loaded.` line.
- [ ] Inspect the same log. Expected: one aggregate load line lists the throttle observer, Airbrake gate, split-airbrake gate, and afterburner gate statuses. An unavailable target is reported as `unavailable` and leaves that vanilla path unchanged; this is not a red Harmony error.
- [ ] Fly at midrange throttle. Expected: normal throttle response is unchanged.
- [ ] Exercise pitch, roll, yaw, wheel brakes, weapons, and Custom Axis 1. Expected: no change attributable to this mod.

## Upper detent

- [ ] Start near 50% and hold increase until the aircraft's captured full-dry/afterburner boundary. Expected: full dry thrust, with no immediate afterburner.
- [ ] Release increase less than 200 ms after reaching the boundary. Expected: afterburner remains off and the attempt resets.
- [x] Press and hold increase again at the boundary for 200 ms. Expected: afterburner becomes available at the dwell boundary. User-confirmed on KR-67.
- [ ] Release increase while remaining at the boundary. Expected: afterburner remains available because the latch stays unlocked.
- [x] Reduce throttle. Expected: afterburner stops normally and the upper detent relocks below the dry-thrust boundary.
- [ ] Return to the full-dry boundary without the extra endpoint hold. Expected: afterburner remains off.

## Lower detent

- [ ] Spawn with throttle already at 0%. Expected: the lower detent starts locked; hold decrease for the configured dwell before the automatic airbrake becomes available.
- [ ] Start near 50% and hold decrease until the display reaches 0%. Expected: genuine zero engine throttle, with no immediate automatic airbrake.
- [ ] While the lower detent is locked, release decrease before the configured dwell, then press increase. Expected: the timer resets and the interruption passes vanilla while throttle responds above idle.
- [x] Release decrease less than 200 ms after reaching 0%. Expected: airbrake remains inhibited and the attempt resets.
- [x] Press and hold decrease again at 0% for 200 ms. Expected: the automatic airbrake begins its vanilla opening behavior. User-confirmed on KR-67.
- [ ] Release decrease while remaining at 0%. Expected: the airbrake remains available because the latch stays unlocked.
- [x] Increase throttle. Expected: the airbrake closes normally and the lower detent relocks.
- [ ] Return to 0% without the extra endpoint hold. Expected: the automatic airbrake remains inhibited.

## AB-4 four-engine case

- [ ] Spawn the Alkyon AB-4 at Maris with both holds set to 200 ms. Expected: status reports both detents after the four matching nozzles and split-airbrake surfaces have run.
- [ ] Use Numpad Minus to stop the engines, then test the lower detent without moving. Expected: the split airbrakes stay closed until decrease is held for the full dwell, then open together.
- [ ] Restart all four engines and test the upper detent. Expected: none enters afterburner before the full dwell; all four enter afterburner together after it.
- [ ] Release increase before 200 ms, then retry. Expected: the first attempt resets and no engine enters afterburner early.
- [ ] Reduce throttle below the dry-thrust boundary. Expected: all four leave afterburner and the upper detent relocks.

## Configuration

- [x] With BepInEx Configuration Manager installed, open the mod panel. Expected: the labels use plain names with units, the status is read-only, and the status row has no Reset button. Verified during UI QA.
- [x] Hover over each control and the status row. Expected: each tooltip is brief and contains only the setting's purpose, units, or essential consequence. Verified during UI QA.
- [x] Enter each selectable aircraft and compare its observed systems with [AIRFRAME-PRESETS.md](AIRFRAME-PRESETS.md). Expected: known IDs use only their pinned features; unknown/new IDs remain outside the preset and use vanilla behavior. This confirms identity/capability presence, not physical detent behavior.
- [ ] Turn off `Use Throttle Relative Axis` in Nuclear Option. Expected: relative-only behavior is bypassed; throttle, airbrake, and afterburner remain vanilla.
- [ ] Set both hold times to 500 ms and relaunch. Expected: both endpoint delays are clearly longer.
- [ ] Disable only the idle detent. Expected: vanilla zero-throttle airbrake behavior and a working upper detent.
- [ ] Disable only the afterburner detent. Expected: vanilla afterburner behavior and a working lower detent.
- [ ] Disable the General master switch. Expected: full vanilla throttle, airbrake, and afterburner behavior.
- [ ] Toggle only `DebugLogging` during an unfinished hold and again after an unlocked latch. Expected: elapsed dwell and unlocked state are preserved.
- [ ] Set each duration to zero. Expected: its endpoint changeover is immediate on an endpoint-plus-command frame.
- [ ] Enter malformed and out-of-range values, then relaunch. Expected: no startup crash; BepInEx uses a valid value or the plugin clamps it.

## Lifecycle and isolation

- [ ] Respawn in a new aircraft. Expected: both holds and latches start locked.
- [ ] Eject and enter another aircraft where the mission allows it. Expected: old aircraft state is not reused.
- [ ] Restart a mission. Expected: both detents reset.
- [ ] Return to the menu and load another mission. Expected: both detents reset and the log contains no repeated patch installation.
- [ ] Fly an aircraft without afterburner. Expected: normal dry-thrust behavior and no errors.
- [ ] Fly an aircraft without an Airbrake component. Expected: normal behavior and no errors.
- [ ] Observe AI and remote aircraft. Expected: no detent-driven change. This observation alone does not prove multiplayer safety.
- [ ] Repeat endpoint tests at multiple frame-rate caps above 10 FPS. Expected: dwell time stays time-based rather than frame-count-based.
- [ ] Repeat with an observer gap longer than 0.1 seconds. Expected: component gates pass vanilla by design until a fresh throttle observation arrives.
- [ ] Hold Axis Modifier while commanding throttle at an endpoint. Expected: an unfinished detent hold cancels and Custom Axis 1 remains vanilla.
- [ ] Switch to absolute or HOTAS throttle mode. Expected: the mod performs no boundary hold or airbrake/afterburner gating.

## Release notes

The tested v0.1.0 DLL above received identity/readiness coverage for all 13
allowlisted airframes and reduced-dwell boundary checks on FS-12, FS-20, KR-67,
and AB-4. The current source requires exact aircraft identity and pins each
airbrake path. Repeat the affected aircraft checks on the exact release DLL.
