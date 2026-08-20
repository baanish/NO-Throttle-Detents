NUCLEAR OPTION DETENTS @VERSION@

Multiplayer use is unverified; hosts or server moderators may prohibit BepInEx
or this mod.

This plugin adds 200 ms virtual detents at idle and each aircraft's captured
full-dry/afterburner boundary. The input must remain held for the entire dwell;
releasing early resets the hold. Recorded v0.1 testing covered identity and
readiness on all 13 allowlisted airframes plus reduced-dwell upper/lower
control-path checks on FS-12, FS-20, KR-67, and AB-4. The runtime received a
manual flight check before an author-metadata-only rebuild.

Install by extracting the ZIP contents into the folder containing
NuclearOption.exe. This plugin-only package requires an existing BepInEx 5
installation. It does not contain or overwrite BepInEx.cfg; BepInEx 6 is not
supported.
This manual-install archive omits the live mod config so updates preserve user
settings; the release default has DebugLogging off. Use the separate `-nomm.zip`
artifact with Nuclear Option Mod Manager.

Confirm BepInEx\LogOutput.log contains "Nuclear Option Detents @VERSION@ loaded."
BepInEx creates BepInEx\config\com.aanish.nuclearoption.detents.cfg with the
defaults on first launch. This ZIP omits that live file so upgrades preserve
existing settings. Text edits apply on the next launch.

To uninstall only this mod, delete BepInEx\plugins\NuclearOptionDetents. You may
also delete BepInEx\config\com.aanish.nuclearoption.detents.cfg.

Version @VERSION@ applies detents only to relative throttle mode. Absolute/HOTAS,
collective, unsupported aircraft, and absent aircraft systems remain vanilla.
Harmony hooks run process-wide, then exact local-aircraft identity checks
decide whether a gate applies. AB-4's upper detent requires all four
captured afterburner nozzles. Other throttle or autopilot mods may conflict.
