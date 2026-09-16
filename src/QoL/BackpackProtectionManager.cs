using System;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;

namespace PeakTrollMod
{
    internal sealed class BackpackProtectionManager
    {
        private readonly ModConfig _settings;
        private readonly ManualLogSource _log;
        private string _status = "Off";
        private float _nextDenialLog;

        public BackpackProtectionManager(ModConfig settings, ManualLogSource log)
        {
            _settings = settings;
            _log = log;
        }

        public bool Enabled
        {
            get { return _settings.BackpackProtectionEnabled.Value; }
            set
            {
                _settings.BackpackProtectionEnabled.Value = value;
                TrollModPlugin plugin = TrollModPlugin.Instance;
                if (plugin != null && plugin.Network != null) plugin.Network.RefreshLocalAdvertisement();
                _status = value ? EnforcementStatus() : "Off";
            }
        }

        public string Status
        {
            get
            {
                if (!Enabled) return "Off";
                _status = EnforcementStatus();
                return _status;
            }
        }

        internal bool AllowPickup(Item item, PhotonView requesterView)
        {
            if (!PhotonNetwork.IsMasterClient || item == null || item.backpackReference.IsNone) return true;
            try
            {
                BackpackReference reference = item.backpackReference.Value.Item2;
                if (reference.type != BackpackReference.BackpackType.Equipped || reference.view == null || reference.view.Owner == null) return true;
                int ownerActor = reference.view.Owner.ActorNumber;
                if (!TrollModPlugin.Instance.Network.IsBackpackProtected(ownerActor)) return true;

                Photon.Realtime.Player requester = requesterView == null ? null : requesterView.Owner;
                if (requester != null && requester.ActorNumber == ownerActor) return true;

                if (requester != null && item.photonView != null)
                    item.photonView.RPC("DenyPickupRPC", requester, Array.Empty<object>());
                if (UnityEngine.Time.unscaledTime >= _nextDenialLog)
                {
                    _nextDenialLog = UnityEngine.Time.unscaledTime + 2f;
                    _log.LogInfo("[PTM] Backpack Protection denied a withdrawal from actor " + ownerActor + " by actor " + (requester == null ? 0 : requester.ActorNumber) + ".");
                }
                return false;
            }
            catch (Exception ex)
            {
                _log.LogWarning("[PTM] Backpack Protection inspection failed safely: " + ex.Message);
                return true;
            }
        }

        private static string EnforcementStatus()
        {
            if (!PhotonNetwork.InRoom) return "Active — will advertise when you join a lobby.";
            TrollModPlugin plugin = TrollModPlugin.Instance;
            if (plugin == null || plugin.Network == null) return "Active — networking is still initializing.";
            if (PhotonNetwork.IsMasterClient) return "Active — this host blocks other scouts from withdrawing your items.";
            if (plugin.Network.HostSupportsRequests) return "Active — the compatible host enforces your lock, including against unmodded scouts.";
            return "Active locally, but the current host needs the same protocol to enforce it.";
        }
    }

    [HarmonyPatch(typeof(Item), "RequestPickup")]
    internal static class BackpackProtectionPickupPatch
    {
        private static bool Prefix(Item __instance, PhotonView __0)
        {
            TrollModPlugin plugin = TrollModPlugin.Instance;
            return plugin == null || plugin.BackpackProtection == null || plugin.BackpackProtection.AllowPickup(__instance, __0);
        }
    }
}
