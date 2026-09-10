using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Photon.Pun;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class SpawnManager
    {
        private sealed class TrackedSpawn { public GameObject Object; public int CreatorActor; public int TargetActor; public bool IsDynamite; public bool IsItemStorm; }
        private sealed class DynamiteShowerJob
        {
            public int TargetActor;
            public int CreatorActor;
            public int Remaining;
            public float Height;
            public float Spread;
            public float Interval;
            public float NextSpawn;
            public bool LightFuses;
            public bool OneLiveOne;
            public int LitOrdinal;
            public int Spawned;
            public string PrefabName;
        }
        private sealed class ItemStormJob
        {
            public int TargetActor;
            public int CreatorActor;
            public int Remaining;
            public float Height;
            public float Spread;
            public float Interval;
            public float NextSpawn;
            public bool RandomItems;
            public string FixedPrefabName;
            public List<string> PrefabNames;
        }
        private readonly ManualLogSource _log;
        private readonly PlayerManager _players;
        private readonly CapabilityRegistry _capabilities;
        private readonly List<TrackedSpawn> _tracked = new List<TrackedSpawn>();
        private readonly List<DynamiteShowerJob> _showers = new List<DynamiteShowerJob>();
        private readonly List<ItemStormJob> _itemStorms = new List<ItemStormJob>();
        private float _nextOrphanCheck;
        private float _nextTargetRefresh;
        public int Count { get { Prune(); return _tracked.Count; } }
        public int DynamiteCount { get { Prune(); int count = 0; for (int i = 0; i < _tracked.Count; i++) if (_tracked[i].IsDynamite) count++; return count; } }
        public int ItemStormCount { get { Prune(); int count = 0; for (int i = 0; i < _tracked.Count; i++) if (_tracked[i].IsItemStorm) count++; return count; } }
        public SpawnManager(ManualLogSource log, PlayerManager players, CapabilityRegistry capabilities) { _log = log; _players = players; _capabilities = capabilities; }

        public ActionResult SpawnScoutmaster(PlayerEntry target, float distance)
        { return SpawnScoutmaster(target, distance, _players.Local == null ? 0 : _players.Local.ActorNumber); }

        public ActionResult SpawnScoutmaster(PlayerEntry target, float distance, int creatorActor)
        {
            if (target == null || target.Character == null) return ActionResult.Fail("Select an individual target.");
            if (!_players.IsHost) return ActionResult.Fail("Host authority is required.");
            if (!_capabilities.Available(FeatureCapability.ScoutmasterSpawn)) return ActionResult.Fail(_capabilities.Reason(FeatureCapability.ScoutmasterSpawn));
            if (!CanSpawn()) return ActionResult.Fail("Maximum mod-spawned object count reached.");
            try
            {
                Vector3 pos = Nearby(target, distance); GameObject go = PhotonNetwork.InstantiateRoomObject("Character_Scoutmaster", pos, Quaternion.identity, 0, null);
                ForceTarget(go,target,300f);
                Track(go, creatorActor, target.ActorNumber); return ActionResult.Ok("Spawned tracked Scoutmaster targeting " + target.Name + ".");
            }
            catch (Exception ex) { _log.LogWarning("Scoutmaster spawn failed: " + ex); return ActionResult.Fail(ex.Message); }
        }

        public ActionResult SpawnZombie(PlayerEntry target, float distance)
        { return SpawnZombie(target, distance, _players.Local == null ? 0 : _players.Local.ActorNumber); }

        public ActionResult SpawnZombieBehind(PlayerEntry target, float distance)
        {
            if (target == null || target.Character == null || target.Character.data == null) return ActionResult.Fail("Select one available target.");
            if (!_players.IsHost) return ActionResult.Fail("Host authority is required.");
            if (!CanSpawn()) return ActionResult.Fail("Maximum mod-spawned object count reached.");
            MushroomZombie prefab = FindZombiePrefab(); if (prefab == null) return ActionResult.Fail("No runtime zombie prefab reference is loaded.");
            try
            {
                distance = Mathf.Clamp(distance, 4f, 12f);
                Vector3 backward = -target.Character.data.lookDirection_Flat;
                if (!PlayerActions.Finite(backward) || backward.sqrMagnitude < .01f) backward = Vector3.back;
                Vector3 desired = target.Character.Center + backward.normalized * distance + Vector3.up * 3f;
                RaycastHit hit; Vector3 position = Physics.Raycast(desired, Vector3.down, out hit, 12f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? hit.point + Vector3.up * .2f : desired;
                GameObject go = PhotonNetwork.Instantiate(prefab.gameObject.name, position, Quaternion.identity, 0, null);
                ForceTarget(go, target, 300f);
                int creator = _players.Local == null ? 0 : _players.Local.ActorNumber;
                Track(go, creator, target.ActorNumber);
                return ActionResult.Ok("Spawned a tracked Mushroom Zombie behind " + target.Name + ".");
            }
            catch (Exception ex) { _log.LogWarning("Behind-target zombie spawn failed: " + ex); return ActionResult.Fail(ex.Message); }
        }

        public ActionResult SpawnZombie(PlayerEntry target, float distance, int creatorActor)
        {
            if (target == null || target.Character == null) return ActionResult.Fail("Select an individual target.");
            if (!_players.IsHost) return ActionResult.Fail("Host authority is required.");
            if (!CanSpawn()) return ActionResult.Fail("Maximum mod-spawned object count reached.");
            MushroomZombie prefab = FindZombiePrefab(); if (prefab == null) return ActionResult.Fail("No runtime zombie prefab reference is loaded.");
            try
            {
                Vector3 pos = Nearby(target, distance); GameObject go = PhotonNetwork.Instantiate(prefab.gameObject.name, pos, Quaternion.identity, 0, null);
                ForceTarget(go,target,300f);
                Track(go, creatorActor, target.ActorNumber); return ActionResult.Ok("Spawned tracked Mushroom Zombie targeting " + target.Name + ".");
            }
            catch (Exception ex) { _log.LogWarning("Zombie spawn failed: " + ex); return ActionResult.Fail(ex.Message); }
        }

        private static MushroomZombie FindZombiePrefab()
        {
            MushroomZombieSpawner[] spawners = Resources.FindObjectsOfTypeAll<MushroomZombieSpawner>();
            for (int i = 0; i < spawners.Length; i++) if (spawners[i] != null && spawners[i].mushroomZombiePrefab != null) return spawners[i].mushroomZombiePrefab;
            return null;
        }

        public ActionResult StartDynamiteShower(PlayerEntry target, int count, float height, float spread, float interval, bool lightFuses)
        {
            if (!_capabilities.Available(FeatureCapability.DynamiteShower)) return ActionResult.Fail(_capabilities.Reason(FeatureCapability.DynamiteShower));
            if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null) return ActionResult.Fail("Join a Photon room before spawning networked dynamite.");
            if (target == null || target.Character == null) return ActionResult.Fail("Select an individual target.");
            string prefabName = FindDynamitePrefabName();
            if (string.IsNullOrEmpty(prefabName)) return ActionResult.Fail("No loaded Dynamite item prefab was found.");
            count = Mathf.Clamp(count, 1, 64);
            height = Mathf.Clamp(height, 6f, 40f);
            spread = Mathf.Clamp(spread, 1f, 12f);
            interval = Mathf.Clamp(interval, .12f, 1.5f);
            Prune();
            int cap = Mathf.Clamp(TrollModPlugin.Instance.Settings.MaxDynamiteObjects.Value, 1, 128);
            int pending = 0;
            for (int i = 0; i < _showers.Count; i++) pending += _showers[i].Remaining;
            count = Mathf.Min(count, cap - DynamiteCount - pending);
            if (count <= 0) return ActionResult.Fail("Maximum mod-spawned object count would be exceeded.");
            _showers.Add(new DynamiteShowerJob { TargetActor = target.ActorNumber, CreatorActor = PhotonNetwork.LocalPlayer.ActorNumber, Remaining = count, Height = height, Spread = spread, Interval = interval, NextSpawn = Time.unscaledTime, LightFuses = lightFuses, PrefabName = prefabName });
            return ActionResult.Ok("Queued " + count + (lightFuses ? " lit" : " unlit") + " dynamite above " + target.Name + ".");
        }

        public ActionResult StartOneLiveOne(PlayerEntry target, int count, float height, float spread, float interval)
        {
            if (!_capabilities.Available(FeatureCapability.OneLiveOne)) return ActionResult.Fail(_capabilities.Reason(FeatureCapability.OneLiveOne));
            if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null) return ActionResult.Fail("Join a Photon room before spawning networked dynamite.");
            if (target == null || target.Character == null) return ActionResult.Fail("Select an individual target.");
            string prefabName = FindDynamitePrefabName();
            if (string.IsNullOrEmpty(prefabName)) return ActionResult.Fail("No loaded Dynamite item prefab was found.");
            count = Mathf.Clamp(count, 2, 64); height = Mathf.Clamp(height, 6f, 40f); spread = Mathf.Clamp(spread, 1f, 12f); interval = Mathf.Clamp(interval, .12f, 1.5f);
            Prune(); int cap = Mathf.Clamp(TrollModPlugin.Instance.Settings.MaxDynamiteObjects.Value, 1, 128); int pending = 0;
            for (int i = 0; i < _showers.Count; i++) pending += _showers[i].Remaining;
            count = Mathf.Min(count, cap - DynamiteCount - pending);
            if (count < 2) return ActionResult.Fail("One Live One needs room for at least two tracked dynamites.");
            _showers.Add(new DynamiteShowerJob { TargetActor = target.ActorNumber, CreatorActor = PhotonNetwork.LocalPlayer.ActorNumber, Remaining = count, Height = height, Spread = spread, Interval = interval, NextSpawn = Time.unscaledTime, LightFuses = false, OneLiveOne = true, LitOrdinal = UnityEngine.Random.Range(0, count), Spawned = 0, PrefabName = prefabName });
            return ActionResult.Ok("Queued " + count + " dynamites above " + target.Name + " with exactly one hidden live fuse.");
        }

        public ActionResult StartItemStorm(PlayerEntry target, int count, float height, float spread, float interval, bool randomItems, string selectedItemName)
        {
            if (!_capabilities.Available(FeatureCapability.ItemStorm)) return ActionResult.Fail(_capabilities.Reason(FeatureCapability.ItemStorm));
            if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null) return ActionResult.Fail("Join a Photon room before spawning networked items.");
            if (target == null || target.Character == null) return ActionResult.Fail("Select an individual target.");
            List<string> prefabs = FindItemPrefabNames();
            if (prefabs.Count == 0) return ActionResult.Fail("No loaded network item prefabs were found.");
            string fixedName = NormalizePrefabName(selectedItemName);
            if (!randomItems)
            {
                int fixedIndex = prefabs.FindIndex(delegate(string value) { return string.Equals(value, fixedName, StringComparison.OrdinalIgnoreCase); });
                if (fixedIndex < 0) return ActionResult.Fail("The selected item is not a loaded network prefab.");
                fixedName = prefabs[fixedIndex];
            }
            count = Mathf.Clamp(count, 1, 64);
            height = Mathf.Clamp(height, 6f, 40f);
            spread = Mathf.Clamp(spread, 1f, 14f);
            interval = Mathf.Clamp(interval, .12f, 1.5f);
            Prune();
            int cap = Mathf.Clamp(TrollModPlugin.Instance.Settings.MaxItemStormObjects.Value, 1, 128);
            int pending = 0;
            for (int i = 0; i < _itemStorms.Count; i++) pending += _itemStorms[i].Remaining;
            count = Mathf.Min(count, cap - ItemStormCount - pending);
            if (count <= 0) return ActionResult.Fail("The configured Item Storm cap has been reached.");
            _itemStorms.Add(new ItemStormJob { TargetActor = target.ActorNumber, CreatorActor = PhotonNetwork.LocalPlayer.ActorNumber, Remaining = count, Height = height, Spread = spread, Interval = interval, NextSpawn = Time.unscaledTime, RandomItems = randomItems, FixedPrefabName = fixedName, PrefabNames = prefabs });
            return ActionResult.Ok("Queued " + count + (randomItems ? " random items" : " copies of " + fixedName) + " above " + target.Name + ".");
        }

        public ActionResult StartMandrakeRain(PlayerEntry target, int count, float height, float spread, float interval)
        {
            if (!_capabilities.Available(FeatureCapability.Mandrake)) return ActionResult.Fail(_capabilities.Reason(FeatureCapability.Mandrake));
            List<string> prefabs = FindItemPrefabNames();
            string mandrake = prefabs.Find(delegate(string value) { return value.IndexOf("Mandrake", StringComparison.OrdinalIgnoreCase) >= 0; });
            if (string.IsNullOrEmpty(mandrake)) return ActionResult.Fail("No loaded Mandrake network prefab was found.");
            return StartItemStorm(target, count, height, spread, interval, false, mandrake);
        }

        public ActionResult SpawnItemNear(PlayerEntry target, string itemName, int count)
        {
            if (!_capabilities.Available(FeatureCapability.ItemStorm)) return ActionResult.Fail(_capabilities.Reason(FeatureCapability.ItemStorm));
            if (target == null || target.Character == null) return ActionResult.Fail("Select an individual target.");
            count = Mathf.Clamp(count, 1, 12);
            int spawned = 0;
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = UnityEngine.Random.insideUnitCircle * 1.5f;
                Vector3 position = target.Character.Center + new Vector3(offset.x, 1.2f + i * .12f, offset.y);
                ActionResult result = SpawnWorldItem(itemName, position);
                if (!result.Success) return spawned == 0 ? result : ActionResult.Ok("Spawned " + spawned + " item(s); stopped because " + result.Message);
                spawned++;
            }
            return ActionResult.Ok("Spawned " + spawned + " " + itemName + " near " + target.Name + ".");
        }

        public ActionResult SpawnWorldItem(string itemName, Vector3 position)
        {
            if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null) return ActionResult.Fail("Join a Photon room before spawning a world item.");
            if (!CanSpawnItemStorm()) return ActionResult.Fail("The configured network-item cap has been reached.");
            List<string> prefabs = FindItemPrefabNames();
            string normalized = NormalizePrefabName(itemName);
            string prefabName = prefabs.Find(delegate(string value) { return string.Equals(value, normalized, StringComparison.OrdinalIgnoreCase); });
            if (string.IsNullOrEmpty(prefabName)) return ActionResult.Fail("The selected item is not a loaded network prefab.");
            try
            {
                GameObject go = PhotonNetwork.Instantiate("0_Items/" + prefabName, position, UnityEngine.Random.rotation, 0, null);
                if (go == null) throw new InvalidOperationException("Photon returned no item object.");
                Item item = go.GetComponent<Item>();
                if (item != null) item.SetKinematicNetworked(false, position, go.transform.rotation);
                Track(go, PhotonNetwork.LocalPlayer.ActorNumber, go.GetComponent<Dynamite>() != null, true);
                return ActionResult.Ok("Spawned " + prefabName + ".");
            }
            catch (Exception ex) { _log.LogWarning("World item spawn failed safely: " + ex.Message); return ActionResult.Fail(ex.Message); }
        }

        private static List<string> FindItemPrefabNames()
        {
            Item[] items = Resources.FindObjectsOfTypeAll<Item>();
            List<string> result = new List<string>();
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    Item item = items[i];
                    if (item == null || item.gameObject == null) continue;
                    bool sceneObject = item.gameObject.scene.IsValid();
                    if ((pass == 0 && sceneObject) || (pass == 1 && !sceneObject)) continue;
                    string name = NormalizePrefabName(item.gameObject.name);
                    if (!string.IsNullOrEmpty(name) && names.Add(name)) result.Add(name);
                }
                if (result.Count > 0) break;
            }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        private static string NormalizePrefabName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            if (name.StartsWith("PTM_TRACKED_", StringComparison.Ordinal)) name = name.Substring("PTM_TRACKED_".Length);
            if (name.EndsWith("(Clone)", StringComparison.Ordinal)) name = name.Substring(0, name.Length - 7).TrimEnd();
            return name.Trim();
        }

        private static string FindDynamitePrefabName()
        {
            Dynamite[] dynamites = Resources.FindObjectsOfTypeAll<Dynamite>();
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < dynamites.Length; i++)
                {
                    if (dynamites[i] == null || dynamites[i].gameObject == null) continue;
                    bool isSceneObject = dynamites[i].gameObject.scene.IsValid();
                    if ((pass == 0 && isSceneObject) || (pass == 1 && !isSceneObject)) continue;
                    Item item = dynamites[i].item != null ? dynamites[i].item : dynamites[i].GetComponent<Item>();
                    if (item == null || item.gameObject == null) continue;
                    string name = item.gameObject.name;
                    if (name.StartsWith("PTM_TRACKED_", StringComparison.Ordinal)) name = name.Substring("PTM_TRACKED_".Length);
                    if (name.EndsWith("(Clone)", StringComparison.Ordinal)) name = name.Substring(0, name.Length - 7).TrimEnd();
                    if (!string.IsNullOrEmpty(name)) return name;
                }
            }
            return null;
        }

        private static Vector3 Nearby(PlayerEntry target, float distance)
        {
            distance = Mathf.Clamp(distance, 4f, 25f); Vector2 circle = UnityEngine.Random.insideUnitCircle.normalized * distance;
            Vector3 desired = target.Character.Center + new Vector3(circle.x, 2f, circle.y); RaycastHit hit;
            return Physics.Raycast(desired, Vector3.down, out hit, 12f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? hit.point + Vector3.up * .2f : desired;
        }

        private bool CanSpawn() { Prune(); int count=0;for(int i=0;i<_tracked.Count;i++)if(!_tracked[i].IsDynamite&&!_tracked[i].IsItemStorm)count++;return count < Mathf.Clamp(TrollModPlugin.Instance.Settings.MaxSpawnedObjects.Value, 1, 64); }
        private bool CanSpawnDynamite() { return DynamiteCount < Mathf.Clamp(TrollModPlugin.Instance.Settings.MaxDynamiteObjects.Value, 1, 128); }
        private bool CanSpawnItemStorm() { return ItemStormCount < Mathf.Clamp(TrollModPlugin.Instance.Settings.MaxItemStormObjects.Value, 1, 128); }
        private void Track(GameObject go, int creatorActor) { Track(go, creatorActor, 0, false, false); }
        private void Track(GameObject go, int creatorActor, int targetActor) { Track(go, creatorActor, targetActor, false, false); }
        private void Track(GameObject go, int creatorActor, bool isDynamite) { Track(go, creatorActor, 0, isDynamite, false); }
        private void Track(GameObject go, int creatorActor, bool isDynamite, bool isItemStorm) { Track(go, creatorActor, 0, isDynamite, isItemStorm); }
        private void Track(GameObject go, int creatorActor, int targetActor, bool isDynamite, bool isItemStorm) { if (go != null) { go.name = "PTM_TRACKED_" + go.name; _tracked.Add(new TrackedSpawn { Object=go, CreatorActor=creatorActor, TargetActor=targetActor, IsDynamite=isDynamite, IsItemStorm=isItemStorm }); _log.LogInfo("Tracking troll spawn view " + (go.GetComponent<PhotonView>() == null ? 0 : go.GetComponent<PhotonView>().ViewID) + " requested by actor " + creatorActor + (targetActor > 0 ? " targeting actor " + targetActor : string.Empty)); } }
        private void Prune() { for (int i = _tracked.Count - 1; i >= 0; i--) if (_tracked[i].Object == null) _tracked.RemoveAt(i); }

        public void Tick()
        {
            Prune(); TickDynamiteShowers(); TickItemStorms(); if(!_players.IsHost||!PhotonNetwork.InRoom||PhotonNetwork.CurrentRoom==null)return;if(Time.unscaledTime>=_nextTargetRefresh){_nextTargetRefresh=Time.unscaledTime+10f;RefreshTrackedTargets();}if(Time.unscaledTime<_nextOrphanCheck)return;_nextOrphanCheck=Time.unscaledTime+5f;
            List<int> departed=new List<int>();for(int i=0;i<_tracked.Count;i++){int actor=_tracked[i].CreatorActor;if(actor>0&&PhotonNetwork.CurrentRoom.GetPlayer(actor)==null&&!departed.Contains(actor))departed.Add(actor);}for(int i=0;i<departed.Count;i++)ClearForCreator(departed[i]);
        }

        private void RefreshTrackedTargets()
        {
            for(int i=0;i<_tracked.Count;i++)
            {
                TrackedSpawn tracked=_tracked[i];if(tracked.IsDynamite||tracked.Object==null||tracked.TargetActor<=0)continue;
                PlayerEntry target=_players.Find(tracked.TargetActor);if(target==null||target.Character==null)continue;
                if(tracked.Object.GetComponent<Scoutmaster>()!=null||tracked.Object.GetComponent<MushroomZombie>()!=null)ForceTarget(tracked.Object,target,300f);
            }
        }

        private void ForceTarget(GameObject enemy, PlayerEntry target, float duration)
        {
            if(!_players.IsHost||enemy==null||target==null||target.Character==null||target.Character.refs==null||target.Character.refs.view==null)return;
            PhotonView view=enemy.GetComponent<PhotonView>();if(view==null||view.ViewID==0)return;
            view.RPC("RPCA_SetCurrentTarget",RpcTarget.All,new object[]{target.Character.refs.view.ViewID,Mathf.Clamp(duration,10f,600f)});
        }

        private void TickDynamiteShowers()
        {
            for (int i = _showers.Count - 1; i >= 0; i--)
            {
                DynamiteShowerJob job = _showers[i];
                if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null || PhotonNetwork.LocalPlayer.ActorNumber != job.CreatorActor) { _showers.RemoveAt(i); continue; }
                if (Time.unscaledTime < job.NextSpawn) continue;
                PlayerEntry target = _players.Find(job.TargetActor);
                if (target == null || target.Character == null) { _log.LogWarning("Cancelled dynamite shower because its target left."); _showers.RemoveAt(i); continue; }
                if (!CanSpawnDynamite()) { _log.LogWarning("Stopped dynamite shower at the configured dynamite cap."); _showers.RemoveAt(i); continue; }
                Vector2 offset = UnityEngine.Random.insideUnitCircle * job.Spread;
                Vector3 position = target.Character.Center + new Vector3(offset.x, job.Height + UnityEngine.Random.Range(-1.5f, 1.5f), offset.y);
                try
                {
                    GameObject go = PhotonNetwork.Instantiate("0_Items/" + job.PrefabName, position, UnityEngine.Random.rotation, 0, null);
                    if (go == null) throw new InvalidOperationException("Photon returned no Dynamite object.");
                    Item item = go.GetComponent<Item>();
                    if (item != null) item.SetKinematicNetworked(false, position, go.transform.rotation);
                    Dynamite dynamite = go.GetComponent<Dynamite>();
                    bool shouldLight = job.LightFuses || (job.OneLiveOne && job.Spawned == job.LitOrdinal);
                    if (shouldLight && dynamite != null) dynamite.LightFlare();
                    Track(go, job.CreatorActor, true);
                }
                catch (Exception ex) { _log.LogWarning("Dynamite shower spawn failed safely: " + ex.Message); _showers.RemoveAt(i); continue; }
                job.Spawned++; job.Remaining--;
                job.NextSpawn = Time.unscaledTime + job.Interval;
                if (job.Remaining <= 0) _showers.RemoveAt(i);
            }
        }

        private void TickItemStorms()
        {
            for (int i = _itemStorms.Count - 1; i >= 0; i--)
            {
                ItemStormJob job = _itemStorms[i];
                if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null || PhotonNetwork.LocalPlayer.ActorNumber != job.CreatorActor) { _itemStorms.RemoveAt(i); continue; }
                if (Time.unscaledTime < job.NextSpawn) continue;
                PlayerEntry target = _players.Find(job.TargetActor);
                if (target == null || target.Character == null) { _log.LogWarning("Cancelled Item Storm because its target left."); _itemStorms.RemoveAt(i); continue; }
                if (!CanSpawnItemStorm()) { _log.LogWarning("Stopped Item Storm at its configured cap."); _itemStorms.RemoveAt(i); continue; }
                if (job.PrefabNames == null || job.PrefabNames.Count == 0) { _itemStorms.RemoveAt(i); continue; }
                string prefabName = job.RandomItems ? job.PrefabNames[UnityEngine.Random.Range(0, job.PrefabNames.Count)] : job.FixedPrefabName;
                Vector2 offset = UnityEngine.Random.insideUnitCircle * job.Spread;
                Vector3 position = target.Character.Center + new Vector3(offset.x, job.Height + UnityEngine.Random.Range(-1.5f, 1.5f), offset.y);
                try
                {
                    GameObject go = PhotonNetwork.Instantiate("0_Items/" + prefabName, position, UnityEngine.Random.rotation, 0, null);
                    if (go == null) throw new InvalidOperationException("Photon returned no item object.");
                    Item item = go.GetComponent<Item>();
                    if (item != null) item.SetKinematicNetworked(false, position, go.transform.rotation);
                    Track(go, job.CreatorActor, go.GetComponent<Dynamite>() != null, true);
                }
                catch (Exception ex)
                {
                    _log.LogWarning("Item Storm could not spawn " + prefabName + ": " + ex.Message);
                    job.PrefabNames.RemoveAll(delegate(string value) { return string.Equals(value, prefabName, StringComparison.OrdinalIgnoreCase); });
                    if (!job.RandomItems || job.PrefabNames.Count == 0) { _itemStorms.RemoveAt(i); continue; }
                }
                job.Remaining--;
                job.NextSpawn = Time.unscaledTime + job.Interval;
                if (job.Remaining <= 0) _itemStorms.RemoveAt(i);
            }
        }

        public void ClearAll()
        {
            _showers.Clear(); _itemStorms.Clear(); Prune();
            for (int i = _tracked.Count - 1; i >= 0; i--)
            {
                GameObject go = _tracked[i].Object; if (go == null) continue;
                try { PhotonView view = go.GetComponent<PhotonView>(); if (!_players.IsHost && view != null && !view.IsMine) continue; if (view != null && view.ViewID != 0) PhotonNetwork.Destroy(go); else UnityEngine.Object.Destroy(go); _tracked.RemoveAt(i); }
                catch (Exception ex) { _log.LogWarning("Tracked spawn cleanup failed: " + ex.Message); }
            }
        }

        public int ClearLocalDynamite()
        {
            _showers.Clear(); Prune(); int removed = 0;
            for (int i = _tracked.Count - 1; i >= 0; i--)
            {
                if (!_tracked[i].IsDynamite) continue;
                GameObject go = _tracked[i].Object;
                try
                {
                    if (go != null)
                    {
                        PhotonView view = go.GetComponent<PhotonView>();
                        if (view != null && !view.IsMine) continue;
                        if (view != null && view.ViewID != 0) PhotonNetwork.Destroy(go); else UnityEngine.Object.Destroy(go);
                    }
                    removed++; _tracked.RemoveAt(i);
                }
                catch (Exception ex) { _log.LogWarning("Dynamite cleanup failed: " + ex.Message); }
            }
            return removed;
        }

        public int ClearLocalItemStorm()
        {
            _itemStorms.Clear(); Prune(); int removed = 0;
            for (int i = _tracked.Count - 1; i >= 0; i--)
            {
                if (!_tracked[i].IsItemStorm) continue;
                GameObject go = _tracked[i].Object;
                try
                {
                    if (go != null)
                    {
                        PhotonView view = go.GetComponent<PhotonView>();
                        if (view != null && !view.IsMine) continue;
                        if (view != null && view.ViewID != 0) PhotonNetwork.Destroy(go); else UnityEngine.Object.Destroy(go);
                    }
                    removed++; _tracked.RemoveAt(i);
                }
                catch (Exception ex) { _log.LogWarning("Item Storm cleanup failed: " + ex.Message); }
            }
            return removed;
        }

        public int ClearForCreator(int creatorActor)
        {
            if (!_players.IsHost) return 0; int removed=0;
            for(int i=_tracked.Count-1;i>=0;i--)
            {
                if(_tracked[i].CreatorActor!=creatorActor)continue;GameObject go=_tracked[i].Object;
                try{if(go!=null){PhotonView view=go.GetComponent<PhotonView>();if(view!=null&&view.ViewID!=0)PhotonNetwork.Destroy(go);else UnityEngine.Object.Destroy(go);}removed++;_tracked.RemoveAt(i);}catch(Exception ex){_log.LogWarning("Requested spawn cleanup failed: "+ex.Message);}
            }
            return removed;
        }
    }
}
