// ABOUTME: Unit tests for the BallSpinner component.
// ABOUTME: Tests spin axis calculation, rotation, and decay over time.

using NUnit.Framework;
using UnityEngine;
using OpenRange.Visualization;
using OpenRange.Physics;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class BallSpinnerTests
    {
        private GameObject _testObject;
        private BallSpinner _spinner;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestBallSpinner");
            _spinner = _testObject.AddComponent<BallSpinner>();
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
        public void Initialize_WithBackspinOnly_SetsSpinning()
        {
            // Act
            _spinner.Initialize(3000f, 0f);

            // Assert
            Assert.IsTrue(_spinner.IsSpinning);
        }

        [Test]
        public void Initialize_WithSidespinOnly_SetsSpinning()
        {
            // Act
            _spinner.Initialize(0f, 500f);

            // Assert
            Assert.IsTrue(_spinner.IsSpinning);
        }

        [Test]
        public void Initialize_WithBothSpins_SetsSpinning()
        {
            // Act
            _spinner.Initialize(3000f, 500f);

            // Assert
            Assert.IsTrue(_spinner.IsSpinning);
        }

        [Test]
        public void Initialize_StoresBackspin()
        {
            // Act
            _spinner.Initialize(3500f, 0f);

            // Assert
            Assert.AreEqual(3500f, _spinner.InitialBackSpin);
            Assert.AreEqual(3500f, _spinner.CurrentBackSpin);
        }

        [Test]
        public void Initialize_StoresSidespin()
        {
            // Act
            _spinner.Initialize(0f, 750f);

            // Assert
            Assert.AreEqual(750f, _spinner.InitialSideSpin);
            Assert.AreEqual(750f, _spinner.CurrentSideSpin);
        }

        [Test]
        public void Initialize_NegativeSidespin_StoredCorrectly()
        {
            // Arrange - negative sidespin = draw/hook

            // Act
            _spinner.Initialize(3000f, -500f);

            // Assert
            Assert.AreEqual(-500f, _spinner.InitialSideSpin);
        }

        #endregion

        #region Spin Axis Tests

        [Test]
        public void SpinAxis_PureBackspin_AlongZAxis()
        {
            // Act
            _spinner.Initialize(3000f, 0f);

            // Assert - Pure backspin should have axis along Z
            Assert.AreEqual(0f, _spinner.SpinAxis.x, 0.01f);
            Assert.AreEqual(0f, _spinner.SpinAxis.y, 0.01f);
            Assert.AreEqual(1f, Mathf.Abs(_spinner.SpinAxis.z), 0.01f);
        }

        [Test]
        public void SpinAxis_PureSidespin_AlongYAxis()
        {
            // Act
            _spinner.Initialize(0f, 1000f);

            // Assert - Pure sidespin should have axis along -Y
            Assert.AreEqual(0f, _spinner.SpinAxis.x, 0.01f);
            Assert.AreEqual(-1f, _spinner.SpinAxis.y, 0.01f);
            Assert.AreEqual(0f, _spinner.SpinAxis.z, 0.01f);
        }

        [Test]
        public void SpinAxis_CombinedSpin_HasBothComponents()
        {
            // Arrange - Equal backspin and sidespin

            // Act
            _spinner.Initialize(1000f, 1000f);

            // Assert - Should have both Y and Z components
            Assert.AreNotEqual(0f, _spinner.SpinAxis.y, "Y component should be non-zero");
            Assert.AreNotEqual(0f, _spinner.SpinAxis.z, "Z component should be non-zero");
        }

        [Test]
        public void SpinAxis_IsNormalized()
        {
            // Act
            _spinner.Initialize(3000f, 500f);

            // Assert
            Assert.AreEqual(1f, _spinner.SpinAxis.magnitude, 0.001f);
        }

        [Test]
        public void SpinAxis_ZeroSpin_DefaultsToForward()
        {
            // Act
            _spinner.Initialize(0f, 0f);

            // Assert
            Assert.AreEqual(Vector3.forward, _spinner.SpinAxis);
        }

        #endregion

        #region Spin Rate Tests

        [Test]
        public void SpinRate_PositiveWithBackspin()
        {
            // Act
            _spinner.Initialize(3000f, 0f);

            // Assert
            Assert.Greater(_spinner.SpinRate, 0f);
        }

        [Test]
        public void SpinRate_PositiveWithSidespin()
        {
            // Act
            _spinner.Initialize(0f, 500f);

            // Assert
            Assert.Greater(_spinner.SpinRate, 0f);
        }

        [Test]
        public void SpinRate_ZeroWithNoSpin()
        {
            // Act
            _spinner.Initialize(0f, 0f);

            // Assert
            Assert.AreEqual(0f, _spinner.SpinRate, 0.001f);
        }

        [Test]
        public void SpinRate_CombinedSpin_GreaterThanIndividual()
        {
            // Arrange
            float backSpin = 3000f;
            float sideSpin = 500f;

            // Act
            _spinner.Initialize(backSpin, sideSpin);
            float combinedRate = _spinner.SpinRate;

            // Calculate individual rates
            float backRate = UnitConversions.RpmToRadS(backSpin);
            float sideRate = UnitConversions.RpmToRadS(sideSpin);

            // Assert - combined rate should equal sqrt(back^2 + side^2)
            float expected = Mathf.Sqrt(backRate * backRate + sideRate * sideRate);
            Assert.AreEqual(expected, combinedRate, 0.01f);
        }

        #endregion

        #region Stop and Reset Tests

        [Test]
        public void Stop_SetsIsSpinningFalse()
        {
            // Arrange
            _spinner.Initialize(3000f, 500f);

            // Act
            _spinner.Stop();

            // Assert
            Assert.IsFalse(_spinner.IsSpinning);
        }

        [Test]
        public void Stop_ResetsCurrentSpins()
        {
            // Arrange
            _spinner.Initialize(3000f, 500f);

            // Act
            _spinner.Stop();

            // Assert
            Assert.AreEqual(0f, _spinner.CurrentBackSpin);
            Assert.AreEqual(0f, _spinner.CurrentSideSpin);
        }

        [Test]
        public void Stop_ResetSpinRate()
        {
            // Arrange
            _spinner.Initialize(3000f, 500f);

            // Act
            _spinner.Stop();

            // Assert
            Assert.AreEqual(0f, _spinner.SpinRate);
        }

        [Test]
        public void Reset_RestoresInitialValues()
        {
            // Arrange
            _spinner.Initialize(3000f, 500f);
            _spinner.Stop();

            // Act
            _spinner.Reset();

            // Assert
            Assert.AreEqual(3000f, _spinner.CurrentBackSpin);
            Assert.AreEqual(500f, _spinner.CurrentSideSpin);
            Assert.IsTrue(_spinner.IsSpinning);
        }

        [Test]
        public void Reset_RestoresSpinRate()
        {
            // Arrange
            _spinner.Initialize(3000f, 500f);
            float initialRate = _spinner.SpinRate;
            _spinner.Stop();

            // Act
            _spinner.Reset();

            // Assert
            Assert.AreEqual(initialRate, _spinner.SpinRate, 0.001f);
        }

        #endregion

        #region Rotation Calculation Tests

        [Test]
        public void CalculateRotation_AtTimeZero_ReturnsIdentity()
        {
            // Arrange
            _spinner.Initialize(3000f, 500f);

            // Act
            Quaternion rotation = _spinner.CalculateRotation(0f);

            // Assert
            Assert.AreEqual(Quaternion.identity, rotation);
        }

        [Test]
        public void CalculateRotation_PositiveTime_ReturnsNonIdentity()
        {
            // Arrange
            _spinner.Initialize(3000f, 0f);

            // Act
            Quaternion rotation = _spinner.CalculateRotation(1f);

            // Assert
            Assert.AreNotEqual(Quaternion.identity, rotation);
        }

        [Test]
        public void CalculateRotation_NotSpinning_ReturnsIdentity()
        {
            // Arrange
            _spinner.Initialize(3000f, 500f);
            _spinner.Stop();

            // Act
            Quaternion rotation = _spinner.CalculateRotation(1f);

            // Assert
            Assert.AreEqual(Quaternion.identity, rotation);
        }

        [Test]
        public void CalculateRotation_MoreTime_MoreRotation()
        {
            // Arrange - use very short times to avoid angle wraparound issues
            // (Quaternion.Angle only returns 0-180 degrees)
            _spinner.Initialize(1000f, 0f);

            // Act - use small time values to stay within 0-180 degree range
            Quaternion rotation1 = _spinner.CalculateRotation(0.01f);
            Quaternion rotation2 = _spinner.CalculateRotation(0.02f);

            // Assert - rotation2 should be greater (angle from identity)
            float angle1 = Quaternion.Angle(Quaternion.identity, rotation1);
            float angle2 = Quaternion.Angle(Quaternion.identity, rotation2);
            Assert.Greater(angle2, angle1);
        }

        #endregion

        #region Decay Tests

        [Test]
        public void GetDecayFactor_AtTimeZero_ReturnsOne()
        {
            // Arrange
            _spinner.Initialize(3000f, 500f);

            // Act
            float decay = _spinner.GetDecayFactor(0f);

            // Assert
            Assert.AreEqual(1f, decay, 0.001f);
        }

        [Test]
        public void GetDecayFactor_PositiveTime_LessThanOne()
        {
            // Arrange
            _spinner.Initialize(3000f, 500f);

            // Act
            float decay = _spinner.GetDecayFactor(1f);

            // Assert
            Assert.Less(decay, 1f);
            Assert.Greater(decay, 0f);
        }

        [Test]
        public void GetDecayFactor_MoreTime_MoreDecay()
        {
            // Arrange
            _spinner.Initialize(3000f, 500f);

            // Act
            float decay1 = _spinner.GetDecayFactor(1f);
            float decay2 = _spinner.GetDecayFactor(5f);

            // Assert
            Assert.Less(decay2, decay1);
        }

        #endregion

        #region Visualization Enable Tests

        [Test]
        public void IsVisualizationEnabled_DefaultTrue()
        {
            // Assert
            Assert.IsTrue(_spinner.IsVisualizationEnabled);
        }

        [Test]
        public void SetVisualizationEnabled_False_DisablesVisualization()
        {
            // Act
            _spinner.SetVisualizationEnabled(false);

            // Assert
            Assert.IsFalse(_spinner.IsVisualizationEnabled);
        }

        [Test]
        public void SetVisualizationEnabled_True_EnablesVisualization()
        {
            // Arrange
            _spinner.SetVisualizationEnabled(false);

            // Act
            _spinner.SetVisualizationEnabled(true);

            // Assert
            Assert.IsTrue(_spinner.IsVisualizationEnabled);
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void Initialize_VeryHighBackspin_HandlesCorrectly()
        {
            // Act & Assert - should not throw
            Assert.DoesNotThrow(() => _spinner.Initialize(15000f, 0f));
            Assert.IsTrue(_spinner.IsSpinning);
        }

        [Test]
        public void Initialize_NegativeBackspin_HandlesCorrectly()
        {
            // Act & Assert - should not throw
            Assert.DoesNotThrow(() => _spinner.Initialize(-1000f, 0f));
        }

        [Test]
        public void CalculateRotation_VeryLargeTime_DoesNotOverflow()
        {
            // Arrange
            _spinner.Initialize(3000f, 500f);

            // Act & Assert - should not throw or return NaN
            Quaternion rotation = _spinner.CalculateRotation(1000f);
            Assert.IsFalse(float.IsNaN(rotation.x));
            Assert.IsFalse(float.IsNaN(rotation.y));
            Assert.IsFalse(float.IsNaN(rotation.z));
            Assert.IsFalse(float.IsNaN(rotation.w));
        }

        #endregion
    }
}
