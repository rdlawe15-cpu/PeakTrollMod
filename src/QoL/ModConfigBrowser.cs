using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;

namespace PeakTrollMod
{
    internal sealed class ModConfigBrowser
    {
        private readonly List<PluginInfo> _plugins = new List<PluginInfo>();
        public IList<PluginInfo> Plugins { get { return _plugins.AsReadOnly(); } }
        public void Refresh()
        {
            _plugins.Clear();
            foreach (KeyValuePair<string, PluginInfo> pair in Chainloader.PluginInfos)
                if (pair.Value != null && pair.Value.Metadata != null && pair.Value.Instance != null) _plugins.Add(pair.Value);
            _plugins.Sort(delegate(PluginInfo a, PluginInfo b) { return string.Compare(a.Metadata.Name, b.Metadata.Name, StringComparison.OrdinalIgnoreCase); });
        }
        public bool IsEnabled(string guid) { PluginInfo info; return Chainloader.PluginInfos.TryGetValue(guid, out info) && info != null && info.Instance != null && info.Instance.enabled; }
        public bool HasEnabledBool(string guid, string key)
        {
            PluginInfo info;
            if (!Chainloader.PluginInfos.TryGetValue(guid, out info) || info == null || info.Instance == null || !info.Instance.enabled) return false;
            ConfigEntryBase[] entries = info.Instance.Config.GetConfigEntries();
            for (int i = 0; i < entries.Length; i++) if (entries[i].Definition.Key == key && entries[i].SettingType == typeof(bool)) return (bool)entries[i].BoxedValue;
            return false;
        }
        public ActionResult SetComponentEnabled(PluginInfo info, bool enabled)
        {
            if (info == null || info.Instance == null) return ActionResult.Fail("Plugin component unavailable.");
            if (info.Metadata.GUID == TrollModPlugin.Guid) return ActionResult.Fail("PEAK Troll Mod cannot disable its own menu. Toggle its modules instead.");
            info.Instance.enabled = enabled;
            return ActionResult.Ok(info.Metadata.Name + (enabled ? " component enabled for this session." : " component disabled for this session; Harmony patches may remain until restart."));
        }
        public ActionResult SetValue(ConfigEntryBase entry, string serialized)
        {
            if (entry == null) return ActionResult.Fail("Config setting unavailable.");
            try { entry.SetSerializedValue(serialized); entry.ConfigFile.Save(); return ActionResult.Ok(entry.Definition.Section + " / " + entry.Definition.Key + " saved. Some mods require a restart to apply it."); }
            catch (Exception ex) { return ActionResult.Fail("Value rejected: " + ex.Message); }
        }
        public ActionResult ResetValue(ConfigEntryBase entry)
        {
            if (entry == null) return ActionResult.Fail("Config setting unavailable.");
            try { entry.BoxedValue = entry.DefaultValue; entry.ConfigFile.Save(); return ActionResult.Ok(entry.Definition.Key + " reset to its default value."); }
            catch (Exception ex) { return ActionResult.Fail("Reset failed: " + ex.Message); }
        }
    }
}
