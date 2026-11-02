using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

namespace FerrariLab
{
    [Serializable] public sealed class ZenoSettings
    {
        public string endpoint = "http://127.0.0.1:8765", client_token = "", microphone = "", microphone_backend = "unity";
        public float microphone_threshold = .012f;
    }
    [Serializable] public sealed class ZenoRequest
    {
        public string session, action, part_id, mode, audio, text, device;
    }
    [Serializable] public sealed class ZenoResponse
    {
        public bool ready, wake_ready;
        public float level;
        public string kind, transcript, answer, audio, error, message, part_id;
    }

    public sealed class FerrariZenoAssistant : MonoBehaviour
    {
        public bool IsEnabled { get; private set; }
        public bool IsBusy { get; private set; }
        public bool IsListening => helperMicrophone ? remoteStarted && !suspended && !IsBusy && !IsSpeaking : mic && mic.IsRecording;
        public bool IsSpeaking => speaker && speaker.isPlaying;
        public bool WakeAvailable { get; private set; }
        public string Status { get; private set; } = "Zeno is off. Microphone is off.";
        public string Answer { get; private set; } = "Point at a part and say: Hey Zeno, what does this do?";
        public string Question { get; private set; } = "";
        public string ContextName { get; private set; } = "Whole car";
        public float MicrophoneLevel => helperMicrophone ? remoteLevel : mic ? mic.Level : 0;
        public float MicrophoneThreshold => helperMicrophone ? .001f : mic ? mic.threshold : .012f;
        FerrariInspectionController controller;
        ZenoMicrophone mic;
        AudioSource speaker;
        ZenoSettings settings;
        UnityWebRequest request;
        string session, capturePart = "", captureMode = "inspection";
        FerrariPart xrPointed;
        bool xrPointerActive, armed, push, suspended, hasFocus = true, appPaused;
        bool helperMicrophone, remoteStarted, remoteNeedsResume;
        float remoteLevel;
        float armDeadline;

        public void Initialize(FerrariInspectionController owner)
        {
            controller = owner;
            mic = gameObject.AddComponent<ZenoMicrophone>();
            mic.SpeechStarted += OnSpeechStarted; mic.Utterance += OnUtterance;
            mic.Failed += message => { mic.Stop(); Status = message; };
            speaker = gameObject.AddComponent<AudioSource>(); speaker.playOnAwake = false; speaker.spatialBlend = 0;
            session = Guid.NewGuid().ToString("N");
        }

        public static string SettingsPath
        {
            get
            {
#if UNITY_EDITOR
                return Path.GetFullPath(Path.Combine(Application.dataPath, "../zeno.local.json"));
#else
                return Path.Combine(Application.persistentDataPath, "zeno.local.json");
#endif
            }
        }

        static System.Diagnostics.Process backendProcess;

        public static bool TryAutoStartBackend()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                if (backendProcess != null && !backendProcess.HasExited) return true;

                string workspace = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
                string pythonExe = Path.Combine(workspace, "ZenoService", ".venv", "Scripts", "python.exe");
                string serverPy = Path.Combine(workspace, "ZenoService", "server.py");

                if (!File.Exists(pythonExe) || !File.Exists(serverPy)) return false;

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = "-u \"" + serverPy + "\"",
                    WorkingDirectory = Path.Combine(workspace, "ZenoService"),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };

                backendProcess = System.Diagnostics.Process.Start(startInfo);
                Debug.Log("[Zeno] Auto-started backend process (PID " + (backendProcess != null ? backendProcess.Id : 0) + ").");
                return backendProcess != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Zeno] Failed to auto-start backend: " + ex.Message);
                return false;
            }
#else
            return false;
