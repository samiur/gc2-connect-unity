// ABOUTME: Unit tests for ConnectionStatusUI component that displays connection state indicator.
// ABOUTME: Tests status text, color, and state change handling.

using NUnit.Framework;
using OpenRange.Core;
using OpenRange.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class ConnectionStatusUITests
    {
        private GameObject _testGO;
        private ConnectionStatusUI _statusUI;
        private GameObject _statusDot;
        private Image _statusDotImage;
        private TextMeshProUGUI _statusText;
        private CanvasGroup _canvasGroup;

        [SetUp]
        public void SetUp()
        {
            // Create test hierarchy
            _testGO = new GameObject("ConnectionStatusUI");
            _canvasGroup = _testGO.AddComponent<CanvasGroup>();

            // Create status dot
            _statusDot = new GameObject("StatusDot");
            _statusDot.transform.SetParent(_testGO.transform);
            _statusDotImage = _statusDot.AddComponent<Image>();

            // Create status text
            var textGO = new GameObject("StatusText");
            textGO.transform.SetParent(_testGO.transform);
            _statusText = textGO.AddComponent<TextMeshProUGUI>();

            // Add component and set references
            _statusUI = _testGO.AddComponent<ConnectionStatusUI>();
            _statusUI.SetReferences(_statusDotImage, _statusText, _canvasGroup);
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
        public void InitialState_StatusDot_IsNotNull()
        {
            Assert.That(_statusUI.StatusDotImage, Is.Not.Null);
        }

        [Test]
        public void InitialState_StatusText_IsNotNull()
        {
            Assert.That(_statusUI.StatusText, Is.Not.Null);
        }

        [Test]
        public void InitialState_CurrentState_IsDisconnected()
        {
            Assert.That(_statusUI.CurrentState, Is.EqualTo(ConnectionState.Disconnected));
        }

        #endregion

        #region UpdateStatus Tests

        [Test]
        public void UpdateStatus_Connected_ShowsGreenColor()
        {
            _statusUI.UpdateStatus(ConnectionState.Connected);

            Assert.That(_statusDotImage.color, Is.EqualTo(UITheme.StatusConnected));
        }

        [Test]
        public void UpdateStatus_Connected_ShowsConnectedText()
        {
            _statusUI.UpdateStatus(ConnectionState.Connected);

            Assert.That(_statusText.text, Is.EqualTo("GC2 Connected"));
        }

        [Test]
        public void UpdateStatus_Connecting_ShowsYellowColor()
        {
            _statusUI.UpdateStatus(ConnectionState.Connecting);

            Assert.That(_statusDotImage.color, Is.EqualTo(UITheme.StatusConnecting));
        }

        [Test]
        public void UpdateStatus_Connecting_ShowsConnectingText()
        {
            _statusUI.UpdateStatus(ConnectionState.Connecting);

            Assert.That(_statusText.text, Is.EqualTo("Connecting..."));
        }

        [Test]
        public void UpdateStatus_Disconnected_ShowsRedColor()
        {
            // First change to a different state, then back to Disconnected
            // (since initial state is already Disconnected, UpdateStatus would early-return)
            _statusUI.UpdateStatus(ConnectionState.Connected);
            _statusUI.UpdateStatus(ConnectionState.Disconnected);

            Assert.That(_statusDotImage.color, Is.EqualTo(UITheme.StatusDisconnected));
        }

        [Test]
        public void UpdateStatus_Disconnected_ShowsDisconnectedText()
        {
            // First change to a different state, then back to Disconnected
            _statusUI.UpdateStatus(ConnectionState.Connected);
            _statusUI.UpdateStatus(ConnectionState.Disconnected);

            Assert.That(_statusText.text, Is.EqualTo("Disconnected"));
        }

        [Test]
        public void UpdateStatus_DeviceNotFound_ShowsGrayColor()
        {
            _statusUI.UpdateStatus(ConnectionState.DeviceNotFound);

            Assert.That(_statusDotImage.color, Is.EqualTo(UITheme.StatusNoDevice));
        }

        [Test]
        public void UpdateStatus_DeviceNotFound_ShowsNoDeviceText()
        {
            _statusUI.UpdateStatus(ConnectionState.DeviceNotFound);

            Assert.That(_statusText.text, Is.EqualTo("No GC2 Detected"));
        }

        [Test]
        public void UpdateStatus_Failed_ShowsRedColor()
        {
            _statusUI.UpdateStatus(ConnectionState.Failed);

            Assert.That(_statusDotImage.color, Is.EqualTo(UITheme.StatusDisconnected));
        }

        [Test]
        public void UpdateStatus_Failed_ShowsFailedText()
        {
            _statusUI.UpdateStatus(ConnectionState.Failed);

            Assert.That(_statusText.text, Is.EqualTo("Connection Failed"));
        }

        [Test]
        public void UpdateStatus_ChangesCurrentState()
        {
            _statusUI.UpdateStatus(ConnectionState.Connected);

            Assert.That(_statusUI.CurrentState, Is.EqualTo(ConnectionState.Connected));
        }

        #endregion

        #region Event Tests

        [Test]
        public void UpdateStatus_FiresOnStatusChanged()
        {
            ConnectionState? receivedState = null;
            _statusUI.OnStatusChanged += state => receivedState = state;

            _statusUI.UpdateStatus(ConnectionState.Connected);

            Assert.That(receivedState, Is.EqualTo(ConnectionState.Connected));
        }

        [Test]
        public void UpdateStatus_SameState_DoesNotFireEvent()
        {
            _statusUI.UpdateStatus(ConnectionState.Disconnected);
            int eventCount = 0;
            _statusUI.OnStatusChanged += _ => eventCount++;

            _statusUI.UpdateStatus(ConnectionState.Disconnected);

            Assert.That(eventCount, Is.EqualTo(0));
        }

        #endregion

        #region Click Handler Tests

        [Test]
        public void OnClick_FiresOnClicked()
        {
            bool clicked = false;
            _statusUI.OnClicked += () => clicked = true;

            _statusUI.SimulateClick();

            Assert.That(clicked, Is.True);
        }

        #endregion

        #region GetStatusText Tests

        [Test]
        public void GetStatusText_Connected_ReturnsCorrectText()
        {
            string text = ConnectionStatusUI.GetStatusText(ConnectionState.Connected);

            Assert.That(text, Is.EqualTo("GC2 Connected"));
        }

        [Test]
        public void GetStatusText_Connecting_ReturnsCorrectText()
        {
            string text = ConnectionStatusUI.GetStatusText(ConnectionState.Connecting);

            Assert.That(text, Is.EqualTo("Connecting..."));
        }

        [Test]
        public void GetStatusText_Disconnected_ReturnsCorrectText()
        {
            string text = ConnectionStatusUI.GetStatusText(ConnectionState.Disconnected);

            Assert.That(text, Is.EqualTo("Disconnected"));
        }

        [Test]
        public void GetStatusText_DeviceNotFound_ReturnsCorrectText()
        {
            string text = ConnectionStatusUI.GetStatusText(ConnectionState.DeviceNotFound);

            Assert.That(text, Is.EqualTo("No GC2 Detected"));
        }

        [Test]
        public void GetStatusText_Failed_ReturnsCorrectText()
        {
            string text = ConnectionStatusUI.GetStatusText(ConnectionState.Failed);

            Assert.That(text, Is.EqualTo("Connection Failed"));
        }

        #endregion

        #region GetStatusColor Tests

        [Test]
        public void GetStatusColor_Connected_ReturnsGreen()
        {
            Color color = ConnectionStatusUI.GetStatusColor(ConnectionState.Connected);

            Assert.That(color, Is.EqualTo(UITheme.StatusConnected));
        }

        [Test]
        public void GetStatusColor_Connecting_ReturnsYellow()
        {
            Color color = ConnectionStatusUI.GetStatusColor(ConnectionState.Connecting);

            Assert.That(color, Is.EqualTo(UITheme.StatusConnecting));
        }

        [Test]
        public void GetStatusColor_Disconnected_ReturnsRed()
        {
            Color color = ConnectionStatusUI.GetStatusColor(ConnectionState.Disconnected);

            Assert.That(color, Is.EqualTo(UITheme.StatusDisconnected));
        }

        [Test]
        public void GetStatusColor_DeviceNotFound_ReturnsGray()
        {
            Color color = ConnectionStatusUI.GetStatusColor(ConnectionState.DeviceNotFound);

            Assert.That(color, Is.EqualTo(UITheme.StatusNoDevice));
        }

        [Test]
        public void GetStatusColor_Failed_ReturnsRed()
        {
            Color color = ConnectionStatusUI.GetStatusColor(ConnectionState.Failed);

            Assert.That(color, Is.EqualTo(UITheme.StatusDisconnected));
        }

        #endregion

        #region Visibility Tests

        [Test]
        public void Show_SetsVisibleTrue()
        {
            _statusUI.Hide(false);

            _statusUI.Show(false);

            Assert.That(_statusUI.IsVisible, Is.True);
        }

        [Test]
        public void Hide_SetsVisibleFalse()
        {
            _statusUI.Show(false);

            _statusUI.Hide(false);

            Assert.That(_statusUI.IsVisible, Is.False);
        }

        [Test]
        public void Show_WithoutAnimation_SetsAlphaToOne()
        {
            _canvasGroup.alpha = 0f;

            _statusUI.Show(false);

            Assert.That(_canvasGroup.alpha, Is.EqualTo(1f));
        }

        [Test]
        public void Hide_WithoutAnimation_SetsAlphaToZero()
        {
            _canvasGroup.alpha = 1f;

            _statusUI.Hide(false);

            Assert.That(_canvasGroup.alpha, Is.EqualTo(0f));
        }

        #endregion
    }
}
