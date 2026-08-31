# Project agent guide

## Scope

Nuclear Option Detents is a client-side BepInEx 5 prototype for relative-throttle
detents. Keep the runtime local: it may gate the local pilot's existing
airbrake and afterburner decisions, but it must not patch remote aircraft,
weapons, network transport, or server logic.

This is a v0.1 prototype. Prefer small, readable changes that a future
contributor can remove, replace, or extend. Keep the behavior obvious; do not
add infrastructure to make a prototype look production-ready.

## Workflow

1. Read the relevant source and the installed-build snapshot in
   `docs/COMPATIBILITY.md` before changing a Harmony target.
2. Run `pwsh ./build/Build.ps1` for the focused tests, Release build, and
   package validation. Pass `-GameDir 'C:\path\to\Nuclear Option'` when the
   game is not discoverable, or set `NUCLEAR_OPTION_DIR` for the same override.
3. Flight-test behavior changes in the installed game. Record what was tested,
   with the game build and aircraft, in the release's changelog entry.
4. For a NOMM listing, submit `dist/NuclearOptionDetents.nomnom.json` as
   `modManifests/NuclearOptionDetents.json` in a PR to `KopterBuzz/NOMNOM`
   (`main`), after the GitHub release is published and with the manifest's
   download URL and SHA-256 matching the released `-nomm.zip` asset.

## Invariants

- Unknown/new aircraft remain vanilla unless the player enables an exact-ID
  custom profile. Collective controls, absolute/HOTAS mode, and absent systems
  always remain vanilla.
- Built-in presets are keyed by `UnitDefinition.jsonKey`; runtime discovery
  cannot add one automatically. A player may opt an unknown aircraft into an
  exact-ID custom profile, which still requires matching local components.
- Afterburner code may only suppress an existing vanilla `true` decision.
- AB-4 requires all four expected afterburner nozzles to match the preset.
- Release packages ship with `DebugLogging = false`.
- Label code inspection and capability capture as inspection evidence. Claim
  flight or multiplayer verification only when a manual record exists.

## Context pointers

- New or changed airframe: read `docs/AIRFRAME-PRESETS.md` and update the
  capture/test record.
- Harmony or field compatibility: read `docs/COMPATIBILITY.md` and run the
  focused build/tests after changing a target.
- Behavior or performance change: read `docs/DESIGN.md`, run focused tests,
  and flight-check the affected behavior in game.
- Packaging or NOMM distribution: the build/package scripts and
  `packaging/NOMNOM-MANIFEST.template.json` are the source of truth for
  archive contents and listing metadata.

Keep documentation concise. Do not duplicate values that can be read directly
from project files or build output.
