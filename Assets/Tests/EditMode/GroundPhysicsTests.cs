// ABOUTME: Unit tests for ground physics bounce and roll behavior.
// ABOUTME: Tests spin-dependent effects, velocity-dependent COR, and landing angle impacts.

using NUnit.Framework;
using OpenRange.Physics;
using UnityEngine;

namespace OpenRange.Tests.EditMode
{
    /// <summary>
    /// Tests for GroundPhysics bounce and roll behavior.
    /// Validates spin-dependent effects, velocity-dependent COR, and realistic ground interactions.
    /// </summary>
    [TestFixture]
    public class GroundPhysicsTests
    {
        private GroundSurface _fairway;
        private GroundSurface _green;
        private GroundSurface _rough;

        [SetUp]
        public void SetUp()
        {
            _fairway = GroundSurface.Fairway;
            _green = GroundSurface.Green;
            _rough = GroundSurface.Rough;
        }

        #region Velocity-Dependent COR Tests

        [Test]
        [Description("COR should decrease with higher impact velocity (Penner model)")]
        public void CalculateCOR_HigherVelocity_LowerCOR()
        {
            // Arrange
            float lowVelocity = 5f;   // m/s (gentle landing)
            float highVelocity = 30f; // m/s (steep/fast landing)

            // Act
            float lowVelCOR = GroundPhysics.CalculateVelocityDependentCOR(lowVelocity, _fairway);
            float highVelCOR = GroundPhysics.CalculateVelocityDependentCOR(highVelocity, _fairway);

            // Assert
            Assert.Greater(lowVelCOR, highVelCOR,
                "Lower impact velocity should produce higher COR");
        }

        [Test]
        [Description("COR should be clamped to valid range [0.15, 0.65]")]
        public void CalculateCOR_ExtremeVelocity_ClampedToValidRange()
        {
            // Arrange
            float veryLowVelocity = 0.1f;  // m/s
            float veryHighVelocity = 100f; // m/s

            // Act
            float lowVelCOR = GroundPhysics.CalculateVelocityDependentCOR(veryLowVelocity, _fairway);
            float highVelCOR = GroundPhysics.CalculateVelocityDependentCOR(veryHighVelocity, _fairway);

            // Assert
            Assert.LessOrEqual(lowVelCOR, PhysicsConstants.MaxCOR,
                "COR should not exceed maximum");
            Assert.GreaterOrEqual(highVelCOR, PhysicsConstants.MinCOR,
                "COR should not go below minimum");
        }

        [Test]
        [Description("Surface COR multiplier should affect final COR")]
        public void CalculateCOR_DifferentSurfaces_DifferentCOR()
        {
            // Arrange - Use lower velocity so COR values don't hit min clamp
            // At 15 m/s, Penner formula gives ~0.15 (min clamp), obscuring differences
            float velocity = 5f; // m/s - gives base COR ~0.35

            // Act
            float fairwayCOR = GroundPhysics.CalculateVelocityDependentCOR(velocity, _fairway);
            float greenCOR = GroundPhysics.CalculateVelocityDependentCOR(velocity, _green);
            float roughCOR = GroundPhysics.CalculateVelocityDependentCOR(velocity, _rough);

            // Assert - Green is softer (lower COR), Rough absorbs more (lowest COR)
            Assert.Greater(fairwayCOR, greenCOR,
                "Fairway should have higher COR than green");
            Assert.Greater(greenCOR, roughCOR,
                "Green should have higher COR than rough");
        }

        #endregion

        #region Spin-Dependent Braking Tests

