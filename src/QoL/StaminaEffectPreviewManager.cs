using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using BepInEx.Bootstrap;
using Photon.Pun;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class StaminaEffectPreviewManager
    {
        private sealed class StatusChange
        {
            public CharacterAfflictions.STATUSTYPE Type;
            public float Requested;
            public float Actual;
        }

        private readonly ModConfig _settings;
        private readonly ManualLogSource _log;
        private readonly List<StatusChange> _changes = new List<StatusChange>();
        private Item _item;
        private float _currentMaximum;
        private float _projectedMaximum;
        private float _extraStamina;
        private bool _infiniteStamina;
        private bool _wouldPassOut;
        private float _healingPool;
        private bool _standaloneDetected;
        private float _nextStandaloneCheck;
        private float _nextErrorLog;
        private Texture2D _pixel;
        private GUIStyle _title;
        private GUIStyle _small;

        public StaminaEffectPreviewManager(ModConfig settings, ManualLogSource log) { _settings = settings; _log = log; }

        public bool StandaloneDetected { get { RefreshStandaloneDetection(); return _standaloneDetected; } }
        public string Status { get { return StandaloneDetected ? "Yielding to standalone Effect Preview" : _settings.StaminaEffectPreviewEnabled.Value ? "Held-item stamina forecast enabled" : "Off"; } }

        public void Tick()
        {
            _item = null;
            _changes.Clear();
            _extraStamina = 0f;
            _infiniteStamina = false;
            _wouldPassOut = false;
            _healingPool = 0f;
            if (!_settings.StaminaEffectPreviewEnabled.Value || StandaloneDetected) return;

            try { UpdatePreview(); }
            catch (Exception ex)
            {
                _item = null; _changes.Clear();
                if (Time.unscaledTime >= _nextErrorLog) { _nextErrorLog = Time.unscaledTime + 10f; _log.LogWarning("[PTM] Stamina preview skipped an unsupported item: " + ex.Message); }
            }
        }

        private void UpdatePreview()
        {
            Character character = Character.localCharacter;
            if (character == null || character.data == null || character.refs == null || character.refs.afflictions == null || character.data.fullyPassedOut) return;
            Item item = character.data.currentItem;
            if (item == null || item.consuming) return;

            Dictionary<CharacterAfflictions.STATUSTYPE, float> requested = new Dictionary<CharacterAfflictions.STATUSTYPE, float>();
            ItemAction[] actions = item.GetComponentsInChildren<ItemAction>(true);
            for (int i = 0; i < actions.Length; i++) ReadAction(actions[i], requested, character.refs.afflictions);
            if (requested.Count == 0 && Mathf.Abs(_extraStamina) < .001f && !_infiniteStamina && _healingPool <= .001f) return;

            CharacterAfflictions afflictions = character.refs.afflictions;
            float currentStatusSum = Mathf.Max(0f, afflictions.statusSum);

            float actualSumDelta = 0f;
            foreach (KeyValuePair<CharacterAfflictions.STATUSTYPE, float> pair in requested)
            {
                float current = Mathf.Max(0f, afflictions.GetCurrentStatus(pair.Key));
                float cap = Mathf.Max(current, afflictions.GetStatusCap(pair.Key));
                float projected = Mathf.Clamp(current + pair.Value, 0f, cap);
                float actual = projected - current;
                if (Mathf.Abs(actual) < .001f) continue;
                _changes.Add(new StatusChange { Type = pair.Key, Requested = pair.Value, Actual = actual });
                actualSumDelta += actual;
            }

            float healingApplied = Mathf.Min(Mathf.Max(0f, currentStatusSum + actualSumDelta), _healingPool);
            actualSumDelta -= healingApplied;

            _currentMaximum = Mathf.Clamp01(character.GetMaxStamina());
            _projectedMaximum = Mathf.Clamp01(_currentMaximum - actualSumDelta);
            _wouldPassOut = currentStatusSum + actualSumDelta >= .99f;
            _item = item;
        }

        public void Draw(bool menuOpen)
        {
            if (menuOpen || _item == null || Event.current == null || Event.current.type != EventType.Repaint) return;
            EnsureAssets();
            float scale = Mathf.Clamp(Screen.width / 1920f, .75f, 1.25f);
            float width = 390f * scale;
            float height = (_settings.StaminaEffectPreviewDetails.Value ? 86f : 58f) * scale;
            Rect panel = new Rect((Screen.width - width) * .5f, Screen.height - 190f * scale, width, height);

            DrawRect(panel, new Color(.025f, .035f, .04f, .9f));
            DrawOutline(panel, _wouldPassOut ? new Color(1f, .28f, .22f, .95f) : new Color(.25f, .78f, .72f, .9f));
            GUI.Label(new Rect(panel.x + 10f * scale, panel.y + 6f * scale, panel.width - 20f * scale, 20f * scale), CleanName(_item.name) + "  •  STAMINA PREVIEW", _title);

            Rect bar = new Rect(panel.x + 10f * scale, panel.y + 31f * scale, panel.width - 20f * scale, 12f * scale);
            DrawRect(bar, new Color(.08f, .1f, .11f, 1f));
            float low = Mathf.Min(_currentMaximum, _projectedMaximum);
            DrawRect(new Rect(bar.x, bar.y, bar.width * low, bar.height), new Color(.3f, .78f, .72f, 1f));
            if (_projectedMaximum > _currentMaximum)
                DrawRect(new Rect(bar.x + bar.width * _currentMaximum, bar.y, bar.width * (_projectedMaximum - _currentMaximum), bar.height), new Color(.35f, 1f, .55f, .95f));
            else if (_projectedMaximum < _currentMaximum)
                DrawRect(new Rect(bar.x + bar.width * _projectedMaximum, bar.y, bar.width * (_currentMaximum - _projectedMaximum), bar.height), new Color(1f, .42f, .22f, .95f));
            DrawOutline(bar, new Color(.75f, .86f, .84f, .75f));

            string summary = Mathf.RoundToInt(_currentMaximum * 100f) + "%  →  " + Mathf.RoundToInt(_projectedMaximum * 100f) + "%";
            if (Mathf.Abs(_extraStamina) >= .001f) summary += "   Extra stamina " + SignedPercent(_extraStamina);
            if (_infiniteStamina) summary += "   ∞ stamina";
            if (_wouldPassOut) summary += "   WARNING: would pass out";
            GUI.Label(new Rect(panel.x + 10f * scale, panel.y + 45f * scale, panel.width - 20f * scale, 18f * scale), summary, _small);
            if (_settings.StaminaEffectPreviewDetails.Value)
                GUI.Label(new Rect(panel.x + 10f * scale, panel.y + 64f * scale, panel.width - 20f * scale, 18f * scale), DetailText(), _small);
        }

        public void ResetScene()
        {
            _item = null;
            _changes.Clear();
            if (_pixel != null) UnityEngine.Object.Destroy(_pixel);
            _pixel = null; _title = null; _small = null;
        }

        private void ReadAction(ItemAction action, Dictionary<CharacterAfflictions.STATUSTYPE, float> requested, CharacterAfflictions current)
        {
            if (action == null) return;
            Type type = action.GetType();
            string name = type.Name;
            if (name == "Action_ModifyStatus")
            {
                object status = Read(type, action, "statusType");
                float amount;
                if (status is CharacterAfflictions.STATUSTYPE && TryFloat(Read(type, action, "changeAmount"), out amount)) Add(requested, (CharacterAfflictions.STATUSTYPE)status, amount);
            }
            else if (name == "Action_RestoreHunger")
            {
                float amount;
                if (TryFloat(Read(type, action, "restorationAmount"), out amount)) Add(requested, ParseStatus("Hunger"), -Mathf.Abs(amount));
            }
            else if (name == "Action_InflictPoison")
            {
                float seconds, perSecond;
                if (TryFloat(Read(type, action, "inflictionTime"), out seconds) && TryFloat(Read(type, action, "poisonPerSecond"), out perSecond)) Add(requested, ParseStatus("Poison"), Mathf.Abs(seconds * perSecond));
            }
            else if (name == "Action_GiveExtraStamina")
            {
                float amount;
                if (TryFloat(Read(type, action, "amount"), out amount)) _extraStamina += amount;
            }
            else if (name == "Action_ApplyInfiniteStamina")
            {
                _infiniteStamina = true;
                float amount; if (TryFloat(Read(type, action, "drowsyAmount"), out amount)) Add(requested, ParseStatus("Drowsy"), amount);
            }
            else if (name == "Action_SuperJumpAmulet")
            {
                float amount; if (TryFloat(Read(type, action, "petrifyPerUse"), out amount)) Add(requested, ParseStatus("Petrify"), amount);
            }
            else if (name == "Action_MoraleBoost")
            {
                float baseline, perScout;
                if (TryFloat(Read(type, action, "baselineStaminaBoost"), out baseline)) _extraStamina += baseline;
                if (TryFloat(Read(type, action, "staminaBoostPerAdditionalScout"), out perScout)) _extraStamina += Mathf.Max(0, PhotonNetwork.CurrentRoom == null ? 0 : PhotonNetwork.CurrentRoom.PlayerCount - 1) * perScout;
            }
            else if (name == "Action_ApplyAffliction" || name == "Action_ApplyMassAffliction")
            {
                ReadAffliction(Read(type, action, "affliction"), requested, current);
                Array extras = Read(type, action, "extraAfflictions") as Array;
                if (extras != null) foreach (object extra in extras) ReadAffliction(extra, requested, current);
            }
        }

        private void ReadAffliction(object affliction, Dictionary<CharacterAfflictions.STATUSTYPE, float> requested, CharacterAfflictions current)
        {
            if (affliction == null) return;
            Type type = affliction.GetType();
            if (type.Name == "Affliction_AdjustStatus")
            {
                object status = Read(type, affliction, "statusType"); float amount;
                if (status is CharacterAfflictions.STATUSTYPE && TryFloat(Read(type, affliction, "statusAmount"), out amount)) Add(requested, (CharacterAfflictions.STATUSTYPE)status, amount);
            }
            else if (type.Name == "Affliction_AddBonusStamina")
            {
                float amount; if (TryFloat(Read(type, affliction, "staminaAmount"), out amount)) _extraStamina += amount;
            }
            else if (type.Name == "Affliction_ClearAllStatus")
            {
                bool includePetrify = Read(type, affliction, "includePetrify") is bool && (bool)Read(type, affliction, "includePetrify");
                foreach (object value in Enum.GetValues(typeof(CharacterAfflictions.STATUSTYPE)))
                {
                    CharacterAfflictions.STATUSTYPE status = (CharacterAfflictions.STATUSTYPE)value;
                    if (!includePetrify && status.ToString() == "Petrify") continue;
                    Add(requested, status, -current.GetCurrentStatus(status));
                }
            }
            else if (type.Name == "Affliction_HealAll")
            {
                float amount; if (TryFloat(Read(type, affliction, "maxHealing"), out amount)) _healingPool += Mathf.Abs(amount);
            }
        }

        private static object Read(Type type, object instance, string name)
        {
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null) return field.GetValue(instance);
            PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return property == null ? null : property.GetValue(instance, null);
        }

        private static bool TryFloat(object value, out float result)
        {
            try { result = value == null ? 0f : Convert.ToSingle(value); return value != null; }
            catch { result = 0f; return false; }
        }

        private static CharacterAfflictions.STATUSTYPE ParseStatus(string name)
        {
            return (CharacterAfflictions.STATUSTYPE)Enum.Parse(typeof(CharacterAfflictions.STATUSTYPE), name, true);
        }

        private static void Add(Dictionary<CharacterAfflictions.STATUSTYPE, float> values, CharacterAfflictions.STATUSTYPE type, float amount)
        {
            float current; values.TryGetValue(type, out current); values[type] = current + amount;
        }

        private string DetailText()
        {
            string text = string.Empty;
            for (int i = 0; i < _changes.Count; i++)
            {
                if (text.Length > 0) text += "   •   ";
                text += _changes[i].Type + " " + SignedPercent(_changes[i].Actual);
            }
            if (text.Length == 0) text = _infiniteStamina ? "Temporary unlimited stamina" : "No condition-bar change";
            if (_healingPool > .001f) text += (text.Length == 0 ? string.Empty : "   •   ") + "All-condition healing up to " + Mathf.RoundToInt(_healingPool * 100f) + "%";
            return text;
        }

        private static string SignedPercent(float value) { return (value >= 0f ? "+" : "−") + Mathf.RoundToInt(Mathf.Abs(value) * 100f) + "%"; }
        private static string CleanName(string value) { return string.IsNullOrEmpty(value) ? "Held item" : value.Replace("(Clone)", string.Empty).Trim(); }

        private void RefreshStandaloneDetection()
        {
            if (Time.unscaledTime < _nextStandaloneCheck) return;
            _nextStandaloneCheck = Time.unscaledTime + 5f;
            _standaloneDetected = false;
            foreach (KeyValuePair<string, BepInEx.PluginInfo> pair in Chainloader.PluginInfos)
            {
                BepInEx.PluginInfo info = pair.Value;
                if (info == null || info.Instance == null || !info.Instance.enabled || pair.Key == TrollModPlugin.Guid) continue;
                string identity = Normalize(pair.Key + " " + (info.Metadata == null ? string.Empty : info.Metadata.Name));
                if (identity.Contains("effectpreview")) { _standaloneDetected = true; break; }
            }
        }

        private static string Normalize(string value)
        {
            char[] buffer = new char[value.Length]; int length = 0;
            for (int i = 0; i < value.Length; i++) if (char.IsLetterOrDigit(value[i])) buffer[length++] = char.ToLowerInvariant(value[i]);
            return new string(buffer, 0, length);
        }

        private void EnsureAssets()
        {
            if (_pixel == null) { _pixel = new Texture2D(1, 1); _pixel.SetPixel(0, 0, Color.white); _pixel.Apply(); }
            if (_title == null) { _title = new GUIStyle(GUI.skin.label); _title.fontSize = 12; _title.fontStyle = FontStyle.Bold; _title.normal.textColor = new Color(.82f, 1f, .96f); }
            if (_small == null) { _small = new GUIStyle(GUI.skin.label); _small.fontSize = 11; _small.normal.textColor = _wouldPassOut ? new Color(1f, .58f, .5f) : new Color(.8f, .88f, .87f); }
        }

        private void DrawRect(Rect rect, Color color) { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, _pixel); GUI.color = old; }
        private void DrawOutline(Rect rect, Color color) { DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color); DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color); DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color); DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color); }
    }
}
