// ABOUTME: Unit tests for ConnectionToast that displays connection status change notifications.
// ABOUTME: Tests toast appearance, messaging, and auto-dismiss behavior.

using NUnit.Framework;
using OpenRange.Core;
using OpenRange.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class ConnectionToastTests
    {
        private GameObject _testGO;
        private ConnectionToast _toast;
        private TextMeshProUGUI _messageText;
        private Image _backgroundImage;
        private CanvasGroup _canvasGroup;

        [SetUp]
        public void SetUp()
        {
            // Create test hierarchy
            _testGO = new GameObject("ConnectionToast");
            _canvasGroup = _testGO.AddComponent<CanvasGroup>();
            _backgroundImage = _testGO.AddComponent<Image>();

            // Create message text
            var textGO = new GameObject("MessageText");
            textGO.transform.SetParent(_testGO.transform);
            _messageText = textGO.AddComponent<TextMeshProUGUI>();

            // Add component and set references
            _toast = _testGO.AddComponent<ConnectionToast>();
            _toast.SetReferences(_messageText, _backgroundImage, _canvasGroup);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGO != null)
            {
                Object.DestroyImmediate(_testGO);
            }
        }

        #region GetToastMessage Tests

        [Test]
        public void GetToastMessage_Connected_ReturnsConnectedMessage()
        {
            string message = ConnectionToast.GetToastMessage(ConnectionState.Connected);

            Assert.That(message, Is.EqualTo("GC2 Connected"));
        }

        [Test]
        public void GetToastMessage_Disconnected_ReturnsDisconnectedMessage()
        {
            string message = ConnectionToast.GetToastMessage(ConnectionState.Disconnected);

            Assert.That(message, Is.EqualTo("GC2 Disconnected"));
        }

        [Test]
        public void GetToastMessage_Connecting_ReturnsConnectingMessage()
        {
            string message = ConnectionToast.GetToastMessage(ConnectionState.Connecting);

            Assert.That(message, Is.EqualTo("Connecting to GC2..."));
        }

        [Test]
        public void GetToastMessage_DeviceNotFound_ReturnsNotFoundMessage()
        {
            string message = ConnectionToast.GetToastMessage(ConnectionState.DeviceNotFound);

            Assert.That(message, Is.EqualTo("No GC2 Device Found"));
        }

        [Test]
        public void GetToastMessage_Failed_ReturnsFailedMessage()
        {
            string message = ConnectionToast.GetToastMessage(ConnectionState.Failed);

            Assert.That(message, Is.EqualTo("Connection Failed"));
        }

        #endregion

        #region GetToastType Tests

        [Test]
        public void GetToastType_Connected_ReturnsSuccess()
        {
            ToastType type = ConnectionToast.GetToastType(ConnectionState.Connected);

            Assert.That(type, Is.EqualTo(ToastType.Success));
        }

        [Test]
        public void GetToastType_Disconnected_ReturnsError()
        {
            ToastType type = ConnectionToast.GetToastType(ConnectionState.Disconnected);

            Assert.That(type, Is.EqualTo(ToastType.Error));
        }

        [Test]
        public void GetToastType_Connecting_ReturnsInfo()
        {
            ToastType type = ConnectionToast.GetToastType(ConnectionState.Connecting);

            Assert.That(type, Is.EqualTo(ToastType.Info));
        }

        [Test]
        public void GetToastType_DeviceNotFound_ReturnsWarning()
        {
            ToastType type = ConnectionToast.GetToastType(ConnectionState.DeviceNotFound);

            Assert.That(type, Is.EqualTo(ToastType.Warning));
        }

        [Test]
        public void GetToastType_Failed_ReturnsError()
        {
            ToastType type = ConnectionToast.GetToastType(ConnectionState.Failed);

            Assert.That(type, Is.EqualTo(ToastType.Error));
        }

        #endregion

        #region Show Tests

        [Test]
        public void ShowForState_Connected_SetsCorrectMessage()
        {
            _toast.ShowForState(ConnectionState.Connected);

            Assert.That(_messageText.text, Is.EqualTo("GC2 Connected"));
        }

        [Test]
        public void ShowForState_Connected_SetsSuccessColor()
        {
            _toast.ShowForState(ConnectionState.Connected);

            Assert.That(_backgroundImage.color, Is.EqualTo(UITheme.ToastSuccess));
        }

        [Test]
        public void ShowForState_Disconnected_SetsCorrectMessage()
        {
            _toast.ShowForState(ConnectionState.Disconnected);

            Assert.That(_messageText.text, Is.EqualTo("GC2 Disconnected"));
        }

        [Test]
        public void ShowForState_Disconnected_SetsErrorColor()
        {
            _toast.ShowForState(ConnectionState.Disconnected);

            Assert.That(_backgroundImage.color, Is.EqualTo(UITheme.ToastError));
        }

        [Test]
        public void ShowForState_Connecting_SetsCorrectMessage()
        {
            _toast.ShowForState(ConnectionState.Connecting);

            Assert.That(_messageText.text, Is.EqualTo("Connecting to GC2..."));
        }

        [Test]
        public void ShowForState_Connecting_SetsInfoColor()
        {
            _toast.ShowForState(ConnectionState.Connecting);

            Assert.That(_backgroundImage.color, Is.EqualTo(UITheme.ToastInfo));
        }

        [Test]
        public void ShowForState_DeviceNotFound_SetsCorrectMessage()
        {
            _toast.ShowForState(ConnectionState.DeviceNotFound);

            Assert.That(_messageText.text, Is.EqualTo("No GC2 Device Found"));
        }

        [Test]
        public void ShowForState_DeviceNotFound_SetsWarningColor()
        {
            _toast.ShowForState(ConnectionState.DeviceNotFound);

            Assert.That(_backgroundImage.color, Is.EqualTo(UITheme.ToastWarning));
        }

        [Test]
        public void ShowForState_Failed_SetsCorrectMessage()
        {
            _toast.ShowForState(ConnectionState.Failed);

            Assert.That(_messageText.text, Is.EqualTo("Connection Failed"));
        }

        [Test]
        public void ShowForState_Failed_SetsErrorColor()
        {
            _toast.ShowForState(ConnectionState.Failed);

            Assert.That(_backgroundImage.color, Is.EqualTo(UITheme.ToastError));
        }

        #endregion

        #region ShouldShowToast Tests

        [Test]
        public void ShouldShowToast_Connected_ReturnsTrue()
        {
            Assert.That(ConnectionToast.ShouldShowToast(ConnectionState.Connected), Is.True);
        }

        [Test]
        public void ShouldShowToast_Disconnected_ReturnsTrue()
        {
            Assert.That(ConnectionToast.ShouldShowToast(ConnectionState.Disconnected), Is.True);
        }

        [Test]
        public void ShouldShowToast_Failed_ReturnsTrue()
        {
            Assert.That(ConnectionToast.ShouldShowToast(ConnectionState.Failed), Is.True);
        }

        [Test]
        public void ShouldShowToast_DeviceNotFound_ReturnsTrue()
        {
            Assert.That(ConnectionToast.ShouldShowToast(ConnectionState.DeviceNotFound), Is.True);
        }

        [Test]
        public void ShouldShowToast_Connecting_ReturnsFalse()
        {
            // Connecting is a transient state - don't spam toasts
            Assert.That(ConnectionToast.ShouldShowToast(ConnectionState.Connecting), Is.False);
        }

        #endregion

        #region Duration Tests

        [Test]
        public void DefaultDuration_IsThreeSeconds()
        {
            Assert.That(ConnectionToast.DefaultDuration, Is.EqualTo(3f));
        }

        #endregion
    }
}
