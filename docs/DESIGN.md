# Design

## Scope

Nuclear Option Detents observes the local player's existing relative-throttle
input after vanilla processing. While a detent is locked or holding, the
runtime parks the public throttle just inside the changeover: a tiny positive
value above idle or just below the preset full-dry boundary. It also keeps the
game's private simulated-throttle value at that parked point so input cannot
build up behind the stop. Once the dwell unlocks, vanilla throttle flow
resumes.

Absolute/HOTAS mode and collective aircraft are pass-through paths. Unknown
aircraft and capabilities marked absent in the explicit preset table are also
vanilla. The throttle observer requires the game to identify the selected
aircraft as local before it writes anything. AI, remote aircraft, missiles,
spectators, networking, damage, weapons, and aircraft definitions remain
vanilla.

## Detent state

Each endpoint has `Locked`, `Holding`, and `Unlocked` states. A hold starts
only when the real endpoint and a command past that endpoint are observed
together. The command must stay active for the configured dwell. Release, Axis
Modifier, disabled flight controls, pause, pilot strength below `0.2`, an
opposite command, or a lost input reference cancels an unfinished hold and
leaves the affected path vanilla. Moving away by the configured hysteresis
relocks an unlocked detent. Scene, aircraft, and mode changes reset both
detents.

Elapsed simulation time controls unlocking, so the result is based on time
rather than a fixed number of frames. The state machine is intentionally kept
separate from game reflection so it can be tested and changed without a live
game.

## Sensitivity and indicator

The sensitivity multiplier scales the game's relative-throttle step before the
state machine sees it. `1x` preserves the observed vanilla value. The feature
requires a supported preset and at least one confirmed live detent capability,
independent of whether an individual detent is disabled. It yields rate control
to PauelsRandomFixes when its verified relative-throttle prefix replaces vanilla
integration. Unknown throttle replacers remain vanilla because their private
accumulator mapping is not known.

The in-game indicator is derived from the canonical boundary-hold result. It
clones the native throttle-label style below the flight HUD's throttle gauge.
It is visible only while the hold parks throttle at a locked or holding
boundary, and disappears on release, bypass, or lifecycle reset.

Auto Hover bypasses detents and sensitivity. The same local throttle observer
runs from the game's fixed-update path while Auto Hover is active, so the
runtime tracks the live accumulator without changing it. Turning Auto Hover
off starts both detents from a fresh locked state.

Foreign Harmony patches on the same throttle method are treated as possible
output owners. If one publishes a throttle value that no longer matches the
game's relative accumulator, detents and sensitivity yield until the values
match again. Neutral input maintains an already parked detent but cannot start
a new boundary hold. The mod reads Harmony ownership once per seat entry, so a
mod that patches or unpatches between flights is detected without a per-frame
patch-table lookup.

## Runtime cost

The fixed-update path performs constant work on the local pilot's throttle. It
does not iterate over the aircraft in a mission. Capability discovery scans
for components when the local player enters an aircraft, with three bounded
retries if an expected component has not loaded yet.

Network Validation is separate from normal operation. When enabled, it tracks
the local aircraft and one selected remote aircraft. Component references stay
cached until either aircraft identity changes.

## Boundary output and capability checks

The local throttle output is parked at `0.0001` above idle or `0.0001` below
the captured afterburner boundary. The offset stays representable through the
game's half-precision network value while remaining inside the vanilla
changeover. The shared throttle value then reaches the aircraft's normal
airbrake, split-surface, engine, and afterburner code.

When the local player enters an aircraft, the runtime matches live airbrakes,
split surfaces, and nozzles through their aircraft-owner fields. It retries
the read-only scan for up to three seconds while an expected capability is
still missing. A preset and the live components must agree before the related
detent can run. AB-4 remains upper-detent eligible only when all four expected
nozzles match its preset. The mod never writes an afterburner decision.

## Compatibility and failure behavior

The patch installer resolves the small set of methods and fields needed by the
current game build. If a target or field is unavailable, that feature reports
unavailable and leaves its vanilla path unchanged. The installed-build
snapshot in [COMPATIBILITY.md](COMPATIBILITY.md) records the inspected patch
points; it is not a runtime version gate.

Debug logging is off by default. When enabled, it records aircraft attachment
and lifecycle resets. Network Validation is a second opt-in switch. With both
switches enabled, it refreshes the human-owner roster once per second and
samples the local aircraft plus one configured remote at 10 Hz. Component
references are resolved once per selected aircraft identity. The records
contain session player indexes, not player names or platform IDs. Patch and
runtime failures are logged once per operation. The plugin performs no
telemetry, network access, self-update, or arbitrary file writes. BepInEx owns
config and log writes.

## Extension points

Presets are keyed by `UnitDefinition.jsonKey`. Add or change an airframe only
after recording the installed component shape in [AIRFRAME-PRESETS.md](AIRFRAME-PRESETS.md)
and adding focused tests. Keep new behavior local and easy to remove.
