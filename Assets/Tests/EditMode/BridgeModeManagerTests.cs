// ABOUTME: Unit tests for BridgeModeManager singleton and state management.
// ABOUTME: Covers state transitions, statistics tracking, and event handling.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using OpenRange.Core;
using OpenRange.GC2;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class BridgeModeManagerTests
    {
        private GameObject _managerObject;
        private BridgeModeManager _manager;
        private List<BridgeModeState> _stateChanges;

        [SetUp]
        public void SetUp()
        {
            _managerObject = new GameObject("BridgeModeManager");
            _manager = _managerObject.AddComponent<BridgeModeManager>();
            _manager.ForceInitializeSingleton();
            _stateChanges = new List<BridgeModeState>();
            _manager.OnBridgeModeStateChanged += state => _stateChanges.Add(state);
        }

        [TearDown]
        public void TearDown()
        {
            if (_manager != null)
            {
                _manager.DisableBridgeMode();
            }
            if (_managerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_managerObject);
            }
            BridgeModeManager.Instance?.GetType()
                .GetProperty("Instance", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                ?.SetValue(null, null);
        }

        #region Initial State Tests

        [Test]
        public void InitialState_IsDisabled()
        {
            Assert.That(_manager.State, Is.EqualTo(BridgeModeState.Disabled));
        }

        [Test]
        public void IsEnabled_WhenDisabled_ReturnsFalse()
        {
            Assert.That(_manager.IsEnabled, Is.False);
        }

        [Test]
        public void IsGSProConnected_WhenDisabled_ReturnsFalse()
        {
            Assert.That(_manager.IsGSProConnected, Is.False);
        }

        [Test]
        public void Statistics_InitialState_AllZero()
        {
            var stats = _manager.Statistics;
            Assert.That(stats.ShotsRelayed, Is.EqualTo(0));
            Assert.That(stats.ShotsRejected, Is.EqualTo(0));
            Assert.That(stats.StartTime, Is.Null);
        }

        #endregion

        #region State Transition Tests

        [Test]
        public void DisableBridgeMode_WhenAlreadyDisabled_DoesNothing()
        {
            _manager.DisableBridgeMode();

            Assert.That(_manager.State, Is.EqualTo(BridgeModeState.Disabled));
            Assert.That(_stateChanges.Count, Is.EqualTo(0));
        }

        [Test]
        public void TransitionToBackgrounded_WhenDisabled_DoesNothing()
        {
            _manager.TransitionToBackgrounded();

            Assert.That(_manager.State, Is.EqualTo(BridgeModeState.Disabled));
            Assert.That(_stateChanges.Count, Is.EqualTo(0));
        }

        [Test]
        public void TransitionToActive_WhenDisabled_DoesNothing()
        {
            _manager.TransitionToActive();

            Assert.That(_manager.State, Is.EqualTo(BridgeModeState.Disabled));
            Assert.That(_stateChanges.Count, Is.EqualTo(0));
        }

        [Test]
        public void TransitionToBackgrounded_WhenActive_TransitionsToBackgrounded()
        {
            _manager.ForceSetState(BridgeModeState.Active);
            _stateChanges.Clear();

            _manager.TransitionToBackgrounded();

            Assert.That(_manager.State, Is.EqualTo(BridgeModeState.Backgrounded));
            Assert.That(_stateChanges.Count, Is.EqualTo(1));
            Assert.That(_stateChanges[0], Is.EqualTo(BridgeModeState.Backgrounded));
        }

        [Test]
        public void TransitionToActive_WhenBackgrounded_TransitionsToActive()
        {
            _manager.ForceSetState(BridgeModeState.Backgrounded);
            _stateChanges.Clear();

            _manager.TransitionToActive();

            Assert.That(_manager.State, Is.EqualTo(BridgeModeState.Active));
            Assert.That(_stateChanges.Count, Is.EqualTo(1));
            Assert.That(_stateChanges[0], Is.EqualTo(BridgeModeState.Active));
        }

        [Test]
        public void TransitionToBackgrounded_WhenBackgrounded_DoesNothing()
        {
            _manager.ForceSetState(BridgeModeState.Backgrounded);
            _stateChanges.Clear();

            _manager.TransitionToBackgrounded();

            Assert.That(_manager.State, Is.EqualTo(BridgeModeState.Backgrounded));
            Assert.That(_stateChanges.Count, Is.EqualTo(0));
        }

        [Test]
        public void TransitionToActive_WhenActive_DoesNothing()
        {
            _manager.ForceSetState(BridgeModeState.Active);
            _stateChanges.Clear();

            _manager.TransitionToActive();

            Assert.That(_manager.State, Is.EqualTo(BridgeModeState.Active));
            Assert.That(_stateChanges.Count, Is.EqualTo(0));
        }

        [Test]
        public void DisableBridgeMode_WhenActive_TransitionsToDisabled()
        {
            _manager.ForceSetState(BridgeModeState.Active);
            _stateChanges.Clear();

            _manager.DisableBridgeMode();

            Assert.That(_manager.State, Is.EqualTo(BridgeModeState.Disabled));
            Assert.That(_stateChanges.Count, Is.EqualTo(1));
            Assert.That(_stateChanges[0], Is.EqualTo(BridgeModeState.Disabled));
        }

        [Test]
        public void DisableBridgeMode_WhenBackgrounded_TransitionsToDisabled()
        {
            _manager.ForceSetState(BridgeModeState.Backgrounded);
            _stateChanges.Clear();

            _manager.DisableBridgeMode();

            Assert.That(_manager.State, Is.EqualTo(BridgeModeState.Disabled));
            Assert.That(_stateChanges.Count, Is.EqualTo(1));
            Assert.That(_stateChanges[0], Is.EqualTo(BridgeModeState.Disabled));
        }

        #endregion

        #region IsEnabled Tests

        [Test]
        public void IsEnabled_WhenActive_ReturnsTrue()
        {
            _manager.ForceSetState(BridgeModeState.Active);
            Assert.That(_manager.IsEnabled, Is.True);
        }

        [Test]
        public void IsEnabled_WhenBackgrounded_ReturnsTrue()
        {
            _manager.ForceSetState(BridgeModeState.Backgrounded);
            Assert.That(_manager.IsEnabled, Is.True);
        }

        #endregion

        #region Statistics Tests

        [Test]
        public void ResetStatistics_ClearsCounters()
        {
            var stats = new BridgeModeStatistics
            {
                ShotsRelayed = 10,
                ShotsRejected = 5,
                StartTime = DateTime.UtcNow.AddMinutes(-10)
            };
            _manager.SetStatistics(stats);

            _manager.ResetStatistics();

            var newStats = _manager.Statistics;
            Assert.That(newStats.ShotsRelayed, Is.EqualTo(0));
            Assert.That(newStats.ShotsRejected, Is.EqualTo(0));
        }

        [Test]
        public void ResetStatistics_WhenEnabled_SetsNewStartTime()
        {
            _manager.ForceSetState(BridgeModeState.Active);
            var oldTime = DateTime.UtcNow.AddMinutes(-10);
            var stats = new BridgeModeStatistics
            {
                ShotsRelayed = 10,
                StartTime = oldTime
            };
            _manager.SetStatistics(stats);

            _manager.ResetStatistics();

            var newStats = _manager.Statistics;
            Assert.That(newStats.StartTime, Is.Not.Null);
            Assert.That(newStats.StartTime, Is.Not.EqualTo(oldTime));
        }

        [Test]
        public void ResetStatistics_WhenDisabled_ClearsStartTime()
        {
            _manager.ForceSetState(BridgeModeState.Disabled);
            var stats = new BridgeModeStatistics
            {
                ShotsRelayed = 10,
                StartTime = DateTime.UtcNow
            };
            _manager.SetStatistics(stats);

            _manager.ResetStatistics();

            var newStats = _manager.Statistics;
            Assert.That(newStats.StartTime, Is.Null);
        }

        #endregion

        #region Configuration Tests

        [Test]
        public void GSProHost_GetSet_Works()
        {
            _manager.GSProHost = "192.168.1.100";
            Assert.That(_manager.GSProHost, Is.EqualTo("192.168.1.100"));
        }

        [Test]
        public void GSProPort_GetSet_Works()
        {
            _manager.GSProPort = 8080;
            Assert.That(_manager.GSProPort, Is.EqualTo(8080));
        }

        [Test]
        public void GSProPort_DefaultValue_Is921()
        {
            var newManager = new GameObject("TestManager").AddComponent<BridgeModeManager>();
            Assert.That(newManager.GSProPort, Is.EqualTo(921));
            UnityEngine.Object.DestroyImmediate(newManager.gameObject);
        }

        #endregion

        #region RelayShotToGSPro Tests

        [Test]
        public void RelayShotToGSPro_WhenDisabled_DoesNothing()
        {
            var shot = CreateTestShot();
            var eventFired = false;
            _manager.OnShotRelayed += _ => eventFired = true;

            _manager.RelayShotToGSPro(shot);

            Assert.That(eventFired, Is.False);
        }

        [Test]
        public void RelayShotToGSPro_WhenNotConnected_IncrementsRejected()
        {
            _manager.ForceSetState(BridgeModeState.Active);
            var shot = CreateTestShot();
            string errorMessage = null;
            _manager.OnShotRelayFailed += (s, msg) => errorMessage = msg;

            _manager.RelayShotToGSPro(shot);

            Assert.That(errorMessage, Is.Not.Null);
            Assert.That(errorMessage, Does.Contain("not connected"));
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnBridgeModeStateChanged_FiresOnStateChange()
        {
            var states = new List<BridgeModeState>();
            _manager.OnBridgeModeStateChanged += states.Add;

            _manager.ForceSetState(BridgeModeState.Disabled);
            _manager.TransitionToBackgrounded(); // Should not fire (wrong starting state)

            // Force to active then background
            _manager.ForceSetState(BridgeModeState.Active);
            states.Clear();
            _manager.TransitionToBackgrounded();

            Assert.That(states.Count, Is.EqualTo(1));
            Assert.That(states[0], Is.EqualTo(BridgeModeState.Backgrounded));
        }

        #endregion

        #region Singleton Tests

        [Test]
        public void Instance_IsSetAfterAwake()
        {
            Assert.That(BridgeModeManager.Instance, Is.EqualTo(_manager));
        }

        [Test]
        public void Instance_SecondInstance_IsDestroyed()
        {
            var secondObject = new GameObject("SecondManager");
            var secondManager = secondObject.AddComponent<BridgeModeManager>();

            // Second manager should destroy itself
            Assert.That(BridgeModeManager.Instance, Is.EqualTo(_manager));

            UnityEngine.Object.DestroyImmediate(secondObject);
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

    #region BridgeModeStatistics Tests

    [TestFixture]
    public class BridgeModeStatisticsTests
    {
        [Test]
        public void Reset_ClearsAllFields()
        {
            var stats = new BridgeModeStatistics
            {
                ShotsRelayed = 100,
                ShotsRejected = 50,
                StartTime = DateTime.UtcNow
            };

            stats.Reset();

            Assert.That(stats.ShotsRelayed, Is.EqualTo(0));
            Assert.That(stats.ShotsRejected, Is.EqualTo(0));
            Assert.That(stats.StartTime, Is.Null);
        }

        [Test]
        public void TotalDuration_WhenStartTimeNull_ReturnsZero()
        {
            var stats = new BridgeModeStatistics { StartTime = null };
            Assert.That(stats.TotalDuration, Is.EqualTo(TimeSpan.Zero));
        }

        [Test]
        public void TotalDuration_WhenStartTimeSet_ReturnsElapsedTime()
        {
            var stats = new BridgeModeStatistics
            {
                StartTime = DateTime.UtcNow.AddSeconds(-10)
            };

            var duration = stats.TotalDuration;

            Assert.That(duration.TotalSeconds, Is.GreaterThanOrEqualTo(9));
            Assert.That(duration.TotalSeconds, Is.LessThanOrEqualTo(12));
        }
    }

    #endregion
}
