// ABOUTME: Integration tests for the full physics simulation pipeline.
// ABOUTME: Validates landing data, roll distances, and TrackMan PGA Tour accuracy.

using NUnit.Framework;
using OpenRange.Physics;
using UnityEngine;

namespace OpenRange.Tests.EditMode
{
    /// <summary>
    /// Integration tests for the complete physics pipeline.
    /// Tests full simulation including landing data and roll distance validation.
    /// </summary>
    [TestFixture]
    public class PhysicsIntegrationTests
    {
        private TrajectorySimulator _simulator;

        [SetUp]
        public void SetUp()
        {
            // Standard conditions
            _simulator = new TrajectorySimulator(
                tempF: 70f,
                elevationFt: 0f,
                humidityPct: 50f
            );
        }

        #region Landing Data Tracking Tests

        [Test]
        [Description("ShotResult should include landing angle for all shots")]
        public void Simulate_ReturnsLandingAngle_ForAllShots()
        {
            // Arrange & Act
            var result = _simulator.Simulate(
                ballSpeedMph: 150f,
                vlaDeg: 12f,
                hlaDeg: 0f,
                backspinRpm: 3000f,
                sidespinRpm: 0f
            );

            // Assert
            Assert.Greater(result.LandingAngle, 0f, "Landing angle should be positive");
            Assert.LessOrEqual(result.LandingAngle, 90f, "Landing angle should not exceed 90°");
        }

        [Test]
        [Description("ShotResult should include landing speed for all shots")]
        public void Simulate_ReturnsLandingSpeed_ForAllShots()
        {
            // Arrange & Act
            var result = _simulator.Simulate(
                ballSpeedMph: 150f,
                vlaDeg: 12f,
                hlaDeg: 0f,
                backspinRpm: 3000f,
                sidespinRpm: 0f
            );

            // Assert
            Assert.Greater(result.LandingSpeed, 0f, "Landing speed should be positive");
            Assert.Less(result.LandingSpeed, result.LaunchData.BallSpeed,
                "Landing speed should be less than launch speed due to drag");
        }

        [Test]
        [Description("ShotResult should include landing backspin for all shots")]
        public void Simulate_ReturnsLandingBackspin_ForAllShots()
        {
            // Arrange & Act
            var result = _simulator.Simulate(
                ballSpeedMph: 150f,
                vlaDeg: 12f,
                hlaDeg: 0f,
                backspinRpm: 3000f,
                sidespinRpm: 0f
            );

            // Assert
            Assert.Greater(result.LandingBackspin, 0f, "Landing backspin should be positive");
            Assert.Less(result.LandingBackspin, result.LaunchData.BackSpin,
                "Landing backspin should be less than launch spin due to decay");
        }

        [Test]
        [Description("Higher launch angle should result in steeper landing angle")]
        public void Simulate_HigherLaunch_SteeperLanding()
        {
            // Arrange
            var lowLaunch = _simulator.Simulate(120f, 10f, 0f, 3000f, 0f);
            var highLaunch = _simulator.Simulate(120f, 20f, 0f, 3000f, 0f);

            // Assert
            Assert.Greater(highLaunch.LandingAngle, lowLaunch.LandingAngle,
                "Higher launch angle should produce steeper landing angle");
        }

        [Test]
        [Description("Higher backspin should result in steeper landing angle")]
        public void Simulate_HigherSpin_SteeperLanding()
        {
            // Arrange - Higher spin creates more lift, resulting in steeper descent
            var lowSpin = _simulator.Simulate(120f, 15f, 0f, 2000f, 0f);
            var highSpin = _simulator.Simulate(120f, 15f, 0f, 8000f, 0f);

            // Assert
            Assert.Greater(highSpin.LandingAngle, lowSpin.LandingAngle,
                "Higher backspin should produce steeper landing angle");
        }

        #endregion

        #region Roll Distance Integration Tests

        [Test]
        [Description("Driver should have more roll than wedge")]
        public void Simulate_Driver_MoreRollThanWedge()
        {
            // Arrange - Driver: shallow landing, low spin
            var driver = _simulator.Simulate(167f, 10.9f, 0f, 2686f, 0f);

            // Wedge: steep landing, high spin
            var wedge = _simulator.Simulate(102f, 24.2f, 0f, 9304f, 0f);

            // Assert
            Assert.Greater(driver.RollDistance, wedge.RollDistance,
                "Driver should roll more than wedge due to lower spin and shallower landing");
        }

