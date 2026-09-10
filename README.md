# PEAK Troll Mod

[![Download latest release](https://img.shields.io/badge/Download-Latest%20Release-2ea44f?style=for-the-badge&logo=github)](https://github.com/rdlawe15-cpu/PeakTrollMod/releases/latest)

A private-lobby trolling and chaos toolkit for **PEAK**, controlled through an in-game menu opened with **F7**.

Skip the spectator wait when joining an expedition in progress, monitor the team, open your backpack instantly, use an upgraded spectator mode, resurrect fallen scouts, fly around the mountain, launch friends into the sky, summon enemy ambushes, create unsettling mirages, or combine multiple effects into custom chaos sequences.

Version 0.4.5 adds a dedicated searchable item-spawner page, held-item and climb stamina forecasts, party supply advice, hotkey conflict diagnosis, effectively unlimited local Rescue Claw reach, real-luggage navigation, Anti-Mirages, Mind Control, Amplify Hunger Rates, Summit Saboteur, new campfire traps, safe troll-effect cards, an optional unlimited-lobby mode, and persistent Favorites & Recent Actions. The GitHub build remains marked as a prerelease while this large update is field-tested.

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

- **Smart Climb Forecast** — Hold reach toward a steep surface to find its next standable ledge and estimate height, climb time, stamina cost, remaining stamina, and a configurable safety reserve using PEAK's live climb and surface modifiers.
- **Party Supply Advisor** — Audits synchronized pocket and optional equipped-backpack contents without moving items, summarizes each scout's food, recovery, climbing, and mobility coverage, and flags shortages or dangerously concentrated supplies.
- **Hotkey Conflict Doctor** — Finds exact duplicate `KeyboardShortcut` and `KeyCode` bindings across enabled BepInEx plugins and offers an unused replacement that is applied only when explicitly clicked.
- **Infinite Rescue Claw Reach** — Gives the locally held Rescue Claw an effectively map-wide 5,000 m targeting range. A genuine distant wall hit zips your scout toward the anchor at a capped 45 m/s and releases near the surface instead of letting the native one-second rope timeout snap first; friend/item rescues and empty shots stay native.
- **Clean Item Spawner** — Search the live PEAK item catalog, choose quantities, give items to a selected scout or yourself, place tracked world items near a scout, and refresh the catalog without leaving the menu.
- **Held-item Stamina Preview** — Hold a usable item to see its predicted stamina capacity, condition changes, extra stamina, and pass-out risk before consuming it. This local overlay automatically yields to the standalone Effect Preview mod when installed.
- **Immortality & Infinite Stamina** — Immortality prevents the local scout from dying or fully passing out and continuously clears every negative stamina-bar condition, including carried-weight encumbrance. Infinite Stamina independently refills usable stamina; both toggles live in Player → Health & Status.
- **Mind Control** — Select one scout and follow them from a third-person camera. A protocol-compatible target receives full normal PEAK movement, camera-look, jump, sprint, crouch, interaction, item-use/drop/switch, ping, and emote input relaying. The host can also use limited native puppet control on unmodded scouts for bounded movement, sprinting, jumping, and crouching; PEAK keeps their own local input active. Pause and voice are never relayed, no desktop input or computer access exists, and F6 releases control.
- **Amplify Hunger Rates** — Select a player and accelerate hunger from 2× to 20×, or restore the normal rate at any time. Compatible owners receive an exact native-rate multiplier; hosts can also affect unmodded targets through bounded native Hunger-status pulses.
- **Real Luggage Directions** — Follow a compact direction-and-distance HUD to the nearest unopened genuine luggage. The search uses PEAK's live luggage registry and rejects native `MirageLuggage`, native `Mirage`, and renderer-only mirages created by this mod.
- **Anti-Mirages** — Hide detected native `MirageLuggage` hierarchies, native `Mirage` particles and referenced objects, and PEAK Troll Mod-created mirages on the installing client wherever they appear. Original renderer states are cached and restored when disabled, during scene changes, or on shutdown.
- **Non-Modal F7 Menu** — Continue moving while the menu is visible. Movement pauses only while a named text field has keyboard focus; Enter, clicking away, or closing the menu releases that focus. Opening PEAK's pause menu with Escape closes F7 automatically.
- **Reliable Typography** — Uses PEAK's native Unity IMGUI font with distinct sizing, weight, color, and spacing for headings, labels, descriptions, navigation, and compact HUD information. Bundled Exo 2 and Inter assets are reserved for a future Unity-native font pipeline because runtime font registration corrupts text in the current PEAK build.
- **Safe Troll Effect Cards** — Inspect a troll effect's target, authority, current strength/count/timing, cleanup behavior, and a small non-executing animation before opening its controls.
- **Unlimited Lobby** — Optionally raise newly hosted rooms from four to 4–30 players and scale campfire food/backpacks for extra and late-joining scouts. Automatically yields when the standalone PEAK Unlimited mod is installed.
- **Favorites & Recent Actions** — Persist quick-access item and effect shortcuts locally so common actions are easy to find again.
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
- **Player Effects** — Reversible owner-controlled flight, ragdoll, knockout, launch, Horizontal Yeet, Sky High, Far Horizon, Off Mountain, teleportation, status effects, and movement-speed controls. Far Horizon sends the selected scout 25–300 m outward through the same bounded native warp/ragdoll path as Sky High.
- **Fake Enemies and Mirages** — Create visual-only scouts, luggage, statues, creatures, and enemy decoys with configurable behaviors.
- **Enemy Ambushes** — Spawn and track supported real enemies when host authority and runtime prefabs are available.
- **Fake Audio** — Play discovered in-game sounds as targeted 2D or positioned 3D cues.
- **Item Chaos** — Item Storm, Mandrake Rain, dynamite showers, Wrong Mountain, and One Live One.
- **Chaos Combo Builder** — Assemble reusable timed sequences from several troll actions.
- **Summit Saboteur** — Arm a hidden host-controlled trap for one scout's final summit approach. It drops their held item, maxes Hunger, briefly locks stamina, places a targeted zombie behind them, shows a fake recovery notice, and sends them toward the far horizon. Native steps still affect unmodded targets; the stamina lock and notice require their compatible client.
- **Position Roulette** — Randomly rotate the positions of available scouts.
- **Campfire Traps** — Choose between returning the lobby to the first segment or instantly killing every living scout within a configurable 3–50 m radius when a campfire is ignited. The igniter is included when nearby.
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

When **Prefer enabled external QoL mods** is on, the built-in Team Status Panel yields to PeakStatsEx's enabled teammate-stamina display, and Quick Backpack yields to EasyBackpack. The held-item stamina forecast always yields to the standalone Effect Preview mod to prevent duplicate overlays. Turn the preference off if you intentionally want the other built-in versions.

## Installation

1. Install **BepInExPack PEAK 5.4.75301** or a newer compatible BepInEx 5 release.
2. Install PEAK Troll Mod through your preferred Thunderstore-compatible mod manager.
3. Launch PEAK through the mod manager.
4. Enter a lobby and press **F7** to open the menu.

## Compatibility

- Mod version: **0.4.5**
- Networking protocol: **v9**
- Inspected PEAK build: **2.4.b (`3e62ee214`)**

Game updates may change internal APIs or prefab availability. Unsupported capabilities disable themselves instead of relying on guessed game behavior.

## Important

This mod is intended for consensual private-lobby fun. It does not spoof player identity, bypass host authority, record voice, grant Steam achievements, or redistribute game assets.

Not affiliated with Aggro Crab, Landfall, Steam, or the developers of PEAK.

Exo 2 and Inter are distributed under the SIL Open Font License 1.1; their license texts are included beside the bundled font files.
