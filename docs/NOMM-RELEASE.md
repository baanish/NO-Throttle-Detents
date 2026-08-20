# NOMM release handoff

NOMM reads listings from the separate NOMNOM registry. A manifest file inside
this repository or release archive does not create a catalog listing.

## Before submission

1. Run `pwsh ./build/Build.ps1` and keep the resulting `-nomm.zip` as the
   catalog artifact. Use `-GameDir` or `NUCLEAR_OPTION_DIR` when discovery does
   not find the game.
2. Run the relevant checks in [MANUAL-TESTS.md](MANUAL-TESTS.md) on the exact
   DLL you intend to distribute and record the results there.
3. Confirm the generated package uses the version from the project file and
   that the default config keeps `DebugLogging = false`.
4. Make the source repository public. NOMNOM rejects DLL mods without public,
   unobfuscated source.
5. Create a GitHub Release tagged with the plugin version. Upload the flat
   `-nomm.zip` first, then the manual plugin-only and standalone ZIPs. NOMM
   supplies BepInEx.
6. Submit `dist/NuclearOptionDetents.nomnom.json` as
   `modManifests/NuclearOptionDetents.json` in a pull request to
   `KopterBuzz/NOMNOM` on its `main` branch. Confirm that its download URL and
   SHA-256 match the published NOMM release asset.

## Package choice

Use the flat NOMM artifact for the catalog. It contains only the plugin DLL,
README, and license at archive root. NOMM extracts that archive into its own
managed plugin directory and writes its own metadata. The archive omits the
live config so updates preserve user settings. Use the plugin-only artifact for
manual BepInEx installs and the standalone artifact for users without BepInEx.

## Listing notes

Describe the mod as a client-local relative-throttle detent prototype. State
the tested game build and aircraft. Cite only manual records from
[MANUAL-TESTS.md](MANUAL-TESTS.md) as flight evidence; the compatibility
snapshot, package validation, and source tests are not.

The manifest ID is `NuclearOptionDetents`, matching the assembly name. Mark it
as `Client`, `plugin`, and game version `0.34.2`. Configuration Manager is
optional and is not a dependency. Do not list compatibility warnings as hard
incompatibilities without a reproduced conflict.
