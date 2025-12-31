// ABOUTME: Unit tests for the SessionManager service.
// ABOUTME: Tests session lifecycle, shot recording, statistics calculation, and history management.

using System;
using NUnit.Framework;
using UnityEngine;
using OpenRange.Core;
using OpenRange.GC2;
using OpenRange.Physics;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class SessionManagerTests
    {
        private GameObject _testObject;
        private SessionManager _sessionManager;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestSessionManager");
            _sessionManager = _testObject.AddComponent<SessionManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testObject);
            }
        }

        #region Session Lifecycle Tests

        [Test]
        public void StartNewSession_SetsIsActiveToTrue()
        {
            // Arrange
            Assert.IsFalse(_sessionManager.IsActive);

            // Act
            _sessionManager.StartNewSession();

            // Assert
            Assert.IsTrue(_sessionManager.IsActive);
        }

        [Test]
        public void StartNewSession_SetsSessionStartTime()
        {
            // Act
            DateTime beforeStart = DateTime.UtcNow;
            _sessionManager.StartNewSession();
            DateTime afterStart = DateTime.UtcNow;

            // Assert
            Assert.GreaterOrEqual(_sessionManager.SessionStartTime, beforeStart);
            Assert.LessOrEqual(_sessionManager.SessionStartTime, afterStart);
        }

        [Test]
        public void StartNewSession_ResetsStatistics()
        {
            // Arrange - Add some shots first
            _sessionManager.StartNewSession();
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult(200f));
            Assert.Greater(_sessionManager.TotalShots, 0);

            // Act
            _sessionManager.StartNewSession();

            // Assert
            Assert.AreEqual(0, _sessionManager.TotalShots);
            Assert.AreEqual(0, _sessionManager.ShotCount);
            Assert.AreEqual(0f, _sessionManager.AverageBallSpeed);
            Assert.AreEqual(0f, _sessionManager.AverageCarryDistance);
            Assert.AreEqual(0f, _sessionManager.LongestCarry);
            Assert.IsNull(_sessionManager.BestShot);
        }

        [Test]
        public void StartNewSession_FiresOnSessionStartedEvent()
        {
            // Arrange
            bool eventFired = false;
            _sessionManager.OnSessionStarted += () => eventFired = true;

            // Act
            _sessionManager.StartNewSession();

            // Assert
            Assert.IsTrue(eventFired);
        }

        [Test]
        public void EndSession_SetsIsActiveToFalse()
        {
            // Arrange
            _sessionManager.StartNewSession();
            Assert.IsTrue(_sessionManager.IsActive);

            // Act
            _sessionManager.EndSession();

            // Assert
            Assert.IsFalse(_sessionManager.IsActive);
        }

        [Test]
        public void EndSession_FiresOnSessionEndedEvent()
        {
            // Arrange
            _sessionManager.StartNewSession();
            bool eventFired = false;
            _sessionManager.OnSessionEnded += () => eventFired = true;

            // Act
            _sessionManager.EndSession();

            // Assert
            Assert.IsTrue(eventFired);
        }

        [Test]
        public void EndSession_WhenNotActive_DoesNotFireEvent()
        {
            // Arrange
            bool eventFired = false;
            _sessionManager.OnSessionEnded += () => eventFired = true;

            // Act
            _sessionManager.EndSession();

            // Assert
            Assert.IsFalse(eventFired);
        }

        [Test]
        public void ElapsedTime_ReturnsZeroWhenNotActive()
        {
            // Assert
            Assert.AreEqual(TimeSpan.Zero, _sessionManager.ElapsedTime);
        }

        [Test]
        public void ElapsedTime_ReturnsPositiveWhenActive()
        {
            // Arrange
            _sessionManager.StartNewSession();

            // Act - Wait a tiny bit to ensure time passes
            System.Threading.Thread.Sleep(10);
            var elapsed = _sessionManager.ElapsedTime;

            // Assert
            Assert.Greater(elapsed.TotalMilliseconds, 0);
        }

        #endregion

        #region Shot Recording Tests

        [Test]
        public void RecordShot_IncrementsTotalShots()
        {
            // Arrange
            _sessionManager.StartNewSession();
            var shot = CreateValidShot();
            var result = CreateValidResult();

            // Act
            _sessionManager.RecordShot(shot, result);

            // Assert
            Assert.AreEqual(1, _sessionManager.TotalShots);
        }

        [Test]
        public void RecordShot_IncrementsShotCount()
        {
            // Arrange
            _sessionManager.StartNewSession();
            var shot = CreateValidShot();
            var result = CreateValidResult();

            // Act
            _sessionManager.RecordShot(shot, result);

            // Assert
            Assert.AreEqual(1, _sessionManager.ShotCount);
        }

        [Test]
        public void RecordShot_FiresOnShotRecordedEvent()
        {
            // Arrange
            _sessionManager.StartNewSession();
            SessionShot receivedShot = null;
            _sessionManager.OnShotRecorded += (s) => receivedShot = s;

            // Act
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult());

            // Assert
            Assert.IsNotNull(receivedShot);
            Assert.AreEqual(1, receivedShot.ShotNumber);
        }

        [Test]
        public void RecordShot_AutoStartsSessionIfNotActive()
        {
            // Arrange
            Assert.IsFalse(_sessionManager.IsActive);

            // Act
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult());

            // Assert
            Assert.IsTrue(_sessionManager.IsActive);
        }

        [Test]
        public void RecordShot_WithNullShot_DoesNotRecord()
        {
            // Arrange
            _sessionManager.StartNewSession();

            // Act
            _sessionManager.RecordShot(null, CreateValidResult());

            // Assert
            Assert.AreEqual(0, _sessionManager.TotalShots);
        }

        [Test]
        public void RecordShot_WithNullResult_DoesNotRecord()
        {
            // Arrange
            _sessionManager.StartNewSession();

            // Act
            _sessionManager.RecordShot(CreateValidShot(), null);

            // Assert
            Assert.AreEqual(0, _sessionManager.TotalShots);
        }

        [Test]
        public void RecordShot_AssignsShotNumber()
        {
            // Arrange
            _sessionManager.StartNewSession();
            SessionShot firstShot = null;
            SessionShot secondShot = null;

            _sessionManager.OnShotRecorded += (s) =>
            {
                if (firstShot == null) firstShot = s;
                else secondShot = s;
            };

            // Act
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult());
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult());

            // Assert
            Assert.AreEqual(1, firstShot.ShotNumber);
            Assert.AreEqual(2, secondShot.ShotNumber);
        }

        #endregion

        #region Shot Retrieval Tests

        [Test]
        public void GetShot_ReturnsCorrectShot()
        {
            // Arrange
            _sessionManager.StartNewSession();
            var shot1 = CreateValidShot(100f);
            var shot2 = CreateValidShot(150f);
            _sessionManager.RecordShot(shot1, CreateValidResult());
            _sessionManager.RecordShot(shot2, CreateValidResult());

            // Act
            var retrieved = _sessionManager.GetShot(0);

            // Assert
            Assert.AreEqual(shot1.BallSpeed, retrieved.ShotData.BallSpeed);
        }

        [Test]
        public void GetShot_ReturnsNullForInvalidIndex()
        {
            // Arrange
            _sessionManager.StartNewSession();
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult());

            // Act & Assert
            Assert.IsNull(_sessionManager.GetShot(-1));
            Assert.IsNull(_sessionManager.GetShot(5));
        }

        [Test]
        public void GetLatestShot_ReturnsLastRecordedShot()
        {
            // Arrange
            _sessionManager.StartNewSession();
            var shot1 = CreateValidShot(100f);
            var shot2 = CreateValidShot(180f);
            _sessionManager.RecordShot(shot1, CreateValidResult());
            _sessionManager.RecordShot(shot2, CreateValidResult());

            // Act
            var latest = _sessionManager.GetLatestShot();

            // Assert
            Assert.AreEqual(shot2.BallSpeed, latest.ShotData.BallSpeed);
        }

        [Test]
        public void GetLatestShot_ReturnsNullWhenNoShots()
        {
            // Arrange
            _sessionManager.StartNewSession();

            // Act
            var latest = _sessionManager.GetLatestShot();

            // Assert
            Assert.IsNull(latest);
        }

        [Test]
        public void GetAllShots_ReturnsAllRecordedShots()
        {
            // Arrange
            _sessionManager.StartNewSession();
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult());
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult());
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult());

            // Act
            var allShots = _sessionManager.GetAllShots();

            // Assert
            Assert.AreEqual(3, allShots.Count);
        }

        [Test]
        public void GetRecentShots_ReturnsMostRecentFirst()
        {
            // Arrange
            _sessionManager.StartNewSession();
            var shot1 = CreateValidShot(100f);
            var shot2 = CreateValidShot(150f);
            var shot3 = CreateValidShot(180f);
            _sessionManager.RecordShot(shot1, CreateValidResult());
            _sessionManager.RecordShot(shot2, CreateValidResult());
            _sessionManager.RecordShot(shot3, CreateValidResult());

            // Act
            var recent = _sessionManager.GetRecentShots(2);

            // Assert
            Assert.AreEqual(2, recent.Count);
            Assert.AreEqual(180f, recent[0].ShotData.BallSpeed);
            Assert.AreEqual(150f, recent[1].ShotData.BallSpeed);
        }

        #endregion

        #region Statistics Tests

        [Test]
        public void AverageBallSpeed_CalculatesCorrectly()
        {
            // Arrange
            _sessionManager.StartNewSession();
            _sessionManager.RecordShot(CreateValidShot(100f), CreateValidResult());
            _sessionManager.RecordShot(CreateValidShot(120f), CreateValidResult());
            _sessionManager.RecordShot(CreateValidShot(140f), CreateValidResult());

            // Assert - Average of 100, 120, 140 = 120
            Assert.AreEqual(120f, _sessionManager.AverageBallSpeed, 0.01f);
        }

        [Test]
        public void AverageCarryDistance_CalculatesCorrectly()
        {
            // Arrange
            _sessionManager.StartNewSession();
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult(200f));
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult(250f));

            // Assert - Average of 200, 250 = 225
            Assert.AreEqual(225f, _sessionManager.AverageCarryDistance, 0.01f);
        }

        [Test]
        public void LongestCarry_TracksMaximum()
        {
            // Arrange
            _sessionManager.StartNewSession();
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult(200f));
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult(280f));
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult(230f));

            // Assert
            Assert.AreEqual(280f, _sessionManager.LongestCarry, 0.01f);
        }

        [Test]
        public void BestShot_ReturnsShotWithLongestCarry()
        {
            // Arrange
            _sessionManager.StartNewSession();
            var shot1 = CreateValidShot(150f);
            var shot2 = CreateValidShot(170f);
            var shot3 = CreateValidShot(160f);
            _sessionManager.RecordShot(shot1, CreateValidResult(200f));
            _sessionManager.RecordShot(shot2, CreateValidResult(280f));
            _sessionManager.RecordShot(shot3, CreateValidResult(230f));

            // Assert
            Assert.IsNotNull(_sessionManager.BestShot);
            Assert.AreEqual(shot2.BallSpeed, _sessionManager.BestShot.ShotData.BallSpeed);
            Assert.AreEqual(280f, _sessionManager.BestShot.Result.CarryDistance, 0.01f);
        }

        [Test]
        public void OnStatisticsUpdated_FiresAfterRecordShot()
        {
            // Arrange
            _sessionManager.StartNewSession();
            bool eventFired = false;
            _sessionManager.OnStatisticsUpdated += () => eventFired = true;

            // Act
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult());

            // Assert
            Assert.IsTrue(eventFired);
        }

        #endregion

        #region History Size Limit Tests

        [Test]
        public void RecordShot_EnforcesMaxHistorySize()
        {
            // Arrange
            _sessionManager.MaxHistorySize = 3;
            _sessionManager.StartNewSession();

            // Act - Record 5 shots
            for (int i = 0; i < 5; i++)
            {
                _sessionManager.RecordShot(CreateValidShot(100f + i), CreateValidResult());
            }

            // Assert - Only last 3 should remain
            Assert.AreEqual(3, _sessionManager.ShotCount);
            // Total should still be 5
            Assert.AreEqual(5, _sessionManager.TotalShots);
        }

        [Test]
        public void RecordShot_RemovesOldestWhenOverLimit()
        {
            // Arrange
            _sessionManager.MaxHistorySize = 2;
            _sessionManager.StartNewSession();

            // Act
            _sessionManager.RecordShot(CreateValidShot(100f), CreateValidResult());
            _sessionManager.RecordShot(CreateValidShot(120f), CreateValidResult());
            _sessionManager.RecordShot(CreateValidShot(140f), CreateValidResult());

            // Assert - Oldest (100f) should be removed
            var oldest = _sessionManager.GetShot(0);
            Assert.AreEqual(120f, oldest.ShotData.BallSpeed);
        }

        [Test]
        public void MaxHistorySize_ClampsToMinimum()
        {
            // Act
            _sessionManager.MaxHistorySize = 0;

            // Assert
            Assert.AreEqual(1, _sessionManager.MaxHistorySize);
        }

        #endregion

        #region Clear History Tests

        [Test]
        public void ClearHistory_RemovesAllShots()
        {
            // Arrange
            _sessionManager.StartNewSession();
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult());
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult());

            // Act
            _sessionManager.ClearHistory();

            // Assert
            Assert.AreEqual(0, _sessionManager.ShotCount);
            Assert.AreEqual(0, _sessionManager.TotalShots);
        }

        [Test]
        public void ClearHistory_ResetsStatistics()
        {
            // Arrange
            _sessionManager.StartNewSession();
            _sessionManager.RecordShot(CreateValidShot(150f), CreateValidResult(250f));

            // Act
            _sessionManager.ClearHistory();

            // Assert
            Assert.AreEqual(0f, _sessionManager.AverageBallSpeed);
            Assert.AreEqual(0f, _sessionManager.AverageCarryDistance);
            Assert.AreEqual(0f, _sessionManager.LongestCarry);
            Assert.IsNull(_sessionManager.BestShot);
        }

        [Test]
        public void ClearHistory_KeepsSessionActive()
        {
            // Arrange
            _sessionManager.StartNewSession();
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult());

            // Act
            _sessionManager.ClearHistory();

            // Assert
            Assert.IsTrue(_sessionManager.IsActive);
        }

        [Test]
        public void ClearHistory_FiresOnStatisticsUpdated()
        {
            // Arrange
            _sessionManager.StartNewSession();
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult());

            bool eventFired = false;
            _sessionManager.OnStatisticsUpdated += () => eventFired = true;

            // Act
            _sessionManager.ClearHistory();

            // Assert
            Assert.IsTrue(eventFired);
        }

        #endregion

        #region SessionShot Tests

        [Test]
        public void SessionShot_ContainsCorrectData()
        {
            // Arrange
            _sessionManager.StartNewSession();
            var shot = CreateValidShot(165f);
            var result = CreateValidResult(275f);

            SessionShot recorded = null;
            _sessionManager.OnShotRecorded += (s) => recorded = s;

            // Act
            _sessionManager.RecordShot(shot, result);

            // Assert
            Assert.IsNotNull(recorded);
            Assert.AreEqual(shot, recorded.ShotData);
            Assert.AreEqual(result, recorded.Result);
            Assert.AreEqual(1, recorded.ShotNumber);
            Assert.IsTrue((DateTime.UtcNow - recorded.Timestamp).TotalSeconds < 1);
        }

        [Test]
        public void SessionShot_ToString_ReturnsReadableFormat()
        {
            // Arrange
            _sessionManager.StartNewSession();
            var shot = CreateValidShot(165f);
            var result = CreateValidResult(275f);

            SessionShot recorded = null;
            _sessionManager.OnShotRecorded += (s) => recorded = s;
            _sessionManager.RecordShot(shot, result);

            // Act
            var str = recorded.ToString();

            // Assert
            Assert.That(str, Does.Contain("Shot #1"));
            Assert.That(str, Does.Contain("165"));
            Assert.That(str, Does.Contain("275"));
        }

        #endregion

        #region Edge Cases

        [Test]
        public void RecordShot_AfterEndSession_StartsNewSession()
        {
            // Arrange
            _sessionManager.StartNewSession();
            _sessionManager.EndSession();
            Assert.IsFalse(_sessionManager.IsActive);

            // Act
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult());

            // Assert
            Assert.IsTrue(_sessionManager.IsActive);
            Assert.AreEqual(1, _sessionManager.TotalShots);
        }

        [Test]
        public void MultipleSessionsWork()
        {
            // Arrange - First session
            _sessionManager.StartNewSession();
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult(200f));
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult(210f));
            Assert.AreEqual(2, _sessionManager.TotalShots);
            Assert.AreEqual(210f, _sessionManager.LongestCarry, 0.01f);

            // Act - Start second session
            _sessionManager.StartNewSession();
            _sessionManager.RecordShot(CreateValidShot(), CreateValidResult(300f));

            // Assert - New session has fresh stats
            Assert.AreEqual(1, _sessionManager.TotalShots);
            Assert.AreEqual(300f, _sessionManager.LongestCarry, 0.01f);
        }

        #endregion

        #region Helper Methods

        private GC2ShotData CreateValidShot(float ballSpeed = 150f)
        {
            return new GC2ShotData
            {
                ShotId = 1,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                BallSpeed = ballSpeed,
                LaunchAngle = 12f,
                Direction = 0f,
                TotalSpin = 3000f,
                BackSpin = 2900f,
                SideSpin = 300f,
                SpinAxis = 5f
            };
        }

        private ShotResult CreateValidResult(float carryDistance = 250f)
        {
            return new ShotResult
            {
                CarryDistance = carryDistance,
                TotalDistance = carryDistance + 10f,
                RollDistance = 10f,
                OfflineDistance = 5f,
                MaxHeight = 85f,
                FlightTime = 5.5f,
                TotalTime = 8f,
                BounceCount = 2,
                Trajectory = new System.Collections.Generic.List<TrajectoryPoint>
                {
                    new TrajectoryPoint(0f, Vector3.zero, Phase.Flight),
                    new TrajectoryPoint(5.5f, new Vector3(carryDistance, 0f, 5f), Phase.Bounce)
                }
            };
        }

        #endregion
    }
}
