// ABOUTME: Unit tests for GC2Protocol parsing functionality.
// ABOUTME: Tests shot data parsing, device status parsing, and message type detection.

using NUnit.Framework;
using UnityEngine;
using OpenRange.GC2;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class GC2ProtocolTests
    {
        #region ParseDeviceStatus Tests

        [Test]
        public void ParseDeviceStatus_ValidReadyStatus_ReturnsCorrectStatus()
        {
            // Arrange
            string message = @"0M
FLAGS=7
BALLS=1
BALL1=198,206,12
";

            // Act
            var result = GC2Protocol.ParseDeviceStatus(message);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Value.IsReady);
            Assert.IsTrue(result.Value.BallDetected);
            Assert.AreEqual(7, result.Value.RawFlags);
            Assert.AreEqual(1, result.Value.BallCount);
        }

        [Test]
        public void ParseDeviceStatus_NotReadyStatus_ReturnsCorrectStatus()
        {
            // Arrange
            string message = @"0M
FLAGS=1
BALLS=1
BALL1=198,206,12
";

            // Act
            var result = GC2Protocol.ParseDeviceStatus(message);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Value.IsReady);
            Assert.IsTrue(result.Value.BallDetected);
            Assert.AreEqual(1, result.Value.RawFlags);
        }

        [Test]
        public void ParseDeviceStatus_NoBallDetected_ReturnsCorrectStatus()
        {
            // Arrange
            string message = @"0M
FLAGS=7
BALLS=0
";

            // Act
            var result = GC2Protocol.ParseDeviceStatus(message);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Value.IsReady);
            Assert.IsFalse(result.Value.BallDetected);
            Assert.AreEqual(0, result.Value.BallCount);
            Assert.IsNull(result.Value.BallPosition);
        }

        [Test]
        public void ParseDeviceStatus_WithBallPosition_ParsesPositionCorrectly()
        {
            // Arrange
            string message = @"0M
FLAGS=7
BALLS=1
BALL1=100.5,200.25,50.75
";

            // Act
            var result = GC2Protocol.ParseDeviceStatus(message);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Value.BallPosition);
            Assert.AreEqual(100.5f, result.Value.BallPosition.Value.x, 0.01f);
            Assert.AreEqual(200.25f, result.Value.BallPosition.Value.y, 0.01f);
            Assert.AreEqual(50.75f, result.Value.BallPosition.Value.z, 0.01f);
        }

        [Test]
        public void ParseDeviceStatus_NullMessage_ReturnsNull()
        {
            // Act
            var result = GC2Protocol.ParseDeviceStatus(null);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void ParseDeviceStatus_EmptyMessage_ReturnsNull()
        {
            // Act
            var result = GC2Protocol.ParseDeviceStatus("");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void ParseDeviceStatus_ShotMessage_ReturnsNull()
        {
            // Arrange - This is a shot message (0H), not status (0M)
            string message = @"0H
SHOT_ID=1
SPEED_MPH=145.2
ELEVATION_DEG=11.8
AZIMUTH_DEG=1.5
SPIN_RPM=2650
BACK_RPM=2480
SIDE_RPM=-320
";

            // Act
            var result = GC2Protocol.ParseDeviceStatus(message);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void ParseDeviceStatus_MissingFlags_ReturnsNull()
        {
            // Arrange - Missing FLAGS field
            string message = @"0M
BALLS=1
BALL1=198,206,12
";

            // Act
            var result = GC2Protocol.ParseDeviceStatus(message);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void ParseDeviceStatus_InvalidBallPosition_ReturnsNullPosition()
        {
            // Arrange
            string message = @"0M
FLAGS=7
BALLS=1
BALL1=invalid
";

            // Act
            var result = GC2Protocol.ParseDeviceStatus(message);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result.Value.BallPosition);
        }

        [Test]
        public void ParseDeviceStatus_PartialBallPosition_ReturnsNullPosition()
        {
            // Arrange
            string message = @"0M
FLAGS=7
BALLS=1
BALL1=100,200
";

            // Act
            var result = GC2Protocol.ParseDeviceStatus(message);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNull(result.Value.BallPosition);
        }

        [Test]
        public void ParseDeviceStatus_LeadingWhitespace_ParsesCorrectly()
        {
            // Arrange
            string message = @"   0M
FLAGS=7
BALLS=1
";

            // Act
            var result = GC2Protocol.ParseDeviceStatus(message);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Value.IsReady);
        }

        #endregion

        #region GetMessageType Tests

        [Test]
        public void GetMessageType_ShotMessage_ReturnsShot()
        {
            // Arrange
            string message = "0H\nSHOT_ID=1\nSPEED_MPH=145.2";

            // Act
            var result = GC2Protocol.GetMessageType(message);

            // Assert
            Assert.AreEqual(GC2MessageType.Shot, result);
        }

        [Test]
        public void GetMessageType_StatusMessage_ReturnsDeviceStatus()
        {
            // Arrange
            string message = "0M\nFLAGS=7\nBALLS=1";

            // Act
            var result = GC2Protocol.GetMessageType(message);

            // Assert
            Assert.AreEqual(GC2MessageType.DeviceStatus, result);
        }

        [Test]
        public void GetMessageType_UnknownMessage_ReturnsUnknown()
        {
            // Arrange
            string message = "SOME_OTHER_DATA=123";

            // Act
            var result = GC2Protocol.GetMessageType(message);

            // Assert
            Assert.AreEqual(GC2MessageType.Unknown, result);
        }

        [Test]
        public void GetMessageType_NullMessage_ReturnsUnknown()
        {
            // Act
            var result = GC2Protocol.GetMessageType(null);

            // Assert
            Assert.AreEqual(GC2MessageType.Unknown, result);
        }

        [Test]
        public void GetMessageType_EmptyMessage_ReturnsUnknown()
        {
            // Act
            var result = GC2Protocol.GetMessageType("");

            // Assert
            Assert.AreEqual(GC2MessageType.Unknown, result);
        }

        [Test]
        public void GetMessageType_LeadingWhitespace_ParsesCorrectly()
        {
            // Arrange
            string message = "   0H\nSHOT_ID=1";

            // Act
            var result = GC2Protocol.GetMessageType(message);

            // Assert
            Assert.AreEqual(GC2MessageType.Shot, result);
        }

        #endregion

        #region IsShotMessage / IsStatusMessage Tests

        [Test]
        public void IsShotMessage_ShotMessage_ReturnsTrue()
        {
            // Arrange
            string message = "0H\nSHOT_ID=1";

            // Act & Assert
            Assert.IsTrue(GC2Protocol.IsShotMessage(message));
        }

        [Test]
        public void IsShotMessage_StatusMessage_ReturnsFalse()
        {
            // Arrange
            string message = "0M\nFLAGS=7";

            // Act & Assert
            Assert.IsFalse(GC2Protocol.IsShotMessage(message));
        }

        [Test]
        public void IsStatusMessage_StatusMessage_ReturnsTrue()
        {
            // Arrange
            string message = "0M\nFLAGS=7";

            // Act & Assert
            Assert.IsTrue(GC2Protocol.IsStatusMessage(message));
        }

        [Test]
        public void IsStatusMessage_ShotMessage_ReturnsFalse()
        {
            // Arrange
            string message = "0H\nSHOT_ID=1";

            // Act & Assert
            Assert.IsFalse(GC2Protocol.IsStatusMessage(message));
        }

        #endregion

        #region Parse Shot Data Tests

        [Test]
        public void Parse_ValidShotData_ReturnsCorrectShot()
        {
            // Arrange
            string message = @"SHOT_ID=1
SPEED_MPH=145.2
ELEVATION_DEG=11.8
AZIMUTH_DEG=1.5
SPIN_RPM=2650
BACK_RPM=2480
SIDE_RPM=-320
HMT=0
";

            // Act
            var result = GC2Protocol.Parse(message);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.ShotId);
            Assert.AreEqual(145.2f, result.BallSpeed, 0.1f);
            Assert.AreEqual(11.8f, result.LaunchAngle, 0.1f);
            Assert.AreEqual(1.5f, result.Direction, 0.1f);
            Assert.AreEqual(2650f, result.TotalSpin, 1f);
            Assert.AreEqual(2480f, result.BackSpin, 1f);
            Assert.AreEqual(-320f, result.SideSpin, 1f);
            Assert.IsFalse(result.HasClubData);
        }

        [Test]
        public void Parse_WithHMTData_ReturnsClubData()
        {
            // Arrange
            string message = @"SHOT_ID=1
SPEED_MPH=150.5
ELEVATION_DEG=12.3
AZIMUTH_DEG=2.1
SPIN_RPM=2800
BACK_RPM=2650
SIDE_RPM=-400
CLUBSPEED_MPH=105.2
HPATH_DEG=3.1
VPATH_DEG=-4.2
FACE_T_DEG=1.5
LIE_DEG=0.5
LOFT_DEG=15.2
HMT=1
";

            // Act
            var result = GC2Protocol.Parse(message);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.HasClubData);
            Assert.AreEqual(105.2f, result.ClubSpeed, 0.1f);
            Assert.AreEqual(3.1f, result.Path, 0.1f);
            Assert.AreEqual(-4.2f, result.AttackAngle, 0.1f);
            Assert.AreEqual(1.5f, result.FaceToTarget, 0.1f);
            Assert.AreEqual(15.2f, result.DynamicLoft, 0.1f);
        }

        [Test]
        public void Parse_NullMessage_ReturnsNull()
        {
            // Act
            var result = GC2Protocol.Parse(null);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void Parse_EmptyMessage_ReturnsNull()
        {
            // Act
            var result = GC2Protocol.Parse("");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void Parse_MissingSpeedField_ReturnsNull()
        {
            // Arrange - Missing SPEED_MPH
            string message = @"SHOT_ID=1
ELEVATION_DEG=11.8
AZIMUTH_DEG=1.5
SPIN_RPM=2650
BACK_RPM=2480
SIDE_RPM=-320
";

            // Act
            var result = GC2Protocol.Parse(message);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region IsValidShot Tests

        [Test]
        public void IsValidShot_ValidShot_ReturnsTrue()
        {
            // Arrange
            var shot = new GC2ShotData
            {
                BallSpeed = 150f,
                LaunchAngle = 12f,
                Direction = 2f,
                TotalSpin = 2800f,
                BackSpin = 2650f,
                SideSpin = -400f,
                SpinAxis = 8f
            };

            // Act & Assert
            Assert.IsTrue(GC2Protocol.IsValidShot(shot));
        }

        [Test]
        public void IsValidShot_PuttMinimumSpeed_ReturnsTrue()
        {
            // Arrange - GC2 can track putts as slow as 1.1 mph
            var shot = new GC2ShotData
            {
                BallSpeed = 1.1f, // Minimum putt speed
                LaunchAngle = 2f, // Low launch for putt
                Direction = 0f,
                TotalSpin = 500f,
                BackSpin = 500f,
                SideSpin = 0f,
                SpinAxis = 0f
            };

            // Act & Assert
            Assert.IsTrue(GC2Protocol.IsValidShot(shot));
        }

        [Test]
        public void IsValidShot_SpeedTooLow_ReturnsFalse()
        {
            // Arrange
            var shot = new GC2ShotData
            {
                BallSpeed = 0.5f, // Below 1.1 mph minimum (putts can be as slow as 1.1)
                LaunchAngle = 12f,
                Direction = 2f,
                TotalSpin = 2800f,
                BackSpin = 2650f,
                SideSpin = -400f
            };

            // Act & Assert
            Assert.IsFalse(GC2Protocol.IsValidShot(shot));
        }

        [Test]
        public void IsValidShot_SpeedTooHigh_ReturnsFalse()
        {
            // Arrange
            var shot = new GC2ShotData
            {
                BallSpeed = 260f, // Above 250 mph maximum
                LaunchAngle = 12f,
                Direction = 2f,
                TotalSpin = 2800f,
                BackSpin = 2650f,
                SideSpin = -400f
            };

            // Act & Assert
            Assert.IsFalse(GC2Protocol.IsValidShot(shot));
        }

        [Test]
        public void IsValidShot_ZeroTotalSpin_ReturnsFalse()
        {
            // Arrange
            var shot = new GC2ShotData
            {
                BallSpeed = 150f,
                LaunchAngle = 12f,
                Direction = 2f,
                TotalSpin = 0f,
                BackSpin = 0f,
                SideSpin = 0f
            };

            // Act & Assert
            Assert.IsFalse(GC2Protocol.IsValidShot(shot));
        }

        [Test]
        public void IsValidShot_BackSpin2222_ReturnsFalse()
        {
            // Arrange - 2222 is a known GC2 error code
            var shot = new GC2ShotData
            {
                BallSpeed = 150f,
                LaunchAngle = 12f,
                Direction = 2f,
                TotalSpin = 2222f,
                BackSpin = 2222f,
                SideSpin = 0f
            };

            // Act & Assert
            Assert.IsFalse(GC2Protocol.IsValidShot(shot));
        }

        [Test]
        public void IsValidShot_DirectionTooExtreme_ReturnsFalse()
        {
            // Arrange
            var shot = new GC2ShotData
            {
                BallSpeed = 150f,
                LaunchAngle = 12f,
                Direction = 50f, // Over 45 degrees
                TotalSpin = 2800f,
                BackSpin = 2650f,
                SideSpin = -400f
            };

            // Act & Assert
            Assert.IsFalse(GC2Protocol.IsValidShot(shot));
        }

        #endregion

        #region Protocol Constants Tests

        [Test]
        public void ShotMessagePrefix_IsCorrect()
        {
            Assert.AreEqual("0H", GC2Protocol.ShotMessagePrefix);
        }

        [Test]
        public void StatusMessagePrefix_IsCorrect()
        {
            Assert.AreEqual("0M", GC2Protocol.StatusMessagePrefix);
        }

        #endregion
    }
}
