using System;
using BepInEx.Logging;

namespace PeakTrollMod
{
    internal sealed class StartAsSkeletonManager
    {
        private readonly ModConfig _settings;
        private readonly ManualLogSource _log;
        private Character _completedCharacter;
        private Character _ownedCharacter;
        private string _status = "Off";
        private float _nextErrorLog;

        public StartAsSkeletonManager(ModConfig settings, ManualLogSource log)
        {
            _settings = settings;
            _log = log;
        }

        public bool Enabled
        {
            get { return _settings.StartAsSkeletonEnabled.Value; }
            set
            {
                if (_settings.StartAsSkeletonEnabled.Value == value) return;
                _settings.StartAsSkeletonEnabled.Value = value;
                if (value)
                {
                    _completedCharacter = null;
                    _status = "Waiting for a living scout on an island.";
                }
                else
                {
                    RestoreOwnedSkeleton();
                    _status = "Off";
                }
            }
        }

        public string Status { get { return _status; } }

        public void Tick()
        {
            Character local = Character.localCharacter;
            if (_ownedCharacter != null)
            {
                if (_ownedCharacter != local || _ownedCharacter.data == null) _ownedCharacter = null;
                else if (!_ownedCharacter.data.isSkeleton)
                {
                    _ownedCharacter = null;
                    _status = "Book of Bones changed your state; the start trigger will not repeat this expedition.";
                }
            }
            if (!Enabled) return;
            if (local == null || local.data == null || !local.IsInitialized || local.inAirport || !GameHandler.IsOnIsland)
            {
                _status = "Waiting for a living scout on an island.";
                return;
            }
            if (local.data.dead || local.data.fullyPassedOut)
            {
                _status = "Waiting until your scout is alive.";
                return;
            }
            if (_completedCharacter == local)
            {
                if (!local.data.isSkeleton) _status = "Start trigger completed for this expedition.";
                return;
            }

            _completedCharacter = local;
            if (local.data.isSkeleton)
            {
                _status = "Already a skeleton — existing state left unchanged.";
                return;
            }
            try
            {
                local.data.SetSkeleton(true);
                _ownedCharacter = local;
                _status = "Active — started this expedition as a skeleton.";
                _log.LogInfo("[PTM] Applied the native Book of Bones skeleton state to the local scout.");
            }
            catch (Exception ex)
            {
                _completedCharacter = null;
                _status = "Skeleton start failed safely; see the BepInEx log.";
                LogFailure("apply", ex);
            }
        }

        public void OnSceneLoaded()
        {
            _completedCharacter = null;
            _status = Enabled ? "Waiting for a living scout on an island." : "Off";
        }

        public void ResetCurrentExpedition()
        {
            Character local = Character.localCharacter;
            RestoreOwnedSkeleton();
            _completedCharacter = local;
            _status = Enabled ? "Reset for this expedition; applies again on the next island." : "Off";
        }

        public void RestoreOwnedSkeleton()
        {
            Character owned = _ownedCharacter;
            _ownedCharacter = null;
            if (owned == null || owned.data == null || !owned.data.isSkeleton) return;
            try
            {
                owned.data.SetSkeleton(false);
                _log.LogInfo("[PTM] Restored the local scout from the mod-owned skeleton state.");
            }
            catch (Exception ex) { LogFailure("restore", ex); }
        }

        private void LogFailure(string operation, Exception ex)
        {
            if (UnityEngine.Time.unscaledTime < _nextErrorLog) return;
            _nextErrorLog = UnityEngine.Time.unscaledTime + 10f;
            _log.LogWarning("[PTM] Start As Skeleton " + operation + " failed safely: " + ex.Message);
        }
    }
}
