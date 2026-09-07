# Development Notes — PEAK 2.4.b

Inspection date: 2026-09-06  
Installed version file: `2.4.b`, build `3e62ee214`  
Primary assembly: `PEAK_Data/Managed/Assembly-CSharp.dll`  
Networking assemblies: `PhotonRealtime.dll`, `PhotonUnityNetworking.dll`, `Photon3Unity3D.dll`  
BepInEx core: BepInEx 5 with Harmony/HarmonyX interop present

Assembly metadata was inspected with the BepInEx-shipped Mono.Cecil. The implementation only names members/prefabs found in this installed build. Compilation succeeded against those exact assemblies. In-game multiplayer behavior has **not** been claimed as tested from this development environment.

## Verified runtime APIs

### Players and character state

- `PlayerHandler.GetAllPlayerCharacters()` is the static player-character source. `PlayerHandler` also exposes actor/player lookup methods.
- `Character` derives from `Photon.Pun.MonoBehaviourPun`. Relevant members include `IsLocal`, `Center`, `Head`, `characterName`, `refs`, `data`, and `WarpPlayerRPC(Vector3,bool)`.
- `Character.CharacterRefs` contains `movement`, `ragdoll`, `items`, `afflictions`, `voice`, `animator`, `mainRenderer`, `customization`, `view`, body parts, and the rendered hierarchy.
- `CharacterData` contains `passedOut`, `fullyPassedOut`, `dead`, movement/look data, held item data, and `lookDirection_Flat`.
- `CharacterRagdoll` contains registered body parts/rigidbodies. Internal `Character.AddForce(Vector3,float,float)` iterates body parts and calls `Bodypart.AddForce`.
- Internal `Character.Fall(float,float)` sends the `[PunRPC] RPCA_Fall` call to `RpcTarget.All` and contains no caller-ownership gate. Internal `Character.AddForceAtPosition(Vector3,Vector3,float)` likewise sends `[PunRPC] RPCA_AddForceAtPosition` to all clients with no caller-ownership gate. Ragdoll therefore uses these exact native game paths and does **not** require the target to install this mod. Force, radius, and duration remain hard-clamped.
- Ordinary Launch now uses that same native `AddForceAtPosition` path rather than the owner-local `Character.AddForce` method, removing its host and target-mod requirements.
- **Off Mountain** samples 24 horizontal directions at 3 m intervals using ordinary world raycasts, selects the nearby direction with the strongest ground loss, and otherwise pushes away from the centroid of PEAK's loaded map-segment reconnect points. It uses the same bounded native ragdoll RPC path.
- Public `PassOutInstantly()` sets the pass-out value and sends `[PunRPC] RPCA_PassOut` to all clients on that character view. Neither path checks host/caller ownership, so Instant Knockout is available to non-host callers and the target does not need the mod.
- `[PunRPC] Character.RPCA_Die()` is the normal all-client death handler: it marks the character dead, unequips/drops their items, handles skeleton visuals, warps the body to the death position, checks the end game, and emits the death event. It contains no host/caller-ownership gate. Eliminate calls this native RPC on the selected character view, so non-host callers work and the target does not need the mod.

### Movement and teleportation

- `CharacterMovement.movementModifier` is a public multiplier and is cached before modification.
- Flight runs only on the selected character owner's compatible client. It caches each registered ragdoll rigidbody's `useGravity` and `maxLinearVelocity`, drives bounded camera-relative velocity during `FixedUpdate`, and restores the cached values on disable or any global reset path.
- `Character.WarpPlayerRPC(Vector3,bool)` is the game RPC used for teleportation. The installed `SimpleTeleport` mod independently invokes this RPC on the moving character's `PhotonView`, corroborating the call shape.
- `MapHandler.segments` contains `MapHandler.MapSegment.reconnectSpawnPos`; the first and final loaded segments are used for start/end destinations instead of hard-coded coordinates.
- Safe teleport probes ground with `Physics.Raycast` and tests body clearance with `Physics.CheckSphere` before sending the warp.
- `[PunRPC] Character.WarpPlayerRPC(Vector3,bool)` delegates to PEAK's local-owner warp routine and has no caller/host gate. Sky High combines the verified fall/force calls with this RPC and clamps requested height to 25–300 m.

### Status effects

