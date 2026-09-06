# PEAK Troll Mod

[![Download latest release](https://img.shields.io/badge/Download-Latest%20Release-2ea44f?style=for-the-badge&logo=github)](https://github.com/rdlawe15-cpu/PeakTrollMod/releases/latest)

PEAK Troll Mod is a BepInEx 5 mod for private/cooperative PEAK lobbies. Press **F7** for a dark in-game control panel covering player physics, movement, visibility, teleportation, statuses, item delivery, tracked enemy ambushes, visual-only mirages, fake audio, enemy decoys, and bounded Chaos Mode.

Version: **0.2.0**

Inspected game build: **PEAK 2.4.b (`3e62ee214`)**

## Safety and authority model

The menu shows an authority badge beside each action:

- 🟢 **Anyone** — local/client-side operation.
- 🟡 **Everyone Needs Mod** — a validated mod message asks the owning or viewing client to perform the effect. Remote recipients need exactly the same protocol version.
- 🔒 **Host Only** — uses PEAK/Photon's genuine network spawning or enemy authority and is disabled for non-hosts.
- ⚠ **Unsupported** — the inspected build did not expose a reliable safe API or prefab path. The control is disabled and explains why.

The mod does not impersonate Photon players, spoof ownership, or bypass host/master-client checks. Mirage Scouts and Fake Enemies contain copied transform, mesh, material, and animator data only. They do not contain Photon views, player controllers, input, voice, inventory, health, AI, colliders, attacks, or account identity.

Use this only with friends who expect modded lobby antics. Mod-rendered effects require the viewing client to have the same protocol version; native PEAK RPC actions are called out separately in the menu.

## Implemented in 0.2.0

- F7 menu with cursor restoration, input blocking, mouse/keyboard support, UI scaling, transparency, target refresh, permission badges, and emergency controls.
- Player discovery through `PlayerHandler.GetAllPlayerCharacters()`.
- Mod-free-target ragdoll through PEAK's native `Fall` and `AddForceAtPosition` Photon RPC paths; targets do not need PEAK Troll Mod.
- Dedicated **Off Mountain** ragdoll samples nearby terrain for the steepest drop and falls back to pushing away from the loaded climb route, while retaining the native RPC path and bounded force.
- Dedicated **Sky High** launch ragdolls a selected player and uses PEAK's native warp RPC to place them 25–300 m overhead; it requires neither host authority nor a target-side mod.
- Non-host/native knockout and elimination through PEAK's `RPCA_PassOut` and `RPCA_Die` paths; targets do not need the mod.
- Native, bounded launch and safe teleport through PEAK's all-client force/warp RPC paths; host authority and a target-side mod are not required.
- Owner-executed movement speed requests for compatible targets. Statuses use PEAK's master-validated native RPC when the menu user is host and the compatible target-owner path otherwise.
- Native item-in-hands spawning through `CharacterItems.SpawnItemInHand`, matching PEAK's master-client item RPC path; the recipient does not need PEAK Troll Mod.
- Renderer-state-cached invisibility synchronized to compatible observers.
- Ground-probed, geometry-checked safe teleport plus map-segment start/end discovery.
- Runtime status list and runtime item catalog, including Mandrake when its item prefab is loaded.
- Reversible all-cosmetics and all-badges toggles that do not grant platform achievements.
- Bounded Item Storm and Mandrake Rain using real networked PEAK item prefabs with tracked cleanup.
- Chaos Combo Builder for reusable timed multi-action sequences, plus Position Roulette and the one-live-fuse Dynamite preset.
- Optional Campfire Reset Trap that returns the lobby to the first segment start when a campfire is lit.
- Optional summit helicopter suppression synchronized among protocol-v5 compatible clients; unmodded clients cannot have their local rescue presentation intercepted.
- Host-only Scoutmaster and Mushroom Zombie ambushes using verified PEAK prefab paths/sources. Each tracked enemy retains the selected target through periodic host-authoritative retargeting.
- A bounded, target-following shower of 1–64 real networked Dynamite items, with height, spread, interval, lit-fuse controls, a separately configurable 1–128 active-item cap, and owner cleanup. It uses PEAK's runtime item prefab and is visible to unmodded clients.
- Native **Horizontal Yeet** ragdoll with an independently bounded 25–150 strength range.
- Independently scrollable menu cards so controls remain reachable at smaller resolutions and UI scales.
- Strict tracking and cleanup of objects created by this mod only.
- One-shot or multi-place ping placement that temporarily intercepts `PointPinger.DoPing`, sends the visual to the selected viewer or every compatible viewer, and restores normal pings after placement.
- Target-only Phantom Ping sequences in breadcrumb-trail, circle, and behind-you patterns, bounded to 3–12 pings at 0.35–3 second intervals with cancellation and reset cleanup.
- Renderer-only Mirage Scouts copied from a participating player's current appearance.
- Visual-only Luggage, Amulet Statue, Capybara, Scoutmaster, Mushroom Zombie, and Looker mirages when a runtime visual source is loaded.
- Fake Enemy behaviors: stand, stare, follow, follow at distance, walk past, look around, run away, charge, vanish when close, and timed despawn.
- “Something's There”, “It's Following You”, and “Did You See That?” fake-enemy presets with runtime audio integration.
- Runtime `AudioClip` discovery and targeted 2D/3D fake sound cues. Audio assets are referenced in memory and are never redistributed.
- Chaos Mode whitelist with bounded cooldowns and values.
- Reversible state cache plus cleanup on scene changes, plugin disable, and application shutdown.
- Capability detection, parameter validation, sender/target validation, exact protocol-version validation, structured logs, and hard object-count limits.

## Deliberately unsupported in PEAK 2.4.b

These controls are visible but disabled rather than backed by guessed APIs:

- Genuine Looker network spawning (no stable Photon prefab path was found).
- Voice stream swapping and talking while knocked out (no verified identity-safe/supported routing hook yet).
- Appearance swapping on real players (no verified reversible cosmetic application API).
- Poison/spore cloud prefab spawning (no stable network prefab paths found). Direct Poison/Spores statuses remain available.
- “Vanish when looked at” (no stable remote camera/FOV contract).
- Atomic two-player teleport swap and destructive held-item replacement.

See [DEVELOPMENT_NOTES.md](DEVELOPMENT_NOTES.md) for the inspected APIs and evidence.

## Build

Requirements:

1. Windows with the .NET Framework 4.7.2 Developer Pack or a compatible MSBuild installation.
2. PEAK installed with BepInEx 5.
3. The game's original managed assemblies. The project references them in place and never copies them into output.

From PowerShell:

```powershell
.\build.ps1
```

For a non-default game location:

```powershell
.\build.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\PEAK'
```

Output: `bin\Release\PeakTrollMod.dll`.

## Install

1. Install `BepInEx-BepInExPack_PEAK-5.4.75301` (or a newer compatible BepInEx 5 pack).
2. Copy `PeakTrollMod.dll` to `PEAK\BepInEx\plugins\PeakTrollMod\`.
3. Launch PEAK and check `BepInEx\LogOutput.log` for `PEAK Troll Mod 0.2.0 loaded`.
4. Every friend who should see targeted mirages/audio must install the same mod version. Native ragdoll, knockout, elimination, and item-in-hand targets do not need it.
5. Press F7 in a lobby/run.

## Thunderstore package

Create an original square **256×256 PNG** named `icon.png` in the project root; see [ICON_INSTRUCTIONS.md](ICON_INSTRUCTIONS.md). Then run:

```powershell
.\package.ps1
```

The script builds Release, creates the required `BepInEx/plugins/PeakTrollMod/` package layout, includes the manifest/docs/icon, and writes `artifacts\PEAK_Troll_Mod-0.2.0.zip`. Do not add PEAK, Unity, Photon, Harmony, or BepInEx DLLs to the archive.

## Troubleshooting

- **F7 does nothing:** confirm the DLL is beneath `BepInEx\plugins`, inspect `LogOutput.log`, and check that another mod did not bind F7. Change `MenuKey` in `BepInEx\config\com.peaktrollmod.core.cfg`.
- **Remote action says the player is incompatible:** that player has not advertised the exact 0.2.0 protocol. Install the same version on all participating clients and rejoin the room.
- **Only the menu user sees placed luggage or Phantom Pings:** update every intended viewer to the protocol-5 build. Mirage props and Phantom Pings are rendered by each receiving client; unmodded clients cannot render them.
- **A mirage type is disabled:** its visual prefab/model is not loaded in the current scene. Use “Refresh Runtime Visual Sources” after the relevant content loads.
- **Zombie spawning is disabled:** the current scene has no live `MushroomZombieSpawner` prefab source.
- **Host button is locked:** only Photon’s current master client can use genuine enemy spawning/retargeting.
- **Build warns about reference assemblies:** install the .NET Framework 4.7.2 Developer Pack. The checked-in project intentionally references PEAK’s own runtime assemblies and does not bundle them.
- **Game updated:** review the capability log and `DEVELOPMENT_NOTES.md`. Unresolved capabilities disable themselves; do not rename guessed methods or prefabs to force them on.

## Privacy and networking

The mod sends only a small validated command payload inside the current Photon room. It does not record voice, synthesize identities, contact an external service, or persist player/account identifiers. Event senders must be present in the room, command/target types are checked, numeric inputs are clamped, and unknown or version-mismatched payloads are rejected.
