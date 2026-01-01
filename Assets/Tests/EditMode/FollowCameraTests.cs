// ABOUTME: Unit tests for FollowCamera ball tracking functionality.
// ABOUTME: Tests target following, damping, height tracking, and ground avoidance.

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using OpenRange.Visualization;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class FollowCameraTests
    {
        private GameObject _followCameraGo;
        private FollowCamera _followCamera;
        private GameObject _cameraGo;
        private Transform _cameraTransform;
        private GameObject _targetGo;
        private Transform _target;

        [SetUp]
        public void SetUp()
        {
            _followCameraGo = new GameObject("FollowCamera");
            _followCamera = _followCameraGo.AddComponent<FollowCamera>();

            _cameraGo = new GameObject("Camera");
            _cameraTransform = _cameraGo.transform;

            _targetGo = new GameObject("Target");
            _target = _targetGo.transform;
        }

        [TearDown]
        public void TearDown()
        {
            if (_followCameraGo != null)
            {
                UnityEngine.Object.DestroyImmediate(_followCameraGo);
            }
            if (_cameraGo != null)
            {
                UnityEngine.Object.DestroyImmediate(_cameraGo);
            }
            if (_targetGo != null)
            {
                UnityEngine.Object.DestroyImmediate(_targetGo);
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
        public void ModeType_ReturnsFollow()
        {
            Assert.AreEqual(CameraMode.Follow, _followCamera.ModeType);
        }

        // ============================================
        // Initial State Tests
        // ============================================

        [Test]
        public void InitialState_IsNotActive()
        {
            Assert.IsFalse(_followCamera.IsActive);
        }

        [Test]
        public void InitialState_DefaultOffset()
        {
            Assert.AreEqual(new Vector3(0f, 5f, -15f), _followCamera.Offset);
        }

        [Test]
        public void InitialState_DefaultDamping()
        {
            Assert.AreEqual(5f, _followCamera.FollowDamping);
        }

        // ============================================
        // Enter/Exit Tests
        // ============================================

        [Test]
        public void Enter_SetsActiveTrue()
        {
            _followCamera.Enter(null, _cameraTransform);

            Assert.IsTrue(_followCamera.IsActive);
        }

        [Test]
        public void Exit_SetsActiveFalse()
        {
            _followCamera.Enter(null, _cameraTransform);

            _followCamera.Exit();

            Assert.IsFalse(_followCamera.IsActive);
        }

        // ============================================
        // Target Tests
        // ============================================

        [Test]
        public void SetTarget_SetsTargetTransform()
        {
            _followCamera.SetTarget(_target);
            _followCamera.Enter(null, _cameraTransform);

            // GetTargetPosition should now work
            Vector3 pos = _followCamera.GetTargetPosition();
            Assert.AreNotEqual(Vector3.zero, pos);
        }

        [Test]
        public void GetTargetPosition_WithNoTarget_ReturnsCameraPosition()
        {
            _followCamera.Enter(null, _cameraTransform);
            _cameraTransform.position = new Vector3(10f, 20f, 30f);

            Vector3 pos = _followCamera.GetTargetPosition();

            Assert.AreEqual(_cameraTransform.position, pos);
        }

        [Test]
        public void GetTargetPosition_WithTarget_ReturnsCalculatedPosition()
        {
            _followCamera.SetTarget(_target);
            _target.position = new Vector3(0f, 0f, 100f);
            _followCamera.Enter(null, _cameraTransform);

            Vector3 pos = _followCamera.GetTargetPosition();

            // Should be behind and above target
            Assert.Greater(pos.y, _target.position.y);
            Assert.Less(pos.z, _target.position.z);
        }

        // ============================================
        // Property Tests
        // ============================================

        [Test]
        public void Offset_CanBeSet()
        {
            Vector3 newOffset = new Vector3(5f, 10f, -20f);

            _followCamera.Offset = newOffset;

            Assert.AreEqual(newOffset, _followCamera.Offset);
        }

        [Test]
        public void FollowDamping_CanBeSet()
        {
            _followCamera.FollowDamping = 10f;

            Assert.AreEqual(10f, _followCamera.FollowDamping);
        }

        [Test]
        public void FollowDamping_ClampedToMinimum()
        {
            _followCamera.FollowDamping = 0f;

            Assert.AreEqual(0.1f, _followCamera.FollowDamping);
        }

        [Test]
        public void LookDamping_CanBeSet()
        {
            _followCamera.LookDamping = 12f;

            Assert.AreEqual(12f, _followCamera.LookDamping);
        }

        [Test]
        public void LookDamping_ClampedToMinimum()
        {
            _followCamera.LookDamping = -5f;

            Assert.AreEqual(0.1f, _followCamera.LookDamping);
        }

        [Test]
        public void MinHeight_CanBeSet()
        {
            _followCamera.MinHeight = 5f;

            Assert.AreEqual(5f, _followCamera.MinHeight);
        }

        [Test]
        public void MinHeight_ClampedToZero()
        {
            _followCamera.MinHeight = -10f;

            Assert.AreEqual(0f, _followCamera.MinHeight);
        }

        [Test]
        public void LookAheadDistance_CanBeSet()
        {
            _followCamera.LookAheadDistance = 20f;

            Assert.AreEqual(20f, _followCamera.LookAheadDistance);
        }

        // ============================================
        // Update Camera Tests
        // ============================================

        [Test]
        public void UpdateCamera_WhenNotActive_DoesNothing()
        {
            _cameraTransform.position = new Vector3(100f, 100f, 100f);
            Vector3 originalPos = _cameraTransform.position;

            _followCamera.UpdateCamera(0.1f);

            Assert.AreEqual(originalPos, _cameraTransform.position);
        }

        [Test]
        public void UpdateCamera_WithNoTarget_DoesNothing()
        {
            _followCamera.Enter(null, _cameraTransform);
            _cameraTransform.position = new Vector3(100f, 100f, 100f);
            Vector3 originalPos = _cameraTransform.position;

            _followCamera.UpdateCamera(0.1f);

            Assert.AreEqual(originalPos, _cameraTransform.position);
        }

        [Test]
        public void UpdateCamera_MovesTowardTarget()
        {
            _followCamera.SetTarget(_target);
            _target.position = new Vector3(0f, 0f, 100f);
            _cameraTransform.position = new Vector3(0f, 0f, 0f);
            _followCamera.Enter(null, _cameraTransform);

            Vector3 startPos = _cameraTransform.position;
            _followCamera.UpdateCamera(0.1f);

            // Camera should have moved toward the target position
            Assert.AreNotEqual(startPos, _cameraTransform.position);
        }

        // ============================================
        // Height Tracking Tests
        // ============================================

        [Test]
        public void UpdateCamera_IncreasesHeightWithTarget()
        {
            _followCamera.SetTarget(_target);
            SetPrivateField(_followCamera, "_heightMultiplier", 0.5f);

            // Target at height 0
            _target.position = new Vector3(0f, 0f, 50f);
            _followCamera.Enter(null, _cameraTransform);
            Vector3 lowHeightPos = _followCamera.GetTargetPosition();

            // Target at height 20
            _target.position = new Vector3(0f, 20f, 50f);
            SetPrivateField(_followCamera, "_lastTargetPosition", _target.position);
            Vector3 highHeightPos = _followCamera.GetTargetPosition();

            // Camera should be higher when target is higher
            Assert.Greater(highHeightPos.y, lowHeightPos.y);
        }

        // ============================================
        // SnapToTarget Tests
        // ============================================

        [Test]
        public void SnapToTarget_MovesCameraImmediately()
        {
            _followCamera.SetTarget(_target);
            _target.position = new Vector3(0f, 0f, 100f);
            _cameraTransform.position = Vector3.zero;
            _followCamera.Enter(null, _cameraTransform);

            _followCamera.SnapToTarget();

            Vector3 expected = _followCamera.GetTargetPosition();
            Assert.AreEqual(expected.x, _cameraTransform.position.x, 0.01f);
            Assert.AreEqual(expected.y, _cameraTransform.position.y, 0.01f);
            Assert.AreEqual(expected.z, _cameraTransform.position.z, 0.01f);
        }

        [Test]
        public void SnapToTarget_WithNoTarget_DoesNothing()
        {
            _cameraTransform.position = new Vector3(10f, 20f, 30f);
            Vector3 originalPos = _cameraTransform.position;
            _followCamera.Enter(null, _cameraTransform);

            _followCamera.SnapToTarget();

            Assert.AreEqual(originalPos, _cameraTransform.position);
        }

        // ============================================
        // ProcessInput Tests
        // ============================================

        [Test]
        public void ProcessInput_DoesNotThrow()
        {
            _followCamera.Enter(null, _cameraTransform);

            Assert.DoesNotThrow(() => _followCamera.ProcessInput());
        }
    }
}