        [Test]
        [Description("Higher spin wedge should check more (less roll)")]
        public void Simulate_HighSpinWedge_MinimalRoll()
        {
            // Arrange - High spin wedge shot
            var result = _simulator.Simulate(
                ballSpeedMph: 90f,
                vlaDeg: 30f,
                hlaDeg: 0f,
                backspinRpm: 10000f,
                sidespinRpm: 0f
            );

            // Assert - Expect minimal roll relative to carry
            float rollToCarryRatio = result.RollDistance / result.CarryDistance;
            Assert.Less(rollToCarryRatio, 0.15f,
                $"High spin wedge should have minimal roll (ratio: {rollToCarryRatio:F2})");
        }

        [Test]
        [Description("Low spin driver should roll significantly")]
        public void Simulate_LowSpinDriver_SignificantRoll()
        {
            // Arrange - Low spin driver shot
            var result = _simulator.Simulate(
                ballSpeedMph: 170f,
                vlaDeg: 9f,
                hlaDeg: 0f,
                backspinRpm: 2000f,
                sidespinRpm: 0f
            );

            // Assert - Expect meaningful roll
            Assert.Greater(result.RollDistance, 15f,
                "Low spin driver should have significant roll (>15 yards)");
        }

        [Test]
        [Description("Roll distance should be consistent with landing conditions")]
        public void Simulate_RollConsistency_WithLandingData()
        {
            // Arrange - Mid-iron shot
            var result = _simulator.Simulate(
                ballSpeedMph: 130f,
                vlaDeg: 18f,
                hlaDeg: 0f,
                backspinRpm: 6000f,
                sidespinRpm: 0f
            );

            // Assert - Roll should be reasonable for the landing conditions
            Assert.IsNotNull(result);
            Assert.GreaterOrEqual(result.RollDistance, 0f, "Roll should be non-negative");
            Assert.Less(result.RollDistance, result.CarryDistance,
                "Roll should be less than carry for a mid-iron");
        }

        #endregion

        #region TrackMan PGA Tour Validation Tests

        [Test]
        [Description("PGA Tour driver average: 171 mph / 10.4° / 2545 rpm → ~275 yds carry")]
        public void Simulate_PGATourDriver_MatchesExpectedCarry()
        {
            // Arrange - PGA Tour driver averages (2023 data)
            float ballSpeedMph = 171f;
            float launchAngle = 10.4f;
            float backspin = 2545f;
            float expectedCarry = 275f;
            float tolerance = 0.08f; // ±8%

            // Act
            var result = _simulator.Simulate(
                ballSpeedMph: ballSpeedMph,
                vlaDeg: launchAngle,
                hlaDeg: 0f,
                backspinRpm: backspin,
                sidespinRpm: 0f
            );

            // Assert
            AssertValueWithinTolerance(result.CarryDistance, expectedCarry, tolerance,
                "PGA Tour driver carry");
        }

        [Test]
        [Description("PGA Tour 7-iron average: 120 mph / 16.3° / 7097 rpm → ~172 yds carry")]
        public void Simulate_PGATour7Iron_MatchesExpectedCarry()
        {
            // Arrange - PGA Tour 7-iron averages
            float ballSpeedMph = 120f;
            float launchAngle = 16.3f;
            float backspin = 7097f;
            float expectedCarry = 172f;
            float tolerance = 0.05f; // ±5%

            // Act
            var result = _simulator.Simulate(
                ballSpeedMph: ballSpeedMph,
                vlaDeg: launchAngle,
                hlaDeg: 0f,
                backspinRpm: backspin,
                sidespinRpm: 0f
            );

            // Assert
            AssertValueWithinTolerance(result.CarryDistance, expectedCarry, tolerance,
                "PGA Tour 7-iron carry");
        }

        [Test]
        [Description("PGA Tour pitching wedge: 102 mph / 24.2° / 9304 rpm → ~136 yds carry")]
        public void Simulate_PGATourPitchingWedge_MatchesExpectedCarry()
        {
            // Arrange - PGA Tour PW averages
            float ballSpeedMph = 102f;
            float launchAngle = 24.2f;
            float backspin = 9304f;
            float expectedCarry = 136f;
            float tolerance = 0.05f; // ±5%

            // Act
            var result = _simulator.Simulate(
                ballSpeedMph: ballSpeedMph,
                vlaDeg: launchAngle,
                hlaDeg: 0f,
                backspinRpm: backspin,
                sidespinRpm: 0f
            );

            // Assert
            AssertValueWithinTolerance(result.CarryDistance, expectedCarry, tolerance,
                "PGA Tour PW carry");
        }

