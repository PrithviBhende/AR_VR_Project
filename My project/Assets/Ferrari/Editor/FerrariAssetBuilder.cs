using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FerrariLab.Editor
{
    [InitializeOnLoad]
    public static class FerrariAssetBuilder
    {
        const string Root = "Assets/Ferrari";
        const string ModelPath = Root + "/Models/FerrariEnzo_Prepared.fbx";
        static string Workspace => Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
        static string Request => Path.Combine(Workspace, "FerrariSource/Member1Build.request");
        static FerrariAssetBuilder() { EditorApplication.update += CheckRequest; }
        static void CheckRequest()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
            File.Copy(Request, Request + ".processing", true);
            File.Delete(Request);
            try { Build(); File.Delete(Request + ".processing"); }
            catch (Exception e) { File.WriteAllText(Path.Combine(Workspace,"FerrariSource/unity_validation_error.txt"),e.ToString()); Debug.LogException(e); }
        }
        [Serializable] class MaterialRow { public string name; public float[] color; public float metallic; public float smoothness; }
        [Serializable] class Manifest { public MaterialRow[] materials; }

        [MenuItem("ARVR Prototype/Ferrari/Build Member 1 Review Scene")]
        public static void Build()
        {
            Directory.CreateDirectory(Root + "/Materials"); Directory.CreateDirectory(Root + "/Prefabs"); Directory.CreateDirectory(Root + "/Scenes");
            AssetDatabase.Refresh();
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.isReadable = true;
            importer.importCameras = false; importer.importLights = false;
            importer.SaveAndReimport();
            var materials = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/prepared_manifest.json")).materials;
            foreach (var row in materials)
            {
                string safe = string.Concat(row.name.Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_'));
                string path = Root + "/Materials/" + safe + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat,path); }
                mat.color = new Color(row.color[0],row.color[1],row.color[2],1);
                mat.SetFloat("_Metallic",row.metallic); mat.SetFloat("_Smoothness",row.smoothness);
                // Imported OBJ shells include inconsistent facing; render both surfaces.
                mat.SetFloat("_Cull",0);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),row.name),mat);
                EditorUtility.SetDirty(mat);
            }
            importer.SaveAndReimport();
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var display = new GameObject("RotatingDisplay");
            var car = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath),scene);
            car.name = "FerrariEnzo";car.transform.SetParent(display.transform,false);
            var ids = new[] {"body","bonnet","bumper_F","bumper_R","door_L","door_R","engine","engine_cover","rear_wing","seat_L","seat_R","steering_wheel","wheel_FL","wheel_FR","wheel_RL","wheel_RR"};
            foreach (var id in ids)
            {
                var t=car.GetComponentsInChildren<Transform>(true).Single(x=>x.name==id);
                var p=t.gameObject.AddComponent<FerrariPart>(); p.partId=id;p.displayName=id.Replace('_',' ');
                p.description="Imported Ferrari " + p.displayName + ". Educational description to be authored and checked in member 2's interaction pass.";
                p.assembledLocalPosition=t.localPosition;p.assembledLocalRotation=t.localRotation;p.assembledLocalScale=t.localScale;p.assemblyCandidate=id!="body";
                foreach(var mesh in t.GetComponentsInChildren<MeshFilter>())
                {
                    if(mesh.sharedMesh && mesh.sharedMesh.vertexCount>0)
                        mesh.gameObject.AddComponent<MeshCollider>().sharedMesh=mesh.sharedMesh;
                }
            }
            var bounds=BoundsOf(car);
            if(bounds.size.x<4 || bounds.size.x>5.5f || bounds.size.y<.8f || bounds.size.y>1.5f) throw new Exception("Unexpected import scale/axes: " + bounds.size);
            car.transform.localPosition=new Vector3(-bounds.center.x,.16f-bounds.min.y,-bounds.center.z);
            bounds=BoundsOf(car);
            var plate=GameObject.CreatePrimitive(PrimitiveType.Cylinder);plate.name="BasePlate";plate.transform.SetParent(display.transform,false);plate.transform.localPosition=new Vector3(0,.075f,0);plate.transform.localScale=new Vector3(6,.075f,6);
            var plateMat=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.065f,.09f,.12f)};
            string platePath=Root+"/Materials/BasePlate.mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(platePath);
            if(existing) { UnityEngine.Object.DestroyImmediate(plateMat);plateMat=existing; } else AssetDatabase.CreateAsset(plateMat,platePath);
            plate.GetComponent<Renderer>().sharedMaterial=plateMat;
            var key=new GameObject("Key light").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.4f;key.transform.rotation=Quaternion.Euler(45,-35,0);key.shadows=LightShadows.Soft;
            var fill=new GameObject("Fill light").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.7f;fill.transform.rotation=Quaternion.Euler(35,145,0);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.45f,.48f,.55f);
            var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.04f,.055f);camera.transform.position=new Vector3(6,3.5f,-6);camera.transform.LookAt(new Vector3(0,.55f,0));camera.fieldOfView=42;
            PrefabUtility.SaveAsPrefabAsset(car,Root+"/Prefabs/FerrariEnzo.prefab");
            EditorSceneManager.SaveScene(scene,Root+"/Scenes/FerrariAssetReview.unity");
            AssetDatabase.SaveAssets();
            var target=new RenderTexture(1600,1000,24);target.antiAliasing=4;camera.targetTexture=target;
            var old=RenderTexture.active;
            try
            {
                camera.Render();RenderTexture.active=target;
                var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(Workspace,"Evidence/Ferrari_Member1_Assembled_Unity.png"),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
            }
            finally { camera.targetTexture=null;RenderTexture.active=old;target.Release();UnityEngine.Object.DestroyImmediate(target); }
            var usedMaterials=car.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
            File.WriteAllText(Path.Combine(Workspace,"FerrariSource/unity_validation.txt"),$"PASS: imported 16 part assemblies. Renderers: {car.GetComponentsInChildren<Renderer>().Length}. Mesh colliders: {car.GetComponentsInChildren<MeshCollider>().Length}. Bounds metres: {bounds.size}. Minimum height: {bounds.min.y}. Base top: 0.15. Scene: {Root}/Scenes/FerrariAssetReview.unity\n" + string.Join("\n",usedMaterials.Select(m=>m.name+" | "+m.shader.name+" | "+AssetDatabase.GetAssetPath(m))));
            EditorSceneManager.CloseScene(scene,true);
            if(previous.IsValid()) SceneManager.SetActiveScene(previous);
            Debug.Log("[Ferrari] Member 1 prefab and review scene saved; validation passed.");
        }
        static Bounds BoundsOf(GameObject go)
        {
            var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;
        }
    }
}
