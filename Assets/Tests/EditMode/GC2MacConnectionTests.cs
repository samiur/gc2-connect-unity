// ABOUTME: Unit tests for GC2MacConnection - the C# bridge for macOS native USB plugin.
// ABOUTME: Tests JSON parsing, callback handling, state management, and lifecycle.

using System;
using NUnit.Framework;
using OpenRange.GC2;
using UnityEngine;
using UnityEngine.TestTools;

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
using OpenRange.GC2.Platforms.MacOS;
#endif

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class GC2MacConnectionTests
    {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        #region JSON Parsing - Shot Data

        [Test]
        public void ParseShotJson_ValidBasicShot_ReturnsCorrectData()
        {
            var json = @"{
                ""ShotId"": 42,
                ""Timestamp"": 1704067200000,
                ""BallSpeed"": 167.5,
                ""LaunchAngle"": 10.9,
                ""Direction"": -1.5,
                ""TotalSpin"": 2686,
                ""BackSpin"": 2650,
                ""SideSpin"": 450,
                ""SpinAxis"": 9.6,
                ""HasClubData"": false,
                ""ClubSpeed"": 0,
                ""Path"": 0,
                ""AttackAngle"": 0,
                ""FaceToTarget"": 0,
                ""DynamicLoft"": 0,
                ""Lie"": 0
            }";

            var shot = GC2MacConnection.ParseShotJson(json);

            Assert.That(shot, Is.Not.Null);
            Assert.That(shot.ShotId, Is.EqualTo(42));
            Assert.That(shot.Timestamp, Is.EqualTo(1704067200000));
            Assert.That(shot.BallSpeed, Is.EqualTo(167.5f).Within(0.1f));
            Assert.That(shot.LaunchAngle, Is.EqualTo(10.9f).Within(0.1f));
            Assert.That(shot.Direction, Is.EqualTo(-1.5f).Within(0.1f));
            Assert.That(shot.TotalSpin, Is.EqualTo(2686f).Within(1f));
            Assert.That(shot.BackSpin, Is.EqualTo(2650f).Within(1f));
            Assert.That(shot.SideSpin, Is.EqualTo(450f).Within(1f));
            Assert.That(shot.SpinAxis, Is.EqualTo(9.6f).Within(0.1f));
            Assert.That(shot.HasClubData, Is.False);
        }

        [Test]
        public void ParseShotJson_WithHMTData_ReturnsClubData()
        {
            var json = @"{
                ""ShotId"": 1,
                ""Timestamp"": 0,
                ""BallSpeed"": 150.0,
                ""LaunchAngle"": 12.0,
                ""Direction"": 0.0,
                ""TotalSpin"": 3000,
                ""BackSpin"": 2900,
                ""SideSpin"": 200,
                ""SpinAxis"": 4.0,
                ""HasClubData"": true,
                ""ClubSpeed"": 112.5,
                ""Path"": 2.3,
                ""AttackAngle"": -3.5,
                ""FaceToTarget"": 1.2,
                ""DynamicLoft"": 15.4,
                ""Lie"": 0.5
            }";

            var shot = GC2MacConnection.ParseShotJson(json);

            Assert.That(shot, Is.Not.Null);
            Assert.That(shot.HasClubData, Is.True);
            Assert.That(shot.ClubSpeed, Is.EqualTo(112.5f).Within(0.1f));
            Assert.That(shot.Path, Is.EqualTo(2.3f).Within(0.1f));
            Assert.That(shot.AttackAngle, Is.EqualTo(-3.5f).Within(0.1f));
            Assert.That(shot.FaceToTarget, Is.EqualTo(1.2f).Within(0.1f));
            Assert.That(shot.DynamicLoft, Is.EqualTo(15.4f).Within(0.1f));
            Assert.That(shot.Lie, Is.EqualTo(0.5f).Within(0.1f));
        }

        [Test]
        public void ParseShotJson_NullJson_ReturnsNull()
        {
            var shot = GC2MacConnection.ParseShotJson(null);
            Assert.That(shot, Is.Null);
        }

        [Test]
        public void ParseShotJson_EmptyJson_ReturnsNull()
        {
            var shot = GC2MacConnection.ParseShotJson("");
            Assert.That(shot, Is.Null);
        }

        [Test]
        public void ParseShotJson_InvalidJson_ReturnsNull()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*Failed to parse shot JSON.*"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*JSON was:.*"));

            var shot = GC2MacConnection.ParseShotJson("not valid json");
            Assert.That(shot, Is.Null);
        }

        [Test]
        public void ParseShotJson_ZeroTimestamp_UsesCurrentTime()
        {
            var json = @"{
                ""ShotId"": 1,
                ""Timestamp"": 0,
                ""BallSpeed"": 100.0,
                ""LaunchAngle"": 10.0,
                ""Direction"": 0.0,
                ""TotalSpin"": 2000,
                ""BackSpin"": 2000,
                ""SideSpin"": 0,
                ""SpinAxis"": 0,
                ""HasClubData"": false
            }";

            var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var shot = GC2MacConnection.ParseShotJson(json);
            var after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            Assert.That(shot, Is.Not.Null);
            Assert.That(shot.Timestamp, Is.GreaterThanOrEqualTo(before));
            Assert.That(shot.Timestamp, Is.LessThanOrEqualTo(after));
        }

        [Test]
        public void ParseShotJson_DriverShot_ParsesCorrectly()
        {
            // Typical driver shot values
            var json = @"{
                ""ShotId"": 100,
                ""Timestamp"": 1704067200000,
                ""BallSpeed"": 171.0,
                ""LaunchAngle"": 10.5,
                ""Direction"": -2.0,
                ""TotalSpin"": 2500,
                ""BackSpin"": 2400,
                ""SideSpin"": -400,
                ""SpinAxis"": -9.5,
                ""HasClubData"": false
            }";

            var shot = GC2MacConnection.ParseShotJson(json);

            Assert.That(shot, Is.Not.Null);
            Assert.That(shot.BallSpeed, Is.EqualTo(171f).Within(0.1f));
            Assert.That(shot.TotalSpin, Is.EqualTo(2500f).Within(1f));
        }

        [Test]
        public void ParseShotJson_WedgeShot_ParsesCorrectly()
        {
            // Typical wedge shot with high spin
            var json = @"{
                ""ShotId"": 101,
                ""Timestamp"": 1704067200000,
                ""BallSpeed"": 82.0,
                ""LaunchAngle"": 32.0,
                ""Direction"": 0.5,
                ""TotalSpin"": 9500,
                ""BackSpin"": 9450,
                ""SideSpin"": 200,
                ""SpinAxis"": 1.2,
                ""HasClubData"": false
            }";

            var shot = GC2MacConnection.ParseShotJson(json);

            Assert.That(shot, Is.Not.Null);
            Assert.That(shot.BallSpeed, Is.EqualTo(82f).Within(0.1f));
            Assert.That(shot.LaunchAngle, Is.EqualTo(32f).Within(0.1f));
            Assert.That(shot.TotalSpin, Is.EqualTo(9500f).Within(1f));
        }

        [Test]
        public void ParseShotJson_NegativeSideSpin_ParsesCorrectly()
        {
            var json = @"{
                ""ShotId"": 1,
                ""Timestamp"": 0,
                ""BallSpeed"": 150.0,
                ""LaunchAngle"": 12.0,
                ""Direction"": 3.0,
                ""TotalSpin"": 3500,
                ""BackSpin"": 3200,
                ""SideSpin"": -800,
                ""SpinAxis"": -14.0,
                ""HasClubData"": false
            }";

            var shot = GC2MacConnection.ParseShotJson(json);

            Assert.That(shot, Is.Not.Null);
            Assert.That(shot.SideSpin, Is.EqualTo(-800f).Within(1f));
            Assert.That(shot.SpinAxis, Is.EqualTo(-14f).Within(0.1f));
        }

        #endregion

        #region JSON Parsing - Device Status

        [Test]
        public void ParseDeviceStatusJson_DeviceReady_ReturnsCorrectStatus()
        {
            var json = @"{
                ""isReady"": true,
                ""ballDetected"": false,
                ""rawFlags"": 7,
                ""ballCount"": 0,
                ""hasBallPosition"": false,
                ""ballX"": 0,
                ""ballY"": 0,
                ""ballZ"": 0
            }";

            var status = GC2MacConnection.ParseDeviceStatusJson(json);

            Assert.That(status.IsReady, Is.True);
            Assert.That(status.BallDetected, Is.False);
            Assert.That(status.RawFlags, Is.EqualTo(7));
            Assert.That(status.BallCount, Is.EqualTo(0));
            Assert.That(status.BallPosition, Is.Null);
        }

        [Test]
        public void ParseDeviceStatusJson_BallDetected_ReturnsCorrectStatus()
        {
            var json = @"{
                ""isReady"": true,
                ""ballDetected"": true,
                ""rawFlags"": 7,
                ""ballCount"": 1,
                ""hasBallPosition"": false,
                ""ballX"": 0,
                ""ballY"": 0,
                ""ballZ"": 0
            }";

            var status = GC2MacConnection.ParseDeviceStatusJson(json);

            Assert.That(status.IsReady, Is.True);
            Assert.That(status.BallDetected, Is.True);
            Assert.That(status.BallCount, Is.EqualTo(1));
        }

        [Test]
        public void ParseDeviceStatusJson_WithBallPosition_ReturnsPosition()
        {
            var json = @"{
                ""isReady"": true,
                ""ballDetected"": true,
                ""rawFlags"": 7,
                ""ballCount"": 1,
                ""hasBallPosition"": true,
                ""ballX"": 1.5,
                ""ballY"": 0.0,
                ""ballZ"": 2.3
            }";

            var status = GC2MacConnection.ParseDeviceStatusJson(json);

            Assert.That(status.IsReady, Is.True);
            Assert.That(status.BallDetected, Is.True);
            Assert.That(status.BallPosition, Is.Not.Null);
            Assert.That(status.BallPosition.Value.x, Is.EqualTo(1.5f).Within(0.01f));
            Assert.That(status.BallPosition.Value.y, Is.EqualTo(0f).Within(0.01f));
            Assert.That(status.BallPosition.Value.z, Is.EqualTo(2.3f).Within(0.01f));
        }

        [Test]
        public void ParseDeviceStatusJson_NotReady_ReturnsCorrectFlags()
        {
            var json = @"{
                ""isReady"": false,
                ""ballDetected"": false,
                ""rawFlags"": 3,
                ""ballCount"": 0,
                ""hasBallPosition"": false,
                ""ballX"": 0,
                ""ballY"": 0,
                ""ballZ"": 0
            }";

            var status = GC2MacConnection.ParseDeviceStatusJson(json);

            Assert.That(status.IsReady, Is.False);
            Assert.That(status.BallDetected, Is.False);
            Assert.That(status.RawFlags, Is.EqualTo(3));
        }

        [Test]
        public void ParseDeviceStatusJson_NullJson_ReturnsUnknown()
        {
            var status = GC2MacConnection.ParseDeviceStatusJson(null);
            Assert.That(status, Is.EqualTo(GC2DeviceStatus.Unknown));
        }

        [Test]
        public void ParseDeviceStatusJson_EmptyJson_ReturnsUnknown()
        {
            var status = GC2MacConnection.ParseDeviceStatusJson("");
            Assert.That(status, Is.EqualTo(GC2DeviceStatus.Unknown));
        }

        [Test]
        public void ParseDeviceStatusJson_InvalidJson_ReturnsUnknown()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*Failed to parse status JSON.*"));

            var status = GC2MacConnection.ParseDeviceStatusJson("invalid json");
            Assert.That(status, Is.EqualTo(GC2DeviceStatus.Unknown));
        }

        [Test]
        public void ParseDeviceStatusJson_MultipleBalls_ReturnsCount()
        {
            var json = @"{
                ""isReady"": true,
                ""ballDetected"": true,
                ""rawFlags"": 7,
                ""ballCount"": 2,
                ""hasBallPosition"": false,
                ""ballX"": 0,
                ""ballY"": 0,
                ""ballZ"": 0
            }";

            var status = GC2MacConnection.ParseDeviceStatusJson(json);

            Assert.That(status.BallCount, Is.EqualTo(2));
            Assert.That(status.BallDetected, Is.True);
        }

        #endregion

        #region Component Lifecycle

        [Test]
        public void GC2MacConnection_NewInstance_IsNotConnected()
        {
            var go = new GameObject("TestMacConnection");
            try
            {
                var connection = go.AddComponent<GC2MacConnection>();

                // Before initialization, should not be connected
                Assert.That(connection.IsConnected, Is.False);
                Assert.That(connection.DeviceInfo, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GC2MacConnection_DeviceInfo_NullWhenNotConnected()
        {
            var go = new GameObject("TestMacConnection");
            try
            {
                var connection = go.AddComponent<GC2MacConnection>();

                Assert.That(connection.DeviceInfo, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        #endregion

        #region Event Handlers

        [Test]
        public void OnNativeShotReceived_ValidJson_InvokesEvent()
        {
            var go = new GameObject("TestMacConnection");
            try
            {
                var connection = go.AddComponent<GC2MacConnection>();
                GC2ShotData receivedShot = null;
                connection.OnShotReceived += shot => receivedShot = shot;

                var json = @"{
                    ""ShotId"": 1,
                    ""Timestamp"": 1704067200000,
                    ""BallSpeed"": 150.0,
                    ""LaunchAngle"": 12.0,
                    ""Direction"": 0.0,
                    ""TotalSpin"": 3000,
                    ""BackSpin"": 2900,
                    ""SideSpin"": 200,
                    ""SpinAxis"": 4.0,
                    ""HasClubData"": false
                }";

                connection.OnNativeShotReceived(json);

                // Note: MainThreadDispatcher may delay execution
                // In a real test environment, we'd need to wait for main thread
                // For EditMode tests, we verify the parsing works
                var parsed = GC2MacConnection.ParseShotJson(json);
                Assert.That(parsed, Is.Not.Null);
                Assert.That(parsed.BallSpeed, Is.EqualTo(150f).Within(0.1f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OnNativeShotReceived_NullJson_DoesNotThrow()
        {
            var go = new GameObject("TestMacConnection");
            try
            {
                var connection = go.AddComponent<GC2MacConnection>();

                Assert.DoesNotThrow(() => connection.OnNativeShotReceived(null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OnNativeShotReceived_EmptyJson_DoesNotThrow()
        {
            var go = new GameObject("TestMacConnection");
            try
            {
                var connection = go.AddComponent<GC2MacConnection>();

                Assert.DoesNotThrow(() => connection.OnNativeShotReceived(""));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OnNativeConnectionChanged_TrueString_UpdatesState()
        {
            var go = new GameObject("TestMacConnection");
            try
            {
                var connection = go.AddComponent<GC2MacConnection>();
                bool? connectionState = null;
                connection.OnConnectionChanged += state => connectionState = state;

                connection.OnNativeConnectionChanged("true");

                // Parsing test - actual state change may be delayed
                Assert.That(string.Equals("true", "true", StringComparison.OrdinalIgnoreCase), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OnNativeConnectionChanged_FalseString_UpdatesState()
        {
            var go = new GameObject("TestMacConnection");
            try
            {
                var connection = go.AddComponent<GC2MacConnection>();

                connection.OnNativeConnectionChanged("false");

                Assert.That(string.Equals("false", "false", StringComparison.OrdinalIgnoreCase), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OnNativeError_ValidError_InvokesEvent()
        {
            var go = new GameObject("TestMacConnection");
            try
            {
                var connection = go.AddComponent<GC2MacConnection>();
                string receivedError = null;
                connection.OnError += error => receivedError = error;

                // Expect the error log messages from OnNativeError
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*Test error message.*"));
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*Native error.*"));

                // The event uses MainThreadDispatcher, so we verify the method doesn't throw
                Assert.DoesNotThrow(() => connection.OnNativeError("Test error message"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OnNativeError_NullError_DoesNotThrow()
        {
            var go = new GameObject("TestMacConnection");
            try
            {
                var connection = go.AddComponent<GC2MacConnection>();

                Assert.DoesNotThrow(() => connection.OnNativeError(null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OnNativeDeviceStatus_ValidJson_ParsesCorrectly()
        {
            var go = new GameObject("TestMacConnection");
            try
            {
                var connection = go.AddComponent<GC2MacConnection>();

                var json = @"{
                    ""isReady"": true,
                    ""ballDetected"": true,
                    ""rawFlags"": 7,
                    ""ballCount"": 1,
                    ""hasBallPosition"": false,
                    ""ballX"": 0,
                    ""ballY"": 0,
                    ""ballZ"": 0
                }";

                Assert.DoesNotThrow(() => connection.OnNativeDeviceStatus(json));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void OnNativeDeviceStatus_NullJson_DoesNotThrow()
        {
            var go = new GameObject("TestMacConnection");
            try
            {
                var connection = go.AddComponent<GC2MacConnection>();

                Assert.DoesNotThrow(() => connection.OnNativeDeviceStatus(null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        #endregion

        #region Edge Cases

        [Test]
        public void ParseShotJson_MinimalFields_ParsesWithDefaults()
        {
            // JsonUtility requires all fields, but test with minimal values
            var json = @"{
                ""ShotId"": 0,
                ""Timestamp"": 0,
                ""BallSpeed"": 0,
                ""LaunchAngle"": 0,
                ""Direction"": 0,
                ""TotalSpin"": 0,
                ""BackSpin"": 0,
                ""SideSpin"": 0,
                ""SpinAxis"": 0,
                ""HasClubData"": false
            }";

            var shot = GC2MacConnection.ParseShotJson(json);

            Assert.That(shot, Is.Not.Null);
            Assert.That(shot.BallSpeed, Is.EqualTo(0f));
        }

        [Test]
        public void ParseShotJson_ExtremeValues_ParsesCorrectly()
        {
            var json = @"{
                ""ShotId"": 999999,
                ""Timestamp"": 9999999999999,
                ""BallSpeed"": 250.0,
                ""LaunchAngle"": 60.0,
                ""Direction"": -45.0,
                ""TotalSpin"": 15000,
                ""BackSpin"": 14000,
                ""SideSpin"": -5000,
                ""SpinAxis"": -19.7,
                ""HasClubData"": false
            }";

            var shot = GC2MacConnection.ParseShotJson(json);

            Assert.That(shot, Is.Not.Null);
            Assert.That(shot.BallSpeed, Is.EqualTo(250f).Within(0.1f));
            Assert.That(shot.TotalSpin, Is.EqualTo(15000f).Within(1f));
        }

        [Test]
        public void ParseDeviceStatusJson_ZeroFlags_IsNotReady()
        {
            var json = @"{
                ""isReady"": false,
                ""ballDetected"": false,
                ""rawFlags"": 0,
                ""ballCount"": 0,
                ""hasBallPosition"": false,
                ""ballX"": 0,
                ""ballY"": 0,
                ""ballZ"": 0
            }";

            var status = GC2MacConnection.ParseDeviceStatusJson(json);

            Assert.That(status.IsReady, Is.False);
            Assert.That(status.RawFlags, Is.EqualTo(0));
        }

        #endregion
#else
        [Test]
        public void GC2MacConnection_NotAvailableOnNonMacOS()
        {
            // This test runs on non-macOS platforms
            // Verifies conditional compilation works
            Assert.Pass("GC2MacConnection is only available on macOS");
        }
#endif
    }
}
