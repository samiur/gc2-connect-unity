// ABOUTME: Runtime panel for firing test shots without GC2 hardware in built applications.
// ABOUTME: Provides presets, sliders for ball/spin/club data, and integrates with ShotProcessor.

using System;
using OpenRange.Core;
using OpenRange.GC2;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.UI
{
    /// <summary>
    /// Runtime panel for firing test shots without GC2 hardware.
    /// Works in built applications, unlike TestShotWindow which is Editor-only.
    /// </summary>
    public class TestShotPanel : MonoBehaviour
    {
        #region Shot Preset Data

        /// <summary>
        /// Preset shot configurations matching TestShotWindow values.
        /// </summary>
        public static class Presets
        {
            public static readonly ShotPreset Driver = new ShotPreset(
                "Driver", 167f, 10.9f, 0f, 2686f, 0f);

            public static readonly ShotPreset SevenIron = new ShotPreset(
                "7-Iron", 120f, 16.3f, 0f, 7097f, 0f);

            public static readonly ShotPreset Wedge = new ShotPreset(
                "Wedge", 102f, 24.2f, 0f, 9304f, 0f);

            public static readonly ShotPreset Hook = new ShotPreset(
                "Hook", 150f, 12f, 0f, 3000f, -1500f);

            public static readonly ShotPreset Slice = new ShotPreset(
                "Slice", 150f, 12f, 0f, 3000f, 1500f);
        }

        /// <summary>
        /// Data structure for a shot preset.
        /// </summary>
        public readonly struct ShotPreset
        {
            public readonly string Name;
            public readonly float BallSpeed;
            public readonly float LaunchAngle;
            public readonly float Direction;
            public readonly float BackSpin;
            public readonly float SideSpin;

            public ShotPreset(string name, float ballSpeed, float launchAngle,
                float direction, float backSpin, float sideSpin)
            {
                Name = name;
                BallSpeed = ballSpeed;
                LaunchAngle = launchAngle;
                Direction = direction;
                BackSpin = backSpin;
                SideSpin = sideSpin;
            }
        }

        #endregion

        #region Layout Constants

        /// <summary>Panel width in pixels.</summary>
        public const float PanelWidth = 300f;

        /// <summary>Panel padding.</summary>
        public const float PanelPadding = 12f;

        /// <summary>Spacing between sections.</summary>
        public const float SectionSpacing = 12f;

        /// <summary>Spacing between items in a section.</summary>
        public const float ItemSpacing = 6f;

        /// <summary>Height of preset buttons.</summary>
        public const float PresetButtonHeight = 36f;

        /// <summary>Height of the fire shot button.</summary>
        public const float FireButtonHeight = 48f;

        /// <summary>Animation duration for show/hide.</summary>
        public const float AnimationDuration = 0.25f;

        #endregion

        #region Slider Range Constants

        public const float MinBallSpeed = 50f;
        public const float MaxBallSpeed = 200f;
        public const float MinLaunchAngle = 0f;
        public const float MaxLaunchAngle = 45f;
        public const float MinDirection = -20f;
        public const float MaxDirection = 20f;
        public const float MinBackSpin = 0f;
        public const float MaxBackSpin = 12000f;
        public const float MinSideSpin = -3000f;
        public const float MaxSideSpin = 3000f;

        // Club data ranges
        public const float MinClubSpeed = 60f;
        public const float MaxClubSpeed = 130f;
        public const float MinAttackAngle = -10f;
        public const float MaxAttackAngle = 10f;
        public const float MinFaceToTarget = -10f;
        public const float MaxFaceToTarget = 10f;
        public const float MinPath = -15f;
        public const float MaxPath = 15f;
        public const float MinDynamicLoft = 5f;
        public const float MaxDynamicLoft = 50f;

        #endregion

        #region Serialized Fields

        [Header("Panel Components")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panelRect;

        [Header("Preset Buttons")]
        [SerializeField] private Button _driverButton;
        [SerializeField] private Button _sevenIronButton;
        [SerializeField] private Button _wedgeButton;
        [SerializeField] private Button _hookButton;
        [SerializeField] private Button _sliceButton;

        [Header("Ball Data Sliders")]
        [SerializeField] private SettingSlider _ballSpeedSlider;
        [SerializeField] private SettingSlider _launchAngleSlider;
        [SerializeField] private SettingSlider _directionSlider;

        [Header("Spin Data Sliders")]
        [SerializeField] private SettingSlider _backSpinSlider;
        [SerializeField] private SettingSlider _sideSpinSlider;

        [Header("Club Data (Optional)")]
        [SerializeField] private SettingToggle _clubDataToggle;
        [SerializeField] private GameObject _clubDataSection;
        [SerializeField] private SettingSlider _clubSpeedSlider;
        [SerializeField] private SettingSlider _attackAngleSlider;
        [SerializeField] private SettingSlider _faceToTargetSlider;
        [SerializeField] private SettingSlider _pathSlider;
        [SerializeField] private SettingSlider _dynamicLoftSlider;

        [Header("Action Buttons")]
        [SerializeField] private Button _fireShotButton;
        [SerializeField] private Button _resetBallButton;
        [SerializeField] private Button _closeButton;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI _statusText;

        #endregion

        #region Private Fields

        private bool _isVisible;
        private float _animationProgress;
        private bool _isAnimating;
        private bool _targetVisible;

        // Current slider values
        private float _ballSpeed = 150f;
        private float _launchAngle = 12f;
        private float _direction = 0f;
        private float _backSpin = 3000f;
        private float _sideSpin = 0f;
        private bool _includeClubData = false;
        private float _clubSpeed = 95f;
        private float _attackAngle = 0f;
        private float _faceToTarget = 0f;
        private float _path = 0f;
        private float _dynamicLoft = 15f;

        #endregion

        #region Events

        /// <summary>
        /// Fired when a test shot is triggered.
        /// </summary>
        public event Action<GC2ShotData> OnTestShotFired;

        /// <summary>
        /// Fired when the panel visibility changes.
        /// </summary>
        public event Action<bool> OnVisibilityChanged;

        #endregion

        #region Public Properties

        /// <summary>
        /// Whether the panel is currently visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Current ball speed value.
        /// </summary>
        public float BallSpeed => _ballSpeed;

        /// <summary>
        /// Current launch angle value.
        /// </summary>
        public float LaunchAngle => _launchAngle;

        /// <summary>
        /// Current direction value.
        /// </summary>
        public float Direction => _direction;

        /// <summary>
        /// Current backspin value.
        /// </summary>
        public float BackSpin => _backSpin;

        /// <summary>
        /// Current sidespin value.
        /// </summary>
        public float SideSpin => _sideSpin;

        /// <summary>
        /// Whether club data is included.
        /// </summary>
        public bool IncludeClubData => _includeClubData;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupSliders();
            SetupButtons();

            // Start hidden
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
            _isVisible = false;
        }

        private void Update()
        {
            HandleAnimation();
            HandleKeyboardShortcuts();
        }

        private void OnDestroy()
        {
            UnsubscribeFromButtons();
            UnsubscribeFromSliders();
        }

        #endregion

        #region Setup Methods

        private void SetupSliders()
        {
            // Ball data sliders
            if (_ballSpeedSlider != null)
            {
                _ballSpeedSlider.SetRange(MinBallSpeed, MaxBallSpeed);
                _ballSpeedSlider.Format = "F0";
                _ballSpeedSlider.Suffix = " mph";
                _ballSpeedSlider.Label = "Ball Speed";
                _ballSpeedSlider.SetWithoutNotify(_ballSpeed);
                _ballSpeedSlider.OnValueChanged += OnBallSpeedChanged;
            }

            if (_launchAngleSlider != null)
            {
                _launchAngleSlider.SetRange(MinLaunchAngle, MaxLaunchAngle);
                _launchAngleSlider.Format = "F1";
                _launchAngleSlider.Suffix = "°";
                _launchAngleSlider.Label = "Launch Angle";
                _launchAngleSlider.SetWithoutNotify(_launchAngle);
                _launchAngleSlider.OnValueChanged += OnLaunchAngleChanged;
            }

            if (_directionSlider != null)
            {
                _directionSlider.SetRange(MinDirection, MaxDirection);
                _directionSlider.Format = "F1";
                _directionSlider.Suffix = "°";
                _directionSlider.Label = "Direction";
                _directionSlider.SetWithoutNotify(_direction);
                _directionSlider.OnValueChanged += OnDirectionChanged;
            }

            // Spin data sliders
            if (_backSpinSlider != null)
            {
                _backSpinSlider.SetRange(MinBackSpin, MaxBackSpin);
                _backSpinSlider.Format = "F0";
                _backSpinSlider.Suffix = " rpm";
                _backSpinSlider.Label = "Back Spin";
                _backSpinSlider.SetWithoutNotify(_backSpin);
                _backSpinSlider.OnValueChanged += OnBackSpinChanged;
            }

            if (_sideSpinSlider != null)
            {
                _sideSpinSlider.SetRange(MinSideSpin, MaxSideSpin);
                _sideSpinSlider.Format = "F0";
                _sideSpinSlider.Suffix = " rpm";
                _sideSpinSlider.Label = "Side Spin";
                _sideSpinSlider.SetWithoutNotify(_sideSpin);
                _sideSpinSlider.OnValueChanged += OnSideSpinChanged;
            }

            // Club data sliders
            SetupClubDataSliders();

            // Club data toggle
            if (_clubDataToggle != null)
            {
                _clubDataToggle.Label = "Include Club Data";
                _clubDataToggle.SetWithoutNotify(_includeClubData);
                _clubDataToggle.OnValueChanged += OnClubDataToggleChanged;
            }

            // Hide club data section initially
            if (_clubDataSection != null)
            {
                _clubDataSection.SetActive(_includeClubData);
            }
        }

        private void SetupClubDataSliders()
        {
            if (_clubSpeedSlider != null)
            {
                _clubSpeedSlider.SetRange(MinClubSpeed, MaxClubSpeed);
                _clubSpeedSlider.Format = "F0";
                _clubSpeedSlider.Suffix = " mph";
                _clubSpeedSlider.Label = "Club Speed";
                _clubSpeedSlider.SetWithoutNotify(_clubSpeed);
                _clubSpeedSlider.OnValueChanged += OnClubSpeedChanged;
            }

            if (_attackAngleSlider != null)
            {
                _attackAngleSlider.SetRange(MinAttackAngle, MaxAttackAngle);
                _attackAngleSlider.Format = "F1";
                _attackAngleSlider.Suffix = "°";
                _attackAngleSlider.Label = "Attack Angle";
                _attackAngleSlider.SetWithoutNotify(_attackAngle);
                _attackAngleSlider.OnValueChanged += OnAttackAngleChanged;
            }

            if (_faceToTargetSlider != null)
            {
                _faceToTargetSlider.SetRange(MinFaceToTarget, MaxFaceToTarget);
                _faceToTargetSlider.Format = "F1";
                _faceToTargetSlider.Suffix = "°";
                _faceToTargetSlider.Label = "Face to Target";
                _faceToTargetSlider.SetWithoutNotify(_faceToTarget);
                _faceToTargetSlider.OnValueChanged += OnFaceToTargetChanged;
            }

            if (_pathSlider != null)
            {
                _pathSlider.SetRange(MinPath, MaxPath);
                _pathSlider.Format = "F1";
                _pathSlider.Suffix = "°";
                _pathSlider.Label = "Path";
                _pathSlider.SetWithoutNotify(_path);
                _pathSlider.OnValueChanged += OnPathChanged;
            }

            if (_dynamicLoftSlider != null)
            {
                _dynamicLoftSlider.SetRange(MinDynamicLoft, MaxDynamicLoft);
                _dynamicLoftSlider.Format = "F1";
                _dynamicLoftSlider.Suffix = "°";
                _dynamicLoftSlider.Label = "Dynamic Loft";
                _dynamicLoftSlider.SetWithoutNotify(_dynamicLoft);
                _dynamicLoftSlider.OnValueChanged += OnDynamicLoftChanged;
            }
        }

        private void SetupButtons()
        {
            if (_driverButton != null)
                _driverButton.onClick.AddListener(OnDriverClicked);

            if (_sevenIronButton != null)
                _sevenIronButton.onClick.AddListener(OnSevenIronClicked);

            if (_wedgeButton != null)
                _wedgeButton.onClick.AddListener(OnWedgeClicked);

            if (_hookButton != null)
                _hookButton.onClick.AddListener(OnHookClicked);

            if (_sliceButton != null)
                _sliceButton.onClick.AddListener(OnSliceClicked);

            if (_fireShotButton != null)
                _fireShotButton.onClick.AddListener(OnFireShotClicked);

            if (_resetBallButton != null)
                _resetBallButton.onClick.AddListener(OnResetBallClicked);

            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void UnsubscribeFromButtons()
        {
            if (_driverButton != null)
                _driverButton.onClick.RemoveListener(OnDriverClicked);

            if (_sevenIronButton != null)
                _sevenIronButton.onClick.RemoveListener(OnSevenIronClicked);

            if (_wedgeButton != null)
                _wedgeButton.onClick.RemoveListener(OnWedgeClicked);

            if (_hookButton != null)
                _hookButton.onClick.RemoveListener(OnHookClicked);

            if (_sliceButton != null)
                _sliceButton.onClick.RemoveListener(OnSliceClicked);

            if (_fireShotButton != null)
                _fireShotButton.onClick.RemoveListener(OnFireShotClicked);

            if (_resetBallButton != null)
                _resetBallButton.onClick.RemoveListener(OnResetBallClicked);

            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        private void UnsubscribeFromSliders()
        {
            if (_ballSpeedSlider != null)
                _ballSpeedSlider.OnValueChanged -= OnBallSpeedChanged;

            if (_launchAngleSlider != null)
                _launchAngleSlider.OnValueChanged -= OnLaunchAngleChanged;

            if (_directionSlider != null)
                _directionSlider.OnValueChanged -= OnDirectionChanged;

            if (_backSpinSlider != null)
                _backSpinSlider.OnValueChanged -= OnBackSpinChanged;

            if (_sideSpinSlider != null)
                _sideSpinSlider.OnValueChanged -= OnSideSpinChanged;

            if (_clubDataToggle != null)
                _clubDataToggle.OnValueChanged -= OnClubDataToggleChanged;

            if (_clubSpeedSlider != null)
                _clubSpeedSlider.OnValueChanged -= OnClubSpeedChanged;

            if (_attackAngleSlider != null)
                _attackAngleSlider.OnValueChanged -= OnAttackAngleChanged;

            if (_faceToTargetSlider != null)
                _faceToTargetSlider.OnValueChanged -= OnFaceToTargetChanged;

            if (_pathSlider != null)
                _pathSlider.OnValueChanged -= OnPathChanged;

            if (_dynamicLoftSlider != null)
                _dynamicLoftSlider.OnValueChanged -= OnDynamicLoftChanged;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Show the panel with animation.
        /// </summary>
        public void Show()
        {
            if (_isVisible) return;

            _targetVisible = true;
            _isAnimating = true;

            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }

            _isVisible = true;
            OnVisibilityChanged?.Invoke(true);
        }

        /// <summary>
        /// Hide the panel with animation.
        /// </summary>
        public void Hide()
        {
            if (!_isVisible) return;

            _targetVisible = false;
            _isAnimating = true;
            _isVisible = false;
            OnVisibilityChanged?.Invoke(false);
        }

        /// <summary>
        /// Toggle panel visibility.
        /// </summary>
        public void Toggle()
        {
            if (_isVisible)
                Hide();
            else
                Show();
        }

        /// <summary>
        /// Apply a preset to the sliders.
        /// </summary>
        public void ApplyPreset(ShotPreset preset)
        {
            _ballSpeed = preset.BallSpeed;
            _launchAngle = preset.LaunchAngle;
            _direction = preset.Direction;
            _backSpin = preset.BackSpin;
            _sideSpin = preset.SideSpin;

            UpdateSliderValues();
        }

        /// <summary>
        /// Create GC2ShotData from current values.
        /// </summary>
        public GC2ShotData CreateShotData()
        {
            var shotData = new GC2ShotData
            {
                BallSpeed = _ballSpeed,
                LaunchAngle = _launchAngle,
                Direction = _direction,
                TotalSpin = Mathf.Sqrt(_backSpin * _backSpin + _sideSpin * _sideSpin),
                BackSpin = _backSpin,
                SideSpin = _sideSpin,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                HasClubData = _includeClubData
            };

            if (_includeClubData)
            {
                shotData.ClubSpeed = _clubSpeed;
                shotData.AttackAngle = _attackAngle;
                shotData.FaceToTarget = _faceToTarget;
                shotData.Path = _path;
                shotData.DynamicLoft = _dynamicLoft;
            }

            return shotData;
        }

        /// <summary>
        /// Fire the test shot by calling ShotProcessor.
        /// </summary>
        public void FireShot()
        {
            var shotData = CreateShotData();

            // Route through GameManager to handle both local visualization and GSPro relay
            if (GameManager.Instance != null)
            {
                // Use environmental conditions from SettingsManager
                var settings = SettingsManager.Instance;
                if (settings != null && GameManager.Instance.ShotProcessor != null)
                {
                    GameManager.Instance.ShotProcessor.SetEnvironmentalConditions(
                        settings.TemperatureF,
                        settings.ElevationFt,
                        settings.HumidityPct,
                        settings.WindEnabled ? settings.WindSpeedMph : 0f,
                        settings.WindDirectionDeg
                    );
                }

                // ProcessShot handles both local visualization AND GSPro relay
                GameManager.Instance.ProcessShot(shotData);

                var modeText = GameManager.Instance.CurrentMode == AppMode.GSPro ? " (GSPro)" : "";
                UpdateStatus($"Shot fired: {_ballSpeed:F0} mph{modeText}");
                Debug.Log($"TestShotPanel: Fired test shot - {_ballSpeed:F1} mph, {_launchAngle:F1}° launch, {_backSpin:F0} rpm backspin, Mode: {GameManager.Instance.CurrentMode}");
            }
            else
            {
                UpdateStatus("GameManager not available");
                Debug.LogWarning("TestShotPanel: GameManager not found");
            }

            OnTestShotFired?.Invoke(shotData);
        }

        /// <summary>
        /// Reset the ball to its starting position.
        /// </summary>
        public void ResetBall()
        {
            var ballController = FindAnyObjectByType<Visualization.BallController>();
            if (ballController != null)
            {
                ballController.Reset();
                UpdateStatus("Ball reset");
            }

            var trajectoryRenderer = FindAnyObjectByType<Visualization.TrajectoryRenderer>();
            if (trajectoryRenderer != null)
            {
                trajectoryRenderer.Hide();
            }
        }

        /// <summary>
        /// Update the status text.
        /// </summary>
        public void UpdateStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message;
            }
        }

        #endregion

        #region Internal Methods (for testing)

        /// <summary>
        /// Force set references for testing.
        /// </summary>
        internal void SetReferences(
            CanvasGroup canvasGroup,
            SettingSlider ballSpeedSlider,
            SettingSlider launchAngleSlider,
            SettingSlider directionSlider,
            SettingSlider backSpinSlider,
            SettingSlider sideSpinSlider,
            SettingToggle clubDataToggle,
            Button fireShotButton)
        {
            _canvasGroup = canvasGroup;
            _ballSpeedSlider = ballSpeedSlider;
            _launchAngleSlider = launchAngleSlider;
            _directionSlider = directionSlider;
            _backSpinSlider = backSpinSlider;
            _sideSpinSlider = sideSpinSlider;
            _clubDataToggle = clubDataToggle;
            _fireShotButton = fireShotButton;
        }

        /// <summary>
        /// Set visibility without animation for testing.
        /// </summary>
        internal void SetVisibleImmediate(bool visible)
        {
            _isVisible = visible;
            _targetVisible = visible;
            _isAnimating = false;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.interactable = visible;
                _canvasGroup.blocksRaycasts = visible;
            }
        }

        #endregion

        #region Private Methods

        private void HandleAnimation()
        {
            if (!_isAnimating) return;

            float targetAlpha = _targetVisible ? 1f : 0f;
            float currentAlpha = _canvasGroup != null ? _canvasGroup.alpha : 0f;

            float newAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime / AnimationDuration);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = newAlpha;
            }

            if (Mathf.Approximately(newAlpha, targetAlpha))
            {
                _isAnimating = false;

                if (!_targetVisible && _canvasGroup != null)
                {
                    _canvasGroup.interactable = false;
                    _canvasGroup.blocksRaycasts = false;
                }
            }
        }

        private void HandleKeyboardShortcuts()
        {
            // T: Toggle panel
            if (Input.GetKeyDown(KeyCode.T))
            {
                Toggle();
            }

            // Only process other shortcuts when panel is visible
            if (!_isVisible) return;

            // D: Fire Driver
            if (Input.GetKeyDown(KeyCode.D))
            {
                ApplyPreset(Presets.Driver);
                FireShot();
            }

            // I: Fire 7-Iron
            if (Input.GetKeyDown(KeyCode.I))
            {
                ApplyPreset(Presets.SevenIron);
                FireShot();
            }

            // W: Fire Wedge
            if (Input.GetKeyDown(KeyCode.W))
            {
                ApplyPreset(Presets.Wedge);
                FireShot();
            }

            // Space: Fire current settings
            if (Input.GetKeyDown(KeyCode.Space))
            {
                FireShot();
            }
        }

        private void UpdateSliderValues()
        {
            if (_ballSpeedSlider != null)
                _ballSpeedSlider.SetWithoutNotify(_ballSpeed);

            if (_launchAngleSlider != null)
                _launchAngleSlider.SetWithoutNotify(_launchAngle);

            if (_directionSlider != null)
                _directionSlider.SetWithoutNotify(_direction);

            if (_backSpinSlider != null)
                _backSpinSlider.SetWithoutNotify(_backSpin);

            if (_sideSpinSlider != null)
                _sideSpinSlider.SetWithoutNotify(_sideSpin);
        }

        #endregion

        #region Slider Event Handlers

        private void OnBallSpeedChanged(float value)
        {
            _ballSpeed = value;
        }

        private void OnLaunchAngleChanged(float value)
        {
            _launchAngle = value;
        }

        private void OnDirectionChanged(float value)
        {
            _direction = value;
        }

        private void OnBackSpinChanged(float value)
        {
            _backSpin = value;
        }

        private void OnSideSpinChanged(float value)
        {
            _sideSpin = value;
        }

        private void OnClubDataToggleChanged(bool value)
        {
            _includeClubData = value;
            if (_clubDataSection != null)
            {
                _clubDataSection.SetActive(value);
            }
        }

        private void OnClubSpeedChanged(float value)
        {
            _clubSpeed = value;
        }

        private void OnAttackAngleChanged(float value)
        {
            _attackAngle = value;
        }

        private void OnFaceToTargetChanged(float value)
        {
            _faceToTarget = value;
        }

        private void OnPathChanged(float value)
        {
            _path = value;
        }

        private void OnDynamicLoftChanged(float value)
        {
            _dynamicLoft = value;
        }

        #endregion

        #region Button Event Handlers

        private void OnDriverClicked()
        {
            ApplyPreset(Presets.Driver);
        }

        private void OnSevenIronClicked()
        {
            ApplyPreset(Presets.SevenIron);
        }

        private void OnWedgeClicked()
        {
            ApplyPreset(Presets.Wedge);
        }

        private void OnHookClicked()
        {
            ApplyPreset(Presets.Hook);
        }

        private void OnSliceClicked()
        {
            ApplyPreset(Presets.Slice);
        }

        private void OnFireShotClicked()
        {
            FireShot();
        }

        private void OnResetBallClicked()
        {
            ResetBall();
        }

        private void OnCloseClicked()
        {
            Hide();
        }

        #endregion
    }
}
