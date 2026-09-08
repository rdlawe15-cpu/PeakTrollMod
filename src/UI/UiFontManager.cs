using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using BepInEx.Logging;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class UiFontManager
    {
        private const uint PrivateFont = 0x10;
        private readonly ManualLogSource _log;
        private readonly List<string> _registeredFiles = new List<string>();
        public static Font DisplayFont { get; private set; }
        public static Font BodyFont { get; private set; }

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int AddFontResourceEx(string fileName, uint flags, IntPtr reserved);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool RemoveFontResourceEx(string fileName, uint flags, IntPtr reserved);

        public UiFontManager(ManualLogSource log) { _log = log; }

        public void Load()
        {
            string assemblyFolder = Path.GetDirectoryName(typeof(TrollModPlugin).Assembly.Location);
            string fontFolder = Path.Combine(assemblyFolder ?? string.Empty, "Fonts");
            DisplayFont = LoadFont(Path.Combine(fontFolder, "Exo2-Variable.ttf"), new[] { "Exo 2", "Exo2" }, "Exo 2");
            BodyFont = LoadFont(Path.Combine(fontFolder, "Inter-Variable.ttf"), new[] { "Inter", "Inter Variable" }, "Inter");
            if (DisplayFont == null || BodyFont == null) _log.LogWarning("[PTM] One or more bundled UI fonts were unavailable; affected styles will use Unity's default font.");
            else _log.LogInfo("Bundled Exo 2 and Inter UI fonts loaded.");
        }

        public void Dispose()
        {
            if (DisplayFont != null) UnityEngine.Object.Destroy(DisplayFont);
            if (BodyFont != null && BodyFont != DisplayFont) UnityEngine.Object.Destroy(BodyFont);
            DisplayFont = null; BodyFont = null;
            if (IsWindows()) for (int i = _registeredFiles.Count - 1; i >= 0; i--) RemoveFontResourceEx(_registeredFiles[i], PrivateFont, IntPtr.Zero);
            _registeredFiles.Clear();
        }

        private Font LoadFont(string path, string[] familyNames, string label)
        {
            if (!File.Exists(path)) { _log.LogWarning("[PTM] Bundled " + label + " file was not found at " + path); return null; }
            try
            {
                if (IsWindows() && AddFontResourceEx(path, PrivateFont, IntPtr.Zero) > 0) _registeredFiles.Add(path);
                Font font = Font.CreateDynamicFontFromOSFont(familyNames, 16);
                if (font != null) { font.name = "PTM_" + label.Replace(" ", string.Empty); font.hideFlags = HideFlags.HideAndDontSave; }
                return font;
            }
            catch (Exception ex) { _log.LogWarning("[PTM] Failed to load bundled " + label + ": " + ex.Message); return null; }
        }

        private static bool IsWindows() { return Environment.OSVersion.Platform == PlatformID.Win32NT; }
    }
}
