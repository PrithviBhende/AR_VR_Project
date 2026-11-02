using System;
using System.Collections.Generic;
using UnityEngine;

namespace FerrariLab
{
    public sealed class ZenoMicrophone : MonoBehaviour
    {
        public event Action SpeechStarted;
        public event Action<float[]> Utterance;
        public event Action<string> Failed;
        public float threshold = .012f;
        public string device = "";
        public bool IsRecording => clip != null;
        public bool InUtterance => speaking;
        public float Level { get; private set; }
        public string ActiveDevice { get; private set; }
        public int CaptureRate { get; private set; }
        const int Rate = 16000, PreRoll = 6400, MaxSamples = 224000;
        AudioClip clip;
        int readPosition, silentSamples, voicedSamples;
        bool speaking;
        float lastPoll, lastAdvanceAt;
        int lastPosition, frameSamples, preRollSamples, maxSpeechSamples;
        readonly Queue<float> pre = new Queue<float>(PreRoll);
        readonly List<float> speech = new List<float>(MaxSamples + PreRoll);
        float[] buffer;

        public bool Begin()
        {
            Stop();
            if (Microphone.devices.Length == 0) { Failed?.Invoke("No microphone found. Connect a microphone and enable Zeno again."); return false; }
            if (!string.IsNullOrEmpty(device) && Array.IndexOf(Microphone.devices, device) < 0)
            { Failed?.Invoke("Configured microphone not found. Check zeno.local.json."); return false; }
            ActiveDevice = string.IsNullOrEmpty(device) ? Microphone.devices[0] : device;
            Microphone.GetDeviceCaps(ActiveDevice, out int minimumRate, out int maximumRate);
            // Many Windows drivers expose only 48 kHz or 44.1 kHz. Capture in
            // their supported format; the service still receives mono 16 kHz.
            int requestedRate = maximumRate > 0 ? Mathf.Clamp(Rate, Mathf.Max(1, minimumRate), maximumRate) : Rate;
            try { clip = Microphone.Start(ActiveDevice, true, 2, requestedRate); }
            catch (Exception) { Failed?.Invoke("Microphone could not start. Check system microphone permission."); return false; }
            if (!clip) { Failed?.Invoke("Microphone unavailable. Check system microphone permission."); return false; }
            CaptureRate = clip.frequency;
            frameSamples = Mathf.Max(1, CaptureRate / 50);
            preRollSamples = CaptureRate * 2 / 5; maxSpeechSamples = CaptureRate * 14;
            buffer = new float[frameSamples * clip.channels]; readPosition = lastPosition = 0;
            lastPoll = lastAdvanceAt = Time.realtimeSinceStartup; return true;
        }

        public void Stop()
        {
            if (clip)
            {
                Microphone.End(ActiveDevice);
                Destroy(clip); clip = null;
            }
            pre.Clear(); speech.Clear(); speaking = false; silentSamples = voicedSamples = 0; Level = 0;
        }

        void Update()
        {
            if (!clip) return;
            float now = Time.realtimeSinceStartup;
            int position = Microphone.GetPosition(ActiveDevice);
            // Zero is a normal position when the looping buffer wraps. Only a
            // sustained lack of progress indicates a stalled capture device.
            if (position >= 0 && position != lastPosition) { lastPosition = position; lastAdvanceAt = now; }
            if (now - lastAdvanceAt > 5)
            { Stop(); Failed?.Invoke("Microphone is not delivering audio. Check the Windows input device, privacy settings, or another app using it exclusively."); return; }
            if (position < 0) return;
            if (now - lastPoll > 1.8f)
            { readPosition = position; pre.Clear(); speech.Clear(); speaking = false; }
            lastPoll = now;
            int available = (position - readPosition + clip.samples) % clip.samples;
            while (clip && available >= frameSamples)
            {
                clip.GetData(buffer, readPosition); readPosition = (readPosition + frameSamples) % clip.samples; available -= frameSamples;
                float sum = 0;
                for (int i = 0; i < frameSamples; i++)
                {
                    float sample = 0;
                    for (int c = 0; c < clip.channels; c++) sample += buffer[i * clip.channels + c];
                    sample /= clip.channels; buffer[i] = sample; sum += sample * sample;
                }
                Level = Mathf.Sqrt(sum / frameSamples);
                bool loud = Level >= threshold;
                if (!speaking && loud)
                {
                    speaking = true; speech.AddRange(pre); pre.Clear(); silentSamples = voicedSamples = 0;
                    SpeechStarted?.Invoke();
                }
                if (speaking)
                {
                    for (int i = 0; i < frameSamples; i++) speech.Add(buffer[i]);
                    silentSamples = loud ? 0 : silentSamples + frameSamples;
                    if (loud) voicedSamples += frameSamples;
                    if (silentSamples >= CaptureRate * .85f || speech.Count >= maxSpeechSamples)
                    {
                        var samples = voicedSamples >= CaptureRate * .18f ? ZenoAudio.ResampleMono(speech.ToArray(), CaptureRate, Rate) : null;
                        speaking = false; speech.Clear(); pre.Clear();
                        if (samples != null) { Utterance?.Invoke(samples); return; }
                    }
                }
                else
                    for (int i = 0; i < frameSamples; i++) { if (pre.Count >= preRollSamples) pre.Dequeue(); pre.Enqueue(buffer[i]); }
            }
        }
        void OnDisable() { Stop(); }
    }
}
