# Installed-build compatibility snapshot

Contributor reference captured from the installed game during v0.1
development: the patch points and fields the mod touches, as they existed in
that build. It is not generated build evidence or a guarantee for later game
updates. After a game update, recheck these targets and run the mod.

- Snapshot result for this captured build: compatible
- Nuclear Option version: Early Access 0.34.2 in the main menu; `Application.version` 0.34.1
- Steam build ID: 24724372
- Unity version: 2022.3.62f2
- `Assembly-CSharp.dll` SHA-256: `EB3B93BDAEC37DD7B3BAB72F801A2C84E5BE2AE3C559F39251E2320AE6B11CCC`
- `Assembly-CSharp.dll` MVID: `6fda19c0-8ef5-445d-835f-84e9933959ab`

## Patch targets

- Throttle observer: `System.Void PilotPlayerState::PlayerThrottleAxis1Controls()`
- Skipped-throttle interruption observer: `System.Void PilotPlayerState::PlayerControls()`
- Airbrake gate: `System.Void Airbrake::Update()`
- Split-airbrake gate: `System.Void ControlSurface::UpdateJobFields()`
- Afterburner gate: `System.Void JetNozzle::Thrust(System.Single thrustAmount, System.Single rpmRatio, System.Single thrustRatio, System.Single throttle, System.Boolean allowAfterburner)`
- Pilot-state reset: `System.Void PilotPlayerState::LeaveState()`
- Aircraft input field: `ControlInputs Aircraft::controlInputs`
- Throttle field: `System.Single ControlInputs::throttle`
- Local aircraft route: `Pilot PilotBaseState::pilot` -> `Aircraft Pilot::aircraft`

## Fields and routes read

- Pilot Rewired field: `Rewired.Player PilotPlayerState::player`
- Pilot control input field: `ControlInputs PilotBaseState::controlInputs`
- Pilot aircraft field: `Aircraft Pilot::aircraft`
- Pilot collective field: `System.Boolean PilotPlayerState::collective`
- Pilot control-strength field: `System.Single PilotPlayerState::pilotStrength`
- Pilot simulated-throttle field: `System.Single PilotPlayerState::simulatedThrottle`
- Airframe identity route: `UnitDefinition Unit::definition` -> `System.String UnitDefinition::jsonKey` / `System.String UnitDefinition::unitName`
- Airbrake control input field: `ControlInputs Airbrake::controlInputs`
- Airbrake aircraft fields: `Aircraft Airbrake::aircraft`, `Aircraft Airbrake::attachedAircraft`
- JetNozzle aircraft field: `Aircraft JetNozzle::aircraft`
- JetNozzle afterburners field: `JetNozzle/Afterburner[] JetNozzle::afterburners`
- Afterburner throttle range fields: `System.Single JetNozzle/Afterburner::throttleStart`, `System.Single JetNozzle/Afterburner::throttleEnd`
- GameManager flight-controls field: `System.Boolean GameManager::flightControlsEnabled`
- PlayerSettings relative-throttle field: `System.Boolean PlayerSettings::throttleUseRelative`
- PlayerSettings invert-collective field: `System.Boolean PlayerSettings::invertCollective`
- PlayerSettings throttle-negative field: `System.Boolean PlayerSettings::throttleUseNegative`
- ControlSurface aircraft field: `Aircraft ControlSurface::aircraft`
- ControlSurface control-input field: `ControlInputs ControlSurface::controlInputs`
- ControlSurface max-split field: `System.Single ControlSurface::maxSplit`
- ControlSurface job fields control-input field: `NuclearOption.Jobs.ControlInputsBurst NuclearOption.Jobs.ControlSurfaceFields::controlInputs`
- ControlSurface job fields max-split field: `System.Single NuclearOption.Jobs.ControlSurfaceFields::maxSplit`
- ControlSurface job execute: `System.Void NuclearOption.Jobs.ControlSurfaceJob_Math::Execute(NuclearOption.Jobs.ControlSurfaceFields& fields, UnityEngine.Quaternion& mainRotation, System.Int32 upperIndex, System.Int32 lowerIndex)`

## Throttle mapping

The build maps the private simulated throttle to the public throttle as
follows. With `throttleUseNegative = false`, the public value equals
`simulatedThrottle`. With `throttleUseNegative = true`, the public value is
`0.5 * (simulatedThrottle + 1)`, so the inverse is
`publicThrottle * 2 - 1`.

## Deviations

- `PilotPlayerState.EnterState(Pilot)` assigns `PilotBaseState.pilot` and
  reads `Pilot.aircraft`, but does not assign the inherited
  `PilotBaseState.aircraft`. The local-aircraft route is therefore
  `PilotBaseState.pilot` -> `Pilot.aircraft`.
