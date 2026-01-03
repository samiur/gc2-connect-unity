// ABOUTME: Unit tests for GC2TCPConnection TCP communication.
// ABOUTME: Tests connection lifecycle, message formatting, parsing, and reconnection.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using OpenRange.GC2;
using OpenRange.GC2.Platforms.TCP;
using UnityEngine;

namespace OpenRange.Tests.EditMode
{
    /// <summary>
    /// Tests for GC2TCPConnection TCP communication.
    /// </summary>
    [TestFixture]
    public class GC2TCPConnectionTests
    {
        private GameObject _testObject;
        private GC2TCPConnection _connection;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestConnection");
            _connection = _testObject.AddComponent<GC2TCPConnection>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_connection != null)
            {
                _connection.Disconnect();
            }
            if (_testObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testObject);
            }
        }

        #region Initialization Tests

        [Test]
        public void Constructor_DefaultValues_AreCorrect()
        {
            Assert.That(_connection.IsConnected, Is.False);
            Assert.That(_connection.DeviceInfo, Is.Null);
            Assert.That(_connection.Mode, Is.EqualTo(TCPMode.Server));
            Assert.That(_connection.Port, Is.EqualTo(8888));
            Assert.That(_connection.Address, Is.EqualTo("127.0.0.1"));
        }

        [Test]
        public void SetConnectionParams_UpdatesHostAndPort()
        {
            _connection.SetConnectionParams("192.168.1.100", 9999);

            Assert.That(_connection.Address, Is.EqualTo("192.168.1.100"));
            Assert.That(_connection.Port, Is.EqualTo(9999));
        }

        [Test]
        public void SetConnectionParams_NullAddress_DefaultsToLocalhost()
        {
            _connection.SetConnectionParams(null, 9999);

            Assert.That(_connection.Address, Is.EqualTo("127.0.0.1"));
        }

        [Test]
        public void SetConnectionParams_InvalidPort_DefaultsTo8888()
        {
            _connection.SetConnectionParams("localhost", 0);

            Assert.That(_connection.Port, Is.EqualTo(8888));
        }

        [Test]
        public void SetMode_UpdatesMode()
        {
            _connection.SetMode(TCPMode.Client);

            Assert.That(_connection.Mode, Is.EqualTo(TCPMode.Client));
        }

        [Test]
        public void IsDeviceAvailable_WhenNotConnected_ReturnsFalse()
        {
            Assert.That(_connection.IsDeviceAvailable(), Is.False);
        }

        #endregion

        #region Message Formatting Tests

        [Test]
        public void FormatShotMessage_BasicShot_FormatsCorrectly()
        {
            var shot = new GC2ShotData
            {
                ShotId = 1,
                BallSpeed = 150.5f,
                LaunchAngle = 12.3f,
                Direction = -2.1f,
                TotalSpin = 3000f,
                BackSpin = 2800f,
                SideSpin = 500f,
                SpinAxis = 10f,
                HasClubData = false
            };

            string message = GC2TCPConnection.FormatShotMessage(shot);

            Assert.That(message, Does.StartWith("0H"));
            Assert.That(message, Does.Contain("SHOT_ID=1"));
            Assert.That(message, Does.Contain("SPEED_MPH=150.50"));
            Assert.That(message, Does.Contain("ELEVATION_DEG=12.30"));
            Assert.That(message, Does.Contain("AZIMUTH_DEG=-2.10"));
            Assert.That(message, Does.Contain("SPIN_RPM=3000"));
            Assert.That(message, Does.Contain("BACK_RPM=2800"));
            Assert.That(message, Does.Contain("SIDE_RPM=500"));
            Assert.That(message, Does.EndWith("\t"));
        }

        [Test]
        public void FormatShotMessage_WithHMTData_IncludesClubFields()
        {
            var shot = new GC2ShotData
            {
                ShotId = 2,
                BallSpeed = 160f,
                LaunchAngle = 11f,
                Direction = 0f,
                TotalSpin = 2500f,
                BackSpin = 2500f,
                SideSpin = 0f,
                HasClubData = true,
                ClubSpeed = 110f,
                Path = -2.5f,
                AttackAngle = -4f,
                FaceToTarget = 1.5f,
                DynamicLoft = 12f
            };

            string message = GC2TCPConnection.FormatShotMessage(shot);

            Assert.That(message, Does.Contain("HMT=1"));
            Assert.That(message, Does.Contain("CLUBSPEED_MPH=110.00"));
            Assert.That(message, Does.Contain("HPATH_DEG=-2.50"));
            Assert.That(message, Does.Contain("VPATH_DEG=-4.00"));
            Assert.That(message, Does.Contain("FACE_T_DEG=1.50"));
            Assert.That(message, Does.Contain("LOFT_DEG=12.00"));
        }

        [Test]
        public void FormatStatusMessage_Ready_FormatsCorrectly()
        {
            string message = GC2TCPConnection.FormatStatusMessage(true, true, new Vector3(10, 20, 100));

            Assert.That(message, Does.StartWith("0M"));
            Assert.That(message, Does.Contain("FLAGS=7"));
            Assert.That(message, Does.Contain("BALLS=1"));
            Assert.That(message, Does.Contain("BALL1=10.0,20.0,100.0"));
            Assert.That(message, Does.EndWith("\t"));
        }

        [Test]
        public void FormatStatusMessage_NotReady_FormatsCorrectly()
        {
            string message = GC2TCPConnection.FormatStatusMessage(false, false, null);

            Assert.That(message, Does.StartWith("0M"));
            Assert.That(message, Does.Contain("FLAGS=1"));
            Assert.That(message, Does.Contain("BALLS=0"));
            Assert.That(message, Does.Not.Contain("BALL1="));
        }

        #endregion

        #region Message Parsing Roundtrip Tests

        [Test]
        public void FormatAndParse_ShotMessage_RoundtripsCorrectly()
        {
            var originalShot = new GC2ShotData
            {
                ShotId = 42,
                BallSpeed = 167.5f,
                LaunchAngle = 10.9f,
                Direction = -1.5f,
                TotalSpin = 2686f,
                BackSpin = 2686f,
                SideSpin = -300f,
                SpinAxis = 6.4f,
                HasClubData = false
            };

            string message = GC2TCPConnection.FormatShotMessage(originalShot);
            var parsedShot = GC2Protocol.Parse(message);

            Assert.That(parsedShot, Is.Not.Null);
            Assert.That(parsedShot.ShotId, Is.EqualTo(42));
            Assert.That(parsedShot.BallSpeed, Is.EqualTo(167.5f).Within(0.01f));
            Assert.That(parsedShot.LaunchAngle, Is.EqualTo(10.9f).Within(0.01f));
            Assert.That(parsedShot.Direction, Is.EqualTo(-1.5f).Within(0.01f));
            Assert.That(parsedShot.BackSpin, Is.EqualTo(2686f).Within(1f));
            Assert.That(parsedShot.SideSpin, Is.EqualTo(-300f).Within(1f));
        }

        [Test]
        public void FormatAndParse_StatusMessage_RoundtripsCorrectly()
        {
            string message = GC2TCPConnection.FormatStatusMessage(true, true, new Vector3(5, 10, 150));
            var status = GC2Protocol.ParseDeviceStatus(message);

            Assert.That(status.HasValue, Is.True);
            Assert.That(status.Value.IsReady, Is.True);
            Assert.That(status.Value.BallDetected, Is.True);
            Assert.That(status.Value.BallPosition.HasValue, Is.True);
            Assert.That(status.Value.BallPosition.Value.x, Is.EqualTo(5f).Within(0.1f));
            Assert.That(status.Value.BallPosition.Value.y, Is.EqualTo(10f).Within(0.1f));
            Assert.That(status.Value.BallPosition.Value.z, Is.EqualTo(150f).Within(0.1f));
        }

        #endregion

        #region Disconnect Tests

        [Test]
        public void Disconnect_WhenNotConnected_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _connection.Disconnect());
        }

        [Test]
        public void Disconnect_ClearsConnectionState()
        {
            _connection.Disconnect();

            Assert.That(_connection.IsConnected, Is.False);
            Assert.That(_connection.IsDeviceAvailable(), Is.False);
        }

        #endregion

        #region DeviceInfo Tests

        [Test]
        public void DeviceInfo_WhenNotConnected_ReturnsNull()
        {
            Assert.That(_connection.DeviceInfo, Is.Null);
        }

        #endregion
    }

    /// <summary>
    /// Tests for GC2TCPListener server functionality.
    /// </summary>
    [TestFixture]
    public class GC2TCPListenerTests
    {
        private GC2TCPListener _listener;

        [SetUp]
        public void SetUp()
        {
            // Use a random port to avoid conflicts
            int port = UnityEngine.Random.Range(50000, 60000);
            _listener = new GC2TCPListener(port);
        }

        [TearDown]
        public void TearDown()
        {
            _listener?.Dispose();
        }

        [Test]
        public void Constructor_SetsPort()
        {
            var listener = new GC2TCPListener(9999);
            Assert.That(listener.Port, Is.EqualTo(9999));
            listener.Dispose();
        }

        [Test]
        public void Constructor_InvalidPort_UsesDefault()
        {
            var listener = new GC2TCPListener(0);
            Assert.That(listener.Port, Is.EqualTo(8888));
            listener.Dispose();
        }

        [Test]
        public void IsRunning_BeforeStart_IsFalse()
        {
            Assert.That(_listener.IsRunning, Is.False);
        }

        [Test]
        public void HasClient_BeforeStart_IsFalse()
        {
            Assert.That(_listener.HasClient, Is.False);
        }

        [Test]
        public void Start_StartsListening()
        {
            bool started = _listener.Start();

            Assert.That(started, Is.True);
            Assert.That(_listener.IsRunning, Is.True);
        }

        [Test]
        public void Start_WhenAlreadyRunning_ReturnsTrue()
        {
            _listener.Start();
            bool secondStart = _listener.Start();

            Assert.That(secondStart, Is.True);
            Assert.That(_listener.IsRunning, Is.True);
        }

        [Test]
        public void Stop_StopsListening()
        {
            _listener.Start();
            _listener.Stop();

            Assert.That(_listener.IsRunning, Is.False);
        }

        [Test]
        public void Stop_WhenNotRunning_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _listener.Stop());
        }

        [Test]
        public void Dispose_StopsListening()
        {
            _listener.Start();
            _listener.Dispose();

            Assert.That(_listener.IsRunning, Is.False);
        }

        [Test]
        public async Task SendShotAsync_WithoutClient_ReturnsFalse()
        {
            _listener.Start();
            var shot = new GC2ShotData { BallSpeed = 150f, BackSpin = 3000f };

            bool result = await _listener.SendShotAsync(shot);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task SendDeviceStatusAsync_WithoutClient_ReturnsFalse()
        {
            _listener.Start();

            bool result = await _listener.SendDeviceStatusAsync(true, true);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task SendDataAsync_WithoutClient_ReturnsFalse()
        {
            _listener.Start();

            bool result = await _listener.SendDataAsync("test data");

            Assert.That(result, Is.False);
        }
    }

    /// <summary>
    /// Integration tests for TCP connection with actual socket communication.
    /// These tests create real TCP connections to verify end-to-end behavior.
    /// </summary>
    [TestFixture]
    public class GC2TCPIntegrationTests
    {
        private GC2TCPListener _server;
        private int _port;

        [SetUp]
        public void SetUp()
        {
            _port = UnityEngine.Random.Range(50000, 60000);
            _server = new GC2TCPListener(_port);
        }

        [TearDown]
        public void TearDown()
        {
            _server?.Dispose();
        }

        [Test]
        public void Server_AcceptsClientConnection()
        {
            bool clientConnected = false;
            _server.OnClientConnected += () => clientConnected = true;
            _server.Start();

            // Connect as client
            using var client = new TcpClient();
            client.Connect("127.0.0.1", _port);

            // Wait for connection event
            Thread.Sleep(100);

            Assert.That(client.Connected, Is.True);
            Assert.That(_server.HasClient, Is.True);
            Assert.That(clientConnected, Is.True);
        }

        [Test]
        public async Task Server_SendsDataToClient()
        {
            _server.Start();

            // Connect as client
            using var client = new TcpClient();
            client.Connect("127.0.0.1", _port);
            Thread.Sleep(100);

            // Send data from server
            var shot = new GC2ShotData
            {
                ShotId = 1,
                BallSpeed = 150f,
                LaunchAngle = 12f,
                Direction = 0f,
                BackSpin = 3000f,
                SideSpin = 0f,
                TotalSpin = 3000f
            };
            await _server.SendShotAsync(shot);

            // Read from client
            var stream = client.GetStream();
            var buffer = new byte[4096];

            // Wait for data to arrive
            int timeout = 0;
            while (!stream.DataAvailable && timeout < 10)
            {
                Thread.Sleep(50);
                timeout++;
            }

            if (stream.DataAvailable)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string received = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Assert.That(received, Does.Contain("0H"));
                Assert.That(received, Does.Contain("SPEED_MPH=150"));
            }
            else
            {
                Assert.Fail("No data received within timeout");
            }
        }

        [Test]
        public async Task Server_SendsDeviceStatusToClient()
        {
            _server.Start();

            // Connect as client
            using var client = new TcpClient();
            client.Connect("127.0.0.1", _port);
            Thread.Sleep(100);

            // Send status from server
            await _server.SendDeviceStatusAsync(true, true, new Vector3(0, 0, 100));

            // Read from client
            var stream = client.GetStream();
            var buffer = new byte[4096];

            // Wait for data to arrive
            int timeout = 0;
            while (!stream.DataAvailable && timeout < 10)
            {
                Thread.Sleep(50);
                timeout++;
            }

            if (stream.DataAvailable)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string received = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Assert.That(received, Does.Contain("0M"));
                Assert.That(received, Does.Contain("FLAGS=7"));
                Assert.That(received, Does.Contain("BALLS=1"));
            }
            else
            {
                Assert.Fail("No data received within timeout");
            }
        }

        [Test]
        public void Server_DetectsClientDisconnection()
        {
            bool clientDisconnected = false;
            _server.OnClientDisconnected += () => clientDisconnected = true;
            _server.Start();

            // Connect and then disconnect
            var client = new TcpClient();
            client.Connect("127.0.0.1", _port);
            Thread.Sleep(100);

            Assert.That(_server.HasClient, Is.True);

            client.Close();
            Thread.Sleep(200);

            Assert.That(clientDisconnected, Is.True);
        }
    }
}
