using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Networking;

namespace PeakTrollMod
{
    internal sealed class AudioManager
    {
        private sealed class Playing { public GameObject Object; public float DestroyAt; public int TargetActor; }
        private sealed class VoiceState { public AudioSource Source; public float Volume; }
        private readonly ManualLogSource _log;
        private readonly List<AudioClip> _clips = new List<AudioClip>();
        private readonly List<Playing> _playing = new List<Playing>();
        private readonly Dictionary<int, VoiceState> _voiceStates = new Dictionary<int, VoiceState>();
        private readonly FieldInfo _voiceAudioSource = ReflectionHelpers.Field(typeof(CharacterVoiceHandler), "<audioSource>k__BackingField");
        private readonly FieldInfo _voiceSourceFallback = ReflectionHelpers.Field(typeof(CharacterVoiceHandler), "m_source");
        private AudioClip _menuClickClip;
        private AudioSource _menuClickSource;
        private float _nextRefresh;
        public IList<AudioClip> Clips { get { return _clips.AsReadOnly(); } }
        public AudioManager(ManualLogSource log, BaseUnityPlugin host) { _log = log; Refresh(); host.StartCoroutine(LoadMenuClick()); }

        private IEnumerator LoadMenuClick()
        {
            string path = Path.Combine(Path.GetDirectoryName(typeof(TrollModPlugin).Assembly.Location), "Audio", "menu-click.mp3");
            if (!File.Exists(path)) { _log.LogWarning("Menu click sound was not found at " + path); yield break; }
            string url = new Uri(path).AbsoluteUri;
            UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG);
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                _log.LogWarning("Menu click sound could not be loaded: " + request.error);
                request.Dispose();
                yield break;
            }
            _menuClickClip = DownloadHandlerAudioClip.GetContent(request);
            request.Dispose();
            if (_menuClickClip == null) { _log.LogWarning("Menu click sound decoded to an empty clip."); yield break; }
            _menuClickClip.name = "PTM_MenuClick";
            GameObject go = new GameObject("PTM_MenuClickAudio");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _menuClickSource = go.AddComponent<AudioSource>();
            _menuClickSource.playOnAwake = false;
            _menuClickSource.spatialBlend = 0f;
            _menuClickSource.ignoreListenerPause = true;
        }

        public void PlayMenuClick()
        {
            if (_menuClickSource != null && _menuClickClip != null) _menuClickSource.PlayOneShot(_menuClickClip, .65f);
        }

        public void Tick()
        {
            if (Time.unscaledTime >= _nextRefresh) { _nextRefresh = Time.unscaledTime + 30f; Refresh(); }
            for (int i = _playing.Count - 1; i >= 0; i--) if (_playing[i].Object == null || Time.unscaledTime >= _playing[i].DestroyAt) { if (_playing[i].Object != null) UnityEngine.Object.Destroy(_playing[i].Object); _playing.RemoveAt(i); }
        }

        private void Refresh()
        {
            AudioClip[] clips = Resources.FindObjectsOfTypeAll<AudioClip>();
            _clips.Clear(); HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < clips.Length; i++) if (clips[i] != null && !string.IsNullOrEmpty(clips[i].name) && names.Add(clips[i].name)) _clips.Add(clips[i]);
            _clips.Sort(delegate(AudioClip a, AudioClip b) { return string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase); });
        }

        public ActionResult PlayDiscovered(string clipName, Vector3 position, float volume, int targetActor)
        {
            AudioClip clip = null;
            for (int i = 0; i < _clips.Count; i++) if (string.Equals(_clips[i].name, clipName, StringComparison.OrdinalIgnoreCase)) { clip = _clips[i]; break; }
            if (clip == null) return ActionResult.Fail("Audio clip is not loaded in the current scene.");
            int cap = Mathf.Clamp(TrollModPlugin.Instance.Settings.MaxSpawnedObjects.Value, 1, 64);
            if (_playing.Count >= cap) { if (_playing[0].Object != null) UnityEngine.Object.Destroy(_playing[0].Object); _playing.RemoveAt(0); }
            GameObject go = new GameObject("PTM_FakeAudio_" + clip.name);
            go.transform.position = position;
            AudioSource source = go.AddComponent<AudioSource>(); source.clip = clip; source.volume = Mathf.Clamp01(volume); source.spatialBlend = 1f; source.rolloffMode = AudioRolloffMode.Linear; source.maxDistance = 35f; source.Play();
            _playing.Add(new Playing { Object = go, DestroyAt = Time.unscaledTime + Mathf.Max(1f, clip.length + .25f), TargetActor = targetActor });
            return ActionResult.Ok("Played " + clip.name + ".");
        }

        public AudioClip FindAssociated(string token)
        {
            for (int i = 0; i < _clips.Count; i++) if (_clips[i].name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) return _clips[i];
            return null;
        }

        public void StopAll() { for (int i = 0; i < _playing.Count; i++) if (_playing[i].Object != null) UnityEngine.Object.Destroy(_playing[i].Object); _playing.Clear(); }
        public void StopForTarget(int actor) { for (int i=_playing.Count-1;i>=0;i--) if(_playing[i].TargetActor==actor){if(_playing[i].Object!=null)UnityEngine.Object.Destroy(_playing[i].Object);_playing.RemoveAt(i);} }

        public ActionResult SetIncomingVoiceMuted(PlayerEntry target, bool muted)
        {
            if (target == null || target.Character == null || target.Character.refs == null || target.Character.refs.voice == null) return ActionResult.Fail("Voice source is not ready.");
            AudioSource source = GetVoiceSource(target.Character.refs.voice); if (source == null) return ActionResult.Fail("Incoming Photon Voice source is not ready.");
            VoiceState state;
            if (muted)
            {
                if (!_voiceStates.TryGetValue(target.ActorNumber, out state)) { state = new VoiceState(); state.Source = source; state.Volume = source.volume; _voiceStates.Add(target.ActorNumber, state); }
                source.volume = 0f; return ActionResult.Ok("Muted " + target.Name + " on this client. Their mod is not required.");
            }
            if (_voiceStates.TryGetValue(target.ActorNumber, out state)) { if (state.Source != null) state.Source.volume = state.Volume; _voiceStates.Remove(target.ActorNumber); }
            return ActionResult.Ok("Restored incoming voice for " + target.Name + ".");
        }

        public ActionResult SetIncomingVoicePreference(PlayerEntry target, float volume, bool muted)
        {
            if (target == null || target.Character == null || target.Character.refs == null || target.Character.refs.voice == null) return ActionResult.Fail("Voice source is not ready.");
            AudioSource source = GetVoiceSource(target.Character.refs.voice); if (source == null) return ActionResult.Fail("Incoming Photon Voice source is not ready.");
            VoiceState state;
            if (!_voiceStates.TryGetValue(target.ActorNumber, out state) || state.Source != source)
            {
                state = new VoiceState(); state.Source = source; state.Volume = source.volume; _voiceStates[target.ActorNumber] = state;
            }
            source.volume = muted ? 0f : Mathf.Clamp01(volume);
            return ActionResult.Ok("Applied saved voice preference for " + target.Name + ".");
        }

        public void RestoreVoice(int actor) { VoiceState state;if(_voiceStates.TryGetValue(actor,out state)){if(state.Source!=null)state.Source.volume=state.Volume;_voiceStates.Remove(actor);} }
        public void RestoreAllVoice() { foreach(KeyValuePair<int,VoiceState> pair in _voiceStates)if(pair.Value.Source!=null)pair.Value.Source.volume=pair.Value.Volume;_voiceStates.Clear(); }
        public void Dispose() { if (_menuClickSource != null) UnityEngine.Object.Destroy(_menuClickSource.gameObject); if (_menuClickClip != null) UnityEngine.Object.Destroy(_menuClickClip); _menuClickSource = null; _menuClickClip = null; }
        private AudioSource GetVoiceSource(CharacterVoiceHandler voice) { AudioSource source=_voiceAudioSource==null?null:_voiceAudioSource.GetValue(voice) as AudioSource;return source!=null?source:(_voiceSourceFallback==null?null:_voiceSourceFallback.GetValue(voice) as AudioSource); }
    }
}
