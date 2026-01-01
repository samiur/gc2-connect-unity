// ABOUTME: Main UI manager singleton that controls panels, toasts, and UI state.
// ABOUTME: Integrates with ShotProcessor and GameManager for event-driven updates.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace OpenRange.UI
{
    /// <summary>
    /// Main UI manager singleton that controls all UI panels and notifications.
    /// Handles panel visibility, toast notifications, and event subscriptions
    /// to core services like ShotProcessor and GameManager.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<UIManager>();
                }
                return _instance;
            }
        }

        [Header("Panel References")]
        [SerializeField] private Transform _panelContainer;

        [Header("Toast Configuration")]
        [SerializeField] private GameObject _toastPrefab;
        [SerializeField] private Transform _toastContainer;
        [SerializeField] private int _maxVisibleToasts = 3;

        [Header("Animation")]
        [SerializeField] private float _panelTransitionDuration = 0.25f;
        [SerializeField] private float _toastSlideDuration = 0.2f;

        private readonly Dictionary<string, GameObject> _panels = new Dictionary<string, GameObject>();
        private readonly Queue<ToastData> _toastQueue = new Queue<ToastData>();
        private readonly List<Toast> _activeToasts = new List<Toast>();
        private bool _isProcessingToasts;
        private ResponsiveLayout _responsiveLayout;

        /// <summary>
        /// Currently active panel name.
        /// </summary>
        public string ActivePanel { get; private set; }

        /// <summary>
        /// Number of toasts currently being displayed.
        /// </summary>
        public int ActiveToastCount => _activeToasts.Count;

        /// <summary>
        /// Number of toasts waiting in queue.
        /// </summary>
        public int QueuedToastCount => _toastQueue.Count;

        /// <summary>
        /// The ResponsiveLayout component if available.
        /// </summary>
        public ResponsiveLayout ResponsiveLayout => _responsiveLayout;

        /// <summary>
        /// Fired when a panel is shown.
        /// </summary>
        public event Action<string> OnPanelShown;

        /// <summary>
        /// Fired when a panel is hidden.
        /// </summary>
        public event Action<string> OnPanelHidden;

        /// <summary>
        /// Fired when a toast is displayed.
        /// </summary>
        public event Action<string, ToastType> OnToastShown;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("UIManager: Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void Start()
        {
            _responsiveLayout = GetComponentInChildren<ResponsiveLayout>();
            if (_responsiveLayout == null)
            {
                _responsiveLayout = FindAnyObjectByType<ResponsiveLayout>();
            }

            DiscoverPanels();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Force initialize the singleton instance (for testing).
        /// </summary>
        public void ForceInitializeSingleton()
        {
            _instance = this;
        }

        /// <summary>
        /// Discover and register all child panels.
        /// </summary>
        private void DiscoverPanels()
        {
            if (_panelContainer == null)
            {
                return;
            }

            foreach (Transform child in _panelContainer)
            {
                RegisterPanel(child.name, child.gameObject);
            }
        }

        #region Panel Management

        /// <summary>
        /// Register a panel with the UI manager.
        /// </summary>
        /// <param name="panelName">Unique name for the panel.</param>
        /// <param name="panel">The panel GameObject.</param>
        public void RegisterPanel(string panelName, GameObject panel)
        {
            if (string.IsNullOrEmpty(panelName) || panel == null)
            {
                return;
            }

            _panels[panelName] = panel;
        }

        /// <summary>
        /// Unregister a panel from the UI manager.
        /// </summary>
        /// <param name="panelName">Name of the panel to unregister.</param>
        public void UnregisterPanel(string panelName)
        {
            _panels.Remove(panelName);
        }

        /// <summary>
        /// Show a panel by name.
        /// </summary>
        /// <param name="panelName">Name of the panel to show.</param>
        /// <param name="animate">Whether to animate the transition.</param>
        /// <returns>True if the panel was found and shown.</returns>
        public bool ShowPanel(string panelName, bool animate = true)
        {
            if (!_panels.TryGetValue(panelName, out GameObject panel))
            {
                Debug.LogWarning($"UIManager: Panel '{panelName}' not found");
                return false;
            }

            if (animate)
            {
                StartCoroutine(AnimatePanelIn(panel));
            }
            else
            {
                panel.SetActive(true);
                SetPanelAlpha(panel, 1f);
            }

            ActivePanel = panelName;
            OnPanelShown?.Invoke(panelName);
            return true;
        }

        /// <summary>
        /// Hide a panel by name.
        /// </summary>
        /// <param name="panelName">Name of the panel to hide.</param>
        /// <param name="animate">Whether to animate the transition.</param>
        /// <returns>True if the panel was found and hidden.</returns>
        public bool HidePanel(string panelName, bool animate = true)
        {
            if (!_panels.TryGetValue(panelName, out GameObject panel))
            {
                Debug.LogWarning($"UIManager: Panel '{panelName}' not found");
                return false;
            }

            if (animate)
            {
                StartCoroutine(AnimatePanelOut(panel));
            }
            else
            {
                panel.SetActive(false);
            }

            if (ActivePanel == panelName)
            {
                ActivePanel = null;
            }

            OnPanelHidden?.Invoke(panelName);
            return true;
        }

        /// <summary>
        /// Toggle a panel's visibility.
        /// </summary>
        /// <param name="panelName">Name of the panel to toggle.</param>
        /// <param name="animate">Whether to animate the transition.</param>
        /// <returns>True if panel is now visible, false if hidden.</returns>
        public bool TogglePanel(string panelName, bool animate = true)
        {
            if (!_panels.TryGetValue(panelName, out GameObject panel))
            {
                Debug.LogWarning($"UIManager: Panel '{panelName}' not found");
                return false;
            }

            if (panel.activeSelf)
            {
                HidePanel(panelName, animate);
                return false;
            }
            else
            {
                ShowPanel(panelName, animate);
                return true;
            }
        }

        /// <summary>
        /// Check if a panel is currently visible.
        /// </summary>
        /// <param name="panelName">Name of the panel.</param>
        /// <returns>True if visible.</returns>
        public bool IsPanelVisible(string panelName)
        {
            if (_panels.TryGetValue(panelName, out GameObject panel))
            {
                return panel.activeSelf;
            }
            return false;
        }

        /// <summary>
        /// Get a registered panel by name.
        /// </summary>
        /// <param name="panelName">Name of the panel.</param>
        /// <returns>The panel GameObject or null if not found.</returns>
        public GameObject GetPanel(string panelName)
        {
            _panels.TryGetValue(panelName, out GameObject panel);
            return panel;
        }

        /// <summary>
        /// Hide all panels.
        /// </summary>
        /// <param name="animate">Whether to animate transitions.</param>
        public void HideAllPanels(bool animate = false)
        {
            foreach (var kvp in _panels)
            {
                if (kvp.Value.activeSelf)
                {
                    HidePanel(kvp.Key, animate);
                }
            }
        }

        private IEnumerator AnimatePanelIn(GameObject panel)
        {
            panel.SetActive(true);

            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }

            float elapsed = 0f;
            canvasGroup.alpha = 0f;

            while (elapsed < _panelTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _panelTransitionDuration;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        private IEnumerator AnimatePanelOut(GameObject panel)
        {
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                panel.SetActive(false);
                yield break;
            }

            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < _panelTransitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _panelTransitionDuration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            panel.SetActive(false);
        }

        private void SetPanelAlpha(GameObject panel, float alpha)
        {
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = alpha;
        }

        #endregion

        #region Toast Notifications

        /// <summary>
        /// Show a toast notification.
        /// </summary>
        /// <param name="message">Message to display.</param>
        /// <param name="duration">Duration in seconds (default 3s).</param>
        /// <param name="type">Toast type for styling.</param>
        public void ShowToast(string message, float duration = -1f, ToastType type = ToastType.Info)
        {
            if (duration < 0)
            {
                duration = UITheme.ToastDefaultDuration;
            }

            var toastData = new ToastData
            {
                Message = message,
                Duration = duration,
                Type = type
            };

            _toastQueue.Enqueue(toastData);

            if (!_isProcessingToasts)
            {
                StartCoroutine(ProcessToastQueue());
            }
        }

        /// <summary>
        /// Show a success toast.
        /// </summary>
        public void ShowSuccessToast(string message, float duration = -1f)
        {
            ShowToast(message, duration, ToastType.Success);
        }

        /// <summary>
        /// Show a warning toast.
        /// </summary>
        public void ShowWarningToast(string message, float duration = -1f)
        {
            ShowToast(message, duration, ToastType.Warning);
        }

        /// <summary>
        /// Show an error toast.
        /// </summary>
        public void ShowErrorToast(string message, float duration = -1f)
        {
            ShowToast(message, duration, ToastType.Error);
        }

        /// <summary>
        /// Clear all toasts immediately.
        /// </summary>
        public void ClearAllToasts()
        {
            _toastQueue.Clear();

            foreach (var toast in _activeToasts.ToArray())
            {
                if (toast != null && toast.gameObject != null)
                {
                    Destroy(toast.gameObject);
                }
            }
            _activeToasts.Clear();
        }

        private IEnumerator ProcessToastQueue()
        {
            _isProcessingToasts = true;

            while (_toastQueue.Count > 0)
            {
                // Wait if we're at max visible toasts
                while (_activeToasts.Count >= _maxVisibleToasts)
                {
                    // Clean up any destroyed toasts
                    _activeToasts.RemoveAll(t => t == null || t.gameObject == null);
                    yield return null;
                }

                var toastData = _toastQueue.Dequeue();
                CreateAndShowToast(toastData);

                // Small delay between toasts
                yield return new WaitForSeconds(0.1f);
            }

            _isProcessingToasts = false;
        }

        private void CreateAndShowToast(ToastData data)
        {
            GameObject toastGO;

            if (_toastPrefab != null && _toastContainer != null)
            {
                toastGO = Instantiate(_toastPrefab, _toastContainer);
            }
            else
            {
                // Create a simple toast programmatically
                toastGO = CreateDefaultToast();
            }

            var toast = toastGO.GetComponent<Toast>();
            if (toast == null)
            {
                toast = toastGO.AddComponent<Toast>();
            }

            toast.Initialize(data.Message, data.Duration, data.Type);
            toast.OnDismissed += () => OnToastDismissed(toast);

            _activeToasts.Add(toast);
            toast.Show();

            OnToastShown?.Invoke(data.Message, data.Type);
        }

        private void OnToastDismissed(Toast toast)
        {
            _activeToasts.Remove(toast);
            if (toast != null && toast.gameObject != null)
            {
                Destroy(toast.gameObject);
            }
        }

        private GameObject CreateDefaultToast()
        {
            // Create a simple toast without a prefab
            var go = new GameObject("Toast");

            if (_toastContainer != null)
            {
                go.transform.SetParent(_toastContainer, false);
            }

            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(400, 60);

            var image = go.AddComponent<Image>();
            image.color = UITheme.ToastInfo;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16, 8);
            textRect.offsetMax = new Vector2(-16, -8);

            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = UITheme.TextPrimary;
            text.fontSize = 16;

            var canvasGroup = go.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            return go;
        }

        private struct ToastData
        {
            public string Message;
            public float Duration;
            public ToastType Type;
        }

        #endregion
    }

    /// <summary>
    /// Toast notification component for displaying temporary messages.
    /// </summary>
    public class Toast : MonoBehaviour
    {
        private string _message;
        private float _duration;
        private ToastType _type;
        private TextMeshProUGUI _text;
        private Image _background;
        private CanvasGroup _canvasGroup;
        private bool _isShowing;
        private float _showTime;

        /// <summary>
        /// Fired when the toast is dismissed.
        /// </summary>
        public event Action OnDismissed;

        /// <summary>
        /// Initialize the toast.
        /// </summary>
        public void Initialize(string message, float duration, ToastType type)
        {
            _message = message;
            _duration = duration;
            _type = type;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            _text = GetComponentInChildren<TextMeshProUGUI>();
            _background = GetComponent<Image>();

            if (_text != null)
            {
                _text.text = message;
            }

            if (_background != null)
            {
                _background.color = GetColorForType(type);
            }
        }

        /// <summary>
        /// Show the toast with animation.
        /// </summary>
        public void Show()
        {
            _isShowing = true;
            _showTime = Time.time;
            StartCoroutine(ShowAnimation());
        }

        /// <summary>
        /// Dismiss the toast immediately.
        /// </summary>
        public void Dismiss()
        {
            if (!_isShowing) return;
            _isShowing = false;
            StartCoroutine(HideAnimation());
        }

        private void Update()
        {
            if (_isShowing && Time.time - _showTime >= _duration)
            {
                Dismiss();
            }
        }

        private IEnumerator ShowAnimation()
        {
            float elapsed = 0f;
            float duration = UITheme.Animation.ToastSlide;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            _canvasGroup.alpha = 1f;
        }

        private IEnumerator HideAnimation()
        {
            float elapsed = 0f;
            float duration = UITheme.Animation.ToastSlide;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            OnDismissed?.Invoke();
        }

        private Color GetColorForType(ToastType type)
        {
            return type switch
            {
                ToastType.Success => UITheme.ToastSuccess,
                ToastType.Warning => UITheme.ToastWarning,
                ToastType.Error => UITheme.ToastError,
                _ => UITheme.ToastInfo
            };
        }
    }
}
