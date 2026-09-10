using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace PeakTrollMod
{
    internal struct HungerRateState
    {
        public bool Applied;
        public float Original;
    }

    internal sealed class HungerAmplifierManager
    {
        private sealed class HostJob
        {
            public float Multiplier;
            public float Pending;
        }

        private readonly ManualLogSource _log;
        private readonly PlayerManager _players;
        private readonly PlayerActions _actions;
        private readonly Dictionary<int, HostJob> _hostJobs = new Dictionary<int, HostJob>();
        private bool _enabled;
        private float _multiplier = 5f;

        public HungerAmplifierManager(ManualLogSource log, PlayerManager players, PlayerActions actions)
        {
            _log = log;
            _players = players;
            _actions = actions;
        }

        public ActionResult Set(bool enabled, float multiplier)
        {
            _multiplier = Mathf.Clamp(multiplier, 2f, 20f);
            _enabled = enabled;
            _log.LogInfo("Hunger rate amplifier " + (enabled ? "set to " + _multiplier.ToString("0.0") + "x." : "disabled."));
            return ActionResult.Ok(enabled ? "Natural hunger rate amplified to " + _multiplier.ToString("0.0") + "x." : "Natural hunger rate restored.");
        }

        public ActionResult SetHostFallback(PlayerEntry target, bool enabled, float multiplier)
        {
            if (!_players.IsHost) return ActionResult.Fail("An unmodded target requires the current Photon host to amplify hunger.");
            if (target == null || target.Character == null || target.Character.refs == null || target.Character.refs.afflictions == null) return ActionResult.Fail("Target hunger state is unavailable.");
            if (!enabled)
            {
                _hostJobs.Remove(target.ActorNumber);
                return ActionResult.Ok("Stopped host-native hunger amplification for " + target.Name + ".");
            }

            HostJob job;
            if (!_hostJobs.TryGetValue(target.ActorNumber, out job))
            {
                job = new HostJob();
                _hostJobs[target.ActorNumber] = job;
            }
            job.Multiplier = Mathf.Clamp(multiplier, 2f, 20f);
            job.Pending = 0f;
            return ActionResult.Ok("Host-native hunger amplification set to " + job.Multiplier.ToString("0.0") + "x for " + target.Name + "; target mod not required.");
        }

        public void Tick()
        {
            if (_hostJobs.Count == 0) return;
            if (!PhotonNetwork.InRoom || !_players.IsHost) { _hostJobs.Clear(); return; }

            float deltaTime = Mathf.Clamp(Time.deltaTime, 0f, .25f);
            if (deltaTime <= 0f) return;
            List<int> actors = new List<int>(_hostJobs.Keys);
            for (int i = 0; i < actors.Count; i++)
            {
                int actor = actors[i];
                HostJob job;
                if (!_hostJobs.TryGetValue(actor, out job)) continue;
                PlayerEntry target = _players.Find(actor);
                if (target == null) { _hostJobs.Remove(actor); continue; }
                if (target.Character == null || target.Character.data == null || target.Character.data.dead || target.Character.refs == null || target.Character.refs.afflictions == null) continue;

                float nativeRate = target.Character.refs.afflictions.hungerPerSecond;
                if (float.IsNaN(nativeRate) || float.IsInfinity(nativeRate) || nativeRate <= 0f) continue;
                job.Pending = Mathf.Min(.25f, job.Pending + nativeRate * (Mathf.Clamp(job.Multiplier, 2f, 20f) - 1f) * deltaTime);
                if (job.Pending < .01f) continue;

                float amount = Mathf.Min(job.Pending, .05f);
                ActionResult result = _actions.ApplyStatusAsHost(target, (int)CharacterAfflictions.STATUSTYPE.Hunger, amount);
                if (result.Success) job.Pending = Mathf.Max(0f, job.Pending - amount);
                else job.Pending = 0f;
            }
        }

        public HungerRateState Begin(CharacterAfflictions afflictions)
        {
            HungerRateState state = new HungerRateState();
            if (!_enabled || afflictions == null || afflictions.character == null || !afflictions.character.IsLocal) return state;
            float original = afflictions.hungerPerSecond;
            if (float.IsNaN(original) || float.IsInfinity(original) || original <= 0f) return state;
            state.Applied = true;
            state.Original = original;
            afflictions.hungerPerSecond = original * Mathf.Clamp(_multiplier, 2f, 20f);
            return state;
        }

        public void Restore(CharacterAfflictions afflictions, HungerRateState state)
        {
            if (state.Applied && afflictions != null) afflictions.hungerPerSecond = state.Original;
        }

        public void Reset()
        {
            _enabled = false;
            _multiplier = 5f;
            _hostJobs.Clear();
        }

        public void ResetTarget(int actor) { _hostJobs.Remove(actor); }
    }

    [HarmonyPatch(typeof(CharacterAfflictions), "UpdateNormalStatuses")]
    internal static class HungerRatePatch
    {
        private static void Prefix(CharacterAfflictions __instance, ref HungerRateState __state)
        {
            TrollModPlugin plugin = TrollModPlugin.Instance;
            __state = plugin == null || plugin.HungerAmplifier == null ? new HungerRateState() : plugin.HungerAmplifier.Begin(__instance);
        }

        private static Exception Finalizer(CharacterAfflictions __instance, HungerRateState __state, Exception __exception)
        {
            TrollModPlugin plugin = TrollModPlugin.Instance;
            if (plugin != null && plugin.HungerAmplifier != null) plugin.HungerAmplifier.Restore(__instance, __state);
            return __exception;
        }
    }
}
