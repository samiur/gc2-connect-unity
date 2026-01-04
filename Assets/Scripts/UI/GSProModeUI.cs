// ABOUTME: UI component for GSPro mode toggle and connection status.
// ABOUTME: Shows mode selection, connection state, and device readiness indicators.

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpenRange.Core;
using OpenRange.Network;
using OpenRange.GC2;
using OpenRange.Utilities;

namespace OpenRange.UI
{
    /// <summary>
    /// UI component for GSPro mode toggle and connection status display.
    /// Shows mode selection (OpenRange/GSPro), connection state, and device readiness.
    /// </summary>
    public class GSProModeUI : MonoBehaviour
    {
        [Header("Mode Selection")]
        [SerializeField] private Toggle _modeToggle;
        [SerializeField] private TextMeshProUGUI _modeLabel;

        [Header("Connection Status")]
        [SerializeField] private Image _connectionIndicator;
        [SerializeField] private TextMeshProUGUI _connectionText;
        [SerializeField] private Button _connectButton;
        [SerializeField] private TextMeshProUGUI _connectButtonText;

        [Header("Device Readiness")]
        [SerializeField] private Image _readyIndicator;
        [SerializeField] private TextMeshProUGUI _readyText;
        [SerializeField] private Image _ballIndicator;
        [SerializeField] private TextMeshProUGUI _ballText;

        [Header("Configuration")]
        [SerializeField] private TMP_InputField _hostInput;
        [SerializeField] private TMP_InputField _portInput;
        [SerializeField] private GameObject _configPanel;

        [Header("Colors")]
        [SerializeField] private Color _connectedColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _connectingColor = new Color(0.9f, 0.7f, 0.1f);
        [SerializeField] private Color _disconnectedColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color _readyColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _notReadyColor = new Color(0.5f, 0.5f, 0.5f);

        private GSProClient _client;
        private bool _isGSProMode;
        private GSProConnectionState _connectionState = GSProConnectionState.Disconnected;
        private bool _isReady;
        private bool _ballDetected;

        /// <summary>Current connection state.</summary>
        public GSProConnectionState ConnectionState => _connectionState;

        /// <summary>Whether GSPro mode is enabled.</summary>
        public bool IsGSProMode => _isGSProMode;

        /// <summary>Whether the device is ready.</summary>
        public bool IsReady => _isReady;

        /// <summary>Whether a ball is detected.</summary>
        public bool BallDetected => _ballDetected;

        /// <summary>The host address from input field.</summary>
        public string Host => _hostInput != null ? _hostInput.text : "127.0.0.1";

        /// <summary>The port from input field.</summary>
        public int Port => _portInput != null && int.TryParse(_portInput.text, out int port) ? port : GSProClient.DefaultPort;

        /// <summary>Fired when mode is toggled.</summary>
        public event Action<bool> OnModeChanged;

        /// <summary>Fired when connect button is clicked.</summary>
        public event Action OnConnectClicked;

        /// <summary>Fired when disconnect button is clicked.</summary>
        public event Action OnDisconnectClicked;

        private void Start()
        {
            SetupListeners();
            UpdateUI();
        }

        private void OnDestroy()
        {
            RemoveListeners();
            UnsubscribeFromClient();
        }

        /// <summary>
        /// Set the GSPro client to monitor.
        /// </summary>
        /// <param name="client">The GSPro client.</param>
        public void SetClient(GSProClient client)
        {
            UnsubscribeFromClient();
            _client = client;
            SubscribeToClient();
            UpdateConnectionState();
        }

        /// <summary>
        /// Set the mode toggle state.
        /// </summary>
        /// <param name="isGSProMode">Whether GSPro mode is enabled.</param>
        public void SetMode(bool isGSProMode)
        {
            _isGSProMode = isGSProMode;

            if (_modeToggle != null)
            {
                _modeToggle.SetIsOnWithoutNotify(isGSProMode);
            }

            UpdateModeLabel();
            UpdateConfigVisibility();
        }

        /// <summary>
        /// Set the connection state.
        /// </summary>
        /// <param name="state">The new connection state.</param>
        public void SetConnectionState(GSProConnectionState state)
        {
            _connectionState = state;
            UpdateConnectionUI();
        }

        /// <summary>
        /// Set the device readiness state.
        /// </summary>
        /// <param name="isReady">Whether the device is ready.</param>
        /// <param name="ballDetected">Whether a ball is detected.</param>
        public void SetReadyState(bool isReady, bool ballDetected)
        {
            _isReady = isReady;
            _ballDetected = ballDetected;
            UpdateReadinessUI();
        }

        /// <summary>
        /// Set the host and port values in the input fields.
        /// </summary>
        /// <param name="host">Host address.</param>
        /// <param name="port">Port number.</param>
        public void SetHostPort(string host, int port)
        {
            if (_hostInput != null)
            {
                _hostInput.text = host ?? "127.0.0.1";
            }

            if (_portInput != null)
            {
                _portInput.text = port.ToString();
            }
        }

