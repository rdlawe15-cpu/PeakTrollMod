using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class LuggageNavigationManager
    {
        private static readonly FieldInfo MirageLuggageRenderers = AccessTools.Field(typeof(MirageLuggage), "renderers");
        private static readonly FieldInfo MirageParticleRenderer = AccessTools.Field(typeof(global::Mirage), "psr");
        private readonly ModConfig _settings;
        private readonly ManualLogSource _log;
        private readonly Dictionary<Renderer, bool> _suppressedRenderers = new Dictionary<Renderer, bool>();
        private Luggage _nearest;
        private float _distance;
        private float _nextLuggageScan;
        private float _nextMirageScan;
        private float _nextErrorLog;
        private bool _wasSuppressing;
        private Texture2D _pixel;
        private GUIStyle _title;
        private GUIStyle _detail;

        public LuggageNavigationManager(ModConfig settings, ManualLogSource log) { _settings = settings; _log = log; }

        public bool DirectionsEnabled { get { return _settings.RealLuggageDirectionsEnabled.Value; } set { _settings.RealLuggageDirectionsEnabled.Value = value; } }
        public bool AntiMiragesEnabled { get { return _settings.MesaAntiMiragesEnabled.Value; } set { _settings.MesaAntiMiragesEnabled.Value = value; } }
        public int HiddenRendererCount { get { return _suppressedRenderers.Count; } }
        public bool InMesa { get { return IsMesa(); } }
        public string LuggageStatus { get { return _nearest == null ? "No unopened real luggage detected" : Mathf.RoundToInt(_distance) + " m to nearest real luggage"; } }
        public string MirageStatus { get { return !AntiMiragesEnabled ? "Off" : !InMesa ? "Armed; activates in the Mesa" : "Hiding " + HiddenRendererCount + " native mirage renderer(s)"; } }

        public void Tick()
        {
            try
            {
                if (DirectionsEnabled && Time.unscaledTime >= _nextLuggageScan) { _nextLuggageScan = Time.unscaledTime + .35f; RefreshNearestLuggage(); }
                if (!DirectionsEnabled) _nearest = null;

                bool suppress = AntiMiragesEnabled && IsMesa();
                if (suppress && Time.unscaledTime >= _nextMirageScan) { _nextMirageScan = Time.unscaledTime + .75f; FindAndSuppressMesaMirages(); }
                if (!suppress && _wasSuppressing) RestoreMirages();
                _wasSuppressing = suppress;
            }
            catch (Exception ex)
            {
                if (Time.unscaledTime >= _nextErrorLog) { _nextErrorLog = Time.unscaledTime + 10f; _log.LogWarning("[PTM] Luggage navigation skipped a frame safely: " + ex.Message); }
            }
        }

        public void Draw(bool menuOpen)
        {
            if (menuOpen || !DirectionsEnabled || Event.current == null || Event.current.type != EventType.Repaint) return;
            Character local = Character.localCharacter;
            if (local == null || local.data == null || local.data.dead || local.data.fullyPassedOut) return;
            EnsureStyles();

            string heading = _nearest == null ? "REAL LUGGAGE  —" : "REAL LUGGAGE  " + DirectionGlyph(local);
            string detail = _nearest == null ? "No unopened genuine luggage detected" : Mathf.RoundToInt(_distance) + " m  •  native and mod mirages ignored";
            float width = 330f;
            Rect panel = new Rect((Screen.width - width) * .5f, 34f, width, 52f);
            Color oldColor = GUI.color; int oldDepth = GUI.depth;
            try
            {
                GUI.depth = -9000;
                DrawRect(panel, new Color(.025f, .04f, .043f, .9f));
                DrawOutline(panel, _nearest == null ? new Color(.45f, .52f, .52f, .85f) : new Color(.25f, .85f, .74f, .95f));
                GUI.Label(new Rect(panel.x + 10f, panel.y + 4f, panel.width - 20f, 23f), heading, _title);
                GUI.Label(new Rect(panel.x + 10f, panel.y + 27f, panel.width - 20f, 18f), detail, _detail);
            }
            finally { GUI.color = oldColor; GUI.depth = oldDepth; }
        }

        public void SuppressNativeMirage(Component component)
        {
            if (component == null || !AntiMiragesEnabled || !_wasSuppressing) return;
            try
            {
                MirageLuggage luggageMirage = component as MirageLuggage;
                if (luggageMirage != null)
                {
                    Renderer[] renderers = MirageLuggageRenderers == null ? null : MirageLuggageRenderers.GetValue(luggageMirage) as Renderer[];
                    Suppress(renderers);
                    return;
                }

                global::Mirage mirage = component as global::Mirage;
                if (mirage == null) return;
                Suppress(mirage.GetComponentsInChildren<Renderer>(true));
                Renderer particleRenderer = MirageParticleRenderer == null ? null : MirageParticleRenderer.GetValue(mirage) as Renderer;
                Suppress(particleRenderer);
                if (mirage.objectsToHide != null)
                    for (int i = 0; i < mirage.objectsToHide.Length; i++) if (mirage.objectsToHide[i] != null) Suppress(mirage.objectsToHide[i].GetComponentsInChildren<Renderer>(true));
            }
            catch (Exception ex)
            {
                if (Time.unscaledTime >= _nextErrorLog) { _nextErrorLog = Time.unscaledTime + 10f; _log.LogWarning("[PTM] Mesa mirage suppression skipped an unsupported object: " + ex.Message); }
            }
        }

        public void ResetScene()
        {
            RestoreMirages();
            _nearest = null; _distance = 0f; _nextLuggageScan = 0f; _nextMirageScan = 0f; _wasSuppressing = false;
            if (_pixel != null) UnityEngine.Object.Destroy(_pixel);
            _pixel = null; _title = null; _detail = null;
        }

        private void RefreshNearestLuggage()
        {
            _nearest = null; _distance = 0f;
            Character local = Character.localCharacter;
            if (local == null || Luggage.ALL_LUGGAGE == null) return;
            float maxDistance = Mathf.Clamp(_settings.RealLuggageMaximumDistance.Value, 25f, 2000f);
            float bestSqr = maxDistance * maxDistance;
            for (int i = 0; i < Luggage.ALL_LUGGAGE.Count; i++)
            {
                Luggage candidate = Luggage.ALL_LUGGAGE[i];
                if (!IsRealUnopenedLuggage(candidate)) continue;
                float sqr = (candidate.transform.position - local.Center).sqrMagnitude;
                if (sqr >= bestSqr) continue;
                bestSqr = sqr; _nearest = candidate;
            }
            if (_nearest != null) _distance = Mathf.Sqrt(bestSqr);
        }

        private static bool IsRealUnopenedLuggage(Luggage luggage)
        {
            if (luggage == null || luggage.IsOpen || !luggage.gameObject.scene.IsValid() || !luggage.gameObject.activeInHierarchy) return false;
            if (luggage.GetComponentInParent<MirageLuggage>() != null || luggage.GetComponentInChildren<MirageLuggage>(true) != null) return false;
            if (luggage.GetComponentInParent<global::Mirage>() != null || luggage.GetComponentInChildren<global::Mirage>(true) != null) return false;
            return !luggage.gameObject.name.StartsWith("PTM_Mirage_", StringComparison.Ordinal);
        }

        private void FindAndSuppressMesaMirages()
        {
            MirageLuggage[] luggageMirages = Resources.FindObjectsOfTypeAll<MirageLuggage>();
            for (int i = 0; i < luggageMirages.Length; i++) if (luggageMirages[i] != null && luggageMirages[i].gameObject.scene.IsValid()) SuppressNativeMirage(luggageMirages[i]);
            global::Mirage[] mirages = Resources.FindObjectsOfTypeAll<global::Mirage>();
            for (int i = 0; i < mirages.Length; i++) if (mirages[i] != null && mirages[i].gameObject.scene.IsValid()) SuppressNativeMirage(mirages[i]);
            RemoveDestroyedRendererKeys();
        }

        private void Suppress(Renderer renderer)
        {
            if (renderer == null) return;
            if (!_suppressedRenderers.ContainsKey(renderer)) _suppressedRenderers.Add(renderer, renderer.enabled);
            renderer.enabled = false;
        }

        private void Suppress(Renderer[] renderers)
        {
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++) Suppress(renderers[i]);
        }

        private void RestoreMirages()
        {
            foreach (KeyValuePair<Renderer, bool> pair in _suppressedRenderers) if (pair.Key != null) pair.Key.enabled = pair.Value;
            _suppressedRenderers.Clear();
        }

        private void RemoveDestroyedRendererKeys()
        {
            List<Renderer> destroyed = null;
            foreach (KeyValuePair<Renderer, bool> pair in _suppressedRenderers) if (pair.Key == null) { if (destroyed == null) destroyed = new List<Renderer>(); destroyed.Add(pair.Key); }
            if (destroyed != null) for (int i = 0; i < destroyed.Count; i++) _suppressedRenderers.Remove(destroyed[i]);
        }

        private static bool IsMesa()
        {
            try { MapHandler map = UnityEngine.Object.FindFirstObjectByType<MapHandler>(); return map != null && map.GetCurrentBiome() == Biome.BiomeType.Mesa; }
            catch { return false; }
        }

        private string DirectionGlyph(Character local)
        {
            if (_nearest == null) return "—";
            Vector3 forward = local.data.lookDirection_Flat; forward.y = 0f;
            Vector3 toward = _nearest.transform.position - local.Center; toward.y = 0f;
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

        private void EnsureStyles()
        {
            if (_pixel != null) return;
            _pixel = new Texture2D(1, 1); _pixel.name = "PTM_LUGGAGE_NAV_PIXEL"; _pixel.hideFlags = HideFlags.HideAndDontSave; _pixel.SetPixel(0, 0, Color.white); _pixel.Apply();
            _title = new GUIStyle(GUI.skin.label); _title.font = UiFontManager.DisplayFont; _title.fontSize = 14; _title.fontStyle = FontStyle.Bold; _title.alignment = TextAnchor.MiddleCenter; _title.normal.textColor = new Color(.45f, 1f, .88f);
            _detail = new GUIStyle(GUI.skin.label); _detail.font = UiFontManager.BodyFont; _detail.fontSize = 11; _detail.alignment = TextAnchor.MiddleCenter; _detail.normal.textColor = new Color(.76f, .84f, .83f);
        }

        private void DrawRect(Rect rect, Color color) { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, _pixel); GUI.color = old; }
        private void DrawOutline(Rect rect, Color color) { DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color); DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color); DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color); DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color); }
    }
}
