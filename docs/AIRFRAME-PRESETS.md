# Airframe presets

`AirframePresetCatalog` in `src/NuclearOptionDetents/Core/AirframePreset.cs`
holds 21 presets keyed by `UnitDefinition.jsonKey`, matched without regard to
case. 16 of them are non-collective with an airbrake or afterburner and get
detents. The 3 collective aircraft and the 2 with neither system stay vanilla.
An ID with no preset stays vanilla unless the player enables a custom profile
for it.

A preset pins the airbrake path (an `Airbrake` component or a split
`ControlSurface`), the afterburner nozzle count and range, and optionally an
earlier upper detent. It does not pin split-surface names or `maxSplit`
values. The `maxSplit` figures below are capture notes. Afterburner ranges are
the full-dry to afterburner boundary in public throttle, not a percentage the
HUD shows. At runtime, live components must still match the preset before a
detent runs ([DESIGN.md](DESIGN.md#capability-discovery-confirms-parts-at-seat-entry)).

## Base game

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

The values are a capability capture from live aircraft loaded into a mission:
`jsonKey`, collective mode, owned `Airbrake` components, `maxSplit` on split
airbrakes, and each owned `JetNozzle`'s afterburner range. The AB-4's upper
detent needs all four nozzles to match.

Manual checks in [CHANGELOG.md](../CHANGELOG.md): 0.1.0 checked identity and
readiness on all 13 and ran reduced-dwell upper and lower checks on the FS-12,
FS-20, KR-67, and AB-4. Live QA in 0.3.0 covered all 13 on Steam build
24724372, with the FS-20 and KR-67 exercising both detents.

## Add-on aircraft

| Add-on | Version inspected | `jsonKey` | Airframe | Airbrake | Afterburner |
| --- | --- | --- | --- | --- | --- |
| Aryx MC260 Chimera | 1.1.9 | `Aryx_CargoPlane1` | MC-260 Chimera | split | no |
| Aryx F-16M | 1.2.3 | `Aryx_F16M_KingViper` | F-16M King Viper | Airbrake component | yes (`0.900000..1.000000`, 1 nozzle) |
| Aryx F-22E | 1.0.0 | `Aryx_F22E_StrikeRaptor` | F-22E Strike Raptor | Airbrake component | yes (`0.950000..1.000000`, 2 nozzles) |
| Aryx F-99 Shrike | 1.1.2 | `Aryx_LightFighter1` | F-99 Shrike | Airbrake component | yes (`0.900000..1.000000`, 2 nozzles) |
| Aryx FS-41 Eclipse | 1.1.6 | `Aryx_Interceptor1` | FS-41 Eclipse | Airbrake component | yes (`0.900000..1.000000`, 2 nozzles) |
| Aryx OA-27 Cavalier | 1.0.0 | `Aryx_PropAttacker1` | OA-27 Cavalier | split | no |
| FS-3 Ternion | 1.0.1 | `P_Trisurface1` | FS-3 Ternion | split | yes (`0.900000..1.000000`, 2 nozzles) |
| Phoenix1509 KR-33 | 1.0.1 | `1509_palafighter1` | KR-33 Agni | Airbrake component | yes (`0.900000..1.000000`, 1 nozzle) |

These values come from inspecting the serialized `AircraftDefinition.jsonKey`,
`Airbrake`, positive `ControlSurface.maxSplit`, and `JetNozzle.afterburners`
data in the installed Blueprinter aircraft bundles. The mod has no dependency
on Blueprinter or the aircraft mods.

The KR-33 1.0.1 capture used UnityPy 1.21.3 to inspect
`1509_PalaFighter1_1.0.1.nobp` (SHA-256
`D4BEF8D30FB0BB153DFB18C3D13F97237F0BA66E14F1DF39C6EEB8A155808092`).
It has one owned `Airbrake`, no positive `ControlSurface.maxSplit`, one
`JetNozzle` stage at `0.8999999761581421..1.0`, and `takeoffDistance=600`,
which selects non-collective controls in the inspected game.

Manual checks in [CHANGELOG.md](../CHANGELOG.md), all on Steam build 24724372:
0.4.0 loaded the MC-260, F-16M, F-99, FS-41, OA-27, and FS-3 and confirmed
their expected components in the live log. The OA-27 held a custom 67%
detent, and an MC-260 ground roll confirmed that its thrust reverser still
works. The reverser stays under the add-on's own Brake-plus-throttle controls,
and this mod does not patch it. 0.4.3 flight-checked the F-22E MIL stop.
0.5.1 play-tested the KR-33; its live log confirmed the component airbrake
and matching afterburner nozzle.

### F-22E stops at MIL, not at its nozzle start

The F-22E 1.0.0 bundle has eight owned `Airbrake` components, no positive
`ControlSurface.maxSplit`, and two `JetNozzle` components with one afterburner
stage each at `0.949999988079071..1.0`. Its `takeoffDistance=350` selects
non-collective controls in the inspected game.

Holding just below `0.95` shows `AFTERBURNER 50%` on the HUD, so the preset
places the upper detent at the HUD's MIL limit, `0.90`, and still requires
both nozzles to match `0.95..1.0`. At the stop, public throttle is `0.8999`,
inside the HUD's `0.88..0.90` MIL region.

Inspection used Mono.Cecil to extract the embedded `.nobp` resource from
`Aryx_F22E_StrikeRaptor_1.0.0.dll` (SHA-256
`A0DF4979C2183EF7A4AE8D6A9DC7CDE3502E52801B1A3C601A99A38FB88E612A`), then
UnityPy 1.21.3 to read its serialized components and Blueprinter patch
manifest. A live flight log confirmed the exact aircraft ID, eight owned
airbrakes, and both matching nozzles.

## Adding or changing a preset

1. Capture the aircraft's `jsonKey`, collective mode, owned `Airbrake`
   components, positive `ControlSurface.maxSplit` for a split airbrake, and
   each owned `JetNozzle`'s afterburner count and range, from the game or
   add-on bundle. With `DebugLogging` on, `Detents attached` reports the ID.
   `Capability scan` reports owned component counts only once a preset or an
   enabled exact-ID custom profile exists, and only for the airbrake path and
   afterburner it declares, so use it to confirm a capture, not to make one.
2. Add the preset to `AirframePresetCatalog` and a focused test in
   `tests/NuclearOptionDetents.Tests/Program.cs`.
3. Add a row above with its evidence, run `pwsh ./build/Build.ps1`, and
   flight-check each detent the preset enables. Record the check in the
   release's changelog entry.

Mark a capability `yes` only after live confirmation. Runtime discovery never
enables a capability the preset marks `no`, and never enables a collective
aircraft. Players can cover an aircraft without a preset through a custom
profile, which still requires matching live components.
