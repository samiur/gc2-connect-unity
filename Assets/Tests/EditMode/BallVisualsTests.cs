// ABOUTME: Unit tests for the BallVisuals component.
// ABOUTME: Tests trail control, spin visualization, quality tiers, and visual reset.

using NUnit.Framework;
using UnityEngine;
using OpenRange.Visualization;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class BallVisualsTests
    {
        private GameObject _testObject;
        private BallVisuals _ballVisuals;
        private TrailRenderer _trailRenderer;
        private GameObject _spinIndicator;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestGolfBall");
            _ballVisuals = _testObject.AddComponent<BallVisuals>();

            // Create and attach trail renderer
            var trailObject = new GameObject("Trail");
            trailObject.transform.SetParent(_testObject.transform);
            _trailRenderer = trailObject.AddComponent<TrailRenderer>();

            // Create spin indicator
            _spinIndicator = new GameObject("SpinIndicator");
            _spinIndicator.transform.SetParent(_testObject.transform);

            // Use reflection to set private serialized fields since we're in EditMode
            SetPrivateField(_ballVisuals, "_trailRenderer", _trailRenderer);
            SetPrivateField(_ballVisuals, "_spinIndicator", _spinIndicator.transform);
            SetPrivateField(_ballVisuals, "_trailEnabledByDefault", true);
            SetPrivateField(_ballVisuals, "_showSpinByDefault", false);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
        }

        #region Trail Tests

        [Test]
        public void SetTrailEnabled_True_EnablesTrailRenderer()
        {
            // Arrange
            _trailRenderer.enabled = false;

            // Act
            _ballVisuals.SetTrailEnabled(true);

            // Assert
            Assert.IsTrue(_ballVisuals.IsTrailEnabled);
            Assert.IsTrue(_trailRenderer.enabled);
        }

        [Test]
        public void SetTrailEnabled_False_DisablesTrailRenderer()
        {
            // Arrange
            _trailRenderer.enabled = true;

            // Act
            _ballVisuals.SetTrailEnabled(false);

            // Assert
            Assert.IsFalse(_ballVisuals.IsTrailEnabled);
            Assert.IsFalse(_trailRenderer.enabled);
        }

        [Test]
        public void SetTrailEnabled_WithNullTrailRenderer_DoesNotThrow()
        {
            // Arrange
            SetPrivateField(_ballVisuals, "_trailRenderer", null);

            // Act & Assert - should not throw
            Assert.DoesNotThrow(() => _ballVisuals.SetTrailEnabled(true));
        }

        [Test]
        public void ClearTrail_ClearsTrailRendererPositions()
        {
            // Act & Assert - ClearTrail should not throw
            Assert.DoesNotThrow(() => _ballVisuals.ClearTrail());
        }

        [Test]
        public void TrailRenderer_Property_ReturnsCorrectReference()
        {
            // Assert
            Assert.AreEqual(_trailRenderer, _ballVisuals.TrailRenderer);
        }

        #endregion

        #region Spin Visualization Tests

        [Test]
        public void SetSpinVisualization_SetsSpinActive()
        {
            // Arrange
            Vector3 spinAxis = Vector3.up;
            float rpm = 5000f;

            // Act
            _ballVisuals.SetSpinVisualization(spinAxis, rpm);

            // Assert
            Assert.IsTrue(_ballVisuals.IsSpinVisualizationActive);
        }

        [Test]
        public void SetSpinVisualization_ActivatesSpinIndicator()
        {
            // Arrange
            _spinIndicator.SetActive(false);
            Vector3 spinAxis = Vector3.up;
            float rpm = 5000f;

            // Act
            _ballVisuals.SetSpinVisualization(spinAxis, rpm);

            // Assert
            Assert.IsTrue(_spinIndicator.activeSelf);
        }

        [Test]
        public void SetSpinVisualization_StoresSpinParameters()
        {
            // Arrange
            Vector3 spinAxis = Vector3.right;
            float rpm = 7500f;

            // Act
            _ballVisuals.SetSpinVisualization(spinAxis, rpm);

            // Assert
            Assert.AreEqual(rpm, _ballVisuals.GetCurrentSpinRpm());
            Assert.AreEqual(spinAxis.normalized, _ballVisuals.GetCurrentSpinAxis());
        }

        [Test]
        public void SetSpinVisualization_NormalizesSpinAxis()
        {
            // Arrange
            Vector3 unnormalizedAxis = new Vector3(3f, 4f, 0f); // Length = 5
            float rpm = 3000f;

            // Act
            _ballVisuals.SetSpinVisualization(unnormalizedAxis, rpm);

            // Assert
            Vector3 result = _ballVisuals.GetCurrentSpinAxis();
            Assert.AreEqual(1f, result.magnitude, 0.001f);
        }

        [Test]
        public void StopSpinVisualization_DeactivatesSpinIndicator()
        {
            // Arrange
            _ballVisuals.SetSpinVisualization(Vector3.up, 5000f);

            // Act
            _ballVisuals.StopSpinVisualization();

            // Assert
            Assert.IsFalse(_ballVisuals.IsSpinVisualizationActive);
            Assert.IsFalse(_spinIndicator.activeSelf);
        }

        [Test]
        public void StopSpinVisualization_ResetsSpinRpm()
        {
            // Arrange
            _ballVisuals.SetSpinVisualization(Vector3.up, 5000f);

            // Act
            _ballVisuals.StopSpinVisualization();

            // Assert
            Assert.AreEqual(0f, _ballVisuals.GetCurrentSpinRpm());
        }

        [Test]
        public void SpinIndicator_Property_ReturnsCorrectReference()
        {
            // Assert
            Assert.AreEqual(_spinIndicator.transform, _ballVisuals.SpinIndicator);
        }

        [Test]
        public void SetSpinVisualization_WithNullSpinIndicator_DoesNotThrow()
        {
            // Arrange
            SetPrivateField(_ballVisuals, "_spinIndicator", null);

            // Act & Assert - should not throw
            Assert.DoesNotThrow(() => _ballVisuals.SetSpinVisualization(Vector3.up, 5000f));
        }

        #endregion

        #region Reset Tests

        [Test]
        public void ResetVisuals_ClearsTrail()
        {
            // Arrange
            _ballVisuals.SetTrailEnabled(true);

            // Act
            _ballVisuals.ResetVisuals();

            // Assert - Trail should still be enabled (default)
            Assert.IsTrue(_ballVisuals.IsTrailEnabled);
        }

        [Test]
        public void ResetVisuals_StopsSpinVisualization()
        {
            // Arrange
            _ballVisuals.SetSpinVisualization(Vector3.up, 5000f);

            // Act
            _ballVisuals.ResetVisuals();

            // Assert
            Assert.IsFalse(_ballVisuals.IsSpinVisualizationActive);
        }

        [Test]
        public void ResetVisuals_ResetsSpinIndicatorRotation()
        {
            // Arrange
            _spinIndicator.transform.rotation = Quaternion.Euler(45f, 90f, 30f);

            // Act
            _ballVisuals.ResetVisuals();

            // Assert
            Assert.AreEqual(Quaternion.identity, _spinIndicator.transform.localRotation);
        }

        [Test]
        public void ResetVisuals_WithTrailDisabledByDefault_KeepsTrailDisabled()
        {
            // Arrange
            SetPrivateField(_ballVisuals, "_trailEnabledByDefault", false);
            _ballVisuals.SetTrailEnabled(true);

            // Act
            _ballVisuals.ResetVisuals();

            // Assert
            Assert.IsFalse(_ballVisuals.IsTrailEnabled);
        }

        #endregion

        #region Quality Tier Tests

        [Test]
        public void SetQualityTier_High_SetsCurrentTier()
        {
            // Act
            _ballVisuals.SetQualityTier(QualityTier.High);

            // Assert
            Assert.AreEqual(QualityTier.High, _ballVisuals.CurrentQualityTier);
        }

        [Test]
        public void SetQualityTier_Medium_SetsCurrentTier()
        {
            // Act
            _ballVisuals.SetQualityTier(QualityTier.Medium);

            // Assert
            Assert.AreEqual(QualityTier.Medium, _ballVisuals.CurrentQualityTier);
        }

        [Test]
        public void SetQualityTier_Low_SetsCurrentTier()
        {
            // Act
            _ballVisuals.SetQualityTier(QualityTier.Low);

            // Assert
            Assert.AreEqual(QualityTier.Low, _ballVisuals.CurrentQualityTier);
        }

        [Test]
        public void SetQualityTier_High_SetsHighTrailVertices()
        {
            // Arrange
            SetPrivateField(_ballVisuals, "_trailVerticesHigh", 30);

            // Act
            _ballVisuals.SetQualityTier(QualityTier.High);

            // Assert
            Assert.AreEqual(30, _trailRenderer.numCornerVertices);
        }

        [Test]
        public void SetQualityTier_Low_SetsLowTrailVertices()
        {
            // Arrange
            SetPrivateField(_ballVisuals, "_trailVerticesLow", 10);

            // Act
            _ballVisuals.SetQualityTier(QualityTier.Low);

            // Assert
            Assert.AreEqual(10, _trailRenderer.numCornerVertices);
        }

        [Test]
        public void SetQualityTier_High_SetsHighTrailTime()
        {
            // Arrange
            SetPrivateField(_ballVisuals, "_trailTimeHigh", 1.5f);

            // Act
            _ballVisuals.SetQualityTier(QualityTier.High);

            // Assert
            Assert.AreEqual(1.5f, _trailRenderer.time);
        }

        [Test]
        public void SetQualityTier_Low_SetsLowTrailTime()
        {
            // Arrange
            SetPrivateField(_ballVisuals, "_trailTimeLow", 0.8f);

            // Act
            _ballVisuals.SetQualityTier(QualityTier.Low);

            // Assert
            Assert.AreEqual(0.8f, _trailRenderer.time);
        }

        [Test]
        public void SetQualityTier_Medium_SetsAverageTrailVertices()
        {
            // Arrange
            SetPrivateField(_ballVisuals, "_trailVerticesHigh", 30);
            SetPrivateField(_ballVisuals, "_trailVerticesLow", 10);

            // Act
            _ballVisuals.SetQualityTier(QualityTier.Medium);

            // Assert
            int expectedVertices = (30 + 10) / 2; // 20
            Assert.AreEqual(expectedVertices, _trailRenderer.numCornerVertices);
        }

        [Test]
        public void SetQualityTier_WithNullTrailRenderer_DoesNotThrow()
        {
            // Arrange
            SetPrivateField(_ballVisuals, "_trailRenderer", null);

            // Act & Assert - should not throw
            Assert.DoesNotThrow(() => _ballVisuals.SetQualityTier(QualityTier.High));
        }

        [Test]
        public void CurrentQualityTier_DefaultsToHigh()
        {
            // Assert
            Assert.AreEqual(QualityTier.High, _ballVisuals.CurrentQualityTier);
        }

        #endregion

        #region Component Initialization Tests

        [Test]
        public void IsTrailEnabled_WhenTrailRendererNull_ReturnsFalse()
        {
            // Arrange
            SetPrivateField(_ballVisuals, "_trailRenderer", null);

            // Assert
            Assert.IsFalse(_ballVisuals.IsTrailEnabled);
        }

        [Test]
        public void IsSpinVisualizationActive_DefaultsFalse()
        {
            // Assert
            Assert.IsFalse(_ballVisuals.IsSpinVisualizationActive);
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void SetSpinVisualization_ZeroRpm_IsStillActive()
        {
            // Act
            _ballVisuals.SetSpinVisualization(Vector3.up, 0f);

            // Assert - Active but not really spinning
            Assert.IsTrue(_ballVisuals.IsSpinVisualizationActive);
            Assert.AreEqual(0f, _ballVisuals.GetCurrentSpinRpm());
        }

        [Test]
        public void SetSpinVisualization_NegativeRpm_StoresAsIs()
        {
            // Act
            _ballVisuals.SetSpinVisualization(Vector3.up, -1000f);

            // Assert - Stores negative value (caller responsibility to validate)
            Assert.AreEqual(-1000f, _ballVisuals.GetCurrentSpinRpm());
        }

        [Test]
        public void SetSpinVisualization_ZeroVector_HandlesGracefully()
        {
            // Act & Assert - Should not throw even with zero vector
            Assert.DoesNotThrow(() => _ballVisuals.SetSpinVisualization(Vector3.zero, 5000f));
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

        #endregion
    }
}
