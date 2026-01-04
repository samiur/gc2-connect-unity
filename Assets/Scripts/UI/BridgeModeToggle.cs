// ABOUTME: UI component for enabling/disabling Bridge Mode with GSPro configuration.
// ABOUTME: Shows toggle, host/port inputs, battery warning, and connection status.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.UI
{
    /// <summary>
    /// UI component for enabling and configuring Bridge Mode.
    /// Includes toggle, GSPro connection settings, and status display.
    /// </summary>
    public class BridgeModeToggle : MonoBehaviour
    {
        #region Static Constants

        /// <summary>Default GSPro host.</summary>
        public const string DefaultHost = "localhost";

        /// <summary>Default GSPro port.</summary>
        public const int DefaultPort = 921;

        /// <summary>Green color for connected status.</summary>
        public static readonly Color ConnectedColor = new Color(0.2f, 0.9f, 0.2f, 1f);

        /// <summary>Gray color for disconnected status.</summary>
        public static readonly Color DisconnectedColor = new Color(0.6f, 0.6f, 0.6f, 1f);

        /// <summary>Yellow color for connecting status.</summary>
        public static readonly Color ConnectingColor = new Color(1f, 0.8f, 0.2f, 1f);

        /// <summary>Red color for error status.</summary>
        public static readonly Color ErrorColor = new Color(0.9f, 0.3f, 0.3f, 1f);

        #endregion

        #region Serialized Fields

        [Header("Main Toggle")]
        [SerializeField] private Toggle _enableToggle;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _descriptionText;

        [Header("Configuration Panel")]
        [SerializeField] private CanvasGroup _configPanel;
        [SerializeField] private TMP_InputField _hostInput;
        [SerializeField] private TMP_InputField _portInput;
        [SerializeField] private Button _connectButton;
        [SerializeField] private TextMeshProUGUI _connectButtonText;
        [SerializeField] private TextMeshProUGUI _statusText;

        [Header("Warnings")]
        [SerializeField] private TextMeshProUGUI _batteryWarningText;

        #endregion

        #region Private Fields

        private bool _isEnabled;
        private string _host = DefaultHost;
        private int _port = DefaultPort;
        private bool _isConnected;

        #endregion

        #region Public Properties

        /// <summary>Whether Bridge Mode is enabled.</summary>
        public bool IsEnabled => _isEnabled;

        /// <summary>GSPro host address.</summary>
        public string Host => _host;

        /// <summary>GSPro port.</summary>
        public int Port => _port;

        /// <summary>Whether currently connected to GSPro.</summary>
        public bool IsConnected => _isConnected;

        #endregion

        #region Events

        /// <summary>Fired when Bridge Mode enabled state changes.</summary>
        public event Action<bool> OnEnabledChanged;

        /// <summary>Fired when host address changes.</summary>
        public event Action<string> OnHostChanged;

        /// <summary>Fired when port changes.</summary>
        public event Action<int> OnPortChanged;

        /// <summary>Fired when connect button is clicked.</summary>
        public event Action OnConnectClicked;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeUI();
            WireUpEvents();
        }

        private void OnDestroy()
        {
            UnwireEvents();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the enabled state of Bridge Mode.
        /// </summary>
        /// <param name="enabled">Whether to enable Bridge Mode.</param>
        public void SetEnabled(bool enabled)
        {
            if (_isEnabled == enabled) return;

            _isEnabled = enabled;

            // Update toggle UI
            if (_enableToggle != null)
            {
                _enableToggle.SetIsOnWithoutNotify(enabled);
            }

            // Show/hide config panel
            UpdateConfigPanelVisibility();

            OnEnabledChanged?.Invoke(enabled);
        }

        /// <summary>
        /// Sets the GSPro host address.
        /// </summary>
        /// <param name="host">Host address.</param>
        public void SetHost(string host)
        {
            _host = string.IsNullOrEmpty(host) ? DefaultHost : host;

            if (_hostInput != null)
            {
                _hostInput.SetTextWithoutNotify(_host);
            }
        }

        /// <summary>
        /// Sets the GSPro port.
        /// </summary>
        /// <param name="port">Port number.</param>
        public void SetPort(int port)
        {
            _port = port <= 0 ? DefaultPort : port;

            if (_portInput != null)
            {
                _portInput.SetTextWithoutNotify(_port.ToString());
            }
        }

        /// <summary>
        /// Sets both host and port configuration.
        /// </summary>
        /// <param name="host">Host address.</param>
        /// <param name="port">Port number.</param>
        public void SetConfiguration(string host, int port)
        {
            SetHost(host);
            SetPort(port);
        }

        /// <summary>
        /// Updates the status text display.
        /// </summary>
        /// <param name="status">Status message to display.</param>
        /// <param name="isConnected">Whether connected (affects color).</param>
        /// <param name="isError">Whether this is an error state.</param>
        public void UpdateStatus(string status, bool isConnected = false, bool isError = false)
        {
            _isConnected = isConnected;

            if (_statusText != null)
            {
                _statusText.text = status;
                _statusText.color = isError ? ErrorColor
                    : isConnected ? ConnectedColor
                    : DisconnectedColor;
            }
        }

        /// <summary>
        /// Shows or hides the battery warning.
        /// </summary>
        /// <param name="show">Whether to show the warning.</param>
        public void ShowBatteryWarning(bool show)
        {
            if (_batteryWarningText != null)
            {
                _batteryWarningText.gameObject.SetActive(show);
            }
        }

        /// <summary>
        /// Enables or disables the connect button.
        /// </summary>
        /// <param name="enabled">Whether the button should be interactable.</param>
        public void SetConnectButtonEnabled(bool enabled)
        {
            if (_connectButton != null)
            {
                _connectButton.interactable = enabled;
            }
        }

        /// <summary>
        /// Sets the connect button text.
        /// </summary>
        /// <param name="text">Button text.</param>
        public void SetConnectButtonText(string text)
        {
            if (_connectButtonText != null)
            {
                _connectButtonText.text = text;
            }
            else if (_connectButton != null)
            {
                // Try to find text component in button
                var buttonText = _connectButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = text;
                }
            }
        }

        /// <summary>
        /// Sets references for testing.
        /// </summary>
        internal void SetReferences(Toggle enableToggle, TMP_InputField hostInput,
            TMP_InputField portInput, TextMeshProUGUI statusText,
            TextMeshProUGUI batteryWarningText, Button connectButton, CanvasGroup configPanel)
        {
            _enableToggle = enableToggle;
            _hostInput = hostInput;
            _portInput = portInput;
            _statusText = statusText;
            _batteryWarningText = batteryWarningText;
            _connectButton = connectButton;
            _configPanel = configPanel;

            // Initialize
            if (_enableToggle != null)
            {
                _enableToggle.SetIsOnWithoutNotify(false);
            }

            if (_hostInput != null)
            {
                _hostInput.SetTextWithoutNotify(_host);
            }

            if (_portInput != null)
            {
                _portInput.SetTextWithoutNotify(_port.ToString());
            }

            if (_configPanel != null)
            {
                _configPanel.alpha = 0f;
            }

            if (_batteryWarningText != null)
            {
                _batteryWarningText.gameObject.SetActive(false);
            }

            WireUpEvents();
        }

        #endregion

        #region Private Methods

        private void InitializeUI()
        {
            // Set default values in input fields
            if (_hostInput != null)
            {
                _hostInput.text = _host;
            }

            if (_portInput != null)
            {
                _portInput.text = _port.ToString();
            }

            // Initialize config panel visibility
            UpdateConfigPanelVisibility();

            // Hide battery warning initially
            if (_batteryWarningText != null)
            {
                _batteryWarningText.gameObject.SetActive(false);
            }
        }

        private void WireUpEvents()
        {
            if (_enableToggle != null)
            {
                _enableToggle.onValueChanged.AddListener(HandleToggleChanged);
            }

            if (_hostInput != null)
            {
                _hostInput.onEndEdit.AddListener(HandleHostChanged);
            }

            if (_portInput != null)
            {
                _portInput.onEndEdit.AddListener(HandlePortChanged);
            }

            if (_connectButton != null)
            {
                _connectButton.onClick.AddListener(HandleConnectClicked);
            }
        }

        private void UnwireEvents()
        {
            if (_enableToggle != null)
            {
                _enableToggle.onValueChanged.RemoveListener(HandleToggleChanged);
            }

            if (_hostInput != null)
            {
                _hostInput.onEndEdit.RemoveListener(HandleHostChanged);
            }

            if (_portInput != null)
            {
                _portInput.onEndEdit.RemoveListener(HandlePortChanged);
            }

            if (_connectButton != null)
            {
                _connectButton.onClick.RemoveListener(HandleConnectClicked);
            }
        }

        private void UpdateConfigPanelVisibility()
        {
            if (_configPanel != null)
            {
                _configPanel.alpha = _isEnabled ? 1f : 0f;
                _configPanel.interactable = _isEnabled;
                _configPanel.blocksRaycasts = _isEnabled;
            }
        }

        private void HandleToggleChanged(bool isOn)
        {
            SetEnabled(isOn);
        }

        private void HandleHostChanged(string value)
        {
            _host = string.IsNullOrEmpty(value) ? DefaultHost : value;
            OnHostChanged?.Invoke(_host);
        }

        private void HandlePortChanged(string value)
        {
            if (int.TryParse(value, out int port) && port > 0)
            {
                _port = port;
            }
            else
            {
                _port = DefaultPort;
                if (_portInput != null)
                {
                    _portInput.SetTextWithoutNotify(_port.ToString());
                }
            }
            OnPortChanged?.Invoke(_port);
        }

        private void HandleConnectClicked()
        {
            OnConnectClicked?.Invoke();
        }

        #endregion
    }
}
