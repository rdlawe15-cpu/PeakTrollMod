using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PeakTrollMod
{
    [BepInProcess("PEAK.exe")]
    [BepInPlugin(Guid, Name, Version)]
    public sealed class TrollModPlugin : BaseUnityPlugin
    {
        public const string Guid = "com.dougl.peaktrollmod";
        public const string Name = "PEAK Troll Mod";
        public const string Version = "0.3.0";

        internal static TrollModPlugin Instance;
        internal ModConfig Settings;
        internal CapabilityRegistry Capabilities;
        internal PlayerManager Players;
        internal TrollNetworkManager Network;
        internal PlayerActions Actions;
        internal SpawnManager Spawns;
        internal MirageManager Mirages;
        internal PingPlacementManager PingPlacement;
        internal PhantomPingManager PhantomPings;
        internal AudioManager Audio;
        internal AppearanceManager Appearance;
        internal ProgressionManager Progression;
        internal CampfireTrollManager CampfireTroll;
        internal HelicopterTrollManager HelicopterTroll;
        internal ChaosManager Chaos;
        internal ResetManager Reset;
        internal TrollUIManager Ui;

        private Harmony _harmony;
        private float _nextDynamicRefresh;
        private bool _wasInRoom;
        private bool _shutdown;

        private void Awake()
        {
            Instance = this;
            Settings = new ModConfig(base.Config);
            Capabilities = new CapabilityRegistry(Logger); Capabilities.Discover();
            Players = new PlayerManager(Logger);
            Audio = new AudioManager(Logger);
            Mirages = new MirageManager(Logger);
            Actions = new PlayerActions(Logger, Players, Capabilities, Audio);
            Spawns = new SpawnManager(Logger, Players, Capabilities);
            Network = new TrollNetworkManager(Logger, Players, Actions, Mirages, Audio);
            PingPlacement = new PingPlacementManager(Logger, Mirages, Players, Network);
            PhantomPings = new PhantomPingManager(Logger, Players);
            Appearance = new AppearanceManager(Logger);
            Progression = new ProgressionManager(Logger, Players, Settings);
            CampfireTroll = new CampfireTrollManager(Logger, Players, Actions, Settings);
            HelicopterTroll = new HelicopterTrollManager(Logger, Settings);
            Reset = new ResetManager(Logger, Actions, Mirages, Spawns, Audio, Appearance);
            Chaos = new ChaosManager(Logger, Players, Network, Actions, Mirages, Audio, Spawns);
            Ui = new TrollUIManager(this);

            _harmony = new Harmony(Guid); _harmony.PatchAll(typeof(TrollModPlugin).Assembly);
            SceneManager.sceneLoaded += OnSceneLoaded;
            Players.Refresh();
            _wasInRoom = PhotonNetwork.InRoom;
            Logger.LogInfo(Name + " " + Version + " loaded for PEAK " + Application.version + ".");
        }

        private void Update()
        {
            if (Settings.MenuKey.Value.IsDown()) Ui.Toggle();
            Players.Tick(); Network.Tick(); Actions.Tick(); Audio.Tick(); Mirages.Tick(); PhantomPings.Tick(); Spawns.Tick(); Progression.Tick(); HelicopterTroll.Tick(); Chaos.Tick(); Ui.Tick();

            bool inRoom = PhotonNetwork.InRoom;
            if (_wasInRoom && !inRoom) Reset.ResetAll();
            _wasInRoom = inRoom;
            if (Time.unscaledTime >= _nextDynamicRefresh)
            {
                _nextDynamicRefresh = Time.unscaledTime + 10f;
                Capabilities.RefreshDynamic(); Actions.RefreshItemCatalog();
            }
        }

        private void OnGUI() { if (Ui != null) Ui.Draw(); }

        private void FixedUpdate() { if (Actions != null) Actions.FixedTick(); }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Reset != null) Reset.ResetAll();
            if (Players != null) Players.Refresh();
            if (Capabilities != null) Capabilities.RefreshDynamic();
            if (Actions != null) Actions.RefreshItemCatalog();
            if (CampfireTroll != null) CampfireTroll.ResetScene();
            if (HelicopterTroll != null) HelicopterTroll.ResetScene();
            DebugLog("Scene loaded: " + scene.name);
        }

        private void OnDisable() { Shutdown(); }
        private void OnApplicationQuit() { Shutdown(); }

        private void Shutdown()
        {
            if (_shutdown) return; _shutdown = true;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Ui != null) Ui.ForceClose();
            if (Reset != null) Reset.ResetAll();
            if (Network != null) Network.Dispose();
            if (_harmony != null) _harmony.UnpatchSelf();
            Instance = null;
        }

        internal void DebugLog(string message) { if (Settings != null && Settings.DebugLogging.Value) Logger.LogInfo("[PTM] " + message); }
    }

    [HarmonyPatch(typeof(Character), "CanDoInput")]
    internal static class GameplayInputPatch
    {
        private static void Postfix(Character __instance, ref bool __result)
        {
            TrollModPlugin plugin = TrollModPlugin.Instance;
            if (plugin != null && plugin.Ui != null && plugin.Ui.IsOpen && __instance != null && __instance.IsLocal) __result = false;
        }
    }

    [HarmonyPatch(typeof(PointPinger), "DoPing")]
    internal static class PingPlacementPatch
    {
        private static bool Prefix(PointPinger __instance)
        {
            TrollModPlugin plugin = TrollModPlugin.Instance;
            if (plugin == null || plugin.PingPlacement == null || !plugin.PingPlacement.Active) return true;
            return !plugin.PingPlacement.TryCapture(__instance);
        }
    }

    [HarmonyPatch(typeof(CursorHandler), "Update")]
    internal static class CursorHandlerPatch
    {
        private static bool Prefix()
        {
            TrollModPlugin plugin = TrollModPlugin.Instance;
            if (plugin == null || plugin.Ui == null || !plugin.Ui.IsOpen) return true;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true; return false;
        }
    }

    [HarmonyPatch(typeof(GUIManager), "UpdateWindowStatus")]
    internal static class WindowStatusPatch
    {
        private static readonly FieldInfo ShowingCursor = AccessTools.Field(typeof(GUIManager), "<windowShowingCursor>k__BackingField");
        private static readonly FieldInfo BlockingInput = AccessTools.Field(typeof(GUIManager), "<windowBlockingInput>k__BackingField");
        private static readonly FieldInfo LastBlockedInput = AccessTools.Field(typeof(GUIManager), "lastBlockedInput");

        private static void Postfix(GUIManager __instance)
        {
            TrollModPlugin plugin = TrollModPlugin.Instance;
            if (plugin == null || plugin.Ui == null || !plugin.Ui.IsOpen || __instance == null) return;
            if (ShowingCursor != null) ShowingCursor.SetValue(__instance, true);
            if (BlockingInput != null) BlockingInput.SetValue(__instance, true);
            if (LastBlockedInput != null) LastBlockedInput.SetValue(__instance, Time.frameCount);
        }
    }
}
