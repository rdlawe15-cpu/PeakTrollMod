using System;
using System.Collections.Generic;
using BepInEx.Logging;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class TrollNetworkManager : IOnEventCallback
    {
        private const byte EventCode = 197;
        private const int ProtocolVersion = 6;
        private readonly ManualLogSource _log;
        private readonly PlayerManager _players;
        private readonly PlayerActions _actions;
        private readonly MirageManager _mirages;
        private readonly AudioManager _audio;
        private readonly HashSet<int> _compatible = new HashSet<int>();
        private readonly Dictionary<int, int> _advertisedProtocols = new Dictionary<int, int>();
        private readonly Dictionary<int, string> _advertisedVersions = new Dictionary<int, string>();
        private readonly Dictionary<int, bool> _advertisedReady = new Dictionary<int, bool>();
        private bool _localReady;
        private float _nextHello;
        private bool _registered;
        public int CompatibleCount { get { return _compatible.Count; } }
        public bool HostSupportsRequests { get { return _players.IsHost || (PhotonNetwork.InRoom && PhotonNetwork.MasterClient != null && IsCompatible(PhotonNetwork.MasterClient.ActorNumber)); } }

        public TrollNetworkManager(ManualLogSource log, PlayerManager players, PlayerActions actions, MirageManager mirages, AudioManager audio)
        {
            _log = log; _players = players; _actions = actions; _mirages = mirages; _audio = audio;
            PhotonNetwork.AddCallbackTarget(this); _registered = true;
        }

        public void Tick()
        {
            if (!PhotonNetwork.InRoom) { _compatible.Clear(); _advertisedProtocols.Clear(); _advertisedVersions.Clear(); _advertisedReady.Clear(); _localReady=false; return; }
            if (PhotonNetwork.LocalPlayer != null) { int actor=PhotonNetwork.LocalPlayer.ActorNumber; _compatible.Add(actor); _advertisedProtocols[actor]=ProtocolVersion; _advertisedVersions[actor]=TrollModPlugin.Version; _advertisedReady[actor]=_localReady; }
            if (Time.unscaledTime >= _nextHello) { _nextHello = Time.unscaledTime + 5f; Broadcast(NetCommand.Hello, Pack(NetCommand.Hello, PhotonNetwork.LocalPlayer.ActorNumber, new object[] { ProtocolVersion, TrollModPlugin.Version, _localReady }), true); }
        }

        public bool IsCompatible(int actor) { return _compatible.Contains(actor); }
        public bool HasAdvertisement(int actor) { return _advertisedProtocols.ContainsKey(actor); }
        public bool IsReady(int actor) { bool ready; return _advertisedReady.TryGetValue(actor,out ready)&&ready; }
        public bool LocalReady { get { return _localReady; } }
        public void SetLocalReady(bool ready) { _localReady=ready;_nextHello=0f; }
        public string CompatibilityStatus(int actor)
        {
            int protocol; string version;
            if (!_advertisedProtocols.TryGetValue(actor, out protocol)) return "No mod detected";
            _advertisedVersions.TryGetValue(actor, out version);
            if (protocol != ProtocolVersion) return "Protocol v" + protocol + " — needs v" + ProtocolVersion;
            if (!string.IsNullOrEmpty(version) && version != TrollModPlugin.Version) return "v" + version + " — version differs";
            return "Compatible v" + (string.IsNullOrEmpty(version) ? TrollModPlugin.Version : version);
        }

        public ActionResult SendToOwner(NetCommand command, PlayerEntry target, object[] args)
        {
            if (target == null) return ActionResult.Fail("No target selected.");
            if (command == NetCommand.Ragdoll) return _actions.RagdollLocal(target, V3(args, 0), Float(args, 3, 20f), Float(args, 4, 1.5f));
            if (command == NetCommand.Launch) return _actions.LaunchLocal(target, V3(args, 0), Float(args, 3, 35f), Bool(args, 4, true));
            if (command == NetCommand.Teleport) return _actions.TeleportLocal(target, V3(args, 0));
            if (command == NetCommand.Knockout) return _actions.KnockoutLocal(target);
            if (command == NetCommand.Eliminate) return _actions.EliminateLocal(target);
            if (command == NetCommand.GiveItem) return _actions.GiveItemLocal(target, String(args, 0, string.Empty, 128));
            if (command == NetCommand.Status && _players.IsHost && Int(args, 0, 0) != 7 && Int(args, 0, 0) != 9 && Int(args, 0, 0) != 12) return _actions.ApplyStatusAsHost(target, Int(args, 0, 0), Float(args, 1, .1f));
            if (target.IsLocal) return Execute(command, target.ActorNumber, args, target.ActorNumber);
            if (!IsCompatible(target.ActorNumber))
            {
                return ActionResult.Fail(target.Name + " does not advertise PEAK Troll Mod; this action has no safe vanilla fallback.");
            }
            return Send(command, target.ActorNumber, args, new int[] { target.ActorNumber });
        }

        public ActionResult RagdollOffMountain(PlayerEntry target, float strength)
        {
            return _actions.RagdollOffMountainLocal(target, Mathf.Clamp(strength, 1f, 75f));
        }

        public ActionResult HorizontalRagdoll(PlayerEntry target, float strength)
        {
            return _actions.HorizontalRagdollLocal(target, Mathf.Clamp(strength, 25f, 150f));
        }

        public ActionResult WrongMountain(PlayerEntry first, PlayerEntry second, float strength)
        {
            return _actions.WrongMountainLocal(first, second, Mathf.Clamp(strength, 25f, 150f));
        }

        public ActionResult PositionRoulette(IList<PlayerEntry> players)
        {
            return _actions.PositionRouletteLocal(players);
        }

        public ActionResult SkyLaunch(PlayerEntry target, float height)
        {
            return _actions.SkyLaunchLocal(target, Mathf.Clamp(height, 25f, 300f));
        }

        public ActionResult HorizonLaunch(PlayerEntry target, float distance)
        {
            return _actions.HorizonLaunchLocal(target, Mathf.Clamp(distance, 25f, 300f));
        }

        public ActionResult Resurrect(PlayerEntry target)
        {
            return _actions.ResurrectAtLastLivingPosition(target);
        }

        public ActionResult ClearStatuses(PlayerEntry target)
        {
            if (target == null) return ActionResult.Fail("No target selected.");
            ActionResult native = _players.IsHost ? _actions.ClearHostStatuses(target) : null;
            ActionResult owner = null;
            if (target.IsLocal) owner = _actions.ClearModStatusesLocal(target);
            else if (IsCompatible(target.ActorNumber)) owner = Send(NetCommand.ClearStatuses, target.ActorNumber, new object[0], new int[] { target.ActorNumber });
            if (owner != null && owner.Success) return owner;
            return native ?? owner ?? ActionResult.Fail(target.Name + " needs PEAK Troll Mod for non-host status cleanup.");
        }

        public ActionResult RequestHostSpawn(NetCommand command, PlayerEntry target, float distance)
        {
            if (target == null || target.Character == null) return ActionResult.Fail("Select an individual target.");
            if (command != NetCommand.HostSpawnScoutmaster && command != NetCommand.HostSpawnZombie) return ActionResult.Fail("Invalid host request.");
            distance = Mathf.Clamp(distance, 4f, 25f);
            if (_players.IsHost) return command == NetCommand.HostSpawnScoutmaster ? TrollModPlugin.Instance.Spawns.SpawnScoutmaster(target, distance) : TrollModPlugin.Instance.Spawns.SpawnZombie(target, distance);
            if (!PhotonNetwork.InRoom || PhotonNetwork.MasterClient == null) return ActionResult.Fail("No Photon host is available.");
            int hostActor = PhotonNetwork.MasterClient.ActorNumber;
            if (!IsCompatible(hostActor)) return ActionResult.Fail("The host needs PEAK Troll Mod for this request.");
            return Send(command, target.ActorNumber, new object[] { distance }, new int[] { hostActor });
        }

        public ActionResult RequestClearMySpawns()
        {
            if (_players.Local == null) return ActionResult.Fail("Local player unavailable.");
            if (_players.IsHost) { int removed=TrollModPlugin.Instance.Spawns.ClearForCreator(_players.Local.ActorNumber); return ActionResult.Ok("Cleared " + removed + " requested spawns."); }
            if (!PhotonNetwork.InRoom || PhotonNetwork.MasterClient == null || !IsCompatible(PhotonNetwork.MasterClient.ActorNumber)) return ActionResult.Fail("A compatible host is required for tracked cleanup.");
            return Send(NetCommand.HostClearRequestedSpawns, _players.Local.ActorNumber, new object[0], new int[] { PhotonNetwork.MasterClient.ActorNumber });
        }

        public ActionResult SendToAll(NetCommand command, int targetActor, object[] args)
        {
            if (!PhotonNetwork.InRoom) return Execute(command, targetActor, args, targetActor);
            return Broadcast(command, Pack(command, targetActor, args), true);
        }

        public ActionResult BroadcastHelicopterSuppression(bool enabled)
        {
            if (!PhotonNetwork.InRoom) return ActionResult.Fail("Not in a Photon room.");
            RaiseEventOptions options = new RaiseEventOptions(); options.Receivers = ReceiverGroup.Others;
            int actor = PhotonNetwork.LocalPlayer == null ? 0 : PhotonNetwork.LocalPlayer.ActorNumber;
            bool ok = PhotonNetwork.RaiseEvent(EventCode, Pack(NetCommand.SetHelicopterSuppression, actor, new object[] { enabled }), options, SendOptions.SendReliable);
            return ok ? ActionResult.Ok("Advertised helicopter suppression.") : ActionResult.Fail("Photon rejected helicopter suppression advertisement.");
        }

        private ActionResult Send(NetCommand command, int targetActor, object[] args, int[] actors)
        {
            if (!PhotonNetwork.InRoom) return ActionResult.Fail("Not in a Photon room.");
            RaiseEventOptions options = new RaiseEventOptions(); options.TargetActors = actors;
            bool ok = PhotonNetwork.RaiseEvent(EventCode, Pack(command, targetActor, args), options, SendOptions.SendReliable);
            return ok ? ActionResult.Ok("Sent " + command + ".") : ActionResult.Fail("Photon rejected " + command + ".");
        }

        private ActionResult Broadcast(NetCommand command, object[] packed, bool reliable)
        {
            if (!PhotonNetwork.InRoom) return ActionResult.Fail("Not in a Photon room.");
            RaiseEventOptions options = new RaiseEventOptions(); options.Receivers = ReceiverGroup.All;
            bool ok = PhotonNetwork.RaiseEvent(EventCode, packed, options, reliable ? SendOptions.SendReliable : SendOptions.SendUnreliable);
            return ok ? ActionResult.Ok("Broadcast " + command + ".") : ActionResult.Fail("Photon rejected broadcast.");
        }

        private static object[] Pack(NetCommand command, int targetActor, object[] args)
        {
            return new object[] { ProtocolVersion, (byte)command, targetActor, args == null ? new object[0] : args };
        }

        public void OnEvent(EventData photonEvent)
        {
            if (photonEvent == null || photonEvent.Code != EventCode || !PhotonNetwork.InRoom) return;
            Photon.Realtime.Player sender = PhotonNetwork.CurrentRoom == null ? null : PhotonNetwork.CurrentRoom.GetPlayer(photonEvent.Sender);
            if (sender == null) { _log.LogWarning("Rejected troll packet from unknown actor."); return; }
            object[] packet = photonEvent.CustomData as object[];
            if (packet == null || packet.Length != 4 || !(packet[0] is int) || !(packet[1] is byte) || !(packet[2] is int) || !(packet[3] is object[])) { _log.LogWarning("Rejected malformed troll packet from " + photonEvent.Sender); return; }
            int protocol = (int)packet[0];
            NetCommand command = (NetCommand)(byte)packet[1];
            if (!Enum.IsDefined(typeof(NetCommand), command)) { _log.LogWarning("Rejected unknown troll command."); return; }
            int targetActor = (int)packet[2];
            object[] args = (object[])packet[3];
            if (command == NetCommand.Hello)
            {
                _advertisedProtocols[photonEvent.Sender] = protocol;
                string version = args.Length > 1 && args[1] is string ? (string)args[1] : string.Empty;
                _advertisedVersions[photonEvent.Sender] = version.Length > 32 ? version.Substring(0,32) : version;
                _advertisedReady[photonEvent.Sender] = args.Length > 2 && args[2] is bool && (bool)args[2];
                if (protocol == ProtocolVersion) _compatible.Add(photonEvent.Sender); else _compatible.Remove(photonEvent.Sender);
                return;
            }
            if (protocol != ProtocolVersion) return;
            _compatible.Add(photonEvent.Sender);
            try { Execute(command, targetActor, args, photonEvent.Sender); }
            catch (Exception ex) { _log.LogWarning("Troll packet failed safely: " + ex.Message); }
        }

        private ActionResult Execute(NetCommand command, int targetActor, object[] args, int senderActor)
        {
            if (command == NetCommand.SetHelicopterSuppression)
            {
                if (TrollModPlugin.Instance != null && TrollModPlugin.Instance.HelicopterTroll != null) TrollModPlugin.Instance.HelicopterTroll.SetRemote(senderActor, Bool(args, 0, false));
                return ActionResult.Ok("Updated compatible-client helicopter suppression.");
            }
            PlayerEntry target = _players.Find(targetActor);
            if (target == null) return ActionResult.Fail("Target left the lobby.");
            if (command == NetCommand.HostSpawnScoutmaster || command == NetCommand.HostSpawnZombie)
            {
                if (!_players.IsHost) return ActionResult.Fail("Ignored host request on a non-host client.");
                float distance = Mathf.Clamp(Float(args, 0, 10f), 4f, 25f);
                return command == NetCommand.HostSpawnScoutmaster ? TrollModPlugin.Instance.Spawns.SpawnScoutmaster(target, distance, senderActor) : TrollModPlugin.Instance.Spawns.SpawnZombie(target, distance, senderActor);
            }
            if(command==NetCommand.HostClearRequestedSpawns){if(!_players.IsHost||targetActor!=senderActor)return ActionResult.Fail("Rejected invalid cleanup request.");int removed=TrollModPlugin.Instance.Spawns.ClearForCreator(senderActor);return ActionResult.Ok("Cleared "+removed+" requested spawns.");}
            bool ownerOnly = command != NetCommand.Visibility && command != NetCommand.Reset && command != NetCommand.MirageScout && command != NetCommand.FakeEnemy && command != NetCommand.ClearMirages;
            if (ownerOnly && !target.IsLocal) return ActionResult.Fail("Ignored on non-owner client.");
            switch (command)
            {
                case NetCommand.Ragdoll: return _actions.RagdollLocal(target, V3(args, 0), Float(args, 3, 30f), Float(args, 4, 1.5f));
                case NetCommand.Launch: return _actions.LaunchLocal(target, V3(args, 0), Float(args, 3, 35f), Bool(args, 4, true));
                case NetCommand.Speed: return _actions.SetSpeedLocal(target, Mathf.Clamp(Float(args, 0, 1f), .25f, 3f));
                case NetCommand.Flight: return _actions.SetFlightLocal(target, Bool(args, 0, false), Mathf.Clamp(Float(args, 1, 10f), 4f, 20f));
                case NetCommand.Visibility: return _actions.SetVisibilityLocal(target, Bool(args, 0, true));
                case NetCommand.Teleport: return _actions.TeleportLocal(target, Position(args, 0, target.Character.Center, 5000f));
                case NetCommand.Knockout: return _actions.KnockoutLocal(target);
                case NetCommand.Eliminate: return _actions.EliminateLocal(target);
                case NetCommand.Status: return _actions.ApplyStatusLocal(target, Mathf.Clamp(Int(args, 0, 0), 0, 14), Mathf.Clamp(Float(args, 1, .1f), 0f, 2f));
                case NetCommand.ClearStatuses: return _actions.ClearModStatusesLocal(target);
                case NetCommand.GiveItem: return _actions.GiveItemLocal(target, String(args, 0, string.Empty, 128));
                case NetCommand.Reset: return ResetTarget(target);
                case NetCommand.FakeAudio: return _audio.PlayDiscovered(String(args, 0, string.Empty, 128), Position(args, 1, target.Character.Center, 50f), Mathf.Clamp(Float(args, 4, .8f), 0f, 1f), targetActor);
                case NetCommand.MirageScout: return _mirages.CreateScout(target, Int(args, 0, targetActor), Position(args, 1, target.Character.Center, 50f), (MirageBehavior)Mathf.Clamp(Int(args, 4, 0), 0, 7), Mathf.Clamp(Float(args, 5, 12f), 1f, 120f), senderActor);
                case NetCommand.FakeEnemy: return _mirages.CreateFakeEnemy(target, (FakeEnemyKind)Mathf.Clamp(Int(args, 0, 0), 0, 2), Position(args, 1, target.Character.Center, 50f), (MirageBehavior)Mathf.Clamp(Int(args, 4, 0), 0, 7), Mathf.Clamp(Float(args, 5, 12f), 1f, 120f), senderActor);
                case NetCommand.ClearMirages: _mirages.ClearAll(); return ActionResult.Ok("Mirages cleared.");
                case NetCommand.PhantomPings: return TrollModPlugin.Instance.PhantomPings.Start(target, (PhantomPingPattern)Mathf.Clamp(Int(args, 0, 0), 0, 2), Mathf.Clamp(Int(args, 1, 6), 3, 12), Mathf.Clamp(Float(args, 2, 1f), .35f, 3f));
                case NetCommand.CancelPhantomPings: TrollModPlugin.Instance.PhantomPings.Cancel(); return ActionResult.Ok("Phantom Pings cancelled.");
                case NetCommand.MirageProp:
                    int kind = Mathf.Clamp(Int(args, 0, 0), 0, 2); Vector3 normal = V3(args, 4); if (normal.sqrMagnitude < .01f) normal = Vector3.up;
                    return _mirages.CreateStatic((MirageKind)kind, Position(args, 1, target.Character.Center, 50f), Quaternion.FromToRotation(Vector3.up, normal.normalized), senderActor, targetActor);
            }
            return ActionResult.Fail("Unsupported command.");
        }

        private static float Float(object[] a, int i, float d) { return a != null && i < a.Length && a[i] is float ? (float)a[i] : d; }
        private static int Int(object[] a, int i, int d) { return a != null && i < a.Length && a[i] is int ? (int)a[i] : d; }
        private static bool Bool(object[] a, int i, bool d) { return a != null && i < a.Length && a[i] is bool ? (bool)a[i] : d; }
        private static string String(object[] a, int i, string d, int max) { string value = a != null && i < a.Length && a[i] is string ? (string)a[i] : d; return value.Length <= max ? value : value.Substring(0, max); }
        private static Vector3 V3(object[] a, int i) { Vector3 value = new Vector3(Float(a, i, 0f), Float(a, i + 1, 0f), Float(a, i + 2, 0f)); return Finite(value) ? value : Vector3.zero; }
        private static Vector3 Position(object[] a, int i, Vector3 origin, float maxDistance) { Vector3 value=V3(a,i);Vector3 delta=value-origin;if(delta.magnitude>maxDistance)value=origin+delta.normalized*maxDistance;return value; }
        private static bool Finite(Vector3 value) { return !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z) && !float.IsInfinity(value.x) && !float.IsInfinity(value.y) && !float.IsInfinity(value.z); }
        private ActionResult ResetTarget(PlayerEntry target) { _audio.StopForTarget(target.ActorNumber); _audio.RestoreVoice(target.ActorNumber); _mirages.ClearTarget(target.ActorNumber); if(target.IsLocal&&TrollModPlugin.Instance.PhantomPings!=null)TrollModPlugin.Instance.PhantomPings.Cancel(); if(TrollModPlugin.Instance.Appearance!=null)TrollModPlugin.Instance.Appearance.Restore(target); return _actions.ResetLocal(target); }

        public void Dispose() { if (_registered) { PhotonNetwork.RemoveCallbackTarget(this); _registered = false; } }
    }
}
