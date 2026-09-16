using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace PeakTrollMod
{
    internal enum EspCategory { Scout, Luggage, Zombie, Scoutmaster }

    internal sealed class ScoutEspManager
    {
        private sealed class EspTarget
        {
            public Component Component;
            public Renderer[] Renderers;
            public EspCategory Category;
            public string Label;
        }

        private readonly PlayerManager _players;
        private readonly ModConfig _settings;
        private readonly ManualLogSource _log;
        private readonly List<EspTarget> _targets = new List<EspTarget>();
        private float _nextScan;
        private float _nextErrorLog;
        private PlayerEntry _nearestScout;
        private float _nearestScoutDistance;
        private Texture2D _pixel;
        private GUIStyle _labelStyle;
        private GUIStyle _compassTitle;
        private GUIStyle _compassDetail;

        public ScoutEspManager(PlayerManager players, ModConfig settings, ManualLogSource log)
        {
            _players = players; _settings = settings; _log = log;
        }

        public bool CompassEnabled { get { return _settings.ScoutCompassEnabled.Value; } set { _settings.ScoutCompassEnabled.Value = value; } }
        public bool ScoutsEnabled { get { return _settings.ScoutEspEnabled.Value; } set { _settings.ScoutEspEnabled.Value = value; Invalidate(); } }
        public bool LuggageEnabled { get { return _settings.LuggageEspEnabled.Value; } set { _settings.LuggageEspEnabled.Value = value; Invalidate(); } }
        public bool ZombiesEnabled { get { return _settings.ZombieEspEnabled.Value; } set { _settings.ZombieEspEnabled.Value = value; Invalidate(); } }
        public bool ScoutmastersEnabled { get { return _settings.ScoutmasterEspEnabled.Value; } set { _settings.ScoutmasterEspEnabled.Value = value; Invalidate(); } }
        public bool LabelsEnabled { get { return _settings.EspLabelsEnabled.Value; } set { _settings.EspLabelsEnabled.Value = value; } }
        public bool TracersEnabled { get { return _settings.EspTracersEnabled.Value; } set { _settings.EspTracersEnabled.Value = value; } }
        public float MaximumDistance { get { return Mathf.Clamp(_settings.EspMaximumDistance.Value, 25f, 2000f); } set { _settings.EspMaximumDistance.Value = Mathf.Clamp(value, 25f, 2000f); } }
        public float OutlineWidth { get { return Mathf.Clamp(_settings.EspOutlineWidth.Value, 1f, 8f); } set { _settings.EspOutlineWidth.Value = Mathf.Clamp(value, 1f, 8f); } }
        public int TargetCount { get { return _targets.Count; } }
        public string CompassStatus { get { return _nearestScout == null ? "No other living scout detected" : Mathf.RoundToInt(_nearestScoutDistance) + " m to " + _nearestScout.Name; } }

        public Color GetColor(EspCategory category)
        {
            string value = category == EspCategory.Scout ? _settings.ScoutEspColor.Value : category == EspCategory.Luggage ? _settings.LuggageEspColor.Value : category == EspCategory.Zombie ? _settings.ZombieEspColor.Value : _settings.ScoutmasterEspColor.Value;
            Color color;
            if (!ColorUtility.TryParseHtmlString(value, out color)) color = DefaultColor(category);
            color.a = .96f;
            return color;
        }

        public void SetColor(EspCategory category, Color color)
        {
            string value = "#" + ColorUtility.ToHtmlStringRGB(new Color(Mathf.Clamp01(color.r), Mathf.Clamp01(color.g), Mathf.Clamp01(color.b), 1f));
            if (category == EspCategory.Scout) { if (_settings.ScoutEspColor.Value != value) _settings.ScoutEspColor.Value = value; }
            else if (category == EspCategory.Luggage) { if (_settings.LuggageEspColor.Value != value) _settings.LuggageEspColor.Value = value; }
            else if (category == EspCategory.Zombie) { if (_settings.ZombieEspColor.Value != value) _settings.ZombieEspColor.Value = value; }
            else if (_settings.ScoutmasterEspColor.Value != value) _settings.ScoutmasterEspColor.Value = value;
        }

        public int Count(EspCategory category)
        {
            int count = 0;
            for (int i = 0; i < _targets.Count; i++) if (_targets[i].Category == category && _targets[i].Component != null) count++;
            return count;
        }

        public void Tick()
        {
            try
            {
                RefreshNearestScout();
                if (!AnyEspEnabled()) { _targets.Clear(); return; }
                if (Time.unscaledTime >= _nextScan) { _nextScan = Time.unscaledTime + .75f; ScanTargets(); }
            }
            catch (Exception ex)
            {
                if (Time.unscaledTime >= _nextErrorLog) { _nextErrorLog = Time.unscaledTime + 10f; _log.LogWarning("[PTM] Scout/ESP scan skipped safely: " + ex.Message); }
            }
        }

        public void Draw(bool menuOpen)
        {
            if (menuOpen || Event.current == null || Event.current.type != EventType.Repaint) return;
            Character local = Character.localCharacter;
            if (local == null || local.data == null || local.data.dead) return;
            EnsureStyles();
            if (AnyEspEnabled()) DrawTargets(local);
            if (CompassEnabled) DrawCompass(local);
        }

        public void ResetScene()
        {
            _targets.Clear(); _nearestScout = null; _nearestScoutDistance = 0f; _nextScan = 0f;
            if (_pixel != null) UnityEngine.Object.Destroy(_pixel);
            _pixel = null; _labelStyle = null; _compassTitle = null; _compassDetail = null;
        }

        private void Invalidate() { _nextScan = 0f; }
        private bool AnyEspEnabled() { return ScoutsEnabled || LuggageEnabled || ZombiesEnabled || ScoutmastersEnabled; }

        private void RefreshNearestScout()
        {
            _nearestScout = null; _nearestScoutDistance = 0f;
            if (!CompassEnabled || _players == null || _players.Local == null || _players.Local.Character == null) return;
            Vector3 origin = _players.Local.Character.Center; float best = float.MaxValue;
            IList<PlayerEntry> entries = _players.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                PlayerEntry entry = entries[i];
                if (entry == null || entry.IsLocal || entry.Character == null || entry.Character.data == null || entry.Character.data.dead) continue;
                float sqr = (entry.Character.Center - origin).sqrMagnitude;
                if (sqr >= best) continue;
                best = sqr; _nearestScout = entry;
            }
            if (_nearestScout != null) _nearestScoutDistance = Mathf.Sqrt(best);
        }

        private void ScanTargets()
        {
            _targets.Clear();
            if (ScoutsEnabled)
            {
                IList<PlayerEntry> entries = _players.Entries;
                for (int i = 0; i < entries.Count; i++)
                {
                    PlayerEntry entry = entries[i];
                    if (entry == null || entry.IsLocal || entry.Character == null || !IsSceneObject(entry.Character)) continue;
                    AddTarget(entry.Character, EspCategory.Scout, string.IsNullOrEmpty(entry.Name) ? "SCOUT" : entry.Name);
                }
            }
            if (LuggageEnabled && Luggage.ALL_LUGGAGE != null)
            {
                for (int i = 0; i < Luggage.ALL_LUGGAGE.Count; i++)
                {
                    Luggage luggage = Luggage.ALL_LUGGAGE[i];
                    if (!IsRealLuggage(luggage)) continue;
                    AddTarget(luggage, EspCategory.Luggage, "LUGGAGE");
                }
            }
            if (ZombiesEnabled)
            {
                MushroomZombie[] zombies = Resources.FindObjectsOfTypeAll<MushroomZombie>();
                for (int i = 0; i < zombies.Length; i++) if (IsSceneObject(zombies[i])) AddTarget(zombies[i], EspCategory.Zombie, "ZOMBIE");
            }
            if (ScoutmastersEnabled)
            {
                Scoutmaster[] scoutmasters = Resources.FindObjectsOfTypeAll<Scoutmaster>();
                for (int i = 0; i < scoutmasters.Length; i++) if (IsSceneObject(scoutmasters[i])) AddTarget(scoutmasters[i], EspCategory.Scoutmaster, "SCOUTMASTER");
            }
        }

        private void AddTarget(Component component, EspCategory category, string label)
        {
            Renderer[] discovered = component.GetComponentsInChildren<Renderer>(false);
            if (discovered == null || discovered.Length == 0) return;
            List<Renderer> modelRenderers = new List<Renderer>();
            for (int i = 0; i < discovered.Length; i++) if (discovered[i] is MeshRenderer || discovered[i] is SkinnedMeshRenderer) modelRenderers.Add(discovered[i]);
            if (modelRenderers.Count == 0) return;
            _targets.Add(new EspTarget { Component = component, Renderers = modelRenderers.ToArray(), Category = category, Label = label });
        }

        private void DrawTargets(Character local)
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            float maximumSqr = MaximumDistance * MaximumDistance;
            Matrix4x4 oldMatrix = GUI.matrix; Color oldColor = GUI.color; int oldDepth = GUI.depth;
            try
            {
                GUI.depth = -8800;
                for (int i = 0; i < _targets.Count; i++)
                {
                    EspTarget target = _targets[i];
                    if (target.Component == null || !target.Component.gameObject.activeInHierarchy) continue;
                    Bounds bounds;
                    if (!TryGetBounds(target.Renderers, out bounds) || (bounds.center - local.Center).sqrMagnitude > maximumSqr) continue;
                    Vector3[] corners = BoundsCorners(bounds);
                    Vector2[] points = new Vector2[8]; bool behind = false;
                    for (int p = 0; p < corners.Length; p++)
                    {
                        Vector3 projected = camera.WorldToScreenPoint(corners[p]);
                        if (projected.z <= .05f) { behind = true; break; }
                        points[p] = new Vector2(projected.x, Screen.height - projected.y);
                    }
                    if (behind) continue;
                    Color color = GetColor(target.Category);
                    DrawBounds(points, color, OutlineWidth);
                    if (TracersEnabled)
                    {
                        Vector3 anchor = camera.WorldToScreenPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
                        DrawLine(new Vector2(Screen.width * .5f, Screen.height - 3f), new Vector2(anchor.x, Screen.height - anchor.y), color, OutlineWidth);
                    }
                    if (LabelsEnabled)
                    {
                        Vector3 top = camera.WorldToScreenPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z));
                        string label = target.Label + "  " + Mathf.RoundToInt(Vector3.Distance(local.Center, bounds.center)) + "m";
                        GUIStyle style = new GUIStyle(_labelStyle); style.normal.textColor = color;
                        GUI.Label(new Rect(top.x - 100f, Screen.height - top.y - 25f, 200f, 22f), label, style);
                    }
                }
            }
            finally { GUI.matrix = oldMatrix; GUI.color = oldColor; GUI.depth = oldDepth; }
        }

        private void DrawCompass(Character local)
        {
            string heading = _nearestScout == null ? "SCOUT COMPASS  —" : "SCOUT COMPASS  " + DirectionGlyph(local, _nearestScout.Character.Center);
            string detail = _nearestScout == null ? "No other living scout detected" : Mathf.RoundToInt(_nearestScoutDistance) + " m  •  " + _nearestScout.Name;
            float width = 310f; Rect panel = new Rect((Screen.width - width) * .5f, 92f, width, 50f);
            Color oldColor = GUI.color; int oldDepth = GUI.depth;
            try
            {
                GUI.depth = -8900;
                DrawRect(panel, new Color(.025f, .04f, .043f, .9f));
                DrawOutline(panel, _nearestScout == null ? new Color(.45f, .52f, .52f, .85f) : GetColor(EspCategory.Scout), 1f);
                GUI.Label(new Rect(panel.x + 10f, panel.y + 3f, panel.width - 20f, 23f), heading, _compassTitle);
                GUI.Label(new Rect(panel.x + 10f, panel.y + 26f, panel.width - 20f, 18f), detail, _compassDetail);
            }
            finally { GUI.color = oldColor; GUI.depth = oldDepth; }
        }

        private static bool TryGetBounds(Renderer[] renderers, out Bounds bounds)
        {
            bounds = new Bounds(); bool found = false;
            if (renderers == null) return false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return found && bounds.size.sqrMagnitude > .0001f;
        }

        private static Vector3[] BoundsCorners(Bounds b)
        {
            Vector3 min = b.min, max = b.max;
            return new[] { new Vector3(min.x,min.y,min.z), new Vector3(max.x,min.y,min.z), new Vector3(max.x,max.y,min.z), new Vector3(min.x,max.y,min.z), new Vector3(min.x,min.y,max.z), new Vector3(max.x,min.y,max.z), new Vector3(max.x,max.y,max.z), new Vector3(min.x,max.y,max.z) };
        }

        private void DrawBounds(Vector2[] p, Color color, float width)
        {
            int[,] edges = { {0,1},{1,2},{2,3},{3,0},{4,5},{5,6},{6,7},{7,4},{0,4},{1,5},{2,6},{3,7} };
            for (int i = 0; i < 12; i++) DrawLine(p[edges[i,0]], p[edges[i,1]], color, width);
        }

        private void DrawLine(Vector2 a, Vector2 b, Color color, float width)
        {
            Vector2 delta = b - a; float length = delta.magnitude;
            if (length < .5f) return;
            Matrix4x4 old = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, a);
            DrawRect(new Rect(a.x, a.y - width * .5f, length, width), color);
            GUI.matrix = old;
        }

        private static bool IsSceneObject(Component component) { return component != null && component.gameObject.scene.IsValid() && component.gameObject.activeInHierarchy; }
        private static bool IsRealLuggage(Luggage luggage)
        {
            if (!IsSceneObject(luggage) || luggage.IsOpen) return false;
            if (luggage.GetComponentInParent<MirageLuggage>() != null || luggage.GetComponentInChildren<MirageLuggage>(true) != null) return false;
            if (luggage.GetComponentInParent<global::Mirage>() != null || luggage.GetComponentInChildren<global::Mirage>(true) != null) return false;
            return !luggage.gameObject.name.StartsWith("PTM_Mirage_", StringComparison.Ordinal);
        }

        private static string DirectionGlyph(Character local, Vector3 destination)
        {
            Vector3 forward = local.data.lookDirection_Flat; forward.y = 0f;
            Vector3 toward = destination - local.Center; toward.y = 0f;
            if (forward.sqrMagnitude < .001f || toward.sqrMagnitude < .001f) return "↑";
            float angle = Vector3.SignedAngle(forward.normalized, toward.normalized, Vector3.up);
            if (angle >= -22.5f && angle < 22.5f) return "↑";
            if (angle >= 22.5f && angle < 67.5f) return "↗";
            if (angle >= 67.5f && angle < 112.5f) return "→";
            if (angle >= 112.5f && angle < 157.5f) return "↘";
            if (angle >= 157.5f || angle < -157.5f) return "↓";
            if (angle >= -157.5f && angle < -112.5f) return "↙";
            if (angle >= -112.5f && angle < -67.5f) return "←";
            return "↖";
        }

        private static Color DefaultColor(EspCategory category)
        {
            if (category == EspCategory.Scout) return new Color(.25f, 1f, .78f);
            if (category == EspCategory.Luggage) return new Color(1f, .76f, .2f);
            if (category == EspCategory.Zombie) return new Color(1f, .25f, .25f);
            return new Color(.72f, .38f, 1f);
        }

        private void EnsureStyles()
        {
            if (_pixel != null) return;
            _pixel = new Texture2D(1,1); _pixel.name = "PTM_ESP_PIXEL"; _pixel.hideFlags = HideFlags.HideAndDontSave; _pixel.SetPixel(0,0,Color.white); _pixel.Apply();
            _labelStyle = new GUIStyle(GUI.skin.label); _labelStyle.font = UiFontManager.DisplayFont; _labelStyle.fontSize = 12; _labelStyle.fontStyle = FontStyle.Bold; _labelStyle.alignment = TextAnchor.MiddleCenter;
            _compassTitle = new GUIStyle(_labelStyle); _compassTitle.fontSize = 14;
            _compassDetail = new GUIStyle(GUI.skin.label); _compassDetail.font = UiFontManager.BodyFont; _compassDetail.fontSize = 11; _compassDetail.alignment = TextAnchor.MiddleCenter; _compassDetail.normal.textColor = new Color(.76f,.84f,.83f);
        }

        private void DrawRect(Rect rect, Color color) { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, _pixel); GUI.color = old; }
        private void DrawOutline(Rect rect, Color color, float width) { DrawRect(new Rect(rect.x,rect.y,rect.width,width),color);DrawRect(new Rect(rect.x,rect.yMax-width,rect.width,width),color);DrawRect(new Rect(rect.x,rect.y,width,rect.height),color);DrawRect(new Rect(rect.xMax-width,rect.y,width,rect.height),color); }
    }
}
