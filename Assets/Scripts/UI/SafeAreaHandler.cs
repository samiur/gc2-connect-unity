// ABOUTME: Adjusts RectTransform to respect device safe area (notches, home indicator).
// ABOUTME: Automatically updates when safe area changes due to orientation or screen changes.

using UnityEngine;

namespace OpenRange.UI
{
    /// <summary>
    /// Handles device safe area adjustments for UI elements.
    /// Automatically adjusts the RectTransform to avoid notches,
    /// home indicators, and other screen cutouts.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaHandler : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool _applyOnStart = true;
        [SerializeField] private bool _listenForChanges = true;

        [Header("Edges to Apply")]
        [SerializeField] private bool _applyTop = true;
        [SerializeField] private bool _applyBottom = true;
        [SerializeField] private bool _applyLeft = true;
        [SerializeField] private bool _applyRight = true;

        [Header("Debug")]
        [SerializeField] private bool _logChanges = false;

        private RectTransform _rectTransform;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private ResponsiveLayout _responsiveLayout;

        /// <summary>
        /// The RectTransform being managed.
        /// </summary>
        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = GetComponent<RectTransform>();
                }
                return _rectTransform;
            }
        }

        /// <summary>
        /// Whether safe area padding is currently applied.
        /// </summary>
        public bool IsApplied { get; private set; }

        /// <summary>
        /// The last applied safe area rect.
        /// </summary>
        public Rect LastAppliedSafeArea => _lastSafeArea;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }

        private void Start()
        {
            // Find ResponsiveLayout if available
            _responsiveLayout = GetComponentInParent<ResponsiveLayout>();
            if (_responsiveLayout == null)
            {
                _responsiveLayout = FindAnyObjectByType<ResponsiveLayout>();
            }

            if (_responsiveLayout != null && _listenForChanges)
            {
                _responsiveLayout.OnSafeAreaChanged += OnSafeAreaChanged;
            }

            if (_applyOnStart)
            {
                ApplySafeArea();
            }
        }

        private void OnDestroy()
        {
            if (_responsiveLayout != null)
            {
                _responsiveLayout.OnSafeAreaChanged -= OnSafeAreaChanged;
            }
        }

        private void Update()
        {
            // Fallback check if ResponsiveLayout is not available
            if (_responsiveLayout == null && _listenForChanges)
            {
                Vector2Int currentSize = new Vector2Int(Screen.width, Screen.height);
                if (currentSize != _lastScreenSize)
                {
                    _lastScreenSize = currentSize;
                    ApplySafeArea();
                }
            }
        }

        /// <summary>
        /// Handler for safe area change events.
        /// </summary>
        private void OnSafeAreaChanged(Rect safeArea)
        {
            ApplySafeArea();
        }

        /// <summary>
        /// Apply safe area padding to the RectTransform.
        /// </summary>
        public void ApplySafeArea()
        {
            Rect safeArea = Screen.safeArea;

            // Skip if safe area hasn't changed
            if (IsApplied && safeArea == _lastSafeArea)
            {
                return;
            }

            _lastSafeArea = safeArea;

            // Get the parent canvas to calculate proper offsets
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                if (_logChanges)
                {
                    Debug.LogWarning("SafeAreaHandler: No parent Canvas found");
                }
                return;
            }

            // Get the canvas rect
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            Vector2 canvasSize = canvasRect.rect.size;

            // Calculate the safe area in canvas space
            float scaleX = canvasSize.x / Screen.width;
            float scaleY = canvasSize.y / Screen.height;

            // Calculate offsets from edges
            float leftOffset = _applyLeft ? safeArea.x * scaleX : 0f;
            float rightOffset = _applyRight ? (Screen.width - (safeArea.x + safeArea.width)) * scaleX : 0f;
            float bottomOffset = _applyBottom ? safeArea.y * scaleY : 0f;
            float topOffset = _applyTop ? (Screen.height - (safeArea.y + safeArea.height)) * scaleY : 0f;

            // Apply to RectTransform anchors and offsets
            // Reset to full stretch first
            RectTransform.anchorMin = Vector2.zero;
            RectTransform.anchorMax = Vector2.one;

            // Apply offsets
            RectTransform.offsetMin = new Vector2(leftOffset, bottomOffset);
            RectTransform.offsetMax = new Vector2(-rightOffset, -topOffset);

            IsApplied = true;

            if (_logChanges)
            {
                Debug.Log($"SafeAreaHandler: Applied safe area - L:{leftOffset:F1} R:{rightOffset:F1} B:{bottomOffset:F1} T:{topOffset:F1}");
            }
        }

        /// <summary>
        /// Reset the RectTransform to ignore safe area.
        /// </summary>
        public void ResetSafeArea()
        {
            RectTransform.anchorMin = Vector2.zero;
            RectTransform.anchorMax = Vector2.one;
            RectTransform.offsetMin = Vector2.zero;
            RectTransform.offsetMax = Vector2.zero;

            IsApplied = false;

            if (_logChanges)
            {
                Debug.Log("SafeAreaHandler: Reset safe area");
            }
        }

        /// <summary>
        /// Set which edges should have safe area padding applied.
        /// </summary>
        /// <param name="top">Apply to top edge.</param>
        /// <param name="bottom">Apply to bottom edge.</param>
        /// <param name="left">Apply to left edge.</param>
        /// <param name="right">Apply to right edge.</param>
        public void SetEdges(bool top, bool bottom, bool left, bool right)
        {
            _applyTop = top;
            _applyBottom = bottom;
            _applyLeft = left;
            _applyRight = right;

            if (IsApplied)
            {
                // Force re-apply with new edge settings
                _lastSafeArea = Rect.zero;
                ApplySafeArea();
            }
        }

        /// <summary>
        /// Get the current safe area insets in canvas units.
        /// </summary>
        /// <returns>Vector4 with (left, right, bottom, top) insets.</returns>
        public Vector4 GetInsets()
        {
            Rect safeArea = Screen.safeArea;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return Vector4.zero;
            }

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            Vector2 canvasSize = canvasRect.rect.size;

            float scaleX = canvasSize.x / Screen.width;
            float scaleY = canvasSize.y / Screen.height;

            float left = safeArea.x * scaleX;
            float right = (Screen.width - (safeArea.x + safeArea.width)) * scaleX;
            float bottom = safeArea.y * scaleY;
            float top = (Screen.height - (safeArea.y + safeArea.height)) * scaleY;

            return new Vector4(left, right, bottom, top);
        }

        /// <summary>
        /// Check if the device has a notch or safe area insets.
        /// </summary>
        /// <returns>True if there are safe area insets.</returns>
        public static bool HasSafeAreaInsets()
        {
            Rect safeArea = Screen.safeArea;
            return safeArea.x > 0 ||
                   safeArea.y > 0 ||
                   safeArea.width < Screen.width ||
                   safeArea.height < Screen.height;
        }
    }
}
