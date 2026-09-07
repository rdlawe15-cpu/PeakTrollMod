using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class BetterSpectatingManager
    {
        private static readonly FieldInfo GodCamField = AccessTools.Field(typeof(MainCameraMovement), "isGodCam");
        private readonly ManualLogSource _log; private readonly PlayerManager _players; private readonly PlayerActions _actions; private readonly ModConfig _settings;
        private MainCameraMovement _camera; private float _nextFind;
        private GUIStyle _title, _text; private Texture2D _pixel;
        public BetterSpectatingManager(ManualLogSource log, PlayerManager players, PlayerActions actions, ModConfig settings) { _log = log; _players = players; _actions = actions; _settings = settings; }
        public bool IsEligible { get { Character c = Character.localCharacter; return _settings.BetterSpectatingEnabled.Value && c != null && c.data != null && c.data.fullyPassedOut; } }
        public bool IsSpectating { get { return IsEligible && Camera != null && MainCameraMovement.IsSpectating; } }
        public Character SpectatedCharacter { get { return Camera == null ? null : MainCameraMovement.specCharacter; } }
        public bool FreeCamera { get { return Camera != null && GodCamField != null && (bool)GodCamField.GetValue(Camera); } }
        private MainCameraMovement Camera { get { if (_camera == null && Time.unscaledTime >= _nextFind) { _nextFind = Time.unscaledTime + 2f; _camera = UnityEngine.Object.FindObjectOfType<MainCameraMovement>(); } return _camera; } }
        public void Tick(bool menuOpen)
        {
            MainCameraMovement camera = Camera;
            if (!IsEligible) { if (camera != null && FreeCamera) SetFreeCamera(false); return; }
            if (menuOpen || camera == null) return;
            try { if (_settings.SpectatePreviousKey.Value.IsDown()) camera.SwapSpecPlayerLeft(); if (_settings.SpectateNextKey.Value.IsDown()) camera.SwapSpecPlayerRight(); if (_settings.SpectateFreeCameraKey.Value.IsDown()) SetFreeCamera(!FreeCamera); }
            catch (Exception ex) { _log.LogWarning("Better Spectating input failed safely: " + ex.Message); }
        }
        public ActionResult SetFreeCamera(bool enabled)
        {
            if (!IsEligible || Camera == null || GodCamField == null) return ActionResult.Fail("Free camera is available only while fully passed out.");
            try { GodCamField.SetValue(Camera, enabled); return ActionResult.Ok(enabled ? "Spectator free camera enabled." : "Returned to the spectated scout."); }
            catch (Exception ex) { return ActionResult.Fail("Free camera unavailable: " + ex.Message); }
        }
        public ActionResult ReviveBesideSpectated()
        {
            PlayerEntry local = _players.Local; Character target = SpectatedCharacter;
            if (local == null || local.Character == null || target == null || target.data == null || target == local.Character) return ActionResult.Fail("No other spectated scout is available.");
            UnityEngine.Vector3 right = target.data.lookDirection_Right; right.y = 0f; if (!PlayerActions.Finite(right) || right.sqrMagnitude < .01f) right = UnityEngine.Vector3.right;
            UnityEngine.Vector3 destination; if (!PlayerActions.TrySafePosition(target.Center + right.normalized * 1.5f, out destination)) return ActionResult.Fail("No safe ground was found beside the spectated scout.");
            SetFreeCamera(false); ActionResult result = _actions.ResurrectLocal(local, destination, false); if (result.Success) _actions.HaltVelocityLocal(local);
            return result.Success ? ActionResult.Ok("Revived beside " + target.characterName + ".") : result;
        }
        public void OnSceneLoaded() { _camera = null; _nextFind = 0f; }

        public void Draw(bool menuOpen)
        {
            if (menuOpen || !_settings.SpectateOverlay.Value || !IsSpectating || FreeCamera) return;
            Character target = SpectatedCharacter; if (target == null || target.data == null) return; EnsureStyles();
            Rect rect = new Rect((Screen.width - 520f) * .5f, Screen.height - 112f, 520f, 86f); Color old=GUI.color; GUI.color=new Color(.025f,.04f,.043f,.9f); GUI.DrawTexture(rect,_pixel); GUI.color=old;
            string name = string.IsNullOrEmpty(target.characterName) ? "Scout" : target.characterName;
            string status = target.data.dead ? "Dead" : target.data.zombified ? "Zombie" : target.data.fullyPassedOut ? "Passed out" : "Alive";
            GUI.Label(new Rect(rect.x+16f,rect.y+8f,488f,26f),"SPECTATING  " + name,_title);
            GUI.Label(new Rect(rect.x+16f,rect.y+35f,488f,42f),"Altitude " + target.Center.y.ToString("0") + "m  •  " + status + "\n" + _settings.SpectatePreviousKey.Value + "/" + _settings.SpectateNextKey.Value + " cycle  •  " + _settings.SpectateFreeCameraKey.Value + " free cam  •  F7 revive controls",_text);
        }
        private void EnsureStyles() { if(_pixel!=null)return;_pixel=new Texture2D(1,1);_pixel.SetPixel(0,0,Color.white);_pixel.Apply();_title=new GUIStyle(GUI.skin.label);_title.fontSize=15;_title.fontStyle=FontStyle.Bold;_title.normal.textColor=new Color(.4f,.95f,.86f);_text=new GUIStyle(GUI.skin.label);_text.fontSize=12;_text.normal.textColor=new Color(.82f,.88f,.87f); }
    }
}
