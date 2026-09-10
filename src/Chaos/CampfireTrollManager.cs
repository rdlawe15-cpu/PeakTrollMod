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
            set
            {
                _settings.CampfireResetEnabled.Value = value;
                if (value) _settings.CampfireDeathTrapEnabled.Value = false;
            }
        }

        public bool DeathTrapEnabled
        {
            get { return _settings.CampfireDeathTrapEnabled.Value; }
            set
            {
                _settings.CampfireDeathTrapEnabled.Value = value;
                if (value) _settings.CampfireResetEnabled.Value = false;
            }
        }

        public float DeathRadius
        {
            get { return Mathf.Clamp(_settings.CampfireDeathRadius.Value, 3f, 50f); }
            set { _settings.CampfireDeathRadius.Value = Mathf.Clamp(value, 3f, 50f); }
        }

        public void ResetScene() { _triggered.Clear(); }

        public void OnCampfireLit(Campfire campfire)
        {
            if (campfire == null || (!Enabled && !DeathTrapEnabled) || !_triggered.Add(campfire.GetInstanceID())) return;
            if (DeathTrapEnabled)
            {
                EliminateNearby(campfire);
                return;
            }

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

        private void EliminateNearby(Campfire campfire)
        {
            Vector3 center = campfire.transform.position;
            if (!PlayerActions.Finite(center)) { _log.LogWarning("Campfire death trap received an invalid campfire position."); return; }

            float radius = DeathRadius;
            float radiusSquared = radius * radius;
            IList<PlayerEntry> entries = _players.Entries;
            int eliminated = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                PlayerEntry player = entries[i];
                if (player == null || player.Character == null || player.Character.data == null || player.Character.data.dead) continue;
                Vector3 position = player.Character.Center;
                if (!PlayerActions.Finite(position) || (position - center).sqrMagnitude > radiusSquared) continue;

                ActionResult result = _actions.EliminateLocal(player);
                if (result.Success) eliminated++;
                else _log.LogWarning("Campfire death trap could not eliminate " + player.Name + ": " + result.Message);
            }
            _log.LogInfo("Campfire death trap eliminated " + eliminated + " scouts within " + radius.ToString("0.#") + " m.");
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
