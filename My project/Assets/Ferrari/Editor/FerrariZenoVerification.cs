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
    public static class FerrariZenoVerification
    {
        [MenuItem("ARVR Prototype/Ferrari/Zeno/Verify Local Network Fixture (Play Mode)")]
        public static void RunNetwork()
        {
            var c = UnityEngine.Object.FindAnyObjectByType<FerrariInspectionController>();
            if (!Application.isPlaying || !c) throw new InvalidOperationException("Open FerrariInspection in Play Mode first.");
            c.StartCoroutine(VerifyNetwork(c));
        }
        static IEnumerator VerifyNetwork(FerrariInspectionController c)
        {
            var results = new List<string>(); int failures = 0;
            Action<bool, string> check = (ok, label) => { results.Add((ok ? "PASS " : "FAIL ") + label); if (!ok) failures++; };
            var ai = c.Zeno;
            var settings = JsonUtility.FromJson<ZenoSettings>(File.ReadAllText(FerrariZenoAssistant.SettingsPath));
            settings.endpoint = "http://127.0.0.1:8766";
            bool muted = c.Voice.Muted;
            try
            {
                ai.Disable(); c.Voice.SetMuted(false); ai.Connect(settings);
                float deadline = Time.realtimeSinceStartup + 10;
                while (ai.IsBusy && Time.realtimeSinceStartup < deadline) yield return null;
                check(ai.IsEnabled && !ai.IsBusy && !ai.IsListening, "Fixture connects without wake capture");
                check(c.Voice.Suppressed, "Online assistant suppresses scripted narration");
                var engine = c.parts.Single(p => p.partId == "engine");
                var bonnet = c.parts.Single(p => p.partId == "bonnet");
                ai.SetPointedPart(engine); ai.AskText("what does this part do?"); ai.SetPointedPart(bonnet);
                deadline = Time.realtimeSinceStartup + 20;
                while (ai.IsBusy && Time.realtimeSinceStartup < deadline) yield return null;
                check(ai.Answer.Contains("Test reply for engine"), "HTTP request freezes engine context before pointer changes");
                check(ai.Question == "what does this part do?", "Question caption arrives through HTTP");
                check(ai.IsSpeaking, "Returned WAV plays in Unity");
                yield return null;
                check(!ai.IsListening, "Microphone remains off during generated reply");
                ai.StopAnswer(); check(!ai.IsSpeaking, "Stop Voice immediately stops reply");
                ai.AskText("why is it important?");
                deadline = Time.realtimeSinceStartup + 20;
                while (ai.IsBusy && Time.realtimeSinceStartup < deadline) yield return null;
                check(ai.Answer.Contains("bonnet") && ai.Answer.Contains("Previous turns: 1"), "Next question uses new part and bounded conversation history");
                ai.ClearConversation(); ai.AskText("new question");
                deadline = Time.realtimeSinceStartup + 20;
                while (ai.IsBusy && Time.realtimeSinceStartup < deadline) yield return null;
                check(ai.Answer.Contains("Previous turns: 0"), "New Chat clears conversation context");
                ai.StopAnswer(); string oldAnswer = ai.Answer;
                ai.AskText("cancel this request"); ai.Disable();
                yield return new WaitForSecondsRealtime(3);
                check(!ai.IsEnabled && !ai.IsBusy && !ai.IsListening && !ai.IsSpeaking && ai.Answer == oldAnswer,
                    "Disable during request rejects late reply and stops all audio");
                check(!c.Voice.Suppressed, "Offline guide restored after cancellation");
            }
            finally
            {
                ai.Disable(); ai.ClearPointedPart(); c.Voice.SetMuted(muted); ai.ClearConversation();
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/zeno_network_verification.txt"));
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllLines(path, new[] { "Zeno local fixture | " + DateTime.Now.ToString("s") + " | failures=" + failures,
                    "Fixture responses only. No live Gemini requests or real microphone speech." }.Concat(results));
                Debug.Log("Zeno network verification: " + results.Count + " checks, " + failures + " failures.");
            }
        }
        [MenuItem("ARVR Prototype/Ferrari/Zeno/Verify Integration (Play Mode)")]
        public static void Run()
        {
            var c = UnityEngine.Object.FindAnyObjectByType<FerrariInspectionController>();
            if (!Application.isPlaying || !c) throw new InvalidOperationException("Open FerrariInspection in Play Mode first.");
            c.StartCoroutine(Verify(c));
        }
        static IEnumerator Verify(FerrariInspectionController c)
        {
            var results = new List<string>(); int failures = 0;
            Action<bool, string> check = (ok, label) => { results.Add((ok ? "PASS " : "FAIL ") + label); if (!ok) failures++; };
            var ai = c.Zeno;
            ai.Disable(); if (c.Assembly.IsActive) c.Assembly.Exit();
            c.Select(null); ai.SetPointedPart(null);
            check(!ai.IsEnabled && !ai.IsListening && !ai.IsSpeaking, "Disabled assistant has no microphone or playback");
            check(ai.ResolvePart() == null, "No selection gives whole-car context");
            var engine = c.parts.Single(p => p.partId == "engine");
            var bonnet = c.parts.Single(p => p.partId == "bonnet");
            c.Select(bonnet); ai.SetPointedPart(engine);
            check(ai.ResolvePart() == engine, "Pointed engine overrides selected bonnet");
            ai.SetPointedPart(null);
            check(ai.ResolvePart() == bonnet, "Missing pointer falls back to selected part");
            foreach (var part in c.parts)
            { ai.SetPointedPart(part); check(ai.ResolvePart() == part, "Context resolves " + part.partId); }
            ai.SetPointedPart(null); c.Assembly.Enter(); c.Assembly.Preview(engine);
            check(ai.ResolvePart() == engine, "Assembly preview provides question context");
            c.Voice.SetSuppressed(true); c.Voice.Present(engine); c.Voice.Replay();
            check(!c.Voice.IsSpeaking, "Scripted narration cannot overlap Zeno");
            ai.Ask(); ai.AskText("what does this do");
            check(!ai.IsBusy && !ai.IsListening, "Ask actions do nothing while AI is off");
            ai.Disable(); check(!c.Voice.Suppressed, "Disabling Zeno restores scripted guide");
            var samples = new float[16000];
            for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(i * .1f) * .4f;
            var encoded = ZenoAudio.Encode(samples);
            var clip = ZenoAudio.Decode(Convert.ToBase64String(encoded));
            check(clip.channels == 1 && clip.frequency == 16000 && Mathf.Abs(clip.length - 1) < .001f, "Audio duration and format survive WAV round trip");
            var decoded = new float[16000]; clip.GetData(decoded, 0);
            check(Mathf.Abs(samples[30] - decoded[30]) < .0001f, "PCM amplitude survives round trip");
            UnityEngine.Object.Destroy(clip);
            bool rejected = false;
            try { ZenoAudio.Decode(Convert.ToBase64String(new byte[12])); } catch (Exception) { rejected = true; }
            check(rejected, "Malformed response audio is rejected");
            yield return null;
            var buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
            check(buttons.Any(b => b.name == "ZENO AI: OFF"), "AI on/off button exists");
            check(buttons.Any(b => b.name == "ASK ZENO"), "Manual voice fallback exists");
            check(buttons.Any(b => b.name == "STOP VOICE"), "Stop speaking control exists");
            check(buttons.Any(b => b.name == "NEW CHAT"), "Conversation reset control exists");
            c.Assembly.Exit(); c.Select(null); ai.ClearPointedPart(); ai.Disable();
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/zeno_verification.txt"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllLines(path, new[] { "Zeno integration | " + DateTime.Now.ToString("s") + " | failures=" + failures,
                "No live Gemini requests or microphone speech are exercised by these checks." }.Concat(results));
            Debug.Log("Zeno verification: " + results.Count + " checks, " + failures + " failures.");
        }
    }
}
