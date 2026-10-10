using System.IO;
using ARVRAutomotive;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARVRAutomotive.Editor
{
    public static class AutomotivePrototypeSceneBuilder
    {
        private const string SceneDirectory = "Assets/AutomotivePrototype/Scenes";
        private const string ScenePath = SceneDirectory + "/AutomotivePrototype.unity";

        [MenuItem("ARVR Prototype/Create or Rebuild Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/AutomotivePrototype"))
            {
                AssetDatabase.CreateFolder("Assets", "AutomotivePrototype");
            }

            if (!AssetDatabase.IsValidFolder(SceneDirectory))
            {
                AssetDatabase.CreateFolder("Assets/AutomotivePrototype", "Scenes");
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrap = new GameObject("Automotive Prototype Bootstrap");
            bootstrap.AddComponent<AutomotivePrototypeBootstrap>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorBuildSettings.scenes = scenes;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ARVR] Prototype scene created successfully at {ScenePath}");
        }

        public static void CreatePrototypeSceneBatch()
        {
            CreatePrototypeScene();
            var markerPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "prototype-build-success.txt"));
            File.WriteAllText(markerPath, "Automotive prototype scene generated successfully.\n");
            EditorApplication.Exit(0);
        }

        [MenuItem("ARVR Prototype/Check Platform Clearance")]
        public static void CheckPlatformClearance()
        {
            var prototype = Object.FindAnyObjectByType<AutomotivePrototypeBootstrap>();
            if (prototype != null && EditorApplication.isPlaying) prototype.ValidateGeometry();
        }

        [MenuItem("ARVR Prototype/Capture Evidence Screenshot")]
        public static void CaptureEvidenceScreenshot()
        {
            var prototype = Object.FindAnyObjectByType<AutomotivePrototypeBootstrap>();
            if (prototype == null || !EditorApplication.isPlaying)
            {
                Debug.LogWarning("[ARVR] Enter Play Mode before capturing evidence.");
                return;
            }

            prototype.CaptureEvidence();
        }
    }
}
