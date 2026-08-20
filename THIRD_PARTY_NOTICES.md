# Third-party notices

Nuclear Option Detents contains original MIT-licensed code. It does not contain or modify Nuclear Option game assemblies.

This release package is a client-side gameplay mod, not an anti-cheat
bypass or a statement of multiplayer approval. It makes no network calls and
does not alter remote aircraft; public-server moderators may still restrict
its use.

## BepInEx 5.4.23.5

The standalone release redistributes the unmodified official `BepInEx_win_x64_5.4.23.5.zip` runtime from the [BepInEx v5.4.23.5 release](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.5). The pinned archive SHA-256 is `82F9878551030F54657792C0740D9D51A09500EEAE1FBA21106B0C441E6732C4`.

The BepInEx repository's license at tag v5.4.23.5 is MIT. The standalone package includes that exact text as `licenses/BepInEx-MIT.txt`.

The package also includes the canonical GNU LGPL 2.1 text as `licenses/BepInEx-LGPL-2.1.txt` for legacy runtime notice completeness. It is not presented as the current BepInEx repository license.

The official BepInEx archive contains its own runtime dependencies, including HarmonyX, Mono.Cecil, MonoMod, and Unity Doorstop. Their upstream projects and license notices are listed by the BepInEx project. Nuclear Option Detents does not modify those binaries.

## Implementation references

The mod was implemented from the behavior specification and local assembly metadata. These public projects were consulted for Nuclear Option modding conventions:

- [CarloApri/NO-mods HeliBinds](https://github.com/CarloApri/NO-mods)
- [qwerty1423/no-autopilot-mod](https://github.com/qwerty1423/no-autopilot-mod)
- [TruffleWolf/NuclearOption-Throttle-Fix](https://github.com/TruffleWolf/NuclearOption-Throttle-Fix)
- [nikkorap/NuclearMods](https://github.com/nikkorap/NuclearMods)

No source from those projects is copied into this repository. Older public decompilations were treated only as background material. The installed `Assembly-CSharp.dll` supplied signatures and IL-level compatibility conclusions, and no proprietary method bodies are committed or packaged.
