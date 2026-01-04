// ABOUTME: Unit tests for GSProRelay lightweight relay component.
// ABOUTME: Covers connection, shot relay, and event handling.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using OpenRange.GC2;
using OpenRange.Network;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class GSProRelayTests
    {
        private GSProRelay _relay;

        [SetUp]
        public void SetUp()
        {
            _relay = new GSProRelay();
        }

        [TearDown]
        public void TearDown()
        {
            _relay?.Dispose();
        }

        #region Constructor Tests

        [Test]
        public void Constructor_InitializesCorrectly()
        {
            Assert.That(_relay, Is.Not.Null);
            Assert.That(_relay.IsConnected, Is.False);
            Assert.That(_relay.ShotCount, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_HostIsNull_Initially()
        {
            Assert.That(_relay.Host, Is.Null);
        }

        [Test]
        public void Constructor_PortIsZero_Initially()
        {
            Assert.That(_relay.Port, Is.EqualTo(0));
        }

        #endregion

        #region Connection Tests

        [Test]
        public void IsConnected_WhenNotConnected_ReturnsFalse()
        {
            Assert.That(_relay.IsConnected, Is.False);
        }

        [Test]
        public void Disconnect_WhenNotConnected_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _relay.Disconnect());
        }

        #endregion

        #region RelayShot Tests

        [Test]
        public void RelayShot_WithNullShot_FiresError()
        {
            var errors = new List<string>();
            _relay.OnError += errors.Add;

            _relay.RelayShot(null);

            Assert.That(errors.Count, Is.EqualTo(1));
            Assert.That(errors[0], Does.Contain("null"));
        }

        [Test]
        public void RelayShot_WhenNotConnected_FiresError()
        {
            var errors = new List<string>();
            _relay.OnError += errors.Add;
            var shot = CreateTestShot();

            _relay.RelayShot(shot);

            Assert.That(errors.Count, Is.EqualTo(1));
            Assert.That(errors[0], Does.Contain("Not connected"));
        }

        #endregion

        #region Device Status Tests

        [Test]
        public void UpdateDeviceStatus_DoesNotThrow_WhenNotConnected()
        {
            Assert.DoesNotThrow(() => _relay.UpdateDeviceStatus(true, true));
        }

        #endregion

        #region Dispose Tests

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            Assert.DoesNotThrow(() =>
            {
                _relay.Dispose();
                _relay.Dispose();
                _relay.Dispose();
            });
        }

        [Test]
        public void IsConnected_AfterDispose_ReturnsFalse()
        {
            _relay.Dispose();
            Assert.That(_relay.IsConnected, Is.False);
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnError_EventCanBeSubscribed()
        {
            var errorReceived = false;
            _relay.OnError += _ => errorReceived = true;

            _relay.RelayShot(null);

            Assert.That(errorReceived, Is.True);
        }

        [Test]
        public void OnConnected_EventCanBeSubscribed()
        {
            var connected = false;
            _relay.OnConnected += () => connected = true;

            // Event should be subscribable without issues
            Assert.That(connected, Is.False);
        }

        [Test]
        public void OnDisconnected_EventCanBeSubscribed()
        {
            var disconnected = false;
            _relay.OnDisconnected += () => disconnected = true;

            // Event should be subscribable without issues
            Assert.That(disconnected, Is.False);
        }

        [Test]
        public void OnShotRelayed_EventCanBeSubscribed()
        {
            GC2ShotData relayedShot = null;
            _relay.OnShotRelayed += shot => relayedShot = shot;

            // Event should be subscribable without issues
            Assert.That(relayedShot, Is.Null);
        }

        #endregion

        #region ShotCount Tests

        [Test]
        public void ShotCount_InitiallyZero()
        {
            Assert.That(_relay.ShotCount, Is.EqualTo(0));
        }

        #endregion

        #region NotifyShotRelayed Tests

        [Test]
        public void NotifyShotRelayed_FiresOnShotRelayedEvent()
        {
            var shot = CreateTestShot();
            GC2ShotData relayedShot = null;
            _relay.OnShotRelayed += s => relayedShot = s;

            _relay.NotifyShotRelayed(shot);

            Assert.That(relayedShot, Is.EqualTo(shot));
        }

        [Test]
        public void NotifyShotRelayed_WithNull_FiresEventWithNull()
        {
            GC2ShotData relayedShot = CreateTestShot();
            _relay.OnShotRelayed += s => relayedShot = s;

            _relay.NotifyShotRelayed(null);

            Assert.That(relayedShot, Is.Null);
        }

        #endregion

        #region Helper Methods

        private GC2ShotData CreateTestShot()
        {
            return new GC2ShotData
            {
                ShotId = 1,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                BallSpeed = 150f,
                LaunchAngle = 12f,
                Direction = 0f,
                TotalSpin = 2800f,
                BackSpin = 2700f,
                SideSpin = 500f,
                SpinAxis = 5f
            };
        }

        #endregion
    }
}
