// ABOUTME: Individual row component for displaying a single shot in the shot history list.
// ABOUTME: Shows shot number, ball speed, carry distance, and time ago with selection highlighting.

using System;
using OpenRange.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.UI
{
    /// <summary>
    /// Individual row component for displaying a single shot in the shot history list.
    /// Shows shot number, ball speed, carry distance, and time ago.
    /// </summary>
    public class ShotHistoryItem : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Text References")]
        [SerializeField] private TextMeshProUGUI _shotNumberText;
        [SerializeField] private TextMeshProUGUI _ballSpeedText;
        [SerializeField] private TextMeshProUGUI _carryDistanceText;
        [SerializeField] private TextMeshProUGUI _timeAgoText;

        [Header("UI References")]
        [SerializeField] private Button _itemButton;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _selectionHighlight;

        [Header("Colors")]
        [SerializeField] private Color _normalColor = new Color(0.15f, 0.15f, 0.2f, 0.8f);
        [SerializeField] private Color _selectedColor = new Color(0.2f, 0.4f, 0.3f, 0.9f);
        [SerializeField] private Color _highlightColor = new Color(0.25f, 0.5f, 0.35f, 0.95f);

        #endregion

        #region Private Fields

        private SessionShot _shotData;
        private bool _isSelected;

        #endregion

        #region Properties

        /// <summary>
        /// The shot data currently displayed.
        /// </summary>
        public SessionShot ShotData => _shotData;

        /// <summary>
        /// Whether this item is currently selected.
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                UpdateSelectionVisual();
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Fired when this item is clicked. Passes the SessionShot data.
        /// </summary>
        public event Action<SessionShot> OnClicked;

        /// <summary>
        /// Fired when replay is requested for this shot.
        /// </summary>
        public event Action<SessionShot> OnReplayRequested;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupButton();
        }

        private void OnDestroy()
        {
            if (_itemButton != null)
            {
                _itemButton.onClick.RemoveListener(HandleClick);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set the shot data to display.
        /// </summary>
        /// <param name="shot">The session shot data.</param>
        public void SetData(SessionShot shot)
        {
            _shotData = shot;
            UpdateDisplay();
        }

        /// <summary>
        /// Clear the displayed data.
        /// </summary>
        public void Clear()
        {
            _shotData = null;

            if (_shotNumberText != null) _shotNumberText.text = "-";
            if (_ballSpeedText != null) _ballSpeedText.text = "-";
            if (_carryDistanceText != null) _carryDistanceText.text = "-";
            if (_timeAgoText != null) _timeAgoText.text = "-";

            IsSelected = false;
        }

        /// <summary>
        /// Refresh the time ago display.
        /// </summary>
        public void RefreshTimeAgo()
        {
            if (_shotData != null && _timeAgoText != null)
            {
                _timeAgoText.text = FormatTimeAgo(_shotData.Timestamp);
            }
        }

        /// <summary>
        /// Request replay of this shot's trajectory.
        /// </summary>
        public void RequestReplay()
        {
            if (_shotData != null)
            {
                OnReplayRequested?.Invoke(_shotData);
            }
        }

        #endregion

        #region Internal Methods (for testing)

        internal string GetShotNumberText() => _shotNumberText != null ? _shotNumberText.text : string.Empty;
        internal string GetBallSpeedText() => _ballSpeedText != null ? _ballSpeedText.text : string.Empty;
        internal string GetCarryDistanceText() => _carryDistanceText != null ? _carryDistanceText.text : string.Empty;
        internal string GetTimeAgoText() => _timeAgoText != null ? _timeAgoText.text : string.Empty;

        internal Color GetBackgroundColor() => _backgroundImage != null ? _backgroundImage.color : Color.clear;

        internal void SetReferences(
            TextMeshProUGUI shotNumberText,
            TextMeshProUGUI ballSpeedText,
            TextMeshProUGUI carryDistanceText,
            TextMeshProUGUI timeAgoText,
            Button itemButton = null,
            Image backgroundImage = null,
            Image selectionHighlight = null)
        {
            _shotNumberText = shotNumberText;
            _ballSpeedText = ballSpeedText;
            _carryDistanceText = carryDistanceText;
            _timeAgoText = timeAgoText;
            _itemButton = itemButton;
            _backgroundImage = backgroundImage;
            _selectionHighlight = selectionHighlight;

            SetupButton();
        }

        internal void SimulateClick()
        {
            HandleClick();
        }

        #endregion

        #region Private Methods

        private void SetupButton()
        {
            if (_itemButton != null)
            {
                _itemButton.onClick.RemoveListener(HandleClick);
                _itemButton.onClick.AddListener(HandleClick);
            }
        }

        private void UpdateDisplay()
        {
            if (_shotData == null)
            {
                Clear();
                return;
            }

            if (_shotNumberText != null)
            {
                _shotNumberText.text = $"#{_shotData.ShotNumber}";
            }

            if (_ballSpeedText != null)
            {
                _ballSpeedText.text = $"{_shotData.ShotData.BallSpeed:F0} mph";
            }

            if (_carryDistanceText != null)
            {
                _carryDistanceText.text = $"{_shotData.Result.CarryDistance:F0} yd";
            }

            if (_timeAgoText != null)
            {
                _timeAgoText.text = FormatTimeAgo(_shotData.Timestamp);
            }

            UpdateSelectionVisual();
        }

        private void UpdateSelectionVisual()
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.color = _isSelected ? _selectedColor : _normalColor;
            }

            if (_selectionHighlight != null)
            {
                _selectionHighlight.gameObject.SetActive(_isSelected);
            }
        }

        private void HandleClick()
        {
            if (_shotData != null)
            {
                OnClicked?.Invoke(_shotData);
            }
        }

        private static string FormatTimeAgo(DateTime timestamp)
        {
            TimeSpan elapsed = DateTime.UtcNow - timestamp;

            if (elapsed.TotalSeconds < 60)
            {
                return "Just now";
            }
            else if (elapsed.TotalMinutes < 60)
            {
                int minutes = (int)elapsed.TotalMinutes;
                return minutes == 1 ? "1 min ago" : $"{minutes} min ago";
            }
            else if (elapsed.TotalHours < 24)
            {
                int hours = (int)elapsed.TotalHours;
                return hours == 1 ? "1 hr ago" : $"{hours} hrs ago";
            }
            else
            {
                int days = (int)elapsed.TotalDays;
                return days == 1 ? "1 day ago" : $"{days} days ago";
            }
        }

        #endregion
    }
}
