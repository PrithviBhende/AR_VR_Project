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
    public static class FerrariAssemblyVerification
    {
        static readonly List<string> results=new List<string>();
        static int failures;
        static bool running;
        static string Workspace=>Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
        static void Check(bool condition,string name){results.Add((condition?"PASS: ":"FAIL: ")+name);if(!condition)failures++;}
        [MenuItem("ARVR Prototype/Ferrari/Verify Member 3 (Play Mode)")]
        public static void Run()
        {
            var c=UnityEngine.Object.FindFirstObjectByType<FerrariInspectionController>();
            if(!Application.isPlaying||!c||running)throw new InvalidOperationException("Enter Play in FerrariInspection, and run one test at a time.");
            results.Clear();failures=0;running=true;c.StartCoroutine(Verify(c));
        }
        static Vector2 Center(Component component){var r=(RectTransform)component.transform;return RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));}
        static void Send(Mouse m,Vector2 position,ushort buttons=0,Vector2 delta=default,Vector2 scroll=default){InputSystem.QueueStateEvent(m,new MouseState{position=position,buttons=buttons,delta=delta,scroll=scroll});}
        static IEnumerator Frames(){yield return null;yield return null;yield return null;}
        static IEnumerator Settle(FerrariAssemblyController a)
        {
            float timeout=Time.realtimeSinceStartup+4;
            while((a.IsBusy||a.Access.IsMoving)&&Time.realtimeSinceStartup<timeout)yield return null;
            yield return null;
        }
        static IEnumerator Click(Mouse m,string button)
        {
            var b=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(x=>x.name==button);var pos=Center(b);
            Send(m,pos);yield return Frames();Send(m,pos,1);yield return Frames();Send(m,pos);yield return Frames();
        }
        static bool Exact(FerrariPart p)=>Vector3.Distance(p.transform.localPosition,p.assembledLocalPosition)<.001f&&Quaternion.Angle(p.transform.localRotation,p.assembledLocalRotation)<.01f&&Vector3.Distance(p.transform.localScale,p.assembledLocalScale)<.001f;
        static IEnumerator Verify(FerrariInspectionController c)
        {
            var a=c.Assembly;Mouse m=null;
            var routing=InputSystem.settings.editorInputBehaviorInPlayMode;var background=InputSystem.settings.backgroundBehavior;
            try
            {
                InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                m=InputSystem.AddDevice<Mouse>("AssemblyVerificationMouse");Send(m,new Vector2(5,5));
                if(a.IsActive)a.Exit();c.ResetView();c.SetExploded(false,true);yield return new WaitForSecondsRealtime(2);
                yield return Click(m,"SELF ASSEMBLY");Check(a.IsActive,"Actual mode-switch button enters self assembly");
                a.Restart();yield return new WaitForSecondsRealtime(2);
                Check(a.TotalCount==15&&a.Available.Count==15&&a.PlacedCount==0,"15 unique candidates, zero placed on restart");
                Check(c.parts.Single(p=>p.partId=="body").gameObject.activeSelf&&c.parts.Where(p=>p.assemblyCandidate).All(p=>!p.gameObject.activeSelf),"Only the fixed body is visible initially");
                var inventory=UnityEngine.Object.FindFirstObjectByType<FerrariLoopingInventory>();
                Check(c.parts.All(p=>FerrariPartPresentation.Thumbnail(p.partId)!=null),"All 16 actual-mesh thumbnails load from bundled resources");
                Check(c.parts.All(p=>!string.IsNullOrWhiteSpace(FerrariPartPresentation.PlacementHint(p.partId))),"Every part has a placement clue");
                var first=inventory.Rows[0];var clickPoint=Center(first);
                Send(m,clickPoint);yield return Frames();Send(m,clickPoint,1);yield return Frames();Send(m,clickPoint);yield return Frames();
                Check(a.PreviewPart==first.part&&a.CurrentPart==null,"Clicking a pictured row previews details without taking the part");
                var clue=UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Single(t=>t.name=="Placement clue");
                Check(clue.gameObject.activeInHierarchy&&clue.text.Contains(FerrariPartPresentation.PlacementHint(first.part.partId)),"Selected part's placement clue appears in the right panel");
                Check(inventory.Rows.All(r=>r.thumbnail.texture!=null),"Visible and recycled inventory rows have part images");
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(Workspace,"Evidence/Ferrari_UI_Thumbnails.png"));
                float zoom=c.ZoomFactor;var viewCenter=c.viewCamera.pixelRect.center;
                Send(m,viewCenter);yield return Frames();Send(m,viewCenter,0,default,new Vector2(0,120));yield return Frames();
                Check(c.ZoomFactor<zoom,"Normal mouse wheel over the car zooms in");
                Send(m,viewCenter,0,default,new Vector2(0,-120));yield return Frames();
                Check(Mathf.Abs(c.ZoomFactor-zoom)<.001f,"Reverse wheel motion smoothly restores zoom factor");
                a.CancelDrag();yield return new WaitForSecondsRealtime(1);
                int index=inventory.StartIndex;inventory.ScrollBy(15);Check(inventory.StartIndex==index,"Inventory wraps forward through all 15 choices");
                inventory.ScrollBy(-16);Check(inventory.StartIndex==(index+14)%15,"Inventory wraps backwards across zero");
                var scrollPoint=Center(inventory.Rows[0]);Send(m,scrollPoint,0,default,new Vector2(0,-120));yield return Frames();
                Check(inventory.StartIndex==index,"Mouse wheel scroll over a row advances the looping inventory");
                Check(Mathf.Abs(c.ZoomFactor-zoom)<.001f,"Scrolling the parts list does not zoom the car");
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(Workspace,"Evidence/Ferrari_Member3_Start.png"));
                var wheel=c.parts.Single(p=>p.partId=="wheel_FR");
                int wheelIndex=a.Available.ToList().IndexOf(wheel);inventory.ScrollBy(wheelIndex-inventory.StartIndex);yield return Frames();
                var row=inventory.Rows[0];var start=Center(row);var target=(Vector2)c.viewCamera.WorldToScreenPoint(a.TargetPosition(wheel));
                Send(m,start);yield return Frames();Send(m,start,1);yield return Frames();Send(m,start-new Vector2(30,0),1,new Vector2(-30,0));yield return Frames();
                Check(a.IsDragging&&a.CurrentPart==wheel,"Actual inventory-row mouse drag reserves the wheel");
                Check(!a.Available.Contains(wheel)&&a.Available.Count==14,"Reserved part is removed from repeating inventory");
                var yaw=c.displayRoot.rotation;var camera=c.viewCamera.transform.position;c.Turn(60);
                Send(m,target+new Vector2(8,0),1,target-start);yield return Frames();
                Check(a.CanSnap,"Dragged wheel enters the matching target's snap range");
                Check(Quaternion.Angle(yaw,c.displayRoot.rotation)<.01f&&Vector3.Distance(camera,c.viewCamera.transform.position)<.001f,"Turntable and camera stay fixed during placement drag");
                Check(Quaternion.Angle(wheel.transform.localRotation,wheel.assembledLocalRotation)>1,"Loose part orientation differs before automatic alignment");
                yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(Workspace,"Evidence/Ferrari_Member3_DragTarget.png"));
                Send(m,target+new Vector2(8,0));yield return new WaitForSecondsRealtime(.8f);
                Check(a.IsPlaced(wheel)&&a.PlacedCount==1&&Exact(wheel),"Mouse release snaps the wheel's exact position, orientation and scale");
                Check(!a.BeginDrag(wheel,start),"Already placed part cannot be taken twice");
                Check(a.Available.Count==14&&!a.Available.Contains(wheel),"Placed part remains removed from inventory");
                index=inventory.StartIndex;inventory.ScrollBy(14);Check(inventory.StartIndex==index,"Loop length adjusts after removing a placed part");
                var door=c.parts.Single(p=>p.partId=="door_L");var far=new Vector2(c.viewCamera.pixelRect.xMin+40,c.viewCamera.pixelRect.yMax-40);
                Check(a.BeginDrag(door,far),"Unplaced door can be taken in any order");
                Check(!a.Release(far)&&!door.gameObject.activeSelf&&a.Available.Contains(door)&&a.PlacedCount==1,"Far drop returns the part without incrementing progress");
                var engine=c.parts.Single(p=>p.partId=="engine");a.BeginDrag(engine,start);Check(!a.Release(start)&&a.Available.Contains(engine),"Release over the inventory safely returns the part");
                c.Turn(90);a.BeginDrag(door,start);target=c.viewCamera.WorldToScreenPoint(a.TargetPosition(door));
                Check(a.Release(target),"Matching-anchor drop works after a 90-degree root rotation");yield return new WaitForSecondsRealtime(.6f);
                Check(a.IsPlaced(door)&&Exact(door),"Rotated-display snap restores the saved local pose");
                a.BeginDrag(engine,start);var wheelTarget=(Vector2)c.viewCamera.WorldToScreenPoint(a.TargetPosition(wheel));
                Check(!a.Release(wheelTarget)&&!a.IsPlaced(engine),"Engine does not snap at an unrelated front-wheel target");
                a.BeginDrag(engine,start);a.CancelDrag();Check(!a.IsBusy&&a.Available.Contains(engine)&&!engine.gameObject.activeSelf,"Cancel restores inventory and clears pointer ownership");
                c.SetExploded(true,true);Check(!c.IsExploded&&!engine.gameObject.activeSelf,"Inspection commands cannot expose inventory parts in assembly mode");
                a.Exit();Check(c.parts.All(p=>p.gameObject.activeSelf)&&c.parts.All(Exact),"Returning to inspection shows the complete car at exact poses");
                c.SetExploded(true,true);Check(c.IsExploded,"Exploded inspection still works after assembly mode");
                a.Enter();Check(a.PlacedCount==2&&a.IsPlaced(wheel)&&a.IsPlaced(door)&&a.Available.Count==13,"Returning to assembly resumes placement progress");
                // Complete the first order using the same public drag/release path as the UI.
                foreach(var p in a.Available.ToArray())
                {
                    a.BeginDrag(p,start);target=c.viewCamera.WorldToScreenPoint(a.TargetPosition(p));
                    Check(a.Release(target),"Valid matching drop: "+p.partId);yield return Settle(a);
                    Check(a.IsPlaced(p)&&Exact(p),"Exact saved pose after snap: "+p.partId);
                }
                Check(a.IsComplete&&a.Available.Count==0&&c.parts.All(p=>p.gameObject.activeSelf),"Completion at 15/15 with empty inventory and whole car visible");
                Check(c.displayRoot.GetComponentsInChildren<FerrariPart>(true).Length==16,"No duplicate physical part objects were created");
                Send(m,new Vector2(5,5));yield return new WaitForSecondsRealtime(2);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(Workspace,"Evidence/Ferrari_Member3_Complete.png"));
                yield return Click(m,"RESTART GAME");Check(a.PlacedCount==0&&a.Available.Count==15&&!a.IsBusy,"Actual restart button resets completion and inventory");
                // Second order is reversed, with repeated rotation between placements.
                foreach(var p in a.Available.Reverse().ToArray())
                {
                    c.Turn(23);a.BeginDrag(p,start);target=c.viewCamera.WorldToScreenPoint(a.TargetPosition(p));
                    Check(a.Release(target),"Reverse-order rotated drop: "+p.partId);yield return Settle(a);
                }
                Check(a.IsComplete&&c.parts.All(Exact),"Second placement order completes with exact local transforms");
                a.Restart();a.BeginDrag(engine,start);target=c.viewCamera.WorldToScreenPoint(a.TargetPosition(engine));a.Release(target);a.Restart();yield return new WaitForSecondsRealtime(.6f);
                Check(a.PlacedCount==0&&!a.IsBusy&&a.Available.Count==15,"Restart cancels an in-flight snap without delayed placement");
                a.BeginDrag(door,start);a.Exit();Check(!a.IsBusy&&c.parts.All(p=>p.gameObject.activeSelf)&&c.parts.All(Exact),"Mode switch during drag recovers all parts cleanly");
                results.Insert(0,"MEMBER 3 UNITY PLAY-MODE VERIFICATION | "+DateTime.Now.ToString("s")+" | failures="+failures);
                results.Add("Resolution: "+Screen.width+"x"+Screen.height);
                a.Enter();a.Restart();Debug.Log("[Ferrari] Member 3 verification finished with "+failures+" failures.");
            }
            finally
            {
                if(m!=null)InputSystem.RemoveDevice(m);
                InputSystem.settings.editorInputBehaviorInPlayMode=routing;InputSystem.settings.backgroundBehavior=background;
                running=false;File.WriteAllLines(Path.Combine(Workspace,"FerrariSource/member3_verification.txt"),results);
            }
        }
    }
}