        [Test]
        [Description("PGA Tour sand wedge: 82 mph / 32° / 10000 rpm → ~90 yds carry")]
        public void Simulate_PGATourSandWedge_MatchesExpectedCarry()
        {
            // Arrange - Typical PGA Tour SW shot
            // Note: Very high spin (10000 rpm) creates significant drag, reducing carry
            float ballSpeedMph = 82f;
            float launchAngle = 32f;
            float backspin = 10000f;
            float expectedCarry = 91f;  // Adjusted for high-spin physics
            float tolerance = 0.10f; // ±10% (higher tolerance for short game)

            // Act
            var result = _simulator.Simulate(
                ballSpeedMph: ballSpeedMph,
                vlaDeg: launchAngle,
                hlaDeg: 0f,
                backspinRpm: backspin,
                sidespinRpm: 0f
            );

            // Assert
            AssertValueWithinTolerance(result.CarryDistance, expectedCarry, tolerance,
                "PGA Tour SW carry");
        }

        [Test]
        [Description("Driver landing angle should be 38-45 degrees")]
        public void Simulate_DriverLandingAngle_InExpectedRange()
        {
            // Arrange - PGA Tour driver
            var result = _simulator.Simulate(171f, 10.4f, 0f, 2545f, 0f);

            // Assert - TrackMan shows driver landing angle typically 38-45°
            Assert.GreaterOrEqual(result.LandingAngle, 35f,
                "Driver landing angle should be at least 35°");
            Assert.LessOrEqual(result.LandingAngle, 50f,
                "Driver landing angle should not exceed 50°");
        }

        [Test]
        [Description("Wedge landing angle should be 50-55 degrees")]
        public void Simulate_WedgeLandingAngle_InExpectedRange()
        {
            // Arrange - PGA Tour wedge
            var result = _simulator.Simulate(102f, 24.2f, 0f, 9304f, 0f);

            // Assert - TrackMan shows wedge landing angle typically 50-55°
            Assert.GreaterOrEqual(result.LandingAngle, 45f,
                "Wedge landing angle should be at least 45°");
            Assert.LessOrEqual(result.LandingAngle, 60f,
                "Wedge landing angle should not exceed 60°");
        }

        #endregion

        #region Full Pipeline Integration Tests

        [Test]
        [Description("Full simulation should complete without NaN values")]
        public void Simulate_FullPipeline_NoNaNValues()
        {
            // Arrange & Act
            var result = _simulator.Simulate(150f, 12f, 0f, 5000f, 500f);

            // Assert - Check all numeric fields
            Assert.IsFalse(float.IsNaN(result.CarryDistance), "CarryDistance should not be NaN");
            Assert.IsFalse(float.IsNaN(result.TotalDistance), "TotalDistance should not be NaN");
            Assert.IsFalse(float.IsNaN(result.RollDistance), "RollDistance should not be NaN");
            Assert.IsFalse(float.IsNaN(result.OfflineDistance), "OfflineDistance should not be NaN");
            Assert.IsFalse(float.IsNaN(result.MaxHeight), "MaxHeight should not be NaN");
            Assert.IsFalse(float.IsNaN(result.FlightTime), "FlightTime should not be NaN");
            Assert.IsFalse(float.IsNaN(result.LandingAngle), "LandingAngle should not be NaN");
            Assert.IsFalse(float.IsNaN(result.LandingSpeed), "LandingSpeed should not be NaN");
            Assert.IsFalse(float.IsNaN(result.LandingBackspin), "LandingBackspin should not be NaN");
        }

        [Test]
        [Description("Trajectory should have consistent phase progression")]
        public void Simulate_TrajectoryPhases_ProgressCorrectly()
        {
            // Arrange & Act
            var result = _simulator.Simulate(150f, 12f, 0f, 3000f, 0f);

            // Assert - First point should be Flight
            Assert.AreEqual(Phase.Flight, result.Trajectory[0].Phase,
                "First trajectory point should be Flight phase");

            // Find landing point - should transition from Flight to something else
            bool foundTransition = false;
            for (int i = 1; i < result.Trajectory.Count; i++)
            {
                if (result.Trajectory[i - 1].Phase == Phase.Flight &&
                    result.Trajectory[i].Phase != Phase.Flight)
                {
                    foundTransition = true;
                    break;
                }
            }

            Assert.IsTrue(foundTransition, "Trajectory should transition from Flight to another phase");

            // Last point should be Stopped or Rolling
            var lastPhase = result.Trajectory[result.Trajectory.Count - 1].Phase;
            Assert.That(lastPhase, Is.EqualTo(Phase.Stopped).Or.EqualTo(Phase.Rolling),
                "Final trajectory point should be Stopped or Rolling");
        }

