// ABOUTME: Unit tests for WaterPlane component that registers with WaterController.
// ABOUTME: Tests registration lifecycle, enable/disable behavior, and renderer access.

using NUnit.Framework;
using UnityEngine;
using OpenRange.Visualization;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class WaterPlaneTests
    {
        private GameObject _controllerGo;
        private WaterController _controller;
        private GameObject _waterPlaneGo;
        private WaterPlane _waterPlane;

        [SetUp]
        public void SetUp()
        {
            // Clean up any existing instance
            if (WaterController.Instance != null)
            {
                Object.DestroyImmediate(WaterController.Instance.gameObject);
            }

            // Create controller first
            _controllerGo = new GameObject("WaterController");
            _controller = _controllerGo.AddComponent<WaterController>();
            _controller.ForceInitializeSingleton();

            // Create water plane
            _waterPlaneGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _waterPlaneGo.name = "TestWaterPlane";
            _waterPlane = _waterPlaneGo.AddComponent<WaterPlane>();

            // In EditMode tests, Awake() is not automatically called
            // Force initialization to set up the renderer reference
            _waterPlane.ForceInitialize();
        }

        [TearDown]
        public void TearDown()
        {
            if (_waterPlaneGo != null)
            {
                Object.DestroyImmediate(_waterPlaneGo);
            }

            if (_controllerGo != null)
            {
                Object.DestroyImmediate(_controllerGo);
            }
        }

        #region Initialization Tests

        [Test]
        public void Renderer_IsNotNull()
        {
            Assert.That(_waterPlane.Renderer, Is.Not.Null);
        }

        [Test]
        public void Renderer_IsMeshRenderer()
        {
            Assert.That(_waterPlane.Renderer, Is.TypeOf<MeshRenderer>());
        }

        [Test]
        public void IsRegistered_DefaultsToFalse()
        {
            // Before OnEnable is called
            var newGo = new GameObject("NewPlane");
            newGo.AddComponent<MeshRenderer>();
            var plane = newGo.AddComponent<WaterPlane>();

            Assert.That(plane.IsRegistered, Is.False);

            Object.DestroyImmediate(newGo);
        }

        #endregion

        #region Registration Tests

        [Test]
        public void RegisterWithController_SetsIsRegisteredTrue()
        {
            _waterPlane.RegisterWithController();

            Assert.That(_waterPlane.IsRegistered, Is.True);
        }

        [Test]
        public void RegisterWithController_WhenAlreadyRegistered_DoesNothing()
        {
            _waterPlane.RegisterWithController();
            Assert.That(_waterPlane.IsRegistered, Is.True);

            // Calling again should not throw or change state
            Assert.DoesNotThrow(() => _waterPlane.RegisterWithController());
            Assert.That(_waterPlane.IsRegistered, Is.True);
        }

        [Test]
        public void UnregisterFromController_SetsIsRegisteredFalse()
        {
            _waterPlane.RegisterWithController();
            Assert.That(_waterPlane.IsRegistered, Is.True);

            _waterPlane.UnregisterFromController();

            Assert.That(_waterPlane.IsRegistered, Is.False);
        }

        [Test]
        public void UnregisterFromController_WhenNotRegistered_DoesNothing()
        {
            Assert.That(_waterPlane.IsRegistered, Is.False);

            Assert.DoesNotThrow(() => _waterPlane.UnregisterFromController());
            Assert.That(_waterPlane.IsRegistered, Is.False);
        }

        [Test]
        public void ForceRegister_RegistersEvenIfAlreadyRegistered()
        {
            _waterPlane.RegisterWithController();
            Assert.That(_waterPlane.IsRegistered, Is.True);

            // ForceRegister resets and re-registers
            Assert.DoesNotThrow(() => _waterPlane.ForceRegister());
            Assert.That(_waterPlane.IsRegistered, Is.True);
        }

        #endregion

        #region Controller Interaction Tests

        [Test]
        public void RegisterWithController_WithoutController_DoesNotThrow()
        {
            // Destroy controller first
            Object.DestroyImmediate(_controllerGo);
            _controllerGo = null;

            Assert.DoesNotThrow(() => _waterPlane.RegisterWithController());
            Assert.That(_waterPlane.IsRegistered, Is.False);
        }

        [Test]
        public void UnregisterFromController_WithoutController_DoesNotThrow()
        {
            _waterPlane.RegisterWithController();

            // Destroy controller
            Object.DestroyImmediate(_controllerGo);
            _controllerGo = null;

            Assert.DoesNotThrow(() => _waterPlane.UnregisterFromController());
        }

        #endregion

        #region Lifecycle Tests

        [Test]
        public void OnDisable_WouldUnregister()
        {
            // Note: In EditMode tests, OnEnable/OnDisable are not automatically called
            // We test the behavior directly
            _waterPlane.RegisterWithController();
            Assert.That(_waterPlane.IsRegistered, Is.True);

            _waterPlane.UnregisterFromController(); // Simulates OnDisable
            Assert.That(_waterPlane.IsRegistered, Is.False);
        }

        [Test]
        public void OnEnable_WouldRegister()
        {
            // Simulates OnEnable behavior
            _waterPlane.RegisterWithController();
            Assert.That(_waterPlane.IsRegistered, Is.True);
        }

        #endregion
    }
}