- `CharacterAfflictions.AddStatus(STATUSTYPE,float,bool,bool,bool,bool)` and `SetStatus(STATUSTYPE,float,bool)` are present.
- `[PunRPC] RPC_ApplyStatusesFromFloatArray(float[],PhotonMessageInfo)` explicitly accepts only the master client. The mod uses this validated native route when it is host (except Weight, Thorns, and Arrow, which PEAK deliberately excludes); non-host callers continue to require a compatible target owner.
- Discovered `STATUSTYPE` values: `Injury`, `Hunger`, `Cold`, `Poison`, `Crab`, `Curse`, `Drowsy`, `Weight`, `Hot`, `Thorns`, `Spores`, `Web`, `Arrow`, `Petrify`, `FlyTrap`.
- The game method enforces ownership/invincibility/status locks and clamps against its own status caps. The mod additionally clamps requested amounts to 0.01–1.0 and tracks only mod-applied statuses for reset.

### Items and held items

- `ItemDatabase` exposes item lookup/load methods, but the mod uses loaded `Item` objects to build a runtime catalog resiliently.
- Internal `CharacterItems.SpawnItemInHand(string)` sends `RPC_SpawnItemInHandMaster` to the master client. `ItemSpawnerEnhanced` installed locally uses the same method via reflection.
- Give Item invokes that verified method directly on the selected character's `CharacterItems`, so PEAK performs the room item spawn through its native master-client RPC. The caller and recipient do not need host authority, and the recipient does not need this mod.
- `Mandrake` is a real `ItemComponent`; its item prefab is selected from the runtime item catalog by name. No audio is extracted or redistributed.
- `Dynamite` is a real `ItemComponent`. `ItemDatabase.Add(Item,Vector3)` verifies PEAK's `0_Items/<item name>` Photon prefab convention without a master-client check, while `Dynamite.LightFlare()` broadcasts the real `SetFlareLitRPC`. Dynamite Shower creates caller-owned network items, optionally lights their genuine fuses, and tracks only those returned objects for owner cleanup.
- `Player.AddItem`, `Player.EmptySlot`, `CharacterItems.DestroyHeldItemRpc`, and item-slot structures exist, but destructive replacement is disabled because a lossless atomic replacement path was not verified.

### Pings

- `PointPinger.DoPing()` calls private `TryGetPingHit(out RaycastHit, Vector3)`, then sends the normal `ReceivePoint_Rpc` with hit point/normal.
- Placement mode patches only `DoPing`, invokes the real hit-test, sends the prop description to the selected compatible viewer (or every compatible viewer), consumes a successful placement ping, and automatically disarms unless Multi-Place is enabled. Each recipient reconstructs a renderer-only prop from its loaded PEAK visual source. When placement is inactive or hit-testing fails, normal ping behavior is untouched.
- Phantom Pings ask only the compatible target owner to invoke the verified local `ReceivePoint_Rpc(Vector3,Vector3)` display path. Breadcrumb, circle, and behind-you positions are ground-probed; each sequence is clamped to 3–12 pings and 0.35–3 second intervals, and a new sequence replaces the previous one.

### Enemies and spawning

- `ScoutmasterSpawner.SpawnScoutmaster()` explicitly checks `PhotonNetwork.IsMasterClient` and calls `PhotonNetwork.InstantiateRoomObject("Character_Scoutmaster", ...)`. The exact prefab path is therefore verified.
- `Scoutmaster.SetCurrentTarget(Character,float)` and `RPCA_SetCurrentTarget(int,float)` exist. The mod invokes targeting only on tracked mod spawns and only as host, stores the selected target actor in its private spawn registry, and refreshes the forced target every ten seconds.
- `MushroomZombieSpawner` contains public `mushroomZombiePrefab`; its `Spawn()` checks master authority and calls `PhotonNetwork.Instantiate(prefab.gameObject.name, ...)`.
- `MushroomZombie.SetCurrentTarget(Character,float)` exists. A live spawner is required to resolve the real prefab name; otherwise the capability stays disabled.
- `Looker` exposes activation/switch RPCs, but no stable Photon prefab path was found. Genuine Looker spawn is unsupported.
- `Scoutmaster`, `MushroomZombie`, and `Looker` render hierarchies can be used as safe runtime sources for local, component-free visual puppets.

### World objects and mirages

- Real types discovered: `Luggage`, `MirageLuggage`, `Mirage`, the game's own `MirageManager`, `Capybara`, `Peak.ScoutStatue`, and `Peak.PropSpawner_AmuletStatues`.
- This project intentionally names its manager inside the `PeakTrollMod` namespace to avoid collision with the game's global `MirageManager`.
- Visual extraction reconstructs transforms and copies `MeshFilter`, `MeshRenderer`, `SkinnedMeshRenderer`, shared runtime materials/meshes, bone mappings, and safe Animator controller/avatar data. It never clones the source GameObject or its gameplay/network components.
- No stable poison-cloud or spore-cloud Photon prefab paths were found. `StatusEmitter` and `StatusField` exist, but fabricating hazards from component guesses would not meet the reliability requirement.

