# Installed-build compatibility snapshot

This is a contributor reference captured from the installed game during v0.1
development. It lists the patch points and fields inspected for the build that
was available then. It is not generated build evidence, an exhaustive method
verification, or a guarantee for later game updates. Recheck the focused build
and run the mod after changing a game build.

- Snapshot result for this captured build: compatible
- Nuclear Option version: Early Access 0.34.2 in the main menu; `Application.version` 0.34.1
- Steam build ID: 24724372
- Unity version: 2022.3.62f2
- `Assembly-CSharp.dll` SHA-256: `EB3B93BDAEC37DD7B3BAB72F801A2C84E5BE2AE3C559F39251E2320AE6B11CCC`
- `Assembly-CSharp.dll` MVID: `6fda19c0-8ef5-445d-835f-84e9933959ab`

## Selected runtime targets

- Throttle observer: `System.Void PilotPlayerState::PlayerThrottleAxis1Controls()`
- Skipped-throttle interruption observer: `System.Void PilotPlayerState::PlayerControls()`
- Airbrake gate: `System.Void Airbrake::Update()`
- Split-airbrake gate: `System.Void ControlSurface::UpdateJobFields()`
- Afterburner gate: `System.Void JetNozzle::Thrust(System.Single thrustAmount, System.Single rpmRatio, System.Single thrustRatio, System.Single throttle, System.Boolean allowAfterburner)`
- Aircraft input field: `ControlInputs Aircraft::controlInputs`
- Throttle field: `System.Single ControlInputs::throttle`
- Pilot-state reset: `System.Void PilotPlayerState::LeaveState()`
- Local aircraft route: `Pilot PilotBaseState::pilot` -> `Aircraft Pilot::aircraft`

## Inspected routes and mechanics

- Pilot Rewired field: `Rewired.Player PilotPlayerState::player`
- Pilot control input field: `ControlInputs PilotBaseState::controlInputs`
- Pilot aircraft field: `Aircraft Pilot::aircraft`
- Pilot collective field: `System.Boolean PilotPlayerState::collective`
- Pilot control-strength field: `System.Single PilotPlayerState::pilotStrength`
- Pilot simulated-throttle field: `System.Single PilotPlayerState::simulatedThrottle`
- PilotPlayerState.EnterState inspection: assigns PilotBaseState.pilot=True, reads Pilot.aircraft=True, assigns inherited PilotBaseState.aircraft=False
- Aircraft control input field: `ControlInputs Aircraft::controlInputs`
- Airframe identity route: `UnitDefinition Unit::definition` -> `System.String UnitDefinition::jsonKey` / `System.String UnitDefinition::unitName`
- Airbrake control input field: `ControlInputs Airbrake::controlInputs`
- Airbrake aircraft field: `Aircraft Airbrake::aircraft`, `Aircraft Airbrake::attachedAircraft`
- Airbrake attached-aircraft field: `Aircraft Airbrake::attachedAircraft`
- JetNozzle aircraft field: `Aircraft JetNozzle::aircraft`
- JetNozzle afterburners field: `JetNozzle/Afterburner[] JetNozzle::afterburners`
- Afterburner throttle range fields: `System.Single JetNozzle/Afterburner::throttleStart`, `System.Single JetNozzle/Afterburner::throttleEnd`
- GameManager flight-controls field: `System.Boolean GameManager::flightControlsEnabled`
- PlayerSettings relative-throttle field: `System.Boolean PlayerSettings::throttleUseRelative`
- PlayerSettings invert-collective field: `System.Boolean PlayerSettings::invertCollective`
- PlayerSettings throttle-negative field: `System.Boolean PlayerSettings::throttleUseNegative`
- ControlSurface job-field update: `System.Void ControlSurface::UpdateJobFields()`
- ControlSurface aircraft field: `Aircraft ControlSurface::aircraft`
- ControlSurface control-input field: `ControlInputs ControlSurface::controlInputs`
- ControlSurface max-split field: `System.Single ControlSurface::maxSplit`
- ControlSurface job fields control-input field: `NuclearOption.Jobs.ControlInputsBurst NuclearOption.Jobs.ControlSurfaceFields::controlInputs`
- ControlSurface job fields max-split field: `System.Single NuclearOption.Jobs.ControlSurfaceFields::maxSplit`
- ControlSurface job execute: `System.Void NuclearOption.Jobs.ControlSurfaceJob_Math::Execute(NuclearOption.Jobs.ControlSurfaceFields& fields, UnityEngine.Quaternion& mainRotation, System.Int32 upperIndex, System.Int32 lowerIndex)`
- ControlSurface job inspection found control-input/throttle/max-split accesses: True/True/True
- ControlSurface snapshot inspection found throttle copy/input storage: True/True
- Throttle action/GetAxisRaw inspection: True
- Simulated-throttle read/write inspection: True/True
- Public ControlInputs.throttle write inspection: True
- Mathf.Clamp01 inspection: True
- GetAxisRawPrev present: True
- Relative/negative settings read: True/True
- Public-to-simulated throttle mapping observed: `throttleUseNegative=false: simulatedThrottle; throttleUseNegative=true: 0.5 * (simulatedThrottle + 1); inverse: publicThrottle * 2 - 1`
- Axis Modifier read in selected hook: True
- Airbrake throttle/zero comparison inspection: True/True
- Afterburner bool parameter: index 4, name `allowAfterburner`

## Deviations

- PilotPlayerState.EnterState(Pilot) assigns PilotBaseState.pilot and reads Pilot.aircraft, but does not assign inherited PilotBaseState.aircraft; the local-aircraft route is PilotBaseState.pilot -> Pilot.aircraft.
