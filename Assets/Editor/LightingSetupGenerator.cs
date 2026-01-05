// ABOUTME: Editor tool for creating and configuring scene lighting, skybox, and reflection probes.
// ABOUTME: Generates MarinaSkybox material and LightingController prefab with proper settings.

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using OpenRange.Visualization;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor utility to generate lighting setup assets and configure scenes.
    /// </summary>
    public static class LightingSetupGenerator
    {
        private const string ShaderPath = "Assets/Shaders/Skybox/StylizedSkybox.shader";
        private const string MaterialsPath = "Assets/Materials/Skybox";
        private const string PrefabsPath = "Assets/Prefabs/Environment";

        [MenuItem("OpenRange/Lighting/Create Skybox Material", priority = 200)]
        public static void CreateSkyboxMaterial()
        {
            EnsureDirectoriesExist();

            string materialPath = $"{MaterialsPath}/MarinaSkybox.mat";

            // Check if material already exists
            var existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existingMaterial != null)
            {
                Debug.Log($"LightingSetupGenerator: Skybox material already exists at {materialPath}");
                Selection.activeObject = existingMaterial;
                return;
            }

            // Find the shader
            var shader = Shader.Find("OpenRange/StylizedSkybox");
            if (shader == null)
            {
                // Try loading from path
                shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            }

            if (shader == null)
            {
                Debug.LogError("LightingSetupGenerator: Could not find StylizedSkybox shader. Make sure it exists at " + ShaderPath);
                return;
            }

            // Create material with Golden Hour preset
            var material = new Material(shader);
            material.name = "MarinaSkybox";

            // Sky colors - Golden Hour preset
            material.SetColor("_TopColor", new Color(0.1f, 0.3f, 0.8f, 1f));
            material.SetColor("_HorizonColor", new Color(1.0f, 0.6f, 0.4f, 1f));
            material.SetColor("_GroundColor", new Color(0.3f, 0.25f, 0.2f, 1f));
            material.SetFloat("_GradientExponent", 1.5f);

            // Sun - HDR values for bloom
            material.SetColor("_SunColor", new Color(1.5f, 1.4f, 1.0f, 1f));
            material.SetFloat("_SunSize", 0.05f);
            material.SetFloat("_SunFalloff", 50f);
            material.SetVector("_SunDirection", new Vector4(0.3f, 0.4f, -0.8f, 0f));

            // Clouds
            material.SetFloat("_CloudDensity", 0.4f);
            material.SetFloat("_CloudSpeed", 0.01f);
            material.SetFloat("_CloudScale", 8f);
            material.SetColor("_CloudColor", Color.white);
            material.SetColor("_CloudShadowColor", new Color(0.6f, 0.6f, 0.7f, 1f));
            material.SetFloat("_CloudHeight", 0.3f);

            // Horizon
            material.SetFloat("_HorizonFogDensity", 0.3f);
            material.SetFloat("_HorizonFogHeight", 0.1f);

            // Quality toggles - enable all by default
            material.SetFloat("_EnableClouds", 1f);
            material.SetFloat("_EnableSun", 1f);
            material.EnableKeyword("_ENABLECLOUDS_ON");
            material.EnableKeyword("_ENABLESUN_ON");

            AssetDatabase.CreateAsset(material, materialPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"LightingSetupGenerator: Created skybox material at {materialPath}");
            Selection.activeObject = material;
        }

        [MenuItem("OpenRange/Lighting/Create Lighting Controller Prefab", priority = 201)]
        public static void CreateLightingControllerPrefab()
        {
            EnsureDirectoriesExist();

            // Load or create skybox material
            string skyboxMaterialPath = $"{MaterialsPath}/MarinaSkybox.mat";
            var skyboxMaterial = AssetDatabase.LoadAssetAtPath<Material>(skyboxMaterialPath);
            if (skyboxMaterial == null)
            {
                CreateSkyboxMaterial();
                skyboxMaterial = AssetDatabase.LoadAssetAtPath<Material>(skyboxMaterialPath);
            }

            // Create root GameObject
            var controllerGo = new GameObject("LightingController");
            var controller = controllerGo.AddComponent<LightingController>();

            // Create directional light
            var lightGo = new GameObject("DirectionalLight");
            lightGo.transform.SetParent(controllerGo.transform);
            lightGo.transform.localPosition = Vector3.zero;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.95f, 0.85f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.8f;
            light.shadowBias = 0.05f;
            light.shadowNormalBias = 0.4f;
            light.shadowNearPlane = 0.1f;

            // Create reflection probe
            var probeGo = new GameObject("ReflectionProbe");
            probeGo.transform.SetParent(controllerGo.transform);
            probeGo.transform.localPosition = new Vector3(0f, 5f, 0f);

            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.resolution = 256;
            probe.size = new Vector3(500f, 100f, 500f);
            probe.boxProjection = true;
            probe.importance = 1;

            // Wire up references using SerializedObject
            var so = new SerializedObject(controller);
            so.FindProperty("_directionalLight").objectReferenceValue = light;
            so.FindProperty("_reflectionProbe").objectReferenceValue = probe;
            so.FindProperty("_skyboxMaterial").objectReferenceValue = skyboxMaterial;
            so.FindProperty("_currentPreset").enumValueIndex = 0; // GoldenHour
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save prefab
            string prefabPath = $"{PrefabsPath}/LightingController.prefab";
            PrefabUtility.SaveAsPrefabAsset(controllerGo, prefabPath);
            Object.DestroyImmediate(controllerGo);

            Debug.Log($"LightingSetupGenerator: Created LightingController prefab at {prefabPath}");
        }

        [MenuItem("OpenRange/Lighting/Setup Marina Lighting", priority = 202)]
        public static void SetupMarinaLighting()
        {
            if (!Application.isBatchMode)
            {
                if (!EditorUtility.DisplayDialog(
                    "Setup Marina Lighting",
                    "This will configure the current scene's lighting settings. Continue?",
                    "Yes", "Cancel"))
                {
                    return;
                }
            }

            // Create materials/prefabs if needed
            CreateSkyboxMaterial();
            CreateLightingControllerPrefab();

            // Load skybox material
            string skyboxMaterialPath = $"{MaterialsPath}/MarinaSkybox.mat";
            var skyboxMaterial = AssetDatabase.LoadAssetAtPath<Material>(skyboxMaterialPath);

            if (skyboxMaterial != null)
            {
                // Apply to render settings
                RenderSettings.skybox = skyboxMaterial;
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.4f, 0.5f, 0.7f);
                RenderSettings.ambientEquatorColor = new Color(0.6f, 0.5f, 0.4f);
                RenderSettings.ambientGroundColor = new Color(0.3f, 0.25f, 0.2f);

                // Enable fog for depth
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = new Color(0.8f, 0.7f, 0.6f);
                RenderSettings.fogDensity = 0.002f;
            }

            // Find or create LightingController in scene
            var existingController = Object.FindFirstObjectByType<LightingController>();
            if (existingController == null)
            {
                // Load prefab and instantiate
                string prefabPath = $"{PrefabsPath}/LightingController.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    if (instance != null)
                    {
                        instance.transform.position = Vector3.zero;
                        Undo.RegisterCreatedObjectUndo(instance, "Create LightingController");
                        Debug.Log("LightingSetupGenerator: Added LightingController to scene");
                    }
                }
            }
            else
            {
                Debug.Log("LightingSetupGenerator: LightingController already exists in scene");
            }

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("LightingSetupGenerator: Marina lighting setup complete");
        }

        [MenuItem("OpenRange/Lighting/Create All Lighting Assets", priority = 199)]
        public static void CreateAllLightingAssets()
        {
            EnsureDirectoriesExist();

            CreateSkyboxMaterial();
            CreateLightingControllerPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("LightingSetupGenerator: All lighting assets created successfully!");
        }

        /// <summary>
        /// Creates a material preset for a specific lighting preset.
        /// </summary>
        /// <param name="presetName">Name of the preset (GoldenHour, Midday, Overcast).</param>
        public static void CreatePresetMaterial(string presetName)
        {
            EnsureDirectoriesExist();

            string materialPath = $"{MaterialsPath}/{presetName}Skybox.mat";

            var existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existingMaterial != null)
            {
                Debug.Log($"LightingSetupGenerator: Preset material already exists at {materialPath}");
                return;
            }

            var shader = Shader.Find("OpenRange/StylizedSkybox");
            if (shader == null)
            {
                shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            }

            if (shader == null)
            {
                Debug.LogError("LightingSetupGenerator: Could not find StylizedSkybox shader");
                return;
            }

            var material = new Material(shader);
            material.name = $"{presetName}Skybox";

            // Apply preset-specific settings
            switch (presetName)
            {
                case "Midday":
                    material.SetColor("_TopColor", new Color(0.15f, 0.4f, 0.9f, 1f));
                    material.SetColor("_HorizonColor", new Color(0.7f, 0.85f, 1.0f, 1f));
                    material.SetColor("_SunColor", new Color(1.2f, 1.2f, 1.0f, 1f));
                    material.SetVector("_SunDirection", new Vector4(0.1f, 0.9f, -0.3f, 0f));
                    material.SetFloat("_CloudDensity", 0.3f);
                    break;

                case "Overcast":
                    material.SetColor("_TopColor", new Color(0.4f, 0.45f, 0.5f, 1f));
                    material.SetColor("_HorizonColor", new Color(0.6f, 0.65f, 0.7f, 1f));
                    material.SetColor("_SunColor", new Color(0.8f, 0.8f, 0.8f, 1f));
                    material.SetVector("_SunDirection", new Vector4(0.2f, 0.6f, -0.5f, 0f));
                    material.SetFloat("_CloudDensity", 0.7f);
                    break;

                default: // GoldenHour
                    material.SetColor("_TopColor", new Color(0.1f, 0.3f, 0.8f, 1f));
                    material.SetColor("_HorizonColor", new Color(1.0f, 0.6f, 0.4f, 1f));
                    material.SetColor("_SunColor", new Color(1.5f, 1.4f, 1.0f, 1f));
                    material.SetVector("_SunDirection", new Vector4(0.3f, 0.4f, -0.8f, 0f));
                    material.SetFloat("_CloudDensity", 0.4f);
                    break;
            }

            // Common settings
            material.SetColor("_GroundColor", new Color(0.3f, 0.25f, 0.2f, 1f));
            material.SetFloat("_GradientExponent", 1.5f);
            material.SetFloat("_SunSize", 0.05f);
            material.SetFloat("_SunFalloff", 50f);
            material.SetFloat("_CloudSpeed", 0.01f);
            material.SetFloat("_CloudScale", 8f);
            material.SetColor("_CloudColor", Color.white);
            material.SetColor("_CloudShadowColor", new Color(0.6f, 0.6f, 0.7f, 1f));
            material.SetFloat("_CloudHeight", 0.3f);
            material.SetFloat("_HorizonFogDensity", 0.3f);
            material.SetFloat("_HorizonFogHeight", 0.1f);
            material.SetFloat("_EnableClouds", 1f);
            material.SetFloat("_EnableSun", 1f);
            material.EnableKeyword("_ENABLECLOUDS_ON");
            material.EnableKeyword("_ENABLESUN_ON");

            AssetDatabase.CreateAsset(material, materialPath);
            Debug.Log($"LightingSetupGenerator: Created preset material at {materialPath}");
        }

        private static void EnsureDirectoriesExist()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            if (!AssetDatabase.IsValidFolder(MaterialsPath))
            {
                AssetDatabase.CreateFolder("Assets/Materials", "Skybox");
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
            if (!AssetDatabase.IsValidFolder("Assets/Shaders/Skybox"))
            {
                AssetDatabase.CreateFolder("Assets/Shaders", "Skybox");
            }
        }
    }
}
