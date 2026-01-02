// ABOUTME: Unit tests for ShotDetailModal UI component.
// ABOUTME: Tests shot data display, ball/result/club sections, comparison to averages, and event handling.

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
    public class ShotDetailModalTests
    {
        private GameObject _modalGO;
        private ShotDetailModal _modal;
        private TextMeshProUGUI _shotNumberText;
        private TextMeshProUGUI _timestampText;

        // Ball data texts
        private TextMeshProUGUI _ballSpeedText;
        private TextMeshProUGUI _launchAngleText;
        private TextMeshProUGUI _directionText;
        private TextMeshProUGUI _backSpinText;
        private TextMeshProUGUI _sideSpinText;

        // Result data texts
        private TextMeshProUGUI _carryText;
        private TextMeshProUGUI _runText;
        private TextMeshProUGUI _totalText;
        private TextMeshProUGUI _apexText;
        private TextMeshProUGUI _offlineText;

        // Comparison texts
        private TextMeshProUGUI _speedDeltaText;
        private TextMeshProUGUI _carryDeltaText;

        private Button _closeButton;
        private Button _replayButton;
        private CanvasGroup _canvasGroup;
        private GameObject _clubDataContainer;
        private SessionShot _testShot;
        private SessionShot _testShotWithClubData;

        [SetUp]
        public void SetUp()
        {
            _modalGO = new GameObject("TestShotDetailModal");
            _modal = _modalGO.AddComponent<ShotDetailModal>();

            // Create shot info texts
            _shotNumberText = CreateTextComponent("ShotNumber");
            _timestampText = CreateTextComponent("Timestamp");

            // Create ball data texts
            _ballSpeedText = CreateTextComponent("BallSpeed");
            _launchAngleText = CreateTextComponent("LaunchAngle");
            _directionText = CreateTextComponent("Direction");
            _backSpinText = CreateTextComponent("BackSpin");
            _sideSpinText = CreateTextComponent("SideSpin");

            // Create result data texts
            _carryText = CreateTextComponent("Carry");
            _runText = CreateTextComponent("Run");
            _totalText = CreateTextComponent("Total");
            _apexText = CreateTextComponent("Apex");
            _offlineText = CreateTextComponent("Offline");

            // Create comparison texts
            _speedDeltaText = CreateTextComponent("SpeedDelta");
            _carryDeltaText = CreateTextComponent("CarryDelta");

            // Create buttons
            var closeGO = new GameObject("CloseButton");
            closeGO.transform.SetParent(_modalGO.transform);
            _closeButton = closeGO.AddComponent<Button>();

            var replayGO = new GameObject("ReplayButton");
            replayGO.transform.SetParent(_modalGO.transform);
            _replayButton = replayGO.AddComponent<Button>();

            // Create club data container
            _clubDataContainer = new GameObject("ClubDataContainer");
            _clubDataContainer.transform.SetParent(_modalGO.transform);

            _canvasGroup = _modalGO.AddComponent<CanvasGroup>();

            // Wire up references
            _modal.SetReferences(
                _shotNumberText,
                _timestampText,
                _ballSpeedText,
                _directionText,
                _launchAngleText,
                _backSpinText,
                _sideSpinText,
                _carryText,
                _runText,
                _totalText,
                _apexText,
                _offlineText,
                _speedDeltaText,
                _carryDeltaText,
                _closeButton,
                _replayButton,
                _canvasGroup,
                _clubDataContainer
            );

            // Create test shot data
            _testShot = CreateTestSessionShot(1, 150f, 250f, false);
            _testShotWithClubData = CreateTestSessionShot(2, 160f, 275f, true);
        }

        [TearDown]
        public void TearDown()
        {
            if (_modalGO != null)
            {
                UnityEngine.Object.DestroyImmediate(_modalGO);
            }
        }

        private TextMeshProUGUI CreateTextComponent(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_modalGO.transform);
            return go.AddComponent<TextMeshProUGUI>();
        }

        private SessionShot CreateTestSessionShot(int shotNumber, float ballSpeed, float carryDistance, bool hasClubData)
        {
            var shotData = new GC2ShotData
            {
                ShotId = shotNumber,
                BallSpeed = ballSpeed,
                LaunchAngle = 12.5f,
                Direction = -2.5f, // 2.5 degrees left
                TotalSpin = 4500f,
                BackSpin = 4100f,
                SideSpin = -300f, // draw
                SpinAxis = 5f,
                HasClubData = hasClubData
            };

            if (hasClubData)
            {
                shotData.ClubSpeed = 105f;
                shotData.Path = 2.5f;
                shotData.AttackAngle = -4.5f;
                shotData.FaceToTarget = 1.5f;
                shotData.DynamicLoft = 14.5f;
            }

            return new SessionShot
            {
                ShotNumber = shotNumber,
                Timestamp = DateTime.UtcNow,
                ShotData = shotData,
                Result = new ShotResult
                {
                    CarryDistance = carryDistance,
                    TotalDistance = carryDistance + 25f,
                    RollDistance = 25f,
                    OfflineDistance = -8.5f, // 8.5 yards left
                    MaxHeight = 92.5f, // feet
                    FlightTime = 5.8f,
                    TotalTime = 7.2f,
                    Trajectory = new List<TrajectoryPoint>()
                }
            };
        }

        #region Initial State Tests

        [Test]
        public void InitialState_IsNotVisible()
        {
            Assert.IsFalse(_modal.IsVisible);
        }

        [Test]
        public void InitialState_NoCurrentShot()
        {
            Assert.IsNull(_modal.CurrentShot);
        }

        #endregion

        #region Show Tests

        [Test]
        public void Show_SetsIsVisibleTrue()
        {
            _modal.Show(_testShot, animate: false);

            Assert.IsTrue(_modal.IsVisible);
        }

        [Test]
        public void Show_SetsCurrentShot()
        {
            _modal.Show(_testShot, animate: false);

            Assert.AreEqual(_testShot, _modal.CurrentShot);
        }

        [Test]
        public void Show_SetsShotNumberTitle()
        {
            _modal.Show(_testShot, animate: false);

            Assert.AreEqual("Shot #1", _modal.GetShotNumberText());
        }

        [Test]
        public void Show_ActivatesGameObject()
        {
            _modalGO.SetActive(false);
            _modal.Show(_testShot, animate: false);

            Assert.IsTrue(_modalGO.activeSelf);
        }

        [Test]
        public void Show_WithoutAnimation_SetsAlphaToOne()
        {
            _canvasGroup.alpha = 0f;
            _modal.Show(_testShot, animate: false);

            Assert.AreEqual(1f, _canvasGroup.alpha);
        }

        #endregion

        #region Ball Data Display Tests

        [Test]
        public void Show_DisplaysBallSpeed()
        {
            _modal.Show(_testShot, animate: false);

            Assert.AreEqual("150.0 mph", _modal.GetBallSpeedText());
        }

        [Test]
        public void Show_DisplaysLaunchAngle()
        {
            _modal.Show(_testShot, animate: false);

            Assert.AreEqual("12.5", _modal.GetLaunchAngleText());
        }

        [Test]
        public void Show_DisplaysDirection_Left()
        {
            _modal.Show(_testShot, animate: false);

            Assert.AreEqual("L2.5", _modal.GetDirectionText());
        }

        [Test]
        public void Show_DisplaysDirection_Right()
        {
            _testShot.ShotData.Direction = 3.5f;
            _modal.Show(_testShot, animate: false);

            Assert.AreEqual("R3.5", _modal.GetDirectionText());
        }

        [Test]
        public void Show_DisplaysBackSpin()
        {
            _modal.Show(_testShot, animate: false);

            Assert.AreEqual("4,100", _modal.GetBackSpinText());
        }

        [Test]
        public void Show_DisplaysSideSpin_Draw()
        {
            _modal.Show(_testShot, animate: false);

            Assert.AreEqual("L300", _modal.GetSideSpinText());
        }

        [Test]
        public void Show_DisplaysSideSpin_Fade()
        {
            _testShot.ShotData.SideSpin = 400f;
            _modal.Show(_testShot, animate: false);

            Assert.AreEqual("R400", _modal.GetSideSpinText());
        }

        #endregion

        #region Result Data Display Tests

        [Test]
        public void Show_DisplaysCarryDistance()
        {
            _modal.Show(_testShot, animate: false);

            Assert.AreEqual("250.0 yd", _modal.GetCarryText());
        }

        [Test]
        public void Show_DisplaysTotalDistance()
        {
            _modal.Show(_testShot, animate: false);

            Assert.AreEqual("275.0 yd", _modal.GetTotalText());
        }

        [Test]
        public void Show_DisplaysApex_ConvertedToYards()
        {
            // 92.5 feet = 30.83 yards
            _modal.Show(_testShot, animate: false);

            Assert.AreEqual("30.8 yd", _modal.GetApexText());
        }

        [Test]
        public void Show_DisplaysRun()
        {
            // Total - Carry = 275 - 250 = 25 yd
            _modal.Show(_testShot, animate: false);

            Assert.AreEqual("25.0 yd", _modal.GetRunText());
        }

        #endregion

        #region Hide Tests

        [Test]
        public void Hide_SetsIsVisibleFalse()
        {
            _modal.Show(_testShot, animate: false);
            _modal.Hide(animate: false);

            Assert.IsFalse(_modal.IsVisible);
        }

        [Test]
        public void Hide_DeactivatesGameObject()
        {
            _modal.Show(_testShot, animate: false);
            _modal.Hide(animate: false);

            Assert.IsFalse(_modalGO.activeSelf);
        }

        #endregion

        #region Event Tests

        [Test]
        public void CloseButton_Click_FiresOnClosed()
        {
            bool eventFired = false;
            _modal.OnClosed += () => eventFired = true;

            _modal.Show(_testShot, animate: false);
            _closeButton.onClick.Invoke();

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void CloseButton_Click_HidesModal()
        {
            _modal.Show(_testShot, animate: false);
            _closeButton.onClick.Invoke();

            Assert.IsFalse(_modal.IsVisible);
        }

        [Test]
        public void ReplayButton_Click_FiresOnReplayRequested()
        {
            SessionShot receivedShot = null;
            _modal.OnReplayRequested += (shot) => receivedShot = shot;

            _modal.Show(_testShot, animate: false);
            _replayButton.onClick.Invoke();

            Assert.AreEqual(_testShot, receivedShot);
        }

        #endregion

        #region Club Data Display Tests

        [Test]
        public void Show_WithClubData_ShowsClubContainer()
        {
            _modal.Show(_testShotWithClubData, animate: false);

            Assert.IsTrue(_modal.IsClubDataVisible());
        }

        [Test]
        public void Show_WithoutClubData_HidesClubContainer()
        {
            _modal.Show(_testShot, animate: false);

            Assert.IsFalse(_modal.IsClubDataVisible());
        }

        [Test]
        public void Show_WithClubData_DisplaysClubSpeed()
        {
            // Set up club speed text reference
            var clubSpeedText = CreateTextComponent("ClubSpeed");
            _modal.SetClubDataReferences(clubSpeedText);

            _modal.Show(_testShotWithClubData, animate: false);

            Assert.AreEqual("105.0 mph", _modal.GetClubSpeedText());
        }

        #endregion

        #region Null Safety Tests

        [Test]
        public void Show_WithNullShot_HandlesGracefully()
        {
            _modal.Show(null, animate: false);

            // Should activate but not set shot
            Assert.IsNull(_modal.CurrentShot);
        }

        [Test]
        public void SetReferences_WithNullTexts_DoesNotThrow()
        {
            var emptyModal = new GameObject("Empty").AddComponent<ShotDetailModal>();

            Assert.DoesNotThrow(() => emptyModal.SetReferences(
                null, null, null, null, null, null, null,
                null, null, null, null, null,
                null, null, null, null, null, null
            ));

            UnityEngine.Object.DestroyImmediate(emptyModal.gameObject);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void Show_ZeroDirection_DisplaysZero()
        {
            _testShot.ShotData.Direction = 0f;
            _modal.Show(_testShot, animate: false);

            Assert.AreEqual("0.0", _modal.GetDirectionText());
        }

        [Test]
        public void Show_ZeroSideSpin_DisplaysZero()
        {
            _testShot.ShotData.SideSpin = 0f;
            _modal.Show(_testShot, animate: false);

            Assert.AreEqual("0", _modal.GetSideSpinText());
        }

        [Test]
        public void Show_HighShotNumber_DisplaysCorrectly()
        {
            _testShot.ShotNumber = 999;
            _modal.Show(_testShot, animate: false);

            Assert.AreEqual("Shot #999", _modal.GetShotNumberText());
        }

        #endregion

        #region Multiple Shows Tests

        [Test]
        public void Show_MultipleTimes_UpdatesCurrentShot()
        {
            _modal.Show(_testShot, animate: false);
            _modal.Show(_testShotWithClubData, animate: false);

            Assert.AreEqual(_testShotWithClubData, _modal.CurrentShot);
        }

        [Test]
        public void Show_MultipleTimes_UpdatesDisplay()
        {
            _modal.Show(_testShot, animate: false);
            Assert.AreEqual("Shot #1", _modal.GetShotNumberText());

            _modal.Show(_testShotWithClubData, animate: false);
            Assert.AreEqual("Shot #2", _modal.GetShotNumberText());
        }

        #endregion

        #region RefreshDisplay Tests

        [Test]
        public void RefreshDisplay_UpdatesAllFields()
        {
            _modal.Show(_testShot, animate: false);
            _testShot.ShotData.BallSpeed = 175f;

            _modal.RefreshDisplay();

            Assert.AreEqual("175.0 mph", _modal.GetBallSpeedText());
        }

        #endregion
    }
}
