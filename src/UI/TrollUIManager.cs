using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class TrollUIManager
    {
        private const float DesignWidth = 1280f;
        private const float DesignHeight = 850f;
        private readonly TrollModPlugin _plugin;
        private readonly string[] _tabs = { "⌂  Home", "♟  Player", "▣  Spawning", "✦  Mirage", "♪  Audio", "●  Appearance", "☠  Enemies", "▲  World", "◈  Chaos", "◎  Lobby", "⚙  Settings", "☷  Mod Config", "▦  Items" };
        private readonly string[] _tabNames = { "Home", "Player", "Spawning", "Mirage", "Audio", "Appearance", "Enemies", "World", "Chaos Mode", "Lobby Readiness", "Settings", "Mod Config", "Item Spawner" };
        private readonly string[] _previewEffects = { "Launch", "Ragdoll", "Flight", "Status Effect", "Phantom Pings", "Fake Enemy", "Item Storm" };
        private int _tab;
        private int _targetIndex;
        private int _statusIndex = 3;
        private int _itemIndex;
        private int _soundIndex;
        private int _enemyIndex;
        private bool _targetAll;
        private string _itemSearch = string.Empty;
        private int _itemQuantity = 1;
        private int _effectPreviewIndex;
        private float _effectPreviewUntil;
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
        private int _modIndex;
        private int _configIndex;
        private string _configEdit = string.Empty;
        private string _configEditKey = string.Empty;
        private ulong _preferenceSteamId;
        private string _preferenceAlias = string.Empty;
        private float _preferenceVolume = 1f;
        private bool _preferenceMuted;
        private bool _preferenceExcluded;
        private Color _preferenceColor = new Color(.35f,.85f,.78f,1f);
        private string _message = "Ready. Private lobbies, compatible friends, sensible chaos.";
        private float _messageUntil;
        private CursorLockMode _oldLock;
        private bool _oldCursor;
        private GUIStyle _title, _logo, _subtitle, _nav, _navSelected, _label, _small, _button, _danger, _success, _disabled, _cardTitle, _badge;
        private Texture2D _pixel;
        private readonly List<Texture2D> _styleTextures = new List<Texture2D>();
        private float _styledTextScale;
        private bool _styledHighContrast;
        private float _nextDrawErrorLog;
        private readonly Dictionary<string, Vector2> _cardScroll = new Dictionary<string, Vector2>();
        private readonly Dictionary<string, float> _buttonHover = new Dictionary<string, float>();
        private readonly Dictionary<string, float> _buttonPulseUntil = new Dictionary<string, float>();
        private int _textFieldIndex;
        private bool _textFieldClicked;
        private bool _isTyping;
        private bool _clearTextFocus;
        public bool IsOpen { get; private set; }
        public bool IsTyping { get { return IsOpen && _isTyping; } }

        public TrollUIManager(TrollModPlugin plugin) { _plugin = plugin; }

        public void Toggle() { if (IsOpen) ForceClose(); else Open(); }
        public void SetOpen(bool open) { if(open&&!IsOpen)Open();else if(!open&&IsOpen)ForceClose(); }
        private void Open() { _oldLock = Cursor.lockState; _oldCursor = Cursor.visible; IsOpen = true; _isTyping = false; _clearTextFocus = true; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        public void ForceClose() { if (!IsOpen) return; IsOpen = false; _isTyping = false; _clearTextFocus = true; Cursor.lockState = _oldLock; Cursor.visible = _oldCursor; _confirmEliminate = false; }
        public void Tick() { if (IsOpen) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; if (_isTyping && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))) { _clearTextFocus = true; _isTyping = false; } } }

        public void ResetVisualAssets()
        {
            if (_pixel != null) UnityEngine.Object.Destroy(_pixel);
            _pixel = null;
            for (int i = 0; i < _styleTextures.Count; i++) if (_styleTextures[i] != null) UnityEngine.Object.Destroy(_styleTextures[i]);
            _styleTextures.Clear();
            _title=null;_logo=null;_subtitle=null;_nav=null;_navSelected=null;_label=null;_small=null;_button=null;_danger=null;_success=null;_disabled=null;_cardTitle=null;_badge=null;
            _buttonHover.Clear(); _buttonPulseUntil.Clear();
        }

        public void Draw()
        {
            if (!IsOpen) return;
            Matrix4x4 oldMatrix=GUI.matrix;Color oldColor=GUI.color;bool oldEnabled=GUI.enabled;int oldDepth=GUI.depth;
            try
            {
                _textFieldIndex=0;_textFieldClicked=false;GUI.enabled=true;GUI.color=Color.white;GUI.depth=-10000;if(_clearTextFocus){GUI.FocusControl(null);_clearTextFocus=false;}Cursor.lockState=CursorLockMode.None;Cursor.visible=true;EnsureStyles();
                float scale = Mathf.Min(Screen.width / DesignWidth, Screen.height / DesignHeight) * Mathf.Clamp(_plugin.Settings.UiScale.Value, .75f, 1.5f);
                float x = (Screen.width - DesignWidth * scale) * .5f; float y = (Screen.height - DesignHeight * scale) * .5f;
                DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(.01f, .016f, .018f, .76f));
                GUI.matrix = Matrix4x4.TRS(new Vector3(x, y, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
                DrawPanel(new Rect(10, 10, 1260, 830), new Color(.035f, .047f, .05f, Mathf.Clamp(_plugin.Settings.Transparency.Value, .55f, 1f)), new Color(.25f, .34f, .35f, 1f), 2f);
                DrawHeader(); DrawSidebar(); DrawContent(); DrawStatusBar(); UpdateTextFocus();
            }
            catch(Exception ex)
            {
                _isTyping=false;ResetVisualAssets();_message="Menu visuals were reloaded after a UI asset error.";_messageUntil=Time.unscaledTime+8f;
                if(Time.unscaledTime>=_nextDrawErrorLog){_nextDrawErrorLog=Time.unscaledTime+5f;_plugin.Warn("Menu draw recovered from: "+ex);}
            }
            finally { GUI.matrix=oldMatrix;GUI.color=oldColor;GUI.enabled=oldEnabled;GUI.depth=oldDepth; }
        }

        private void DrawHeader()
        {
            GUIStyle brand = new GUIStyle(_logo); brand.normal.textColor = new Color(.28f, .78f, .75f); GUI.Label(new Rect(42, 24, 150, 32), "PEAK", brand);
            GUIStyle brandSub = new GUIStyle(_logo); brandSub.fontSize = 21; GUI.Label(new Rect(42, 50, 150, 54), "TROLL\nMOD", brandSub);
            GUI.Label(new Rect(238, 29, 520, 44), HeadingForTab(), _title);
            GUI.Label(new Rect(240, 74, 570, 24), SubtitleForTab(), _subtitle);
            if (_tab != 0 && (_tab < 9 || _tab == 12))
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
            for (int i = 0; i < _tabs.Length; i++) if (AnimatedButton(new Rect(24, 132 + i * 47, 180, 41), _tabs[i], i == _tab ? _navSelected : _nav)) _tab = i;
            GUI.Label(new Rect(35, 752, 165, 24), "Version " + TrollModPlugin.Version, _small);
        }

        private void DrawContent()
        {
            Rect area = new Rect(220, 128, 1030, 622);
            if (_tab == 0) DrawHome(area); else if (_tab == 1) DrawPlayer(area); else if (_tab == 2) DrawSpawning(area); else if (_tab == 3) DrawMirage(area); else if (_tab == 4) DrawAudio(area); else if (_tab == 5) DrawAppearance(area); else if (_tab == 6) DrawEnemies(area); else if (_tab == 7) DrawWorld(area); else if (_tab == 8) DrawChaos(area); else if (_tab == 9) DrawLobby(area); else if (_tab == 10) DrawSettings(area); else if (_tab == 11) DrawModConfig(area); else DrawItems(area);
        }

        private void DrawItems(Rect area)
        {
            if (!_plugin.Settings.ItemSpawnerEnabled.Value) { UnsupportedPage("ITEM SPAWNER DISABLED", "Enable Item Spawner under Mod Config → Built-in Quality of Life."); return; }
            Card(new Rect(238, 142, 990, 78), "▦  Runtime Item Search", new Color(.28f, .78f, .75f), delegate
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Search", _label, GUILayout.Width(58));
                _itemSearch = TextField(_itemSearch, _button, GUILayout.Height(30));
                if (AnimatedButton("CLEAR", _button, GUILayout.Width(72), GUILayout.Height(30))) { _itemSearch = string.Empty; _itemIndex = 0; }
                if (AnimatedButton("REFRESH", _button, GUILayout.Width(88), GUILayout.Height(30))) { _plugin.Actions.RefreshItemCatalog(); _itemIndex = 0; Say("Runtime item catalog refreshed."); }
                GUILayout.EndHorizontal();
            });
            Card(new Rect(238, 234, 430, 486), "☷  Item Catalog", new Color(.35f, .65f, 1f), delegate
            {
                List<Item> items = FilteredItems();
                GUILayout.Label(items.Count + " matching item(s)", _small);
                if (_plugin.Settings.FavoritesAndRecentsEnabled.Value)
                {
                    DrawItemShortcuts("★ Favorites", _plugin.Shortcuts.FavoriteItems);
                    DrawItemShortcuts("↻ Recent", _plugin.Shortcuts.RecentItems);
                    GUILayout.Space(6);
                }
                for (int i = 0; i < items.Count; i++)
                {
                    string name = items[i] == null ? string.Empty : items[i].name;
                    if (string.IsNullOrEmpty(name)) continue;
                    if (AnimatedButton((i == _itemIndex ? "●  " : "○  ") + name, i == _itemIndex ? _navSelected : _button, GUILayout.Height(29))) _itemIndex = i;
                }
            });
            Card(new Rect(684, 234, 544, 238), "◆  Selected Item", new Color(.86f, .62f, .25f), delegate
            {
                string item = CurrentItemName(); bool valid = FilteredItems().Count > 0;
                GUILayout.Label(item, _cardTitle);
                GUILayout.Label("Discovered from PEAK's currently loaded runtime item catalog. Nothing is bundled or guessed.", _small);
                GUILayout.BeginHorizontal(); GUILayout.Label("Quantity  " + _itemQuantity, _label, GUILayout.Width(105)); _itemQuantity = Mathf.RoundToInt(GUILayout.HorizontalSlider(_itemQuantity, 1, 12)); GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUI.enabled = valid;
                if (CapabilityButton("GIVE TO TARGET", FeatureCapability.GiveItem, _success, 34)) GiveSelectedItem(false);
                if (CapabilityButton("GIVE TO ME", FeatureCapability.GiveItem, _button, 34)) GiveSelectedItem(true);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUI.enabled = valid && !_targetAll;
                if (CapabilityButton("SPAWN ON GROUND NEAR TARGET", FeatureCapability.ItemStorm, _button, 34)) SpawnSelectedItem();
                GUI.enabled = true;
                if (_plugin.Settings.FavoritesAndRecentsEnabled.Value && AnimatedButton(_plugin.Shortcuts.IsFavoriteItem(item) ? "★ REMOVE FAVORITE" : "☆ ADD FAVORITE", _button, GUILayout.Height(30))) Say((_plugin.Shortcuts.ToggleFavoriteItem(item) ? "Favorited " : "Removed favorite ") + item + ".");
            });
            Card(new Rect(684, 488, 544, 232), "✦  Safe Effect Preview", new Color(.63f, .42f, 1f), delegate
            {
                if (!_plugin.Settings.EffectPreviewEnabled.Value) { GUILayout.Label("Effect Preview is disabled in Mod Config.", _label); return; }
                string effect = _previewEffects[_effectPreviewIndex];
                GUILayout.BeginHorizontal(); if (AnimatedButton("‹", _button, GUILayout.Width(40), GUILayout.Height(30))) _effectPreviewIndex = (_effectPreviewIndex + _previewEffects.Length - 1) % _previewEffects.Length; GUILayout.Label(effect, _cardTitle); if (AnimatedButton("›", _button, GUILayout.Width(40), GUILayout.Height(30))) _effectPreviewIndex = (_effectPreviewIndex + 1) % _previewEffects.Length; GUILayout.EndHorizontal();
                GUILayout.Label(EffectPreviewText(effect), _label);
                GUILayout.Label("Target: " + CurrentTargetName() + "   •   Preview never executes or networks the effect.", _small);
                if (Time.unscaledTime < _effectPreviewUntil) GUILayout.Label("PREVIEW  " + PreviewGlyph(effect) + "  " + EffectPreviewValues(effect), _success);
                GUILayout.BeginHorizontal(); if (AnimatedButton("PREVIEW SAFELY", _success, GUILayout.Height(32))) PreviewEffect(effect); if (AnimatedButton(_plugin.Shortcuts.IsFavoriteEffect(effect) ? "★" : "☆", _button, GUILayout.Width(48), GUILayout.Height(32))) Say((_plugin.Shortcuts.ToggleFavoriteEffect(effect) ? "Favorited " : "Removed favorite ") + effect + "."); if (AnimatedButton("OPEN CONTROLS", _button, GUILayout.Height(32))) OpenEffectControls(effect); GUILayout.EndHorizontal();
                if (_plugin.Settings.FavoritesAndRecentsEnabled.Value) { GUILayout.Label("★ Favorites",_small); DrawEffectShortcuts(_plugin.Shortcuts.FavoriteEffects); GUILayout.Label("↻ Recent",_small); DrawEffectShortcuts(_plugin.Shortcuts.RecentEffects); }
            });
        }

        private void DrawHome(Rect area)
        {
            Card(new Rect(238, 218, 310, 202), "◆  Session", new Color(.25f, .6f, 1f), delegate
            {
                GUILayout.Label("PEAK version: " + Application.version, _label); GUILayout.Label("Host: " + (_plugin.Players.IsHost ? "Yes" : "No"), _label);
                GUILayout.Label("Local: " + (_plugin.Players.Local == null ? "Not resolved" : _plugin.Players.Local.Name), _label); GUILayout.Label("Lobby players: " + _plugin.Players.Entries.Count, _label);
                GUILayout.Label("Compatible clients: " + _plugin.Network.CompatibleCount, _label);
                GUILayout.Label("Last Steam lobby: " + _plugin.QuickReconnect.LastLobbyDisplay, _small);
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
            Card(new Rect(238, 438, 990, 300), "↻  Reconnect & Emergency Recovery", new Color(.25f, 1f, .5f), delegate
            {
                GUILayout.BeginHorizontal();
                if (CapabilityButton("RECONNECT LAST LOBBY", FeatureCapability.QuickReconnect, _success, 40)) ReconnectLastLobby();
                if (CapabilityButton("LAST SAFE GROUND", FeatureCapability.EmergencyRecovery, _button, 40)) Say(_plugin.Recovery.RecoverLastSafeGround());
                if (CapabilityButton("NEAREST SAFE SCOUT", FeatureCapability.EmergencyRecovery, _button, 40)) Say(_plugin.Recovery.RecoverNearestScout());
                if (CapabilityButton("ACTIVE CHECKPOINT", FeatureCapability.EmergencyRecovery, _button, 40)) Say(_plugin.Recovery.RecoverCheckpoint());
                GUILayout.EndHorizontal();
                GUILayout.Space(8);
                GUILayout.BeginHorizontal();
                if (CapabilityButton("STOP FLIGHT + VELOCITY", FeatureCapability.EmergencyRecovery, _button, 38)) Say(_plugin.Recovery.StabilizeSelf());
                if (CapabilityButton("RESTORE SELF", FeatureCapability.EmergencyRecovery, _button, 38)) Say(_plugin.Recovery.RestoreSelf());
                GUILayout.EndHorizontal();
                GUILayout.Space(12);
                GUILayout.BeginHorizontal(); if (AnimatedButton("STOP CHAOS", _danger, GUILayout.Height(42))) { _plugin.Chaos.Stop(); _plugin.Chaos.StopCombo(); Say("Chaos and combo runner stopped."); } if (AnimatedButton("CLEAR MIRAGES", _button, GUILayout.Height(42))) { _plugin.Mirages.ClearAll(); Say("All local mirages cleared."); } if (AnimatedButton("CLEAR TROLL SPAWNS", _button, GUILayout.Height(42))) ClearTrollSpawns(); GUILayout.EndHorizontal();
                GUILayout.Space(8); if (AnimatedButton("RESET ALL TROLL EFFECTS", _danger, GUILayout.Height(42))) EmergencyReset();
                GUILayout.Label("Recovery uses recorded safe ground and native PEAK warps. Cleanup only touches state and objects tracked by this mod.", _small);
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
                Badge(PermissionKind.Anyone);
                GUILayout.BeginHorizontal();
                if (AnimatedButton(_plugin.SurvivalAssist.Immortality ? "IMMORTALITY: ON" : "IMMORTALITY: OFF", _plugin.SurvivalAssist.Immortality ? _success : _button, GUILayout.Height(34))) { _plugin.SurvivalAssist.Immortality = !_plugin.SurvivalAssist.Immortality; Say("Immortality " + (_plugin.SurvivalAssist.Immortality ? "enabled." : "disabled.")); }
                if (AnimatedButton(_plugin.SurvivalAssist.InfiniteStamina ? "INFINITE STAMINA: ON" : "INFINITE STAMINA: OFF", _plugin.SurvivalAssist.InfiniteStamina ? _success : _button, GUILayout.Height(34))) { _plugin.SurvivalAssist.InfiniteStamina = !_plugin.SurvivalAssist.InfiniteStamina; Say("Infinite Stamina " + (_plugin.SurvivalAssist.InfiniteStamina ? "enabled." : "disabled.")); }
                GUILayout.EndHorizontal();
                GUILayout.Label("Self-only survival options; switch off to restore normal rules immediately.", _small);
                GUILayout.BeginHorizontal();
                if (CapabilityButton(_plugin.NoWait.Enabled ? "NO WAIT: ON" : "NO WAIT: OFF", FeatureCapability.Resurrect, _plugin.NoWait.Enabled ? _success : _button, 38)) { _plugin.NoWait.Enabled = !_plugin.NoWait.Enabled; Say("No Wait " + (_plugin.NoWait.Enabled ? "enabled." : "disabled.")); }
                if (CapabilityButton("RESURRECT SELF", FeatureCapability.Resurrect, _success, 38)) ResurrectSelf();
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                if (CapabilityButton("DESTINATION: " + NoWaitDestinationName(), FeatureCapability.Resurrect, _button, 34)) CycleNoWaitDestination();
                if (CapabilityButton(_plugin.NoWait.PreserveReconnectState ? "KEEP RECONNECT GEAR: ON" : "KEEP RECONNECT GEAR: OFF", FeatureCapability.Resurrect, _plugin.NoWait.PreserveReconnectState ? _success : _button, 34)) { _plugin.NoWait.PreserveReconnectState = !_plugin.NoWait.PreserveReconnectState; Say("Reconnect state preservation " + (_plugin.NoWait.PreserveReconnectState ? "enabled." : "disabled.")); }
                GUILayout.EndHorizontal();
                if (CapabilityButton(_targetAll ? "RESURRECT EVERYONE" : "RESURRECT SELECTED", FeatureCapability.Resurrect, _success, 36)) ResurrectSelected();
                GUILayout.Space(12);
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
                Badge(PermissionKind.Anyone); _itemSearch=TextField(_itemSearch, _button, GUILayout.Height(28)); string item = CurrentItemName(); if (AnimatedButton(item + "   ▾", _button, GUILayout.Height(30))) CycleItem();
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
            Card(new Rect(238,150,480,360),"✹  Dynamite Shower",new Color(1f,.42f,.2f),delegate
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
            Card(new Rect(735,150,493,360),"☂  Item Storm",new Color(.35f,.68f,1f),delegate
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
            Card(new Rect(238,525,480,210),"⌖  Navigation & Campfire",new Color(.3f,1f,.55f),delegate
            {
                Badge(PermissionKind.Anyone);GUILayout.Label("Nearest unopened genuine luggage; native and mod mirages are ignored.",_small);
                if(AnimatedButton(_plugin.LuggageNavigation.DirectionsEnabled?"REAL LUGGAGE DIRECTIONS: ON":"REAL LUGGAGE DIRECTIONS: OFF",_plugin.LuggageNavigation.DirectionsEnabled?_success:_button,GUILayout.Height(34))){_plugin.LuggageNavigation.DirectionsEnabled=!_plugin.LuggageNavigation.DirectionsEnabled;Say("Real luggage directions "+(_plugin.LuggageNavigation.DirectionsEnabled?"enabled.":"disabled."));}
                GUILayout.Label(_plugin.LuggageNavigation.LuggageStatus,_small);
                GUILayout.Label("Search range  "+Mathf.RoundToInt(_plugin.Settings.RealLuggageMaximumDistance.Value)+" m",_small);_plugin.Settings.RealLuggageMaximumDistance.Value=GUILayout.HorizontalSlider(_plugin.Settings.RealLuggageMaximumDistance.Value,25f,2000f);
                GUILayout.Space(8);
                Badge(PermissionKind.Anyone);GUILayout.Label("Lighting a campfire returns the lobby to the start.",_small);
                if(CapabilityButton(_plugin.CampfireTroll.Enabled?"ENABLED — CLICK TO DISABLE":"ENABLE CAMPFIRE TRAP",FeatureCapability.CampfireReset,_plugin.CampfireTroll.Enabled?_success:_danger,34)){_plugin.CampfireTroll.Enabled=!_plugin.CampfireTroll.Enabled;Say("Campfire reset trap "+(_plugin.CampfireTroll.Enabled?"enabled.":"disabled."));}
            });
            Card(new Rect(735,525,493,210),"◉  Mesa Clarity & Rescue",new Color(1f,.65f,.22f),delegate
            {
                Badge(PermissionKind.Anyone);GUILayout.Label("Locally hides native Mesa mirage visuals and restores them outside the biome.",_small);
                if(AnimatedButton(_plugin.LuggageNavigation.AntiMiragesEnabled?"ANTI-MIRAGES: ON":"ANTI-MIRAGES: OFF",_plugin.LuggageNavigation.AntiMiragesEnabled?_success:_button,GUILayout.Height(34))){_plugin.LuggageNavigation.AntiMiragesEnabled=!_plugin.LuggageNavigation.AntiMiragesEnabled;Say("Mesa Anti-Mirages "+(_plugin.LuggageNavigation.AntiMiragesEnabled?"enabled.":"disabled."));}
                GUILayout.Label(_plugin.LuggageNavigation.MirageStatus,_small);GUILayout.Space(8);
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

        private void DrawLobby(Rect area)
        {
            Card(new Rect(238, 142, 326, 202), "◎  Lobby Readiness", new Color(.3f,.7f,1f), delegate
            {
                string code=_plugin.LobbyReadiness.LobbyCode;GUILayout.Label("Host: "+_plugin.LobbyReadiness.HostName,_label);GUILayout.Label("Host migration: "+_plugin.LobbyReadiness.HostMigration,_small);GUILayout.Label("Loaded players: "+_plugin.Players.Entries.Count,_label);GUILayout.Label("Compatible clients: "+_plugin.Network.CompatibleCount,_label);
                if(AnimatedButton(_plugin.Network.LocalReady?"READY — CLICK TO UNREADY":"MARK MYSELF READY",_plugin.Network.LocalReady?_success:_button,GUILayout.Height(31))){_plugin.Network.SetLocalReady(!_plugin.Network.LocalReady);Say(_plugin.Network.LocalReady?"Marked ready for compatible clients.":"Marked not ready.");}
                GUILayout.Label("Lobby code: "+(string.IsNullOrEmpty(code)?"Unavailable":code),_small);GUI.enabled=!string.IsNullOrEmpty(code);if(AnimatedButton("COPY LOBBY CODE",_button,GUILayout.Height(31))){GUIUtility.systemCopyBuffer=code;Say("Lobby code copied to the clipboard.");}GUI.enabled=true;
            });
            Card(new Rect(580, 142, 648, 202), "♟  Player Readiness & Versions", new Color(.4f,.85f,.65f), delegate
            {
                IList<PlayerEntry> entries=_plugin.Players.Entries;if(entries.Count==0){GUILayout.Label("No lobby players resolved.",_label);return;}
                for(int i=0;i<entries.Count;i++){PlayerEntry entry=entries[i];GUILayout.BeginHorizontal();GUIStyle ns=new GUIStyle(_label);ns.normal.textColor=_plugin.Settings.HighContrast.Value?Color.white:_plugin.PlayerPreferences.DisplayColor(entry);GUILayout.Label(_plugin.PlayerPreferences.DisplayName(entry)+(entry.IsLocal?" (You)":""),ns,GUILayout.Width(190));string readiness=_plugin.Network.HasAdvertisement(entry.ActorNumber)?(_plugin.Network.IsReady(entry.ActorNumber)?"READY":"NOT READY"):_plugin.LobbyReadiness.Readiness(entry);GUILayout.Label(readiness,_small,GUILayout.Width(90));GUILayout.Label(_plugin.Network.CompatibilityStatus(entry.ActorNumber),_small);if(AnimatedButton("EDIT",_button,GUILayout.Width(54),GUILayout.Height(25))){_targetAll=false;_targetIndex=i;LoadPreferenceEditor(entry); }GUILayout.EndHorizontal();}
            });
            Card(new Rect(238, 360, 480, 360), "♥  Persistent Player Preferences", new Color(.72f,.48f,1f), delegate
            {
                PlayerEntry target=Target();if(target==null){GUILayout.Label("Select a player from the roster.",_label);return;}EnsurePreferenceEditor(target);GUILayout.Label("Editing: "+target.Name,_label);GUILayout.Label("Saved locally by Steam ID; aliases and colors appear only in this mod.",_small);
                GUILayout.Label("Preferred display name",_small);_preferenceAlias=TextField(_preferenceAlias,_button,GUILayout.Height(30));
                GUILayout.Label("Voice volume  "+Mathf.RoundToInt(_preferenceVolume*100f)+"%",_label);_preferenceVolume=GUILayout.HorizontalSlider(_preferenceVolume,0f,1f);_preferenceMuted=GUILayout.Toggle(_preferenceMuted,"Always mute locally");_preferenceExcluded=GUILayout.Toggle(_preferenceExcluded,"Exclude from random troll targeting");
                GUILayout.Label("UI name color",_small);GUILayout.Label("Red",_small);_preferenceColor.r=GUILayout.HorizontalSlider(_preferenceColor.r,0f,1f);GUILayout.Label("Green",_small);_preferenceColor.g=GUILayout.HorizontalSlider(_preferenceColor.g,0f,1f);GUILayout.Label("Blue",_small);_preferenceColor.b=GUILayout.HorizontalSlider(_preferenceColor.b,0f,1f);
                GUILayout.BeginHorizontal();if(AnimatedButton("SAVE PREFERENCE",_success,GUILayout.Height(34)))Say(SavePreference(target));if(AnimatedButton("RESET",_button,GUILayout.Height(34))){Say(_plugin.PlayerPreferences.Reset(target));LoadPreferenceEditor(target);}GUILayout.EndHorizontal();
            });
            Card(new Rect(735, 360, 493, 360), "⚕  Emergency Recovery", new Color(1f,.58f,.25f), delegate
            {
                GUILayout.Label("Local, safe-ground recovery. No host permission or target mod is needed.",_small);GUILayout.Space(8);
                if(AnimatedButton("UNSTUCK — LAST SAFE GROUND",_success,GUILayout.Height(38)))Say(_plugin.Recovery.RecoverLastSafeGround());
                GUILayout.BeginHorizontal();if(AnimatedButton("NEAREST SAFE SCOUT",_button,GUILayout.Height(36)))Say(_plugin.Recovery.RecoverNearestScout());if(AnimatedButton("ACTIVE CHECKPOINT",_button,GUILayout.Height(36)))Say(_plugin.Recovery.RecoverCheckpoint());GUILayout.EndHorizontal();
                if(AnimatedButton("RETURN TO EXPEDITION START",_button,GUILayout.Height(36)))Say(_plugin.Recovery.RecoverStart());
                GUILayout.BeginHorizontal();if(AnimatedButton("STOP FLIGHT + VELOCITY",_button,GUILayout.Height(36)))Say(_plugin.Recovery.StabilizeSelf());if(AnimatedButton("RESTORE SELF",_button,GUILayout.Height(36)))Say(_plugin.Recovery.RestoreSelf());GUILayout.EndHorizontal();
                if(AnimatedButton("CLEAR LOCAL MOD EFFECTS",_danger,GUILayout.Height(38))){_plugin.Reset.ResetAll();Say("Cleared locally tracked troll effects and restored reversible state.");}
                GUILayout.Label("Recovery can revive you when required and clears residual rigidbody velocity after moving.",_small);
            });
        }

        private void DrawSettings(Rect area)
        {
            Card(new Rect(238, 178, 480, 500), "⚙  Interface, Accessibility & Safety", new Color(.35f, .65f, 1f), delegate
            {
                GUILayout.Label("Menu key: " + _plugin.Settings.MenuKey.Value, _label); GUILayout.Label("UI scale " + _plugin.Settings.UiScale.Value.ToString("0.00"), _label); _plugin.Settings.UiScale.Value = GUILayout.HorizontalSlider(_plugin.Settings.UiScale.Value, .75f, 1.5f);
                GUILayout.Label("Transparency " + _plugin.Settings.Transparency.Value.ToString("0.00"), _label); _plugin.Settings.Transparency.Value = GUILayout.HorizontalSlider(_plugin.Settings.Transparency.Value, .55f, 1f);
                GUILayout.BeginHorizontal();GUILayout.Label("Text size  "+_plugin.Settings.TextScale.Value.ToString("0.00")+"x",_label);if(AnimatedButton("CYCLE",_button,GUILayout.Width(90),GUILayout.Height(28)))CycleTextScale();GUILayout.EndHorizontal();
                _plugin.Settings.HighContrast.Value=GUILayout.Toggle(_plugin.Settings.HighContrast.Value,"High-contrast status colors");
                GUILayout.Label("Camera shake  "+Mathf.RoundToInt(_plugin.Settings.CameraShakeScale.Value*100f)+"%",_label);_plugin.Settings.CameraShakeScale.Value=GUILayout.HorizontalSlider(_plugin.Settings.CameraShakeScale.Value,0f,1f);
                _plugin.Settings.ReduceFlashingEffects.Value=GUILayout.Toggle(_plugin.Settings.ReduceFlashingEffects.Value,"Reduce repeated/flashing troll effects");
                if(AnimatedButton("MENU INPUT: "+_plugin.Settings.MenuActivation.Value.ToString().ToUpperInvariant(),_button,GUILayout.Height(30)))_plugin.Settings.MenuActivation.Value=_plugin.Settings.MenuActivation.Value==MenuActivationMode.Toggle?MenuActivationMode.Hold:MenuActivationMode.Toggle;
                GUILayout.Label("All menu, backpack, and spectator shortcuts can be changed on Mod Config.",_small);GUILayout.Space(8);
                _plugin.Settings.ConfirmDestructive.Value = GUILayout.Toggle(_plugin.Settings.ConfirmDestructive.Value, "Confirm destructive actions"); _plugin.Settings.ExcludeSelf.Value = GUILayout.Toggle(_plugin.Settings.ExcludeSelf.Value, "Exclude self from random targets");
                GUILayout.Label("Maximum tracked objects " + _plugin.Settings.MaxSpawnedObjects.Value, _label); _plugin.Settings.MaxSpawnedObjects.Value = Mathf.RoundToInt(GUILayout.HorizontalSlider(_plugin.Settings.MaxSpawnedObjects.Value, 4, 64));
                GUILayout.Label("Maximum dynamite objects " + _plugin.Settings.MaxDynamiteObjects.Value, _label); _plugin.Settings.MaxDynamiteObjects.Value = Mathf.RoundToInt(GUILayout.HorizontalSlider(_plugin.Settings.MaxDynamiteObjects.Value, 8, 128));
                GUILayout.Label("Maximum Item Storm objects " + _plugin.Settings.MaxItemStormObjects.Value, _label); _plugin.Settings.MaxItemStormObjects.Value = Mathf.RoundToInt(GUILayout.HorizontalSlider(_plugin.Settings.MaxItemStormObjects.Value, 8, 128));
            });
            Card(new Rect(735, 178, 493, 500), "⌁  Diagnostics", new Color(.63f, .42f, 1f), delegate { _plugin.Settings.DebugLogging.Value = GUILayout.Toggle(_plugin.Settings.DebugLogging.Value, "Debug logging"); _plugin.Settings.NetworkDiagnostics.Value = GUILayout.Toggle(_plugin.Settings.NetworkDiagnostics.Value, "Network diagnostics"); _plugin.Settings.MirageDebug.Value = GUILayout.Toggle(_plugin.Settings.MirageDebug.Value, "Mirage debug info"); _plugin.Settings.FakeEnemyDebug.Value = GUILayout.Toggle(_plugin.Settings.FakeEnemyDebug.Value, "Fake Enemy debug info"); GUILayout.Space(12); if (AnimatedButton("REFRESH CAPABILITIES", _button, GUILayout.Height(40))) { _plugin.Capabilities.RefreshDynamic(); _plugin.Actions.RefreshItemCatalog(); _plugin.ModBrowser.Refresh(); Say("Runtime capabilities refreshed."); } GUILayout.Label("Camera shake scaling is applied at PEAK's final gamefeel rotation output, so it also covers native events.",_small);GUILayout.Label("Reduced flashing currently caps and slows repeated Phantom Pings and removes menu press pulses; it does not alter unrelated game cinematics.",_small);GUILayout.Label("SpongePEAKLib: not used; Photon RaiseEvent provides the required validated mod channel.", _small); });
        }

        private void DrawModConfig(Rect area)
        {
            Card(new Rect(238, 142, 990, 178), "◆  Built-in Quality of Life", new Color(.25f, .75f, .7f), delegate
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical(GUILayout.Width(300)); _plugin.Settings.TeamStatusEnabled.Value = GUILayout.Toggle(_plugin.Settings.TeamStatusEnabled.Value, "Team Status Panel"); GUILayout.Label(_plugin.TeamStatus.SuppressedByExternal ? "Yielding to PeakStatsEx" : "Nearby stamina + conditions", _small); GUILayout.EndVertical();
                GUILayout.BeginVertical(GUILayout.Width(300)); _plugin.Settings.QuickBackpackEnabled.Value = GUILayout.Toggle(_plugin.Settings.QuickBackpackEnabled.Value, "Quick Backpack  [" + _plugin.Settings.QuickBackpackKey.Value + "]"); GUILayout.Label(_plugin.QuickBackpack.SuppressedByExternal ? "Yielding to EasyBackpack" : "Opens the equipped pack wheel", _small); GUILayout.EndVertical();
                GUILayout.BeginVertical(GUILayout.Width(300)); _plugin.Settings.BetterSpectatingEnabled.Value = GUILayout.Toggle(_plugin.Settings.BetterSpectatingEnabled.Value, "Better Spectating"); _plugin.Settings.SpectateGhostPings.Value = GUILayout.Toggle(_plugin.Settings.SpectateGhostPings.Value, "Ghost pings"); GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical(GUILayout.Width(300)); _plugin.Settings.ItemSpawnerEnabled.Value = GUILayout.Toggle(_plugin.Settings.ItemSpawnerEnabled.Value, "Item Spawner"); _plugin.Settings.EffectPreviewEnabled.Value = GUILayout.Toggle(_plugin.Settings.EffectPreviewEnabled.Value, "Safe Troll Effect Cards"); GUILayout.EndVertical();
                GUILayout.BeginVertical(GUILayout.Width(300)); _plugin.Settings.FavoritesAndRecentsEnabled.Value = GUILayout.Toggle(_plugin.Settings.FavoritesAndRecentsEnabled.Value, "Favorites & Recents"); GUILayout.Label("Persistent local quick access", _small); GUILayout.EndVertical();
                GUILayout.BeginVertical(GUILayout.Width(300)); GUI.enabled=!Photon.Pun.PhotonNetwork.InRoom&&!_plugin.UnlimitedLobby.StandaloneDetected; _plugin.Settings.UnlimitedLobbyEnabled.Value = GUILayout.Toggle(_plugin.Settings.UnlimitedLobbyEnabled.Value, "Unlimited Lobby"); GUI.enabled=true; GUILayout.Label(_plugin.UnlimitedLobby.Status, _small); GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical(GUILayout.Width(300)); GUI.enabled=!_plugin.StaminaEffectPreview.StandaloneDetected; _plugin.Settings.StaminaEffectPreviewEnabled.Value=GUILayout.Toggle(_plugin.Settings.StaminaEffectPreviewEnabled.Value,"Held-item Stamina Preview"); GUI.enabled=true; GUILayout.Label(_plugin.StaminaEffectPreview.Status,_small); GUILayout.EndVertical();
                GUILayout.BeginVertical(GUILayout.Width(300)); GUI.enabled=_plugin.Settings.StaminaEffectPreviewEnabled.Value&&!_plugin.StaminaEffectPreview.StandaloneDetected; _plugin.Settings.StaminaEffectPreviewDetails.Value=GUILayout.Toggle(_plugin.Settings.StaminaEffectPreviewDetails.Value,"Detailed condition changes"); GUI.enabled=true; GUILayout.Label("Shows the result before consumption",_small); GUILayout.EndVertical();
                GUILayout.BeginVertical(GUILayout.Width(300)); _plugin.LuggageNavigation.DirectionsEnabled=GUILayout.Toggle(_plugin.LuggageNavigation.DirectionsEnabled,"Real Luggage Directions"); _plugin.LuggageNavigation.AntiMiragesEnabled=GUILayout.Toggle(_plugin.LuggageNavigation.AntiMiragesEnabled,"Mesa Anti-Mirages"); GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal(); GUI.enabled=!Photon.Pun.PhotonNetwork.InRoom&&!_plugin.UnlimitedLobby.StandaloneDetected; GUILayout.Label("Unlimited lobby cap  " + _plugin.UnlimitedLobby.MaxPlayers, _small, GUILayout.Width(155)); _plugin.Settings.UnlimitedLobbyMaxPlayers.Value=Mathf.RoundToInt(GUILayout.HorizontalSlider(_plugin.Settings.UnlimitedLobbyMaxPlayers.Value,4,30,GUILayout.Width(220))); GUI.enabled=true; _plugin.Settings.UnlimitedLobbyScaleSupplies.Value=GUILayout.Toggle(_plugin.Settings.UnlimitedLobbyScaleSupplies.Value,"Scale food + backpacks"); GUILayout.EndHorizontal();
                _plugin.Settings.PreferExternalQualityOfLifeMods.Value = GUILayout.Toggle(_plugin.Settings.PreferExternalQualityOfLifeMods.Value, "Prefer enabled external QoL mods when features overlap");
                GUILayout.BeginHorizontal(); if (AnimatedButton(_plugin.BetterSpectating.FreeCamera ? "RETURN TO SCOUT" : "SPECTATOR FREE CAM", _button, GUILayout.Height(30))) Say(_plugin.BetterSpectating.SetFreeCamera(!_plugin.BetterSpectating.FreeCamera)); if (AnimatedButton("REVIVE BESIDE SPECTATED", _success, GUILayout.Height(30))) Say(_plugin.BetterSpectating.ReviveBesideSpectated()); GUILayout.EndHorizontal();
            });
            Card(new Rect(238, 334, 320, 388), "☷  Loaded Mods", new Color(.45f, .65f, 1f), delegate
            {
                if (AnimatedButton("REFRESH LOADED MODS", _button, GUILayout.Height(30))) { _plugin.ModBrowser.Refresh(); _modIndex=0; _configIndex=0; _configEditKey=string.Empty; Say("Loaded mod list refreshed."); }
                IList<PluginInfo> plugins = _plugin.ModBrowser.Plugins;
                for (int i=0;i<plugins.Count;i++) { PluginInfo info=plugins[i]; string marker=info.Instance.enabled?"● ":"○ "; if(AnimatedButton(marker+info.Metadata.Name+(i==_modIndex?"  ›":""),i==_modIndex?_navSelected:_button,GUILayout.Height(29))){_modIndex=i;_configIndex=0;_configEditKey=string.Empty;} }
            });
            Card(new Rect(574, 334, 654, 388), "⚙  Selected Mod & Settings", new Color(.68f, .46f, 1f), delegate
            {
                PluginInfo info=SelectedPlugin(); if(info==null){GUILayout.Label("No loaded plugin selected.",_label);return;}
                GUILayout.Label(info.Metadata.Name+"  v"+info.Metadata.Version,_cardTitle); GUILayout.Label(info.Metadata.GUID,_small);
                bool isSelf=info.Metadata.GUID==TrollModPlugin.Guid; GUI.enabled=!isSelf; if(AnimatedButton(info.Instance.enabled?"DISABLE COMPONENT THIS SESSION":"ENABLE COMPONENT THIS SESSION",info.Instance.enabled?_danger:_success,GUILayout.Height(34)))Say(_plugin.ModBrowser.SetComponentEnabled(info,!info.Instance.enabled)); GUI.enabled=true;
                GUILayout.Label(isSelf?"Use the module toggles above for this mod.":"Component switches are session-only. Harmony patches may remain until restart.",_small); GUILayout.Space(8);
                ConfigEntryBase[] entries=info.Instance.Config.GetConfigEntries(); if(entries.Length==0){GUILayout.Label("This plugin exposes no BepInEx settings.",_label);return;} if(_configIndex>=entries.Length)_configIndex=0;
                GUILayout.BeginHorizontal(); if(AnimatedButton("‹",_button,GUILayout.Width(42),GUILayout.Height(30))){_configIndex=(_configIndex+entries.Length-1)%entries.Length;_configEditKey=string.Empty;} GUILayout.Label((_configIndex+1)+" / "+entries.Length,_label,GUILayout.Width(70)); if(AnimatedButton("NEXT SETTING  ›",_button,GUILayout.Height(30))){_configIndex=(_configIndex+1)%entries.Length;_configEditKey=string.Empty;} GUILayout.EndHorizontal();
                ConfigEntryBase entry=entries[_configIndex]; string key=info.Metadata.GUID+"|"+entry.Definition.Section+"|"+entry.Definition.Key; if(_configEditKey!=key){_configEditKey=key;_configEdit=entry.GetSerializedValue();}
                GUILayout.Label(entry.Definition.Section+"  /  "+entry.Definition.Key,_label); if(!string.IsNullOrEmpty(entry.Description.Description))GUILayout.Label(entry.Description.Description,_small);
                if(entry.SettingType==typeof(bool)){bool value=(bool)entry.BoxedValue;if(AnimatedButton(value?"ON — CLICK TO TURN OFF":"OFF — CLICK TO TURN ON",value?_success:_button,GUILayout.Height(34))){Say(_plugin.ModBrowser.SetValue(entry,(!value).ToString()));_configEdit=entry.GetSerializedValue();}}
                else { GUILayout.BeginHorizontal(); _configEdit=TextField(_configEdit,_button,GUILayout.Height(32)); if(AnimatedButton("APPLY",_success,GUILayout.Width(80),GUILayout.Height(32))){Say(_plugin.ModBrowser.SetValue(entry,_configEdit));_configEdit=entry.GetSerializedValue();} GUILayout.EndHorizontal(); }
                GUILayout.BeginHorizontal(); GUILayout.Label("Current: "+entry.GetSerializedValue(),_small); if(AnimatedButton("RESET DEFAULT",_button,GUILayout.Width(125),GUILayout.Height(27))){Say(_plugin.ModBrowser.ResetValue(entry));_configEdit=entry.GetSerializedValue();} GUILayout.EndHorizontal();
                GUILayout.Label("Changes are saved to the owning mod's config. Live application depends on that mod; restart PEAK when in doubt.",_small);
            });
        }

        private PluginInfo SelectedPlugin() { IList<PluginInfo> plugins=_plugin.ModBrowser.Plugins;if(plugins.Count==0)return null;if(_modIndex>=plugins.Count)_modIndex=0;return plugins[_modIndex]; }

        private void DrawItemShortcuts(string title, IList<string> values)
        {
            if (values == null || values.Count == 0) return;
            GUILayout.Label(title, _small);
            int count = Mathf.Min(values.Count, 4);
            for (int i = 0; i < count; i++) if (AnimatedButton(values[i], _button, GUILayout.Height(26))) SelectItemNamed(values[i]);
        }

        private void DrawEffectShortcuts(IList<string> values)
        {
            if (values == null || values.Count == 0) return;
            GUILayout.BeginHorizontal();
            int count = Mathf.Min(values.Count, 3);
            for (int i = 0; i < count; i++) if (AnimatedButton("★ " + values[i], _button, GUILayout.Height(25))) SelectEffect(values[i]);
            GUILayout.EndHorizontal();
        }

        private void GiveSelectedItem(bool self)
        {
            string item = CurrentItemName(); PlayerEntry target = self ? _plugin.Players.Local : Target();
            if (self && target == null) { Say("Local player unavailable."); return; }
            if (!self && !_targetAll && target == null) { Say("Select a target first."); return; }
            if (!self && _targetAll && _plugin.Players.Entries.Count == 0) { Say("No lobby players are available."); return; }
            ActionResult last = null;
            for (int i = 0; i < _itemQuantity; i++)
            {
                if (self) last = _plugin.Network.SendToOwner(NetCommand.GiveItem, target, new object[] { item });
                else { Owner(NetCommand.GiveItem, new object[] { item }); last = ActionResult.Ok("Requested " + _itemQuantity + " item(s)."); }
            }
            _plugin.Shortcuts.RecordItem(item); _plugin.Shortcuts.RecordEffect("Give Item"); if (last == null) Say("No target available."); else Say(last);
        }

        private void SpawnSelectedItem()
        {
            PlayerEntry target = Target(); string item = CurrentItemName();
            if (target == null) { Say("Select one target first."); return; }
            ActionResult result = _plugin.Spawns.SpawnItemNear(target, item, _itemQuantity);
            if (result.Success) { _plugin.Shortcuts.RecordItem(item); _plugin.Shortcuts.RecordEffect("Spawn Item"); }
            Say(result);
        }

        private void SelectItemNamed(string name)
        {
            _itemSearch = string.Empty;
            IList<Item> items = _plugin.Actions.Items;
            for (int i = 0; i < items.Count; i++) if (items[i] != null && string.Equals(items[i].name, name, StringComparison.OrdinalIgnoreCase)) { _itemIndex = i; Say("Selected " + items[i].name + "."); return; }
            Say(name + " is not currently loaded.");
        }

        private void PreviewEffect(string effect)
        {
            _effectPreviewUntil = Time.unscaledTime + 4f;
            if (_plugin.Settings.FavoritesAndRecentsEnabled.Value) _plugin.Shortcuts.RecordEffect(effect);
            Say("Safe preview started for " + effect + "; no gameplay action was sent.");
        }

        private void SelectEffect(string effect)
        {
            if (effect == "Give Item" || effect == "Spawn Item") { _tab = 12; Say("Opened Item Spawner for " + effect + "."); return; }
            for (int i = 0; i < _previewEffects.Length; i++) if (string.Equals(_previewEffects[i], effect, StringComparison.OrdinalIgnoreCase)) { _effectPreviewIndex = i; PreviewEffect(effect); return; }
        }

        private string EffectPreviewText(string effect)
        {
            if (effect == "Launch") return "Shows the configured direction, force, and ragdoll handoff before a launch.";
            if (effect == "Ragdoll") return "Shows force and recovery behavior before the native synchronized fall is requested.";
            if (effect == "Flight") return "Shows flight speed, boost behavior, recipient requirements, and cleanup behavior.";
            if (effect == "Status Effect") return "Shows the selected condition and bounded amount without changing scout afflictions.";
            if (effect == "Phantom Pings") return "Shows pattern, ping count, interval, audience, and cancellation behavior.";
            if (effect == "Fake Enemy") return "Shows the selected harmless visual decoy behavior and lifetime.";
            return "Shows Item Storm quantity, height, spread, interval, and item selection mode.";
        }

        private string EffectPreviewValues(string effect)
        {
            if (effect == "Launch") return _launchStrength.ToString("0") + " force  ↑";
            if (effect == "Ragdoll") return _ragdollStrength.ToString("0") + " force  ↗";
            if (effect == "Flight") return _flightSpeed.ToString("0") + " m/s  ⇧";
            if (effect == "Status Effect") return ((CharacterAfflictions.STATUSTYPE)_statusIndex) + "  " + _statusAmount.ToString("0.00");
            if (effect == "Phantom Pings") return ((PhantomPingPattern)_phantomPingPattern) + "  ×" + _phantomPingCount + "  " + _phantomPingInterval.ToString("0.00") + "s";
            if (effect == "Fake Enemy") return ((FakeEnemyKind)_enemyIndex) + "  " + _lifetime.ToString("0") + "s";
            return _itemStormCount + " items  " + _itemStormHeight.ToString("0") + "m high";
        }

        private string PreviewGlyph(string effect)
        {
            int frame = Mathf.FloorToInt(Time.unscaledTime * 5f) % 4;
            string dots = new string('·', frame + 1);
            if (effect == "Launch" || effect == "Flight") return "SCOUT  ↑" + dots;
            if (effect == "Ragdoll") return "SCOUT  ↗" + dots;
            if (effect == "Phantom Pings") return "•  •  •" + dots;
            if (effect == "Item Storm") return "▣  ↓  ▣" + dots;
            return "◇" + dots;
        }

        private void OpenEffectControls(string effect)
        {
            if (effect == "Phantom Pings" || effect == "Fake Enemy") _tab = 3;
            else if (effect == "Item Storm") _tab = 7;
            else _tab = 1;
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
        private void ResurrectSelected(){if(_targetAll){Say(_plugin.NoWait.QueueResurrectAll());return;}PlayerEntry target=Target();if(target==null){Say("No target available.");return;}Say(_plugin.Network.Resurrect(target));}
        private void ResurrectSelf(){PlayerEntry local=_plugin.Players.Local;if(local==null){Say("Local player unavailable.");return;}Say(_plugin.Network.Resurrect(local));}
        private void ReconnectLastLobby(){ActionResult result=_plugin.QuickReconnect.Reconnect();Say(result);if(result.Success)ForceClose();}
        private string NoWaitDestinationName(){return _plugin.NoWait.Destination==NoWaitDestination.NearestLiving?"NEAREST":_plugin.NoWait.Destination==NoWaitDestination.LowestLiving?"LOWEST":"CHECKPOINT";}
        private void CycleNoWaitDestination(){_plugin.NoWait.Destination=(NoWaitDestination)(((int)_plugin.NoWait.Destination+1)%3);Say("No Wait destination set to "+NoWaitDestinationName().ToLowerInvariant()+".");}
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
        private string CurrentTargetName() { if(_targetAll)return "Everyone";PlayerEntry t=Target(); return t==null?"No players":_plugin.PlayerPreferences.DisplayName(t)+(t.IsLocal?" (You)":""); }
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
        private string SubtitleForTab() { string[] s={"Session overview, capabilities, and emergency cleanup.","Target, manipulate, and mess with players in your lobby.","Host-authorized genuine enemy ambushes with strict tracking.","Harmless visual decoys, fake enemies, and ping placement.","Runtime-discovered 3D sound cues and voice capability status.","Cosmetic confusion without identity impersonation.","Controls for mod-spawned genuine enemies.","Environmental hazards separated from direct statuses.","Bounded randomized events with compatible-client validation.","Readiness, compatibility, persistent friend preferences, and recovery.","Interface, accessibility, safety limits, and diagnostics.","Toggle built-ins and safely edit loaded BepInEx mod settings.","Search, preview, favorite, give, or place runtime-discovered PEAK items."}; return s[_tab]; }
        private string HeadingForTab() { return _tab == 1 ? "PLAYER CONTROLS" : _tabNames[_tab].ToUpperInvariant(); }
        private void EmergencyReset(){for(int i=0;i<_plugin.Players.Entries.Count;i++)_plugin.Network.SendToAll(NetCommand.Reset,_plugin.Players.Entries[i].ActorNumber,new object[0]);if(_plugin.Players.Local!=null)_plugin.Network.SendToAll(NetCommand.ClearMirages,_plugin.Players.Local.ActorNumber,new object[0]);_plugin.Reset.ResetAll();Say("Cleanup and restoration completed on compatible clients.");}
        private void EnsurePreferenceEditor(PlayerEntry entry){if(entry!=null&&_preferenceSteamId!=entry.SteamUserId)LoadPreferenceEditor(entry);}
        private void LoadPreferenceEditor(PlayerEntry entry){PlayerPreference p=_plugin.PlayerPreferences.Get(entry);_preferenceSteamId=p.SteamId;_preferenceAlias=p.Alias;_preferenceVolume=p.VoiceVolume;_preferenceMuted=p.Muted;_preferenceExcluded=p.ExcludeFromRandom;_preferenceColor=p.Color;}
        private ActionResult SavePreference(PlayerEntry entry){PlayerPreference p=new PlayerPreference{SteamId=entry.SteamUserId,Alias=_preferenceAlias,VoiceVolume=_preferenceVolume,Muted=_preferenceMuted,ExcludeFromRandom=_preferenceExcluded,Color=_preferenceColor};ActionResult result=_plugin.PlayerPreferences.Save(entry,p);if(result.Success)LoadPreferenceEditor(entry);return result;}
        private void CycleTextScale(){float value=_plugin.Settings.TextScale.Value;_plugin.Settings.TextScale.Value=value<.9f?1f:value<1.05f?1.15f:value<1.2f?1.3f:.85f;}

        private string TextField(string value, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect rect=GUILayoutUtility.GetRect(new GUIContent(value??string.Empty),style,options);string name="PTM.Text."+_textFieldIndex++;GUI.SetNextControlName(name);string result=GUI.TextField(rect,value??string.Empty,style);
            if(Event.current.type==EventType.MouseDown&&rect.Contains(Event.current.mousePosition))_textFieldClicked=true;
            return result;
        }

        private void UpdateTextFocus()
        {
            if(Event.current.type==EventType.MouseDown&&!_textFieldClicked)GUI.FocusControl(null);
            string focused=GUI.GetNameOfFocusedControl();_isTyping=!string.IsNullOrEmpty(focused)&&focused.StartsWith("PTM.Text.",StringComparison.Ordinal);
        }

        private void EnsureStyles()
        {
            float textScale=Mathf.Clamp(_plugin.Settings.TextScale.Value,.85f,1.3f);bool high=_plugin.Settings.HighContrast.Value;bool ready=_pixel!=null&&_title!=null&&_nav!=null&&_button!=null&&_danger!=null&&_success!=null;if(ready&&Mathf.Abs(textScale-_styledTextScale)<.001f&&high==_styledHighContrast)return;ResetVisualAssets();_pixel = new Texture2D(1,1);_pixel.name="PTM_UI_PIXEL";_pixel.hideFlags=HideFlags.HideAndDontSave;_pixel.SetPixel(0,0,Color.white);_pixel.Apply();_styledTextScale=textScale;_styledHighContrast=high;
            Color muted=high?new Color(.86f,.91f,.9f):new Color(.58f,.67f,.67f);Color body=high?Color.white:new Color(.9f,.94f,.93f);
            _logo=Style(Scaled(25),FontStyle.Bold,Color.white,TextAnchor.MiddleLeft); _title=Style(Scaled(31),FontStyle.Bold,Color.white,TextAnchor.MiddleLeft); _subtitle=Style(Scaled(13),FontStyle.Normal,muted,TextAnchor.MiddleLeft);
            _label=Style(Scaled(14),FontStyle.Normal,body,TextAnchor.MiddleLeft); _small=Style(Scaled(12),FontStyle.Normal,muted,TextAnchor.MiddleLeft); _cardTitle=Style(Scaled(18),FontStyle.Bold,Color.white,TextAnchor.MiddleLeft); _badge=Style(Scaled(11),FontStyle.Bold,high?Color.white:new Color(.72f,.78f,.76f),TextAnchor.MiddleLeft);
            _nav=ButtonStyle(Scaled(15),high?Color.white:new Color(.65f,.72f,.71f),new Color(.025f,.035f,.038f,1f)); _nav.alignment=TextAnchor.MiddleLeft; _nav.padding=new RectOffset(18,8,0,0);
            _navSelected=ButtonStyle(Scaled(15),Color.white,new Color(.08f,.28f,.28f,.98f)); _navSelected.alignment=TextAnchor.MiddleLeft; _navSelected.padding=new RectOffset(18,8,0,0);
            _button=ButtonStyle(Scaled(13),body,new Color(.105f,.135f,.14f,1f)); _danger=ButtonStyle(Scaled(13),Color.white,new Color(high?.72f:.55f,.16f,.12f,1f)); _success=ButtonStyle(Scaled(13),Color.white,new Color(.08f,high?.55f:.38f,.34f,1f)); _disabled=ButtonStyle(Scaled(13),new Color(.4f,.46f,.45f),new Color(.065f,.08f,.082f,1f));
        }
        private int Scaled(int value){return Mathf.RoundToInt(value*Mathf.Clamp(_plugin.Settings.TextScale.Value,.85f,1.3f));}
        private GUIStyle Style(int size, FontStyle font, Color color, TextAnchor align) { GUIStyle s=new GUIStyle(GUI.skin.label);s.fontSize=size;s.fontStyle=font;s.normal.textColor=color;s.alignment=align;s.wordWrap=true;return s; }
        private GUIStyle ButtonStyle(int size, Color textColor, Color bg) { GUIStyle s=new GUIStyle(GUI.skin.button);s.fontSize=size;s.fontStyle=FontStyle.Bold;s.normal.textColor=textColor;s.hover.textColor=Color.white;s.active.textColor=Color.white;s.normal.background=Tint(bg);s.hover.background=Tint(new Color(Mathf.Min(1f,bg.r+.09f),Mathf.Min(1f,bg.g+.16f),Mathf.Min(1f,bg.b+.15f),bg.a));s.active.background=Tint(new Color(Mathf.Min(1f,bg.r+.16f),Mathf.Min(1f,bg.g+.22f),Mathf.Min(1f,bg.b+.20f),bg.a));s.padding=new RectOffset(10,10,6,6);return s; }
        private bool AnimatedButton(string text, GUIStyle style, params GUILayoutOption[] options) { Rect rect=GUILayoutUtility.GetRect(new GUIContent(text),style,options);return AnimatedButton(rect,text,style); }
        private bool AnimatedButton(Rect rect, string text, GUIStyle style)
        {
            string key=_tab+"|"+text+"|"+Mathf.RoundToInt(rect.x)+"|"+Mathf.RoundToInt(rect.y);float hover;_buttonHover.TryGetValue(key,out hover);bool over=rect.Contains(Event.current.mousePosition)&&GUI.enabled;hover=Mathf.MoveTowards(hover,over?1f:0f,Time.unscaledDeltaTime*9f);_buttonHover[key]=hover;
            float until;_buttonPulseUntil.TryGetValue(key,out until);float pulse=_plugin.Settings.ReduceFlashingEffects.Value?0f:(until>Time.unscaledTime?Mathf.Clamp01((until-Time.unscaledTime)/.16f):0f);float glow=Mathf.Max(hover,pulse);if(glow>.01f){float spread=1f+glow*3f;DrawPanel(new Rect(rect.x-spread,rect.y-spread,rect.width+spread*2f,rect.height+spread*2f),new Color(.12f,.48f,.45f,.10f*glow),new Color(.31f,.92f,.85f,.75f*glow),1f);}
            Color outline=!GUI.enabled?new Color(.18f,.22f,.22f,.9f):style==_danger?new Color(.72f,.28f,.22f,.95f):style==_success||style==_navSelected?new Color(.25f,.72f,.67f,.95f):new Color(.25f,.36f,.37f,.95f);if(over)outline=Color.Lerp(outline,new Color(.42f,1f,.91f,1f),.72f);Outline(rect,outline,1f);
            Rect animated=rect;if(pulse>0f){float inset=Mathf.Sin((1f-pulse)*Mathf.PI)*1.5f;animated=new Rect(rect.x+inset,rect.y+inset,rect.width-inset*2f,rect.height-inset*2f);}bool clicked=GUI.Button(animated,text,style);if(clicked){_buttonPulseUntil[key]=Time.unscaledTime+.16f;GUI.FocusControl(null);_isTyping=false;}return clicked;
        }
        private Texture2D Tint(Color color) { Texture2D t=new Texture2D(1,1);t.name="PTM_UI_STYLE";t.hideFlags=HideFlags.HideAndDontSave;t.SetPixel(0,0,color);t.Apply();_styleTextures.Add(t);return t; }
        private void DrawPanel(Rect rect, Color fill, Color border, float width) { DrawRect(rect,border);DrawRect(new Rect(rect.x+width,rect.y+width,rect.width-width*2,rect.height-width*2),fill); }
        private void Line(Rect rect, Color color) { DrawRect(rect,color); }
        private void Outline(Rect rect, Color color, float width) { DrawRect(new Rect(rect.x-width,rect.y-width,rect.width+width*2f,width),color);DrawRect(new Rect(rect.x-width,rect.y+rect.height,rect.width+width*2f,width),color);DrawRect(new Rect(rect.x-width,rect.y,width,rect.height),color);DrawRect(new Rect(rect.x+rect.width,rect.y,width,rect.height),color); }
        private void DrawRect(Rect rect, Color color) { Color old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,_pixel);GUI.color=old; }
    }
}
