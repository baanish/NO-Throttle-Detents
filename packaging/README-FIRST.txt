NUCLEAR OPTION DETENTS @VERSION@

This standalone package holds the mod plus BepInEx 5.4.23.5, for a game that
has no BepInEx yet. If BepInEx 5 is already installed, use the plugin-only ZIP
instead. For Nuclear Option Mod Manager, use the -nomm.zip. BepInEx 6 is not
supported.

INSTALL

1. Close Nuclear Option.
2. In Steam, right-click Nuclear Option, then choose Manage -> Browse local files.
3. Extract the CONTENTS of this ZIP into the folder containing NuclearOption.exe.
   winhttp.dll, doorstop_config.ini, and the BepInEx folder must sit directly
   beside NuclearOption.exe, not inside an extra folder.
4. Launch the game through Steam.
5. Check that BepInEx\LogOutput.log contains
   "Nuclear Option Detents @VERSION@ loaded."

ANALOG THROTTLE SETTING

Keyboard and button throttles work with any game setting. An analog throttle
axis (HOTAS lever, slider) needs "Use Throttle Relative Axis" on in the game's
own Controls menu, beside Invert Pitch, not the F1 menu. It is off in a new
install. While it is off, an analog axis stays vanilla and the F1 status reads
NOT APPLICABLE.

SETTINGS

The config file is BepInEx\config\com.baanish.nuclearoption.detents.cfg.
Settings, supported aircraft, and known limits:
https://github.com/baanish/NO-Throttle-Detents

TROUBLESHOOTING

If BepInEx\LogOutput.log does not exist, look for an extra wrapper folder and
check whether security software quarantined winhttp.dll. The load line reports
the throttle patch status. If the patch is unavailable, for example after a
game update, the throttle stays vanilla.

UNINSTALL

To remove only the mod, delete BepInEx\plugins\NuclearOptionDetents, and
optionally the config file above.

To remove BepInEx as well, when no other mod uses it, also delete the BepInEx
folder, .doorstop_version, doorstop_config.ini, winhttp.dll, README-FIRST.txt,
THIRD_PARTY_NOTICES.md, changelog.txt, and the licenses folder. Do not delete
other game files. Renaming winhttp.dll to winhttp.dll.disabled turns the
loader off temporarily. Steam file verification does not reliably remove the
loader files.
