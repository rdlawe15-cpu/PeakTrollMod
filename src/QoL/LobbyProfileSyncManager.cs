using System;
using System.Collections.Generic;
using Photon.Pun;

namespace PeakTrollMod
{
    internal sealed class LobbyProfileSyncManager
    {
        private readonly ProfileManager _profiles;
        private readonly PlayerManager _players;
        private TrollNetworkManager _network;
        private readonly Dictionary<int,string> _responses = new Dictionary<int,string>();
        private string _pendingName=string.Empty;
        private string _pendingCode=string.Empty;
        private int _pendingHost;

        public LobbyProfileSyncManager(ProfileManager profiles, PlayerManager players){_profiles=profiles;_players=players;}
        public void AttachNetwork(TrollNetworkManager network){_network=network;}
        public bool HasPendingOffer { get { return !string.IsNullOrEmpty(_pendingCode); } }
        public string PendingName { get { return _pendingName; } }
        public string Status { get { return HasPendingOffer ? "Host offered profile: "+_pendingName : (_players.IsHost ? ResponseSummary() : "No pending host profile offer"); } }

        public ActionResult Offer(string name)
        {
            if(!_players.IsHost)return ActionResult.Fail("Only the current host can offer a lobby profile.");
            ActionResult saved=_profiles.SaveCurrent(name);if(!saved.Success)return saved;_responses.Clear();
            return _network.OfferLobbyProfile(_profiles.ActiveName,_profiles.SavedCode);
        }

        public ActionResult ReceiveOffer(int senderActor,string name,string code)
        {
            if(!PhotonNetwork.InRoom||PhotonNetwork.MasterClient==null||senderActor!=PhotonNetwork.MasterClient.ActorNumber)return ActionResult.Fail("Rejected profile offer from a non-host sender.");
            string decoded;if(!_profiles.ValidateCode(code,out decoded))return ActionResult.Fail("Rejected invalid lobby profile code.");
            _pendingHost=senderActor;_pendingName=decoded;_pendingCode=code;return ActionResult.Ok("Lobby host offered profile "+decoded+". Review it on Profiles.");
        }

        public ActionResult Accept()
        {
            if(!HasPendingOffer)return ActionResult.Fail("No host profile offer is pending.");string name=_pendingName;ActionResult result=_profiles.ImportAndApply(_pendingCode);
            if(result.Success&&_network!=null)_network.RespondLobbyProfile(_pendingHost,true,name);ClearPending();return result;
        }

        public ActionResult Decline()
        {
            if(!HasPendingOffer)return ActionResult.Fail("No host profile offer is pending.");string name=_pendingName;if(_network!=null)_network.RespondLobbyProfile(_pendingHost,false,name);ClearPending();return ActionResult.Ok("Declined profile "+name+".");
        }

        public ActionResult ReceiveResponse(int senderActor,bool accepted,string name)
        {
            if(!_players.IsHost)return ActionResult.Fail("Ignored profile response on a non-host client.");PlayerEntry player=_players.Find(senderActor);if(player==null)return ActionResult.Fail("Profile respondent left the lobby.");
            _responses[senderActor]=(accepted?"Accepted ":"Declined ")+name;return ActionResult.Ok(player.Name+" "+(accepted?"accepted.":"declined."));
        }

        public void Tick(){if(!PhotonNetwork.InRoom)Reset();else if(HasPendingOffer&&(PhotonNetwork.MasterClient==null||PhotonNetwork.MasterClient.ActorNumber!=_pendingHost))ClearPending();}
        public void Reset(){ClearPending();_responses.Clear();}
        private void ClearPending(){_pendingHost=0;_pendingName=string.Empty;_pendingCode=string.Empty;}
        private string ResponseSummary(){int accepted=0,declined=0;foreach(KeyValuePair<int,string> pair in _responses){if(pair.Value.StartsWith("Accepted",StringComparison.Ordinal))accepted++;else declined++;}return _responses.Count==0?"No responses to the latest offer":accepted+" accepted • "+declined+" declined";}
    }
}
