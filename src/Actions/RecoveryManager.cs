using BepInEx.Logging;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class RecoveryManager
    {
        private readonly ManualLogSource _log;
        private readonly PlayerManager _players;
        private readonly PlayerActions _actions;
        private Vector3 _lastSafePosition;
        private bool _hasLastSafePosition;
        private Character _trackedCharacter;
        private Character _settleCharacter;
        private float _settleUntil;

        public RecoveryManager(ManualLogSource log, PlayerManager players, PlayerActions actions)
        {
            _log = log;
            _players = players;
            _actions = actions;
        }

        public bool HasLastSafePosition { get { return _hasLastSafePosition; } }

        public void Tick()
        {
            PlayerEntry local = _players.Local;
            if (local == null || local.Character == null || local.Character.data == null || !local.Character.IsInitialized) return;
            if (_settleCharacter != null)
            {
                if (Time.unscaledTime <= _settleUntil && local.Character == _settleCharacter) _actions.HaltVelocityLocal(local);
                else _settleCharacter = null;
            }
            if (_trackedCharacter != local.Character) { _trackedCharacter = local.Character; _hasLastSafePosition = false; }
            CharacterData data = local.Character.data;
            if (data.dead || data.fullyPassedOut || !data.isGrounded || data.groundedFor < .75f || data.isInWater || data.isClimbingAnything) return;
            Vector3 safe;
            if (!PlayerActions.TrySafePosition(local.Character.Center, out safe)) return;
            _lastSafePosition = safe;
            _hasLastSafePosition = true;
        }

        public void OnSceneLoaded()
        {
            _trackedCharacter = null;
            _settleCharacter = null;
            _hasLastSafePosition = false;
        }

        public ActionResult RecoverLastSafeGround()
        {
            if (!_hasLastSafePosition) return ActionResult.Fail("No safe grounded position has been recorded in this scene yet.");
            return MoveLocalTo(_lastSafePosition, "last safe ground");
        }

        public ActionResult RecoverNearestScout()
        {
            PlayerEntry local = _players.Local;
            if (local == null || local.Character == null) return ActionResult.Fail("Local player unavailable.");
            PlayerEntry nearest = null;
            Vector3 destination = Vector3.zero;
            float best = float.MaxValue;
            bool originValid = PlayerActions.Finite(local.Character.Center);
            for (int i = 0; i < _players.Entries.Count; i++)
            {
                PlayerEntry candidate = _players.Entries[i];
                Vector3 safe;
                if (!TrySafeScout(candidate, local, out safe)) continue;
                float distance = originValid ? (candidate.Character.Center - local.Character.Center).sqrMagnitude : candidate.Character.Center.y;
                if (distance < best) { best = distance; nearest = candidate; destination = safe; }
            }
            if (nearest == null) return ActionResult.Fail("No living scout is standing on safe ground.");
            return MoveLocalTo(destination, "safe ground beside " + nearest.Name);
        }

        public ActionResult RecoverCheckpoint()
        {
            Vector3 checkpoint;
            if (!_actions.TryGetCheckpointPosition(out checkpoint)) return ActionResult.Fail("The active checkpoint spawn is unavailable or unsafe.");
            return MoveLocalTo(checkpoint, "the active checkpoint");
        }

        public ActionResult RecoverStart()
        {
            Vector3 start;
            if (!_actions.TryGetStartEnd(false, out start)) return ActionResult.Fail("The expedition start reference is unavailable.");
            return MoveLocalTo(start, "the expedition start");
        }

        public ActionResult StabilizeSelf()
        {
            PlayerEntry local = _players.Local;
            if (local == null) return ActionResult.Fail("Local player unavailable.");
            _actions.StopFlightLocal(local);
            return _actions.HaltVelocityLocal(local);
        }

        public ActionResult RestoreSelf()
        {
            PlayerEntry local = _players.Local;
            if (local == null) return ActionResult.Fail("Local player unavailable.");
            ActionResult result = _actions.ResetLocal(local);
            _actions.HaltVelocityLocal(local);
            return result.Success ? ActionResult.Ok("Restored local flight, speed, visibility, statuses, and movement state.") : result;
        }

        private ActionResult MoveLocalTo(Vector3 destination, string label)
        {
            PlayerEntry local = _players.Local;
            if (local == null || local.Character == null || local.Character.data == null) return ActionResult.Fail("Local player unavailable.");
            _actions.StopFlightLocal(local);
            _actions.HaltVelocityLocal(local);
            ActionResult result = local.Character.data.dead || local.Character.data.fullyPassedOut
                ? _actions.ResurrectLocal(local, destination, false)
                : _actions.TeleportLocal(local, destination);
            if (!result.Success) return result;
            _actions.HaltVelocityLocal(local);
            _settleCharacter = local.Character;
            _settleUntil = Time.unscaledTime + 1f;
            _log.LogInfo("Emergency Recovery moved the local scout to " + label + ".");
            return ActionResult.Ok("Recovered to " + label + ".");
        }

        internal static bool TrySafeScout(PlayerEntry candidate, PlayerEntry excluded, out Vector3 safe)
        {
            safe = Vector3.zero;
            if (candidate == null || candidate == excluded || candidate.Character == null || candidate.Character.data == null || !candidate.Character.IsInitialized) return false;
            CharacterData data = candidate.Character.data;
            if (data.dead || data.fullyPassedOut || !data.isGrounded || data.groundedFor < .75f || data.isInWater || data.isClimbingAnything) return false;
            Vector3 right = data.lookDirection_Right; right.y = 0f;
            if (!PlayerActions.Finite(right) || right.sqrMagnitude < .01f) right = Vector3.right;
            return PlayerActions.TrySafePosition(candidate.Character.Center + right.normalized * 1.5f, out safe);
        }
    }
}
