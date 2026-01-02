// ABOUTME: Reusable slider component for numeric range settings in the settings panel.
// ABOUTME: Supports label, value display, formatting, suffix, and range configuration.

using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.UI
{
    /// <summary>
    /// A reusable slider for numeric range settings.
    /// Used in the SettingsPanel for settings like Temperature, Elevation, Volume.
    /// </summary>
    public class SettingSlider : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private Slider _slider;
        [SerializeField] private TextMeshProUGUI _labelText;
        [SerializeField] private TextMeshProUGUI _valueText;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _fillImage;

        #endregion

        #region Private Fields

        private bool _isInitialized;
        private string _format = "F0";
        private string _suffix = "";
        private bool _suppressEvents;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the slider value changes.
        /// </summary>
        public event Action<float> OnValueChanged;

        #endregion

        #region Public Properties

        /// <summary>
        /// The label displayed above the slider.
        /// </summary>
        public string Label
        {
            get => _labelText != null ? _labelText.text : string.Empty;
            set { if (_labelText != null) _labelText.text = value; }
        }

        /// <summary>
        /// The suffix appended to the value display (e.g., " mph", " °F").
        /// </summary>
        public string Suffix
        {
            get => _suffix;
            set
            {
                _suffix = value ?? "";
                UpdateValueDisplay();
            }
        }

        /// <summary>
        /// The format string for the value display (e.g., "F0", "F1", "P0").
        /// </summary>
        public string Format
        {
            get => _format;
            set
            {
                _format = value ?? "F0";
                UpdateValueDisplay();
            }
        }

        /// <summary>
        /// The current slider value.
        /// </summary>
        public float Value
        {
            get => _slider != null ? _slider.value : 0f;
            set
            {
                if (_slider != null)
                {
                    float clampedValue = Mathf.Clamp(value, _slider.minValue, _slider.maxValue);
                    if (!Mathf.Approximately(_slider.value, clampedValue))
                    {
                        _slider.value = clampedValue;
                        UpdateValueDisplay();
                        OnValueChanged?.Invoke(clampedValue);
                    }
                }
            }
        }

        /// <summary>
        /// The normalized value (0-1) of the slider.
        /// </summary>
        public float NormalizedValue
        {
            get => _slider != null ? _slider.normalizedValue : 0f;
            set
            {
                if (_slider != null)
                {
                    float newValue = Mathf.Lerp(_slider.minValue, _slider.maxValue, Mathf.Clamp01(value));
                    Value = newValue;
                }
            }
        }

        /// <summary>
        /// The minimum allowed value.
        /// </summary>
        public float MinValue => _slider != null ? _slider.minValue : 0f;

        /// <summary>
        /// The maximum allowed value.
        /// </summary>
        public float MaxValue => _slider != null ? _slider.maxValue : 0f;

        /// <summary>
        /// Whether the slider uses whole numbers only.
        /// </summary>
        public bool WholeNumbers
        {
            get => _slider != null && _slider.wholeNumbers;
            set
            {
                if (_slider != null)
                {
                    _slider.wholeNumbers = value;
                    UpdateValueDisplay();
                }
            }
        }

        /// <summary>
        /// Whether the slider can be interacted with.
        /// </summary>
        public bool IsInteractable
        {
            get => _slider != null && _slider.interactable;
            set { if (_slider != null) _slider.interactable = value; }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            if (_slider != null)
            {
                _slider.onValueChanged.RemoveListener(HandleSliderChanged);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the slider range.
        /// </summary>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        public void SetRange(float min, float max)
        {
            if (_slider != null)
            {
                // Swap if min > max
                if (min > max)
                {
                    (min, max) = (max, min);
                }

                _slider.minValue = min;
                _slider.maxValue = max;
                UpdateValueDisplay();
            }
        }

        /// <summary>
        /// Sets the value without firing the OnValueChanged event.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public void SetWithoutNotify(float value)
        {
            if (_slider != null)
            {
                _suppressEvents = true;
                float clampedValue = Mathf.Clamp(value, _slider.minValue, _slider.maxValue);
                _slider.SetValueWithoutNotify(clampedValue);
                UpdateValueDisplay();
                _suppressEvents = false;
            }
        }

        /// <summary>
        /// Updates font sizes based on screen category.
        /// </summary>
        /// <param name="category">The current screen category.</param>
        public void UpdateFontSize(ScreenCategory category)
        {
            int fontSize = UITheme.GetFontSize(category, FontCategory.Normal);

            if (_labelText != null)
            {
                _labelText.fontSize = fontSize;
            }

            if (_valueText != null)
            {
                _valueText.fontSize = fontSize;
            }
        }

        #endregion

        #region Internal Methods (for testing)

        /// <summary>
        /// Force sets references for testing.
        /// </summary>
        internal void SetReferences(Slider slider, TextMeshProUGUI labelText, TextMeshProUGUI valueText,
            Image backgroundImage = null, Image fillImage = null)
        {
            _slider = slider;
            _labelText = labelText;
            _valueText = valueText;
            _backgroundImage = backgroundImage;
            _fillImage = fillImage;

            Initialize();
        }

        #endregion

        #region Private Methods

        private void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            if (_slider != null)
            {
                _slider.onValueChanged.AddListener(HandleSliderChanged);
            }

            UpdateValueDisplay();
            _isInitialized = true;
        }

        private void HandleSliderChanged(float value)
        {
            UpdateValueDisplay();

            if (!_suppressEvents)
            {
                OnValueChanged?.Invoke(value);
            }
        }

        private void UpdateValueDisplay()
        {
            if (_valueText == null || _slider == null)
            {
                return;
            }

            string formattedValue = _slider.value.ToString(_format, CultureInfo.InvariantCulture);
            _valueText.text = formattedValue + _suffix;
        }

        #endregion
    }
}
