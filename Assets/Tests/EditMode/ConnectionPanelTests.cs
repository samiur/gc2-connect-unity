// ABOUTME: Unit tests for ConnectionPanel component that displays detailed connection information.
// ABOUTME: Tests panel visibility, device info display, and button handlers.

using NUnit.Framework;
using OpenRange.Core;
using OpenRange.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class ConnectionPanelTests
    {
        private GameObject _testGO;
        private ConnectionPanel _panel;
        private TextMeshProUGUI _statusText;
        private TextMeshProUGUI _deviceInfoText;
        private TextMeshProUGUI _modeText;
        private TextMeshProUGUI _lastShotText;
        private Image _statusDotImage;
        private Button _connectButton;
        private Button _disconnectButton;
        private Button _retryButton;
        private Button _closeButton;
        private CanvasGroup _canvasGroup;

        [SetUp]
        public void SetUp()
        {
            // Create test hierarchy
            _testGO = new GameObject("ConnectionPanel");
            _canvasGroup = _testGO.AddComponent<CanvasGroup>();

            // Create status dot
            var statusDotGO = new GameObject("StatusDot");
            statusDotGO.transform.SetParent(_testGO.transform);
            _statusDotImage = statusDotGO.AddComponent<Image>();

            // Create text elements
            _statusText = CreateText("StatusText");
            _deviceInfoText = CreateText("DeviceInfoText");
            _modeText = CreateText("ModeText");
            _lastShotText = CreateText("LastShotText");

            // Create buttons
            _connectButton = CreateButton("ConnectButton");
            _disconnectButton = CreateButton("DisconnectButton");
            _retryButton = CreateButton("RetryButton");
            _closeButton = CreateButton("CloseButton");

            // Add component and set references
            _panel = _testGO.AddComponent<ConnectionPanel>();
            _panel.SetReferences(
                _statusDotImage, _statusText, _deviceInfoText, _modeText, _lastShotText,
                _connectButton, _disconnectButton, _retryButton, _closeButton, _canvasGroup
            );
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGO != null)
            {
                Object.DestroyImmediate(_testGO);
            }
        }

        private TextMeshProUGUI CreateText(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_testGO.transform);
            return go.AddComponent<TextMeshProUGUI>();
        }

        private Button CreateButton(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_testGO.transform);
            go.AddComponent<Image>();
            return go.AddComponent<Button>();
        }

        #region Initial State Tests

        [Test]
        public void InitialState_IsNotVisible()
        {
            Assert.That(_panel.IsVisible, Is.False);
        }

        [Test]
        public void InitialState_CurrentState_IsDisconnected()
        {
            Assert.That(_panel.CurrentState, Is.EqualTo(ConnectionState.Disconnected));
        }

        #endregion

        #region UpdateStatus Tests

        [Test]
        public void UpdateStatus_Connected_ShowsGreenColor()
        {
            _panel.UpdateStatus(ConnectionState.Connected);

            Assert.That(_statusDotImage.color, Is.EqualTo(UITheme.StatusConnected));
        }

        [Test]
        public void UpdateStatus_Connected_ShowsConnectedText()
        {
            _panel.UpdateStatus(ConnectionState.Connected);

            Assert.That(_statusText.text, Is.EqualTo("GC2 Connected"));
        }

        [Test]
        public void UpdateStatus_Connected_ShowsDisconnectButton()
        {
            _panel.UpdateStatus(ConnectionState.Connected);

            Assert.That(_disconnectButton.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void UpdateStatus_Connected_HidesConnectButton()
        {
            _panel.UpdateStatus(ConnectionState.Connected);

            Assert.That(_connectButton.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void UpdateStatus_Disconnected_ShowsConnectButton()
        {
            _panel.UpdateStatus(ConnectionState.Disconnected);

            Assert.That(_connectButton.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void UpdateStatus_Disconnected_HidesDisconnectButton()
        {
            _panel.UpdateStatus(ConnectionState.Disconnected);

            Assert.That(_disconnectButton.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void UpdateStatus_Connecting_ShowsConnectingText()
        {
            _panel.UpdateStatus(ConnectionState.Connecting);

            Assert.That(_statusText.text, Is.EqualTo("Connecting..."));
        }

        [Test]
        public void UpdateStatus_Failed_ShowsRetryButton()
        {
            _panel.UpdateStatus(ConnectionState.Failed);

            Assert.That(_retryButton.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void UpdateStatus_DeviceNotFound_ShowsNoDeviceText()
        {
            _panel.UpdateStatus(ConnectionState.DeviceNotFound);

            Assert.That(_statusText.text, Is.EqualTo("No GC2 Detected"));
        }

        [Test]
        public void UpdateStatus_ChangesCurrentState()
        {
            _panel.UpdateStatus(ConnectionState.Connected);

            Assert.That(_panel.CurrentState, Is.EqualTo(ConnectionState.Connected));
        }

        #endregion

        #region Device Info Tests

        [Test]
        public void SetDeviceInfo_UpdatesDeviceInfoText()
        {
            _panel.SetDeviceInfo("GC2-12345", "v1.2.3");

            Assert.That(_deviceInfoText.text, Does.Contain("GC2-12345"));
            Assert.That(_deviceInfoText.text, Does.Contain("v1.2.3"));
        }

        [Test]
        public void SetDeviceInfo_NullSerial_ShowsNotAvailable()
        {
            _panel.SetDeviceInfo(null, null);

            Assert.That(_deviceInfoText.text, Does.Contain("N/A"));
        }

        [Test]
        public void SetConnectionMode_USB_UpdatesModeText()
        {
            _panel.SetConnectionMode(ConnectionMode.USB);

            Assert.That(_modeText.text, Is.EqualTo("USB"));
        }

        [Test]
        public void SetConnectionMode_TCP_UpdatesModeText()
        {
            _panel.SetConnectionMode(ConnectionMode.TCP);

            Assert.That(_modeText.text, Is.EqualTo("TCP"));
        }

        [Test]
        public void SetLastShotTime_UpdatesLastShotText()
        {
            var time = System.DateTime.Now;

            _panel.SetLastShotTime(time);

            Assert.That(_lastShotText.text, Is.Not.Empty);
        }

        [Test]
        public void SetLastShotTime_Null_ShowsNoShots()
        {
            _panel.SetLastShotTime(null);

            Assert.That(_lastShotText.text, Is.EqualTo("No shots yet"));
        }

        #endregion

        #region Show/Hide Tests

        [Test]
        public void Show_SetsIsVisibleTrue()
        {
            _panel.Show(false);

            Assert.That(_panel.IsVisible, Is.True);
        }

        [Test]
        public void Show_WithoutAnimation_SetsAlphaToOne()
        {
            _canvasGroup.alpha = 0f;

            _panel.Show(false);

            Assert.That(_canvasGroup.alpha, Is.EqualTo(1f));
        }

        [Test]
        public void Hide_SetsIsVisibleFalse()
        {
            _panel.Show(false);
            _panel.Hide(false);

            Assert.That(_panel.IsVisible, Is.False);
        }

        [Test]
        public void Hide_WithoutAnimation_SetsAlphaToZero()
        {
            _canvasGroup.alpha = 1f;

            _panel.Hide(false);

            Assert.That(_canvasGroup.alpha, Is.EqualTo(0f));
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnCloseClicked_FiresOnCloseRequested()
        {
            bool fired = false;
            _panel.OnCloseRequested += () => fired = true;

            _panel.SimulateCloseClick();

            Assert.That(fired, Is.True);
        }

        [Test]
        public void OnConnectClicked_FiresOnConnectRequested()
        {
            bool fired = false;
            _panel.OnConnectRequested += () => fired = true;

            _panel.SimulateConnectClick();

            Assert.That(fired, Is.True);
        }

        [Test]
        public void OnDisconnectClicked_FiresOnDisconnectRequested()
        {
            bool fired = false;
            _panel.OnDisconnectRequested += () => fired = true;

            _panel.SimulateDisconnectClick();

            Assert.That(fired, Is.True);
        }

        [Test]
        public void OnRetryClicked_FiresOnRetryRequested()
        {
            bool fired = false;
            _panel.OnRetryRequested += () => fired = true;

            _panel.SimulateRetryClick();

            Assert.That(fired, Is.True);
        }

        #endregion

        #region Toggle Tests

        [Test]
        public void Toggle_WhenHidden_ShowsPanel()
        {
            _panel.Hide(false);

            _panel.Toggle(false);

            Assert.That(_panel.IsVisible, Is.True);
        }

        [Test]
        public void Toggle_WhenVisible_HidesPanel()
        {
            _panel.Show(false);

            _panel.Toggle(false);

            Assert.That(_panel.IsVisible, Is.False);
        }

        #endregion

        #region Button State Tests

        [Test]
        public void UpdateButtonStates_Connected_OnlyShowsDisconnect()
        {
            _panel.UpdateStatus(ConnectionState.Connected);

            Assert.That(_connectButton.gameObject.activeSelf, Is.False);
            Assert.That(_disconnectButton.gameObject.activeSelf, Is.True);
            Assert.That(_retryButton.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void UpdateButtonStates_Disconnected_OnlyShowsConnect()
        {
            _panel.UpdateStatus(ConnectionState.Disconnected);

            Assert.That(_connectButton.gameObject.activeSelf, Is.True);
            Assert.That(_disconnectButton.gameObject.activeSelf, Is.False);
            Assert.That(_retryButton.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void UpdateButtonStates_Failed_ShowsConnectAndRetry()
        {
            _panel.UpdateStatus(ConnectionState.Failed);

            Assert.That(_connectButton.gameObject.activeSelf, Is.True);
            Assert.That(_disconnectButton.gameObject.activeSelf, Is.False);
            Assert.That(_retryButton.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void UpdateButtonStates_Connecting_HidesAllActionButtons()
        {
            _panel.UpdateStatus(ConnectionState.Connecting);

            Assert.That(_connectButton.gameObject.activeSelf, Is.False);
            Assert.That(_disconnectButton.gameObject.activeSelf, Is.False);
            Assert.That(_retryButton.gameObject.activeSelf, Is.False);
        }

        #endregion
    }
}