#endif
        }

        public void Toggle() { if (IsEnabled) Disable(); else StartCoroutine(EnableRoutine()); }
        public void Connect(ZenoSettings connection) { if (!IsEnabled && !IsBusy) StartCoroutine(EnableRoutine(connection)); }

        IEnumerator EnableRoutine(ZenoSettings connection = null)
        {
            if (IsEnabled || IsBusy) yield break;
            try
            {
                settings = connection ?? JsonUtility.FromJson<ZenoSettings>(File.ReadAllText(SettingsPath));
                if (settings == null || string.IsNullOrWhiteSpace(settings.client_token) ||
                    !Uri.TryCreate(settings.endpoint, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != "https" && !(uri.Scheme == "http" && uri.IsLoopback)))
                    throw new FormatException();
            }
            catch (Exception)
            { Status = "Zeno setup is missing. Run ZenoService/setup.ps1 and configure.ps1 first."; yield break; }
            IsEnabled = true; IsBusy = true; suspended = !hasFocus || appPaused; Status = "Connecting to Zeno...";
            controller.Voice.SetSuppressed(true);
            request = UnityWebRequest.Get(settings.endpoint.TrimEnd('/') + "/health");
            request.SetRequestHeader("X-Zeno-Token", settings.client_token); request.timeout = 2;
            yield return request.SendWebRequest();
            var response = ReadResponse();

            if (response == null)
            {
                if (TryAutoStartBackend())
                {
                    Status = "Starting Zeno AI backend service...";
                    float deadline = Time.realtimeSinceStartup + 8f;
                    while (Time.realtimeSinceStartup < deadline)
                    {
                        yield return new WaitForSecondsRealtime(0.5f);
                        request = UnityWebRequest.Get(settings.endpoint.TrimEnd('/') + "/health");
                        request.SetRequestHeader("X-Zeno-Token", settings.client_token); request.timeout = 2;
                        yield return request.SendWebRequest();
                        response = ReadResponse();
                        if (response != null && response.ready) break;
                    }
                }
            }

            if (response == null) { string error = Status; Disable(); Status = !string.IsNullOrEmpty(error) ? error : "Cannot reach Zeno. Start ZenoService/start.ps1 and try again."; yield break; }
            WakeAvailable = response.wake_ready;
            if (!response.ready) { Disable(); Status = "Add a Gemini API key with configure.ps1, then restart Zeno."; yield break; }
            helperMicrophone = settings.microphone_backend == "helper";
            if (helperMicrophone)
            {
                IsBusy = false; Status = "Connecting to Windows microphone...";
                StartCoroutine(RemoteLoop()); yield break;
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
            {
                var callbacks = new UnityEngine.Android.PermissionCallbacks();
                bool completed = false, granted = false;
                callbacks.PermissionGranted += _ => { granted = true; completed = true; };
                callbacks.PermissionDenied += _ => completed = true;
                callbacks.PermissionDeniedAndDontAskAgain += _ => completed = true;
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone, callbacks);
                float deadline = Time.realtimeSinceStartup + 30;
                while (!completed && Time.realtimeSinceStartup < deadline) yield return null;
                if (!granted) { Disable(); Status = "Microphone permission is required for voice questions."; yield break; }
            }
#else
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
#endif
            mic.threshold = Mathf.Clamp(settings.microphone_threshold, .001f, .2f); mic.device = settings.microphone;
            IsBusy = false; ResumeListening();
        }

        public void Disable()
        {
            IsEnabled = false; IsBusy = false; armed = push = suspended = false;
            if (request != null) { request.Abort(); request.Dispose(); request = null; }
            StopAllCoroutines();
            if (remoteStarted && settings != null) StartCoroutine(RemoteStop());
            remoteStarted = remoteNeedsResume = helperMicrophone = false; remoteLevel = 0;
            if (mic) mic.Stop(); StopReply();
            if (controller && controller.Voice) controller.Voice.SetSuppressed(false);
            // A new ID means an old server response can never enter the next conversation.
            session = Guid.NewGuid().ToString("N");
            Status = "Zeno is off. Microphone is off.";
        }

        public void Ask()
        {
            if (!IsEnabled || IsBusy) return;
            suspended = false;
            if (helperMicrophone)
            {
                StopReply(); CaptureContext();
                StartCoroutine(RemoteCommand("ask"));
                Status = "Listening to your question..."; return;
            }
            StopAllCoroutines();
            StopReply(); mic.Stop(); CaptureContext();
            push = armed = true; armDeadline = Time.realtimeSinceStartup + 10;
            Status = "Listening to your question..."; mic.Begin();
        }

        // These methods are the future XR ray hover adapter; never move a part to answer a question.
        public void SetPointedPart(FerrariPart part) { xrPointerActive = true; xrPointed = part; }
        public void ClearPointedPart() { xrPointerActive = false; xrPointed = null; }
        public FerrariPart ResolvePart()
        {
            if (xrPointerActive && xrPointed && Array.IndexOf(controller.parts, xrPointed) >= 0) return xrPointed;
            if (!xrPointerActive && Mouse.current != null && controller.viewCamera &&
                !(EventSystem.current && EventSystem.current.IsPointerOverGameObject()))
            {
                var position = Mouse.current.position.ReadValue();
                if (controller.viewCamera.pixelRect.Contains(position) && Physics.Raycast(controller.viewCamera.ScreenPointToRay(position), out var hit, 100))
                {
                    var part = hit.collider.GetComponentInParent<FerrariPart>();
                    if (part && Array.IndexOf(controller.parts, part) >= 0) return part;
                }
            }
            return controller.Assembly.IsActive ? controller.Assembly.TargetPart : controller.Selected;
        }
        void CaptureContext()
        {
            var part = ResolvePart(); capturePart = part ? part.partId : "";
            ContextName = part ? FerrariPartPresentation.DisplayName(part.partId) : "Whole car / no part selected";
            captureMode = controller.Assembly.IsActive ? "assembly" : controller.IsExploded ? "exploded" : "inspection";
        }
        void OnSpeechStarted()
        {
            if (!armed) CaptureContext();
            Status = armed ? "Listening to your question..." : "Checking for Hey Zeno...";
        }
        void OnUtterance(float[] samples)
        {
            if (!IsEnabled || IsBusy) return;
            mic.Stop();
            string action = push ? "push" : armed ? "question" : "wake";
            armed = push = false;
            StartCoroutine(Send(new ZenoRequest { action = action, audio = Convert.ToBase64String(ZenoAudio.Encode(samples)) }));
        }

        // Also usable by future accessible text input or XR UI; it uses the same context pipeline.
        public void AskText(string text)
        {
            if (!IsEnabled || IsBusy || string.IsNullOrWhiteSpace(text)) return;
            StopAllCoroutines();
            StopReply(); mic.Stop(); CaptureContext(); armed = push = false;
            StartCoroutine(Send(new ZenoRequest { action = "text", text = text }));
        }
        IEnumerator Send(ZenoRequest body)
        {
            IsBusy = true; Status = body.action == "wake" ? "Checking wake phrase locally..." : "Zeno is thinking...";
            body.session = session; body.part_id = capturePart; body.mode = captureMode;
            request = new UnityWebRequest(settings.endpoint.TrimEnd('/') + "/turn", "POST");
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));
            request.downloadHandler = new DownloadHandlerBuffer(); request.timeout = 85;
            request.SetRequestHeader("Content-Type", "application/json"); request.SetRequestHeader("X-Zeno-Token", settings.client_token);
            yield return request.SendWebRequest();
            var response = ReadResponse(); IsBusy = false;
            if (response == null)
            {
                // Explicit recovery avoids silently repeating paid requests after a network failure.
                string error = Status; Disable(); Status = error; yield break;
            }
            if (response.kind == "wake")
            {
                armed = true; push = false; armDeadline = Time.realtimeSinceStartup + 10;
                Status = "Yes? Ask your question."; if (!suspended) mic.Begin(); yield break;
            }
            if (response.kind == "answer")
            {
                Question = response.transcript ?? ""; Answer = response.answer ?? "";
                Status = "Zeno answered" + (string.IsNullOrEmpty(response.message) ? "" : " — " + response.message);
                if (!string.IsNullOrEmpty(response.audio))
                {
                    try { StopReply(); speaker.clip = ZenoAudio.Decode(response.audio); speaker.Play(); Status = "Zeno is speaking"; }
                    catch (Exception error) { Status = "Answer ready. Audio playback failed: " + error.Message; }
                    while (speaker.isPlaying) yield return null;
                    StopReply();
                }
                // Brief echo guard before starting another wake capture.
                yield return new WaitForSecondsRealtime(.45f);
            }
            ResumeListening();
        }
        ZenoResponse ReadResponse()
        {
            ZenoResponse response = null;
            try { response = JsonUtility.FromJson<ZenoResponse>(request.downloadHandler.text); } catch (Exception) { }
            bool ok = request.result == UnityWebRequest.Result.Success && response != null && string.IsNullOrEmpty(response.error);
            if (!ok) Status = !string.IsNullOrEmpty(response?.error) ? response.error : "Cannot reach Zeno. Start ZenoService/start.ps1 and try again.";
            request.Dispose(); request = null;
            return ok ? response : null;
        }
        void ResumeListening()
        {
            if (!IsEnabled) return;
            armed = push = false;
            if (helperMicrophone)
            {
                remoteNeedsResume = true;
                Status = suspended ? "Zeno paused while Unity is in the background" : "Listening on Windows microphone — say Hey Zeno";
                StartCoroutine(RemoteLoop());
                return;
            }
            Status = suspended ? "Zeno paused while Unity is in the background" :
                WakeAvailable ? "Listening locally — say Hey Zeno + your question" : "Wake model unavailable — press Ask Zeno";
            if (WakeAvailable && !suspended) mic.Begin();
        }
        public void StopAnswer()
        {
            if (!IsEnabled || IsBusy) return;
            if (helperMicrophone) { StopReply(); remoteNeedsResume = true; Status = "Listening for Hey Zeno..."; return; }
            StopAllCoroutines(); StopReply(); mic.Stop(); ResumeListening();
        }
        public void ClearConversation()
        {
            if (IsBusy) return;
            session = Guid.NewGuid().ToString("N"); Question = ""; Answer = "New conversation. Point at a part and ask Zeno.";
            if (IsEnabled) { if (helperMicrophone) { remoteStarted = false; remoteNeedsResume = true; StopReply(); } else StopAnswer(); }
        }
        void StopReply()
        {
            if (!speaker) return;
            speaker.Stop(); if (speaker.clip) { Destroy(speaker.clip); speaker.clip = null; }
        }
        void Update()
        {
            if (speaker && controller && controller.Voice)
            { speaker.volume = controller.Voice.Volume; speaker.mute = controller.Voice.Muted; }
            if (armed && !mic.InUtterance && Time.realtimeSinceStartup > armDeadline)
            { mic.Stop(); ResumeListening(); }
        }
        void SetSuspended(bool value)
        {
            if (!IsEnabled || suspended == value) return;
            suspended = value;
            if (value)
            {
                mic.Stop();
                if (helperMicrophone && remoteStarted) { StartCoroutine(RemoteStop()); remoteStarted = false; }
                Status = "Zeno paused while Unity is in the background";
            }
            else if (!IsBusy) { if (helperMicrophone) Status = "Connecting to Windows microphone..."; else ResumeListening(); }
        }
        void OnApplicationFocus(bool focus) { hasFocus = focus; SetSuspended(!hasFocus || appPaused); }
        void OnApplicationPause(bool pause) { appPaused = pause; SetSuspended(!hasFocus || appPaused); }
        void OnDisable() { Disable(); }
        void OnApplicationQuit()
        {
            if (backendProcess != null && !backendProcess.HasExited)
            {
                try { backendProcess.Kill(); } catch { }
                backendProcess = null;
            }
        }

        IEnumerator RemoteCall(ZenoRequest body, Action<ZenoResponse> completed)
        {
            body.session = session;
            using (var call = new UnityWebRequest(settings.endpoint.TrimEnd('/') + "/mic", "POST"))
            {
                call.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));
                call.downloadHandler = new DownloadHandlerBuffer(); call.timeout = 15;
                call.SetRequestHeader("Content-Type", "application/json");
                call.SetRequestHeader("X-Zeno-Token", settings.client_token);
                yield return call.SendWebRequest();
                ZenoResponse answer = null;
                try { answer = JsonUtility.FromJson<ZenoResponse>(call.downloadHandler.text); } catch (Exception) { }
                if (call.result != UnityWebRequest.Result.Success || answer == null)
                    Status = !string.IsNullOrEmpty(answer?.error) ? answer.error :
                        "Microphone helper unavailable. Check ZenoService/server.py and retry.";
                completed?.Invoke(answer);
            }
        }
        IEnumerator RemoteCommand(string action)
        {
            yield return RemoteCall(new ZenoRequest { action = action }, null);
        }
        IEnumerator RemoteStop()
        {
            yield return RemoteCall(new ZenoRequest { action = "stop" }, null);
        }
        IEnumerator RemoteLoop()
        {
            while (IsEnabled && helperMicrophone)
            {
                if (suspended) { yield return new WaitForSecondsRealtime(.2f); continue; }
                if (!remoteStarted)
                {
                    ZenoResponse started = null;
                    yield return RemoteCall(new ZenoRequest { action = "start", device = settings.microphone }, value => started = value);
                    if (!IsEnabled) yield break;
                    remoteStarted = started != null && started.kind == "listening";
                    if (!remoteStarted)
                    {
                        Status = !string.IsNullOrEmpty(started?.error) ? started.error :
                            "Windows microphone unavailable. Check Sound settings and Zeno helper.";
                        yield return new WaitForSecondsRealtime(2); continue;
                    }
                    Status = "Listening on Windows microphone — say Hey Zeno";
                }
                if (remoteNeedsResume)
                {
                    remoteNeedsResume = false;
                    yield return RemoteCall(new ZenoRequest { action = "resume" }, null);
                }
                if (IsBusy || IsSpeaking) { yield return null; continue; }
                var part = ResolvePart();
                ZenoResponse incoming = null;
                yield return RemoteCall(new ZenoRequest { action = "poll", part_id = part ? part.partId : "",
                    mode = controller.Assembly.IsActive ? "assembly" : controller.IsExploded ? "exploded" : "inspection" }, value => incoming = value);
                if (!IsEnabled || incoming == null) { yield return new WaitForSecondsRealtime(.3f); continue; }
                remoteLevel = incoming.level;
                if (incoming.kind == "stopped") { remoteStarted = false; Status = incoming.error ?? "Microphone capture stopped."; continue; }
                if (incoming.kind == "wake") Status = "Yes? Ask your question.";
                if (incoming.kind == "ignored")
                {
                    Question = incoming.transcript ?? "";
                    Answer = "Wake phrase not recognized. Say Hey Zeno clearly, or press ASK ZENO and speak.";
                    Status = "I heard you, but not the wake phrase";
                }
                if (incoming.kind == "error") { Status = incoming.message; remoteNeedsResume = true; }
                if (incoming.kind == "answer")
                {
                    Question = incoming.transcript ?? ""; Answer = incoming.answer ?? "";
                    Status = "Zeno answered"; remoteNeedsResume = true;
                    if (!string.IsNullOrEmpty(incoming.audio))
                    {
                        try { StopReply(); speaker.clip = ZenoAudio.Decode(incoming.audio); speaker.Play(); Status = "Zeno is speaking"; }
                        catch (Exception error) { Status = "Answer ready. Audio playback failed: " + error.Message; }
                        while (speaker.isPlaying) yield return null;
                        StopReply(); yield return new WaitForSecondsRealtime(.45f);
                        Status = suspended ? "Zeno paused while Unity is in the background" : "Listening on Windows microphone — say Hey Zeno";
                    }
                    else Status = "Answer ready. Speech generation failed; check ZenoService.";
                }
                yield return new WaitForSecondsRealtime(.2f);
            }
        }
    }
}
