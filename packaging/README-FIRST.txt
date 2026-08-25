NUCLEAR OPTION DETENTS @VERSION@

Multiplayer use is unverified; hosts or server moderators may prohibit BepInEx
or this mod.

This is the standalone fresh-install package. It includes BepInEx 5.4.23.5.
The packaged defaults have Debug Logging and Network Validation off.

INSTALL

1. Close Nuclear Option.
2. In Steam, right-click Nuclear Option, then choose Manage -> Browse local files.
3. Open NuclearOptionDetents-v@VERSION@-standalone-fresh-install-win-x64.zip.
4. Extract the ZIP's CONTENTS into the folder containing NuclearOption.exe.
   Do not extract this standalone package over an existing BepInEx install;
   use the plugin-only ZIP for that case. BepInEx 6 is not supported.
5. Launch the game normally through Steam.
6. Open BepInEx\LogOutput.log and find "Nuclear Option Detents @VERSION@ loaded."
7. Configuration is in BepInEx\config\com.baanish.nuclearoption.detents.cfg.

Do not create an extra wrapper folder. After extraction, winhttp.dll,
doorstop_config.ini, and the BepInEx folder must be directly beside
NuclearOption.exe.

DEFAULT BEHAVIOR

At idle, keep holding decrease for 200 ms before the automatic airbrake can
open. At the aircraft's captured full-dry/afterburner boundary, keep holding
increase for 200 ms before afterburner is allowed. Releasing early resets the
hold. A small indicator shows active locks. Relative-throttle sensitivity is
configurable on aircraft with a supported detent; use PauelsRandomFixes for
other aircraft. Relative throttle mode is required; absolute/HOTAS mode remains vanilla
in version @VERSION@. Auto Hover temporarily bypasses detents and sensitivity.

EXISTING BEPINEX INSTALLATION

Use the plugin-only ZIP instead. It does not replace BepInEx.cfg or the mod's
existing live config. For NOMM, use the separate flat `-nomm.zip` artifact;
NOMM supplies BepInEx and should not be combined with this bundle.

TROUBLESHOOTING

If BepInEx\LogOutput.log does not exist, check for an extra wrapper folder and
check whether security software quarantined winhttp.dll. The plugin writes one
load line with its throttle-patch status. If the target is unavailable, the mod
leaves throttle behavior unchanged. Other throttle or autopilot mods may
conflict. A game update can make the patch unavailable.

UNINSTALL OR DISABLE

To remove only the mod, delete BepInEx\plugins\NuclearOptionDetents. Optionally
delete BepInEx\config\com.baanish.nuclearoption.detents.cfg.

If no other mods use BepInEx, you may also remove the BepInEx folder,
.doorstop_version, doorstop_config.ini, winhttp.dll, README-FIRST.txt,
THIRD_PARTY_NOTICES.md, changelog.txt, and the licenses folder. Do not delete
unrelated game files. Renaming winhttp.dll to winhttp.dll.disabled temporarily
disables the loader. Steam file verification does not reliably remove extra
loader files.
