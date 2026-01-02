// ABOUTME: Compact top-left panel displaying current session statistics.
// ABOUTME: Shows elapsed time, total shots, average speed, and longest carry with live updates.

using System;
using System.Collections;
using OpenRange.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.UI
{
    /// <summary>
    /// Compact panel displaying current session statistics.
    /// Shows elapsed time, total shots, average ball speed, and longest carry.
    /// Taps/clicks expand the ShotHistoryPanel.
    /// </summary>
    public class SessionInfoPanel : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Text References")]
        [SerializeField] private TextMeshProUGUI _sessionTimeText;
        [SerializeField] private TextMeshProUGUI _totalShotsText;
        [SerializeField] private TextMeshProUGUI _avgSpeedText;
        [SerializeField] private TextMeshProUGUI _longestCarryText;

        [Header("UI References")]
        [SerializeField] private Button _expandButton;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _backgroundImage;

        [Header("Configuration")]
        [SerializeField] private float _timeUpdateInterval = 1f;

        #endregion

        #region Private Fields

        private SessionManager _sessionManager;
        private Coroutine _timeUpdateCoroutine;
        private bool _isVisible = true;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the panel is clicked to expand shot history.
        /// </summary>
        public event Action OnExpandClicked;

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
            SetupButtonListener();
            FindSessionManager();
            StartTimeUpdates();
        }

        private void OnEnable()
        {
            SubscribeToSessionEvents();
            StartTimeUpdates();
            UpdateDisplay();
        }

        private void OnDisable()
        {
            UnsubscribeFromSessionEvents();
            StopTimeUpdates();
        }

        private void OnDestroy()
        {
            UnsubscribeFromSessionEvents();
            StopTimeUpdates();

            if (_expandButton != null)
            {
                _expandButton.onClick.RemoveListener(HandleExpandClicked);
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
            UpdateDisplay();
        }

        /// <summary>
        /// Force refresh the display with current session data.
        /// </summary>
        public void RefreshDisplay()
        {
            UpdateDisplay();
        }

        /// <summary>
        /// Show the panel with optional animation.
        /// </summary>
        public void Show(bool animate = true)
        {
            _isVisible = true;
            gameObject.SetActive(true);

            if (animate && _canvasGroup != null)
            {
                StartCoroutine(AnimateFade(0f, 1f, UITheme.Animation.Fast));
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

            if (animate && _canvasGroup != null)
            {
                StartCoroutine(AnimateFadeOut());
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Gets whether the panel is visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        #endregion

        #region Internal Methods (for testing)

        internal string GetSessionTimeText() => _sessionTimeText != null ? _sessionTimeText.text : string.Empty;
        internal string GetTotalShotsText() => _totalShotsText != null ? _totalShotsText.text : string.Empty;
        internal string GetAvgSpeedText() => _avgSpeedText != null ? _avgSpeedText.text : string.Empty;
        internal string GetLongestCarryText() => _longestCarryText != null ? _longestCarryText.text : string.Empty;

        internal void SetReferences(
            TextMeshProUGUI sessionTimeText,
            TextMeshProUGUI totalShotsText,
            TextMeshProUGUI avgSpeedText,
            TextMeshProUGUI longestCarryText,
            Button expandButton = null,
            CanvasGroup canvasGroup = null,
            Image backgroundImage = null)
        {
            _sessionTimeText = sessionTimeText;
            _totalShotsText = totalShotsText;
            _avgSpeedText = avgSpeedText;
            _longestCarryText = longestCarryText;
            _expandButton = expandButton;
            _canvasGroup = canvasGroup;
            _backgroundImage = backgroundImage;

            // Wire up button listener for tests (Start() isn't called in EditMode)
            SetupButtonListener();
        }

        internal void ForceUpdateDisplay()
        {
            UpdateDisplay();
        }

        #endregion

        #region Private Methods

        private void SetupButtonListener()
        {
            if (_expandButton != null)
            {
                _expandButton.onClick.AddListener(HandleExpandClicked);
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
                _sessionManager.OnSessionStarted += HandleSessionStarted;
                _sessionManager.OnSessionEnded += HandleSessionEnded;
                _sessionManager.OnShotRecorded += HandleShotRecorded;
                _sessionManager.OnStatisticsUpdated += HandleStatisticsUpdated;
            }
        }

        private void UnsubscribeFromSessionEvents()
        {
            if (_sessionManager != null)
            {
                _sessionManager.OnSessionStarted -= HandleSessionStarted;
                _sessionManager.OnSessionEnded -= HandleSessionEnded;
                _sessionManager.OnShotRecorded -= HandleShotRecorded;
                _sessionManager.OnStatisticsUpdated -= HandleStatisticsUpdated;
            }
        }

        private void StartTimeUpdates()
        {
            if (_timeUpdateCoroutine == null && gameObject.activeInHierarchy)
            {
                _timeUpdateCoroutine = StartCoroutine(UpdateTimeRoutine());
            }
        }

        private void StopTimeUpdates()
        {
            if (_timeUpdateCoroutine != null)
            {
                StopCoroutine(_timeUpdateCoroutine);
                _timeUpdateCoroutine = null;
            }
        }

        private IEnumerator UpdateTimeRoutine()
        {
            while (true)
            {
                UpdateSessionTime();
                yield return new WaitForSeconds(_timeUpdateInterval);
            }
        }

        private void UpdateDisplay()
        {
            UpdateSessionTime();
            UpdateTotalShots();
            UpdateAverageSpeed();
            UpdateLongestCarry();
        }

        private void UpdateSessionTime()
        {
            if (_sessionTimeText == null) return;

            if (_sessionManager == null || !_sessionManager.IsActive)
            {
                _sessionTimeText.text = "00:00:00";
                return;
            }

            TimeSpan elapsed = _sessionManager.ElapsedTime;
            _sessionTimeText.text = FormatTimeSpan(elapsed);
        }

        private void UpdateTotalShots()
        {
            if (_totalShotsText == null) return;

            int totalShots = _sessionManager != null ? _sessionManager.TotalShots : 0;
            _totalShotsText.text = totalShots.ToString();
        }

        private void UpdateAverageSpeed()
        {
            if (_avgSpeedText == null) return;

            if (_sessionManager == null || _sessionManager.TotalShots == 0)
            {
                _avgSpeedText.text = "-";
                return;
            }

            float avgSpeed = _sessionManager.AverageBallSpeed;
            _avgSpeedText.text = $"{avgSpeed:F0}";
        }

        private void UpdateLongestCarry()
        {
            if (_longestCarryText == null) return;

            if (_sessionManager == null || _sessionManager.TotalShots == 0)
            {
                _longestCarryText.text = "-";
                return;
            }

            float longestCarry = _sessionManager.LongestCarry;
            _longestCarryText.text = $"{longestCarry:F0}";
        }

        private static string FormatTimeSpan(TimeSpan timeSpan)
        {
            return $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }

        private void HandleExpandClicked()
        {
            OnExpandClicked?.Invoke();
        }

        private void HandleSessionStarted()
        {
            UpdateDisplay();
        }

        private void HandleSessionEnded()
        {
            StopTimeUpdates();
        }

        private void HandleShotRecorded(SessionShot shot)
        {
            UpdateTotalShots();
        }

        private void HandleStatisticsUpdated()
        {
            UpdateAverageSpeed();
            UpdateLongestCarry();
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
            yield return AnimateFade(1f, 0f, UITheme.Animation.Fast);
            gameObject.SetActive(false);
        }

        #endregion
    }
}
