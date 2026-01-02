// ABOUTME: Unit tests for SessionInfoPanel UI component.
// ABOUTME: Tests display updates, time formatting, statistics display, and event handling.

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
    public class SessionInfoPanelTests
    {
        private GameObject _panelGO;
        private SessionInfoPanel _panel;
        private TextMeshProUGUI _sessionTimeText;
        private TextMeshProUGUI _totalShotsText;
        private TextMeshProUGUI _avgSpeedText;
        private TextMeshProUGUI _longestCarryText;
        private Button _expandButton;
        private CanvasGroup _canvasGroup;
        private GameObject _sessionManagerGO;
        private SessionManager _sessionManager;

        [SetUp]
        public void SetUp()
        {
            _panelGO = new GameObject("TestSessionInfoPanel");
            _panel = _panelGO.AddComponent<SessionInfoPanel>();

            // Create text components
            _sessionTimeText = CreateTextComponent("SessionTime");
            _totalShotsText = CreateTextComponent("TotalShots");
            _avgSpeedText = CreateTextComponent("AvgSpeed");
            _longestCarryText = CreateTextComponent("LongestCarry");

            // Create button and canvas group
            var buttonGO = new GameObject("ExpandButton");
            buttonGO.transform.SetParent(_panelGO.transform);
            _expandButton = buttonGO.AddComponent<Button>();

            _canvasGroup = _panelGO.AddComponent<CanvasGroup>();

            // Wire up references
            _panel.SetReferences(
                _sessionTimeText,
                _totalShotsText,
                _avgSpeedText,
                _longestCarryText,
                _expandButton,
                _canvasGroup
            );

            // Create session manager
            _sessionManagerGO = new GameObject("TestSessionManager");
            _sessionManager = _sessionManagerGO.AddComponent<SessionManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_panelGO != null)
            {
                UnityEngine.Object.DestroyImmediate(_panelGO);
            }
            if (_sessionManagerGO != null)
            {
                UnityEngine.Object.DestroyImmediate(_sessionManagerGO);
            }
        }

        private TextMeshProUGUI CreateTextComponent(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_panelGO.transform);
            return go.AddComponent<TextMeshProUGUI>();
        }

        #region Initial State Tests

        [Test]
        public void InitialState_SessionTimeIsZero()
        {
            _panel.SetSessionManager(_sessionManager);
            _panel.ForceUpdateDisplay();

            Assert.AreEqual("00:00:00", _panel.GetSessionTimeText());
        }

        [Test]
        public void InitialState_TotalShotsIsZero()
        {
            _panel.SetSessionManager(_sessionManager);
            _panel.ForceUpdateDisplay();

            Assert.AreEqual("0", _panel.GetTotalShotsText());
        }

        [Test]
        public void InitialState_AvgSpeedIsDash()
        {
            _panel.SetSessionManager(_sessionManager);
            _panel.ForceUpdateDisplay();

            Assert.AreEqual("-", _panel.GetAvgSpeedText());
        }

        [Test]
        public void InitialState_LongestCarryIsDash()
        {
            _panel.SetSessionManager(_sessionManager);
            _panel.ForceUpdateDisplay();

            Assert.AreEqual("-", _panel.GetLongestCarryText());
        }

        [Test]
        public void InitialState_IsVisible()
        {
            Assert.IsTrue(_panel.IsVisible);
        }

        #endregion

        #region Session Manager Integration Tests

        [Test]
        public void SetSessionManager_UpdatesDisplay()
        {
            _sessionManager.StartNewSession();
            RecordTestShot(_sessionManager, 150f, 250f);

            _panel.SetSessionManager(_sessionManager);

            Assert.AreEqual("1", _panel.GetTotalShotsText());
        }

        [Test]
        public void WithNoSessionManager_DisplaysDefaults()
        {
            _panel.SetSessionManager(null);
            _panel.ForceUpdateDisplay();

            Assert.AreEqual("00:00:00", _panel.GetSessionTimeText());
            Assert.AreEqual("0", _panel.GetTotalShotsText());
        }

        #endregion

        #region Statistics Display Tests

        [Test]
        public void AfterShot_TotalShotsUpdates()
        {
            _panel.SetSessionManager(_sessionManager);
            _sessionManager.StartNewSession();
            RecordTestShot(_sessionManager, 150f, 250f);
            _panel.ForceUpdateDisplay();

            Assert.AreEqual("1", _panel.GetTotalShotsText());
        }

        [Test]
        public void AfterMultipleShots_TotalShotsUpdates()
        {
            _panel.SetSessionManager(_sessionManager);
            _sessionManager.StartNewSession();
            RecordTestShot(_sessionManager, 150f, 250f);
            RecordTestShot(_sessionManager, 140f, 230f);
            RecordTestShot(_sessionManager, 160f, 270f);
            _panel.ForceUpdateDisplay();

            Assert.AreEqual("3", _panel.GetTotalShotsText());
        }

        [Test]
        public void AfterShot_AvgSpeedUpdates()
        {
            _panel.SetSessionManager(_sessionManager);
            _sessionManager.StartNewSession();
            RecordTestShot(_sessionManager, 150f, 250f);
            _panel.ForceUpdateDisplay();

            Assert.AreEqual("150", _panel.GetAvgSpeedText());
        }

        [Test]
        public void AfterMultipleShots_AvgSpeedCalculatesCorrectly()
        {
            _panel.SetSessionManager(_sessionManager);
            _sessionManager.StartNewSession();
            RecordTestShot(_sessionManager, 150f, 250f);
            RecordTestShot(_sessionManager, 160f, 270f);
            _panel.ForceUpdateDisplay();

            // Average of 150 and 160 = 155
            Assert.AreEqual("155", _panel.GetAvgSpeedText());
        }

        [Test]
        public void AfterShot_LongestCarryUpdates()
        {
            _panel.SetSessionManager(_sessionManager);
            _sessionManager.StartNewSession();
            RecordTestShot(_sessionManager, 150f, 250f);
            _panel.ForceUpdateDisplay();

            Assert.AreEqual("250", _panel.GetLongestCarryText());
        }

        [Test]
        public void AfterMultipleShots_LongestCarryIsMax()
        {
            _panel.SetSessionManager(_sessionManager);
            _sessionManager.StartNewSession();
            RecordTestShot(_sessionManager, 150f, 250f);
            RecordTestShot(_sessionManager, 160f, 270f);
            RecordTestShot(_sessionManager, 140f, 230f);
            _panel.ForceUpdateDisplay();

            Assert.AreEqual("270", _panel.GetLongestCarryText());
        }

        #endregion

        #region Time Formatting Tests

        [Test]
        public void FormatTime_UnderOneMinute_ShowsZeroMinutes()
        {
            // When session is inactive, time should be 00:00:00
            _panel.SetSessionManager(_sessionManager);
            _panel.ForceUpdateDisplay();

            Assert.AreEqual("00:00:00", _panel.GetSessionTimeText());
        }

        #endregion

        #region Visibility Tests

        [Test]
        public void Show_SetsIsVisibleTrue()
        {
            _panel.Hide(animate: false);
            _panel.Show(animate: false);

            Assert.IsTrue(_panel.IsVisible);
        }

        [Test]
        public void Hide_SetsIsVisibleFalse()
        {
            _panel.Show(animate: false);
            _panel.Hide(animate: false);

            Assert.IsFalse(_panel.IsVisible);
        }

        [Test]
        public void Show_WithoutAnimation_SetsAlphaToOne()
        {
            _canvasGroup.alpha = 0f;
            _panel.Show(animate: false);

            Assert.AreEqual(1f, _canvasGroup.alpha);
        }

        #endregion

        #region Event Tests

        [Test]
        public void ExpandButton_Click_FiresOnExpandClicked()
        {
            bool eventFired = false;
            _panel.OnExpandClicked += () => eventFired = true;

            _expandButton.onClick.Invoke();

            Assert.IsTrue(eventFired);
        }

        #endregion

        #region RefreshDisplay Tests

        [Test]
        public void RefreshDisplay_UpdatesAllFields()
        {
            _panel.SetSessionManager(_sessionManager);
            _sessionManager.StartNewSession();
            RecordTestShot(_sessionManager, 150f, 250f);

            _panel.RefreshDisplay();

            Assert.AreEqual("1", _panel.GetTotalShotsText());
            Assert.AreEqual("150", _panel.GetAvgSpeedText());
            Assert.AreEqual("250", _panel.GetLongestCarryText());
        }

        #endregion

        #region Null Safety Tests

        [Test]
        public void SetReferences_WithNullTexts_DoesNotThrow()
        {
            var emptyPanel = new GameObject("Empty").AddComponent<SessionInfoPanel>();

            Assert.DoesNotThrow(() => emptyPanel.SetReferences(null, null, null, null));

            UnityEngine.Object.DestroyImmediate(emptyPanel.gameObject);
        }

        [Test]
        public void ForceUpdateDisplay_WithNullSessionManager_DoesNotThrow()
        {
            _panel.SetSessionManager(null);

            Assert.DoesNotThrow(() => _panel.ForceUpdateDisplay());
        }

        #endregion

        #region Helper Methods

        private void RecordTestShot(SessionManager sessionManager, float ballSpeed, float carryDistance)
        {
            var shot = new GC2ShotData
            {
                ShotId = sessionManager.TotalShots + 1,
                BallSpeed = ballSpeed,
                LaunchAngle = 12f,
                Direction = 0f,
                BackSpin = 2500f,
                SideSpin = 0f
            };

            var result = new ShotResult
            {
                CarryDistance = carryDistance,
                TotalDistance = carryDistance + 20f,
                MaxHeight = 80f,
                Trajectory = new List<TrajectoryPoint>()
            };

            sessionManager.RecordShot(shot, result);
        }

        #endregion
    }
}
