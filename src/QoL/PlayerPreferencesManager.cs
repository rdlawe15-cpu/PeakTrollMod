using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BepInEx.Logging;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class PlayerPreference
    {
        public ulong SteamId; public string Alias = string.Empty; public float VoiceVolume = 1f; public bool Muted; public bool ExcludeFromRandom; public Color Color = new Color(.35f,.85f,.78f,1f);
        public PlayerPreference Clone() { return new PlayerPreference { SteamId=SteamId,Alias=Alias,VoiceVolume=VoiceVolume,Muted=Muted,ExcludeFromRandom=ExcludeFromRandom,Color=Color }; }
    }

    internal sealed class PlayerPreferencesManager
    {
        private readonly ManualLogSource _log; private readonly PlayerManager _players; private readonly AudioManager _audio; private readonly ModConfig _settings;
        private readonly Dictionary<ulong,PlayerPreference> _saved=new Dictionary<ulong,PlayerPreference>(); private float _nextApply;
        public PlayerPreferencesManager(ManualLogSource log, PlayerManager players, AudioManager audio, ModConfig settings) { _log=log;_players=players;_audio=audio;_settings=settings;Load();_players.ExcludeFromRandom=IsExcluded; }
        public PlayerPreference Get(PlayerEntry entry) { PlayerPreference value; if(entry!=null&&entry.SteamUserId!=0UL&&_saved.TryGetValue(entry.SteamUserId,out value))return value.Clone();return new PlayerPreference { SteamId=entry==null?0UL:entry.SteamUserId }; }
        public string DisplayName(PlayerEntry entry) { PlayerPreference value; return entry!=null&&entry.SteamUserId!=0UL&&_saved.TryGetValue(entry.SteamUserId,out value)&&!string.IsNullOrEmpty(value.Alias)?value.Alias:entry==null?"Unknown":entry.Name; }
        public Color DisplayColor(PlayerEntry entry) { PlayerPreference value; return entry!=null&&entry.SteamUserId!=0UL&&_saved.TryGetValue(entry.SteamUserId,out value)?value.Color:new Color(.35f,.85f,.78f,1f); }
        public bool IsExcluded(PlayerEntry entry) { PlayerPreference value; return entry!=null&&entry.SteamUserId!=0UL&&_saved.TryGetValue(entry.SteamUserId,out value)&&value.ExcludeFromRandom; }
        public ActionResult Save(PlayerEntry entry, PlayerPreference value)
        {
            if(entry==null||entry.SteamUserId==0UL)return ActionResult.Fail("This player's Steam identity is not available yet.");
            value.SteamId=entry.SteamUserId;value.VoiceVolume=Mathf.Clamp01(value.VoiceVolume);value.Alias=(value.Alias??string.Empty).Trim();if(value.Alias.Length>24)value.Alias=value.Alias.Substring(0,24);value.Color=new Color(Mathf.Clamp01(value.Color.r),Mathf.Clamp01(value.Color.g),Mathf.Clamp01(value.Color.b),1f);
            _saved[value.SteamId]=value.Clone();Persist();Apply(entry,value);return ActionResult.Ok("Saved local preferences for "+entry.Name+".");
        }
        public ActionResult Reset(PlayerEntry entry)
        {
            if(entry==null||entry.SteamUserId==0UL)return ActionResult.Fail("This player's Steam identity is not available yet.");
            _saved.Remove(entry.SteamUserId);Persist();_audio.RestoreVoice(entry.ActorNumber);return ActionResult.Ok("Removed saved preferences for "+entry.Name+".");
        }
        public void Tick() { if(Time.unscaledTime<_nextApply)return;_nextApply=Time.unscaledTime+2f;for(int i=0;i<_players.Entries.Count;i++){PlayerEntry e=_players.Entries[i];PlayerPreference p;if(e!=null&&e.SteamUserId!=0UL&&_saved.TryGetValue(e.SteamUserId,out p))Apply(e,p);} }
        private void Apply(PlayerEntry entry,PlayerPreference value){if(!entry.IsLocal)_audio.SetIncomingVoicePreference(entry,value.VoiceVolume,value.Muted);}
        private void Persist()
        {
            List<string> rows=new List<string>();foreach(KeyValuePair<ulong,PlayerPreference> pair in _saved){PlayerPreference p=pair.Value;string alias=Convert.ToBase64String(Encoding.UTF8.GetBytes(p.Alias??string.Empty));rows.Add(pair.Key+","+p.VoiceVolume.ToString("0.###",CultureInfo.InvariantCulture)+","+(p.Muted?"1":"0")+","+(p.ExcludeFromRandom?"1":"0")+","+p.Color.r.ToString("0.###",CultureInfo.InvariantCulture)+","+p.Color.g.ToString("0.###",CultureInfo.InvariantCulture)+","+p.Color.b.ToString("0.###",CultureInfo.InvariantCulture)+","+alias);}_settings.PlayerPreferencesData.Value=string.Join(";",rows.ToArray());
        }
        private void Load()
        {
            try{string[] rows=(_settings.PlayerPreferencesData.Value??string.Empty).Split(new[]{';'},StringSplitOptions.RemoveEmptyEntries);for(int i=0;i<rows.Length;i++){string[] v=rows[i].Split(',');ulong id;float volume,r,g,b;if(v.Length!=8||!ulong.TryParse(v[0],out id)||!float.TryParse(v[1],NumberStyles.Float,CultureInfo.InvariantCulture,out volume)||!float.TryParse(v[4],NumberStyles.Float,CultureInfo.InvariantCulture,out r)||!float.TryParse(v[5],NumberStyles.Float,CultureInfo.InvariantCulture,out g)||!float.TryParse(v[6],NumberStyles.Float,CultureInfo.InvariantCulture,out b))continue;_saved[id]=new PlayerPreference{SteamId=id,VoiceVolume=Mathf.Clamp01(volume),Muted=v[2]=="1",ExcludeFromRandom=v[3]=="1",Color=new Color(Mathf.Clamp01(r),Mathf.Clamp01(g),Mathf.Clamp01(b),1f),Alias=Encoding.UTF8.GetString(Convert.FromBase64String(v[7]))};}}
            catch(Exception ex){_saved.Clear();_log.LogWarning("Player preferences could not be loaded: "+ex.Message);}
        }
    }
}
