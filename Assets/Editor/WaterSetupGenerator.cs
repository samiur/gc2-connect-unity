// ABOUTME: Editor tool for creating water materials, prefabs, and controller setup.
// ABOUTME: Generates Ocean and Pond water presets with proper shader and URP configuration.

using UnityEngine;
using UnityEditor;
using System.IO;
using OpenRange.Visualization;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor tool for generating water materials, prefabs, and setup.
    /// </summary>
    public static class WaterSetupGenerator
    {
        private const string MaterialsPath = "Assets/Materials/Environment";
        private const string PrefabsPath = "Assets/Prefabs/Environment";
        private const string TexturesPath = "Assets/Textures/Water";
        private const string ShaderName = "OpenRange/StylizedWater";

        [MenuItem("OpenRange/Create Ocean Water Material", priority = 310)]
        public static void CreateOceanWaterMaterial()
        {
            EnsureDirectoriesExist();

            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"Water shader '{ShaderName}' not found. Make sure StylizedWater.shader exists.");
                return;
            }

            var material = new Material(shader);
            material.name = "OceanWater";

            // Apply ocean preset settings
            material.SetColor("_ShallowColor", new Color(0.2f, 0.6f, 0.8f, 0.8f));
            material.SetColor("_DeepColor", new Color(0.05f, 0.2f, 0.4f, 1.0f));
            material.SetFloat("_DepthFadeDistance", 8.0f);
            material.SetFloat("_WaveHeight", 0.3f);
            material.SetFloat("_WaveSpeed", 1.0f);
            material.SetFloat("_WaveLength", 8.0f);
            material.SetFloat("_WaveSteepness", 0.5f);
            material.SetVector("_WaveDirection", new Vector4(1, 0, 0.5f, 0).normalized);
            material.SetColor("_FoamColor", new Color(1.0f, 1.0f, 1.0f, 0.8f));
            material.SetFloat("_FoamWidth", 0.5f);
            material.SetFloat("_FoamIntensity", 1.0f);
            material.SetFloat("_ReflectionStrength", 0.5f);
            material.SetFloat("_FresnelPower", 5.0f);
            material.SetFloat("_NormalStrength", 1.0f);
            material.SetFloat("_NormalSpeed1", 0.02f);
            material.SetFloat("_NormalSpeed2", 0.015f);
            material.SetFloat("_NormalScale", 1.0f);
            material.SetFloat("_Smoothness", 0.9f);
            material.SetFloat("_SpecularStrength", 1.0f);

            // Enable all features by default
            material.EnableKeyword("_ENABLEWAVES_ON");
            material.EnableKeyword("_ENABLEFOAM_ON");
            material.EnableKeyword("_ENABLEREFLECTION_ON");

            // Set render queue for transparency
            material.renderQueue = 3000;

            string path = $"{MaterialsPath}/OceanWater.mat";
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();

            Debug.Log($"Created Ocean Water material at {path}");
            Selection.activeObject = material;
        }

        [MenuItem("OpenRange/Create Pond Water Material", priority = 311)]
        public static void CreatePondWaterMaterial()
        {
            EnsureDirectoriesExist();

            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"Water shader '{ShaderName}' not found. Make sure StylizedWater.shader exists.");
                return;
            }

            var material = new Material(shader);
            material.name = "PondWater";

            // Apply pond preset settings (calmer, greener)
            material.SetColor("_ShallowColor", new Color(0.3f, 0.5f, 0.4f, 0.7f));
            material.SetColor("_DeepColor", new Color(0.1f, 0.25f, 0.2f, 1.0f));
            material.SetFloat("_DepthFadeDistance", 3.0f);
            material.SetFloat("_WaveHeight", 0.05f);
            material.SetFloat("_WaveSpeed", 0.5f);
            material.SetFloat("_WaveLength", 4.0f);
            material.SetFloat("_WaveSteepness", 0.2f);
            material.SetVector("_WaveDirection", new Vector4(1, 0, 0.3f, 0).normalized);
            material.SetColor("_FoamColor", new Color(0.9f, 0.95f, 0.9f, 0.5f));
            material.SetFloat("_FoamWidth", 0.2f);
            material.SetFloat("_FoamIntensity", 0.5f);
            material.SetFloat("_ReflectionStrength", 0.7f);
            material.SetFloat("_FresnelPower", 4.0f);
            material.SetFloat("_NormalStrength", 0.5f);
            material.SetFloat("_NormalSpeed1", 0.01f);
            material.SetFloat("_NormalSpeed2", 0.008f);
            material.SetFloat("_NormalScale", 0.5f);
            material.SetFloat("_Smoothness", 0.95f);
            material.SetFloat("_SpecularStrength", 0.8f);

            // Enable features (but waves are subtle)
            material.EnableKeyword("_ENABLEWAVES_ON");
            material.EnableKeyword("_ENABLEFOAM_ON");
            material.EnableKeyword("_ENABLEREFLECTION_ON");

            // Set render queue for transparency
            material.renderQueue = 3000;

            string path = $"{MaterialsPath}/PondWater.mat";
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();

            Debug.Log($"Created Pond Water material at {path}");
            Selection.activeObject = material;
        }

        [MenuItem("OpenRange/Create Water Plane Prefab", priority = 312)]
        public static void CreateWaterPlanePrefab()
        {
            EnsureDirectoriesExist();

            // Check for ocean material, create if needed
            var oceanMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/OceanWater.mat");
            if (oceanMaterial == null)
            {
                CreateOceanWaterMaterial();
                oceanMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/OceanWater.mat");
            }

            // Create water plane GameObject
            var waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            waterPlane.name = "WaterPlane";

            // Scale for a larger water surface
            waterPlane.transform.localScale = new Vector3(10, 1, 10);

            // Apply material
            var renderer = waterPlane.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = oceanMaterial;

            // Remove collider (water shouldn't have physics by default)
            var collider = waterPlane.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            // Add WaterPlane component for registration with WaterController
            var waterPlaneComponent = waterPlane.AddComponent<WaterPlane>();

            // Save prefab
            string prefabPath = $"{PrefabsPath}/WaterPlane.prefab";
            PrefabUtility.SaveAsPrefabAsset(waterPlane, prefabPath);
            Object.DestroyImmediate(waterPlane);

            Debug.Log($"Created Water Plane prefab at {prefabPath}");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        [MenuItem("OpenRange/Create Water Controller Prefab", priority = 313)]
        public static void CreateWaterControllerPrefab()
        {
            EnsureDirectoriesExist();

            // Check for ocean material
            var oceanMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/OceanWater.mat");
            if (oceanMaterial == null)
            {
                CreateOceanWaterMaterial();
                oceanMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/OceanWater.mat");
            }

            // Create controller GameObject
            var controllerGo = new GameObject("WaterController");

            // Add WaterController component
            var controller = controllerGo.AddComponent<OpenRange.Visualization.WaterController>();

            // Set material reference using SerializedObject
            var so = new SerializedObject(controller);
            so.FindProperty("_waterMaterial").objectReferenceValue = oceanMaterial;
            so.FindProperty("_currentPreset").enumValueIndex = 0; // Ocean
            so.FindProperty("_waterEnabled").boolValue = true;
            so.FindProperty("_reflectionLayers").intValue = -1; // Everything
            so.FindProperty("_reflectionClipPlaneOffset").floatValue = 0.07f;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save prefab
            string prefabPath = $"{PrefabsPath}/WaterController.prefab";
            PrefabUtility.SaveAsPrefabAsset(controllerGo, prefabPath);
            Object.DestroyImmediate(controllerGo);

            Debug.Log($"Created Water Controller prefab at {prefabPath}");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        [MenuItem("OpenRange/Create All Water Prefabs", priority = 314)]
        public static void CreateAllWaterPrefabs()
        {
            // Skip dialog in batchmode
            if (!Application.isBatchMode)
            {
                if (!EditorUtility.DisplayDialog("Create All Water Prefabs",
                    "This will create:\n" +
                    "- Ocean Water material\n" +
                    "- Pond Water material\n" +
                    "- Water Plane prefab\n" +
                    "- Water Controller prefab\n\n" +
                    "Continue?", "Create", "Cancel"))
                {
                    return;
                }
            }

            CreateOceanWaterMaterial();
            CreatePondWaterMaterial();
            CreateWaterPlanePrefab();
            CreateWaterControllerPrefab();

            Debug.Log("Created all water prefabs and materials successfully!");
        }

        [MenuItem("OpenRange/Create Water Normal Map", priority = 315)]
        public static void CreateWaterNormalMap()
        {
            EnsureDirectoriesExist();

            // Create a simple procedural normal map
            int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            texture.name = "WaterNormal";

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Generate a simple wave normal pattern
                    float u = (float)x / size;
                    float v = (float)y / size;

                    // Multi-octave noise simulation
                    float nx = Mathf.Sin(u * 8 * Mathf.PI) * 0.3f + Mathf.Sin(u * 16 * Mathf.PI + v * 8 * Mathf.PI) * 0.1f;
                    float ny = Mathf.Cos(v * 8 * Mathf.PI) * 0.3f + Mathf.Cos(v * 16 * Mathf.PI + u * 8 * Mathf.PI) * 0.1f;

                    // Convert to normal map format (0.5 = neutral)
                    Color normalColor = new Color(
                        nx * 0.5f + 0.5f,
                        ny * 0.5f + 0.5f,
                        1.0f,
                        1.0f
                    );

                    texture.SetPixel(x, y, normalColor);
                }
            }

            texture.Apply();

            // Save texture
            byte[] pngData = texture.EncodeToPNG();
            string path = $"{TexturesPath}/WaterNormal.png";
            File.WriteAllBytes(path, pngData);
            Object.DestroyImmediate(texture);

            AssetDatabase.Refresh();

            // Configure as normal map
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear;
                importer.SaveAndReimport();
            }

            Debug.Log($"Created Water Normal Map at {path}");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void EnsureDirectoriesExist()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            if (!AssetDatabase.IsValidFolder(MaterialsPath))
            {
                AssetDatabase.CreateFolder("Assets/Materials", "Environment");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(PrefabsPath))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Environment");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Textures"))
            {
                AssetDatabase.CreateFolder("Assets", "Textures");
            }
            if (!AssetDatabase.IsValidFolder(TexturesPath))
            {
                AssetDatabase.CreateFolder("Assets/Textures", "Water");
            }
        }
    }
}
