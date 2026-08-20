# Airframe presets

The mod uses an explicit allowlist keyed by the installed build's `UnitDefinition.jsonKey`.
The preset is selected only after the local `Unit.definition` and its `jsonKey` are resolved.
Unknown, missing, or newly released IDs stay completely vanilla until a reviewed preset is added.

| `jsonKey` | Airframe | Collective | Airbrake | Afterburner |
| --- | --- | ---: | ---: | ---: |
| `COIN` | CI-22 Cricket | no | no | no |
| `VTOLTrainer1` | VT-7 Vagrant | no | Airbrake component | no |
| `UtilityHelo1` | UH-90 Ibis | yes | no | no |
| `AttackHelo1` | SAH-46 Chicane | yes | no | no |
| `CAS1` | A-19 Brawler | no | split (capture `maxSplit=45`) | no |
| `Fighter1` | FS-12 Revoker | no | Airbrake component | yes (`0.900000..1.000000`, 1 nozzle) |
| `SmallFighter1` | FS-20 Vortex | no | Airbrake component | yes (`0.900000..1.000000`, 1 nozzle) |
| `QuadVTOL1` | VL-49 Tarantula | yes | no | no |
| `Multirole1` | KR-67 Ifrit | no | split (capture `maxSplit=25`) | yes (`0.900000..1.000000`, 2 nozzles) |
| `EW1` | EW-25 Medusa | no | no | no |
| `Darkreach` | SFB-81 Darkreach | no | split (capture `maxSplit=30`) | no (2 nozzles) |
| `FastBomber1` | Alkyon AB-4 | no | split (capture `maxSplit=60`) | yes (`0.900000..1.000000`, 4 nozzles) |
| `trainer` | T/A-30 Compass | no | Airbrake component | no |

The table values came from live aircraft instances loaded into a mission. The
capture read `UnitDefinition.jsonKey`, collective mode, local `Airbrake`
components, `ControlSurface.maxSplit` for split airbrakes, and the afterburner
range on each local `JetNozzle`. The preset pins the captured airbrake path
(component or split) and afterburner range/nozzle count; it does not pin an
exact split-surface name or `maxSplit` value. The parenthetical `maxSplit`
values are capture notes only. A missing, extra, unreadable, or mismatched
nozzle leaves the afterburner detent vanilla. AB-4 is intentionally pinned to
four matching nozzles; one missing or mismatched engine leaves its upper detent
vanilla. The captured afterburner ranges describe the aircraft's full-dry to
afterburner boundary, not a user-visible 100% throttle value. These values
document aircraft capabilities, not physical detent behavior.

An expected capability is gated only after live confirmation. A preset marked
`no`, a collective aircraft, or an unknown/new ID cannot be enabled by runtime
discovery. The hidden UFO asset was not selectable or captured and is not
allowlisted.
