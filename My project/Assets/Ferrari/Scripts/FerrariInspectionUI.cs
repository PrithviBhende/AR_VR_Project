using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace FerrariLab
{
    public sealed class FerrariInspectionUI : MonoBehaviour
    {
        public FerrariInspectionController controller;
        Font font, titleFont;
        RawImage ferrariLogo;
        readonly System.Collections.Generic.Dictionary<FerrariPart, Image> inspectionRows = new System.Collections.Generic.Dictionary<FerrariPart, Image>();
        Text nameText, description, status, mode, autoText, modeToggleText, listTitle, hint, targetText;
        RectTransform inspectionList, assemblyList, targetMarker, infoPanel;
        RawImage detailThumbnail;
        Text placementHint;
        Text voiceSubtitle, voiceMuteText;
        RawImage voiceMuteIcon;
        Texture2D soundIconTex, muteIconTex;
        public static Sprite glassPanelSprite;
        public static Sprite glassCardDefaultSprite, glassCardSelectedSprite, glassCardHoverSprite;
        public static Sprite btnOutlineOrangeSprite, btnOutlineCyanSprite, btnOutlineGraySprite;
        Text zenoToggleText, zenoStatus, zenoAnswer, zenoContext, zenoMute;
        RectTransform voicePanel, zenoPanel;
        Button zenoAsk;
        Button aiToggleBtn, modeToggleBtn, zenoAskBtn, zenoStopBtn, zenoMuteBtn, zenoNewChatBtn;
        Slider volumeSlider;
        float lastZenoSpeechSignal;
        float infoAnimProgress;
        readonly System.Collections.Generic.List<GameObject> inspectionControls = new System.Collections.Generic.List<GameObject>();
        readonly System.Collections.Generic.List<GameObject> assemblyControls = new System.Collections.Generic.List<GameObject>();
        readonly Color ink = new Color(.90f, .94f, .98f), cyan = new Color(.18f, .8f, .95f);
        RectTransform canvas;

        void EnsureSliderListener()
        {
            if (volumeSlider == null)
            {
                var s = GameObject.Find("Narration volume");
                if (s != null) volumeSlider = s.GetComponent<Slider>();
            }
            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
                volumeSlider.onValueChanged.AddListener(OnSliderValueChanged);
            }
        }
        void OnSliderValueChanged(float val)
        {
            if (controller != null && controller.Voice != null)
                controller.Voice.SetVolume(val);
        }

        void EnsureAllButtonListeners()
        {
            EnsureSliderListener();
            if (aiToggleBtn == null)
            {
                var go = GameObject.Find("ZENO AI: OFF") ?? GameObject.Find("ZENO AI: ON");
                if (go != null) aiToggleBtn = go.GetComponent<Button>();
            }
            if (aiToggleBtn != null)
            {
                aiToggleBtn.onClick.RemoveListener(OnAiToggleClicked);
                aiToggleBtn.onClick.AddListener(OnAiToggleClicked);
            }
            if (modeToggleBtn == null)
            {
                var go = GameObject.Find("SELF ASSEMBLY");
                if (go != null) modeToggleBtn = go.GetComponent<Button>();
            }
            if (modeToggleBtn != null)
            {
                modeToggleBtn.onClick.RemoveListener(OnModeToggleClicked);
                modeToggleBtn.onClick.AddListener(OnModeToggleClicked);
            }
            if (zenoPanel != null)
            {
                if (zenoAskBtn == null)
                {
                    var tr = zenoPanel.Find("ASK ZENO");
                    if (tr != null) zenoAskBtn = tr.GetComponent<Button>();
                }
                if (zenoAskBtn != null)
                {
                    zenoAskBtn.onClick.RemoveListener(OnZenoAskClicked);
                    zenoAskBtn.onClick.AddListener(OnZenoAskClicked);
                }
                if (zenoStopBtn == null)
                {
                    var tr = zenoPanel.Find("STOP VOICE");
                    if (tr != null) zenoStopBtn = tr.GetComponent<Button>();
                }
                if (zenoStopBtn != null)
                {
                    zenoStopBtn.onClick.RemoveListener(OnZenoStopClicked);
                    zenoStopBtn.onClick.AddListener(OnZenoStopClicked);
                }
                if (zenoMuteBtn == null)
                {
                    var tr = zenoPanel.Find("MUTE");
                    if (tr != null) zenoMuteBtn = tr.GetComponent<Button>();
                }
                if (zenoMuteBtn != null)
                {
                    zenoMuteBtn.onClick.RemoveListener(OnZenoMuteClicked);
                    zenoMuteBtn.onClick.AddListener(OnZenoMuteClicked);
                }
                if (zenoNewChatBtn == null)
                {
                    var tr = zenoPanel.Find("NEW CHAT");
                    if (tr != null) zenoNewChatBtn = tr.GetComponent<Button>();
                }
                if (zenoNewChatBtn != null)
                {
                    zenoNewChatBtn.onClick.RemoveListener(OnZenoNewChatClicked);
                    zenoNewChatBtn.onClick.AddListener(OnZenoNewChatClicked);
                }
            }
        }

        void OnAiToggleClicked()
        {
            if (controller != null && controller.Zeno != null)
                controller.Zeno.Toggle();
        }
        void OnModeToggleClicked()
        {
            if (controller != null && controller.Assembly != null)
                controller.Assembly.ToggleMode();
        }
        void OnZenoAskClicked()
        {
            if (controller != null && controller.Zeno != null)
                controller.Zeno.Ask();
        }
        void OnZenoStopClicked()
        {
            if (controller != null && controller.Zeno != null)
                controller.Zeno.StopAnswer();
        }
        void OnZenoMuteClicked()
        {
            if (controller != null && controller.Voice != null)
                controller.Voice.SetMuted(!controller.Voice.Muted);
        }
        void OnZenoNewChatClicked()
        {
            if (controller != null && controller.Zeno != null)
                controller.Zeno.ClearConversation();
        }

        void Start()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var michroma = Resources.Load<Font>("Fonts/Michroma");
            titleFont = michroma != null ? michroma : font;

            glassPanelSprite = Load9SliceAsset("Lab/glass_panel_9slice", new Vector4(28, 28, 28, 28));
            glassCardDefaultSprite = Load9SliceAsset("Lab/glass_card_default_9slice", new Vector4(18, 18, 18, 18));
            glassCardSelectedSprite = Load9SliceAsset("Lab/glass_card_selected_9slice", new Vector4(18, 18, 18, 18));
            glassCardHoverSprite = Load9SliceAsset("Lab/glass_card_hover_9slice", new Vector4(18, 18, 18, 18));
            btnOutlineOrangeSprite = Load9SliceAsset("Lab/btn_outline_orange_9slice", new Vector4(16, 16, 16, 16));
            btnOutlineCyanSprite = Load9SliceAsset("Lab/btn_outline_cyan_9slice", new Vector4(16, 16, 16, 16));
            btnOutlineGraySprite = Load9SliceAsset("Lab/btn_outline_gray_9slice", new Vector4(16, 16, 16, 16));

            var go = new GameObject("Ferrari Inspection UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = (RectTransform)go.transform;
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440, 900);
            scaler.matchWidthOrHeight = .5f;

            if (!EventSystem.current)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            var top = Panel("Header", canvas, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -128), Vector2.zero, Color.clear);
            top.GetComponent<Image>().raycastTarget = false;

            var logoObject = new GameObject("Ferrari logo", typeof(RectTransform), typeof(RawImage));
            var logoRect = (RectTransform)logoObject.transform;
            logoRect.SetParent(top, false);
            logoRect.anchorMin = logoRect.anchorMax = new Vector2(.5f, 1);
            logoRect.pivot = new Vector2(.5f, 1);
            logoRect.anchoredPosition = new Vector2(0, -1);
            logoRect.sizeDelta = new Vector2(400, 105);
            ferrariLogo = logoObject.GetComponent<RawImage>();
            ferrariLogo.texture = Resources.Load<Texture2D>("Lab/FerrariLogo");
            ferrariLogo.material = Resources.Load<Material>("Lab/FerrariLogo");
            ferrariLogo.uvRect = new Rect(.06f, .34f, .89f, .48f);
            ferrariLogo.raycastTarget = false;

            var subtitle = Label(top, "EXPLORATION LAB", 22, new Vector2(-300, -105), new Vector2(600, 30), cyan);
            subtitle.font = titleFont;
            subtitle.rectTransform.anchorMin = subtitle.rectTransform.anchorMax = new Vector2(.5f, 1);
            subtitle.alignment = TextAnchor.MiddleCenter;
            subtitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            subtitle.verticalOverflow = VerticalWrapMode.Overflow;

            var toggle = Button(top, "SELF ASSEMBLY", new Vector2(-236, -36), new Vector2(214, 44), OnModeToggleClicked, new Color(.045f, .43f, .57f));
            ((RectTransform)toggle.transform).anchorMin = ((RectTransform)toggle.transform).anchorMax = Vector2.one;
            modeToggleText = toggle.GetComponentInChildren<Text>();
            modeToggleBtn = toggle;

            var aiToggle = Button(top, "ZENO AI: OFF", new Vector2(-394, -36), new Vector2(146, 44), OnAiToggleClicked, new Color(.12f, .20f, .26f));
            ((RectTransform)aiToggle.transform).anchorMin = ((RectTransform)aiToggle.transform).anchorMax = Vector2.one;
            zenoToggleText = aiToggle.GetComponentInChildren<Text>();
            aiToggleBtn = aiToggle;

            // Left Part Information Panel (slides/pops in from left edge when part selected)
            var info = Panel("Information", canvas, new Vector2(0f, .30f), new Vector2(0f, .81f), Vector2.zero, Vector2.zero, Color.white);
            info.pivot = new Vector2(0f, 0.5f);
            info.sizeDelta = new Vector2(332f, 0f);
            info.anchoredPosition = new Vector2(-360f, 0f);
            infoPanel = info;
            infoPanel.gameObject.SetActive(false);
            var infoIm = info.GetComponent<Image>();
            infoIm.sprite = glassPanelSprite;
            infoIm.type = Image.Type.Sliced;
            infoIm.color = Color.white;
            Label(info, "PART INFORMATION", 19, new Vector2(22, -20), new Vector2(290, 28), ink);
            nameText = Label(info, "Choose a part", 25, new Vector2(22, -65), new Vector2(290, 62), cyan);
            description = Label(info, "", 18, new Vector2(22, -244), new Vector2(288, 96), ink);
            detailThumbnail = Thumbnail(info, null, new Vector2(88, -127), new Vector2(156, 110));
            placementHint = Label(info, "", 16, new Vector2(22, -354), new Vector2(288, 105), cyan);
            placementHint.gameObject.name = "Placement clue";

            var right = Panel("Assemblies", canvas, new Vector2(.765f, .15f), new Vector2(.985f, .81f), Vector2.zero, Vector2.zero, Color.white);
            var rightIm = right.GetComponent<Image>();
            rightIm.sprite = glassPanelSprite;
            rightIm.type = Image.Type.Sliced;
            rightIm.color = Color.white;
            listTitle = Label(right, "ASSEMBLIES  /  SCROLL", 19, new Vector2(22, -20), new Vector2(285, 30), ink);
            var viewport = Panel("Part list", right, Vector2.zero, Vector2.one, new Vector2(10, 14), new Vector2(-10, -66), Color.clear);
            viewport.GetComponent<Image>().raycastTarget = false;
            viewport.gameObject.AddComponent<RectMask2D>();
            inspectionList = viewport;

            var content = new GameObject("Items", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = new Vector2(0, controller.parts.Length * 68 + 8);
            for (int i = 0; i < controller.parts.Length; i++)
            {
                var part = controller.parts[i];
                Vector2 rowPos = new Vector2(6, -6 - i * 68);
                Vector2 rowSize = new Vector2(286, 62);
                var button = Button(content, part.displayName, rowPos, rowSize, () => controller.Select(part), new Color(.075f, .115f, .145f));
                var label = button.GetComponentInChildren<Text>();
                label.fontSize = 18;
                label.alignment = TextAnchor.MiddleLeft;
                label.rectTransform.anchoredPosition = new Vector2(88, 0);
                label.rectTransform.sizeDelta = new Vector2(192, 62);
                var rowIm = button.GetComponent<Image>();
                rowIm.sprite = glassCardDefaultSprite;
                rowIm.type = Image.Type.Sliced;
                rowIm.color = Color.white;
                inspectionRows[part] = rowIm;
                Thumbnail((RectTransform)button.transform, FerrariPartPresentation.Thumbnail(part.partId), new Vector2(10, -6), new Vector2(70, 50));
                var hover = button.gameObject.AddComponent<FerrariCardHover>();
                hover.Setup(part, controller, rowPos, rowSize);
            }

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28;

            assemblyList = Panel("Looping assembly inventory", right, new Vector2(0, 0), new Vector2(1, 1), new Vector2(10, 14), new Vector2(-10, -66), Color.clear);
            assemblyList.GetComponent<Image>().raycastTarget = false;
            assemblyList.gameObject.AddComponent<RectMask2D>();
            var inventory = assemblyList.gameObject.AddComponent<FerrariLoopingInventory>();
            inventory.assembly = controller.Assembly;
            inventory.font = font;
            assemblyList.gameObject.SetActive(false);

            // Sleek floating bottom controls bar (no black background box)
            var bottom = Panel("Controls", canvas, new Vector2(.08f, 0), new Vector2(.92f, 0), new Vector2(0, 8), new Vector2(0, 68), Color.clear);
            bottom.GetComponent<Image>().color = Color.clear;
            bottom.GetComponent<Image>().raycastTarget = false;
            inspectionControls.Add(Button(bottom, "EXPLODE  [E]", new Vector2(22, -12), new Vector2(175, 44), () => controller.SetExploded(true), new Color(.79f, .23f, .10f)).gameObject);
            inspectionControls.Add(Button(bottom, "ASSEMBLE  [A]", new Vector2(207, -12), new Vector2(175, 44), () => controller.SetExploded(false), new Color(.045f, .43f, .57f)).gameObject);
            inspectionControls.Add(Button(bottom, "RESET LAYOUT", new Vector2(392, -12), new Vector2(175, 44), controller.ResetLayout, new Color(.12f, .20f, .26f)).gameObject);
            assemblyControls.Add(Button(bottom, "RESTART GAME", new Vector2(22, -12), new Vector2(175, 44), controller.Assembly.Restart, new Color(.79f, .23f, .10f)).gameObject);
            assemblyControls.Add(Button(bottom, "CANCEL  [ESC]", new Vector2(207, -12), new Vector2(175, 44), controller.Assembly.CancelDrag, new Color(.12f, .20f, .26f)).gameObject);
            assemblyControls.Add(Button(bottom, "INSPECTION", new Vector2(392, -12), new Vector2(175, 44), controller.Assembly.Exit, new Color(.045f, .43f, .57f)).gameObject);
            Button(bottom, "TURN <", new Vector2(587, -12), new Vector2(105, 44), () => controller.Turn(30), new Color(.12f, .20f, .26f));
            Button(bottom, "TURN >", new Vector2(702, -12), new Vector2(105, 44), () => controller.Turn(-30), new Color(.12f, .20f, .26f));
            autoText = Button(bottom, "AUTO: OFF", new Vector2(817, -12), new Vector2(140, 44), () => controller.AutoRotate = !controller.AutoRotate, new Color(.12f, .20f, .26f)).GetComponentInChildren<Text>();
            inspectionControls.Add(autoText.transform.parent.gameObject);
            inspectionControls.Add(Button(bottom, "RESET ALL  [R]", new Vector2(977, -12), new Vector2(188, 44), controller.ResetView, new Color(.12f, .20f, .26f)).gameObject);

            // Deactivated status / hint labels (removed yellow circled text)
            mode = Label(bottom, "", 17, Vector2.zero, Vector2.zero, cyan);
            mode.gameObject.SetActive(false);
            status = Label(bottom, "", 17, Vector2.zero, Vector2.zero, ink);
            status.gameObject.SetActive(false);
            hint = Label(canvas, "", 16, Vector2.zero, Vector2.zero, new Color(.64f, .73f, .8f));
            hint.gameObject.SetActive(false);

            // Voice Guide panel - floating transparent controls without outer box
            voicePanel = Panel("Voice guide", canvas, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero, Color.clear);
            voicePanel.pivot = new Vector2(0.5f, 0f);
            voicePanel.anchoredPosition = new Vector2(0, 142);
            voicePanel.sizeDelta = new Vector2(364, 64);

            var replayTex = Resources.Load<Texture2D>("Lab/icon_replay");
            var hintTex = Resources.Load<Texture2D>("Lab/icon_hint");
            muteIconTex = Resources.Load<Texture2D>("Lab/icon_mute");
            soundIconTex = Resources.Load<Texture2D>("Lab/icon_sound");

            float btnSize = 58f;
            IconButton(voicePanel, "REPLAY", new Vector2(8, -3), new Vector2(btnSize, btnSize), replayTex, controller.Voice.Replay, out _);
            IconButton(voicePanel, "PART HINT", new Vector2(78, -3), new Vector2(btnSize, btnSize), hintTex, controller.Voice.GiveHint, out _);
            var muteBtn = IconButton(voicePanel, "MUTE", new Vector2(148, -3), new Vector2(btnSize, btnSize), soundIconTex, () => controller.Voice.SetMuted(!controller.Voice.Muted), out voiceMuteIcon);
            voiceMuteText = Label((RectTransform)muteBtn.transform, "", 1, Vector2.zero, Vector2.zero, Color.clear);

            // Audio volume slider: pure white, thin white stick rail + active white stick fill + circular white handle knob (no black background)
            var volumeBar = Panel("Narration volume", voicePanel, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero, Color.clear);
            volumeBar.pivot = new Vector2(0f, 0.5f);
            volumeBar.anchoredPosition = new Vector2(224, -32);
            volumeBar.sizeDelta = new Vector2(130, 24);

            var railObj = new GameObject("Rail", typeof(RectTransform), typeof(Image));
            var railRt = (RectTransform)railObj.transform;
            railRt.SetParent(volumeBar, false);
            railRt.anchorMin = new Vector2(0f, 0.5f);
            railRt.anchorMax = new Vector2(1f, 0.5f);
            railRt.pivot = new Vector2(0.5f, 0.5f);
            railRt.anchoredPosition = Vector2.zero;
            railRt.sizeDelta = new Vector2(0, 4f);
            var railImage = railObj.GetComponent<Image>();
            railImage.color = new Color(1f, 1f, 1f, 0.35f);
            railImage.raycastTarget = false;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            var fillAreaRt = (RectTransform)fillArea.transform;
            fillAreaRt.SetParent(volumeBar, false);
            fillAreaRt.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRt.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRt.pivot = new Vector2(0.5f, 0.5f);
            fillAreaRt.anchoredPosition = Vector2.zero;
            fillAreaRt.sizeDelta = new Vector2(-22f, 4f);

            var fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            var fillRt = (RectTransform)fillObj.transform;
            fillRt.SetParent(fillAreaRt, false);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.anchoredPosition = Vector2.zero;
            fillRt.sizeDelta = Vector2.zero;
            var fillImage = fillObj.GetComponent<Image>();
            fillImage.color = new Color(1f, 1f, 1f, 0.95f);
            fillImage.raycastTarget = false;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            var handleAreaRt = (RectTransform)handleArea.transform;
            handleAreaRt.SetParent(volumeBar, false);
            handleAreaRt.anchorMin = new Vector2(0f, 0.5f);
            handleAreaRt.anchorMax = new Vector2(1f, 0.5f);
            handleAreaRt.pivot = new Vector2(0.5f, 0.5f);
            handleAreaRt.anchoredPosition = Vector2.zero;
            handleAreaRt.sizeDelta = new Vector2(-22f, 22f);

            var handleObj = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            var handleRt = (RectTransform)handleObj.transform;
            handleRt.SetParent(handleAreaRt, false);
            handleRt.anchorMin = new Vector2(0.5f, 0.5f);
            handleRt.anchorMax = new Vector2(0.5f, 0.5f);
            handleRt.pivot = new Vector2(0.5f, 0.5f);
            handleRt.anchoredPosition = Vector2.zero;
            handleRt.sizeDelta = new Vector2(22f, 22f);
            var handleHit = handleObj.GetComponent<Image>();
            handleHit.color = Color.clear;

            var knobObj = new GameObject("Knob", typeof(RectTransform), typeof(Image));
            var knobRt = (RectTransform)knobObj.transform;
            knobRt.SetParent(handleRt, false);
            knobRt.anchorMin = knobRt.anchorMax = knobRt.pivot = new Vector2(0.5f, 0.5f);
            knobRt.anchoredPosition = Vector2.zero;
            knobRt.sizeDelta = new Vector2(22f, 22f);
            var knobImage = knobObj.GetComponent<Image>();
            var circleTex = Resources.Load<Texture2D>("Lab/slider_circle_handle");
            if (circleTex != null)
            {
                knobImage.sprite = Sprite.Create(circleTex, new Rect(0, 0, circleTex.width, circleTex.height), new Vector2(0.5f, 0.5f));
            }
            knobImage.color = Color.white;
            knobImage.raycastTarget = false;

            volumeSlider = volumeBar.gameObject.AddComponent<Slider>();
            volumeSlider.fillRect = fillRt;
            volumeSlider.handleRect = handleRt;
            volumeSlider.targetGraphic = knobImage;
            volumeSlider.direction = Slider.Direction.LeftToRight;
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            var sliderColors = volumeSlider.colors;
            sliderColors.normalColor = Color.white;
            sliderColors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            sliderColors.pressedColor = new Color(0.85f, 0.95f, 1f, 1f);
            volumeSlider.colors = sliderColors;
            volumeSlider.SetValueWithoutNotify(controller.Voice.Volume);
            volumeSlider.onValueChanged.AddListener(OnSliderValueChanged);

            // Zeno AI toolbar - sleek glass panel matching the left/right inspection blocks
            zenoPanel = Panel("Zeno conversation", canvas, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero, Color.white);
            zenoPanel.pivot = new Vector2(0.5f, 0f);
            zenoPanel.anchoredPosition = new Vector2(0, 136);
            zenoPanel.sizeDelta = new Vector2(740, 96);

            var zenoIm = zenoPanel.GetComponent<Image>();
            zenoIm.sprite = glassPanelSprite;
            zenoIm.type = Image.Type.Sliced;
            zenoIm.color = Color.white;
            zenoIm.raycastTarget = false;

            var zenoTitle = Label(zenoPanel, "ZENO / AI", 16, new Vector2(26, -18), new Vector2(110, 28), cyan);
            zenoTitle.font = titleFont;

            zenoAsk = Button(zenoPanel, "ASK ZENO", new Vector2(148, -14), new Vector2(116, 34), OnZenoAskClicked, new Color(.045f, .43f, .57f));
            zenoAskBtn = zenoAsk;
            zenoStopBtn = Button(zenoPanel, "STOP VOICE", new Vector2(276, -14), new Vector2(124, 34), OnZenoStopClicked, new Color(.12f, .20f, .26f));
            zenoMuteBtn = Button(zenoPanel, "MUTE", new Vector2(412, -14), new Vector2(96, 34), OnZenoMuteClicked, new Color(.12f, .20f, .26f));
            zenoMute = zenoMuteBtn.GetComponentInChildren<Text>();
            zenoNewChatBtn = Button(zenoPanel, "NEW CHAT", new Vector2(520, -14), new Vector2(118, 34), OnZenoNewChatClicked, new Color(.12f, .20f, .26f));

            zenoStatus = Label(zenoPanel, "", 15, new Vector2(26, -56), new Vector2(390, 26), cyan);
            zenoContext = Label(zenoPanel, "", 13, new Vector2(424, -56), new Vector2(288, 26), ink);
            zenoPanel.gameObject.SetActive(false);

            // Standalone Movie Caption (for both Voice Guide part explanations & Zeno AI responses)
            var captionObj = new GameObject("Voice subtitles", typeof(RectTransform), typeof(Text));
            var captionRt = (RectTransform)captionObj.transform;
            captionRt.SetParent(canvas, false);
            captionRt.anchorMin = new Vector2(0.5f, 0f);
            captionRt.anchorMax = new Vector2(0.5f, 0f);
            captionRt.pivot = new Vector2(0.5f, 0f);
            captionRt.anchoredPosition = new Vector2(0, 80);
            captionRt.sizeDelta = new Vector2(980, 56);

            voiceSubtitle = captionObj.GetComponent<Text>();
            voiceSubtitle.font = font;
            voiceSubtitle.fontSize = 20;
            voiceSubtitle.color = new Color(.96f, .98f, 1f, 1f);
            voiceSubtitle.alignment = TextAnchor.LowerCenter;
            voiceSubtitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            voiceSubtitle.verticalOverflow = VerticalWrapMode.Truncate;
            voiceSubtitle.raycastTarget = false;
            voiceSubtitle.supportRichText = false;

            var captionOutline = captionObj.AddComponent<Outline>();
            captionOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            captionOutline.effectDistance = new Vector2(1.5f, -1.5f);
            var captionShadow = captionObj.AddComponent<Shadow>();
            captionShadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            captionShadow.effectDistance = new Vector2(2f, -2f);
            zenoAnswer = voiceSubtitle;

            targetMarker = new GameObject("Matching snap target", typeof(RectTransform)).GetComponent<RectTransform>();
            targetMarker.SetParent(canvas, false);
            targetMarker.anchorMin = targetMarker.anchorMax = targetMarker.pivot = new Vector2(.5f, .5f);
            Panel("Top", targetMarker, new Vector2(0, 1), Vector2.one, new Vector2(0, -3), Vector2.zero, cyan).GetComponent<Image>().raycastTarget = false;
            Panel("Bottom", targetMarker, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 3), cyan).GetComponent<Image>().raycastTarget = false;
            Panel("Left", targetMarker, Vector2.zero, new Vector2(0, 1), Vector2.zero, new Vector2(3, 0), cyan).GetComponent<Image>().raycastTarget = false;
            Panel("Right", targetMarker, new Vector2(1, 0), Vector2.one, new Vector2(-3, 0), Vector2.zero, cyan).GetComponent<Image>().raycastTarget = false;
            targetText = Label(targetMarker, "DROP HERE", 17, new Vector2(-100, 48), new Vector2(300, 42), cyan);
            targetText.alignment = TextAnchor.MiddleCenter;

            controller.viewCamera.rect = new Rect(0, 0, 1, 1);
            gameObject.AddComponent<FerrariLabBackdrop>().Initialize(controller);
        }

        void Update()
        {
            if (!ferrariLogo || !infoPanel) return;
            EnsureAllButtonListeners();
            if (volumeSlider != null && Mathf.Abs(volumeSlider.value - controller.Voice.Volume) > 0.001f)
            {
                volumeSlider.SetValueWithoutNotify(controller.Voice.Volume);
            }
            // Old garage light source dimming and brightening pulse
            ferrariLogo.color = new Color(.9f, .96f, 1, Mathf.Lerp(.35f, 1f, Mathf.SmoothStep(0, 1, (Mathf.Sin(Time.unscaledTime * 2.1f) + 1) * .5f)));
            foreach (var row in inspectionRows)
            {
                if (row.Value == null || row.Value.GetComponent<FerrariCardHover>() != null) continue;
                bool isSel = row.Key == controller.Selected;
                row.Value.sprite = isSel ? glassCardSelectedSprite : glassCardDefaultSprite;
                row.Value.color = Color.white;
            }

            if (voiceMuteIcon != null)
            {
                voiceMuteIcon.texture = controller.Voice.Muted ? muteIconTex : soundIconTex;
                voiceMuteIcon.color = controller.Voice.Muted ? new Color(1f, 0.5f, 0.45f, 0.95f) : new Color(.88f, .96f, 1f, .92f);
            }
            if (voiceMuteText != null)
            {
                voiceMuteText.text = controller.Voice.Muted ? "UNMUTE" : "MUTE";
            }

            var zeno = controller.Zeno;
            zenoToggleText.text = zeno.IsEnabled ? "ZENO AI: ON" : "ZENO AI: OFF";
            zenoPanel.gameObject.SetActive(zeno.IsEnabled);
            voicePanel.gameObject.SetActive(!zeno.IsEnabled);
            zenoAsk.interactable = zeno.IsEnabled && !zeno.IsBusy;
            zenoMute.text = controller.Voice.Muted ? "UNMUTE" : "MUTE";
            zenoStatus.text = zeno.Status;

            if (zeno.IsListening && zeno.MicrophoneLevel >= zeno.MicrophoneThreshold) lastZenoSpeechSignal = Time.unscaledTime;
            string micSignal = zeno.IsListening ? Time.unscaledTime - lastZenoSpeechSignal < 1.1f ? "MIC: hearing you" :
                zeno.MicrophoneLevel >= zeno.MicrophoneThreshold * .4f ? "MIC: quiet" : "MIC: no sound detected" : "MIC: paused";
            zenoContext.text = "ABOUT: " + zeno.ContextName + "   " + micSignal;

            // Captions: display part explanation or Zeno AI answer as clean floating movie caption
            bool hasZenoAnswer = zeno.IsEnabled && !string.IsNullOrWhiteSpace(zeno.Answer);
            bool hasVoiceExplanation = !zeno.IsEnabled && controller.Voice.SelectedPart != null && !string.IsNullOrWhiteSpace(controller.Voice.Subtitle) && controller.Voice.Subtitle != "Select a part to hear its explanation.";

            if (hasZenoAnswer)
            {
                voiceSubtitle.text = zeno.Answer;
                voiceSubtitle.enabled = true;
            }
            else if (hasVoiceExplanation)
            {
                voiceSubtitle.text = controller.Voice.Subtitle;
                voiceSubtitle.enabled = true;
            }
            else
            {
                voiceSubtitle.text = "";
                voiceSubtitle.enabled = false;
            }

            var assembly = controller.Assembly;
            bool active = assembly.IsActive;
            inspectionList.gameObject.SetActive(!active);
            assemblyList.gameObject.SetActive(active);
            foreach (var go in inspectionControls) go.SetActive(!active);
            foreach (var go in assemblyControls) go.SetActive(active);
            modeToggleText.text = active ? "BACK TO INSPECTION" : "SELF ASSEMBLY";
            listTitle.text = active ? "UNPLACED  /  LOOP SCROLL" : "ASSEMBLIES  /  SCROLL";

            var presentedPart = active ? assembly.TargetPart : controller.Selected;
            bool isPartSelected = presentedPart != null;

            // Pop animation from left edge when part selected, smooth slide back when deselected
            if (isPartSelected)
            {
                if (!infoPanel.gameObject.activeSelf) infoPanel.gameObject.SetActive(true);
                infoAnimProgress = Mathf.MoveTowards(infoAnimProgress, 1f, Time.unscaledDeltaTime * 3.8f);
                float t = infoAnimProgress;
                float c1 = 1.6f, c3 = c1 + 1f;
                float easePop = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                infoPanel.anchoredPosition = new Vector2(Mathf.LerpUnclamped(-360f, 22f, easePop), 0f);
                float scale = Mathf.LerpUnclamped(0.92f, 1f, easePop);
                infoPanel.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                infoAnimProgress = Mathf.MoveTowards(infoAnimProgress, 0f, Time.unscaledDeltaTime * 4.5f);
                float easeBack = infoAnimProgress * infoAnimProgress;
                infoPanel.anchoredPosition = new Vector2(Mathf.Lerp(-360f, 22f, easeBack), 0f);
                infoPanel.localScale = Vector3.one;
                if (infoAnimProgress <= 0.001f && infoPanel.gameObject.activeSelf)
                {
                    infoPanel.gameObject.SetActive(false);
                }
            }

            if (presentedPart)
            {
                nameText.text = presentedPart.displayName;
                description.text = FerrariPartPresentation.Purpose(presentedPart.partId);
                detailThumbnail.gameObject.SetActive(true);
                var thumb = FerrariPartPresentation.Thumbnail(presentedPart.partId);
                detailThumbnail.texture = thumb;
                if (thumb != null && thumb.height > 0)
                {
                    float aspect = (float)thumb.width / thumb.height;
                    float h = 105f;
                    float w = h * aspect;
                    detailThumbnail.rectTransform.sizeDelta = new Vector2(w, h);
                    detailThumbnail.rectTransform.anchoredPosition = new Vector2((332f - w) * 0.5f, -130f);
                }
                placementHint.gameObject.SetActive(true);
                placementHint.text = "WHERE IT FITS\n" + FerrariPartPresentation.PlacementHint(presentedPart.partId);
            }

            targetMarker.gameObject.SetActive(active && assembly.TargetPart && !assembly.IsSnapping);
            if (targetMarker.gameObject.activeSelf)
            {
                var world = assembly.TargetPosition(assembly.TargetPart);
                var screen = controller.viewCamera.WorldToScreenPoint(world);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, null, out var local);
                targetMarker.anchoredPosition = local;
                float radius = Vector2.Distance(screen, controller.viewCamera.WorldToScreenPoint(world + controller.viewCamera.transform.right * FerrariAssemblyController.SnapRadius));
                targetMarker.sizeDelta = Vector2.one * Mathf.Clamp(radius * 2 / canvas.GetComponent<Canvas>().scaleFactor, 36, 130);
                Color color = assembly.CanSnap ? new Color(.3f, 1, .45f) : cyan;
                foreach (var im in targetMarker.GetComponentsInChildren<Image>()) im.color = color;
                targetText.color = color;
                targetText.text = assembly.CanSnap ? "RELEASE TO SNAP" : "MATCHING TARGET";
                targetText.rectTransform.anchoredPosition = new Vector2((targetMarker.sizeDelta.x - 300) / 2, 48);
            }

            if (active)
            {
                var part = assembly.TargetPart;
                nameText.text = assembly.IsComplete ? "Car complete!" : part ? part.displayName : "Build the Ferrari";
                description.text = assembly.IsComplete ? "All 15 movable assemblies are in their saved positions and orientations. You can rotate your completed car, return to inspection, or restart." : part ? FerrariPartPresentation.Purpose(part.partId) : "Click a part's picture or name for its purpose and placement clue. Drag it onto the marked target, then release when it turns green.\n\nScroll over the car to zoom. Scroll over this list to cycle the 15 parts.";
                status.text = assembly.Status + (assembly.Access.IsMoving ? "  |  Access panels moving" : "");
                mode.text = assembly.PlacedCount + " / " + assembly.TotalCount + " PLACED";
                return;
            }

            if (controller.Selected)
            {
                nameText.text = controller.Selected.displayName;
                description.text = FerrariPartPresentation.Purpose(controller.Selected.partId);
            }
            status.text = controller.Status;
            mode.text = controller.IsAnimating ? "MOVING..." : controller.IsExploded ? "EXPLODED" : "ASSEMBLED";
            autoText.text = controller.AutoRotate ? "AUTO: ON" : "AUTO: OFF";
        }

        public static Sprite Load9SliceAsset(string path, Vector4 border)
        {
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        public static void Frame(RectTransform parent, Color color)
        {
            for (int i = 0; i < 4; i++)
            {
                var edge = new GameObject("Glass border", typeof(RectTransform), typeof(Image));
                var r = (RectTransform)edge.transform;
                r.SetParent(parent, false);
                r.anchorMin = i == 0 ? new Vector2(0, 1) : i == 3 ? new Vector2(1, 0) : Vector2.zero;
                r.anchorMax = i == 1 ? new Vector2(1, 0) : i == 2 ? new Vector2(0, 1) : Vector2.one;
                r.offsetMin = i == 0 ? new Vector2(0, -1) : i == 3 ? new Vector2(-1, 0) : Vector2.zero;
                r.offsetMax = i == 1 ? new Vector2(0, 1) : i == 2 ? new Vector2(1, 0) : Vector2.zero;
                var im = edge.GetComponent<Image>();
                im.color = color;
                im.raycastTarget = false;
            }
        }

        RectTransform Panel(string name, RectTransform parent, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offMin;
            rt.offsetMax = offMax;
            go.GetComponent<Image>().color = color;
            return rt;
        }

        RawImage Thumbnail(RectTransform parent, Texture texture, Vector2 position, Vector2 size)
        {
            var go = new GameObject("Part image", typeof(RectTransform), typeof(RawImage));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            var image = go.GetComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            return image;
        }

        Text Label(RectTransform parent, string value, int size, Vector2 pos, Vector2 dimensions, Color color)
        {
            var go = new GameObject(value.Length > 30 ? value.Substring(0, 30) : value, typeof(RectTransform), typeof(Text));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = dimensions;
            var text = go.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        Button Button(RectTransform parent, string title, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction action, Color color)
        {
            var rt = Panel(title, parent, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero, Color.clear);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var im = rt.GetComponent<Image>();
            
            // Glowing edge outline without background
            Sprite outlineSprite = btnOutlineGraySprite;
            if (color.r > color.b && color.r > 0.5f) outlineSprite = btnOutlineOrangeSprite;
            else if (color.b > color.r && color.g > 0.35f) outlineSprite = btnOutlineCyanSprite;

            im.sprite = outlineSprite;
            im.type = Image.Type.Sliced;
            im.color = Color.white;
            im.raycastTarget = true;

            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = im;
            b.onClick.AddListener(action);
            var text = Label(rt, title, 17, Vector2.zero, size, ink);
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            var colors = b.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            b.colors = colors;
            return b;
        }

        Button IconButton(RectTransform parent, string title, Vector2 pos, Vector2 size, Texture2D icon, UnityEngine.Events.UnityAction action, out RawImage iconImage)
        {
            var go = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var bg = go.GetComponent<Image>();
            bg.color = Color.clear;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(RawImage));
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.SetParent(rt, false);
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.sizeDelta = Vector2.zero;
            iconRt.anchoredPosition = Vector2.zero;

            var raw = iconGo.GetComponent<RawImage>();
            raw.texture = icon;
            raw.color = new Color(.88f, .96f, 1f, .92f);
            raw.raycastTarget = false;
            iconImage = raw;

            var b = go.GetComponent<Button>();
            b.targetGraphic = raw;
            var colors = b.colors;
            colors.normalColor = new Color(.88f, .96f, 1f, .92f);
            colors.highlightedColor = new Color(.35f, .95f, 1f, 1f);
            colors.pressedColor = new Color(.2f, .65f, .9f, 1f);
            b.colors = colors;
            b.onClick.AddListener(action);
            return b;
        }
    }

    [AddComponentMenu("UI/Effects/Vertical Gradient")]
    public sealed class UIVerticalGradient : BaseMeshEffect
    {
        public Color topColor = Color.white;
        public Color bottomColor = new Color(.42f, .58f, .72f, 1f);

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;
            int count = vh.currentVertCount;
            if (count == 0) return;
            UIVertex vertex = default;
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                if (vertex.position.y < minY) minY = vertex.position.y;
                if (vertex.position.y > maxY) maxY = vertex.position.y;
            }
            float height = maxY - minY;
            if (height <= 0.001f) return;
            for (int i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                float t = Mathf.Clamp01((vertex.position.y - minY) / height);
                Color c = Color.Lerp(bottomColor, topColor, t);
                vertex.color = new Color32(
                    (byte)(c.r * vertex.color.r),
                    (byte)(c.g * vertex.color.g),
                    (byte)(c.b * vertex.color.b),
                    (byte)(c.a * vertex.color.a)
                );
                vh.SetUIVertex(vertex, i);
            }
        }
    }
}
