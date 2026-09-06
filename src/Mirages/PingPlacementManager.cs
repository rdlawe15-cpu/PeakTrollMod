using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class PingPlacementManager
    {
        private readonly ManualLogSource _log;
        private readonly MirageManager _mirages;
        private readonly PlayerManager _players;
        private readonly TrollNetworkManager _network;
        private readonly MethodInfo _tryHit;
        private readonly FieldInfo _character;
        public bool Active { get; private set; }
        public bool MultiPlace { get; set; }
        public MirageKind Kind { get; private set; }
        private int _targetActor;

        public PingPlacementManager(ManualLogSource log, MirageManager mirages, PlayerManager players, TrollNetworkManager network)
        {
            _log = log; _mirages = mirages; _players = players; _network = network;
            _tryHit = ReflectionHelpers.Method(typeof(PointPinger), "TryGetPingHit", new Type[] { typeof(RaycastHit).MakeByRefType(), typeof(Vector3) });
            _character = ReflectionHelpers.Field(typeof(PointPinger), "character");
        }

        public void Begin(MirageKind kind, bool multi, int targetActor) { Kind = kind; MultiPlace = multi; _targetActor = targetActor; Active = true; }
        public void Cancel() { Active = false; }

        public bool TryCapture(PointPinger pinger)
        {
            if (!Active || pinger == null || _tryHit == null) return false;
            Character owner = _character == null ? null : _character.GetValue(pinger) as Character;
            if (owner != null && !owner.IsLocal) return false;
            try
            {
                object[] args = new object[] { new RaycastHit(), Input.mousePosition };
                bool hit = (bool)_tryHit.Invoke(pinger, args); if (!hit) return false;
                RaycastHit raycast = (RaycastHit)args[0];
                int creator = _players.Local == null ? 0 : _players.Local.ActorNumber;
                ActionResult result = _targetActor < 0
                    ? PlaceForEveryone(raycast, creator)
                    : PlaceForTarget(_players.Find(_targetActor), raycast, creator);
                _log.LogInfo("Ping placement " + Kind + ": " + result.Message);
                if (!MultiPlace) Active = false;
                return result.Success;
            }
            catch (Exception ex) { _log.LogWarning("Ping interception failed safely: " + ex.Message); return false; }
        }

        private ActionResult PlaceForEveryone(RaycastHit raycast, int creator)
        {
            int placed = 0;
            IList<PlayerEntry> players = _players.Entries;
            for (int i = 0; i < players.Count; i++)
            {
                ActionResult current = PlaceForTarget(players[i], raycast, creator);
                if (current.Success) placed++;
            }
            return placed > 0
                ? ActionResult.Ok("Placed " + Kind + " for " + placed + " compatible viewer(s).")
                : ActionResult.Fail("No compatible viewers were available for this mirage.");
        }

        private ActionResult PlaceForTarget(PlayerEntry target, RaycastHit raycast, int creator)
        {
            if (target == null) return ActionResult.Fail("Selected target left the lobby.");
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, raycast.normal);
            if (target.IsLocal) return _mirages.CreateStatic(Kind, raycast.point, rotation, creator, target.ActorNumber);
            return _network.SendToOwner(NetCommand.MirageProp, target, new object[] { (int)Kind, raycast.point.x, raycast.point.y, raycast.point.z, raycast.normal.x, raycast.normal.y, raycast.normal.z });
        }
    }
}
