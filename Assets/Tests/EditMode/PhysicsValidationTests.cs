// ABOUTME: Integration tests for physics engine validation against Nathan model.
// ABOUTME: Ensures carry distances match expected values within tolerance.

using NUnit.Framework;
using OpenRange.Physics;

namespace OpenRange.Tests.EditMode
{
    /// <summary>
    /// Physics validation tests verifying the trajectory simulator
    /// produces results that match the Nathan model within acceptable tolerance.
    /// </summary>
    [TestFixture]
    public class PhysicsValidationTests
    {
        private TrajectorySimulator _simulator;

        [SetUp]
        public void SetUp()
        {
            // Standard conditions from PHYSICS.md
            _simulator = new TrajectorySimulator(
                tempF: 70f,
                elevationFt: 0f,
                humidityPct: 50f
            );
        }

        #region Validation Test Cases from PHYSICS.md

        [Test]
        [Description("Driver high speed: 167 mph / 10.9° / 2686 rpm → 275 yds (±5%)")]
        public void Simulate_DriverHighSpeed_WithinTolerance()
        {
            // Arrange
            float ballSpeedMph = 167f;
            float launchAngle = 10.9f;
            float backspin = 2686f;
            float expectedCarry = 275f;
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
            AssertCarryWithinTolerance(result.CarryDistance, expectedCarry, tolerance);
        }

        [Test]
        [Description("Driver mid speed: 160 mph / 11.0° / 3000 rpm → 259 yds (±3%)")]
        public void Simulate_DriverMidSpeed_WithinTolerance()
        {
            // Arrange
            float ballSpeedMph = 160f;
            float launchAngle = 11.0f;
            float backspin = 3000f;
            float expectedCarry = 259f;
            float tolerance = 0.03f; // ±3%

            // Act
            var result = _simulator.Simulate(
                ballSpeedMph: ballSpeedMph,
                vlaDeg: launchAngle,
                hlaDeg: 0f,
                backspinRpm: backspin,
                sidespinRpm: 0f
            );

            // Assert
            AssertCarryWithinTolerance(result.CarryDistance, expectedCarry, tolerance);
        }

        [Test]
        [Description("7-Iron: 120 mph / 16.3° / 7097 rpm → 172 yds (±5%)")]
        public void Simulate_SevenIron_WithinTolerance()
        {
            // Arrange
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
            AssertCarryWithinTolerance(result.CarryDistance, expectedCarry, tolerance);
        }

        [Test]
        [Description("Wedge: 102 mph / 24.2° / 9304 rpm → 136 yds (±5%)")]
        public void Simulate_Wedge_WithinTolerance()
        {
            // Arrange
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
            AssertCarryWithinTolerance(result.CarryDistance, expectedCarry, tolerance);
        }

        #endregion

        #region Trajectory Integrity Tests

        [Test]
        public void Simulate_TrajectoryStartsAtOrigin()
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
            Assert.IsNotNull(result.Trajectory);
            Assert.Greater(result.Trajectory.Count, 0);

            var firstPoint = result.Trajectory[0];
            Assert.AreEqual(0f, firstPoint.Position.x, 0.1f, "First point X should be 0");
            Assert.AreEqual(0f, firstPoint.Position.y, 0.1f, "First point Y should be 0");
            Assert.AreEqual(0f, firstPoint.Position.z, 0.1f, "First point Z should be 0");
        }

