using BepInEx.Logging;
using System.Collections.Generic;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class ChaosManager
    {
        private sealed class ComboJob { public int TargetActor; public List<int> Steps; public int Index; public float NextStep; }
        private readonly ManualLogSource _log;
        private readonly PlayerManager _players;
        private readonly TrollNetworkManager _network;
        private readonly PlayerActions _actions;
        private readonly MirageManager _mirages;
        private readonly AudioManager _audio;
        private readonly SpawnManager _spawns;
        private ComboJob _combo;
        private float _next;
        public bool Active { get; private set; }
        public float MinimumDelay = 8f;
        public float MaximumDelay = 18f;
        public bool ExcludeSelf = true;
        public bool ComboMandrakeRain = true;
        public bool ComboKnockout = true;
        public bool ComboSkyHigh = true;
        public bool ComboHorizontalYeet = true;
        public bool ComboOneLiveOne;
        public float ComboStepDelay = 2f;
        public bool ComboRunning { get { return _combo != null; } }
        public string ComboStatus { get { return _combo == null ? "Stopped" : "Step " + (_combo.Index + 1) + " / " + _combo.Steps.Count; } }

        public ChaosManager(ManualLogSource log, PlayerManager players, TrollNetworkManager network, PlayerActions actions, MirageManager mirages, AudioManager audio, SpawnManager spawns)
        { _log = log; _players = players; _network = network; _actions = actions; _mirages = mirages; _audio = audio; _spawns = spawns; }

        public void Start() { MinimumDelay = Mathf.Clamp(MinimumDelay, 3f, 60f); MaximumDelay = Mathf.Clamp(MaximumDelay, MinimumDelay, 120f); Active = true; Schedule(); }
        public void Stop() { Active = false; }
        public void StopCombo() { _combo = null; }
        public ActionResult StartCombo(PlayerEntry target)
        {
            if (target == null || target.Character == null) return ActionResult.Fail("Select one available scout for the combo.");
            List<int> steps = new List<int>();
            if (ComboMandrakeRain) steps.Add(0);
            if (ComboKnockout) steps.Add(1);
            if (ComboSkyHigh) steps.Add(2);
            if (ComboHorizontalYeet) steps.Add(3);
            if (ComboOneLiveOne) steps.Add(4);
            if (steps.Count == 0) return ActionResult.Fail("Enable at least one combo step.");
            ComboStepDelay = Mathf.Clamp(ComboStepDelay, .5f, 10f);
            _combo = new ComboJob { TargetActor = target.ActorNumber, Steps = steps, Index = 0, NextStep = Time.unscaledTime };
            return ActionResult.Ok("Started a reusable " + steps.Count + "-step combo on " + target.Name + ".");
        }
        private void Schedule() { _next = Time.unscaledTime + UnityEngine.Random.Range(MinimumDelay, MaximumDelay); }

        public void Tick()
        {
            TickCombo();
            if (!Active || Time.unscaledTime < _next) return; Schedule();
            PlayerEntry target = _players.Random(ExcludeSelf); if (target == null) return;
            if (!target.IsLocal && !_network.IsCompatible(target.ActorNumber))
            {
                if (_network.HostSupportsRequests)
                {
                    ActionResult vanillaResult = _network.RequestHostSpawn(UnityEngine.Random.value < .5f ? NetCommand.HostSpawnScoutmaster : NetCommand.HostSpawnZombie, target, UnityEngine.Random.Range(7f, 14f));
                    _log.LogInfo("Chaos used an unmodded-safe host spawn for " + target.Name + ": " + vanillaResult.Message);
                }
                return;
            }
            int choice = UnityEngine.Random.Range(0, 7); ActionResult result;
            Vector3 random = new Vector3(UnityEngine.Random.Range(-.4f, .4f), 1f, UnityEngine.Random.Range(-.4f, .4f)).normalized;
            if (choice == 0) result = _network.SendToOwner(NetCommand.Ragdoll, target, new object[] { random.x, random.y, random.z, UnityEngine.Random.Range(15f, 55f), 1.5f });
            else if (choice == 1) result = _network.SendToOwner(NetCommand.Launch, target, new object[] { random.x, Mathf.Abs(random.y), random.z, UnityEngine.Random.Range(20f, 50f), true });
            else if (choice == 2) result = _network.SendToOwner(NetCommand.Speed, target, new object[] { UnityEngine.Random.Range(.35f, 2.8f) });
            else if (choice == 3) result = _network.SendToOwner(NetCommand.Knockout, target, new object[0]);
            else if (choice == 4) result = _network.SendToOwner(NetCommand.Status, target, new object[] { UnityEngine.Random.Range(0, 11), UnityEngine.Random.Range(.05f, .25f) });
            else
            {
                Vector3 pos = target.Character.Center - target.Character.data.lookDirection_Flat * UnityEngine.Random.Range(7f, 12f);
                NetCommand cmd = choice == 5 ? NetCommand.MirageScout : NetCommand.FakeEnemy;
                object[] args = cmd == NetCommand.MirageScout ? new object[] { target.ActorNumber, pos.x, pos.y, pos.z, (int)MirageBehavior.StandAndStare, 10f } : new object[] { UnityEngine.Random.Range(0, 3), pos.x, pos.y, pos.z, (int)MirageBehavior.VanishWhenClose, 12f };
                result = _network.SendToOwner(cmd, target, args);
            }
            _log.LogInfo("Chaos event " + choice + " -> " + target.Name + ": " + result.Message);
        }

        private void TickCombo()
        {
            if (_combo == null || Time.unscaledTime < _combo.NextStep) return;
            PlayerEntry target = _players.Find(_combo.TargetActor);
            if (target == null || target.Character == null) { _log.LogWarning("Combo cancelled because its target left."); _combo = null; return; }
            int step = _combo.Steps[_combo.Index]; ActionResult result;
            if (step == 0) result = _spawns.StartMandrakeRain(target, 10, 15f, 5f, .18f);
            else if (step == 1) result = _network.SendToOwner(NetCommand.Knockout, target, new object[0]);
            else if (step == 2) result = _network.SkyLaunch(target, 120f);
            else if (step == 3) result = _network.HorizontalRagdoll(target, 90f);
            else result = _spawns.StartOneLiveOne(target, 12, 16f, 6f, .22f);
            _log.LogInfo("Combo step " + step + " -> " + target.Name + ": " + result.Message);
            _combo.Index++;
            if (_combo.Index >= _combo.Steps.Count) _combo = null;
            else _combo.NextStep = Time.unscaledTime + Mathf.Clamp(ComboStepDelay, .5f, 10f);
        }
    }
}
