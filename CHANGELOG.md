# Changelog

## Unreleased (0.4.0)

- Added a compact Team Status Panel with nearby scout stamina, distance, life state, and prominent conditions.
- Added Quick Backpack with a configurable hotkey and the native equipped-backpack wheel.
- Added Better Spectating with configurable player cycling, target altitude/state overlay, PEAK's native free camera, cooldown-respecting ghost pings, and revive-beside-spectated controls.
- Added a Mod Config page for toggling the built-in QoL modules, browsing all loaded BepInEx plugins, session-level component switches, and persisted editing/resetting of exposed settings through BepInEx's own serializer.
- Added automatic overlap protection for PeakStatsEx teammate stamina and EasyBackpack, with a preference toggle for users who want the built-in implementations instead.
- Added No Wait: a local late-join detector that revives you beside the nearest living scout when entering an expedition already in progress, including when you are not host.
- Added independent Resurrect Selected, Resurrect Everyone, and Resurrect Self controls using PEAK's native all-client revive RPC; targets do not need the mod.
- Limited No Wait to the initial room-join window and consumed it on normal airport entry or a stable living spawn, preventing it from becoming a permanent auto-revive cheat.
- Upgraded No Wait to wait for stable grounded scouts, support nearest/lowest/checkpoint destinations, halt residual ragdoll velocity, retain reconnect inventory, and restore reconnect statuses, thorns, extra stamina, and petrification.
- Staggered Resurrect Everyone at 1.5-second intervals to reduce synchronization spikes.
- Added Quick Reconnect, which persists the last observed Steam lobby ID and rejoins through PEAK's native lobby lookup and game-version validation flow.
- Added Emergency Recovery controls for last safe ground, nearest safely grounded scout, active checkpoint, flight/velocity stabilization, and reversible local-state restoration.
- Preserved compatible-client protocol v6 because resurrection uses PEAK's existing native synchronization rather than a new mod packet.

## 0.3.5 Beta — 2026-09-07

- Redesigned the F7 overlay around the selected expedition-console concept with a calmer slate/teal palette, compact header targeting, restrained cards, and clearer navigation states.
- Reorganized Player Controls around a large Movement workspace with flight controls first, a dedicated Health & Status panel, and compact Teleport, Visibility & Speed, and Inventory & Reset panels.
- Added a teal hover glow and a short press pulse animation to interactive menu buttons.
- Increased standard Launch strength from 65 to 120 and standard/Off Mountain Ragdoll strength from 35 to 75; all incoming values remain hard-clamped.
- Preserved the compatible-client networking protocol at v6 because this release changes presentation and existing action limits without adding packet types.

## 0.3.0 — 2026-09-07

- Added reversible owner-controlled flight to the Player → Movement card, with a bounded 4–20 m/s speed control, Shift boost, and WASD/Space/Ctrl controls.
- Flight automatically restores each character rigidbody's gravity and velocity limit during disable, player reset, scene changes, lobby exit, plugin shutdown, and emergency cleanup.
- Updated compatible-client networking to protocol v6.

## 0.2.0 — 2026-09-06

- Added bounded, target-only Phantom Pings with breadcrumb-trail, circle, and behind-you patterns, configurable count and interval, explicit cancellation, and reset cleanup.
- Updated compatible-client networking to protocol v5.
- Added compatible-client summit helicopter suppression, propagated through the current protocol; a compatible host is required to suppress the authoritative rescue completion.
- Added the persistent Campfire Reset Trap: the first observed ignition of each campfire warps every available scout back to the first segment start using native PEAK RPCs.
- Added a reusable timed Chaos Combo Builder with selectable Mandrake Rain, knockout, Sky High, Horizontal Yeet, and One Live One steps.
- Added Position Roulette, a randomized no-fixed-point rotation of every available scout's position.
- Added One Live One, which mixes exactly one randomly placed lit dynamite into an otherwise unlit tracked shower.
- Added persistent, reversible local toggles for all cosmetics and all badges without granting Steam/platform achievements.
- Added local badge-sash refresh and network synchronization while All Badges is enabled.
- Added real networked Mandrake Rain above the selected player, using the bounded Item Storm scheduler and cleanup registry.
- Added Wrong Mountain and the general real-item Item Storm preset.

## 0.1.0 — 2026-09-06

- Initial buildable BepInEx 5 project for inspected PEAK 2.4.b assemblies.
- Added F7 control menu, capability/permission display, runtime player/item/audio/prefab discovery, and BepInEx configuration.
- Added validated mod-to-mod Photon messaging with owner/audience execution rules.
- Added reversible player physics, speed, visibility, teleport, knockout, elimination, status, and item actions.
- Ragdoll specifically uses PEAK's native all-client `Fall` and `AddForceAtPosition` RPCs, so the target does not need the mod.
- Added a native **Off Mountain** ragdoll action with terrain-drop sampling and a route-aware fallback direction.
- Added bounded **Sky High** launch using PEAK's native ragdoll/warp RPCs.
- Promoted ordinary Launch and safe Teleport to native mod-free-target actions for both host and non-host callers.
- Added PEAK's master-validated native status path for hosts; non-host status actions retain their compatible-target requirement.
- Added target-following **Dynamite Shower** with count, height, spread, interval, fuse, global-cap, tracking, and owner-cleanup controls.
- Increased Dynamite Shower to 64 per action with a separate configurable active-dynamite cap up to 128.
- Added persistent selected-player targeting for tracked Scoutmasters and Mushroom Zombies.
- Added a bounded native **Horizontal Yeet** ragdoll action and independent scrolling for every menu card.
- Instant Knockout and Eliminate use PEAK's native all-client pass-out/death RPC paths, so they work for non-host callers and unmodded targets.
- Give Item now invokes PEAK's verified `SpawnItemInHand` master-client RPC path directly, matching the working third-party spawner behavior and removing the recipient-mod requirement.
- Added host-authoritative tracked Scoutmaster and Mushroom Zombie ambushes and targeting.
- Added ping placement, renderer-only Mirage Scouts/props/fake enemies, behaviors, fake audio, and presets.
- Ping-placed Luggage, Statue, and Capybara mirages now replicate to the selected compatible viewer or all compatible viewers, with audience-aware cleanup.
- Added bounded Chaos Mode, cleanup/reset lifecycle, diagnostics, and Thunderstore packaging files.
- Left unverified voice, appearance, Looker-spawn, and cloud-hazard features disabled with documented reasons.
