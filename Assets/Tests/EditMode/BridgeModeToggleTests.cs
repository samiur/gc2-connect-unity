// ABOUTME: Unit tests for BridgeModeToggle UI component.
// ABOUTME: Tests toggle state, GSPro config display, enable/disable functionality.

using NUnit.Framework;
using OpenRange.Core;
using OpenRange.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class BridgeModeToggleTests
    {
        private GameObject _testGO;
        private BridgeModeToggle _toggle;
        private Toggle _enableToggle;
        private TMP_InputField _hostInput;
        private TMP_InputField _portInput;
        private TextMeshProUGUI _statusText;
        private TextMeshProUGUI _batteryWarningText;
        private Button _connectButton;
        private CanvasGroup _configPanel;

        [SetUp]
        public void SetUp()
        {
            // Create test hierarchy
            _testGO = new GameObject("BridgeModeToggle");

            // Create enable toggle
            var toggleGO = new GameObject("EnableToggle");
            toggleGO.transform.SetParent(_testGO.transform);
            _enableToggle = toggleGO.AddComponent<Toggle>();

            // Create host input
            var hostInputGO = new GameObject("HostInput");
            hostInputGO.transform.SetParent(_testGO.transform);
            _hostInput = hostInputGO.AddComponent<TMP_InputField>();
            var hostTextGO = new GameObject("Text");
            hostTextGO.transform.SetParent(hostInputGO.transform);
            _hostInput.textComponent = hostTextGO.AddComponent<TextMeshProUGUI>();

            // Create port input
            var portInputGO = new GameObject("PortInput");
            portInputGO.transform.SetParent(_testGO.transform);
            _portInput = portInputGO.AddComponent<TMP_InputField>();
            var portTextGO = new GameObject("Text");
            portTextGO.transform.SetParent(portInputGO.transform);
            _portInput.textComponent = portTextGO.AddComponent<TextMeshProUGUI>();

            // Create status text
            var statusTextGO = new GameObject("StatusText");
            statusTextGO.transform.SetParent(_testGO.transform);
            _statusText = statusTextGO.AddComponent<TextMeshProUGUI>();

            // Create battery warning text
            var batteryWarningGO = new GameObject("BatteryWarning");
            batteryWarningGO.transform.SetParent(_testGO.transform);
            _batteryWarningText = batteryWarningGO.AddComponent<TextMeshProUGUI>();

            // Create connect button
            var connectButtonGO = new GameObject("ConnectButton");
            connectButtonGO.transform.SetParent(_testGO.transform);
            _connectButton = connectButtonGO.AddComponent<Button>();

            // Create config panel
            var configPanelGO = new GameObject("ConfigPanel");
            configPanelGO.transform.SetParent(_testGO.transform);
            _configPanel = configPanelGO.AddComponent<CanvasGroup>();

            // Add component and set references
            _toggle = _testGO.AddComponent<BridgeModeToggle>();
            _toggle.SetReferences(_enableToggle, _hostInput, _portInput, _statusText,
                _batteryWarningText, _connectButton, _configPanel);
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
        public void InitialState_IsNotEnabled()
        {
            Assert.That(_toggle.IsEnabled, Is.False);
        }

        [Test]
        public void InitialState_ToggleIsOff()
        {
            Assert.That(_enableToggle.isOn, Is.False);
        }

        [Test]
        public void InitialState_DefaultHost_IsLocalhost()
        {
            Assert.That(_toggle.Host, Is.EqualTo("localhost"));
        }

        [Test]
        public void InitialState_DefaultPort_Is921()
        {
            Assert.That(_toggle.Port, Is.EqualTo(921));
        }

        [Test]
        public void InitialState_ConfigPanel_IsHidden()
        {
            Assert.That(_configPanel.alpha, Is.EqualTo(0f));
        }

        #endregion

        #region Toggle State Tests

        [Test]
        public void SetEnabled_True_SetsIsEnabledTrue()
        {
            _toggle.SetEnabled(true);

            Assert.That(_toggle.IsEnabled, Is.True);
        }

        [Test]
        public void SetEnabled_True_TurnsToggleOn()
        {
            _toggle.SetEnabled(true);

            Assert.That(_enableToggle.isOn, Is.True);
        }

        [Test]
        public void SetEnabled_True_ShowsConfigPanel()
        {
            _toggle.SetEnabled(true);

            Assert.That(_configPanel.alpha, Is.EqualTo(1f));
        }

        [Test]
        public void SetEnabled_False_SetsIsEnabledFalse()
        {
            _toggle.SetEnabled(true);
            _toggle.SetEnabled(false);

            Assert.That(_toggle.IsEnabled, Is.False);
        }

        [Test]
        public void SetEnabled_False_HidesConfigPanel()
        {
            _toggle.SetEnabled(true);
            _toggle.SetEnabled(false);

            Assert.That(_configPanel.alpha, Is.EqualTo(0f));
        }

        [Test]
        public void SetEnabled_FiresOnEnabledChangedEvent()
        {
            bool? receivedValue = null;
            _toggle.OnEnabledChanged += value => receivedValue = value;

            _toggle.SetEnabled(true);

            Assert.That(receivedValue, Is.True);
        }

        [Test]
        public void SetEnabled_SameValue_DoesNotFireEvent()
        {
            int eventCount = 0;
            _toggle.OnEnabledChanged += _ => eventCount++;

            _toggle.SetEnabled(false);
            _toggle.SetEnabled(false);

            Assert.That(eventCount, Is.EqualTo(0));
        }

        #endregion

        #region Host/Port Configuration Tests

        [Test]
        public void SetHost_UpdatesHostProperty()
        {
            _toggle.SetHost("192.168.1.100");

            Assert.That(_toggle.Host, Is.EqualTo("192.168.1.100"));
        }

        [Test]
        public void SetHost_UpdatesInputField()
        {
            _toggle.SetHost("192.168.1.100");

            Assert.That(_hostInput.text, Is.EqualTo("192.168.1.100"));
        }

        [Test]
        public void SetPort_UpdatesPortProperty()
        {
            _toggle.SetPort(9001);

            Assert.That(_toggle.Port, Is.EqualTo(9001));
        }

        [Test]
        public void SetPort_UpdatesInputField()
        {
            _toggle.SetPort(9001);

            Assert.That(_portInput.text, Is.EqualTo("9001"));
        }

        [Test]
        public void SetHost_Empty_DefaultsToLocalhost()
        {
            _toggle.SetHost("");

            Assert.That(_toggle.Host, Is.EqualTo("localhost"));
        }

        [Test]
        public void SetPort_Zero_DefaultsTo921()
        {
            _toggle.SetPort(0);

            Assert.That(_toggle.Port, Is.EqualTo(921));
        }

        [Test]
        public void SetPort_Negative_DefaultsTo921()
        {
            _toggle.SetPort(-1);

            Assert.That(_toggle.Port, Is.EqualTo(921));
        }

        [Test]
        public void SetConfiguration_SetsHostAndPort()
        {
            _toggle.SetConfiguration("10.0.0.1", 8080);

            Assert.That(_toggle.Host, Is.EqualTo("10.0.0.1"));
            Assert.That(_toggle.Port, Is.EqualTo(8080));
        }

        #endregion

        #region Status Display Tests

        [Test]
        public void UpdateStatus_SetsStatusText()
        {
            _toggle.UpdateStatus("Connected to GSPro");

            Assert.That(_statusText.text, Is.EqualTo("Connected to GSPro"));
        }

        [Test]
        public void UpdateStatus_Connected_SetsGreenColor()
        {
            _toggle.UpdateStatus("Connected", isConnected: true);

            Assert.That(_statusText.color, Is.EqualTo(BridgeModeToggle.ConnectedColor));
        }

        [Test]
        public void UpdateStatus_Disconnected_SetsGrayColor()
        {
            _toggle.UpdateStatus("Disconnected", isConnected: false);

            Assert.That(_statusText.color, Is.EqualTo(BridgeModeToggle.DisconnectedColor));
        }

        [Test]
        public void ShowBatteryWarning_ShowsWarningText()
        {
            _toggle.ShowBatteryWarning(true);

            Assert.That(_batteryWarningText.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void ShowBatteryWarning_False_HidesWarningText()
        {
            _toggle.ShowBatteryWarning(true);
            _toggle.ShowBatteryWarning(false);

            Assert.That(_batteryWarningText.gameObject.activeSelf, Is.False);
        }

        #endregion

        #region Connect Button Tests

        [Test]
        public void SetConnectButtonEnabled_True_EnablesButton()
        {
            _toggle.SetConnectButtonEnabled(true);

            Assert.That(_connectButton.interactable, Is.True);
        }

        [Test]
        public void SetConnectButtonEnabled_False_DisablesButton()
        {
            _toggle.SetConnectButtonEnabled(false);

            Assert.That(_connectButton.interactable, Is.False);
        }

        [Test]
        public void SetConnectButtonText_UpdatesButtonText()
        {
            // Add button text component
            var buttonTextGO = new GameObject("Text");
            buttonTextGO.transform.SetParent(_connectButton.transform);
            var buttonText = buttonTextGO.AddComponent<TextMeshProUGUI>();
            _toggle.SetConnectButtonText("Disconnect");

            // The button text should be updated if it exists
            Assert.That(buttonText.text, Is.EqualTo("Disconnect"));
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnConnectClicked_FiresWhenButtonClicked()
        {
            bool eventFired = false;
            _toggle.OnConnectClicked += () => eventFired = true;

            // Simulate button click
            _connectButton.onClick.Invoke();

            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void OnHostChanged_FiresWhenHostInputChanges()
        {
            string receivedHost = null;
            _toggle.OnHostChanged += host => receivedHost = host;

            _hostInput.onEndEdit.Invoke("newhost.local");

            Assert.That(receivedHost, Is.EqualTo("newhost.local"));
        }

        [Test]
        public void OnPortChanged_FiresWhenPortInputChanges()
        {
            int receivedPort = 0;
            _toggle.OnPortChanged += port => receivedPort = port;

            _portInput.onEndEdit.Invoke("8888");

            Assert.That(receivedPort, Is.EqualTo(8888));
        }

        #endregion

        #region Static Constants Tests

        [Test]
        public void DefaultPort_Is921()
        {
            Assert.That(BridgeModeToggle.DefaultPort, Is.EqualTo(921));
        }

        [Test]
        public void DefaultHost_IsLocalhost()
        {
            Assert.That(BridgeModeToggle.DefaultHost, Is.EqualTo("localhost"));
        }

        #endregion
    }
}
