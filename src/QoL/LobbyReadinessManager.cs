using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Peak.Network;

namespace PeakTrollMod
{
    internal sealed class LobbyReadinessManager
    {
        private readonly PlayerManager _players; private int _hostActor; private int _hostChanges;
        public LobbyReadinessManager(PlayerManager players){_players=players;}
        public void Tick()
        {
            if(!PhotonNetwork.InRoom){_hostActor=0;_hostChanges=0;return;}int current=PhotonNetwork.MasterClient==null?0:PhotonNetwork.MasterClient.ActorNumber;if(_hostActor!=0&&current!=0&&current!=_hostActor)_hostChanges++;_hostActor=current;
        }
        public string HostName { get { PlayerEntry host=_players.Find(_hostActor);return host==null?(PhotonNetwork.MasterClient==null?"Unavailable":PhotonNetwork.MasterClient.NickName):host.Name; } }
        public string HostMigration { get { return _hostChanges==0?"Stable":"Changed "+_hostChanges+(_hostChanges==1?" time":" times"); } }
        public string LobbyCode { get { try{IMatchmakingAPI api=NetCode.Matchmaking;return api==null||!api.InLobby?string.Empty:api.LobbyId;}catch{return string.Empty;} } }
        public string Readiness(PlayerEntry entry)
        {
            if(entry==null||entry.Character==null)return "NOT LOADED";
            return entry.Character.IsInitialized?"LOADED":"LOADING";
        }
    }
}
