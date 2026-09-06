using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class ProgressionManager
    {
        private readonly ManualLogSource _log;
        private readonly PlayerManager _players;
        private readonly ModConfig _settings;
        private readonly MethodInfo _setBadgeStatus;
        private CharacterData _lastBadgeData;
        private float _nextRefresh;

        public ProgressionManager(ManualLogSource log, PlayerManager players, ModConfig settings)
        {
            _log = log; _players = players; _settings = settings;
            _setBadgeStatus = AccessTools.Method(typeof(CharacterData), "SetBadgeStatus");
        }

        public bool CosmeticsEnabled { get { return _settings.UnlockAllCosmetics.Value; } }
        public bool BadgesEnabled { get { return _settings.UnlockAllBadges.Value; } }

        public ActionResult SetCosmetics(bool enabled)
        {
            _settings.UnlockAllCosmetics.Value = enabled;
            return ActionResult.Ok(enabled ? "All PEAK cosmetics are available while the mod is enabled. Reopen the passport if it is already open." : "Cosmetics now use your real earned unlock state.");
        }

        public ActionResult SetBadges(bool enabled)
        {
            _settings.UnlockAllBadges.Value = enabled;
            _lastBadgeData = null;
            RefreshBadges(true);
            return ActionResult.Ok(enabled ? "All PEAK badges are enabled and the local sash was refreshed; platform achievements were not granted." : "Badges and the local sash were restored to the real earned state.");
        }

        public void Tick()
        {
            if (!_settings.UnlockAllBadges.Value || Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 3f;
            RefreshBadges(false);
        }

        private void RefreshBadges(bool force)
        {
            PlayerEntry local = _players.Local;
            CharacterData data = local == null || local.Character == null ? null : local.Character.data;
            if (data == null || _setBadgeStatus == null) return;
            bool needsRefresh = force || data != _lastBadgeData || data.badgeStatus == null || data.badgeStatus.Length == 0;
            if (!needsRefresh && _settings.UnlockAllBadges.Value)
                for (int i = 0; i < data.badgeStatus.Length; i++) if (!data.badgeStatus[i]) { needsRefresh = true; break; }
            if (!needsRefresh) return;
            try { _setBadgeStatus.Invoke(data, null); _lastBadgeData = data; }
            catch (Exception ex) { _log.LogWarning("Badge refresh failed safely: " + ex.Message); }
        }
    }

    [HarmonyPatch(typeof(CustomizationOption), "get_IsLocked")]
    internal static class AllCosmeticsUnlockPatch
    {
        private static bool Prefix(ref bool __result)
        {
            TrollModPlugin plugin = TrollModPlugin.Instance;
            if (plugin == null || plugin.Settings == null || !plugin.Settings.UnlockAllCosmetics.Value) return true;
            __result = false; return false;
        }
    }

    [HarmonyPatch(typeof(BadgeData), "get_IsLocked")]
    internal static class AllBadgesUnlockPatch
    {
        private static bool Prefix(ref bool __result)
        {
            TrollModPlugin plugin = TrollModPlugin.Instance;
            if (plugin == null || plugin.Settings == null || !plugin.Settings.UnlockAllBadges.Value) return true;
            __result = false; return false;
        }
    }
}
