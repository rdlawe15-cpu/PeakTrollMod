# PEAK Troll Mod

[![Download latest release](https://img.shields.io/badge/Download-Latest%20Release-2ea44f?style=for-the-badge&logo=github)](https://github.com/rdlawe15-cpu/PeakTrollMod/releases/latest)

A private-lobby trolling and chaos toolkit for **PEAK**, controlled through an in-game menu opened with **F7**.

Skip the spectator wait when joining an expedition in progress, resurrect fallen scouts, fly around the mountain, launch friends into the sky, summon enemy ambushes, create unsettling mirages, place Phantom Pings, or combine multiple effects into custom chaos sequences.

> Use only in private/cooperative lobbies where everyone is comfortable with modded antics.

## Features

- **Phantom Pings** — Target one player with Breadcrumb Trail, Circle, or Behind You ping patterns.
- **No Wait Plus** — Join an expedition in progress beside the nearest or lowest safely grounded scout, or at the active checkpoint. Reconnecting scouts can retain restored inventory and conditions, and post-warp velocity is cleared.
- **Quick Reconnect** — Remembers the last valid Steam lobby and reconnects through PEAK's native version-checked lobby flow.
- **Emergency Recovery** — Return yourself to recorded safe ground, the nearest safe scout, or the active checkpoint; stop flight and dangerous velocity; or restore reversible local changes.
- **Resurrection Controls** — Revive the selected scout, yourself, or queue the whole fallen party at safe intervals through PEAK's native synchronized revive path; the caller does not need to be host and the target does not need the mod.
- **Player Effects** — Reversible owner-controlled flight, ragdoll, knockout, launch, Horizontal Yeet, Sky High, Off Mountain, teleportation, status effects, and movement-speed controls.
- **Fake Enemies and Mirages** — Create visual-only scouts, luggage, statues, creatures, and enemy decoys with configurable behaviors.
- **Enemy Ambushes** — Spawn and track supported real enemies when host authority and runtime prefabs are available.
- **Fake Audio** — Play discovered in-game sounds as targeted 2D or positioned 3D cues.
- **Item Chaos** — Item Storm, Mandrake Rain, dynamite showers, Wrong Mountain, and One Live One.
- **Chaos Combo Builder** — Assemble reusable timed sequences from several troll actions.
- **Position Roulette** — Randomly rotate the positions of available scouts.
- **Campfire Reset Trap** — Return the lobby to the first segment when a campfire is ignited.
- **Helicopter Suppression** — Suppress the summit rescue presentation for compatible clients.
- **Cosmetic Controls** — Reversible local all-cosmetics and all-badges toggles without granting platform achievements.
- **Safe Cleanup** — Tracks mod-created objects and restores reversible state during resets, scene changes, shutdown, or plugin disable.

## Multiplayer Compatibility

The menu labels actions by their authority requirements:

- **Anyone** — Local or client-side action.
- **Everyone Needs Mod** — Intended recipients need the same mod and protocol version.
- **Host Only** — Requires the current Photon master client.
- **Unsupported** — Disabled when PEAK does not expose a reliable compatible API.

Some native PEAK actions—including resurrection, recovery warps, ragdoll, knockout, teleport, and several item effects—can affect unmodded targets. No Wait and Quick Reconnect operate on the installing client. Phantom Pings, mirages, fake audio, and other mod-rendered effects require compatible clients.

For the best experience, have everyone install the same version before joining the lobby.

## Installation

1. Install **BepInExPack PEAK 5.4.75301** or a newer compatible BepInEx 5 release.
2. Install PEAK Troll Mod through your preferred Thunderstore-compatible mod manager.
3. Launch PEAK through the mod manager.
4. Enter a lobby and press **F7** to open the menu.

## Compatibility

- Mod version: **0.4.0 Development**
- Networking protocol: **v6**
- Inspected PEAK build: **2.4.b (`3e62ee214`)**

Game updates may change internal APIs or prefab availability. Unsupported capabilities disable themselves instead of relying on guessed game behavior.

## Important

This mod is intended for consensual private-lobby fun. It does not spoof player identity, bypass host authority, record voice, grant Steam achievements, or redistribute game assets.

Not affiliated with Aggro Crab, Landfall, Steam, or the developers of PEAK.
