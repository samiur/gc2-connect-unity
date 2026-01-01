// ABOUTME: Editor tool that creates and configures URP quality tier assets.
// ABOUTME: Generates Low, Medium, and High quality URP assets with appropriate settings.

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.IO;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor utility to create and configure URP quality tier assets.
    /// </summary>
    public static class URPQualitySetup
    {
        private const string SettingsPath = "Assets/Settings";

        [MenuItem("OpenRange/Create URP Quality Assets", priority = 110)]
        public static void CreateURPQualityAssets()
        {
            if (!EditorUtility.DisplayDialog(
                "Create URP Quality Assets",
                "This will create Low, Medium, and High quality URP assets in Assets/Settings/. " +
                "Existing assets will be overwritten. Continue?",
                "Create",
                "Cancel"))
            {
                return;
            }

            EnsureDirectoryExists();

            CreateLowQualityAsset();
            CreateMediumQualityAsset();
            CreateHighQualityAsset();

            AssetDatabase.Refresh();
            Debug.Log("URPQualitySetup: All URP quality assets created successfully!");
        }

        [MenuItem("OpenRange/Configure QualityManager References", priority = 111)]
        public static void ConfigureQualityManagerReferences()
        {
            var qualityManager = Object.FindFirstObjectByType<Core.QualityManager>();
            if (qualityManager == null)
            {
                Debug.LogWarning("URPQualitySetup: No QualityManager found in scene. Please run in a scene with QualityManager.");
                return;
            }

            var lowAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                $"{SettingsPath}/URP-LowQuality.asset");
            var mediumAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                $"{SettingsPath}/URP-MediumQuality.asset");
            var highAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                $"{SettingsPath}/URP-HighQuality.asset");

            if (lowAsset == null || mediumAsset == null || highAsset == null)
            {
                Debug.LogWarning("URPQualitySetup: URP assets not found. Run 'Create URP Quality Assets' first.");
                return;
            }

            var so = new SerializedObject(qualityManager);
            so.FindProperty("_lowQualityAsset").objectReferenceValue = lowAsset;
            so.FindProperty("_mediumQualityAsset").objectReferenceValue = mediumAsset;
            so.FindProperty("_highQualityAsset").objectReferenceValue = highAsset;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(qualityManager);
            Debug.Log("URPQualitySetup: QualityManager references configured successfully!");
        }

        private static void EnsureDirectoryExists()
        {
            if (!AssetDatabase.IsValidFolder(SettingsPath))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }
        }

        private static void CreateLowQualityAsset()
        {
            // Create renderer first
            var renderer = CreateRenderer("URP-LowQuality_Renderer");
            string rendererPath = $"{SettingsPath}/URP-LowQuality_Renderer.asset";
            SaveRendererAsset(renderer, rendererPath);

            // Create pipeline asset with the renderer
            var asset = UniversalRenderPipelineAsset.Create(renderer);

            // Low quality settings
            asset.renderScale = 0.75f;
            asset.msaaSampleCount = (int)MsaaQuality.Disabled;
            asset.supportsHDR = false;

            // Shadows
            asset.shadowDistance = 25f;
            asset.shadowCascadeCount = 1;

            // Additional settings
            asset.supportsCameraDepthTexture = false;
            asset.supportsCameraOpaqueTexture = false;

            SavePipelineAsset(asset, $"{SettingsPath}/URP-LowQuality.asset");
            Debug.Log("URPQualitySetup: Low quality asset created");
        }

        private static void CreateMediumQualityAsset()
        {
            // Create renderer first
            var renderer = CreateRenderer("URP-MediumQuality_Renderer");
            string rendererPath = $"{SettingsPath}/URP-MediumQuality_Renderer.asset";
            SaveRendererAsset(renderer, rendererPath);

            // Create pipeline asset with the renderer
            var asset = UniversalRenderPipelineAsset.Create(renderer);

            // Medium quality settings
            asset.renderScale = 1.0f;
            asset.msaaSampleCount = (int)MsaaQuality._2x;
            asset.supportsHDR = true;

            // Shadows
            asset.shadowDistance = 50f;
            asset.shadowCascadeCount = 2;

            // Additional settings
            asset.supportsCameraDepthTexture = true;
            asset.supportsCameraOpaqueTexture = true;

            SavePipelineAsset(asset, $"{SettingsPath}/URP-MediumQuality.asset");
            Debug.Log("URPQualitySetup: Medium quality asset created");
        }

        private static void CreateHighQualityAsset()
        {
            // Create renderer first
            var renderer = CreateRenderer("URP-HighQuality_Renderer");
            string rendererPath = $"{SettingsPath}/URP-HighQuality_Renderer.asset";
            SaveRendererAsset(renderer, rendererPath);

            // Create pipeline asset with the renderer
            var asset = UniversalRenderPipelineAsset.Create(renderer);

            // High quality settings
            asset.renderScale = 1.0f;
            asset.msaaSampleCount = (int)MsaaQuality._4x;
            asset.supportsHDR = true;

            // Shadows
            asset.shadowDistance = 100f;
            asset.shadowCascadeCount = 4;

            // Additional settings
            asset.supportsCameraDepthTexture = true;
            asset.supportsCameraOpaqueTexture = true;

            SavePipelineAsset(asset, $"{SettingsPath}/URP-HighQuality.asset");
            Debug.Log("URPQualitySetup: High quality asset created");
        }

        private static UniversalRendererData CreateRenderer(string name)
        {
            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            renderer.name = name;
            return renderer;
        }

        private static void SaveRendererAsset(UniversalRendererData renderer, string path)
        {
            var existingAsset = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (existingAsset != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.CreateAsset(renderer, path);
            EditorUtility.SetDirty(renderer);
        }

        private static void SavePipelineAsset(UniversalRenderPipelineAsset asset, string path)
        {
            var existingAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            if (existingAsset != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.CreateAsset(asset, path);
            EditorUtility.SetDirty(asset);
        }
    }
}
