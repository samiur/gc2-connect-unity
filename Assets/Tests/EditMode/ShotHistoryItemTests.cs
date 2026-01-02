// ABOUTME: Unit tests for ShotHistoryItem UI component.
// ABOUTME: Tests shot data display, selection state, time formatting, and event handling.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using OpenRange.Core;
using OpenRange.GC2;
using OpenRange.Physics;
using OpenRange.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class ShotHistoryItemTests
    {
        private GameObject _itemGO;
        private ShotHistoryItem _item;
        private TextMeshProUGUI _shotNumberText;
        private TextMeshProUGUI _ballSpeedText;
        private TextMeshProUGUI _carryDistanceText;
        private TextMeshProUGUI _timeAgoText;
        private Button _itemButton;
        private Image _backgroundImage;
        private Image _selectionHighlight;
        private SessionShot _testShot;

        [SetUp]
        public void SetUp()
        {
            _itemGO = new GameObject("TestShotHistoryItem");
            _item = _itemGO.AddComponent<ShotHistoryItem>();

            // Create text components
            _shotNumberText = CreateTextComponent("ShotNumber");
            _ballSpeedText = CreateTextComponent("BallSpeed");
            _carryDistanceText = CreateTextComponent("CarryDistance");
            _timeAgoText = CreateTextComponent("TimeAgo");

            // Create button and images
            var buttonGO = new GameObject("ItemButton");
            buttonGO.transform.SetParent(_itemGO.transform);
            _itemButton = buttonGO.AddComponent<Button>();

            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(_itemGO.transform);
            _backgroundImage = bgGO.AddComponent<Image>();

            var highlightGO = new GameObject("SelectionHighlight");
            highlightGO.transform.SetParent(_itemGO.transform);
            _selectionHighlight = highlightGO.AddComponent<Image>();

            // Wire up references
            _item.SetReferences(
                _shotNumberText,
                _ballSpeedText,
                _carryDistanceText,
                _timeAgoText,
                _itemButton,
                _backgroundImage,
                _selectionHighlight
            );

            // Create test shot data
            _testShot = CreateTestSessionShot(1, 150f, 250f);
        }

        [TearDown]
        public void TearDown()
        {
            if (_itemGO != null)
            {
                UnityEngine.Object.DestroyImmediate(_itemGO);
            }
        }

        private TextMeshProUGUI CreateTextComponent(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_itemGO.transform);
            return go.AddComponent<TextMeshProUGUI>();
        }

        private SessionShot CreateTestSessionShot(int shotNumber, float ballSpeed, float carryDistance)
        {
            return new SessionShot
            {
                ShotNumber = shotNumber,
                Timestamp = DateTime.UtcNow,
                ShotData = new GC2ShotData
                {
                    ShotId = shotNumber,
                    BallSpeed = ballSpeed,
                    LaunchAngle = 12f,
                    Direction = 0f,
                    BackSpin = 2500f,
                    SideSpin = 0f
                },
                Result = new ShotResult
                {
                    CarryDistance = carryDistance,
                    TotalDistance = carryDistance + 20f,
                    MaxHeight = 80f,
                    Trajectory = new List<TrajectoryPoint>()
                }
            };
        }

        #region SetData Tests

        [Test]
        public void SetData_DisplaysShotNumber()
        {
            _item.SetData(_testShot);

            Assert.AreEqual("#1", _item.GetShotNumberText());
        }

        [Test]
        public void SetData_DisplaysBallSpeed()
        {
            _item.SetData(_testShot);

            Assert.AreEqual("150 mph", _item.GetBallSpeedText());
        }

        [Test]
        public void SetData_DisplaysCarryDistance()
        {
            _item.SetData(_testShot);

            Assert.AreEqual("250 yd", _item.GetCarryDistanceText());
        }

        [Test]
        public void SetData_StoresShotData()
        {
            _item.SetData(_testShot);

            Assert.AreEqual(_testShot, _item.ShotData);
        }

        [Test]
        public void SetData_DifferentShotNumber_DisplaysCorrectly()
        {
            var shot = CreateTestSessionShot(42, 160f, 275f);
            _item.SetData(shot);

            Assert.AreEqual("#42", _item.GetShotNumberText());
        }

        [Test]
        public void SetData_RoundsBallSpeed()
        {
            var shot = CreateTestSessionShot(1, 150.7f, 250f);
            _item.SetData(shot);

            Assert.AreEqual("151 mph", _item.GetBallSpeedText());
        }

        [Test]
        public void SetData_RoundsCarryDistance()
        {
            var shot = CreateTestSessionShot(1, 150f, 250.8f);
            _item.SetData(shot);

            Assert.AreEqual("251 yd", _item.GetCarryDistanceText());
        }

        #endregion

        #region Time Ago Tests

        [Test]
        public void SetData_JustNow_DisplaysJustNow()
        {
            var shot = CreateTestSessionShot(1, 150f, 250f);
            shot.Timestamp = DateTime.UtcNow;
            _item.SetData(shot);

            Assert.AreEqual("Just now", _item.GetTimeAgoText());
        }

        [Test]
        public void SetData_OneMinuteAgo_DisplaysOneMinAgo()
        {
            var shot = CreateTestSessionShot(1, 150f, 250f);
            shot.Timestamp = DateTime.UtcNow.AddMinutes(-1);
            _item.SetData(shot);

            Assert.AreEqual("1 min ago", _item.GetTimeAgoText());
        }

        [Test]
        public void SetData_MultipleMinutesAgo_DisplaysMinutesAgo()
        {
            var shot = CreateTestSessionShot(1, 150f, 250f);
            shot.Timestamp = DateTime.UtcNow.AddMinutes(-5);
            _item.SetData(shot);

            Assert.AreEqual("5 min ago", _item.GetTimeAgoText());
        }

        [Test]
        public void SetData_OneHourAgo_DisplaysOneHrAgo()
        {
            var shot = CreateTestSessionShot(1, 150f, 250f);
            shot.Timestamp = DateTime.UtcNow.AddHours(-1);
            _item.SetData(shot);

            Assert.AreEqual("1 hr ago", _item.GetTimeAgoText());
        }

        [Test]
        public void SetData_MultipleHoursAgo_DisplaysHoursAgo()
        {
            var shot = CreateTestSessionShot(1, 150f, 250f);
            shot.Timestamp = DateTime.UtcNow.AddHours(-3);
            _item.SetData(shot);

            Assert.AreEqual("3 hrs ago", _item.GetTimeAgoText());
        }

        [Test]
        public void SetData_OneDayAgo_DisplaysOneDayAgo()
        {
            var shot = CreateTestSessionShot(1, 150f, 250f);
            shot.Timestamp = DateTime.UtcNow.AddDays(-1);
            _item.SetData(shot);

            Assert.AreEqual("1 day ago", _item.GetTimeAgoText());
        }

        [Test]
        public void SetData_MultipleDaysAgo_DisplaysDaysAgo()
        {
            var shot = CreateTestSessionShot(1, 150f, 250f);
            shot.Timestamp = DateTime.UtcNow.AddDays(-3);
            _item.SetData(shot);

            Assert.AreEqual("3 days ago", _item.GetTimeAgoText());
        }

        [Test]
        public void RefreshTimeAgo_UpdatesDisplay()
        {
            var shot = CreateTestSessionShot(1, 150f, 250f);
            shot.Timestamp = DateTime.UtcNow.AddMinutes(-10);
            _item.SetData(shot);

            string originalText = _item.GetTimeAgoText();
            _item.RefreshTimeAgo();

            // Should still show the same time (10 minutes ago)
            Assert.AreEqual("10 min ago", _item.GetTimeAgoText());
        }

        #endregion

        #region Selection Tests

        [Test]
        public void IsSelected_InitiallyFalse()
        {
            Assert.IsFalse(_item.IsSelected);
        }

        [Test]
        public void IsSelected_SetTrue_UpdatesProperty()
        {
            _item.IsSelected = true;

            Assert.IsTrue(_item.IsSelected);
        }

        [Test]
        public void IsSelected_SetTrue_ShowsHighlight()
        {
            _item.SetData(_testShot);
            _item.IsSelected = true;

            Assert.IsTrue(_selectionHighlight.gameObject.activeSelf);
        }

        [Test]
        public void IsSelected_SetFalse_HidesHighlight()
        {
            _item.SetData(_testShot);
            _item.IsSelected = true;
            _item.IsSelected = false;

            Assert.IsFalse(_selectionHighlight.gameObject.activeSelf);
        }

        #endregion

        #region Event Tests

        [Test]
        public void Click_FiresOnClicked()
        {
            _item.SetData(_testShot);
            SessionShot receivedShot = null;
            _item.OnClicked += (shot) => receivedShot = shot;

            _item.SimulateClick();

            Assert.AreEqual(_testShot, receivedShot);
        }

        [Test]
        public void RequestReplay_FiresOnReplayRequested()
        {
            _item.SetData(_testShot);
            SessionShot receivedShot = null;
            _item.OnReplayRequested += (shot) => receivedShot = shot;

            _item.RequestReplay();

            Assert.AreEqual(_testShot, receivedShot);
        }

        [Test]
        public void Click_WithNullData_DoesNotFire()
        {
            bool eventFired = false;
            _item.OnClicked += (shot) => eventFired = true;

            _item.SimulateClick();

            Assert.IsFalse(eventFired);
        }

        [Test]
        public void RequestReplay_WithNullData_DoesNotFire()
        {
            bool eventFired = false;
            _item.OnReplayRequested += (shot) => eventFired = true;

            _item.RequestReplay();

            Assert.IsFalse(eventFired);
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_ResetsShotData()
        {
            _item.SetData(_testShot);
            _item.Clear();

            Assert.IsNull(_item.ShotData);
        }

        [Test]
        public void Clear_ResetsShotNumberText()
        {
            _item.SetData(_testShot);
            _item.Clear();

            Assert.AreEqual("-", _item.GetShotNumberText());
        }

        [Test]
        public void Clear_ResetsBallSpeedText()
        {
            _item.SetData(_testShot);
            _item.Clear();

            Assert.AreEqual("-", _item.GetBallSpeedText());
        }

        [Test]
        public void Clear_ResetsCarryDistanceText()
        {
            _item.SetData(_testShot);
            _item.Clear();

            Assert.AreEqual("-", _item.GetCarryDistanceText());
        }

        [Test]
        public void Clear_ResetsTimeAgoText()
        {
            _item.SetData(_testShot);
            _item.Clear();

            Assert.AreEqual("-", _item.GetTimeAgoText());
        }

        [Test]
        public void Clear_ResetsIsSelected()
        {
            _item.SetData(_testShot);
            _item.IsSelected = true;
            _item.Clear();

            Assert.IsFalse(_item.IsSelected);
        }

        #endregion

        #region Null Safety Tests

        [Test]
        public void SetData_WithNullShot_ClearsData()
        {
            _item.SetData(_testShot);
            _item.SetData(null);

            Assert.IsNull(_item.ShotData);
            Assert.AreEqual("-", _item.GetShotNumberText());
        }

        [Test]
        public void SetReferences_WithNullTexts_DoesNotThrow()
        {
            var emptyItem = new GameObject("Empty").AddComponent<ShotHistoryItem>();

            Assert.DoesNotThrow(() => emptyItem.SetReferences(null, null, null, null));

            UnityEngine.Object.DestroyImmediate(emptyItem.gameObject);
        }

        [Test]
        public void GetShotNumberText_WithNullReference_ReturnsEmpty()
        {
            var emptyItem = new GameObject("Empty").AddComponent<ShotHistoryItem>();

            Assert.AreEqual(string.Empty, emptyItem.GetShotNumberText());

            UnityEngine.Object.DestroyImmediate(emptyItem.gameObject);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void SetData_LowBallSpeed_DisplaysCorrectly()
        {
            var shot = CreateTestSessionShot(1, 50f, 80f);
            _item.SetData(shot);

            Assert.AreEqual("50 mph", _item.GetBallSpeedText());
            Assert.AreEqual("80 yd", _item.GetCarryDistanceText());
        }

        [Test]
        public void SetData_HighBallSpeed_DisplaysCorrectly()
        {
            var shot = CreateTestSessionShot(1, 200f, 350f);
            _item.SetData(shot);

            Assert.AreEqual("200 mph", _item.GetBallSpeedText());
            Assert.AreEqual("350 yd", _item.GetCarryDistanceText());
        }

        [Test]
        public void SetData_HighShotNumber_DisplaysCorrectly()
        {
            var shot = CreateTestSessionShot(999, 150f, 250f);
            _item.SetData(shot);

            Assert.AreEqual("#999", _item.GetShotNumberText());
        }

        #endregion
    }
}
