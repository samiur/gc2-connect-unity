// ABOUTME: Editor tool for creating environment prefabs and materials.
// ABOUTME: Creates DistanceMarker, TargetGreen, TeeMat prefabs with proper configuration.

using UnityEditor;
using UnityEngine;
using OpenRange.Visualization;
using TMPro;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor utility to generate environment prefabs and materials.
    /// </summary>
    public static class EnvironmentGenerator
    {
        private const string MaterialsPath = "Assets/Materials/Environment";
        private const string PrefabsPath = "Assets/Prefabs/Environment";

        [MenuItem("OpenRange/Create All Environment Prefabs", priority = 140)]
        public static void CreateAllEnvironmentPrefabs()
        {
            EnsureDirectoriesExist();

            CreateEnvironmentMaterials();
            CreateDistanceMarkerPrefab();
            CreateTargetGreenPrefab();
            CreateTeeMatPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("EnvironmentGenerator: All environment prefabs created successfully!");
        }

        [MenuItem("OpenRange/Create Distance Marker Prefab", priority = 141)]
        public static void CreateDistanceMarkerPrefab()
        {
            EnsureDirectoriesExist();
            CreateEnvironmentMaterials();

            // Create root GameObject
            var markerGo = new GameObject("DistanceMarker");
            var marker = markerGo.AddComponent<DistanceMarker>();

            // Load materials
            var postMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/SignPost.mat");
            var signMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/SignFace.mat");

            // Create post (cylinder)
            var postGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            postGo.name = "Post";
            postGo.transform.SetParent(markerGo.transform);
            postGo.transform.localPosition = new Vector3(0f, 1f, 0f);
            postGo.transform.localScale = new Vector3(0.1f, 1f, 0.1f);

            // Remove collider
            var postCollider = postGo.GetComponent<Collider>();
            if (postCollider != null)
            {
                Object.DestroyImmediate(postCollider);
            }

            var postRenderer = postGo.GetComponent<MeshRenderer>();
            if (postMaterial != null)
            {
                postRenderer.sharedMaterial = postMaterial;
            }

            // Create sign (cube)
            var signGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signGo.name = "Sign";
            signGo.transform.SetParent(markerGo.transform);
            signGo.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            signGo.transform.localScale = new Vector3(0.8f, 0.5f, 0.05f);

            // Remove collider
            var signCollider = signGo.GetComponent<Collider>();
            if (signCollider != null)
            {
                Object.DestroyImmediate(signCollider);
            }

            var signRenderer = signGo.GetComponent<MeshRenderer>();
            if (signMaterial != null)
            {
                signRenderer.sharedMaterial = signMaterial;
            }

            // Create distance text (TextMeshPro)
            var textGo = new GameObject("DistanceText");
            textGo.transform.SetParent(markerGo.transform);
            textGo.transform.localPosition = new Vector3(0f, 2.2f, 0.03f);
            textGo.transform.localRotation = Quaternion.identity;

            var distanceText = textGo.AddComponent<TextMeshPro>();
            distanceText.text = "100";
            distanceText.fontSize = 3;
            distanceText.alignment = TextAlignmentOptions.Center;
            distanceText.color = new Color(0.1f, 0.3f, 0.1f); // Dark green
            distanceText.fontStyle = FontStyles.Bold;

            var textRect = distanceText.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(1f, 0.5f);

            // Wire up references using SerializedObject
            var so = new SerializedObject(marker);
            so.FindProperty("_postTransform").objectReferenceValue = postGo.transform;
            so.FindProperty("_signTransform").objectReferenceValue = signGo.transform;
            so.FindProperty("_distanceText").objectReferenceValue = distanceText;
            so.FindProperty("_signRenderer").objectReferenceValue = signRenderer;
            so.FindProperty("_postRenderer").objectReferenceValue = postRenderer;
            so.FindProperty("_distance").intValue = 100;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save prefab
            string prefabPath = $"{PrefabsPath}/DistanceMarker.prefab";
            PrefabUtility.SaveAsPrefabAsset(markerGo, prefabPath);
            Object.DestroyImmediate(markerGo);

            Debug.Log($"EnvironmentGenerator: Created DistanceMarker prefab at {prefabPath}");
        }

        [MenuItem("OpenRange/Create Target Green Prefab", priority = 142)]
        public static void CreateTargetGreenPrefab()
        {
            EnsureDirectoriesExist();
            CreateEnvironmentMaterials();

            // Create root GameObject
            var greenGo = new GameObject("TargetGreen");
            var green = greenGo.AddComponent<TargetGreen>();

            // Load materials
            var greenMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/Green.mat");
            var poleMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/SignPost.mat");
            var flagMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/Flag.mat");

            // Create green surface (cylinder, flattened)
            var surfaceGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            surfaceGo.name = "GreenSurface";
            surfaceGo.transform.SetParent(greenGo.transform);
            surfaceGo.transform.localPosition = Vector3.zero;
            surfaceGo.transform.localScale = new Vector3(15f, 0.05f, 15f); // Medium size by default

            // Remove collider
            var surfaceCollider = surfaceGo.GetComponent<Collider>();
            if (surfaceCollider != null)
            {
                Object.DestroyImmediate(surfaceCollider);
            }

            var surfaceRenderer = surfaceGo.GetComponent<MeshRenderer>();
            if (greenMaterial != null)
            {
                surfaceRenderer.sharedMaterial = greenMaterial;
            }

            // Create flag pole (cylinder)
            var poleGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            poleGo.name = "FlagPole";
            poleGo.transform.SetParent(greenGo.transform);
            poleGo.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            poleGo.transform.localScale = new Vector3(0.05f, 1.25f, 0.05f);

            // Remove collider
            var poleCollider = poleGo.GetComponent<Collider>();
            if (poleCollider != null)
            {
                Object.DestroyImmediate(poleCollider);
            }

            var poleRenderer = poleGo.GetComponent<MeshRenderer>();
            if (poleMaterial != null)
            {
                poleRenderer.sharedMaterial = poleMaterial;
            }

            // Create flag (cube, thin)
            var flagGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flagGo.name = "Flag";
            flagGo.transform.SetParent(poleGo.transform);
            flagGo.transform.localPosition = new Vector3(0.25f, 0.9f, 0f);
            flagGo.transform.localScale = new Vector3(10f, 6f, 0.2f); // Scaled relative to pole

            // Remove collider
            var flagCollider = flagGo.GetComponent<Collider>();
            if (flagCollider != null)
            {
                Object.DestroyImmediate(flagCollider);
            }

            var flagRenderer = flagGo.GetComponent<MeshRenderer>();
            if (flagMaterial != null)
            {
                flagRenderer.sharedMaterial = flagMaterial;
            }

            // Wire up references using SerializedObject
            var so = new SerializedObject(green);
            so.FindProperty("_greenSurface").objectReferenceValue = surfaceGo.transform;
            so.FindProperty("_flagPole").objectReferenceValue = poleGo.transform;
            so.FindProperty("_flag").objectReferenceValue = flagGo.transform;
            so.FindProperty("_greenRenderer").objectReferenceValue = surfaceRenderer;
            so.FindProperty("_poleRenderer").objectReferenceValue = poleRenderer;
            so.FindProperty("_flagRenderer").objectReferenceValue = flagRenderer;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save prefab
            string prefabPath = $"{PrefabsPath}/TargetGreen.prefab";
            PrefabUtility.SaveAsPrefabAsset(greenGo, prefabPath);
            Object.DestroyImmediate(greenGo);

            Debug.Log($"EnvironmentGenerator: Created TargetGreen prefab at {prefabPath}");
        }

        [MenuItem("OpenRange/Create Tee Mat Prefab", priority = 143)]
        public static void CreateTeeMatPrefab()
        {
            EnsureDirectoriesExist();
            CreateEnvironmentMaterials();

            // Create root GameObject
            var matGo = new GameObject("TeeMat");
            var mat = matGo.AddComponent<TeeMat>();

            // Load material
            var matMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/TeeMat.mat");

            // Create mat surface (cube)
            var surfaceGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            surfaceGo.name = "Mat";
            surfaceGo.transform.SetParent(matGo.transform);
            surfaceGo.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            surfaceGo.transform.localScale = new Vector3(2f, 0.02f, 3f);

            // Remove collider
            var surfaceCollider = surfaceGo.GetComponent<Collider>();
            if (surfaceCollider != null)
            {
                Object.DestroyImmediate(surfaceCollider);
            }

            var surfaceRenderer = surfaceGo.GetComponent<MeshRenderer>();
            if (matMaterial != null)
            {
                surfaceRenderer.sharedMaterial = matMaterial;
            }

            // Create spawn point
            var spawnGo = new GameObject("SpawnPoint");
            spawnGo.transform.SetParent(matGo.transform);
            spawnGo.transform.localPosition = new Vector3(0f, 0.02f, 0f);

            // Create boundaries (line renderer)
            var boundariesGo = new GameObject("Boundaries");
            boundariesGo.transform.SetParent(matGo.transform);
            boundariesGo.transform.localPosition = Vector3.zero;

            var lineRenderer = boundariesGo.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.startWidth = 0.02f;
            lineRenderer.endWidth = 0.02f;
            lineRenderer.positionCount = 4;
            lineRenderer.SetPositions(new Vector3[]
            {
                new Vector3(-1f, 0.025f, -1.5f),
                new Vector3(1f, 0.025f, -1.5f),
                new Vector3(1f, 0.025f, 1.5f),
                new Vector3(-1f, 0.025f, 1.5f)
            });

            // Use URP unlit material for line
            var lineMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (lineMaterial == null)
            {
                lineMaterial = new Material(Shader.Find("Unlit/Color"));
            }
            lineMaterial.color = new Color(1f, 1f, 1f, 0.5f);
            lineMaterial.name = "BoundaryLine";

            string lineMaterialPath = $"{MaterialsPath}/BoundaryLine.mat";
            var existingLineMat = AssetDatabase.LoadAssetAtPath<Material>(lineMaterialPath);
            if (existingLineMat == null)
            {
                AssetDatabase.CreateAsset(lineMaterial, lineMaterialPath);
                lineRenderer.sharedMaterial = lineMaterial;
            }
            else
            {
                lineRenderer.sharedMaterial = existingLineMat;
                Object.DestroyImmediate(lineMaterial);
            }

            // Wire up references using SerializedObject
            var so = new SerializedObject(mat);
            so.FindProperty("_matSurface").objectReferenceValue = surfaceGo.transform;
            so.FindProperty("_spawnPoint").objectReferenceValue = spawnGo.transform;
            so.FindProperty("_boundaries").objectReferenceValue = boundariesGo.transform;
            so.FindProperty("_matRenderer").objectReferenceValue = surfaceRenderer;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save prefab
            string prefabPath = $"{PrefabsPath}/TeeMat.prefab";
            PrefabUtility.SaveAsPrefabAsset(matGo, prefabPath);
            Object.DestroyImmediate(matGo);

            Debug.Log($"EnvironmentGenerator: Created TeeMat prefab at {prefabPath}");
        }

        [MenuItem("OpenRange/Create Environment Materials", priority = 144)]
        public static void CreateEnvironmentMaterials()
        {
            EnsureDirectoriesExist();

            CreateMaterial("FairwayGrass", new Color(0.29f, 0.49f, 0.35f), 0.2f, 0f);
            CreateMaterial("Green", new Color(0.18f, 0.35f, 0.15f), 0.3f, 0f);
            CreateMaterial("TeeMat", new Color(0.18f, 0.31f, 0.18f), 0.1f, 0f);
            CreateMaterial("SignPost", new Color(0.55f, 0.27f, 0.07f), 0.3f, 0f); // Wood brown
            CreateMaterial("SignFace", new Color(1f, 1f, 1f), 0.1f, 0f); // White
            CreateMaterial("Flag", new Color(1f, 0.1f, 0.1f), 0.2f, 0f); // Red
            CreateMaterial("Water", new Color(0.12f, 0.56f, 1f), 0.9f, 0f, 0.7f); // Blue with alpha

            Debug.Log("EnvironmentGenerator: All environment materials created");
        }

        /// <summary>
        /// Create a material with specified properties.
        /// </summary>
        private static Material CreateMaterial(string name, Color color, float smoothness, float metallic, float alpha = 1f)
        {
            string materialPath = $"{MaterialsPath}/{name}.mat";

            // Check if material already exists
            var existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existingMaterial != null)
            {
                return existingMaterial;
            }

            // Find URP Lit shader
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                Debug.LogError($"EnvironmentGenerator: No valid shader found for material {name}");
                return null;
            }

            var material = new Material(shader);
            material.name = name;

            // Set colors
            Color materialColor = color;
            materialColor.a = alpha;

            material.SetColor("_BaseColor", materialColor);
            material.SetColor("_Color", materialColor); // Fallback for Standard shader
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);

            // Handle transparency if needed
            if (alpha < 1f)
            {
                material.SetFloat("_Surface", 1f); // Transparent
                material.SetFloat("_Blend", 0f); // Alpha
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = 3000; // Transparent queue
            }

            AssetDatabase.CreateAsset(material, materialPath);
            return material;
        }

        /// <summary>
        /// Ensure all required directories exist.
        /// </summary>
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
        }
    }
}
