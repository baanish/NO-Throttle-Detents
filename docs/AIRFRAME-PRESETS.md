# Airframe presets

The mod uses an explicit allowlist keyed by the installed build's `UnitDefinition.jsonKey`.
It recognizes the 20 IDs below; 15 of them have an airbrake or afterburner capability, so
detents activate only on those.
The preset is selected only after the local `Unit.definition` and its `jsonKey` are resolved.
Unknown, missing, or newly released IDs stay vanilla unless the player enables
their detected-aircraft profile.

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

## Optional aircraft mods

| Add-on | Version inspected | `jsonKey` | Airframe | Airbrake | Afterburner |
| --- | --- | --- | --- | --- | --- |
| Aryx MC260 Chimera | 1.1.9 | `Aryx_CargoPlane1` | MC-260 Chimera | split | no |
| Aryx F-16M | 1.2.3 | `Aryx_F16M_KingViper` | F-16M King Viper | Airbrake component | yes (`0.900000..1.000000`, 1 nozzle) |
| Aryx F-22E | 1.0.0 | `Aryx_F22E_StrikeRaptor` | F-22E Strike Raptor | Airbrake component | yes (`0.950000..1.000000`, 2 nozzles) |
| Aryx F-99 Shrike | 1.1.2 | `Aryx_LightFighter1` | F-99 Shrike | Airbrake component | yes (`0.900000..1.000000`, 2 nozzles) |
| Aryx FS-41 Eclipse | 1.1.6 | `Aryx_Interceptor1` | FS-41 Eclipse | Airbrake component | yes (`0.900000..1.000000`, 2 nozzles) |
| Aryx OA-27 Cavalier | 1.0.0 | `Aryx_PropAttacker1` | OA-27 Cavalier | split | no |
| FS-3 Ternion | 1.0.1 | `P_Trisurface1` | FS-3 Ternion | split | yes (`0.900000..1.000000`, 2 nozzles) |

These add-on values come from the serialized `AircraftDefinition.jsonKey`,
local `Airbrake`, positive `ControlSurface.maxSplit`, and
`JetNozzle.afterburners` data in the installed Blueprinter aircraft bundles.
Runtime still confirms the expected components below the selected local
aircraft before enabling either detent.

The F-22E 1.0.0 bundle contains eight `Airbrake` components owned by its
aircraft, no positive `ControlSurface.maxSplit`, and two `JetNozzle` components
with one afterburner stage each at `0.949999988079071..1.0`.
Its `takeoffDistance=350` selects non-collective controls in the inspected game.
The preset validates those nozzle values but deliberately places the upper
detent at the HUD's MIL limit, `0.90`. Holding just below `0.95` instead produces
an `AFTERBURNER 50%` indication. At the MIL stop, public throttle is
`0.8999`, inside the HUD's `0.88..0.90` MIL region.

F-22E inspection used Mono.Cecil to extract the embedded `.nobp` resource from
`Aryx_F22E_StrikeRaptor_1.0.0.dll`, then UnityPy 1.21.3 to read its serialized
components and Blueprinter patch manifest. The DLL SHA-256 was
`A0DF4979C2183EF7A4AE8D6A9DC7CDE3502E52801B1A3C601A99A38FB88E612A`.
The live flight log on Steam build 24724372 confirmed the exact aircraft ID,
eight owned airbrakes, and both matching nozzles. A user flight check confirmed
the MIL stop in Detents 0.4.3 on Nuclear Option 0.34.2, Steam build 24724372.

The MC-260's idle detent applies to its split airbrake. Its separate thrust
reverser remains under the aircraft mod's controls: hold Brake, apply more than
25% throttle, keep the gear down below 50 m radar altitude, and begin above
3 m/s. Nuclear Option Detents does not patch the reverser; a live ground-roll
check confirmed both can remain enabled together.

The base-game table values came from live aircraft loaded into a mission:
`UnitDefinition.jsonKey`, collective mode, local `Airbrake` components,
`ControlSurface.maxSplit` for split airbrakes, and the afterburner range on
each local `JetNozzle`.

A preset pins the airbrake path (component or split), afterburner range, and
nozzle count. It can specify an earlier upper detent for the aircraft's MIL
limit without relaxing nozzle validation. The runtime confirms capabilities
from components below the selected local aircraft. It does not pin a split-surface name or `maxSplit`
value; the parenthetical `maxSplit` figures are capture notes. A missing, extra,
unreadable, or mismatched nozzle leaves the afterburner detent vanilla, and the
AB-4 needs all four matching nozzles. The captured ranges describe the full-dry
to afterburner boundary, not a user-visible 100% throttle value. The table
documents aircraft capabilities, not physical detent behavior.

Allowlist a built-in capability only after live confirmation. Runtime discovery
cannot enable a preset marked `no` or a collective aircraft. Each aircraft in
the installed aircraft catalog gets an independent custom profile keyed by its
exact ID; seat entry remains a fallback for definitions absent from that list.
Endpoint capabilities still require matching live components.
