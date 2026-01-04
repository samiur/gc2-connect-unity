// ABOUTME: Floating UI overlay shown when Bridge Mode is active in foreground.
// ABOUTME: Displays GSPro connection status, shots relayed count, and expandable controls.

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OpenRange.UI
{
    /// <summary>
    /// Connection status for Bridge Mode overlay.
    /// </summary>
    public enum BridgeConnectionStatus
    {
        /// <summary>Not connected to GSPro.</summary>
        Disconnected,
        /// <summary>Attempting to connect.</summary>
        Connecting,
        /// <summary>Successfully connected to GSPro.</summary>
        Connected,
        /// <summary>Connection error occurred.</summary>
        Error
    }

    /// <summary>
    /// Floating overlay shown when Bridge Mode is active.
    /// Displays minimal status info and can be expanded for controls.
    /// </summary>
    public class BridgeModeOverlay : MonoBehaviour, IPointerClickHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        #region Static Colors

        /// <summary>Green color for connected state.</summary>
        public static readonly Color ConnectedColor = new Color(0.2f, 0.9f, 0.2f, 1f);

        /// <summary>Yellow color for connecting state.</summary>
        public static readonly Color ConnectingColor = new Color(1f, 0.8f, 0.2f, 1f);

        /// <summary>Gray color for disconnected state.</summary>
        public static readonly Color DisconnectedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        /// <summary>Red color for error state.</summary>
        public static readonly Color ErrorColor = new Color(0.9f, 0.2f, 0.2f, 1f);

        #endregion

        #region Serialized Fields

        [Header("Compact View")]
        [SerializeField] private Image _statusIcon;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _shotsRelayedText;

        [Header("Expanded Panel")]
        [SerializeField] private GameObject _expandedPanel;
        [SerializeField] private Button _disableButton;
        [SerializeField] private TextMeshProUGUI _durationText;

        [Header("Settings")]
        [SerializeField] private CanvasGroup _canvasGroup;

        #endregion

        #region Private Fields

        private RectTransform _rectTransform;
        private Canvas _parentCanvas;
        private bool _isVisible;
        private bool _isExpanded;
        private bool _isDragging;
        private int _shotsRelayed;
        private BridgeConnectionStatus _connectionStatus = BridgeConnectionStatus.Disconnected;
        private DateTime _startTime;
        private Coroutine _durationUpdateCoroutine;
        private Vector2 _dragOffset;

        // Animation
        private Coroutine _fadeCoroutine;
        private const float FadeDuration = 0.2f;

        #endregion

        #region Public Properties

        /// <summary>Whether the overlay is visible.</summary>
        public bool IsVisible => _isVisible;

        /// <summary>Whether the expanded panel is showing.</summary>
        public bool IsExpanded => _isExpanded;

        /// <summary>Number of shots relayed this session.</summary>
        public int ShotsRelayed => _shotsRelayed;

        /// <summary>Current connection status.</summary>
        public BridgeConnectionStatus ConnectionStatus => _connectionStatus;

        #endregion

        #region Events

        /// <summary>Fired when the overlay is expanded.</summary>
        public event Action OnExpanded;

        /// <summary>Fired when the overlay is collapsed.</summary>
        public event Action OnCollapsed;

        /// <summary>Fired when connection status changes.</summary>
        public event Action<BridgeConnectionStatus> OnConnectionStatusChanged;

        /// <summary>Fired when shots relayed count changes.</summary>
        public event Action<int> OnShotsRelayedChanged;

        /// <summary>Fired when disable button is clicked.</summary>
        public event Action OnDisableRequested;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
            {
                _rectTransform = gameObject.AddComponent<RectTransform>();
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            _parentCanvas = GetComponentInParent<Canvas>();

            // Initialize expanded panel as hidden
            if (_expandedPanel != null)
            {
                _expandedPanel.SetActive(false);
            }

            // Wire up disable button
            if (_disableButton != null)
            {
                _disableButton.onClick.AddListener(HandleDisableClick);
            }
        }

        private void OnDestroy()
        {
            StopAllAnimations();

            if (_disableButton != null)
            {
                _disableButton.onClick.RemoveListener(HandleDisableClick);
            }
        }

        #endregion

        #region IPointerClickHandler

        public void OnPointerClick(PointerEventData eventData)
        {
            // Only toggle if not dragging
            if (!_isDragging)
            {
                ToggleExpand();
            }
        }

        #endregion

        #region IDragHandler

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;

            if (_rectTransform != null && _parentCanvas != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _parentCanvas.transform as RectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 localPoint);

                _dragOffset = (Vector2)_rectTransform.localPosition - localPoint;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_rectTransform == null || _parentCanvas == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parentCanvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint);

            _rectTransform.localPosition = localPoint + _dragOffset;
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            // Reset dragging flag after a short delay to prevent click triggering
            StartCoroutine(ResetDraggingFlag());
        }

        private IEnumerator ResetDraggingFlag()
        {
            yield return null; // Wait one frame
            _isDragging = false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the overlay.
        /// </summary>
        /// <param name="animate">Whether to animate the transition.</param>
        public void Show(bool animate = true)
        {
            _isVisible = true;
            gameObject.SetActive(true);

            if (animate && gameObject.activeInHierarchy)
            {
                StopFadeAnimation();
                _fadeCoroutine = StartCoroutine(AnimateFade(1f));
            }
            else if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            // Start duration updates
            _startTime = DateTime.UtcNow;
            StartDurationUpdates();
        }

        /// <summary>
        /// Hides the overlay.
        /// </summary>
        /// <param name="animate">Whether to animate the transition.</param>
        public void Hide(bool animate = true)
        {
            _isVisible = false;
            StopDurationUpdates();

            if (animate && gameObject.activeInHierarchy)
            {
                StopFadeAnimation();
                _fadeCoroutine = StartCoroutine(AnimateFadeAndDeactivate());
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
        /// Expands the overlay to show additional controls.
        /// </summary>
        /// <param name="animate">Whether to animate the transition.</param>
        public void Expand(bool animate = true)
        {
            if (_isExpanded) return;

            _isExpanded = true;

            if (_expandedPanel != null)
            {
                _expandedPanel.SetActive(true);
            }

            OnExpanded?.Invoke();
        }

        /// <summary>
        /// Collapses the overlay to minimal view.
        /// </summary>
        /// <param name="animate">Whether to animate the transition.</param>
        public void Collapse(bool animate = true)
        {
            if (!_isExpanded) return;

            _isExpanded = false;

            if (_expandedPanel != null)
            {
                _expandedPanel.SetActive(false);
            }

            OnCollapsed?.Invoke();
        }

        /// <summary>
        /// Toggles between expanded and collapsed states.
        /// </summary>
        /// <param name="animate">Whether to animate the transition.</param>
        public void ToggleExpand(bool animate = true)
        {
            if (_isExpanded)
            {
                Collapse(animate);
            }
            else
            {
                Expand(animate);
            }
        }

        /// <summary>
        /// Updates the connection status display.
        /// </summary>
        /// <param name="status">New connection status.</param>
        public void UpdateConnectionStatus(BridgeConnectionStatus status)
        {
            if (_connectionStatus == status) return;

            _connectionStatus = status;

            // Update visuals
            if (_statusIcon != null)
            {
                _statusIcon.color = GetStatusColor(status);
            }

            if (_statusText != null)
            {
                _statusText.text = GetStatusText(status);
            }

            OnConnectionStatusChanged?.Invoke(status);
        }

        /// <summary>
        /// Updates the shots relayed count.
        /// </summary>
        /// <param name="count">New shot count.</param>
        public void UpdateShotsRelayed(int count)
        {
            _shotsRelayed = count;

            if (_shotsRelayedText != null)
            {
                _shotsRelayedText.text = FormatShotsText(count);
            }

            OnShotsRelayedChanged?.Invoke(count);
        }

        /// <summary>
        /// Increments the shots relayed count by one.
        /// </summary>
        public void IncrementShotsRelayed()
        {
            UpdateShotsRelayed(_shotsRelayed + 1);
        }

        /// <summary>
        /// Resets the shots relayed count to zero.
        /// </summary>
        public void ResetShotsRelayed()
        {
            UpdateShotsRelayed(0);
        }

        /// <summary>
        /// Sets references for testing.
        /// </summary>
        internal void SetReferences(Image statusIcon, TextMeshProUGUI statusText,
            TextMeshProUGUI shotsRelayedText, GameObject expandedPanel, CanvasGroup canvasGroup)
        {
            _statusIcon = statusIcon;
            _statusText = statusText;
            _shotsRelayedText = shotsRelayedText;
            _expandedPanel = expandedPanel;
            _canvasGroup = canvasGroup;

            if (_expandedPanel != null)
            {
                _expandedPanel.SetActive(false);
            }
        }

        #endregion

        #region Static Helper Methods

        /// <summary>
        /// Gets the display text for a connection status.
        /// </summary>
        public static string GetStatusText(BridgeConnectionStatus status)
        {
            return status switch
            {
                BridgeConnectionStatus.Connected => "GSPro Connected",
                BridgeConnectionStatus.Connecting => "Connecting...",
                BridgeConnectionStatus.Disconnected => "Disconnected",
                BridgeConnectionStatus.Error => "Connection Error",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Gets the display color for a connection status.
        /// </summary>
        public static Color GetStatusColor(BridgeConnectionStatus status)
        {
            return status switch
            {
                BridgeConnectionStatus.Connected => ConnectedColor,
                BridgeConnectionStatus.Connecting => ConnectingColor,
                BridgeConnectionStatus.Disconnected => DisconnectedColor,
                BridgeConnectionStatus.Error => ErrorColor,
                _ => DisconnectedColor
            };
        }

        /// <summary>
        /// Formats the shots count with proper singular/plural form.
        /// </summary>
        public static string FormatShotsText(int count)
        {
            return count == 1 ? "1 shot" : $"{count} shots";
        }

        #endregion

        #region Private Methods

        private void HandleDisableClick()
        {
            OnDisableRequested?.Invoke();
        }

        private void StartDurationUpdates()
        {
            StopDurationUpdates();
            if (gameObject.activeInHierarchy)
            {
                _durationUpdateCoroutine = StartCoroutine(UpdateDurationLoop());
            }
        }

        private void StopDurationUpdates()
        {
            if (_durationUpdateCoroutine != null)
            {
                StopCoroutine(_durationUpdateCoroutine);
                _durationUpdateCoroutine = null;
            }
        }

        private IEnumerator UpdateDurationLoop()
        {
            while (_isVisible)
            {
                UpdateDurationDisplay();
                yield return new WaitForSeconds(1f);
            }
        }

        private void UpdateDurationDisplay()
        {
            if (_durationText == null) return;

            TimeSpan duration = DateTime.UtcNow - _startTime;
            _durationText.text = FormatDuration(duration);
        }

        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours}h {duration.Minutes:D2}m";
            }
            else
            {
                return $"{duration.Minutes}m {duration.Seconds:D2}s";
            }
        }

        private void StopFadeAnimation()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
        }

        private void StopAllAnimations()
        {
            StopFadeAnimation();
            StopDurationUpdates();
        }

        private IEnumerator AnimateFade(float targetAlpha)
        {
            if (_canvasGroup == null)
            {
                yield break;
            }

            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < FadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / FadeDuration;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
            _fadeCoroutine = null;
        }

        private IEnumerator AnimateFadeAndDeactivate()
        {
            yield return AnimateFade(0f);
            gameObject.SetActive(false);
        }

        #endregion
    }
}
