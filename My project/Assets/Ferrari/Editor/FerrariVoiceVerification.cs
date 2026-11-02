using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FerrariLab.Editor
{
    public static class FerrariVoiceVerification
    {
        static bool running;
        static readonly List<string> results = new List<string>();
        static int failures;
        static void Check(bool value, string message)
        { results.Add((value ? "PASS: " : "FAIL: ") + message); if (!value) failures++; }
        [MenuItem("ARVR Prototype/Ferrari/Voice Guide/Verify Voice Guide (Play Mode)")]
        public static void Run()
        {
            var c = UnityEngine.Object.FindAnyObjectByType<FerrariInspectionController>();
            if (!Application.isPlaying || !c || running) throw new InvalidOperationException("Run in FerrariInspection Play Mode, one suite at a time.");
            running = true; failures = 0; results.Clear(); c.StartCoroutine(Verify(c));
        }
        static IEnumerator Verify(FerrariInspectionController c)
        {
            var voice = c.Voice;
            bool muted = voice.Muted; float volume = voice.Volume;
            try
            {
                if (c.Assembly.IsActive) c.Assembly.Exit();
                c.ResetView(); c.SetExploded(false, true); voice.SetMuted(false); voice.SetVolume(.65f);
                Check(voice.CatalogCount == 16, "Catalog contains all 16 part IDs");
                foreach (var p in c.parts)
                    foreach (var kind in new[] { "explanation", "hint" })
                    {
                        var clip = Resources.Load<AudioClip>("FerrariNarration/" + p.partId + "_" + kind);
                        Check(clip && clip.length > 1 && clip.channels == 1, p.partId + " " + kind + " has bundled mono narration");
                    }
                var engine = c.parts.Single(p => p.partId == "engine");
                c.Select(engine); yield return null; yield return null;
                Check(voice.SelectedPart == engine && voice.IsSpeaking, "Inspection selection starts narration");
                Check(voice.Subtitle.Contains(FerrariPartPresentation.Purpose("engine")), "Caption uses the selected engine explanation");
                var speaker = voice.GetComponent<AudioSource>();
                Check(speaker.spatialBlend == 0 && !speaker.loop, "Narrator is consistent in both ears and does not loop");
                var clipBefore = speaker.clip;
                c.Select(c.parts.Single(p => p.partId == "bonnet")); yield return null; yield return null;
                Check(speaker.clip != clipBefore && voice.CurrentClipKey == "bonnet_explanation", "Changing selection replaces the previous clip");
                voice.GiveHint();
                Check(voice.CurrentClipKey == "bonnet_hint" && voice.Subtitle.Contains(FerrariPartPresentation.PlacementHint("bonnet")), "Hint has its own clip and matching captions");
                voice.SetMuted(true);
                Check(!voice.IsSpeaking && voice.Subtitle.Length > 0, "Mute immediately stops audio and preserves captions");
                voice.SetMuted(false); voice.Replay(); yield return null;
                Check(voice.IsSpeaking && voice.CurrentClipKey == "bonnet_hint", "Replay plays the current hint after unmuting");
                var slider = UnityEngine.Object.FindObjectsByType<Slider>().Single(s => s.name == "Narration volume");
                slider.value = .3f;
                Check(Mathf.Abs(voice.Volume - .3f) < .001f && Mathf.Abs(speaker.volume - .3f) < .001f, "UI volume slider changes narrator volume");
                c.Select(null); yield return null; yield return null;
                Check(!voice.IsSpeaking && voice.CurrentClipKey == null, "Deselecting clears stale narration");
                c.Assembly.Enter(); c.Assembly.Restart(); c.Assembly.Preview(engine); yield return null; yield return null;
                Check(voice.SelectedPart == engine && voice.CurrentClipKey == "engine_explanation", "Assembly inventory selection also narrates");
                c.Assembly.CancelDrag(); yield return null; yield return null;
                Check(!voice.IsSpeaking, "Cancelling assembly selection stops narration");
                c.Assembly.Exit(); c.Select(engine); yield return new WaitForSecondsRealtime(.5f);
                var caption = UnityEngine.Object.FindObjectsByType<Text>().Single(t => t.name == "Voice subtitles");
                Check(caption.text == voice.Subtitle, "Visible subtitles match the selected audio text");
                var workspace = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(workspace, "Evidence/Ferrari_Voice_Guide.png"));
            }
            finally
            {
                voice.SetMuted(muted); voice.SetVolume(volume); c.Select(null);
                var path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/voice_verification.txt"));
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllLines(path, new[] { "Voice guide verification | " + DateTime.Now.ToString("s") + " | failures=" + failures }.Concat(results));
                running = false; Debug.Log("Ferrari voice verification: " + results.Count + " checks, " + failures + " failures.");
            }
        }
    }
}
