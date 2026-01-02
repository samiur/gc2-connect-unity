// ABOUTME: Reusable dropdown component for enum/option settings in the settings panel.
// ABOUTME: Supports label, options management, enum binding, and value changed events.

using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace OpenRange.UI
{
    /// <summary>
    /// A reusable dropdown for option/enum settings.
    /// Used in the SettingsPanel for settings like QualityTier, DistanceUnit.
    /// </summary>
    public class SettingDropdown : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private TMP_Dropdown _dropdown;
        [SerializeField] private TextMeshProUGUI _labelText;

        #endregion

        #region Private Fields

        private bool _isInitialized;
        private bool _suppressEvents;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the selected option changes.
        /// </summary>
        public event Action<int> OnValueChanged;

        #endregion

        #region Public Properties

        /// <summary>
        /// The label displayed above the dropdown.
        /// </summary>
        public string Label
        {
            get => _labelText != null ? _labelText.text : string.Empty;
            set { if (_labelText != null) _labelText.text = value; }
        }

        /// <summary>
        /// The currently selected option index.
        /// </summary>
        public int SelectedIndex
        {
            get => _dropdown != null ? _dropdown.value : 0;
            set
            {
                if (_dropdown != null && _dropdown.options.Count > 0)
                {
                    int clampedValue = Mathf.Clamp(value, 0, _dropdown.options.Count - 1);
                    if (_dropdown.value != clampedValue)
                    {
                        _dropdown.value = clampedValue;
                        OnValueChanged?.Invoke(clampedValue);
                    }
                }
            }
        }

        /// <summary>
        /// The text of the currently selected option.
        /// </summary>
        public string SelectedText
        {
            get
            {
                if (_dropdown == null || _dropdown.options.Count == 0)
                {
                    return string.Empty;
                }
                return _dropdown.options[_dropdown.value].text;
            }
        }

        /// <summary>
        /// The number of options in the dropdown.
        /// </summary>
        public int OptionCount => _dropdown != null ? _dropdown.options.Count : 0;

        /// <summary>
        /// Whether the dropdown can be interacted with.
        /// </summary>
        public bool IsInteractable
        {
            get => _dropdown != null && _dropdown.interactable;
            set { if (_dropdown != null) _dropdown.interactable = value; }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            if (_dropdown != null)
            {
                _dropdown.onValueChanged.RemoveListener(HandleDropdownChanged);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the dropdown options from a string array.
        /// </summary>
        /// <param name="options">The option strings.</param>
        public void SetOptions(string[] options)
        {
            if (_dropdown == null)
            {
                return;
            }

            _dropdown.ClearOptions();

            if (options == null || options.Length == 0)
            {
                return;
            }

            var optionDataList = new List<TMP_Dropdown.OptionData>();
            foreach (string option in options)
            {
                optionDataList.Add(new TMP_Dropdown.OptionData(option));
            }

            _dropdown.AddOptions(optionDataList);
        }

        /// <summary>
        /// Sets the dropdown options from a string list.
        /// </summary>
        /// <param name="options">The option strings.</param>
        public void SetOptions(List<string> options)
        {
            SetOptions(options?.ToArray() ?? Array.Empty<string>());
        }

        /// <summary>
        /// Sets the dropdown options from an enum type.
        /// </summary>
        /// <typeparam name="T">The enum type.</typeparam>
        public void SetOptionsFromEnum<T>() where T : Enum
        {
            string[] names = Enum.GetNames(typeof(T));
            SetOptions(names);
        }

        /// <summary>
        /// Gets the text of an option at the specified index.
        /// </summary>
        /// <param name="index">The option index.</param>
        /// <returns>The option text, or empty string if invalid index.</returns>
        public string GetOptionText(int index)
        {
            if (_dropdown == null || index < 0 || index >= _dropdown.options.Count)
            {
                return string.Empty;
            }
            return _dropdown.options[index].text;
        }

        /// <summary>
        /// Gets the selected value as an enum.
        /// </summary>
        /// <typeparam name="T">The enum type.</typeparam>
        /// <returns>The selected enum value.</returns>
        public T GetSelectedEnum<T>() where T : Enum
        {
            return (T)Enum.ToObject(typeof(T), SelectedIndex);
        }

        /// <summary>
        /// Sets the selected value from an enum.
        /// </summary>
        /// <typeparam name="T">The enum type.</typeparam>
        /// <param name="value">The enum value to select.</param>
        public void SetSelectedEnum<T>(T value) where T : Enum
        {
            SelectedIndex = Convert.ToInt32(value);
        }

        /// <summary>
        /// Sets the selected index without firing the OnValueChanged event.
        /// </summary>
        /// <param name="index">The index to select.</param>
        public void SetWithoutNotify(int index)
        {
            if (_dropdown != null && _dropdown.options.Count > 0)
            {
                _suppressEvents = true;
                int clampedValue = Mathf.Clamp(index, 0, _dropdown.options.Count - 1);
                _dropdown.SetValueWithoutNotify(clampedValue);
                _suppressEvents = false;
            }
        }

        /// <summary>
        /// Updates font sizes based on screen category.
        /// </summary>
        /// <param name="category">The current screen category.</param>
        public void UpdateFontSize(ScreenCategory category)
        {
            if (_labelText != null)
            {
                _labelText.fontSize = UITheme.GetFontSize(category, FontCategory.Normal);
            }
        }

        #endregion

        #region Internal Methods (for testing)

        /// <summary>
        /// Force sets references for testing.
        /// </summary>
        internal void SetReferences(TMP_Dropdown dropdown, TextMeshProUGUI labelText)
        {
            _dropdown = dropdown;
            _labelText = labelText;

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

            if (_dropdown != null)
            {
                _dropdown.onValueChanged.AddListener(HandleDropdownChanged);
            }

            _isInitialized = true;
        }

        private void HandleDropdownChanged(int index)
        {
            if (!_suppressEvents)
            {
                OnValueChanged?.Invoke(index);
            }
        }

        #endregion
    }
}
