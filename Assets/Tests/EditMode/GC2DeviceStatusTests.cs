// ABOUTME: Unit tests for GC2DeviceStatus struct.
// ABOUTME: Tests construction, equality, and status interpretation.

using NUnit.Framework;
using UnityEngine;
using OpenRange.GC2;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class GC2DeviceStatusTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_FlagsReady_SetsIsReadyTrue()
        {
            // Act
            var status = new GC2DeviceStatus(GC2DeviceStatus.FlagsReady, 0);

            // Assert
            Assert.IsTrue(status.IsReady);
            Assert.AreEqual(7, status.RawFlags);
        }

        [Test]
        public void Constructor_FlagsNotReady_SetsIsReadyFalse()
        {
            // Act
            var status = new GC2DeviceStatus(GC2DeviceStatus.FlagsNotReady, 0);

            // Assert
            Assert.IsFalse(status.IsReady);
            Assert.AreEqual(1, status.RawFlags);
        }

        [Test]
        public void Constructor_BallsGreaterThanZero_SetsBallDetectedTrue()
        {
            // Act
            var status = new GC2DeviceStatus(7, 1);

            // Assert
            Assert.IsTrue(status.BallDetected);
            Assert.AreEqual(1, status.BallCount);
        }

        [Test]
        public void Constructor_BallsZero_SetsBallDetectedFalse()
        {
            // Act
            var status = new GC2DeviceStatus(7, 0);

            // Assert
            Assert.IsFalse(status.BallDetected);
            Assert.AreEqual(0, status.BallCount);
        }

        [Test]
        public void Constructor_WithBallPosition_StoresPosition()
        {
            // Arrange
            var position = new Vector3(100, 200, 50);

            // Act
            var status = new GC2DeviceStatus(7, 1, position);

            // Assert
            Assert.IsNotNull(status.BallPosition);
            Assert.AreEqual(position, status.BallPosition.Value);
        }

        [Test]
        public void Constructor_WithoutBallPosition_PositionIsNull()
        {
            // Act
            var status = new GC2DeviceStatus(7, 1);

            // Assert
            Assert.IsNull(status.BallPosition);
        }

        #endregion

        #region Constants Tests

        [Test]
        public void FlagsReady_Is7()
        {
            Assert.AreEqual(7, GC2DeviceStatus.FlagsReady);
        }

        [Test]
        public void FlagsNotReady_Is1()
        {
            Assert.AreEqual(1, GC2DeviceStatus.FlagsNotReady);
        }

        #endregion

        #region Unknown Status Tests

        [Test]
        public void Unknown_IsNotReady()
        {
            // Act
            var status = GC2DeviceStatus.Unknown;

            // Assert
            Assert.IsFalse(status.IsReady);
        }

        [Test]
        public void Unknown_HasNoBall()
        {
            // Act
            var status = GC2DeviceStatus.Unknown;

            // Assert
            Assert.IsFalse(status.BallDetected);
            Assert.AreEqual(0, status.BallCount);
        }

        [Test]
        public void Unknown_HasNoPosition()
        {
            // Act
            var status = GC2DeviceStatus.Unknown;

            // Assert
            Assert.IsNull(status.BallPosition);
        }

        [Test]
        public void Unknown_HasZeroFlags()
        {
            // Act
            var status = GC2DeviceStatus.Unknown;

            // Assert
            Assert.AreEqual(0, status.RawFlags);
        }

        #endregion

        #region Equality Tests

        [Test]
        public void Equals_SameValues_ReturnsTrue()
        {
            // Arrange
            var status1 = new GC2DeviceStatus(7, 1, new Vector3(100, 200, 50));
            var status2 = new GC2DeviceStatus(7, 1, new Vector3(100, 200, 50));

            // Act & Assert
            Assert.IsTrue(status1.Equals(status2));
            Assert.IsTrue(status1 == status2);
        }

        [Test]
        public void Equals_DifferentFlags_ReturnsFalse()
        {
            // Arrange
            var status1 = new GC2DeviceStatus(7, 1);
            var status2 = new GC2DeviceStatus(1, 1);

            // Act & Assert
            Assert.IsFalse(status1.Equals(status2));
            Assert.IsTrue(status1 != status2);
        }

        [Test]
        public void Equals_DifferentBallCount_ReturnsFalse()
        {
            // Arrange
            var status1 = new GC2DeviceStatus(7, 1);
            var status2 = new GC2DeviceStatus(7, 0);

            // Act & Assert
            Assert.IsFalse(status1.Equals(status2));
        }

        [Test]
        public void Equals_DifferentPosition_ReturnsFalse()
        {
            // Arrange
            var status1 = new GC2DeviceStatus(7, 1, new Vector3(100, 200, 50));
            var status2 = new GC2DeviceStatus(7, 1, new Vector3(200, 300, 100));

            // Act & Assert
            Assert.IsFalse(status1.Equals(status2));
        }

        [Test]
        public void Equals_OneWithPosition_OneWithout_ReturnsFalse()
        {
            // Arrange
            var status1 = new GC2DeviceStatus(7, 1, new Vector3(100, 200, 50));
            var status2 = new GC2DeviceStatus(7, 1);

            // Act & Assert
            Assert.IsFalse(status1.Equals(status2));
        }

        [Test]
        public void Equals_Object_SameValues_ReturnsTrue()
        {
            // Arrange
            var status1 = new GC2DeviceStatus(7, 1);
            object status2 = new GC2DeviceStatus(7, 1);

            // Act & Assert
            Assert.IsTrue(status1.Equals(status2));
        }

        [Test]
        public void Equals_Object_DifferentType_ReturnsFalse()
        {
            // Arrange
            var status = new GC2DeviceStatus(7, 1);
            object other = "not a status";

            // Act & Assert
            Assert.IsFalse(status.Equals(other));
        }

        [Test]
        public void Equals_Object_Null_ReturnsFalse()
        {
            // Arrange
            var status = new GC2DeviceStatus(7, 1);

            // Act & Assert
            Assert.IsFalse(status.Equals(null));
        }

        #endregion

        #region GetHashCode Tests

        [Test]
        public void GetHashCode_SameValues_ReturnsSameHash()
        {
            // Arrange
            var status1 = new GC2DeviceStatus(7, 1, new Vector3(100, 200, 50));
            var status2 = new GC2DeviceStatus(7, 1, new Vector3(100, 200, 50));

            // Act & Assert
            Assert.AreEqual(status1.GetHashCode(), status2.GetHashCode());
        }

        [Test]
        public void GetHashCode_DifferentValues_ReturnsDifferentHash()
        {
            // Arrange
            var status1 = new GC2DeviceStatus(7, 1);
            var status2 = new GC2DeviceStatus(1, 0);

            // Act & Assert
            // Note: Different values don't guarantee different hash codes,
            // but for these significantly different values, it's likely
            Assert.AreNotEqual(status1.GetHashCode(), status2.GetHashCode());
        }

        #endregion

        #region ToString Tests

        [Test]
        public void ToString_Ready_ContainsReady()
        {
            // Arrange
            var status = new GC2DeviceStatus(7, 1);

            // Act
            string result = status.ToString();

            // Assert
            Assert.IsTrue(result.Contains("Ready"));
        }

        [Test]
        public void ToString_NotReady_ContainsNotReady()
        {
            // Arrange
            var status = new GC2DeviceStatus(1, 0);

            // Act
            string result = status.ToString();

            // Assert
            Assert.IsTrue(result.Contains("NotReady"));
        }

        [Test]
        public void ToString_BallDetected_ContainsBallInfo()
        {
            // Arrange
            var status = new GC2DeviceStatus(7, 1, new Vector3(100, 200, 50));

            // Act
            string result = status.ToString();

            // Assert
            Assert.IsTrue(result.Contains("Ball@"));
        }

        [Test]
        public void ToString_NoBall_ContainsNoBall()
        {
            // Arrange
            var status = new GC2DeviceStatus(7, 0);

            // Act
            string result = status.ToString();

            // Assert
            Assert.IsTrue(result.Contains("NoBall"));
        }

        [Test]
        public void ToString_ContainsFlags()
        {
            // Arrange
            var status = new GC2DeviceStatus(7, 1);

            // Act
            string result = status.ToString();

            // Assert
            Assert.IsTrue(result.Contains("FLAGS=7"));
        }

        [Test]
        public void ToString_ContainsBalls()
        {
            // Arrange
            var status = new GC2DeviceStatus(7, 1);

            // Act
            string result = status.ToString();

            // Assert
            Assert.IsTrue(result.Contains("BALLS=1"));
        }

        #endregion

        #region Operator Tests

        [Test]
        public void OperatorEquals_SameValues_ReturnsTrue()
        {
            // Arrange
            var status1 = new GC2DeviceStatus(7, 1);
            var status2 = new GC2DeviceStatus(7, 1);

            // Act & Assert
            Assert.IsTrue(status1 == status2);
        }

        [Test]
        public void OperatorNotEquals_DifferentValues_ReturnsTrue()
        {
            // Arrange
            var status1 = new GC2DeviceStatus(7, 1);
            var status2 = new GC2DeviceStatus(1, 0);

            // Act & Assert
            Assert.IsTrue(status1 != status2);
        }

        #endregion
    }
}
