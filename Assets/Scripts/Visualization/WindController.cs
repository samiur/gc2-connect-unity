// ABOUTME: Singleton controller for managing global wind parameters used by grass and other shaders.
// ABOUTME: Integrates with SettingsManager for wind settings and QualityManager for quality tier adjustments.

using System;
using UnityEngine;
using OpenRange.Core;
using CoreQualityTier = OpenRange.Core.QualityTier;

namespace OpenRange.Visualization
{
    /// <summary>
    /// Controls global wind parameters for shader-based wind animation.
    /// Updates global shader properties that grass and foliage shaders read.
    /// </summary>
    public class WindController : MonoBehaviour
    {
        // Shader property IDs for performance
        private static readonly int GlobalWindDirectionId = Shader.PropertyToID("_GlobalWindDirection");
        private static readonly int GlobalWindSpeedId = Shader.PropertyToID("_GlobalWindSpeed");
        private static readonly int GlobalWindStrengthId = Shader.PropertyToID("_GlobalWindStrength");
        private static readonly int GlobalWindTimeId = Shader.PropertyToID("_GlobalWindTime");

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static WindController Instance { get; private set; }

        [Header("Wind Settings")]
        [SerializeField] private float _windSpeed = 1.0f;
        [SerializeField] private float _windStrength = 1.0f;
        [SerializeField] [Range(0, 360)] private float _windDirectionDegrees = 0f;
        [SerializeField] private float _turbulence = 0.5f;

        [Header("Gust Settings")]
        [SerializeField] private bool _gustingEnabled = true;
        [SerializeField] private float _gustStrength = 0.3f;
        [SerializeField] private float _gustFrequency = 1.0f;

        [Header("Quality Settings")]
        [SerializeField] private bool _windEnabled = true;

        private float _windTime;
        private Vector3 _windDirection = Vector3.forward;
        private CoreQualityTier _currentQualityTier = CoreQualityTier.High;
        private bool _isInitialized;
        private bool _wasWindEnabled;

