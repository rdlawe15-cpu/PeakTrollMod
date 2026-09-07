using BepInEx.Configuration;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class ModConfig
    {
        public readonly ConfigEntry<KeyboardShortcut> MenuKey;
        public readonly ConfigEntry<float> UiScale;
        public readonly ConfigEntry<float> Transparency;
        public readonly ConfigEntry<bool> ConfirmDestructive;
        public readonly ConfigEntry<bool> ExcludeSelf;
        public readonly ConfigEntry<bool> DebugLogging;
        public readonly ConfigEntry<bool> NetworkDiagnostics;
        public readonly ConfigEntry<bool> MirageDebug;
        public readonly ConfigEntry<bool> FakeEnemyDebug;
        public readonly ConfigEntry<int> MaxSpawnedObjects;
        public readonly ConfigEntry<int> MaxDynamiteObjects;
        public readonly ConfigEntry<int> MaxItemStormObjects;
        public readonly ConfigEntry<float> ChaosMinDelay;
        public readonly ConfigEntry<float> ChaosMaxDelay;
        public readonly ConfigEntry<bool> UnlockAllCosmetics;
        public readonly ConfigEntry<bool> UnlockAllBadges;
        public readonly ConfigEntry<bool> CampfireResetEnabled;
        public readonly ConfigEntry<bool> HelicopterSuppressionEnabled;
        public readonly ConfigEntry<bool> NoWaitEnabled;
        public readonly ConfigEntry<NoWaitDestination> NoWaitDestinationMode;
        public readonly ConfigEntry<bool> NoWaitPreserveReconnectState;
        public readonly ConfigEntry<string> LastLobbyId;
        public readonly ConfigEntry<bool> TeamStatusEnabled;
        public readonly ConfigEntry<float> TeamStatusDistance;
        public readonly ConfigEntry<float> TeamStatusScale;
        public readonly ConfigEntry<bool> TeamStatusShowSelf;
        public readonly ConfigEntry<bool> QuickBackpackEnabled;
        public readonly ConfigEntry<KeyboardShortcut> QuickBackpackKey;
        public readonly ConfigEntry<bool> BetterSpectatingEnabled;
        public readonly ConfigEntry<KeyboardShortcut> SpectatePreviousKey;
        public readonly ConfigEntry<KeyboardShortcut> SpectateNextKey;
        public readonly ConfigEntry<KeyboardShortcut> SpectateFreeCameraKey;
        public readonly ConfigEntry<bool> SpectateGhostPings;
        public readonly ConfigEntry<bool> SpectateOverlay;
        public readonly ConfigEntry<bool> PreferExternalQualityOfLifeMods;
        public readonly ConfigEntry<string> PlayerPreferencesData;
        public readonly ConfigEntry<float> TextScale;
        public readonly ConfigEntry<bool> HighContrast;
        public readonly ConfigEntry<float> CameraShakeScale;
        public readonly ConfigEntry<bool> ReduceFlashingEffects;
        public readonly ConfigEntry<MenuActivationMode> MenuActivation;
        public readonly ConfigEntry<bool> ItemSpawnerEnabled;
        public readonly ConfigEntry<bool> EffectPreviewEnabled;
        public readonly ConfigEntry<bool> FavoritesAndRecentsEnabled;
        public readonly ConfigEntry<string> FavoriteItemsData;
        public readonly ConfigEntry<string> RecentItemsData;
        public readonly ConfigEntry<string> FavoriteEffectsData;
        public readonly ConfigEntry<string> RecentEffectsData;
        public readonly ConfigEntry<bool> UnlimitedLobbyEnabled;
        public readonly ConfigEntry<int> UnlimitedLobbyMaxPlayers;
        public readonly ConfigEntry<bool> UnlimitedLobbyScaleSupplies;
        public readonly ConfigEntry<bool> UnlimitedLobbyExtraFood;
        public readonly ConfigEntry<bool> UnlimitedLobbyExtraBackpacks;

        public ModConfig(ConfigFile config)
        {
            MenuKey = config.Bind("UI", "MenuKey", new KeyboardShortcut(KeyCode.F7), "Open or close the troll menu.");
            UiScale = config.Bind("UI", "Scale", 1.0f, "UI scale (0.75-1.5).");
            Transparency = config.Bind("UI", "Transparency", 0.96f, "Window opacity (0.55-1.0).");
            ConfirmDestructive = config.Bind("Safety", "ConfirmDestructiveActions", true, "Require a second click for elimination and bulk reset.");
            ExcludeSelf = config.Bind("Targeting", "ExcludeSelf", true, "Exclude the local player from random targets.");
            DebugLogging = config.Bind("Diagnostics", "DebugLogging", false, "Enable detailed diagnostics.");
            NetworkDiagnostics = config.Bind("Diagnostics", "NetworkDiagnostics", false, "Log validated mod messages.");
            MirageDebug = config.Bind("Diagnostics", "MirageDebugInfo", false, "Log mirage lifecycle.");
            FakeEnemyDebug = config.Bind("Diagnostics", "FakeEnemyDebugInfo", false, "Log fake-enemy lifecycle.");
            MaxSpawnedObjects = config.Bind("Safety", "MaximumModSpawnedObjects", 24, "Hard cap for tracked spawns and mirages.");
            MaxDynamiteObjects = config.Bind("Safety", "MaximumDynamiteObjects", 64, "Separate hard cap for caller-owned Dynamite Shower items (1-128).");
            MaxItemStormObjects = config.Bind("Safety", "MaximumItemStormObjects", 64, "Separate hard cap for caller-owned Item Storm objects (1-128).");
            ChaosMinDelay = config.Bind("Chaos", "MinimumDelay", 8f, "Minimum chaos event delay.");
            ChaosMaxDelay = config.Bind("Chaos", "MaximumDelay", 18f, "Maximum chaos event delay.");
            UnlockAllCosmetics = config.Bind("Progression", "UnlockAllCosmetics", false, "Expose all PEAK cosmetics while the mod is enabled without granting platform achievements.");
            UnlockAllBadges = config.Bind("Progression", "UnlockAllBadges", false, "Expose all PEAK badges and sync the local sash while enabled without granting platform achievements.");
            CampfireResetEnabled = config.Bind("World", "CampfireTeleportsEveryoneToStart", false, "When this client observes a campfire ignition, teleport all scouts back to the first segment start.");
            HelicopterSuppressionEnabled = config.Bind("World", "SuppressSummitHelicopter", false, "Suppress the summit rescue sequence locally and advertise suppression to compatible clients.");
            NoWaitEnabled = config.Bind("Player", "NoWait", true, "When joining an expedition already in progress, automatically revive the local scout beside the nearest living player.");
            NoWaitDestinationMode = config.Bind("Player", "NoWaitDestination", NoWaitDestination.NearestLiving, "Where No Wait places a late joiner: nearest living scout, lowest living scout, or the active checkpoint.");
            NoWaitPreserveReconnectState = config.Bind("Player", "NoWaitPreserveReconnectState", true, "When reconnecting, preserve the local scout's restored inventory, statuses, thorns, extra stamina, and petrification state.");
            LastLobbyId = config.Bind("Reconnect", "LastLobbyId", string.Empty, "Last valid Steam lobby ID observed by Quick Reconnect.");
            TeamStatusEnabled = config.Bind("Quality of Life", "TeamStatusPanel", true, "Show a compact teammate stamina and condition panel.");
            TeamStatusDistance = config.Bind("Quality of Life", "TeamStatusDistance", 45f, "Maximum world distance for the team panel (10-200 metres).");
            TeamStatusScale = config.Bind("Quality of Life", "TeamStatusScale", 1f, "Team panel scale (0.7-1.4).");
            TeamStatusShowSelf = config.Bind("Quality of Life", "TeamStatusShowSelf", false, "Include the local scout in the team panel.");
            QuickBackpackEnabled = config.Bind("Quality of Life", "QuickBackpack", true, "Open the equipped backpack wheel with a hotkey.");
            QuickBackpackKey = config.Bind("Quality of Life", "QuickBackpackKey", new KeyboardShortcut(KeyCode.B), "Hold to open the equipped backpack wheel.");
            BetterSpectatingEnabled = config.Bind("Quality of Life", "BetterSpectating", true, "Add spectator shortcuts, overlay, free camera, ghost pings, and revive-near-target.");
            SpectatePreviousKey = config.Bind("Quality of Life", "SpectatePreviousKey", new KeyboardShortcut(KeyCode.Q), "Cycle to the previous living scout while spectating.");
            SpectateNextKey = config.Bind("Quality of Life", "SpectateNextKey", new KeyboardShortcut(KeyCode.E), "Cycle to the next living scout while spectating.");
            SpectateFreeCameraKey = config.Bind("Quality of Life", "SpectateFreeCameraKey", new KeyboardShortcut(KeyCode.F8), "Toggle PEAK's free camera while spectating.");
            SpectateGhostPings = config.Bind("Quality of Life", "SpectateGhostPings", true, "Allow normal point pings while fully passed out.");
            SpectateOverlay = config.Bind("Quality of Life", "SpectateOverlay", true, "Show the current spectator target, altitude, status, and shortcuts.");
            PreferExternalQualityOfLifeMods = config.Bind("Compatibility", "PreferExternalQualityOfLifeMods", true, "Yield duplicate built-in features to enabled PeakStatsEx and EasyBackpack components.");
            PlayerPreferencesData = config.Bind("Player Preferences", "SavedPlayers", string.Empty, "Serialized per-Steam-player aliases, colors, voice settings, and random-target exclusions.");
            TextScale = config.Bind("Accessibility", "TextScale", 1f, "Menu text scale (0.85-1.30).");
            HighContrast = config.Bind("Accessibility", "HighContrastColors", false, "Use brighter text and stronger status contrast.");
            CameraShakeScale = config.Bind("Accessibility", "CameraShakeScale", 1f, "Scale PEAK camera shake from 0 (off) to 1 (normal).");
            ReduceFlashingEffects = config.Bind("Accessibility", "ReduceFlashingTrollEffects", false, "Slow and cap repeated visual troll effects such as Phantom Pings.");
            MenuActivation = config.Bind("Accessibility", "MenuActivation", MenuActivationMode.Toggle, "Open F7 as a toggle or only while the shortcut is held.");
            ItemSpawnerEnabled = config.Bind("Quality of Life", "ItemSpawner", true, "Show the searchable item spawner page in the F7 menu.");
            EffectPreviewEnabled = config.Bind("Quality of Life", "EffectPreview", true, "Show safe, non-executing effect previews before use.");
            FavoritesAndRecentsEnabled = config.Bind("Quality of Life", "FavoritesAndRecentActions", true, "Remember favorite and recently used items/effect previews.");
            FavoriteItemsData = config.Bind("Quick Access", "FavoriteItems", string.Empty, "Newline-separated favorite runtime item names.");
            RecentItemsData = config.Bind("Quick Access", "RecentItems", string.Empty, "Newline-separated recently used runtime item names.");
            FavoriteEffectsData = config.Bind("Quick Access", "FavoriteEffects", string.Empty, "Newline-separated favorite effect preview names.");
            RecentEffectsData = config.Bind("Quick Access", "RecentEffects", string.Empty, "Newline-separated recently previewed effects.");
            UnlimitedLobbyEnabled = config.Bind("Unlimited Lobby", "Enabled", false, "Raise the next hosted lobby's player cap. Automatically yields to the standalone PEAK Unlimited mod.");
            UnlimitedLobbyMaxPlayers = config.Bind("Unlimited Lobby", "MaxPlayers", 12, "Maximum players for newly hosted rooms (4-30). Change before creating the lobby.");
            UnlimitedLobbyScaleSupplies = config.Bind("Unlimited Lobby", "ScaleCampfireSupplies", true, "Scale campfire food and backpack spawns for players above the vanilla four-player count.");
            UnlimitedLobbyExtraFood = config.Bind("Unlimited Lobby", "ExtraCampfireFood", true, "Spawn one additional marshmallow per player above four, including late joiners.");
            UnlimitedLobbyExtraBackpacks = config.Bind("Unlimited Lobby", "ExtraBackpacks", true, "Spawn roughly one additional backpack per four players above the vanilla cap.");
        }
    }
}
