// ABOUTME: Unit tests for OrbitCamera user-controlled orbit functionality.
// ABOUTME: Tests orbit parameters, zoom limits, rotation limits, and ground avoidance.

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using OpenRange.Visualization;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class OrbitCameraTests
    {
        private GameObject _orbitCameraGo;
        private OrbitCamera _orbitCamera;
        private GameObject _cameraGo;
        private Transform _cameraTransform;

        [SetUp]
        public void SetUp()
        {
            _orbitCameraGo = new GameObject("OrbitCamera");
            _orbitCamera = _orbitCameraGo.AddComponent<OrbitCamera>();

            _cameraGo = new GameObject("Camera");
            _cameraTransform = _cameraGo.transform;
        }

        [TearDown]
        public void TearDown()
        {
            if (_orbitCameraGo != null)
            {
                UnityEngine.Object.DestroyImmediate(_orbitCameraGo);
            }
            if (_cameraGo != null)
            {
                UnityEngine.Object.DestroyImmediate(_cameraGo);
            }
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        // ============================================
        // Mode Type Tests
        // ============================================

        [Test]
        public void ModeType_ReturnsFreeOrbit()
        {
            Assert.AreEqual(CameraMode.FreeOrbit, _orbitCamera.ModeType);
        }

        // ============================================
        // Initial State Tests
        // ============================================

        [Test]
        public void InitialState_IsNotActive()
        {
            Assert.IsFalse(_orbitCamera.IsActive);
        }

        [Test]
        public void InitialState_DefaultOrbitCenter()
        {
            Assert.AreEqual(new Vector3(0f, 0f, 125f), _orbitCamera.OrbitCenter);
        }

        [Test]
        public void InitialState_DefaultMinDistance()
        {
            Assert.AreEqual(10f, _orbitCamera.MinDistance);
        }

        [Test]
        public void InitialState_DefaultMaxDistance()
        {
            Assert.AreEqual(200f, _orbitCamera.MaxDistance);
        }

        // ============================================
        // Enter/Exit Tests
        // ============================================

        [Test]
        public void Enter_SetsActiveTrue()
        {
            _orbitCamera.Enter(null, _cameraTransform);

            Assert.IsTrue(_orbitCamera.IsActive);
        }

        [Test]
        public void Exit_SetsActiveFalse()
        {
            _orbitCamera.Enter(null, _cameraTransform);

            _orbitCamera.Exit();

            Assert.IsFalse(_orbitCamera.IsActive);
        }

        [Test]
        public void Enter_CalculatesInitialAngles()
        {
            _cameraTransform.position = new Vector3(50f, 30f, 125f);
            _orbitCamera.OrbitCenter = new Vector3(0f, 0f, 125f);

            _orbitCamera.Enter(null, _cameraTransform);

            // Should have calculated yaw (90 degrees since camera is at x=50)
            Assert.AreEqual(90f, _orbitCamera.CurrentYaw, 1f);
        }

        // ============================================
        // Property Tests
        // ============================================

        [Test]
        public void OrbitCenter_CanBeSet()
        {
            Vector3 newCenter = new Vector3(10f, 5f, 100f);

            _orbitCamera.OrbitCenter = newCenter;

            Assert.AreEqual(newCenter, _orbitCamera.OrbitCenter);
        }

        [Test]
        public void MinDistance_CanBeSet()
        {
            _orbitCamera.MinDistance = 20f;

            Assert.AreEqual(20f, _orbitCamera.MinDistance);
        }

        [Test]
        public void MinDistance_ClampedToMinimum()
        {
            _orbitCamera.MinDistance = 0f;

            Assert.AreEqual(1f, _orbitCamera.MinDistance);
        }

        [Test]
        public void MaxDistance_CanBeSet()
        {
            _orbitCamera.MaxDistance = 300f;

            Assert.AreEqual(300f, _orbitCamera.MaxDistance);
        }

        [Test]
        public void MaxDistance_ClampedToMinDistance()
        {
            _orbitCamera.MinDistance = 50f;
            _orbitCamera.MaxDistance = 30f;

            Assert.AreEqual(51f, _orbitCamera.MaxDistance);
        }

        // ============================================
        // Orbit Parameter Tests
        // ============================================

        [Test]
        public void SetOrbitParameters_SetsYaw()
        {
            _orbitCamera.Enter(null, _cameraTransform);

            _orbitCamera.SetOrbitParameters(45f, 30f, 50f, instant: true);

            Assert.AreEqual(45f, _orbitCamera.CurrentYaw, 0.01f);
        }

        [Test]
        public void SetOrbitParameters_SetsPitch()
        {
            _orbitCamera.Enter(null, _cameraTransform);

            _orbitCamera.SetOrbitParameters(0f, 45f, 50f, instant: true);

            Assert.AreEqual(45f, _orbitCamera.CurrentPitch, 0.01f);
        }

        [Test]
        public void SetOrbitParameters_SetsDistance()
        {
            _orbitCamera.Enter(null, _cameraTransform);

            _orbitCamera.SetOrbitParameters(0f, 30f, 75f, instant: true);

            Assert.AreEqual(75f, _orbitCamera.CurrentDistance, 0.01f);
        }

        [Test]
        public void SetOrbitParameters_ClampsPitch()
        {
            SetPrivateField(_orbitCamera, "_minPitch", 5f);
            SetPrivateField(_orbitCamera, "_maxPitch", 85f);
            _orbitCamera.Enter(null, _cameraTransform);

            _orbitCamera.SetOrbitParameters(0f, 100f, 50f, instant: true);

            Assert.AreEqual(85f, _orbitCamera.CurrentPitch, 0.01f);
        }

        [Test]
        public void SetOrbitParameters_ClampsDistance()
        {
            _orbitCamera.MinDistance = 10f;
            _orbitCamera.MaxDistance = 100f;
            _orbitCamera.Enter(null, _cameraTransform);

            _orbitCamera.SetOrbitParameters(0f, 30f, 200f, instant: true);

            Assert.AreEqual(100f, _orbitCamera.CurrentDistance, 0.01f);
        }

        // ============================================
        // Reset Tests
        // ============================================

        [Test]
        public void ResetToDefault_ResetsYaw()
        {
            _orbitCamera.Enter(null, _cameraTransform);
            _orbitCamera.SetOrbitParameters(90f, 60f, 100f, instant: true);

            _orbitCamera.ResetToDefault();
            // Simulate update to apply
            for (int i = 0; i < 100; i++)
            {
                _orbitCamera.UpdateCamera(0.1f);
            }

            Assert.AreEqual(0f, _orbitCamera.CurrentYaw, 1f);
        }

        [Test]
        public void ResetToDefault_ResetsPitchToDefault()
        {
            _orbitCamera.Enter(null, _cameraTransform);
            _orbitCamera.SetOrbitParameters(0f, 80f, 100f, instant: true);

            _orbitCamera.ResetToDefault();
            for (int i = 0; i < 100; i++)
            {
                _orbitCamera.UpdateCamera(0.1f);
            }

            Assert.AreEqual(30f, _orbitCamera.CurrentPitch, 1f);
        }

        // ============================================
        // UpdateCamera Tests
        // ============================================

        [Test]
        public void UpdateCamera_WhenNotActive_DoesNothing()
        {
            _cameraTransform.position = new Vector3(100f, 100f, 100f);
            Vector3 originalPos = _cameraTransform.position;

            _orbitCamera.UpdateCamera(0.1f);

            Assert.AreEqual(originalPos, _cameraTransform.position);
        }

        [Test]
        public void UpdateCamera_PositionsCamera()
        {
            _cameraTransform.position = Vector3.zero;
            _orbitCamera.OrbitCenter = new Vector3(0f, 0f, 50f);
            _orbitCamera.Enter(null, _cameraTransform);
            _orbitCamera.SetOrbitParameters(0f, 30f, 50f, instant: true);

            _orbitCamera.UpdateCamera(0.1f);

            // Camera should be positioned on orbit sphere
            Vector3 toCamera = _cameraTransform.position - _orbitCamera.OrbitCenter;
            Assert.AreEqual(50f, toCamera.magnitude, 0.5f);
        }

        [Test]
        public void UpdateCamera_CameraLooksAtCenter()
        {
            _orbitCamera.OrbitCenter = new Vector3(0f, 0f, 50f);
            _orbitCamera.Enter(null, _cameraTransform);
            _orbitCamera.SetOrbitParameters(0f, 30f, 50f, instant: true);

            _orbitCamera.UpdateCamera(0.1f);

            // Camera forward should point toward center
            Vector3 toCenter = (_orbitCamera.OrbitCenter - _cameraTransform.position).normalized;
            Assert.AreEqual(toCenter.x, _cameraTransform.forward.x, 0.1f);
            Assert.AreEqual(toCenter.y, _cameraTransform.forward.y, 0.1f);
            Assert.AreEqual(toCenter.z, _cameraTransform.forward.z, 0.1f);
        }

        // ============================================
        // Ground Avoidance Tests
        // ============================================

        [Test]
        public void UpdateCamera_EnforcesGroundClearance()
        {
            SetPrivateField(_orbitCamera, "_groundClearance", 5f);
            _orbitCamera.OrbitCenter = new Vector3(0f, 0f, 50f);
            _orbitCamera.Enter(null, _cameraTransform);

            // Set pitch to look from below (negative y)
            _orbitCamera.SetOrbitParameters(0f, -10f, 50f, instant: true);
            _orbitCamera.UpdateCamera(0.1f);

            // Camera should not go below ground clearance
            Assert.GreaterOrEqual(_cameraTransform.position.y, 5f);
        }

        // ============================================
        // Target Tests
        // ============================================

        [Test]
        public void SetTarget_UsesTargetAsOrbitCenter()
        {
            var targetGo = new GameObject("Target");
            targetGo.transform.position = new Vector3(10f, 5f, 100f);

            _orbitCamera.SetTarget(targetGo.transform);
            _orbitCamera.Enter(null, _cameraTransform);
            _orbitCamera.SetOrbitParameters(0f, 30f, 50f, instant: true);
            _orbitCamera.UpdateCamera(0.1f);

            // Camera should orbit around target, not default center
            Vector3 toCamera = _cameraTransform.position - targetGo.transform.position;
            Assert.AreEqual(50f, toCamera.magnitude, 0.5f);

            UnityEngine.Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void GetTargetPosition_ReturnsOrbitPosition()
        {
            _orbitCamera.OrbitCenter = new Vector3(0f, 0f, 50f);
            _orbitCamera.Enter(null, _cameraTransform);
            _orbitCamera.SetOrbitParameters(0f, 30f, 50f, instant: true);

            Vector3 pos = _orbitCamera.GetTargetPosition();

            // Should be on orbit sphere
            Vector3 toPos = pos - _orbitCamera.OrbitCenter;
            Assert.AreEqual(50f, toPos.magnitude, 0.5f);
        }

        // ============================================
        // ProcessInput Tests
        // ============================================

        [Test]
        public void ProcessInput_WhenNotActive_DoesNothing()
        {
            Assert.DoesNotThrow(() => _orbitCamera.ProcessInput());
        }

        [Test]
        public void ProcessInput_WhenActive_DoesNotThrow()
        {
            _orbitCamera.Enter(null, _cameraTransform);

            Assert.DoesNotThrow(() => _orbitCamera.ProcessInput());
        }
    }
}
