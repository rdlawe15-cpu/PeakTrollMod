using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;

namespace PeakTrollMod
{
    internal sealed class AppearanceManager
    {
        private readonly ManualLogSource _log;
        private readonly MethodInfo _applyData;
        private readonly HashSet<int> _modified = new HashSet<int>();

        public AppearanceManager(ManualLogSource log)
        {
            _log = log;
            _applyData = ReflectionHelpers.Method(typeof(CharacterCustomization), "OnPlayerDataChange", new Type[] { typeof(PersistentPlayerData) });
        }

        public ActionResult SwapLocal(PlayerEntry first, PlayerEntry second, bool clothesOnly, bool colorOnly)
        {
            if (first == null || second == null || first.Character == null || second.Character == null || first.ActorNumber == second.ActorNumber) return ActionResult.Fail("Select two different players.");
            try
            {
                PersistentPlayerDataService service = GameHandler.GetService<PersistentPlayerDataService>();
                PersistentPlayerData firstData = service.GetPlayerData(first.ActorNumber);
                PersistentPlayerData secondData = service.GetPlayerData(second.ActorNumber);
                if (firstData == null || secondData == null) return ActionResult.Fail("Player customization data is not ready.");
                Apply(first, Compose(firstData, secondData, clothesOnly, colorOnly));
                Apply(second, Compose(secondData, firstData, clothesOnly, colorOnly));
                _modified.Add(first.ActorNumber); _modified.Add(second.ActorNumber);
                return ActionResult.Ok("Swapped appearances locally. Unmodded players are supported.");
            }
            catch (Exception ex) { _log.LogWarning("Local appearance swap failed: " + ex); return ActionResult.Fail("Appearance data is not ready: " + ex.Message); }
        }

        public ActionResult ShuffleLocal(IList<PlayerEntry> players, bool clothesOnly, bool colorOnly)
        {
            if (players == null || players.Count < 2) return ActionResult.Fail("At least two players are required.");
            try
            {
                PersistentPlayerDataService service = GameHandler.GetService<PersistentPlayerDataService>();
                List<PersistentPlayerData> data = new List<PersistentPlayerData>();
                for (int i = 0; i < players.Count; i++) data.Add(service.GetPlayerData(players[i].ActorNumber));
                for (int i = 0; i < players.Count; i++)
                {
                    int source = (i + 1) % players.Count;
                    if (data[i] == null || data[source] == null) continue;
                    Apply(players[i], Compose(data[i], data[source], clothesOnly, colorOnly));
                    _modified.Add(players[i].ActorNumber);
                }
                return ActionResult.Ok("Shuffled lobby appearances on this client.");
            }
            catch (Exception ex) { _log.LogWarning("Local appearance shuffle failed: " + ex); return ActionResult.Fail(ex.Message); }
        }

        public ActionResult Restore(PlayerEntry player)
        {
            if (player == null || player.Character == null) return ActionResult.Fail("Player unavailable.");
            try
            {
                PersistentPlayerDataService service = GameHandler.GetService<PersistentPlayerDataService>();
                PersistentPlayerData actual = service.GetPlayerData(player.ActorNumber);
                if (actual == null) return ActionResult.Fail("Player customization data is not ready.");
                Apply(player, actual); _modified.Remove(player.ActorNumber);
                return ActionResult.Ok("Restored " + player.Name + "'s actual appearance locally.");
            }
            catch (Exception ex) { return ActionResult.Fail(ex.Message); }
        }

        public void RestoreAll(IList<PlayerEntry> players)
        {
            if (players == null) return;
            for (int i = 0; i < players.Count; i++) if (_modified.Contains(players[i].ActorNumber)) Restore(players[i]);
            _modified.Clear();
        }

        private void Apply(PlayerEntry player, PersistentPlayerData data)
        {
            if (player.Character.refs == null || player.Character.refs.customization == null || _applyData == null) throw new InvalidOperationException("Character customization apply API unavailable.");
            ReflectionHelpers.Invoke(player.Character.refs.customization, _applyData, data);
        }

        private static PersistentPlayerData Compose(PersistentPlayerData target, PersistentPlayerData source, bool clothesOnly, bool colorOnly)
        {
            CharacterCustomizationData t = target.customizationData; CharacterCustomizationData s = source.customizationData;
            CharacterCustomizationData c = new CharacterCustomizationData();
            c.currentSkin = colorOnly || (!clothesOnly && !colorOnly) ? s.currentSkin : t.currentSkin;
            c.currentEyes = !clothesOnly && !colorOnly ? s.currentEyes : t.currentEyes;
            c.currentMouth = !clothesOnly && !colorOnly ? s.currentMouth : t.currentMouth;
            c.currentOutfit = colorOnly ? t.currentOutfit : s.currentOutfit;
            c.currentHat = colorOnly ? t.currentHat : s.currentHat;
            c.currentAccessory = colorOnly ? t.currentAccessory : s.currentAccessory;
            c.currentSash = colorOnly ? t.currentSash : s.currentSash;
            c.currentMedal = colorOnly ? t.currentMedal : s.currentMedal;
            PersistentPlayerData result = new PersistentPlayerData(); result.customizationData = c; return result;
        }
    }
}
