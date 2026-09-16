using System;
using BepInEx.Logging;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class InfiniteJetpackFuelManager
    {
        private readonly ModConfig _settings;
        private readonly ManualLogSource _log;
        private ItemInstanceData _cachedData;
        private FloatItemData _cachedFuel;
        private FloatItemData _cachedPercentage;
        private float _originalFuel;
        private float _originalPercentage;
        private bool _hadPercentage;
        private float _nextErrorLog;
        private string _status = "Off";

        public InfiniteJetpackFuelManager(ModConfig settings, ManualLogSource log)
        {
            _settings = settings;
            _log = log;
        }

        public bool Enabled
        {
            get { return _settings.InfiniteJetpackFuelEnabled.Value; }
            set
            {
                if (!value) Restore();
                _settings.InfiniteJetpackFuelEnabled.Value = value;
                _status = value ? "Equip a Jetpack to keep its fuel full." : "Off";
            }
        }

        public string Status { get { return _status; } }

        public void Tick()
        {
            if (!Enabled)
            {
                if (_cachedData != null) Restore();
                return;
            }

            Character character = Character.localCharacter;
            if (character == null || character.player == null || character.data == null || character.data.dead)
            {
                Restore();
                _status = "Waiting for the local scout.";
                return;
            }

            BackpackSlot slot = character.player.backpackSlot;
            if (slot == null || slot.backpackType != BackpackSlot.BackpackType.Jetpack || slot.data == null || slot.prefab == null)
            {
                Restore();
                _status = "Equip a Jetpack to keep its fuel full.";
                return;
            }

            try
            {
                JetpackItem jetpack = slot.prefab.GetComponent<JetpackItem>();
                FloatItemData fuel;
                if (jetpack == null || !slot.data.TryGetDataEntry<FloatItemData>(DataEntryKey.Fuel, out fuel) || fuel == null)
                {
                    Restore();
                    _status = "This equipped backpack does not expose PEAK's jetpack fuel data.";
                    return;
                }

                if (!ReferenceEquals(_cachedData, slot.data))
                {
                    Restore();
                    Cache(slot.data, fuel);
                }

                float maximumFuel = jetpack.startingFuel;
                if (float.IsNaN(maximumFuel) || float.IsInfinity(maximumFuel) || maximumFuel <= 0f) maximumFuel = Mathf.Max(1f, _originalFuel);
                _cachedFuel.Value = maximumFuel;
                if (_cachedPercentage != null) _cachedPercentage.Value = 1f;
                _status = "Active — local Jetpack fuel is held at " + maximumFuel.ToString("0.#") + ".";
            }
            catch (Exception ex)
            {
                Restore();
                _status = "Jetpack fuel update skipped safely.";
                if (Time.unscaledTime >= _nextErrorLog)
                {
                    _nextErrorLog = Time.unscaledTime + 10f;
                    _log.LogWarning("[PTM] Infinite Jetpack Fuel skipped an update: " + ex.Message);
                }
            }
        }

        private void Cache(ItemInstanceData data, FloatItemData fuel)
        {
            _cachedData = data;
            _cachedFuel = fuel;
            _originalFuel = fuel.Value;
            _cachedPercentage = null;
            _hadPercentage = data.TryGetDataEntry<FloatItemData>(DataEntryKey.UseRemainingPercentage, out _cachedPercentage) && _cachedPercentage != null;
            _originalPercentage = _hadPercentage ? _cachedPercentage.Value : 0f;
        }

        public void Restore()
        {
            try
            {
                if (_cachedFuel != null) _cachedFuel.Value = _originalFuel;
                if (_hadPercentage && _cachedPercentage != null) _cachedPercentage.Value = _originalPercentage;
            }
            catch (Exception ex)
            {
                if (Time.unscaledTime >= _nextErrorLog)
                {
                    _nextErrorLog = Time.unscaledTime + 10f;
                    _log.LogWarning("[PTM] Infinite Jetpack Fuel cleanup failed safely: " + ex.Message);
                }
            }
            _cachedData = null;
            _cachedFuel = null;
            _cachedPercentage = null;
            _originalFuel = 0f;
            _originalPercentage = 0f;
            _hadPercentage = false;
        }

        public void ResetScene()
        {
            Restore();
            _status = Enabled ? "Waiting for the local scout." : "Off";
        }
    }
}