        [Test]
        [Description("High backspin should reduce horizontal velocity after bounce")]
        public void Bounce_HighBackspin_ReducedHorizontalVelocity()
        {
            // Arrange
            Vector3 position = new Vector3(100f, 0f, 0f);
            Vector3 velocity = new Vector3(10f, -15f, 0f); // Coming down at angle
            float lowSpin = 2000f;   // Driver-like
            float highSpin = 9000f;  // Wedge-like
            float landingAngle = 50f;

            // Act
            var lowSpinResult = GroundPhysics.Bounce(position, velocity, lowSpin, landingAngle, _fairway);
            var highSpinResult = GroundPhysics.Bounce(position, velocity, highSpin, landingAngle, _fairway);

            // Assert - High spin should have lower horizontal velocity
            float lowSpinHorizontal = new Vector2(lowSpinResult.Velocity.x, lowSpinResult.Velocity.z).magnitude;
            float highSpinHorizontal = new Vector2(highSpinResult.Velocity.x, highSpinResult.Velocity.z).magnitude;

            Assert.Less(highSpinHorizontal, lowSpinHorizontal,
                "High backspin should reduce horizontal velocity more than low spin");
        }

        [Test]
        [Description("Zero backspin should not apply spin braking")]
        public void Bounce_ZeroBackspin_NoSpinBraking()
        {
            // Arrange
            Vector3 position = new Vector3(100f, 0f, 0f);
            Vector3 velocity = new Vector3(10f, -10f, 0f);
            float backspin = 0f;
            float landingAngle = 45f;

            // Act
            var result = GroundPhysics.Bounce(position, velocity, backspin, landingAngle, _fairway);

            // Assert - Should still have reasonable horizontal velocity
            float horizontalSpeed = new Vector2(result.Velocity.x, result.Velocity.z).magnitude;
            Assert.Greater(horizontalSpeed, 0f, "Zero spin bounce should maintain some forward velocity");
        }

        #endregion

        #region Landing Angle Effect Tests

        [Test]
        [Description("Steeper landing angle should increase friction effect")]
        public void Bounce_SteeperAngle_MoreFrictionEffect()
        {
            // Arrange
            Vector3 position = new Vector3(100f, 0f, 0f);
            float backspin = 5000f;

            // Shallow angle (driver-like) - mostly horizontal velocity
            Vector3 shallowVel = new Vector3(20f, -8f, 0f);
            float shallowAngle = 22f; // degrees

            // Steep angle (wedge-like) - more vertical velocity
            Vector3 steepVel = new Vector3(10f, -15f, 0f);
            float steepAngle = 56f; // degrees

            // Act
            var shallowResult = GroundPhysics.Bounce(position, shallowVel, backspin, shallowAngle, _fairway);
            var steepResult = GroundPhysics.Bounce(position, steepVel, backspin, steepAngle, _fairway);

            // Assert - Steep angle should have more friction effect
            // Comparing horizontal velocity as percentage of pre-bounce horizontal
            float shallowPreHorizontal = Mathf.Sqrt(shallowVel.x * shallowVel.x + shallowVel.z * shallowVel.z);
            float shallowPostHorizontal = Mathf.Sqrt(shallowResult.Velocity.x * shallowResult.Velocity.x +
                                                      shallowResult.Velocity.z * shallowResult.Velocity.z);
            float shallowRetained = shallowPostHorizontal / shallowPreHorizontal;

            float steepPreHorizontal = Mathf.Sqrt(steepVel.x * steepVel.x + steepVel.z * steepVel.z);
            float steepPostHorizontal = Mathf.Sqrt(steepResult.Velocity.x * steepResult.Velocity.x +
                                                    steepResult.Velocity.z * steepResult.Velocity.z);
            float steepRetained = steepPostHorizontal / steepPreHorizontal;

            Assert.Less(steepRetained, shallowRetained,
                "Steeper landing angle should retain less horizontal velocity");
        }

        #endregion

        #region Spin Reversal Tests

