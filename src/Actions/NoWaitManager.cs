using BepInEx.Logging;
using Photon.Pun;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class NoWaitManager
    {
        private const float JoinWindowSeconds = 90f;
        private readonly ManualLogSource _log;
        private readonly PlayerManager _players;
        private readonly PlayerActions _actions;
        private readonly CapabilityRegistry _capabilities;
        private readonly ModConfig _settings;
        private bool _wasInRoom;
        private bool _recentRoomJoin;
        private float _roomJoinedAt;
        private float _nextAttempt;
        private Character _trackedLocalCharacter;

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

        public void Tick()
        {
            bool inRoom = PhotonNetwork.InRoom;
            if (!_wasInRoom && inRoom)
            {
                _recentRoomJoin = true;
                _roomJoinedAt = Time.unscaledTime;
                _trackedLocalCharacter = null;
            }
            else if (_wasInRoom && !inRoom)
            {
                _recentRoomJoin = false;
                _trackedLocalCharacter = null;
            }
            _wasInRoom = inRoom;

            if (!inRoom || !Enabled || !_recentRoomJoin || !_capabilities.Available(FeatureCapability.Resurrect)) return;
            if (Time.unscaledTime - _roomJoinedAt > JoinWindowSeconds) { _recentRoomJoin = false; return; }

            PlayerEntry local = _players.Local;
            if (local == null || local.Character == null || local.Character.data == null || !local.Character.IsInitialized) return;
            if (_trackedLocalCharacter != local.Character) { _trackedLocalCharacter = local.Character; _nextAttempt = 0f; }

            // Entering through the airport is a normal pre-expedition join. Consuming the
            // window here prevents No Wait from becoming an auto-revive on a later death.
            if (local.Character.inAirport) { _recentRoomJoin = false; return; }
            if (!local.Character.data.dead && !local.Character.data.fullyPassedOut)
            {
                // The reconnect setup may expose an apparently living character before
                // applying its spectator state. Keep watching briefly for that transition.
                if (Time.unscaledTime - _roomJoinedAt > 15f) _recentRoomJoin = false;
                return;
            }
            if (Time.unscaledTime < _nextAttempt) return;
            _nextAttempt = Time.unscaledTime + 1f;

            PlayerEntry nearest = FindNearestLiving(local);
            if (nearest == null) return;
            ActionResult result = _actions.ResurrectNearPlayer(local, nearest);
            if (!result.Success) { _log.LogWarning("No Wait revive attempt failed: " + result.Message); return; }
            _recentRoomJoin = false;
            _log.LogInfo("No Wait joined " + local.Name + " beside " + nearest.Name + ".");
        }

        public void OnSceneLoaded()
        {
            _trackedLocalCharacter = null;
            _nextAttempt = 0f;
        }

        private PlayerEntry FindNearestLiving(PlayerEntry local)
        {
            PlayerEntry nearest = null;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < _players.Entries.Count; i++)
            {
                PlayerEntry candidate = _players.Entries[i];
                if (candidate == null || candidate.ActorNumber == local.ActorNumber || candidate.Character == null || candidate.Character.data == null || !candidate.Character.IsInitialized) continue;
                if (candidate.Character.data.dead || candidate.Character.data.fullyPassedOut) continue;
                float distance = (candidate.Character.Center - local.Character.Center).sqrMagnitude;
                if (distance < nearestDistance) { nearestDistance = distance; nearest = candidate; }
            }
            return nearest;
        }
    }
}
