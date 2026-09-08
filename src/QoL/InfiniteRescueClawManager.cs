using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using Photon.Pun;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class InfiniteRescueClawManager
    {
        private const float EffectiveInfiniteRange = 5000f;
        private const float ZipSpeed = 45f;
        private const float ZipVelocityLimit = 90f;
        private const float ExtendedWallHookTime = 120f;
        private static readonly FieldInfo IsPullingField = ReflectionHelpers.Field(typeof(RescueHook), "isPulling");
        private static readonly FieldInfo HitNothingField = ReflectionHelpers.Field(typeof(RescueHook), "hitNothing");
        private static readonly FieldInfo TargetPlayerField = ReflectionHelpers.Field(typeof(RescueHook), "targetPlayer");
        private static readonly FieldInfo TargetRigField = ReflectionHelpers.Field(typeof(RescueHook), "targetRig");
        private static readonly FieldInfo TargetPositionField = ReflectionHelpers.Field(typeof(RescueHook), "targetPos");
        private static readonly FieldInfo RagdollRigidbodiesField = ReflectionHelpers.Field(typeof(CharacterRagdoll), "rigidbodies");

        private sealed class BodyState
        {
            public bool UseGravity;
            public float MaxLinearVelocity;
        }

        private sealed class HookState
        {
            public float Range;
            public float RangeDownward;
            public float MaxWallHookTime;
            public bool Zipping;
            public float ZipDeadline;
            public Character Character;
            public readonly Dictionary<Rigidbody, BodyState> Bodies = new Dictionary<Rigidbody, BodyState>();
        }

        private readonly ModConfig _settings;
        private readonly ManualLogSource _log;
        private readonly Dictionary<RescueHook, HookState> _states = new Dictionary<RescueHook, HookState>();
        private bool _standaloneDetected;
        private float _nextStandaloneCheck;
        private float _nextErrorLog;

        public InfiniteRescueClawManager(ModConfig settings, ManualLogSource log) { _settings = settings; _log = log; }
        public bool Enabled { get { return _settings.InfiniteRescueClawReachEnabled.Value; } set { _settings.InfiniteRescueClawReachEnabled.Value = value; if (!value) RestoreAll(); } }
        public bool StandaloneDetected { get { RefreshStandaloneDetection(); return _standaloneDetected; } }
        public string Status { get { return StandaloneDetected ? "Yielding to standalone Rescue Hook Infinite Range" : Enabled ? "Map-wide wall grapple and distant-anchor zip enabled" : "Off"; } }

        public void Tick()
        {
            RefreshStandaloneDetection();
            if (!Enabled || _standaloneDetected) { RestoreAll(); return; }
            try
            {
                Character character = Character.localCharacter;
                Item held = character == null || character.data == null ? null : character.data.currentItem;
                RestoreNoLongerHeld(held);
                if (held == null) return;
                RescueHook[] hooks = held.GetComponentsInChildren<RescueHook>(true);
                for (int i = 0; i < hooks.Length; i++) ApplyRange(hooks[i]);
            }
            catch (Exception ex) { FailSafe("range update", ex); }
        }

        public void FixedTick()
        {
            if (!Enabled || _standaloneDetected) return;
            try
            {
                Character character = Character.localCharacter;
                Item held = character == null || character.data == null ? null : character.data.currentItem;
                if (character == null || character.data == null || character.data.dead || character.data.fullyPassedOut || held == null) { StopAllZips(true); return; }
                RescueHook[] hooks = held.GetComponentsInChildren<RescueHook>(true);
                for (int i = 0; i < hooks.Length; i++) UpdateZip(hooks[i], character);
            }
            catch (Exception ex) { FailSafe("grapple zip", ex); }
        }

        private void ApplyRange(RescueHook hook)
        {
            if (hook == null) return;
            HookState state;
            if (!_states.TryGetValue(hook, out state))
            {
                state = new HookState { Range = hook.range, RangeDownward = hook.rangeDownward, MaxWallHookTime = hook.maxWallHookTime };
                _states.Add(hook, state);
            }
            hook.range = EffectiveInfiniteRange;
            hook.rangeDownward = EffectiveInfiniteRange;
        }

        private void UpdateZip(RescueHook hook, Character character)
        {
            HookState state;
            if (hook == null || !_states.TryGetValue(hook, out state)) return;
            bool pulling = ReadBool(IsPullingField, hook);
            bool hitNothing = ReadBool(HitNothingField, hook);
            Character targetPlayer = ReadObject<Character>(TargetPlayerField, hook);
            Rigidbody targetRig = ReadObject<Rigidbody>(TargetRigField, hook);
            Vector3 target = TargetPositionField == null ? Vector3.zero : (Vector3)TargetPositionField.GetValue(hook);
            bool distantWall = pulling && !hitNothing && targetPlayer == null && targetRig == null && PlayerActions.Finite(target);
            float distance = distantWall ? Vector3.Distance(character.Center, target) : 0f;
            float nativeRange = target.y < character.Center.y ? state.RangeDownward : state.Range;

            if (!state.Zipping)
            {
                if (!distantWall || distance <= Mathf.Max(5f, nativeRange + 1f)) return;
                StartZip(hook, state, character, distance);
            }

            if (!distantWall || state.Character != character || Time.unscaledTime >= state.ZipDeadline)
            {
                StopZip(hook, state, true);
                return;
            }

            float stopDistance = Mathf.Max(2f, hook.stopPullDistance);
            if (distance <= stopDistance + .35f)
            {
                StopZip(hook, state, true);
                return;
            }

            Vector3 direction = (target - character.Center).normalized;
            float speed = Mathf.Min(ZipSpeed, Mathf.Max(8f, (distance - stopDistance) * 4f));
            Vector3 velocity = direction * speed;
            foreach (KeyValuePair<Rigidbody, BodyState> pair in state.Bodies)
            {
                Rigidbody body = pair.Key;
                if (body == null) continue;
                body.useGravity = false;
                body.maxLinearVelocity = Mathf.Max(pair.Value.MaxLinearVelocity, ZipVelocityLimit);
                body.linearVelocity = velocity;
            }
            character.data.sinceGrounded = 0f;
        }

        private void StartZip(RescueHook hook, HookState state, Character character, float distance)
        {
            if (character.refs == null || character.refs.ragdoll == null || RagdollRigidbodiesField == null) return;
            List<Rigidbody> bodies = RagdollRigidbodiesField.GetValue(character.refs.ragdoll) as List<Rigidbody>;
            if (bodies == null) return;
            state.Bodies.Clear();
            for (int i = 0; i < bodies.Count; i++)
            {
                Rigidbody body = bodies[i];
                if (body != null && !state.Bodies.ContainsKey(body)) state.Bodies.Add(body, new BodyState { UseGravity = body.useGravity, MaxLinearVelocity = body.maxLinearVelocity });
            }
            if (state.Bodies.Count == 0) return;
            state.Character = character;
            state.ZipDeadline = Time.unscaledTime + Mathf.Clamp(distance / ZipSpeed + 2f, 3f, ExtendedWallHookTime);
            state.Zipping = true;
            hook.maxWallHookTime = ExtendedWallHookTime;
        }

        private void StopZip(RescueHook hook, HookState state, bool releaseHook)
        {
            if (!state.Zipping) return;
            foreach (KeyValuePair<Rigidbody, BodyState> pair in state.Bodies)
            {
                Rigidbody body = pair.Key;
                if (body == null) continue;
                body.useGravity = pair.Value.UseGravity;
                body.maxLinearVelocity = pair.Value.MaxLinearVelocity;
                body.linearVelocity = Vector3.ClampMagnitude(body.linearVelocity, 8f);
            }
            state.Bodies.Clear(); state.Character = null; state.Zipping = false; state.ZipDeadline = 0f;
            if (hook != null)
            {
                hook.maxWallHookTime = state.MaxWallHookTime;
                if (releaseHook && hook.photonView != null && hook.photonView.IsMine) hook.photonView.RPC("RPCA_LetGo", RpcTarget.All, Array.Empty<object>());
            }
        }

        private void StopAllZips(bool releaseHook)
        {
            foreach (KeyValuePair<RescueHook, HookState> pair in _states) if (pair.Value.Zipping) StopZip(pair.Key, pair.Value, releaseHook);
        }

        private void RestoreNoLongerHeld(Item held)
        {
            List<RescueHook> remove = new List<RescueHook>();
            foreach (KeyValuePair<RescueHook, HookState> pair in _states)
            {
                RescueHook hook = pair.Key;
                if (hook == null) { RestoreBodies(pair.Value); remove.Add(hook); continue; }
                if (held == null || hook.GetComponentInParent<Item>() != held) { StopZip(hook, pair.Value, true); RestoreHook(hook, pair.Value); remove.Add(hook); }
            }
            for (int i = 0; i < remove.Count; i++) _states.Remove(remove[i]);
        }

        public void RestoreAll()
        {
            foreach (KeyValuePair<RescueHook, HookState> pair in _states)
            {
                if (pair.Key != null) { StopZip(pair.Key, pair.Value, true); RestoreHook(pair.Key, pair.Value); }
                else RestoreBodies(pair.Value);
            }
            _states.Clear();
        }

        private static void RestoreHook(RescueHook hook, HookState state) { hook.range = state.Range; hook.rangeDownward = state.RangeDownward; hook.maxWallHookTime = state.MaxWallHookTime; }
        private static void RestoreBodies(HookState state)
        {
            foreach (KeyValuePair<Rigidbody, BodyState> pair in state.Bodies) if (pair.Key != null) { pair.Key.useGravity = pair.Value.UseGravity; pair.Key.maxLinearVelocity = pair.Value.MaxLinearVelocity; pair.Key.linearVelocity = Vector3.ClampMagnitude(pair.Key.linearVelocity, 8f); }
            state.Bodies.Clear(); state.Character = null; state.Zipping = false; state.ZipDeadline = 0f;
        }

        public void ResetScene() { RestoreAll(); }

        private void FailSafe(string operation, Exception ex)
        {
            RestoreAll();
            if (Time.unscaledTime >= _nextErrorLog) { _nextErrorLog = Time.unscaledTime + 10f; _log.LogWarning("[PTM] Infinite Rescue Claw restored defaults after a " + operation + " error: " + ex.Message); }
        }

        private static bool ReadBool(FieldInfo field, RescueHook hook) { return field != null && (bool)field.GetValue(hook); }
        private static T ReadObject<T>(FieldInfo field, RescueHook hook) where T : class { return field == null ? null : field.GetValue(hook) as T; }

        private void RefreshStandaloneDetection()
        {
            if (Time.unscaledTime < _nextStandaloneCheck) return;
            _nextStandaloneCheck = Time.unscaledTime + 5f;
            _standaloneDetected = false;
            foreach (KeyValuePair<string, PluginInfo> pair in Chainloader.PluginInfos)
            {
                PluginInfo info = pair.Value;
                if (info == null || info.Instance == null || !info.Instance.enabled || pair.Key == TrollModPlugin.Guid) continue;
                string identity = Normalize(pair.Key + " " + (info.Metadata == null ? string.Empty : info.Metadata.Name));
                if (identity.Contains("rescuehookinfiniterange") || identity.Contains("infiniterescueclaw") || identity.Contains("infiniterescuehook")) { _standaloneDetected = true; break; }
            }
        }

        private static string Normalize(string value)
        {
            char[] buffer = new char[value.Length]; int length = 0;
            for (int i = 0; i < value.Length; i++) if (char.IsLetterOrDigit(value[i])) buffer[length++] = char.ToLowerInvariant(value[i]);
            return new string(buffer, 0, length);
        }
    }
}
