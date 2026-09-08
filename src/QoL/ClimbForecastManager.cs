using System;
using BepInEx.Logging;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class ClimbForecastManager
    {
        private readonly ModConfig _settings;
        private readonly ManualLogSource _log;
        private float _nextScan;
        private float _nextErrorLog;
        private bool _visible;
        private float _height;
        private float _seconds;
        private float _cost;
        private float _remaining;
        private string _rating = "NO TARGET";
        private string _detail = string.Empty;
        private Texture2D _pixel;
        private GUIStyle _title;
        private GUIStyle _text;

        public ClimbForecastManager(ModConfig settings, ManualLogSource log) { _settings = settings; _log = log; }
        public bool Enabled { get { return _settings.ClimbForecastEnabled.Value; } set { _settings.ClimbForecastEnabled.Value = value; } }
        public float MaximumHeight { get { return Mathf.Clamp(_settings.ClimbForecastMaximumHeight.Value, 3f, 20f); } set { _settings.ClimbForecastMaximumHeight.Value = Mathf.Clamp(value, 3f, 20f); } }
        public float SafetyReserve { get { return Mathf.Clamp(_settings.ClimbForecastSafetyReserve.Value, 0f, .5f); } set { _settings.ClimbForecastSafetyReserve.Value = Mathf.Clamp(value, 0f, .5f); } }
        public string Status { get { return !Enabled ? "Off" : _visible ? _rating + " — " + _detail : "Reach toward a wall to scan its next ledge"; } }

        public void Tick()
        {
            if (Time.unscaledTime < _nextScan) return;
            _nextScan = Time.unscaledTime + .12f;
            _visible = false;
            if (!Enabled || !GameHandler.IsOnIsland) return;
            try { Scan(); }
            catch (Exception ex)
            {
                if (Time.unscaledTime >= _nextErrorLog) { _nextErrorLog = Time.unscaledTime + 10f; _log.LogWarning("[PTM] Climb forecast skipped a scan: " + ex.Message); }
            }
        }

        private void Scan()
        {
            Character character = Character.localCharacter;
            if (character == null || character.data == null || character.refs == null || character.refs.climbing == null || character.data.dead || character.data.fullyPassedOut) return;
            if (!character.data.isReaching && !character.data.isClimbing) return;
            Vector3 direction = character.data.lookDirection;
            if (direction.sqrMagnitude < .1f) return;
            RaycastHit wall;
            if (!Physics.Raycast(character.Head, direction.normalized, out wall, 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;
            if (wall.collider == null || wall.collider.GetComponentInParent<Character>() != null || wall.collider.GetComponentInParent<Item>() != null) return;
            float wallAngle = Vector3.Angle(Vector3.up, wall.normal);
            if (wallAngle < 48f) return;

            Vector3 normal = wall.normal.normalized;
            float edgeHeight = FindWallEdge(wall.point, normal, wall.collider);
            if (edgeHeight < 0f) { SetUnavailable("NO LEDGE", "No edge found within " + MaximumHeight.ToString("0") + "m"); return; }

            RaycastHit ledge;
            Vector3 ledgeProbe = wall.point + Vector3.up * (edgeHeight + 1.4f) - normal * .55f;
            if (!Physics.Raycast(ledgeProbe, Vector3.down, out ledge, 2.6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) || Vector3.Dot(ledge.normal, Vector3.up) < .58f)
            { SetUnavailable("NO FOOTING", "Edge found, but no standable top was detected"); return; }

            _height = ledge.point.y - character.Center.y;
            if (_height <= .25f) return;
            bool clear = !Physics.CheckSphere(ledge.point + Vector3.up * .85f, .32f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            CharacterClimbing climbing = character.refs.climbing;
            ClimbModifierSurface modifier = wall.collider.GetComponentInParent<ClimbModifierSurface>();
            if (modifier != null && modifier.onlySlideDown) { SetUnavailable("NO CLIMB", "This surface only permits downward sliding"); return; }
            float staminaMod = Mathf.Max(.05f, character.data.staminaMod);
            float speedMultiplier = modifier == null ? 1f : Mathf.Max(.05f, modifier.speedMultiplier);
            float usageMultiplier = modifier == null ? 1f : Mathf.Max(0f, modifier.staminaUsageMultiplier);
            float verticalSpeed = Mathf.Max(.05f, climbing.climbSpeed * climbing.climbSpeedMod * staminaMod * speedMultiplier);
            _seconds = Mathf.Clamp(_height / verticalSpeed, 0f, 120f);
            float angleUsage = character.data.staticClimbCost || (modifier != null && modifier.staticClimbCost) ? 1f : Mathf.Lerp(.2f, 1f, Mathf.InverseLerp(40f, 60f, wallAngle));
            float usePerSecond = Mathf.Max(0f, climbing.maxStaminaUsage * angleUsage * staminaMod * usageMultiplier * Ascents.climbStaminaMultiplier);
            float attachCost = !character.data.isClimbing && character.data.hasClimbedSinceGrounded ? Mathf.Min(.3f, Vector3.Distance(character.Center, wall.point) * .15f) : 0f;
            _cost = Mathf.Clamp(_seconds * usePerSecond + attachCost, 0f, 5f);
            if (character.infiniteStam || _settings.InfiniteStaminaEnabled.Value) _cost = 0f;
            float available = Mathf.Max(0f, character.GetTotalStamina());
            _remaining = available - _cost;
            float reserve = Mathf.Max(0f, character.GetMaxStamina()) * SafetyReserve;
            if (!clear) _rating = "BLOCKED";
            else if (_remaining < 0f) _rating = "NOT ENOUGH";
            else if (_remaining < reserve) _rating = "TIGHT";
            else _rating = "SAFE";
            _detail = _height.ToString("0.0") + "m • " + _seconds.ToString("0.0") + "s • cost " + Mathf.RoundToInt(_cost * 100f) + "% • left " + Mathf.RoundToInt(Mathf.Max(0f, _remaining) * 100f) + "%";
            _visible = true;
        }

        private float FindWallEdge(Vector3 point, Vector3 normal, Collider original)
        {
            float max = MaximumHeight;
            for (float height = .25f; height <= max; height += .25f)
            {
                RaycastHit sample;
                Vector3 origin = point + Vector3.up * height + normal * .4f;
                bool found = Physics.Raycast(origin, -normal, out sample, 1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                if (!found || sample.collider == null || (sample.collider != original && Vector3.Angle(sample.normal, normal) > 30f)) return height;
            }
            return -1f;
        }

        private void SetUnavailable(string rating, string detail) { _rating = rating; _detail = detail; _height = _seconds = _cost = _remaining = 0f; _visible = true; }

        public void Draw(bool menuOpen)
        {
            if (menuOpen || !_visible || Event.current == null || Event.current.type != EventType.Repaint) return;
            EnsureAssets();
            float scale = Mathf.Clamp(Screen.width / 1920f, .75f, 1.25f);
            Rect panel = new Rect((Screen.width - 460f * scale) * .5f, Screen.height - 272f * scale, 460f * scale, 58f * scale);
            Color accent = _rating == "SAFE" ? new Color(.25f, .9f, .7f) : _rating == "TIGHT" ? new Color(1f, .7f, .2f) : new Color(1f, .34f, .25f);
            DrawRect(panel, new Color(.025f, .035f, .04f, .92f)); DrawOutline(panel, accent);
            _title.normal.textColor = accent;
            GUI.Label(new Rect(panel.x + 10f * scale, panel.y + 5f * scale, panel.width - 20f * scale, 21f * scale), "CLIMB FORECAST  •  " + _rating, _title);
            GUI.Label(new Rect(panel.x + 10f * scale, panel.y + 30f * scale, panel.width - 20f * scale, 20f * scale), _detail, _text);
        }

        public void ResetScene() { _visible = false; if (_pixel != null) UnityEngine.Object.Destroy(_pixel); _pixel = null; _title = null; _text = null; }
        private void EnsureAssets() { if (_pixel != null) return; _pixel = new Texture2D(1, 1); _pixel.SetPixel(0, 0, Color.white); _pixel.Apply(); _title = new GUIStyle(GUI.skin.label); _title.font = UiFontManager.DisplayFont; _title.fontSize = 12; _title.fontStyle = FontStyle.Bold; _text = new GUIStyle(GUI.skin.label); _text.font = UiFontManager.BodyFont; _text.fontSize = 11; _text.normal.textColor = new Color(.84f, .9f, .89f); }
        private void DrawRect(Rect rect, Color color) { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, _pixel); GUI.color = old; }
        private void DrawOutline(Rect rect, Color color) { DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color); DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color); DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color); DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color); }
    }
}