        /// <summary>
        /// Whether wind animation is currently enabled.
        /// </summary>
        public bool WindEnabled
        {
            get => _windEnabled;
            set
            {
                if (_windEnabled != value)
                {
                    _windEnabled = value;
                    UpdateGlobalShaderProperties();
                    OnWindChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Wind speed multiplier (affects animation speed).
        /// </summary>
        public float WindSpeed
        {
            get => _windSpeed;
            set
            {
                float clamped = Mathf.Max(0, value);
                if (!Mathf.Approximately(_windSpeed, clamped))
                {
                    _windSpeed = clamped;
                    UpdateGlobalShaderProperties();
                    OnWindChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Wind strength multiplier (affects displacement amount).
        /// </summary>
        public float WindStrength
        {
            get => _windStrength;
            set
            {
                float clamped = Mathf.Clamp(value, 0, 2);
                if (!Mathf.Approximately(_windStrength, clamped))
                {
                    _windStrength = clamped;
                    UpdateGlobalShaderProperties();
                    OnWindChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Wind direction in degrees (0 = North/+Z, 90 = East/+X, 180 = South/-Z, 270 = West/-X).
        /// </summary>
        public float WindDirectionDegrees
        {
            get => _windDirectionDegrees;
            set
            {
                float normalized = Mathf.Repeat(value, 360f);
                if (!Mathf.Approximately(_windDirectionDegrees, normalized))
                {
                    _windDirectionDegrees = normalized;
                    UpdateWindDirection();
                    UpdateGlobalShaderProperties();
                    OnWindChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Wind direction as a normalized Vector3 (XZ plane).
        /// </summary>
        public Vector3 WindDirection => _windDirection;

        /// <summary>
        /// Wind turbulence amount (0-2).
        /// </summary>
        public float Turbulence
        {
            get => _turbulence;
            set
            {
                float clamped = Mathf.Clamp(value, 0, 2);
                if (!Mathf.Approximately(_turbulence, clamped))
                {
                    _turbulence = clamped;
                    OnWindChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Whether wind gusts are enabled.
        /// </summary>
        public bool GustingEnabled
        {
            get => _gustingEnabled;
            set
            {
                if (_gustingEnabled != value)
                {
                    _gustingEnabled = value;
                    OnWindChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Gust strength multiplier.
        /// </summary>
        public float GustStrength
        {
            get => _gustStrength;
            set
            {
                float clamped = Mathf.Clamp(value, 0, 1);
                if (!Mathf.Approximately(_gustStrength, clamped))
                {
                    _gustStrength = clamped;
                    OnWindChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Gust frequency (gusts per second).
        /// </summary>
        public float GustFrequency
        {
            get => _gustFrequency;
            set
            {
                float clamped = Mathf.Clamp(value, 0, 5);
                if (!Mathf.Approximately(_gustFrequency, clamped))
                {
                    _gustFrequency = clamped;
                    OnWindChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Current quality tier affecting wind.
        /// </summary>
        public CoreQualityTier CurrentQualityTier => _currentQualityTier;

        /// <summary>
        /// Whether the controller is initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Fired when any wind parameter changes.
        /// </summary>
        public event Action OnWindChanged;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Initialize();
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            // Update wind time for shader animation
            if (_windEnabled && IsWindEnabledForQuality())
            {
                _windTime += Time.deltaTime;
                Shader.SetGlobalFloat(GlobalWindTimeId, _windTime);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                UnsubscribeFromManagers();

                // Reset global shader properties
                Shader.SetGlobalVector(GlobalWindDirectionId, new Vector4(0, 0, 1, 0));
                Shader.SetGlobalFloat(GlobalWindSpeedId, 0);
                Shader.SetGlobalFloat(GlobalWindStrengthId, 0);
                Shader.SetGlobalFloat(GlobalWindTimeId, 0);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the wind controller.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            UpdateWindDirection();

            // Subscribe to SettingsManager
            if (SettingsManager.Instance != null)
            {
                ApplySettingsManagerValues();
                SettingsManager.Instance.OnSettingsChanged += HandleSettingsChanged;
            }

            // Subscribe to QualityManager
            if (QualityManager.Instance != null)
            {
                _currentQualityTier = QualityManager.Instance.EffectiveTier;
                QualityManager.Instance.OnQualityTierChanged += HandleQualityTierChanged;
            }

            _wasWindEnabled = _windEnabled;
            UpdateGlobalShaderProperties();

            _isInitialized = true;
        }

        /// <summary>
        /// Forces an immediate update of all global shader properties.
        /// </summary>
        public void ForceUpdate()
        {
            UpdateWindDirection();
            UpdateGlobalShaderProperties();
        }

        /// <summary>
        /// Gets the effective wind strength accounting for quality tier.
        /// </summary>
        /// <returns>The effective wind strength (0 if disabled).</returns>
        public float GetEffectiveStrength()
        {
            if (!_windEnabled || !IsWindEnabledForQuality())
            {
                return 0f;
            }

            return _windStrength;
        }

        /// <summary>
        /// Gets the effective wind direction as a Vector3.
        /// </summary>
        /// <returns>Normalized wind direction vector.</returns>
        public Vector3 GetWindDirectionVector()
        {
            return _windDirection;
        }

        /// <summary>
        /// Converts wind speed in mph to a shader-friendly multiplier.
        /// </summary>
        /// <param name="windSpeedMph">Wind speed in miles per hour.</param>
        /// <returns>Normalized wind speed multiplier (0-2 range).</returns>
        public static float WindSpeedMphToMultiplier(float windSpeedMph)
        {
            // Map 0-30 mph to 0-2 multiplier
            return Mathf.Clamp(windSpeedMph / 15f, 0f, 2f);
        }

        /// <summary>
        /// Converts wind direction in degrees to a normalized Vector3.
        /// </summary>
        /// <param name="degrees">Wind direction in degrees (0 = North/+Z).</param>
        /// <returns>Normalized direction vector on XZ plane.</returns>
        public static Vector3 DegreesToDirection(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(radians), 0, Mathf.Cos(radians)).normalized;
        }

        /// <summary>
        /// Forces initialization of the singleton instance.
        /// Used in EditMode tests where Awake() is not called.
        /// </summary>
        internal void ForceInitializeSingleton()
        {
            Instance = this;
            Initialize();
        }

        #endregion

        #region Private Methods

        private void UpdateWindDirection()
        {
            _windDirection = DegreesToDirection(_windDirectionDegrees);
        }

        private void UpdateGlobalShaderProperties()
        {
            bool effectivelyEnabled = _windEnabled && IsWindEnabledForQuality();

            // Wind direction with enabled flag in W component
            Vector4 windDirWithFlag = new Vector4(
                _windDirection.x,
                _windDirection.y,
                _windDirection.z,
                effectivelyEnabled ? 1f : 0f
            );

            Shader.SetGlobalVector(GlobalWindDirectionId, windDirWithFlag);
            Shader.SetGlobalFloat(GlobalWindSpeedId, effectivelyEnabled ? _windSpeed : 0f);
            Shader.SetGlobalFloat(GlobalWindStrengthId, effectivelyEnabled ? _windStrength : 0f);
        }

        private bool IsWindEnabledForQuality()
        {
            // Disable wind on Low quality
            return _currentQualityTier != CoreQualityTier.Low;
        }

        private void HandleSettingsChanged()
        {
            ApplySettingsManagerValues();
        }

        private void ApplySettingsManagerValues()
        {
            if (SettingsManager.Instance == null)
            {
                return;
            }

            bool settingsWindEnabled = SettingsManager.Instance.WindEnabled;
            float settingsWindSpeed = SettingsManager.Instance.WindSpeedMph;
            float settingsWindDirection = SettingsManager.Instance.WindDirectionDeg;

            // Apply settings
            _windEnabled = settingsWindEnabled;
            _windSpeed = WindSpeedMphToMultiplier(settingsWindSpeed);
            _windStrength = WindSpeedMphToMultiplier(settingsWindSpeed);
            _windDirectionDegrees = settingsWindDirection;

            UpdateWindDirection();
            UpdateGlobalShaderProperties();

            // Fire event if wind state changed
            if (_wasWindEnabled != _windEnabled)
            {
                _wasWindEnabled = _windEnabled;
                OnWindChanged?.Invoke();
            }
        }

        private void HandleQualityTierChanged(CoreQualityTier tier)
        {
            if (_currentQualityTier == tier)
            {
                return;
            }

            _currentQualityTier = tier;
            UpdateGlobalShaderProperties();
            OnWindChanged?.Invoke();
        }

        private void UnsubscribeFromManagers()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.OnSettingsChanged -= HandleSettingsChanged;
            }

            if (QualityManager.Instance != null)
            {
                QualityManager.Instance.OnQualityTierChanged -= HandleQualityTierChanged;
            }
        }

        #endregion
    }
}
