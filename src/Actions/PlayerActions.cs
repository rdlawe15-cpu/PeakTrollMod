using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using Photon.Pun;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class PlayerActions
    {
        private sealed class WrongMountainJob
        {
            public int FirstActor;
            public int SecondActor;
            public Vector3 Direction;
            public float Strength;
            public float ExecuteAt;
        }
        private sealed class CachedState
        {
            public bool HasSpeed;
            public float Speed;
            public bool FlightActive;
            public float FlightSpeed;
            public readonly Dictionary<Rigidbody, FlightBodyState> FlightBodies = new Dictionary<Rigidbody, FlightBodyState>();
            public readonly Dictionary<Renderer, bool> Renderers = new Dictionary<Renderer, bool>();
            public readonly HashSet<CharacterAfflictions.STATUSTYPE> Statuses = new HashSet<CharacterAfflictions.STATUSTYPE>();
            public readonly Dictionary<CharacterAfflictions.STATUSTYPE, float> HostStatusDeltas = new Dictionary<CharacterAfflictions.STATUSTYPE, float>();
        }
        private sealed class FlightBodyState
        {
            public bool UseGravity;
            public float MaxLinearVelocity;
        }

        private readonly ManualLogSource _log;
        private readonly PlayerManager _players;
        private readonly CapabilityRegistry _capabilities;
        private readonly AudioManager _audio;
        private readonly Dictionary<int, CachedState> _states = new Dictionary<int, CachedState>();
        private readonly List<Item> _items = new List<Item>();
        private readonly List<WrongMountainJob> _wrongMountainJobs = new List<WrongMountainJob>();
        private readonly MethodInfo _addForceAtPosition;
        private readonly MethodInfo _fall;
        private readonly MethodInfo _die;
        private readonly MethodInfo _spawnItem;
        private readonly MethodInfo _hostApplyStatuses;
        private readonly FieldInfo _ragdollRigidbodies;
        public IList<Item> Items { get { return _items.AsReadOnly(); } }

        public PlayerActions(ManualLogSource log, PlayerManager players, CapabilityRegistry capabilities, AudioManager audio)
        {
            _log = log; _players = players; _capabilities = capabilities; _audio = audio;
            _addForceAtPosition = ReflectionHelpers.Method(typeof(Character), "AddForceAtPosition", new Type[] { typeof(Vector3), typeof(Vector3), typeof(float) });
            _fall = ReflectionHelpers.Method(typeof(Character), "Fall", new Type[] { typeof(float), typeof(float) });
            _die = ReflectionHelpers.Method(typeof(Character), "DieInstantly", Type.EmptyTypes);
            _spawnItem = ReflectionHelpers.Method(typeof(CharacterItems), "SpawnItemInHand", new Type[] { typeof(string) });
            _hostApplyStatuses = ReflectionHelpers.Method(typeof(CharacterAfflictions), "RPC_ApplyStatusesFromFloatArray", new Type[] { typeof(float[]), typeof(PhotonMessageInfo) });
            _ragdollRigidbodies = ReflectionHelpers.Field(typeof(CharacterRagdoll), "rigidbodies");
            RefreshItemCatalog();
        }

        private CachedState State(PlayerEntry entry)
        {
            CachedState state;
            if (!_states.TryGetValue(entry.ActorNumber, out state)) { state = new CachedState(); _states.Add(entry.ActorNumber, state); }
            return state;
        }

        public void RefreshItemCatalog()
        {
            Item[] found = Resources.FindObjectsOfTypeAll<Item>();
            _items.Clear();
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < found.Length; i++)
            {
                Item item = found[i];
                if (item == null || string.IsNullOrEmpty(item.name) || !names.Add(item.name)) continue;
                _items.Add(item);
            }
            _items.Sort(delegate(Item a, Item b) { return string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase); });
        }

        public ActionResult RagdollLocal(PlayerEntry target, Vector3 direction, float strength, float seconds)
        {
            if (!_capabilities.Available(FeatureCapability.Ragdoll)) return Missing(FeatureCapability.Ragdoll);
            if (target == null || target.Character == null) return ActionResult.Fail("Target unavailable.");
            strength = Mathf.Clamp(strength, 1f, 75f); seconds = Mathf.Clamp(seconds, .25f, 5f);
            direction = direction.sqrMagnitude < .01f ? Vector3.up : direction.normalized;
            try
            {
                ReflectionHelpers.Invoke(target.Character, _fall, seconds, 0f);
                ReflectionHelpers.Invoke(target.Character, _addForceAtPosition, direction * strength, target.Character.Center, 4f);
                return ActionResult.Ok("Ragdolled " + target.Name + " through PEAK's native RPCs; their mod is not required.");
            }
            catch (Exception ex) { return Error("Ragdoll", ex); }
        }

        public ActionResult RagdollOffMountainLocal(PlayerEntry target, float strength)
        {
            if (!_capabilities.Available(FeatureCapability.Ragdoll)) return Missing(FeatureCapability.Ragdoll);
            if (target == null || target.Character == null) return ActionResult.Fail("Target unavailable.");
            strength = Mathf.Clamp(strength, 1f, 75f);
            bool foundDrop;
            Vector3 direction = FindDropDirection(target.Character.Center, out foundDrop);
            try
            {
                ReflectionHelpers.Invoke(target.Character, _fall, 2.5f, 0f);
                ReflectionHelpers.Invoke(target.Character, _addForceAtPosition, direction * strength, target.Character.Center, 4f);
                return ActionResult.Ok("Ragdolled " + target.Name + (foundDrop ? " toward the steepest nearby drop." : " away from the climb route (no nearby drop was detected)."));
            }
            catch (Exception ex) { return Error("Ragdoll off mountain", ex); }
        }

        public ActionResult HorizontalRagdollLocal(PlayerEntry target, float strength)
        {
            if (!_capabilities.Available(FeatureCapability.Ragdoll)) return Missing(FeatureCapability.Ragdoll);
            if (target == null || target.Character == null) return ActionResult.Fail("Target unavailable.");
            strength = Mathf.Clamp(strength, 25f, 150f);
            Vector3 direction = Vector3.zero;
            if (_players.Local != null && _players.Local.Character != null) direction = target.Character.Center - _players.Local.Character.Center;
            direction.y = 0f;
            if (direction.sqrMagnitude < .01f)
            {
                Vector2 random = UnityEngine.Random.insideUnitCircle.normalized;
                direction = new Vector3(random.x, 0f, random.y);
            }
            direction = (direction.normalized + Vector3.up * .06f).normalized;
            try
            {
                ReflectionHelpers.Invoke(target.Character, _fall, 5f, 0f);
                ReflectionHelpers.Invoke(target.Character, _addForceAtPosition, direction * strength, target.Character.Center, 4f);
                return ActionResult.Ok("Ragdolled " + target.Name + " horizontally at bounded strength " + strength.ToString("0") + ".");
            }
            catch (Exception ex) { return Error("Horizontal ragdoll", ex); }
        }

        public ActionResult WrongMountainLocal(PlayerEntry first, PlayerEntry second, float strength)
        {
            if (!_capabilities.Available(FeatureCapability.WrongMountain)) return Missing(FeatureCapability.WrongMountain);
            if (first == null || second == null || first.Character == null || second.Character == null) return ActionResult.Fail("Two available players are required.");
            if (first.ActorNumber == second.ActorNumber) return ActionResult.Fail("Wrong Mountain needs two different players.");
            if (first.Character.refs == null || second.Character.refs == null || first.Character.refs.view == null || second.Character.refs.view == null) return ActionResult.Fail("A target Photon view is unavailable.");
            Vector3 firstPosition = first.Character.Center;
            Vector3 secondPosition = second.Character.Center;
            Vector3 direction = secondPosition - firstPosition; direction.y = 0f;
            if (direction.sqrMagnitude < .01f)
            {
                Vector2 random = UnityEngine.Random.insideUnitCircle.normalized;
                direction = new Vector3(random.x, 0f, random.y);
            }
            direction = (direction.normalized + Vector3.up * .08f).normalized;
            strength = Mathf.Clamp(strength, 25f, 150f);
            try
            {
                first.Character.refs.view.RPC("WarpPlayerRPC", RpcTarget.All, new object[] { secondPosition, true });
                second.Character.refs.view.RPC("WarpPlayerRPC", RpcTarget.All, new object[] { firstPosition, true });
                GiveRandomItem(first);
                GiveRandomItem(second);
                _wrongMountainJobs.Add(new WrongMountainJob { FirstActor = first.ActorNumber, SecondActor = second.ActorNumber, Direction = direction, Strength = strength, ExecuteAt = Time.unscaledTime + .35f });
                return ActionResult.Ok("Swapped " + first.Name + " with " + second.Name + "; opposite ragdoll throws and random hand items are queued.");
            }
            catch (Exception ex) { return Error("Wrong Mountain", ex); }
        }

        public ActionResult PositionRouletteLocal(IList<PlayerEntry> entries)
        {
            if (!_capabilities.Available(FeatureCapability.PositionRoulette)) return Missing(FeatureCapability.PositionRoulette);
            List<PlayerEntry> players = new List<PlayerEntry>();
            Dictionary<int, Vector3> original = new Dictionary<int, Vector3>();
            if (entries != null)
                for (int i = 0; i < entries.Count; i++)
                {
                    PlayerEntry player = entries[i];
                    if (player == null || player.Character == null || player.Character.refs == null || player.Character.refs.view == null || original.ContainsKey(player.ActorNumber)) continue;
                    players.Add(player); original.Add(player.ActorNumber, player.Character.Center);
                }
            if (players.Count < 2) return ActionResult.Fail("Position Roulette needs at least two available scouts.");
            for (int i = players.Count - 1; i > 0; i--)
            {
                int swap = UnityEngine.Random.Range(0, i + 1); PlayerEntry temp = players[i]; players[i] = players[swap]; players[swap] = temp;
            }
            try
            {
                for (int i = 0; i < players.Count; i++)
                {
                    PlayerEntry destinationOwner = players[(i + 1) % players.Count];
                    players[i].Character.refs.view.RPC("WarpPlayerRPC", RpcTarget.All, new object[] { original[destinationOwner.ActorNumber], true });
                }
                return ActionResult.Ok("Rotated " + players.Count + " scouts through a randomized no-fixed-point position roulette.");
            }
            catch (Exception ex) { return Error("Position Roulette", ex); }
        }

        private void GiveRandomItem(PlayerEntry target)
        {
            if (_items.Count == 0 || target == null || target.Character == null || target.Character.refs == null || target.Character.refs.items == null) return;
            Item item = _items[UnityEngine.Random.Range(0, _items.Count)];
            if (item != null && !string.IsNullOrEmpty(item.name)) ReflectionHelpers.Invoke(target.Character.refs.items, _spawnItem, item.name);
        }

        public void Tick()
        {
            for (int i = _wrongMountainJobs.Count - 1; i >= 0; i--)
            {
                WrongMountainJob job = _wrongMountainJobs[i];
                if (Time.unscaledTime < job.ExecuteAt) continue;
                _wrongMountainJobs.RemoveAt(i);
                PlayerEntry first = _players.Find(job.FirstActor);
                PlayerEntry second = _players.Find(job.SecondActor);
                if (first == null || second == null || first.Character == null || second.Character == null) continue;
                try
                {
                    ReflectionHelpers.Invoke(first.Character, _fall, 5f, 0f);
                    ReflectionHelpers.Invoke(second.Character, _fall, 5f, 0f);
                    ReflectionHelpers.Invoke(first.Character, _addForceAtPosition, job.Direction * job.Strength, first.Character.Center, 4f);
                    ReflectionHelpers.Invoke(second.Character, _addForceAtPosition, -job.Direction * job.Strength, second.Character.Center, 4f);
                }
                catch (Exception ex) { _log.LogWarning("Delayed Wrong Mountain throw failed safely: " + ex.Message); }
            }
        }

        public void FixedTick()
        {
            foreach (KeyValuePair<int, CachedState> pair in _states)
            {
                CachedState state = pair.Value;
                if (!state.FlightActive) continue;
                PlayerEntry target = _players.Find(pair.Key);
                if (target == null || !target.IsLocal || target.Character == null || target.Character.data == null || target.Character.data.dead)
                {
                    RestoreFlight(state);
                    continue;
                }

                Vector3 direction = Vector3.zero;
                TrollModPlugin plugin = TrollModPlugin.Instance;
                if (plugin == null || plugin.Ui == null || !plugin.Ui.IsOpen)
                {
                    Vector3 forward = target.Character.data.lookDirection_Flat; forward.y = 0f;
                    Vector3 right = target.Character.data.lookDirection_Right; right.y = 0f;
                    if (forward.sqrMagnitude > .01f) forward.Normalize();
                    if (right.sqrMagnitude > .01f) right.Normalize();
                    float forwardInput = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
                    float rightInput = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
                    float upInput = (Input.GetKey(KeyCode.Space) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ? 1f : 0f);
                    direction = forward * forwardInput + right * rightInput + Vector3.up * upInput;
                    if (direction.sqrMagnitude > 1f) direction.Normalize();
                }
                float boost = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 1.75f : 1f;
                Vector3 velocity = direction * state.FlightSpeed * boost;
                foreach (KeyValuePair<Rigidbody, FlightBodyState> body in state.FlightBodies)
                {
                    if (body.Key == null) continue;
                    body.Key.useGravity = false;
                    body.Key.maxLinearVelocity = Mathf.Max(body.Key.maxLinearVelocity, state.FlightSpeed * 2f);
                    body.Key.linearVelocity = velocity;
                }
            }
        }

        private Vector3 FindDropDirection(Vector3 origin, out bool foundDrop)
        {
            float baseGroundY;
            TryGroundHeight(origin, 14f, out baseGroundY);
            float bestScore = float.MinValue;
            Vector3 best = Vector3.zero;
            const int samples = 24;
            for (int i = 0; i < samples; i++)
            {
                float angle = i * Mathf.PI * 2f / samples;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                float score = 0f;
                bool missingGround = false;
                for (int step = 1; step <= 4; step++)
                {
                    float distance = step * 3f;
                    float groundY;
                    if (!TryGroundHeight(origin + direction * distance, 28f, out groundY))
                    {
                        score += 80f + (5 - step) * 12f;
                        missingGround = true;
                        break;
                    }
                    float drop = baseGroundY - groundY;
                    if (drop > 0f) score += drop * (6f - step);
                    else score += drop * .5f;
                }
                if (missingGround) score += 20f;
                if (score > bestScore) { bestScore = score; best = direction; }
            }

            foundDrop = bestScore >= 18f && best.sqrMagnitude > .01f;
            if (!foundDrop) best = DirectionAwayFromRoute(origin);
            return (best.normalized + Vector3.up * .22f).normalized;
        }

        private static bool TryGroundHeight(Vector3 position, float depth, out float height)
        {
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up * 4f, Vector3.down, out hit, depth, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                height = hit.point.y;
                return true;
            }
            height = position.y - depth;
            return false;
        }

        private Vector3 DirectionAwayFromRoute(Vector3 origin)
        {
            Vector3 center = Vector3.zero;
            int count = 0;
            MapHandler map = UnityEngine.Object.FindFirstObjectByType<MapHandler>();
            if (map != null && map.segments != null)
            {
                for (int i = 0; i < map.segments.Length; i++)
                {
                    MapHandler.MapSegment segment = map.segments[i];
                    if (segment == null || segment.reconnectSpawnPos == null) continue;
                    center += segment.reconnectSpawnPos.position;
                    count++;
                }
            }
            if (count > 0) center /= count;
            else if (_players.Local != null && _players.Local.Character != null) center = _players.Local.Character.Center;
            else center = origin - Vector3.forward;
            Vector3 direction = origin - center; direction.y = 0f;
            return direction.sqrMagnitude > .01f ? direction.normalized : Vector3.forward;
        }

        public ActionResult LaunchLocal(PlayerEntry target, Vector3 direction, float strength, bool ragdoll)
        {
            if (!_capabilities.Available(FeatureCapability.Launch)) return Missing(FeatureCapability.Launch);
            if (target == null || target.Character == null) return ActionResult.Fail("Target unavailable.");
            strength = Mathf.Clamp(strength, 5f, 120f);
            direction = direction.sqrMagnitude < .01f ? Vector3.up : direction.normalized;
            try
            {
                if (ragdoll) ReflectionHelpers.Invoke(target.Character, _fall, 1.5f, 0f);
                ReflectionHelpers.Invoke(target.Character, _addForceAtPosition, direction * strength, target.Character.Center, 4f);
                return ActionResult.Ok("Launched " + target.Name + " through PEAK's native force RPC; no host or target mod required.");
            }
            catch (Exception ex) { return Error("Launch", ex); }
        }

        public ActionResult SkyLaunchLocal(PlayerEntry target, float height)
        {
            if (!_capabilities.Available(FeatureCapability.SkyLaunch)) return Missing(FeatureCapability.SkyLaunch);
            if (target == null || target.Character == null || target.Character.refs == null || target.Character.refs.view == null) return ActionResult.Fail("Target view unavailable.");
            height = Mathf.Clamp(height, 25f, 300f);
            Vector3 destination = target.Character.Center + Vector3.up * height;
            try
            {
                ReflectionHelpers.Invoke(target.Character, _fall, 5f, 0f);
                ReflectionHelpers.Invoke(target.Character, _addForceAtPosition, Vector3.up * 18f, target.Character.Center, 4f);
                target.Character.refs.view.RPC("WarpPlayerRPC", RpcTarget.All, new object[] { destination, true });
                return ActionResult.Ok("Sent " + target.Name + " " + height.ToString("0") + "m into the sky through PEAK's native RPCs.");
            }
            catch (Exception ex) { return Error("Sky launch", ex); }
        }

        public ActionResult SetSpeedLocal(PlayerEntry target, float multiplier)
        {
            if (!_capabilities.Available(FeatureCapability.Speed)) return Missing(FeatureCapability.Speed);
            if (target == null || target.Character == null || target.Character.refs == null || target.Character.refs.movement == null) return ActionResult.Fail("Movement component unavailable.");
            CachedState state = State(target);
            if (!state.HasSpeed) { state.Speed = target.Character.refs.movement.movementModifier; state.HasSpeed = true; }
            target.Character.refs.movement.movementModifier = Mathf.Clamp(multiplier, .25f, 3f);
            return ActionResult.Ok("Speed set to " + multiplier.ToString("0.00") + "x.");
        }

        public ActionResult SetFlightLocal(PlayerEntry target, bool enabled, float speed)
        {
            if (!_capabilities.Available(FeatureCapability.Flight)) return Missing(FeatureCapability.Flight);
            if (target == null || !target.IsLocal || target.Character == null || target.Character.refs == null || target.Character.refs.ragdoll == null || _ragdollRigidbodies == null) return ActionResult.Fail("Flight requires the selected player's compatible owner client.");
            CachedState state = State(target);
            if (!enabled)
            {
                RestoreFlight(state);
                return ActionResult.Ok("Flight disabled for " + target.Name + ".");
            }

            speed = Mathf.Clamp(speed, 4f, 20f);
            IList<Rigidbody> bodies = _ragdollRigidbodies.GetValue(target.Character.refs.ragdoll) as IList<Rigidbody>;
            if (bodies == null) return ActionResult.Fail("No character rigidbody registry was available for flight.");
            for (int i = 0; i < bodies.Count; i++)
            {
                Rigidbody body = bodies[i];
                if (body == null) continue;
                if (!state.FlightBodies.ContainsKey(body)) state.FlightBodies.Add(body, new FlightBodyState { UseGravity = body.useGravity, MaxLinearVelocity = body.maxLinearVelocity });
                body.useGravity = false;
                body.maxLinearVelocity = Mathf.Max(body.maxLinearVelocity, speed * 2f);
            }
            if (state.FlightBodies.Count == 0) return ActionResult.Fail("No character rigidbodies were available for flight.");
            state.FlightSpeed = speed;
            state.FlightActive = true;
            return ActionResult.Ok("Flight enabled for " + target.Name + " at " + speed.ToString("0") + " m/s. Close F7, then use WASD, Space/Ctrl, and Shift.");
        }

        public ActionResult StopFlightLocal(PlayerEntry target)
        {
            if (target == null) return ActionResult.Fail("Local player unavailable.");
            CachedState state;
            if (!_states.TryGetValue(target.ActorNumber, out state) || !state.FlightActive) return ActionResult.Ok("Flight was already disabled.");
            RestoreFlight(state);
            return ActionResult.Ok("Flight disabled and its cached physics restored.");
        }

        private static void RestoreFlight(CachedState state)
        {
            if (state == null) return;
            foreach (KeyValuePair<Rigidbody, FlightBodyState> pair in state.FlightBodies)
            {
                if (pair.Key == null) continue;
                pair.Key.linearVelocity = Vector3.zero;
                pair.Key.useGravity = pair.Value.UseGravity;
                pair.Key.maxLinearVelocity = pair.Value.MaxLinearVelocity;
            }
            state.FlightBodies.Clear();
            state.FlightActive = false;
        }

        public ActionResult SetVisibilityLocal(PlayerEntry target, bool visible)
        {
            if (!_capabilities.Available(FeatureCapability.Visibility)) return Missing(FeatureCapability.Visibility);
            if (target == null || target.Character == null) return ActionResult.Fail("Target unavailable.");
            CachedState state = State(target);
            Renderer[] renderers = target.Character.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!state.Renderers.ContainsKey(renderer)) state.Renderers.Add(renderer, renderer.enabled);
                renderer.enabled = visible;
            }
            return ActionResult.Ok(visible ? "Visibility restored." : "Target hidden on compatible clients.");
        }

        public ActionResult TeleportLocal(PlayerEntry target, Vector3 position)
        {
            if (!_capabilities.Available(FeatureCapability.Teleport)) return Missing(FeatureCapability.Teleport);
            if (target == null || target.Character == null || target.Character.refs == null || target.Character.refs.view == null) return ActionResult.Fail("Target view unavailable.");
            Vector3 safe;
            if (!TrySafePosition(position, out safe)) return ActionResult.Fail("No safe ground near destination.");
            try
            {
                target.Character.refs.view.RPC("WarpPlayerRPC", RpcTarget.All, new object[] { safe, true });
                return ActionResult.Ok("Teleported " + target.Name + ".");
            }
            catch (Exception ex) { return Error("Teleport", ex); }
        }

        public bool TryGetStartEnd(bool end, out Vector3 position)
        {
            position = Vector3.zero;
            MapHandler map = UnityEngine.Object.FindFirstObjectByType<MapHandler>();
            if (map == null || map.segments == null || map.segments.Length == 0) return false;
            int index = end ? map.segments.Length - 1 : 0;
            MapHandler.MapSegment segment = map.segments[index];
            if (segment == null || segment.reconnectSpawnPos == null) return false;
            position = segment.reconnectSpawnPos.position;
            return true;
        }

        public bool TryGetCheckpointPosition(out Vector3 position)
        {
            position = Vector3.zero;
            MapHandler map = UnityEngine.Object.FindFirstObjectByType<MapHandler>();
            if (map == null || MapHandler.CurrentBaseCampSpawnPoint == null) return false;
            return TrySafePosition(MapHandler.CurrentBaseCampSpawnPoint.position, out position);
        }

        public ActionResult KnockoutLocal(PlayerEntry target)
        {
            if (!_capabilities.Available(FeatureCapability.Knockout)) return Missing(FeatureCapability.Knockout);
            if (target == null || target.Character == null) return ActionResult.Fail("Target unavailable.");
            try { target.Character.PassOutInstantly(); return ActionResult.Ok("Knocked out " + target.Name + " through PEAK's native RPC; no host or target mod required."); }
            catch (Exception ex) { return Error("Knockout", ex); }
        }

        public ActionResult EliminateLocal(PlayerEntry target)
        {
            if (!_capabilities.Available(FeatureCapability.Eliminate)) return Missing(FeatureCapability.Eliminate);
            if (target == null || target.Character == null || target.Character.refs == null || target.Character.refs.view == null) return ActionResult.Fail("Target view unavailable.");
            try { target.Character.refs.view.RPC("RPCA_Die", RpcTarget.All, new object[0]); return ActionResult.Ok("Eliminated " + target.Name + " through PEAK's native death RPC; no host or target mod required."); }
            catch (Exception ex) { return Error("Eliminate", ex); }
        }

        public ActionResult ApplyStatusLocal(PlayerEntry target, int statusIndex, float amount)
        {
            if (!_capabilities.Available(FeatureCapability.StatusEffects)) return Missing(FeatureCapability.StatusEffects);
            if (target == null || target.Character == null || target.Character.refs.afflictions == null) return ActionResult.Fail("Affliction component unavailable.");
            CharacterAfflictions.STATUSTYPE status = (CharacterAfflictions.STATUSTYPE)Mathf.Clamp(statusIndex, 0, 14);
            amount = Mathf.Clamp(amount, 0f, 2f);
            try
            {
                bool applied = target.Character.refs.afflictions.AddStatus(status, amount, false, true, true, false);
                if (applied) State(target).Statuses.Add(status);
                return applied ? ActionResult.Ok("Applied " + status + ".") : ActionResult.Fail("The game rejected that status.");
            }
            catch (Exception ex) { return Error("Status", ex); }
        }

        public ActionResult ApplyStatusAsHost(PlayerEntry target, int statusIndex, float amount)
        {
            if (!_players.IsHost) return ActionResult.Fail("PEAK accepts this native status RPC only from the host.");
            if (_hostApplyStatuses == null) return ActionResult.Fail("PEAK's host status RPC is unavailable in this game build.");
            if (target == null || target.Character == null || target.Character.refs == null || target.Character.refs.afflictions == null || target.Character.refs.view == null || target.Character.refs.view.Owner == null) return ActionResult.Fail("Target afflictions/view unavailable.");
            statusIndex = Mathf.Clamp(statusIndex, 0, 14);
            if (statusIndex == 7 || statusIndex == 9 || statusIndex == 12) return ActionResult.Fail("PEAK's host status RPC excludes this status; a compatible target client is required.");
            CharacterAfflictions.STATUSTYPE status = (CharacterAfflictions.STATUSTYPE)statusIndex;
            amount = Mathf.Clamp(amount, .01f, 2f);
            try
            {
                CharacterAfflictions afflictions = target.Character.refs.afflictions;
                float remainingForStatus = Mathf.Max(0f, afflictions.GetStatusCap(status) - afflictions.GetCurrentStatus(status) - afflictions.GetIncrementalStatus(status));
                float accepted = Mathf.Min(amount, remainingForStatus, Mathf.Max(0f, 2f - afflictions.statusSum));
                if (accepted <= 0f) return ActionResult.Fail("That status is already at PEAK's applicable cap.");
                float[] delta = new float[15]; delta[statusIndex] = accepted;
                target.Character.refs.view.RPC("RPC_ApplyStatusesFromFloatArray", target.Character.refs.view.Owner, new object[] { delta });
                CachedState state = State(target); float previous; state.HostStatusDeltas.TryGetValue(status, out previous); state.HostStatusDeltas[status] = previous + accepted;
                return ActionResult.Ok("Host applied " + status + " through PEAK's native validated status RPC; target mod not required.");
            }
            catch (Exception ex) { return Error("Host status", ex); }
        }

        public ActionResult ClearHostStatuses(PlayerEntry target)
        {
            if (!_players.IsHost) return ActionResult.Fail("Only the host can clear host-applied native statuses.");
            if (_hostApplyStatuses == null) return ActionResult.Fail("PEAK's host status RPC is unavailable in this game build.");
            if (target == null || target.Character == null || target.Character.refs == null || target.Character.refs.view == null || target.Character.refs.view.Owner == null) return ActionResult.Fail("Target view unavailable.");
            CachedState state = State(target);
            if (state.HostStatusDeltas.Count == 0) return ActionResult.Ok("No host-applied troll statuses were tracked for " + target.Name + ".");
            try
            {
                float[] delta = new float[15];
                foreach (KeyValuePair<CharacterAfflictions.STATUSTYPE, float> pair in state.HostStatusDeltas) delta[(int)pair.Key] = -Mathf.Max(0f, pair.Value);
                target.Character.refs.view.RPC("RPC_ApplyStatusesFromFloatArray", target.Character.refs.view.Owner, new object[] { delta });
                state.HostStatusDeltas.Clear();
                return ActionResult.Ok("Cleared tracked host-applied statuses through PEAK's native RPC.");
            }
            catch (Exception ex) { return Error("Host status cleanup", ex); }
        }

        public ActionResult ClearModStatusesLocal(PlayerEntry target)
        {
            if (target == null || target.Character == null || target.Character.refs.afflictions == null) return ActionResult.Fail("Affliction component unavailable.");
            CachedState state = State(target);
            foreach (CharacterAfflictions.STATUSTYPE status in state.Statuses) target.Character.refs.afflictions.SetStatus(status, 0f, true);
            state.Statuses.Clear();
            return ActionResult.Ok("Cleared mod-applied statuses.");
        }

        public ActionResult GiveItemLocal(PlayerEntry target, string itemName)
        {
            if (!_capabilities.Available(FeatureCapability.GiveItem)) return Missing(FeatureCapability.GiveItem);
            if (target == null || target.Character == null || target.Character.refs.items == null || string.IsNullOrEmpty(itemName)) return ActionResult.Fail("Item or target unavailable.");
            Item match = null;
            for (int i = 0; i < _items.Count; i++) if (string.Equals(_items[i].name, itemName, StringComparison.OrdinalIgnoreCase)) { match = _items[i]; break; }
            if (match == null) return ActionResult.Fail("Item is not in the runtime catalog.");
            try { ReflectionHelpers.Invoke(target.Character.refs.items, _spawnItem, match.name); return ActionResult.Ok("Requested " + match.name + " in " + target.Name + "'s hands."); }
            catch (Exception ex) { return Error("Give item", ex); }
        }

        public ActionResult ResurrectAtLastLivingPosition(PlayerEntry target)
        {
            if (target == null || target.Character == null) return ActionResult.Fail("Target unavailable.");
            Vector3 desired = target.Character.LastLivingPosition;
            if (!Finite(desired) || desired.sqrMagnitude < .01f) desired = target.Character.Center;
            return ResurrectLocal(target, desired, false);
        }

        public ActionResult ResurrectNearPlayer(PlayerEntry target, PlayerEntry anchor)
        {
            if (anchor == null || anchor.Character == null) return ActionResult.Fail("No living player is available to join.");
            Vector3 direction = anchor.Character.data == null ? Vector3.right : anchor.Character.data.lookDirection_Right;
            direction.y = 0f;
            if (!Finite(direction) || direction.sqrMagnitude < .01f) direction = Vector3.right;
            return ResurrectLocal(target, anchor.Character.Center + direction.normalized * 1.5f, false);
        }

        public ActionResult ResurrectLocal(PlayerEntry target, Vector3 desiredPosition, bool applyPostReviveStatus)
        {
            if (!_capabilities.Available(FeatureCapability.Resurrect)) return Missing(FeatureCapability.Resurrect);
            if (target == null || target.Character == null || target.Character.data == null || target.Character.refs == null || target.Character.refs.view == null) return ActionResult.Fail("Target revive view unavailable.");
            if (!target.Character.data.dead && !target.Character.data.fullyPassedOut) return ActionResult.Fail(target.Name + " is already alive.");
            if (!Finite(desiredPosition)) return ActionResult.Fail("Revive destination is invalid.");
            Vector3 safe;
            if (!TrySafePosition(desiredPosition, out safe)) return ActionResult.Fail("No safe ground was found near the revive destination.");
            try
            {
                target.Character.refs.view.RPC("RPCA_ReviveAtPosition", RpcTarget.All, new object[] { safe, applyPostReviveStatus, -1 });
                return ActionResult.Ok("Resurrected " + target.Name + " through PEAK's native synchronized revive; host and target mod are not required.");
            }
            catch (Exception ex) { return Error("Resurrect", ex); }
        }

        public ActionResult HaltVelocityLocal(PlayerEntry target)
        {
            if (target == null || target.Character == null || target.Character.refs == null || target.Character.refs.ragdoll == null) return ActionResult.Fail("Local ragdoll unavailable.");
            try
            {
                target.Character.refs.ragdoll.HaltBodyVelocity(true);
                target.Character.data.fallSeconds = 0f;
                return ActionResult.Ok("Stopped local character velocity and reset fall damage buildup.");
            }
            catch (Exception ex) { return Error("Stabilize player", ex); }
        }

        public ActionResult ResetLocal(PlayerEntry target)
        {
            if (target == null || target.Character == null) return ActionResult.Fail("Target unavailable.");
            CachedState state;
            if (!_states.TryGetValue(target.ActorNumber, out state)) return ActionResult.Ok("No reversible troll state was cached.");
            RestoreFlight(state);
            if (state.HasSpeed && target.Character.refs != null && target.Character.refs.movement != null) target.Character.refs.movement.movementModifier = state.Speed;
            foreach (KeyValuePair<Renderer, bool> pair in state.Renderers) if (pair.Key != null) pair.Key.enabled = pair.Value;
            if (target.Character.refs != null && target.Character.refs.afflictions != null)
                foreach (CharacterAfflictions.STATUSTYPE status in state.Statuses) target.Character.refs.afflictions.SetStatus(status, 0f, true);
            if (_players.IsHost && state.HostStatusDeltas.Count > 0) ClearHostStatuses(target);
            _states.Remove(target.ActorNumber);
            return ActionResult.Ok("Restored reversible changes for " + target.Name + ".");
        }

        public void ResetKnownPlayers() { _wrongMountainJobs.Clear(); List<PlayerEntry> copy = new List<PlayerEntry>(_players.Entries); for (int i = 0; i < copy.Count; i++) ResetLocal(copy[i]); _states.Clear(); }

        internal static bool TrySafePosition(Vector3 desired, out Vector3 safe)
        {
            safe = desired;
            RaycastHit hit;
            Vector3 probe = desired + Vector3.up * 2f;
            if (Physics.Raycast(probe, Vector3.down, out hit, 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) safe = hit.point + Vector3.up * 1.1f;
            else return false;
            if (Physics.CheckSphere(safe, .35f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) safe += Vector3.up * 1.25f;
            return !Physics.CheckSphere(safe, .35f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        internal static bool Finite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x) && !float.IsNaN(value.y) && !float.IsInfinity(value.y) && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }

        private ActionResult Missing(FeatureCapability capability) { return ActionResult.Fail(capability + " unsupported: " + _capabilities.Reason(capability)); }
        private ActionResult Error(string action, Exception ex) { _log.LogWarning(action + " failed: " + ex); return ActionResult.Fail(action + " failed safely: " + ex.Message); }
    }
}
