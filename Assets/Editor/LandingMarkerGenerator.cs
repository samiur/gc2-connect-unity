// ABOUTME: Editor tool that generates the LandingMarker and LandingDust prefabs with materials.
// ABOUTME: Creates properly configured visual components for landing position feedback.

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor utility to generate the LandingMarker and LandingDust prefabs and materials.
    /// </summary>
    public static class LandingMarkerGenerator
    {
        private const string MaterialsPath = "Assets/Materials/Effects";
        private const string PrefabsPath = "Assets/Prefabs/Effects";

        [MenuItem("OpenRange/Create Landing Marker Prefab", priority = 157)]
        public static void CreateLandingMarkerPrefab()
        {
            if (!EditorUtility.DisplayDialog(
                "Create Landing Marker Prefab",
                "This will create the LandingMarker prefab and material. Existing files will be overwritten. Continue?",
                "Create",
                "Cancel"))
            {
                return;
            }

            EnsureDirectoriesExist();

            // Create material
            var ringMaterial = CreateLandingMarkerMaterial();

            // Create prefab
            CreateLandingMarkerPrefabAsset(ringMaterial);

            AssetDatabase.Refresh();
            Debug.Log("LandingMarkerGenerator: Landing marker prefab and material created successfully!");
        }

        [MenuItem("OpenRange/Create Landing Dust Prefab", priority = 158)]
        public static void CreateLandingDustPrefab()
        {
            if (!EditorUtility.DisplayDialog(
                "Create Landing Dust Prefab",
                "This will create the LandingDust particle effect prefab and material. Existing files will be overwritten. Continue?",
                "Create",
                "Cancel"))
            {
                return;
            }

            EnsureDirectoriesExist();

            // Create material
            var dustMaterial = CreateLandingDustMaterial();

            // Create prefab
            CreateLandingDustPrefabAsset(dustMaterial);

            AssetDatabase.Refresh();
            Debug.Log("LandingMarkerGenerator: Landing dust prefab and material created successfully!");
        }

        [MenuItem("OpenRange/Create All Landing Effects", priority = 159)]
        public static void CreateAllLandingEffects()
        {
            if (!EditorUtility.DisplayDialog(
                "Create All Landing Effects",
                "This will create the LandingMarker and LandingDust prefabs with materials. Existing files will be overwritten. Continue?",
                "Create",
                "Cancel"))
            {
                return;
            }

            EnsureDirectoriesExist();

            // Create landing marker
            var ringMaterial = CreateLandingMarkerMaterial();
            CreateLandingMarkerPrefabAsset(ringMaterial);

            // Create landing dust
            var dustMaterial = CreateLandingDustMaterial();
            CreateLandingDustPrefabAsset(dustMaterial);

            AssetDatabase.Refresh();
            Debug.Log("LandingMarkerGenerator: All landing effects created successfully!");
        }

        private static void EnsureDirectoriesExist()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            if (!AssetDatabase.IsValidFolder(MaterialsPath))
            {
                AssetDatabase.CreateFolder("Assets/Materials", "Effects");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(PrefabsPath))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Effects");
            }
        }

        /// <summary>
        /// Creates the landing marker ring material.
        /// </summary>
        private static Material CreateLandingMarkerMaterial()
        {
            // Use URP unlit shader for clean ring rendering
            Shader ringShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (ringShader == null)
            {
                ringShader = Shader.Find("Particles/Standard Unlit");
            }
            if (ringShader == null)
            {
                ringShader = Shader.Find("Unlit/Transparent");
            }

            var material = new Material(ringShader);
            material.name = "LandingMarker";

            // White with transparency for ring glow effect
            material.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.8f));

            // Set rendering mode to transparent
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f); // Transparent
            }
            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f); // Alpha blend
            }

            // Render queue for transparent objects
            material.renderQueue = (int)RenderQueue.Transparent;

            // Save material
            string materialPath = $"{MaterialsPath}/LandingMarker.mat";
            AssetDatabase.CreateAsset(material, materialPath);
            Debug.Log($"LandingMarkerGenerator: Created landing marker material at {materialPath}");

            return material;
        }

        /// <summary>
        /// Creates the landing dust particle material.
        /// </summary>
        private static Material CreateLandingDustMaterial()
        {
            // Use URP particles shader
            Shader dustShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (dustShader == null)
            {
                dustShader = Shader.Find("Particles/Standard Unlit");
            }
            if (dustShader == null)
            {
                dustShader = Shader.Find("Unlit/Transparent");
            }

            var material = new Material(dustShader);
            material.name = "LandingDust";

            // Tan/brown color for dust
            Color dustColor = new Color(0.83f, 0.65f, 0.46f, 0.7f); // #D4A574 with alpha
            material.SetColor("_BaseColor", dustColor);

            // Set rendering mode to transparent
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f); // Transparent
            }
            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 0f); // Alpha blend
            }

            // Render queue for transparent objects
            material.renderQueue = (int)RenderQueue.Transparent;

            // Save material
            string materialPath = $"{MaterialsPath}/LandingDust.mat";
            AssetDatabase.CreateAsset(material, materialPath);
            Debug.Log($"LandingMarkerGenerator: Created landing dust material at {materialPath}");

            return material;
        }

        /// <summary>
        /// Creates the LandingMarker prefab with ring and text components.
        /// </summary>
        private static void CreateLandingMarkerPrefabAsset(Material ringMaterial)
        {
            // Create root object
            var markerGo = new GameObject("LandingMarker");

            // Create ring child (using a flattened cylinder)
            var ringGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringGo.name = "Ring";
            ringGo.transform.SetParent(markerGo.transform);
            ringGo.transform.localPosition = Vector3.zero;
            ringGo.transform.localScale = new Vector3(3f, 0.01f, 3f); // Flat ring, 3m diameter

            // Remove collider - we don't need physics
            var collider = ringGo.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            // Apply material
            var ringRenderer = ringGo.GetComponent<Renderer>();
            if (ringRenderer != null && ringMaterial != null)
            {
                ringRenderer.material = ringMaterial;
                ringRenderer.shadowCastingMode = ShadowCastingMode.Off;
                ringRenderer.receiveShadows = false;
            }

            // Create carry distance text
            var carryTextGo = new GameObject("CarryDistanceText");
            carryTextGo.transform.SetParent(markerGo.transform);
            carryTextGo.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            carryTextGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Face up

            var carryText = carryTextGo.AddComponent<TextMeshPro>();
            carryText.text = "0.0 yd";
            carryText.fontSize = 3f;
            carryText.alignment = TextAlignmentOptions.Center;
            carryText.color = Color.white;

            // Set up RectTransform for text
            var carryRect = carryTextGo.GetComponent<RectTransform>();
            carryRect.sizeDelta = new Vector2(5f, 2f);

            // Create total distance text
            var totalTextGo = new GameObject("TotalDistanceText");
            totalTextGo.transform.SetParent(markerGo.transform);
            totalTextGo.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            totalTextGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Face up

            var totalText = totalTextGo.AddComponent<TextMeshPro>();
            totalText.text = "(Total: 0.0 yd)";
            totalText.fontSize = 2f;
            totalText.alignment = TextAlignmentOptions.Center;
            totalText.color = new Color(0.8f, 0.8f, 0.8f, 1f); // Slightly dimmer

            // Set up RectTransform for total text
            var totalRect = totalTextGo.GetComponent<RectTransform>();
            totalRect.sizeDelta = new Vector2(5f, 1.5f);

            // Add LandingMarker component
            var landingMarker = markerGo.AddComponent<Visualization.LandingMarker>();

            // Wire up references using SerializedObject
            var so = new SerializedObject(landingMarker);
            so.FindProperty("_ringTransform").objectReferenceValue = ringGo.transform;
            so.FindProperty("_ringRenderer").objectReferenceValue = ringRenderer;
            so.FindProperty("_carryDistanceText").objectReferenceValue = carryText;
            so.FindProperty("_totalDistanceText").objectReferenceValue = totalText;
            so.FindProperty("_ringDiameter").floatValue = 3f;
            so.FindProperty("_heightOffset").floatValue = 0.05f;
            so.FindProperty("_showTotalDistance").boolValue = true;
            so.FindProperty("_distanceFormat").stringValue = "{0:F1} yd";
            so.FindProperty("_fadeInDuration").floatValue = 0.3f;
            so.FindProperty("_autoHideDuration").floatValue = 5f;
            so.FindProperty("_fadeOutDuration").floatValue = 1f;
            so.FindProperty("_fadeOutDurationHigh").floatValue = 1f;
            so.FindProperty("_fadeOutDurationLow").floatValue = 0.5f;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save as prefab
            string prefabPath = $"{PrefabsPath}/LandingMarker.prefab";
            PrefabUtility.SaveAsPrefabAsset(markerGo, prefabPath);
            Object.DestroyImmediate(markerGo);

            Debug.Log($"LandingMarkerGenerator: Created landing marker prefab at {prefabPath}");
        }

        /// <summary>
        /// Creates the LandingDust prefab with particle system.
        /// </summary>
        private static void CreateLandingDustPrefabAsset(Material dustMaterial)
        {
            // Create root object with particle system
            var dustGo = new GameObject("LandingDust");
            var ps = dustGo.AddComponent<ParticleSystem>();

            // Configure main module
            var main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startColor = new Color(0.83f, 0.65f, 0.46f, 0.7f); // Tan/brown
            main.gravityModifier = 0.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.None;

            // Configure emission module
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            // Add burst for impact
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 30) // 30 particles at time 0
            });

            // Configure shape module (hemisphere pointing up)
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.3f;
            shape.rotation = new Vector3(-90f, 0f, 0f); // Point upward

            // Configure color over lifetime (fade out)
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.83f, 0.65f, 0.46f), 0f),
                    new GradientColorKey(new Color(0.83f, 0.65f, 0.46f), 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.7f, 0f),
                    new GradientAlphaKey(0.7f, 0.3f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            // Configure size over lifetime (shrink)
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

            // Configure renderer
            var renderer = dustGo.GetComponent<ParticleSystemRenderer>();
            if (renderer != null && dustMaterial != null)
            {
                renderer.material = dustMaterial;
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.sortingOrder = 2;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            // Add ImpactEffect component
            var impactEffect = dustGo.AddComponent<Visualization.ImpactEffect>();

            // Wire up references using SerializedObject
            var so = new SerializedObject(impactEffect);
            so.FindProperty("_particleSystem").objectReferenceValue = ps;
            so.FindProperty("_minVelocity").floatValue = 5f;
            so.FindProperty("_maxVelocity").floatValue = 50f;
            so.FindProperty("_minEmissionMultiplier").floatValue = 0.5f;
            so.FindProperty("_maxEmissionMultiplier").floatValue = 1.5f;
            so.FindProperty("_particleCountHigh").intValue = 30;
            so.FindProperty("_particleCountMedium").intValue = 20;
            so.FindProperty("_particleCountLow").intValue = 10;
            so.FindProperty("_autoDestroy").boolValue = false;
            so.FindProperty("_returnToPool").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save as prefab
            string prefabPath = $"{PrefabsPath}/LandingDust.prefab";
            PrefabUtility.SaveAsPrefabAsset(dustGo, prefabPath);
            Object.DestroyImmediate(dustGo);

            Debug.Log($"LandingMarkerGenerator: Created landing dust prefab at {prefabPath}");
        }
    }
}
