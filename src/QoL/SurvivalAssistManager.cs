using System;
using BepInEx.Logging;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class SurvivalAssistManager
    {
        private readonly ModConfig _settings;
        private readonly ManualLogSource _log;
        private float _nextErrorLog;
        private float _nextCleanse;

        public SurvivalAssistManager(ModConfig settings, ManualLogSource log) { _settings = settings; _log = log; }
        public bool Immortality { get { return _settings.ImmortalityEnabled.Value; } set { _settings.ImmortalityEnabled.Value = value; } }
        public bool InfiniteStamina { get { return _settings.InfiniteStaminaEnabled.Value; } set { _settings.InfiniteStaminaEnabled.Value = value; } }

        public void Tick()
        {
            if (!Immortality && !InfiniteStamina) return;
            Character character = Character.localCharacter;
            if (character == null || character.data == null || character.refs == null || character.refs.afflictions == null || character.data.dead) return;
            try
            {
                if (Immortality && Time.unscaledTime >= _nextCleanse) { _nextCleanse = Time.unscaledTime + .1f; ClearNegativeStatuses(character.refs.afflictions); }
                if (InfiniteStamina && !character.data.fullyPassedOut) Character.GainFullStamina();
            }
            catch (Exception ex)
            {
                if (Time.unscaledTime >= _nextErrorLog) { _nextErrorLog = Time.unscaledTime + 10f; _log.LogWarning("[PTM] Survival assistance skipped an update: " + ex.Message); }
            }
        }

        private static void ClearNegativeStatuses(CharacterAfflictions afflictions)
        {
            bool changed = false;
            if (afflictions.physicalThorns != null && afflictions.physicalThorns.Count > 0) { afflictions.RemoveAllThorns(); changed = true; }
            foreach (object value in Enum.GetValues(typeof(CharacterAfflictions.STATUSTYPE)))
            {
                CharacterAfflictions.STATUSTYPE type = (CharacterAfflictions.STATUSTYPE)value;
                if (afflictions.GetCurrentStatus(type) <= 0f && afflictions.GetIncrementalStatus(type) <= 0f) continue;
                afflictions.SetStatus(type, 0f, false);
                changed = true;
            }
            if (changed) afflictions.PushStatuses(null);
        }

        public void ResetScene()
        {
            // Both options are config-backed; no game state is permanently modified.
        }
    }
}
