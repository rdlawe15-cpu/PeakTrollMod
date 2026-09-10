using System;
using System.Reflection;
using BepInEx.Logging;
using Photon.Pun;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class MindControlManager
    {
        private const int JumpPressed = 1 << 0;
        private const int JumpHeld = 1 << 1;
        private const int SprintPressed = 1 << 2;
        private const int SprintHeld = 1 << 3;
        private const int SprintTogglePressed = 1 << 4;
        private const int SprintToggleHeld = 1 << 5;
        private const int CrouchPressed = 1 << 6;
        private const int CrouchHeld = 1 << 7;
        private const int CrouchTogglePressed = 1 << 8;
        private const int InteractPressed = 1 << 9;
        private const int InteractHeld = 1 << 10;
        private const int InteractReleased = 1 << 11;
        private const int DropPressed = 1 << 12;
        private const int DropHeld = 1 << 13;
        private const int DropReleased = 1 << 14;
        private const int PrimaryPressed = 1 << 15;
        private const int PrimaryHeld = 1 << 16;
        private const int PrimaryReleased = 1 << 17;
        private const int SecondaryPressed = 1 << 18;
        private const int SecondaryHeld = 1 << 19;
        private const int SecondaryReleased = 1 << 20;
        private const int PingPressed = 1 << 21;
        private const int SelectForwardPressed = 1 << 22;
        private const int SelectBackwardPressed = 1 << 23;
        private const int UnselectPressed = 1 << 24;
        private const int BackpackPressed = 1 << 25;
        private const int ScrollForwardPressed = 1 << 26;
        private const int ScrollForwardHeld = 1 << 27;
        private const int ScrollBackwardPressed = 1 << 28;
        private const int ScrollBackwardHeld = 1 << 29;
        private const int EmoteHeld = 1 << 30;
        private const int AllowedMask = JumpPressed | JumpHeld | SprintPressed | SprintHeld | SprintTogglePressed | SprintToggleHeld | CrouchPressed | CrouchHeld | CrouchTogglePressed | InteractPressed | InteractHeld | InteractReleased | DropPressed | DropHeld | DropReleased | PrimaryPressed | PrimaryHeld | PrimaryReleased | SecondaryPressed | SecondaryHeld | SecondaryReleased | PingPressed | SelectForwardPressed | SelectBackwardPressed | UnselectPressed | BackpackPressed | ScrollForwardPressed | ScrollForwardHeld | ScrollBackwardPressed | ScrollBackwardHeld | EmoteHeld;
        private const int HeldMask = JumpHeld | SprintHeld | SprintToggleHeld | CrouchHeld | InteractHeld | DropHeld | PrimaryHeld | SecondaryHeld | ScrollForwardHeld | ScrollBackwardHeld | EmoteHeld;
        private const int PulseMask = AllowedMask & ~HeldMask;

        private readonly ManualLogSource _log;
        private readonly PlayerManager _players;
        private readonly TrollNetworkManager _network;
        private readonly ModConfig _settings;
        private readonly CapabilityRegistry _capabilities;
        private readonly MethodInfo _addForceAtPosition;

        private int _controlledActor;
        private int _controllerActor;
        private int _session;
        private int _sequence;
        private int _lastVictimSequence = -1;
        private bool _pending;
        private bool _hostPuppetMode;
        private bool _puppetCrouched;
        private float _pendingUntil;
        private float _sessionUntil;
        private float _lastPacketAt;
        private float _nextSend;
        private Vector2 _controllerMovement;
        private Vector2 _controllerLook;
        private float _controllerScroll;
        private int _controllerHeld;
        private int _controllerPulses;
        private Vector2 _victimMovement;
        private Vector2 _victimLook;
        private float _victimScroll;
        private int _victimHeld;
        private int _victimPulses;
        private Vector3 _cameraPosition;
        private bool _cameraInitialized;
        private Vector3 _puppetForward;
        private GUIStyle _overlayTitle;
        private GUIStyle _overlayText;
        private string _status = "Inactive";

        public MindControlManager(ManualLogSource log, PlayerManager players, TrollNetworkManager network, ModConfig settings, CapabilityRegistry capabilities)
        {
            _log = log; _players = players; _network = network; _settings = settings; _capabilities = capabilities;
            _addForceAtPosition = ReflectionHelpers.Method(typeof(Character), "AddForceAtPosition", new Type[] { typeof(Vector3), typeof(Vector3), typeof(float) });
        }

        public bool IsControlling { get { return _controlledActor > 0 && !_pending; } }
        public bool IsPending { get { return _controlledActor > 0 && _pending; } }
        public bool IsControlled { get { return _controllerActor > 0; } }
        public bool Active { get { return IsControlling || IsPending || IsControlled; } }
        public string Status { get { return _status; } }

        public ActionResult Start(PlayerEntry target)
        {
            if (!_capabilities.Available(FeatureCapability.MindControl)) return ActionResult.Fail(_capabilities.Reason(FeatureCapability.MindControl));
            if (target == null || target.Character == null || target.Character.data == null) return ActionResult.Fail("Select one available scout.");
            if (target.IsLocal) return ActionResult.Fail("Mind Control requires a different scout.");
            if (target.Character.data.dead || target.Character.data.fullyPassedOut) return ActionResult.Fail("The selected scout must be conscious.");
            if (Active) Stop();

            int session = UnityEngine.Random.Range(1, int.MaxValue);
            float duration = Mathf.Clamp(_settings.MindControlDuration.Value, 10f, 180f);
            if (!_network.IsCompatible(target.ActorNumber))
            {
                if (!_players.IsHost) return ActionResult.Fail(target.Name + " is unmodded. Only the host can use limited puppet control on unmodded scouts.");
                if (_addForceAtPosition == null || target.Character.refs == null || target.Character.refs.view == null) return ActionResult.Fail("PEAK's native puppet-control path is unavailable in this game build.");
                _controlledActor = target.ActorNumber;
                _session = session;
                _hostPuppetMode = true;
                _sessionUntil = Time.unscaledTime + duration;
                _nextSend = 0f;
                _cameraInitialized = false;
                _puppetForward = FlatForward(target.Character);
                _status = "Host puppet control: " + target.Name + ". Their own controls remain active; F6 releases.";
                return ActionResult.Ok("Host puppet control started for " + target.Name + ". Movement, sprint, jump, and crouch use PEAK's native RPCs; their own controls cannot be suppressed without the mod.");
            }
            ActionResult sent = _network.SendMindControlCommand(NetCommand.MindControlStart, target.ActorNumber, new object[] { session, duration }, true);
            if (!sent.Success) return sent;
            _controlledActor = target.ActorNumber;
            _session = session;
            _pending = true;
            _pendingUntil = Time.unscaledTime + 4f;
            _sessionUntil = Time.unscaledTime + duration;
            _status = "Waiting for " + target.Name + " to accept the control session.";
            return ActionResult.Ok("Mind Control requested for " + target.Name + ". It will use game inputs only; F6 releases either side.");
        }

        public ActionResult Stop()
        {
            if (!Active) return ActionResult.Ok("Mind Control is already inactive.");
            if (_hostPuppetMode && _puppetCrouched)
            {
                PlayerEntry puppet = _players.Find(_controlledActor);
                try { if (puppet != null && puppet.Character != null && puppet.Character.refs != null && puppet.Character.refs.view != null) puppet.Character.refs.view.RPC("RPCA_SetCrouch", RpcTarget.All, new object[] { false }); }
                catch (Exception ex) { _log.LogWarning("Host puppet crouch cleanup failed safely: " + ex.Message); }
            }
            if (_controlledActor > 0 && !_hostPuppetMode) _network.SendMindControlCommand(NetCommand.MindControlStop, _controlledActor, new object[] { _session }, true);
            if (_controllerActor > 0) _network.SendMindControlCommand(NetCommand.MindControlStop, _controllerActor, new object[] { _session }, true);
            string message = IsControlled ? "Mind Control broken locally." : "Mind Control released.";
            Clear();
            _status = message;
            return ActionResult.Ok(message);
        }

        public ActionResult ReceiveStart(int senderActor, int session, float duration)
        {
            PlayerEntry local = _players.Local;
            PlayerEntry sender = _players.Find(senderActor);
            bool accepted = local != null && local.IsLocal && sender != null && !sender.IsLocal && session > 0 && !IsControlled && !IsControlling && !IsPending && local.Character != null && local.Character.data != null && !local.Character.data.dead && !local.Character.data.fullyPassedOut;
            _network.SendMindControlCommand(NetCommand.MindControlAck, senderActor, new object[] { session, accepted }, true);
            if (!accepted) return ActionResult.Fail("Mind Control request rejected because this client is unavailable or already in a session.");

            _controllerActor = senderActor;
            _session = session;
            _sessionUntil = Time.unscaledTime + Mathf.Clamp(duration, 10f, 180f);
            _lastPacketAt = Time.unscaledTime;
            _lastVictimSequence = -1;
            _status = "Controlled by " + sender.Name + ". Press F6 to break free.";
            return ActionResult.Ok(_status);
        }

        public ActionResult ReceiveAck(int senderActor, int session, bool accepted)
        {
            if (!IsPending || senderActor != _controlledActor || session != _session) return ActionResult.Fail("Ignored an unrelated Mind Control acknowledgement.");
            PlayerEntry target = _players.Find(senderActor);
            if (!accepted)
            {
                string name = target == null ? "The target" : target.Name;
                Clear();
                _status = name + " could not enter Mind Control.";
                return ActionResult.Fail(_status);
            }
            _pending = false;
            _nextSend = 0f;
            _sequence = 0;
            _cameraInitialized = false;
            _status = "Controlling " + (target == null ? "selected scout" : target.Name) + ". Press F6 to release.";
            return ActionResult.Ok(_status);
        }

        public ActionResult ReceiveInput(int senderActor, int session, int sequence, float moveX, float moveY, float lookX, float lookY, float scroll, int mask)
        {
            if (!IsControlled || senderActor != _controllerActor || session != _session) return ActionResult.Fail("Ignored input outside the active Mind Control session.");
            if (sequence <= _lastVictimSequence) return ActionResult.Fail("Ignored stale Mind Control input.");
            _lastVictimSequence = sequence;
            _lastPacketAt = Time.unscaledTime;
            _victimMovement = Vector2.ClampMagnitude(new Vector2(Clean(moveX, -1f, 1f), Clean(moveY, -1f, 1f)), 1f);
            _victimLook += new Vector2(Clean(lookX, -50f, 50f), Clean(lookY, -50f, 50f));
            _victimLook = new Vector2(Mathf.Clamp(_victimLook.x, -100f, 100f), Mathf.Clamp(_victimLook.y, -100f, 100f));
            _victimScroll = Mathf.Clamp(_victimScroll + Clean(scroll, -10f, 10f), -20f, 20f);
            mask &= AllowedMask;
            _victimHeld = mask & HeldMask;
            _victimPulses |= mask & PulseMask;
            return ActionResult.Ok("Mind Control input accepted.");
        }

        public ActionResult ReceiveStop(int senderActor, int session)
        {
            if (session != _session) return ActionResult.Fail("Ignored a stale Mind Control stop.");
            if (IsControlled && senderActor == _controllerActor) { Clear(); _status = "Mind Control released by the controller."; return ActionResult.Ok(_status); }
            if ((IsControlling || IsPending) && senderActor == _controlledActor) { Clear(); _status = "The selected scout broke free from Mind Control."; return ActionResult.Ok(_status); }
            return ActionResult.Fail("Ignored a Mind Control stop from an unrelated player.");
        }

        public void Tick()
        {
            if (!Active) return;
            if (_settings.MindControlEscapeKey.Value.IsDown()) { Stop(); return; }
            if (!Photon.Pun.PhotonNetwork.InRoom) { Clear(); _status = "Mind Control ended because the lobby closed."; return; }
            if (IsPending && Time.unscaledTime >= _pendingUntil) { Clear(); _status = "Mind Control request timed out."; return; }
            if (Time.unscaledTime >= _sessionUntil) { Stop(); _status = "Mind Control duration ended."; return; }

            if (IsControlling)
            {
                PlayerEntry target = _players.Find(_controlledActor);
                if (target == null || target.Character == null || target.Character.data == null || target.Character.data.dead || target.Character.data.fullyPassedOut) { Stop(); _status = "Mind Control ended because the target became unavailable."; return; }
                if (_hostPuppetMode)
                {
                    if (!_players.IsHost) { Stop(); _status = "Host puppet control ended because host authority was lost."; return; }
                    if (Time.unscaledTime >= _nextSend) ApplyHostPuppetInput(target);
                }
                else if (Time.unscaledTime >= _nextSend) SendControllerInput();
            }
            else if (IsControlled && (Time.unscaledTime - _lastPacketAt > 2f || _players.Find(_controllerActor) == null))
            {
                int controller = _controllerActor;
                int session = _session;
                Clear();
                if (controller > 0) _network.SendMindControlCommand(NetCommand.MindControlStop, controller, new object[] { session }, true);
                _status = "Mind Control ended because its input stream stopped.";
            }
        }

        public void OnInputSampled(CharacterInput input, bool canDoInput)
        {
            Character local = Character.localCharacter;
            if (input == null || local == null || local.input != input) return;
            if (IsControlling)
            {
                Capture(input);
                ClearGameplayInput(input);
            }
            else if (IsControlled)
            {
                if (canDoInput) Apply(input);
                else ClearGameplayInput(input);
            }
        }

        public void OverrideCamera(MainCameraMovement camera)
        {
            if (!IsControlling || camera == null) return;
            PlayerEntry target = _players.Find(_controlledActor);
            if (target == null || target.Character == null || target.Character.data == null) return;
            Vector3 forward = _hostPuppetMode ? _puppetForward : FlatForward(target.Character);

            Vector3 focus = target.Character.Center + Vector3.up * 1.1f;
            float distance = Mathf.Clamp(_settings.MindControlCameraDistance.Value, 3f, 10f);
            Vector3 desired = focus - forward * distance + Vector3.up * 1.4f;
            Vector3 ray = desired - focus;
            RaycastHit hit;
            if (ray.sqrMagnitude > .01f && Physics.Raycast(focus + Vector3.up * .35f, ray.normalized, out hit, ray.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) desired = hit.point + hit.normal * .2f;
            if (!_cameraInitialized) { _cameraPosition = desired; _cameraInitialized = true; }
            else _cameraPosition = Vector3.Lerp(_cameraPosition, desired, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
            camera.transform.position = _cameraPosition;
            Vector3 look = focus - _cameraPosition;
            if (look.sqrMagnitude > .01f) camera.transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
        }

        public void Draw(bool menuOpen)
        {
            if (menuOpen || (!IsControlling && !IsControlled) || Event.current == null || Event.current.type != EventType.Repaint) return;
            EnsureStyles();
            PlayerEntry other = _players.Find(IsControlled ? _controllerActor : _controlledActor);
            string name = other == null ? "Unknown scout" : other.Name;
            string title = IsControlled ? "MIND CONTROLLED BY " + name.ToUpperInvariant() : (_hostPuppetMode ? "HOST PUPPET — " : "MIND CONTROL — ") + name.ToUpperInvariant();
            string detail = IsControlled ? "F6 break free  •  Pause and voice remain yours" : (_hostPuppetMode ? "WASD push  •  Shift sprint  •  Space jump  •  C crouch  •  F6 release" : "WASD move  •  Mouse look  •  normal action keys  •  F6 release");
            Rect panel = new Rect((Screen.width - 560f) * .5f, 28f, 560f, 62f);
            Color oldColor = GUI.color; int oldDepth = GUI.depth;
            try
            {
                GUI.depth = -9500;
                GUI.color = new Color(.025f, .035f, .04f, .94f); GUI.Box(panel, GUIContent.none);
                GUI.color = Color.white;
                GUI.Label(new Rect(panel.x + 12f, panel.y + 5f, panel.width - 24f, 25f), title, _overlayTitle);
                GUI.Label(new Rect(panel.x + 12f, panel.y + 31f, panel.width - 24f, 22f), detail, _overlayText);
            }
            finally { GUI.color = oldColor; GUI.depth = oldDepth; }
        }

        private void Capture(CharacterInput input)
        {
            _controllerMovement = Vector2.ClampMagnitude(input.movementInput, 1f);
            _controllerLook += input.lookInput;
            _controllerLook = new Vector2(Mathf.Clamp(_controllerLook.x, -100f, 100f), Mathf.Clamp(_controllerLook.y, -100f, 100f));
            _controllerScroll = Mathf.Clamp(_controllerScroll + input.scrollInput, -20f, 20f);
            int mask = ReadMask(input);
            _controllerHeld = mask & HeldMask;
            _controllerPulses |= mask & PulseMask;
        }

        private void SendControllerInput()
        {
            _nextSend = Time.unscaledTime + .05f;
            int mask = _controllerHeld | _controllerPulses;
            object[] args = { _session, ++_sequence, _controllerMovement.x, _controllerMovement.y, _controllerLook.x, _controllerLook.y, _controllerScroll, mask };
            ActionResult sent = _network.SendMindControlCommand(NetCommand.MindControlInput, _controlledActor, args, false);
            if (sent.Success) { _controllerPulses = 0; _controllerLook = Vector2.zero; _controllerScroll = 0f; }
        }

        private void ApplyHostPuppetInput(PlayerEntry target)
        {
            _nextSend = Time.unscaledTime + .1f;
            try
            {
                float yaw = Mathf.Clamp(_controllerLook.x, -50f, 50f) * .16f;
                if (Mathf.Abs(yaw) > .001f) _puppetForward = Quaternion.AngleAxis(yaw, Vector3.up) * _puppetForward;
                _puppetForward.y = 0f;
                if (!Finite(_puppetForward) || _puppetForward.sqrMagnitude < .01f) _puppetForward = FlatForward(target.Character);
                _puppetForward.Normalize();
                Vector3 right = Vector3.Cross(Vector3.up, _puppetForward).normalized;
                Vector3 direction = _puppetForward * _controllerMovement.y + right * _controllerMovement.x;
                float magnitude = Mathf.Clamp01(direction.magnitude);
                if (magnitude > .02f)
                {
                    direction.Normalize();
                    int liveMask = _controllerHeld | _controllerPulses;
                    bool sprint = Has(liveMask, SprintHeld) || Has(liveMask, SprintToggleHeld);
                    float force = Mathf.Clamp(_settings.MindControlHostPuppetForce.Value, 1f, 8f) * (sprint ? 1.65f : 1f) * magnitude;
                    ReflectionHelpers.Invoke(target.Character, _addForceAtPosition, direction * force, target.Character.Center, 3f);
                }
                int mask = _controllerHeld | _controllerPulses;
                if (Has(mask, JumpPressed)) target.Character.refs.view.RPC("JumpRpc", RpcTarget.All, new object[] { false });
                if (Has(mask, CrouchPressed) || Has(mask, CrouchTogglePressed))
                {
                    _puppetCrouched = !_puppetCrouched;
                    target.Character.refs.view.RPC("RPCA_SetCrouch", RpcTarget.All, new object[] { _puppetCrouched });
                }
                _controllerPulses = 0;
                _controllerLook = Vector2.zero;
                _controllerScroll = 0f;
            }
            catch (Exception ex)
            {
                _log.LogWarning("Host puppet input failed safely: " + ex.Message);
                Stop();
                _status = "Host puppet control stopped because PEAK rejected a native action.";
            }
        }

        private void Apply(CharacterInput input)
        {
            int mask = _victimHeld | _victimPulses;
            input.movementInput = _victimMovement;
            input.lookInput = _victimLook;
            input.scrollInput = _victimScroll;
            input.jumpWasPressed = Has(mask, JumpPressed); input.jumpIsPressed = Has(mask, JumpHeld);
            input.sprintWasPressed = Has(mask, SprintPressed); input.sprintIsPressed = Has(mask, SprintHeld); input.sprintToggleWasPressed = Has(mask, SprintTogglePressed); input.sprintToggleIsPressed = Has(mask, SprintToggleHeld);
            input.crouchWasPressed = Has(mask, CrouchPressed); input.crouchIsPressed = Has(mask, CrouchHeld); input.crouchToggleWasPressed = Has(mask, CrouchTogglePressed);
            input.interactWasPressed = Has(mask, InteractPressed); input.interactIsPressed = Has(mask, InteractHeld); input.interactWasReleased = Has(mask, InteractReleased);
            input.dropWasPressed = Has(mask, DropPressed); input.dropIsPressed = Has(mask, DropHeld); input.dropWasReleased = Has(mask, DropReleased);
            input.usePrimaryWasPressed = Has(mask, PrimaryPressed); input.usePrimaryIsPressed = Has(mask, PrimaryHeld); input.usePrimaryWasReleased = Has(mask, PrimaryReleased);
            input.useSecondaryWasPressed = Has(mask, SecondaryPressed); input.useSecondaryIsPressed = Has(mask, SecondaryHeld); input.useSecondaryWasReleased = Has(mask, SecondaryReleased);
            input.pingWasPressed = Has(mask, PingPressed);
            if (!input.itemSwitchBlocked) { input.selectSlotForwardWasPressed = Has(mask, SelectForwardPressed); input.selectSlotBackwardWasPressed = Has(mask, SelectBackwardPressed); input.unselectSlotWasPressed = Has(mask, UnselectPressed); input.selectBackpackWasPressed = Has(mask, BackpackPressed); }
            else { input.selectSlotForwardWasPressed = false; input.selectSlotBackwardWasPressed = false; input.unselectSlotWasPressed = false; input.selectBackpackWasPressed = false; }
            input.scrollForwardWasPressed = Has(mask, ScrollForwardPressed); input.scrollForwardIsPressed = Has(mask, ScrollForwardHeld); input.scrollBackwardWasPressed = Has(mask, ScrollBackwardPressed); input.scrollBackwardIsPressed = Has(mask, ScrollBackwardHeld); input.emoteIsPressed = Has(mask, EmoteHeld);
            _victimPulses = 0; _victimLook = Vector2.zero; _victimScroll = 0f;
        }

        private static int ReadMask(CharacterInput input)
        {
            int mask = 0;
            Add(ref mask, input.jumpWasPressed, JumpPressed); Add(ref mask, input.jumpIsPressed, JumpHeld);
            Add(ref mask, input.sprintWasPressed, SprintPressed); Add(ref mask, input.sprintIsPressed, SprintHeld); Add(ref mask, input.sprintToggleWasPressed, SprintTogglePressed); Add(ref mask, input.sprintToggleIsPressed, SprintToggleHeld);
            Add(ref mask, input.crouchWasPressed, CrouchPressed); Add(ref mask, input.crouchIsPressed, CrouchHeld); Add(ref mask, input.crouchToggleWasPressed, CrouchTogglePressed);
            Add(ref mask, input.interactWasPressed, InteractPressed); Add(ref mask, input.interactIsPressed, InteractHeld); Add(ref mask, input.interactWasReleased, InteractReleased);
            Add(ref mask, input.dropWasPressed, DropPressed); Add(ref mask, input.dropIsPressed, DropHeld); Add(ref mask, input.dropWasReleased, DropReleased);
            Add(ref mask, input.usePrimaryWasPressed, PrimaryPressed); Add(ref mask, input.usePrimaryIsPressed, PrimaryHeld); Add(ref mask, input.usePrimaryWasReleased, PrimaryReleased);
            Add(ref mask, input.useSecondaryWasPressed, SecondaryPressed); Add(ref mask, input.useSecondaryIsPressed, SecondaryHeld); Add(ref mask, input.useSecondaryWasReleased, SecondaryReleased);
            Add(ref mask, input.pingWasPressed, PingPressed); Add(ref mask, input.selectSlotForwardWasPressed, SelectForwardPressed); Add(ref mask, input.selectSlotBackwardWasPressed, SelectBackwardPressed); Add(ref mask, input.unselectSlotWasPressed, UnselectPressed); Add(ref mask, input.selectBackpackWasPressed, BackpackPressed);
            Add(ref mask, input.scrollForwardWasPressed, ScrollForwardPressed); Add(ref mask, input.scrollForwardIsPressed, ScrollForwardHeld); Add(ref mask, input.scrollBackwardWasPressed, ScrollBackwardPressed); Add(ref mask, input.scrollBackwardIsPressed, ScrollBackwardHeld); Add(ref mask, input.emoteIsPressed, EmoteHeld);
            return mask;
        }

        private static void ClearGameplayInput(CharacterInput input)
        {
            input.movementInput = Vector2.zero; input.lookInput = Vector2.zero; input.scrollInput = 0f;
            input.jumpWasPressed = false; input.jumpIsPressed = false; input.sprintWasPressed = false; input.sprintIsPressed = false; input.sprintToggleWasPressed = false; input.sprintToggleIsPressed = false;
            input.crouchWasPressed = false; input.crouchIsPressed = false; input.crouchToggleWasPressed = false;
            input.interactWasPressed = false; input.interactIsPressed = false; input.interactWasReleased = false; input.dropWasPressed = false; input.dropIsPressed = false; input.dropWasReleased = false;
            input.usePrimaryWasPressed = false; input.usePrimaryIsPressed = false; input.usePrimaryWasReleased = false; input.useSecondaryWasPressed = false; input.useSecondaryIsPressed = false; input.useSecondaryWasReleased = false;
            input.pingWasPressed = false; input.selectSlotForwardWasPressed = false; input.selectSlotBackwardWasPressed = false; input.unselectSlotWasPressed = false; input.selectBackpackWasPressed = false;
            input.scrollForwardWasPressed = false; input.scrollForwardIsPressed = false; input.scrollBackwardWasPressed = false; input.scrollBackwardIsPressed = false; input.emoteIsPressed = false;
        }

        private void Clear()
        {
            _controlledActor = 0; _controllerActor = 0; _session = 0; _sequence = 0; _lastVictimSequence = -1; _pending = false; _hostPuppetMode = false; _puppetCrouched = false; _pendingUntil = 0f; _sessionUntil = 0f; _lastPacketAt = 0f; _nextSend = 0f;
            _controllerMovement = Vector2.zero; _controllerLook = Vector2.zero; _controllerScroll = 0f; _controllerHeld = 0; _controllerPulses = 0;
            _victimMovement = Vector2.zero; _victimLook = Vector2.zero; _victimScroll = 0f; _victimHeld = 0; _victimPulses = 0; _cameraInitialized = false; _puppetForward = Vector3.forward;
        }

        public void ResetTarget(int actor)
        {
            if (_controlledActor == actor || _controllerActor == actor || (_players.Local != null && _players.Local.ActorNumber == actor && Active)) { Clear(); _status = "Mind Control cleared by player reset."; }
        }

        public void Reset() { Clear(); _status = "Inactive"; }

        private void EnsureStyles()
        {
            if (_overlayTitle != null) return;
            _overlayTitle = new GUIStyle(GUI.skin.label); _overlayTitle.font = UiFontManager.DisplayFont; _overlayTitle.fontSize = 15; _overlayTitle.fontStyle = FontStyle.Bold; _overlayTitle.alignment = TextAnchor.MiddleCenter; _overlayTitle.normal.textColor = new Color(.4f, 1f, .9f);
            _overlayText = new GUIStyle(GUI.skin.label); _overlayText.font = UiFontManager.BodyFont; _overlayText.fontSize = 12; _overlayText.alignment = TextAnchor.MiddleCenter; _overlayText.normal.textColor = new Color(.82f, .88f, .87f);
        }

        private static bool Has(int mask, int flag) { return (mask & flag) != 0; }
        private static void Add(ref int mask, bool value, int flag) { if (value) mask |= flag; }
        private static Vector3 FlatForward(Character character)
        {
            Vector3 forward = character != null && character.data != null ? character.data.lookDirection_Flat : Vector3.forward;
            forward.y = 0f;
            if (!Finite(forward) || forward.sqrMagnitude < .01f) forward = character == null ? Vector3.forward : character.transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude < .01f ? Vector3.forward : forward.normalized;
        }
        private static bool Finite(Vector3 value) { return !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z) && !float.IsInfinity(value.x) && !float.IsInfinity(value.y) && !float.IsInfinity(value.z); }
        private static float Clean(float value, float min, float max) { return float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Clamp(value, min, max); }
    }
}
