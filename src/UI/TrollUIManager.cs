using System;
using System.Collections.Generic;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class TrollUIManager
    {
        private const float DesignWidth = 1280f;
        private const float DesignHeight = 850f;
        private readonly TrollModPlugin _plugin;
        private readonly string[] _tabs = { "⌂  Home", "♟  Player", "▣  Spawning", "✦  Mirage", "♪  Audio", "●  Appearance", "☠  Enemies", "▲  World", "◈  Chaos", "⚙  Settings" };
        private readonly string[] _tabNames = { "Home", "Player", "Spawning", "Mirage", "Audio", "Appearance", "Enemies", "World", "Chaos Mode", "Settings" };
        private int _tab;
        private int _targetIndex;
        private int _statusIndex = 3;
        private int _itemIndex;
        private int _soundIndex;
        private int _enemyIndex;
        private bool _targetAll;
        private string _itemSearch = string.Empty;
        private float _ragdollStrength = 30f;
        private float _horizontalRagdollStrength = 90f;
        private float _launchStrength = 35f;
        private float _speed = 1f;
        private float _flightSpeed = 10f;
        private float _statusAmount = .15f;
        private float _spawnDistance = 10f;
        private float _lifetime = 12f;
        private float _volume = .8f;
        private float _skyHeight = 150f;
        private int _dynamiteCount = 6;
        private float _dynamiteHeight = 18f;
        private float _dynamiteSpread = 5f;
        private float _dynamiteInterval = .3f;
        private bool _lightDynamite = true;
        private int _itemStormCount = 20;
        private float _itemStormHeight = 18f;
        private float _itemStormSpread = 7f;
        private float _itemStormInterval = .2f;
        private bool _itemStormRandom = true;
        private bool _randomLaunch;
        private bool _ragdollLaunch = true;
        private bool _multiPlace;
        private int _phantomPingPattern;
        private int _phantomPingCount = 6;
        private float _phantomPingInterval = 1f;
        private bool _clothesOnly;
        private bool _colorOnly;
        private bool _confirmEliminate;
        private string _message = "Ready. Private lobbies, compatible friends, sensible chaos.";
        private float _messageUntil;
        private CursorLockMode _oldLock;
        private bool _oldCursor;
        private GUIStyle _title, _logo, _subtitle, _nav, _navSelected, _label, _small, _button, _danger, _success, _disabled, _cardTitle, _badge;
        private Texture2D _pixel;
        private readonly Dictionary<string, Vector2> _cardScroll = new Dictionary<string, Vector2>();
        private readonly Dictionary<string, float> _buttonHover = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _buttonPulseUntil = new Dictionary<string, float>();
        public bool IsOpen { get; private set; }

        public TrollUIManager(TrollModPlugin plugin) { _plugin = plugin; }

        public void Toggle() { if (IsOpen) ForceClose(); else Open(); }
        private void Open() { _oldLock = Cursor.lockState; _oldCursor = Cursor.visible; IsOpen = true; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        public void ForceClose() { if (!IsOpen) return; IsOpen = false; Cursor.lockState = _oldLock; Cursor.visible = _oldCursor; _confirmEliminate = false; }
        public void Tick() { if (IsOpen) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; } }

        public void Draw()
        {
            if (!IsOpen) return; EnsureStyles();
            Matrix4x4 old = GUI.matrix; float scale = Mathf.Min(Screen.width / DesignWidth, Screen.height / DesignHeight) * Mathf.Clamp(_plugin.Settings.UiScale.Value, .75f, 1.5f);
            float x = (Screen.width - DesignWidth * scale) * .5f; float y = (Screen.height - DesignHeight * scale) * .5f;
            GUI.color = Color.white; DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(.01f, .016f, .018f, .76f));
            GUI.matrix = Matrix4x4.TRS(new Vector3(x, y, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
            DrawPanel(new Rect(10, 10, 1260, 830), new Color(.035f, .047f, .05f, Mathf.Clamp(_plugin.Settings.Transparency.Value, .55f, 1f)), new Color(.25f, .34f, .35f, 1f), 2f);
            DrawHeader(); DrawSidebar(); DrawContent(); DrawStatusBar();
            GUI.matrix = old; GUI.color = Color.white;
        }

        private void DrawHeader()
        {
            GUIStyle brand = new GUIStyle(_logo); brand.normal.textColor = new Color(.28f, .78f, .75f); GUI.Label(new Rect(42, 24, 150, 32), "PEAK", brand);
            GUIStyle brandSub = new GUIStyle(_logo); brandSub.fontSize = 21; GUI.Label(new Rect(42, 50, 150, 54), "TROLL\nMOD", brandSub);
            GUI.Label(new Rect(238, 29, 520, 44), HeadingForTab(), _title);
            GUI.Label(new Rect(240, 74, 570, 24), SubtitleForTab(), _subtitle);
            if (_tab != 0 && _tab != 9)
            {
                GUI.Label(new Rect(826, 41, 72, 30), "Target", _label);
                if (AnimatedButton(new Rect(895, 32, 235, 44), CurrentTargetName() + "   ▾", _button)) CycleTarget();
                if (AnimatedButton(new Rect(1138, 32, 42, 44), "R", _button)) SelectRandomTarget();
                if (AnimatedButton(new Rect(1186, 32, 48, 44), "ALL", _button)) _targetAll = true;
                if (AnimatedButton(new Rect(1238, 32, 24, 44), "×", _button)) ForceClose();
            }
            else if (AnimatedButton(new Rect(1182, 32, 52, 44), "F7", _button)) ForceClose();
            Line(new Rect(210, 116, 1040, 1), new Color(.20f, .31f, .32f, .85f));
        }

        private void DrawSidebar()
        {
            DrawPanel(new Rect(18, 120, 192, 690), new Color(.025f, .035f, .038f, .96f), new Color(.13f, .2f, .21f, 1f), 1f);
            for (int i = 0; i < _tabs.Length; i++) if (AnimatedButton(new Rect(24, 136 + i * 55, 180, 48), _tabs[i], i == _tab ? _navSelected : _nav)) _tab = i;
            GUI.Label(new Rect(35, 756, 165, 24), "Version " + TrollModPlugin.Version, _small);
        }

        private void DrawContent()
        {
            Rect area = new Rect(220, 128, 1030, 622);
            if (_tab == 0) DrawHome(area); else if (_tab == 1) DrawPlayer(area); else if (_tab == 2) DrawSpawning(area); else if (_tab == 3) DrawMirage(area); else if (_tab == 4) DrawAudio(area); else if (_tab == 5) DrawAppearance(area); else if (_tab == 6) DrawEnemies(area); else if (_tab == 7) DrawWorld(area); else if (_tab == 8) DrawChaos(area); else DrawSettings(area);
        }

        private void DrawHome(Rect area)
        {
            Card(new Rect(238, 218, 310, 202), "◆  Session", new Color(.25f, .6f, 1f), delegate
            {
                GUILayout.Label("PEAK version: " + Application.version, _label); GUILayout.Label("Host: " + (_plugin.Players.IsHost ? "Yes" : "No"), _label);
                GUILayout.Label("Local: " + (_plugin.Players.Local == null ? "Not resolved" : _plugin.Players.Local.Name), _label); GUILayout.Label("Lobby players: " + _plugin.Players.Entries.Count, _label);
                GUILayout.Label("Compatible clients: " + _plugin.Network.CompatibleCount, _label);
            });
            Card(new Rect(558, 218, 310, 202), "✦  Active Effects", new Color(.62f, .42f, 1f), delegate
            {
                GUILayout.Label("Chaos: " + (_plugin.Chaos.Active ? "ACTIVE" : "Stopped"), _label); GUILayout.Label("Mirages: " + _plugin.Mirages.Count, _label);
                GUILayout.Label("Fake enemies: " + _plugin.Mirages.FakeEnemyCount, _label); GUILayout.Label("Genuine troll spawns: " + _plugin.Spawns.Count, _label);
                GUILayout.Label("Networking: " + (Photon.Pun.PhotonNetwork.InRoom ? "Connected" : "Offline"), _label);
            });
            Card(new Rect(878, 218, 350, 202), "!  Authority & Safety", new Color(1f, .68f, .22f), delegate
            {
                GUILayout.Label("🟢 Local / harmless visual", _small); GUILayout.Label("🟡 Target needs this mod", _small); GUILayout.Label("🔒 Photon host authority", _small); GUILayout.Label("⚠ Disabled when unresolved", _small);
            });
            Card(new Rect(238, 438, 990, 240), "↻  Emergency Controls", new Color(.25f, 1f, .5f), delegate
            {
                GUILayout.BeginHorizontal(); if (AnimatedButton("STOP CHAOS", _danger, GUILayout.Height(42))) { _plugin.Chaos.Stop(); _plugin.Chaos.StopCombo(); Say("Chaos and combo runner stopped."); } if (AnimatedButton("CLEAR MIRAGES", _button, GUILayout.Height(42))) { _plugin.Mirages.ClearAll(); Say("All local mirages cleared."); } if (AnimatedButton("CLEAR TROLL SPAWNS", _button, GUILayout.Height(42))) ClearTrollSpawns(); GUILayout.EndHorizontal();
                GUILayout.Space(12); if (AnimatedButton("RESET ALL TROLL EFFECTS", _danger, GUILayout.Height(48))) EmergencyReset();
                GUILayout.Space(8); GUILayout.Label("Cleanup only touches cached player changes and objects created by this mod.", _small);
            });
        }

        private void DrawPlayer(Rect area)
        {
            Card(new Rect(238, 144, 480, 364), "↗  Movement", new Color(.28f, .78f, .75f), delegate
            {
                Badge(PermissionKind.EveryoneNeedsMod); GUILayout.Label("Flight", _label);
                GUILayout.BeginHorizontal(); if (CapabilityButton("ON", FeatureCapability.Flight, _success, 38)) Owner(NetCommand.Flight, new object[] { true, _flightSpeed }); if (CapabilityButton("OFF", FeatureCapability.Flight, _button, 38)) Owner(NetCommand.Flight, new object[] { false, _flightSpeed }); GUILayout.EndHorizontal();
                GUILayout.Label("Flight speed                                      " + _flightSpeed.ToString("0") + " m/s", _small); _flightSpeed = GUILayout.HorizontalSlider(_flightSpeed, 4f, 20f);
                GUILayout.Label("[W] [A] [S] [D] move   [SPACE] up   [CTRL] down   [SHIFT] boost", _small);
                GUILayout.Space(12); Badge(PermissionKind.Anyone); GUILayout.Label("Launch strength  " + _launchStrength.ToString("0"), _label); _launchStrength = GUILayout.HorizontalSlider(_launchStrength, 5f, 120f);
                GUILayout.BeginHorizontal(); _randomLaunch = GUILayout.Toggle(_randomLaunch, "Random direction"); _ragdollLaunch = GUILayout.Toggle(_ragdollLaunch, "Ragdoll"); GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal(); if (CapabilityButton("LAUNCH", FeatureCapability.Launch, _button, 34)) { Vector3 d = _randomLaunch ? new Vector3(UnityEngine.Random.Range(-.5f,.5f),1f,UnityEngine.Random.Range(-.5f,.5f)).normalized : Vector3.up; Owner(NetCommand.Launch, new object[] { d.x, d.y, d.z, _launchStrength, _ragdollLaunch }); } if(CapabilityButton("SKY HIGH",FeatureCapability.SkyLaunch,_danger,34))SkyLaunch(); GUILayout.EndHorizontal();
                GUILayout.Label("Sky height  " + _skyHeight.ToString("0") + "m", _small); _skyHeight=GUILayout.HorizontalSlider(_skyHeight,25f,300f);
                GUILayout.Label("Ragdoll strength  " + _ragdollStrength.ToString("0"), _label); _ragdollStrength = GUILayout.HorizontalSlider(_ragdollStrength, 1f, 75f);
                GUILayout.BeginHorizontal(); if (CapabilityButton("RAGDOLL", FeatureCapability.Ragdoll, _button, 30)) { Vector3 d = new Vector3(UnityEngine.Random.Range(-.5f,.5f), .5f, UnityEngine.Random.Range(-.5f,.5f)).normalized; Owner(NetCommand.Ragdoll, new object[] { d.x,d.y,d.z,_ragdollStrength,1.5f }); } if (CapabilityButton("OFF MOUNTAIN", FeatureCapability.Ragdoll, _danger, 30)) RagdollOffMountain(); GUILayout.EndHorizontal();
                GUILayout.Label("Horizontal Yeet strength  "+_horizontalRagdollStrength.ToString("0"),_label);_horizontalRagdollStrength=GUILayout.HorizontalSlider(_horizontalRagdollStrength,25f,150f);if(CapabilityButton("HORIZONTAL YEET",FeatureCapability.Ragdoll,_danger,32))HorizontalRagdoll();
            });
            Card(new Rect(730, 144, 498, 364), "♥  Health & Status", new Color(.83f, .48f, .38f), delegate
            {
                Badge(PermissionKind.Anyone); GUILayout.Label("Native knockout and elimination work without the target mod.", _small);
                GUILayout.BeginHorizontal(); if (CapabilityButton("KNOCK OUT", FeatureCapability.Knockout, _button, 38)) Owner(NetCommand.Knockout, new object[0]);
                if (CapabilityButton(_confirmEliminate ? "CONFIRM ELIMINATE" : "ELIMINATE", FeatureCapability.Eliminate, _danger, 38)) { if (!_plugin.Settings.ConfirmDestructive.Value || _confirmEliminate) { Owner(NetCommand.Eliminate, new object[0]); _confirmEliminate = false; } else { _confirmEliminate = true; Say("Click Eliminate again to confirm."); } } GUILayout.EndHorizontal();
                GUILayout.Space(12); GUILayout.Label("Status effect", _label);
                GUILayout.BeginHorizontal(); if (AnimatedButton(((CharacterAfflictions.STATUSTYPE)_statusIndex).ToString() + "   ▾", _button, GUILayout.Width(280), GUILayout.Height(36))) _statusIndex = (_statusIndex + 1) % 15; if (CapabilityButton("APPLY", FeatureCapability.StatusEffects, _success, 36)) Owner(NetCommand.Status, new object[] { _statusIndex, _statusAmount }); GUILayout.EndHorizontal();
                GUILayout.Label("Intensity  " + _statusAmount.ToString("0.00"), _small); _statusAmount = GUILayout.HorizontalSlider(_statusAmount, .01f, .5f);
                GUILayout.Label(_plugin.Players.IsHost&&_statusIndex!=7&&_statusIndex!=9&&_statusIndex!=12?"Host-native status; target mod not required.":"Status requires the selected target's mod.",_small);
                if(AnimatedButton("CLEAR MOD STATUSES",_button,GUILayout.Height(34)))ClearStatuses();
            });
            Card(new Rect(238, 522, 310, 168), "⌖  Teleport", new Color(.43f, .66f, .64f), delegate
            {
                Badge(PermissionKind.Anyone); if (CapabilityButton("MYSELF TO TARGET", FeatureCapability.Teleport, _success, 30)) TeleportSelfToTarget();
                if (AnimatedButton("TARGET TO ME", _button, GUILayout.Height(30)) && _plugin.Players.Local != null) Teleport(_plugin.Players.Local.Character.Center);
                GUILayout.BeginHorizontal(); if (AnimatedButton("TO START", _button)) { Vector3 p; if (_plugin.Actions.TryGetStartEnd(false,out p)) Teleport(p); else Say("Start reference unavailable."); } if (AnimatedButton("TO END", _button)) { Vector3 p; if (_plugin.Actions.TryGetStartEnd(true,out p)) Teleport(p); else Say("End reference unavailable."); } GUILayout.EndHorizontal();
                if (AnimatedButton("RANDOM NEAR PLAYER", _button)) { PlayerEntry r=_plugin.Players.Random(true); if(r!=null) Teleport(r.Character.Center + UnityEngine.Random.insideUnitSphere * 3f); }
            });
            Card(new Rect(560, 522, 330, 168), "◉  Visibility & Speed", new Color(.28f, .78f, .75f), delegate
            {
                Badge(PermissionKind.EveryoneNeedsMod); GUILayout.BeginHorizontal(); if (CapabilityButton("HIDE", FeatureCapability.Visibility, _button, 30)) Broadcast(NetCommand.Visibility, new object[] { false }); if (CapabilityButton("SHOW", FeatureCapability.Visibility, _button, 30)) Broadcast(NetCommand.Visibility, new object[] { true }); GUILayout.EndHorizontal();
                GUILayout.Label("Movement speed  " + _speed.ToString("0.00") + "x", _small); _speed = GUILayout.HorizontalSlider(_speed, .25f, 3f);
                GUILayout.BeginHorizontal(); if (CapabilityButton("APPLY", FeatureCapability.Speed, _success, 30)) Owner(NetCommand.Speed, new object[] { _speed }); if (AnimatedButton("RESET 1.0x", _button, GUILayout.Height(30))) Owner(NetCommand.Speed, new object[] { 1f }); GUILayout.EndHorizontal();
            });
            Card(new Rect(902, 522, 326, 168), "▣  Inventory & Reset", new Color(.86f, .62f, .25f), delegate
            {
                Badge(PermissionKind.Anyone); _itemSearch=GUILayout.TextField(_itemSearch, _button, GUILayout.Height(28)); string item = CurrentItemName(); if (AnimatedButton(item + "   ▾", _button, GUILayout.Height(30))) CycleItem();
                GUILayout.BeginHorizontal(); if (CapabilityButton("GIVE ITEM", FeatureCapability.GiveItem, _button, 32)) Owner(NetCommand.GiveItem, new object[] { item }); if (AnimatedButton("RESET PLAYER", _danger, GUILayout.Height(32))) Broadcast(NetCommand.Reset, new object[0]); GUILayout.EndHorizontal();
            });
        }

        private void DrawSpawning(Rect area)
        {
            Card(new Rect(238, 220, 315, 270), "☠  Scoutmaster Ambush", new Color(1f, .3f, .35f), delegate { Badge(_plugin.Players.IsHost?PermissionKind.HostOnly:PermissionKind.EveryoneNeedsMod); Distance(); GUI.enabled=_plugin.Network.HostSupportsRequests; if (AnimatedButton("REQUEST SCOUTMASTER", _button, GUILayout.Height(42))) Say(_plugin.Network.RequestHostSpawn(NetCommand.HostSpawnScoutmaster,Target(),_spawnDistance)); GUI.enabled=true; GUILayout.Label(_plugin.Players.IsHost?"Executed with local host authority.":"A compatible host executes the verified room spawn.", _small); });
            Card(new Rect(565, 220, 315, 270), "♟  Mushroom Zombie", new Color(.45f, .8f, .35f), delegate { Badge(_plugin.Players.IsHost?PermissionKind.HostOnly:PermissionKind.EveryoneNeedsMod); Distance(); GUI.enabled=_plugin.Network.HostSupportsRequests; if (AnimatedButton("REQUEST ZOMBIE", _button, GUILayout.Height(42))) Say(_plugin.Network.RequestHostSpawn(NetCommand.HostSpawnZombie,Target(),_spawnDistance)); GUI.enabled=true; GUILayout.Label("A compatible host resolves the vanilla spawner prefab.", _small); });
            Card(new Rect(892, 220, 336, 270), "◉  The Looker", new Color(.63f, .42f, 1f), delegate { Badge(PermissionKind.Unsupported); Unsupported("GENUINE LOOKER SPAWN", _plugin.Capabilities.Reason(FeatureCapability.LookerSpawn)); GUILayout.Label("A harmless fake Looker becomes available when a visual source is loaded.", _small); });
            Card(new Rect(238, 506, 990, 154), "↻  Tracked Cleanup", new Color(.3f, 1f, .55f), delegate { GUILayout.Label("Active mod-spawned enemies: " + _plugin.Spawns.Count, _label); GUILayout.BeginHorizontal();GUI.enabled=_plugin.Network.HostSupportsRequests;if(AnimatedButton("CLEAR MY REQUESTED SPAWNS",_button,GUILayout.Height(44)))Say(_plugin.Network.RequestClearMySpawns());GUI.enabled=true;GUI.enabled=_plugin.Players.IsHost;if (AnimatedButton("HOST: CLEAR ALL", _danger, GUILayout.Height(44))) { _plugin.Spawns.ClearAll(); Say("Cleared only objects in the mod tracking registry."); }GUI.enabled=true;GUILayout.EndHorizontal(); });
        }

        private void DrawMirage(Rect area)
        {
            Card(new Rect(238, 218, 315, 265), "✦  Ping Placement", new Color(.35f, .65f, 1f), delegate
            {
                Badge(PermissionKind.EveryoneNeedsMod); GUILayout.Label("Placed for the selected viewer or every compatible viewer.", _small); GUILayout.Label("Type: " + _plugin.PingPlacement.Kind, _label); GUILayout.BeginHorizontal(); if (CapabilityButton("Luggage", FeatureCapability.MirageLuggage, _button, 30)) BeginPing(MirageKind.Luggage); if (CapabilityButton("Statue", FeatureCapability.MirageStatue, _button, 30)) BeginPing(MirageKind.AmuletStatue); if (CapabilityButton("Capybara", FeatureCapability.MirageCapybara, _button, 30)) BeginPing(MirageKind.CapybaraPool); GUILayout.EndHorizontal();
                _multiPlace = GUILayout.Toggle(_multiPlace, "Multi-place"); if (AnimatedButton("CANCEL PLACEMENT", _danger)) _plugin.PingPlacement.Cancel(); GUILayout.Label(_plugin.PingPlacement.Active ? "Aim and use the normal ping input." : "Placement inactive.", _small);
            });
            Card(new Rect(565, 218, 315, 265), "♟  Mirage Scout", new Color(.63f, .42f, 1f), delegate
            {
                Badge(PermissionKind.EveryoneNeedsMod); GUILayout.Label("Copies renderers, meshes, materials and animator only.", _small); Lifetime();
                if (AnimatedButton("STAND & STARE BEHIND", _button, GUILayout.Height(38))) CreateScout(MirageBehavior.StandAndStare);
                if (AnimatedButton("FOLLOW AT DISTANCE", _button, GUILayout.Height(38))) CreateScout(MirageBehavior.FollowAtDistance);
                if (AnimatedButton("RUN AWAY", _button, GUILayout.Height(38))) CreateScout(MirageBehavior.RunAway);
            });
            Card(new Rect(892, 218, 336, 265), "☠  Fake Enemy", new Color(1f, .35f, .42f), delegate
            {
                Badge(PermissionKind.EveryoneNeedsMod); if (AnimatedButton(((FakeEnemyKind)_enemyIndex).ToString() + "  ▾", _button)) _enemyIndex = (_enemyIndex + 1) % 3; Lifetime();
                FeatureCapability fakeCapability=_enemyIndex==0?FeatureCapability.FakeEnemyScoutmaster:_enemyIndex==1?FeatureCapability.FakeEnemyZombie:FeatureCapability.FakeEnemyLooker;
                if (CapabilityButton("SOMETHING'S THERE", fakeCapability, _button, 38)) CreateFake(MirageBehavior.StandAndStare);
                if (CapabilityButton("IT'S FOLLOWING YOU", fakeCapability, _button, 38)) CreateFake(MirageBehavior.FollowAtDistance);
                if (CapabilityButton("VANISH WHEN CLOSE", fakeCapability, _button, 38)) CreateFake(MirageBehavior.VanishWhenClose);
            });
            Card(new Rect(238, 500, 480, 190), "⌖  Phantom Pings", new Color(.35f, .65f, 1f), delegate
            {
                Badge(PermissionKind.EveryoneNeedsMod); GUILayout.Label("Target-only decoy pings; one bounded sequence per viewer.", _small);
                if (AnimatedButton(((PhantomPingPattern)_phantomPingPattern).ToString() + "  ▾", _button, GUILayout.Height(30))) _phantomPingPattern = (_phantomPingPattern + 1) % 3;
                GUILayout.Label("Count " + _phantomPingCount, _small); _phantomPingCount = Mathf.RoundToInt(GUILayout.HorizontalSlider(_phantomPingCount, 3, 12));
                GUILayout.Label("Interval " + _phantomPingInterval.ToString("0.00") + "s", _small); _phantomPingInterval = GUILayout.HorizontalSlider(_phantomPingInterval, .35f, 3f);
                GUILayout.BeginHorizontal(); if (CapabilityButton("START", FeatureCapability.PhantomPings, _button, 34)) Owner(NetCommand.PhantomPings, new object[] { _phantomPingPattern, _phantomPingCount, _phantomPingInterval }); if (AnimatedButton("CANCEL", _danger, GUILayout.Height(34))) Owner(NetCommand.CancelPhantomPings, new object[0]); GUILayout.EndHorizontal();
            });
            Card(new Rect(735, 500, 493, 190), "↶  Mirage Registry", new Color(.3f, 1f, .55f), delegate { GUILayout.Label("Active: " + _plugin.Mirages.Count + "    Fake enemies: " + _plugin.Mirages.FakeEnemyCount, _label); if (AnimatedButton("UNDO LAST LOCAL", _button, GUILayout.Height(34))) _plugin.Mirages.UndoLast(); if (AnimatedButton("CLEAR FAKE ENEMIES LOCAL", _button, GUILayout.Height(34))) _plugin.Mirages.ClearType(MirageKind.FakeEnemy); if (AnimatedButton("CLEAR FOR AUDIENCE", _danger, GUILayout.Height(34))) ClearMirageAudience(); });
        }

        private void DrawAudio(Rect area)
        {
            Card(new Rect(238, 220, 480, 310), "♪  Fake Sound Cue", new Color(.35f, .65f, 1f), delegate
            {
                Badge(PermissionKind.EveryoneNeedsMod); GUILayout.Label("Runtime clip: " + CurrentSoundName(), _label); if (AnimatedButton("NEXT DISCOVERED CLIP", _button)) CycleSound();
                GUILayout.Label("Volume " + _volume.ToString("0.00"), _label); _volume = GUILayout.HorizontalSlider(_volume, 0f, 1f);
                GUILayout.Label("Position is behind the selected player.", _small); if (AnimatedButton("PLAY 3D SOUND", _button, GUILayout.Height(44))) { PlayerEntry t=Target(); if(t!=null){Vector3 p=t.Character.Center-t.Character.data.lookDirection_Flat*3f; Owner(NetCommand.FakeAudio,new object[]{CurrentSoundName(),p.x,p.y,p.z,_volume});} }
                if(AnimatedButton("PLAY LOCALLY NEAR TARGET",_success,GUILayout.Height(36))){PlayerEntry t=Target();if(t!=null){Vector3 p=t.Character.Center-t.Character.data.lookDirection_Flat*3f;Say(_plugin.Audio.PlayDiscovered(CurrentSoundName(),p,_volume,t.ActorNumber));}}
                GUILayout.Label("Clips are referenced from PEAK at runtime and are never redistributed.", _small);
            });
            Card(new Rect(735, 220, 493, 310), "♬  Voice", new Color(.63f, .42f, 1f), delegate { Badge(PermissionKind.Anyone); GUILayout.Label("Local incoming voice control works on unmodded players.", _small); if(CapabilityButton("MUTE SELECTED INCOMING VOICE",FeatureCapability.IncomingVoiceMute,_button,38))MuteTargets(true);if(CapabilityButton("RESTORE SELECTED VOICE",FeatureCapability.IncomingVoiceMute,_success,38))MuteTargets(false);Unsupported("VOICE SWAP / TALK WHILE KO'D", "Safe remote Photon Voice rerouting is not exposed."); });
            Card(new Rect(238, 548, 990, 110), "i  Runtime Discovery", new Color(.3f, 1f, .55f), delegate { GUILayout.Label(_plugin.Audio.Clips.Count + " AudioClip assets currently discovered. Enemy cues are available only when their vanilla clips are loaded.", _label); });
        }

        private void DrawAppearance(Rect area)
        {
            Card(new Rect(238,220,480,330),"●  Local Appearance Swap",new Color(.63f,.42f,1f),delegate
            {
                Badge(PermissionKind.Anyone);GUILayout.Label("Observer-local: selected players do not need the mod.",_small);
                _clothesOnly=GUILayout.Toggle(_clothesOnly,"Clothes only");_colorOnly=GUILayout.Toggle(_colorOnly,"Color only");
                if(CapabilityButton("SWAP WITH RANDOM PLAYER",FeatureCapability.AppearanceSwap,_button,42)){PlayerEntry first=Target();PlayerEntry second=PickPartner(first);Say(_plugin.Appearance.SwapLocal(first,second,_clothesOnly,_colorOnly));}
                if(CapabilityButton("SHUFFLE EVERYONE LOCALLY",FeatureCapability.AppearanceSwap,_button,42))Say(_plugin.Appearance.ShuffleLocal(_plugin.Players.Entries,_clothesOnly,_colorOnly));
                if(AnimatedButton("RESTORE SELECTED",_success,GUILayout.Height(38)))RestoreAppearances();
            });
            Card(new Rect(735,220,493,330),"i  What Other Players See",new Color(.35f,.65f,1f),delegate
            {
                GUILayout.Label("This mode changes only your rendered view of scouts. It never changes Steam, Photon, username, account identity, or the other player's saved cosmetics.",_label);
                GUILayout.Space(12);GUILayout.Label("To confuse another participant, that observer still needs PEAK Troll Mod so their own client can render the swap.",_small);
            });
            Card(new Rect(238,565,990,125),"★  Local Collection Unlocks",new Color(.3f,1f,.55f),delegate
            {
                Badge(PermissionKind.Anyone);GUILayout.BeginHorizontal();
                if(CapabilityButton(_plugin.Progression.CosmeticsEnabled?"ALL COSMETICS: ENABLED":"ENABLE ALL COSMETICS",FeatureCapability.CosmeticUnlocks,_plugin.Progression.CosmeticsEnabled?_success:_button,38))Say(_plugin.Progression.SetCosmetics(!_plugin.Progression.CosmeticsEnabled));
                if(CapabilityButton(_plugin.Progression.BadgesEnabled?"ALL BADGES: ENABLED":"ENABLE ALL BADGES",FeatureCapability.BadgeUnlocks,_plugin.Progression.BadgesEnabled?_success:_button,38))Say(_plugin.Progression.SetBadges(!_plugin.Progression.BadgesEnabled));
                GUILayout.EndHorizontal();GUILayout.Label("Persistent mod toggles only. They do not grant Steam/platform achievements; disable either toggle to restore the real earned state.",_small);
            });
        }
        private void DrawEnemies(Rect area) { UnsupportedPage("Enemy targeting", "Target controls are implemented only at spawn time for mod-spawned Scoutmasters and Mushroom Zombies. Persistent AI overrides are disabled to avoid fighting host authority."); }
        private void DrawWorld(Rect area)
        {
            Card(new Rect(238,220,480,390),"✹  Dynamite Shower",new Color(1f,.42f,.2f),delegate
            {
                Badge(PermissionKind.Anyone);GUILayout.Label("Real networked PEAK dynamite; target needs no mod.",_small);
                GUILayout.Label("Count  "+_dynamiteCount,_label);_dynamiteCount=Mathf.RoundToInt(GUILayout.HorizontalSlider(_dynamiteCount,1,64));
                GUILayout.Label("Height  "+_dynamiteHeight.ToString("0")+"m",_label);_dynamiteHeight=GUILayout.HorizontalSlider(_dynamiteHeight,6f,40f);
                GUILayout.Label("Spread  "+_dynamiteSpread.ToString("0")+"m",_label);_dynamiteSpread=GUILayout.HorizontalSlider(_dynamiteSpread,1f,12f);
                GUILayout.Label("Interval  "+_dynamiteInterval.ToString("0.00")+"s",_label);_dynamiteInterval=GUILayout.HorizontalSlider(_dynamiteInterval,.12f,1.5f);
                _lightDynamite=GUILayout.Toggle(_lightDynamite,"Light fuses before dropping");
                if(CapabilityButton("START DYNAMITE SHOWER",FeatureCapability.DynamiteShower,_danger,42))StartDynamiteShower();
                if(AnimatedButton("CANCEL + CLEAR MY DYNAMITE",_button,GUILayout.Height(34)))Say("Removed "+_plugin.Spawns.ClearLocalDynamite()+" tracked dynamite.");
                GUILayout.Label("Active tracked dynamite: "+_plugin.Spawns.DynamiteCount,_small);
            });
            Card(new Rect(735,220,493,390),"☂  Item Storm",new Color(.35f,.68f,1f),delegate
            {
                Badge(PermissionKind.Anyone);GUILayout.Label("Real networked PEAK items; target needs no mod.",_small);
                _itemStormRandom=GUILayout.Toggle(_itemStormRandom,"Random item from the loaded catalog");
                if(!_itemStormRandom){GUILayout.Label("Selected: "+CurrentItemName(),_small);if(AnimatedButton("CYCLE SELECTED ITEM",_button,GUILayout.Height(30)))CycleItem();}
                GUILayout.Label("Count  "+_itemStormCount,_label);_itemStormCount=Mathf.RoundToInt(GUILayout.HorizontalSlider(_itemStormCount,1,64));
                GUILayout.Label("Height  "+_itemStormHeight.ToString("0")+"m",_label);_itemStormHeight=GUILayout.HorizontalSlider(_itemStormHeight,6f,40f);
                GUILayout.Label("Spread  "+_itemStormSpread.ToString("0")+"m",_label);_itemStormSpread=GUILayout.HorizontalSlider(_itemStormSpread,1f,14f);
                GUILayout.Label("Interval  "+_itemStormInterval.ToString("0.00")+"s",_label);_itemStormInterval=GUILayout.HorizontalSlider(_itemStormInterval,.12f,1.5f);
                if(CapabilityButton("START ITEM STORM",FeatureCapability.ItemStorm,_danger,42))StartItemStorm();
                if(CapabilityButton("MANDRAKE RAIN",FeatureCapability.Mandrake,_danger,42))StartMandrakeRain();
                if(AnimatedButton("CANCEL + CLEAR MY ITEMS",_button,GUILayout.Height(34)))Say("Removed "+_plugin.Spawns.ClearLocalItemStorm()+" tracked storm items.");
                GUILayout.Label("Active tracked storm items: "+_plugin.Spawns.ItemStormCount,_small);
                GUILayout.Label("Poison/Spore clouds remain unsupported; use direct statuses on Player.",_small);
            });
            Card(new Rect(238,625,480,110),"⌂  Campfire Reset Trap",new Color(.3f,1f,.55f),delegate
            {
                Badge(PermissionKind.Anyone);GUILayout.Label("Lighting a campfire returns the lobby to the start.",_small);
                if(CapabilityButton(_plugin.CampfireTroll.Enabled?"ENABLED — CLICK TO DISABLE":"ENABLE CAMPFIRE TRAP",FeatureCapability.CampfireReset,_plugin.CampfireTroll.Enabled?_success:_danger,34)){_plugin.CampfireTroll.Enabled=!_plugin.CampfireTroll.Enabled;Say("Campfire reset trap "+(_plugin.CampfireTroll.Enabled?"enabled.":"disabled."));}
            });
            Card(new Rect(735,625,493,110),"🚁  No Rescue Helicopter",new Color(1f,.65f,.22f),delegate
            {
                Badge(PermissionKind.EveryoneNeedsMod);GUILayout.Label("Suppresses the summit rescue on compatible clients; the host must be compatible to stop completion.",_small);
                if(CapabilityButton(_plugin.HelicopterTroll.LocalEnabled?"SUPPRESSION ENABLED":"SUPPRESS SUMMIT HELICOPTER",FeatureCapability.HelicopterSuppression,_plugin.HelicopterTroll.LocalEnabled?_success:_danger,34))Say(_plugin.HelicopterTroll.SetLocalEnabled(!_plugin.HelicopterTroll.LocalEnabled));
            });
        }

        private void DrawChaos(Rect area)
        {
            Card(new Rect(238,220,315,480),"◈  Chaos Controller",new Color(.63f,.42f,1f),delegate
            {
                GUILayout.Label(_plugin.Chaos.Active ? "CHAOS IS ACTIVE" : "Chaos is stopped", _plugin.Chaos.Active ? _success : _label);
                GUILayout.Label("Minimum delay " + _plugin.Chaos.MinimumDelay.ToString("0") + "s", _label); _plugin.Chaos.MinimumDelay = GUILayout.HorizontalSlider(_plugin.Chaos.MinimumDelay, 3f, 60f);
                GUILayout.Label("Maximum delay " + _plugin.Chaos.MaximumDelay.ToString("0") + "s", _label); _plugin.Chaos.MaximumDelay = GUILayout.HorizontalSlider(_plugin.Chaos.MaximumDelay, 3f, 120f);
                _plugin.Chaos.ExcludeSelf = GUILayout.Toggle(_plugin.Chaos.ExcludeSelf, "Exclude self");
                GUILayout.BeginHorizontal(); if (AnimatedButton("START", _success, GUILayout.Height(46))) { _plugin.Chaos.Start(); Say("Chaos started."); } if (AnimatedButton("STOP", _danger, GUILayout.Height(46))) { _plugin.Chaos.Stop(); Say("Chaos stopped."); } GUILayout.EndHorizontal();
                GUILayout.Space(10);Badge(PermissionKind.Anyone);GUILayout.Label("Wrong Mountain swaps the selected scout with a random different scout, gives both random items, then throws them in opposite directions.",_small);
                if(CapabilityButton("WRONG MOUNTAIN",FeatureCapability.WrongMountain,_danger,42))WrongMountain();
            });
            Card(new Rect(565,220,330,480),"⏱  Chaos Combo Builder",new Color(.35f,.68f,1f),delegate
            {
                Badge(PermissionKind.Anyone);GUILayout.Label("Reusable fixed-order sequence for the selected scout:",_small);
                _plugin.Chaos.ComboMandrakeRain=GUILayout.Toggle(_plugin.Chaos.ComboMandrakeRain,"1. Mandrake Rain");
                _plugin.Chaos.ComboKnockout=GUILayout.Toggle(_plugin.Chaos.ComboKnockout,"2. Instant Knockout");
                _plugin.Chaos.ComboSkyHigh=GUILayout.Toggle(_plugin.Chaos.ComboSkyHigh,"3. Sky High");
                _plugin.Chaos.ComboHorizontalYeet=GUILayout.Toggle(_plugin.Chaos.ComboHorizontalYeet,"4. Horizontal Yeet");
                _plugin.Chaos.ComboOneLiveOne=GUILayout.Toggle(_plugin.Chaos.ComboOneLiveOne,"5. One Live One");
                GUILayout.Label("Delay  "+_plugin.Chaos.ComboStepDelay.ToString("0.0")+"s",_label);_plugin.Chaos.ComboStepDelay=GUILayout.HorizontalSlider(_plugin.Chaos.ComboStepDelay,.5f,10f);
                GUILayout.Label("Runner: "+_plugin.Chaos.ComboStatus,_small);
                GUI.enabled=!_targetAll;if(CapabilityButton("RUN SAVED COMBO",FeatureCapability.ComboBuilder,_danger,42))Say(_plugin.Chaos.StartCombo(Target()));GUI.enabled=true;
                if(AnimatedButton("CANCEL COMBO",_button,GUILayout.Height(34))){_plugin.Chaos.StopCombo();Say("Combo cancelled.");}
                if(_targetAll)GUILayout.Label("Choose one scout; combo sequences do not use ALL.",_small);
            });
            Card(new Rect(907,220,321,480),"☠  Lobby Presets",new Color(1f,.42f,.2f),delegate
            {
                Badge(PermissionKind.Anyone);GUILayout.Label("Position Roulette moves every scout to another scout's former position. Nobody stays put.",_small);
                if(CapabilityButton("POSITION ROULETTE",FeatureCapability.PositionRoulette,_danger,44))Say(_plugin.Network.PositionRoulette(_plugin.Players.Entries));
                GUILayout.Space(16);GUILayout.Label("One Live One drops many convincing duds with exactly one live fuse at a randomized point in the shower.",_small);
                GUILayout.Label("Count  "+_dynamiteCount,_label);_dynamiteCount=Mathf.RoundToInt(GUILayout.HorizontalSlider(_dynamiteCount,2,64));
                if(CapabilityButton("ONE LIVE ONE",FeatureCapability.OneLiveOne,_danger,44))StartOneLiveOne();
                if(AnimatedButton("CANCEL + CLEAR DYNAMITE",_button,GUILayout.Height(34)))Say("Removed "+_plugin.Spawns.ClearLocalDynamite()+" tracked dynamite.");
            });
        }

        private void DrawSettings(Rect area)
        {
            Card(new Rect(238, 218, 480, 380), "⚙  Interface & Safety", new Color(.35f, .65f, 1f), delegate
            {
                GUILayout.Label("Menu key: " + _plugin.Settings.MenuKey.Value, _label); GUILayout.Label("UI scale " + _plugin.Settings.UiScale.Value.ToString("0.00"), _label); _plugin.Settings.UiScale.Value = GUILayout.HorizontalSlider(_plugin.Settings.UiScale.Value, .75f, 1.5f);
                GUILayout.Label("Transparency " + _plugin.Settings.Transparency.Value.ToString("0.00"), _label); _plugin.Settings.Transparency.Value = GUILayout.HorizontalSlider(_plugin.Settings.Transparency.Value, .55f, 1f);
                _plugin.Settings.ConfirmDestructive.Value = GUILayout.Toggle(_plugin.Settings.ConfirmDestructive.Value, "Confirm destructive actions"); _plugin.Settings.ExcludeSelf.Value = GUILayout.Toggle(_plugin.Settings.ExcludeSelf.Value, "Exclude self from random targets");
                GUILayout.Label("Maximum tracked objects " + _plugin.Settings.MaxSpawnedObjects.Value, _label); _plugin.Settings.MaxSpawnedObjects.Value = Mathf.RoundToInt(GUILayout.HorizontalSlider(_plugin.Settings.MaxSpawnedObjects.Value, 4, 64));
                GUILayout.Label("Maximum dynamite objects " + _plugin.Settings.MaxDynamiteObjects.Value, _label); _plugin.Settings.MaxDynamiteObjects.Value = Mathf.RoundToInt(GUILayout.HorizontalSlider(_plugin.Settings.MaxDynamiteObjects.Value, 8, 128));
                GUILayout.Label("Maximum Item Storm objects " + _plugin.Settings.MaxItemStormObjects.Value, _label); _plugin.Settings.MaxItemStormObjects.Value = Mathf.RoundToInt(GUILayout.HorizontalSlider(_plugin.Settings.MaxItemStormObjects.Value, 8, 128));
            });
            Card(new Rect(735, 218, 493, 380), "⌁  Diagnostics", new Color(.63f, .42f, 1f), delegate { _plugin.Settings.DebugLogging.Value = GUILayout.Toggle(_plugin.Settings.DebugLogging.Value, "Debug logging"); _plugin.Settings.NetworkDiagnostics.Value = GUILayout.Toggle(_plugin.Settings.NetworkDiagnostics.Value, "Network diagnostics"); _plugin.Settings.MirageDebug.Value = GUILayout.Toggle(_plugin.Settings.MirageDebug.Value, "Mirage debug info"); _plugin.Settings.FakeEnemyDebug.Value = GUILayout.Toggle(_plugin.Settings.FakeEnemyDebug.Value, "Fake Enemy debug info"); GUILayout.Space(12); if (AnimatedButton("REFRESH CAPABILITIES", _button, GUILayout.Height(40))) { _plugin.Capabilities.RefreshDynamic(); _plugin.Actions.RefreshItemCatalog(); Say("Runtime capabilities refreshed."); } GUILayout.Label("SpongePEAKLib: not used; Photon RaiseEvent provides the required validated mod channel.", _small); });
        }

        private void UnsupportedPage(string title, string reason)
        {
            Card(new Rect(238, 230, 990, 300), "⚠  " + title, new Color(1f, .65f, .22f), delegate { Badge(PermissionKind.Unsupported); GUILayout.Space(12); GUILayout.Label(reason, _label); GUILayout.Space(18); GUILayout.Label("This page remains visible so capability loss after a PEAK update is explicit rather than silently unsafe.", _small); });
        }

        private void DrawStatusBar()
        {
            DrawPanel(new Rect(228, 762, 1012, 48), new Color(.045f, .065f, .067f, .98f), new Color(.18f, .28f, .28f, 1f), 1f);
            GUIStyle style = new GUIStyle(_small); style.alignment = TextAnchor.MiddleLeft; style.normal.textColor = Time.unscaledTime < _messageUntil ? new Color(.48f, .9f, .78f) : new Color(.58f, .67f, .66f); GUI.Label(new Rect(248, 770, 950, 32), _message, style);
        }

        private void Card(Rect rect, string title, Color accent, Action draw)
        {
            DrawPanel(rect, new Color(.055f, .072f, .074f, .98f), new Color(accent.r*.55f,accent.g*.55f,accent.b*.55f,1f), 1f); GUIStyle ts = new GUIStyle(_cardTitle); ts.normal.textColor = accent; GUI.Label(new Rect(rect.x + 14, rect.y + 10, rect.width - 28, 30), title, ts);
            GUILayout.BeginArea(new Rect(rect.x + 14, rect.y + 46, rect.width - 28, rect.height - 56)); Vector2 scroll; if(!_cardScroll.TryGetValue(title,out scroll))scroll=Vector2.zero;scroll=GUILayout.BeginScrollView(scroll,false,false);draw();GUILayout.EndScrollView();_cardScroll[title]=scroll;GUILayout.EndArea();
        }

        private void Badge(PermissionKind permission) { string text = permission == PermissionKind.Anyone ? "🟢 Anyone" : permission == PermissionKind.EveryoneNeedsMod ? "🟡 Everyone Needs Mod" : permission == PermissionKind.HostOnly ? "🔒 Host Only" : "⚠ Unsupported"; GUILayout.Label(text, _badge); }
        private bool CapabilityButton(string text, FeatureCapability capability, GUIStyle style, float height) { bool old=GUI.enabled;GUI.enabled=old&&_plugin.Capabilities.Available(capability);bool clicked=AnimatedButton(text,style,GUILayout.Height(height));GUI.enabled=old;return clicked; }
        private void Unsupported(string name, string reason) { GUI.enabled = false; AnimatedButton(name, _disabled, GUILayout.Height(32)); GUI.enabled = true; GUILayout.Label("⚠ " + reason, _small); }
        private void Distance() { GUILayout.Label("Spawn distance " + _spawnDistance.ToString("0") + "m", _label); _spawnDistance = GUILayout.HorizontalSlider(_spawnDistance, 4f, 25f); }
        private void Lifetime() { GUILayout.Label("Despawn " + _lifetime.ToString("0") + "s", _small); _lifetime = GUILayout.HorizontalSlider(_lifetime, 3f, 60f); }
        private void BeginPing(MirageKind kind) { PlayerEntry target=Target();if(!_targetAll&&target==null){Say("Select a target first.");return;} _plugin.PingPlacement.Begin(kind, _multiPlace, _targetAll?-1:target.ActorNumber); Say("Ping placement armed for " + (_targetAll?"every compatible viewer":target.Name) + ". Close F7, aim, and ping."); ForceClose(); }
        private void RagdollOffMountain(){if(_targetAll){IList<PlayerEntry> list=_plugin.Players.Entries;if(list.Count==0){Say("No targets available.");return;}ActionResult last=null;for(int i=0;i<list.Count;i++)last=_plugin.Network.RagdollOffMountain(list[i],_ragdollStrength);Say(last==null?"No targets available.":"Ragdolled lobby players toward nearby drops. Last result: "+last.Message);return;}PlayerEntry target=Target();if(target==null){Say("No target available.");return;}Say(_plugin.Network.RagdollOffMountain(target,_ragdollStrength));}
        private void HorizontalRagdoll(){if(_targetAll){IList<PlayerEntry> list=_plugin.Players.Entries;if(list.Count==0){Say("No targets available.");return;}ActionResult last=null;for(int i=0;i<list.Count;i++)last=_plugin.Network.HorizontalRagdoll(list[i],_horizontalRagdollStrength);Say(last==null?"No targets available.":"Applied horizontal yeet to lobby players. Last result: "+last.Message);return;}PlayerEntry target=Target();if(target==null){Say("No target available.");return;}Say(_plugin.Network.HorizontalRagdoll(target,_horizontalRagdollStrength));}
        private void SkyLaunch(){if(_targetAll){IList<PlayerEntry> list=_plugin.Players.Entries;if(list.Count==0){Say("No targets available.");return;}ActionResult last=null;for(int i=0;i<list.Count;i++)last=_plugin.Network.SkyLaunch(list[i],_skyHeight);Say(last==null?"No targets available.":"Sent lobby players sky high. Last result: "+last.Message);return;}PlayerEntry target=Target();if(target==null){Say("No target available.");return;}Say(_plugin.Network.SkyLaunch(target,_skyHeight));}
        private void ClearStatuses(){if(_targetAll){IList<PlayerEntry> list=_plugin.Players.Entries;if(list.Count==0){Say("No targets available.");return;}ActionResult last=null;for(int i=0;i<list.Count;i++)last=_plugin.Network.ClearStatuses(list[i]);Say(last==null?"No targets available.":"Cleared tracked statuses for eligible players. Last result: "+last.Message);return;}PlayerEntry target=Target();if(target==null){Say("No target available.");return;}Say(_plugin.Network.ClearStatuses(target));}
        private void StartDynamiteShower(){if(_targetAll){IList<PlayerEntry> list=_plugin.Players.Entries;if(list.Count==0){Say("No targets available.");return;}ActionResult last=null;for(int i=0;i<list.Count;i++)last=_plugin.Spawns.StartDynamiteShower(list[i],_dynamiteCount,_dynamiteHeight,_dynamiteSpread,_dynamiteInterval,_lightDynamite);Say(last==null?"No targets available.":"Queued showers for eligible lobby players. Last result: "+last.Message);return;}PlayerEntry target=Target();if(target==null){Say("No target available.");return;}Say(_plugin.Spawns.StartDynamiteShower(target,_dynamiteCount,_dynamiteHeight,_dynamiteSpread,_dynamiteInterval,_lightDynamite));}
        private void StartOneLiveOne(){if(_targetAll){IList<PlayerEntry> list=_plugin.Players.Entries;if(list.Count==0){Say("No targets available.");return;}ActionResult last=null;for(int i=0;i<list.Count;i++)last=_plugin.Spawns.StartOneLiveOne(list[i],_dynamiteCount,_dynamiteHeight,_dynamiteSpread,_dynamiteInterval);Say(last==null?"No targets available.":"Queued One Live One for eligible lobby players. Last result: "+last.Message);return;}PlayerEntry target=Target();if(target==null){Say("No target available.");return;}Say(_plugin.Spawns.StartOneLiveOne(target,_dynamiteCount,_dynamiteHeight,_dynamiteSpread,_dynamiteInterval));}
        private void StartItemStorm(){string item=CurrentItemName();if(_targetAll){IList<PlayerEntry> list=_plugin.Players.Entries;if(list.Count==0){Say("No targets available.");return;}ActionResult last=null;for(int i=0;i<list.Count;i++)last=_plugin.Spawns.StartItemStorm(list[i],_itemStormCount,_itemStormHeight,_itemStormSpread,_itemStormInterval,_itemStormRandom,item);Say(last==null?"No targets available.":"Queued Item Storms for eligible lobby players. Last result: "+last.Message);return;}PlayerEntry target=Target();if(target==null){Say("No target available.");return;}Say(_plugin.Spawns.StartItemStorm(target,_itemStormCount,_itemStormHeight,_itemStormSpread,_itemStormInterval,_itemStormRandom,item));}
        private void StartMandrakeRain(){if(_targetAll){IList<PlayerEntry> list=_plugin.Players.Entries;if(list.Count==0){Say("No targets available.");return;}ActionResult last=null;for(int i=0;i<list.Count;i++)last=_plugin.Spawns.StartMandrakeRain(list[i],_itemStormCount,_itemStormHeight,_itemStormSpread,_itemStormInterval);Say(last==null?"No targets available.":"Queued Mandrake Rain for eligible lobby players. Last result: "+last.Message);return;}PlayerEntry target=Target();if(target==null){Say("No target available.");return;}Say(_plugin.Spawns.StartMandrakeRain(target,_itemStormCount,_itemStormHeight,_itemStormSpread,_itemStormInterval));}
        private void WrongMountain(){if(_targetAll){Say("Wrong Mountain needs one selected scout so it can choose a different partner.");return;}PlayerEntry first=Target();PlayerEntry second=PickRandomPartner(first);if(first==null||second==null){Say("Wrong Mountain needs at least two available scouts.");return;}Say(_plugin.Network.WrongMountain(first,second,_horizontalRagdollStrength));}
        private void ClearMirageAudience(){if(_targetAll){IList<PlayerEntry> list=_plugin.Players.Entries;for(int i=0;i<list.Count;i++)_plugin.Network.SendToOwner(NetCommand.ClearMirages,list[i],new object[0]);Say("Cleared mirages for all compatible viewers.");return;}PlayerEntry target=Target();if(target==null){Say("No target available.");return;}Say(_plugin.Network.SendToOwner(NetCommand.ClearMirages,target,new object[0]));}
        private void CreateScout(MirageBehavior behavior) { PlayerEntry t=Target(); if(t==null)return; Vector3 p=t.Character.Center-t.Character.data.lookDirection_Flat*8f; Owner(NetCommand.MirageScout,new object[]{t.ActorNumber,p.x,p.y,p.z,(int)behavior,_lifetime}); }
        private void CreateFake(MirageBehavior behavior) { PlayerEntry t=Target(); if(t==null)return; Vector3 p=t.Character.Center-t.Character.data.lookDirection_Flat*8f; Owner(NetCommand.FakeEnemy,new object[]{_enemyIndex,p.x,p.y,p.z,(int)behavior,_lifetime}); }
        private void Teleport(Vector3 p) { Owner(NetCommand.Teleport, new object[] { p.x, p.y, p.z }); }
        private void TeleportSelfToTarget(){PlayerEntry destination=Target();PlayerEntry local=_plugin.Players.Local;if(destination==null||local==null){Say("Player unavailable.");return;}Vector3 p=destination.Character.Center;Say(_plugin.Network.SendToOwner(NetCommand.Teleport,local,new object[]{p.x,p.y,p.z}));}
        private void MuteTargets(bool muted){if(_targetAll){IList<PlayerEntry> list=_plugin.Players.Entries;for(int i=0;i<list.Count;i++)_plugin.Audio.SetIncomingVoiceMuted(list[i],muted);Say(muted?"Muted all available incoming voices locally.":"Restored all available incoming voices.");return;}Say(_plugin.Audio.SetIncomingVoiceMuted(Target(),muted));}
        private void RestoreAppearances(){if(_targetAll){_plugin.Appearance.RestoreAll(_plugin.Players.Entries);Say("Restored all actual appearances locally.");return;}Say(_plugin.Appearance.Restore(Target()));}
        private void ClearTrollSpawns(){if(_plugin.Players.IsHost){_plugin.Spawns.ClearAll();Say("All tracked troll spawns cleared.");}else Say(_plugin.Network.RequestClearMySpawns());}
        private void Owner(NetCommand command, object[] args) { if(_targetAll){IList<PlayerEntry> list=_plugin.Players.Entries;if(list.Count==0){Say("No targets available.");return;}ActionResult last=null;for(int i=0;i<list.Count;i++)last=_plugin.Network.SendToOwner(command,list[i],args);Say(last==null?"No targets available.":"Applied to eligible lobby players. Last result: "+last.Message);return;} PlayerEntry t=Target(); if(t==null){Say("No target available.");return;} Say(_plugin.Network.SendToOwner(command,t,args)); }
        private void Broadcast(NetCommand command, object[] args) { if(_targetAll){IList<PlayerEntry> list=_plugin.Players.Entries;for(int i=0;i<list.Count;i++)_plugin.Network.SendToAll(command,list[i].ActorNumber,args);Say("Broadcast for all lobby players.");return;} PlayerEntry t=Target(); if(t==null){Say("No target available.");return;} Say(_plugin.Network.SendToAll(command,t.ActorNumber,args)); }
        private PlayerEntry Target() { IList<PlayerEntry> list=_plugin.Players.Entries; if(list.Count==0)return null; if(_targetIndex>=list.Count)_targetIndex=0; return list[_targetIndex]; }
        private string CurrentTargetName() { if(_targetAll)return "Everyone";PlayerEntry t=Target(); return t==null?"No players":t.ToString(); }
        private void CycleTarget() { _targetAll=false;if(_plugin.Players.Entries.Count>0)_targetIndex=(_targetIndex+1)%_plugin.Players.Entries.Count; }
        private void SelectRandomTarget(){PlayerEntry r=_plugin.Players.Random(_plugin.Settings.ExcludeSelf.Value);if(r==null)return;_targetAll=false;for(int i=0;i<_plugin.Players.Entries.Count;i++)if(_plugin.Players.Entries[i].ActorNumber==r.ActorNumber){_targetIndex=i;break;}}
        private PlayerEntry PickPartner(PlayerEntry first){IList<PlayerEntry> list=_plugin.Players.Entries;if(list.Count<2)return null;for(int offset=1;offset<=list.Count;offset++){PlayerEntry candidate=list[(_targetIndex+offset)%list.Count];if(first==null||candidate.ActorNumber!=first.ActorNumber)return candidate;}return null;}
        private PlayerEntry PickRandomPartner(PlayerEntry first){IList<PlayerEntry> list=_plugin.Players.Entries;if(first==null||list.Count<2)return null;List<PlayerEntry> candidates=new List<PlayerEntry>();for(int i=0;i<list.Count;i++)if(list[i]!=null&&list[i].Character!=null&&list[i].ActorNumber!=first.ActorNumber)candidates.Add(list[i]);return candidates.Count==0?null:candidates[UnityEngine.Random.Range(0,candidates.Count)];}
        private List<Item> FilteredItems(){List<Item> result=new List<Item>();IList<Item> items=_plugin.Actions.Items;for(int i=0;i<items.Count;i++)if(string.IsNullOrEmpty(_itemSearch)||items[i].name.IndexOf(_itemSearch,StringComparison.OrdinalIgnoreCase)>=0)result.Add(items[i]);return result;}
        private string CurrentItemName() { List<Item> items=FilteredItems(); if(items.Count==0)return "No matching runtime items"; if(_itemIndex>=items.Count)_itemIndex=0; return items[_itemIndex].name; }
        private void CycleItem() { List<Item> items=FilteredItems();if(items.Count>0)_itemIndex=(_itemIndex+1)%items.Count; }
        private void SelectItemContaining(string token) { IList<Item> items=_plugin.Actions.Items; for(int i=0;i<items.Count;i++)if(items[i].name.IndexOf(token,StringComparison.OrdinalIgnoreCase)>=0){_itemIndex=i;Say("Selected "+items[i].name+".");return;}Say(token+" is not loaded in the runtime item catalog."); }
        private string CurrentSoundName() { IList<AudioClip> clips=_plugin.Audio.Clips; if(clips.Count==0)return "No runtime clips"; if(_soundIndex>=clips.Count)_soundIndex=0; return clips[_soundIndex].name; }
        private void CycleSound() { if(_plugin.Audio.Clips.Count>0)_soundIndex=(_soundIndex+1)%_plugin.Audio.Clips.Count; }
        private void Say(ActionResult result) { Say(result.Message); }
        private void Say(string text) { _message=text; _messageUntil=Time.unscaledTime+6f; }
        private string SubtitleForTab() { string[] s={"Session overview, capabilities, and emergency cleanup.","Target, manipulate, and mess with players in your lobby.","Host-authorized genuine enemy ambushes with strict tracking.","Harmless visual decoys, fake enemies, and ping placement.","Runtime-discovered 3D sound cues and voice capability status.","Cosmetic confusion without identity impersonation.","Controls for mod-spawned genuine enemies.","Environmental hazards separated from direct statuses.","Bounded randomized events with compatible-client validation.","Interface, safety limits, and diagnostics."}; return s[_tab]; }
        private string HeadingForTab() { return _tab == 1 ? "PLAYER CONTROLS" : _tabNames[_tab].ToUpperInvariant(); }
        private void EmergencyReset(){for(int i=0;i<_plugin.Players.Entries.Count;i++)_plugin.Network.SendToAll(NetCommand.Reset,_plugin.Players.Entries[i].ActorNumber,new object[0]);if(_plugin.Players.Local!=null)_plugin.Network.SendToAll(NetCommand.ClearMirages,_plugin.Players.Local.ActorNumber,new object[0]);_plugin.Reset.ResetAll();Say("Cleanup and restoration completed on compatible clients.");}

        private void EnsureStyles()
        {
            if (_pixel != null) return; _pixel = new Texture2D(1,1); _pixel.SetPixel(0,0,Color.white); _pixel.Apply();
            _logo=Style(25,FontStyle.Bold,Color.white,TextAnchor.MiddleLeft); _title=Style(31,FontStyle.Bold,Color.white,TextAnchor.MiddleLeft); _subtitle=Style(13,FontStyle.Normal,new Color(.58f,.67f,.67f),TextAnchor.MiddleLeft);
            _label=Style(14,FontStyle.Normal,new Color(.9f,.94f,.93f),TextAnchor.MiddleLeft); _small=Style(12,FontStyle.Normal,new Color(.58f,.67f,.66f),TextAnchor.MiddleLeft); _cardTitle=Style(18,FontStyle.Bold,Color.white,TextAnchor.MiddleLeft); _badge=Style(11,FontStyle.Bold,new Color(.72f,.78f,.76f),TextAnchor.MiddleLeft);
            _nav=ButtonStyle(15,new Color(.65f,.72f,.71f),new Color(.025f,.035f,.038f,1f)); _nav.alignment=TextAnchor.MiddleLeft; _nav.padding=new RectOffset(18,8,0,0);
            _navSelected=ButtonStyle(15,Color.white,new Color(.08f,.28f,.28f,.98f)); _navSelected.alignment=TextAnchor.MiddleLeft; _navSelected.padding=new RectOffset(18,8,0,0);
            _button=ButtonStyle(13,new Color(.92f,.95f,.94f),new Color(.105f,.135f,.14f,1f)); _danger=ButtonStyle(13,Color.white,new Color(.55f,.20f,.16f,1f)); _success=ButtonStyle(13,Color.white,new Color(.10f,.38f,.36f,1f)); _disabled=ButtonStyle(13,new Color(.4f,.46f,.45f),new Color(.065f,.08f,.082f,1f));
        }
        private GUIStyle Style(int size, FontStyle font, Color color, TextAnchor align) { GUIStyle s=new GUIStyle(GUI.skin.label);s.fontSize=size;s.fontStyle=font;s.normal.textColor=color;s.alignment=align;s.wordWrap=true;return s; }
        private GUIStyle ButtonStyle(int size, Color textColor, Color bg) { GUIStyle s=new GUIStyle(GUI.skin.button);s.fontSize=size;s.fontStyle=FontStyle.Bold;s.normal.textColor=textColor;s.hover.textColor=Color.white;s.active.textColor=Color.white;s.normal.background=Tint(bg);s.hover.background=Tint(new Color(Mathf.Min(1f,bg.r+.09f),Mathf.Min(1f,bg.g+.16f),Mathf.Min(1f,bg.b+.15f),bg.a));s.active.background=Tint(new Color(Mathf.Min(1f,bg.r+.16f),Mathf.Min(1f,bg.g+.22f),Mathf.Min(1f,bg.b+.20f),bg.a));s.padding=new RectOffset(10,10,6,6);return s; }
        private bool AnimatedButton(string text, GUIStyle style, params GUILayoutOption[] options) { Rect rect=GUILayoutUtility.GetRect(new GUIContent(text),style,options);return AnimatedButton(rect,text,style); }
        private bool AnimatedButton(Rect rect, string text, GUIStyle style)
        {
            string key=_tab+"|"+text+"|"+Mathf.RoundToInt(rect.x)+"|"+Mathf.RoundToInt(rect.y);float hover;_buttonHover.TryGetValue(key,out hover);bool over=rect.Contains(Event.current.mousePosition)&&GUI.enabled;hover=Mathf.MoveTowards(hover,over?1f:0f,Time.unscaledDeltaTime*9f);_buttonHover[key]=hover;
            float until;_buttonPulseUntil.TryGetValue(key,out until);float pulse=until>Time.unscaledTime?Mathf.Clamp01((until-Time.unscaledTime)/.16f):0f;float glow=Mathf.Max(hover,pulse);if(glow>.01f){float spread=1f+glow*3f;DrawPanel(new Rect(rect.x-spread,rect.y-spread,rect.width+spread*2f,rect.height+spread*2f),new Color(.12f,.48f,.45f,.10f*glow),new Color(.31f,.92f,.85f,.75f*glow),1f);}
            Rect animated=rect;if(pulse>0f){float inset=Mathf.Sin((1f-pulse)*Mathf.PI)*1.5f;animated=new Rect(rect.x+inset,rect.y+inset,rect.width-inset*2f,rect.height-inset*2f);}bool clicked=GUI.Button(animated,text,style);if(clicked)_buttonPulseUntil[key]=Time.unscaledTime+.16f;return clicked;
        }
        private Texture2D Tint(Color color) { Texture2D t=new Texture2D(1,1);t.SetPixel(0,0,color);t.Apply();return t; }
        private void DrawPanel(Rect rect, Color fill, Color border, float width) { DrawRect(rect,border);DrawRect(new Rect(rect.x+width,rect.y+width,rect.width-width*2,rect.height-width*2),fill); }
        private void Line(Rect rect, Color color) { DrawRect(rect,color); }
        private void DrawRect(Rect rect, Color color) { Color old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,_pixel);GUI.color=old; }
    }
}
