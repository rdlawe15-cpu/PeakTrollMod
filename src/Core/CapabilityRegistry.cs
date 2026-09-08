using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class CapabilityRegistry
    {
        private readonly Dictionary<FeatureCapability, string> _unavailable = new Dictionary<FeatureCapability, string>();
        private readonly ManualLogSource _log;
        private bool _discovered;

        public CapabilityRegistry(ManualLogSource log) { _log = log; }

        public void Discover()
        {
            Dictionary<FeatureCapability, string> previous = new Dictionary<FeatureCapability, string>(_unavailable);
            _unavailable.Clear();
            Need(FeatureCapability.PlayerDiscovery, typeof(PlayerHandler).GetMethod("GetAllPlayerCharacters") != null, "PlayerHandler.GetAllPlayerCharacters missing");
            Need(FeatureCapability.Ragdoll, ReflectionHelpers.HasMethod(typeof(Character), "AddForceAtPosition", typeof(Vector3), typeof(Vector3), typeof(float)) && ReflectionHelpers.HasMethod(typeof(Character), "Fall", typeof(float), typeof(float)), "native Character.AddForceAtPosition/Fall RPC paths missing");
            Need(FeatureCapability.Launch, ReflectionHelpers.HasMethod(typeof(Character), "AddForceAtPosition", typeof(Vector3), typeof(Vector3), typeof(float)), "native Character.AddForceAtPosition RPC path missing");
            Need(FeatureCapability.Speed, ReflectionHelpers.HasField(typeof(CharacterMovement), "movementModifier"), "CharacterMovement.movementModifier missing");
            Need(FeatureCapability.Flight, ReflectionHelpers.HasField(typeof(CharacterRagdoll), "rigidbodies") && typeof(Rigidbody).GetProperty("linearVelocity") != null && typeof(Rigidbody).GetProperty("useGravity") != null, "character rigidbodies or flight physics properties missing");
            Need(FeatureCapability.Visibility, typeof(Renderer) != null, "Unity Renderer unavailable");
            Need(FeatureCapability.Teleport, typeof(Character).GetMethod("WarpPlayerRPC") != null, "Character.WarpPlayerRPC missing");
            Need(FeatureCapability.StartEndTeleport, typeof(MapHandler).GetField("segments") != null, "MapHandler segment references missing");
            Need(FeatureCapability.Resurrect, ReflectionHelpers.HasMethod(typeof(Character), "RPCA_ReviveAtPosition", typeof(Vector3), typeof(bool), typeof(int)), "Character.RPCA_ReviveAtPosition missing");
            Need(FeatureCapability.QuickReconnect, ReflectionHelpers.HasMethod(typeof(SteamLobbyHandler), "TryJoinLobby", typeof(Steamworks.CSteamID)), "Steam lobby join path missing");
            Need(FeatureCapability.EmergencyRecovery, Available(FeatureCapability.Teleport) && ReflectionHelpers.HasMethod(typeof(CharacterRagdoll), "HaltBodyVelocity", typeof(bool)), "safe warp or rigidbody halt path missing");
            Need(FeatureCapability.Knockout, typeof(Character).GetMethod("PassOutInstantly") != null, "Character.PassOutInstantly missing");
            Need(FeatureCapability.Eliminate, typeof(Character).GetMethod("RPCA_Die") != null, "Character.RPCA_Die missing");
            Need(FeatureCapability.StatusEffects, typeof(CharacterAfflictions).GetMethod("AddStatus") != null, "CharacterAfflictions.AddStatus missing");
            Need(FeatureCapability.GiveItem, ReflectionHelpers.HasMethod(typeof(CharacterItems), "SpawnItemInHand", typeof(string)), "CharacterItems.SpawnItemInHand missing");
            Need(FeatureCapability.SkyLaunch, typeof(Character).GetMethod("WarpPlayerRPC") != null && Available(FeatureCapability.Ragdoll), "native warp/ragdoll RPC paths missing");
            Need(FeatureCapability.HorizonLaunch, typeof(Character).GetMethod("WarpPlayerRPC") != null && Available(FeatureCapability.Ragdoll), "native warp/ragdoll RPC paths missing");
            Need(FeatureCapability.DynamiteShower, HasLoaded(typeof(Dynamite)) && typeof(Dynamite).GetMethod("LightFlare") != null, "no loaded Dynamite item prefab or fuse API");
            Need(FeatureCapability.ItemStorm, HasLoaded(typeof(Item)) && typeof(Item).GetMethod("SetKinematicNetworked") != null, "no loaded network item prefab or physics API");
            Need(FeatureCapability.WrongMountain, Available(FeatureCapability.Teleport) && Available(FeatureCapability.Ragdoll) && Available(FeatureCapability.GiveItem), "native warp, ragdoll, or item RPC path missing");
            Need(FeatureCapability.CosmeticUnlocks, typeof(CustomizationOption).GetProperty("IsLocked") != null, "CustomizationOption.IsLocked missing");
            Need(FeatureCapability.BadgeUnlocks, typeof(BadgeData).GetProperty("IsLocked") != null && typeof(CharacterData).GetMethod("SetBadgeStatus", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null, "badge lock or sash synchronization API missing");
            Need(FeatureCapability.PositionRoulette, Available(FeatureCapability.Teleport), "native warp RPC path missing");
            Need(FeatureCapability.OneLiveOne, Available(FeatureCapability.DynamiteShower), "networked Dynamite spawning or fuse API missing");
            Need(FeatureCapability.ComboBuilder, Available(FeatureCapability.Ragdoll) && Available(FeatureCapability.Knockout) && Available(FeatureCapability.SkyLaunch), "one or more native combo action paths are missing");
            Need(FeatureCapability.CampfireReset, ReflectionHelpers.Method(typeof(Campfire), "Light_Rpc", new Type[] { typeof(bool), typeof(float) }) != null && Available(FeatureCapability.StartEndTeleport) && Available(FeatureCapability.Teleport), "campfire ignition or native start-warp path missing");
            Need(FeatureCapability.HelicopterSuppression, ReflectionHelpers.Method(typeof(PeakHandler), "SummonHelicopter", Type.EmptyTypes) != null && ReflectionHelpers.HasField(typeof(PeakHandler), "summonedHelicopter"), "summit helicopter summon path missing");
            Need(FeatureCapability.Mandrake, Available(FeatureCapability.ItemStorm) && HasLoadedItem("Mandrake"), "no loaded network Mandrake item prefab");
            Need(FeatureCapability.ScoutmasterSpawn, ReflectionHelpers.HasMethod(typeof(Scoutmaster), "SetCurrentTarget", typeof(Character), typeof(float)), "Scoutmaster.SetCurrentTarget missing");
            Need(FeatureCapability.MushroomZombieSpawn, FindZombiePrefab() != null, "no loaded MushroomZombieSpawner prefab reference");
            Need(FeatureCapability.LookerSpawn, false, "no verified network prefab path in PEAK 2.4.b");
            Need(FeatureCapability.PingPlacement, ReflectionHelpers.HasMethod(typeof(PointPinger), "TryGetPingHit", typeof(RaycastHit).MakeByRefType(), typeof(Vector3)), "PointPinger.TryGetPingHit missing");
            Need(FeatureCapability.PhantomPings, ReflectionHelpers.HasMethod(typeof(PointPinger), "ReceivePoint_Rpc", typeof(Vector3), typeof(Vector3)) && ReflectionHelpers.HasField(typeof(PointPinger), "character"), "PointPinger.ReceivePoint_Rpc or character reference missing");
            Need(FeatureCapability.MirageScout, true, "");
            Need(FeatureCapability.MirageLuggage, HasLoaded(typeof(Luggage)), "no loaded Luggage visual source");
            Need(FeatureCapability.MirageStatue, HasLoaded(typeof(Peak.ScoutStatue)), "no loaded ScoutStatue visual source");
            Need(FeatureCapability.MirageCapybara, HasLoaded(typeof(Capybara)), "no loaded Capybara visual source");
            Need(FeatureCapability.FakeEnemyScoutmaster, HasLoaded(typeof(Scoutmaster)), "no loaded Scoutmaster visual source");
            Need(FeatureCapability.FakeEnemyZombie, HasLoaded(typeof(MushroomZombie)), "no loaded MushroomZombie visual source");
            Need(FeatureCapability.FakeEnemyLooker, HasLoaded(typeof(Looker)), "no loaded Looker visual source");
            Need(FeatureCapability.FakeAudio, true, "");
            Need(FeatureCapability.ZombieTargeting, ReflectionHelpers.HasMethod(typeof(MushroomZombie), "SetCurrentTarget", typeof(Character), typeof(float)), "MushroomZombie.SetCurrentTarget missing");
            Need(FeatureCapability.AppearanceSwap, ReflectionHelpers.HasMethod(typeof(CharacterCustomization), "OnPlayerDataChange", typeof(PersistentPlayerData)), "CharacterCustomization.OnPlayerDataChange missing");
            Need(FeatureCapability.VoiceSwap, false, "voice routing ownership and group mutation are not safely exposed");
            Need(FeatureCapability.IncomingVoiceMute, ReflectionHelpers.HasField(typeof(CharacterVoiceHandler), "<audioSource>k__BackingField") || ReflectionHelpers.HasField(typeof(CharacterVoiceHandler), "m_source"), "Character voice AudioSource missing");
            Need(FeatureCapability.TalkWhileKnockedOut, false, "no supported voice privilege hook verified");
            Need(FeatureCapability.PoisonCloud, false, "no verified network hazard prefab path");
            Need(FeatureCapability.SporeCloud, false, "no verified network hazard prefab path");
            if (!_discovered || previous.Count != _unavailable.Count) _log.LogInfo("Capability discovery complete: " + (Enum.GetValues(typeof(FeatureCapability)).Length - _unavailable.Count) + " available, " + _unavailable.Count + " unavailable.");
            if (!_discovered) foreach (KeyValuePair<FeatureCapability, string> pair in _unavailable) _log.LogWarning("Capability " + pair.Key + " disabled: " + pair.Value);
            _discovered = true;
        }

        private void Need(FeatureCapability feature, bool condition, string reason) { if (!condition) _unavailable[feature] = reason; }
        private static bool HasLoaded(Type type) { return Resources.FindObjectsOfTypeAll(type).Length > 0; }
        private static bool HasLoadedItem(string token)
        {
            Item[] items = Resources.FindObjectsOfTypeAll<Item>();
            for (int i = 0; i < items.Length; i++) if (items[i] != null && items[i].gameObject != null && items[i].gameObject.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
        private static MushroomZombie FindZombiePrefab()
        {
            MushroomZombieSpawner[] spawners = Resources.FindObjectsOfTypeAll<MushroomZombieSpawner>();
            for (int i = 0; i < spawners.Length; i++) if (spawners[i] != null && spawners[i].mushroomZombiePrefab != null) return spawners[i].mushroomZombiePrefab;
            return null;
        }
        public bool Available(FeatureCapability feature) { return !_unavailable.ContainsKey(feature); }
        public string Reason(FeatureCapability feature) { string value; return _unavailable.TryGetValue(feature, out value) ? value : string.Empty; }
        public void RefreshDynamic() { Discover(); }
    }
}
