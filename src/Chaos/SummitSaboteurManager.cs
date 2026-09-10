using System;
using BepInEx.Logging;
using Photon.Pun;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class SummitSaboteurManager
    {
        private sealed class SabotageJob
        {
            public int TargetActor;
            public Vector3 Summit;
            public float TriggerDistance;
            public float StaminaLockSeconds;
            public float LaunchDistance;
            public bool Triggered;
            public int Stage;
            public float NextStage;
        }

        private readonly ManualLogSource _log;
        private readonly PlayerManager _players;
        private readonly PlayerActions _actions;
        private readonly TrollNetworkManager _network;
        private readonly SpawnManager _spawns;
        private readonly CapabilityRegistry _capabilities;
        private SabotageJob _job;
        private float _nextCheck;
        private float _victimUntil;
        private bool _victimCached;
        private float _victimStamina;
        private float _victimExtraStamina;
        private string _status = "Not armed";

        public SummitSaboteurManager(ManualLogSource log, PlayerManager players, PlayerActions actions, TrollNetworkManager network, SpawnManager spawns, CapabilityRegistry capabilities)
        {
            _log = log; _players = players; _actions = actions; _network = network; _spawns = spawns; _capabilities = capabilities;
        }

        public bool Armed { get { return _job != null; } }
        public string Status { get { return _status; } }

        public ActionResult Arm(PlayerEntry target, float triggerDistance, float staminaLockSeconds, float launchDistance)
        {
            if (!_players.IsHost) return ActionResult.Fail("Summit Saboteur requires host authority for the zombie and max-Hunger steps.");
            if (!_capabilities.Available(FeatureCapability.SummitSaboteur)) return ActionResult.Fail(_capabilities.Reason(FeatureCapability.SummitSaboteur));
            if (target == null || target.Character == null || target.Character.data == null) return ActionResult.Fail("Select one available scout.");
            Vector3 summit;
            if (!_actions.TryGetStartEnd(true, out summit) || !PlayerActions.Finite(summit)) return ActionResult.Fail("The final summit reference is not loaded yet.");

            _job = new SabotageJob
            {
                TargetActor = target.ActorNumber,
                Summit = summit,
                TriggerDistance = Mathf.Clamp(triggerDistance, 8f, 35f),
                StaminaLockSeconds = Mathf.Clamp(staminaLockSeconds, 2f, 10f),
                LaunchDistance = Mathf.Clamp(launchDistance, 50f, 300f)
            };
            _nextCheck = 0f;
            _status = "Armed for " + target.Name + ". Waiting for summit approach.";
            return ActionResult.Ok(_status);
        }

        public ActionResult Cancel()
        {
            if (_job == null) return ActionResult.Ok("Summit Saboteur is not armed.");
            PlayerEntry target = _players.Find(_job.TargetActor);
            if (_job.Triggered && target != null && (target.IsLocal || _network.IsCompatible(target.ActorNumber))) _network.SendToOwner(NetCommand.SummitSabotageVictim, target, new object[] { 0f });
            _job = null;
            _status = "Cancelled";
            return ActionResult.Ok("Summit Saboteur cancelled.");
        }

        public void Tick()
        {
            TickVictim();
            if (_job == null) return;
            if (!PhotonNetwork.InRoom || !_players.IsHost) { _job = null; _status = "Cancelled because host authority was lost."; return; }

            PlayerEntry target = _players.Find(_job.TargetActor);
            if (target == null || target.Character == null || target.Character.data == null) { _job = null; _status = "Cancelled because the target left."; return; }
            if (!_job.Triggered)
            {
                if (Time.unscaledTime < _nextCheck) return;
                _nextCheck = Time.unscaledTime + .2f;
                if (target.Character.data.dead) { _status = "Armed, but the target is currently dead."; return; }
                float distance = Vector3.Distance(target.Character.Center, _job.Summit);
                _status = "Armed for " + target.Name + " — " + distance.ToString("0") + " m from trigger.";
                if (distance <= _job.TriggerDistance) Trigger(target);
                return;
            }

            if (Time.unscaledTime < _job.NextStage) return;
            if (_job.Stage == 1)
            {
                LogStep("zombie", _spawns.SpawnZombieBehind(target, 6f));
                _job.Stage = 2;
                _job.NextStage = Time.unscaledTime + .75f;
                return;
            }

            LogStep("horizontal launch", _network.HorizonLaunch(target, _job.LaunchDistance));
            _status = "Triggered on " + target.Name + ".";
            _job = null;
        }

        private void Trigger(PlayerEntry target)
        {
            _job.Triggered = true;
            _job.Stage = 1;
            _job.NextStage = Time.unscaledTime + .75f;
            _status = "Triggered — sabotage sequence running.";
            LogStep("held-item drop", _actions.DropHeldItemLocal(target));
            LogStep("max Hunger", _actions.ApplyStatusAsHost(target, (int)CharacterAfflictions.STATUSTYPE.Hunger, 2f));
            if (target.IsLocal || _network.IsCompatible(target.ActorNumber)) LogStep("stamina lock", _network.SendToOwner(NetCommand.SummitSabotageVictim, target, new object[] { _job.StaminaLockSeconds }));
            else _log.LogInfo("Summit Saboteur skipped the stamina lock and fake notice because " + target.Name + " is unmodded.");
        }

        private void LogStep(string step, ActionResult result) { _log.LogInfo("Summit Saboteur " + step + ": " + result.Message); }

        public ActionResult ActivateVictim(float seconds)
        {
            seconds = Mathf.Clamp(seconds, 0f, 10f);
            if (seconds <= 0f) { RestoreVictim(); return ActionResult.Ok("Summit Saboteur stamina lock cleared."); }
            Character character = Character.localCharacter;
            if (character == null || character.data == null) return ActionResult.Fail("Local character stamina is unavailable.");
            if (!_victimCached) { _victimStamina = character.data.currentStamina; _victimExtraStamina = character.data.extraStamina; _victimCached = true; }
            _victimUntil = Mathf.Max(_victimUntil, Time.unscaledTime + seconds);
            ShowRecoveryNotice();
            return ActionResult.Ok("Summit Saboteur victim effect started for " + seconds.ToString("0.0") + " seconds.");
        }

        private void TickVictim()
        {
            if (!_victimCached) return;
            if (Time.unscaledTime >= _victimUntil) { RestoreVictim(); return; }
            Character character = Character.localCharacter;
            if (character == null || character.data == null || character.data.dead) return;
            character.data.currentStamina = 0f;
            character.data.extraStamina = 0f;
            character.data.staminaDelta = 0f;
            character.data.sinceUseStamina = 0f;
        }

        private static void ShowRecoveryNotice()
        {
            UI_Notifications[] notifications = Resources.FindObjectsOfTypeAll<UI_Notifications>();
            for (int i = 0; i < notifications.Length; i++)
            {
                UI_Notifications notification = notifications[i];
                if (notification == null || notification.gameObject == null || !notification.gameObject.activeInHierarchy) continue;
                notification.AddNotification("Connection recovered");
                return;
            }
        }

        private void RestoreVictim()
        {
            if (_victimCached)
            {
                Character character = Character.localCharacter;
                if (character != null && character.data != null && !character.data.dead)
                {
                    if (!float.IsNaN(_victimStamina) && !float.IsInfinity(_victimStamina)) character.data.currentStamina = _victimStamina;
                    if (!float.IsNaN(_victimExtraStamina) && !float.IsInfinity(_victimExtraStamina)) character.data.extraStamina = _victimExtraStamina;
                }
            }
            _victimCached = false;
            _victimUntil = 0f;
        }

        public void ResetTarget(int actor)
        {
            if (_job != null && _job.TargetActor == actor) { _job = null; _status = "Cancelled by player reset."; }
        }

        public void Reset()
        {
            _job = null;
            _status = "Not armed";
            RestoreVictim();
        }
    }
}
