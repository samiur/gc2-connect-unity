// ABOUTME: Full-screen modal displaying detailed shot data with comparison to session averages.
// ABOUTME: Shows all shot metrics, physics results, and provides replay functionality.

using System;
using System.Collections;
using OpenRange.Core;
using OpenRange.GC2;
using OpenRange.Physics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.UI
{
    /// <summary>
    /// Full-screen modal displaying detailed shot data.
    /// Shows all shot metrics with comparison to session averages.
    /// </summary>
    public class ShotDetailModal : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Shot Info")]
        [SerializeField] private TextMeshProUGUI _shotNumberText;
        [SerializeField] private TextMeshProUGUI _timestampText;

        [Header("Ball Data")]
        [SerializeField] private TextMeshProUGUI _ballSpeedText;
        [SerializeField] private TextMeshProUGUI _directionText;
        [SerializeField] private TextMeshProUGUI _launchAngleText;
        [SerializeField] private TextMeshProUGUI _backSpinText;
        [SerializeField] private TextMeshProUGUI _sideSpinText;

        [Header("Result Data")]
        [SerializeField] private TextMeshProUGUI _carryText;
        [SerializeField] private TextMeshProUGUI _runText;
        [SerializeField] private TextMeshProUGUI _totalText;
        [SerializeField] private TextMeshProUGUI _apexText;
        [SerializeField] private TextMeshProUGUI _offlineText;

        [Header("Comparison (Delta from Average)")]
        [SerializeField] private TextMeshProUGUI _speedDeltaText;
        [SerializeField] private TextMeshProUGUI _carryDeltaText;

        [Header("Club Data (HMT)")]
        [SerializeField] private TextMeshProUGUI _clubSpeedText;
        [SerializeField] private TextMeshProUGUI _clubPathText;
        [SerializeField] private TextMeshProUGUI _attackAngleText;
        [SerializeField] private TextMeshProUGUI _faceAngleText;
        [SerializeField] private TextMeshProUGUI _dynamicLoftText;
        [SerializeField] private GameObject _clubDataContainer;

        [Header("Buttons")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _replayButton;

        [Header("UI References")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _backgroundOverlay;

        #endregion

        #region Private Fields

        private SessionShot _currentShot;
        private SessionManager _sessionManager;
        private bool _isVisible;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the modal is currently visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// The currently displayed shot.
        /// </summary>
        public SessionShot CurrentShot => _currentShot;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the modal is closed.
        /// </summary>
        public event Action OnClosed;

        /// <summary>
        /// Fired when replay is requested.
        /// </summary>
        public event Action<SessionShot> OnReplayRequested;

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

        private void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
            }

            if (_replayButton != null)
            {
                _replayButton.onClick.RemoveListener(HandleReplayClicked);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the SessionManager reference manually.
        /// </summary>
        public void SetSessionManager(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        /// <summary>
        /// Show the modal with the specified shot.
        /// </summary>
        public void Show(SessionShot shot, bool animate = true)
        {
            _currentShot = shot;
            _isVisible = true;
            gameObject.SetActive(true);

            UpdateDisplay();

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
        /// Hide the modal.
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
        /// Refresh the display with current shot data.
        /// </summary>
        public void RefreshDisplay()
        {
            UpdateDisplay();
        }

        #endregion

        #region Internal Methods (for testing)

        internal string GetTitleText() => _shotNumberText != null ? _shotNumberText.text : string.Empty;
        internal string GetShotNumberText() => _shotNumberText != null ? _shotNumberText.text : string.Empty;
        internal string GetTimestampText() => _timestampText != null ? _timestampText.text : string.Empty;
        internal string GetBallSpeedText() => _ballSpeedText != null ? _ballSpeedText.text : string.Empty;
        internal string GetDirectionText() => _directionText != null ? _directionText.text : string.Empty;
        internal string GetLaunchAngleText() => _launchAngleText != null ? _launchAngleText.text : string.Empty;
        internal string GetBackSpinText() => _backSpinText != null ? _backSpinText.text : string.Empty;
        internal string GetSideSpinText() => _sideSpinText != null ? _sideSpinText.text : string.Empty;
        internal string GetCarryText() => _carryText != null ? _carryText.text : string.Empty;
        internal string GetRunText() => _runText != null ? _runText.text : string.Empty;
        internal string GetTotalText() => _totalText != null ? _totalText.text : string.Empty;
        internal string GetApexText() => _apexText != null ? _apexText.text : string.Empty;
        internal string GetOfflineText() => _offlineText != null ? _offlineText.text : string.Empty;
        internal string GetFlightTimeText() => _timestampText != null ? _timestampText.text : string.Empty; // Using timestamp as placeholder
        internal string GetSpeedCompareText() => _speedDeltaText != null ? _speedDeltaText.text : string.Empty;
        internal string GetCarryCompareText() => _carryDeltaText != null ? _carryDeltaText.text : string.Empty;
        internal string GetClubSpeedText() => _clubSpeedText != null ? _clubSpeedText.text : string.Empty;
        internal string GetPathText() => _clubPathText != null ? _clubPathText.text : string.Empty;
        internal string GetAttackAngleText() => _attackAngleText != null ? _attackAngleText.text : string.Empty;
        internal string GetFaceAngleText() => _faceAngleText != null ? _faceAngleText.text : string.Empty;
        internal string GetDynamicLoftText() => _dynamicLoftText != null ? _dynamicLoftText.text : string.Empty;
        internal bool IsClubDataVisible() => _clubDataContainer != null && _clubDataContainer.activeSelf;

        internal void SetReferences(
            TextMeshProUGUI shotNumberText,
            TextMeshProUGUI timestampText,
            TextMeshProUGUI ballSpeedText,
            TextMeshProUGUI directionText,
            TextMeshProUGUI launchAngleText,
            TextMeshProUGUI backSpinText,
            TextMeshProUGUI sideSpinText,
            TextMeshProUGUI carryText,
            TextMeshProUGUI runText,
            TextMeshProUGUI totalText,
            TextMeshProUGUI apexText,
            TextMeshProUGUI offlineText,
            TextMeshProUGUI speedDeltaText = null,
            TextMeshProUGUI carryDeltaText = null,
            Button closeButton = null,
            Button replayButton = null,
            CanvasGroup canvasGroup = null,
            GameObject clubDataContainer = null)
        {
            _shotNumberText = shotNumberText;
            _timestampText = timestampText;
            _ballSpeedText = ballSpeedText;
            _directionText = directionText;
            _launchAngleText = launchAngleText;
            _backSpinText = backSpinText;
            _sideSpinText = sideSpinText;
            _carryText = carryText;
            _runText = runText;
            _totalText = totalText;
            _apexText = apexText;
            _offlineText = offlineText;
            _speedDeltaText = speedDeltaText;
            _carryDeltaText = carryDeltaText;
            _closeButton = closeButton;
            _replayButton = replayButton;
            _canvasGroup = canvasGroup;
            _clubDataContainer = clubDataContainer;

            SetupButtonListeners();
        }

        internal void SetClubDataReferences(
            TextMeshProUGUI clubSpeedText,
            TextMeshProUGUI clubPathText = null,
            TextMeshProUGUI attackAngleText = null,
            TextMeshProUGUI faceAngleText = null,
            TextMeshProUGUI dynamicLoftText = null)
        {
            _clubSpeedText = clubSpeedText;
            _clubPathText = clubPathText;
            _attackAngleText = attackAngleText;
            _faceAngleText = faceAngleText;
            _dynamicLoftText = dynamicLoftText;
        }

        internal void ForceUpdateDisplay()
        {
            UpdateDisplay();
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

            if (_replayButton != null)
            {
                _replayButton.onClick.RemoveListener(HandleReplayClicked);
                _replayButton.onClick.AddListener(HandleReplayClicked);
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

        private void UpdateDisplay()
        {
            if (_currentShot == null)
            {
                ClearDisplay();
                return;
            }

            UpdateShotInfo();
            UpdateBallData();
            UpdateResultData();
            UpdateComparison();
            UpdateClubData();
        }

        private void ClearDisplay()
        {
            if (_shotNumberText != null) _shotNumberText.text = "-";
            if (_timestampText != null) _timestampText.text = "-";
            if (_ballSpeedText != null) _ballSpeedText.text = "-";
            if (_directionText != null) _directionText.text = "-";
            if (_launchAngleText != null) _launchAngleText.text = "-";
            if (_backSpinText != null) _backSpinText.text = "-";
            if (_sideSpinText != null) _sideSpinText.text = "-";
            if (_carryText != null) _carryText.text = "-";
            if (_runText != null) _runText.text = "-";
            if (_totalText != null) _totalText.text = "-";
            if (_apexText != null) _apexText.text = "-";
            if (_offlineText != null) _offlineText.text = "-";
            if (_speedDeltaText != null) _speedDeltaText.text = "";
            if (_carryDeltaText != null) _carryDeltaText.text = "";
            if (_clubDataContainer != null) _clubDataContainer.SetActive(false);
        }

        private void UpdateShotInfo()
        {
            if (_shotNumberText != null)
            {
                _shotNumberText.text = $"Shot #{_currentShot.ShotNumber}";
            }

            if (_timestampText != null)
            {
                _timestampText.text = _currentShot.Timestamp.ToLocalTime().ToString("HH:mm:ss");
            }
        }

        private void UpdateBallData()
        {
            var shotData = _currentShot.ShotData;
            if (shotData == null) return;

            if (_ballSpeedText != null)
            {
                _ballSpeedText.text = $"{shotData.BallSpeed:F1} mph";
            }

            if (_directionText != null)
            {
                string prefix = shotData.Direction < 0 ? "L" : "R";
                float absDir = Mathf.Abs(shotData.Direction);
                _directionText.text = absDir < 0.1f ? "0.0" : $"{prefix}{absDir:F1}";
            }

            if (_launchAngleText != null)
            {
                _launchAngleText.text = $"{shotData.LaunchAngle:F1}";
            }

            if (_backSpinText != null)
            {
                _backSpinText.text = $"{(int)shotData.BackSpin:N0}";
            }

            if (_sideSpinText != null)
            {
                string prefix = shotData.SideSpin < 0 ? "L" : "R";
                int absSpin = Mathf.Abs((int)shotData.SideSpin);
                _sideSpinText.text = absSpin < 10 ? "0" : $"{prefix}{absSpin:N0}";
            }
        }

        private void UpdateResultData()
        {
            var result = _currentShot.Result;
            if (result == null) return;

            if (_carryText != null)
            {
                _carryText.text = $"{result.CarryDistance:F1} yd";
            }

            if (_runText != null)
            {
                float run = result.TotalDistance - result.CarryDistance;
                _runText.text = $"{run:F1} yd";
            }

            if (_totalText != null)
            {
                _totalText.text = $"{result.TotalDistance:F1} yd";
            }

            if (_apexText != null)
            {
                // Convert feet to yards
                float apexYards = result.MaxHeight / 3f;
                _apexText.text = $"{apexYards:F1} yd";
            }

            if (_offlineText != null)
            {
                string prefix = result.OfflineDistance < 0 ? "L" : "R";
                float absOffline = Mathf.Abs(result.OfflineDistance);
                _offlineText.text = absOffline < 0.1f ? "0.0 yd" : $"{prefix}{absOffline:F1} yd";
            }
        }

        private void UpdateComparison()
        {
            if (_sessionManager == null || _sessionManager.TotalShots <= 1)
            {
                if (_speedDeltaText != null) _speedDeltaText.text = "";
                if (_carryDeltaText != null) _carryDeltaText.text = "";
                return;
            }

            // Speed delta
            if (_speedDeltaText != null && _currentShot.ShotData != null)
            {
                float delta = _currentShot.ShotData.BallSpeed - _sessionManager.AverageBallSpeed;
                _speedDeltaText.text = FormatDelta(delta);
                _speedDeltaText.color = GetDeltaColor(delta);
            }

            // Carry delta
            if (_carryDeltaText != null && _currentShot.Result != null)
            {
                float delta = _currentShot.Result.CarryDistance - _sessionManager.AverageCarryDistance;
                _carryDeltaText.text = FormatDelta(delta);
                _carryDeltaText.color = GetDeltaColor(delta);
            }
        }

        private void UpdateClubData()
        {
            if (_currentShot.ShotData == null || !_currentShot.ShotData.HasClubData)
            {
                if (_clubDataContainer != null)
                {
                    _clubDataContainer.SetActive(false);
                }
                return;
            }

            if (_clubDataContainer != null)
            {
                _clubDataContainer.SetActive(true);
            }

            var shotData = _currentShot.ShotData;

            if (_clubSpeedText != null)
            {
                _clubSpeedText.text = $"{shotData.ClubSpeed:F1} mph";
            }

            if (_clubPathText != null)
            {
                string prefix = shotData.Path < 0 ? "Out-to-In" : "In-to-Out";
                _clubPathText.text = Mathf.Abs(shotData.Path) < 0.5f
                    ? "Square"
                    : $"{prefix} {Mathf.Abs(shotData.Path):F1}";
            }

            if (_attackAngleText != null)
            {
                string prefix = shotData.AttackAngle < 0 ? "Down" : "Up";
                _attackAngleText.text = Mathf.Abs(shotData.AttackAngle) < 0.5f
                    ? "Level"
                    : $"{prefix} {Mathf.Abs(shotData.AttackAngle):F1}";
            }

            if (_faceAngleText != null)
            {
                string prefix = shotData.FaceToTarget < 0 ? "Closed" : "Open";
                _faceAngleText.text = Mathf.Abs(shotData.FaceToTarget) < 0.5f
                    ? "Square"
                    : $"{prefix} {Mathf.Abs(shotData.FaceToTarget):F1}";
            }

            if (_dynamicLoftText != null)
            {
                _dynamicLoftText.text = $"{shotData.DynamicLoft:F1}";
            }
        }

        private static string FormatDelta(float delta)
        {
            if (Mathf.Abs(delta) < 0.1f)
            {
                return "";
            }

            string sign = delta >= 0 ? "+" : "";
            return $"({sign}{delta:F1})";
        }

        private static Color GetDeltaColor(float delta)
        {
            if (delta > 0.5f) return UITheme.AccentGreen;
            if (delta < -0.5f) return UITheme.TotalRed;
            return UITheme.TextSecondary;
        }

        private void HandleCloseClicked()
        {
            Hide();
            OnClosed?.Invoke();
        }

        private void HandleReplayClicked()
        {
            if (_currentShot != null)
            {
                OnReplayRequested?.Invoke(_currentShot);
                Hide();
            }
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
