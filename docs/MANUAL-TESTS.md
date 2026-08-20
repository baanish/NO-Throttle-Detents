# Manual flight tests

Record each run against the exact DLL SHA-256, game build, aircraft, control
device, frame-rate cap, and other installed mods. A capability capture is not
a detent test, and a result from one DLL is not evidence for another. Do not
mark multiplayer safe from code inspection alone.

## Test history

### Airframe capability capture - 2026-08-18

- Game: main-menu version 0.34.2, `Application.version` 0.34.1.
- Free Flight, Heartland, Boscali Defense Force, Maris Airport. All 13
  selectable allowlisted airframes entered; identity, collective mode,
  airbrake path, and afterburner range/nozzle count captured for each.
  Results are in [AIRFRAME-PRESETS.md](AIRFRAME-PRESETS.md). The hidden UFO
  asset was not selectable and is not allowlisted.
- The capture ran with 2000 ms dwells and debug logging enabled; those are not
  release defaults. This pass captured capabilities and did not flight-test
  detents.
- Dev DLL SHA-256
  `443BF72E9BC432E18296373461CE0AC6ED70C4B0F7265EAA0C624095B4AEF7D9` passed a
  KR-67 Ifrit relative-throttle flight with 200 ms dwells. Earlier dev builds
  also passed 200 ms detent checks on several fixed-wing aircraft, including
  the FS-20 Vortex, without per-airframe records.
- EW-25 Medusa held about 120 FPS over a 20-second sample with no per-nozzle
  compatibility log spam (earlier dev DLL; not re-flown on the DLLs below).

### v0.1.0 smoke pass - 2026-08-19

- DLL SHA-256:
  `A812F8C77DA11B1F9422952A94326FB5C5CEA2D9DA2D57EA070E4D21B491F09C`.
- Game: main-menu version 0.34.2, `Application.version` 0.34.1, Steam build
  24724372. Free Flight, Heartland, Maris Airport; relative throttle enabled;
  autoland disabled; roughly 100-115 FPS.
- Vanilla pass-through confirmed on CI-22 and EW-25 (no matching capability)
  and UH-90, SAH-46, and VL-49 (collective).
- Airbrake readiness confirmed on T/A-30, VT-7, A-19, and SFB-81. Both-detent
  readiness and stationary upper/lower control-path checks passed on FS-12,
  FS-20, KR-67, and AB-4. AB-4 matched all four expected nozzles.
- EW-25 held roughly 99-103 FPS over a ten-second cockpit check; an earlier
  10 FPS regression did not recur.
- The pass used a dwell shorter than the release default to prove boundary
  gating and release only. Not covered: running-engine afterburner flames,
  AB-4 four-engine synchronization, physical surface movement on T/A-30 and
  SFB-81, and A-19 closure.
- Configuration Manager UI QA (v0.1): plain labels with units, a read-only
  status row without a Reset button, and brief tooltips.

### Final v0.1.0 confirmation - 2026-08-19

- Release DLL SHA-256
  `E7AFD87AC3224672906540A50147C74B248D90BFD239F79F5138ABDA57121E6C` passed a
  manual flight check with no per-airframe record.
- The published package ships DLL SHA-256
  `803297E94829CC580E5240DB6CE1BF0AFD5DAC36C784869882D6C8A8EB4E209C`, rebuilt
  from the flight-checked DLL with corrected author, plugin GUID, Harmony ID,
  and config namespace. Detent logic did not change; the published DLL was not
  re-flown. Repeat the affected aircraft checks on the exact release DLL.

## Checklist

### Baseline

- [ ] Confirm the Release package's default config has `DebugLogging = false`.
- [ ] Launch with a clean extraction of the standalone ZIP. Expected: Nuclear Option reaches the main menu.
- [ ] Inspect `BepInEx\LogOutput.log`. Expected: one `Nuclear Option Detents 0.1.0 loaded.` line.
- [ ] Inspect the same log. Expected: one aggregate load line lists the throttle observer, Airbrake gate, split-airbrake gate, and afterburner gate statuses. An unavailable target is reported as `unavailable` and leaves that vanilla path unchanged; this is not a red Harmony error.
- [ ] Fly at midrange throttle. Expected: normal throttle response is unchanged.
- [ ] Exercise pitch, roll, yaw, wheel brakes, weapons, and Custom Axis 1. Expected: no change attributable to this mod.

