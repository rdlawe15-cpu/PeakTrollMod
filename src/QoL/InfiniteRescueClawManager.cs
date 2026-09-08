using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class InfiniteRescueClawManager
    {
        private const float EffectiveInfiniteRange = 5000f;
        private readonly ModConfig _settings;
        private readonly ManualLogSource _log;
        private readonly Dictionary<RescueHook, Vector2> _originalRanges = new Dictionary<RescueHook, Vector2>();
        private bool _standaloneDetected;
        private float _nextStandaloneCheck;
        private float _nextErrorLog;

        public InfiniteRescueClawManager(ModConfig settings, ManualLogSource log) { _settings = settings; _log = log; }
        public bool Enabled { get { return _settings.InfiniteRescueClawReachEnabled.Value; } set { _settings.InfiniteRescueClawReachEnabled.Value = value; if (!value) RestoreAll(); } }
        public bool StandaloneDetected { get { RefreshStandaloneDetection(); return _standaloneDetected; } }
        public string Status { get { return StandaloneDetected ? "Yielding to standalone Rescue Hook Infinite Range" : Enabled ? "Local Rescue Claw range: effectively unlimited" : "Off"; } }

        public void Tick()
        {
            RefreshStandaloneDetection();
            if (!Enabled || _standaloneDetected) { RestoreAll(); return; }
            try
            {
                Character character = Character.localCharacter;
                Item held = character == null || character.data == null ? null : character.data.currentItem;
                RestoreNoLongerHeld(held);
                if (held == null) return;
                RescueHook[] hooks = held.GetComponentsInChildren<RescueHook>(true);
                for (int i = 0; i < hooks.Length; i++) Apply(hooks[i]);
            }
            catch (Exception ex)
            {
                RestoreAll();
                if (Time.unscaledTime >= _nextErrorLog) { _nextErrorLog = Time.unscaledTime + 10f; _log.LogWarning("[PTM] Infinite Rescue Claw Reach restored defaults after an error: " + ex.Message); }
            }
        }

        private void Apply(RescueHook hook)
        {
            if (hook == null) return;
            if (!_originalRanges.ContainsKey(hook)) _originalRanges[hook] = new Vector2(hook.range, hook.rangeDownward);
            hook.range = EffectiveInfiniteRange;
            hook.rangeDownward = EffectiveInfiniteRange;
        }

        private void RestoreNoLongerHeld(Item held)
        {
            List<RescueHook> remove = new List<RescueHook>();
            foreach (KeyValuePair<RescueHook, Vector2> pair in _originalRanges)
            {
                RescueHook hook = pair.Key;
                if (hook == null) { remove.Add(hook); continue; }
                if (held == null || hook.GetComponentInParent<Item>() != held) { hook.range = pair.Value.x; hook.rangeDownward = pair.Value.y; remove.Add(hook); }
            }
            for (int i = 0; i < remove.Count; i++) _originalRanges.Remove(remove[i]);
        }

        public void RestoreAll()
        {
            foreach (KeyValuePair<RescueHook, Vector2> pair in _originalRanges)
                if (pair.Key != null) { pair.Key.range = pair.Value.x; pair.Key.rangeDownward = pair.Value.y; }
            _originalRanges.Clear();
        }

        public void ResetScene() { RestoreAll(); }

        private void RefreshStandaloneDetection()
        {
            if (Time.unscaledTime < _nextStandaloneCheck) return;
            _nextStandaloneCheck = Time.unscaledTime + 5f;
            _standaloneDetected = false;
            foreach (KeyValuePair<string, PluginInfo> pair in Chainloader.PluginInfos)
            {
                PluginInfo info = pair.Value;
                if (info == null || info.Instance == null || !info.Instance.enabled || pair.Key == TrollModPlugin.Guid) continue;
                string identity = Normalize(pair.Key + " " + (info.Metadata == null ? string.Empty : info.Metadata.Name));
                if (identity.Contains("rescuehookinfiniterange") || identity.Contains("infiniterescueclaw") || identity.Contains("infiniterescuehook")) { _standaloneDetected = true; break; }
            }
        }

        private static string Normalize(string value)
        {
            char[] buffer = new char[value.Length]; int length = 0;
            for (int i = 0; i < value.Length; i++) if (char.IsLetterOrDigit(value[i])) buffer[length++] = char.ToLowerInvariant(value[i]);
            return new string(buffer, 0, length);
        }
    }
}
