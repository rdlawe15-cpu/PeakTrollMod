using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace PeakTrollMod
{
    internal sealed class ProfileManager
    {
        private const string Prefix = "PTM1.";
        private readonly ModConfig _settings;

        public ProfileManager(ModConfig settings) { _settings = settings; }

        public string ActiveName { get { return string.IsNullOrEmpty(_settings.ActiveProfileName.Value) ? "Custom" : _settings.ActiveProfileName.Value; } }
        public string SavedCode { get { return _settings.SavedProfileCode.Value ?? string.Empty; } }

        public ActionResult ApplyPreset(string name)
        {
            if (name != "Vanilla+" && name != "Accessibility" && name != "Large Lobby" && name != "Practice") return ActionResult.Fail("Unknown profile preset.");
            ApplySafeBaseline();
            if (name == "Vanilla+")
            {
                _settings.ScoutCompassEnabled.Value = true;
            }
            else if (name == "Accessibility")
            {
                _settings.ScoutCompassEnabled.Value = true; _settings.HighContrast.Value = true; _settings.TextScale.Value = 1.15f;
                _settings.CameraShakeScale.Value = 0f; _settings.ReduceFlashingEffects.Value = true; _settings.TeamStatusDistance.Value = 90f;
            }
            else if (name == "Large Lobby")
            {
                _settings.ScoutCompassEnabled.Value = true; _settings.TeamStatusDistance.Value = 120f;
                _settings.UnlimitedLobbyEnabled.Value = true; _settings.UnlimitedLobbyMaxPlayers.Value = 12;
                _settings.UnlimitedLobbyScaleSupplies.Value = true; _settings.UnlimitedLobbyExtraFood.Value = true; _settings.UnlimitedLobbyExtraBackpacks.Value = true;
            }
            else if (name == "Practice")
            {
                _settings.ScoutCompassEnabled.Value = true; _settings.RealLuggageDirectionsEnabled.Value = true; _settings.LuggageEspEnabled.Value = true;
                _settings.ImmortalityEnabled.Value = true; _settings.InfiniteStaminaEnabled.Value = true; _settings.InfiniteJetpackFuelEnabled.Value = true;
                _settings.InfiniteRescueClawReachEnabled.Value = true;
            }
            _settings.ActiveProfileName.Value = name;
            _settings.SavedProfileCode.Value = Encode(name);
            return ActionResult.Ok(name + " profile applied.");
        }

        public ActionResult SaveCurrent(string name)
        {
            name = CleanName(name);
            _settings.ActiveProfileName.Value = name;
            _settings.SavedProfileCode.Value = Encode(name);
            return ActionResult.Ok("Saved current settings as " + name + ".");
        }

        public ActionResult ImportAndApply(string code)
        {
            string name, payload;
            if (!TryDecode(code, out name, out payload)) return ActionResult.Fail("That profile code is invalid or from an unsupported format.");
            try
            {
                string[] pairs = payload.Split(';');
                for (int i = 0; i < pairs.Length; i++)
                {
                    int split = pairs[i].IndexOf('='); if (split <= 0) continue;
                    ApplyValue(pairs[i].Substring(0, split), pairs[i].Substring(split + 1));
                }
                _settings.ActiveProfileName.Value = name; _settings.SavedProfileCode.Value = code.Trim();
                return ActionResult.Ok("Applied profile " + name + ".");
            }
            catch { return ActionResult.Fail("The profile was valid but contained an unsupported setting value."); }
        }

        public bool ValidateCode(string code, out string name)
        {
            string payload; return TryDecode(code, out name, out payload);
        }

        private void ApplySafeBaseline()
        {
            _settings.NoWaitEnabled.Value=true; _settings.TeamStatusEnabled.Value=true; _settings.QuickBackpackEnabled.Value=true; _settings.BetterSpectatingEnabled.Value=true;
            _settings.StaminaEffectPreviewEnabled.Value=true; _settings.ClimbForecastEnabled.Value=true; _settings.PartySupplyAdvisorEnabled.Value=true; _settings.HotkeyConflictDoctorEnabled.Value=true;
            _settings.HighContrast.Value=false; _settings.TextScale.Value=1f; _settings.CameraShakeScale.Value=1f; _settings.ReduceFlashingEffects.Value=false; _settings.TeamStatusDistance.Value=45f;
            _settings.ScoutCompassEnabled.Value=false; _settings.ScoutEspEnabled.Value=false; _settings.LuggageEspEnabled.Value=false; _settings.ZombieEspEnabled.Value=false; _settings.ScoutmasterEspEnabled.Value=false;
            _settings.EspLabelsEnabled.Value=true; _settings.EspTracersEnabled.Value=false; _settings.RealLuggageDirectionsEnabled.Value=false;
            _settings.ImmortalityEnabled.Value=false; _settings.InfiniteStaminaEnabled.Value=false; _settings.InfiniteJetpackFuelEnabled.Value=false; _settings.InfiniteRescueClawReachEnabled.Value=false;
            _settings.UnlimitedLobbyEnabled.Value=false;
        }

        private string Encode(string name)
        {
            StringBuilder b = new StringBuilder(); b.Append("name=").Append(Uri.EscapeDataString(CleanName(name)));
            Add(b,"nw",_settings.NoWaitEnabled.Value);Add(b,"ts",_settings.TeamStatusEnabled.Value);Add(b,"td",_settings.TeamStatusDistance.Value);Add(b,"qb",_settings.QuickBackpackEnabled.Value);Add(b,"bs",_settings.BetterSpectatingEnabled.Value);
            Add(b,"ep",_settings.StaminaEffectPreviewEnabled.Value);Add(b,"cf",_settings.ClimbForecastEnabled.Value);Add(b,"ps",_settings.PartySupplyAdvisorEnabled.Value);Add(b,"hk",_settings.HotkeyConflictDoctorEnabled.Value);
            Add(b,"hc",_settings.HighContrast.Value);Add(b,"tx",_settings.TextScale.Value);Add(b,"cs",_settings.CameraShakeScale.Value);Add(b,"rf",_settings.ReduceFlashingEffects.Value);
            Add(b,"sc",_settings.ScoutCompassEnabled.Value);Add(b,"se",_settings.ScoutEspEnabled.Value);Add(b,"le",_settings.LuggageEspEnabled.Value);Add(b,"ze",_settings.ZombieEspEnabled.Value);Add(b,"me",_settings.ScoutmasterEspEnabled.Value);Add(b,"el",_settings.EspLabelsEnabled.Value);Add(b,"et",_settings.EspTracersEnabled.Value);Add(b,"ed",_settings.EspMaximumDistance.Value);Add(b,"ew",_settings.EspOutlineWidth.Value);
            Add(b,"rd",_settings.RealLuggageDirectionsEnabled.Value);Add(b,"im",_settings.ImmortalityEnabled.Value);Add(b,"is",_settings.InfiniteStaminaEnabled.Value);Add(b,"jf",_settings.InfiniteJetpackFuelEnabled.Value);Add(b,"rc",_settings.InfiniteRescueClawReachEnabled.Value);
            Add(b,"ul",_settings.UnlimitedLobbyEnabled.Value);Add(b,"um",_settings.UnlimitedLobbyMaxPlayers.Value);Add(b,"us",_settings.UnlimitedLobbyScaleSupplies.Value);Add(b,"uf",_settings.UnlimitedLobbyExtraFood.Value);Add(b,"ub",_settings.UnlimitedLobbyExtraBackpacks.Value);
            return Prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(b.ToString()));
        }

        private static void Add(StringBuilder b,string key,bool value){b.Append(';').Append(key).Append('=').Append(value?'1':'0');}
        private static void Add(StringBuilder b,string key,int value){b.Append(';').Append(key).Append('=').Append(value.ToString(CultureInfo.InvariantCulture));}
        private static void Add(StringBuilder b,string key,float value){b.Append(';').Append(key).Append('=').Append(value.ToString("0.###",CultureInfo.InvariantCulture));}

        private bool TryDecode(string code, out string name, out string payload)
        {
            name=string.Empty;payload=string.Empty;if(string.IsNullOrEmpty(code))return false;code=code.Trim();if(code.Length>4096||!code.StartsWith(Prefix,StringComparison.Ordinal))return false;
            try{byte[] bytes=Convert.FromBase64String(code.Substring(Prefix.Length));payload=Encoding.UTF8.GetString(bytes);if(payload.Length>3000||!payload.StartsWith("name=",StringComparison.Ordinal))return false;int end=payload.IndexOf(';');string raw=end<0?payload.Substring(5):payload.Substring(5,end-5);name=CleanName(Uri.UnescapeDataString(raw));return !string.IsNullOrEmpty(name);}catch{return false;}
        }

        private void ApplyValue(string key,string value)
        {
            bool flag=value=="1";float number;int whole;
            float.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out number);int.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out whole);
            if(key=="nw")_settings.NoWaitEnabled.Value=flag;else if(key=="ts")_settings.TeamStatusEnabled.Value=flag;else if(key=="td")_settings.TeamStatusDistance.Value=Mathf.Clamp(number,10f,200f);else if(key=="qb")_settings.QuickBackpackEnabled.Value=flag;else if(key=="bs")_settings.BetterSpectatingEnabled.Value=flag;
            else if(key=="ep")_settings.StaminaEffectPreviewEnabled.Value=flag;else if(key=="cf")_settings.ClimbForecastEnabled.Value=flag;else if(key=="ps")_settings.PartySupplyAdvisorEnabled.Value=flag;else if(key=="hk")_settings.HotkeyConflictDoctorEnabled.Value=flag;
            else if(key=="hc")_settings.HighContrast.Value=flag;else if(key=="tx")_settings.TextScale.Value=Mathf.Clamp(number,.85f,1.3f);else if(key=="cs")_settings.CameraShakeScale.Value=Mathf.Clamp01(number);else if(key=="rf")_settings.ReduceFlashingEffects.Value=flag;
            else if(key=="sc")_settings.ScoutCompassEnabled.Value=flag;else if(key=="se")_settings.ScoutEspEnabled.Value=flag;else if(key=="le")_settings.LuggageEspEnabled.Value=flag;else if(key=="ze")_settings.ZombieEspEnabled.Value=flag;else if(key=="me")_settings.ScoutmasterEspEnabled.Value=flag;else if(key=="el")_settings.EspLabelsEnabled.Value=flag;else if(key=="et")_settings.EspTracersEnabled.Value=flag;else if(key=="ed")_settings.EspMaximumDistance.Value=Mathf.Clamp(number,25f,2000f);else if(key=="ew")_settings.EspOutlineWidth.Value=Mathf.Clamp(number,1f,8f);
            else if(key=="rd")_settings.RealLuggageDirectionsEnabled.Value=flag;else if(key=="im")_settings.ImmortalityEnabled.Value=flag;else if(key=="is")_settings.InfiniteStaminaEnabled.Value=flag;else if(key=="jf")_settings.InfiniteJetpackFuelEnabled.Value=flag;else if(key=="rc")_settings.InfiniteRescueClawReachEnabled.Value=flag;
            else if(key=="ul")_settings.UnlimitedLobbyEnabled.Value=flag;else if(key=="um")_settings.UnlimitedLobbyMaxPlayers.Value=Mathf.Clamp(whole,4,30);else if(key=="us")_settings.UnlimitedLobbyScaleSupplies.Value=flag;else if(key=="uf")_settings.UnlimitedLobbyExtraFood.Value=flag;else if(key=="ub")_settings.UnlimitedLobbyExtraBackpacks.Value=flag;
        }

        private static string CleanName(string value)
        {
            if(string.IsNullOrEmpty(value))return "Custom";value=value.Trim();if(value.Length>32)value=value.Substring(0,32);return value.Replace("\r"," ").Replace("\n"," ");
        }
    }
}
