using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FerrariLab.Editor
{
    public static class FerrariInspectionBuilder
    {
        public const string ScenePath="Assets/Ferrari/Scenes/FerrariInspection.unity";
        [MenuItem("ARVR Prototype/Ferrari/Build Member 2 Inspection Scene")]
        public static void Build()
        {
            if(Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before building.");
            if(File.Exists(ScenePath)) throw new InvalidOperationException("Inspection scene already exists. Open it rather than replacing your work.");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var display=new GameObject("RotatingDisplay");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Ferrari/Prefabs/FerrariEnzo.prefab");
            if(!prefab)throw new InvalidOperationException("Member 1 prefab missing.");
            var car=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);car.transform.SetParent(display.transform,false);
            var plate=GameObject.CreatePrimitive(PrimitiveType.Cylinder);plate.name="BasePlate";plate.transform.SetParent(display.transform,false);plate.transform.localPosition=new Vector3(0,.075f,0);plate.transform.localScale=new Vector3(6,.075f,6);plate.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Ferrari/Materials/BasePlate.mat");
            // Radial ticks make rotation of the circular plate visible.
            var tickMat=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.15f,.65f,.8f)};
            AssetDatabase.CreateAsset(tickMat,"Assets/Ferrari/Materials/TurntableTicks.mat");
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6;var tick=GameObject.CreatePrimitive(PrimitiveType.Cube);tick.name="Plate marker "+i;tick.transform.SetParent(display.transform,false);tick.transform.localPosition=new Vector3(Mathf.Cos(a)*2.8f,.155f,Mathf.Sin(a)*2.8f);tick.transform.localScale=new Vector3(.15f,.008f,.035f);tick.transform.localRotation=Quaternion.Euler(0,-i*30,0);tick.GetComponent<Renderer>().sharedMaterial=tickMat;UnityEngine.Object.DestroyImmediate(tick.GetComponent<Collider>());
            }
            var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.016f,.028f,.043f);camera.fieldOfView=42;camera.nearClipPlane=.05f;camera.farClipPlane=100;camera.transform.position=new Vector3(7,4.5f,-7);camera.transform.LookAt(new Vector3(0,.8f,0));camera.gameObject.AddComponent<AudioListener>();
            var key=new GameObject("Key light").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.6f;key.shadows=LightShadows.Soft;key.transform.rotation=Quaternion.Euler(45,-35,0);
            var fill=new GameObject("Fill light").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.8f;fill.transform.rotation=Quaternion.Euler(35,145,0);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.45f,.48f,.55f);
            var controller=new GameObject("Ferrari Inspection").AddComponent<FerrariInspectionController>();controller.displayRoot=display.transform;controller.viewCamera=camera;controller.parts=car.GetComponentsInChildren<FerrariPart>().OrderBy(p=>p.partId).ToArray();
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
            // Preserve all older scene entries, but launch the Ferrari checkpoint first in builds.
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath)).ToArray();
            Selection.activeGameObject=controller.gameObject;
            Debug.Log("[Ferrari] Member 2 inspection scene saved. Enter Play to inspect.");
        }
    }
}