        /// <summary>
        /// Set references for testing.
        /// </summary>
        public void SetReferences(
            Toggle modeToggle,
            TextMeshProUGUI modeLabel,
            Image connectionIndicator,
            TextMeshProUGUI connectionText,
            Button connectButton,
            TextMeshProUGUI connectButtonText,
            Image readyIndicator,
            TextMeshProUGUI readyText,
            Image ballIndicator,
            TextMeshProUGUI ballText,
            TMP_InputField hostInput,
            TMP_InputField portInput,
            GameObject configPanel)
        {
            _modeToggle = modeToggle;
            _modeLabel = modeLabel;
            _connectionIndicator = connectionIndicator;
            _connectionText = connectionText;
            _connectButton = connectButton;
            _connectButtonText = connectButtonText;
            _readyIndicator = readyIndicator;
            _readyText = readyText;
            _ballIndicator = ballIndicator;
            _ballText = ballText;
            _hostInput = hostInput;
            _portInput = portInput;
            _configPanel = configPanel;

            SetupListeners();
        }

        private void SetupListeners()
        {
            if (_modeToggle != null)
            {
                _modeToggle.onValueChanged.AddListener(HandleModeToggled);
            }

            if (_connectButton != null)
            {
                _connectButton.onClick.AddListener(HandleConnectClicked);
            }
        }

        private void RemoveListeners()
        {
            if (_modeToggle != null)
            {
                _modeToggle.onValueChanged.RemoveListener(HandleModeToggled);
            }

            if (_connectButton != null)
            {
                _connectButton.onClick.RemoveListener(HandleConnectClicked);
            }
        }

        private void SubscribeToClient()
        {
            if (_client != null)
            {
                _client.OnConnected += HandleClientConnected;
                _client.OnDisconnected += HandleClientDisconnected;
                _client.OnError += HandleClientError;
            }
        }

        private void UnsubscribeFromClient()
        {
            if (_client != null)
            {
                _client.OnConnected -= HandleClientConnected;
                _client.OnDisconnected -= HandleClientDisconnected;
                _client.OnError -= HandleClientError;
            }
        }

        private void HandleModeToggled(bool isOn)
        {
            _isGSProMode = isOn;
            UpdateModeLabel();
            UpdateConfigVisibility();
            OnModeChanged?.Invoke(isOn);
        }

        private void HandleConnectClicked()
        {
            if (_connectionState == GSProConnectionState.Connected)
            {
                OnDisconnectClicked?.Invoke();
            }
            else
            {
                OnConnectClicked?.Invoke();
            }
        }

        private void HandleClientConnected()
        {
            MainThreadDispatcher.Enqueue(() => SetConnectionState(GSProConnectionState.Connected));
        }

        private void HandleClientDisconnected()
        {
            MainThreadDispatcher.Enqueue(() => SetConnectionState(GSProConnectionState.Disconnected));
        }

        private void HandleClientError(string error)
        {
            Debug.LogWarning($"GSProModeUI: Client error - {error}");
        }

        private void UpdateConnectionState()
        {
            if (_client == null)
            {
                SetConnectionState(GSProConnectionState.Disconnected);
            }
            else if (_client.IsConnected)
            {
                SetConnectionState(GSProConnectionState.Connected);
            }
            else if (_client.IsReconnecting)
            {
                SetConnectionState(GSProConnectionState.Connecting);
            }
            else
            {
                SetConnectionState(GSProConnectionState.Disconnected);
            }
        }

        private void UpdateUI()
        {
            UpdateModeLabel();
            UpdateConnectionUI();
            UpdateReadinessUI();
            UpdateConfigVisibility();
        }

        private void UpdateModeLabel()
        {
            if (_modeLabel != null)
            {
                _modeLabel.text = _isGSProMode ? "GSPro Mode" : "Open Range Mode";
            }
        }

        private void UpdateConnectionUI()
        {
            if (_connectionIndicator != null)
            {
                _connectionIndicator.color = _connectionState switch
                {
                    GSProConnectionState.Connected => _connectedColor,
                    GSProConnectionState.Connecting => _connectingColor,
                    _ => _disconnectedColor
                };
            }

            if (_connectionText != null)
            {
                _connectionText.text = _connectionState switch
                {
                    GSProConnectionState.Connected => "Connected",
                    GSProConnectionState.Connecting => "Connecting...",
                    GSProConnectionState.Failed => "Failed",
                    _ => "Disconnected"
                };
            }

            if (_connectButtonText != null)
            {
                _connectButtonText.text = _connectionState == GSProConnectionState.Connected
                    ? "Disconnect"
                    : "Connect";
            }

            if (_connectButton != null)
            {
                _connectButton.interactable = _connectionState != GSProConnectionState.Connecting;
            }
        }

        private void UpdateReadinessUI()
        {
            if (_readyIndicator != null)
            {
                _readyIndicator.color = _isReady ? _readyColor : _notReadyColor;
            }

            if (_readyText != null)
            {
                _readyText.text = _isReady ? "Ready" : "Not Ready";
            }

            if (_ballIndicator != null)
            {
                _ballIndicator.color = _ballDetected ? _readyColor : _notReadyColor;
            }

            if (_ballText != null)
            {
                _ballText.text = _ballDetected ? "Ball" : "No Ball";
            }
        }

        private void UpdateConfigVisibility()
        {
            if (_configPanel != null)
            {
                _configPanel.SetActive(_isGSProMode);
            }
        }
    }

    /// <summary>
    /// GSPro connection state.
    /// </summary>
    public enum GSProConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Failed
    }
}
