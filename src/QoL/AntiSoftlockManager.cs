using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace PeakTrollMod
{
    internal enum SoftlockRepair { None, LastSafe, SafeScout, Checkpoint, Start, Stabilize, Restore }
    internal sealed class SoftlockIssue
    {
        public string Title; public string Detail; public string RepairLabel; public SoftlockRepair Repair;
    }

    internal sealed class AntiSoftlockManager
    {
        private readonly PlayerManager _players; private readonly RecoveryManager _recovery; private readonly ModConfig _settings; private readonly ManualLogSource _log;
        private readonly List<SoftlockIssue> _issues=new List<SoftlockIssue>(); private Character _tracked; private Vector3 _samplePosition; private float _lastMovementTime; private float _nextScan; private float _uninitializedSince; private float _incapacitatedSince; private bool _wasIncapacitated;
        public AntiSoftlockManager(PlayerManager players,RecoveryManager recovery,ModConfig settings,ManualLogSource log){_players=players;_recovery=recovery;_settings=settings;_log=log;}
        public bool Enabled { get{return _settings.AntiSoftlockDoctorEnabled.Value;} set{_settings.AntiSoftlockDoctorEnabled.Value=value;if(!value)_issues.Clear();} }
        public float DetectionDelay { get{return Mathf.Clamp(_settings.AntiSoftlockDetectionDelay.Value,6f,30f);} set{_settings.AntiSoftlockDetectionDelay.Value=Mathf.Clamp(value,6f,30f);} }
        public IList<SoftlockIssue> Issues { get{return _issues.AsReadOnly();} }
        public string Status { get{return !Enabled?"Diagnostics disabled":_issues.Count==0?"No likely softlock detected":_issues.Count+" recovery recommendation"+(_issues.Count==1?"":"s");} }

        public void Tick()
        {
            if(!Enabled)return;if(Time.unscaledTime<_nextScan)return;_nextScan=Time.unscaledTime+.5f;_issues.Clear();PlayerEntry local=_players.Local;
            if(local==null||local.Character==null){Add("Scout not loaded","The local scout object is unavailable. Wait for loading to finish; if it persists, reconnect.","NO AUTOMATIC FIX",SoftlockRepair.None);return;}
            Character c=local.Character;if(_tracked!=c){_tracked=c;_samplePosition=c.Center;_lastMovementTime=Time.unscaledTime;_uninitializedSince=Time.unscaledTime;}
            if(!c.IsInitialized){if(Time.unscaledTime-_uninitializedSince>DetectionDelay)Add("Initialization stalled","The local scout has not initialized within the expected window. Reconnecting is safer than mutating a partially loaded character.","RECONNECT REQUIRED",SoftlockRepair.None);return;}
            if(!PlayerActions.Finite(c.Center)){Add("Invalid position","The local scout position contains invalid coordinates.","RECOVER TO CHECKPOINT",SoftlockRepair.Checkpoint);return;}
            CharacterData d=c.data;if(d==null)return;
            bool incapacitated=d.dead||d.fullyPassedOut;
            if(incapacitated){if(!_wasIncapacitated)_incapacitatedSince=Time.unscaledTime;_wasIncapacitated=true;if(Time.unscaledTime-_incapacitatedSince>DetectionDelay)Add("Unable to rejoin play","Your scout has remained dead or fully passed out. Recovery can revive you at a safe teammate or checkpoint.","REVIVE NEAR TEAM",SoftlockRepair.SafeScout);return;}
            _wasIncapacitated=false;
            if(d.isGrounded||d.isClimbingAnything||d.isInWater){_samplePosition=c.Center;_lastMovementTime=Time.unscaledTime;return;}
            if((c.Center-_samplePosition).sqrMagnitude>.16f){_samplePosition=c.Center;_lastMovementTime=Time.unscaledTime;}
            if(Time.unscaledTime-_lastMovementTime>DetectionDelay)Add("Motionless off ground","Your scout has barely moved while neither grounded, climbing, nor swimming.",_recovery.HasLastSafePosition?"LAST SAFE GROUND":"NEAREST SAFE SCOUT",_recovery.HasLastSafePosition?SoftlockRepair.LastSafe:SoftlockRepair.SafeScout);
        }

        public ActionResult Repair(int index)
        {
            if(index<0||index>=_issues.Count)return ActionResult.Fail("That diagnostic is no longer active.");SoftlockRepair repair=_issues[index].Repair;ActionResult result;
            if(repair==SoftlockRepair.None)return ActionResult.Fail("This state needs a reconnect rather than an automatic mutation.");
            if(repair==SoftlockRepair.LastSafe)result=_recovery.RecoverLastSafeGround();else if(repair==SoftlockRepair.SafeScout){result=_recovery.RecoverNearestScout();if(!result.Success)result=_recovery.RecoverCheckpoint();if(!result.Success)result=_recovery.RecoverStart();}
            else if(repair==SoftlockRepair.Checkpoint){result=_recovery.RecoverCheckpoint();if(!result.Success)result=_recovery.RecoverStart();}else if(repair==SoftlockRepair.Start)result=_recovery.RecoverStart();else if(repair==SoftlockRepair.Stabilize)result=_recovery.StabilizeSelf();else result=_recovery.RestoreSelf();
            if(result.Success){_lastMovementTime=Time.unscaledTime;_issues.Clear();_log.LogInfo("Anti-Softlock Doctor applied an explicitly confirmed repair.");}return result;
        }

        public void ResetScene(){_issues.Clear();_tracked=null;_lastMovementTime=Time.unscaledTime;_uninitializedSince=Time.unscaledTime;_incapacitatedSince=0f;_wasIncapacitated=false;}
        private void Add(string title,string detail,string label,SoftlockRepair repair){_issues.Add(new SoftlockIssue{Title=title,Detail=detail,RepairLabel=label,Repair=repair});}
    }
}
