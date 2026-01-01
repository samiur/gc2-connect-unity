// ABOUTME: Unit tests for CameraController camera mode management.
// ABOUTME: Tests mode switching, transitions, ball controller integration, and static/topdown modes.

using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using OpenRange.Visualization;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class CameraControllerTests
    {
        private GameObject _controllerGo;
        private CameraController _controller;
        private Camera _camera;
        private GameObject _cameraGo;

        [SetUp]
        public void SetUp()
        {
            _controllerGo = new GameObject("CameraController");
            _controller = _controllerGo.AddComponent<CameraController>();

            _cameraGo = new GameObject("Camera");
            _cameraGo.transform.SetParent(_controllerGo.transform);
            _camera = _cameraGo.AddComponent<Camera>();

            // Set camera reference via reflection
            SetPrivateField(_controller, "_camera", _camera);
            SetPrivateField(_controller, "_cameraTransform", _cameraGo.transform);
        }

        [TearDown]
        public void TearDown()
        {
            if (_controllerGo != null)
            {
                UnityEngine.Object.DestroyImmediate(_controllerGo);
            }
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        private T GetPrivateField<T>(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)field?.GetValue(obj);
        }

        // ============================================
        // Initial State Tests
        // ============================================

        [Test]
        public void InitialState_CameraIsSet()
        {
            Assert.IsNotNull(_controller.Camera);
            Assert.AreEqual(_camera, _controller.Camera);
        }

        [Test]
        public void InitialState_CameraTransformIsSet()
        {
            Assert.IsNotNull(_controller.CameraTransform);
            Assert.AreEqual(_cameraGo.transform, _controller.CameraTransform);
        }

        [Test]
        public void InitialState_AutoSwitchModesIsTrue()
        {
            Assert.IsTrue(_controller.AutoSwitchModes);
        }

        [Test]
        public void InitialState_IsNotTransitioning()
        {
            Assert.IsFalse(_controller.IsTransitioning);
        }

        // ============================================
        // Mode Switching Tests
        // ============================================

        [Test]
        public void SetMode_Static_SetsCurrentMode()
        {
            _controller.SetMode(CameraMode.Static, instant: true);

            Assert.AreEqual(CameraMode.Static, _controller.CurrentMode);
        }

        [Test]
        public void SetMode_TopDown_SetsCurrentMode()
        {
            _controller.SetMode(CameraMode.TopDown, instant: true);

            Assert.AreEqual(CameraMode.TopDown, _controller.CurrentMode);
        }

        [Test]
        public void SetMode_Instant_MovesCamera()
        {
            Vector3 staticPos = new Vector3(0f, 3f, -8f);
            _controller.StaticPosition = staticPos;

            _controller.SetMode(CameraMode.Static, instant: true);

            Assert.AreEqual(staticPos, _controller.CameraTransform.position);
        }

        [Test]
        public void SetMode_TopDownInstant_PositionsCameraOverhead()
        {
            float height = 100f;
            SetPrivateField(_controller, "_topDownHeight", height);

            _controller.SetMode(CameraMode.TopDown, instant: true);

            Assert.AreEqual(height, _controller.CameraTransform.position.y, 0.01f);
        }

        [Test]
        public void SetMode_TopDown_CameraLooksDown()
        {
            _controller.SetMode(CameraMode.TopDown, instant: true);

            // Camera should be looking straight down
            Vector3 forward = _controller.CameraTransform.forward;
            Assert.AreEqual(-1f, forward.y, 0.01f);
        }

        [Test]
        public void SetMode_SameMode_DoesNotTransition()
        {
            _controller.SetMode(CameraMode.Static, instant: true);
            bool eventFired = false;
            _controller.OnModeChanged += (mode) => eventFired = true;

            _controller.SetMode(CameraMode.Static); // Same mode

            Assert.IsFalse(eventFired);
        }

        [Test]
        public void SetMode_FiresOnModeChangedEvent()
        {
            CameraMode? firedMode = null;
            _controller.OnModeChanged += (mode) => firedMode = mode;

            _controller.SetMode(CameraMode.TopDown, instant: true);

            Assert.AreEqual(CameraMode.TopDown, firedMode);
        }

        // ============================================
        // Transition Tests
        // ============================================

        [Test]
        public void SetMode_WithDuration_StartsTransition()
        {
            _controller.TransitionDuration = 0.5f;
            _controller.SetMode(CameraMode.Static, instant: true);

            _controller.SetMode(CameraMode.TopDown, instant: false);

            Assert.IsTrue(_controller.IsTransitioning);
        }

        [Test]
        public void SetMode_ZeroDuration_DoesNotTransition()
        {
            _controller.TransitionDuration = 0f;
            _controller.SetMode(CameraMode.Static, instant: true);

            _controller.SetMode(CameraMode.TopDown, instant: false);

            Assert.IsFalse(_controller.IsTransitioning);
        }

        [Test]
        public void SetMode_Transition_FiresOnTransitionStarted()
        {
            CameraMode? fromMode = null;
            CameraMode? toMode = null;
            _controller.OnTransitionStarted += (from, to) =>
            {
                fromMode = from;
                toMode = to;
            };

            _controller.TransitionDuration = 0.5f;
            _controller.SetMode(CameraMode.Static, instant: true);
            _controller.SetMode(CameraMode.TopDown, instant: false);

            Assert.AreEqual(CameraMode.Static, fromMode);
            Assert.AreEqual(CameraMode.TopDown, toMode);
        }

        // ============================================
        // Static Position Tests
        // ============================================

        [Test]
        public void StaticPosition_CanBeSet()
        {
            Vector3 newPos = new Vector3(10f, 20f, -30f);
            _controller.StaticPosition = newPos;

            Assert.AreEqual(newPos, _controller.StaticPosition);
        }

        [Test]
        public void StaticLookAt_CanBeSet()
        {
            Vector3 newLookAt = new Vector3(0f, 0f, 100f);
            _controller.StaticLookAt = newLookAt;

            Assert.AreEqual(newLookAt, _controller.StaticLookAt);
        }

        [Test]
        public void SetStaticPosition_UpdatesCameraIfInStaticMode()
        {
            _controller.SetMode(CameraMode.Static, instant: true);
            Vector3 newPos = new Vector3(5f, 10f, -15f);
            Vector3 newLookAt = new Vector3(0f, 0f, 50f);

            _controller.SetStaticPosition(newPos, newLookAt);

            Assert.AreEqual(newPos, _controller.CameraTransform.position);
        }

        // ============================================
        // TopDown Height Tests
        // ============================================

        [Test]
        public void TopDownHeight_CanBeSet()
        {
            _controller.TopDownHeight = 150f;

            Assert.AreEqual(150f, _controller.TopDownHeight);
        }

        // ============================================
        // Transition Duration Tests
        // ============================================

        [Test]
        public void TransitionDuration_CanBeSet()
        {
            _controller.TransitionDuration = 1.5f;

            Assert.AreEqual(1.5f, _controller.TransitionDuration);
        }

        [Test]
        public void TransitionDuration_ClampedToMinimumZero()
        {
            _controller.TransitionDuration = -1f;

            Assert.AreEqual(0f, _controller.TransitionDuration);
        }

        // ============================================
        // AutoSwitchModes Tests
        // ============================================

        [Test]
        public void AutoSwitchModes_CanBeDisabled()
        {
            _controller.AutoSwitchModes = false;

            Assert.IsFalse(_controller.AutoSwitchModes);
        }

        // ============================================
        // SnapTo Tests
        // ============================================

        [Test]
        public void SnapTo_MovesCameraToPosition()
        {
            Vector3 position = new Vector3(10f, 20f, 30f);
            Vector3 lookAt = Vector3.zero;

            _controller.SnapTo(position, lookAt);

            Assert.AreEqual(position, _controller.CameraTransform.position);
        }

        [Test]
        public void SnapTo_CameraLooksAtTarget()
        {
            Vector3 position = new Vector3(0f, 10f, -10f);
            Vector3 lookAt = Vector3.zero;

            _controller.SnapTo(position, lookAt);

            Vector3 expectedForward = (lookAt - position).normalized;
            Assert.AreEqual(expectedForward.x, _controller.CameraTransform.forward.x, 0.01f);
            Assert.AreEqual(expectedForward.y, _controller.CameraTransform.forward.y, 0.01f);
            Assert.AreEqual(expectedForward.z, _controller.CameraTransform.forward.z, 0.01f);
        }

        [Test]
        public void SnapTo_StopsTransition()
        {
            _controller.TransitionDuration = 1f;
            _controller.SetMode(CameraMode.Static, instant: true);
            _controller.SetMode(CameraMode.TopDown, instant: false);
            Assert.IsTrue(_controller.IsTransitioning);

            _controller.SnapTo(Vector3.zero, Vector3.forward);

            Assert.IsFalse(_controller.IsTransitioning);
        }

        // ============================================
        // Ball Transform Tests
        // ============================================

        [Test]
        public void SetBallTransform_SetsReference()
        {
            var ballGo = new GameObject("Ball");

            _controller.SetBallTransform(ballGo.transform);

            Assert.AreEqual(ballGo.transform, _controller.BallTransform);

            UnityEngine.Object.DestroyImmediate(ballGo);
        }

        // ============================================
        // Mode Registration Tests
        // ============================================

        [Test]
        public void RegisterMode_AddsModeToDictionary()
        {
            var mockMode = _controllerGo.AddComponent<MockCameraMode>();

            _controller.RegisterMode(CameraMode.Follow, mockMode);

            // Verify by getting the mode
            _controller.SetMode(CameraMode.Follow, instant: true);
            var handler = _controller.GetCurrentModeHandler();
            Assert.AreEqual(mockMode, handler);
        }

        // ============================================
        // Camera Reference Tests
        // ============================================

        [Test]
        public void SetCamera_UpdatesCameraReference()
        {
            var newCameraGo = new GameObject("NewCamera");
            var newCamera = newCameraGo.AddComponent<Camera>();

            _controller.SetCamera(newCamera);

            Assert.AreEqual(newCamera, _controller.Camera);
            Assert.AreEqual(newCameraGo.transform, _controller.CameraTransform);

            UnityEngine.Object.DestroyImmediate(newCameraGo);
        }
    }

    /// <summary>
    /// Mock camera mode for testing registration.
    /// </summary>
    public class MockCameraMode : MonoBehaviour, ICameraMode
    {
        public CameraMode ModeType => CameraMode.Follow;
        public bool EnterCalled { get; private set; }
        public bool ExitCalled { get; private set; }
        public bool UpdateCalled { get; private set; }

        public void Enter(CameraController controller, Transform camera)
        {
            EnterCalled = true;
        }

        public void Exit()
        {
            ExitCalled = true;
        }

        public void UpdateCamera(float deltaTime)
        {
            UpdateCalled = true;
        }

        public void ProcessInput()
        {
        }

        public void SetTarget(Transform target)
        {
        }

        public Vector3 GetTargetPosition()
        {
            return Vector3.zero;
        }
    }
}
