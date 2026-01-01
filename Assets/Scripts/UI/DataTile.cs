// ABOUTME: Reusable UI component for displaying a single data value with label and unit.
// ABOUTME: Supports value formatting, direction prefixes (L/R), highlighting, and responsive sizing.

using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.UI
{
    /// <summary>
    /// A reusable data display tile showing a label, value, and unit.
    /// Used in the ShotDataBar for displaying shot metrics.
    /// </summary>
    public class DataTile : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private TextMeshProUGUI _labelText;
        [SerializeField] private TextMeshProUGUI _valueText;
        [SerializeField] private TextMeshProUGUI _unitText;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private CanvasGroup _canvasGroup;

        #endregion

        #region Private Fields

        private Color _normalValueColor = UITheme.TextPrimary;
        private Color _highlightValueColor = UITheme.TotalRed;
        private bool _isHighlighted;
        private Coroutine _animationCoroutine;
        private float _currentValue;
        private bool _hasValue;

        #endregion

        #region Public Properties

        /// <summary>
        /// The label displayed above the value.
        /// </summary>
        public string Label
        {
            get => _labelText != null ? _labelText.text : string.Empty;
            set { if (_labelText != null) _labelText.text = value; }
        }

        /// <summary>
        /// The unit displayed below the value.
        /// </summary>
        public string Unit
        {
            get => _unitText != null ? _unitText.text : string.Empty;
            set { if (_unitText != null) _unitText.text = value; }
        }

        /// <summary>
        /// Whether the tile is highlighted (e.g., Total distance).
        /// </summary>
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set
            {
                _isHighlighted = value;
                UpdateValueColor();
            }
        }

        /// <summary>
        /// The current numeric value displayed.
        /// </summary>
        public float CurrentValue => _currentValue;

        /// <summary>
        /// Whether the tile has a value set.
        /// </summary>
        public bool HasValue => _hasValue;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void OnDisable()
        {
            StopAnimation();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the value with a specific format string.
        /// </summary>
        /// <param name="value">The numeric value to display.</param>
        /// <param name="format">Format string (e.g., "F1" for 1 decimal, "N0" for thousands separator).</param>
        /// <param name="animate">Whether to animate the value change.</param>
        public void SetValue(float value, string format = "F1", bool animate = true)
        {
            _currentValue = value;
            _hasValue = true;

            if (_valueText != null)
            {
                _valueText.text = value.ToString(format, CultureInfo.InvariantCulture);
            }

            if (animate && gameObject.activeInHierarchy)
            {
                AnimateValueChange();
            }
        }

        /// <summary>
        /// Sets the value with a direction prefix (L/R).
        /// Positive values show as R (right), negative as L (left).
        /// </summary>
        /// <param name="value">The numeric value (sign determines L/R).</param>
        /// <param name="format">Format string for the absolute value.</param>
        /// <param name="animate">Whether to animate the value change.</param>
        public void SetValueWithDirection(float value, string format = "F1", bool animate = true)
        {
            _currentValue = value;
            _hasValue = true;

            if (_valueText != null)
            {
                string prefix = value < 0 ? "L" : "R";
                float absValue = Mathf.Abs(value);

                // Don't show direction for very small values
                if (absValue < 0.1f)
                {
                    _valueText.text = "0";
                }
                else
                {
                    _valueText.text = $"{prefix}{absValue.ToString(format, CultureInfo.InvariantCulture)}";
                }
            }

            if (animate && gameObject.activeInHierarchy)
            {
                AnimateValueChange();
            }
        }

        /// <summary>
        /// Sets the value with a thousands separator (for spin values).
        /// </summary>
        /// <param name="value">The numeric value to display.</param>
        /// <param name="animate">Whether to animate the value change.</param>
        public void SetValueWithThousands(float value, bool animate = true)
        {
            _currentValue = value;
            _hasValue = true;

            if (_valueText != null)
            {
                _valueText.text = ((int)Mathf.Abs(value)).ToString("N0", CultureInfo.InvariantCulture);
            }

            if (animate && gameObject.activeInHierarchy)
            {
                AnimateValueChange();
            }
        }

        /// <summary>
        /// Sets the value with direction prefix and thousands separator (for sidespin).
        /// </summary>
        /// <param name="value">The numeric value (sign determines L/R).</param>
        /// <param name="animate">Whether to animate the value change.</param>
        public void SetValueWithDirectionAndThousands(float value, bool animate = true)
        {
            _currentValue = value;
            _hasValue = true;

            if (_valueText != null)
            {
                string prefix = value < 0 ? "L" : "R";
                int absValue = (int)Mathf.Abs(value);

                if (absValue < 10)
                {
                    _valueText.text = "0";
                }
                else
                {
                    _valueText.text = $"{prefix}{absValue.ToString("N0", CultureInfo.InvariantCulture)}";
                }
            }

            if (animate && gameObject.activeInHierarchy)
            {
                AnimateValueChange();
            }
        }

        /// <summary>
        /// Sets the raw text value directly.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="animate">Whether to animate the value change.</param>
        public void SetText(string text, bool animate = true)
        {
            _hasValue = true;

            if (_valueText != null)
            {
                _valueText.text = text;
            }

            if (animate && gameObject.activeInHierarchy)
            {
                AnimateValueChange();
            }
        }

        /// <summary>
        /// Clears the displayed value.
        /// </summary>
        public void Clear()
        {
            _hasValue = false;
            _currentValue = 0f;

            if (_valueText != null)
            {
                _valueText.text = "-";
            }

            StopAnimation();
        }

        /// <summary>
        /// Updates font sizes based on screen category.
        /// </summary>
        /// <param name="category">The current screen category.</param>
        public void UpdateFontSize(ScreenCategory category)
        {
            if (_labelText != null)
            {
                _labelText.fontSize = UITheme.GetFontSize(category, FontCategory.Small);
            }

            if (_valueText != null)
            {
                _valueText.fontSize = UITheme.GetFontSize(category, FontCategory.DataValue);
            }

            if (_unitText != null)
            {
                _unitText.fontSize = UITheme.GetFontSize(category, FontCategory.Small);
            }
        }

        /// <summary>
        /// Sets the highlight color for the value (e.g., red for Total).
        /// </summary>
        /// <param name="color">The color to use when highlighted.</param>
        public void SetHighlightColor(Color color)
        {
            _highlightValueColor = color;
            UpdateValueColor();
        }

        /// <summary>
        /// Sets the normal (non-highlighted) color for the value.
        /// </summary>
        /// <param name="color">The color to use when not highlighted.</param>
        public void SetNormalColor(Color color)
        {
            _normalValueColor = color;
            UpdateValueColor();
        }

        #endregion

        #region Internal Methods (for testing)

        /// <summary>
        /// Gets the current displayed text value.
        /// </summary>
        internal string GetDisplayedValue()
        {
            return _valueText != null ? _valueText.text : string.Empty;
        }

        /// <summary>
        /// Gets the current value color.
        /// </summary>
        internal Color GetValueColor()
        {
            return _valueText != null ? _valueText.color : Color.white;
        }

        /// <summary>
        /// Force sets references for testing.
        /// </summary>
        internal void SetReferences(TextMeshProUGUI label, TextMeshProUGUI value, TextMeshProUGUI unit,
            Image background = null, CanvasGroup canvasGroup = null)
        {
            _labelText = label;
            _valueText = value;
            _unitText = unit;
            _backgroundImage = background;
            _canvasGroup = canvasGroup;

            UpdateValueColor();
        }

        #endregion

        #region Private Methods

        private void UpdateValueColor()
        {
            if (_valueText != null)
            {
                _valueText.color = _isHighlighted ? _highlightValueColor : _normalValueColor;
            }
        }

        private void AnimateValueChange()
        {
            StopAnimation();
            _animationCoroutine = StartCoroutine(AnimateHighlight());
        }

        private void StopAnimation()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }

            // Reset alpha
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }
        }

        private IEnumerator AnimateHighlight()
        {
            if (_canvasGroup == null)
            {
                yield break;
            }

            float duration = UITheme.Animation.DataHighlight;
            float halfDuration = duration / 2f;

            // Fade out slightly
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0.6f, t);
                yield return null;
            }

            // Fade back in
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                _canvasGroup.alpha = Mathf.Lerp(0.6f, 1f, t);
                yield return null;
            }

            _canvasGroup.alpha = 1f;
            _animationCoroutine = null;
        }

        #endregion
    }
}
