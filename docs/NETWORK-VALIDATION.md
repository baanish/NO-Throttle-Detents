# Network validation

Network Validation records the local aircraft and one selected remote aircraft
as they appear on one client. It does not patch networking or change an
aircraft. The observer refreshes the owner list once per second, resolves
components once per selected aircraft identity, and samples at 10 Hz.
With both aircraft present, that is at most 20 sample records per second.

Both players should use the same release-candidate DLL. On the observing
client, set these values in
`BepInEx\config\com.baanish.nuclearoption.detents.cfg`:

```ini
[General]
DebugLogging = true
NetworkValidation = true
NetworkValidationOwner = 3
```

Set `NetworkValidationOwner` to the other player's session index. `-1` records
the local aircraft and an owner roster without sampling a remote. If the index
is unknown, make a short local-only capture and run the analyzer; its
`target selection` result lists the available remote owners.

Join the same mission. On the other client, hold and cross the idle/airbrake
and full-dry/afterburner detents. Quit the game and copy
`BepInEx\LogOutput.log` before launching it again.

Run the analyzer from the repository:

```powershell
pwsh ./tools/Test-NetworkValidation.ps1 -LogPath 'C:\path\to\LogOutput.log'
```

The observer samples no unrelated remote aircraft. `-Owner` remains available
when analyzing an older log that contains multiple remotes:

```powershell
pwsh ./tools/Test-NetworkValidation.ps1 -LogPath 'C:\path\to\LogOutput.log' -Owner 3
```

Results are `PASS`, `FAIL`, or `INCONCLUSIVE`; an inconclusive result needs
another capture.

Turn Network Validation and Debug Logging off after the check. They are both
off in release packages.
