# Ferrari Exploration Lab — project guide

## What it does

This is a **Unity 6 desktop prototype** for learning the Ferrari Enzo's visible assemblies. The car sits on a rotating plate. Players inspect 16 named assemblies, explode and drag parts, zoom, and read each part's purpose. In **Self Assembly**, they choose from a scrolling list of 15 movable parts, drag them near their saved locations, and watch them snap into position. Doors and covers open temporarily to make interior placement visible. The right panel shows a part image, description, and placement hint.

The bundled voice guide reads prepared explanations and hints without internet. **Zeno AI** can instead listen for “Hey Zeno” or the **ASK ZENO** button, use the pointed or selected part as context, answer through Google Gemini, and speak with a male Windows voice. Captions, mute, volume, Stop Voice, and New Chat are available. **HTC VIVE Focus 3 support is planned; this build has desktop mouse controls and has not been validated on the headset.**

## Open and run

Open `My project` in **Unity 6000.4.11f1**, open `Assets/Ferrari/Scenes/FerrariInspection.unity`, and press Play. The Unity project needs its `Assets`, `Packages`, and `ProjectSettings` folders together. To use Zeno, configure a private Gemini key and start `ZenoService/start.ps1`; see `ZenoService/README.md`. The game and bundled voice guide run without Zeno.

## Files used by the game

| File or group | Purpose |
| --- | --- |
| `My project/Assets/Ferrari/Scenes/FerrariInspection.unity` | Main playable scene: car, turntable, camera, lights, and controller. |
| `My project/Assets/Ferrari/Scenes/FerrariAssetReview.unity` | Separate model and import review scene used during asset editing. |
| `My project/Assets/Ferrari/Models/FerrariEnzo_Prepared.fbx` | Imported car geometry with 16 separate assembly roots. |
| `My project/Assets/Ferrari/Prefabs/FerrariEnzo.prefab` | Reusable Ferrari hierarchy, part components, and colliders used by the scene. |
| `My project/Assets/Ferrari/prepared_manifest.json` | Material data read when rebuilding the imported prefab. |
| `My project/Assets/Ferrari/Materials/*.mat` | Car paint, glass, metal, leather, rubber, lights, carbon, and trim; `BasePlate.mat` and `TurntableTicks.mat` color the display. Keep every material with the prefab and FBX. |
| `My project/Assets/Ferrari/Resources/FerrariPartThumbnails/<part-id>.png` | The 16 images beside part names and in the information panel. IDs are `body`, `bonnet`, `bumper_F`, `bumper_R`, `door_L`, `door_R`, `engine`, `engine_cover`, `rear_wing`, `seat_L`, `seat_R`, `steering_wheel`, and `wheel_FL`, `wheel_FR`, `wheel_RL`, `wheel_RR`. |
| `My project/Assets/Ferrari/Resources/FerrariNarration/catalog.json` | Authored explanation and placement hint for each part; also supplies part context to Zeno. |
| `My project/Assets/Ferrari/Resources/FerrariNarration/<part-id>_explanation.wav` and `<part-id>_hint.wav` | 32 bundled offline voice clips, one explanation and one hint per part. |
| `My project/Assets/InputSystem_Actions.inputactions` | Unity input actions referenced by project settings. |
| `My project/Assets/Settings/*.asset` | Universal Render Pipeline assets for PC/mobile renderers, quality, postprocessing, and global rendering settings. |
| `My project/Packages/manifest.json` and `packages-lock.json` | Unity package dependencies and pinned package versions. |
| `My project/ProjectSettings/*` | Unity editor, input, graphics, audio, scene build list, and project version settings. `EditorBuildSettings.asset` selects the playable scene. |
| Every Unity `.meta` file | Stable asset IDs used by scene, prefab, material, and resource references. Keep each beside its asset. |

The individual material files are `AmberGlass_1`, `Back_M_Metal_1`, `Back_Metal_1`, `Base_1`, `BasePlate`, `black_plastic__matt_1`, `black_plastic_shiny_1`, `black_rubber_1`, `brown_leather_1`, `brushed_metal_1`, `Carpet_v1_10_1`, `Chrome_1`, `ClearGlass_1`, `default`, `DRL_CAR_1`, `Gloss_carbon_1`, `Gold_Matt_1`, `Headlight_1`, `high_gloss_1`, `Mat_1_1` through `Mat_8_1`, `Material`, `Matt_carbon_1`, `Red_Plastic_1`, `RedGlass_1`, `rubber_tire_1`, `Silver_1`, `Tailight_1`, `TintedGlass_1`, and `TurntableTicks` (all `.mat`). Imported names identify source surface finishes; the `Mat_*` files are generic source surfaces. Rendering settings are `DefaultVolumeProfile`, `Mobile_Renderer`, `Mobile_RPAsset`, `PC_Renderer`, `PC_RPAsset`, `SampleSceneProfile`, and `UniversalRenderPipelineGlobalSettings` (all `.asset`).

### Gameplay scripts — `My project/Assets/Ferrari/Scripts/`

