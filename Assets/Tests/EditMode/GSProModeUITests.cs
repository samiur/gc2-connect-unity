// ABOUTME: Unit tests for GSProModeUI component.
// ABOUTME: Tests mode toggle, connection status, and readiness indicators.

using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpenRange.UI;
using OpenRange.Network;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class GSProModeUITests
    {
        private GameObject _rootObject;
        private GSProModeUI _ui;
        private Toggle _modeToggle;
        private TextMeshProUGUI _modeLabel;
        private Image _connectionIndicator;
        private TextMeshProUGUI _connectionText;
        private Button _connectButton;
        private TextMeshProUGUI _connectButtonText;
        private Image _readyIndicator;
        private TextMeshProUGUI _readyText;
        private Image _ballIndicator;
        private TextMeshProUGUI _ballText;
        private TMP_InputField _hostInput;
        private TMP_InputField _portInput;
        private GameObject _configPanel;

        [SetUp]
        public void SetUp()
        {
            _rootObject = new GameObject("GSProModeUI");
            _ui = _rootObject.AddComponent<GSProModeUI>();

            CreateUIComponents();

            _ui.SetReferences(
                _modeToggle,
                _modeLabel,
                _connectionIndicator,
                _connectionText,
                _connectButton,
                _connectButtonText,
                _readyIndicator,
                _readyText,
                _ballIndicator,
                _ballText,
                _hostInput,
                _portInput,
                _configPanel
            );
        }

        [TearDown]
        public void TearDown()
        {
            if (_rootObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_rootObject);
            }
        }

        private void CreateUIComponents()
        {
            // Mode toggle
            var toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(_rootObject.transform);
            _modeToggle = toggleObj.AddComponent<Toggle>();
            var toggleBg = new GameObject("Background").AddComponent<Image>();
            toggleBg.transform.SetParent(toggleObj.transform);
            _modeToggle.targetGraphic = toggleBg;

            // Mode label
            var labelObj = new GameObject("ModeLabel");
            labelObj.transform.SetParent(_rootObject.transform);
            _modeLabel = labelObj.AddComponent<TextMeshProUGUI>();

            // Connection indicator
            var connIndObj = new GameObject("ConnectionIndicator");
            connIndObj.transform.SetParent(_rootObject.transform);
            _connectionIndicator = connIndObj.AddComponent<Image>();

            // Connection text
            var connTextObj = new GameObject("ConnectionText");
            connTextObj.transform.SetParent(_rootObject.transform);
            _connectionText = connTextObj.AddComponent<TextMeshProUGUI>();

            // Connect button
            var buttonObj = new GameObject("ConnectButton");
            buttonObj.transform.SetParent(_rootObject.transform);
            _connectButton = buttonObj.AddComponent<Button>();
            var buttonImage = buttonObj.AddComponent<Image>();
            _connectButton.targetGraphic = buttonImage;

            // Connect button text
            var buttonTextObj = new GameObject("ButtonText");
            buttonTextObj.transform.SetParent(buttonObj.transform);
            _connectButtonText = buttonTextObj.AddComponent<TextMeshProUGUI>();

            // Ready indicator
            var readyIndObj = new GameObject("ReadyIndicator");
            readyIndObj.transform.SetParent(_rootObject.transform);
            _readyIndicator = readyIndObj.AddComponent<Image>();

            // Ready text
            var readyTextObj = new GameObject("ReadyText");
            readyTextObj.transform.SetParent(_rootObject.transform);
            _readyText = readyTextObj.AddComponent<TextMeshProUGUI>();

            // Ball indicator
            var ballIndObj = new GameObject("BallIndicator");
            ballIndObj.transform.SetParent(_rootObject.transform);
            _ballIndicator = ballIndObj.AddComponent<Image>();

            // Ball text
            var ballTextObj = new GameObject("BallText");
            ballTextObj.transform.SetParent(_rootObject.transform);
            _ballText = ballTextObj.AddComponent<TextMeshProUGUI>();

            // Host input - simplified for testing
            var hostInputObj = new GameObject("HostInput");
            hostInputObj.transform.SetParent(_rootObject.transform);
            _hostInput = hostInputObj.AddComponent<TMP_InputField>();
            var hostTextArea = new GameObject("TextArea").AddComponent<RectTransform>();
            hostTextArea.transform.SetParent(hostInputObj.transform);
            var hostText = new GameObject("Text").AddComponent<TextMeshProUGUI>();
            hostText.transform.SetParent(hostTextArea.transform);
            _hostInput.textViewport = hostTextArea;
            _hostInput.textComponent = hostText;

            // Port input - simplified for testing
            var portInputObj = new GameObject("PortInput");
            portInputObj.transform.SetParent(_rootObject.transform);
            _portInput = portInputObj.AddComponent<TMP_InputField>();
            var portTextArea = new GameObject("TextArea").AddComponent<RectTransform>();
            portTextArea.transform.SetParent(portInputObj.transform);
            var portText = new GameObject("Text").AddComponent<TextMeshProUGUI>();
            portText.transform.SetParent(portTextArea.transform);
            _portInput.textViewport = portTextArea;
            _portInput.textComponent = portText;

            // Config panel
            _configPanel = new GameObject("ConfigPanel");
            _configPanel.transform.SetParent(_rootObject.transform);
        }

        #region Initial State Tests

        [Test]
        public void InitialState_IsNotGSProMode()
        {
            Assert.IsFalse(_ui.IsGSProMode);
        }

        [Test]
        public void InitialState_IsDisconnected()
        {
            Assert.AreEqual(GSProConnectionState.Disconnected, _ui.ConnectionState);
        }

        [Test]
        public void InitialState_IsNotReady()
        {
            Assert.IsFalse(_ui.IsReady);
        }

        [Test]
        public void InitialState_NoBallDetected()
        {
            Assert.IsFalse(_ui.BallDetected);
        }

        #endregion

        #region SetMode Tests

        [Test]
        public void SetMode_True_SetsGSProMode()
        {
            _ui.SetMode(true);

            Assert.IsTrue(_ui.IsGSProMode);
        }

        [Test]
        public void SetMode_False_SetsOpenRangeMode()
        {
            _ui.SetMode(true);
            _ui.SetMode(false);

            Assert.IsFalse(_ui.IsGSProMode);
        }

        [Test]
        public void SetMode_True_UpdatesModeLabel()
        {
            _ui.SetMode(true);

            Assert.AreEqual("GSPro Mode", _modeLabel.text);
        }

        [Test]
        public void SetMode_False_UpdatesModeLabel()
        {
            _ui.SetMode(false);

            Assert.AreEqual("Open Range Mode", _modeLabel.text);
        }

        [Test]
        public void SetMode_True_ShowsConfigPanel()
        {
            _ui.SetMode(true);

            Assert.IsTrue(_configPanel.activeSelf);
        }

        [Test]
        public void SetMode_False_HidesConfigPanel()
        {
            _ui.SetMode(true);
            _ui.SetMode(false);

            Assert.IsFalse(_configPanel.activeSelf);
        }

        [Test]
        public void SetMode_UpdatesToggleWithoutNotify()
        {
            bool eventFired = false;
            _modeToggle.onValueChanged.AddListener(_ => eventFired = true);

            _ui.SetMode(true);

            // SetIsOnWithoutNotify should not trigger the listener
            Assert.IsFalse(eventFired);
        }

        #endregion

        #region SetConnectionState Tests

        [Test]
        public void SetConnectionState_Connected_UpdatesState()
        {
            _ui.SetConnectionState(GSProConnectionState.Connected);

            Assert.AreEqual(GSProConnectionState.Connected, _ui.ConnectionState);
        }

        [Test]
        public void SetConnectionState_Connected_UpdatesText()
        {
            _ui.SetConnectionState(GSProConnectionState.Connected);

            Assert.AreEqual("Connected", _connectionText.text);
        }

        [Test]
        public void SetConnectionState_Connecting_UpdatesText()
        {
            _ui.SetConnectionState(GSProConnectionState.Connecting);

            Assert.AreEqual("Connecting...", _connectionText.text);
        }

        [Test]
        public void SetConnectionState_Disconnected_UpdatesText()
        {
            _ui.SetConnectionState(GSProConnectionState.Disconnected);

            Assert.AreEqual("Disconnected", _connectionText.text);
        }

        [Test]
        public void SetConnectionState_Failed_UpdatesText()
        {
            _ui.SetConnectionState(GSProConnectionState.Failed);

            Assert.AreEqual("Failed", _connectionText.text);
        }

        [Test]
        public void SetConnectionState_Connected_ButtonSaysDisconnect()
        {
            _ui.SetConnectionState(GSProConnectionState.Connected);

            Assert.AreEqual("Disconnect", _connectButtonText.text);
        }

        [Test]
        public void SetConnectionState_Disconnected_ButtonSaysConnect()
        {
            _ui.SetConnectionState(GSProConnectionState.Disconnected);

            Assert.AreEqual("Connect", _connectButtonText.text);
        }

        [Test]
        public void SetConnectionState_Connecting_DisablesButton()
        {
            _ui.SetConnectionState(GSProConnectionState.Connecting);

            Assert.IsFalse(_connectButton.interactable);
        }

        [Test]
        public void SetConnectionState_Connected_EnablesButton()
        {
            _ui.SetConnectionState(GSProConnectionState.Connected);

            Assert.IsTrue(_connectButton.interactable);
        }

        #endregion

        #region SetReadyState Tests

        [Test]
        public void SetReadyState_Ready_UpdatesIsReady()
        {
            _ui.SetReadyState(true, false);

            Assert.IsTrue(_ui.IsReady);
        }

        [Test]
        public void SetReadyState_BallDetected_UpdatesBallDetected()
        {
            _ui.SetReadyState(false, true);

            Assert.IsTrue(_ui.BallDetected);
        }

        [Test]
        public void SetReadyState_Ready_UpdatesText()
        {
            _ui.SetReadyState(true, false);

            Assert.AreEqual("Ready", _readyText.text);
        }

        [Test]
        public void SetReadyState_NotReady_UpdatesText()
        {
            _ui.SetReadyState(false, false);

            Assert.AreEqual("Not Ready", _readyText.text);
        }

        [Test]
        public void SetReadyState_BallDetected_UpdatesText()
        {
            _ui.SetReadyState(false, true);

            Assert.AreEqual("Ball", _ballText.text);
        }

        [Test]
        public void SetReadyState_NoBall_UpdatesText()
        {
            _ui.SetReadyState(false, false);

            Assert.AreEqual("No Ball", _ballText.text);
        }

        #endregion

        #region SetHostPort Tests

        [Test]
        public void SetHostPort_SetsHost()
        {
            _ui.SetHostPort("192.168.1.100", 921);

            Assert.AreEqual("192.168.1.100", _hostInput.text);
        }

        [Test]
        public void SetHostPort_SetsPort()
        {
            _ui.SetHostPort("localhost", 1234);

            Assert.AreEqual("1234", _portInput.text);
        }

        [Test]
        public void SetHostPort_NullHost_SetsDefault()
        {
            _ui.SetHostPort(null, 921);

            Assert.AreEqual("127.0.0.1", _hostInput.text);
        }

        [Test]
        public void Host_ReturnsInputValue()
        {
            _hostInput.text = "test.example.com";

            Assert.AreEqual("test.example.com", _ui.Host);
        }

        [Test]
        public void Port_ReturnsInputValue()
        {
            _portInput.text = "8888";

            Assert.AreEqual(8888, _ui.Port);
        }

        [Test]
        public void Port_InvalidValue_ReturnsDefault()
        {
            _portInput.text = "invalid";

            Assert.AreEqual(GSProClient.DefaultPort, _ui.Port);
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnModeChanged_FiresWhenToggled()
        {
            bool eventFired = false;
            bool receivedValue = false;
            _ui.OnModeChanged += value =>
            {
                eventFired = true;
                receivedValue = value;
            };

            _modeToggle.isOn = true;

            Assert.IsTrue(eventFired);
            Assert.IsTrue(receivedValue);
        }

        [Test]
        public void OnConnectClicked_FiresWhenDisconnected()
        {
            bool eventFired = false;
            _ui.OnConnectClicked += () => eventFired = true;
            _ui.SetConnectionState(GSProConnectionState.Disconnected);

            _connectButton.onClick.Invoke();

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void OnDisconnectClicked_FiresWhenConnected()
        {
            bool eventFired = false;
            _ui.OnDisconnectClicked += () => eventFired = true;
            _ui.SetConnectionState(GSProConnectionState.Connected);

            _connectButton.onClick.Invoke();

            Assert.IsTrue(eventFired);
        }

        #endregion

        #region SetClient Tests

        [Test]
        public void SetClient_Null_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _ui.SetClient(null));
        }

        [Test]
        public void SetClient_WithClient_DoesNotThrow()
        {
            var client = new GSProClient();

            Assert.DoesNotThrow(() => _ui.SetClient(client));

            client.Dispose();
        }

        [Test]
        public void SetClient_DisconnectedClient_ShowsDisconnected()
        {
            var client = new GSProClient();
            _ui.SetClient(client);

            Assert.AreEqual(GSProConnectionState.Disconnected, _ui.ConnectionState);

            client.Dispose();
        }

        #endregion
    }
}
