// ABOUTME: UI component that displays GC2 connection status as a colored indicator with text.
// ABOUTME: Subscribes to GameManager.OnConnectionStateChanged and updates visual state accordingly.

using System;
using System.Collections;
using OpenRange.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.UI
{
    /// <summary>
    /// Displays GC2 connection status as a small corner indicator.
    /// Shows a colored dot and status text that reflects the current connection state.
    /// Tap to expand the connection details panel.
    /// </summary>
    public class ConnectionStatusUI : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private Image _statusDotImage;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _clickButton;

        #endregion

        #region Private Fields

        private ConnectionState _currentState = ConnectionState.Disconnected;
        private bool _isVisible = true;
        private Coroutine _animationCoroutine;

        #endregion

        #region Public Properties

        /// <summary>
        /// The current connection state being displayed.
        /// </summary>
        public ConnectionState CurrentState => _currentState;

        /// <summary>
        /// Whether the indicator is visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Reference to the status dot image for testing.
        /// </summary>
        public Image StatusDotImage => _statusDotImage;

        /// <summary>
        /// Reference to the status text for testing.
        /// </summary>
        public TextMeshProUGUI StatusText => _statusText;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the status changes.
        /// </summary>
        public event Action<ConnectionState> OnStatusChanged;

        /// <summary>
        /// Fired when the indicator is clicked/tapped.
        /// </summary>
        public event Action OnClicked;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_clickButton != null)
            {
                _clickButton.onClick.AddListener(HandleClick);
            }
        }

        private void Start()
        {
            // Subscribe to GameManager connection state changes if available
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnConnectionStateChanged += UpdateStatus;

                // Initialize with current state
                UpdateStatus(GameManager.Instance.ConnectionState);
            }
        }

        private void OnDestroy()
        {
            if (_clickButton != null)
            {
                _clickButton.onClick.RemoveListener(HandleClick);
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
            if (_currentState == state)
            {
                return;
            }

            _currentState = state;

            // Update visuals
            if (_statusDotImage != null)
            {
                _statusDotImage.color = GetStatusColor(state);
            }

            if (_statusText != null)
            {
                _statusText.text = GetStatusText(state);
            }

            OnStatusChanged?.Invoke(state);
        }

        /// <summary>
        /// Shows the indicator.
        /// </summary>
        /// <param name="animate">Whether to animate the transition.</param>
        public void Show(bool animate = true)
        {
            _isVisible = true;

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
        /// Hides the indicator.
        /// </summary>
        /// <param name="animate">Whether to animate the transition.</param>
        public void Hide(bool animate = true)
        {
            _isVisible = false;

            if (animate && gameObject.activeInHierarchy)
            {
                StopAnimation();
                _animationCoroutine = StartCoroutine(AnimateFade(0f));
            }
            else if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// Simulates a click for testing.
        /// </summary>
        public void SimulateClick()
        {
            HandleClick();
        }

        /// <summary>
        /// Sets references for testing.
        /// </summary>
        internal void SetReferences(Image statusDot, TextMeshProUGUI statusText, CanvasGroup canvasGroup)
        {
            _statusDotImage = statusDot;
            _statusText = statusText;
            _canvasGroup = canvasGroup;
        }

        #endregion

        #region Static Helper Methods

        /// <summary>
        /// Gets the status text for a given connection state.
        /// </summary>
        public static string GetStatusText(ConnectionState state)
        {
            return state switch
            {
                ConnectionState.Connected => "GC2 Connected",
                ConnectionState.Connecting => "Connecting...",
                ConnectionState.Disconnected => "Disconnected",
                ConnectionState.DeviceNotFound => "No GC2 Detected",
                ConnectionState.Failed => "Connection Failed",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Gets the status color for a given connection state.
        /// </summary>
        public static Color GetStatusColor(ConnectionState state)
        {
            return state switch
            {
                ConnectionState.Connected => UITheme.StatusConnected,
                ConnectionState.Connecting => UITheme.StatusConnecting,
                ConnectionState.Disconnected => UITheme.StatusDisconnected,
                ConnectionState.DeviceNotFound => UITheme.StatusNoDevice,
                ConnectionState.Failed => UITheme.StatusDisconnected,
                _ => UITheme.StatusNoDevice
            };
        }

        #endregion

        #region Private Methods

        private void HandleClick()
        {
            OnClicked?.Invoke();
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
            float duration = UITheme.Animation.Fast;

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

        #endregion
    }
}
