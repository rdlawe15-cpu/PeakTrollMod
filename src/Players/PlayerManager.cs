using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Photon.Pun;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class PlayerManager
    {
        private readonly ManualLogSource _log;
        private readonly List<PlayerEntry> _entries = new List<PlayerEntry>();
        private float _nextRefresh;
        public IList<PlayerEntry> Entries { get { return _entries.AsReadOnly(); } }
        public PlayerEntry Local { get; private set; }

        public PlayerManager(ManualLogSource log) { _log = log; }

        public void Tick()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 1f;
            Refresh();
        }

        public void Refresh()
        {
            _entries.Clear();
            Local = null;
            try
            {
                if (!PlayerHandler.Exists) return;
                List<Character> characters = PlayerHandler.GetAllPlayerCharacters();
                for (int i = 0; i < characters.Count; i++)
                {
                    Character c = characters[i];
                    if (c == null || c.refs == null || c.refs.view == null) continue;
                    PlayerEntry entry = new PlayerEntry();
                    entry.Character = c;
                    entry.ActorNumber = c.refs.view.OwnerActorNr;
                    entry.Name = string.IsNullOrEmpty(c.characterName) ? "Player " + entry.ActorNumber : c.characterName;
                    entry.IsLocal = c.IsLocal;
                    _entries.Add(entry);
                    if (entry.IsLocal) Local = entry;
                }
            }
            catch (Exception ex) { _log.LogWarning("Player resolution failed: " + ex.Message); }
        }

        public PlayerEntry Find(int actor)
        {
            for (int i = 0; i < _entries.Count; i++) if (_entries[i].ActorNumber == actor) return _entries[i];
            return null;
        }

        public PlayerEntry Random(bool excludeSelf)
        {
            List<PlayerEntry> candidates = new List<PlayerEntry>();
            for (int i = 0; i < _entries.Count; i++) if (!excludeSelf || !_entries[i].IsLocal) candidates.Add(_entries[i]);
            return candidates.Count == 0 ? null : candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        public bool IsHost { get { return PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient; } }
    }
}
