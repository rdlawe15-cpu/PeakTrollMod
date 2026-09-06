using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class HelicopterTrollManager
    {
        private readonly ManualLogSource _log;
        private readonly ModConfig _settings;
        private readonly HashSet<int> _remoteSuppressors = new HashSet<int>();
        private readonly FieldInfo _summonedField;
        private PeakHandler _suppressedHandler;
        private float _nextAdvertise;

        public HelicopterTrollManager(ManualLogSource log, ModConfig settings)
        {
            _log = log; _settings = settings; _summonedField = AccessTools.Field(typeof(PeakHandler), "summonedHelicopter");
        }

        public bool LocalEnabled { get { return _settings.HelicopterSuppressionEnabled.Value; } }
        public bool Active { get { return LocalEnabled || _remoteSuppressors.Count > 0; } }

        public ActionResult SetLocalEnabled(bool enabled)
        {
            _settings.HelicopterSuppressionEnabled.Value = enabled;
            if (TrollModPlugin.Instance != null && TrollModPlugin.Instance.Network != null) TrollModPlugin.Instance.Network.BroadcastHelicopterSuppression(enabled);
            if (!Active) RestoreSuppressedFlag();
            return ActionResult.Ok(enabled ? "Summit helicopter suppression enabled and advertised to compatible clients." : "Local helicopter suppression disabled; compatible clients were notified.");
        }

        public void SetRemote(int actor, bool enabled)
        {
            if (enabled) _remoteSuppressors.Add(actor); else _remoteSuppressors.Remove(actor);
            if (!Active) RestoreSuppressedFlag();
        }

        public void Tick()
        {
            if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
            {
                List<int> departed = new List<int>();
                foreach (int actor in _remoteSuppressors) if (PhotonNetwork.CurrentRoom.GetPlayer(actor) == null) departed.Add(actor);
                for (int i = 0; i < departed.Count; i++) _remoteSuppressors.Remove(departed[i]);
                if (LocalEnabled && Time.unscaledTime >= _nextAdvertise)
                {
                    _nextAdvertise = Time.unscaledTime + 5f;
                    if (TrollModPlugin.Instance != null && TrollModPlugin.Instance.Network != null) TrollModPlugin.Instance.Network.BroadcastHelicopterSuppression(true);
                }
            }
            else _remoteSuppressors.Clear();
            if (!Active) RestoreSuppressedFlag();
        }

        public void ResetScene()
        {
            _remoteSuppressors.Clear(); _suppressedHandler = null; _nextAdvertise = 0f;
        }

        public bool TrySuppress(PeakHandler handler)
        {
            if (!Active || handler == null || _summonedField == null) return false;
            _summonedField.SetValue(handler, true);
            _suppressedHandler = handler;
            _log.LogInfo("Suppressed the local summit helicopter sequence.");
            return true;
        }

        private void RestoreSuppressedFlag()
        {
            if (_suppressedHandler == null || _summonedField == null) return;
            _summonedField.SetValue(_suppressedHandler, false); _suppressedHandler = null;
        }
    }

    [HarmonyPatch(typeof(PeakHandler), "SummonHelicopter")]
    internal static class SummitHelicopterPatch
    {
        private static bool Prefix(PeakHandler __instance)
        {
            TrollModPlugin plugin = TrollModPlugin.Instance;
            return plugin == null || plugin.HelicopterTroll == null || !plugin.HelicopterTroll.TrySuppress(__instance);
        }
    }
}
