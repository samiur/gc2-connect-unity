// ABOUTME: Main settings panel containing all application settings sections.
// ABOUTME: Binds to SettingsManager for persistence and provides reset to defaults.

using System;
using OpenRange.Core;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.UI
{
    /// <summary>
    /// Main settings panel containing all application settings.
    /// Organizes settings into sections: Graphics, Units, Environment, Connection, Audio.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Panel")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _resetButton;

        [Header("Graphics Settings")]
        [SerializeField] private SettingDropdown _qualityDropdown;
        [SerializeField] private SettingDropdown _frameRateDropdown;

        [Header("Units Settings")]
        [SerializeField] private SettingDropdown _distanceUnitDropdown;
        [SerializeField] private SettingDropdown _speedUnitDropdown;
        [SerializeField] private SettingDropdown _tempUnitDropdown;

        [Header("Environment Settings")]
        [SerializeField] private SettingSlider _temperatureSlider;
        [SerializeField] private SettingSlider _elevationSlider;
        [SerializeField] private SettingSlider _humiditySlider;
        [SerializeField] private SettingToggle _windEnabledToggle;
        [SerializeField] private SettingSlider _windSpeedSlider;
        [SerializeField] private SettingSlider _windDirectionSlider;

        [Header("Connection Settings")]
        [SerializeField] private SettingToggle _autoConnectToggle;

        [Header("Audio Settings")]
        [SerializeField] private SettingSlider _masterVolumeSlider;
        [SerializeField] private SettingSlider _effectsVolumeSlider;

        #endregion

        #region Private Fields

        private SettingsManager _settingsManager;
        private bool _isInitialized;
        private bool _suppressSettingsEvents;

        #endregion

        #region Events

        /// <summary>Fired when the panel is shown.</summary>
        public event Action OnPanelShown;

        /// <summary>Fired when the panel is hidden.</summary>
        public event Action OnPanelHidden;

        /// <summary>Fired when the reset button is clicked.</summary>
        public event Action OnResetClicked;

        #endregion

        #region Public Properties

        /// <summary>Whether the panel is currently visible.</summary>
        public bool IsVisible => _canvasGroup != null && _canvasGroup.alpha > 0.5f;

        /// <summary>Quality tier dropdown control.</summary>
        public SettingDropdown QualityDropdown => _qualityDropdown;

        /// <summary>Frame rate dropdown control.</summary>
        public SettingDropdown FrameRateDropdown => _frameRateDropdown;

        /// <summary>Distance unit dropdown control.</summary>
        public SettingDropdown DistanceUnitDropdown => _distanceUnitDropdown;

        /// <summary>Speed unit dropdown control.</summary>
        public SettingDropdown SpeedUnitDropdown => _speedUnitDropdown;

        /// <summary>Temperature unit dropdown control.</summary>
        public SettingDropdown TempUnitDropdown => _tempUnitDropdown;

        /// <summary>Temperature slider control.</summary>
        public SettingSlider TemperatureSlider => _temperatureSlider;

        /// <summary>Elevation slider control.</summary>
        public SettingSlider ElevationSlider => _elevationSlider;

        /// <summary>Humidity slider control.</summary>
        public SettingSlider HumiditySlider => _humiditySlider;

        /// <summary>Wind enabled toggle control.</summary>
        public SettingToggle WindEnabledToggle => _windEnabledToggle;

        /// <summary>Wind speed slider control.</summary>
        public SettingSlider WindSpeedSlider => _windSpeedSlider;

        /// <summary>Wind direction slider control.</summary>
        public SettingSlider WindDirectionSlider => _windDirectionSlider;

        /// <summary>Auto-connect toggle control.</summary>
        public SettingToggle AutoConnectToggle => _autoConnectToggle;

        /// <summary>Master volume slider control.</summary>
        public SettingSlider MasterVolumeSlider => _masterVolumeSlider;

        /// <summary>Effects volume slider control.</summary>
        public SettingSlider EffectsVolumeSlider => _effectsVolumeSlider;

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
            SetupControlLabels();
            SetupControlRanges();
            BindToSettingsManager();
            Hide();
        }

        private void OnDestroy()
        {
            UnbindFromSettingsManager();

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseClicked);
            }

            if (_resetButton != null)
            {
                _resetButton.onClick.RemoveListener(OnResetButtonClicked);
            }

            UnsubscribeFromControls();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the settings panel.
        /// </summary>
        public void Show()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            RefreshFromSettings();
            OnPanelShown?.Invoke();
        }

        /// <summary>
        /// Hides the settings panel.
        /// </summary>
        public void Hide()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            // Save settings when closing
            if (_settingsManager != null)
            {
                _settingsManager.SaveSettings();
            }

            OnPanelHidden?.Invoke();
        }

        /// <summary>
        /// Toggles the panel visibility.
        /// </summary>
        public void Toggle()
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        /// <summary>
        /// Updates the wind control enabled/disabled states based on WindEnabled toggle.
        /// </summary>
        public void UpdateWindControlStates()
        {
            bool windEnabled = _windEnabledToggle != null && _windEnabledToggle.IsOn;

            if (_windSpeedSlider != null)
            {
                _windSpeedSlider.IsInteractable = windEnabled;
            }

            if (_windDirectionSlider != null)
            {
                _windDirectionSlider.IsInteractable = windEnabled;
            }
        }

        /// <summary>
        /// Sets the SettingsManager to bind to.
        /// </summary>
        /// <param name="settingsManager">The settings manager.</param>
        public void SetSettingsManager(SettingsManager settingsManager)
        {
            UnbindFromSettingsManager();
            _settingsManager = settingsManager;
            BindToSettingsManager();
            RefreshFromSettings();
        }

        #endregion

        #region Internal Methods (for testing)

        /// <summary>
        /// Force sets references for testing.
        /// </summary>
        internal void SetReferences(
            CanvasGroup canvasGroup,
            Button closeButton,
            Button resetButton,
            SettingDropdown qualityDropdown,
            SettingDropdown frameRateDropdown,
            SettingDropdown distanceUnitDropdown,
            SettingDropdown speedUnitDropdown,
            SettingDropdown tempUnitDropdown,
            SettingSlider temperatureSlider,
            SettingSlider elevationSlider,
            SettingSlider humiditySlider,
            SettingToggle windEnabledToggle,
            SettingSlider windSpeedSlider,
            SettingSlider windDirectionSlider,
            SettingToggle autoConnectToggle,
            SettingSlider masterVolumeSlider,
            SettingSlider effectsVolumeSlider)
        {
            _canvasGroup = canvasGroup;
            _closeButton = closeButton;
            _resetButton = resetButton;
            _qualityDropdown = qualityDropdown;
            _frameRateDropdown = frameRateDropdown;
            _distanceUnitDropdown = distanceUnitDropdown;
            _speedUnitDropdown = speedUnitDropdown;
            _tempUnitDropdown = tempUnitDropdown;
            _temperatureSlider = temperatureSlider;
            _elevationSlider = elevationSlider;
            _humiditySlider = humiditySlider;
            _windEnabledToggle = windEnabledToggle;
            _windSpeedSlider = windSpeedSlider;
            _windDirectionSlider = windDirectionSlider;
            _autoConnectToggle = autoConnectToggle;
            _masterVolumeSlider = masterVolumeSlider;
            _effectsVolumeSlider = effectsVolumeSlider;
        }

        /// <summary>
        /// Simulates a reset button click for testing.
        /// </summary>
        internal void SimulateResetClick()
        {
            OnResetButtonClicked();
        }

        #endregion

        #region Private Methods

        private void SetupButtonListeners()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseClicked);
            }

            if (_resetButton != null)
            {
                _resetButton.onClick.AddListener(OnResetButtonClicked);
            }
        }

        private void SetupControlLabels()
        {
            // Graphics
            if (_qualityDropdown != null)
            {
                _qualityDropdown.Label = "Quality";
                _qualityDropdown.SetOptionsFromEnum<QualityTier>();
            }

            if (_frameRateDropdown != null)
            {
                _frameRateDropdown.Label = "Frame Rate";
                _frameRateDropdown.SetOptions(new[] { "30 FPS", "60 FPS", "120 FPS" });
            }

            // Units
            if (_distanceUnitDropdown != null)
            {
                _distanceUnitDropdown.Label = "Distance";
                _distanceUnitDropdown.SetOptionsFromEnum<DistanceUnit>();
            }

            if (_speedUnitDropdown != null)
            {
                _speedUnitDropdown.Label = "Speed";
                _speedUnitDropdown.SetOptionsFromEnum<SpeedUnit>();
            }

            if (_tempUnitDropdown != null)
            {
                _tempUnitDropdown.Label = "Temperature";
                _tempUnitDropdown.SetOptionsFromEnum<TemperatureUnit>();
            }

            // Environment
            if (_temperatureSlider != null)
            {
                _temperatureSlider.Label = "Temperature";
                _temperatureSlider.Suffix = " °F";
            }

            if (_elevationSlider != null)
            {
                _elevationSlider.Label = "Elevation";
                _elevationSlider.Suffix = " ft";
            }

            if (_humiditySlider != null)
            {
                _humiditySlider.Label = "Humidity";
                _humiditySlider.Suffix = "%";
            }

            if (_windEnabledToggle != null)
            {
                _windEnabledToggle.Label = "Wind Enabled";
            }

            if (_windSpeedSlider != null)
            {
                _windSpeedSlider.Label = "Wind Speed";
                _windSpeedSlider.Suffix = " mph";
            }

            if (_windDirectionSlider != null)
            {
                _windDirectionSlider.Label = "Wind Direction";
                _windDirectionSlider.Suffix = "°";
            }

            // Connection
            if (_autoConnectToggle != null)
            {
                _autoConnectToggle.Label = "Auto-Connect";
            }

            // Audio
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.Label = "Master Volume";
                _masterVolumeSlider.Suffix = "%";
            }

            if (_effectsVolumeSlider != null)
            {
                _effectsVolumeSlider.Label = "Effects Volume";
                _effectsVolumeSlider.Suffix = "%";
            }
        }

        private void SetupControlRanges()
        {
            // Environment ranges
            if (_temperatureSlider != null)
            {
                _temperatureSlider.SetRange(40f, 100f);
                _temperatureSlider.WholeNumbers = true;
            }

            if (_elevationSlider != null)
            {
                _elevationSlider.SetRange(0f, 8000f);
                _elevationSlider.WholeNumbers = true;
            }

            if (_humiditySlider != null)
            {
                _humiditySlider.SetRange(0f, 100f);
                _humiditySlider.WholeNumbers = true;
            }

            if (_windSpeedSlider != null)
            {
                _windSpeedSlider.SetRange(0f, 30f);
                _windSpeedSlider.WholeNumbers = true;
            }

            if (_windDirectionSlider != null)
            {
                _windDirectionSlider.SetRange(0f, 360f);
                _windDirectionSlider.WholeNumbers = true;
            }

            // Audio ranges (0-100 percent)
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.SetRange(0f, 100f);
                _masterVolumeSlider.WholeNumbers = true;
            }

            if (_effectsVolumeSlider != null)
            {
                _effectsVolumeSlider.SetRange(0f, 100f);
                _effectsVolumeSlider.WholeNumbers = true;
            }
        }

        private void BindToSettingsManager()
        {
            if (_settingsManager == null)
            {
                _settingsManager = SettingsManager.Instance;
            }

            if (_settingsManager != null)
            {
                _settingsManager.OnSettingsChanged += OnSettingsChanged;
            }

            SubscribeToControls();
        }

        private void UnbindFromSettingsManager()
        {
            if (_settingsManager != null)
            {
                _settingsManager.OnSettingsChanged -= OnSettingsChanged;
            }
        }

        private void SubscribeToControls()
        {
            // Graphics
            if (_qualityDropdown != null)
            {
                _qualityDropdown.OnValueChanged += OnQualityChanged;
            }

            if (_frameRateDropdown != null)
            {
                _frameRateDropdown.OnValueChanged += OnFrameRateChanged;
            }

            // Units
            if (_distanceUnitDropdown != null)
            {
                _distanceUnitDropdown.OnValueChanged += OnDistanceUnitChanged;
            }

            if (_speedUnitDropdown != null)
            {
                _speedUnitDropdown.OnValueChanged += OnSpeedUnitChanged;
            }

            if (_tempUnitDropdown != null)
            {
                _tempUnitDropdown.OnValueChanged += OnTempUnitChanged;
            }

            // Environment
            if (_temperatureSlider != null)
            {
                _temperatureSlider.OnValueChanged += OnTemperatureChanged;
            }

            if (_elevationSlider != null)
            {
                _elevationSlider.OnValueChanged += OnElevationChanged;
            }

            if (_humiditySlider != null)
            {
                _humiditySlider.OnValueChanged += OnHumidityChanged;
            }

            if (_windEnabledToggle != null)
            {
                _windEnabledToggle.OnValueChanged += OnWindEnabledChanged;
            }

            if (_windSpeedSlider != null)
            {
                _windSpeedSlider.OnValueChanged += OnWindSpeedChanged;
            }

            if (_windDirectionSlider != null)
            {
                _windDirectionSlider.OnValueChanged += OnWindDirectionChanged;
            }

            // Connection
            if (_autoConnectToggle != null)
            {
                _autoConnectToggle.OnValueChanged += OnAutoConnectChanged;
            }

            // Audio
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.OnValueChanged += OnMasterVolumeChanged;
            }

            if (_effectsVolumeSlider != null)
            {
                _effectsVolumeSlider.OnValueChanged += OnEffectsVolumeChanged;
            }
        }

        private void UnsubscribeFromControls()
        {
            // Graphics
            if (_qualityDropdown != null)
            {
                _qualityDropdown.OnValueChanged -= OnQualityChanged;
            }

            if (_frameRateDropdown != null)
            {
                _frameRateDropdown.OnValueChanged -= OnFrameRateChanged;
            }

            // Units
            if (_distanceUnitDropdown != null)
            {
                _distanceUnitDropdown.OnValueChanged -= OnDistanceUnitChanged;
            }

            if (_speedUnitDropdown != null)
            {
                _speedUnitDropdown.OnValueChanged -= OnSpeedUnitChanged;
            }

            if (_tempUnitDropdown != null)
            {
                _tempUnitDropdown.OnValueChanged -= OnTempUnitChanged;
            }

            // Environment
            if (_temperatureSlider != null)
            {
                _temperatureSlider.OnValueChanged -= OnTemperatureChanged;
            }

            if (_elevationSlider != null)
            {
                _elevationSlider.OnValueChanged -= OnElevationChanged;
            }

            if (_humiditySlider != null)
            {
                _humiditySlider.OnValueChanged -= OnHumidityChanged;
            }

            if (_windEnabledToggle != null)
            {
                _windEnabledToggle.OnValueChanged -= OnWindEnabledChanged;
            }

            if (_windSpeedSlider != null)
            {
                _windSpeedSlider.OnValueChanged -= OnWindSpeedChanged;
            }

            if (_windDirectionSlider != null)
            {
                _windDirectionSlider.OnValueChanged -= OnWindDirectionChanged;
            }

            // Connection
            if (_autoConnectToggle != null)
            {
                _autoConnectToggle.OnValueChanged -= OnAutoConnectChanged;
            }

            // Audio
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.OnValueChanged -= OnMasterVolumeChanged;
            }

            if (_effectsVolumeSlider != null)
            {
                _effectsVolumeSlider.OnValueChanged -= OnEffectsVolumeChanged;
            }
        }

        private void RefreshFromSettings()
        {
            if (_settingsManager == null)
            {
                return;
            }

            _suppressSettingsEvents = true;

            // Graphics
            if (_qualityDropdown != null)
            {
                _qualityDropdown.SetWithoutNotify((int)_settingsManager.QualityTier);
            }

            if (_frameRateDropdown != null)
            {
                int frameRateIndex = _settingsManager.TargetFrameRate switch
                {
                    30 => 0,
                    60 => 1,
                    120 => 2,
                    _ => 1
                };
                _frameRateDropdown.SetWithoutNotify(frameRateIndex);
            }

            // Units
            if (_distanceUnitDropdown != null)
            {
                _distanceUnitDropdown.SetWithoutNotify((int)_settingsManager.DistanceUnit);
            }

            if (_speedUnitDropdown != null)
            {
                _speedUnitDropdown.SetWithoutNotify((int)_settingsManager.SpeedUnit);
            }

            if (_tempUnitDropdown != null)
            {
                _tempUnitDropdown.SetWithoutNotify((int)_settingsManager.TemperatureUnit);
            }

            // Environment
            if (_temperatureSlider != null)
            {
                _temperatureSlider.SetWithoutNotify(_settingsManager.TemperatureF);
            }

            if (_elevationSlider != null)
            {
                _elevationSlider.SetWithoutNotify(_settingsManager.ElevationFt);
            }

            if (_humiditySlider != null)
            {
                _humiditySlider.SetWithoutNotify(_settingsManager.HumidityPct);
            }

            if (_windEnabledToggle != null)
            {
                _windEnabledToggle.SetWithoutNotify(_settingsManager.WindEnabled);
            }

            if (_windSpeedSlider != null)
            {
                _windSpeedSlider.SetWithoutNotify(_settingsManager.WindSpeedMph);
            }

            if (_windDirectionSlider != null)
            {
                _windDirectionSlider.SetWithoutNotify(_settingsManager.WindDirectionDeg);
            }

            // Connection
            if (_autoConnectToggle != null)
            {
                _autoConnectToggle.SetWithoutNotify(_settingsManager.AutoConnect);
            }

            // Audio (convert 0-1 to 0-100 percent)
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.SetWithoutNotify(_settingsManager.MasterVolume * 100f);
            }

            if (_effectsVolumeSlider != null)
            {
                _effectsVolumeSlider.SetWithoutNotify(_settingsManager.EffectsVolume * 100f);
            }

            UpdateWindControlStates();
            _suppressSettingsEvents = false;
        }

        private void OnCloseClicked()
        {
            Hide();
        }

        private void OnResetButtonClicked()
        {
            if (_settingsManager != null)
            {
                _settingsManager.ResetToDefaults();
            }

            RefreshFromSettings();
            OnResetClicked?.Invoke();
        }

        private void OnSettingsChanged()
        {
            if (!_suppressSettingsEvents)
            {
                RefreshFromSettings();
            }
        }

        #region Control Event Handlers

        private void OnQualityChanged(int index)
        {
            if (_settingsManager != null && !_suppressSettingsEvents)
            {
                _settingsManager.QualityTier = (QualityTier)index;
            }
        }

        private void OnFrameRateChanged(int index)
        {
            if (_settingsManager != null && !_suppressSettingsEvents)
            {
                _settingsManager.TargetFrameRate = index switch
                {
                    0 => 30,
                    1 => 60,
                    2 => 120,
                    _ => 60
                };
            }
        }

        private void OnDistanceUnitChanged(int index)
        {
            if (_settingsManager != null && !_suppressSettingsEvents)
            {
                _settingsManager.DistanceUnit = (DistanceUnit)index;
            }
        }

        private void OnSpeedUnitChanged(int index)
        {
            if (_settingsManager != null && !_suppressSettingsEvents)
            {
                _settingsManager.SpeedUnit = (SpeedUnit)index;
            }
        }

        private void OnTempUnitChanged(int index)
        {
            if (_settingsManager != null && !_suppressSettingsEvents)
            {
                _settingsManager.TemperatureUnit = (TemperatureUnit)index;
            }
        }

        private void OnTemperatureChanged(float value)
        {
            if (_settingsManager != null && !_suppressSettingsEvents)
            {
                _settingsManager.TemperatureF = value;
            }
        }

        private void OnElevationChanged(float value)
        {
            if (_settingsManager != null && !_suppressSettingsEvents)
            {
                _settingsManager.ElevationFt = value;
            }
        }

        private void OnHumidityChanged(float value)
        {
            if (_settingsManager != null && !_suppressSettingsEvents)
            {
                _settingsManager.HumidityPct = value;
            }
        }

        private void OnWindEnabledChanged(bool value)
        {
            if (_settingsManager != null && !_suppressSettingsEvents)
            {
                _settingsManager.WindEnabled = value;
            }

            UpdateWindControlStates();
        }

        private void OnWindSpeedChanged(float value)
        {
            if (_settingsManager != null && !_suppressSettingsEvents)
            {
                _settingsManager.WindSpeedMph = value;
            }
        }

        private void OnWindDirectionChanged(float value)
        {
            if (_settingsManager != null && !_suppressSettingsEvents)
            {
                _settingsManager.WindDirectionDeg = value;
            }
        }

        private void OnAutoConnectChanged(bool value)
        {
            if (_settingsManager != null && !_suppressSettingsEvents)
            {
                _settingsManager.AutoConnect = value;
            }
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (_settingsManager != null && !_suppressSettingsEvents)
            {
                _settingsManager.MasterVolume = value / 100f;
            }
        }

        private void OnEffectsVolumeChanged(float value)
        {
            if (_settingsManager != null && !_suppressSettingsEvents)
            {
                _settingsManager.EffectsVolume = value / 100f;
            }
        }

        #endregion

        #endregion
    }
}
