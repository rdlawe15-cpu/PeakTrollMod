using BepInEx.Logging;

namespace PeakTrollMod
{
    internal sealed class ResetManager
    {
        private readonly ManualLogSource _log;
        private readonly PlayerActions _actions;
        private readonly MirageManager _mirages;
        private readonly SpawnManager _spawns;
        private readonly AudioManager _audio;
        private readonly AppearanceManager _appearance;
        public ResetManager(ManualLogSource log, PlayerActions actions, MirageManager mirages, SpawnManager spawns, AudioManager audio, AppearanceManager appearance) { _log = log; _actions = actions; _mirages = mirages; _spawns = spawns; _audio = audio; _appearance = appearance; }
        public void ResetAll() { TrollModPlugin plugin = TrollModPlugin.Instance; if (plugin != null && plugin.Chaos != null) { plugin.Chaos.Stop(); plugin.Chaos.StopCombo(); } if(plugin!=null&&plugin.SummitSaboteur!=null)plugin.SummitSaboteur.Reset(); if(plugin!=null&&plugin.MindControl!=null)plugin.MindControl.Reset(); if(plugin!=null&&plugin.PhantomPings!=null)plugin.PhantomPings.Cancel(); if(plugin!=null&&plugin.InfiniteRescueClaw!=null)plugin.InfiniteRescueClaw.RestoreAll(); if(plugin!=null&&plugin.HungerAmplifier!=null)plugin.HungerAmplifier.Reset(); if(plugin!=null&&plugin.ClimbForecast!=null)plugin.ClimbForecast.ResetScene(); if(plugin!=null&&plugin.PartySupplyAdvisor!=null)plugin.PartySupplyAdvisor.ResetScene(); if(plugin!=null&&plugin.Players!=null)_appearance.RestoreAll(plugin.Players.Entries); _actions.ResetKnownPlayers(); _mirages.ClearAll(); _spawns.ClearAll(); _audio.StopAll(); _audio.RestoreAllVoice(); _log.LogInfo("All reversible troll state cleaned up."); }
    }
}
