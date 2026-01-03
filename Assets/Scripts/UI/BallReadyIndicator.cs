// ABOUTME: UI component showing when GC2 device is ready and a ball is detected.
// ABOUTME: Displays visual states: Disconnected, Warming Up, Place Ball, and READY with pulse animation.

using System;
using System.Collections;
using OpenRange.Core;
using OpenRange.GC2;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.UI
{
    /// <summary>
    /// Visual states for the ball ready indicator.
    /// </summary>
    public enum BallReadyState
    {
        /// <summary>Device is disconnected.</summary>
        Disconnected,
        /// <summary>Device is connected but not ready (warming up).</summary>
        WarmingUp,
        /// <summary>Device is ready but no ball detected.</summary>
        PlaceBall,
        /// <summary>Device is ready and ball is detected.</summary>
        Ready
    }

    /// <summary>
    /// UI component that displays GC2 device ready state and ball detection status.
    /// Shows prominent visual feedback to let users know when they can swing.
    /// </summary>
    public class BallReadyIndicator : MonoBehaviour
    {
        #region Static Colors

        /// <summary>Gray color for disconnected state.</summary>
        public static readonly Color DisconnectedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        /// <summary>Yellow color for warming up state.</summary>
        public static readonly Color WarmingUpColor = new Color(1f, 0.8f, 0.2f, 1f);

        /// <summary>Green outline color for place ball state.</summary>
        public static readonly Color PlaceBallColor = new Color(0.3f, 0.8f, 0.3f, 0.6f);

        /// <summary>Solid green color for ready state.</summary>
        public static readonly Color ReadyColor = new Color(0.2f, 0.9f, 0.2f, 1f);

        #endregion

        #region Serialized Fields

        [SerializeField] private Image _statusIcon;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private CanvasGroup _canvasGroup;

        #endregion

        #region Private Fields

        private BallReadyState _currentVisualState = BallReadyState.Disconnected;
        private ConnectionState _lastConnectionState = ConnectionState.Disconnected;
        private bool _isReadyToHit;
        private bool _isVisible = true;
        private bool _isPulsing;
        private bool _visualsInitialized;
        private Coroutine _animationCoroutine;
        private Coroutine _pulseCoroutine;

        // Pulse animation settings
        private const float PulseMinScale = 1.0f;
        private const float PulseMaxScale = 1.08f;
        private const float PulseDuration = 0.6f;

        #endregion

        #region Public Properties

        /// <summary>
        /// Whether the device is ready and ball is detected - user can swing.
        /// </summary>
        public bool IsReadyToHit => _isReadyToHit;

        /// <summary>
        /// Current visual state being displayed.
        /// </summary>
        public BallReadyState CurrentVisualState => _currentVisualState;

        /// <summary>
        /// Whether the indicator is visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Whether the pulse animation is active.
        /// </summary>
        public bool IsPulsing => _isPulsing;

        /// <summary>
        /// Reference to status icon for testing.
        /// </summary>
        public Image StatusIcon => _statusIcon;

        /// <summary>
        /// Reference to status text for testing.
        /// </summary>
        public TextMeshProUGUI StatusText => _statusText;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the ready-to-hit state changes.
        /// </summary>
        public event Action<bool> OnReadyStateChanged;

        /// <summary>
        /// Fired when the visual state changes.
        /// </summary>
        public event Action<BallReadyState> OnVisualStateChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            SubscribeToGameManager();
        }

        private void OnDestroy()
        {
            UnsubscribeFromGameManager();
            StopAllAnimations();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the indicator state based on connection and device status.
        /// </summary>
        /// <param name="connectionState">Current GC2 connection state.</param>
        /// <param name="deviceStatus">Current device status from 0M messages (null if unknown).</param>
        public void UpdateState(ConnectionState connectionState, GC2DeviceStatus? deviceStatus)
        {
            // Determine new visual state
            BallReadyState newVisualState = DetermineVisualState(connectionState, deviceStatus);

            // Determine if ready to hit
            bool newReadyToHit = connectionState == ConnectionState.Connected &&
                                 deviceStatus.HasValue &&
                                 deviceStatus.Value.IsReady &&
                                 deviceStatus.Value.BallDetected;

            // Update visuals if state changed or first time
            bool visualStateChanged = newVisualState != _currentVisualState;
            bool connectionStateChanged = connectionState != _lastConnectionState;
            if (visualStateChanged || connectionStateChanged || !_visualsInitialized)
            {
                _currentVisualState = newVisualState;
                _lastConnectionState = connectionState;
                _visualsInitialized = true;
                UpdateVisuals(newVisualState, connectionState);
                if (visualStateChanged)
                {
                    OnVisualStateChanged?.Invoke(newVisualState);
                }
            }

            // Handle ready state change
            if (newReadyToHit != _isReadyToHit)
            {
                _isReadyToHit = newReadyToHit;
                OnReadyStateChanged?.Invoke(_isReadyToHit);

                // Start or stop pulse animation
                if (_isReadyToHit)
                {
                    StartPulse();
                }
                else
                {
                    StopPulse();
                }
            }
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
                StopFadeAnimation();
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
                StopFadeAnimation();
                _animationCoroutine = StartCoroutine(AnimateFade(0f));
            }
            else if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// Stops the pulse animation manually.
        /// </summary>
        public void StopPulse()
        {
            _isPulsing = false;
            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }

            // Reset scale
            if (_statusIcon != null)
            {
                _statusIcon.transform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Sets references for testing.
        /// </summary>
        internal void SetReferences(Image statusIcon, TextMeshProUGUI statusText, CanvasGroup canvasGroup)
        {
            _statusIcon = statusIcon;
            _statusText = statusText;
            _canvasGroup = canvasGroup;
        }

        #endregion

        #region Static Helper Methods

        /// <summary>
        /// Gets the display text for a given state.
        /// </summary>
        public static string GetStateText(BallReadyState state)
        {
            return state switch
            {
                BallReadyState.Disconnected => "Connect GC2",
                BallReadyState.WarmingUp => "Warming Up...",
                BallReadyState.PlaceBall => "Place Ball",
                BallReadyState.Ready => "READY",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Gets the display color for a given state.
        /// </summary>
        public static Color GetStateColor(BallReadyState state)
        {
            return state switch
            {
                BallReadyState.Disconnected => DisconnectedColor,
                BallReadyState.WarmingUp => WarmingUpColor,
                BallReadyState.PlaceBall => PlaceBallColor,
                BallReadyState.Ready => ReadyColor,
                _ => DisconnectedColor
            };
        }

        #endregion

        #region Private Methods

        private void SubscribeToGameManager()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnConnectionStateChanged += HandleConnectionStateChanged;
                GameManager.Instance.OnDeviceStatusChanged += HandleDeviceStatusChanged;

                // Initialize with current state
                UpdateState(GameManager.Instance.ConnectionState, GameManager.Instance.CurrentDeviceStatus);
            }
        }

        private void UnsubscribeFromGameManager()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnConnectionStateChanged -= HandleConnectionStateChanged;
                GameManager.Instance.OnDeviceStatusChanged -= HandleDeviceStatusChanged;
            }
        }

        private void HandleConnectionStateChanged(ConnectionState newState)
        {
            // Get current device status from GameManager
            GC2DeviceStatus? deviceStatus = GameManager.Instance?.CurrentDeviceStatus;
            UpdateState(newState, deviceStatus);
        }

        private void HandleDeviceStatusChanged(GC2DeviceStatus status)
        {
            // Get current connection state from GameManager
            ConnectionState connectionState = GameManager.Instance?.ConnectionState ?? ConnectionState.Disconnected;
            UpdateState(connectionState, status);
        }

        private BallReadyState DetermineVisualState(ConnectionState connectionState, GC2DeviceStatus? deviceStatus)
        {
            // Not connected
            if (connectionState == ConnectionState.Disconnected ||
                connectionState == ConnectionState.DeviceNotFound ||
                connectionState == ConnectionState.Failed)
            {
                return BallReadyState.Disconnected;
            }

            // Connecting
            if (connectionState == ConnectionState.Connecting)
            {
                return BallReadyState.WarmingUp;
            }

            // Connected - check device status
            if (connectionState == ConnectionState.Connected)
            {
                if (!deviceStatus.HasValue)
                {
                    return BallReadyState.WarmingUp;
                }

                if (!deviceStatus.Value.IsReady)
                {
                    return BallReadyState.WarmingUp;
                }

                if (!deviceStatus.Value.BallDetected)
                {
                    return BallReadyState.PlaceBall;
                }

                return BallReadyState.Ready;
            }

            return BallReadyState.Disconnected;
        }

        private void UpdateVisuals(BallReadyState state, ConnectionState connectionState)
        {
            // Update icon color
            if (_statusIcon != null)
            {
                _statusIcon.color = GetStateColor(state);
            }

            // Update text
            if (_statusText != null)
            {
                // Show "Connecting..." specifically when actively connecting
                if (connectionState == ConnectionState.Connecting)
                {
                    _statusText.text = "Connecting...";
                }
                else
                {
                    _statusText.text = GetStateText(state);
                }

                // Use bold for READY state
                _statusText.fontStyle = state == BallReadyState.Ready
                    ? FontStyles.Bold
                    : FontStyles.Normal;
            }
        }

        private void StartPulse()
        {
            if (_isPulsing) return;

            _isPulsing = true;
            if (gameObject.activeInHierarchy && _statusIcon != null)
            {
                _pulseCoroutine = StartCoroutine(PulseAnimation());
            }
        }

        private void StopFadeAnimation()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }
        }

        private void StopAllAnimations()
        {
            StopFadeAnimation();
            StopPulse();
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

        private IEnumerator PulseAnimation()
        {
            if (_statusIcon == null)
            {
                yield break;
            }

            Transform iconTransform = _statusIcon.transform;

            while (_isPulsing)
            {
                // Scale up
                float elapsed = 0f;
                while (elapsed < PulseDuration / 2f)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / (PulseDuration / 2f);
                    float scale = Mathf.Lerp(PulseMinScale, PulseMaxScale, t);
                    iconTransform.localScale = Vector3.one * scale;
                    yield return null;
                }

                // Scale down
                elapsed = 0f;
                while (elapsed < PulseDuration / 2f)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / (PulseDuration / 2f);
                    float scale = Mathf.Lerp(PulseMaxScale, PulseMinScale, t);
                    iconTransform.localScale = Vector3.one * scale;
                    yield return null;
                }
            }

            iconTransform.localScale = Vector3.one;
            _pulseCoroutine = null;
        }

        #endregion
    }
}
