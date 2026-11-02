using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FerrariLab.Editor
{
    public static class FerrariWorldPlacementVerification
    {
        static bool running;
        static int failures;
        static readonly List<string> results = new List<string>();
        static void Check(bool ok, string label) { results.Add((ok ? "PASS: " : "FAIL: ") + label); if (!ok) failures++; }
        [MenuItem("ARVR Prototype/Ferrari/Verify World Placement (Play Mode)")]
        public static void Run()
        {
            var c = UnityEngine.Object.FindAnyObjectByType<FerrariInspectionController>();
            if (!Application.isPlaying || !c || running) throw new InvalidOperationException("Run in FerrariInspection Play Mode, one suite at a time.");
            running = true; failures = 0; results.Clear(); c.StartCoroutine(Verify(c));
        }
        static IEnumerator Verify(FerrariInspectionController c)
        {
            var root = c.displayRoot; var position = root.position; var rotation = root.rotation; var scale = root.localScale;
            var a = c.Assembly;
            try
            {
                a.Enter();
                var p = c.parts.Single(x => x.partId == "wheel_RR");
                var parent = p.transform.parent;
                foreach (float factor in new[] { .25f, 1f, 1.5f })
                {
                    a.Restart(); root.localScale = scale * factor; root.position = new Vector3(.3f, .8f, -.2f); root.rotation = Quaternion.Euler(0, 65, 0);
                    var target = a.TargetPosition(p);
                    var correctRotation = parent.rotation * p.assembledLocalRotation;
                    Check(a.BeginWorldDrag(p, target + root.TransformVector(new Vector3(.75f, 0, 0)), correctRotation), "World grab starts at scale " + factor);
                    Check(!a.CanSnap && !a.ReleaseWorld() && a.PlacedCount == 0, "Out-of-range world release returns the part at scale " + factor);
                    Check(a.BeginWorldDrag(p, target, correctRotation), "Part can be taken again after a miss");
                    Check(p.transform.parent == parent, "World grab preserves the saved-pose parent");
                    Check(!a.BeginWorldDrag(p, target, correctRotation), "A busy placement cannot reserve a duplicate");
                    Check(a.CanSnap && a.ReleaseWorld(), "Scaled matching anchor accepts world release");
                    float deadline = Time.realtimeSinceStartup + 4;
                    while (a.IsBusy && Time.realtimeSinceStartup < deadline) yield return null;
                    Check(!a.IsBusy && a.PlacedCount == 1 && a.IsPlaced(p), "World placement completes at scale " + factor);
                    Check(Vector3.Distance(p.transform.localPosition, p.assembledLocalPosition) < .001f && Quaternion.Angle(p.transform.localRotation, p.assembledLocalRotation) < .01f, "World placement restores exact local pose");
                    Check(!a.BeginWorldDrag(p, target, correctRotation), "Already placed part cannot be duplicated");
                }
            }
            finally
            {
                a.CancelDrag(); root.localScale = scale; root.position = position; a.Restart(); a.Exit(); root.rotation = rotation;
                var file = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Logs/world_placement_verification.txt"));
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                File.WriteAllLines(file, new[] { "World pose API verification (not hardware) | " + DateTime.Now.ToString("s") + " | failures=" + failures }.Concat(results));
                running = false; Debug.Log("Ferrari world placement: " + results.Count + " checks, " + failures + " failures.");
            }
        }
    }
}
