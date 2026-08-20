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
vanilla. Harmony hooks run process-wide; each gate then requires an exact
local-aircraft identity match before it can change behavior. AI, remote
aircraft, missiles, spectators, networking, damage, weapons, and aircraft
definitions remain vanilla.

## Detent state

Each endpoint has `Locked`, `Holding`, and `Unlocked` states. A hold starts
only when the real endpoint and a command past that endpoint are observed
together. The command must stay active for the configured dwell. Release, Axis
Modifier, disabled flight controls, pause, pilot strength below `0.2`, an
opposite command, or a lost input reference cancels an unfinished hold and
leaves the affected path vanilla. Moving away by the configured hysteresis
relocks an unlocked detent. Scene, aircraft, and mode changes reset both
detents.

Component gates also pass vanilla when their throttle observation is older than
`0.1` seconds. This freshness guard is independent of the simulation-time
dwell clock.

Elapsed simulation time controls unlocking, so the result is based on time
rather than a fixed number of frames. The state machine is intentionally kept
separate from game reflection so it can be tested and changed without a live
game.

## Component gates

The idle gate temporarily avoids the exact-zero input that opens a component
airbrake. A separate path covers split airbrakes. Each preset pins which path
the aircraft uses, but not an exact surface name or `maxSplit` value. Both
paths restore the original input after vanilla code runs.

The afterburner gate runs only for an allowlisted local aircraft and only when
vanilla already requested afterburner. It can suppress that existing `true`
request while the upper detent is locked; it never turns afterburner on.
AB-4 remains upper-detent eligible only when all four expected nozzles match
its preset.

## Compatibility and failure behavior

The patch installer resolves the small set of methods and fields needed by the
current game build. If a target or field is unavailable, that feature reports
unavailable and leaves its vanilla path unchanged. The installed
build snapshot in [COMPATIBILITY.md](COMPATIBILITY.md) is inspection reference
for contributors, not a runtime version gate or a claim of future compatibility.

Debug logging is off by default. When enabled, it records aircraft attachment
and lifecycle resets. Patch and runtime failures are logged once per operation.
The plugin performs no telemetry, network access, self-update, or arbitrary
file writes; BepInEx owns config and log writes.

## Extension points

Presets are keyed by `UnitDefinition.jsonKey`. Add or change an airframe only
after recording the installed component shape in [AIRFRAME-PRESETS.md](AIRFRAME-PRESETS.md)
and adding focused tests. Keep new behavior local and easy to remove: v0.1 is
a starting point for future features, simplification, or replacement.