| File | Purpose |
| --- | --- |
| `FerrariInspectionController.cs` | Main inspection state, selection, explosion, display rotation, zoom, and mode changes. |
| `FerrariInspectionUI.cs` | Builds and updates the screen panels and buttons. |
| `FerrariLabBackdrop.cs` | Displays the lab backdrop behind the live car and adds illuminated turntable rings. |
| `FerrariPart.cs` | Identifies a selectable car assembly and stores its saved pose. |
| `FerrariPartPresentation.cs` | Part names, educational descriptions, placement hints, and thumbnail lookup. |
| `FerrariAssemblyController.cs` | Drag, target checking, snap animation, completion, and world-pose placement for future VR input. |
| `FerrariAccessAnimator.cs` | Opens and closes access panels while placing interior parts. |
| `FerrariLoopingInventory.cs` | Repeating scroll list of available assembly parts. |
| `FerrariInventoryItem.cs` | Interaction for one inventory entry. |
| `FerrariVoiceGuide.cs` | Offline narration, captions, mute, replay, hints, and volume. |
| `FerrariZenoAssistant.cs` | Zeno session, part context, AI switch, questions, answer captions, and playback. |
| `ZenoMicrophone.cs` | Unity microphone capture option. |
| `ZenoAudio.cs` | WAV encoding, decoding, and sample-rate conversion for Zeno. |

### Unity editor helpers — `My project/Assets/Ferrari/Editor/`

These run in the **Unity Editor only** and are retained for rebuilding assets or checking gameplay. `FerrariAssetBuilder.cs` creates the prefab and review scene; `FerrariInspectionBuilder.cs` creates the playable scene; `FerrariThumbnailBuilder.cs` renders the 16 part images; `FerrariVoiceAuthoring.cs` exports narration text. `FerrariAccessVerification.cs`, `FerrariAssemblyVerification.cs`, `FerrariInspectionVerification.cs`, `FerrariVoiceVerification.cs`, `FerrariWorldPlacementVerification.cs`, `FerrariZenoVerification.cs`, and `ZenoMicrophoneVerification.cs` check their named features and can regenerate evidence or test logs.

## Zeno service — `ZenoService/`

| File or group | Purpose |
| --- | --- |
| `server.py` | Local authenticated API, Gemini questions, answer history, part context, and speech response. |
| `remote_mic.py` | Follows the Windows default microphone, detects speech, and sends captured questions. |
| `whisper_wake.py` | Local Whisper recognition for “Hey Zeno” and similar pronunciations. |
| `audio_tools.py` | Validates and converts microphone audio. |
| `speak.ps1` | Produces reply WAV audio with the male Microsoft David Windows voice. |
| `start.ps1` | Starts the local Python service. |
| `setup.ps1`, `install_whisper.py`, `requirements.txt` | Recreate Python dependencies and the small Whisper wake model on another PC. |
| `configure.ps1` | Private Gemini key and local connection setup. |
| `check_input.py`, `check_bridge.py` | Troubleshoot the selected microphone and Unity connection. |
| `test_server.py`, `test_unity_bridge.py`, `verify_whisper.py`, `verify_live.py` | Service, fixture, wake-model, and optional live-provider checks. `verify_live.py` uses API quota. |
| `README.md` | Full Zeno setup, controls, troubleshooting, and future headset integration notes. |
| `.local/settings.json`, `.venv/`, `models/`, and `My project/zeno.local.json` | Private key/settings, installed Python environment, wake model, and Unity connection settings. They are local and ignored by Git; do not publish the key. |

## Source, evidence, and maintenance

`My project/Assets/Ferrari/Resources/Lab/Background.png` is the supplied empty lab image, used as a lightweight desktop background. The UI uses translucent side panels and a slowly blinking Ferrari title. This background is a flat image; the car and turntable remain interactive 3D objects.

`My project/Assets/Ferrari/Resources/Lab/FerrariLogo.png` is the supplied Ferrari logo. `FerrariLogo.shader` hides its white background at display time, and `FerrariLogo.mat` keeps that shader available in builds. The logo retains the title's slow blink.

`MarbleSurface.shader` and `.mat` draw the dark procedural stone on the turntable. `CyanGlow.shader` and `.mat` draw its luminous rim and a subtle light pool on the floor. The car's existing paint, glass, and metal materials use smoother finishes and clear coat where supported by Unity's Lit shader.

`FerrariSource/Ferrari_Imported.blend` is the imported editable source; `FerrariSource/FerrariEnzo_Prepared.blend` is the regrouped editable car; `FerrariSource/prepared_manifest.json` records the source-to-assembly mapping. `Tools/prepare_ferrari.py` rebuilds the model from Blender, `Tools/validate_ferrari.py` checks the exported hierarchy, `Tools/generate_ferrari_voice.ps1` regenerates bundled narration, and `Tools/ferrari_dev.py` is a local Unity Editor command helper.

`Evidence/*.png` holds demonstration screenshots; `output/documentation/ARVR_50 Percent Milestone Report_230929332.pdf` is the submitted 50% report. The root `README.md` is the quick start. `.gitattributes` configures Git LFS for large assets, and `.gitignore` excludes caches, local keys, and generated files. Unity's `Library`, `Temp`, `Logs`, `UserSettings`, and generated IDE files can be recreated and are not needed in a shared project copy.
