# Changelog

## 0.4.5 — 2026-09-10

- Added Mind Control for one selected scout, with full acknowledged input relaying for compatible clients plus a host-only native puppet fallback for unmodded scouts. The fallback provides bounded movement, sprint, jump, and crouch while leaving the target's own local controls active. Sessions include a third-person camera, reliable cleanup, and F6 release; pause, voice, and non-game computer input are explicitly excluded.
- Updated compatible-client networking to protocol v9 for validated Mind Control start, acknowledgement, input-stream, and stop commands.
- Fixed Anti-Mirages' first-pass activation guard and expanded the local filter beyond the Mesa: it now immediately suppresses full native `MirageLuggage` hierarchies, `Mirage` particles and referenced objects, and PEAK Troll Mod-created mirages wherever they appear, while retaining reversible renderer restoration.
- Added Summit Saboteur, a cancellable host-only final-approach trap with adjustable trigger distance, stamina-lock duration, and horizontal launch distance. Its staged sequence drops the selected scout's held slot item, maxes Hunger, briefly suppresses stamina, spawns a tracked targeted zombie behind them, displays a fake recovery notice on compatible targets, and launches them toward the horizon.
- Updated compatible-client networking to protocol v8 for the bounded owner-side Summit Saboteur stamina/notification step.
- Added Amplify Hunger Rates controls with an adjustable 2×–20× multiplier and explicit normal-rate reset. Compatible owners temporarily scale PEAK's native `hungerPerSecond` only during `UpdateNormalStatuses`; the host can additionally affect unmodded targets by accumulating the same extra rate and sending bounded native Hunger-status pulses.
- Updated compatible-client networking to protocol v7 for the validated hunger-amplifier command before the subsequent v8 Summit Saboteur addition.
- Added an opt-in Campfire Death Trap that uses PEAK's native synchronized death RPC to eliminate the igniter and every other living scout within a configurable 3–50 m radius. It is mutually exclusive with Campfire Reset in the menu and takes precedence if both settings are enabled externally.

### Earlier prerelease contents

- Added **Far Horizon**, a selectable 25–300 m horizontal counterpart to Sky High that chooses an outward route direction, finds a clear endpoint offset, and combines PEAK's native ragdoll, force, and warp RPCs.
- Upgraded Infinite Rescue Claw Reach with a local, bounded distant-wall zip that extends the one-second wall timeout only while travelling, releases near the anchor, ignores friend/item/empty hits, and restores cached ragdoll physics on every exit path.
- Expanded Immortality to continuously clear every negative stamina-bar condition, including carried-weight encumbrance, incremental status buildup, and physical thorns, while preserving the separate Infinite Stamina toggle.
- Fixed PEAK's dynamic-font text-buffer corruption that could repeat the current tab's final sentence across every control or leave every caption blank. The F7 menu now uses Unity's reliable native IMGUI font and normalizes/restores all global GUI color state.
- Added Smart Climb Forecast, a read-only local ledge probe that estimates climb height, time, stamina cost, remaining reserve, and blocked/unsafe outcomes from PEAK's live climb and surface values.
- Added Party Supply Advisor with synchronized pocket/backpack summaries and recommendations for missing recovery, food, climbing, mobility, antidote, distribution, and single-carrier risks.
- Added Hotkey Conflict Doctor for exact enabled-plugin `KeyboardShortcut`/`KeyCode` collisions, with explicit-click reassignment to an unused suggested key through the owning configuration.
- Added reversible Infinite Rescue Claw Reach for the locally held claw, including upward/downward range restoration and automatic yielding to recognized standalone infinite-range rescue-hook mods.
- Added the Exo 2 and Inter font assets for future Unity-native font packaging; PEAK's current legacy IMGUI path uses its native font because runtime TTF registration corrupts shared text rendering.
- Added a fully client-side held-item stamina preview that forecasts condition-bar capacity, individual affliction changes, extra stamina, and pass-out risk before consumption.
- Added automatic overlap protection that yields the built-in stamina forecast whenever the standalone Effect Preview plugin is enabled.
- Added independent self-only Immortality and Infinite Stamina toggles under Player → Health & Status, with immediate opt-out and no permanent stat changes.
- Added Real Luggage Directions, a compact local compass and distance readout sourced from PEAK's live unopened-luggage registry while explicitly filtering native and mod-created mirages.
- Added a reversible client-only Mesa Anti-Mirages option that suppresses verified `MirageLuggage` and `Mirage` renderers only in the Mesa and restores their cached states when inactive.
- Made the F7 menu non-modal for character and flight movement, while named text fields temporarily block gameplay input until Enter, an outside click, or menu closure releases focus.
- Made Escape close the F7 overlay as PEAK's pause menu opens.
- Added a dedicated searchable Item Spawner page backed by PEAK's live runtime catalog, with quantity selection, give-to-target/self actions, tracked ground spawning, refresh controls, and no bundled game assets.
- Added safe Effect Preview cards that show the selected target, authority context, live effect parameters, and a non-executing animation before routing to the real controls.
- Added an optional Unlimited Lobby mode for newly hosted 4–30 player rooms, plus host-owned campfire food and backpack scaling for extra and late-joining scouts.
- Added automatic overlap protection that disables the built-in unlimited-lobby patches whenever the standalone PEAK Unlimited plugin is loaded.
- Added persistent Favorites & Recent Actions for runtime items and effect previews.
- Added a persistent one-pixel outline to every menu button, with authority-aware color accents that brighten into the existing hover glow.
- Isolated the menu from shared Unity IMGUI state so another UI cannot leave every button disabled, forced the F7 panel to a stable foreground depth, and added automatic style-texture rebuilding after scene changes or draw failures.
- Kept compatible-client networking at protocol v6; the new item and lobby behavior uses verified native PEAK/Photon paths and the preview/shortcut state is local.

## 0.4.0 — 2026-09-07

- Added Persistent Player Preferences keyed by Steam user ID, including local alias/color, voice volume, mute, and exclusion from random troll targeting.
- Added a Lobby Readiness page with explicit compatible-client ready state, loaded/loading fallback for unmodded players, mod/version/protocol status, host identity and migration count, player count, and one-click lobby-code copying.
- Expanded Emergency Recovery with return-to-expedition-start and local-only tracked-effect cleanup alongside safe-ground, scout, checkpoint, physics, and self-restoration controls.
- Added accessibility controls for menu text size, high-contrast status colors, final-output camera-shake scaling, reduced repeated Phantom Pings/menu pulses, toggle-or-hold menu behavior, and configurable shortcuts.
- Extended the existing protocol-v6 Hello metadata without adding a new packet type, preserving protocol compatibility while current clients exchange readiness and exact version information.
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
