# PEAK Troll Mod

[![Download latest release](https://img.shields.io/badge/Download-Latest%20Release-2ea44f?style=for-the-badge&logo=github)](https://github.com/rdlawe15-cpu/PeakTrollMod/releases/latest)

A private-lobby trolling and chaos toolkit for **PEAK**, controlled through an in-game menu opened with **F7**.

Skip the spectator wait when joining an expedition in progress, monitor the team, open your backpack instantly, use an upgraded spectator mode, resurrect fallen scouts, fly around the mountain, launch friends into the sky, summon enemy ambushes, create unsettling mirages, or combine multiple effects into custom chaos sequences.

> Use only in private/cooperative lobbies where everyone is comfortable with modded antics.

## v0.4 Quality-of-Life Bundle

- **No Wait Plus** automatically revives late-joining scouts and safely places them beside the team or at a checkpoint, with optional reconnect-state restoration.
- **Quick Reconnect** remembers the last valid Steam lobby and rejoins it through PEAK's native version-checked flow.
- **Emergency Recovery** returns you to safe ground, a nearby scout, the expedition start, or a checkpoint and can clear dangerous velocity or reversible effects.
- **Team Status Panel** keeps nearby scout stamina, distance, life state, and important conditions visible in a compact HUD.
- **Quick Backpack** opens the equipped backpack wheel instantly with a configurable hotkey.
- **Better Spectating** adds configurable player cycling, target information, native free camera access, ghost pings, and revive-beside-target controls.
- **Mod Config** provides one place to toggle built-in modules and inspect or edit settings exposed by other loaded BepInEx mods.
- **Persistent Player Preferences** remembers aliases, UI colors, voice settings, and random-target exclusions for your regular Steam friends.
- **Lobby Readiness** shows ready and loading states, mod/version/protocol compatibility, host changes, player count, and a copyable lobby code.
- **Accessibility Options** add interface scaling, high-contrast status colors, camera-shake scaling, reduced repeated effects, flexible menu input, and configurable shortcuts.

## Features

- **Phantom Pings** — Target one player with Breadcrumb Trail, Circle, or Behind You ping patterns.
- **No Wait Plus** — Join an expedition in progress beside the nearest or lowest safely grounded scout, or at the active checkpoint. Reconnecting scouts can retain restored inventory and conditions, and post-warp velocity is cleared.
- **Quick Reconnect** — Remembers the last valid Steam lobby and reconnects through PEAK's native version-checked lobby flow.
- **Emergency Recovery** — Return yourself to recorded safe ground, the nearest safe scout, or the active checkpoint; stop flight and dangerous velocity; or restore reversible local changes.
- **Team Status Panel** — Shows nearby scouts' stamina, distance, life state, and important conditions in a compact HUD.
- **Quick Backpack** — Opens the equipped backpack wheel with a configurable hotkey (default **B**).
- **Better Spectating** — Cycle scouts with configurable keys, inspect target altitude/state, use PEAK's native free camera, place ghost pings, or revive beside the spectated scout.
- **Persistent Player Preferences** — Save local aliases, UI colors, voice volume/mute, and random-target exclusions by Steam player so they return with your friends.
- **Lobby Readiness** — See loaded players, opt-in ready state, mod/version/protocol compatibility, current host and host changes, plus copy the Steam lobby code in one click.
- **Accessibility Options** — Adjustable interface/text scale, high-contrast status colors, camera-shake scaling, reduced repeated visual effects, toggle/hold menu input, and configurable shortcuts.
- **Mod Config** — Toggle PEAK Troll Mod modules, browse every loaded BepInEx plugin, soft-enable/disable plugin components for the current session, and edit their exposed settings with the owning config serializer.
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

## Mod Config and Compatibility

Open **Lobby** from the F7 sidebar for readiness, compatibility, persistent friend preferences, and the expanded Emergency Recovery toolkit. Readiness is explicitly advertised between current compatible clients; unmodded players are shown as loaded/loading rather than being assigned a false ready state.

Open **Mod Config** to control the built-in quality-of-life modules or edit settings exposed by other loaded BepInEx mods. Setting changes are written to each mod's own config file. Whether they apply immediately depends on that mod, so restart PEAK when a setting does not update live.

Runtime component switches are session-only. They cannot guarantee that Harmony patches are removed, and some disabled components cannot initialize again until restart. PEAK Troll Mod therefore never offers to disable its own menu from inside the menu.

When **Prefer enabled external QoL mods** is on, the built-in Team Status Panel yields to PeakStatsEx's enabled teammate-stamina display, and Quick Backpack yields to EasyBackpack. Turn the preference off if you intentionally want the built-in versions.

## Installation

1. Install **BepInExPack PEAK 5.4.75301** or a newer compatible BepInEx 5 release.
2. Install PEAK Troll Mod through your preferred Thunderstore-compatible mod manager.
3. Launch PEAK through the mod manager.
4. Enter a lobby and press **F7** to open the menu.

## Compatibility

- Mod version: **0.4.0**
- Networking protocol: **v6**
- Inspected PEAK build: **2.4.b (`3e62ee214`)**

Game updates may change internal APIs or prefab availability. Unsupported capabilities disable themselves instead of relying on guessed game behavior.

## Important

This mod is intended for consensual private-lobby fun. It does not spoof player identity, bypass host authority, record voice, grant Steam achievements, or redistribute game assets.

Not affiliated with Aggro Crab, Landfall, Steam, or the developers of PEAK.
