using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FerrariLab
{
    // One physical object per stable part ID. Inventory entries are views, never spawned parts.
    public sealed class FerrariAssemblyController : MonoBehaviour
    {
        public const float SnapRadius = .50f;
        public FerrariInspectionController inspection { get; private set; }
        public FerrariAccessAnimator Access { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsDragging { get; private set; }
        public bool IsSnapping { get; private set; }
        public bool IsBusy => IsDragging || IsSnapping;
        public FerrariPart CurrentPart { get; private set; }
        public FerrariPart PreviewPart { get; private set; }
        public FerrariPart TargetPart => CurrentPart ? CurrentPart : PreviewPart;
        public int PlacedCount => placed.Count;
        public int TotalCount => candidates.Length;
        public bool IsComplete => PlacedCount == TotalCount;
        public int Revision { get; private set; }
        public string Status { get; private set; } = "Drag a part from the list to its matching target.";
        public IReadOnlyList<FerrariPart> Available => available;
        public bool CanSnap => CurrentPart && CurrentPart.gameObject.activeSelf && Vector3.Distance(CurrentPart.transform.position,TargetPosition(CurrentPart)) <= SnapRadius;
        readonly HashSet<string> placed = new HashSet<string>();
        readonly List<FerrariPart> available = new List<FerrariPart>();
        FerrariPart[] candidates;
        Plane dragPlane;
        Coroutine snap;

        public void Initialize(FerrariInspectionController owner)
        {
            inspection=owner;
            candidates=owner.parts.Where(p=>p.assemblyCandidate).ToArray();
            Access=gameObject.AddComponent<FerrariAccessAnimator>();Access.Initialize(this);
            RefreshInventory();
        }
        void RefreshInventory()
        {
            available.Clear();available.AddRange(candidates.Where(p=>!placed.Contains(p.partId)&&p!=CurrentPart));Revision++;
        }
        public bool IsPlaced(FerrariPart p) => placed.Contains(p.partId);
        public Vector3 TargetPosition(FerrariPart p) => p.transform.parent.TransformPoint(p.assembledLocalPosition);
        public void ToggleMode() { if(IsActive) Exit(); else Enter(); }
        public void Enter()
        {
            if(IsActive)return;
            inspection.Select(null);inspection.SetExploded(false,true);inspection.AutoRotate=false;
            IsActive=true;
            foreach(var p in inspection.parts){p.RestoreAssembledPose();p.gameObject.SetActive(!p.assemblyCandidate||placed.Contains(p.partId));}
            Status=IsComplete?"Assembly complete! Restart to play again.":"Drag a part from the looping list to its matching target.";
            RefreshInventory();
        }
        public void Exit()
        {
            if(!IsActive)return;
            CancelDrag();Access.RestoreClosedImmediately();PreviewPart=null;inspection.Select(null);IsActive=false;
            foreach(var p in inspection.parts)p.gameObject.SetActive(true);
            inspection.SetExploded(false,true);Revision++;
        }
        public void Restart()
        {
            if(!IsActive)return;
            CancelDrag();Access.RestoreClosedImmediately();placed.Clear();PreviewPart=null;inspection.Select(null);inspection.AutoRotate=false;
            inspection.displayRoot.localRotation=Quaternion.identity;
            foreach(var p in inspection.parts){p.RestoreAssembledPose();p.gameObject.SetActive(!p.assemblyCandidate);}
            Status="New assembly: drag any of the 15 parts to its target.";RefreshInventory();
        }
        public void Preview(FerrariPart p)
        {
            if(!IsActive||IsBusy||!available.Contains(p))return;
            PreviewPart=p;Access.RequestFor(p);Revision++;Status="Read the placement clue, then drag "+p.displayName+" onto its target.";
        }
        public bool BeginDrag(FerrariPart p,Vector2 screenPosition)
        {
            if(!IsActive||IsBusy||!p||!available.Contains(p))return false;
            inspection.AutoRotate=false;inspection.Select(null);CurrentPart=p;PreviewPart=p;IsDragging=true;
            Access.RequestFor(p);
            p.RestoreAssembledPose();p.transform.localRotation=p.assembledLocalRotation*Quaternion.Euler(0,25,0);
            dragPlane=new Plane(inspection.viewCamera.transform.forward,TargetPosition(p));
            RefreshInventory();DragPointer(screenPosition);return true;
        }
        public void DragPointer(Vector2 screenPosition)
        {
            if(!IsDragging||!CurrentPart)return;
            bool inView=inspection.viewCamera.pixelRect.Contains(screenPosition);
            CurrentPart.gameObject.SetActive(inView);
            if(!inView){Status="Move into the car view; release over the list to return this part.";return;}
            var ray=inspection.viewCamera.ScreenPointToRay(screenPosition);
            if(!dragPlane.Raycast(ray,out float enter))return;
            var local=inspection.displayRoot.InverseTransformPoint(ray.GetPoint(enter));
            local.x=Mathf.Clamp(local.x,-5,5);local.z=Mathf.Clamp(local.z,-5,5);local.y=Mathf.Clamp(local.y,.16f,4.5f);
            CurrentPart.transform.position=inspection.displayRoot.TransformPoint(local);
            var bounds=inspection.BoundsOf(CurrentPart);
            if(bounds.min.y<.16f)CurrentPart.transform.position+=Vector3.up*(.16f-bounds.min.y);
            Status=CanSnap?"IN RANGE — release to snap "+CurrentPart.displayName+" into place.":"Move "+CurrentPart.displayName+" closer to the marked target.";
        }
        public bool Release(Vector2 screenPosition)
        {
            if(!IsDragging||!CurrentPart)return false;
            DragPointer(screenPosition);
            if(!inspection.viewCamera.pixelRect.Contains(screenPosition)||!CanSnap)
            {
                string name=CurrentPart.displayName;CancelDrag();Status="Not close enough: "+name+" returned to the list.";return false;
            }
            IsDragging=false;IsSnapping=true;Status=Access.ReadyForPlacement?"Placing "+CurrentPart.displayName+"...":"Opening access panel before placement...";
            snap=StartCoroutine(SnapIntoPlace());return true;
        }
        IEnumerator SnapIntoPlace()
        {
            var p=CurrentPart;var from=p.transform.localPosition;var rotation=p.transform.localRotation;
            // A quick drop must not push an interior part through a still-closed placed panel.
            while(!Access.ReadyForPlacement)yield return null;
            Status="Placing "+p.displayName+"...";
            for(float t=0;t<1;t+=Time.deltaTime/.4f)
            {
                float ease=Mathf.SmoothStep(0,1,t);
                p.transform.localPosition=Vector3.Lerp(from,p.assembledLocalPosition,ease);
                p.transform.localRotation=Quaternion.Slerp(rotation,p.assembledLocalRotation,ease);
                yield return null;
            }
            p.RestoreAssembledPose();placed.Add(p.partId);CurrentPart=null;PreviewPart=null;IsSnapping=false;snap=null;
            Access.Close();
            Status=IsComplete?"COMPLETE — all 15 parts assembled. Restart to play again!":"Placed "+p.displayName+". Choose the next part.";
            RefreshInventory();
        }
        public void CancelDrag()
        {
            if(snap!=null){StopCoroutine(snap);snap=null;}
            if(CurrentPart){CurrentPart.RestoreAssembledPose();CurrentPart.gameObject.SetActive(placed.Contains(CurrentPart.partId));}
            CurrentPart=null;IsDragging=false;IsSnapping=false;PreviewPart=null;RefreshInventory();
            Access.Close();
            Status="Part returned to the list. Choose another part.";
        }
        void Update()
        {
            if(!IsActive)return;
            if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)CancelDrag();
        }
        void OnApplicationFocus(bool focused){if(!focused&&IsActive){CancelDrag();Access.RestoreClosedImmediately();}}
        void OnDisable(){if(candidates!=null){if(IsBusy)CancelDrag();Access.RestoreClosedImmediately();}}
    }
}
