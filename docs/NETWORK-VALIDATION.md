# Network validation

Network Validation checks how one player's detents look from another client.
The observing client logs its own aircraft and one selected remote aircraft
at 10 Hz. It does not patch networking or change any aircraft.

## Capture a log

1. Give both players the same DLL.
2. On the observing client, set these values in
   `BepInEx\config\com.baanish.nuclearoption.detents.cfg`:

   ```ini
   [General]
   DebugLogging = true
   NetworkValidation = true
   NetworkValidationOwner = 3
   ```

   Set `NetworkValidationOwner` to the other player's session index. If you
   do not know it, leave it at `-1`, make a short capture, and run the
   analyzer: its `target selection` result lists the remote owners it saw.
3. Join the same mission. On the other client, hold through the idle and
   afterburner detents.
4. Quit the game and copy `BepInEx\LogOutput.log` before launching again,
   since the next launch overwrites it.

## Analyze it

From the repository root:

```powershell
pwsh ./tools/Test-NetworkValidation.ps1 -LogPath 'C:\path\to\LogOutput.log'
```

For an older log that sampled several remote aircraft, pick one with
`-Owner`:

```powershell
pwsh ./tools/Test-NetworkValidation.ps1 -LogPath 'C:\path\to\LogOutput.log' -Owner 3
```

Each check reports `PASS`, `FAIL`, or `INCONCLUSIVE`. An inconclusive check
needs another capture.

Turn `NetworkValidation` and `DebugLogging` off afterwards. Both are off in
release packages.
