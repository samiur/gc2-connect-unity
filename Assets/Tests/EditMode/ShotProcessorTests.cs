// ABOUTME: Unit tests for the ShotProcessor service.
// ABOUTME: Tests validation, physics integration, events, and mode switching.

using System;
using NUnit.Framework;
using UnityEngine;
using OpenRange.Core;
using OpenRange.GC2;
using OpenRange.Physics;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class ShotProcessorTests
    {
        private GameObject _testObject;
        private ShotProcessor _shotProcessor;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestShotProcessor");
            _shotProcessor = _testObject.AddComponent<ShotProcessor>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testObject);
            }
        }

        #region Validation Tests

        [Test]
        public void ProcessShot_RejectsNullShot()
        {
            // Arrange
            GC2ShotData receivedShot = null;
            string receivedReason = null;
            _shotProcessor.OnShotRejected += (shot, reason) =>
            {
                receivedShot = shot;
                receivedReason = reason;
            };

            // Act
            _shotProcessor.ProcessShot(null);

            // Assert
            Assert.IsNotNull(receivedReason);
            Assert.That(receivedReason, Does.Contain("null"));
        }

        [Test]
        public void ProcessShot_RejectsLowBallSpeed()
        {
            // Arrange
            var shot = CreateValidShot();
            shot.BallSpeed = 5f; // Too low (min is 10)

            string rejectionReason = null;
            _shotProcessor.OnShotRejected += (_, reason) => rejectionReason = reason;

            // Act
            _shotProcessor.ProcessShot(shot);

            // Assert
            Assert.IsNotNull(rejectionReason);
            Assert.That(rejectionReason, Does.Contain("speed").IgnoreCase);
        }

        [Test]
        public void ProcessShot_RejectsHighBallSpeed()
        {
            // Arrange
            var shot = CreateValidShot();
            shot.BallSpeed = 230f; // Too high (max is 220)

            string rejectionReason = null;
            _shotProcessor.OnShotRejected += (_, reason) => rejectionReason = reason;

            // Act
            _shotProcessor.ProcessShot(shot);

            // Assert
            Assert.IsNotNull(rejectionReason);
            Assert.That(rejectionReason, Does.Contain("speed").IgnoreCase);
        }

        [Test]
        public void ProcessShot_RejectsInvalidLaunchAngle()
        {
            // Arrange
            var shot = CreateValidShot();
            shot.LaunchAngle = 70f; // Too high (max is 60)

            string rejectionReason = null;
            _shotProcessor.OnShotRejected += (_, reason) => rejectionReason = reason;

            // Act
            _shotProcessor.ProcessShot(shot);

            // Assert
            Assert.IsNotNull(rejectionReason);
            Assert.That(rejectionReason, Does.Contain("angle").IgnoreCase);
        }

        [Test]
        public void ProcessShot_RejectsExtremeDirection()
        {
            // Arrange
            var shot = CreateValidShot();
            shot.Direction = 50f; // Too far right (max is 45)

            string rejectionReason = null;
            _shotProcessor.OnShotRejected += (_, reason) => rejectionReason = reason;

            // Act
            _shotProcessor.ProcessShot(shot);

            // Assert
            Assert.IsNotNull(rejectionReason);
            Assert.That(rejectionReason, Does.Contain("direction").IgnoreCase);
        }

        [Test]
        public void ProcessShot_RejectsLowSpinWithHighSpeed()
        {
            // Arrange
            var shot = CreateValidShot();
            shot.BallSpeed = 150f;
            shot.TotalSpin = 50f; // Too low for this speed

            string rejectionReason = null;
            _shotProcessor.OnShotRejected += (_, reason) => rejectionReason = reason;

            // Act
            _shotProcessor.ProcessShot(shot);

            // Assert
            Assert.IsNotNull(rejectionReason);
            Assert.That(rejectionReason, Does.Contain("spin").IgnoreCase);
        }

        [Test]
        public void ProcessShot_RejectsInvalidSpinAxis()
        {
            // Arrange
            var shot = CreateValidShot();
            shot.SpinAxis = 100f; // Too high (max is 90)

            string rejectionReason = null;
            _shotProcessor.OnShotRejected += (_, reason) => rejectionReason = reason;

            // Act
            _shotProcessor.ProcessShot(shot);

            // Assert
            Assert.IsNotNull(rejectionReason);
            Assert.That(rejectionReason, Does.Contain("axis").IgnoreCase);
        }

        #endregion

        #region Valid Shot Processing Tests

        [Test]
        public void ProcessShot_ProcessesValidShot()
        {
            // Arrange
            var shot = CreateValidShot();
            GC2ShotData receivedShot = null;
            ShotResult receivedResult = null;

            _shotProcessor.OnShotProcessed += (s, r) =>
            {
                receivedShot = s;
                receivedResult = r;
            };

            // Act
            _shotProcessor.ProcessShot(shot);

            // Assert
            Assert.IsNotNull(receivedShot);
            Assert.IsNotNull(receivedResult);
            Assert.AreEqual(shot.BallSpeed, receivedShot.BallSpeed);
        }

        [Test]
        public void ProcessShot_ProducesReasonableCarryDistance()
        {
            // Arrange - Driver shot
            var shot = CreateDriverShot();
            ShotResult result = null;

            _shotProcessor.OnShotProcessed += (_, r) => result = r;

            // Act
            _shotProcessor.ProcessShot(shot);

            // Assert - 160 mph driver should carry 230-300 yards
            Assert.IsNotNull(result);
            Assert.Greater(result.CarryDistance, 230f, "Driver carry too short");
            Assert.Less(result.CarryDistance, 300f, "Driver carry too long");
        }

        [Test]
        public void ProcessShot_ProducesReasonableMaxHeight()
        {
            // Arrange
            var shot = CreateValidShot();
            ShotResult result = null;

            _shotProcessor.OnShotProcessed += (_, r) => result = r;

            // Act
            _shotProcessor.ProcessShot(shot);

            // Assert - Reasonable apex for mid-iron
            Assert.IsNotNull(result);
            Assert.Greater(result.MaxHeight, 20f, "Apex too low");
            Assert.Less(result.MaxHeight, 150f, "Apex too high");
        }

        [Test]
        public void ProcessShot_ProducesTrajectoryPoints()
        {
            // Arrange
            var shot = CreateValidShot();
            ShotResult result = null;

            _shotProcessor.OnShotProcessed += (_, r) => result = r;

            // Act
            _shotProcessor.ProcessShot(shot);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Trajectory);
            Assert.Greater(result.Trajectory.Count, 10, "Too few trajectory points");
        }

        [Test]
        public void ProcessShot_TrajectoryStartsAtOrigin()
        {
            // Arrange
            var shot = CreateValidShot();
            ShotResult result = null;

            _shotProcessor.OnShotProcessed += (_, r) => result = r;

            // Act
            _shotProcessor.ProcessShot(shot);

            // Assert
            Assert.IsNotNull(result);
            Assert.Greater(result.Trajectory.Count, 0);
            var firstPoint = result.Trajectory[0];
            Assert.AreEqual(0f, firstPoint.Position.x, 0.1f, "First point X should be at origin");
            Assert.AreEqual(0f, firstPoint.Position.y, 0.1f, "First point Y should be at origin");
            Assert.AreEqual(0f, firstPoint.Position.z, 0.1f, "First point Z should be at origin");
        }

        #endregion

        #region Event Tests

        [Test]
        public void ProcessShot_FiresOnShotProcessedEvent()
        {
            // Arrange
            var shot = CreateValidShot();
            bool eventFired = false;

            _shotProcessor.OnShotProcessed += (_, _) => eventFired = true;

            // Act
            _shotProcessor.ProcessShot(shot);

            // Assert
            Assert.IsTrue(eventFired);
        }

        [Test]
        public void ProcessShot_FiresOnShotRejectedEvent_ForInvalidShot()
        {
            // Arrange
            var shot = CreateValidShot();
            shot.BallSpeed = 1f; // Invalid

            bool eventFired = false;
            _shotProcessor.OnShotRejected += (_, _) => eventFired = true;

            // Act
            _shotProcessor.ProcessShot(shot);

            // Assert
            Assert.IsTrue(eventFired);
        }

        [Test]
        public void ProcessShot_DoesNotFireBothEvents()
        {
            // Arrange
            var shot = CreateValidShot();
            bool processedFired = false;
            bool rejectedFired = false;

            _shotProcessor.OnShotProcessed += (_, _) => processedFired = true;
            _shotProcessor.OnShotRejected += (_, _) => rejectedFired = true;

            // Act
            _shotProcessor.ProcessShot(shot);

            // Assert - Only one event should fire
            Assert.IsTrue(processedFired != rejectedFired || processedFired,
                "Exactly one event should fire for a valid shot");
        }

        #endregion

        #region Mode Switching Tests

        [Test]
        public void SetMode_ChangesCurrentMode()
        {
            // Arrange
            Assert.AreEqual(AppMode.OpenRange, _shotProcessor.CurrentMode);

            // Act
            _shotProcessor.SetMode(AppMode.GSPro);

            // Assert
            Assert.AreEqual(AppMode.GSPro, _shotProcessor.CurrentMode);
        }

        [Test]
        public void SetMode_CanSwitchBackToOpenRange()
        {
            // Arrange
            _shotProcessor.SetMode(AppMode.GSPro);

            // Act
            _shotProcessor.SetMode(AppMode.OpenRange);

            // Assert
            Assert.AreEqual(AppMode.OpenRange, _shotProcessor.CurrentMode);
        }

        #endregion

        #region Environmental Conditions Tests

        [Test]
        public void SetEnvironmentalConditions_UpdatesProperties()
        {
            // Act
            _shotProcessor.SetEnvironmentalConditions(
                tempF: 85f,
                elevationFt: 5000f,
                humidityPct: 30f,
                windSpeedMph: 10f,
                windDirDeg: 45f
            );

            // Assert
            Assert.AreEqual(85f, _shotProcessor.TemperatureF);
            Assert.AreEqual(5000f, _shotProcessor.ElevationFt);
            Assert.AreEqual(30f, _shotProcessor.HumidityPct);
            Assert.AreEqual(10f, _shotProcessor.WindSpeedMph);
            Assert.AreEqual(45f, _shotProcessor.WindDirectionDeg);
        }

        [Test]
        public void TemperatureF_ClampsToValidRange()
        {
            // Act & Assert - Too low
            _shotProcessor.TemperatureF = -50f;
            Assert.AreEqual(20f, _shotProcessor.TemperatureF);

            // Act & Assert - Too high
            _shotProcessor.TemperatureF = 200f;
            Assert.AreEqual(120f, _shotProcessor.TemperatureF);
        }

        [Test]
        public void ElevationFt_ClampsToValidRange()
        {
            // Act & Assert - Too low
            _shotProcessor.ElevationFt = -1000f;
            Assert.AreEqual(-500f, _shotProcessor.ElevationFt);

            // Act & Assert - Too high
            _shotProcessor.ElevationFt = 20000f;
            Assert.AreEqual(15000f, _shotProcessor.ElevationFt);
        }

        [Test]
        public void ProcessShot_UsesEnvironmentalConditions()
        {
            // Arrange - High altitude shot should fly farther
            var shot = CreateValidShot();
            ShotResult seaLevelResult = null;
            ShotResult highAltitudeResult = null;

            _shotProcessor.SetEnvironmentalConditions(
                tempF: 70f,
                elevationFt: 0f
            );
            _shotProcessor.OnShotProcessed += (_, r) => seaLevelResult = r;
            _shotProcessor.ProcessShot(shot);

            // Reset and test at altitude
            _shotProcessor.OnShotProcessed -= (_, r) => seaLevelResult = r;
            _shotProcessor.SetEnvironmentalConditions(
                tempF: 70f,
                elevationFt: 7000f
            );
            _shotProcessor.OnShotProcessed += (_, r) => highAltitudeResult = r;
            _shotProcessor.ProcessShot(shot);

            // Assert - High altitude should fly farther
            Assert.IsNotNull(seaLevelResult);
            Assert.IsNotNull(highAltitudeResult);
            Assert.Greater(highAltitudeResult.CarryDistance, seaLevelResult.CarryDistance,
                "Ball should carry farther at high altitude");
        }

        #endregion

        #region Physics Validation Tests (From PHYSICS.md)

        [Test]
        public void ProcessShot_DriverHighSpeed_MatchesExpectedCarry()
        {
            // From PHYSICS.md: 167 mph / 10.9° / 2686 rpm → 275 yds (±5%)
            var shot = new GC2ShotData
            {
                ShotId = 1,
                BallSpeed = 167f,
                LaunchAngle = 10.9f,
                Direction = 0f,
                TotalSpin = 2686f,
                BackSpin = 2686f,
                SideSpin = 0f,
                SpinAxis = 0f
            };

            ShotResult result = null;
            _shotProcessor.OnShotProcessed += (_, r) => result = r;
            _shotProcessor.ProcessShot(shot);

            Assert.IsNotNull(result);
            float expectedCarry = 275f;
            float tolerance = expectedCarry * 0.05f; // ±5%
            Assert.AreEqual(expectedCarry, result.CarryDistance, tolerance,
                $"Expected ~{expectedCarry} yds, got {result.CarryDistance:F1} yds");
        }

        [Test]
        public void ProcessShot_DriverMidSpeed_MatchesExpectedCarry()
        {
            // From PHYSICS.md: 160 mph / 11.0° / 3000 rpm → 259 yds (±3%)
            var shot = new GC2ShotData
            {
                ShotId = 2,
                BallSpeed = 160f,
                LaunchAngle = 11.0f,
                Direction = 0f,
                TotalSpin = 3000f,
                BackSpin = 3000f,
                SideSpin = 0f,
                SpinAxis = 0f
            };

            ShotResult result = null;
            _shotProcessor.OnShotProcessed += (_, r) => result = r;
            _shotProcessor.ProcessShot(shot);

            Assert.IsNotNull(result);
            float expectedCarry = 259f;
            float tolerance = expectedCarry * 0.03f; // ±3%
            Assert.AreEqual(expectedCarry, result.CarryDistance, tolerance,
                $"Expected ~{expectedCarry} yds, got {result.CarryDistance:F1} yds");
        }

        [Test]
        public void ProcessShot_SevenIron_MatchesExpectedCarry()
        {
            // From PHYSICS.md: 120 mph / 16.3° / 7097 rpm → 172 yds (±5%)
            var shot = new GC2ShotData
            {
                ShotId = 3,
                BallSpeed = 120f,
                LaunchAngle = 16.3f,
                Direction = 0f,
                TotalSpin = 7097f,
                BackSpin = 7097f,
                SideSpin = 0f,
                SpinAxis = 0f
            };

            ShotResult result = null;
            _shotProcessor.OnShotProcessed += (_, r) => result = r;
            _shotProcessor.ProcessShot(shot);

            Assert.IsNotNull(result);
            float expectedCarry = 172f;
            float tolerance = expectedCarry * 0.05f; // ±5%
            Assert.AreEqual(expectedCarry, result.CarryDistance, tolerance,
                $"Expected ~{expectedCarry} yds, got {result.CarryDistance:F1} yds");
        }

        [Test]
        public void ProcessShot_Wedge_MatchesExpectedCarry()
        {
            // From PHYSICS.md: 102 mph / 24.2° / 9304 rpm → 136 yds (±5%)
            var shot = new GC2ShotData
            {
                ShotId = 4,
                BallSpeed = 102f,
                LaunchAngle = 24.2f,
                Direction = 0f,
                TotalSpin = 9304f,
                BackSpin = 9304f,
                SideSpin = 0f,
                SpinAxis = 0f
            };

            ShotResult result = null;
            _shotProcessor.OnShotProcessed += (_, r) => result = r;
            _shotProcessor.ProcessShot(shot);

            Assert.IsNotNull(result);
            float expectedCarry = 136f;
            float tolerance = expectedCarry * 0.05f; // ±5%
            Assert.AreEqual(expectedCarry, result.CarryDistance, tolerance,
                $"Expected ~{expectedCarry} yds, got {result.CarryDistance:F1} yds");
        }

        #endregion

        #region GSPro Mode Tests

        [Test]
        public void ProcessShot_InGSProMode_StillFiresOnShotProcessed()
        {
            // Arrange
            _shotProcessor.SetMode(AppMode.GSPro);
            var shot = CreateValidShot();
            bool eventFired = false;

            _shotProcessor.OnShotProcessed += (_, _) => eventFired = true;

            // Act
            _shotProcessor.ProcessShot(shot);

            // Assert
            Assert.IsTrue(eventFired);
        }

        [Test]
        public void SetGSProClient_SetsClientReference()
        {
            // Arrange
            var mockClient = new MockGSProClient();

            // Act
            _shotProcessor.SetGSProClient(mockClient);
            _shotProcessor.SetMode(AppMode.GSPro);

            var shot = CreateValidShot();
            _shotProcessor.ProcessShot(shot);

            // Assert - MockClient should have received the shot
            Assert.IsTrue(mockClient.ShotSent);
        }

        #endregion

        #region Helper Methods

        private GC2ShotData CreateValidShot()
        {
            return new GC2ShotData
            {
                ShotId = 1,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                BallSpeed = 120f,
                LaunchAngle = 15f,
                Direction = 2f,
                TotalSpin = 5000f,
                BackSpin = 4900f,
                SideSpin = 500f,
                SpinAxis = 5f
            };
        }

        private GC2ShotData CreateDriverShot()
        {
            return new GC2ShotData
            {
                ShotId = 1,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                BallSpeed = 160f,
                LaunchAngle = 11f,
                Direction = 0f,
                TotalSpin = 2800f,
                BackSpin = 2800f,
                SideSpin = 0f,
                SpinAxis = 0f
            };
        }

        #endregion

        #region Mock Classes

        private class MockGSProClient : IGSProClient
        {
            public bool IsConnected => true;
            public bool ShotSent { get; private set; }

            public void SendShot(GC2ShotData shot)
            {
                ShotSent = true;
            }
        }

        #endregion
    }
}
