using System;
using BepInEx.Logging;
using Peak.Network;
using Photon.Pun;
using Steamworks;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class QuickReconnectManager
    {
        private readonly ManualLogSource _log;
        private readonly ModConfig _settings;
        private readonly CapabilityRegistry _capabilities;
        private float _nextCapture;

        public QuickReconnectManager(ManualLogSource log, ModConfig settings, CapabilityRegistry capabilities)
        {
            _log = log;
            _settings = settings;
            _capabilities = capabilities;
        }

        public bool HasLastLobby { get { ulong id; return ulong.TryParse(_settings.LastLobbyId.Value, out id) && id != 0UL; } }
        public string LastLobbyDisplay { get { return HasLastLobby ? _settings.LastLobbyId.Value : "Not saved"; } }

        public void Tick()
        {
            if (Time.unscaledTime < _nextCapture) return;
            _nextCapture = Time.unscaledTime + 1f;
            try
            {
                IMatchmakingAPI matchmaking = NetCode.Matchmaking;
                if (matchmaking == null || !matchmaking.InLobby) return;
                string id = matchmaking.LobbyId;
                ulong parsed;
                if (!ulong.TryParse(id, out parsed) || parsed == 0UL || string.Equals(id, _settings.LastLobbyId.Value, StringComparison.Ordinal)) return;
                _settings.LastLobbyId.Value = id;
                _log.LogInfo("Quick Reconnect saved Steam lobby " + id + ".");
            }
            catch (Exception ex) { _log.LogWarning("Quick Reconnect could not capture the current lobby: " + ex.Message); }
        }

        public ActionResult Reconnect()
        {
            if (!_capabilities.Available(FeatureCapability.QuickReconnect)) return ActionResult.Fail("Quick Reconnect unsupported: " + _capabilities.Reason(FeatureCapability.QuickReconnect));
            if (PhotonNetwork.InRoom) return ActionResult.Fail("Leave the current Photon room before reconnecting.");
            ulong lobbyId;
            if (!ulong.TryParse(_settings.LastLobbyId.Value, out lobbyId) || lobbyId == 0UL) return ActionResult.Fail("No previous Steam lobby has been saved yet.");
            try
            {
                SteamLobbyHandler handler = GameHandler.GetService<SteamLobbyHandler>();
                if (handler == null) return ActionResult.Fail("PEAK's Steam lobby service is not initialized yet.");
                handler.TryJoinLobby(new CSteamID(lobbyId));
                return ActionResult.Ok("Reconnecting to the last Steam lobby…");
            }
            catch (Exception ex)
            {
                _log.LogWarning("Quick Reconnect failed safely: " + ex);
                return ActionResult.Fail("Quick Reconnect failed safely: " + ex.Message);
            }
        }
    }
}
