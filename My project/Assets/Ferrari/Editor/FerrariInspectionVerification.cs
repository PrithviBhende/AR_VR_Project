using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace FerrariLab.Editor
{
    public static class FerrariInspectionVerification
    {
        static readonly List<string> results=new List<string>();
        static string Workspace => Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
        static int failures;
        static void Check(bool valid,string label) { results.Add((valid?"PASS: ":"FAIL: ")+label);if(!valid)failures++; }
        [MenuItem("ARVR Prototype/Ferrari/Verify Member 2 (Play Mode)")]
        public static void Run()
        {
            var c=UnityEngine.Object.FindFirstObjectByType<FerrariInspectionController>();
            if(!Application.isPlaying||!c)throw new InvalidOperationException("Open FerrariInspection and enter Play Mode first.");
            results.Clear();failures=0;c.StartCoroutine(Verify(c));
        }
        static void MouseFrame(Mouse mouse, Vector2 position, ushort buttons=0, Vector2 delta=default)
        { InputSystem.QueueStateEvent(mouse,new MouseState{position=position,buttons=buttons,delta=delta}); }
        static Vector2 ButtonPoint(string title)
        {
            var button=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.name==title);
            var rt=(RectTransform)button.transform;return RectTransformUtility.WorldToScreenPoint(null,rt.TransformPoint(rt.rect.center));
        }
        static Vector2 PickPoint(FerrariInspectionController c,FerrariPart p)
        {
            Physics.SyncTransforms();
            foreach(var collider in p.GetComponentsInChildren<Collider>())
            {
                Vector3 screen=c.viewCamera.WorldToScreenPoint(collider.bounds.center);
                if(c.viewCamera.pixelRect.Contains(screen) && Physics.Raycast(c.viewCamera.ScreenPointToRay(screen),out var hit,100) && hit.collider.GetComponentInParent<FerrariPart>()==p)return screen;
            }
            // Wheel centres can be empty space between spokes. Search the actual projected mesh.
            var bounds=c.BoundsOf(p);Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);
            for(int i=0;i<8;i++)
            {
                var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                Vector2 screen=c.viewCamera.WorldToScreenPoint(corner);min=Vector2.Min(min,screen);max=Vector2.Max(max,screen);
            }
            for(float y=min.y;y<=max.y;y+=2)for(float x=min.x;x<=max.x;x+=2)
            {
                var point=new Vector2(x,y);
                if(c.viewCamera.pixelRect.Contains(point)&&Physics.Raycast(c.viewCamera.ScreenPointToRay(point),out var hit,100)&&hit.collider.GetComponentInParent<FerrariPart>()==p)return point;
            }
            return new Vector2(-100,-100);
        }
        static IEnumerator Verify(FerrariInspectionController c)
        {
            Mouse testMouse=null;
            var oldRouting=InputSystem.settings.editorInputBehaviorInPlayMode;
            var oldBackground=InputSystem.settings.backgroundBehavior;
            try
            {
                if(c.Assembly.IsActive)c.Assembly.Exit();
                InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                testMouse=InputSystem.AddDevice<Mouse>("FerrariVerificationMouse");
                MouseFrame(testMouse,new Vector2(5,5));
                c.ResetView();c.SetExploded(false,true);
                yield return new WaitForSecondsRealtime(3);
                Check(c.parts.Length==16 && c.parts.Select(p=>p.partId).Distinct().Count()==16,"16 unique part IDs");
                Check(c.parts.All(p=>p.transform.IsChildOf(c.displayRoot)),"All parts remain beneath the shared rotating root");
                Check(c.parts.All(p=>c.BoundsOf(p).min.y>=.149f),"Assembled mesh bounds above plate (Y >= 0.15)");
                Check(c.parts.All(p=>!p.description.Contains("to be authored")),"Educational descriptions assigned to all 16 parts");
                Check(c.parts.All(p=>p.GetComponentsInChildren<Collider>().Length>0),"Every assembly has selection colliders");
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(Workspace,"Evidence/Ferrari_Member2_Assembled.png"));
                var point=ButtonPoint("EXPLODE  [E]");
                MouseFrame(testMouse,point);yield return null;yield return null;
                MouseFrame(testMouse,point,1);yield return null;yield return null;
                MouseFrame(testMouse,point);yield return new WaitForSecondsRealtime(3);
                Check(c.IsExploded&&!c.IsAnimating,"Actual UI explode button completes animated transition");
                Check(c.parts.All(p=>c.BoundsOf(p).min.y>=.199f),"Every exploded assembly above base (Y >= 0.20)");
                foreach(var p in c.parts) results.Add("BOUNDS: "+p.partId+" center="+c.BoundsOf(p).center+" size="+c.BoundsOf(p).size);
                for(int i=0;i<c.parts.Length;i++) for(int j=i+1;j<c.parts.Length;j++)
                {
                    var a=c.BoundsOf(c.parts[i]);var b=c.BoundsOf(c.parts[j]);var penetration=Vector3.Min(a.max,b.max)-Vector3.Max(a.min,b.min);
                    if(penetration.x>.03f&&penetration.y>.03f&&penetration.z>.03f)results.Add("AABB OVERLAP REVIEW: "+c.parts[i].partId+" / "+c.parts[j].partId+" depths="+penetration);
                }
                var wheel=c.parts.Single(p=>p.partId=="wheel_FR");
                point=PickPoint(c,wheel);Check(point.x>=0,"Front right wheel is raycast-accessible in exploded view");
                MouseFrame(testMouse,point);yield return null;yield return null;
                var oldPosition=wheel.transform.position;var oldRotation=c.displayRoot.rotation;
                MouseFrame(testMouse,point,1);yield return null;yield return null;
                Check(c.Selected==wheel,"Mouse click selects the wheel through its mesh collider");
                MouseFrame(testMouse,point+new Vector2(70,40),1,new Vector2(70,40));yield return null;yield return null;
                Check(c.IsDragging&&Vector3.Distance(oldPosition,wheel.transform.position)>.02f,"Mouse drag moves the selected exploded part");
                c.Turn(30);Check(Quaternion.Angle(oldRotation,c.displayRoot.rotation)<.01f,"Display rotation blocked during part drag");
                MouseFrame(testMouse,point+new Vector2(70,40));yield return null;yield return null;
                Check(!c.IsDragging,"Mouse release ends dragging");
                c.Select(wheel);c.MoveSelectedTo(new Vector3(1,-100,0));Check(c.BoundsOf(wheel).min.y>=.199f,"Dragging below floor clamps the entire mesh above the base");
                c.MoveSelectedTo(new Vector3(100,2,100));var constrained=c.displayRoot.InverseTransformPoint(wheel.transform.position);Check(constrained.x<=5.51f&&constrained.z<=5.51f,"Extreme drag remains within the inspection area");
                c.ResetLayout();yield return new WaitForSecondsRealtime(1.2f);
                Check(c.parts.All(p=>Vector3.Distance(p.transform.localPosition,c.ExplodedLocalPosition(p))<.001f),"Reset layout restores all authored exploded positions");
                point=c.viewCamera.pixelRect.center;oldRotation=c.displayRoot.rotation;
                MouseFrame(testMouse,point);yield return null;yield return null;
                MouseFrame(testMouse,point,4);yield return null;yield return null;
                MouseFrame(testMouse,point+new Vector2(80,30),4,new Vector2(80,30));yield return null;yield return null;
                MouseFrame(testMouse,point+new Vector2(80,30));yield return null;yield return null;
                Check(Quaternion.Angle(oldRotation,c.displayRoot.rotation)>5,"Middle-mouse horizontal drag rotates the display");
                Check(Vector3.Angle(c.displayRoot.up,Vector3.up)<.01f,"Display rotation stays on vertical Y only");
                Check(c.parts.All(p=>Vector3.Distance(p.transform.localPosition,c.ExplodedLocalPosition(p))<.001f),"Exploded parts keep their local arrangement during root rotation");
                Check(c.displayRoot.Find("BasePlate")!=null,"Base plate belongs to same rotating root");
                foreach(var p in c.parts) { c.Select(p);Check(c.Selected==p&&p.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterials.All(m=>m.color.g>.7f)),"Selection highlight: "+p.partId); }
                c.Select(c.parts.Single(p=>p.partId=="engine"));
                c.displayRoot.localRotation=Quaternion.identity;MouseFrame(testMouse,new Vector2(5,5));
                yield return new WaitForSecondsRealtime(3);yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(Workspace,"Evidence/Ferrari_Member2_Exploded.png"));
                point=ButtonPoint("ASSEMBLE  [A]");MouseFrame(testMouse,point);yield return null;yield return null;
                MouseFrame(testMouse,point,1);yield return null;yield return null;MouseFrame(testMouse,point);yield return new WaitForSecondsRealtime(1.2f);
                Check(!c.IsExploded&&!c.IsAnimating,"Actual UI assemble button completes transition");
                Check(c.parts.All(p=>Vector3.Distance(p.transform.localPosition,p.assembledLocalPosition)<.001f&&Quaternion.Angle(p.transform.localRotation,p.assembledLocalRotation)<.01f&&Vector3.Distance(p.transform.localScale,p.assembledLocalScale)<.001f),"Assemble exactly restores saved positions, rotations and scales");
                c.Turn(47);c.ResetView();yield return new WaitForSecondsRealtime(1);
                Check(c.Selected==null&&!c.AutoRotate&&!c.IsExploded&&Quaternion.Angle(c.displayRoot.rotation,Quaternion.identity)<.01f,"Reset all clears selection, mode, auto-rotation and display yaw");
                results.Insert(0,"MEMBER 2 UNITY PLAY-MODE VERIFICATION | "+DateTime.Now.ToString("s")+" | failures="+failures);
                results.Add("Resolution: "+Screen.width+"x"+Screen.height);
                Debug.Log("[Ferrari] Verification finished: "+failures+" failures. See FerrariSource/member2_verification.txt");
            }
            finally
            {
                if(testMouse!=null)InputSystem.RemoveDevice(testMouse);
                InputSystem.settings.editorInputBehaviorInPlayMode=oldRouting;
                InputSystem.settings.backgroundBehavior=oldBackground;
                File.WriteAllLines(Path.Combine(Workspace,"FerrariSource/member2_verification.txt"),results);
            }
        }
    }
}