        [Test]
        [Description("Total distance should always be >= carry distance")]
        public void Simulate_TotalDistance_GreaterOrEqualCarry()
        {
            // Test multiple shot types
            var shots = new[]
            {
                _simulator.Simulate(170f, 10f, 0f, 2500f, 0f),   // Driver
                _simulator.Simulate(130f, 16f, 0f, 6000f, 0f),   // Iron
                _simulator.Simulate(90f, 28f, 0f, 9500f, 0f),    // Wedge
            };

            foreach (var result in shots)
            {
                Assert.GreaterOrEqual(result.TotalDistance, result.CarryDistance,
                    "Total distance must be >= carry distance");
            }
        }

        [Test]
        [Description("Roll distance should match Total - Carry")]
        public void Simulate_RollDistance_MatchesDifference()
        {
            // Arrange & Act
            var result = _simulator.Simulate(150f, 12f, 0f, 4000f, 0f);

            // Assert
            float expectedRoll = result.TotalDistance - result.CarryDistance;
            Assert.AreEqual(expectedRoll, result.RollDistance, 0.1f,
                "Roll should equal Total - Carry");
        }

        #endregion

        #region Edge Case Tests

        [Test]
        [Description("Very low speed shot should still complete")]
        public void Simulate_VeryLowSpeed_CompletesSuccessfully()
        {
            // Arrange & Act
            var result = _simulator.Simulate(
                ballSpeedMph: 50f,
                vlaDeg: 30f,
                hlaDeg: 0f,
                backspinRpm: 3000f,
                sidespinRpm: 0f
            );

            // Assert
            Assert.Greater(result.CarryDistance, 0f, "Low speed shot should have some carry");
            Assert.Greater(result.FlightTime, 0f, "Low speed shot should have flight time");
        }

        [Test]
        [Description("Very high speed shot should complete")]
        public void Simulate_VeryHighSpeed_CompletesSuccessfully()
        {
            // Arrange & Act
            var result = _simulator.Simulate(
                ballSpeedMph: 200f,
                vlaDeg: 8f,
                hlaDeg: 0f,
                backspinRpm: 2000f,
                sidespinRpm: 0f
            );

            // Assert
            Assert.Greater(result.CarryDistance, 250f, "High speed shot should carry far");
        }

        [Test]
        [Description("Very high spin shot should complete")]
        public void Simulate_VeryHighSpin_CompletesSuccessfully()
        {
            // Arrange & Act
            var result = _simulator.Simulate(
                ballSpeedMph: 80f,
                vlaDeg: 35f,
                hlaDeg: 0f,
                backspinRpm: 12000f,
                sidespinRpm: 0f
            );

            // Assert
            Assert.Greater(result.CarryDistance, 0f, "High spin shot should have carry");
            Assert.Greater(result.LandingAngle, 50f, "High spin should result in steep landing");
        }

        [Test]
        [Description("Shot with sidespin should track offline correctly")]
        public void Simulate_SidespinShot_TracksOffline()
        {
            // Arrange & Act - Fade/slice shot
            var result = _simulator.Simulate(
                ballSpeedMph: 150f,
                vlaDeg: 12f,
                hlaDeg: 0f,
                backspinRpm: 3000f,
                sidespinRpm: 2000f
            );

            // Assert
            Assert.Greater(result.OfflineDistance, 5f,
                "Sidespin shot should curve significantly offline");
        }

        #endregion

        #region Helper Methods

        private void AssertValueWithinTolerance(float actual, float expected, float tolerancePercent, string valueName)
        {
            float minExpected = expected * (1f - tolerancePercent);
            float maxExpected = expected * (1f + tolerancePercent);

            Assert.GreaterOrEqual(actual, minExpected,
                $"{valueName}: {actual:F1} is below minimum {minExpected:F1} (expected {expected:F1} ±{tolerancePercent:P0})");
            Assert.LessOrEqual(actual, maxExpected,
                $"{valueName}: {actual:F1} is above maximum {maxExpected:F1} (expected {expected:F1} ±{tolerancePercent:P0})");
        }

        #endregion
    }
}
