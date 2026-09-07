using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace PeakTrollMod
{
    internal sealed class ActionShortcutManager
    {
        private const int RecentLimit = 8;
        private readonly ConfigEntry<string> _favoriteItems;
        private readonly ConfigEntry<string> _recentItems;
        private readonly ConfigEntry<string> _favoriteEffects;
        private readonly ConfigEntry<string> _recentEffects;

        public ActionShortcutManager(ModConfig settings)
        {
            _favoriteItems = settings.FavoriteItemsData;
            _recentItems = settings.RecentItemsData;
            _favoriteEffects = settings.FavoriteEffectsData;
            _recentEffects = settings.RecentEffectsData;
        }

        public IList<string> FavoriteItems { get { return Parse(_favoriteItems.Value); } }
        public IList<string> RecentItems { get { return Parse(_recentItems.Value); } }
        public IList<string> FavoriteEffects { get { return Parse(_favoriteEffects.Value); } }
        public IList<string> RecentEffects { get { return Parse(_recentEffects.Value); } }

        public bool IsFavoriteItem(string name) { return Contains(Parse(_favoriteItems.Value), name); }
        public bool IsFavoriteEffect(string name) { return Contains(Parse(_favoriteEffects.Value), name); }

        public bool ToggleFavoriteItem(string name) { return Toggle(_favoriteItems, name); }
        public bool ToggleFavoriteEffect(string name) { return Toggle(_favoriteEffects, name); }
        public void RecordItem(string name) { Record(_recentItems, name); }
        public void RecordEffect(string name) { Record(_recentEffects, name); }

        private static bool Toggle(ConfigEntry<string> entry, string value)
        {
            List<string> list = Parse(entry.Value);
            int index = Find(list, value);
            if (index >= 0) { list.RemoveAt(index); entry.Value = Join(list); return false; }
            if (!string.IsNullOrEmpty(value)) list.Add(value.Trim());
            entry.Value = Join(list);
            return true;
        }

        private static void Record(ConfigEntry<string> entry, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            List<string> list = Parse(entry.Value);
            int index = Find(list, value);
            if (index >= 0) list.RemoveAt(index);
            list.Insert(0, value.Trim());
            while (list.Count > RecentLimit) list.RemoveAt(list.Count - 1);
            entry.Value = Join(list);
        }

        private static bool Contains(List<string> values, string value) { return Find(values, value) >= 0; }
        private static int Find(List<string> values, string value)
        {
            for (int i = 0; i < values.Count; i++) if (string.Equals(values[i], value, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        private static List<string> Parse(string value)
        {
            List<string> result = new List<string>();
            if (string.IsNullOrEmpty(value)) return result;
            string[] parts = value.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                if (!string.IsNullOrEmpty(part) && !Contains(result, part)) result.Add(part);
            }
            return result;
        }

        private static string Join(List<string> values) { return string.Join("\n", values.ToArray()); }
    }
}
