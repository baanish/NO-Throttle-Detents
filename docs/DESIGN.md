# How the detents work

The mod adds a Harmony postfix to the local pilot's throttle method,
`PilotPlayerState.PlayerThrottleAxis1Controls()`. After the game updates the
throttle each frame, `RuntimeController.ObserveThrottle` decides whether a
detent applies. If one does, it parks the public throttle just inside the
boundary and writes the matching value into the game's private
`simulatedThrottle` accumulator, so input cannot build up behind the stop.
When the hold completes, the mod stops writing and the game's throttle flow
resumes.

Two smaller patches support that one. A postfix on `PlayerControls()` cancels
a pending hold on frames where the throttle method did not run, and a prefix
on `LeaveState()` resets everything when the pilot leaves the seat.

## A detent needs the local pilot, a matched aircraft, and a key or button throttle

The runtime writes nothing unless all of these hold:

- `GameManager.IsLocalAircraft` accepts the aircraft.
- The aircraft's `UnitDefinition.jsonKey` has a built-in preset in
  `AirframePresetCatalog`, or the player enabled a custom profile for that
  exact ID.
- Live components confirm the capability the detent protects (see
  [capability discovery](#capability-discovery-confirms-parts-at-seat-entry)).
- The last input on the Throttle action came from a key or button (Rewired
  `GetCurrentInputSources`). The game ramps digital input the same way whether
  or not Use Throttle Relative Axis is on, so that setting does not matter.
  Analog throttle axes are not supported: in absolute mode the game sets the
  throttle straight from the lever, and pinning it would fight the lever.
- The aircraft is not collective.
- Auto Hover is off, and no other mod is publishing the throttle.

Every other path is a pass-through. AI, remote aircraft, missiles, spectators,
networking, damage, weapons, and aircraft definitions stay vanilla. While Auto
Hover is on, the observer tracks the accumulator without changing it, and
turning Auto Hover off starts both detents locked.

## Each endpoint detent is a three-state machine

`EndpointDetent` moves between `Locked`, `Holding`, and `Unlocked`. A hold
starts when the throttle is at the boundary and the player commands past it.
Any throttle input that moves the throttle counts as a command: an axis
magnitude above 0.1, the game's own deadzone.

The command must stay active for the configured dwell. The dwell accumulates
elapsed simulation time, not frames. Release, an opposite command, Axis
Modifier, disabled flight controls, pause, pilot strength below `0.2`, or a
lost input reference cancels an unfinished hold.

Once unlocked, the detent lets the throttle pass until it moves
`ResetHysteresis` back from the boundary, which locks it again. Scene,
aircraft, and mode changes reset both detents. The state machine holds no
game references, so `tests/NuclearOptionDetents.Tests` exercises it without
the game.

## The parked throttle sits 0.0001 inside the boundary

`ThrottleBoundaryHold` parks the throttle `0.0001` above idle or `0.0001`
below the preset's upper boundary. The offset survives the game's
half-precision network throttle value and stays inside the vanilla
changeover, so the aircraft's own airbrake, split-surface, engine, and
afterburner code sees an ordinary throttle value. Neutral input keeps an
already parked detent in place but cannot start a new hold.

The upper boundary is normally the captured afterburner start. A preset can
place it earlier: the F-22E stops at its HUD MIL limit, `0.90`, while its
nozzles still have to match `0.95..1.0`. A confirmed live nozzle start below
the preset boundary lowers the stop.

The mod never writes an afterburner decision. Holding the throttle below the
boundary is the only way it delays one.

## Custom detents follow the throttle gauge's percentages

Custom detents use the percentage range the aircraft's `ThrottleGauge`
displays, so the hold label and the gauge agree. They stop crossings in either
direction and track release state per position, so nearby positions cannot
mask one another. Each profile matches one exact `UnitDefinition.jsonKey`, and
a malformed list disables that aircraft's custom detents.

A built-in preset always wins for the endpoint detents. A profile on a
built-in aircraft adds its custom detents on top.

## Capability discovery confirms parts at seat entry

When the local player enters an aircraft, the runtime scans `Airbrake`,
`ControlSurface`, and `JetNozzle` components and keeps only those whose owner
field points at that aircraft. A preset and the live components must agree:

- A component-airbrake preset needs at least one owned `Airbrake`.
- A split-airbrake preset needs an owned `ControlSurface` with positive
  `maxSplit`.
- An afterburner preset needs exactly its nozzle count, with every nozzle's
  afterburner range matching the preset. The AB-4 therefore needs all four.

If an expected part is still missing, the runtime rescans: up to 12 scans
0.25 s apart, about three seconds. A capability still unconfirmed after that
stays vanilla for the rest of the seat.

## Sensitivity rescales the game's own step

The multiplier recomputes the game's keyboard throttle step with a scaled
`deltaTime` before the detents see the value. It runs only on aircraft with a
live detent, endpoint or custom. It first checks that the accumulator moved
by the step vanilla should have taken, and leaves the frame alone if not.
`1x` changes nothing.

## Other throttle mods take priority

The throttle postfix runs at Harmony `Priority.Last`, so it sees the final
value any other patch produced. It reads Harmony's patch list for the
throttle method once per seat entry, not every frame.

- PauelsRandomFixes' `ThrottleRelativeVelocity` prefix replaces the game's
  step but keeps its signed accumulator, so the detents keep working and the
  multiplier defers to that mod's sensitivity.
- Any other prefix that skips the original turns the detents off, because its
  accumulator mapping is unknown.
- Any other patch that publishes a throttle value different from the
  accumulator makes detents and sensitivity yield until the values match
  again.

## The HUD indicator mirrors the hold

The indicator clones the game's throttle-label style below the flight HUD's
throttle gauge. It shows only while a hold parks the throttle, and disappears
on release, bypass, or reset. A render failure disables the indicator and
leaves the detents running.

## Runtime cost stays on one aircraft

Each frame does constant work on the local pilot's throttle and never iterates
over the aircraft in a mission. Component scans run only at seat entry and
during the bounded rescan above. Network Validation, when enabled, samples the
local aircraft and one selected remote at 10 Hz
([NETWORK-VALIDATION.md](NETWORK-VALIDATION.md)).

## Failures leave the game vanilla

The patch installer resolves the methods and fields listed in
[COMPATIBILITY.md](COMPATIBILITY.md). If one is missing, the affected feature
reports unavailable and its vanilla path runs unchanged. The load line in
`BepInEx\LogOutput.log` states the throttle patch status. A runtime exception
resets the detent state and is logged once per operation.

Debug logging records aircraft attachment, capability scans, lifecycle resets,
and changes to the throttle input device. Network Validation records
session player indexes, not player names or platform IDs. The plugin has no
telemetry, network access, or self-update. BepInEx writes the config and log.

To add or change an airframe, follow
[AIRFRAME-PRESETS.md](AIRFRAME-PRESETS.md).
