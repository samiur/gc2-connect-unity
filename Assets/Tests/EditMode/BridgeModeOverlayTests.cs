// ABOUTME: Unit tests for BridgeModeOverlay UI component.
// ABOUTME: Tests display states, statistics updates, expand/collapse, and drag functionality.

using NUnit.Framework;
using OpenRange.Core;
using OpenRange.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class BridgeModeOverlayTests
    {
        private GameObject _testGO;
        private BridgeModeOverlay _overlay;
        private Image _statusIcon;
        private TextMeshProUGUI _statusText;
        private TextMeshProUGUI _shotsRelayedText;
        private GameObject _expandedPanel;
        private CanvasGroup _canvasGroup;

        [SetUp]
        public void SetUp()
        {
            // Create test hierarchy
            _testGO = new GameObject("BridgeModeOverlay");
            _testGO.AddComponent<RectTransform>();
            _canvasGroup = _testGO.AddComponent<CanvasGroup>();

            // Create status icon
            var iconGO = new GameObject("StatusIcon");
            iconGO.transform.SetParent(_testGO.transform);
            _statusIcon = iconGO.AddComponent<Image>();

            // Create status text
            var statusTextGO = new GameObject("StatusText");
            statusTextGO.transform.SetParent(_testGO.transform);
            _statusText = statusTextGO.AddComponent<TextMeshProUGUI>();

            // Create shots relayed text
            var shotsTextGO = new GameObject("ShotsRelayedText");
            shotsTextGO.transform.SetParent(_testGO.transform);
            _shotsRelayedText = shotsTextGO.AddComponent<TextMeshProUGUI>();

            // Create expanded panel
            _expandedPanel = new GameObject("ExpandedPanel");
            _expandedPanel.transform.SetParent(_testGO.transform);
            _expandedPanel.AddComponent<CanvasGroup>();

            // Add component and set references
            _overlay = _testGO.AddComponent<BridgeModeOverlay>();
            _overlay.SetReferences(_statusIcon, _statusText, _shotsRelayedText, _expandedPanel, _canvasGroup);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGO != null)
            {
                Object.DestroyImmediate(_testGO);
            }
        }

        #region Initial State Tests

        [Test]
        public void InitialState_IsCollapsed()
        {
            Assert.That(_overlay.IsExpanded, Is.False);
        }

        [Test]
        public void InitialState_IsNotVisible()
        {
            Assert.That(_overlay.IsVisible, Is.False);
        }

        [Test]
        public void InitialState_ShotsRelayed_IsZero()
        {
            Assert.That(_overlay.ShotsRelayed, Is.EqualTo(0));
        }

        [Test]
        public void InitialState_ConnectionStatus_IsDisconnected()
        {
            Assert.That(_overlay.ConnectionStatus, Is.EqualTo(BridgeConnectionStatus.Disconnected));
        }

        #endregion

        #region Visibility Tests

        [Test]
        public void Show_SetsIsVisibleTrue()
        {
            _overlay.Show(animate: false);

            Assert.That(_overlay.IsVisible, Is.True);
        }

        [Test]
        public void Show_SetsCanvasGroupAlphaToOne()
        {
            _overlay.Show(animate: false);

            Assert.That(_canvasGroup.alpha, Is.EqualTo(1f));
        }

        [Test]
        public void Hide_SetsIsVisibleFalse()
        {
            _overlay.Show(animate: false);
            _overlay.Hide(animate: false);

            Assert.That(_overlay.IsVisible, Is.False);
        }

        [Test]
        public void Hide_SetsCanvasGroupAlphaToZero()
        {
            _overlay.Show(animate: false);
            _overlay.Hide(animate: false);

            Assert.That(_canvasGroup.alpha, Is.EqualTo(0f));
        }

        #endregion

        #region Expand/Collapse Tests

        [Test]
        public void Expand_SetsIsExpandedTrue()
        {
            _overlay.Expand(animate: false);

            Assert.That(_overlay.IsExpanded, Is.True);
        }

        [Test]
        public void Expand_ShowsExpandedPanel()
        {
            _overlay.Expand(animate: false);

            Assert.That(_expandedPanel.activeSelf, Is.True);
        }

        [Test]
        public void Collapse_SetsIsExpandedFalse()
        {
            _overlay.Expand(animate: false);
            _overlay.Collapse(animate: false);

            Assert.That(_overlay.IsExpanded, Is.False);
        }

        [Test]
        public void Collapse_HidesExpandedPanel()
        {
            _overlay.Expand(animate: false);
            _overlay.Collapse(animate: false);

            Assert.That(_expandedPanel.activeSelf, Is.False);
        }

        [Test]
        public void ToggleExpand_TogglesState()
        {
            Assert.That(_overlay.IsExpanded, Is.False);

            _overlay.ToggleExpand(animate: false);
            Assert.That(_overlay.IsExpanded, Is.True);

            _overlay.ToggleExpand(animate: false);
            Assert.That(_overlay.IsExpanded, Is.False);
        }

        [Test]
        public void Expand_FiresOnExpandedEvent()
        {
            bool eventFired = false;
            _overlay.OnExpanded += () => eventFired = true;

            _overlay.Expand(animate: false);

            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void Collapse_FiresOnCollapsedEvent()
        {
            _overlay.Expand(animate: false);
            bool eventFired = false;
            _overlay.OnCollapsed += () => eventFired = true;

            _overlay.Collapse(animate: false);

            Assert.That(eventFired, Is.True);
        }

        #endregion

        #region Connection Status Tests

        [Test]
        public void UpdateConnectionStatus_Connected_UpdatesStatus()
        {
            _overlay.UpdateConnectionStatus(BridgeConnectionStatus.Connected);

            Assert.That(_overlay.ConnectionStatus, Is.EqualTo(BridgeConnectionStatus.Connected));
        }

        [Test]
        public void UpdateConnectionStatus_Connected_SetsGreenColor()
        {
            _overlay.UpdateConnectionStatus(BridgeConnectionStatus.Connected);

            Assert.That(_statusIcon.color, Is.EqualTo(BridgeModeOverlay.ConnectedColor));
        }

        [Test]
        public void UpdateConnectionStatus_Connecting_SetsYellowColor()
        {
            _overlay.UpdateConnectionStatus(BridgeConnectionStatus.Connecting);

            Assert.That(_statusIcon.color, Is.EqualTo(BridgeModeOverlay.ConnectingColor));
        }

        [Test]
        public void UpdateConnectionStatus_Disconnected_SetsGrayColor()
        {
            // First set to a different status, then test Disconnected
            _overlay.UpdateConnectionStatus(BridgeConnectionStatus.Connected);
            _overlay.UpdateConnectionStatus(BridgeConnectionStatus.Disconnected);

            Assert.That(_statusIcon.color, Is.EqualTo(BridgeModeOverlay.DisconnectedColor));
        }

        [Test]
        public void UpdateConnectionStatus_Error_SetsRedColor()
        {
            _overlay.UpdateConnectionStatus(BridgeConnectionStatus.Error);

            Assert.That(_statusIcon.color, Is.EqualTo(BridgeModeOverlay.ErrorColor));
        }

        [Test]
        public void UpdateConnectionStatus_Connected_UpdatesStatusText()
        {
            _overlay.UpdateConnectionStatus(BridgeConnectionStatus.Connected);

            Assert.That(_statusText.text, Is.EqualTo("GSPro Connected"));
        }

        [Test]
        public void UpdateConnectionStatus_Connecting_UpdatesStatusText()
        {
            _overlay.UpdateConnectionStatus(BridgeConnectionStatus.Connecting);

            Assert.That(_statusText.text, Is.EqualTo("Connecting..."));
        }

        [Test]
        public void UpdateConnectionStatus_Disconnected_UpdatesStatusText()
        {
            // First set to a different status, then test Disconnected
            _overlay.UpdateConnectionStatus(BridgeConnectionStatus.Connected);
            _overlay.UpdateConnectionStatus(BridgeConnectionStatus.Disconnected);

            Assert.That(_statusText.text, Is.EqualTo("Disconnected"));
        }

        [Test]
        public void UpdateConnectionStatus_FiresOnConnectionStatusChangedEvent()
        {
            BridgeConnectionStatus receivedStatus = BridgeConnectionStatus.Disconnected;
            _overlay.OnConnectionStatusChanged += status => receivedStatus = status;

            _overlay.UpdateConnectionStatus(BridgeConnectionStatus.Connected);

            Assert.That(receivedStatus, Is.EqualTo(BridgeConnectionStatus.Connected));
        }

        #endregion

        #region Shots Relayed Tests

        [Test]
        public void UpdateShotsRelayed_UpdatesCount()
        {
            _overlay.UpdateShotsRelayed(5);

            Assert.That(_overlay.ShotsRelayed, Is.EqualTo(5));
        }

        [Test]
        public void UpdateShotsRelayed_UpdatesText()
        {
            _overlay.UpdateShotsRelayed(42);

            Assert.That(_shotsRelayedText.text, Is.EqualTo("42 shots"));
        }

        [Test]
        public void UpdateShotsRelayed_One_ShowsSingular()
        {
            _overlay.UpdateShotsRelayed(1);

            Assert.That(_shotsRelayedText.text, Is.EqualTo("1 shot"));
        }

        [Test]
        public void UpdateShotsRelayed_Zero_ShowsPlural()
        {
            _overlay.UpdateShotsRelayed(0);

            Assert.That(_shotsRelayedText.text, Is.EqualTo("0 shots"));
        }

        [Test]
        public void UpdateShotsRelayed_FiresOnShotsRelayedChangedEvent()
        {
            int receivedCount = 0;
            _overlay.OnShotsRelayedChanged += count => receivedCount = count;

            _overlay.UpdateShotsRelayed(7);

            Assert.That(receivedCount, Is.EqualTo(7));
        }

        [Test]
        public void IncrementShotsRelayed_IncrementsCount()
        {
            _overlay.UpdateShotsRelayed(5);
            _overlay.IncrementShotsRelayed();

            Assert.That(_overlay.ShotsRelayed, Is.EqualTo(6));
        }

        [Test]
        public void ResetShotsRelayed_SetsCountToZero()
        {
            _overlay.UpdateShotsRelayed(10);
            _overlay.ResetShotsRelayed();

            Assert.That(_overlay.ShotsRelayed, Is.EqualTo(0));
        }

        #endregion

        #region Static Helper Tests

        [Test]
        public void GetStatusText_Connected_ReturnsCorrectText()
        {
            Assert.That(BridgeModeOverlay.GetStatusText(BridgeConnectionStatus.Connected),
                Is.EqualTo("GSPro Connected"));
        }

        [Test]
        public void GetStatusText_Connecting_ReturnsCorrectText()
        {
            Assert.That(BridgeModeOverlay.GetStatusText(BridgeConnectionStatus.Connecting),
                Is.EqualTo("Connecting..."));
        }

        [Test]
        public void GetStatusText_Disconnected_ReturnsCorrectText()
        {
            Assert.That(BridgeModeOverlay.GetStatusText(BridgeConnectionStatus.Disconnected),
                Is.EqualTo("Disconnected"));
        }

        [Test]
        public void GetStatusText_Error_ReturnsCorrectText()
        {
            Assert.That(BridgeModeOverlay.GetStatusText(BridgeConnectionStatus.Error),
                Is.EqualTo("Connection Error"));
        }

        [Test]
        public void GetStatusColor_Connected_ReturnsGreen()
        {
            Assert.That(BridgeModeOverlay.GetStatusColor(BridgeConnectionStatus.Connected),
                Is.EqualTo(BridgeModeOverlay.ConnectedColor));
        }

        [Test]
        public void GetStatusColor_Error_ReturnsRed()
        {
            Assert.That(BridgeModeOverlay.GetStatusColor(BridgeConnectionStatus.Error),
                Is.EqualTo(BridgeModeOverlay.ErrorColor));
        }

        #endregion

        #region Format Shots Text Tests

        [Test]
        public void FormatShotsText_Zero_ReturnsPlural()
        {
            Assert.That(BridgeModeOverlay.FormatShotsText(0), Is.EqualTo("0 shots"));
        }

        [Test]
        public void FormatShotsText_One_ReturnsSingular()
        {
            Assert.That(BridgeModeOverlay.FormatShotsText(1), Is.EqualTo("1 shot"));
        }

        [Test]
        public void FormatShotsText_Many_ReturnsPlural()
        {
            Assert.That(BridgeModeOverlay.FormatShotsText(100), Is.EqualTo("100 shots"));
        }

        #endregion
    }
}
