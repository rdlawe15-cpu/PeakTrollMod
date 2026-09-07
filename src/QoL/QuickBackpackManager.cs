using System;
using BepInEx.Logging;

namespace PeakTrollMod
{
    internal sealed class QuickBackpackManager
    {
        private readonly ManualLogSource _log; private readonly ModConfig _settings; private readonly ModConfigBrowser _mods;
        public QuickBackpackManager(ManualLogSource log, ModConfig settings, ModConfigBrowser mods) { _log = log; _settings = settings; _mods = mods; }
        public bool SuppressedByExternal { get { return _settings.PreferExternalQualityOfLifeMods.Value && _mods.IsEnabled("nickklmao.easybackpack"); } }
        public void Tick(bool menuOpen)
        {
            if (menuOpen || !_settings.QuickBackpackEnabled.Value || SuppressedByExternal || !_settings.QuickBackpackKey.Value.IsDown()) return;
            Character character = Character.localCharacter;
            if (character == null || character.data == null || character.player == null || character.data.dead || character.data.fullyPassedOut || character.data.passedOut || character.data.usingWheel) return;
            try
            {
                BackpackSlot slot = character.player.backpackSlot;
                if (slot == null || slot.IsEmpty() || GUIManager.instance == null) return;
                int slots = 4;
                if (character.refs != null && character.refs.backpackHandler != null && character.refs.backpackHandler.activeBackpackVisuals != null) slots = character.refs.backpackHandler.activeBackpackVisuals.slotCount;
                GUIManager.instance.OpenBackpackWheel(BackpackReference.GetFromEquippedBackpack(character), slots, slot.backpackType);
            }
            catch (Exception ex) { _log.LogWarning("Quick Backpack failed safely: " + ex.Message); }
        }
    }
}
