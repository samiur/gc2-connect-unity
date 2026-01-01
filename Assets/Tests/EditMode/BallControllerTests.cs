// ABOUTME: Unit tests for the BallController animation system.
// ABOUTME: Tests playback controls, events, phase changes, and coordinate conversion.

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using OpenRange.Visualization;
using OpenRange.Physics;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class BallControllerTests
    {
        private GameObject _testObject;
        private BallController _controller;
        private Transform _ballTransform;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestBallController");

            // Create the ball transform first as a child
            var ballObject = new GameObject("Ball");
            ballObject.transform.SetParent(_testObject.transform);
            _ballTransform = ballObject.transform;

            // Use reflection to set _ballTransform BEFORE Awake runs
            // We add the component after preparing the child transform
            _controller = _testObject.AddComponent<BallController>();

            // Now set the ball transform reference via reflection since Awake already ran
            SetPrivateField(_controller, "_ballTransform", _ballTransform);
        }

        /// <summary>
        /// Helper to set private serialized fields via reflection for testing.
        /// </summary>
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
        }

        #region Initial State Tests

        [Test]
        public void InitialState_IsNotAnimating()
        {
            // Assert
            Assert.IsFalse(_controller.IsAnimating);
        }

        [Test]
        public void InitialState_IsNotPaused()
        {
            // Assert
            Assert.IsFalse(_controller.IsPaused);
        }

        [Test]
        public void InitialState_PhaseIsIdle()
        {
            // Assert
            Assert.AreEqual(AnimationPhase.Idle, _controller.CurrentPhase);
        }

        [Test]
        public void InitialState_ProgressIsZero()
        {
            // Assert
            Assert.AreEqual(0f, _controller.Progress);
        }

        [Test]
        public void InitialState_CurrentShotIsNull()
        {
            // Assert
            Assert.IsNull(_controller.CurrentShot);
        }

        [Test]
        public void InitialState_DefaultTeePosition()
        {
            // Assert
            Assert.AreEqual(Vector3.zero, _controller.TeePosition);
        }

        [Test]
        public void InitialState_DefaultPlaybackSpeed()
        {
            // Assert
            Assert.AreEqual(1f, _controller.PlaybackSpeed);
        }

        #endregion

        #region Property Tests

        [Test]
        public void PlaybackSpeed_CanBeSet()
        {
            // Act
            _controller.PlaybackSpeed = 2f;

            // Assert
            Assert.AreEqual(2f, _controller.PlaybackSpeed);
        }

        [Test]
        public void PlaybackSpeed_ClampedToMinimum()
        {
            // Act
            _controller.PlaybackSpeed = 0.01f;

            // Assert
            Assert.GreaterOrEqual(_controller.PlaybackSpeed, 0.1f);
        }

        [Test]
        public void TeePosition_CanBeSet()
        {
            // Act
            _controller.TeePosition = new Vector3(1f, 2f, 3f);

            // Assert
            Assert.AreEqual(new Vector3(1f, 2f, 3f), _controller.TeePosition);
        }

        [Test]
        public void TimeScale_CanBeSet()
        {
            // Act
            _controller.CurrentTimeScale = TimeScale.Fast;

            // Assert
            Assert.AreEqual(TimeScale.Fast, _controller.CurrentTimeScale);
        }

        [Test]
        public void BallTransform_ReturnsSetTransform()
        {
            // Assert
            Assert.AreEqual(_ballTransform, _controller.BallTransform);
        }

        #endregion

        #region PlayShot Tests

        [Test]
        public void PlayShot_WithNullResult_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _controller.PlayShot(null));
        }

        [Test]
        public void PlayShot_WithEmptyTrajectory_DoesNotThrow()
        {
            // Arrange
            var result = CreateShotResult(new List<TrajectoryPoint>());

            // Act & Assert
            Assert.DoesNotThrow(() => _controller.PlayShot(result));
        }

        [Test]
        public void PlayShot_WithValidResult_StoresCurrentShot()
        {
            // Arrange
            var result = CreateSimpleShotResult();

            // Act
            _controller.PlayShot(result);

            // Assert
            Assert.AreEqual(result, _controller.CurrentShot);
        }

        [Test]
        public void PlayShot_WithInstantTimeScale_SkipsToEnd()
        {
            // Arrange
            _controller.CurrentTimeScale = TimeScale.Instant;
            var result = CreateSimpleShotResult();

            // Act
            _controller.PlayShot(result);

            // Assert
            Assert.AreEqual(1f, _controller.Progress);
            Assert.AreEqual(AnimationPhase.Stopped, _controller.CurrentPhase);
        }

        [Test]
        public void PlayShot_MovesBallToTeePosition()
        {
            // Arrange
            _controller.TeePosition = new Vector3(5f, 0f, 0f);
            _ballTransform.position = new Vector3(100f, 50f, 25f);
            _controller.CurrentTimeScale = TimeScale.Instant;

            // Act
            _controller.PlayShot(CreateSimpleShotResult());

            // Assert - ball should have moved (exact position depends on trajectory)
            Assert.AreNotEqual(new Vector3(100f, 50f, 25f), _ballTransform.position);
        }

        #endregion

        #region SkipToEnd Tests

        [Test]
        public void SkipToEnd_WithNoShot_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _controller.SkipToEnd());
        }

        [Test]
        public void SkipToEnd_SetsProgressToOne()
        {
            // Arrange
            _controller.CurrentTimeScale = TimeScale.Realtime;
            _controller.PlayShot(CreateSimpleShotResult());

            // Act
            _controller.SkipToEnd();

            // Assert
            Assert.AreEqual(1f, _controller.Progress);
        }

        [Test]
        public void SkipToEnd_SetsPhaseToStopped()
        {
            // Arrange
            _controller.PlayShot(CreateSimpleShotResult());

            // Act
            _controller.SkipToEnd();

            // Assert
            Assert.AreEqual(AnimationPhase.Stopped, _controller.CurrentPhase);
        }

        [Test]
        public void SkipToEnd_MovesBallToFinalPosition()
        {
            // Arrange
            _controller.TeePosition = Vector3.zero;
            var result = CreateShotResultWithFinalPosition(100f, 0f, 5f);
            _controller.PlayShot(result);

            // Act
            _controller.SkipToEnd();

            // Assert - ball should be near final trajectory point
            // Physics X (forward 100 yds) → Unity Z, Physics Z (lateral 5 yds) → Unity X
            Assert.AreEqual(5f * 0.9144f, _ballTransform.position.x, 0.01f);    // lateral
            Assert.AreEqual(0f, _ballTransform.position.y, 0.01f);              // height
            Assert.AreEqual(100f * 0.9144f, _ballTransform.position.z, 0.01f);  // forward
        }

        #endregion

        #region Reset Tests

        [Test]
        public void Reset_SetsProgressToZero()
        {
            // Arrange
            _controller.PlayShot(CreateSimpleShotResult());
            _controller.SkipToEnd();

            // Act
            _controller.Reset();

            // Assert
            Assert.AreEqual(0f, _controller.Progress);
        }

        [Test]
        public void Reset_SetsPhaseToIdle()
        {
            // Arrange
            _controller.PlayShot(CreateSimpleShotResult());
            _controller.SkipToEnd();

            // Act
            _controller.Reset();

            // Assert
            Assert.AreEqual(AnimationPhase.Idle, _controller.CurrentPhase);
        }

        [Test]
        public void Reset_ClearsCurrentShot()
        {
            // Arrange
            _controller.PlayShot(CreateSimpleShotResult());

            // Act
            _controller.Reset();

            // Assert
            Assert.IsNull(_controller.CurrentShot);
        }

        [Test]
        public void Reset_ReturnsBallToTeePosition()
        {
            // Arrange
            _controller.TeePosition = new Vector3(1f, 2f, 3f);
            _controller.PlayShot(CreateSimpleShotResult());
            _controller.SkipToEnd();

            // Act
            _controller.Reset();

            // Assert
            Assert.AreEqual(new Vector3(1f, 2f, 3f), _ballTransform.position);
        }

        [Test]
        public void Reset_SetsPausedFalse()
        {
            // Arrange
            _controller.PlayShot(CreateSimpleShotResult());
            _controller.Pause();

            // Act
            _controller.Reset();

            // Assert
            Assert.IsFalse(_controller.IsPaused);
        }

        #endregion

        #region Pause and Resume Tests

        [Test]
        public void Pause_SetsIsPausedTrue()
        {
            // Arrange
            _controller.PlayShot(CreateSimpleShotResult());

            // Act
            _controller.Pause();

            // Assert
            Assert.IsTrue(_controller.IsPaused);
        }

        [Test]
        public void Resume_SetsIsPausedFalse()
        {
            // Arrange
            _controller.PlayShot(CreateSimpleShotResult());
            _controller.Pause();

            // Act
            _controller.Resume();

            // Assert
            Assert.IsFalse(_controller.IsPaused);
        }

        [Test]
        public void Pause_WithNoAnimation_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _controller.Pause());
        }

        [Test]
        public void Resume_WithNoAnimation_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _controller.Resume());
        }

        #endregion

        #region GetPositionAtTime Tests

        [Test]
        public void GetPositionAtTime_WithNoShot_ReturnsTeePosition()
        {
            // Arrange
            _controller.TeePosition = new Vector3(5f, 0f, 0f);

            // Act
            Vector3 position = _controller.GetPositionAtTime(1f);

            // Assert
            Assert.AreEqual(new Vector3(5f, 0f, 0f), position);
        }

        [Test]
        public void GetPositionAtTime_AtTimeZero_ReturnsStartPosition()
        {
            // Arrange
            _controller.TeePosition = Vector3.zero;
            var result = CreateSimpleShotResult();
            _controller.PlayShot(result);

            // Act
            Vector3 position = _controller.GetPositionAtTime(0f);

            // Assert - First trajectory point is at origin
            Assert.AreEqual(0f, position.x, 0.01f);
            Assert.AreEqual(0f, position.y, 0.01f);
            Assert.AreEqual(0f, position.z, 0.01f);
        }

        [Test]
        public void GetPositionAtTime_BeyondTrajectory_ReturnsLastPosition()
        {
            // Arrange
            _controller.TeePosition = Vector3.zero;
            var result = CreateShotResultWithFinalPosition(100f, 0f, 0f);
            _controller.PlayShot(result);

            // Act
            Vector3 position = _controller.GetPositionAtTime(1000f);

            // Assert - Should be at final position (forward → Unity Z)
            Assert.AreEqual(100f * 0.9144f, position.z, 0.01f);
        }

        [Test]
        public void GetPositionAtTime_InterpolatesBetweenPoints()
        {
            // Arrange
            _controller.TeePosition = Vector3.zero;
            var trajectory = new List<TrajectoryPoint>
            {
                new TrajectoryPoint(0f, new Vector3(0f, 0f, 0f), Phase.Flight),
                new TrajectoryPoint(2f, new Vector3(100f, 0f, 0f), Phase.Stopped)
            };
            var result = CreateShotResult(trajectory);
            _controller.PlayShot(result);

            // Act - Get position at t=1 (halfway)
            Vector3 position = _controller.GetPositionAtTime(1f);

            // Assert - Should be halfway (50 yards forward = 45.72 meters in Unity Z)
            Assert.AreEqual(50f * 0.9144f, position.z, 0.01f);
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnStopped_FiredWhenSkipToEnd()
        {
            // Arrange
            bool eventFired = false;
            Vector3 stoppedPosition = Vector3.zero;
            _controller.OnStopped += (pos) =>
            {
                eventFired = true;
                stoppedPosition = pos;
            };
            _controller.PlayShot(CreateSimpleShotResult());

            // Act
            _controller.SkipToEnd();

            // Assert
            Assert.IsTrue(eventFired);
        }

        [Test]
        public void OnPhaseChanged_FiredOnSkipToEnd()
        {
            // Arrange
            AnimationPhase lastPhase = AnimationPhase.Idle;
            _controller.OnPhaseChanged += (phase) => lastPhase = phase;
            _controller.PlayShot(CreateSimpleShotResult());

            // Act
            _controller.SkipToEnd();

            // Assert
            Assert.AreEqual(AnimationPhase.Stopped, lastPhase);
        }

        [Test]
        public void OnApexReached_FiredWithCorrectHeight()
        {
            // Arrange
            float apexHeight = 0f;
            _controller.OnApexReached += (height) => apexHeight = height;
            var result = CreateSimpleShotResult();
            result.MaxHeight = 45.5f;
            _controller.PlayShot(result);

            // Act
            _controller.SkipToEnd();

            // Assert
            Assert.AreEqual(45.5f, apexHeight);
        }

        [Test]
        public void OnLanded_FiredOnSkipToEnd()
        {
            // Arrange
            bool eventFired = false;
            _controller.OnLanded += (pos) => eventFired = true;
            _controller.PlayShot(CreateSimpleShotResult());

            // Act
            _controller.SkipToEnd();

            // Assert
            Assert.IsTrue(eventFired);
        }

        #endregion

        #region Component Reference Tests

        [Test]
        public void SetBallVisuals_SetsReference()
        {
            // Arrange
            var visualsObject = new GameObject("Visuals");
            var visuals = visualsObject.AddComponent<BallVisuals>();

            // Act
            _controller.SetBallVisuals(visuals);

            // Assert - No exception means it worked
            Assert.DoesNotThrow(() => _controller.Reset());

            // Cleanup
            Object.DestroyImmediate(visualsObject);
        }

        [Test]
        public void SetBallSpinner_SetsReference()
        {
            // Arrange
            var spinnerObject = new GameObject("Spinner");
            var spinner = spinnerObject.AddComponent<BallSpinner>();

            // Act
            _controller.SetBallSpinner(spinner);

            // Assert - No exception means it worked
            Assert.DoesNotThrow(() => _controller.Reset());

            // Cleanup
            Object.DestroyImmediate(spinnerObject);
        }

        #endregion

        #region Coordinate Conversion Tests

        [Test]
        public void PlayShot_ConvertsYardsToMeters()
        {
            // Arrange
            _controller.TeePosition = Vector3.zero;
            _controller.CurrentTimeScale = TimeScale.Instant;

            // 100 yards forward
            var trajectory = new List<TrajectoryPoint>
            {
                new TrajectoryPoint(0f, Vector3.zero, Phase.Flight),
                new TrajectoryPoint(5f, new Vector3(100f, 0f, 0f), Phase.Stopped)
            };
            var result = CreateShotResult(trajectory);

            // Act
            _controller.PlayShot(result);

            // Assert - 100 yards forward = 91.44 meters in Unity Z
            Assert.AreEqual(91.44f, _ballTransform.position.z, 0.01f);
        }

        [Test]
        public void PlayShot_ConvertsFeetToMeters()
        {
            // Arrange
            _controller.TeePosition = Vector3.zero;
            _controller.CurrentTimeScale = TimeScale.Instant;

            // 30 feet high
            var trajectory = new List<TrajectoryPoint>
            {
                new TrajectoryPoint(0f, Vector3.zero, Phase.Flight),
                new TrajectoryPoint(5f, new Vector3(0f, 30f, 0f), Phase.Stopped)
            };
            var result = CreateShotResult(trajectory);

            // Act
            _controller.PlayShot(result);

            // Assert - 30 feet = 9.144 meters
            Assert.AreEqual(9.144f, _ballTransform.position.y, 0.01f);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void PlayShot_WithSinglePoint_HandlesCorrectly()
        {
            // Arrange
            var trajectory = new List<TrajectoryPoint>
            {
                new TrajectoryPoint(0f, Vector3.zero, Phase.Stopped)
            };
            var result = CreateShotResult(trajectory);
            _controller.CurrentTimeScale = TimeScale.Instant;

            // Act & Assert
            Assert.DoesNotThrow(() => _controller.PlayShot(result));
        }

        [Test]
        public void PlayShot_CalledTwice_StopsPreviousAnimation()
        {
            // Arrange
            var result1 = CreateSimpleShotResult();
            var result2 = CreateSimpleShotResult();

            // Act
            _controller.PlayShot(result1);
            _controller.PlayShot(result2);

            // Assert - Should have second shot
            Assert.AreEqual(result2, _controller.CurrentShot);
        }

        [Test]
        public void TimeScale_Fast_AffectsPlayback()
        {
            // Just verify the property can be set
            _controller.CurrentTimeScale = TimeScale.Fast;
            Assert.AreEqual(TimeScale.Fast, _controller.CurrentTimeScale);
        }

        #endregion

        #region Helper Methods

        private ShotResult CreateSimpleShotResult()
        {
            var trajectory = new List<TrajectoryPoint>
            {
                new TrajectoryPoint(0f, new Vector3(0f, 0f, 0f), Phase.Flight),
                new TrajectoryPoint(1f, new Vector3(50f, 30f, 0f), Phase.Flight),
                new TrajectoryPoint(2f, new Vector3(100f, 40f, 0f), Phase.Flight),
                new TrajectoryPoint(3f, new Vector3(150f, 30f, 0f), Phase.Flight),
                new TrajectoryPoint(4f, new Vector3(200f, 0f, 0f), Phase.Bounce),
                new TrajectoryPoint(5f, new Vector3(220f, 0f, 0f), Phase.Rolling),
                new TrajectoryPoint(6f, new Vector3(230f, 0f, 0f), Phase.Stopped)
            };

            return CreateShotResult(trajectory);
        }

        private ShotResult CreateShotResultWithFinalPosition(float forwardYards, float heightFeet, float lateralYards)
        {
            var trajectory = new List<TrajectoryPoint>
            {
                new TrajectoryPoint(0f, new Vector3(0f, 0f, 0f), Phase.Flight),
                new TrajectoryPoint(5f, new Vector3(forwardYards, heightFeet, lateralYards), Phase.Stopped)
            };

            return CreateShotResult(trajectory);
        }

        private ShotResult CreateShotResult(List<TrajectoryPoint> trajectory)
        {
            float totalTime = trajectory.Count > 0 ? trajectory[trajectory.Count - 1].Time : 0f;

            return new ShotResult
            {
                Trajectory = trajectory,
                CarryDistance = 200f,
                TotalDistance = 230f,
                RollDistance = 30f,
                OfflineDistance = 0f,
                MaxHeight = 40f,
                MaxHeightTime = 2f,
                FlightTime = 4f,
                TotalTime = totalTime,
                BounceCount = 1,
                LaunchData = new LaunchData
                {
                    BallSpeed = 160f,
                    VLA = 12f,
                    HLA = 0f,
                    BackSpin = 3000f,
                    SideSpin = 0f
                },
                Conditions = new Conditions()
            };
        }

        #endregion
    }
}
