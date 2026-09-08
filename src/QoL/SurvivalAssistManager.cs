using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class SurvivalAssistManager
    {
        private readonly ModConfig _settings;

        public SurvivalAssistManager(ModConfig settings) { _settings = settings; }
        public bool Immortality { get { return _settings.ImmortalityEnabled.Value; } set { _settings.ImmortalityEnabled.Value = value; } }
        public bool InfiniteStamina { get { return _settings.InfiniteStaminaEnabled.Value; } set { _settings.InfiniteStaminaEnabled.Value = value; } }

        public void Tick()
        {
            if (!InfiniteStamina) return;
            Character character = Character.localCharacter;
            if (character == null || character.data == null || character.data.dead || character.data.fullyPassedOut) return;
            Character.GainFullStamina();
        }

        public void ResetScene()
        {
            // Both options are config-backed; no game state is permanently modified.
        }
    }
}