        [Test]
        [Description("Extreme backspin with steep angle should trigger spin reversal")]
        public void Bounce_ExtremeConditions_SpinReversalPossible()
        {
            // Arrange - Conditions that should trigger spin reversal
            Vector3 position = new Vector3(100f, 0f, 0f);
            Vector3 velocity = new Vector3(3f, -12f, 0f); // Low horizontal, steep descent
            float extremeBackspin = 10000f;  // Very high spin
            float steepAngle = 55f;

            // Act
            var result = GroundPhysics.Bounce(position, velocity, extremeBackspin, steepAngle, _fairway);

            // Assert - Ball should either nearly stop or reverse
            // Forward velocity should be very low or negative
            Assert.LessOrEqual(result.Velocity.x, 1f,
                "Extreme backspin with steep angle should nearly stop or reverse horizontal velocity");
        }

        [Test]
        [Description("Spin reversal flag should be set when ball checks hard")]
        public void Bounce_SpinReversalConditions_FlagSet()
        {
            // Arrange
            Vector3 position = new Vector3(100f, 0f, 0f);
            Vector3 velocity = new Vector3(2f, -15f, 0f);
            float extremeBackspin = 11000f;
            float steepAngle = 52f;

            // Act
            var result = GroundPhysics.Bounce(position, velocity, extremeBackspin, steepAngle, _fairway);

            // Assert - SpinReversed flag should indicate check/reversal occurred
            Assert.IsTrue(result.SpinReversed || result.Velocity.x <= 1f,
                "Extreme conditions should either set spin reversal flag or nearly stop the ball");
        }

        [Test]
        [Description("Normal conditions should not trigger spin reversal")]
        public void Bounce_NormalConditions_NoSpinReversal()
        {
            // Arrange - Normal driver-like conditions
            Vector3 position = new Vector3(100f, 0f, 0f);
            Vector3 velocity = new Vector3(25f, -10f, 0f);
            float normalBackspin = 2500f;
            float shallowAngle = 25f;

            // Act
            var result = GroundPhysics.Bounce(position, velocity, normalBackspin, shallowAngle, _fairway);

            // Assert
            Assert.IsFalse(result.SpinReversed,
                "Normal conditions should not trigger spin reversal");
            Assert.Greater(result.Velocity.x, 5f,
                "Normal bounce should maintain significant forward velocity");
        }

        #endregion

        #region Post-Bounce Spin Tests

        [Test]
        [Description("Post-bounce spin should be reduced from impact")]
        public void Bounce_SpinReduction_SpinDecreases()
        {
            // Arrange
            Vector3 position = new Vector3(100f, 0f, 0f);
            Vector3 velocity = new Vector3(15f, -12f, 0f);
            float initialBackspin = 7000f;
            float landingAngle = 45f;

            // Act
            var result = GroundPhysics.Bounce(position, velocity, initialBackspin, landingAngle, _fairway);

            // Assert
            Assert.Less(result.Spin, initialBackspin,
                "Post-bounce spin should be less than initial spin");
            Assert.Greater(result.Spin, 0f,
                "Post-bounce spin should still be positive");
        }

        [Test]
        [Description("Rough surface should absorb more spin than fairway")]
        public void Bounce_RoughSurface_MoreSpinAbsorption()
        {
            // Arrange
            Vector3 position = new Vector3(100f, 0f, 0f);
            Vector3 velocity = new Vector3(15f, -12f, 0f);
            float backspin = 7000f;
            float landingAngle = 45f;

            // Act
            var fairwayResult = GroundPhysics.Bounce(position, velocity, backspin, landingAngle, _fairway);
            var roughResult = GroundPhysics.Bounce(position, velocity, backspin, landingAngle, _rough);

            // Assert
            Assert.Less(roughResult.Spin, fairwayResult.Spin,
                "Rough should absorb more spin than fairway");
        }

        #endregion

        #region Shot Type Behavior Tests

