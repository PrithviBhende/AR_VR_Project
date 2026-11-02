import os
from pptx import Presentation
from pptx.util import Inches, Pt
from pptx.enum.shapes import MSO_SHAPE
from pptx.enum.text import PP_ALIGN
from pptx.dml.color import RGBColor

def create_presentation():
    prs = Presentation()
    prs.slide_width = Inches(13.333)
    prs.slide_height = Inches(7.5)
    blank_layout = prs.slide_layouts[6]

    # Color Palette
    BG_COLOR = RGBColor(10, 15, 26)        # Deep Charcoal Navy
    CARD_BG = RGBColor(16, 24, 38)         # Frosted Dark Glass
    TECH_BG = RGBColor(13, 20, 32)         # Technical Deep Dive Panel
    BORDER_CYAN = RGBColor(0, 210, 255)    # Neon Cyan
    BORDER_MUTED = RGBColor(32, 52, 78)    # Slate Blue Outline
    BORDER_RED = RGBColor(255, 60, 40)     # Ferrari Red
    TEXT_WHITE = RGBColor(255, 255, 255)
    TEXT_LIGHT = RGBColor(210, 225, 240)
    TEXT_CYAN = RGBColor(0, 210, 255)
    TEXT_GOLD = RGBColor(255, 195, 50)
    TEXT_MUTED = RGBColor(140, 165, 195)

    base_dir = r"C:\Users\prith\Documents\MYSPACE\PROJECTS\AR VR"
    uploaded_dir = r"C:\Users\prith\.gemini\antigravity\brain\906f6145-8fa6-4b4b-9dfb-57e1aa038d49\.user_uploaded"
    evidence_dir = os.path.join(base_dir, "Evidence")

    slides_data = [
        # SLIDE 1: INTRO (PRESENTER 1)
        {
            "presenter": "PRESENTER 1: PROJECT OVERVIEW & CONCEPT",
            "presenter_color": BORDER_CYAN,
            "title": "FERRARI ENZO EXPLORATION LAB",
            "subtitle": "Interactive Digital Twin, Exploded-View Kinematics & Multimodal AI Assistant",
            "image": os.path.join(uploaded_dir, "media_1793643812785_00dd0b4c.png"),
            "bullets": [
                ("Interactive Automotive Experience:", " A full 3D digital replica of the iconic Ferrari Enzo built in Unity 6, combining real-time visual fidelity, mechanical assembly learning, and conversational AI."),
                ("Multi-Disciplinary Innovation:", " Bridges mechanical CAD design, game engine kinematics, sci-fi glassmorphic UI, and on-device neural voice intelligence."),
                ("Seamless Real-Time Interaction:", " Explore the car as an assembled supercar, trigger smooth exploded-view disassembly, or solve the self-assembly puzzle piece by piece.")
            ],
            "tech_title": "HIGH-LEVEL SYSTEM ARCHITECTURE",
            "tech_body": "Built on Unity 6 (URP Forward+). The digital twin is decomposed into 16 independent sub-assemblies with discrete pivot references, synchronized with an asynchronous Python FastAPI microservice powering Gemini 2.5 AI and local Whisper voice capture.",
            "caption_title": "LIVE SHOWROOM DEMO",
            "caption_items": [
                "• 360° Motorized Turntable with inertial damping and manual angle stepping",
                "• Holographic Heads-Up Display featuring frosted glass panels and neon accents",
                "• Ambient hangar environment with volumetric lighting and floor reflections"
            ],
            "notes": (
                "PRESENTER 1 SCRIPT (Slide 1 of 2):\n"
                "• Introduction: 'Welcome everyone. Today our team is presenting the Ferrari Enzo Exploration Lab—an interactive 3D digital twin platform built from the ground up in Unity 6.'\n"
                "• Purpose: 'Our goal was to transform passive CAD models into an active, gamified learning lab where users can inspect parts, explode the supercar to see its inner engineering, and even converse with an AI automotive tutor.'\n"
                "• High-Level Overview: 'The project spans four core pillars: CAD decomposition in Blender, real-time URP graphics, mathematical exploded kinematics, and an AI voice tutor named Zeno.'\n"
                "• Technical Cue: 'Everything runs in real time at 60+ FPS with sub-150ms voice latency.'"
            )
        },

        # SLIDE 2: CAD PREPARATION (PRESENTER 1)
        {
            "presenter": "PRESENTER 1: 3D CAD & MESH PIPELINE",
            "presenter_color": BORDER_CYAN,
            "title": "3D CAD DECOMPOSITION & OPTIMIZATION",
            "subtitle": "Hierarchical Mesh Separation, Origin Normalization & Pivot Alignment in Blender 5.1",
            "image": os.path.join(uploaded_dir, "media_1793644725696_451aaf1d.png"),
            "bullets": [
                ("16 Modular Sub-Assemblies:", " Deconstructed raw CAD into 16 distinct mechanical components: Monocoque Chassis, V12 Engine, Active Diffusers, Carbon Brakes, and Wheels."),
                ("Origin Normalization:", " Set individual local pivot origins for every assembly rather than sharing a single world center, enabling independent rotation and explosion vectors."),
                ("Clean Mesh Hierarchy:", " Removed non-manifold geometry, recalculated normal vectors, and structured collections to import cleanly into Unity with zero transform drift.")
            ],
            "tech_title": "TECHNICAL DEEP DIVE: PIVOT & TRANSFORM NORMALIZATION",
            "tech_body": "In raw CAD, all vertices frequently share a root (0,0,0) transform. In Blender, we executed 'Origin to Geometry Center' and aligned local coordinate frames. This mathematical baseline ensures exploded expansion vectors translate along clean outward normals without mesh distortion.",
            "caption_title": "BLENDER 5.1 WORKSPACE",
            "caption_items": [
                "• Transform Coordinates: Wheel_RL localized at (X: 1.527m, Y: -0.789m, Z: 0.351m)",
                "• Collection Hierarchy: Clean parenting under FerrariEnzo root container",
                "• Optimized Polycount: High visual fidelity with real-time performance budget"
            ],
            "notes": (
                "PRESENTER 1 SCRIPT (Slide 2 of 2):\n"
                "• Explanation: 'Before any coding in Unity, the raw 3D model needed extensive CAD preparation. In raw models, cars are often exported as a single solid mesh or have all their pivot points stuck at the floor.'\n"
                "• The Problem: 'If you try to explode or rotate parts without individual pivots, they fly off in erratic directions or spin wildly off-center.'\n"
                "• Our Solution in Blender: 'We segmented the Ferrari Enzo into 16 logical engineering assemblies. For each part, such as the rear wheel shown here, we baked the pivot point directly into its geometric center and normalized local axes.'\n"
                "• Transition to Presenter 2: 'With our clean CAD assets exported, Presenter 2 will now explain how we brought them to life inside Unity 6 with real-time shaders and lighting.'"
            )
        },

        # SLIDE 3: ENVIRONMENT & SHADERS (PRESENTER 2)
        {
            "presenter": "PRESENTER 2: GRAPHICS & ENVIRONMENT",
            "presenter_color": BORDER_RED,
            "title": "REAL-TIME ENVIRONMENT & PBR SHADING",
            "subtitle": "Unity 6 URP Pipeline, Custom Clearcoat Automotive Shaders & Neon Hangar",
            "image": os.path.join(uploaded_dir, "media_1793644729692_c97f8f1e.png"),
            "bullets": [
                ("Universal Render Pipeline (URP):", " Leveraged Unity 6 URP Forward+ rendering path for high-performance multi-light shading and sharp reflections on high-poly meshes."),
                ("Multi-Layer Automotive Paint:", " Engineered realistic Ferrari Rosso Corsa red using a dual-layer PBR shader: a metallic basecoat combined with a high-specular clearcoat lacquer."),
                ("Cyberpunk Automotive Hangar:", " Built an atmospheric test lab featuring glowing cyan floor grids, a motorized 360° turntable, and ambient industrial hangar architecture.")
            ],
            "tech_title": "TECHNICAL DEEP DIVE: MODULAR C# SCRIPT ARCHITECTURE",
            "tech_body": "Assets/Ferrari/Scripts encapsulates the core runtime: FerrariInspectionUI manages the HUD raycasts and animations, FerrariAssemblyManager controls spatial kinematics, and FerrariPartData stores metadata, audio guides, and localized snap thresholds.",
            "caption_title": "UNITY 6 SCENE & ASSET HIERARCHY",
            "caption_items": [
                "• Unity 6.4 (6000.4.11f1) with DirectX 12 hardware acceleration",
                "• Modular script separation: UI, kinematics, audio, and network AI",
                "• Post-processing stack: ACES Tonemapping, Bloom, and Ambient Occlusion"
            ],
            "notes": (
                "PRESENTER 2 SCRIPT (Slide 1 of 2):\n"
                "• Explanation: 'Thank you. Once the 3D meshes entered Unity 6, our priority was visual immersion and rock-solid 60 FPS performance.'\n"
                "• Shaders: 'Automotive paint is notoriously difficult to render. We used a multi-layer PBR shader to recreate authentic Ferrari Rosso Corsa red, giving it deep metallic flake reflections and a shiny outer clearcoat.'\n"
                "• Environment: 'We placed the car in a futuristic garage hangar with neon cyan floor tracks and an interactive motorized turntable that can spin 360 degrees or step angle by angle.'\n"
                "• Technical Architecture: 'In the project view, you can see our modular C# codebase. Each component has a single responsibility—from UI management to kinematic physics.'"
            )
        },

        # SLIDE 4: HOLOGRAPHIC UI (PRESENTER 2)
        {
            "presenter": "PRESENTER 2: INTERACTIVE UI & INSPECTION",
            "presenter_color": BORDER_RED,
            "title": "HOLOGRAPHIC GLASS UI & COMPONENT INSPECTION",
            "subtitle": "Procedural 9-Slice Frosted Glass, Cyan Selection Outlines & Pop-Out Specification Cards",
            "image": os.path.join(uploaded_dir, "media_1793643816153_d6296901.png"),
            "bullets": [
                ("Sci-Fi Glassmorphism:", " Created custom 9-slice rounded glass panels with illuminated neon borders, replacing flat rectangular UI with a sleek aerospace HUD aesthetic."),
                ("Dual-Mode Selection:", " Users can click directly on any 3D car part in the viewport or select from the right-hand assemblies scrollbar with instant bidirectional highlighting."),
                ("Dynamic Part Inspection:", " Selecting a part highlights its mesh in glowing cyan, isolates its transparent silhouette, and slides out an engineering card detailing materials and fitment.")
            ],
            "tech_title": "TECHNICAL DEEP DIVE: EVENT DRIVEN 9-SLICE UI & SPRITE RENDERING",
            "tech_body": "UI cards use procedural 9-slice sliced sprites (border margins 16px) preventing corner stretching at any resolution. Unity EventSystem raycasting links 3D MeshColliders to 2D UI cards with smooth spring lerping (Vector3.Lerp) and dynamic subtitle text rendering.",
            "caption_title": "COMPONENT INSPECTION IN ACTION",
            "caption_items": [
                "• Left Panel: Pop-out engineering specs, description, and 'WHERE IT FITS' guidance",
                "• Center Viewport: Glowing cyan mesh highlight on active component (Body & Cockpit)",
                "• Right Panel: Interactive scroll list with part thumbnail preview and state sync"
            ],
            "notes": (
                "PRESENTER 2 SCRIPT (Slide 2 of 2):\n"
                "• Explanation: 'Next, I developed the holographic user interface and inspection system.'\n"
                "• Glassmorphism Design: 'We wanted the interface to look like an Iron Man style aerospace HUD. We designed procedural 9-slice frosted glass panels with glowing cyan border outlines.'\n"
                "• Interaction: 'When a user clicks any part—like the front bumper shown here—the 3D car mesh lights up in cyan, and the left card pops out with technical specs and installation hints.'\n"
                "• Sound & Accessibility: 'We also built a custom circular volume slider and audio replay controls so users can listen to spoken engineering guides.'\n"
                "• Transition to Presenter 3: 'Now, Presenter 3 will explain our 3D exploded kinematics and the interactive self-assembly puzzle.'"
            )
        },

        # SLIDE 5: EXPLODED VIEW (PRESENTER 3)
        {
            "presenter": "PRESENTER 3: KINEMATICS & MECHANICS",
            "presenter_color": BORDER_CYAN,
            "title": "3D EXPLODED-VIEW KINEMATICS",
            "subtitle": "Multi-Axis Disassembly Vectors, Smooth-Step Interpolation & Spatial Offsets",
            "image": os.path.join(uploaded_dir, "media_1793643827545_0f4d2bf5.png"),
            "bullets": [
                ("One-Click 3D Disassembly:", " Pressing 'EXPLODE [E]' smoothly disassembles the Ferrari Enzo in full 3D space, revealing the chassis, powertrain, and internal components."),
                ("Radial Expansion Geometry:", " Every part moves along a mathematically calculated spatial vector radiating from the vehicle centerline, preventing clipping or visual overlaps."),
                ("Smooth Mechanical Interpolation:", " Non-linear acceleration and deceleration curves make the explosion feel mechanical and weightful rather than abrupt or robotic.")
            ],
            "tech_title": "TECHNICAL DEEP DIVE: MATHEMATICAL DISASSEMBLY VECTORS",
            "tech_body": "Each part's target position is computed as: P_exploded(i) = P_assembled(i) + α * Direction(i) * OffsetMagnitude. During runtime, positions are interpolated using Mathf.SmoothStep(0, 1, t) over a 1.2s duration with collision-free radial offsets.",
            "caption_title": "SUSPENDED KINEMATIC DISASSEMBLY",
            "caption_items": [
                "• Body panels, doors, and engine cover lift vertically and laterally",
                "• Front and rear bumpers project outward along vehicle longitudinal axis",
                "• Wheels and suspension assemblies translate radially outward along wheel axles"
            ],
            "notes": (
                "PRESENTER 3 SCRIPT (Slide 1 of 2):\n"
                "• Explanation: 'Thank you. One of the flagship features of our lab is the 3D Exploded View.'\n"
                "• The Goal: 'Supercars are tightly packaged engineering marvels. By pressing 'E' or clicking the Explode button, all 16 components smoothly detach and float in 3D space.'\n"
                "• Kinematic Math: 'We did not just push parts outward randomly. Each part has a customized directional vector. Bumpers move along the X-axis, doors lift outward and up, and wheels translate along their axle lines.'\n"
                "• Smooth Transitions: 'We used Hermite SmoothStep interpolation so parts accelerate smoothly and decelerate to a gentle stop without collision.'"
            )
        },

        # SLIDE 6: SELF-ASSEMBLY PUZZLE (PRESENTER 3)
        {
            "presenter": "PRESENTER 3: ASSEMBLY GAMEPLAY",
            "presenter_color": BORDER_CYAN,
            "title": "GAMIFIED SELF-ASSEMBLY & SPATIAL SNAPPING",
            "subtitle": "Interactive Reassembly Challenge, Real-Time Snap Tolerance & Looping Carousel",
            "image": os.path.join(uploaded_dir, "media_1793643831277_1bc94257.png"),
            "bullets": [
                ("Educational Assembly Mode:", " Strips the supercar down to its central monocoque core and challenges the user to reassemble the entire vehicle part by part."),
                ("Infinite Looping Part Tray:", " Unplaced components are stored in a sleek carousel at the bottom of the screen with quick navigation and real-time placement status."),
                ("Magnetic Snap Feedback:", " Dragging a part near its correct chassis socket triggers an automatic magnetic snap, accompanied by a satisfying mechanical latch sound and visual flash.")
            ],
            "tech_title": "TECHNICAL DEEP DIVE: 3D SPATIAL SNAP & TOLERANCE ENGINE",
            "tech_body": "Calculates Euclidean distance ||P_drag - P_target|| < SnapThreshold (0.35m) and orientation delta Angle(Q_drag, Q_target) < 25°. When validated, the system triggers a damped Slerp snap animation, updates the assembly state machine, and advances the part tray.",
            "caption_title": "SELF-ASSEMBLY IN PROGRESS",
            "caption_items": [
                "• Bare central chassis ready to receive mechanical and aerodynamic body panels",
                "• Ghost alignment guides show where remaining components attach",
                "• Bottom looping inventory displays remaining assemblies to be placed"
            ],
            "notes": (
                "PRESENTER 3 SCRIPT (Slide 2 of 2):\n"
                "• Explanation: 'Beyond just looking at the exploded view, we wanted an interactive challenge. That is where Self-Assembly Mode comes in.'\n"
                "• How it Works: 'When you enter Self Assembly, the car is dismantled down to its central frame. The unplaced parts line up in an infinite looping carousel at the bottom.'\n"
                "• Snapping Mechanics: 'The user selects a part and drags it into position. Our snapping algorithm calculates the distance and angle between the dragged part and its socket.'\n"
                "• Feedback: 'When brought within tolerance, the part snaps magnetically into place with a mechanical latch sound and a cyan flash!'\n"
                "• Transition to Presenter 4: 'Now Presenter 4 will present our conversational AI assistant, Zeno, and our future VR integration roadmap.'"
            )
        },

        # SLIDE 7: ZENO AI ASSISTANT (PRESENTER 4)
        {
            "presenter": "PRESENTER 4: AI & VOICE ASSISTANT",
            "presenter_color": BORDER_RED,
            "title": "ZENO AI: MULTIMODAL VOICE ASSISTANT",
            "subtitle": "On-Device Whisper Wake-Word ('Hey Zeno'), FastAPI Microservice & Gemini 2.5 Knowledge",
            "image": os.path.join(uploaded_dir, "media_1793643823733_68f00d3e.png"),
            "bullets": [
                ("Conversational Automotive Tutor:", " Users can talk directly to 'Zeno', asking natural questions like 'How much downforce does the front bumper generate?' or 'Explain the V12 engine.'"),
                ("Hands-Free Voice Activation:", " Built-in wake-word detection triggers on 'Hey Zeno' via Windows WASAPI audio capture, or through the holographic toolbar buttons."),
                ("Context-Aware Intelligence:", " Automatically injects the currently selected car component into the AI prompt, allowing Zeno to provide hyper-specific engineering explanations.")
            ],
            "tech_title": "TECHNICAL DEEP DIVE: FASTAPI MICROSERVICE & ASYNC PIPELINE",
            "tech_body": "Unity communicates via UnityWebRequest with an asynchronous Python FastAPI service (ZenoService). Audio is captured at 16kHz, transcribed locally via Whisper, processed through Gemini 2.5 with engineering system instructions, and spoken back via text-to-speech with visual subtitles.",
            "caption_title": "ZENO VOICE BAR ACTIVE",
            "caption_items": [
                "• Sleek glass toolbar with 'ASK ZENO', 'STOP VOICE', 'MUTE', and 'NEW CHAT'",
                "• Live microphone listener indicator: 'Listening on Windows microphone — say Hey Zeno'",
                "• Automatic backend recovery daemon ensures uninterrupted voice interaction"
            ],
            "notes": (
                "PRESENTER 4 SCRIPT (Slide 1 of 2):\n"
                "• Explanation: 'Thank you. I developed the multimodal voice assistant called Zeno AI.'\n"
                "• Vision: 'Instead of just reading static text cards, users can speak directly with an intelligent automotive engineering tutor.'\n"
                "• Natural Dialogue: 'You can say 'Hey Zeno' or click ASK ZENO and ask anything about the Ferrari Enzo. Zeno knows the car's aerodynamics, torque curves, carbon fiber chassis, and race history.'\n"
                "• Context Awareness: 'If you have the front bonnet selected, Zeno immediately understands you are asking about that specific part.'\n"
                "• Architecture: 'Under the hood, a lightweight Python FastAPI microservice handles local Whisper speech recognition and connects to Gemini 2.5 AI for expert engineering responses.'"
            )
        },

        # SLIDE 8: FUTURE WORK - VR INTEGRATION (PRESENTER 4)
        {
            "presenter": "PRESENTER 4: FUTURE VR ROADMAP",
            "presenter_color": BORDER_RED,
            "title": "FUTURE WORK: AR / VR HEADSET INTEGRATION",
            "subtitle": "OpenXR Multi-Platform Support, 6DoF Natural Hand Tracking & Spatial Passthrough",
            "image": os.path.join(evidence_dir, "vr_roadmap_diagram.png"),
            "bullets": [
                ("6DoF Direct Hand Tracking:", " Transition from mouse clicks to natural two-handed grabbing, pulling, and rotating parts using Meta Quest 3 and Apple Vision Pro optical hand tracking."),
                ("Life-Scale Mixed Reality:", " Passthrough AR will allow users to place a true 1:1 life-sized Ferrari Enzo onto their real physical garage or living room floor."),
                ("Collaborative Engineering Lab:", " Multi-user spatial networking allowing remote students and engineers to assemble, inspect, and discuss car mechanics in a shared VR room.")
            ],
            "tech_title": "TECHNICAL DEEP DIVE: OPENXR & HIGH-PERFORMANCE XR PIPELINE",
            "tech_body": "Migrating to Unity XR Interaction Toolkit (XRI 3.x) with the OpenXR standard runtime. Maintaining 90 FPS stereo rendering through Single-Pass Instancing, Dynamic Foveated Rendering, and GPU mesh level-of-detail (LOD) optimization to guarantee zero motion sickness.",
            "caption_title": "SPATIAL COMPUTING ARCHITECTURE",
            "caption_items": [
                "• Layer 1: Hardware & optical tracking (Quest 3, Vision Pro, Spatial Audio)",
                "• Layer 2: OpenXR runtime & Unity XR Interaction Toolkit (XRI)",
                "• Layer 3: Spatial snap assembly, physical haptics & holographic Zeno avatar"
            ],
            "notes": (
                "PRESENTER 4 SCRIPT (Slide 2 of 2):\n"
                "• Future Vision: 'While our desktop application is fully functional, our primary roadmap for future work is full AR and VR headset integration.'\n"
                "• 6DoF Hand Tracking: 'Imagine picking up the V12 engine cover with your actual hands, feeling haptic feedback as it snaps into the chassis, and walking around the car at true 1-to-1 scale.'\n"
                "• Mixed Reality: 'Using passthrough cameras on headsets like the Meta Quest 3 or Apple Vision Pro, you can project the Ferrari Enzo directly into your real room.'\n"
                "• OpenXR Standard: 'Because our C# architecture is modular, porting to OpenXR ensures universal support across all modern VR headsets while maintaining 90 FPS smoothness.'"
            )
        },

        # SLIDE 9: CONCLUSION & TEAM SUMMARY (ALL PRESENTERS)
        {
            "presenter": "ALL PRESENTERS: PROJECT CONCLUSION",
            "presenter_color": BORDER_CYAN,
            "title": "PROJECT CONCLUSION & TECHNICAL MILESTONES",
            "subtitle": "Verified End-to-End Pipeline, Automated Test Verification & Demonstration",
            "image": os.path.join(evidence_dir, "project_summary_card.png"),
            "bullets": [
                ("Complete End-to-End Pipeline:", " Successfully united CAD prep in Blender, real-time URP graphics, mathematical exploded kinematics, and neural voice AI into a unified Unity application."),
                ("100% Automated Test Pass Rate:", " Executed 42 automated headless verification tests covering UI raycasts, assembly state logic, audio sync, and voice backend communication—achieving 0 failures."),
                ("Modular & Scalable Foundation:", " Clean decoupled C# architecture ready for immediate expansion into classroom curricula, automotive showrooms, and spatial computing headsets.")
            ],
            "tech_title": "TECHNICAL MILESTONES & PERFORMANCE METRICS",
            "tech_body": "Delivers rock-solid 60+ FPS on standard hardware, <150ms local voice wake latency, 16 fully normalized mechanical CAD assemblies, and zero memory leaks across extended interactive inspection sessions.",
            "caption_title": "SYSTEM VERIFICATION STATUS",
            "caption_items": [
                "• 42 / 42 Automated Verification Checks Passed with 0 errors",
                "• 16 Independent Functional Sub-Assemblies with custom pivot math",
                "• Ready for live demonstration and questions from the panel"
            ],
            "notes": (
                "ALL PRESENTERS SCRIPT (Slide 9 wrap-up):\n"
                "• Summary: 'To conclude, our team has created a complete, verified digital twin experience combining mechanical engineering, game development, and modern AI.'\n"
                "• Testing & Reliability: 'We ran 42 automated tests covering every subsystem—from audio volume sync and button raycasts to Zeno's microservice—passing with 100% reliability.'\n"
                "• Team Wrap-Up: 'Thank you for your time. We are now excited to run a live demonstration of the Ferrari Enzo Exploration Lab and answer any questions!'"
            )
        }
    ]

    for idx, data in enumerate(slides_data):
        slide = prs.slides.add_slide(blank_layout)

        # 1. Slide Solid Background
        bg = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, 0, 0, prs.slide_width, prs.slide_height)
        bg.fill.solid()
        bg.fill.fore_color.rgb = BG_COLOR
        bg.line.fill.background()

        # 2. Presenter Pill Badge (Top Left)
        badge = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(0.8), Inches(0.4), Inches(4.3), Inches(0.35))
        badge.fill.solid()
        badge.fill.fore_color.rgb = RGBColor(18, 28, 45)
        badge.line.color.rgb = data["presenter_color"]
        badge.line.width = Pt(1.5)
        btf = badge.text_frame
        btf.word_wrap = True
        btf.margin_left = Inches(0.15)
        btf.margin_top = Inches(0.04)
        bp = btf.paragraphs[0]
        bp.text = data["presenter"]
        bp.font.size = Pt(10.5)
        bp.font.bold = True
        bp.font.name = "Segoe UI"
        bp.font.color.rgb = data["presenter_color"]

        # Slide Number Badge (Top Right)
        num_badge = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(11.3), Inches(0.4), Inches(1.2), Inches(0.35))
        num_badge.fill.solid()
        num_badge.fill.fore_color.rgb = RGBColor(18, 28, 45)
        num_badge.line.color.rgb = BORDER_MUTED
        num_badge.line.width = Pt(1.0)
        ntf = num_badge.text_frame
        ntf.margin_top = Inches(0.04)
        np = ntf.paragraphs[0]
        np.alignment = PP_ALIGN.CENTER
        np.text = f"{idx + 1} / 9"
        np.font.size = Pt(10.5)
        np.font.bold = True
        np.font.name = "Segoe UI"
        np.font.color.rgb = TEXT_MUTED

        # 3. Slide Title & Subtitle Box
        title_box = slide.shapes.add_textbox(Inches(0.8), Inches(0.78), Inches(11.7), Inches(0.95))
        ttf = title_box.text_frame
        ttf.word_wrap = True
        ttf.margin_left = Inches(0)
        ttf.margin_top = Inches(0)

        tp = ttf.paragraphs[0]
        tp.text = data["title"]
        tp.font.size = Pt(21)
        tp.font.bold = True
        tp.font.name = "Segoe UI"
        tp.font.color.rgb = TEXT_WHITE

        sp = ttf.add_paragraph()
        sp.text = data["subtitle"]
        sp.font.size = Pt(12)
        sp.font.name = "Segoe UI"
        sp.font.color.rgb = TEXT_CYAN

        # 4. LEFT COLUMN: Main Bullet Card
        left_card = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(0.8), Inches(1.85), Inches(5.8), Inches(3.2))
        left_card.fill.solid()
        left_card.fill.fore_color.rgb = CARD_BG
        left_card.line.color.rgb = BORDER_MUTED
        left_card.line.width = Pt(1.0)

        ltf = left_card.text_frame
        ltf.word_wrap = True
        ltf.margin_left = Inches(0.25)
        ltf.margin_right = Inches(0.25)
        ltf.margin_top = Inches(0.2)
        ltf.margin_bottom = Inches(0.2)

        for b_idx, (b_hdr, b_text) in enumerate(data["bullets"]):
            p = ltf.paragraphs[0] if b_idx == 0 else ltf.add_paragraph()
            p.space_after = Pt(8)
            p.font.size = Pt(11)
            p.font.name = "Segoe UI"

            run1 = p.add_run()
            run1.text = "• " + b_hdr
            run1.font.bold = True
            run1.font.color.rgb = TEXT_WHITE

            run2 = p.add_run()
            run2.text = b_text
            run2.font.bold = False
            run2.font.color.rgb = TEXT_LIGHT

        # 5. LEFT COLUMN: Technical Deep Dive Card
        tech_card = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(0.8), Inches(5.2), Inches(5.8), Inches(1.8))
        tech_card.fill.solid()
        tech_card.fill.fore_color.rgb = TECH_BG
        tech_card.line.color.rgb = BORDER_CYAN
        tech_card.line.width = Pt(1.5)

        ttf = tech_card.text_frame
        ttf.word_wrap = True
        ttf.margin_left = Inches(0.22)
        ttf.margin_right = Inches(0.22)
        ttf.margin_top = Inches(0.14)

        tp1 = ttf.paragraphs[0]
        tp1.text = "⚙  " + data["tech_title"]
        tp1.font.size = Pt(10.5)
        tp1.font.bold = True
        tp1.font.name = "Segoe UI"
        tp1.font.color.rgb = TEXT_CYAN
        tp1.space_after = Pt(4)

        tp2 = ttf.add_paragraph()
        tp2.text = data["tech_body"]
        tp2.font.size = Pt(9.5)
        tp2.font.name = "Segoe UI"
        tp2.font.color.rgb = TEXT_LIGHT

        # 6. RIGHT COLUMN: Framed Image Showcase
        img_x = Inches(6.85)
        img_y = Inches(1.85)
        img_w = Inches(5.68)
        img_h = Inches(3.02)

        # Image Frame Border
        frame = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, img_x - Inches(0.04), img_y - Inches(0.04), img_w + Inches(0.08), img_h + Inches(0.08))
        frame.fill.solid()
        frame.fill.fore_color.rgb = RGBColor(15, 24, 38)
        frame.line.color.rgb = data["presenter_color"]
        frame.line.width = Pt(1.5)

        if os.path.exists(data["image"]):
            slide.shapes.add_picture(data["image"], img_x, img_y, img_w, img_h)

        # 7. RIGHT COLUMN: Caption & Details Card
        cap_card = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(6.81), Inches(5.02), Inches(5.76), Inches(1.98))
        cap_card.fill.solid()
        cap_card.fill.fore_color.rgb = CARD_BG
        cap_card.line.color.rgb = BORDER_MUTED
        cap_card.line.width = Pt(1.0)

        ctf = cap_card.text_frame
        ctf.word_wrap = True
        ctf.margin_left = Inches(0.22)
        ctf.margin_right = Inches(0.22)
        ctf.margin_top = Inches(0.14)

        cp1 = ctf.paragraphs[0]
        cp1.text = "🔍  " + data["caption_title"]
        cp1.font.size = Pt(10.5)
        cp1.font.bold = True
        cp1.font.name = "Segoe UI"
        cp1.font.color.rgb = TEXT_GOLD
        cp1.space_after = Pt(4)

        for c_item in data["caption_items"]:
            cp = ctf.add_paragraph()
            cp.text = c_item
            cp.font.size = Pt(9.5)
            cp.font.name = "Segoe UI"
            cp.font.color.rgb = TEXT_LIGHT
            cp.space_after = Pt(2)

        # 8. Speaker Notes
        notes_slide = slide.notes_slide
        ntf = notes_slide.notes_text_frame
        ntf.text = data["notes"]

    out_file = os.path.join(base_dir, "Ferrari_Enzo_Exploration_Lab.pptx")
    prs.save(out_file)
    print("Presentation created successfully at:", out_file)
    return out_file

if __name__ == "__main__":
    create_presentation()
