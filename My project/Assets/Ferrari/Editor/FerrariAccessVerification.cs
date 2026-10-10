using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FerrariLab.Editor
{
    public static class FerrariAccessVerification
    {
        static readonly List<string> results=new List<string>();
        static bool running;
        static int failures;
        static string Workspace=>Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
        static void Check(bool valid,string label){results.Add((valid?"PASS: ":"FAIL: ")+label);if(!valid)failures++;}
        static FerrariPart Part(FerrariAssemblyController a,string id)=>a.inspection.parts.Single(p=>p.partId==id);
        static bool Exact(FerrariPart p)=>Vector3.Distance(p.transform.localPosition,p.assembledLocalPosition)<.0001f&&Quaternion.Angle(p.transform.localRotation,p.assembledLocalRotation)<.001f&&Vector3.Distance(p.transform.localScale,p.assembledLocalScale)<.0001f;
        static IEnumerator Settle(FerrariAssemblyController a)
        {
            float deadline=Time.realtimeSinceStartup+5;
            while((a.IsBusy||a.Access.IsMoving)&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return null;
        }
        static IEnumerator Place(FerrariAssemblyController a,string id)
        {
            var p=Part(a,id);a.BeginDrag(p,new Vector2(5,5));
            a.Release(a.inspection.viewCamera.WorldToScreenPoint(a.TargetPosition(p)));yield return Settle(a);
        }
        [MenuItem("ARVR Prototype/Ferrari/Verify Access Animations (Play Mode)")]
        public static void Run()
        {
            var c=UnityEngine.Object.FindFirstObjectByType<FerrariInspectionController>();
            if(!Application.isPlaying||!c||running)throw new InvalidOperationException("Enter Play; run only one verifier at a time.");
            running=true;results.Clear();failures=0;c.StartCoroutine(Verify(c.Assembly));
        }
        static IEnumerator Verify(FerrariAssemblyController a)
        {
            try
            {
                a.Enter();a.Restart();yield return new WaitForSecondsRealtime(1);
                var c=a.inspection;
                a.Preview(Part(a,"seat_L"));yield return new WaitForSecondsRealtime(.85f);
                Check(!Part(a,"door_L").gameObject.activeSelf&&a.Access.OpenAmount("door_L")==0,"Unplaced door stays in inventory when selecting a seat");
                a.Preview(Part(a,"engine"));yield return new WaitForSecondsRealtime(.85f);
                Check(!Part(a,"engine_cover").gameObject.activeSelf&&!a.Access.AnyOpen,"Unplaced engine cover is not spawned or counted as placed");
                string[][] cases={new[]{"door_L","seat_L"},new[]{"door_R","seat_R"},new[]{"door_L","steering_wheel"},new[]{"engine_cover","engine"},new[]{"bonnet","bumper_F"}};
                foreach(var pair in cases)
                {
                    a.Restart();yield return Place(a,pair[0]);
                    var panel=Part(a,pair[0]);var interior=Part(a,pair[1]);var parent=panel.transform.parent;
                    var savedPosition=panel.assembledLocalPosition;var savedRotation=panel.assembledLocalRotation;
                    Check(a.IsPlaced(panel)&&Exact(panel),"Placed panel begins exactly closed: "+pair[0]);
                    a.Preview(interior);yield return new WaitForSecondsRealtime(.22f);
                    Check(a.Access.OpenAmount(pair[0])>0&&a.Access.OpenAmount(pair[0])<1,"Opening is animated, not a teleport: "+pair[0]+" for "+pair[1]);
                    yield return new WaitForSecondsRealtime(.65f);
                    Check(a.Access.OpenAmount(pair[0])>.999f&&Quaternion.Angle(panel.transform.localRotation,savedRotation)>45,"Panel reaches the open hinge pose: "+pair[0]);
                    Check(c.BoundsOf(panel).min.y>=.149f,"Open panel stays above the base: "+pair[0]);
                    Check(panel.transform.parent==parent&&panel.assembledLocalPosition==savedPosition&&panel.assembledLocalRotation==savedRotation,"Hierarchy and saved closed poses remain immutable: "+pair[0]);
                    Check(a.PlacedCount==1&&!a.Available.Contains(panel)&&a.Available.Count==14,"Opening never changes placement progress or inventory: "+pair[0]);
                    var openPosition=panel.transform.localPosition;var openRotation=panel.transform.localRotation;
                    c.Turn(55);yield return null;
                    Check(Vector3.Distance(openPosition,panel.transform.localPosition)<.0001f&&Quaternion.Angle(openRotation,panel.transform.localRotation)<.001f,"Open hinge remains correct while display rotates: "+pair[0]);
                    if(pair[1]=="seat_L"||pair[1]=="engine")
                    {
                        yield return new WaitForSecondsRealtime(.6f);yield return new WaitForEndOfFrame();
                        ScreenCapture.CaptureScreenshot(Path.Combine(Workspace,"Evidence/Ferrari_Access_"+pair[1]+".png"));
                    }
                    a.BeginDrag(interior,new Vector2(5,5));
                    Check(a.Release(c.viewCamera.WorldToScreenPoint(a.TargetPosition(interior))),"Interior drop is accepted with access open: "+pair[1]);
                    yield return Settle(a);
                    Check(a.IsPlaced(interior)&&Exact(interior)&&Exact(panel)&&!a.Access.AnyOpen,"Interior snaps and panel closes to its exact original pose: "+pair[1]);
                    Check(a.PlacedCount==2,"Only the new interior/service part increments progress: "+pair[1]);
                }
                // Both doors exist: only the side needed for the selected part should open.
                a.Restart();yield return Place(a,"door_L");yield return Place(a,"door_R");
                a.Preview(Part(a,"seat_L"));yield return new WaitForSecondsRealtime(.85f);
                Check(a.Access.OpenAmount("door_L")>.99f&&a.Access.OpenAmount("door_R")==0,"Left seat opens only the left placed door");
                a.Preview(Part(a,"seat_R"));yield return new WaitForSecondsRealtime(.85f);
                Check(a.Access.OpenAmount("door_R")>.99f&&Exact(Part(a,"door_L")),"Switching selection closes the old door and opens the correct side");
                a.Preview(Part(a,"wheel_FR"));yield return Settle(a);
                Check(!a.Access.AnyOpen,"Unrelated wheel selection closes access panels");
                a.Preview(Part(a,"seat_L"));yield return new WaitForSecondsRealtime(.3f);a.CancelDrag();yield return Settle(a);
                Check(!a.Access.AnyOpen&&Exact(Part(a,"door_L")),"Cancel during opening safely reverses the animation");
                var seat=Part(a,"seat_L");a.BeginDrag(seat,new Vector2(5,5));a.Release(new Vector2(5,5));yield return Settle(a);
                Check(!a.Access.AnyOpen&&a.Available.Contains(seat)&&a.PlacedCount==2,"Invalid drop returns the interior and closes its door");
                // A fast drop before the door has opened queues the snap until access is ready.
                a.BeginDrag(seat,new Vector2(5,5));a.Release(c.viewCamera.WorldToScreenPoint(a.TargetPosition(seat)));
                yield return new WaitForSecondsRealtime(.2f);
                Check(a.IsSnapping&&!a.IsPlaced(seat)&&!a.Access.ReadyForPlacement,"Immediate drop waits for the access opening before snap");
                yield return Settle(a);Check(a.IsPlaced(seat)&&Exact(seat)&&Exact(Part(a,"door_L")),"Queued drop finishes and closes the door");
                a.Preview(Part(a,"seat_R"));yield return new WaitForSecondsRealtime(.2f);a.Restart();yield return new WaitForSecondsRealtime(.85f);
                Check(!a.Access.AnyOpen&&a.PlacedCount==0&&a.Available.Count==15&&c.parts.All(Exact),"Restart interrupts opening and restores all closed anchors");
                yield return Place(a,"engine_cover");a.Preview(Part(a,"engine"));yield return new WaitForSecondsRealtime(.3f);a.Exit();
                Check(!a.Access.AnyOpen&&c.parts.All(Exact)&&c.parts.All(p=>p.gameObject.activeSelf),"Mode exit during opening restores the full closed car");
                c.SetExploded(true,true);yield return null;
                Check(c.parts.All(p=>Vector3.Distance(p.transform.localPosition,c.ExplodedLocalPosition(p))<.001f),"Disabled assembly hinges do not overwrite inspection explosion poses");
                c.SetExploded(false,true);a.Enter();a.Restart();
                yield return Place(a,"engine_cover");var engine=Part(a,"engine");a.BeginDrag(engine,new Vector2(5,5));a.Release(c.viewCamera.WorldToScreenPoint(a.TargetPosition(engine)));a.Restart();yield return new WaitForSecondsRealtime(1.5f);
                Check(a.PlacedCount==0&&!a.IsBusy&&!a.Access.AnyOpen&&a.Available.Count==15,"Restart cancels a snap waiting for the cover without delayed placement");
                results.Insert(0,"ACCESS ANIMATION VERIFICATION | "+DateTime.Now.ToString("s")+" | failures="+failures);
                results.Add("Resolution: "+Screen.width+"x"+Screen.height);
                Debug.Log("[Ferrari] Access-animation verification finished with "+failures+" failures.");
            }
            finally{running=false;File.WriteAllLines(Path.Combine(Workspace,"FerrariSource/access_verification.txt"),results);}
        }
        [MenuItem("ARVR Prototype/Ferrari/Show Access Animation Demo (Play Mode)")]
        public static void Demo()
        {
            var c=UnityEngine.Object.FindFirstObjectByType<FerrariInspectionController>();
            if(!Application.isPlaying||!c||running)throw new InvalidOperationException("Enter Play and finish tests first.");
            c.StartCoroutine(DemoRoutine(c.Assembly));
        }
        static IEnumerator DemoRoutine(FerrariAssemblyController a)
        {
            a.Enter();a.Restart();
            foreach(var id in new[]{"door_L","door_R","engine_cover","bonnet","wheel_FL","wheel_FR","wheel_RL","wheel_RR"})yield return Place(a,id);
            a.Preview(Part(a,"seat_R"));
        }
    }
}
