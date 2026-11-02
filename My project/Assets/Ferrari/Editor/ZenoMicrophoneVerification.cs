using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FerrariLab.Editor
{
    public static class ZenoMicrophoneVerification
    {
        [MenuItem("ARVR Prototype/Ferrari/Zeno/Verify Local Microphone (Play Mode)")]
        public static void Run()
        {
            var owner = UnityEngine.Object.FindAnyObjectByType<FerrariInspectionController>();
            if (!Application.isPlaying || !owner) throw new InvalidOperationException("Use FerrariInspection Play Mode.");
            owner.Zeno.Disable(); owner.StartCoroutine(Verify());
        }
        static IEnumerator Verify()
        {
            var results = new List<string>(); int failures = 0;
            Action<bool, string> check = (ok, message) => { results.Add((ok ? "PASS " : "FAIL ") + message); if (!ok) failures++; };
            foreach (int rate in new[] { 16000, 44100, 48000 })
            {
                var input = new float[rate];
                for (int i = 0; i < input.Length; i++) input[i] = .25f;
                var output = ZenoAudio.ResampleMono(input, rate);
                check(output.Length == 16000, rate + " Hz converts to exactly one second at 16 kHz");
                check(Mathf.Abs(output[0] - .25f) < .0001f && Mathf.Abs(output[15999] - .25f) < .0001f,
                    rate + " Hz conversion preserves level and endpoint samples");
                for (int i = 0; i < input.Length; i++) input[i] = Mathf.Sin(2 * Mathf.PI * 1000 * i / rate) * .5f;
                output = ZenoAudio.ResampleMono(input, rate);
                int crossings = 0;
                for (int i = 1; i < output.Length; i++) if (output[i - 1] <= 0 && output[i] > 0) crossings++;
                check(Math.Abs(crossings - 1000) <= 1, rate + " Hz conversion preserves 1 kHz voice-band pitch");
            }
            var go = new GameObject("Zeno local microphone test");
            var mic = go.AddComponent<ZenoMicrophone>();
            string error = ""; mic.Failed += message => error = message;
            try
            {
                bool started = mic.Begin(); check(started, "Capture starts at a supported device rate");
                results.Add("Device: " + mic.ActiveDevice + " | capture rate: " + mic.CaptureRate);
                float deadline = Time.realtimeSinceStartup + 9, peak = 0;
                int last = 0, advances = 0, wraps = 0;
                while (started && mic.IsRecording && Time.realtimeSinceStartup < deadline)
                {
                    int position = Microphone.GetPosition(mic.ActiveDevice);
                    if (position != last) advances++;
                    if (position >= 0 && position < last) wraps++;
                    last = position; peak = Mathf.Max(peak, mic.Level);
                    yield return null;
                }
                check(mic.IsRecording && string.IsNullOrEmpty(error), "Capture stays active for nine seconds without false permission error");
                check(advances > 10, "Device delivers advancing audio samples");
                check(wraps >= 2, "Multiple normal buffer wraps do not stop capture");
                results.Add("Progress observations: " + advances + " | wraps: " + wraps + " | peak RMS: " + peak.ToString("F4"));
                if (error.Length > 0) results.Add("Capture error: " + error);
                mic.Stop(); check(!mic.IsRecording, "Stop releases microphone capture");
            }
            finally
            {
                mic.Stop(); UnityEngine.Object.Destroy(go);
                results.Insert(0, "Local microphone verification | " + DateTime.Now.ToString("s") + " | failures=" + failures);
                results.Insert(1, "No speech content saved or sent to Gemini. Only rates, levels and sample progress logged.");
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/zeno_microphone_verification.txt"));
                Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllLines(path, results);
                Debug.Log("Zeno microphone verification finished with " + failures + " failures.");
            }
        }
    }
}
