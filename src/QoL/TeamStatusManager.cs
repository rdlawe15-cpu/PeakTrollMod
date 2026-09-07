using System;
using System.Collections.Generic;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class TeamStatusManager
    {
        private readonly PlayerManager _players; private readonly ModConfig _settings; private readonly ModConfigBrowser _mods;
        private GUIStyle _name, _detail; private Texture2D _pixel;
        public TeamStatusManager(PlayerManager players, ModConfig settings, ModConfigBrowser mods) { _players = players; _settings = settings; _mods = mods; }
        public bool SuppressedByExternal { get { return _settings.PreferExternalQualityOfLifeMods.Value && _mods.HasEnabledBool("com.github.LengSword.PeakStatsEx", "DisplayTeammateStaminaBars"); } }

        public void Draw(bool menuOpen)
        {
            if (menuOpen || !_settings.TeamStatusEnabled.Value || SuppressedByExternal || !GameHandler.IsOnIsland || _players.Local == null || _players.Local.Character == null) return;
            EnsureStyles(); float scale = Mathf.Clamp(_settings.TeamStatusScale.Value, .7f, 1.4f); Matrix4x4 oldMatrix = GUI.matrix; Color oldColor = GUI.color;
            GUI.matrix = Matrix4x4.TRS(new Vector3(Screen.width - 330f * scale - 18f, 18f, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
            float y = 0f; int shown = 0; float maxDistance = Mathf.Clamp(_settings.TeamStatusDistance.Value, 10f, 200f);
            IList<PlayerEntry> entries = _players.Entries;
            for (int i = 0; i < entries.Count && shown < 6; i++)
            {
                PlayerEntry entry = entries[i]; Character c = entry == null ? null : entry.Character;
                if (c == null || c.data == null || c.refs == null || (!_settings.TeamStatusShowSelf.Value && entry.IsLocal)) continue;
                float distance = Vector3.Distance(_players.Local.Character.Center, c.Center); if (!entry.IsLocal && distance > maxDistance) continue;
                DrawCard(new Rect(0f, y, 330f, 58f), entry, distance); y += 64f; shown++;
            }
            GUI.matrix = oldMatrix; GUI.color = oldColor;
        }

        private void DrawCard(Rect rect, PlayerEntry entry, float distance)
        {
            Character c = entry.Character; CharacterData data = c.data; DrawRect(rect, new Color(.025f, .04f, .043f, .88f)); DrawRect(new Rect(rect.x, rect.y, 4f, rect.height), StateColor(data));
            string state = data.dead ? "DEAD" : data.fullyPassedOut ? "DOWN" : data.zombified ? "ZOMBIE" : "ACTIVE";
            GUI.Label(new Rect(12f, rect.y + 6f, 205f, 22f), entry.Name, _name); GUI.Label(new Rect(218f, rect.y + 7f, 100f, 20f), state + "  " + distance.ToString("0") + "m", _detail);
            float stamina = Mathf.Clamp01(data.TotalStamina); DrawRect(new Rect(12f, rect.y + 33f, 132f, 8f), new Color(.12f, .16f, .16f, 1f)); DrawRect(new Rect(12f, rect.y + 33f, 132f * stamina, 8f), new Color(.26f, .82f, .7f, 1f));
            GUI.Label(new Rect(151f, rect.y + 26f, 168f, 24f), "STA " + Mathf.RoundToInt(stamina * 100f) + "%  " + Conditions(c), _detail);
        }

        private string Conditions(Character c)
        {
            if (c.refs == null || c.refs.afflictions == null) return ""; string result = ""; int count = 0;
            CharacterAfflictions.STATUSTYPE[] types = { CharacterAfflictions.STATUSTYPE.Injury, CharacterAfflictions.STATUSTYPE.Hunger, CharacterAfflictions.STATUSTYPE.Cold, CharacterAfflictions.STATUSTYPE.Poison, CharacterAfflictions.STATUSTYPE.Curse, CharacterAfflictions.STATUSTYPE.Hot, CharacterAfflictions.STATUSTYPE.Spores, CharacterAfflictions.STATUSTYPE.Petrify };
            for (int i = 0; i < types.Length && count < 2; i++) if (c.refs.afflictions.GetCurrentStatus(types[i]) >= .08f) { if (count > 0) result += ", "; result += types[i].ToString(); count++; }
            return result.Length == 0 ? "OK" : result;
        }
        private Color StateColor(CharacterData data) { return data.dead ? new Color(.8f,.18f,.18f,1f) : data.fullyPassedOut ? new Color(1f,.55f,.15f,1f) : data.zombified ? new Color(.5f,.82f,.25f,1f) : new Color(.2f,.75f,.7f,1f); }
        private void EnsureStyles() { if (_pixel != null) return; _pixel = new Texture2D(1,1); _pixel.SetPixel(0,0,Color.white); _pixel.Apply(); _name = new GUIStyle(GUI.skin.label); _name.fontSize=14; _name.fontStyle=FontStyle.Bold; _name.normal.textColor=Color.white; _detail=new GUIStyle(GUI.skin.label);_detail.fontSize=11;_detail.alignment=TextAnchor.MiddleRight;_detail.normal.textColor=new Color(.72f,.8f,.79f); }
        private void DrawRect(Rect rect, Color color) { Color old=GUI.color; GUI.color=color; GUI.DrawTexture(rect,_pixel); GUI.color=old; }
    }
}
