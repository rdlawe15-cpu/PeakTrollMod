using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class HotkeyBindingRecord
    {
        public PluginInfo Plugin;
        public ConfigEntryBase Entry;
        public KeyCode MainKey;
        public KeyCode[] Modifiers;
        public string Shortcut;
        public string Label { get { return Plugin.Metadata.Name + " • " + Entry.Definition.Key; } }
    }

    internal sealed class HotkeyConflictRecord
    {
        public string Shortcut;
        public readonly List<HotkeyBindingRecord> Bindings = new List<HotkeyBindingRecord>();
        public HotkeyBindingRecord SuggestedTarget;
        public KeyCode SuggestedKey;
        public string SuggestedShortcut;
    }

    internal sealed class HotkeyConflictDoctor
    {
        private static readonly KeyCode[] CandidateKeys = { KeyCode.F6, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12, KeyCode.Home, KeyCode.End, KeyCode.Insert, KeyCode.Delete, KeyCode.PageUp, KeyCode.PageDown, KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3 };
        private readonly ModConfigBrowser _browser;
        private readonly ModConfig _settings;
        private readonly ManualLogSource _log;
        private readonly List<HotkeyConflictRecord> _conflicts = new List<HotkeyConflictRecord>();
        private readonly HashSet<string> _used = new HashSet<string>(StringComparer.Ordinal);
        private float _nextRefresh;
        private float _nextErrorLog;
        private string _error = string.Empty;
        public HotkeyConflictDoctor(ModConfigBrowser browser, ModConfig settings, ManualLogSource log) { _browser = browser; _settings = settings; _log = log; }
        public bool Enabled { get { return _settings.HotkeyConflictDoctorEnabled.Value; } set { _settings.HotkeyConflictDoctorEnabled.Value = value; if (!value) _conflicts.Clear(); } }
        public IList<HotkeyConflictRecord> Conflicts { get { return _conflicts.AsReadOnly(); } }
        public string Status { get { return !Enabled ? "Off" : !string.IsNullOrEmpty(_error) ? _error : _conflicts.Count == 0 ? "No exact enabled-plugin shortcut conflicts found" : _conflicts.Count + " exact shortcut conflict(s) found"; } }

        public void Tick() { if (!Enabled || Time.unscaledTime < _nextRefresh) return; _nextRefresh = Time.unscaledTime + 5f; Refresh(); }

        public void Refresh()
        {
            try { RefreshCore(); _error = string.Empty; }
            catch (Exception ex)
            {
                _conflicts.Clear(); _error = "Shortcut scan unavailable for one or more loaded configs";
                if (Time.unscaledTime >= _nextErrorLog) { _nextErrorLog = Time.unscaledTime + 10f; _log.LogWarning("[PTM] Hotkey Conflict Doctor skipped a scan: " + ex.Message); }
            }
        }

        private void RefreshCore()
        {
            _conflicts.Clear(); _used.Clear();
            if (!Enabled) return;
            Dictionary<string, List<HotkeyBindingRecord>> groups = new Dictionary<string, List<HotkeyBindingRecord>>(StringComparer.Ordinal);
            IList<PluginInfo> plugins = _browser.Plugins;
            for (int i = 0; i < plugins.Count; i++)
            {
                PluginInfo plugin = plugins[i];
                if (plugin == null || plugin.Instance == null || !plugin.Instance.enabled) continue;
                ICollection<ConfigEntryBase> entries = ((IDictionary<ConfigDefinition, ConfigEntryBase>)plugin.Instance.Config).Values;
                foreach (ConfigEntryBase entry in entries)
                {
                    HotkeyBindingRecord binding;
                    if (!TryRead(plugin, entry, out binding) || binding.MainKey == KeyCode.None) continue;
                    string canonical = Canonical(binding.MainKey, binding.Modifiers);
                    _used.Add(canonical);
                    List<HotkeyBindingRecord> list;
                    if (!groups.TryGetValue(canonical, out list)) { list = new List<HotkeyBindingRecord>(); groups[canonical] = list; }
                    list.Add(binding);
                }
            }
            foreach (KeyValuePair<string, List<HotkeyBindingRecord>> pair in groups)
            {
                if (pair.Value.Count < 2) continue;
                HotkeyConflictRecord conflict = new HotkeyConflictRecord { Shortcut = pair.Value[0].Shortcut };
                conflict.Bindings.AddRange(pair.Value);
                conflict.SuggestedTarget = ChooseTarget(pair.Value);
                conflict.SuggestedKey = FindSuggestion(conflict.SuggestedTarget == null ? new KeyCode[0] : conflict.SuggestedTarget.Modifiers);
                conflict.SuggestedShortcut = FormatShortcut(conflict.SuggestedKey, conflict.SuggestedTarget == null ? new KeyCode[0] : conflict.SuggestedTarget.Modifiers);
                _conflicts.Add(conflict);
            }
            _conflicts.Sort(delegate(HotkeyConflictRecord a, HotkeyConflictRecord b) { return string.Compare(a.Shortcut, b.Shortcut, StringComparison.OrdinalIgnoreCase); });
        }

        public ActionResult ApplySuggestion(HotkeyConflictRecord conflict)
        {
            if (conflict == null || conflict.SuggestedTarget == null || conflict.SuggestedKey == KeyCode.None) return ActionResult.Fail("No safe unused suggestion is available.");
            try
            {
                HotkeyBindingRecord target = conflict.SuggestedTarget;
                if (target.Entry.SettingType == typeof(KeyboardShortcut)) target.Entry.BoxedValue = new KeyboardShortcut(conflict.SuggestedKey, target.Modifiers);
                else if (target.Entry.SettingType == typeof(KeyCode)) target.Entry.BoxedValue = conflict.SuggestedKey;
                else return ActionResult.Fail("The selected binding type is no longer supported.");
                target.Entry.ConfigFile.Save();
                string message = target.Label + " reassigned to " + conflict.SuggestedShortcut + ". Some mods require restart.";
                Refresh();
                return ActionResult.Ok(message);
            }
            catch (Exception ex) { return ActionResult.Fail("Shortcut update failed: " + ex.Message); }
        }

        private static bool TryRead(PluginInfo plugin, ConfigEntryBase entry, out HotkeyBindingRecord binding)
        {
            binding = null;
            if (entry == null) return false;
            if (entry.SettingType == typeof(KeyboardShortcut))
            {
                KeyboardShortcut shortcut = (KeyboardShortcut)entry.BoxedValue;
                List<KeyCode> modifiers = new List<KeyCode>(shortcut.Modifiers);
                binding = new HotkeyBindingRecord { Plugin = plugin, Entry = entry, MainKey = shortcut.MainKey, Modifiers = modifiers.ToArray(), Shortcut = shortcut.ToString() };
                return true;
            }
            if (entry.SettingType == typeof(KeyCode))
            {
                KeyCode key = (KeyCode)entry.BoxedValue;
                binding = new HotkeyBindingRecord { Plugin = plugin, Entry = entry, MainKey = key, Modifiers = new KeyCode[0], Shortcut = key.ToString() };
                return true;
            }
            return false;
        }

        private static HotkeyBindingRecord ChooseTarget(List<HotkeyBindingRecord> bindings)
        {
            for (int i = bindings.Count - 1; i >= 0; i--) if (bindings[i].Plugin.Metadata.GUID != TrollModPlugin.Guid) return bindings[i];
            return bindings[bindings.Count - 1];
        }

        private KeyCode FindSuggestion(KeyCode[] modifiers)
        {
            for (int i = 0; i < CandidateKeys.Length; i++) if (!_used.Contains(Canonical(CandidateKeys[i], modifiers))) return CandidateKeys[i];
            return KeyCode.None;
        }

        private static string Canonical(KeyCode main, KeyCode[] modifiers)
        {
            int[] values = new int[modifiers == null ? 0 : modifiers.Length];
            for (int i = 0; i < values.Length; i++) values[i] = (int)modifiers[i];
            Array.Sort(values);
            string result = ((int)main).ToString();
            for (int i = 0; i < values.Length; i++) result += "+" + values[i];
            return result;
        }

        private static string FormatShortcut(KeyCode main, KeyCode[] modifiers)
        {
            if (main == KeyCode.None) return "unavailable";
            string result = string.Empty;
            if (modifiers != null) for (int i = 0; i < modifiers.Length; i++) result += modifiers[i] + " + ";
            return result + main;
        }
    }
}
