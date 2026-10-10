using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FerrariLab
{
    // Illustrative hinges for the imported visual assemblies, not mechanically verified CAD joints.
    // All math is in each panel's unchanged parent space, so turntable rotation is independent.
    public sealed class FerrariAccessAnimator : MonoBehaviour
    {
        sealed class Panel
        {
            public FerrariPart part;
            public Vector3 pivot, axis;
            public float angle, amount, target;
        }
        readonly List<Panel> panels=new List<Panel>();
        FerrariAssemblyController assembly;
        public bool IsMoving => panels.Any(p=>Mathf.Abs(p.amount-p.target)>.001f);
        public bool ReadyForPlacement => panels.All(p=>p.target<.5f||p.amount>=.999f);
        public bool AnyOpen => panels.Any(p=>p.amount>.001f);
        public const float Duration=.70f;
        public float OpenAmount(string id) => panels.FirstOrDefault(p=>p.part.partId==id)?.amount??0;

        public void Initialize(FerrariAssemblyController owner)
        {
            assembly=owner;
            foreach(var part in owner.inspection.parts.Where(p=>p.partId=="door_L"||p.partId=="door_R"||p.partId=="engine_cover"||p.partId=="bonnet"))
            {
                var root=owner.inspection.displayRoot;
                var world=owner.inspection.BoundsOf(part);
                var bounds=new Bounds(root.InverseTransformPoint(world.center),Vector3.zero);
                for(int i=0;i<8;i++)bounds.Encapsulate(root.InverseTransformPoint(world.center+Vector3.Scale(world.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));
                Vector3 pivot,axis;float angle;
                if(part.partId.StartsWith("door_"))
                {
                    // Hinge near the front upper corner; lift the trailing door edge and swing it outward.
                    pivot=new Vector3(bounds.max.x-.10f,bounds.max.y-.25f,bounds.center.z);
                    axis=new Vector3(part.partId=="door_L"?-.65f:.65f,part.partId=="door_L"?.45f:-.45f,-1).normalized;angle=68;
                }
                else if(part.partId=="engine_cover")
                {
                    pivot=new Vector3(bounds.min.x+.04f,bounds.max.y-.06f,bounds.center.z);
                    axis=Vector3.forward;angle=66;
                }
                else
                {
                    // Front compartment service access when placing the front bumper assembly.
                    pivot=new Vector3(bounds.min.x+.05f,bounds.max.y-.06f,bounds.center.z);
                    axis=Vector3.forward;angle=55;
                }
                panels.Add(new Panel{part=part,pivot=part.transform.parent.InverseTransformPoint(root.TransformPoint(pivot)),axis=part.transform.parent.InverseTransformDirection(root.TransformDirection(axis)).normalized,angle=angle});
            }
        }
        public void RequestFor(FerrariPart interior)
        {
            string id=interior?interior.partId:"";
            foreach(var p in panels)
            {
                bool required=(p.part.partId=="door_L"&&(id=="seat_L"||id=="steering_wheel"))
                    ||(p.part.partId=="door_R"&&id=="seat_R")
                    ||(p.part.partId=="engine_cover"&&id=="engine")
                    ||(p.part.partId=="bonnet"&&id=="bumper_F");
                p.target=assembly.IsActive&&required&&assembly.IsPlaced(p.part)?1:0;
            }
        }
        public void Close(){foreach(var p in panels)p.target=0;}
        public void RestoreClosedImmediately()
        {
            foreach(var p in panels){p.target=0;p.amount=0;if(p.part)p.part.RestoreAssembledPose();}
        }
        void LateUpdate()
        {
            if(!assembly||!assembly.IsActive)return;
            foreach(var p in panels)
            {
                // Never animate or reveal an unplaced or currently dragged palette object.
                if(!assembly.IsPlaced(p.part)){p.amount=0;p.target=0;continue;}
                p.amount=Mathf.MoveTowards(p.amount,p.target,Time.deltaTime/Duration);
                if(p.amount<=0){p.part.RestoreAssembledPose();continue;}
                var rotation=Quaternion.AngleAxis(p.angle*Mathf.SmoothStep(0,1,p.amount),p.axis);
                p.part.transform.localPosition=p.pivot+rotation*(p.part.assembledLocalPosition-p.pivot);
                p.part.transform.localRotation=rotation*p.part.assembledLocalRotation;
                p.part.transform.localScale=p.part.assembledLocalScale;
            }
        }
        void OnDisable(){if(assembly)RestoreClosedImmediately();}
    }
}
