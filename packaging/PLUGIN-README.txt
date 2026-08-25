NUCLEAR OPTION DETENTS @VERSION@

Multiplayer use is unverified; hosts or server moderators may prohibit BepInEx
or this mod.

This plugin adds 200 ms virtual detents at idle and each aircraft's captured
full-dry/afterburner boundary. The input must remain held for the entire dwell;
releasing early resets the hold.

Install by extracting the ZIP contents into the folder containing
NuclearOption.exe. This plugin-only package requires an existing BepInEx 5
installation. It does not contain or overwrite BepInEx.cfg; BepInEx 6 is not
supported.
This manual-install archive omits the live mod config so updates preserve user
settings; release defaults have Debug Logging and Network Validation off. Use
the separate `-nomm.zip` artifact with Nuclear Option Mod Manager.

Confirm BepInEx\LogOutput.log contains "Nuclear Option Detents @VERSION@ loaded."
BepInEx creates BepInEx\config\com.baanish.nuclearoption.detents.cfg with the
defaults on first launch. This ZIP omits that live file so upgrades preserve
existing settings. Text edits apply on the next launch.

To uninstall only this mod, delete BepInEx\plugins\NuclearOptionDetents. You may
also delete BepInEx\config\com.baanish.nuclearoption.detents.cfg.

Version @VERSION@ applies detents only to relative throttle mode. Absolute/HOTAS,
collective, unsupported aircraft, and absent aircraft systems remain vanilla.
The patched throttle route belongs to the local pilot, and the runtime checks
the game-local aircraft before applying a detent. Auto Hover bypasses detents
and sensitivity until it is turned off. AB-4's upper detent requires all four
captured afterburner nozzles.
The sensitivity multiplier covers aircraft with a supported detent. Use
PauelsRandomFixes for other aircraft; when active, it owns sensitivity.
Other throttle, airbrake, afterburner, or autopilot mods may conflict.
