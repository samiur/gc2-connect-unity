// ABOUTME: Editor tool that generates the TrajectoryLine prefab and material.
// ABOUTME: Creates properly configured LineRenderer with gradient for trajectory visualization.

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor utility to generate the TrajectoryLine prefab and material.
    /// </summary>
    public static class TrajectoryLineGenerator
    {
        private const string MaterialsPath = "Assets/Materials/Effects";
        private const string PrefabsPath = "Assets/Prefabs/Effects";

        [MenuItem("OpenRange/Create Trajectory Line Prefab", priority = 155)]
        public static void CreateTrajectoryLinePrefab()
        {
            if (!EditorUtility.DisplayDialog(
                "Create Trajectory Line Prefab",
                "This will create the TrajectoryLine prefab and material. Existing files will be overwritten. Continue?",
                "Create",
                "Cancel"))
            {
                return;
            }

            EnsureDirectoriesExist();

            // Create material first
            var material = CreateTrajectoryLineMaterial();

            // Create prediction material
            var predictionMaterial = CreatePredictionLineMaterial();

            // Create main trajectory line prefab
            CreateTrajectoryLinePrefabAsset(material, predictionMaterial);

            AssetDatabase.Refresh();
            Debug.Log("TrajectoryLineGenerator: Trajectory line prefab and materials created successfully!");
        }

        [MenuItem("OpenRange/Create Trajectory Line Material Only", priority = 156)]
        public static void CreateTrajectoryLineMaterialOnly()
        {
            EnsureDirectoriesExist();
            CreateTrajectoryLineMaterial();
            CreatePredictionLineMaterial();
            AssetDatabase.Refresh();
            Debug.Log("TrajectoryLineGenerator: Trajectory line materials created");
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
        /// Creates the trajectory line material (for actual path).
        /// </summary>
        private static Material CreateTrajectoryLineMaterial()
        {
            // Use URP unlit shader for clean line rendering
            Shader lineShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (lineShader == null)
            {
                lineShader = Shader.Find("Particles/Standard Unlit");
            }
            if (lineShader == null)
            {
                // Fallback to unlit shader
                lineShader = Shader.Find("Unlit/Color");
            }

            var material = new Material(lineShader);
            material.name = "TrajectoryLine";

            // Set up for additive blending for a glowing effect
            material.SetColor("_BaseColor", new Color(1f, 1f, 1f, 1f));

            // Set rendering mode to additive
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f); // Transparent
            }
            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", 1f); // Additive
            }

            // Render queue for transparent objects
            material.renderQueue = (int)RenderQueue.Transparent;

            // Save material
            string materialPath = $"{MaterialsPath}/TrajectoryLine.mat";
            AssetDatabase.CreateAsset(material, materialPath);
            Debug.Log($"TrajectoryLineGenerator: Created trajectory material at {materialPath}");

            return material;
        }

        /// <summary>
        /// Creates the prediction line material (different color/style).
        /// </summary>
        private static Material CreatePredictionLineMaterial()
        {
            // Use URP unlit shader for clean line rendering
            Shader lineShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (lineShader == null)
            {
                lineShader = Shader.Find("Particles/Standard Unlit");
            }
            if (lineShader == null)
            {
                lineShader = Shader.Find("Unlit/Color");
            }

            var material = new Material(lineShader);
            material.name = "PredictionLine";

            // Yellow-orange color for prediction
            material.SetColor("_BaseColor", new Color(1f, 0.8f, 0.2f, 0.8f));

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
            material.renderQueue = (int)RenderQueue.Transparent - 1;

            // Save material
            string materialPath = $"{MaterialsPath}/PredictionLine.mat";
            AssetDatabase.CreateAsset(material, materialPath);
            Debug.Log($"TrajectoryLineGenerator: Created prediction material at {materialPath}");

            return material;
        }

        /// <summary>
        /// Creates the TrajectoryLine prefab with LineRenderer and TrajectoryRenderer.
        /// </summary>
        private static void CreateTrajectoryLinePrefabAsset(Material material, Material predictionMaterial)
        {
            // Create root object
            var lineGo = new GameObject("TrajectoryLine");

            // Add main LineRenderer for actual trajectory
            var lineRenderer = lineGo.AddComponent<LineRenderer>();
            ConfigureLineRenderer(lineRenderer, material, CreateActualGradient());

            // Create child for prediction line
            var predictionGo = new GameObject("PredictionLine");
            predictionGo.transform.SetParent(lineGo.transform);
            predictionGo.transform.localPosition = Vector3.zero;

            var predictionLineRenderer = predictionGo.AddComponent<LineRenderer>();
            ConfigureLineRenderer(predictionLineRenderer, predictionMaterial, CreatePredictionGradient());

            // Add TrajectoryRenderer component
            var trajectoryRenderer = lineGo.AddComponent<Visualization.TrajectoryRenderer>();

            // Wire up references using SerializedObject
            var so = new SerializedObject(trajectoryRenderer);
            so.FindProperty("_lineRenderer").objectReferenceValue = lineRenderer;
            so.FindProperty("_predictionLineRenderer").objectReferenceValue = predictionLineRenderer;
            so.FindProperty("_displayMode").enumValueIndex = 0; // Actual mode
            so.FindProperty("_lineWidthStart").floatValue = 0.05f;
            so.FindProperty("_lineWidthEnd").floatValue = 0.01f;
            so.FindProperty("_vertexCountHigh").intValue = 100;
            so.FindProperty("_vertexCountMedium").intValue = 50;
            so.FindProperty("_vertexCountLow").intValue = 20;
            so.FindProperty("_yardsToUnits").floatValue = 0.9144f;
            so.FindProperty("_feetToUnits").floatValue = 0.3048f;

            // Set up gradients
            SetSerializedGradient(so, "_actualColorGradient", CreateActualGradient());
            SetSerializedGradient(so, "_predictionColorGradient", CreatePredictionGradient());

            so.ApplyModifiedPropertiesWithoutUndo();

            // Save as prefab
            string prefabPath = $"{PrefabsPath}/TrajectoryLine.prefab";
            PrefabUtility.SaveAsPrefabAsset(lineGo, prefabPath);
            Object.DestroyImmediate(lineGo);

            Debug.Log($"TrajectoryLineGenerator: Created trajectory line prefab at {prefabPath}");
        }

        /// <summary>
        /// Configure a LineRenderer with standard settings.
        /// </summary>
        private static void ConfigureLineRenderer(LineRenderer lineRenderer, Material material, Gradient colorGradient)
        {
            // Width
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0.01f;

            // Material
            lineRenderer.material = material;

            // Color gradient
            lineRenderer.colorGradient = colorGradient;

            // Use world space
            lineRenderer.useWorldSpace = true;

            // Corner and cap vertices for smoothness
            lineRenderer.numCornerVertices = 5;
            lineRenderer.numCapVertices = 5;

            // No shadows for trajectory line
            lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;

            // Sorting order
            lineRenderer.sortingOrder = 1;

            // Initially disabled
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 0;
        }

        /// <summary>
        /// Create the default gradient for actual trajectory (white to cyan).
        /// </summary>
        private static Gradient CreateActualGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.5f, 1f, 1f), 0.5f),
                    new GradientColorKey(Color.cyan, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.8f),
                    new GradientAlphaKey(0.5f, 1f)
                }
            );
            return gradient;
        }

        /// <summary>
        /// Create the default gradient for prediction trajectory (yellow-orange).
        /// </summary>
        private static Gradient CreatePredictionGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.yellow, 0f),
                    new GradientColorKey(new Color(1f, 0.5f, 0f), 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.8f, 0f),
                    new GradientAlphaKey(0.3f, 1f)
                }
            );
            return gradient;
        }

        /// <summary>
        /// Helper to set a gradient on a SerializedProperty.
        /// </summary>
        private static void SetSerializedGradient(SerializedObject so, string propertyName, Gradient gradient)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null)
            {
                prop.gradientValue = gradient;
            }
        }
    }
}
