// ABOUTME: Editor tool for setting up the hybrid grass system with terrain textures and 3D grass blades.
// ABOUTME: Creates procedural grass blade meshes, terrain materials, and GrassRenderer prefabs.

using UnityEditor;
using UnityEngine;
using System.IO;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor utility to create grass system assets including meshes, materials, and prefabs.
    /// </summary>
    public static class GrassSystemSetup
    {
        private const string MeshPath = "Assets/Meshes/Environment";
        private const string MaterialsPath = "Assets/Materials/Terrain";
        private const string PrefabsPath = "Assets/Prefabs/Environment";
        private const string TexturesPath = "Assets/Textures/Terrain";
        private const string ShadersPath = "Assets/Shaders";

        #region Menu Items

        [MenuItem("OpenRange/Grass System/Create All Grass Assets", priority = 300)]
        public static void CreateAllGrassAssets()
        {
            EnsureDirectoriesExist();
            CreateGrassBladeMesh();
            CreateInstancedGrassMaterial();
            CreateTerrainMaterial();
            CreateGrassRendererPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("GrassSystemSetup: Created all grass system assets");
        }

        [MenuItem("OpenRange/Grass System/Create Grass Blade Mesh", priority = 301)]
        public static void CreateGrassBladeMeshMenuItem()
        {
            EnsureDirectoriesExist();
            CreateGrassBladeMesh();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("OpenRange/Grass System/Create Instanced Grass Material", priority = 302)]
        public static void CreateInstancedGrassMaterialMenuItem()
        {
            EnsureDirectoriesExist();
            CreateInstancedGrassMaterial();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("OpenRange/Grass System/Create Terrain Material", priority = 303)]
        public static void CreateTerrainMaterialMenuItem()
        {
            EnsureDirectoriesExist();
            CreateTerrainMaterial();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("OpenRange/Grass System/Create Grass Renderer Prefab", priority = 304)]
        public static void CreateGrassRendererPrefabMenuItem()
        {
            EnsureDirectoriesExist();
            CreateGrassRendererPrefab();
            AssetDatabase.SaveAssets();
        }

        #endregion

        #region Asset Creation

        /// <summary>
        /// Creates a procedural grass blade mesh.
        /// </summary>
        public static Mesh CreateGrassBladeMesh()
        {
            string meshPath = $"{MeshPath}/GrassBlade.asset";

            // Check if mesh exists
            var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existingMesh != null)
            {
                Debug.Log($"GrassSystemSetup: Grass blade mesh already exists at {meshPath}");
                return existingMesh;
            }

            // Create a simple grass blade mesh
            // Blade is made of 3 quads (6 triangles) for some curvature
            Mesh mesh = new Mesh();
            mesh.name = "GrassBlade";

            // Blade dimensions (will be scaled per-instance)
            float width = 1f;
            float height = 1f;
            int segments = 3;

            int vertCount = (segments + 1) * 2;
            Vector3[] vertices = new Vector3[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            Vector2[] uvs = new Vector2[vertCount];
            int[] triangles = new int[segments * 6];

            // Generate vertices along the blade
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float y = t * height;

                // Blade narrows towards tip
                float currentWidth = width * (1f - t * 0.7f);

                // Slight curve forward at top
                float z = t * t * 0.1f;

                int baseIdx = i * 2;
                vertices[baseIdx] = new Vector3(-currentWidth * 0.5f, y, z);
                vertices[baseIdx + 1] = new Vector3(currentWidth * 0.5f, y, z);

                // Normals point outward (will be adjusted per-instance)
                normals[baseIdx] = new Vector3(-0.3f, 0f, 1f).normalized;
                normals[baseIdx + 1] = new Vector3(0.3f, 0f, 1f).normalized;

                // UVs
                uvs[baseIdx] = new Vector2(0f, t);
                uvs[baseIdx + 1] = new Vector2(1f, t);
            }

            // Generate triangles
            for (int i = 0; i < segments; i++)
            {
                int baseIdx = i * 6;
                int vertBase = i * 2;

                // First triangle
                triangles[baseIdx] = vertBase;
                triangles[baseIdx + 1] = vertBase + 2;
                triangles[baseIdx + 2] = vertBase + 1;

                // Second triangle
                triangles[baseIdx + 3] = vertBase + 1;
                triangles[baseIdx + 4] = vertBase + 2;
                triangles[baseIdx + 5] = vertBase + 3;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            // Save mesh as asset
            AssetDatabase.CreateAsset(mesh, meshPath);
            Debug.Log($"GrassSystemSetup: Created grass blade mesh at {meshPath}");

            return mesh;
        }

        /// <summary>
        /// Creates the instanced grass material.
        /// </summary>
        public static Material CreateInstancedGrassMaterial()
        {
            string materialPath = $"{MaterialsPath}/InstancedGrass.mat";

            var existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existingMaterial != null)
            {
                Debug.Log($"GrassSystemSetup: Instanced grass material already exists at {materialPath}");
                return existingMaterial;
            }

            // Find shader
            Shader shader = Shader.Find("OpenRange/InstancedGrass");
            if (shader == null)
            {
                shader = AssetDatabase.LoadAssetAtPath<Shader>($"{ShadersPath}/Environment/InstancedGrass.shader");
            }

            if (shader == null)
            {
                Debug.LogError("GrassSystemSetup: Could not find InstancedGrass shader");
                return null;
            }

            Material material = new Material(shader);
            material.name = "InstancedGrass";

            // Set default colors (fairway grass)
            material.SetColor("_BaseColor", new Color(0.15f, 0.45f, 0.1f, 1f));
            material.SetColor("_TipColor", new Color(0.3f, 0.65f, 0.2f, 1f));
            material.SetFloat("_TipBlendStart", 0.3f);
            material.SetFloat("_TipBlendEnd", 0.9f);
            material.SetFloat("_WindStrength", 1f);
            material.SetFloat("_WindSpeed", 1.5f);
            material.SetFloat("_WindTurbulence", 0.4f);
            material.SetFloat("_Smoothness", 0.1f);

            // Enable GPU instancing
            material.enableInstancing = true;

            AssetDatabase.CreateAsset(material, materialPath);
            Debug.Log($"GrassSystemSetup: Created instanced grass material at {materialPath}");

            return material;
        }

        /// <summary>
        /// Creates the terrain material with texture splatting.
        /// </summary>
        public static Material CreateTerrainMaterial()
        {
            string materialPath = $"{MaterialsPath}/GolfTerrain.mat";

            var existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existingMaterial != null)
            {
                Debug.Log($"GrassSystemSetup: Terrain material already exists at {materialPath}");
                return existingMaterial;
            }

            // Find shader
            Shader shader = Shader.Find("OpenRange/GolfTerrain");
            if (shader == null)
            {
                shader = AssetDatabase.LoadAssetAtPath<Shader>($"{ShadersPath}/Terrain/GolfTerrain.shader");
            }

            if (shader == null)
            {
                Debug.LogError("GrassSystemSetup: Could not find GolfTerrain shader");
                return null;
            }

            Material material = new Material(shader);
            material.name = "GolfTerrain";

            // Load and assign textures
            AssignTerrainTextures(material);

            // Set default tiling
            material.SetFloat("_FairwayScale", 15f);
            material.SetFloat("_RoughScale", 10f);
            material.SetFloat("_GreenScale", 20f);
            material.SetFloat("_SandScale", 8f);

            // Set default tints (slight variations)
            material.SetColor("_FairwayColor", new Color(1f, 1f, 1f, 1f));
            material.SetColor("_RoughColor", new Color(0.95f, 1f, 0.95f, 1f));
            material.SetColor("_GreenColor", new Color(1f, 1.05f, 1f, 1f));
            material.SetColor("_SandColor", new Color(1f, 1f, 1f, 1f));

            material.SetFloat("_BlendSharpness", 2f);
            material.SetFloat("_NormalStrength", 1f);
            material.SetFloat("_DetailFadeStart", 50f);
            material.SetFloat("_DetailFadeEnd", 100f);

            AssetDatabase.CreateAsset(material, materialPath);
            Debug.Log($"GrassSystemSetup: Created terrain material at {materialPath}");

            return material;
        }

        private static void AssignTerrainTextures(Material material)
        {
            // Load OpenGolfSim textures
            var fairwayTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesPath}/gen_fairway_tex.png");
            var fairwayNormal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesPath}/gen_fairway_map.png");
            var roughTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesPath}/gen_rough_tex.png");
            var roughNormal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesPath}/gen_rough_map.png");
            var greenTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesPath}/gen_green_tex.png");
            var greenNormal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesPath}/gen_green_map.png");
            var sandTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesPath}/gen_sand_tex.png");
            var sandNormal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturesPath}/gen_sand_map.png");

            if (fairwayTex != null) material.SetTexture("_FairwayTex", fairwayTex);
            if (fairwayNormal != null) material.SetTexture("_FairwayNormal", fairwayNormal);
            if (roughTex != null) material.SetTexture("_RoughTex", roughTex);
            if (roughNormal != null) material.SetTexture("_RoughNormal", roughNormal);
            if (greenTex != null) material.SetTexture("_GreenTex", greenTex);
            if (greenNormal != null) material.SetTexture("_GreenNormal", greenNormal);
            if (sandTex != null) material.SetTexture("_SandTex", sandTex);
            if (sandNormal != null) material.SetTexture("_SandNormal", sandNormal);

            if (fairwayTex == null)
            {
                Debug.LogWarning("GrassSystemSetup: Fairway texture not found. Run texture download first.");
            }
        }

        /// <summary>
        /// Creates the GrassRenderer prefab.
        /// </summary>
        public static GameObject CreateGrassRendererPrefab()
        {
            string prefabPath = $"{PrefabsPath}/GrassRenderer.prefab";

            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
            {
                Debug.Log($"GrassSystemSetup: GrassRenderer prefab already exists at {prefabPath}");
                return existingPrefab;
            }

            // Create GameObject
            GameObject go = new GameObject("GrassRenderer");

            // Add GrassRenderer component
            var grassRenderer = go.AddComponent<Visualization.GrassRenderer>();

            // Configure via SerializedObject
            var so = new SerializedObject(grassRenderer);

            // Load mesh and material
            var bladeMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{MeshPath}/GrassBlade.asset");
            var grassMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsPath}/InstancedGrass.mat");

            if (bladeMesh != null)
            {
                so.FindProperty("_grassBladeMesh").objectReferenceValue = bladeMesh;
            }
            if (grassMaterial != null)
            {
                so.FindProperty("_grassMaterial").objectReferenceValue = grassMaterial;
            }

            // Set default area (near tee)
            so.FindProperty("_areaCenter").vector3Value = Vector3.zero;
            so.FindProperty("_areaSize").vector2Value = new Vector2(15f, 40f); // 15m wide, 40m long
            so.FindProperty("_nearDistance").floatValue = -5f; // Behind tee
            so.FindProperty("_farDistance").floatValue = 35f; // Forward

            // Set density
            so.FindProperty("_bladesPerSquareMeter").intValue = 80;
            so.FindProperty("_minBladeHeight").floatValue = 0.08f;
            so.FindProperty("_maxBladeHeight").floatValue = 0.2f;
            so.FindProperty("_minBladeWidth").floatValue = 0.015f;
            so.FindProperty("_maxBladeWidth").floatValue = 0.03f;

            so.ApplyModifiedPropertiesWithoutUndo();

            // Save prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            Debug.Log($"GrassSystemSetup: Created GrassRenderer prefab at {prefabPath}");
            return prefab;
        }

        #endregion

        #region Helpers

        private static void EnsureDirectoriesExist()
        {
            string[] directories = new[]
            {
                MeshPath,
                MaterialsPath,
                PrefabsPath,
                TexturesPath,
                $"{ShadersPath}/Terrain",
                $"{ShadersPath}/Environment"
            };

            foreach (var dir in directories)
            {
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    string[] parts = dir.Split('/');
                    string currentPath = parts[0];
                    for (int i = 1; i < parts.Length; i++)
                    {
                        string nextPath = currentPath + "/" + parts[i];
                        if (!AssetDatabase.IsValidFolder(nextPath))
                        {
                            AssetDatabase.CreateFolder(currentPath, parts[i]);
                        }
                        currentPath = nextPath;
                    }
                }
            }
        }

        #endregion
    }
}
