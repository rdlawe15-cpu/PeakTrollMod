using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class MirageManager
    {
        private readonly ManualLogSource _log;
        private readonly List<MirageRecord> _records = new List<MirageRecord>();
        public int Count { get { return _records.Count; } }
        public int FakeEnemyCount { get { int n = 0; for (int i = 0; i < _records.Count; i++) if (_records[i].Kind == MirageKind.FakeEnemy) n++; return n; } }
        public MirageManager(ManualLogSource log) { _log = log; }

        public void Tick()
        {
            for (int i = _records.Count - 1; i >= 0; i--) if (_records[i].Object == null) _records.RemoveAt(i);
        }

        public ActionResult CreateStatic(MirageKind kind, Vector3 position, Quaternion rotation, int creator, int targetActor = 0)
        {
            if (!CanCreate()) return ActionResult.Fail("Maximum mod-spawned object count reached.");
            Type type = kind == MirageKind.Luggage ? typeof(Luggage) : kind == MirageKind.AmuletStatue ? typeof(Peak.ScoutStatue) : kind == MirageKind.CapybaraPool ? typeof(Capybara) : null;
            if (type == null) return ActionResult.Fail("Unsupported static mirage type.");
            UnityEngine.Object[] sources = Resources.FindObjectsOfTypeAll(type); GameObject source = null;
            for (int i = 0; i < sources.Length; i++) { Component component = sources[i] as Component; if (component != null && component.gameObject.scene.IsValid()) { source = component.gameObject; break; } }
            if (source == null && sources.Length > 0) { Component component = sources[0] as Component; if (component != null) source = component.gameObject; }
            if (source == null) return ActionResult.Fail("No loaded visual source for " + kind + ".");
            GameObject clone = VisualCloneFactory.CloneVisuals(source, "PTM_Mirage_" + kind, position, rotation);
            if (clone == null) return ActionResult.Fail("Visual extraction failed.");
            Register(kind, clone, targetActor, creator);
            return ActionResult.Ok("Created " + kind + " mirage.");
        }

        public ActionResult CreateScout(PlayerEntry victim, int appearanceActor, Vector3 position, MirageBehavior behavior, float lifetime, int creator)
        {
            if (!CanCreate()) return ActionResult.Fail("Maximum mod-spawned object count reached.");
            PlayerEntry appearance = TrollModPlugin.Instance.Players.Find(appearanceActor);
            if (appearance == null || appearance.Character == null) return ActionResult.Fail("Appearance source left the lobby.");
            GameObject clone = VisualCloneFactory.CloneVisuals(appearance.Character.gameObject, "PTM_MirageScout", position, Quaternion.LookRotation((victim.Character.Center - position).normalized, Vector3.up));
            if (clone == null) return ActionResult.Fail("Scout visual extraction failed.");
            clone.AddComponent<MiragePuppetController>().Initialize(victim.Character, behavior, lifetime, 2.2f, 5f, 2f);
            Register(MirageKind.Scout, clone, victim.ActorNumber, creator);
            return ActionResult.Ok("Created Mirage Scout for " + victim.Name + ".");
        }

        public ActionResult CreateFakeEnemy(PlayerEntry victim, FakeEnemyKind enemy, Vector3 position, MirageBehavior behavior, float lifetime, int creator)
        {
            if (!CanCreate()) return ActionResult.Fail("Maximum mod-spawned object count reached.");
            Type type = enemy == FakeEnemyKind.Scoutmaster ? typeof(Scoutmaster) : enemy == FakeEnemyKind.MushroomZombie ? typeof(MushroomZombie) : typeof(Looker);
            UnityEngine.Object[] sources = Resources.FindObjectsOfTypeAll(type); GameObject source = null;
            for (int i = 0; i < sources.Length; i++) { Component c = sources[i] as Component; if (c != null) { source = c.gameObject; if (source.activeInHierarchy) break; } }
            if (source == null) return ActionResult.Fail("No loaded safe visual source for " + enemy + ".");
            Vector3 look = victim.Character.Center - position; look.y = 0f;
            GameObject clone = VisualCloneFactory.CloneVisuals(source, "PTM_FakeEnemy_" + enemy, position, look.sqrMagnitude > .01f ? Quaternion.LookRotation(look.normalized, Vector3.up) : Quaternion.identity);
            if (clone == null) return ActionResult.Fail("Enemy visual extraction failed.");
            clone.AddComponent<MiragePuppetController>().Initialize(victim.Character, behavior, lifetime, 2.5f, 6f, 2.5f);
            Register(MirageKind.FakeEnemy, clone, victim.ActorNumber, creator);
            return ActionResult.Ok("Created harmless fake " + enemy + ".");
        }

        private void Register(MirageKind kind, GameObject go, int target, int creator)
        {
            MirageRecord record = new MirageRecord(); record.Id = Guid.NewGuid(); record.Kind = kind; record.CreatorActor = creator; record.CreatedUtc = DateTime.UtcNow; record.Position = go.transform.position; record.Rotation = go.transform.rotation; record.TargetActor = target; record.Object = go; _records.Add(record);
            TrollModPlugin plugin = TrollModPlugin.Instance;
            if (plugin != null && plugin.LuggageNavigation != null) plugin.LuggageNavigation.SuppressModMirage(go);
            if (TrollModPlugin.Instance.Settings.MirageDebug.Value) _log.LogInfo("Mirage " + record.Id + " created: " + kind);
        }

        private bool CanCreate() { Tick(); return _records.Count < Mathf.Clamp(TrollModPlugin.Instance.Settings.MaxSpawnedObjects.Value, 1, 64); }

        public void SuppressTrackedVisuals(Action<Renderer> suppress)
        {
            if (suppress == null) return;
            for (int i = 0; i < _records.Count; i++)
            {
                GameObject visual = _records[i].Object;
                if (visual == null) continue;
                Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
                for (int j = 0; j < renderers.Length; j++) suppress(renderers[j]);
            }
        }

        public void UndoLast() { if (_records.Count == 0) return; MirageRecord record = _records[_records.Count - 1]; if (record.Object != null) UnityEngine.Object.Destroy(record.Object); _records.RemoveAt(_records.Count - 1); }
        public void ClearType(MirageKind kind) { for (int i = _records.Count - 1; i >= 0; i--) if (_records[i].Kind == kind) { if (_records[i].Object != null) UnityEngine.Object.Destroy(_records[i].Object); _records.RemoveAt(i); } }
        public void ClearTarget(int actor) { for (int i=_records.Count-1;i>=0;i--) if(_records[i].TargetActor==actor){if(_records[i].Object!=null)UnityEngine.Object.Destroy(_records[i].Object);_records.RemoveAt(i);} }
        public void ClearAll() { for (int i = 0; i < _records.Count; i++) if (_records[i].Object != null) UnityEngine.Object.Destroy(_records[i].Object); _records.Clear(); }
    }
}
