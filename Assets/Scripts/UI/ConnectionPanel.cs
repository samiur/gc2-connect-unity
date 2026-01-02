// ABOUTME: Modal panel displaying detailed GC2 connection information and controls.
// ABOUTME: Shows device info, connection mode, last shot time, and connect/disconnect buttons.

using System;
using System.Collections;
using OpenRange.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.UI
{
    /// <summary>
    /// Modal panel showing detailed connection information.
    /// Provides connect/disconnect controls and device details.
    /// </summary>
    public class ConnectionPanel : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Status Display")]
        [SerializeField] private Image _statusDotImage;
        [SerializeField] private TextMeshProUGUI _statusText;

        [Header("Device Info")]
        [SerializeField] private TextMeshProUGUI _deviceInfoText;
        [SerializeField] private TextMeshProUGUI _modeText;
        [SerializeField] private TextMeshProUGUI _lastShotText;

        [Header("Buttons")]
        [SerializeField] private Button _connectButton;
        [SerializeField] private Button _disconnectButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _closeButton;

        [Header("Animation")]
        [SerializeField] private CanvasGroup _canvasGroup;

        #endregion

        #region Private Fields

        private ConnectionState _currentState = ConnectionState.Disconnected;
        private bool _isVisible;
        private Coroutine _animationCoroutine;

        #endregion

        #region Public Properties

        /// <summary>
        /// The current connection state being displayed.
        /// </summary>
        public ConnectionState CurrentState => _currentState;

        /// <summary>
        /// Whether the panel is visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the close button is clicked.
        /// </summary>
        public event Action OnCloseRequested;

        /// <summary>
        /// Fired when the connect button is clicked.
        /// </summary>
        public event Action OnConnectRequested;

        /// <summary>
        /// Fired when the disconnect button is clicked.
        /// </summary>
        public event Action OnDisconnectRequested;

        /// <summary>
        /// Fired when the retry button is clicked.
        /// </summary>
        public event Action OnRetryRequested;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            // Wire up button handlers
            if (_connectButton != null)
            {
                _connectButton.onClick.AddListener(HandleConnectClick);
            }
            if (_disconnectButton != null)
            {
                _disconnectButton.onClick.AddListener(HandleDisconnectClick);
            }
            if (_retryButton != null)
            {
                _retryButton.onClick.AddListener(HandleRetryClick);
            }
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(HandleCloseClick);
            }

            // Initialize button states
            UpdateButtonStates();
        }

        private void Start()
        {
            // Subscribe to GameManager if available
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnConnectionStateChanged += UpdateStatus;

                // Initialize with current state
                UpdateStatus(GameManager.Instance.ConnectionState);
            }

            // Start hidden
            Hide(false);
        }

        private void OnDestroy()
        {
            // Cleanup button handlers
            if (_connectButton != null)
            {
                _connectButton.onClick.RemoveListener(HandleConnectClick);
            }
            if (_disconnectButton != null)
            {
                _disconnectButton.onClick.RemoveListener(HandleDisconnectClick);
            }
            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveListener(HandleRetryClick);
            }
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleCloseClick);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnConnectionStateChanged -= UpdateStatus;
            }

            StopAnimation();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the displayed status to match the given connection state.
        /// </summary>
        /// <param name="state">The connection state to display.</param>
        public void UpdateStatus(ConnectionState state)
        {
            _currentState = state;

            // Update status display
            if (_statusDotImage != null)
            {
                _statusDotImage.color = ConnectionStatusUI.GetStatusColor(state);
            }

            if (_statusText != null)
            {
                _statusText.text = ConnectionStatusUI.GetStatusText(state);
            }

            UpdateButtonStates();
        }

        /// <summary>
        /// Sets the device information to display.
        /// </summary>
        /// <param name="serial">Device serial number.</param>
        /// <param name="firmware">Firmware version.</param>
        public void SetDeviceInfo(string serial, string firmware)
        {
            if (_deviceInfoText == null)
            {
                return;
            }

            string serialDisplay = string.IsNullOrEmpty(serial) ? "N/A" : serial;
            string firmwareDisplay = string.IsNullOrEmpty(firmware) ? "N/A" : firmware;

            _deviceInfoText.text = $"Serial: {serialDisplay}\nFirmware: {firmwareDisplay}";
        }

        /// <summary>
        /// Sets the connection mode to display.
        /// </summary>
        /// <param name="mode">The connection mode.</param>
        public void SetConnectionMode(ConnectionMode mode)
        {
            if (_modeText != null)
            {
                _modeText.text = mode.ToString();
            }
        }

        /// <summary>
        /// Sets the last shot time to display.
        /// </summary>
        /// <param name="time">The time of the last shot, or null if no shots.</param>
        public void SetLastShotTime(DateTime? time)
        {
            if (_lastShotText == null)
            {
                return;
            }

            if (time.HasValue)
            {
                var elapsed = DateTime.Now - time.Value;

                if (elapsed.TotalSeconds < 60)
                {
                    _lastShotText.text = $"{(int)elapsed.TotalSeconds}s ago";
                }
                else if (elapsed.TotalMinutes < 60)
                {
                    _lastShotText.text = $"{(int)elapsed.TotalMinutes}m ago";
                }
                else
                {
                    _lastShotText.text = time.Value.ToString("HH:mm:ss");
                }
            }
            else
            {
                _lastShotText.text = "No shots yet";
            }
        }

        /// <summary>
        /// Shows the panel.
        /// </summary>
        /// <param name="animate">Whether to animate the transition.</param>
        public void Show(bool animate = true)
        {
            _isVisible = true;
            gameObject.SetActive(true);

            if (animate && gameObject.activeInHierarchy)
            {
                StopAnimation();
                _animationCoroutine = StartCoroutine(AnimateFade(1f));
            }
            else if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }
        }

        /// <summary>
        /// Hides the panel.
        /// </summary>
        /// <param name="animate">Whether to animate the transition.</param>
        public void Hide(bool animate = true)
        {
            _isVisible = false;

            if (animate && gameObject.activeInHierarchy)
            {
                StopAnimation();
                _animationCoroutine = StartCoroutine(AnimateFadeAndDeactivate());
            }
            else
            {
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 0f;
                }
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Toggles panel visibility.
        /// </summary>
        /// <param name="animate">Whether to animate the transition.</param>
        public void Toggle(bool animate = true)
        {
            if (_isVisible)
            {
                Hide(animate);
            }
            else
            {
                Show(animate);
            }
        }

        /// <summary>
        /// Simulates a close button click for testing.
        /// </summary>
        public void SimulateCloseClick()
        {
            HandleCloseClick();
        }

        /// <summary>
        /// Simulates a connect button click for testing.
        /// </summary>
        public void SimulateConnectClick()
        {
            HandleConnectClick();
        }

        /// <summary>
        /// Simulates a disconnect button click for testing.
        /// </summary>
        public void SimulateDisconnectClick()
        {
            HandleDisconnectClick();
        }

        /// <summary>
        /// Simulates a retry button click for testing.
        /// </summary>
        public void SimulateRetryClick()
        {
            HandleRetryClick();
        }

        /// <summary>
        /// Sets references for testing.
        /// </summary>
        internal void SetReferences(
            Image statusDot, TextMeshProUGUI statusText,
            TextMeshProUGUI deviceInfoText, TextMeshProUGUI modeText, TextMeshProUGUI lastShotText,
            Button connectButton, Button disconnectButton, Button retryButton, Button closeButton,
            CanvasGroup canvasGroup)
        {
            _statusDotImage = statusDot;
            _statusText = statusText;
            _deviceInfoText = deviceInfoText;
            _modeText = modeText;
            _lastShotText = lastShotText;
            _connectButton = connectButton;
            _disconnectButton = disconnectButton;
            _retryButton = retryButton;
            _closeButton = closeButton;
            _canvasGroup = canvasGroup;

            UpdateButtonStates();
        }

        #endregion

        #region Private Methods

        private void UpdateButtonStates()
        {
            bool showConnect = _currentState == ConnectionState.Disconnected
                            || _currentState == ConnectionState.DeviceNotFound
                            || _currentState == ConnectionState.Failed;

            bool showDisconnect = _currentState == ConnectionState.Connected;

            bool showRetry = _currentState == ConnectionState.Failed;

            // Hide all action buttons during connecting
            if (_currentState == ConnectionState.Connecting)
            {
                showConnect = false;
                showDisconnect = false;
                showRetry = false;
            }

            if (_connectButton != null)
            {
                _connectButton.gameObject.SetActive(showConnect);
            }
            if (_disconnectButton != null)
            {
                _disconnectButton.gameObject.SetActive(showDisconnect);
            }
            if (_retryButton != null)
            {
                _retryButton.gameObject.SetActive(showRetry);
            }
        }

        private void HandleCloseClick()
        {
            OnCloseRequested?.Invoke();
            Hide();
        }

        private void HandleConnectClick()
        {
            OnConnectRequested?.Invoke();

            // Call GameManager if available
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ConnectToGC2();
            }
        }

        private void HandleDisconnectClick()
        {
            OnDisconnectRequested?.Invoke();

            // Call GameManager if available
            if (GameManager.Instance != null)
            {
                GameManager.Instance.DisconnectFromGC2();
            }
        }

        private void HandleRetryClick()
        {
            OnRetryRequested?.Invoke();

            // Call GameManager if available
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ConnectToGC2();
            }
        }

        private void StopAnimation()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }
        }

        private IEnumerator AnimateFade(float targetAlpha)
        {
            if (_canvasGroup == null)
            {
                yield break;
            }

            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;
            float duration = UITheme.Animation.Normal;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
            _animationCoroutine = null;
        }

        private IEnumerator AnimateFadeAndDeactivate()
        {
            yield return AnimateFade(0f);
            gameObject.SetActive(false);
        }

        #endregion
    }

    /// <summary>
    /// Connection mode enum.
    /// </summary>
    public enum ConnectionMode
    {
        USB,
        TCP
    }
}
