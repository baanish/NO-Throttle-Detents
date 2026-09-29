# Installed-build compatibility snapshot

The game methods and fields the mod patches or reads, as inspected in the
installed build below. This is inspection evidence, not a runtime version
gate or a promise for later builds. After a game update, recheck these
targets, run `pwsh ./build/Build.ps1`, and flight-check the mod.

| Item | Value |
| --- | --- |
| Nuclear Option version | Early Access 0.34.2 in the main menu, `Application.version` 0.34.1 |
| Steam build ID | 24724372 |
| Unity version | 2022.3.62f2 |
| `Assembly-CSharp.dll` SHA-256 | `EB3B93BDAEC37DD7B3BAB72F801A2C84E5BE2AE3C559F39251E2320AE6B11CCC` |
| `Assembly-CSharp.dll` MVID | `6fda19c0-8ef5-445d-835f-84e9933959ab` |

## Patched methods

- Throttle observer: `System.Void PilotPlayerState::PlayerThrottleAxis1Controls()`
- Skipped-throttle interruption observer: `System.Void PilotPlayerState::PlayerControls()`
- Pilot-state reset: `System.Void PilotPlayerState::LeaveState()`

## Fields written

- Throttle field: `System.Single ControlInputs::throttle`
- Pilot simulated-throttle field: `System.Single PilotPlayerState::simulatedThrottle`

## Fields and routes read

- Local aircraft route: `Pilot PilotBaseState::pilot` -> `Aircraft Pilot::aircraft`
- Pilot Rewired field: `Rewired.Player PilotPlayerState::player`
- Throttle input device: `IList<InputActionSourceData> Rewired.Player::GetCurrentInputSources(string)` -> `controllerType`, `actionElementMap.elementType`
- Pilot control input field: `ControlInputs PilotBaseState::controlInputs`
- Pilot collective field: `System.Boolean PilotPlayerState::collective`
- Pilot control-strength field: `System.Single PilotPlayerState::pilotStrength`
- Local-aircraft check: `System.Boolean GameManager::IsLocalAircraft(Aircraft)`
- Auto Hover check: `System.Boolean Aircraft::IsAutoHoverEnabled()`
- Airframe identity route: `UnitDefinition Unit::definition` -> `System.String UnitDefinition::jsonKey` / `System.String UnitDefinition::unitName`
- Airbrake owner fields: `Aircraft Airbrake::aircraft`, `Aircraft Airbrake::attachedAircraft`
- ControlSurface owner field: `Aircraft ControlSurface::aircraft`
- ControlSurface max-split field: `System.Single ControlSurface::maxSplit`
- JetNozzle owner field: `Aircraft JetNozzle::aircraft`
- JetNozzle afterburners field: `JetNozzle/Afterburner[] JetNozzle::afterburners`
- Afterburner throttle range fields: `System.Single JetNozzle/Afterburner::throttleStart`, `System.Single JetNozzle/Afterburner::throttleEnd`
- GameManager flight-controls field: `System.Boolean GameManager::flightControlsEnabled`
- PlayerSettings relative-throttle field: `System.Boolean PlayerSettings::throttleUseRelative`
- PlayerSettings invert-collective field: `System.Boolean PlayerSettings::invertCollective`
- PlayerSettings throttle-negative field: `System.Boolean PlayerSettings::throttleUseNegative`
- Flight HUD center: `UnityEngine.Transform FlightHud::GetHUDCenter()`
- Flight HUD throttle label: `TMPro.TextMeshProUGUI ThrottleGauge::throttleLabel`
- Flight HUD percentage regions: `ThrottleGauge/ThrottleRegion[] ThrottleGauge::throttleRegions` -> `showPercent`, `start`, and `end`

Network Validation also reads, only while both diagnostic switches are on:

- Owner route: `NuclearOption.Networking.Player Aircraft::Player`, `System.Int32 Player::PlayerIndex`, `Aircraft Player::Aircraft`, and the inherited `System.Boolean Mirage.NetworkBehaviour::IsLocalPlayer`
- Aircraft input field: `ControlInputs Aircraft::controlInputs`
- Sampled state: `Airbrake.active`, `Airbrake.openAmount`, `ControlSurface.splitAmount`, and `JetNozzle/Afterburner.afterburnerAmount`

## Throttle mapping

With `throttleUseNegative = false`, the public throttle equals
`simulatedThrottle`. With `throttleUseNegative = true`, the public throttle is
`0.5 * (simulatedThrottle + 1)`, and the inverse is `publicThrottle * 2 - 1`.

## Deviations

- `PilotPlayerState.EnterState(Pilot)` assigns `PilotBaseState.pilot` and
  reads `Pilot.aircraft`, but does not assign the inherited
  `PilotBaseState.aircraft`. The local-aircraft route is therefore
  `PilotBaseState.pilot` -> `Pilot.aircraft`.

## Recognized throttle replacement

The mod treats one foreign patch on `PlayerThrottleAxis1Controls()` as
compatible: the prefix `PRF.Fixes.ThrottleRelativeVelocity.ThrottleAxis1ControlsReplacer`
from PauelsRandomFixes. [DESIGN.md](DESIGN.md#other-throttle-mods-take-priority)
describes how other patches are handled.
