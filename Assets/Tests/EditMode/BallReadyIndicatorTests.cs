// ABOUTME: Unit tests for BallReadyIndicator UI component that shows device ready and ball detected status.
// ABOUTME: Tests visual states, events, and IsReadyToHit property for all connection/device combinations.

using NUnit.Framework;
using OpenRange.Core;
using OpenRange.GC2;
using OpenRange.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class BallReadyIndicatorTests
    {
        private GameObject _testGO;
        private BallReadyIndicator _indicator;
        private Image _statusIcon;
        private TextMeshProUGUI _statusText;
        private CanvasGroup _canvasGroup;

        [SetUp]
        public void SetUp()
        {
            // Create test hierarchy
            _testGO = new GameObject("BallReadyIndicator");
            _canvasGroup = _testGO.AddComponent<CanvasGroup>();

            // Create status icon
            var iconGO = new GameObject("StatusIcon");
            iconGO.transform.SetParent(_testGO.transform);
            _statusIcon = iconGO.AddComponent<Image>();

            // Create status text
            var textGO = new GameObject("StatusText");
            textGO.transform.SetParent(_testGO.transform);
            _statusText = textGO.AddComponent<TextMeshProUGUI>();

            // Add component and set references
            _indicator = _testGO.AddComponent<BallReadyIndicator>();
            _indicator.SetReferences(_statusIcon, _statusText, _canvasGroup);
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
        public void InitialState_StatusIcon_IsNotNull()
        {
            Assert.That(_indicator.StatusIcon, Is.Not.Null);
        }

        [Test]
        public void InitialState_StatusText_IsNotNull()
        {
            Assert.That(_indicator.StatusText, Is.Not.Null);
        }

        [Test]
        public void InitialState_IsReadyToHit_IsFalse()
        {
            Assert.That(_indicator.IsReadyToHit, Is.False);
        }

        [Test]
        public void InitialState_CurrentVisualState_IsDisconnected()
        {
            Assert.That(_indicator.CurrentVisualState, Is.EqualTo(BallReadyState.Disconnected));
        }

        #endregion

        #region Visual State Tests - Disconnected

        [Test]
        public void UpdateState_Disconnected_ShowsGrayColor()
        {
            _indicator.UpdateState(ConnectionState.Disconnected, null);

            Assert.That(_statusIcon.color, Is.EqualTo(BallReadyIndicator.DisconnectedColor));
        }

        [Test]
        public void UpdateState_Disconnected_ShowsConnectText()
        {
            _indicator.UpdateState(ConnectionState.Disconnected, null);

            Assert.That(_statusText.text, Is.EqualTo("Connect GC2"));
        }

        [Test]
        public void UpdateState_DeviceNotFound_ShowsGrayColor()
        {
            _indicator.UpdateState(ConnectionState.DeviceNotFound, null);

            Assert.That(_statusIcon.color, Is.EqualTo(BallReadyIndicator.DisconnectedColor));
        }

        [Test]
        public void UpdateState_DeviceNotFound_ShowsConnectText()
        {
            _indicator.UpdateState(ConnectionState.DeviceNotFound, null);

            Assert.That(_statusText.text, Is.EqualTo("Connect GC2"));
        }

        [Test]
        public void UpdateState_Failed_ShowsGrayColor()
        {
            _indicator.UpdateState(ConnectionState.Failed, null);

            Assert.That(_statusIcon.color, Is.EqualTo(BallReadyIndicator.DisconnectedColor));
        }

        #endregion

        #region Visual State Tests - Warming Up

        [Test]
        public void UpdateState_ConnectedNotReady_ShowsYellowColor()
        {
            var deviceStatus = new GC2DeviceStatus(1, 0); // FLAGS=1 (not ready), BALLS=0

            _indicator.UpdateState(ConnectionState.Connected, deviceStatus);

            Assert.That(_statusIcon.color, Is.EqualTo(BallReadyIndicator.WarmingUpColor));
        }

        [Test]
        public void UpdateState_ConnectedNotReady_ShowsWarmingUpText()
        {
            var deviceStatus = new GC2DeviceStatus(1, 0); // FLAGS=1 (not ready), BALLS=0

            _indicator.UpdateState(ConnectionState.Connected, deviceStatus);

            Assert.That(_statusText.text, Is.EqualTo("Warming Up..."));
        }

        [Test]
        public void UpdateState_Connecting_ShowsYellowColor()
        {
            _indicator.UpdateState(ConnectionState.Connecting, null);

            Assert.That(_statusIcon.color, Is.EqualTo(BallReadyIndicator.WarmingUpColor));
        }

        [Test]
        public void UpdateState_Connecting_ShowsConnectingText()
        {
            _indicator.UpdateState(ConnectionState.Connecting, null);

            Assert.That(_statusText.text, Is.EqualTo("Connecting..."));
        }

        #endregion

        #region Visual State Tests - Place Ball

        [Test]
        public void UpdateState_ReadyNoBall_ShowsGreenOutlineColor()
        {
            var deviceStatus = new GC2DeviceStatus(7, 0); // FLAGS=7 (ready), BALLS=0

            _indicator.UpdateState(ConnectionState.Connected, deviceStatus);

            Assert.That(_statusIcon.color, Is.EqualTo(BallReadyIndicator.PlaceBallColor));
        }

        [Test]
        public void UpdateState_ReadyNoBall_ShowsPlaceBallText()
        {
            var deviceStatus = new GC2DeviceStatus(7, 0); // FLAGS=7 (ready), BALLS=0

            _indicator.UpdateState(ConnectionState.Connected, deviceStatus);

            Assert.That(_statusText.text, Is.EqualTo("Place Ball"));
        }

        #endregion

        #region Visual State Tests - Ready

        [Test]
        public void UpdateState_ReadyWithBall_ShowsSolidGreenColor()
        {
            var deviceStatus = new GC2DeviceStatus(7, 1); // FLAGS=7 (ready), BALLS=1

            _indicator.UpdateState(ConnectionState.Connected, deviceStatus);

            Assert.That(_statusIcon.color, Is.EqualTo(BallReadyIndicator.ReadyColor));
        }

        [Test]
        public void UpdateState_ReadyWithBall_ShowsReadyText()
        {
            var deviceStatus = new GC2DeviceStatus(7, 1); // FLAGS=7 (ready), BALLS=1

            _indicator.UpdateState(ConnectionState.Connected, deviceStatus);

            Assert.That(_statusText.text, Is.EqualTo("READY"));
        }

        #endregion

        #region IsReadyToHit Property Tests

        [Test]
        public void IsReadyToHit_Disconnected_ReturnsFalse()
        {
            _indicator.UpdateState(ConnectionState.Disconnected, null);

            Assert.That(_indicator.IsReadyToHit, Is.False);
        }

        [Test]
        public void IsReadyToHit_ConnectedNotReady_ReturnsFalse()
        {
            var deviceStatus = new GC2DeviceStatus(1, 1); // FLAGS=1 (not ready), BALLS=1

            _indicator.UpdateState(ConnectionState.Connected, deviceStatus);

            Assert.That(_indicator.IsReadyToHit, Is.False);
        }

        [Test]
        public void IsReadyToHit_ReadyNoBall_ReturnsFalse()
        {
            var deviceStatus = new GC2DeviceStatus(7, 0); // FLAGS=7 (ready), BALLS=0

            _indicator.UpdateState(ConnectionState.Connected, deviceStatus);

            Assert.That(_indicator.IsReadyToHit, Is.False);
        }

        [Test]
        public void IsReadyToHit_ReadyWithBall_ReturnsTrue()
        {
            var deviceStatus = new GC2DeviceStatus(7, 1); // FLAGS=7 (ready), BALLS=1

            _indicator.UpdateState(ConnectionState.Connected, deviceStatus);

            Assert.That(_indicator.IsReadyToHit, Is.True);
        }

        [Test]
        public void IsReadyToHit_NullDeviceStatus_ReturnsFalse()
        {
            _indicator.UpdateState(ConnectionState.Connected, null);

            Assert.That(_indicator.IsReadyToHit, Is.False);
        }

        #endregion

        #region Event Tests

        [Test]
        public void UpdateState_ToReady_FiresOnReadyStateChanged()
        {
            bool eventFired = false;
            bool receivedValue = false;
            _indicator.OnReadyStateChanged += value =>
            {
                eventFired = true;
                receivedValue = value;
            };
            var deviceStatus = new GC2DeviceStatus(7, 1);

            _indicator.UpdateState(ConnectionState.Connected, deviceStatus);

            Assert.That(eventFired, Is.True);
            Assert.That(receivedValue, Is.True);
        }

        [Test]
        public void UpdateState_FromReadyToNotReady_FiresOnReadyStateChanged()
        {
            var readyStatus = new GC2DeviceStatus(7, 1);
            var notReadyStatus = new GC2DeviceStatus(7, 0);
            _indicator.UpdateState(ConnectionState.Connected, readyStatus);

            bool eventFired = false;
            bool receivedValue = true;
            _indicator.OnReadyStateChanged += value =>
            {
                eventFired = true;
                receivedValue = value;
            };

            _indicator.UpdateState(ConnectionState.Connected, notReadyStatus);

            Assert.That(eventFired, Is.True);
            Assert.That(receivedValue, Is.False);
        }

        [Test]
        public void UpdateState_SameReadyState_DoesNotFireEvent()
        {
            var deviceStatus = new GC2DeviceStatus(7, 1);
            _indicator.UpdateState(ConnectionState.Connected, deviceStatus);

            int eventCount = 0;
            _indicator.OnReadyStateChanged += _ => eventCount++;

            _indicator.UpdateState(ConnectionState.Connected, deviceStatus);

            Assert.That(eventCount, Is.EqualTo(0));
        }

        [Test]
        public void UpdateState_VisualStateChanges_FiresOnVisualStateChanged()
        {
            BallReadyState? receivedState = null;
            _indicator.OnVisualStateChanged += state => receivedState = state;

            var deviceStatus = new GC2DeviceStatus(7, 0);
            _indicator.UpdateState(ConnectionState.Connected, deviceStatus);

            Assert.That(receivedState, Is.EqualTo(BallReadyState.PlaceBall));
        }

        #endregion

        #region Visual State Property Tests

        [Test]
        public void CurrentVisualState_Disconnected_ReturnsDisconnected()
        {
            _indicator.UpdateState(ConnectionState.Disconnected, null);

            Assert.That(_indicator.CurrentVisualState, Is.EqualTo(BallReadyState.Disconnected));
        }

        [Test]
        public void CurrentVisualState_Connecting_ReturnsWarmingUp()
        {
            _indicator.UpdateState(ConnectionState.Connecting, null);

            Assert.That(_indicator.CurrentVisualState, Is.EqualTo(BallReadyState.WarmingUp));
        }

        [Test]
        public void CurrentVisualState_ConnectedNotReady_ReturnsWarmingUp()
        {
            var deviceStatus = new GC2DeviceStatus(1, 0);
            _indicator.UpdateState(ConnectionState.Connected, deviceStatus);

            Assert.That(_indicator.CurrentVisualState, Is.EqualTo(BallReadyState.WarmingUp));
        }

        [Test]
        public void CurrentVisualState_ReadyNoBall_ReturnsPlaceBall()
        {
            var deviceStatus = new GC2DeviceStatus(7, 0);
            _indicator.UpdateState(ConnectionState.Connected, deviceStatus);

            Assert.That(_indicator.CurrentVisualState, Is.EqualTo(BallReadyState.PlaceBall));
        }

        [Test]
        public void CurrentVisualState_ReadyWithBall_ReturnsReady()
        {
            var deviceStatus = new GC2DeviceStatus(7, 1);
            _indicator.UpdateState(ConnectionState.Connected, deviceStatus);

            Assert.That(_indicator.CurrentVisualState, Is.EqualTo(BallReadyState.Ready));
        }

        #endregion

        #region Visibility Tests

        [Test]
        public void Show_SetsVisibleTrue()
        {
            _indicator.Hide(false);

            _indicator.Show(false);

            Assert.That(_indicator.IsVisible, Is.True);
        }

        [Test]
        public void Hide_SetsVisibleFalse()
        {
            _indicator.Show(false);

            _indicator.Hide(false);

            Assert.That(_indicator.IsVisible, Is.False);
        }

        [Test]
        public void Show_WithoutAnimation_SetsAlphaToOne()
        {
            _canvasGroup.alpha = 0f;

            _indicator.Show(false);

            Assert.That(_canvasGroup.alpha, Is.EqualTo(1f));
        }

        [Test]
        public void Hide_WithoutAnimation_SetsAlphaToZero()
        {
            _canvasGroup.alpha = 1f;

            _indicator.Hide(false);

            Assert.That(_canvasGroup.alpha, Is.EqualTo(0f));
        }

        #endregion

        #region Static Helper Tests

        [Test]
        public void GetStateText_Disconnected_ReturnsConnectGC2()
        {
            string text = BallReadyIndicator.GetStateText(BallReadyState.Disconnected);

            Assert.That(text, Is.EqualTo("Connect GC2"));
        }

        [Test]
        public void GetStateText_WarmingUp_ReturnsWarmingUp()
        {
            string text = BallReadyIndicator.GetStateText(BallReadyState.WarmingUp);

            Assert.That(text, Is.EqualTo("Warming Up..."));
        }

        [Test]
        public void GetStateText_PlaceBall_ReturnsPlaceBall()
        {
            string text = BallReadyIndicator.GetStateText(BallReadyState.PlaceBall);

            Assert.That(text, Is.EqualTo("Place Ball"));
        }

        [Test]
        public void GetStateText_Ready_ReturnsREADY()
        {
            string text = BallReadyIndicator.GetStateText(BallReadyState.Ready);

            Assert.That(text, Is.EqualTo("READY"));
        }

        [Test]
        public void GetStateColor_Disconnected_ReturnsGray()
        {
            Color color = BallReadyIndicator.GetStateColor(BallReadyState.Disconnected);

            Assert.That(color, Is.EqualTo(BallReadyIndicator.DisconnectedColor));
        }

        [Test]
        public void GetStateColor_WarmingUp_ReturnsYellow()
        {
            Color color = BallReadyIndicator.GetStateColor(BallReadyState.WarmingUp);

            Assert.That(color, Is.EqualTo(BallReadyIndicator.WarmingUpColor));
        }

        [Test]
        public void GetStateColor_PlaceBall_ReturnsGreenOutline()
        {
            Color color = BallReadyIndicator.GetStateColor(BallReadyState.PlaceBall);

            Assert.That(color, Is.EqualTo(BallReadyIndicator.PlaceBallColor));
        }

        [Test]
        public void GetStateColor_Ready_ReturnsSolidGreen()
        {
            Color color = BallReadyIndicator.GetStateColor(BallReadyState.Ready);

            Assert.That(color, Is.EqualTo(BallReadyIndicator.ReadyColor));
        }

        #endregion

        #region Null Handling Tests

        [Test]
        public void UpdateState_WithNullIcon_DoesNotThrow()
        {
            _indicator.SetReferences(null, _statusText, _canvasGroup);

            Assert.DoesNotThrow(() => _indicator.UpdateState(ConnectionState.Connected, new GC2DeviceStatus(7, 1)));
        }

        [Test]
        public void UpdateState_WithNullText_DoesNotThrow()
        {
            _indicator.SetReferences(_statusIcon, null, _canvasGroup);

            Assert.DoesNotThrow(() => _indicator.UpdateState(ConnectionState.Connected, new GC2DeviceStatus(7, 1)));
        }

        [Test]
        public void Show_WithNullCanvasGroup_DoesNotThrow()
        {
            _indicator.SetReferences(_statusIcon, _statusText, null);

            Assert.DoesNotThrow(() => _indicator.Show(false));
        }

        #endregion

        #region Pulse Animation Tests

        [Test]
        public void UpdateState_ToReady_SetsPulsingTrue()
        {
            var deviceStatus = new GC2DeviceStatus(7, 1);

            _indicator.UpdateState(ConnectionState.Connected, deviceStatus);

            Assert.That(_indicator.IsPulsing, Is.True);
        }

        [Test]
        public void UpdateState_FromReadyToNotReady_SetsPulsingFalse()
        {
            var readyStatus = new GC2DeviceStatus(7, 1);
            var notReadyStatus = new GC2DeviceStatus(7, 0);
            _indicator.UpdateState(ConnectionState.Connected, readyStatus);

            _indicator.UpdateState(ConnectionState.Connected, notReadyStatus);

            Assert.That(_indicator.IsPulsing, Is.False);
        }

        [Test]
        public void StopPulse_SetsPulsingFalse()
        {
            var deviceStatus = new GC2DeviceStatus(7, 1);
            _indicator.UpdateState(ConnectionState.Connected, deviceStatus);

            _indicator.StopPulse();

            Assert.That(_indicator.IsPulsing, Is.False);
        }

        #endregion
    }
}
