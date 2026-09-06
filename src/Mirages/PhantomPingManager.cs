using System;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class PhantomPingManager
    {
        private const int MaximumPings = 12;
        private readonly ManualLogSource _log;
        private readonly PlayerManager _players;
        private readonly MethodInfo _receivePoint;
        private readonly FieldInfo _character;
        private int _targetActor;
        private int _remaining;
        private int _total;
        private float _interval;
        private float _nextPing;
        private PhantomPingPattern _pattern;

        public bool Active { get { return _remaining > 0; } }

        public PhantomPingManager(ManualLogSource log, PlayerManager players)
        {
            _log = log;
            _players = players;
            _receivePoint = ReflectionHelpers.Method(typeof(PointPinger), "ReceivePoint_Rpc", new Type[] { typeof(Vector3), typeof(Vector3) });
            _character = ReflectionHelpers.Field(typeof(PointPinger), "character");
        }

        public ActionResult Start(PlayerEntry target, PhantomPingPattern pattern, int count, float interval)
        {
            if (target == null || target.Character == null || !target.IsLocal) return ActionResult.Fail("Phantom Pings must run on the target owner's client.");
            if (_receivePoint == null || _character == null) return ActionResult.Fail("The verified point-ping display API is unavailable.");
            _targetActor = target.ActorNumber;
            _pattern = (PhantomPingPattern)Mathf.Clamp((int)pattern, 0, 2);
            _total = Mathf.Clamp(count, 3, MaximumPings);
            _remaining = _total;
            _interval = Mathf.Clamp(interval, .35f, 3f);
            _nextPing = Time.unscaledTime;
            return ActionResult.Ok("Started " + _pattern + " with " + _total + " bounded phantom pings.");
        }

        public void Tick()
        {
            if (!Active || Time.unscaledTime < _nextPing) return;
            PlayerEntry target = _players.Find(_targetActor);
            if (target == null || target.Character == null || !target.IsLocal) { Cancel(); return; }
            PointPinger pinger = FindPinger(target.Character);
            if (pinger == null) { _log.LogWarning("Phantom Pings stopped: target PointPinger was unavailable."); Cancel(); return; }

            int index = _total - _remaining;
            Vector3 position = PositionFor(target.Character, index);
            Vector3 normal = Vector3.up;
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up * 12f, Vector3.down, out hit, 30f)) { position = hit.point; normal = hit.normal; }
            try { _receivePoint.Invoke(pinger, new object[] { position, normal }); }
            catch (Exception ex) { _log.LogWarning("Phantom ping display failed safely: " + ex.Message); Cancel(); return; }

            _remaining--;
            _nextPing = Time.unscaledTime + _interval;
        }

        private Vector3 PositionFor(Character target, int index)
        {
            Vector3 forward = target.data.lookDirection_Flat;
            forward.y = 0f; if (forward.sqrMagnitude < .01f) forward = target.transform.forward; forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            if (_pattern == PhantomPingPattern.BreadcrumbTrail) return target.Center + forward * (4f + index * 2f);
            if (_pattern == PhantomPingPattern.Circle)
            {
                float angle = (Mathf.PI * 2f * index) / Mathf.Max(3, _total);
                return target.Center + forward * (Mathf.Cos(angle) * 6f) + right * (Mathf.Sin(angle) * 6f);
            }
            float side = index % 2 == 0 ? 1f : -1f;
            return target.Center - forward * (3f + index * .4f) + right * side * 1.5f;
        }

        private PointPinger FindPinger(Character character)
        {
            PointPinger[] pingers = Resources.FindObjectsOfTypeAll<PointPinger>();
            for (int i = 0; i < pingers.Length; i++) if (pingers[i] != null && _character.GetValue(pingers[i]) as Character == character) return pingers[i];
            return null;
        }

        public void Cancel() { _remaining = 0; }
    }
}
