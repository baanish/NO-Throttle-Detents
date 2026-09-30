NUCLEAR OPTION DETENTS @VERSION@

This folder holds the Nuclear Option Detents plugin for BepInEx 5. BepInEx 6
is not supported.

INSTALL INTO AN EXISTING BEPINEX 5

Extract the plugin-only ZIP into the folder containing NuclearOption.exe and
merge its BepInEx folder. The ZIP leaves BepInEx.cfg and your existing mod
config alone. BepInEx creates
BepInEx\config\com.baanish.nuclearoption.detents.cfg on first launch.
Launch the game, then check that BepInEx\LogOutput.log contains
"Nuclear Option Detents @VERSION@ loaded."

SUPPORTED CONTROLS

Keyboard and button throttles, with any game setting. Analog throttle axes
(HOTAS levers, sliders, sticks) are not supported and stay vanilla.

Settings, supported aircraft, and known limits:
https://github.com/baanish/NO-Throttle-Detents

UNINSTALL

Delete BepInEx\plugins\NuclearOptionDetents, and optionally
BepInEx\config\com.baanish.nuclearoption.detents.cfg.
