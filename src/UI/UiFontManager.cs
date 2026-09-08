using BepInEx.Logging;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class UiFontManager
    {
        private readonly ManualLogSource _log;
        private int _revision;

        // A null GUIStyle font intentionally inherits PEAK/Unity's native IMGUI font.
        // Dynamic fonts created from process-private TTF registrations corrupt IMGUI's
        // shared text buffer in this PEAK build, causing every control to repeat the
        // final string drawn in the current tab or to render no text at all.
        public static Font DisplayFont { get { return null; } }
        public static Font BodyFont { get { return null; } }
        public int Revision { get { return _revision; } }

        public UiFontManager(ManualLogSource log) { _log = log; }

        public void Load()
        {
            _revision++;
            _log.LogInfo("Using Unity's native IMGUI font for reliable menu text rendering.");
        }

        public void PrepareForRendering() { }
        public void Dispose() { }
    }
}
