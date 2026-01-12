// ABOUTME: Unit tests for GrassRenderer GPU instanced grass component.
// ABOUTME: Tests initialization, quality tier settings, and public API.

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using OpenRange.Visualization;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class GrassRendererTests
    {
        private GameObject _testObject;
        private GrassRenderer _renderer;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestGrassRenderer");
            _renderer = _testObject.AddComponent<GrassRenderer>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
        }

        #region Initialization Tests

        [Test]
        public void GrassRenderer_InitialState_IsNotInitialized()
        {
            Assert.IsFalse(_renderer.IsInitialized);
        }

        [Test]
        public void GrassRenderer_InitialState_HasZeroInstances()
        {
            Assert.AreEqual(0, _renderer.InstanceCount);
        }

        [Test]
        public void GrassRenderer_GrassBladeMesh_IsNullByDefault()
        {
            Assert.IsNull(_renderer.GrassBladeMesh);
        }

        [Test]
        public void GrassRenderer_GrassMaterial_IsNullByDefault()
        {
            Assert.IsNull(_renderer.GrassMaterial);
        }

        #endregion

        #region Property Tests

        [Test]
        public void GrassRenderer_AreaSize_CanBeSet()
        {
            var newSize = new Vector2(30f, 60f);
            _renderer.AreaSize = newSize;
            Assert.AreEqual(newSize, _renderer.AreaSize);
        }

        [Test]
        public void GrassRenderer_BladesPerSquareMeter_CanBeSet()
        {
            _renderer.BladesPerSquareMeter = 50;
            Assert.AreEqual(50, _renderer.BladesPerSquareMeter);
        }

        [Test]
        public void GrassRenderer_BladesPerSquareMeter_IsClamped_Min()
        {
            _renderer.BladesPerSquareMeter = 0;
            Assert.AreEqual(1, _renderer.BladesPerSquareMeter);
        }

        [Test]
        public void GrassRenderer_BladesPerSquareMeter_IsClamped_Max()
        {
            _renderer.BladesPerSquareMeter = 1000;
            Assert.AreEqual(500, _renderer.BladesPerSquareMeter);
        }

        [Test]
        public void GrassRenderer_GrassBladeMesh_CanBeSet()
        {
            var mesh = new Mesh();
            _renderer.GrassBladeMesh = mesh;
            Assert.AreEqual(mesh, _renderer.GrassBladeMesh);
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void GrassRenderer_GrassMaterial_CanBeSet()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _renderer.GrassMaterial = material;
            Assert.AreEqual(material, _renderer.GrassMaterial);
            Object.DestroyImmediate(material);
        }

        #endregion

        #region SetArea Tests

        [Test]
        public void SetArea_UpdatesAreaSize()
        {
            var center = new Vector3(10f, 0f, 20f);
            var size = new Vector2(25f, 50f);
            _renderer.SetArea(center, size, 0f, 40f);

            Assert.AreEqual(size, _renderer.AreaSize);
        }

        #endregion

        #region SetDensity Tests

        [Test]
        public void SetDensity_UpdatesBladesPerSquareMeter()
        {
            _renderer.SetDensity(75);
            Assert.AreEqual(75, _renderer.BladesPerSquareMeter);
        }

        [Test]
        public void SetDensity_ClampsValue_TooLow()
        {
            _renderer.SetDensity(-10);
            Assert.AreEqual(1, _renderer.BladesPerSquareMeter);
        }

        [Test]
        public void SetDensity_ClampsValue_TooHigh()
        {
            _renderer.SetDensity(600);
            Assert.AreEqual(500, _renderer.BladesPerSquareMeter);
        }

        #endregion

        #region Component Tests

        [Test]
        public void GrassRenderer_IsMonoBehaviour()
        {
            Assert.IsInstanceOf<MonoBehaviour>(_renderer);
        }

        [Test]
        public void GrassRenderer_CanBeDestroyed()
        {
            Object.DestroyImmediate(_renderer);
            Assert.IsTrue(_renderer == null);
        }

        #endregion
    }
}
