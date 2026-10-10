using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ARVRAutomotive
{
    public sealed class AutomotivePrototypeBootstrap : MonoBehaviour
    {
        private readonly List<VehicleComponent> components = new();
        private readonly List<RaycastResult> uiHits = new();
        private Transform turntable;
        private Camera mainCamera;
        private Font uiFont;
        private Text componentNameText, componentDescriptionText, statusText, spinText;
        private VehicleComponent selectedComponent;
        private float explosionTarget, explosionProgress, rotationAngle, pitch = 24f, zoom = 1f;
        private bool autoRotate, pointerStartedInScene, dragging;
        private Vector2 pointerStart, previousPointer;
        private Material paint, glass, rubber, metal, dark, orange, purple, green, lamp;

        private void Awake()
        {
            if (GameObject.Find("Automotive Learning Environment") != null) return;
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildEnvironment();
            BuildVehicle();
            BuildInterface();
            Assemble();
            ValidateGeometry();
        }

        private void Update()
        {
            HandleInput();
            explosionProgress = Mathf.MoveTowards(explosionProgress, explosionTarget, Time.deltaTime * .8f);
            float eased = Mathf.SmoothStep(0f, 1f, explosionProgress);
            foreach (var component in components) component.ApplyExplosion(eased);
            if (autoRotate) rotationAngle += Time.deltaTime * 16f;
            turntable.localRotation = Quaternion.Euler(0f, rotationAngle, 0f);
            PositionCamera(eased);
        }

        private void BuildEnvironment()
        {
            var environment = new GameObject("Automotive Learning Environment").transform;
            turntable = new GameObject("ROTATING DISPLAY - platform and vehicle").transform;
            turntable.SetParent(environment, false);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.55f, .65f, .8f);
            RenderSettings.ambientEquatorColor = new Color(.28f, .34f, .43f);
            RenderSettings.ambientGroundColor = new Color(.12f, .15f, .2f);
            mainCamera = new GameObject("Main Camera").AddComponent<Camera>();
            mainCamera.tag = "MainCamera";
            mainCamera.transform.SetParent(environment, false);
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(.027f, .042f, .061f);
            mainCamera.fieldOfView = 38f;
            mainCamera.nearClipPlane = .1f;
            mainCamera.farClipPlane = 100f;
            mainCamera.allowMSAA = true;
            // Clear the entire target, including the margins around the inset 3D viewport.
            var backgroundCamera = new GameObject("Full screen background").AddComponent<Camera>();
            backgroundCamera.transform.SetParent(environment, false);
            backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
            backgroundCamera.backgroundColor = mainCamera.backgroundColor;
            backgroundCamera.cullingMask = 0;
            backgroundCamera.depth = -10;
            mainCamera.depth = 0;
            var key = new GameObject("Soft key light").AddComponent<Light>();
            key.transform.SetParent(environment, false);
            key.type = LightType.Directional;
            key.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            key.intensity = 1.8f;
            key.shadows = LightShadows.Soft;
            var fill = new GameObject("Cool fill light").AddComponent<Light>();
            fill.transform.SetParent(environment, false);
            fill.type = LightType.Directional;
            fill.transform.rotation = Quaternion.Euler(30f, 145f, 0f);
            fill.intensity = .75f;
            fill.color = new Color(.65f, .8f, 1f);
            var baseMaterial = MakeMaterial("Platform edge", new Color(.055f, .08f, .11f), .45f, .5f);
            var topMaterial = MakeMaterial("Platform surface", new Color(.09f, .15f, .21f), .3f, .55f);
            var cyan = MakeMaterial("Platform markings", new Color(.12f, .7f, .86f));
            Part("Rotating plinth", PrimitiveType.Cylinder, turntable, new Vector3(0, -.04f, 0), new Vector3(9, .16f, 9), baseMaterial, false);
            Part("Rotating top", PrimitiveType.Cylinder, turntable, new Vector3(0, .1f, 0), new Vector3(8.65f, .04f, 8.65f), topMaterial, false);
            for (int i = 0; i < 48; i++)
            {
                float angle = i * 7.5f;
                float a = angle * Mathf.Deg2Rad;
                var tick = Part("Turntable index", PrimitiveType.Cube, turntable, new Vector3(Mathf.Sin(a) * 4.12f, .148f, Mathf.Cos(a) * 4.12f), new Vector3(.025f, .012f, i % 4 == 0 ? .24f : .12f), cyan, false);
                tick.transform.localRotation = Quaternion.Euler(0, angle, 0);
            }
            Part("Front orientation marker", PrimitiveType.Cube, turntable, new Vector3(0, .152f, -3.8f), new Vector3(.65f, .018f, .14f), cyan, false);
        }

        private void BuildVehicle()
        {
            paint = MakeMaterial("Ocean blue body", new Color(.055f, .36f, .58f), .55f, .72f);
            glass = MakeMaterial("Smoked windows", new Color(.055f, .13f, .2f), .35f, .86f);
            rubber = MakeMaterial("Rubber", new Color(.022f, .027f, .034f), .05f, .22f);
            metal = MakeMaterial("Brushed alloy", new Color(.63f, .71f, .78f), .65f, .55f);
            dark = MakeMaterial("Chassis steel", new Color(.13f, .17f, .22f), .5f, .4f);
            orange = MakeMaterial("Engine orange", new Color(.92f, .34f, .085f));
            purple = MakeMaterial("Transmission purple", new Color(.51f, .27f, .72f));
            green = MakeMaterial("Battery green", new Color(.1f, .55f, .37f));
            lamp = MakeMaterial("Headlamp lenses", new Color(.83f, .93f, 1f), .1f, .8f);

            var chassis = Component("CHASSIS", "The structural frame supports the body, engine and suspension. Cross-members distribute loads and help the vehicle resist bending.\n\nIn exploded view, the frame stays above the display platform.", Vector3.zero, new Vector3(0, .15f, 0));
            foreach (float z in new[] { -.86f, .86f }) Box("Frame rail", chassis, new Vector3(0, .73f, z), new Vector3(5.9f, .18f, .17f), dark);
            foreach (float x in new[] { -2.1f, -.7f, .7f, 2.1f }) Box("Cross member", chassis, new Vector3(x, .73f, 0), new Vector3(.17f, .18f, 1.9f), dark);
            Box("Passenger floor", chassis, new Vector3(-.45f, .87f, 0), new Vector3(3, .08f, 2.08f), dark);
            foreach (float x in new[] { -2.05f, 2.05f }) Cylinder("Axle", chassis, new Vector3(x, .82f, 0), new Vector3(.12f, 1.22f, .12f), dark, new Vector3(90, 0, 0));

            var body = Component("BODY & CABIN", "The body encloses the passenger cabin and protects the vehicle systems. The bonnet covers the engine bay.\n\nUse EXPLODE to lift the body and reveal the engine, battery, transmission and supporting chassis.", Vector3.zero, new Vector3(0, 2.75f, 0));
            BuildBodySides(body);
            Box("Bonnet", body, new Vector3(2.15f, 1.72f, 0), new Vector3(2.08f, .12f, 2.34f), paint);
            Box("Rear deck", body, new Vector3(-2.51f, 1.7f, 0), new Vector3(1.13f, .13f, 2.34f), paint);
            BuildCabin(body);
            Box("Roof", body, new Vector3(-.325f, 2.455f, 0), new Vector3(1.76f, .075f, 1.99f), paint);
            foreach (float side in new[] { -1f, 1f })
            {
                Beam("Windscreen pillar", body, new Vector3(1.16f, 1.76f, side * 1.13f), new Vector3(.49f, 2.44f, side * .96f), .085f, paint);
                Beam("Rear pillar", body, new Vector3(-1.96f, 1.76f, side * 1.13f), new Vector3(-1.14f, 2.44f, side * .96f), .1f, paint);
                Beam("Centre pillar", body, new Vector3(-.37f, 1.76f, side * 1.135f), new Vector3(-.37f, 2.44f, side * .96f), .085f, dark);
                Box("Window sill", body, new Vector3(-.4f, 1.76f, side * 1.135f), new Vector3(3.05f, .045f, .045f), metal);
                foreach (float x in new[] { .55f, -1.05f }) Box("Door handle", body, new Vector3(x, 1.62f, side * 1.256f), new Vector3(.25f, .045f, .035f), metal);
                Box("Door seam", body, new Vector3(-.36f, 1.32f, side * 1.253f), new Vector3(.018f, .68f, .012f), dark);
                Box("Wing mirror", body, new Vector3(1.04f, 1.89f, side * 1.37f), new Vector3(.3f, .16f, .29f), paint);
                Box("Headlight", body, new Vector3(3.19f, 1.55f, side * .94f), new Vector3(.075f, .16f, .49f), lamp);
                Box("Tail light", body, new Vector3(-3.18f, 1.5f, side * .91f), new Vector3(.06f, .17f, .56f), orange);
            }
            Box("Front bumper", body, new Vector3(3.15f, .99f, 0), new Vector3(.18f, .25f, 2.42f), dark);
            Box("Rear bumper", body, new Vector3(-3.15f, 1.03f, 0), new Vector3(.17f, .27f, 2.42f), dark);
            Box("Grille", body, new Vector3(3.25f, 1.36f, 0), new Vector3(.04f, .36f, 1.25f), rubber);
            for (int i = -3; i <= 3; i++) Box("Grille slat", body, new Vector3(3.28f, 1.36f, i * .16f), new Vector3(.035f, .27f, .025f), metal);

            var engine = Component("ENGINE", "The engine converts fuel energy into mechanical rotation. Its crankshaft supplies torque to the transmission.\n\nOrange identifies the engine assembly. It fits beneath the bonnet in assembled view.", new Vector3(1.86f, 1.2f, 0), new Vector3(1.35f, 1.25f, -.55f));
            Box("Engine block", engine, Vector3.zero, new Vector3(1.12f, .55f, .97f), orange);
            Box("Cylinder head", engine, new Vector3(0, .34f, 0), new Vector3(1.15f, .16f, .95f), metal);
            for (int i = 0; i < 4; i++)
            {
                Box("Head rib", engine, new Vector3(-.42f + i * .28f, .435f, 0), new Vector3(.06f, .025f, .76f), dark);
                Cylinder("Intake runner", engine, new Vector3(-.4f + i * .26f, .1f, -.55f), new Vector3(.12f, .2f, .12f), metal, new Vector3(90, 0, 0));
            }
            Cylinder("Crank pulley", engine, new Vector3(.63f, -.05f, 0), new Vector3(.35f, .055f, .35f), dark, new Vector3(0, 0, 90));

            var transmission = Component("TRANSMISSION", "The transmission changes the speed and torque delivered from the engine to the wheels. Different gear ratios support starting, acceleration and cruising.\n\nPurple identifies the gearbox and drive shaft.", new Vector3(.55f, 1.05f, 0), new Vector3(-.4f, 1.4f, -2.05f));
            Box("Gearbox", transmission, Vector3.zero, new Vector3(1.13f, .48f, .73f), purple);
            Cylinder("Bell housing", transmission, new Vector3(.53f, 0, 0), new Vector3(.65f, .15f, .65f), purple, new Vector3(0, 0, 90));
            Cylinder("Drive shaft", transmission, new Vector3(-1.25f, -.03f, 0), new Vector3(.12f, .85f, .12f), metal, new Vector3(0, 0, 90));
            var battery = Component("BATTERY", "The battery stores electrical energy. It powers the starter motor and supplies electrical systems when needed.\n\nGreen identifies the battery, with two terminals on its top surface.", new Vector3(1.9f, 1.26f, .85f), new Vector3(.1f, 1.15f, 1.7f));
            Box("Battery case", battery, Vector3.zero, new Vector3(.67f, .42f, .45f), green);
            Box("Battery lid", battery, new Vector3(0, .24f, 0), new Vector3(.69f, .06f, .47f), dark);
            foreach (float x in new[] { -.21f, .21f }) Cylinder("Terminal", battery, new Vector3(x, .31f, 0), new Vector3(.07f, .04f, .07f), metal, Vector3.zero);
            foreach (float x in new[] { -2.05f, 2.05f })
            foreach (float z in new[] { -1.28f, 1.28f }) BuildWheel(x, z);
        }

        private void BuildWheel(float x, float z)
        {
            float side = Mathf.Sign(z);
            var wheel = Component((x > 0 ? "FRONT " : "REAR ") + (z < 0 ? "LEFT WHEEL" : "RIGHT WHEEL"), "The tyre provides grip and cushions road impacts. The alloy wheel supports the tyre and transfers forces between the axle and the road.\n\nExploded view separates each wheel from its axle.", new Vector3(x, .82f, z), new Vector3(Mathf.Sign(x) * .35f, .12f, side * 1.25f));
            Cylinder("Tyre", wheel, Vector3.zero, new Vector3(1.12f, .19f, 1.12f), rubber, new Vector3(90, 0, 0));
            Cylinder("Alloy rim", wheel, new Vector3(0, 0, side * .202f), new Vector3(.79f, .025f, .79f), metal, new Vector3(90, 0, 0));
            Cylinder("Rim inset", wheel, new Vector3(0, 0, side * .229f), new Vector3(.65f, .012f, .65f), dark, new Vector3(90, 0, 0));
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f * Mathf.Deg2Rad;
                var spoke = Box("Alloy spoke", wheel, new Vector3(Mathf.Sin(a) * .17f, Mathf.Cos(a) * .17f, side * .255f), new Vector3(.075f, .34f, .035f), metal);
                spoke.transform.localRotation = Quaternion.Euler(0, 0, -i * 72f);
            }
            Cylinder("Wheel hub", wheel, new Vector3(0, 0, side * .272f), new Vector3(.19f, .022f, .19f), metal, new Vector3(90, 0, 0));
        }

        private void BuildBodySides(Transform parent)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                var vertices = new List<Vector3>(); var triangles = new List<int>();
                for (int i = 0; i < 100; i++)
                {
                    float x0 = Mathf.Lerp(-3.12f, 3.12f, i / 100f), x1 = Mathf.Lerp(-3.12f, 3.12f, (i + 1) / 100f);
                    float y0 = ArchHeight(x0), y1 = ArchHeight(x1);
                    float top0 = 1.73f - .09f * Mathf.Pow(Mathf.Abs(x0) / 3.12f, 5), top1 = 1.73f - .09f * Mathf.Pow(Mathf.Abs(x1) / 3.12f, 5);
                    Vector3 a = new(x0, y0, side * 1.25f), b = new(x1, y1, side * 1.25f), c = new(x1, top1, side * 1.25f), d = new(x0, top0, side * 1.25f);
                    Vector3 ai = new(x0, y0, side * 1.12f), bi = new(x1, y1, side * 1.12f), ci = new(x1, top1, side * 1.12f), di = new(x0, top0, side * 1.12f);
                    Quad(vertices, triangles, a, b, c, d, side < 0);
                    Quad(vertices, triangles, d, c, ci, di, side < 0);
                    Quad(vertices, triangles, ai, bi, b, a, side < 0);
                    Quad(vertices, triangles, bi, ai, di, ci, side < 0);
                }
                MeshPart("Sculpted body with wheel arches", parent, vertices, triangles, paint);
            }
        }

        private static float ArchHeight(float x)
        {
            float dx = Mathf.Min(Mathf.Abs(x - 2.05f), Mathf.Abs(x + 2.05f));
            return dx < .65f ? .82f + Mathf.Sqrt(.65f * .65f - dx * dx) : .78f;
        }

        private void BuildCabin(Transform parent)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            Vector3 a = new(-1.95f, 1.75f, -1.12f), b = new(1.15f, 1.75f, -1.12f), c = new(.48f, 2.43f, -.94f), d = new(-1.13f, 2.43f, -.94f);
            Vector3 e = new(-1.95f, 1.75f, 1.12f), f = new(1.15f, 1.75f, 1.12f), g = new(.48f, 2.43f, .94f), h = new(-1.13f, 2.43f, .94f);
            Quad(v, t, a, b, c, d, true); Quad(v, t, e, h, g, f, true);
            Quad(v, t, b, f, g, c, true); Quad(v, t, a, d, h, e, true);
            MeshPart("Glazed passenger cabin", parent, v, t, glass);
        }

        private void BuildInterface()
        {
            var canvas = new GameObject("Readable learning interface", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().pixelPerfect = true;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0f;
            if (EventSystem.current == null) new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var header = Panel("Header", canvas.transform);
            Anchors(header, new Vector2(0, 1), Vector2.one, new Vector2(0, -70), Vector2.zero);
            Label(header, "AUTOMOTIVE XR LAB", 26, new Vector2(24, -12), new Vector2(680, 32), true);
            Label(header, "Interactive vehicle systems  /  30% prototype", 16, new Vector2(25, -44), new Vector2(700, 22));
            var info = Panel("Component information", canvas.transform);
            Anchors(info, new Vector2(1, 0), Vector2.one, new Vector2(-326, 16), new Vector2(-16, -86));
            Label(info, "COMPONENT INSPECTOR", 16, new Vector2(20, -20), new Vector2(270, 26), true).color = new Color(.2f, .78f, .94f);
            componentNameText = Label(info, "SELECT A PART", 27, new Vector2(20, -58), new Vector2(270, 76), true);
            var descriptionViewport = new GameObject("Scrollable description", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            descriptionViewport.transform.SetParent(info, false);
            var descriptionRect = descriptionViewport.GetComponent<RectTransform>();
            Anchors(descriptionRect, Vector2.zero, Vector2.one, new Vector2(20, 140), new Vector2(-20, -145));
            descriptionViewport.GetComponent<Image>().color = Color.clear;
            componentDescriptionText = Label(descriptionRect, "Click the body or a wheel to inspect it.\n\nUse EXPLODE to reveal the internal systems, then click a coloured component.", 20, Vector2.zero, Vector2.zero);
            var textRect = componentDescriptionText.rectTransform;
            textRect.anchorMin = new Vector2(0, 1); textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(.5f, 1); textRect.sizeDelta = Vector2.zero;
            componentDescriptionText.verticalOverflow = VerticalWrapMode.Overflow;
            componentDescriptionText.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = descriptionViewport.GetComponent<ScrollRect>();
            scroll.viewport = descriptionRect; scroll.content = textRect; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 24;
            var hints = Label(info, "Drag: turn entire display\nRight-drag: adjust height\nScroll: zoom   |   Space: spin\nE: explode   A: assemble\nR: reset   F12: screenshot", 16, new Vector2(20, 20), new Vector2(275, 105));
            hints.rectTransform.anchorMin = hints.rectTransform.anchorMax = Vector2.zero;
            hints.rectTransform.pivot = Vector2.zero;
            hints.color = new Color(.64f, .76f, .83f);
            var controls = Panel("Display controls", canvas.transform);
            Anchors(controls, Vector2.zero, new Vector2(1, 0), new Vector2(16, 16), new Vector2(-342, 140));
            AddButton(controls, "EXPLODE", Explode, 0, 0, new Color(.85f, .25f, .065f));
            AddButton(controls, "ASSEMBLE", Assemble, 1, 0, new Color(.025f, .42f, .63f));
            AddButton(controls, "RESET VIEW", ResetView, 2, 0, new Color(.21f, .28f, .36f));
            AddButton(controls, "TURN LEFT", () => Turn(-30), 0, 1, new Color(.13f, .23f, .3f));
            spinText = AddButton(controls, "AUTO ROTATE", ToggleSpin, 1, 1, new Color(.13f, .23f, .3f));
            AddButton(controls, "TURN RIGHT", () => Turn(30), 2, 1, new Color(.13f, .23f, .3f));
            statusText = Label(canvas.transform, "ASSEMBLED VIEW", 17, new Vector2(24, -87), new Vector2(700, 25), true);
            statusText.color = new Color(.2f, .78f, .94f);
        }

        private void PositionCamera(float progress)
        {
            float scale = Screen.width / 1280f;
            float bottom = 146f * scale / Screen.height, top = 76f * scale / Screen.height;
            mainCamera.rect = new UnityEngine.Rect(.0125f, bottom, .72f, Mathf.Max(.25f, 1f - top - bottom));
            var target = Vector3.Lerp(new Vector3(0, 1.15f, 0), new Vector3(0, 2.25f, 0), progress);
            float halfHeight = Mathf.Max(Mathf.Lerp(4.1f, 4.65f, progress), 5.5f / mainCamera.aspect);
            float distance = halfHeight / Mathf.Tan(mainCamera.fieldOfView * .5f * Mathf.Deg2Rad) * zoom;
            mainCamera.transform.position = target + Quaternion.Euler(pitch, -42f, 0) * new Vector3(0, 0, -distance);
            mainCamera.transform.LookAt(target);
        }

        private bool OverUI(Vector2 position)
        {
            if (EventSystem.current == null) return false;
            uiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, uiHits);
            return uiHits.Count > 0;
        }

        private void HandleInput()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 pos = mouse.position.ReadValue();
                if (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)
                {
                    pointerStartedInScene = !OverUI(pos) && mainCamera.pixelRect.Contains(pos);
                    pointerStart = previousPointer = pos;
                    dragging = false;
                }
                if (pointerStartedInScene && (mouse.leftButton.isPressed || mouse.rightButton.isPressed))
                {
                    if ((pos - pointerStart).sqrMagnitude > 25f) dragging = true;
                    if (dragging)
                    {
                        Vector2 delta = pos - previousPointer;
                        Turn(-delta.x * .34f);
                        if (mouse.rightButton.isPressed) pitch = Mathf.Clamp(pitch - delta.y * .18f, 12f, 65f);
                    }
                    previousPointer = pos;
                }
                if (mouse.leftButton.wasReleasedThisFrame)
                {
                    if (pointerStartedInScene && !dragging && !OverUI(pos))
                        Select(Physics.Raycast(mainCamera.ScreenPointToRay(pos), out var hit, 100) ? hit.collider.GetComponentInParent<VehicleComponent>() : null);
                    pointerStartedInScene = false;
                }
                if (mouse.rightButton.wasReleasedThisFrame) pointerStartedInScene = false;
                if (!OverUI(pos)) zoom = Mathf.Clamp(zoom - mouse.scroll.ReadValue().y * .0007f, .7f, 1.45f);
            }
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.eKey.wasPressedThisFrame) Explode();
            if (keyboard.aKey.wasPressedThisFrame) Assemble();
            if (keyboard.rKey.wasPressedThisFrame) ResetView();
            if (keyboard.spaceKey.wasPressedThisFrame) ToggleSpin();
            if (keyboard.f12Key.wasPressedThisFrame) CaptureEvidence();
            if (keyboard.escapeKey.wasPressedThisFrame) Select(null);
            if (keyboard.leftArrowKey.isPressed) Turn(-65f * Time.deltaTime);
            if (keyboard.rightArrowKey.isPressed) Turn(65f * Time.deltaTime);
        }

        public void Explode() { explosionTarget = 1; statusText.text = "EXPLODED VIEW  /  9 selectable assemblies"; }
        public void Assemble() { explosionTarget = 0; statusText.text = "ASSEMBLED VIEW  /  drag the display to explore"; }
        private void Turn(float degrees) { autoRotate = false; rotationAngle += degrees; UpdateSpinLabel(); }
        private void ToggleSpin() { autoRotate = !autoRotate; UpdateSpinLabel(); }
        private void UpdateSpinLabel() { if (spinText != null) spinText.text = autoRotate ? "PAUSE ROTATION" : "AUTO ROTATE"; }
        private void ResetView() { Assemble(); Select(null); rotationAngle = 0; pitch = 24; zoom = 1; autoRotate = false; UpdateSpinLabel(); }
        private void Select(VehicleComponent component)
        {
            if (selectedComponent != null) selectedComponent.SetHighlighted(false);
            selectedComponent = component;
            componentDescriptionText.rectTransform.anchoredPosition = Vector2.zero;
            if (component != null)
            {
                component.SetHighlighted(true);
                componentNameText.text = component.DisplayName;
                componentDescriptionText.text = component.Description;
                autoRotate = false; UpdateSpinLabel();
            }
            else
            {
                componentNameText.text = "SELECT A PART";
                componentDescriptionText.text = "Click the body or a wheel to inspect it.\n\nUse EXPLODE to reveal the internal systems, then click a coloured component.";
            }
        }

        public void CaptureEvidence()
        {
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Evidence"));
            Directory.CreateDirectory(directory);
            string name = explosionTarget > .5f ? "Prototype-V2-Exploded.png" : "Prototype-V2-Assembled.png";
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, name), 2);
            Debug.Log("[ARVR] Evidence saved: " + Path.Combine(directory, name));
        }

        public void ValidateGeometry()
        {
            float lowest = float.MaxValue;
            foreach (var component in components)
            {
                Debug.Assert(component.transform.parent == turntable, "Every assembly must rotate with the platform.");
                foreach (var renderer in component.GetComponentsInChildren<Renderer>()) lowest = Mathf.Min(lowest, renderer.bounds.min.y);
            }
            Debug.Assert(lowest > .161f, "A vehicle component intersects the display platform.");
            Debug.Log($"[ARVR] Geometry check: {components.Count} rotating assemblies; lowest vehicle point {lowest:F3}; platform top 0.161. All explosion offsets are upward.");
        }

        private Transform Component(string name, string description, Vector3 position, Vector3 offset)
        {
            var go = new GameObject(name);
            go.transform.SetParent(turntable, false);
            go.transform.localPosition = position;
            var component = go.AddComponent<VehicleComponent>();
            component.Configure(name, description, position, position + offset);
            components.Add(component);
            return go.transform;
        }
        private static Material MakeMaterial(string name, Color color, float metallic = .15f, float smoothness = .4f)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { name = name, color = color };
            material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", smoothness);
            return material;
        }
        private static GameObject Part(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Destroy(go.GetComponent<Collider>());
            return go;
        }
        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material) => Part(name, PrimitiveType.Cube, parent, position, scale, material);
        private static void Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3 rotation)
        {
            Part(name, PrimitiveType.Cylinder, parent, position, scale, material).transform.localRotation = Quaternion.Euler(rotation);
        }
        private static void Beam(string name, Transform parent, Vector3 a, Vector3 b, float width, Material material)
        {
            Box(name, parent, (a + b) * .5f, new Vector3(width, Vector3.Distance(a, b), width), material).transform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
        }
        private static void Quad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool reverse = false)
        {
            int i = v.Count; v.AddRange(new[] { a, b, c, d });
            t.AddRange(reverse ? new[] { i, i + 2, i + 1, i, i + 3, i + 2 } : new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
        }
        private static void MeshPart(string name, Transform parent, List<Vector3> vertices, List<int> triangles, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material; go.GetComponent<MeshCollider>().sharedMesh = mesh;
        }
        private static RectTransform Panel(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(.045f, .075f, .105f, .98f);
            return go.GetComponent<RectTransform>();
        }
        private static void Anchors(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax; }
        private Text Label(Transform parent, string text, int size, Vector2 position, Vector2 dimensions, bool bold = false)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>(); label.font = uiFont; label.fontSize = size; label.text = text; label.color = new Color(.92f, .96f, 1f);
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0, 1); label.rectTransform.pivot = new Vector2(0, 1);
            label.rectTransform.anchoredPosition = position; label.rectTransform.sizeDelta = dimensions;
            return label;
        }
        private Text AddButton(Transform parent, string text, UnityEngine.Events.UnityAction action, int column, int row, Color color)
        {
            var rect = Panel(text, parent); rect.GetComponent<Image>().color = color;
            Anchors(rect, new Vector2(column / 3f, row == 0 ? 1 : 0), new Vector2((column + 1) / 3f, row == 0 ? 1 : 0), new Vector2(7, row == 0 ? -55 : 7), new Vector2(-7, row == 0 ? -7 : 55));
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>(); button.onClick.AddListener(action);
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var label = Label(rect, text, 20, Vector2.zero, Vector2.zero, true); label.alignment = TextAnchor.MiddleCenter;
            Anchors(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return label;
        }
    }

    public sealed class VehicleComponent : MonoBehaviour
    {
        public string DisplayName { get; private set; }
        public string Description { get; private set; }
        private Vector3 assembledPosition, explodedPosition;
        public void Configure(string displayName, string description, Vector3 assembled, Vector3 exploded)
        { DisplayName = displayName; Description = description; assembledPosition = assembled; explodedPosition = exploded; }
        public void ApplyExplosion(float progress) => transform.localPosition = Vector3.Lerp(assembledPosition, explodedPosition, progress);
        public void SetHighlighted(bool highlighted)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if (!highlighted) { renderer.SetPropertyBlock(null); continue; }
                var block = new MaterialPropertyBlock();
                var color = Color.Lerp(renderer.sharedMaterial.color, new Color(.5f, .92f, 1f), .4f);
                block.SetColor("_BaseColor", color); block.SetColor("_Color", color); renderer.SetPropertyBlock(block);
            }
        }
    }
}
