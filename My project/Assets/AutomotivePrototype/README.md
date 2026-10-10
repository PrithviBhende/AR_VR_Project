# Automotive XR Lab - Initial Prototype

This Unity milestone demonstrates the central interaction proposed in the synopsis: an automotive assembly that can be separated into an exploded view and inspected component by component.

## Run the prototype

1. Open `Assets/AutomotivePrototype/Scenes/AutomotivePrototype.unity`.
2. Enter Play Mode.
3. Use the on-screen buttons or keyboard shortcuts.

## Controls

- Left click a component: show its name and educational description.
- Left mouse drag: rotate the disc, car and all components together.
- Right mouse drag: rotate the display and adjust viewing height.
- Turn Left / Turn Right buttons (or arrow keys): rotate the display.
- Auto Rotate button (or Space): start/pause turntable rotation.
- Mouse wheel: zoom.
- `E`: exploded view.
- `A`: assembled view.
- `R`: reset the display, camera and selection.
- `F12`: save a supersampled screenshot in `Evidence` beside the Unity project.
- **ARVR Prototype > Check Platform Clearance** checks geometry in Play Mode.
- For a clear presentation, maximize the Game tab, use a 16:9 view and turn off Low Resolution Aspect Ratios in the resolution dropdown.

## Implemented milestone features

- Real-time 3D educational environment.
- Sedan-style vehicle with wheel arches, glazed cabin, lights, grille and nine selectable assemblies.
- Engine and battery contained under the bonnet when assembled.
- Shared rotating platform hierarchy, including all exploded components.
- Upward-only explosion offsets; chassis remains above the platform.
- Smooth exploded and assembled views.
- Component highlighting and information display.
- Drag, button, keyboard and automatic turntable controls; zoom and viewing-height adjustment.
- Presentation-ready milestone interface.

HTC VIVE Focus 3 controller input and detailed imported vehicle models are intentionally reserved for the next implementation phase.