### Audio and voice

- `CharacterVoiceHandler` owns a Photon Voice recorder, voice `AudioSource`, mixer routing, privilege checks, and `Peak.CharacterVoiceTransformProvider`.
- `VoiceClientHandler` initializes the Photon Voice connection/recorder.
- No supported identity-safe source rerouting contract was verified, so Voice Swap and Talk While Knocked Out are disabled.
- Fake cues use `Resources.FindObjectsOfTypeAll<AudioClip>()` and temporary `AudioSource` objects. Clip names are validated against the receiving client's loaded catalog.

## Authority classification

| Feature | UI badge | Enforcement |
|---|---|---|
| Local menu, discovery, local cleanup | 🟢 Anyone | Client-only |
| Ragdoll | 🟢 Anyone | Calls PEAK's native `Fall` and `AddForceAtPosition` methods, which broadcast their verified game `[PunRPC]` methods; target mod not required |
| Instant Knockout / Eliminate | 🟢 Anyone | Calls PEAK's native all-client `RPCA_PassOut` / `RPCA_Die` path; no host or target mod required |
| Give item | 🟢 Anyone | Calls PEAK's native `SpawnItemInHand` → master-client RPC path; target mod not required |
| Sky High / Dynamite Shower | 🟢 Anyone | Native character RPCs and caller-owned PEAK item prefabs; target mod and host authority not required |
| Launch / teleport | 🟢 Anyone | Native all-client character force/warp RPCs; host and target mod not required |
| No Wait / resurrection | 🟢 Anyone | Native `RPCA_ReviveAtPosition` broadcast clears death/pass-out state and warps the target; host and target mod not required |
| Speed / Flight | 🟡 Everyone Needs Mod | Request sent only to compatible target owner; flight caches and restores registered rigidbody gravity/velocity limits |
| Statuses | 🔒 Host native / 🟡 non-host | PEAK validates the host sender natively; otherwise a compatible target owner executes the normal path |
| Visibility | 🟡 Everyone Needs Mod | Compatible observers apply/restore cached renderer state locally |
| Mirage Scout, props, fake enemies, fake sound | 🟡 Everyone Needs Mod | Intended victim renders/plays locally; no real identity/entity created |
| Phantom Pings | 🟡 Everyone Needs Mod | Compatible target owner renders a bounded local decoy-ping sequence; no room-wide ping RPC is sent |
| Scoutmaster/Zombie spawn and tracked enemy targeting | 🔒 Host Only | `PhotonNetwork.IsMasterClient` checked before verified spawning/targeting paths |
| Genuine Looker, voice features, appearance swap, hazard clouds | ⚠ Unsupported | Disabled; no request is sent |

## Network validation

- Custom Photon event code 197, exact protocol 6 / mod version `0.4.0` development.
- Sender must resolve to a player in the current room.
- Target actor must resolve to a participating character.
- Command enum, argument count/type, enum range, string length, position type, speed, force, duration, volume, status, and object limits are checked or clamped.
- Unknown, malformed, or version-mismatched messages are rejected and logged.
- No network logic lives in UI rendering code; UI calls the network abstraction.

## Capability behavior

Startup checks cache reflected methods/fields. Scene/runtime refresh discovers loaded items, clips, visual sources, and the live Mushroom Zombie prefab source. Missing capability buttons disable automatically and show the reason. Reflection is cached and never performed per frame.

## Remaining in-game validation checklist

- Verify force magnitudes on each biome/latency profile and adjust the existing hard caps downward if needed.
- Confirm `WarpPlayerRPC` behavior for every ownership/host migration combination.
- Validate status synchronization on four-client lobbies.
- Confirm the receiving client has every requested item prefab loaded before master spawning.
- Exercise host migration with tracked room objects; cleanup remains host-only.
- Inspect Mirage bone/animator alignment for every cosmetic and enemy variant.
- Verify ping-placed props, Phantom Ping patterns/cancellation, flight controls/restoration, and audience cleanup with two-, three-, and four-client protocol-6 lobbies.
- Validate lit/unlit Dynamite Shower timing, cleanup, and the 300 m Sky High ceiling across each biome in a private multiplayer lobby.
- Confirm Photon custom event code 198 does not conflict with the final dependency set.
- Profile a full 24-object mirage cap and verify allocations during creation only.

Do not move an item from this checklist to “confirmed working” until exercised in the live game with logs and multiple clients.
