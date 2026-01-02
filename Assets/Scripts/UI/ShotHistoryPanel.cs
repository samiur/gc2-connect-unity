// ABOUTME: Expandable side panel displaying scrollable shot history with statistics summary.
// ABOUTME: Shows all recorded shots, allows selection and replay, with clear history functionality.

using System;
using System.Collections;
using System.Collections.Generic;
using OpenRange.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.UI
{
    /// <summary>
    /// Expandable side panel displaying scrollable shot history.
    /// Shows all recorded shots with statistics summary at top.
    /// </summary>
    public class ShotHistoryPanel : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Statistics Summary")]
        [SerializeField] private TextMeshProUGUI _totalShotsText;
        [SerializeField] private TextMeshProUGUI _avgSpeedText;
        [SerializeField] private TextMeshProUGUI _avgCarryText;
        [SerializeField] private TextMeshProUGUI _longestCarryText;

        [Header("Shot List")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _contentContainer;
        [SerializeField] private ShotHistoryItem _itemPrefab;

        [Header("Buttons")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _clearHistoryButton;

        [Header("UI References")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _backgroundImage;

        [Header("Configuration")]
        [SerializeField] private float _itemHeight = 60f;
        [SerializeField] private float _itemSpacing = 4f;

        #endregion

        #region Private Fields

        private SessionManager _sessionManager;
        private readonly List<ShotHistoryItem> _activeItems = new List<ShotHistoryItem>();
        private readonly Queue<ShotHistoryItem> _itemPool = new Queue<ShotHistoryItem>();
        private SessionShot _selectedShot;
        private bool _isVisible;
        private Coroutine _refreshCoroutine;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the panel is currently visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// The currently selected shot.
        /// </summary>
        public SessionShot SelectedShot => _selectedShot;

        /// <summary>
        /// Number of items currently displayed.
        /// </summary>
        public int DisplayedItemCount => _activeItems.Count;

        #endregion

        #region Events

        /// <summary>
        /// Fired when a shot is selected.
        /// </summary>
        public event Action<SessionShot> OnShotSelected;

        /// <summary>
        /// Fired when replay is requested for a shot.
        /// </summary>
        public event Action<SessionShot> OnReplayRequested;

        /// <summary>
        /// Fired when the panel is closed.
        /// </summary>
        public event Action OnClosed;

        /// <summary>
        /// Fired when history is cleared.
        /// </summary>
        public event Action OnHistoryCleared;

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
            SetupButtonListeners();
            FindSessionManager();
            Hide(animate: false);
        }

        private void OnEnable()
        {
            SubscribeToSessionEvents();
            StartTimeRefresh();
        }

        private void OnDisable()
        {
            UnsubscribeFromSessionEvents();
            StopTimeRefresh();
        }

        private void OnDestroy()
        {
            UnsubscribeFromSessionEvents();

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
            }

            if (_clearHistoryButton != null)
            {
                _clearHistoryButton.onClick.RemoveListener(HandleClearHistoryClicked);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the SessionManager reference manually.
        /// </summary>
        public void SetSessionManager(SessionManager sessionManager)
        {
            UnsubscribeFromSessionEvents();
            _sessionManager = sessionManager;
            SubscribeToSessionEvents();
            RefreshAll();
        }

        /// <summary>
        /// Set the item prefab for instantiation.
        /// </summary>
        public void SetItemPrefab(ShotHistoryItem prefab)
        {
            _itemPrefab = prefab;
        }

        /// <summary>
        /// Show the panel with optional animation.
        /// </summary>
        public void Show(bool animate = true)
        {
            _isVisible = true;
            gameObject.SetActive(true);
            RefreshAll();

            if (animate && _canvasGroup != null)
            {
                StartCoroutine(AnimateFade(0f, 1f, UITheme.Animation.PanelTransition));
            }
            else if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }
        }

        /// <summary>
        /// Hide the panel with optional animation.
        /// </summary>
        public void Hide(bool animate = true)
        {
            _isVisible = false;

            if (animate && _canvasGroup != null && gameObject.activeInHierarchy)
            {
                StartCoroutine(AnimateFadeOut());
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
        /// Toggle the panel visibility.
        /// </summary>
        public void Toggle()
        {
            if (_isVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        /// <summary>
        /// Refresh all displayed data.
        /// </summary>
        public void RefreshAll()
        {
            RefreshStatistics();
            RefreshShotList();
        }

        /// <summary>
        /// Select a shot by index.
        /// </summary>
        public void SelectShot(int index)
        {
            if (index >= 0 && index < _activeItems.Count)
            {
                var item = _activeItems[index];
                HandleShotClicked(item.ShotData);
            }
        }

        /// <summary>
        /// Clear selection.
        /// </summary>
        public void ClearSelection()
        {
            _selectedShot = null;
            foreach (var item in _activeItems)
            {
                item.IsSelected = false;
            }
        }

        /// <summary>
        /// Scroll to the most recent shot.
        /// </summary>
        public void ScrollToTop()
        {
            if (_scrollRect != null)
            {
                _scrollRect.normalizedPosition = new Vector2(0, 1);
            }
        }

        /// <summary>
        /// Scroll to the oldest shot.
        /// </summary>
        public void ScrollToBottom()
        {
            if (_scrollRect != null)
            {
                _scrollRect.normalizedPosition = new Vector2(0, 0);
            }
        }

        #endregion

        #region Internal Methods (for testing)

        internal string GetTotalShotsText() => _totalShotsText != null ? _totalShotsText.text : string.Empty;
        internal string GetAvgSpeedText() => _avgSpeedText != null ? _avgSpeedText.text : string.Empty;
        internal string GetAvgCarryText() => _avgCarryText != null ? _avgCarryText.text : string.Empty;
        internal string GetLongestCarryText() => _longestCarryText != null ? _longestCarryText.text : string.Empty;

        internal List<ShotHistoryItem> GetActiveItems() => _activeItems;

        internal void SetReferences(
            TextMeshProUGUI totalShotsText,
            TextMeshProUGUI avgSpeedText,
            TextMeshProUGUI avgCarryText,
            TextMeshProUGUI longestCarryText,
            ScrollRect scrollRect = null,
            RectTransform contentContainer = null,
            Button closeButton = null,
            Button clearHistoryButton = null,
            CanvasGroup canvasGroup = null)
        {
            _totalShotsText = totalShotsText;
            _avgSpeedText = avgSpeedText;
            _avgCarryText = avgCarryText;
            _longestCarryText = longestCarryText;
            _scrollRect = scrollRect;
            _contentContainer = contentContainer;
            _closeButton = closeButton;
            _clearHistoryButton = clearHistoryButton;
            _canvasGroup = canvasGroup;

            SetupButtonListeners();
        }

        internal void ForceRefreshStatistics()
        {
            RefreshStatistics();
        }

        internal void ForceRefreshShotList()
        {
            RefreshShotList();
        }

        #endregion

        #region Private Methods

        private void SetupButtonListeners()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (_clearHistoryButton != null)
            {
                _clearHistoryButton.onClick.RemoveListener(HandleClearHistoryClicked);
                _clearHistoryButton.onClick.AddListener(HandleClearHistoryClicked);
            }
        }

        private void FindSessionManager()
        {
            if (_sessionManager == null && GameManager.Instance != null)
            {
                _sessionManager = GameManager.Instance.SessionManager;
            }

            if (_sessionManager == null)
            {
                _sessionManager = FindAnyObjectByType<SessionManager>();
            }
        }

        private void SubscribeToSessionEvents()
        {
            if (_sessionManager != null)
            {
                _sessionManager.OnShotRecorded += HandleShotRecorded;
                _sessionManager.OnStatisticsUpdated += HandleStatisticsUpdated;
                _sessionManager.OnSessionStarted += HandleSessionStarted;
                _sessionManager.OnSessionEnded += HandleSessionEnded;
            }
        }

        private void UnsubscribeFromSessionEvents()
        {
            if (_sessionManager != null)
            {
                _sessionManager.OnShotRecorded -= HandleShotRecorded;
                _sessionManager.OnStatisticsUpdated -= HandleStatisticsUpdated;
                _sessionManager.OnSessionStarted -= HandleSessionStarted;
                _sessionManager.OnSessionEnded -= HandleSessionEnded;
            }
        }

        private void StartTimeRefresh()
        {
            if (_refreshCoroutine == null && gameObject.activeInHierarchy)
            {
                _refreshCoroutine = StartCoroutine(TimeRefreshRoutine());
            }
        }

        private void StopTimeRefresh()
        {
            if (_refreshCoroutine != null)
            {
                StopCoroutine(_refreshCoroutine);
                _refreshCoroutine = null;
            }
        }

        private IEnumerator TimeRefreshRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(30f);

                foreach (var item in _activeItems)
                {
                    item.RefreshTimeAgo();
                }
            }
        }

        private void RefreshStatistics()
        {
            if (_sessionManager == null)
            {
                ClearStatistics();
                return;
            }

            if (_totalShotsText != null)
            {
                _totalShotsText.text = _sessionManager.TotalShots.ToString();
            }

            if (_sessionManager.TotalShots == 0)
            {
                if (_avgSpeedText != null) _avgSpeedText.text = "-";
                if (_avgCarryText != null) _avgCarryText.text = "-";
                if (_longestCarryText != null) _longestCarryText.text = "-";
                return;
            }

            if (_avgSpeedText != null)
            {
                _avgSpeedText.text = $"{_sessionManager.AverageBallSpeed:F0} mph";
            }

            if (_avgCarryText != null)
            {
                _avgCarryText.text = $"{_sessionManager.AverageCarryDistance:F0} yd";
            }

            if (_longestCarryText != null)
            {
                _longestCarryText.text = $"{_sessionManager.LongestCarry:F0} yd";
            }
        }

        private void ClearStatistics()
        {
            if (_totalShotsText != null) _totalShotsText.text = "0";
            if (_avgSpeedText != null) _avgSpeedText.text = "-";
            if (_avgCarryText != null) _avgCarryText.text = "-";
            if (_longestCarryText != null) _longestCarryText.text = "-";
        }

        private void RefreshShotList()
        {
            // Return all active items to pool
            foreach (var item in _activeItems)
            {
                item.OnClicked -= HandleShotClicked;
                item.OnReplayRequested -= HandleReplayRequested;
                item.gameObject.SetActive(false);
                _itemPool.Enqueue(item);
            }
            _activeItems.Clear();

            if (_sessionManager == null || _contentContainer == null)
            {
                return;
            }

            // Get shots (most recent first)
            var shots = _sessionManager.GetRecentShots(_sessionManager.ShotCount);

            // Create items for each shot
            foreach (var shot in shots)
            {
                var item = GetOrCreateItem();
                item.SetData(shot);
                item.IsSelected = (_selectedShot != null && _selectedShot.ShotNumber == shot.ShotNumber);
                _activeItems.Add(item);
            }

            // Update content container size
            UpdateContentSize();
        }

        private ShotHistoryItem GetOrCreateItem()
        {
            ShotHistoryItem item;

            if (_itemPool.Count > 0)
            {
                item = _itemPool.Dequeue();
                item.gameObject.SetActive(true);
            }
            else if (_itemPrefab != null)
            {
                var go = Instantiate(_itemPrefab.gameObject, _contentContainer);
                item = go.GetComponent<ShotHistoryItem>();
            }
            else
            {
                // Create minimal item programmatically
                item = CreateMinimalItem();
            }

            item.OnClicked += HandleShotClicked;
            item.OnReplayRequested += HandleReplayRequested;
            item.transform.SetAsLastSibling();

            return item;
        }

        private ShotHistoryItem CreateMinimalItem()
        {
            var go = new GameObject("ShotHistoryItem");
            go.transform.SetParent(_contentContainer, false);

            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, _itemHeight);

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = _itemHeight;
            layoutElement.flexibleWidth = 1f;

            var item = go.AddComponent<ShotHistoryItem>();
            return item;
        }

        private void UpdateContentSize()
        {
            if (_contentContainer == null) return;

            float totalHeight = _activeItems.Count * (_itemHeight + _itemSpacing);
            _contentContainer.sizeDelta = new Vector2(_contentContainer.sizeDelta.x, totalHeight);
        }

        private void HandleShotRecorded(SessionShot shot)
        {
            if (_isVisible)
            {
                RefreshShotList();
                ScrollToTop();
            }
        }

        private void HandleStatisticsUpdated()
        {
            if (_isVisible)
            {
                RefreshStatistics();
            }
        }

        private void HandleSessionStarted()
        {
            if (_isVisible)
            {
                RefreshAll();
            }
        }

        private void HandleSessionEnded()
        {
            // Keep displaying current data
        }

        private void HandleShotClicked(SessionShot shot)
        {
            _selectedShot = shot;

            // Update selection state on all items
            foreach (var item in _activeItems)
            {
                item.IsSelected = (item.ShotData.ShotNumber == shot.ShotNumber);
            }

            OnShotSelected?.Invoke(shot);
        }

        private void HandleReplayRequested(SessionShot shot)
        {
            OnReplayRequested?.Invoke(shot);
        }

        private void HandleCloseClicked()
        {
            Hide();
            OnClosed?.Invoke();
        }

        private void HandleClearHistoryClicked()
        {
            if (_sessionManager != null)
            {
                _sessionManager.ClearHistory();
            }

            ClearSelection();
            RefreshAll();
            OnHistoryCleared?.Invoke();
        }

        private IEnumerator AnimateFade(float from, float to, float duration)
        {
            if (_canvasGroup == null) yield break;

            float elapsed = 0f;
            _canvasGroup.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _canvasGroup.alpha = Mathf.Lerp(from, to, t);
                yield return null;
            }

            _canvasGroup.alpha = to;
        }

        private IEnumerator AnimateFadeOut()
        {
            yield return AnimateFade(1f, 0f, UITheme.Animation.PanelTransition);
            gameObject.SetActive(false);
        }

        #endregion
    }
}
