// ABOUTME: Unit tests for ShotHistoryPanel UI component.
// ABOUTME: Tests shot list display, scrolling, selection, statistics summary, and event handling.

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
    public class ShotHistoryPanelTests
    {
        private GameObject _panelGO;
        private ShotHistoryPanel _panel;
        private TextMeshProUGUI _totalShotsText;
        private TextMeshProUGUI _avgSpeedText;
        private TextMeshProUGUI _avgCarryText;
        private TextMeshProUGUI _longestCarryText;
        private ScrollRect _scrollRect;
        private RectTransform _contentContainer;
        private Button _closeButton;
        private Button _clearHistoryButton;
        private CanvasGroup _canvasGroup;
        private GameObject _sessionManagerGO;
        private SessionManager _sessionManager;
        private ShotHistoryItem _itemPrefab;
        private GameObject _itemPrefabGO;

        [SetUp]
        public void SetUp()
        {
            _panelGO = new GameObject("TestShotHistoryPanel");
            _panel = _panelGO.AddComponent<ShotHistoryPanel>();

            // Create text components
            _totalShotsText = CreateTextComponent("TotalShots");
            _avgSpeedText = CreateTextComponent("AvgSpeed");
            _avgCarryText = CreateTextComponent("AvgCarry");
            _longestCarryText = CreateTextComponent("LongestCarry");

            // Create scroll view
            var scrollGO = new GameObject("ScrollView");
            scrollGO.transform.SetParent(_panelGO.transform);
            _scrollRect = scrollGO.AddComponent<ScrollRect>();

            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(scrollGO.transform);
            _contentContainer = contentGO.AddComponent<RectTransform>();
            _scrollRect.content = _contentContainer;

            // Create buttons
            var closeGO = new GameObject("CloseButton");
            closeGO.transform.SetParent(_panelGO.transform);
            _closeButton = closeGO.AddComponent<Button>();

            var clearGO = new GameObject("ClearButton");
            clearGO.transform.SetParent(_panelGO.transform);
            _clearHistoryButton = clearGO.AddComponent<Button>();

            _canvasGroup = _panelGO.AddComponent<CanvasGroup>();

            // Create item prefab
            _itemPrefabGO = new GameObject("ItemPrefab");
            _itemPrefab = _itemPrefabGO.AddComponent<ShotHistoryItem>();
            SetupItemPrefab(_itemPrefab);

            // Wire up references
            _panel.SetReferences(
                _totalShotsText,
                _avgSpeedText,
                _avgCarryText,
                _longestCarryText,
                _scrollRect,
                _contentContainer,
                _closeButton,
                _clearHistoryButton,
                _canvasGroup
            );
            _panel.SetItemPrefab(_itemPrefab);

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
            if (_itemPrefabGO != null)
            {
                UnityEngine.Object.DestroyImmediate(_itemPrefabGO);
            }
        }

        private TextMeshProUGUI CreateTextComponent(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_panelGO.transform);
            return go.AddComponent<TextMeshProUGUI>();
        }

        private void SetupItemPrefab(ShotHistoryItem item)
        {
            var shotNumberText = new GameObject("ShotNumber").AddComponent<TextMeshProUGUI>();
            shotNumberText.transform.SetParent(item.transform);
            var ballSpeedText = new GameObject("BallSpeed").AddComponent<TextMeshProUGUI>();
            ballSpeedText.transform.SetParent(item.transform);
            var carryText = new GameObject("Carry").AddComponent<TextMeshProUGUI>();
            carryText.transform.SetParent(item.transform);
            var timeText = new GameObject("Time").AddComponent<TextMeshProUGUI>();
            timeText.transform.SetParent(item.transform);

            item.SetReferences(shotNumberText, ballSpeedText, carryText, timeText);
        }

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

        #region Initial State Tests

        [Test]
        public void InitialState_IsNotVisible()
        {
            Assert.IsFalse(_panel.IsVisible);
        }

        [Test]
        public void InitialState_NoSelectedShot()
        {
            Assert.IsNull(_panel.SelectedShot);
        }

        [Test]
        public void InitialState_DisplayedItemCountIsZero()
        {
            Assert.AreEqual(0, _panel.DisplayedItemCount);
        }

        #endregion

        #region Statistics Display Tests

        [Test]
        public void SetSessionManager_NoShots_DisplaysZeroTotal()
        {
            _panel.SetSessionManager(_sessionManager);
            _panel.Show(animate: false);

            Assert.AreEqual("0", _panel.GetTotalShotsText());
        }

        [Test]
        public void SetSessionManager_NoShots_DisplaysDash()
        {
            _panel.SetSessionManager(_sessionManager);
            _panel.Show(animate: false);

            Assert.AreEqual("-", _panel.GetAvgSpeedText());
            Assert.AreEqual("-", _panel.GetAvgCarryText());
            Assert.AreEqual("-", _panel.GetLongestCarryText());
        }

        [Test]
        public void AfterShot_TotalShotsUpdates()
        {
            _panel.SetSessionManager(_sessionManager);
            _sessionManager.StartNewSession();
            RecordTestShot(_sessionManager, 150f, 250f);
            _panel.Show(animate: false);

            Assert.AreEqual("1", _panel.GetTotalShotsText());
        }

        [Test]
        public void AfterShot_AvgSpeedUpdates()
        {
            _panel.SetSessionManager(_sessionManager);
            _sessionManager.StartNewSession();
            RecordTestShot(_sessionManager, 150f, 250f);
            _panel.Show(animate: false);

            Assert.AreEqual("150 mph", _panel.GetAvgSpeedText());
        }

        [Test]
        public void AfterShot_AvgCarryUpdates()
        {
            _panel.SetSessionManager(_sessionManager);
            _sessionManager.StartNewSession();
            RecordTestShot(_sessionManager, 150f, 250f);
            _panel.Show(animate: false);

            Assert.AreEqual("250 yd", _panel.GetAvgCarryText());
        }

        [Test]
        public void AfterShot_LongestCarryUpdates()
        {
            _panel.SetSessionManager(_sessionManager);
            _sessionManager.StartNewSession();
            RecordTestShot(_sessionManager, 150f, 250f);
            _panel.Show(animate: false);

            Assert.AreEqual("250 yd", _panel.GetLongestCarryText());
        }

        [Test]
        public void AfterMultipleShots_LongestCarryIsMax()
        {
            _panel.SetSessionManager(_sessionManager);
            _sessionManager.StartNewSession();
            RecordTestShot(_sessionManager, 150f, 250f);
            RecordTestShot(_sessionManager, 160f, 275f);
            RecordTestShot(_sessionManager, 140f, 230f);
            _panel.Show(animate: false);

            Assert.AreEqual("275 yd", _panel.GetLongestCarryText());
        }

        #endregion

        #region Shot List Display Tests

        [Test]
        public void RefreshShotList_CreatesItemsForShots()
        {
            _panel.SetSessionManager(_sessionManager);
            _sessionManager.StartNewSession();
            RecordTestShot(_sessionManager, 150f, 250f);
            RecordTestShot(_sessionManager, 160f, 275f);
            _panel.Show(animate: false);
            _panel.ForceRefreshShotList();

            Assert.AreEqual(2, _panel.DisplayedItemCount);
        }

        [Test]
        public void RefreshShotList_WithNoShots_ShowsNoItems()
        {
            _panel.SetSessionManager(_sessionManager);
            _panel.Show(animate: false);
            _panel.ForceRefreshShotList();

            Assert.AreEqual(0, _panel.DisplayedItemCount);
        }

        #endregion

        #region Visibility Tests

        [Test]
        public void Show_SetsIsVisibleTrue()
        {
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
        public void Toggle_WhenHidden_Shows()
        {
            _panel.Hide(animate: false);
            _panel.Toggle();

            Assert.IsTrue(_panel.IsVisible);
        }

        [Test]
        public void Toggle_WhenVisible_Hides()
        {
            _panel.Show(animate: false);
            _panel.Toggle();

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

        #region Selection Tests

        [Test]
        public void SelectShot_SetsSelectedShot()
        {
            _panel.SetSessionManager(_sessionManager);
            _sessionManager.StartNewSession();
            RecordTestShot(_sessionManager, 150f, 250f);
            _panel.Show(animate: false);
            _panel.ForceRefreshShotList();

            _panel.SelectShot(0);

            Assert.IsNotNull(_panel.SelectedShot);
        }

        [Test]
        public void SelectShot_InvalidIndex_DoesNothing()
        {
            _panel.SetSessionManager(_sessionManager);
            _panel.Show(animate: false);

            _panel.SelectShot(999);

            Assert.IsNull(_panel.SelectedShot);
        }

        [Test]
        public void SelectShot_NegativeIndex_DoesNothing()
        {
            _panel.SetSessionManager(_sessionManager);
            _panel.Show(animate: false);

            _panel.SelectShot(-1);

            Assert.IsNull(_panel.SelectedShot);
        }

        [Test]
        public void ClearSelection_ResetsSelectedShot()
        {
            _panel.SetSessionManager(_sessionManager);
            _sessionManager.StartNewSession();
            RecordTestShot(_sessionManager, 150f, 250f);
            _panel.Show(animate: false);
            _panel.ForceRefreshShotList();
            _panel.SelectShot(0);

            _panel.ClearSelection();

            Assert.IsNull(_panel.SelectedShot);
        }

        #endregion

        #region Event Tests

        [Test]
        public void SelectShot_FiresOnShotSelected()
        {
            _panel.SetSessionManager(_sessionManager);
            _sessionManager.StartNewSession();
            RecordTestShot(_sessionManager, 150f, 250f);
            _panel.Show(animate: false);
            _panel.ForceRefreshShotList();

            SessionShot receivedShot = null;
            _panel.OnShotSelected += (shot) => receivedShot = shot;

            _panel.SelectShot(0);

            Assert.IsNotNull(receivedShot);
        }

        [Test]
        public void CloseButton_Click_FiresOnClosed()
        {
            bool eventFired = false;
            _panel.OnClosed += () => eventFired = true;

            _panel.Show(animate: false);
            _closeButton.onClick.Invoke();

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void ClearHistoryButton_Click_FiresOnHistoryCleared()
        {
            _panel.SetSessionManager(_sessionManager);
            bool eventFired = false;
            _panel.OnHistoryCleared += () => eventFired = true;

            _panel.Show(animate: false);
            _clearHistoryButton.onClick.Invoke();

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void ClearHistoryButton_ClearsSessionHistory()
        {
            _panel.SetSessionManager(_sessionManager);
            _sessionManager.StartNewSession();
            RecordTestShot(_sessionManager, 150f, 250f);
            RecordTestShot(_sessionManager, 160f, 275f);
            _panel.Show(animate: false);

            _clearHistoryButton.onClick.Invoke();

            Assert.AreEqual(0, _panel.DisplayedItemCount);
        }

        #endregion

        #region Scroll Tests

        [Test]
        public void ScrollToTop_DoesNotThrow()
        {
            // ScrollRect normalizedPosition requires a layout pass in Unity
            // so we can only verify the method doesn't throw in EditMode
            Assert.DoesNotThrow(() => _panel.ScrollToTop());
        }

        [Test]
        public void ScrollToBottom_DoesNotThrow()
        {
            // ScrollRect normalizedPosition requires a layout pass in Unity
            // so we can only verify the method doesn't throw in EditMode
            Assert.DoesNotThrow(() => _panel.ScrollToBottom());
        }

        #endregion

        #region Null Safety Tests

        [Test]
        public void SetSessionManager_WithNull_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _panel.SetSessionManager(null));
        }

        [Test]
        public void RefreshAll_WithNoSessionManager_DoesNotThrow()
        {
            _panel.SetSessionManager(null);

            Assert.DoesNotThrow(() => _panel.RefreshAll());
        }

        [Test]
        public void SetReferences_WithNullValues_DoesNotThrow()
        {
            var emptyPanel = new GameObject("Empty").AddComponent<ShotHistoryPanel>();

            Assert.DoesNotThrow(() => emptyPanel.SetReferences(null, null, null, null));

            UnityEngine.Object.DestroyImmediate(emptyPanel.gameObject);
        }

        #endregion

        #region RefreshAll Tests

        [Test]
        public void RefreshAll_UpdatesStatistics()
        {
            _panel.SetSessionManager(_sessionManager);
            _sessionManager.StartNewSession();
            RecordTestShot(_sessionManager, 150f, 250f);
            _panel.Show(animate: false);

            _panel.RefreshAll();

            Assert.AreEqual("1", _panel.GetTotalShotsText());
        }

        [Test]
        public void RefreshAll_UpdatesShotList()
        {
            _panel.SetSessionManager(_sessionManager);
            _sessionManager.StartNewSession();
            RecordTestShot(_sessionManager, 150f, 250f);
            _panel.Show(animate: false);

            _panel.RefreshAll();

            Assert.AreEqual(1, _panel.DisplayedItemCount);
        }

        #endregion
    }
}
