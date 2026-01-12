// ABOUTME: Editor tool for creating grass materials and WindController prefab.
// ABOUTME: Generates Fairway, Rough, and Green grass material presets using the StylizedGrass shader.

using UnityEditor;
using UnityEngine;
using OpenRange.Visualization;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor utility to generate grass materials and wind controller prefab.
    /// </summary>
    public static class GrassShaderSetup
    {
        private const string ShaderPath = "Assets/Shaders/Environment/StylizedGrass.shader";
        private const string MaterialsPath = "Assets/Materials/Environment";
        private const string PrefabsPath = "Assets/Prefabs/Environment";

        /// <summary>
        /// Grass preset type for material generation.
        /// </summary>
        public enum GrassPreset
        {
            /// <summary>Bright fairway grass with subtle wind.</summary>
            Fairway,
            /// <summary>Darker rough grass with more wind movement.</summary>
            Rough,
            /// <summary>Short putting green with no wind.</summary>
            Green
        }

        /// <summary>
        /// Configuration for a grass material preset.
        /// </summary>
        public struct GrassPresetConfig
        {
            public Color BaseColor;
            public Color TipColor;
            public float TipBlendStart;
            public float TipBlendEnd;
            public float GrassHeight;
            public float WindStrength;
            public float WindSpeed;
            public float Turbulence;
            public float GustStrength;
            public bool EnableWind;
            public bool EnableTipColor;
        }

        [MenuItem("OpenRange/Materials/Create Grass Materials", priority = 210)]
        public static void CreateGrassMaterials()
        {
            EnsureDirectoriesExist();

            CreateGrassMaterial(GrassPreset.Fairway);
            CreateGrassMaterial(GrassPreset.Rough);
            CreateGrassMaterial(GrassPreset.Green);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("GrassShaderSetup: Created all grass material presets");
        }

        [MenuItem("OpenRange/Materials/Create Fairway Grass Material", priority = 211)]
        public static void CreateFairwayGrassMaterial()
        {
            EnsureDirectoriesExist();
            CreateGrassMaterial(GrassPreset.Fairway);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("OpenRange/Materials/Create Rough Grass Material", priority = 212)]
        public static void CreateRoughGrassMaterial()
        {
            EnsureDirectoriesExist();
            CreateGrassMaterial(GrassPreset.Rough);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("OpenRange/Materials/Create Green Grass Material", priority = 213)]
        public static void CreateGreenGrassMaterial()
        {
            EnsureDirectoriesExist();
            CreateGrassMaterial(GrassPreset.Green);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("OpenRange/Materials/Create WindController Prefab", priority = 220)]
        public static void CreateWindControllerPrefab()
        {
            EnsureDirectoriesExist();

            string prefabPath = $"{PrefabsPath}/WindController.prefab";

            // Check if prefab already exists
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
            {
                Debug.Log($"GrassShaderSetup: WindController prefab already exists at {prefabPath}");
                Selection.activeObject = existingPrefab;
                return;
            }

            // Create GameObject with WindController
            var controllerGo = new GameObject("WindController");
            var controller = controllerGo.AddComponent<WindController>();

            // Configure default settings using SerializedObject
            var so = new SerializedObject(controller);
            so.FindProperty("_windSpeed").floatValue = 1.0f;
            so.FindProperty("_windStrength").floatValue = 1.0f;
            so.FindProperty("_windDirectionDegrees").floatValue = 45f;
            so.FindProperty("_turbulence").floatValue = 0.5f;
            so.FindProperty("_gustingEnabled").boolValue = true;
            so.FindProperty("_gustStrength").floatValue = 0.3f;
            so.FindProperty("_gustFrequency").floatValue = 1.0f;
            so.FindProperty("_windEnabled").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(controllerGo, prefabPath);
            Object.DestroyImmediate(controllerGo);

            Debug.Log($"GrassShaderSetup: Created WindController prefab at {prefabPath}");

            // Select the created prefab
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        [MenuItem("OpenRange/Materials/Create All Grass Assets", priority = 209)]
        public static void CreateAllGrassAssets()
        {
            EnsureDirectoriesExist();

            CreateGrassMaterials();
            CreateWindControllerPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("GrassShaderSetup: All grass assets created successfully!");
        }

        /// <summary>
        /// Creates a grass material for the specified preset.
        /// </summary>
        /// <param name="preset">The preset type.</param>
        /// <returns>The created or existing material.</returns>
        public static Material CreateGrassMaterial(GrassPreset preset)
        {
            string materialName = GetMaterialName(preset);
            string materialPath = $"{MaterialsPath}/{materialName}.mat";

            // Find the shader first
            var shader = Shader.Find("OpenRange/StylizedGrass");
            if (shader == null)
            {
                shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            }

            if (shader == null)
            {
                Debug.LogError("GrassShaderSetup: Could not find StylizedGrass shader. Make sure it exists at " + ShaderPath);
                return null;
            }

            // Check if material already exists
            var existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existingMaterial != null)
            {
                // Check if it has the correct shader
                if (existingMaterial.shader == shader)
                {
                    Debug.Log($"GrassShaderSetup: Material already exists with correct shader at {materialPath}");
                    return existingMaterial;
                }

                // Delete material with wrong shader and recreate
                Debug.Log($"GrassShaderSetup: Deleting material with wrong shader ({existingMaterial.shader?.name ?? "NULL"}) at {materialPath}");
                AssetDatabase.DeleteAsset(materialPath);
            }

            // Create material
            var material = new Material(shader);
            material.name = materialName;

            // Apply preset configuration
            var config = GetPresetConfig(preset);
            ApplyConfigToMaterial(material, config);

            // Save asset
            AssetDatabase.CreateAsset(material, materialPath);

            Debug.Log($"GrassShaderSetup: Created grass material with StylizedGrass shader at {materialPath}");
            return material;
        }

        /// <summary>
        /// Gets the configuration for a grass preset.
        /// </summary>
        /// <param name="preset">The preset type.</param>
        /// <returns>The preset configuration.</returns>
        public static GrassPresetConfig GetPresetConfig(GrassPreset preset)
        {
            return preset switch
            {
                GrassPreset.Fairway => new GrassPresetConfig
                {
                    // Bright, healthy fairway grass
                    BaseColor = new Color(0.18f, 0.55f, 0.15f, 1f),  // #2E8C26 - rich green
                    TipColor = new Color(0.35f, 0.72f, 0.25f, 1f),   // #59B840 - lighter green tips
                    TipBlendStart = 0.4f,
                    TipBlendEnd = 0.9f,
                    GrassHeight = 0.3f,
                    WindStrength = 0.6f,
                    WindSpeed = 1.5f,
                    Turbulence = 0.4f,
                    GustStrength = 0.2f,
                    EnableWind = true,
                    EnableTipColor = true
                },
                GrassPreset.Rough => new GrassPresetConfig
                {
                    // Taller, wilder rough grass
                    BaseColor = new Color(0.15f, 0.42f, 0.12f, 1f),  // #266B1E - darker green
                    TipColor = new Color(0.45f, 0.58f, 0.22f, 1f),   // #739438 - yellower tips
                    TipBlendStart = 0.3f,
                    TipBlendEnd = 1.0f,
                    GrassHeight = 0.6f,
                    WindStrength = 1.0f,
                    WindSpeed = 2.0f,
                    Turbulence = 0.7f,
                    GustStrength = 0.4f,
                    EnableWind = true,
                    EnableTipColor = true
                },
                GrassPreset.Green => new GrassPresetConfig
                {
                    // Short, uniform putting green
                    BaseColor = new Color(0.22f, 0.58f, 0.22f, 1f),  // #389438 - uniform green
                    TipColor = new Color(0.25f, 0.62f, 0.25f, 1f),   // #409E40 - barely different
                    TipBlendStart = 0.8f,
                    TipBlendEnd = 1.0f,
                    GrassHeight = 0.05f,
                    WindStrength = 0f,
                    WindSpeed = 0f,
                    Turbulence = 0f,
                    GustStrength = 0f,
                    EnableWind = false,
                    EnableTipColor = false
                },
                _ => GetPresetConfig(GrassPreset.Fairway)
            };
        }

        /// <summary>
        /// Gets the material name for a preset.
        /// </summary>
        /// <param name="preset">The preset type.</param>
        /// <returns>The material file name (without extension).</returns>
        public static string GetMaterialName(GrassPreset preset)
        {
            return preset switch
            {
                GrassPreset.Fairway => "FairwayGrass",
                GrassPreset.Rough => "RoughGrass",
                GrassPreset.Green => "GreenGrass",
                _ => "FairwayGrass"
            };
        }

        private static void ApplyConfigToMaterial(Material material, GrassPresetConfig config)
        {
            // Colors
            material.SetColor("_BaseColor", config.BaseColor);
            material.SetColor("_TipColor", config.TipColor);
            material.SetFloat("_TipBlendStart", config.TipBlendStart);
            material.SetFloat("_TipBlendEnd", config.TipBlendEnd);

            // Grass properties
            material.SetFloat("_GrassHeight", config.GrassHeight);
            material.SetFloat("_GrassWidth", 0.1f);

            // Wind
            material.SetFloat("_WindStrength", config.WindStrength);
            material.SetFloat("_WindSpeed", config.WindSpeed);
            material.SetFloat("_WindTurbulence", config.Turbulence);
            material.SetFloat("_GustStrength", config.GustStrength);
            material.SetFloat("_GustFrequency", 1.0f);

            // Lighting
            material.SetColor("_ShadowColor", new Color(0.08f, 0.15f, 0.08f, 1f));
            material.SetFloat("_Smoothness", 0.2f);
            material.SetFloat("_SpecularStrength", 0.1f);

            // Quality toggles
            material.SetFloat("_EnableWind", config.EnableWind ? 1f : 0f);
            material.SetFloat("_EnableTipColor", config.EnableTipColor ? 1f : 0f);

            // Shader keywords
            if (config.EnableWind)
            {
                material.EnableKeyword("_ENABLEWIND_ON");
            }
            else
            {
                material.DisableKeyword("_ENABLEWIND_ON");
            }

            if (config.EnableTipColor)
            {
                material.EnableKeyword("_ENABLETIPCOLOR_ON");
            }
            else
            {
                material.DisableKeyword("_ENABLETIPCOLOR_ON");
            }
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
            if (!AssetDatabase.IsValidFolder("Assets/Shaders"))
            {
                AssetDatabase.CreateFolder("Assets", "Shaders");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Shaders/Environment"))
            {
                AssetDatabase.CreateFolder("Assets/Shaders", "Environment");
            }
        }
    }
}
