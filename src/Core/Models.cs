using System;
using UnityEngine;

namespace PeakTrollMod
{
    internal enum FeatureCapability
    {
        PlayerDiscovery, Ragdoll, Launch, Speed, Visibility, Teleport, StartEndTeleport,
        Knockout, Eliminate, StatusEffects, GiveItem, ScoutmasterSpawn, MushroomZombieSpawn,
        LookerSpawn, Mandrake, PingPlacement, MirageLuggage, MirageStatue, MirageCapybara,
        MirageScout, FakeEnemyScoutmaster, FakeEnemyZombie, FakeEnemyLooker, FakeAudio,
        AppearanceSwap, VoiceSwap, IncomingVoiceMute, TalkWhileKnockedOut, PoisonCloud, SporeCloud, ZombieTargeting,
        DynamiteShower, ItemStorm, SkyLaunch, WrongMountain, CosmeticUnlocks, BadgeUnlocks,
        PositionRoulette, OneLiveOne, ComboBuilder, CampfireReset, HelicopterSuppression, PhantomPings
    }

    internal enum PermissionKind { Anyone, EveryoneNeedsMod, HostOnly, Unsupported }

    internal enum NetCommand : byte
    {
        Hello = 1, Ragdoll = 2, Launch = 3, Speed = 4, Visibility = 5, Teleport = 6,
        Knockout = 7, Eliminate = 8, Status = 9, ClearStatuses = 10, GiveItem = 11,
        Reset = 12, FakeAudio = 13, MirageScout = 14, FakeEnemy = 15, ClearMirages = 16,
        HostSpawnScoutmaster = 17, HostSpawnZombie = 18, HostClearRequestedSpawns = 19,
        MirageProp = 20, SetHelicopterSuppression = 21, PhantomPings = 22, CancelPhantomPings = 23
    }

    internal enum MirageKind { Luggage, AmuletStatue, CapybaraPool, Scout, FakeEnemy }
    internal enum FakeEnemyKind { Scoutmaster, MushroomZombie, Looker }
    internal enum MirageBehavior { StandStill, StandAndStare, FollowAtDistance, ApproachSlowly, ChargeTarget, WalkAcross, RunAway, VanishWhenClose }
    internal enum PhantomPingPattern { BreadcrumbTrail, Circle, BehindYou }

    internal sealed class ActionResult
    {
        public readonly bool Success;
        public readonly string Message;
        private ActionResult(bool success, string message) { Success = success; Message = message; }
        public static ActionResult Ok(string message) { return new ActionResult(true, message); }
        public static ActionResult Fail(string message) { return new ActionResult(false, message); }
    }

    internal sealed class PlayerEntry
    {
        public int ActorNumber;
        public string Name;
        public Character Character;
        public bool IsLocal;
        public override string ToString() { return Name + (IsLocal ? " (You)" : ""); }
    }

    internal sealed class MirageRecord
    {
        public Guid Id;
        public MirageKind Kind;
        public int CreatorActor;
        public DateTime CreatedUtc;
        public Vector3 Position;
        public Quaternion Rotation;
        public int TargetActor;
        public GameObject Object;
    }
}
