using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Photon.Pun;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class NoWaitManager
    {
        private const float JoinWindowSeconds = 90f;
        private const float ReviveQueueInterval = 1.5f;
        private readonly ManualLogSource _log;
        private readonly PlayerManager _players;
        private readonly PlayerActions _actions;
        private readonly CapabilityRegistry _capabilities;
        private readonly ModConfig _settings;
        private readonly List<int> _reviveQueue = new List<int>();
        private bool _wasInRoom;
        private bool _recentRoomJoin;
        private float _roomJoinedAt;
        private float _nextAttempt;
        private float _nextQueuedRevive;
        private Character _trackedLocalCharacter;
        private CharacterItems _suppressedDropItems;
        private float _suppressDropUntil;
        private bool _restorePending;
        private ReconnectData _restoreData;
        private Character _restoreCharacter;
        private Character _settleCharacter;
        private float _restoreAt;
        private float _settleUntil;

        public NoWaitManager(ManualLogSource log, PlayerManager players, PlayerActions actions, CapabilityRegistry capabilities, ModConfig settings)
        {
            _log = log;
            _players = players;
            _actions = actions;
            _capabilities = capabilities;
            _settings = settings;
            _wasInRoom = PhotonNetwork.InRoom;
            _recentRoomJoin = _wasInRoom;
            _roomJoinedAt = Time.unscaledTime;
        }

        public bool Enabled
        {
            get { return _settings.NoWaitEnabled.Value; }
            set { _settings.NoWaitEnabled.Value = value; }
        }

        public NoWaitDestination Destination
        {
            get { return _settings.NoWaitDestinationMode.Value; }
            set { _settings.NoWaitDestinationMode.Value = value; }
        }

        public bool PreserveReconnectState
        {
            get { return _settings.NoWaitPreserveReconnectState.Value; }
            set { _settings.NoWaitPreserveReconnectState.Value = value; }
        }

        public void Tick()
        {
            TickPostRevive();
            TickReviveQueue();

            bool inRoom = PhotonNetwork.InRoom;
            if (!_wasInRoom && inRoom)
            {
                _recentRoomJoin = true;
                _roomJoinedAt = Time.unscaledTime;
                _trackedLocalCharacter = null;
            }
            else if (_wasInRoom && !inRoom) ResetRoomState();
            _wasInRoom = inRoom;

            if (!inRoom || !Enabled || !_recentRoomJoin || !_capabilities.Available(FeatureCapability.Resurrect)) return;
            if (Time.unscaledTime - _roomJoinedAt > JoinWindowSeconds) { _recentRoomJoin = false; return; }

            PlayerEntry local = _players.Local;
            if (local == null || local.Character == null || local.Character.data == null || !local.Character.IsInitialized) return;
            if (_trackedLocalCharacter != local.Character) { _trackedLocalCharacter = local.Character; _nextAttempt = 0f; }

            // Airport entry is a normal pre-expedition join, not a late island join.
            if (local.Character.inAirport || !GameHandler.IsOnIsland) { if (local.Character.inAirport) _recentRoomJoin = false; return; }
            if (!local.Character.data.dead && !local.Character.data.fullyPassedOut)
            {
                // Reconnect initialization may expose a briefly living character before
                // applying its spectator state, so keep watching for a short grace period.
                if (Time.unscaledTime - _roomJoinedAt > 15f) _recentRoomJoin = false;
                return;
            }
            if (Time.unscaledTime < _nextAttempt) return;
            _nextAttempt = Time.unscaledTime + 1f;

            Vector3 destination;
            string destinationName;
            if (!TryResolveDestination(local, out destination, out destinationName)) return;

            // Snapshot whatever state PEAK has initialized. This covers both Photon rejoin
            // and ordinary lobby re-entry paths, which do not report HasRejoined uniformly.
            bool preserve = PreserveReconnectState && local.Character.refs != null && local.Character.refs.items != null;
            if (preserve) BeginReconnectPreservation(local.Character);
            ActionResult result = _actions.ResurrectLocal(local, destination, false);
            if (!result.Success)
            {
                CancelReconnectPreservation();
                _log.LogWarning("No Wait revive attempt failed: " + result.Message);
                return;
            }

            _actions.HaltVelocityLocal(local);
            _settleCharacter = local.Character;
            _settleUntil = Time.unscaledTime + 1f;
            _recentRoomJoin = false;
            _log.LogInfo("No Wait joined " + local.Name + " at " + destinationName + ".");
        }

        public ActionResult QueueResurrectAll()
        {
            _reviveQueue.Clear();
            for (int i = 0; i < _players.Entries.Count; i++)
            {
                PlayerEntry entry = _players.Entries[i];
                if (entry != null && entry.Character != null && entry.Character.data != null && (entry.Character.data.dead || entry.Character.data.fullyPassedOut)) _reviveQueue.Add(entry.ActorNumber);
            }
            if (_reviveQueue.Count == 0) return ActionResult.Fail("No dead or fully passed-out players are available.");
            _nextQueuedRevive = 0f;
            return ActionResult.Ok("Queued " + _reviveQueue.Count + " resurrection" + (_reviveQueue.Count == 1 ? "." : "s at safe intervals."));
        }

        public bool ShouldSuppressDrop(CharacterItems items)
        {
            return items != null && items == _suppressedDropItems && Time.unscaledTime <= _suppressDropUntil;
        }

        public void OnSceneLoaded()
        {
            _trackedLocalCharacter = null;
            _nextAttempt = 0f;
            _reviveQueue.Clear();
        }

        private bool TryResolveDestination(PlayerEntry local, out Vector3 destination, out string name)
        {
            destination = Vector3.zero;
            name = string.Empty;
            if (Destination == NoWaitDestination.Checkpoint)
            {
                if (!_actions.TryGetCheckpointPosition(out destination)) return false;
                name = "the active checkpoint";
                return true;
            }

            PlayerEntry selected = null;
            float best = float.MaxValue;
            for (int i = 0; i < _players.Entries.Count; i++)
            {
                PlayerEntry candidate = _players.Entries[i];
                Vector3 safe;
                if (!RecoveryManager.TrySafeScout(candidate, local, out safe)) continue;
                float score = Destination == NoWaitDestination.LowestLiving || !PlayerActions.Finite(local.Character.Center) ? candidate.Character.Center.y : (candidate.Character.Center - local.Character.Center).sqrMagnitude;
                if (score < best) { best = score; selected = candidate; destination = safe; }
            }
            if (selected == null) return false;
            name = (Destination == NoWaitDestination.LowestLiving ? "the lowest living scout, " : "the nearest living scout, ") + selected.Name;
            return true;
        }

        private void BeginReconnectPreservation(Character character)
        {
            try
            {
                _restoreData = ReconnectData.CreateFromCharacter(character);
                _restoreCharacter = character;
                _suppressedDropItems = character.refs.items;
                _suppressDropUntil = Time.unscaledTime + 1.5f;
                _restoreAt = Time.unscaledTime + .2f;
                _restorePending = true;
            }
            catch (Exception ex)
            {
                CancelReconnectPreservation();
                _log.LogWarning("No Wait could not snapshot reconnect state: " + ex.Message);
            }
        }

        private void TickPostRevive()
        {
            if (_restorePending && Time.unscaledTime >= _restoreAt)
            {
                try
                {
                    if (_restoreCharacter != null && _restoreCharacter.refs != null && _restoreCharacter.refs.afflictions != null)
                    {
                        _restoreCharacter.refs.afflictions.ApplyReconnectData(_restoreData);
                        _restoreCharacter.data.extraStamina = _restoreData.extraStamina;
                        _restoreCharacter.data.SetPetrify(_restoreData.petrify);
                        _log.LogInfo("No Wait restored reconnect statuses and retained inventory.");
                    }
                }
                catch (Exception ex) { _log.LogWarning("No Wait reconnect-state restore failed safely: " + ex.Message); }
                _restorePending = false;
            }
            if (_settleCharacter != null && Time.unscaledTime <= _settleUntil)
            {
                PlayerEntry local = _players.Local;
                if (local != null && local.Character == _settleCharacter) _actions.HaltVelocityLocal(local);
            }
            if (_restoreCharacter != null && Time.unscaledTime > _suppressDropUntil)
            {
                _restoreCharacter = null;
                _suppressedDropItems = null;
            }
            if (_settleCharacter != null && Time.unscaledTime > _settleUntil) _settleCharacter = null;
        }

        private void TickReviveQueue()
        {
            if (_reviveQueue.Count == 0 || Time.unscaledTime < _nextQueuedRevive) return;
            if (!PhotonNetwork.InRoom) { _reviveQueue.Clear(); return; }
            int actor = _reviveQueue[0];
            _reviveQueue.RemoveAt(0);
            _nextQueuedRevive = Time.unscaledTime + ReviveQueueInterval;
            PlayerEntry target = _players.Find(actor);
            if (target == null || target.Character == null || target.Character.data == null || (!target.Character.data.dead && !target.Character.data.fullyPassedOut)) return;
            ActionResult result = _actions.ResurrectAtLastLivingPosition(target);
            if (!result.Success) _log.LogWarning("Queued resurrection failed safely: " + result.Message);
        }

        private void CancelReconnectPreservation()
        {
            _restorePending = false;
            _restoreCharacter = null;
            _suppressedDropItems = null;
            _suppressDropUntil = 0f;
        }

        private void ResetRoomState()
        {
            _recentRoomJoin = false;
            _trackedLocalCharacter = null;
            _reviveQueue.Clear();
            _settleCharacter = null;
            CancelReconnectPreservation();
        }
    }
}
