using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FerrariLab.Editor
{
    public static class FerrariThumbnailBuilder
    {
        [MenuItem("ARVR Prototype/Ferrari/Build Part Thumbnails")]
        public static void Build()
        {
            if(Application.isPlaying)throw new System.InvalidOperationException("Stop Play before generating thumbnails.");
            const string folder="Assets/Ferrari/Resources/FerrariPartThumbnails";
            Directory.CreateDirectory(folder);
            var previous=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            var oldTarget=RenderTexture.active;RenderTexture target=null;
            try
            {
                SceneManager.SetActiveScene(scene);
                var car=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Ferrari/Prefabs/FerrariEnzo.prefab"),scene);
                car.transform.position+=Vector3.one*1000;
                foreach(var t in car.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
                var parts=car.GetComponentsInChildren<FerrariPart>(true);
                foreach(var p in parts)p.gameObject.SetActive(false);
                var camera=new GameObject("Thumbnail Camera").AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.065f,.09f);camera.orthographic=true;camera.nearClipPlane=.01f;camera.farClipPlane=50;
                var key=new GameObject("Thumbnail Key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.7f;key.cullingMask=1<<31;key.transform.rotation=Quaternion.Euler(40,-35,0);
                var fill=new GameObject("Thumbnail Fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.8f;fill.cullingMask=1<<31;fill.transform.rotation=Quaternion.Euler(30,150,0);
                RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.55f,.57f,.60f);
                target=new RenderTexture(256,180,24){antiAliasing=4};camera.targetTexture=target;
                foreach(var part in parts)
                {
                    part.gameObject.SetActive(true);
                    var renderers=part.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);
                    var direction=new Vector3(1,.75f,-1).normalized;
                    camera.transform.position=b.center+direction*10;camera.transform.LookAt(b.center);
                    float extent=.05f;
                    for(int i=0;i<8;i++)
                    {
                        var point=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                        var local=camera.transform.InverseTransformPoint(point);
                        extent=Mathf.Max(extent,Mathf.Abs(local.y),Mathf.Abs(local.x)/(256f/180));
                    }
                    camera.orthographicSize=extent*1.15f;
                    camera.Render();RenderTexture.active=target;
                    var texture=new Texture2D(256,180,TextureFormat.RGB24,false);
                    try{texture.ReadPixels(new Rect(0,0,256,180),0,0);texture.Apply();File.WriteAllBytes(folder+"/"+part.partId+".png",texture.EncodeToPNG());}
                    finally{Object.DestroyImmediate(texture);}
                    part.gameObject.SetActive(false);
                }
                camera.targetTexture=null;
            }
            finally
            {
                RenderTexture.active=oldTarget;if(target){target.Release();Object.DestroyImmediate(target);}
                EditorSceneManager.CloseScene(scene,true);if(previous.IsValid())SceneManager.SetActiveScene(previous);
            }
            AssetDatabase.Refresh();
            foreach(var path in Directory.GetFiles(folder,"*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=256;importer.SaveAndReimport();
            }
            Debug.Log("[Ferrari] Rendered 16 part thumbnails from the actual imported meshes.");
        }
    }
}
