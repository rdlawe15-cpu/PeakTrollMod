using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class CampfireTrollManager
    {
        private readonly ManualLogSource _log;
        private readonly PlayerManager _players;
        private readonly PlayerActions _actions;
        private readonly ModConfig _settings;
        private readonly HashSet<int> _triggered = new HashSet<int>();

        public CampfireTrollManager(ManualLogSource log, PlayerManager players, PlayerActions actions, ModConfig settings)
        { _log = log; _players = players; _actions = actions; _settings = settings; }

        public bool Enabled
        {
            get { return _settings.CampfireResetEnabled.Value; }
            set { _settings.CampfireResetEnabled.Value = value; }
        }

        public void ResetScene() { _triggered.Clear(); }

        public void OnCampfireLit(Campfire campfire)
        {
            if (!Enabled || campfire == null || !_triggered.Add(campfire.GetInstanceID())) return;
            Vector3 start;
            if (!_actions.TryGetStartEnd(false, out start)) { _log.LogWarning("Campfire reset could not resolve the first segment start."); return; }
            IList<PlayerEntry> entries = _players.Entries;
            int moved = 0;
            float radius = entries.Count <= 1 ? 0f : Mathf.Min(2.5f, .55f * entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                PlayerEntry player = entries[i];
                if (player == null || player.Character == null || player.Character.refs == null || player.Character.refs.view == null) continue;
                float angle = entries.Count <= 1 ? 0f : i * Mathf.PI * 2f / entries.Count;
                Vector3 destination = start + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                try { player.Character.refs.view.RPC("WarpPlayerRPC", RpcTarget.All, new object[] { destination, true }); moved++; }
                catch (Exception ex) { _log.LogWarning("Campfire reset warp failed for " + player.Name + ": " + ex.Message); }
            }
            _log.LogInfo("Campfire troll returned " + moved + " scouts to the start.");
        }
    }

    [HarmonyPatch(typeof(Campfire), "Light_Rpc")]
    internal static class CampfireLightPatch
    {
        private static void Postfix(Campfire __instance)
        {
            TrollModPlugin plugin = TrollModPlugin.Instance;
            if (plugin != null && plugin.CampfireTroll != null) plugin.CampfireTroll.OnCampfireLit(__instance);
        }
    }
}
