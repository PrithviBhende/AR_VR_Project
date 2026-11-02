# Zeno automotive voice assistant

Zeno connects the existing Ferrari learning scene to Google Gemini. It answers questions about the pointed or selected assembly, why it matters, where it fits, the Enzo, and other cars. Answers use short spoken explanations and matching captions. This is an AI tutor, so uncertain specifications should be checked against an authoritative source.

## Start on this computer

The Python environment and small English Whisper wake model have been installed locally. They are ignored by Git; run setup again on another computer.

1. Create a Gemini API key in [Google AI Studio](https://aistudio.google.com/apikey).
2. From the repository folder, run the private key prompt:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\ZenoService\configure.ps1
   ```

3. Start the helper and leave its terminal running:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\ZenoService\start.ps1
   ```

4. Open `My project/Assets/Ferrari/Scenes/FerrariInspection.unity` in Unity and press Play. Click **ZENO AI: OFF** to enable it. Grant microphone access if requested.
5. Point at a visible part and say **“Hey Zeno, what does this part do?”** as one sentence. Alternatively, say **“Hey Zeno”**, wait for **“Yes? Ask your question”**, and then speak. Finish with a short pause.
6. You can also select a part, click **ASK ZENO**, and speak without the wake phrase. **STOP VOICE** interrupts an answer; **NEW CHAT** clears conversation history. The existing mute/volume setting applies to Zeno too.

The process-level PowerShell flag allows these local scripts to run without changing the computer's persistent execution policy. Never paste the Gemini key into Unity scripts, the chat, screenshots, or a commit. The key is in `ZenoService/.local/settings.json`; the Unity connection token is in `My project/zeno.local.json`. Both paths are ignored by Git. Restart the helper after changing the key or model.

If PowerShell blocks scripts, open `.local/settings.json` in Notepad, paste the key between the empty quotes of `"gemini_api_key": ""`, and save. Preserve the connection token and other settings. From the `ZenoService` folder, start the helper directly with `& ".\.venv\Scripts\python.exe" ".\server.py"`; this does not run a PowerShell script.

## What to try

- Point at the engine: “Hey Zeno, why is this important?”
- Point at the front bonnet: “Hey Zeno, is the engine underneath this?”
- Select a seat in assembly mode: “Hey Zeno, where does this fit?”
- “Hey Zeno, compare the Ferrari Enzo with the Ferrari F40.”
- “Hey Zeno, tell me an interesting fact about this car.”
- After an answer, say “Hey Zeno, explain that more simply.”

Each new question requires the wake phrase or Ask button. The assistant keeps the last four exchanges for follow-up questions. It does not listen while speaking. Point at the part before beginning the wake phrase: the context is captured at the start of that utterance and retained for a two-stage wake/question exchange. Changing the pointer while waiting does not redirect an answer. An explicitly named car or part in the question takes precedence over the pointer in the tutor instructions.

## Small local installation

On another Windows computer with Python installed:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\ZenoService\setup.ps1
```

You can provide `-Python 'C:\path\to\python.exe'`. This checkout was tested with Python 3.12.14. Setup creates a project-local environment, installs the small sound-device dependency, downloads the official whisper.cpp Windows CPU program (about 8.5 MB) and English tiny model (about 75 MB), and generates local connection settings. Windows' installed speech voice speaks answers, so there is no downloaded large language model, GPU requirement, paid speech-generation service, or new Unity package.

The default hosted model is `gemini-3.1-flash-lite`, configurable in the local service settings. A valid API account, internet access, and available quota are required. Free access and billing depend on the account and model; the application does not promise unlimited free usage. The implementation uses Gemini's documented audio input and JSON response support through its REST Generate Content endpoint, without adding a Gemini SDK to Unity.

## Recognition and data flow

Unity microphone → short mono 16 kHz WAV → local helper → local wake detection → Gemini (only an activated question plus part context and short history) → text answer → Windows speech → Unity audio and captions.

Unity did not list the WH-1000XM5 microphone, so this computer uses the local helper backend. The ignored `My project/zeno.local.json` selects `"microphone_backend": "helper"` and `"microphone": ""`. An empty microphone name follows the Windows default recording device, whether that is the laptop microphone or a connected headset selected as default. The helper reconnects when Windows changes its default input. Unity continues to send the current pointed part while listening, and the helper fixes that context when speech begins. The on-screen microphone status reads **hearing you**, **quiet**, or **no sound detected**.

- AI starts **off** every Play session. Turning it off releases the microphone, stops playback, aborts the client request, and starts a fresh conversation ID next time. Losing Unity focus pauses microphone capture; a reply already playing may finish. Regaining focus resumes listening while the AI switch remains on.
- While enabled, short speech segments go to the helper on **this computer** for local wake detection. Non-wake speech is not sent to Gemini. Wake-only utterances arm one follow-up question; otherwise the question is included with the wake phrase.
- Supported wake spellings include Zeno, Zeeno, Zino, Jeeno and Zzno, with phonetic recognition variants. Whisper's small English model recognized the first four in generated-speech checks. Recognition depends on the speaker, microphone, and noise; the Ask button is the fallback.
- Capture ends after approximately 0.85 seconds of silence, with a maximum of about 14 seconds per utterance. If you pause for longer mid-question, it may be treated as a complete question.
- Unity records at the microphone's supported rate (including 48 kHz and 44.1 kHz) and converts each utterance to mono 16 kHz for the helper. Normal recording-buffer wraps are not treated as capture failures.
- The mic is stopped during processing and playback to avoid the assistant hearing itself. Losing focus or pausing the app pauses capture until Unity is active again.
- No user recordings are written by Unity. The helper does not log transcripts or keys. It retains up to four exchanges per session in memory, expires idle sessions after 30 minutes, and uses a temporary WAV for synthesized output that is deleted after returning it. New Chat uses a fresh session; old server memory expires automatically.
- Disabling cannot retract a request already received by Gemini. A late response is ignored by Unity. Google processes activated questions under the API account's applicable data terms.
- The helper accepts a private connection token and binds to loopback only. It is a single-user development service, not a public deployment. It limits each session to eight answer requests per minute and performs one request at a time.

## Part context and factual boundaries

Mouse ray hover takes priority over the selected inspection/assembly part. If neither exists, Zeno receives whole-car context and is instructed to clarify ambiguous references. Part IDs are resolved against the existing authored narration catalog on the helper. It knows the front bonnet differs from the rear engine cover, and that the car is a visual teaching model rather than a functional mechanical simulation. General comparisons use Gemini's knowledge; there is no live web search or guaranteed current price/specification lookup.

## Troubleshooting

- **Cannot reach Zeno:** run `start.ps1`; check it reports port 8765. Connection-token settings must match; do not replace one file independently.
- **Add a Gemini API key:** run `configure.ps1`, then stop/restart the helper.
- **Quota/access/model error:** check the Gemini account and model access. No automatic paid retries are performed.
- **No microphone:** enable microphone access for desktop apps in Windows settings. For the helper backend, `microphone` is matched against the Windows input name. For the Unity backend, an empty `microphone` uses the first Unity input and an exact Unity device name selects another input.
- **Missed speech:** try Ask Zeno. Reduce `microphone_threshold` from `0.012` toward `0.006` for a quiet mic; raise it for background noise. Toggle Zeno off/on after changes. Use headphones and speak clearly.
- **Captions but no sound:** unmute the voice control and check volume/output device. Windows speech failure preserves text answers.

## Later HTC VIVE Focus 3 integration

The current implementation is a **Windows desktop prototype**, not a verified standalone headset deployment. The Unity capture/network/audio code includes Android microphone permission handling. A future XR ray interactor should call `SetPointedPart(part)` on hover and `ClearPointedPart()` when the ray is removed, and connect spatial buttons to `Toggle`, `Ask`, `StopAnswer`, and `ClearConversation`.

For Focus 3, a secure reachable helper endpoint, headset configuration, controller wiring, lifecycle testing, and spatial UI are still required. Loopback on a headset refers to the headset itself; it will not reach this PC. The current helper uses Windows speech, so it needs a Windows host or a different speech adapter if deployed elsewhere. Do not ship provider keys in the Android build. The desktop JSON is read from the Unity project root; built players read `zeno.local.json` from `Application.persistentDataPath`. The client allows HTTPS endpoints and loopback HTTP only.

## Verification

```powershell
.\ZenoService\.venv\Scripts\python.exe -m unittest discover -s ZenoService -v
.\ZenoService\.venv\Scripts\python.exe ZenoService\verify_whisper.py
```

Unity menus under **ARVR Prototype → Ferrari → Zeno** provide integration checks. The network check requires the explicit `test_unity_bridge.py` fixture on port 8766. It uses clearly labelled synthetic replies and makes no Gemini calls; do not confuse it with the real helper. Test reports are in the ignored `Logs` folder. Real microphone wake accuracy, live Gemini answer quality/latency, and headset testing must be recorded separately.

## Implementation sources

- [Gemini audio input](https://ai.google.dev/gemini-api/docs/generate-content/audio)
- [Gemini Generate Content REST API](https://ai.google.dev/api/generate-content)
- [Gemini 3.1 Flash-Lite](https://ai.google.dev/gemini-api/docs/models/gemini-3.1-flash-lite)
- [whisper.cpp](https://github.com/ggml-org/whisper.cpp) and its [CPU release](https://github.com/ggml-org/whisper.cpp/releases/tag/b5454)
- [Official Whisper model downloads](https://github.com/ggml-org/whisper.cpp/blob/master/models/README.md)