        [Test]
        public void Simulate_TrajectoryEndsNearGround()
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
            Assert.IsNotNull(result.Trajectory);
            var lastPoint = result.Trajectory[result.Trajectory.Count - 1];
            Assert.LessOrEqual(lastPoint.Position.y, 1f, "Final height should be near ground");
        }

        [Test]
        public void Simulate_ApexHigherThanStartAndEnd()
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
            Assert.Greater(result.MaxHeight, 0f, "Max height should be positive");
            Assert.Greater(result.MaxHeight, result.Trajectory[0].Position.y, "Apex higher than start");
        }

        [Test]
        public void Simulate_FlightTimeIsPositive()
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
            Assert.Greater(result.FlightTime, 0f, "Flight time should be positive");
            Assert.Greater(result.TotalTime, result.FlightTime, "Total time should exceed flight time");
        }

        [Test]
        public void Simulate_RollDistanceIsNonNegative()
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
            Assert.GreaterOrEqual(result.RollDistance, 0f, "Roll distance should be non-negative");
            Assert.AreEqual(result.TotalDistance, result.CarryDistance + result.RollDistance, 0.1f,
                "Total = Carry + Roll");
        }

        #endregion

        #region Side Spin Tests

        [Test]
        public void Simulate_RightSideSpin_ProducesRightOffline()
        {
            // Arrange - Right sidespin (fade/slice)
            var result = _simulator.Simulate(
                ballSpeedMph: 150f,
                vlaDeg: 12f,
                hlaDeg: 0f,
                backspinRpm: 3000f,
                sidespinRpm: 1500f // Right sidespin
            );

            // Assert - Ball should end up right of center (positive Z)
            Assert.Greater(result.OfflineDistance, 0f, "Right sidespin should produce right offline");
        }

        [Test]
        public void Simulate_LeftSideSpin_ProducesLeftOffline()
        {
            // Arrange - Left sidespin (draw/hook)
            var result = _simulator.Simulate(
                ballSpeedMph: 150f,
                vlaDeg: 12f,
                hlaDeg: 0f,
                backspinRpm: 3000f,
                sidespinRpm: -1500f // Left sidespin
            );

            // Assert - Ball should end up left of center (negative Z)
            Assert.Less(result.OfflineDistance, 0f, "Left sidespin should produce left offline");
        }

        [Test]
        public void Simulate_NoSideSpin_StraightShot()
        {
            // Arrange - No sidespin, straight launch
            var result = _simulator.Simulate(
                ballSpeedMph: 150f,
                vlaDeg: 12f,
                hlaDeg: 0f,
                backspinRpm: 3000f,
                sidespinRpm: 0f
            );

            // Assert - Ball should be close to center (within 1 yard)
            Assert.AreEqual(0f, result.OfflineDistance, 1f, "No sidespin should be relatively straight");
        }

        #endregion

        #region Environmental Conditions Tests

        [Test]
        public void Simulate_HigherAltitude_LongerCarry()
        {
            // Arrange
            var seaLevel = new TrajectorySimulator(tempF: 70f, elevationFt: 0f);
            var denver = new TrajectorySimulator(tempF: 70f, elevationFt: 5280f); // Mile high

            // Act
            var seaLevelResult = seaLevel.Simulate(150f, 12f, 0f, 3000f, 0f);
            var denverResult = denver.Simulate(150f, 12f, 0f, 3000f, 0f);

            // Assert - Ball should carry farther at altitude due to thinner air
            Assert.Greater(denverResult.CarryDistance, seaLevelResult.CarryDistance,
                "Higher altitude should produce longer carry");
        }

        [Test]
        public void Simulate_HigherTemperature_LongerCarry()
        {
            // Arrange
            var cold = new TrajectorySimulator(tempF: 40f, elevationFt: 0f);
            var hot = new TrajectorySimulator(tempF: 100f, elevationFt: 0f);

            // Act
            var coldResult = cold.Simulate(150f, 12f, 0f, 3000f, 0f);
            var hotResult = hot.Simulate(150f, 12f, 0f, 3000f, 0f);

            // Assert - Ball should carry farther in hotter air due to lower density
            Assert.Greater(hotResult.CarryDistance, coldResult.CarryDistance,
                "Higher temperature should produce longer carry");
        }

        [Test]
        public void Simulate_Headwind_ShorterCarry()
        {
            // Arrange
            var noWind = new TrajectorySimulator(tempF: 70f, elevationFt: 0f, windSpeedMph: 0f);
            var headwind = new TrajectorySimulator(tempF: 70f, elevationFt: 0f, windSpeedMph: 15f, windDirDeg: 0f);

            // Act
            var noWindResult = noWind.Simulate(150f, 12f, 0f, 3000f, 0f);
            var headwindResult = headwind.Simulate(150f, 12f, 0f, 3000f, 0f);

            // Assert - Ball should carry shorter into a headwind
            Assert.Less(headwindResult.CarryDistance, noWindResult.CarryDistance,
                "Headwind should reduce carry");
        }

        [Test]
        public void Simulate_Tailwind_LongerCarry()
        {
            // Arrange
            var noWind = new TrajectorySimulator(tempF: 70f, elevationFt: 0f, windSpeedMph: 0f);
            var tailwind = new TrajectorySimulator(tempF: 70f, elevationFt: 0f, windSpeedMph: 15f, windDirDeg: 180f);

            // Act
            var noWindResult = noWind.Simulate(150f, 12f, 0f, 3000f, 0f);
            var tailwindResult = tailwind.Simulate(150f, 12f, 0f, 3000f, 0f);

            // Assert - Ball should carry farther with a tailwind
            Assert.Greater(tailwindResult.CarryDistance, noWindResult.CarryDistance,
                "Tailwind should increase carry");
        }

        #endregion

        #region Helper Methods

        private void AssertCarryWithinTolerance(float actual, float expected, float tolerancePercent)
        {
            float minExpected = expected * (1f - tolerancePercent);
            float maxExpected = expected * (1f + tolerancePercent);

            Assert.GreaterOrEqual(actual, minExpected,
                $"Carry {actual:F1} yds is below minimum {minExpected:F1} yds ({tolerancePercent:P0} tolerance)");
            Assert.LessOrEqual(actual, maxExpected,
                $"Carry {actual:F1} yds is above maximum {maxExpected:F1} yds ({tolerancePercent:P0} tolerance)");
        }

        #endregion
    }
}
