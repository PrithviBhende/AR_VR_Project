using System;
using System.Collections.Generic;
using UnityEngine;

namespace FerrariLab
{
    [Serializable] public sealed class FerrariVoiceEntry
    {
        public string id, explanation, hint;
    }
    [Serializable] public sealed class FerrariVoiceCatalog
    {
        public FerrariVoiceEntry[] entries;
    }

    // One narrator for both input modes; audio and subtitles share the same authored catalog.
    public sealed class FerrariVoiceGuide : MonoBehaviour
    {
        const string ResourceFolder = "FerrariNarration/";
        readonly Dictionary<string, FerrariVoiceEntry> entries = new Dictionary<string, FerrariVoiceEntry>();
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        FerrariInspectionController controller;
        AudioSource speaker;
        FerrariPart selected;
        bool hintSelected;
        public bool Suppressed { get; private set; }
        public void SetSuppressed(bool value) { Suppressed = value; if (value && speaker) speaker.Stop(); }
        public string Subtitle { get; private set; } = "Select a part to hear its explanation.";
        public string AudioStatus { get; private set; } = "Ready";
        public bool Muted { get; private set; }
        public float Volume { get; private set; }
        public bool IsSpeaking => speaker && speaker.isPlaying;
        public FerrariPart SelectedPart => selected;
        public string CurrentClipKey { get; private set; }
        public int CatalogCount { get { EnsureCatalogLoaded(); return entries.Count; } }

        void EnsureCatalogLoaded()
        {
            if (entries.Count > 0) return;
            var asset = Resources.Load<TextAsset>(ResourceFolder + "catalog");
            if (asset)
            {
                var catalog = JsonUtility.FromJson<FerrariVoiceCatalog>(asset.text);
                if (catalog?.entries != null)
                    foreach (var entry in catalog.entries) entries[entry.id] = entry;
            }
        }

        public void Initialize(FerrariInspectionController owner)
        {
            controller = owner;
            speaker = GetComponent<AudioSource>();
            if (!speaker) speaker = gameObject.AddComponent<AudioSource>();
            speaker.playOnAwake = false; speaker.loop = false; speaker.spatialBlend = 0;
            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat("Ferrari.Voice.Volume", .75f));
            Muted = PlayerPrefs.GetInt("Ferrari.Voice.Muted", 0) == 1;
            speaker.volume = Volume; speaker.mute = Muted;
            EnsureCatalogLoaded();
        }
        void Update()
        {
            var part = controller.Assembly.IsActive ? controller.Assembly.TargetPart : controller.Selected;
            if (part != selected) Present(part);
        }
        public void Present(FerrariPart part)
        {
            speaker.Stop(); selected = part; hintSelected = false;
            if (!part)
            {
                Subtitle = "Select a part to hear its explanation.";
                AudioStatus = Muted ? "Muted" : "Ready"; CurrentClipKey = null; return;
            }
            Speak(false);
        }
        public void Replay() { if (selected) Speak(hintSelected); }
        public void Explain() { if (selected) Speak(false); }
        public void GiveHint() { if (selected) Speak(true); }
        void Speak(bool hint)
        {
            EnsureCatalogLoaded();
            if (!speaker) speaker = GetComponent<AudioSource>();
            if (speaker) speaker.Stop(); hintSelected = hint;
            entries.TryGetValue(selected.partId, out var entry);
            Subtitle = entry != null ? (hint ? entry.hint : entry.explanation)
                : selected.displayName + ". " + (hint ? FerrariPartPresentation.PlacementHint(selected.partId) : FerrariPartPresentation.Purpose(selected.partId));
            CurrentClipKey = selected.partId + (hint ? "_hint" : "_explanation");
            if (!clips.TryGetValue(CurrentClipKey, out var clip))
            { clip = Resources.Load<AudioClip>(ResourceFolder + CurrentClipKey); clips[CurrentClipKey] = clip; }
            if (speaker) speaker.clip = clip;
            if (!clip) { AudioStatus = "Text only - audio unavailable"; return; }
            AudioStatus = Muted ? "Muted - captions available" : "Voice guide";
            if (!Muted && !Suppressed && speaker) speaker.Play();
        }
        public void SetMuted(bool muted)
        {
            if (!speaker) speaker = GetComponent<AudioSource>();
            Muted = muted; if (speaker) speaker.mute = muted;
            if (muted && speaker) speaker.Stop();
            AudioStatus = muted ? "Muted - captions available" : "Ready - press replay";
            PlayerPrefs.SetInt("Ferrari.Voice.Muted", muted ? 1 : 0); PlayerPrefs.Save();
        }
        public void SetVolume(float volume)
        {
            if (!speaker) speaker = GetComponent<AudioSource>();
            Volume = Mathf.Clamp01(volume); if (speaker) speaker.volume = Volume;
            PlayerPrefs.SetFloat("Ferrari.Voice.Volume", Volume);
        }
        void OnApplicationPause(bool paused) { if (paused && speaker) speaker.Stop(); }
        void OnApplicationFocus(bool focused) { if (!focused && speaker) speaker.Stop(); }
        void OnDisable() { if (speaker) speaker.Stop(); PlayerPrefs.Save(); }
    }
}
