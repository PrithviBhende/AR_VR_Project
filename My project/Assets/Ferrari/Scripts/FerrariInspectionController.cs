using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace FerrariLab
{
    // Inspection owns shared camera/turntable; assembly owns pointer placement in its mode.
    public sealed class FerrariInspectionController : MonoBehaviour
    {
        public Transform displayRoot;
        public Camera viewCamera;
        public FerrariPart[] parts;
        public bool IsExploded { get; private set; }
        public bool IsAnimating { get; private set; }
        public bool IsDragging { get; private set; }
        public FerrariPart Selected { get; private set; }
        public bool AutoRotate { get; set; }
        public float ZoomFactor => zoom;
        public FerrariAssemblyController Assembly { get; private set; }
        public string Status { get; private set; } = "Select a part, or explode the car to explore inside.";
        readonly Dictionary<FerrariPart, Vector3> exploded = new Dictionary<FerrariPart, Vector3>();
        readonly Dictionary<FerrariPart, Renderer[]> renderers = new Dictionary<FerrariPart, Renderer[]>();
        readonly Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
        Material highlight;
        Vector3[] starts;
        Quaternion[] rotations;
        float transition;
        Vector2 pressPosition;
        Plane dragPlane;
        Vector3 grabOffset;
        bool pendingDrag, rotating;
        float distance = 11, requestedDistance = 11, zoom = 1;
        Vector3 focus = new Vector3(0, .8f, 0);
        static readonly Vector3 ViewDirection = new Vector3(1, .65f, -1).normalized;

        void Awake()
        {
            Application.runInBackground = true;
            if (parts == null || parts.Length == 0) parts = displayRoot.GetComponentsInChildren<FerrariPart>();
            highlight = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            highlight.color = new Color(.12f, .85f, 1f); highlight.SetFloat("_Metallic", .25f);
            highlight.SetFloat("_Smoothness", .6f); highlight.SetFloat("_Cull", 0);
            foreach (var p in parts)
            {
                renderers[p] = p.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers[p]) originalMaterials[r] = r.sharedMaterials;
                ConfigurePart(p);
                exploded[p] = p.assembledLocalPosition + p.transform.parent.InverseTransformVector(displayRoot.TransformVector(Offset(p.partId)));
            }
            // Keep each complete assembly above the plate, including wide, thin panels.
            foreach (var p in parts)
            {
                p.transform.localPosition = exploded[p];
                var b = BoundsOf(p);
                if (b.min.y < .22f) p.transform.position += Vector3.up * (.22f - b.min.y);
                exploded[p] = p.transform.localPosition;
                p.RestoreAssembledPose();
            }
            Assembly=gameObject.AddComponent<FerrariAssemblyController>();Assembly.Initialize(this);
            gameObject.AddComponent<FerrariInspectionUI>().controller = this;
        }

        static Vector3 Offset(string id)
        {
            switch (id)
            {
                case "bonnet": return new Vector3(1.25f, 2.4f, 0);
                case "bumper_F": return new Vector3(2.2f, .45f, 0);
                case "bumper_R": return new Vector3(-2.2f, .45f, 0);
                case "door_L": return new Vector3(0, .6f, 2.4f);
                case "door_R": return new Vector3(0, .6f, -2.4f);
                case "engine": return new Vector3(-1.25f, 1.7f, 0);
                case "engine_cover": return new Vector3(-1.1f, 3.7f, 0);
                case "rear_wing": return new Vector3(-2.5f, 2.7f, 0);
                case "seat_L": return new Vector3(0, 2.3f, .8f);
                case "seat_R": return new Vector3(0, 2.3f, -.8f);
                case "steering_wheel": return new Vector3(.45f, 3.4f, 0);
                case "wheel_FL": case "wheel_RL": return new Vector3(0, .5f, 1.6f);
                case "wheel_FR": case "wheel_RR": return new Vector3(0, .5f, -1.6f);
                default: return Vector3.zero;
            }
        }

        static void ConfigurePart(FerrariPart p)
        {
            switch (p.partId)
            {
                case "body": p.displayName="Body & cockpit"; p.description="The main body group gives the car its shape and surrounds the passenger compartment. This imported visual assembly also contains fixed trim and structural-looking surfaces; it is not a separately engineered chassis."; break;
                case "bonnet": p.displayName="Front bonnet"; p.description="The bonnet closes the front compartment. On this rear-engined car it is not the engine cover. Removing it helps reveal how the exterior panels fit around the central body."; break;
                case "bumper_F": p.displayName="Front bumper"; p.description="The front bumper assembly forms the nose of the vehicle. Its openings and shape guide airflow. This model illustrates panel placement, not crash performance."; break;
                case "bumper_R": p.displayName="Rear bumper"; p.description="The rear bumper completes the back of the body. It sits behind the engine region and integrates exterior trim. The visual mesh does not simulate impact absorption."; break;
                case "door_L": case "door_R": p.displayName=p.partId=="door_L"?"Left door":"Right door"; p.description="The door provides access to the cockpit and closes the side opening. In this learning view it separates as one assembly; the prototype does not simulate its hinges or locking mechanism."; break;
                case "engine": p.displayName="Rear engine assembly"; p.description="The engine converts fuel energy into mechanical power to drive the vehicle. It is located behind the passenger compartment in this car. The source mesh is a visual engine group, not a working mechanical simulation."; break;
                case "engine_cover": p.displayName="Engine cover"; p.description="This rear panel encloses the engine region. Separating it exposes the engine assembly underneath. It is distinct from the bonnet at the front of the car."; break;
                case "rear_wing": p.displayName="Rear aerodynamic assembly"; p.description="This rear exterior group contributes to the car's aerodynamic shape. Wings and spoilers can influence airflow and stability. No aerodynamic forces are calculated in this prototype."; break;
                case "seat_L": case "seat_R": p.displayName=p.partId=="seat_L"?"Left seat":"Right seat"; p.description="The seat supports and positions an occupant inside the cockpit. Its assembled position must remain aligned with the cabin floor. Safety restraints and seat adjustment are outside this prototype."; break;
                case "steering_wheel": p.displayName="Steering wheel"; p.description="The steering wheel is the driver's directional control. In a real vehicle it connects to the steering system. Here it is an individually selectable visual part with a saved cockpit position."; break;
                default: p.displayName=(p.partId.Contains("F")?"Front":"Rear")+(p.partId.EndsWith("L")?" left wheel":" right wheel"); p.description="The wheel assembly combines the visible tyre and rim. The tyre contacts the road and provides grip, while the rim supports it. Each wheel has its own saved position; tyre forces and suspension motion are not simulated."; break;
            }
        }

        public Bounds BoundsOf(FerrariPart p)
        {
            var rs=renderers[p]; var b=rs[0].bounds;
            for(int i=1;i<rs.Length;i++) b.Encapsulate(rs[i].bounds);
            return b;
        }
        public Vector3 ExplodedLocalPosition(FerrariPart p) => exploded[p];

        public void Select(FerrariPart p)
        {
            if (Selected) foreach(var r in renderers[Selected]) r.sharedMaterials=originalMaterials[r];
            Selected=p;
            if (p) foreach(var r in renderers[p]) r.sharedMaterials=Enumerable.Repeat(highlight,originalMaterials[r].Length).ToArray();
            Status = p ? (IsExploded ? "Selected: "+p.displayName+". Drag its mesh to move it." : "Selected: "+p.displayName+". Explode to move this part.") : "Select a part in the scene or the list.";
        }
        public void SetExploded(bool value, bool instant=false)
        {
            if(Assembly && Assembly.IsActive)return;
            CancelPointer(); AutoRotate=false; IsExploded=value;
            starts=parts.Select(p=>p.transform.localPosition).ToArray(); rotations=parts.Select(p=>p.transform.localRotation).ToArray();
            transition=0; IsAnimating=true;
            Status=value?"Exploded inspection: click and drag a part. Middle-drag turns the entire display.":"Assembled view: select a part to learn about it.";
            if(instant) { ApplyPose(1); IsAnimating=false; }
        }
        void ApplyPose(float t)
        {
            for(int i=0;i<parts.Length;i++)
            {
                var p=parts[i]; p.transform.localPosition=Vector3.Lerp(starts[i],IsExploded?exploded[p]:p.assembledLocalPosition,t);
                p.transform.localRotation=Quaternion.Slerp(rotations[i],p.assembledLocalRotation,t);
                p.transform.localScale=p.assembledLocalScale;
            }
        }
        public void ResetView() { if(Assembly && Assembly.IsActive){Assembly.Restart();return;} displayRoot.localRotation=Quaternion.identity; zoom=1; Select(null); SetExploded(false); }
        public void Turn(float degrees) { if(!IsDragging&&!pendingDrag&&!(Assembly&&Assembly.IsBusy)) displayRoot.Rotate(Vector3.up,degrees,Space.Self); }
        public void ResetLayout() { SetExploded(IsExploded); }
        void CancelPointer() { pendingDrag=false; IsDragging=false; rotating=false; }
        void OnApplicationFocus(bool focused) { if(!focused) CancelPointer(); }
        void OnDisable() { CancelPointer(); }
        void OnDestroy() { if(highlight) Destroy(highlight); }
        bool OverUI => EventSystem.current && EventSystem.current.IsPointerOverGameObject();

        void Update()
        {
            bool assembling=Assembly&&Assembly.IsActive;
            if(Assembly&&Assembly.IsBusy){CancelPointer();return;}
            if(IsAnimating) { transition+=Time.deltaTime/.75f; ApplyPose(Mathf.SmoothStep(0,1,transition)); if(transition>=1)IsAnimating=false; }
            var mouse=Mouse.current;
            if(mouse!=null)
            {
                Vector2 pos=mouse.position.ReadValue();
                bool inView=viewCamera.pixelRect.Contains(pos);
                if(mouse.middleButton.wasPressedThisFrame && inView && !OverUI && !pendingDrag && !IsDragging) rotating=true;
                if(!mouse.middleButton.isPressed) rotating=false;
                if(rotating) Turn(-mouse.delta.ReadValue().x*.3f);
                if(!assembling && mouse.leftButton.wasPressedThisFrame && !rotating && inView && !OverUI && !IsAnimating)
                {
                    var ray=viewCamera.ScreenPointToRay(pos);
                    if(Physics.Raycast(ray,out var hit,100))
                    {
                        var p=hit.collider.GetComponentInParent<FerrariPart>(); Select(p);
                        if(p && IsExploded) { pendingDrag=true; pressPosition=pos; dragPlane=new Plane(viewCamera.transform.forward,hit.point); grabOffset=p.transform.position-hit.point; AutoRotate=false; }
                    } else Select(null);
                }
                if(pendingDrag && mouse.leftButton.isPressed)
                {
                    if((pos-pressPosition).sqrMagnitude>25) IsDragging=true;
                    if(IsDragging)
                    {
                        // Keep the drag point in the 3D viewport; UI never moves the object off screen.
                        var rect=viewCamera.pixelRect;
                        pos=new Vector2(Mathf.Clamp(pos.x,rect.xMin+24,rect.xMax-24),Mathf.Clamp(pos.y,rect.yMin+24,rect.yMax-24));
                        var ray=viewCamera.ScreenPointToRay(pos);
                        if(dragPlane.Raycast(ray,out float enter)) MoveSelectedTo(ray.GetPoint(enter)+grabOffset);
                    }
                }
                if(!mouse.leftButton.isPressed) { pendingDrag=false; IsDragging=false; }
                if(inView&&!OverUI&&!IsDragging&&!pendingDrag) zoom=Mathf.Clamp(zoom*Mathf.Exp(-mouse.scroll.ReadValue().y*.0045f),.35f,2.2f);
            }
            var keyboard=Keyboard.current;
            if(keyboard!=null && !pendingDrag)
            {
                if(!assembling&&keyboard.eKey.wasPressedThisFrame) SetExploded(true);
                if(!assembling&&keyboard.aKey.wasPressedThisFrame) SetExploded(false);
                if(keyboard.rKey.wasPressedThisFrame) ResetView();
                if(keyboard.leftArrowKey.isPressed) Turn(45*Time.deltaTime);
                if(keyboard.rightArrowKey.isPressed) Turn(-45*Time.deltaTime);
            }
            if(AutoRotate&&!IsDragging&&!pendingDrag&&!rotating) Turn(15*Time.deltaTime);
        }

        public void MoveSelectedTo(Vector3 worldPosition)
        {
            if(!Selected||!IsExploded||IsAnimating) return;
            var local=displayRoot.InverseTransformPoint(worldPosition);
            local.x=Mathf.Clamp(local.x,-5.5f,5.5f);local.z=Mathf.Clamp(local.z,-5.5f,5.5f);local.y=Mathf.Clamp(local.y,.2f,5.5f);
            Selected.transform.position=displayRoot.TransformPoint(local);
            var b=BoundsOf(Selected);
            if(b.min.y<.20f) Selected.transform.position+=Vector3.up*(.20f-b.min.y);
        }

        void LateUpdate()
        {
            if(IsDragging||pendingDrag||(Assembly&&Assembly.IsBusy)) return; // No camera drift under the cursor while dragging.
            var b=new Bounds(new Vector3(0,.15f,0),new Vector3(6,.3f,6));
            foreach(var p in parts) b.Encapsulate(BoundsOf(p));
            var target=new Vector3(0,b.center.y,0);
            float vf=viewCamera.fieldOfView*Mathf.Deg2Rad*.5f;
            float hf=Mathf.Atan(Mathf.Tan(vf)*viewCamera.aspect);
            // Fit projected bounds, including depth, instead of an oversized enclosing sphere.
            // This keeps the model legible while smoothly adapting as the turntable rotates.
            var right=Vector3.Cross(Vector3.up,ViewDirection).normalized;
            var up=Vector3.Cross(ViewDirection,right).normalized;
            requestedDistance=6;
            FitBounds(new Bounds(new Vector3(0,.075f,0),new Vector3(6,.15f,6)),target,right,up,hf,vf);
            foreach(var p in parts)FitBounds(BoundsOf(p),target,right,up,hf,vf);
            requestedDistance*=1.12f;
            distance=Mathf.Lerp(distance,requestedDistance*zoom,1-Mathf.Exp(-10*Time.deltaTime));
            focus=Vector3.Lerp(focus,target,1-Mathf.Exp(-5*Time.deltaTime));
            viewCamera.transform.position=focus+ViewDirection*distance; viewCamera.transform.LookAt(focus);
        }
        void FitBounds(Bounds bounds,Vector3 target,Vector3 right,Vector3 up,float hf,float vf)
        {
            for(int i=0;i<8;i++)
            {
                var q=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))-target;
                float depth=Vector3.Dot(q,ViewDirection);
                requestedDistance=Mathf.Max(requestedDistance,Mathf.Abs(Vector3.Dot(q,right))/Mathf.Tan(hf)+depth,Mathf.Abs(Vector3.Dot(q,up))/Mathf.Tan(vf)+depth);
            }
        }
    }
}
