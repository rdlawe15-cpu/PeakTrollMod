using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace PeakTrollMod
{
    [Flags]
    internal enum SupplyKind { None = 0, Food = 1, Medical = 2, Climbing = 4, Mobility = 8, Antidote = 16 }

    internal sealed class PartySupplyPlayerSummary
    {
        public string Name;
        public int PocketItems;
        public int BackpackItems;
        public int Food;
        public int Medical;
        public int Climbing;
        public int Mobility;
        public int Antidotes;
        public int TotalItems { get { return PocketItems + BackpackItems; } }
    }

    internal sealed class PartySupplyAdvisorManager
    {
        private readonly PlayerManager _players;
        private readonly ModConfig _settings;
        private readonly ManualLogSource _log;
        private readonly List<PartySupplyPlayerSummary> _summaries = new List<PartySupplyPlayerSummary>();
        private readonly List<string> _recommendations = new List<string>();
        private readonly Dictionary<string, SupplyKind> _classification = new Dictionary<string, SupplyKind>(StringComparer.OrdinalIgnoreCase);
        private float _nextRefresh;
        private float _nextErrorLog;
        private string _error = string.Empty;
        public PartySupplyAdvisorManager(PlayerManager players, ModConfig settings, ManualLogSource log) { _players = players; _settings = settings; _log = log; }
        public bool Enabled { get { return _settings.PartySupplyAdvisorEnabled.Value; } set { _settings.PartySupplyAdvisorEnabled.Value = value; } }
        public bool IncludeBackpacks { get { return _settings.PartySupplyIncludeBackpacks.Value; } set { _settings.PartySupplyIncludeBackpacks.Value = value; } }
        public IList<PartySupplyPlayerSummary> Players { get { return _summaries.AsReadOnly(); } }
        public IList<string> Recommendations { get { return _recommendations.AsReadOnly(); } }
        public string Status { get { return !Enabled ? "Off" : !string.IsNullOrEmpty(_error) ? _error : _summaries.Count == 0 ? "Waiting for synchronized party inventories" : _summaries.Count + " scout loadout(s), " + _recommendations.Count + " recommendation(s)"; } }

        public void Tick()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 1.5f;
            Refresh();
        }

        public void Refresh()
        {
            try { RefreshCore(); _error = string.Empty; }
            catch (Exception ex)
            {
                _summaries.Clear(); _recommendations.Clear(); _error = "Supply analysis unavailable for current synchronized data";
                if (Time.unscaledTime >= _nextErrorLog) { _nextErrorLog = Time.unscaledTime + 10f; _log.LogWarning("[PTM] Party Supply Advisor skipped a refresh: " + ex.Message); }
            }
        }

        private void RefreshCore()
        {
            _summaries.Clear(); _recommendations.Clear();
            if (!Enabled || !GameHandler.IsOnIsland) return;
            int totalFood = 0, totalMedical = 0, totalClimbing = 0, totalMobility = 0, totalAntidotes = 0, injured = 0, poisoned = 0;
            int minItems = int.MaxValue, maxItems = -1; PartySupplyPlayerSummary lightest = null, heaviest = null, climbingCarrier = null; int climbingCarriers = 0;
            IList<PlayerEntry> entries = _players.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                PlayerEntry entry = entries[i]; Character character = entry == null ? null : entry.Character;
                if (character == null || character.data == null || character.data.dead || character.player == null) continue;
                PartySupplyPlayerSummary summary = new PartySupplyPlayerSummary { Name = entry.Name };
                Player player = character.player;
                ItemSlot[] slots = player.itemSlots;
                if (slots != null) for (int slot = 0; slot < slots.Length; slot++) Collect(slots[slot], summary, false);
                if (IncludeBackpacks) CollectBackpack(player.backpackSlot, summary);
                _summaries.Add(summary);
                totalFood += summary.Food; totalMedical += summary.Medical; totalClimbing += summary.Climbing; totalMobility += summary.Mobility; totalAntidotes += summary.Antidotes;
                if (summary.Climbing > 0) { climbingCarriers++; climbingCarrier = summary; }
                if (summary.TotalItems < minItems) { minItems = summary.TotalItems; lightest = summary; }
                if (summary.TotalItems > maxItems) { maxItems = summary.TotalItems; heaviest = summary; }
                if (character.refs != null && character.refs.afflictions != null)
                {
                    if (character.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Injury) >= .2f) injured++;
                    if (character.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Poison) >= .08f) poisoned++;
                }
            }
            if (_summaries.Count == 0) return;
            if (totalClimbing == 0) _recommendations.Add("No rope, chain, piton, spike, or climbing cannon detected.");
            if (totalMedical == 0) _recommendations.Add(injured > 0 ? injured + " injured scout(s), but no recovery item was detected." : "No medical or recovery item was detected.");
            else if (injured > totalMedical) _recommendations.Add(injured + " injured scouts share only " + totalMedical + " detected recovery item(s).");
            if (poisoned > 0 && totalAntidotes == 0) _recommendations.Add(poisoned + " poisoned scout(s), but no antidote was detected.");
            if (totalFood == 0) _recommendations.Add("No packaged food, berries, mushrooms, or hunger-restoring item detected.");
            if (_summaries.Count > 1 && heaviest != null && lightest != null && maxItems - minItems >= 3) _recommendations.Add("Redistribute supplies: " + heaviest.Name + " carries " + maxItems + ", while " + lightest.Name + " carries " + minItems + ".");
            if (_summaries.Count > 2 && totalClimbing > 1 && climbingCarriers == 1 && climbingCarrier != null) _recommendations.Add("All climbing support is concentrated on " + climbingCarrier.Name + ".");
            if (totalMobility == 0 && _recommendations.Count < 4) _recommendations.Add("No emergency mobility item such as a balloon or parachute was detected.");
            if (_recommendations.Count == 0) _recommendations.Add("Party coverage looks balanced from synchronized item data.");
        }

        private void CollectBackpack(BackpackSlot backpackSlot, PartySupplyPlayerSummary summary)
        {
            if (backpackSlot == null || backpackSlot.data == null) return;
            BackpackData data;
            if (!backpackSlot.data.TryGetDataEntry<BackpackData>(DataEntryKey.BackpackData, out data) || data == null || data.itemSlots == null) return;
            for (int i = 0; i < data.itemSlots.Length; i++) Collect(data.itemSlots[i], summary, true);
        }

        private void Collect(ItemSlot slot, PartySupplyPlayerSummary summary, bool backpack)
        {
            if (slot == null || slot.prefab == null) return;
            if (backpack) summary.BackpackItems++; else summary.PocketItems++;
            SupplyKind kind = Classify(slot.prefab);
            if ((kind & SupplyKind.Food) != 0) summary.Food++;
            if ((kind & SupplyKind.Medical) != 0) summary.Medical++;
            if ((kind & SupplyKind.Climbing) != 0) summary.Climbing++;
            if ((kind & SupplyKind.Mobility) != 0) summary.Mobility++;
            if ((kind & SupplyKind.Antidote) != 0) summary.Antidotes++;
        }

        private SupplyKind Classify(Item item)
        {
            string name = ((item.UIData == null || string.IsNullOrEmpty(item.UIData.itemName)) ? item.name : item.UIData.itemName).ToLowerInvariant();
            string cacheKey = item.itemID + "|" + name;
            SupplyKind cached; if (_classification.TryGetValue(cacheKey, out cached)) return cached;
            SupplyKind kind = SupplyKind.None;
            if ((item.itemTags & (Item.ItemTags.PackagedFood | Item.ItemTags.Berry | Item.ItemTags.Mushroom)) != 0 || Contains(name, "food", "berry", "mushroom", "ration", "marshmallow", "coconut", "banana")) kind |= SupplyKind.Food;
            if (Contains(name, "bandage", "medkit", "medical", "first aid", "remedy", "cure", "antidote", "panacea")) kind |= SupplyKind.Medical;
            if (Contains(name, "antidote")) kind |= SupplyKind.Antidote | SupplyKind.Medical;
            if (Contains(name, "rope", "chain", "piton", "climbing spike", "rescue claw", "rescue hook")) kind |= SupplyKind.Climbing;
            if (Contains(name, "balloon", "parachute", "cannon", "magic bean", "teleporter")) kind |= SupplyKind.Mobility;
            ItemAction[] actions = item.GetComponentsInChildren<ItemAction>(true);
            for (int i = 0; i < actions.Length; i++)
            {
                string action = actions[i] == null ? string.Empty : actions[i].GetType().Name.ToLowerInvariant();
                if (action.Contains("heal")) kind |= SupplyKind.Medical;
                if (action.Contains("restorehunger")) kind |= SupplyKind.Food;
            }
            _classification[cacheKey] = kind;
            return kind;
        }

        private static bool Contains(string value, params string[] tokens) { for (int i = 0; i < tokens.Length; i++) if (value.Contains(tokens[i])) return true; return false; }
        public void ResetScene() { _summaries.Clear(); _recommendations.Clear(); _classification.Clear(); _error = string.Empty; _nextRefresh = 0f; }
    }
}