### Upper detent

- [ ] Start near 50% and hold increase until the aircraft's captured full-dry/afterburner boundary. Expected: full dry thrust, with no immediate afterburner.
- [ ] Release increase less than 200 ms after reaching the boundary. Expected: afterburner remains off and the attempt resets.
- [ ] Press and hold increase again at the boundary for 200 ms. Expected: afterburner becomes available at the dwell boundary.
- [ ] Release increase while remaining at the boundary. Expected: afterburner remains available because the latch stays unlocked.
- [ ] Reduce throttle. Expected: afterburner stops normally and the upper detent relocks below the dry-thrust boundary.
- [ ] Return to the full-dry boundary without the extra endpoint hold. Expected: afterburner remains off.

### Lower detent

- [ ] Spawn with throttle already at 0%. Expected: the lower detent starts locked; hold decrease for the configured dwell before the automatic airbrake becomes available.
- [ ] Start near 50% and hold decrease until the display reaches 0%. Expected: genuine zero engine throttle, with no immediate automatic airbrake.
- [ ] While the lower detent is locked, release decrease before the configured dwell, then press increase. Expected: the timer resets and the interruption passes vanilla while throttle responds above idle.
- [ ] Release decrease less than 200 ms after reaching 0%. Expected: airbrake remains inhibited and the attempt resets.
- [ ] Press and hold decrease again at 0% for 200 ms. Expected: the automatic airbrake begins its vanilla opening behavior.
- [ ] Release decrease while remaining at 0%. Expected: the airbrake remains available because the latch stays unlocked.
- [ ] Increase throttle. Expected: the airbrake closes normally and the lower detent relocks.
- [ ] Return to 0% without the extra endpoint hold. Expected: the automatic airbrake remains inhibited.

### AB-4 four-engine case

- [ ] Spawn the Alkyon AB-4 at Maris with both holds set to 200 ms. Expected: status reports both detents after the four matching nozzles and split-airbrake surfaces have run.
- [ ] Use Numpad Minus to stop the engines, then test the lower detent without moving. Expected: the split airbrakes stay closed until decrease is held for the full dwell, then open together.
- [ ] Restart all four engines and test the upper detent. Expected: none enters afterburner before the full dwell; all four enter afterburner together after it.
- [ ] Release increase before 200 ms, then retry. Expected: the first attempt resets and no engine enters afterburner early.
- [ ] Reduce throttle below the dry-thrust boundary. Expected: all four leave afterburner and the upper detent relocks.

### Configuration

- [ ] With BepInEx Configuration Manager installed, open the mod panel. Expected: the labels use plain names with units, the status is read-only, and the status row has no Reset button.
- [ ] Hover over each control and the status row. Expected: each tooltip is brief and contains only the setting's purpose, units, or essential consequence.
- [ ] Enter each selectable aircraft and compare its observed systems with [AIRFRAME-PRESETS.md](AIRFRAME-PRESETS.md). Expected: known IDs use only their pinned features; unknown or new IDs stay vanilla. This confirms identity and capability presence, not physical detent behavior.
- [ ] Turn off `Use Throttle Relative Axis` in Nuclear Option. Expected: relative-only behavior is bypassed; throttle, airbrake, and afterburner remain vanilla.
- [ ] Set both hold times to 500 ms and relaunch. Expected: both endpoint delays are clearly longer.
- [ ] Disable only the idle detent. Expected: vanilla zero-throttle airbrake behavior and a working upper detent.
- [ ] Disable only the afterburner detent. Expected: vanilla afterburner behavior and a working lower detent.
- [ ] Disable the General master switch. Expected: full vanilla throttle, airbrake, and afterburner behavior.
- [ ] Toggle only `DebugLogging` during an unfinished hold and again after an unlocked latch. Expected: elapsed dwell and unlocked state are preserved.
- [ ] Set each duration to zero. Expected: its endpoint changeover is immediate on an endpoint-plus-command frame.
- [ ] Enter malformed and out-of-range values, then relaunch. Expected: no startup crash; BepInEx uses a valid value or the plugin clamps it.

### Lifecycle and isolation

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
