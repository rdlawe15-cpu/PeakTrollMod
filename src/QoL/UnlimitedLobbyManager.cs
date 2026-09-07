using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class UnlimitedLobbyManager
    {
        private sealed class ProvisionState { public int Food; public int Backpacks; }
        private readonly ManualLogSource _log;
        private readonly ModConfig _settings;
        private readonly PlayerActions _actions;
        private readonly SpawnManager _spawns;
        private readonly Dictionary<int, ProvisionState> _provisions = new Dictionary<int, ProvisionState>();
        private float _nextScan;
        internal static UnlimitedLobbyManager Current;

        public UnlimitedLobbyManager(ManualLogSource log, ModConfig settings, PlayerActions actions, SpawnManager spawns)
        {
            _log = log; _settings = settings; _actions = actions; _spawns = spawns; Current = this;
        }

        public bool StandaloneDetected
        {
            get
            {
                foreach (KeyValuePair<string, BepInEx.PluginInfo> pair in Chainloader.PluginInfos)
                {
                    BepInEx.PluginInfo info = pair.Value;
                    if (info == null || info.Metadata == null || info.Metadata.GUID == TrollModPlugin.Guid) continue;
                    string identity = (info.Metadata.GUID + " " + info.Metadata.Name + " " + (info.Instance == null ? string.Empty : info.Instance.GetType().Assembly.GetName().Name)).Replace(" ", string.Empty);
                    if (identity.IndexOf("PEAKUnlimited", StringComparison.OrdinalIgnoreCase) >= 0) return true;
                }
                return false;
            }
        }

        public bool Active { get { return _settings.UnlimitedLobbyEnabled.Value && !StandaloneDetected; } }
        public int MaxPlayers { get { return Mathf.Clamp(_settings.UnlimitedLobbyMaxPlayers.Value, 4, 30); } }
        public string Status { get { return StandaloneDetected ? "Yielding to standalone PEAK Unlimited" : Active ? "Enabled for the next hosted lobby" : "Off"; } }

        public void Tick()
        {
            if (!Active || !_settings.UnlimitedLobbyScaleSupplies.Value || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null) return;
            if (Time.unscaledTime < _nextScan) return;
            _nextScan = Time.unscaledTime + 2f;
            int extraPlayers = Mathf.Max(0, PhotonNetwork.CurrentRoom.PlayerCount - 4);
            Campfire[] campfires = Resources.FindObjectsOfTypeAll<Campfire>();
            for (int i = 0; i < campfires.Length; i++)
            {
                Campfire campfire = campfires[i];
                if (campfire == null || campfire.gameObject == null || !campfire.gameObject.scene.IsValid() || !campfire.isActiveAndEnabled) continue;
                if (!string.IsNullOrEmpty(campfire.nameOverride) && campfire.nameOverride.IndexOf("PORTABLE", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                ProvisionState state;
                int id = campfire.GetInstanceID();
                if (!_provisions.TryGetValue(id, out state)) { state = new ProvisionState(); _provisions[id] = state; }
                if (_settings.UnlimitedLobbyExtraFood.Value) SpawnMissing(campfire, FindItem("Marshmallow"), extraPlayers, ref state.Food, 2.7f);
                int backpacks = _settings.UnlimitedLobbyExtraBackpacks.Value ? Mathf.CeilToInt(extraPlayers / 4f) : 0;
                SpawnMissing(campfire, FindItem("Backpack"), backpacks, ref state.Backpacks, 3.6f);
            }
        }

        private void SpawnMissing(Campfire campfire, string itemName, int desired, ref int spawned, float radius)
        {
            if (desired <= spawned || string.IsNullOrEmpty(itemName)) return;
            int missing = desired - spawned;
            for (int i = 0; i < missing; i++)
            {
                float angle = ((spawned + i) * 137.5f) * Mathf.Deg2Rad;
                Vector3 position = campfire.transform.position + new Vector3(Mathf.Cos(angle) * radius, 1.1f, Mathf.Sin(angle) * radius);
                ActionResult result = _spawns.SpawnWorldItem(itemName, position);
                if (!result.Success) { _log.LogWarning("Unlimited lobby provision spawn stopped: " + result.Message); return; }
                spawned++;
            }
        }

        private string FindItem(string token)
        {
            for (int i = 0; i < _actions.Items.Count; i++)
            {
                Item item = _actions.Items[i];
                if (item != null && item.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) return item.name;
            }
            return string.Empty;
        }

        public void ResetScene() { _provisions.Clear(); _nextScan = 0f; }
    }

    [HarmonyPatch]
    internal static class UnlimitedMaxPlayersPatch
    {
        private static MethodBase TargetMethod()
        {
            Type type = AccessTools.TypeByName("Peak.Network.NetworkingUtilities");
            return type == null ? null : AccessTools.PropertyGetter(type, "MAX_PLAYERS");
        }

        private static bool Prefix(ref int __result)
        {
            UnlimitedLobbyManager manager = UnlimitedLobbyManager.Current;
            if (manager == null || !manager.Active) return true;
            __result = manager.MaxPlayers;
            return false;
        }
    }

    [HarmonyPatch]
    internal static class UnlimitedHostRoomOptionsPatch
    {
        private static MethodBase TargetMethod()
        {
            Type type = AccessTools.TypeByName("Peak.Network.NetworkingUtilities");
            return type == null ? null : AccessTools.Method(type, "HostRoomOptions", new Type[0]);
        }

        private static bool Prefix(ref RoomOptions __result)
        {
            UnlimitedLobbyManager manager = UnlimitedLobbyManager.Current;
            if (manager == null || !manager.Active) return true;
            __result = new RoomOptions { IsVisible = false, MaxPlayers = (byte)manager.MaxPlayers, PublishUserId = true };
            return false;
        }
    }
}
