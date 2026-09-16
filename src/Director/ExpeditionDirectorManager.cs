using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BepInEx.Logging;
using Photon.Pun;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class ExpeditionDirectorManager
    {
        private const string CodePrefix = "PTMD1.";
        private readonly ManualLogSource _log;
        private readonly PlayerManager _players;
        private readonly PlayerActions _actions;
        private readonly SpawnManager _spawns;
        private readonly ChaosManager _chaos;
        private readonly TrollNetworkManager _network;
        private readonly ModConfig _settings;
        private readonly Dictionary<int, bool> _lastDead = new Dictionary<int, bool>();
        private readonly Dictionary<int, bool> _offerResponses = new Dictionary<int, bool>();
        private float _runStarted;
        private float _elapsedBeforeRun;
        private float _nextEvent;
        private float _startedAt;
        private float _rosterGraceUntil;
        private int _events;
        private int _rescues;
        private int _deaths;
        private string _lastEvent = "No director event has run yet.";
        private string _pendingOfferName = string.Empty;
        private string _pendingOfferCode = string.Empty;
        private int _pendingOfferHost;

        public ExpeditionMode Mode = ExpeditionMode.ZombieOutbreak;
        public float DurationSeconds = 600f;
        public float EventInterval = 25f;
        public int MaximumEnemies = 10;
        public int StartingDifficulty = 2;
        public bool AdaptiveDifficulty = true;
        public bool ZombieWaves = true;
        public bool ScoutmasterEvents;
        public bool SupplyDrops = true;
        public bool AutoRevive;
        public bool ChaosEvents;
        public DirectorRoundState State { get; private set; }
        public int AliveCount { get; private set; }
        public int DeadCount { get; private set; }
        public int EventsTriggered { get { return _events; } }
        public int Rescues { get { return _rescues; } }
        public int Deaths { get { return _deaths; } }
        public string LastEvent { get { return _lastEvent; } }
        public bool Active { get { return State == DirectorRoundState.Running || State == DirectorRoundState.Paused; } }
        public bool IsPaused { get { return State == DirectorRoundState.Paused; } }
        public float Elapsed { get { return _elapsedBeforeRun + (State == DirectorRoundState.Running ? Mathf.Max(0f, Time.unscaledTime - _runStarted) : 0f); } }
        public float Remaining { get { return Mathf.Max(0f, DurationSeconds - Elapsed); } }
        public float NextEventSeconds { get { return State == DirectorRoundState.Running ? Mathf.Max(0f, _nextEvent - Time.unscaledTime) : 0f; } }
        public int Difficulty { get { int rise = AdaptiveDifficulty && DurationSeconds > 0f ? Mathf.FloorToInt((Elapsed / DurationSeconds) * 5f) : 0; return Mathf.Clamp(StartingDifficulty + rise, 1, 10); } }
        public int Score { get { return Mathf.Max(0, Mathf.RoundToInt(Elapsed) + _events * 20 + _rescues * 100 - _deaths * 25); } }
        public string ModeName { get { return NameFor(Mode); } }
        public bool HasPendingOffer { get { return !string.IsNullOrEmpty(_pendingOfferCode); } }
        public string PendingOfferName { get { return _pendingOfferName; } }
        public string OfferResponseSummary { get { int accepted=0;foreach(KeyValuePair<int,bool> pair in _offerResponses)if(pair.Value)accepted++;return _offerResponses.Count==0?"No responses to the latest scenario offer":accepted+" accepted • "+(_offerResponses.Count-accepted)+" declined"; } }
        public string Status { get { return State + " • " + ModeName + " • difficulty " + Difficulty; } }
        public string Objective { get { if (Mode == ExpeditionMode.RescueRush) return "Revive fallen scouts and keep the whole party moving."; if (Mode == ExpeditionMode.Hardcore) return "Reach the summit with no automatic rescues or supply drops."; if (Mode == ExpeditionMode.ChaosClimb) return "Keep climbing while the director changes the pressure."; if (Mode == ExpeditionMode.Survival) return "Keep at least one scout alive until the timer expires."; if (Mode == ExpeditionMode.Custom) return "Complete your custom scenario rules before time expires."; return "Survive escalating zombie waves until the timer expires."; } }

        public ExpeditionDirectorManager(ManualLogSource log, PlayerManager players, PlayerActions actions, SpawnManager spawns, ChaosManager chaos, TrollNetworkManager network, ModConfig settings)
        {
            _log = log; _players = players; _actions = actions; _spawns = spawns; _chaos = chaos; _network = network; _settings = settings; State = DirectorRoundState.Stopped;
            ApplyPreset(ExpeditionMode.ZombieOutbreak);
            if (!string.IsNullOrEmpty(settings.DirectorScenarioCode.Value)) ImportCode(settings.DirectorScenarioCode.Value);
        }

        public ActionResult ApplyPreset(ExpeditionMode mode)
        {
            if (Active) return ActionResult.Fail("Stop the current round before changing its scenario.");
            Mode = mode;
            if (mode == ExpeditionMode.ZombieOutbreak) { DurationSeconds=600f;EventInterval=24f;MaximumEnemies=12;StartingDifficulty=2;AdaptiveDifficulty=true;ZombieWaves=true;ScoutmasterEvents=false;SupplyDrops=true;AutoRevive=false;ChaosEvents=false; }
            else if (mode == ExpeditionMode.Survival) { DurationSeconds=480f;EventInterval=30f;MaximumEnemies=8;StartingDifficulty=2;AdaptiveDifficulty=true;ZombieWaves=true;ScoutmasterEvents=true;SupplyDrops=true;AutoRevive=true;ChaosEvents=false; }
            else if (mode == ExpeditionMode.RescueRush) { DurationSeconds=420f;EventInterval=36f;MaximumEnemies=6;StartingDifficulty=2;AdaptiveDifficulty=true;ZombieWaves=true;ScoutmasterEvents=false;SupplyDrops=true;AutoRevive=false;ChaosEvents=false; }
            else if (mode == ExpeditionMode.Hardcore) { DurationSeconds=900f;EventInterval=18f;MaximumEnemies=16;StartingDifficulty=4;AdaptiveDifficulty=true;ZombieWaves=true;ScoutmasterEvents=true;SupplyDrops=false;AutoRevive=false;ChaosEvents=false; }
            else if (mode == ExpeditionMode.ChaosClimb) { DurationSeconds=600f;EventInterval=20f;MaximumEnemies=10;StartingDifficulty=3;AdaptiveDifficulty=true;ZombieWaves=true;ScoutmasterEvents=true;SupplyDrops=true;AutoRevive=false;ChaosEvents=true; }
            ClampRules();
            return ActionResult.Ok("Loaded the " + ModeName + " scenario preset.");
        }

        public ActionResult Start()
        {
            if (!PhotonNetwork.InRoom) return ActionResult.Fail("Join a Photon room before starting a director round.");
            if (!_players.IsHost) return ActionResult.Fail("Only the current Photon host can start Expedition Director.");
            if (Active) return ActionResult.Fail("A director round is already active.");
            ClampRules(); UpdateRoster();
            if (AliveCount == 0) return ActionResult.Fail("At least one living scout is required to start.");
            _spawns.ClearDirectorSpawns(); _chaos.StopCombo(); _lastDead.Clear();
            _events=0;_rescues=0;_deaths=0;_elapsedBeforeRun=0f;_runStarted=Time.unscaledTime;_startedAt=Time.unscaledTime;_rosterGraceUntil=Time.unscaledTime+12f;_nextEvent=Time.unscaledTime+Mathf.Min(8f,EventInterval);_lastEvent="Round initialized; first event is scheduled.";
            State=DirectorRoundState.Running; CaptureRoster();
            return ActionResult.Ok("Started " + ModeName + ". " + Objective);
        }

        public ActionResult TogglePause()
        {
            if (!_players.IsHost) return ActionResult.Fail("Only the host can pause Expedition Director.");
            if (State == DirectorRoundState.Running) { _elapsedBeforeRun += Mathf.Max(0f,Time.unscaledTime-_runStarted);State=DirectorRoundState.Paused;return ActionResult.Ok("Expedition Director paused."); }
            if (State == DirectorRoundState.Paused) { State=DirectorRoundState.Running;_runStarted=Time.unscaledTime;_nextEvent=Time.unscaledTime+Mathf.Min(5f,EventInterval);return ActionResult.Ok("Expedition Director resumed."); }
            return ActionResult.Fail("No active round can be paused.");
        }

        public ActionResult Stop(bool cleanup)
        {
            bool hadRound = Active || State == DirectorRoundState.Completed || State == DirectorRoundState.Failed;
            if (State == DirectorRoundState.Running) _elapsedBeforeRun += Mathf.Max(0f,Time.unscaledTime-_runStarted);
            State=DirectorRoundState.Stopped;_chaos.StopCombo();int removed=cleanup?_spawns.ClearDirectorSpawns():0;
            return hadRound ? ActionResult.Ok("Stopped Expedition Director" + (cleanup ? " and removed " + removed + " director-owned spawn(s)." : ".")) : ActionResult.Ok("Expedition Director is already stopped.");
        }

        public void Tick()
        {
            if (!PhotonNetwork.InRoom) ClearPendingOffer(); else if (HasPendingOffer && (PhotonNetwork.MasterClient == null || PhotonNetwork.MasterClient.ActorNumber != _pendingOfferHost)) ClearPendingOffer();
            if (!Active) { UpdateRoster(); return; }
            if (!PhotonNetwork.InRoom || !_players.IsHost) { Stop(true); _lastEvent="Round stopped because host authority was lost."; return; }
            UpdateRoster(); if (!RosterReady()) return; TrackRosterTransitions();
            if (State != DirectorRoundState.Running) return;
            if (AliveCount == 0 && Time.unscaledTime>_rosterGraceUntil && Time.unscaledTime-_startedAt>10f) { Finish(false,"Every scout is down."); return; }
            if (Elapsed >= DurationSeconds) { Finish(AliveCount>0,"The scenario timer was completed."); return; }
            if (Time.unscaledTime >= _nextEvent) { RunEvent(); _nextEvent=Time.unscaledTime+Mathf.Clamp(EventInterval-(Difficulty-1)*.7f,5f,120f); }
        }

        public void OnSceneLoaded() { if (State == DirectorRoundState.Running) _nextEvent=Time.unscaledTime+Mathf.Min(8f,EventInterval);_rosterGraceUntil=Time.unscaledTime+12f;_lastDead.Clear();CaptureRoster(); }

        private void RunEvent()
        {
            List<PlayerEntry> living = LivingPlayers(); if (living.Count == 0) return;
            PlayerEntry target=living[UnityEngine.Random.Range(0,living.Count)];_events++;
            if (AutoRevive && DeadCount>0) { PlayerEntry dead=FirstDead();if(dead!=null){ActionResult revived=_actions.ResurrectNearPlayer(dead,target);_lastEvent="Auto-rescue: "+revived.Message;return;} }
            if (SupplyDrops && _events%3==0) { ActionResult supply=_spawns.SpawnDirectorSupply(target);if(supply.Success){_lastEvent="Supply event: "+supply.Message;return;} }
            if (ChaosEvents && _events%2==0) { Vector3 direction=new Vector3(UnityEngine.Random.Range(-.45f,.45f),.65f,UnityEngine.Random.Range(-.45f,.45f)).normalized;ActionResult chaos=_actions.RagdollLocal(target,direction,Mathf.Clamp(10f+Difficulty*3f,10f,45f),1.2f);_lastEvent="Chaos pulse: "+chaos.Message;return; }
            if (!ZombieWaves && !ScoutmasterEvents) { _lastEvent="Rule pulse "+_events+" completed with enemy spawning disabled."; return; }
            int room=Mathf.Max(0,MaximumEnemies-_spawns.DirectorEnemyCount);int wave=Mathf.Min(room,1+Difficulty/3);
            if (wave<=0){_lastEvent="Enemy cap reached; the director held this wave.";return;}
            int spawned=0;string last="";
            for(int i=0;i<wave;i++)
            {
                ActionResult result;
                bool scoutmaster=ScoutmasterEvents&&(!ZombieWaves||(Difficulty>=5&&(_events+i)%4==0));
                result=scoutmaster?_spawns.SpawnDirectorScoutmaster(target,UnityEngine.Random.Range(8f,15f)):_spawns.SpawnDirectorZombie(target,UnityEngine.Random.Range(7f,14f));
                last=result.Message;if(result.Success)spawned++;
            }
            _lastEvent=spawned>0?"Wave "+_events+": spawned "+spawned+" threat(s) near "+target.Name+".":"Wave held: "+last;
            _log.LogInfo("Expedition Director event: "+_lastEvent);
        }

        private void Finish(bool success,string reason)
        {
            if(State==DirectorRoundState.Running)_elapsedBeforeRun+=Mathf.Max(0f,Time.unscaledTime-_runStarted);
            State=success?DirectorRoundState.Completed:DirectorRoundState.Failed;_lastEvent=reason+" Final score: "+Score+".";_chaos.StopCombo();_spawns.ClearDirectorSpawns();
        }

        public string ExportCode()
        {
            ClampRules();
            string payload=((int)Mode)+"|"+Mathf.RoundToInt(DurationSeconds)+"|"+Mathf.RoundToInt(EventInterval)+"|"+MaximumEnemies+"|"+StartingDifficulty+"|"+B(AdaptiveDifficulty)+"|"+B(ZombieWaves)+"|"+B(ScoutmasterEvents)+"|"+B(SupplyDrops)+"|"+B(AutoRevive)+"|"+B(ChaosEvents);
            string code=CodePrefix+Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));_settings.DirectorScenarioCode.Value=code;return code;
        }

        public ActionResult OfferScenario()
        {
            if (!_players.IsHost) return ActionResult.Fail("Only the current host can offer scenario rules.");
            _offerResponses.Clear(); string code=ExportCode(); return _network.OfferDirectorScenario(ModeName,code);
        }

        public ActionResult ReceiveOffer(int senderActor,string name,string code)
        {
            if(!PhotonNetwork.InRoom||PhotonNetwork.MasterClient==null||senderActor!=PhotonNetwork.MasterClient.ActorNumber)return ActionResult.Fail("Rejected Director offer from a non-host sender.");
            string decoded;if(!ValidateCode(code,out decoded))return ActionResult.Fail("Rejected invalid Director scenario code.");
            _pendingOfferHost=senderActor;_pendingOfferName=string.IsNullOrEmpty(name)?decoded:name;_pendingOfferCode=code;return ActionResult.Ok("Host offered "+_pendingOfferName+" rules. Review them on Expedition Director.");
        }

        public ActionResult AcceptOffer()
        {
            if(!HasPendingOffer)return ActionResult.Fail("No Director scenario offer is pending.");string name=_pendingOfferName;ActionResult result=ImportCode(_pendingOfferCode);if(result.Success)_network.RespondDirectorScenario(_pendingOfferHost,true,name);ClearPendingOffer();return result;
        }

        public ActionResult DeclineOffer()
        {
            if(!HasPendingOffer)return ActionResult.Fail("No Director scenario offer is pending.");string name=_pendingOfferName;_network.RespondDirectorScenario(_pendingOfferHost,false,name);ClearPendingOffer();return ActionResult.Ok("Declined "+name+" rules.");
        }

        public ActionResult ReceiveResponse(int senderActor,bool accepted,string name)
        {
            if(!_players.IsHost)return ActionResult.Fail("Ignored Director response on a non-host client.");PlayerEntry player=_players.Find(senderActor);if(player==null)return ActionResult.Fail("Director respondent left the lobby.");_offerResponses[senderActor]=accepted;return ActionResult.Ok(player.Name+(accepted?" accepted ":" declined ")+name+".");
        }

        public bool ValidateCode(string code,out string modeName)
        {
            modeName=string.Empty;if(string.IsNullOrEmpty(code)||code.Length>512||!code.StartsWith(CodePrefix,StringComparison.Ordinal))return false;
            try{string[] p=Encoding.UTF8.GetString(Convert.FromBase64String(code.Substring(CodePrefix.Length))).Split('|');if(p.Length!=11)return false;int mode,duration,interval,max,difficulty;if(!int.TryParse(p[0],out mode)||mode<0||mode>(int)ExpeditionMode.Custom||!int.TryParse(p[1],out duration)||duration<60||duration>3600||!int.TryParse(p[2],out interval)||interval<5||interval>120||!int.TryParse(p[3],out max)||max<1||max>24||!int.TryParse(p[4],out difficulty)||difficulty<1||difficulty>10)return false;bool flag;for(int i=5;i<11;i++)if(!PB(p[i],out flag))return false;modeName=NameFor((ExpeditionMode)mode);return true;}catch{return false;}
        }

        public ActionResult ImportCode(string code)
        {
            if (Active) return ActionResult.Fail("Stop the current round before importing a scenario.");
            if (string.IsNullOrEmpty(code) || code.Length>512 || !code.StartsWith(CodePrefix,StringComparison.Ordinal)) return ActionResult.Fail("Scenario code must be a bounded PTMD1 code.");
            try
            {
                string payload=Encoding.UTF8.GetString(Convert.FromBase64String(code.Substring(CodePrefix.Length)));string[] p=payload.Split('|');if(p.Length!=11)return ActionResult.Fail("Scenario code has the wrong field count.");
                int mode,duration,interval,max,difficulty;if(!int.TryParse(p[0],NumberStyles.Integer,CultureInfo.InvariantCulture,out mode)||mode<0||mode>(int)ExpeditionMode.Custom)return ActionResult.Fail("Scenario mode is invalid.");
                if(!int.TryParse(p[1],out duration)||!int.TryParse(p[2],out interval)||!int.TryParse(p[3],out max)||!int.TryParse(p[4],out difficulty))return ActionResult.Fail("Scenario numeric fields are invalid.");
                bool adaptive,zombies,scouts,supplies,revive,chaos;if(!PB(p[5],out adaptive)||!PB(p[6],out zombies)||!PB(p[7],out scouts)||!PB(p[8],out supplies)||!PB(p[9],out revive)||!PB(p[10],out chaos))return ActionResult.Fail("Scenario flags are invalid.");
                Mode=(ExpeditionMode)mode;DurationSeconds=duration;EventInterval=interval;MaximumEnemies=max;StartingDifficulty=difficulty;AdaptiveDifficulty=adaptive;ZombieWaves=zombies;ScoutmasterEvents=scouts;SupplyDrops=supplies;AutoRevive=revive;ChaosEvents=chaos;ClampRules();_settings.DirectorScenarioCode.Value=ExportCode();
                return ActionResult.Ok("Imported validated "+ModeName+" scenario rules.");
            }
            catch(Exception ex){return ActionResult.Fail("Scenario code could not be decoded: "+ex.Message);}
        }

        private void ClampRules(){DurationSeconds=Mathf.Clamp(DurationSeconds,60f,3600f);EventInterval=Mathf.Clamp(EventInterval,5f,120f);MaximumEnemies=Mathf.Clamp(MaximumEnemies,1,24);StartingDifficulty=Mathf.Clamp(StartingDifficulty,1,10);if(!ZombieWaves&&!ScoutmasterEvents&&!SupplyDrops&&!AutoRevive&&!ChaosEvents)ZombieWaves=true;}
        private void ClearPendingOffer(){_pendingOfferHost=0;_pendingOfferName=string.Empty;_pendingOfferCode=string.Empty;}
        private static int B(bool value){return value?1:0;} private static bool PB(string value,out bool result){if(value=="1"){result=true;return true;}if(value=="0"){result=false;return true;}result=false;return false;}
        private static string NameFor(ExpeditionMode mode){if(mode==ExpeditionMode.ZombieOutbreak)return "Zombie Outbreak";if(mode==ExpeditionMode.RescueRush)return "Rescue Rush";if(mode==ExpeditionMode.ChaosClimb)return "Chaos Climb";return mode.ToString();}
        private List<PlayerEntry> LivingPlayers(){List<PlayerEntry> result=new List<PlayerEntry>();for(int i=0;i<_players.Entries.Count;i++){PlayerEntry p=_players.Entries[i];if(p!=null&&p.Character!=null&&p.Character.data!=null&&!p.Character.data.dead&&!p.Character.data.fullyPassedOut)result.Add(p);}return result;}
        private PlayerEntry FirstDead(){for(int i=0;i<_players.Entries.Count;i++){PlayerEntry p=_players.Entries[i];if(IsDead(p))return p;}return null;}
        private static bool IsDead(PlayerEntry p){return p==null||p.Character==null||p.Character.data==null||p.Character.data.dead||p.Character.data.fullyPassedOut;}
        private bool RosterReady(){if(_players.Entries.Count==0)return false;for(int i=0;i<_players.Entries.Count;i++)if(_players.Entries[i]==null||_players.Entries[i].Character==null||_players.Entries[i].Character.data==null)return false;return true;}
        private void UpdateRoster(){AliveCount=0;DeadCount=0;for(int i=0;i<_players.Entries.Count;i++){if(IsDead(_players.Entries[i]))DeadCount++;else AliveCount++;}}
        private void CaptureRoster(){for(int i=0;i<_players.Entries.Count;i++)_lastDead[_players.Entries[i].ActorNumber]=IsDead(_players.Entries[i]);}
        private void TrackRosterTransitions(){for(int i=0;i<_players.Entries.Count;i++){PlayerEntry p=_players.Entries[i];bool dead=IsDead(p);bool before;if(_lastDead.TryGetValue(p.ActorNumber,out before)){if(!before&&dead)_deaths++;else if(before&&!dead)_rescues++;}_lastDead[p.ActorNumber]=dead;}}
    }
}
