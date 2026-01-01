// ABOUTME: Unit tests for the TrajectoryRenderer component.
// ABOUTME: Tests line rendering, quality tiers, fade out, and coordinate conversion.

using NUnit.Framework;
using UnityEngine;
using OpenRange.Visualization;
using OpenRange.Physics;
using System.Collections.Generic;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class TrajectoryRendererTests
    {
        private GameObject _testObject;
        private TrajectoryRenderer _trajectoryRenderer;
        private LineRenderer _lineRenderer;
        private LineRenderer _predictionLineRenderer;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestTrajectoryLine");
            _trajectoryRenderer = _testObject.AddComponent<TrajectoryRenderer>();

            // Create and attach main line renderer
            _lineRenderer = _testObject.AddComponent<LineRenderer>();
            _lineRenderer.enabled = false;

            // Create prediction line renderer as child
            var predictionGo = new GameObject("PredictionLine");
            predictionGo.transform.SetParent(_testObject.transform);
            _predictionLineRenderer = predictionGo.AddComponent<LineRenderer>();
            _predictionLineRenderer.enabled = false;

            // Use reflection to set private serialized fields since we're in EditMode
            SetPrivateField(_trajectoryRenderer, "_lineRenderer", _lineRenderer);
            SetPrivateField(_trajectoryRenderer, "_predictionLineRenderer", _predictionLineRenderer);
            SetPrivateField(_trajectoryRenderer, "_vertexCountHigh", 100);
            SetPrivateField(_trajectoryRenderer, "_vertexCountMedium", 50);
            SetPrivateField(_trajectoryRenderer, "_vertexCountLow", 20);
            SetPrivateField(_trajectoryRenderer, "_yardsToUnits", 0.9144f);
            SetPrivateField(_trajectoryRenderer, "_feetToUnits", 0.3048f);
            SetPrivateField(_trajectoryRenderer, "_lineWidthStart", 0.05f);
            SetPrivateField(_trajectoryRenderer, "_lineWidthEnd", 0.01f);

            // Create default gradients
            var actualGradient = CreateActualGradient();
            var predictionGradient = CreatePredictionGradient();
            SetPrivateField(_trajectoryRenderer, "_actualColorGradient", actualGradient);
            SetPrivateField(_trajectoryRenderer, "_predictionColorGradient", predictionGradient);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
        }

        #region ShowTrajectory Tests

        [Test]
        public void ShowTrajectory_ValidResult_EnablesLineRenderer()
        {
            // Arrange
            var result = CreateTestShotResult(10);

            // Act
            _trajectoryRenderer.ShowTrajectory(result);

            // Assert
            Assert.IsTrue(_lineRenderer.enabled);
            Assert.IsTrue(_trajectoryRenderer.IsVisible);
        }

        [Test]
        public void ShowTrajectory_ValidResult_SetsPositionCount()
        {
            // Arrange
            var result = CreateTestShotResult(10);

            // Act
            _trajectoryRenderer.ShowTrajectory(result);

            // Assert
            Assert.AreEqual(10, _lineRenderer.positionCount);
        }

        [Test]
        public void ShowTrajectory_NullResult_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _trajectoryRenderer.ShowTrajectory(null));
            Assert.IsFalse(_trajectoryRenderer.IsVisible);
        }

        [Test]
        public void ShowTrajectory_EmptyTrajectory_DoesNotThrow()
        {
            // Arrange
            var result = new ShotResult { Trajectory = new List<TrajectoryPoint>() };

            // Act & Assert
            Assert.DoesNotThrow(() => _trajectoryRenderer.ShowTrajectory(result));
            Assert.IsFalse(_trajectoryRenderer.IsVisible);
        }

        [Test]
        public void ShowTrajectory_LargeTrajectory_ResamplesPoints()
        {
            // Arrange - Create trajectory with more points than vertex count
            var result = CreateTestShotResult(200);
            _trajectoryRenderer.SetQualityTier(QualityTier.High); // 100 vertices

            // Act
            _trajectoryRenderer.ShowTrajectory(result);

            // Assert - Should be resampled to 100 points
            Assert.AreEqual(100, _lineRenderer.positionCount);
        }

        [Test]
        public void ShowTrajectory_SmallTrajectory_UsesAllPoints()
        {
            // Arrange - Create trajectory with fewer points than vertex count
            var result = CreateTestShotResult(25);
            _trajectoryRenderer.SetQualityTier(QualityTier.High); // 100 vertices

            // Act
            _trajectoryRenderer.ShowTrajectory(result);

            // Assert - Should use all 25 points
            Assert.AreEqual(25, _lineRenderer.positionCount);
        }

        [Test]
        public void ShowTrajectory_SetsLineWidths()
        {
            // Arrange
            var result = CreateTestShotResult(10);

            // Act
            _trajectoryRenderer.ShowTrajectory(result);

            // Assert
            Assert.AreEqual(0.05f, _lineRenderer.startWidth);
            Assert.AreEqual(0.01f, _lineRenderer.endWidth);
        }

        [Test]
        public void ShowTrajectory_ConvertsCoordinatesCorrectly()
        {
            // Arrange - Create trajectory with known positions
            var result = new ShotResult
            {
                Trajectory = new List<TrajectoryPoint>
                {
                    new TrajectoryPoint(0f, new Vector3(0f, 0f, 0f), Phase.Flight),
                    new TrajectoryPoint(1f, new Vector3(100f, 32.8084f, 10f), Phase.Flight) // 100 yards forward, ~10 meters high, 10 yards right
                }
            };

            // Act
            _trajectoryRenderer.ShowTrajectory(result);

            // Assert
            Vector3[] positions = new Vector3[2];
            _lineRenderer.GetPositions(positions);

            // First point should be at origin (with offset)
            Assert.AreEqual(0f, positions[0].x, 0.001f);
            Assert.AreEqual(0f, positions[0].y, 0.001f);
            Assert.AreEqual(0f, positions[0].z, 0.001f);

            // Second point: X = 100 yards * 0.9144 = 91.44m, Y = 32.8084 feet * 0.3048 = 10m, Z = 10 yards * 0.9144 = 9.144m
            Assert.AreEqual(91.44f, positions[1].x, 0.01f);
            Assert.AreEqual(10f, positions[1].y, 0.01f);
            Assert.AreEqual(9.144f, positions[1].z, 0.01f);
        }

        [Test]
        public void ShowTrajectory_AppliesOriginOffset()
        {
            // Arrange
            _trajectoryRenderer.OriginOffset = new Vector3(5f, 1f, 2f);
            var result = new ShotResult
            {
                Trajectory = new List<TrajectoryPoint>
                {
                    new TrajectoryPoint(0f, new Vector3(0f, 0f, 0f), Phase.Flight)
                }
            };

            // Act
            _trajectoryRenderer.ShowTrajectory(result);

            // Assert
            Vector3[] positions = new Vector3[1];
            _lineRenderer.GetPositions(positions);

            Assert.AreEqual(5f, positions[0].x, 0.001f);
            Assert.AreEqual(1f, positions[0].y, 0.001f);
            Assert.AreEqual(2f, positions[0].z, 0.001f);
        }

        #endregion

        #region ShowPrediction Tests

        [Test]
        public void ShowPrediction_ValidResult_EnablesPredictionRenderer()
        {
            // Arrange
            var result = CreateTestShotResult(10);

            // Act
            _trajectoryRenderer.ShowPrediction(result);

            // Assert
            Assert.IsTrue(_predictionLineRenderer.enabled);
            Assert.IsTrue(_trajectoryRenderer.IsPredictionVisible);
        }

        [Test]
        public void ShowPrediction_NullResult_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _trajectoryRenderer.ShowPrediction(null));
            Assert.IsFalse(_trajectoryRenderer.IsPredictionVisible);
        }

        [Test]
        public void ShowPrediction_WithNullPredictionRenderer_UsesMainRenderer()
        {
            // Arrange
            SetPrivateField(_trajectoryRenderer, "_predictionLineRenderer", null);
            var result = CreateTestShotResult(10);

            // Act
            _trajectoryRenderer.ShowPrediction(result);

            // Assert - Should use main line renderer
            Assert.IsTrue(_lineRenderer.enabled);
            Assert.IsTrue(_trajectoryRenderer.IsVisible);
        }

        #endregion

        #region Hide Tests

        [Test]
        public void Hide_DisablesLineRenderer()
        {
            // Arrange
            _trajectoryRenderer.ShowTrajectory(CreateTestShotResult(10));
            Assert.IsTrue(_trajectoryRenderer.IsVisible);

            // Act
            _trajectoryRenderer.Hide();

            // Assert
            Assert.IsFalse(_lineRenderer.enabled);
            Assert.IsFalse(_trajectoryRenderer.IsVisible);
        }

        [Test]
        public void Hide_DisablesPredictionRenderer()
        {
            // Arrange
            _trajectoryRenderer.ShowPrediction(CreateTestShotResult(10));
            Assert.IsTrue(_trajectoryRenderer.IsPredictionVisible);

            // Act
            _trajectoryRenderer.Hide();

            // Assert
            Assert.IsFalse(_predictionLineRenderer.enabled);
            Assert.IsFalse(_trajectoryRenderer.IsPredictionVisible);
        }

        [Test]
        public void Hide_WhenAlreadyHidden_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _trajectoryRenderer.Hide());
        }

        [Test]
        public void HidePrediction_HidesOnlyPredictionLine()
        {
            // Arrange
            _trajectoryRenderer.ShowTrajectory(CreateTestShotResult(10));
            _trajectoryRenderer.ShowPrediction(CreateTestShotResult(10));

            // Act
            _trajectoryRenderer.HidePrediction();

            // Assert
            Assert.IsTrue(_trajectoryRenderer.IsVisible); // Main still visible
            Assert.IsFalse(_trajectoryRenderer.IsPredictionVisible);
        }

        #endregion

        #region Quality Tier Tests

        [Test]
        public void SetQualityTier_High_SetsCorrectVertexCount()
        {
            // Act
            _trajectoryRenderer.SetQualityTier(QualityTier.High);

            // Assert
            Assert.AreEqual(QualityTier.High, _trajectoryRenderer.CurrentQualityTier);
            Assert.AreEqual(100, _trajectoryRenderer.CurrentVertexCount);
        }

        [Test]
        public void SetQualityTier_Medium_SetsCorrectVertexCount()
        {
            // Act
            _trajectoryRenderer.SetQualityTier(QualityTier.Medium);

            // Assert
            Assert.AreEqual(QualityTier.Medium, _trajectoryRenderer.CurrentQualityTier);
            Assert.AreEqual(50, _trajectoryRenderer.CurrentVertexCount);
        }

        [Test]
        public void SetQualityTier_Low_SetsCorrectVertexCount()
        {
            // Act
            _trajectoryRenderer.SetQualityTier(QualityTier.Low);

            // Assert
            Assert.AreEqual(QualityTier.Low, _trajectoryRenderer.CurrentQualityTier);
            Assert.AreEqual(20, _trajectoryRenderer.CurrentVertexCount);
        }

        [Test]
        public void SetQualityTier_WhileVisible_ReRendersWithNewVertexCount()
        {
            // Arrange
            _trajectoryRenderer.SetQualityTier(QualityTier.High);
            var result = CreateTestShotResult(150);
            _trajectoryRenderer.ShowTrajectory(result);
            Assert.AreEqual(100, _lineRenderer.positionCount);

            // Act
            _trajectoryRenderer.SetQualityTier(QualityTier.Low);

            // Assert
            Assert.AreEqual(20, _lineRenderer.positionCount);
        }

        [Test]
        public void CurrentQualityTier_DefaultsToHigh()
        {
            // Assert
            Assert.AreEqual(QualityTier.High, _trajectoryRenderer.CurrentQualityTier);
        }

        #endregion

        #region DisplayMode Tests

        [Test]
        public void CurrentDisplayMode_DefaultsToActual()
        {
            // Assert
            Assert.AreEqual(TrajectoryRenderer.DisplayMode.Actual, _trajectoryRenderer.CurrentDisplayMode);
        }

        [Test]
        public void CurrentDisplayMode_CanBeSet()
        {
            // Act
            _trajectoryRenderer.CurrentDisplayMode = TrajectoryRenderer.DisplayMode.Predicted;

            // Assert
            Assert.AreEqual(TrajectoryRenderer.DisplayMode.Predicted, _trajectoryRenderer.CurrentDisplayMode);
        }

        [Test]
        public void CurrentDisplayMode_CanBeSetToBoth()
        {
            // Act
            _trajectoryRenderer.CurrentDisplayMode = TrajectoryRenderer.DisplayMode.Both;

            // Assert
            Assert.AreEqual(TrajectoryRenderer.DisplayMode.Both, _trajectoryRenderer.CurrentDisplayMode);
        }

        #endregion

        #region GetPositionAtProgress Tests

        [Test]
        public void GetPositionAtProgress_Zero_ReturnsStartPosition()
        {
            // Arrange
            var result = new ShotResult
            {
                Trajectory = new List<TrajectoryPoint>
                {
                    new TrajectoryPoint(0f, new Vector3(0f, 0f, 0f), Phase.Flight),
                    new TrajectoryPoint(1f, new Vector3(100f, 32.8084f, 0f), Phase.Flight)
                }
            };
            _trajectoryRenderer.ShowTrajectory(result);

            // Act
            Vector3 pos = _trajectoryRenderer.GetPositionAtProgress(0f);

            // Assert
            Assert.AreEqual(0f, pos.x, 0.001f);
            Assert.AreEqual(0f, pos.y, 0.001f);
        }

        [Test]
        public void GetPositionAtProgress_One_ReturnsEndPosition()
        {
            // Arrange
            var result = new ShotResult
            {
                Trajectory = new List<TrajectoryPoint>
                {
                    new TrajectoryPoint(0f, new Vector3(0f, 0f, 0f), Phase.Flight),
                    new TrajectoryPoint(1f, new Vector3(100f, 32.8084f, 0f), Phase.Flight)
                }
            };
            _trajectoryRenderer.ShowTrajectory(result);

            // Act
            Vector3 pos = _trajectoryRenderer.GetPositionAtProgress(1f);

            // Assert - 100 yards = 91.44m
            Assert.AreEqual(91.44f, pos.x, 0.01f);
        }

        [Test]
        public void GetPositionAtProgress_Half_ReturnsInterpolatedPosition()
        {
            // Arrange
            var result = new ShotResult
            {
                Trajectory = new List<TrajectoryPoint>
                {
                    new TrajectoryPoint(0f, new Vector3(0f, 0f, 0f), Phase.Flight),
                    new TrajectoryPoint(1f, new Vector3(100f, 0f, 0f), Phase.Flight)
                }
            };
            _trajectoryRenderer.ShowTrajectory(result);

            // Act
            Vector3 pos = _trajectoryRenderer.GetPositionAtProgress(0.5f);

            // Assert - 50 yards = 45.72m
            Assert.AreEqual(45.72f, pos.x, 0.01f);
        }

        [Test]
        public void GetPositionAtProgress_NoShot_ReturnsOriginOffset()
        {
            // Arrange
            _trajectoryRenderer.OriginOffset = new Vector3(1f, 2f, 3f);

            // Act
            Vector3 pos = _trajectoryRenderer.GetPositionAtProgress(0.5f);

            // Assert
            Assert.AreEqual(1f, pos.x);
            Assert.AreEqual(2f, pos.y);
            Assert.AreEqual(3f, pos.z);
        }

        [Test]
        public void GetPositionAtProgress_NegativeProgress_ClampsToZero()
        {
            // Arrange
            var result = new ShotResult
            {
                Trajectory = new List<TrajectoryPoint>
                {
                    new TrajectoryPoint(0f, new Vector3(0f, 0f, 0f), Phase.Flight),
                    new TrajectoryPoint(1f, new Vector3(100f, 0f, 0f), Phase.Flight)
                }
            };
            _trajectoryRenderer.ShowTrajectory(result);

            // Act
            Vector3 pos = _trajectoryRenderer.GetPositionAtProgress(-0.5f);

            // Assert
            Assert.AreEqual(0f, pos.x, 0.001f);
        }

        [Test]
        public void GetPositionAtProgress_OverOne_ClampsToOne()
        {
            // Arrange
            var result = new ShotResult
            {
                Trajectory = new List<TrajectoryPoint>
                {
                    new TrajectoryPoint(0f, new Vector3(0f, 0f, 0f), Phase.Flight),
                    new TrajectoryPoint(1f, new Vector3(100f, 0f, 0f), Phase.Flight)
                }
            };
            _trajectoryRenderer.ShowTrajectory(result);

            // Act
            Vector3 pos = _trajectoryRenderer.GetPositionAtProgress(1.5f);

            // Assert - 100 yards = 91.44m
            Assert.AreEqual(91.44f, pos.x, 0.01f);
        }

        #endregion

        #region Property Tests

        [Test]
        public void LineRenderer_Property_ReturnsCorrectReference()
        {
            // Assert
            Assert.AreEqual(_lineRenderer, _trajectoryRenderer.LineRenderer);
        }

        [Test]
        public void PredictionLineRenderer_Property_ReturnsCorrectReference()
        {
            // Assert
            Assert.AreEqual(_predictionLineRenderer, _trajectoryRenderer.PredictionLineRenderer);
        }

        [Test]
        public void IsVisible_DefaultsFalse()
        {
            // Assert
            Assert.IsFalse(_trajectoryRenderer.IsVisible);
        }

        [Test]
        public void IsPredictionVisible_DefaultsFalse()
        {
            // Assert
            Assert.IsFalse(_trajectoryRenderer.IsPredictionVisible);
        }

        [Test]
        public void OriginOffset_CanBeSetAndGet()
        {
            // Arrange
            var offset = new Vector3(10f, 5f, 3f);

            // Act
            _trajectoryRenderer.OriginOffset = offset;

            // Assert
            Assert.AreEqual(offset, _trajectoryRenderer.OriginOffset);
        }

        #endregion

        #region Gradient Tests

        [Test]
        public void SetActualColorGradient_DoesNotThrow()
        {
            // Arrange
            var gradient = new Gradient();

            // Act & Assert
            Assert.DoesNotThrow(() => _trajectoryRenderer.SetActualColorGradient(gradient));
        }

        [Test]
        public void SetPredictionColorGradient_DoesNotThrow()
        {
            // Arrange
            var gradient = new Gradient();

            // Act & Assert
            Assert.DoesNotThrow(() => _trajectoryRenderer.SetPredictionColorGradient(gradient));
        }

        #endregion

        #region FadeOut Tests

        [Test]
        public void FadeOut_WhenNotVisible_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _trajectoryRenderer.FadeOut(1f));
        }

        [Test]
        public void FadeOut_ZeroDuration_HidesImmediately()
        {
            // Arrange
            _trajectoryRenderer.ShowTrajectory(CreateTestShotResult(10));
            Assert.IsTrue(_trajectoryRenderer.IsVisible);

            // Act
            _trajectoryRenderer.FadeOut(0f);

            // Assert - With zero duration, the coroutine calls Hide() before yield break,
            // which executes synchronously, so IsVisible should be false
            Assert.IsFalse(_trajectoryRenderer.IsVisible);
        }

        #endregion

        #region Null Handling Tests

        [Test]
        public void ShowTrajectory_WithNullLineRenderer_DoesNotThrow()
        {
            // Arrange
            SetPrivateField(_trajectoryRenderer, "_lineRenderer", null);

            // Act & Assert
            Assert.DoesNotThrow(() => _trajectoryRenderer.ShowTrajectory(CreateTestShotResult(10)));
        }

        [Test]
        public void Hide_WithNullLineRenderers_DoesNotThrow()
        {
            // Arrange
            SetPrivateField(_trajectoryRenderer, "_lineRenderer", null);
            SetPrivateField(_trajectoryRenderer, "_predictionLineRenderer", null);

            // Act & Assert
            Assert.DoesNotThrow(() => _trajectoryRenderer.Hide());
        }

        [Test]
        public void SetLineRenderer_SetsReference()
        {
            // Arrange
            var newLineRenderer = _testObject.AddComponent<LineRenderer>();

            // Act
            _trajectoryRenderer.SetLineRenderer(newLineRenderer);

            // Assert
            Assert.AreEqual(newLineRenderer, _trajectoryRenderer.LineRenderer);
        }

        [Test]
        public void SetPredictionLineRenderer_SetsReference()
        {
            // Arrange
            var newLineRenderer = _testObject.AddComponent<LineRenderer>();

            // Act
            _trajectoryRenderer.SetPredictionLineRenderer(newLineRenderer);

            // Assert
            Assert.AreEqual(newLineRenderer, _trajectoryRenderer.PredictionLineRenderer);
        }

        #endregion

        #region Helper Methods

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

        /// <summary>
        /// Create a test shot result with a simple trajectory.
        /// </summary>
        private ShotResult CreateTestShotResult(int pointCount)
        {
            var trajectory = new List<TrajectoryPoint>();
            for (int i = 0; i < pointCount; i++)
            {
                float t = (float)i / (pointCount - 1);
                float x = t * 200f; // 200 yards total
                float y = 4f * t * (1f - t) * 100f; // Parabolic arc, max height ~100 feet
                float z = t * 10f; // Slight right drift

                trajectory.Add(new TrajectoryPoint(t * 5f, new Vector3(x, y, z), Phase.Flight));
            }

            return new ShotResult
            {
                Trajectory = trajectory,
                CarryDistance = 180f,
                TotalDistance = 200f,
                MaxHeight = 100f,
                FlightTime = 5f,
                TotalTime = 6f
            };
        }

        /// <summary>
        /// Create the default gradient for actual trajectory.
        /// </summary>
        private Gradient CreateActualGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.cyan, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.5f, 1f)
                }
            );
            return gradient;
        }

        /// <summary>
        /// Create the default gradient for prediction trajectory.
        /// </summary>
        private Gradient CreatePredictionGradient()
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

        #endregion
    }
}
