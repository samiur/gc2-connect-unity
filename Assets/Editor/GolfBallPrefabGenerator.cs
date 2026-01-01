// ABOUTME: Editor tool that generates the GolfBall prefab and associated materials.
// ABOUTME: Creates properly configured ball mesh, materials, and trail renderer.

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor utility to generate the GolfBall prefab and materials.
    /// </summary>
    public static class GolfBallPrefabGenerator
    {
        private const string MaterialsPath = "Assets/Materials/Ball";
        private const string PrefabsPath = "Assets/Prefabs/Ball";
        private const string TexturesPath = "Assets/Textures/Ball";

        // Golf ball constants
        private const float GolfBallDiameterMeters = 0.04267f; // Regulation golf ball diameter
        private const float GolfBallRadius = GolfBallDiameterMeters / 2f;

        [MenuItem("OpenRange/Create Golf Ball Prefab", priority = 150)]
        public static void CreateGolfBallPrefab()
        {
            if (!EditorUtility.DisplayDialog(
                "Create Golf Ball Prefab",
                "This will create the GolfBall prefab, material, and trail prefab. Existing files will be overwritten. Continue?",
                "Create",
                "Cancel"))
            {
                return;
            }

            EnsureDirectoriesExist();

            // Create material first
            var material = CreateGolfBallMaterial();

            // Create trail prefab
            var trailPrefab = CreateBallTrailPrefab();

            // Create main golf ball prefab
            CreateGolfBallPrefabAsset(material, trailPrefab);

            AssetDatabase.Refresh();
            Debug.Log("GolfBallPrefabGenerator: Golf ball prefab and materials created successfully!");
        }

        [MenuItem("OpenRange/Create Golf Ball Material Only", priority = 151)]
        public static void CreateGolfBallMaterialOnly()
        {
            EnsureDirectoriesExist();
            CreateGolfBallMaterial();
            AssetDatabase.Refresh();
            Debug.Log("GolfBallPrefabGenerator: Golf ball material created");
        }

        [MenuItem("OpenRange/Create Ball Trail Prefab Only", priority = 152)]
        public static void CreateBallTrailPrefabOnly()
        {
            EnsureDirectoriesExist();
            CreateBallTrailPrefab();
            AssetDatabase.Refresh();
            Debug.Log("GolfBallPrefabGenerator: Ball trail prefab created");
        }

        private static void EnsureDirectoriesExist()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            if (!AssetDatabase.IsValidFolder(MaterialsPath))
            {
                AssetDatabase.CreateFolder("Assets/Materials", "Ball");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(PrefabsPath))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Ball");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Textures"))
            {
                AssetDatabase.CreateFolder("Assets", "Textures");
            }
            if (!AssetDatabase.IsValidFolder(TexturesPath))
            {
                AssetDatabase.CreateFolder("Assets/Textures", "Ball");
            }
        }

        /// <summary>
        /// Creates the golf ball URP/Lit material.
        /// </summary>
        private static Material CreateGolfBallMaterial()
        {
            // Find the URP Lit shader
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null)
            {
                Debug.LogWarning("URP Lit shader not found, falling back to Standard shader");
                urpLitShader = Shader.Find("Standard");
            }

            var material = new Material(urpLitShader);
            material.name = "GolfBall";

            // Set material properties for a golf ball appearance
            // White base color with slight off-white tint
            material.SetColor("_BaseColor", new Color(0.98f, 0.98f, 0.96f, 1f));

            // Smoothness - golf balls are fairly shiny
            material.SetFloat("_Smoothness", 0.8f);

            // Metallic - golf balls are not metallic
            material.SetFloat("_Metallic", 0f);

            // Enable specular highlights for that glossy look
            if (material.HasProperty("_SpecularHighlights"))
            {
                material.SetFloat("_SpecularHighlights", 1f);
            }

            // Save the material
            string materialPath = $"{MaterialsPath}/GolfBall.mat";
            AssetDatabase.CreateAsset(material, materialPath);
            Debug.Log($"GolfBallPrefabGenerator: Created material at {materialPath}");

            return material;
        }

        /// <summary>
        /// Creates the ball trail prefab with TrailRenderer.
        /// </summary>
        private static GameObject CreateBallTrailPrefab()
        {
            var trailGo = new GameObject("BallTrail");

            var trailRenderer = trailGo.AddComponent<TrailRenderer>();

            // Trail width: starts at ball size, tapers to nothing
            trailRenderer.startWidth = 0.02f;
            trailRenderer.endWidth = 0.005f;

            // Trail time
            trailRenderer.time = 1.5f;

            // Trail vertices for smoothness
            trailRenderer.numCornerVertices = 30;
            trailRenderer.numCapVertices = 15;

            // Minimum vertex distance
            trailRenderer.minVertexDistance = 0.05f;

            // Create trail material
            var trailMaterial = CreateTrailMaterial();
            trailRenderer.material = trailMaterial;

            // Set up gradient (white to transparent)
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.8f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            trailRenderer.colorGradient = gradient;

            // Shadow settings
            trailRenderer.shadowCastingMode = ShadowCastingMode.Off;
            trailRenderer.receiveShadows = false;

            // Auto destruct disabled - we manage lifecycle
            trailRenderer.autodestruct = false;

            // Save as prefab
            string prefabPath = $"{PrefabsPath}/BallTrail.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(trailGo, prefabPath);
            Object.DestroyImmediate(trailGo);

            Debug.Log($"GolfBallPrefabGenerator: Created trail prefab at {prefabPath}");

            return prefab;
        }

        /// <summary>
        /// Creates material for the trail renderer.
        /// </summary>
        private static Material CreateTrailMaterial()
        {
            // Use URP unlit shader for trails
            Shader trailShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (trailShader == null)
            {
                trailShader = Shader.Find("Particles/Standard Unlit");
            }
            if (trailShader == null)
            {
                // Fallback to any unlit shader
                trailShader = Shader.Find("Unlit/Color");
            }

            var material = new Material(trailShader);
            material.name = "BallTrail";

            // Set up for additive blending for a glowing effect
            material.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.8f));

            // Enable soft particles if available
            if (material.HasProperty("_SoftParticlesEnabled"))
            {
                material.SetFloat("_SoftParticlesEnabled", 1f);
            }

            // Set rendering mode to additive/transparent
            material.SetFloat("_Surface", 1f); // Transparent
            material.SetFloat("_Blend", 0f); // Alpha

            // Save material
            string materialPath = $"{MaterialsPath}/BallTrail.mat";
            AssetDatabase.CreateAsset(material, materialPath);
            Debug.Log($"GolfBallPrefabGenerator: Created trail material at {materialPath}");

            return material;
        }

        /// <summary>
        /// Creates the main GolfBall prefab.
        /// </summary>
        private static void CreateGolfBallPrefabAsset(Material material, GameObject trailPrefab)
        {
            // Create root object
            var ballGo = new GameObject("GolfBall");
            ballGo.tag = "Ball";
            ballGo.layer = LayerMask.NameToLayer("Default"); // Will need "Ball" layer later

            // Create mesh child (sphere)
            var meshGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            meshGo.name = "Mesh";
            meshGo.transform.SetParent(ballGo.transform);
            meshGo.transform.localPosition = Vector3.zero;
            meshGo.transform.localScale = Vector3.one * GolfBallDiameterMeters;

            // Remove the collider (we use our own physics)
            Object.DestroyImmediate(meshGo.GetComponent<SphereCollider>());

            // Apply material
            var renderer = meshGo.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            // Enable shadows
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;

            // Create spin indicator child (hidden by default)
            var spinIndicatorGo = new GameObject("SpinIndicator");
            spinIndicatorGo.transform.SetParent(ballGo.transform);
            spinIndicatorGo.transform.localPosition = Vector3.zero;
            spinIndicatorGo.SetActive(false);

            // Add a simple arrow indicator for spin direction (optional visual)
            // For now, just a small cylinder pointing in the spin direction
            var spinArrow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spinArrow.name = "SpinArrow";
            spinArrow.transform.SetParent(spinIndicatorGo.transform);
            spinArrow.transform.localPosition = new Vector3(0, GolfBallRadius * 1.5f, 0);
            spinArrow.transform.localScale = new Vector3(0.005f, GolfBallRadius * 0.5f, 0.005f);
            Object.DestroyImmediate(spinArrow.GetComponent<CapsuleCollider>());

            // Give spin arrow a red color for visibility
            var spinArrowMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            spinArrowMat.SetColor("_BaseColor", Color.red);
            spinArrowMat.name = "SpinIndicator";
            spinArrow.GetComponent<MeshRenderer>().sharedMaterial = spinArrowMat;

            string spinMatPath = $"{MaterialsPath}/SpinIndicator.mat";
            AssetDatabase.CreateAsset(spinArrowMat, spinMatPath);

            // Create trail attachment point
            var trailAttachGo = new GameObject("TrailAttach");
            trailAttachGo.transform.SetParent(ballGo.transform);
            trailAttachGo.transform.localPosition = Vector3.zero;

            // Add trail renderer as child (instantiate from prefab reference later)
            // For the prefab, we'll add a TrailRenderer component directly
            var trailRenderer = trailAttachGo.AddComponent<TrailRenderer>();

            // Copy settings from trail prefab
            if (trailPrefab != null)
            {
                var sourceTrail = trailPrefab.GetComponent<TrailRenderer>();
                if (sourceTrail != null)
                {
                    trailRenderer.startWidth = sourceTrail.startWidth;
                    trailRenderer.endWidth = sourceTrail.endWidth;
                    trailRenderer.time = sourceTrail.time;
                    trailRenderer.numCornerVertices = sourceTrail.numCornerVertices;
                    trailRenderer.numCapVertices = sourceTrail.numCapVertices;
                    trailRenderer.minVertexDistance = sourceTrail.minVertexDistance;
                    trailRenderer.colorGradient = sourceTrail.colorGradient;
                    trailRenderer.material = sourceTrail.sharedMaterial;
                    trailRenderer.shadowCastingMode = sourceTrail.shadowCastingMode;
                    trailRenderer.receiveShadows = sourceTrail.receiveShadows;
                }
            }

            // Add BallVisuals component
            var ballVisuals = ballGo.AddComponent<Visualization.BallVisuals>();

            // Add BallController component for animation
            var ballController = ballGo.AddComponent<Visualization.BallController>();

            // Add BallSpinner component for spin visualization
            var ballSpinner = ballGo.AddComponent<Visualization.BallSpinner>();

            // Wire up BallVisuals references using SerializedObject
            var so = new SerializedObject(ballVisuals);
            so.FindProperty("_trailRenderer").objectReferenceValue = trailRenderer;
            so.FindProperty("_spinIndicator").objectReferenceValue = spinIndicatorGo.transform;
            so.FindProperty("_trailEnabledByDefault").boolValue = true;
            so.FindProperty("_showSpinByDefault").boolValue = false;
            so.FindProperty("_spinVisualizationScale").floatValue = 0.1f; // Slow down visual spin
            so.FindProperty("_trailVerticesHigh").intValue = 30;
            so.FindProperty("_trailVerticesLow").intValue = 10;
            so.FindProperty("_trailTimeHigh").floatValue = 1.5f;
            so.FindProperty("_trailTimeLow").floatValue = 0.8f;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save as prefab
            string prefabPath = $"{PrefabsPath}/GolfBall.prefab";
            PrefabUtility.SaveAsPrefabAsset(ballGo, prefabPath);
            Object.DestroyImmediate(ballGo);

            Debug.Log($"GolfBallPrefabGenerator: Created golf ball prefab at {prefabPath}");
        }

        /// <summary>
        /// Utility to create or get the Ball layer.
        /// </summary>
        [MenuItem("OpenRange/Create Ball Layer", priority = 160)]
        public static void CreateBallLayer()
        {
            // Check if layer already exists
            int existingLayer = LayerMask.NameToLayer("Ball");
            if (existingLayer != -1)
            {
                Debug.Log($"GolfBallPrefabGenerator: Ball layer already exists at index {existingLayer}");
                return;
            }

            // Find first empty user layer (layers 8-31 are user layers)
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset"));
            SerializedProperty layers = tagManager.FindProperty("layers");

            for (int i = 8; i < 32; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = "Ball";
                    tagManager.ApplyModifiedProperties();
                    Debug.Log($"GolfBallPrefabGenerator: Created Ball layer at index {i}");
                    return;
                }
            }

            Debug.LogWarning("GolfBallPrefabGenerator: No empty layer slots available for Ball layer");
        }
    }
}