        [Test]
        [Description("Driver shot should bounce and roll significantly")]
        public void Bounce_DriverConditions_SignificantBounce()
        {
            // Arrange - Typical driver landing
            Vector3 position = new Vector3(250f, 0f, 0f);
            Vector3 velocity = new Vector3(30f, -15f, 0f); // Fast, shallow
            float driverBackspin = 2500f;
            float driverLandingAngle = 38f;

            // Act
            var result = GroundPhysics.Bounce(position, velocity, driverBackspin, driverLandingAngle, _fairway);

            // Assert - Should have significant forward velocity after bounce
            Assert.Greater(result.Velocity.x, 10f,
                "Driver should maintain significant forward velocity after bounce");
            Assert.Greater(result.Velocity.y, 2f,
                "Driver should bounce up noticeably");
            Assert.IsFalse(result.SpinReversed,
                "Driver should not trigger spin reversal");
        }

        [Test]
        [Description("7-Iron should have moderate check")]
        public void Bounce_SevenIronConditions_ModerateCheck()
        {
            // Arrange - Typical 7-iron landing
            Vector3 position = new Vector3(170f, 0f, 0f);
            Vector3 velocity = new Vector3(15f, -20f, 0f);
            float ironBackspin = 7000f;
            float ironLandingAngle = 48f;

            // Act
            var result = GroundPhysics.Bounce(position, velocity, ironBackspin, ironLandingAngle, _fairway);

            // Assert
            Assert.Less(result.Velocity.x, 10f,
                "7-Iron should check more than driver");
            Assert.Greater(result.Velocity.x, 2f,
                "7-Iron should still roll forward some");
        }

        [Test]
        [Description("Wedge shot should check hard with minimal roll")]
        public void Bounce_WedgeConditions_HardCheck()
        {
            // Arrange - Typical wedge landing
            Vector3 position = new Vector3(130f, 0f, 0f);
            Vector3 velocity = new Vector3(8f, -18f, 0f);
            float wedgeBackspin = 9500f;
            float wedgeLandingAngle = 52f;

            // Act
            var result = GroundPhysics.Bounce(position, velocity, wedgeBackspin, wedgeLandingAngle, _fairway);

            // Assert
            Assert.Less(result.Velocity.x, 5f,
                "Wedge should check hard with low forward velocity");
        }

        #endregion

        #region BounceResult Structure Tests

        [Test]
        [Description("BounceResult should return all required fields")]
        public void Bounce_ReturnsCompleteResult()
        {
            // Arrange
            Vector3 position = new Vector3(100f, 0f, 0f);
            Vector3 velocity = new Vector3(15f, -12f, 0f);
            float backspin = 5000f;
            float landingAngle = 45f;

            // Act
            var result = GroundPhysics.Bounce(position, velocity, backspin, landingAngle, _fairway);

            // Assert - All fields should be valid
            Assert.IsFalse(float.IsNaN(result.Position.x), "Position X should not be NaN");
            Assert.IsFalse(float.IsNaN(result.Velocity.x), "Velocity X should not be NaN");
            Assert.IsFalse(float.IsNaN(result.Spin), "Spin should not be NaN");
            Assert.GreaterOrEqual(result.Position.y, 0f, "Position Y should be at or above ground");
        }

        #endregion

        #region Backward Compatibility Tests

        [Test]
        [Description("Old Bounce signature should still work")]
        public void Bounce_OldSignature_StillWorks()
        {
            // Arrange
            Vector3 position = new Vector3(100f, 0f, 0f);
            Vector3 velocity = new Vector3(15f, -12f, 0f);
            float backspin = 5000f;

            // Act - Using old signature (without landing angle)
            var (newPos, newVel, newSpin) = GroundPhysics.Bounce(position, velocity, backspin, _fairway);

            // Assert
            Assert.IsFalse(float.IsNaN(newPos.x), "Position should be valid");
            Assert.IsFalse(float.IsNaN(newVel.x), "Velocity should be valid");
            Assert.GreaterOrEqual(newSpin, 0f, "Spin should be non-negative");
        }

        #endregion
    }
}
