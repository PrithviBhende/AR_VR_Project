using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FerrariLab.Editor
{
    public static class FerrariVoiceAuthoring
    {
        [MenuItem("ARVR Prototype/Ferrari/Voice Guide/Export Narration Text")]
        public static void Export()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Ferrari/Prefabs/FerrariEnzo.prefab");
            if (!prefab) throw new System.InvalidOperationException("Ferrari prefab is missing.");
            var catalog = new FerrariVoiceCatalog { entries = prefab.GetComponentsInChildren<FerrariPart>(true)
                .OrderBy(p => p.partId).Select(p => new FerrariVoiceEntry {
                    id = p.partId,
                    explanation = FerrariPartPresentation.DisplayName(p.partId) + ". " + FerrariPartPresentation.Purpose(p.partId),
                    hint = "Placement hint. " + FerrariPartPresentation.PlacementHint(p.partId)
                }).ToArray() };
            var folder = "Assets/Ferrari/Resources/FerrariNarration";
            Directory.CreateDirectory(folder);
            File.WriteAllText(folder + "/catalog.json", JsonUtility.ToJson(catalog, true));
            AssetDatabase.Refresh(); Debug.Log("Exported narration for " + catalog.entries.Length + " assemblies.");
        }
    }
    public sealed class FerrariVoiceImport : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/Ferrari/Resources/FerrariNarration/")) return;
            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = .65f; importer.defaultSampleSettings = settings;
        }
    }
}
