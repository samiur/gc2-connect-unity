// ABOUTME: Reusable toggle switch component for boolean settings in the settings panel.
// ABOUTME: Supports label, interactable state, and value changed events.

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.UI
{
    /// <summary>
    /// A reusable toggle switch for boolean settings.
    /// Used in the SettingsPanel for on/off settings like WindEnabled, AutoConnect.
    /// </summary>
    public class SettingToggle : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private Toggle _toggle;
        [SerializeField] private TextMeshProUGUI _labelText;
        [SerializeField] private Image _backgroundImage;

        #endregion

        #region Private Fields

        private bool _isInitialized;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the toggle value changes.
        /// </summary>
        public event Action<bool> OnValueChanged;

        #endregion

        #region Public Properties

        /// <summary>
        /// The label displayed next to the toggle.
        /// </summary>
        public string Label
        {
            get => _labelText != null ? _labelText.text : string.Empty;
            set { if (_labelText != null) _labelText.text = value; }
        }

        /// <summary>
        /// The current toggle state.
        /// </summary>
        public bool IsOn
        {
            get => _toggle != null && _toggle.isOn;
            set
            {
                if (_toggle != null && _toggle.isOn != value)
                {
                    _toggle.isOn = value;
                    OnValueChanged?.Invoke(value);
                }
            }
        }

        /// <summary>
        /// Whether the toggle can be interacted with.
        /// </summary>
        public bool IsInteractable
        {
            get => _toggle != null && _toggle.interactable;
            set { if (_toggle != null) _toggle.interactable = value; }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            if (_toggle != null)
            {
                _toggle.onValueChanged.RemoveListener(HandleToggleChanged);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the value without firing the OnValueChanged event.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public void SetWithoutNotify(bool value)
        {
            if (_toggle != null)
            {
                _toggle.SetIsOnWithoutNotify(value);
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
        internal void SetReferences(Toggle toggle, TextMeshProUGUI labelText, Image backgroundImage = null)
        {
            _toggle = toggle;
            _labelText = labelText;
            _backgroundImage = backgroundImage;

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

            if (_toggle != null)
            {
                _toggle.onValueChanged.AddListener(HandleToggleChanged);
            }

            _isInitialized = true;
        }

        private void HandleToggleChanged(bool value)
        {
            OnValueChanged?.Invoke(value);
        }

        #endregion
    }
}
