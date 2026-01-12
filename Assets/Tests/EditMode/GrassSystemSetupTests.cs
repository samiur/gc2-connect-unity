// ABOUTME: Unit tests for GrassSystemSetup editor tool that creates grass assets.
// ABOUTME: Tests procedural mesh generation and asset path constants.

using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class GrassSystemSetupTests
    {
        #region Asset Existence Tests

        [Test]
        public void GrassBladeMesh_Exists()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes/Environment/GrassBlade.asset");
            Assert.IsNotNull(mesh, "GrassBlade.asset should exist at Assets/Meshes/Environment/");
        }

        [Test]
        public void InstancedGrassMaterial_Exists()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain/InstancedGrass.mat");
            Assert.IsNotNull(material, "InstancedGrass.mat should exist at Assets/Materials/Terrain/");
        }

        [Test]
        public void GolfTerrainMaterial_Exists()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain/GolfTerrain.mat");
            Assert.IsNotNull(material, "GolfTerrain.mat should exist at Assets/Materials/Terrain/");
        }

        [Test]
        public void GrassRendererPrefab_Exists()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/GrassRenderer.prefab");
            Assert.IsNotNull(prefab, "GrassRenderer.prefab should exist at Assets/Prefabs/Environment/");
        }

        #endregion

        #region Grass Blade Mesh Tests

        [Test]
        public void GrassBladeMesh_HasVertices()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes/Environment/GrassBlade.asset");
            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.vertexCount, 0, "Grass blade mesh should have vertices");
        }

        [Test]
        public void GrassBladeMesh_HasTriangles()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes/Environment/GrassBlade.asset");
            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.triangles.Length, 0, "Grass blade mesh should have triangles");
        }

        [Test]
        public void GrassBladeMesh_HasNormals()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes/Environment/GrassBlade.asset");
            Assert.IsNotNull(mesh);
            Assert.AreEqual(mesh.vertexCount, mesh.normals.Length, "All vertices should have normals");
        }

        [Test]
        public void GrassBladeMesh_HasUVs()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes/Environment/GrassBlade.asset");
            Assert.IsNotNull(mesh);
            Assert.AreEqual(mesh.vertexCount, mesh.uv.Length, "All vertices should have UVs");
        }

        [Test]
        public void GrassBladeMesh_HasCorrectName()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Meshes/Environment/GrassBlade.asset");
            Assert.IsNotNull(mesh);
            Assert.AreEqual("GrassBlade", mesh.name);
        }

        #endregion

        #region Instanced Grass Material Tests

        [Test]
        public void InstancedGrassMaterial_HasGPUInstancingEnabled()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain/InstancedGrass.mat");
            Assert.IsNotNull(material);
            Assert.IsTrue(material.enableInstancing, "Instanced grass material should have GPU instancing enabled");
        }

        [Test]
        public void InstancedGrassMaterial_HasShader()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain/InstancedGrass.mat");
            Assert.IsNotNull(material);
            Assert.IsNotNull(material.shader, "Material should have a shader assigned");
        }

        [Test]
        public void InstancedGrassMaterial_HasBaseColor()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain/InstancedGrass.mat");
            Assert.IsNotNull(material);
            Assert.IsTrue(material.HasProperty("_BaseColor"), "Material should have _BaseColor property");
        }

        [Test]
        public void InstancedGrassMaterial_HasTipColor()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain/InstancedGrass.mat");
            Assert.IsNotNull(material);
            Assert.IsTrue(material.HasProperty("_TipColor"), "Material should have _TipColor property");
        }

        [Test]
        public void InstancedGrassMaterial_HasWindStrength()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain/InstancedGrass.mat");
            Assert.IsNotNull(material);
            Assert.IsTrue(material.HasProperty("_WindStrength"), "Material should have _WindStrength property");
        }

        #endregion

        #region GrassRenderer Prefab Tests

        [Test]
        public void GrassRendererPrefab_HasGrassRendererComponent()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/GrassRenderer.prefab");
            Assert.IsNotNull(prefab);

            var grassRenderer = prefab.GetComponent<OpenRange.Visualization.GrassRenderer>();
            Assert.IsNotNull(grassRenderer, "GrassRenderer prefab should have GrassRenderer component");
        }

        [Test]
        public void GrassRendererPrefab_HasGrassBladeMeshAssigned()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/GrassRenderer.prefab");
            Assert.IsNotNull(prefab);

            var grassRenderer = prefab.GetComponent<OpenRange.Visualization.GrassRenderer>();
            Assert.IsNotNull(grassRenderer);
            Assert.IsNotNull(grassRenderer.GrassBladeMesh, "GrassRenderer should have grass blade mesh assigned");
        }

        [Test]
        public void GrassRendererPrefab_HasGrassMaterialAssigned()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/GrassRenderer.prefab");
            Assert.IsNotNull(prefab);

            var grassRenderer = prefab.GetComponent<OpenRange.Visualization.GrassRenderer>();
            Assert.IsNotNull(grassRenderer);
            Assert.IsNotNull(grassRenderer.GrassMaterial, "GrassRenderer should have grass material assigned");
        }

        #endregion

        #region Golf Terrain Material Tests

        [Test]
        public void GolfTerrainMaterial_HasShader()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain/GolfTerrain.mat");
            Assert.IsNotNull(material);
            Assert.IsNotNull(material.shader, "Material should have a shader assigned");
        }

        [Test]
        public void GolfTerrainMaterial_HasFairwayScale()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain/GolfTerrain.mat");
            Assert.IsNotNull(material);
            Assert.IsTrue(material.HasProperty("_FairwayScale"), "Material should have _FairwayScale property");
        }

        [Test]
        public void GolfTerrainMaterial_HasBlendSharpness()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Terrain/GolfTerrain.mat");
            Assert.IsNotNull(material);
            Assert.IsTrue(material.HasProperty("_BlendSharpness"), "Material should have _BlendSharpness property");
        }

        #endregion
    }
}
