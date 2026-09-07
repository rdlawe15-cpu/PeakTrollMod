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
        }
    }
}
