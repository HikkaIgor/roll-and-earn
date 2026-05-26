#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;

namespace RollAndEarn
{
    public static class FontSetup
    {
        private const string FONTS_DIR = "Assets/Fonts";
        private const string RESOURCES_DIR = "Assets/Resources/RollAndEarn/Fonts";

        [MenuItem("RollAndEarn/Generate Font Assets")]
        public static void GenerateFontAssets()
        {
            if (!AssetDatabase.IsValidFolder(RESOURCES_DIR))
                AssetDatabase.CreateFolder("Assets/Resources/RollAndEarn", "Fonts");

            CreateSDF("Cinzel-Variable.ttf", "Cinzel SDF");
            CreateSDF("MedievalSharp-Regular.ttf", "MedievalSharp SDF");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FontSetup] Font assets generated successfully.");
        }

        private static void CreateSDF(string sourceTtf, string assetName)
        {
            var fontPath = $"{FONTS_DIR}/{sourceTtf}";
            var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (font == null)
            {
                Debug.LogError($"[FontSetup] Source font not found: {fontPath}");
                return;
            }

            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{RESOURCES_DIR}/{assetName}.asset");
            if (existing != null)
            {
                Debug.Log($"[FontSetup] {assetName} already exists, regenerating...");
                AssetDatabase.DeleteAsset($"{RESOURCES_DIR}/{assetName}.asset");
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(font);
            if (fontAsset == null)
            {
                Debug.LogError($"[FontSetup] Failed to create font asset from {sourceTtf}");
                return;
            }

            AssetDatabase.CreateAsset(fontAsset, $"{RESOURCES_DIR}/{assetName}.asset");
            Debug.Log($"[FontSetup] Created {assetName}.asset");
        }
    }
}
#endif
