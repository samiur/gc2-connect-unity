// ABOUTME: Service that manages persistent user settings with PlayerPrefs storage.
// ABOUTME: Provides settings categories for graphics, units, environment, connection, and audio.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenRange.Core
{
    /// <summary>
    /// Manages persistent application settings using PlayerPrefs.
    /// Singleton pattern ensures consistent settings access across the application.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        private const string PrefsPrefix = "OpenRange_";

        public static SettingsManager Instance { get; private set; }

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLogging = false;

        private AppSettings _settings;
        private bool _isInitialized;

        /// <summary>Current application settings.</summary>
        public AppSettings Settings => _settings;

        /// <summary>Whether settings have been loaded.</summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>Fired when any setting changes.</summary>
        public event Action OnSettingsChanged;

        #region Graphics Settings

        /// <summary>Quality tier setting (Low/Medium/High/Auto).</summary>
        public QualityTier QualityTier
        {
            get => _settings.QualityTier;
            set => SetSetting(ref _settings.QualityTier, value, "QualityTier");
        }

        /// <summary>Target frame rate (30/60/120).</summary>
        public int TargetFrameRate
        {
            get => _settings.TargetFrameRate;
            set => SetSetting(ref _settings.TargetFrameRate, ClampFrameRate(value), "TargetFrameRate");
        }

        #endregion

        #region Unit Settings

        /// <summary>Distance display unit (Yards/Meters).</summary>
        public DistanceUnit DistanceUnit
        {
            get => _settings.DistanceUnit;
            set => SetSetting(ref _settings.DistanceUnit, value, "DistanceUnit");
        }

        /// <summary>Speed display unit (MPH/KPH).</summary>
        public SpeedUnit SpeedUnit
        {
            get => _settings.SpeedUnit;
            set => SetSetting(ref _settings.SpeedUnit, value, "SpeedUnit");
        }

        /// <summary>Temperature display unit (Fahrenheit/Celsius).</summary>
        public TemperatureUnit TemperatureUnit
        {
            get => _settings.TemperatureUnit;
            set => SetSetting(ref _settings.TemperatureUnit, value, "TemperatureUnit");
        }

        #endregion

        #region Environment Settings

        /// <summary>Temperature in Fahrenheit (40-100).</summary>
        public float TemperatureF
        {
            get => _settings.TemperatureF;
            set => SetSetting(ref _settings.TemperatureF, Mathf.Clamp(value, 40f, 100f), "TemperatureF");
        }

        /// <summary>Elevation in feet above sea level (0-8000).</summary>
        public float ElevationFt
        {
            get => _settings.ElevationFt;
            set => SetSetting(ref _settings.ElevationFt, Mathf.Clamp(value, 0f, 8000f), "ElevationFt");
        }

        /// <summary>Relative humidity percentage (0-100).</summary>
        public float HumidityPct
        {
            get => _settings.HumidityPct;
            set => SetSetting(ref _settings.HumidityPct, Mathf.Clamp(value, 0f, 100f), "HumidityPct");
        }

        /// <summary>Whether wind simulation is enabled.</summary>
        public bool WindEnabled
        {
            get => _settings.WindEnabled;
            set => SetSetting(ref _settings.WindEnabled, value, "WindEnabled");
        }

        /// <summary>Wind speed in mph (0-30).</summary>
        public float WindSpeedMph
        {
            get => _settings.WindSpeedMph;
            set => SetSetting(ref _settings.WindSpeedMph, Mathf.Clamp(value, 0f, 30f), "WindSpeedMph");
        }

        /// <summary>Wind direction in degrees (0-360, 0 = headwind).</summary>
        public float WindDirectionDeg
        {
            get => _settings.WindDirectionDeg;
            set => SetSetting(ref _settings.WindDirectionDeg, Mathf.Repeat(value, 360f), "WindDirectionDeg");
        }

        #endregion

        #region Connection Settings

        /// <summary>Whether to auto-connect to GC2 on startup.</summary>
        public bool AutoConnect
        {
            get => _settings.AutoConnect;
            set => SetSetting(ref _settings.AutoConnect, value, "AutoConnect");
        }

        /// <summary>GSPro host address.</summary>
        public string GSProHost
        {
            get => _settings.GSProHost;
            set => SetSetting(ref _settings.GSProHost, value ?? "127.0.0.1", "GSProHost");
        }

        /// <summary>GSPro port number.</summary>
        public int GSProPort
        {
            get => _settings.GSProPort;
            set => SetSetting(ref _settings.GSProPort, Mathf.Clamp(value, 1, 65535), "GSProPort");
        }

        #endregion

        #region Audio Settings

        /// <summary>Master volume (0-1).</summary>
        public float MasterVolume
        {
            get => _settings.MasterVolume;
            set => SetSetting(ref _settings.MasterVolume, Mathf.Clamp01(value), "MasterVolume");
        }

        /// <summary>Effects volume (0-1).</summary>
        public float EffectsVolume
        {
            get => _settings.EffectsVolume;
            set => SetSetting(ref _settings.EffectsVolume, Mathf.Clamp01(value), "EffectsVolume");
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadSettings();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _isInitialized)
            {
                SaveSettings();
            }
        }

        private void OnApplicationQuit()
        {
            if (_isInitialized)
            {
                SaveSettings();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Load settings from PlayerPrefs.
        /// </summary>
        public void LoadSettings()
        {
            _settings = new AppSettings();

            // Graphics
            _settings.QualityTier = LoadEnum("QualityTier", AppSettings.DefaultQualityTier);
            _settings.TargetFrameRate = PlayerPrefs.GetInt(GetKey("TargetFrameRate"), AppSettings.DefaultTargetFrameRate);

            // Units
            _settings.DistanceUnit = LoadEnum("DistanceUnit", AppSettings.DefaultDistanceUnit);
            _settings.SpeedUnit = LoadEnum("SpeedUnit", AppSettings.DefaultSpeedUnit);
            _settings.TemperatureUnit = LoadEnum("TemperatureUnit", AppSettings.DefaultTemperatureUnit);

            // Environment
            _settings.TemperatureF = PlayerPrefs.GetFloat(GetKey("TemperatureF"), AppSettings.DefaultTemperatureF);
            _settings.ElevationFt = PlayerPrefs.GetFloat(GetKey("ElevationFt"), AppSettings.DefaultElevationFt);
            _settings.HumidityPct = PlayerPrefs.GetFloat(GetKey("HumidityPct"), AppSettings.DefaultHumidityPct);
            _settings.WindEnabled = PlayerPrefs.GetInt(GetKey("WindEnabled"), AppSettings.DefaultWindEnabled ? 1 : 0) == 1;
            _settings.WindSpeedMph = PlayerPrefs.GetFloat(GetKey("WindSpeedMph"), AppSettings.DefaultWindSpeedMph);
            _settings.WindDirectionDeg = PlayerPrefs.GetFloat(GetKey("WindDirectionDeg"), AppSettings.DefaultWindDirectionDeg);

            // Connection
            _settings.AutoConnect = PlayerPrefs.GetInt(GetKey("AutoConnect"), AppSettings.DefaultAutoConnect ? 1 : 0) == 1;
            _settings.GSProHost = PlayerPrefs.GetString(GetKey("GSProHost"), AppSettings.DefaultGSProHost);
            _settings.GSProPort = PlayerPrefs.GetInt(GetKey("GSProPort"), AppSettings.DefaultGSProPort);

            // Audio
            _settings.MasterVolume = PlayerPrefs.GetFloat(GetKey("MasterVolume"), AppSettings.DefaultMasterVolume);
            _settings.EffectsVolume = PlayerPrefs.GetFloat(GetKey("EffectsVolume"), AppSettings.DefaultEffectsVolume);

            _isInitialized = true;

            if (_enableDebugLogging)
            {
                Debug.Log("SettingsManager: Settings loaded from PlayerPrefs");
            }
        }

        /// <summary>
        /// Save all settings to PlayerPrefs.
        /// </summary>
        public void SaveSettings()
        {
            if (!_isInitialized)
            {
                return;
            }

            // Graphics
            SaveEnum("QualityTier", _settings.QualityTier);
            PlayerPrefs.SetInt(GetKey("TargetFrameRate"), _settings.TargetFrameRate);

            // Units
            SaveEnum("DistanceUnit", _settings.DistanceUnit);
            SaveEnum("SpeedUnit", _settings.SpeedUnit);
            SaveEnum("TemperatureUnit", _settings.TemperatureUnit);

            // Environment
            PlayerPrefs.SetFloat(GetKey("TemperatureF"), _settings.TemperatureF);
            PlayerPrefs.SetFloat(GetKey("ElevationFt"), _settings.ElevationFt);
            PlayerPrefs.SetFloat(GetKey("HumidityPct"), _settings.HumidityPct);
            PlayerPrefs.SetInt(GetKey("WindEnabled"), _settings.WindEnabled ? 1 : 0);
            PlayerPrefs.SetFloat(GetKey("WindSpeedMph"), _settings.WindSpeedMph);
            PlayerPrefs.SetFloat(GetKey("WindDirectionDeg"), _settings.WindDirectionDeg);

            // Connection
            PlayerPrefs.SetInt(GetKey("AutoConnect"), _settings.AutoConnect ? 1 : 0);
            PlayerPrefs.SetString(GetKey("GSProHost"), _settings.GSProHost);
            PlayerPrefs.SetInt(GetKey("GSProPort"), _settings.GSProPort);

            // Audio
            PlayerPrefs.SetFloat(GetKey("MasterVolume"), _settings.MasterVolume);
            PlayerPrefs.SetFloat(GetKey("EffectsVolume"), _settings.EffectsVolume);

            PlayerPrefs.Save();

            if (_enableDebugLogging)
            {
                Debug.Log("SettingsManager: Settings saved to PlayerPrefs");
            }
        }

        /// <summary>
        /// Reset all settings to default values.
        /// </summary>
        public void ResetToDefaults()
        {
            // Graphics
            _settings.QualityTier = AppSettings.DefaultQualityTier;
            _settings.TargetFrameRate = AppSettings.DefaultTargetFrameRate;

            // Units
            _settings.DistanceUnit = AppSettings.DefaultDistanceUnit;
            _settings.SpeedUnit = AppSettings.DefaultSpeedUnit;
            _settings.TemperatureUnit = AppSettings.DefaultTemperatureUnit;

            // Environment
            _settings.TemperatureF = AppSettings.DefaultTemperatureF;
            _settings.ElevationFt = AppSettings.DefaultElevationFt;
            _settings.HumidityPct = AppSettings.DefaultHumidityPct;
            _settings.WindEnabled = AppSettings.DefaultWindEnabled;
            _settings.WindSpeedMph = AppSettings.DefaultWindSpeedMph;
            _settings.WindDirectionDeg = AppSettings.DefaultWindDirectionDeg;

            // Connection
            _settings.AutoConnect = AppSettings.DefaultAutoConnect;
            _settings.GSProHost = AppSettings.DefaultGSProHost;
            _settings.GSProPort = AppSettings.DefaultGSProPort;

            // Audio
            _settings.MasterVolume = AppSettings.DefaultMasterVolume;
            _settings.EffectsVolume = AppSettings.DefaultEffectsVolume;

            SaveSettings();

            if (_enableDebugLogging)
            {
                Debug.Log("SettingsManager: Settings reset to defaults");
            }

            OnSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Clear all persisted settings from PlayerPrefs.
        /// </summary>
        public void ClearPersistedSettings()
        {
            string[] keys = {
                "QualityTier", "TargetFrameRate",
                "DistanceUnit", "SpeedUnit", "TemperatureUnit",
                "TemperatureF", "ElevationFt", "HumidityPct",
                "WindEnabled", "WindSpeedMph", "WindDirectionDeg",
                "AutoConnect", "GSProHost", "GSProPort",
                "MasterVolume", "EffectsVolume"
            };

            foreach (var key in keys)
            {
                PlayerPrefs.DeleteKey(GetKey(key));
            }

            PlayerPrefs.Save();

            if (_enableDebugLogging)
            {
                Debug.Log("SettingsManager: Persisted settings cleared");
            }
        }

        /// <summary>
        /// Get a copy of the current settings.
        /// </summary>
        /// <returns>A copy of the current AppSettings.</returns>
        public AppSettings GetSettingsCopy()
        {
            return new AppSettings
            {
                QualityTier = _settings.QualityTier,
                TargetFrameRate = _settings.TargetFrameRate,
                DistanceUnit = _settings.DistanceUnit,
                SpeedUnit = _settings.SpeedUnit,
                TemperatureUnit = _settings.TemperatureUnit,
                TemperatureF = _settings.TemperatureF,
                ElevationFt = _settings.ElevationFt,
                HumidityPct = _settings.HumidityPct,
                WindEnabled = _settings.WindEnabled,
                WindSpeedMph = _settings.WindSpeedMph,
                WindDirectionDeg = _settings.WindDirectionDeg,
                AutoConnect = _settings.AutoConnect,
                GSProHost = _settings.GSProHost,
                GSProPort = _settings.GSProPort,
                MasterVolume = _settings.MasterVolume,
                EffectsVolume = _settings.EffectsVolume
            };
        }

        #endregion

        #region Private Helpers

        private string GetKey(string settingName)
        {
            return PrefsPrefix + settingName;
        }

        private T LoadEnum<T>(string key, T defaultValue) where T : Enum
        {
            int value = PlayerPrefs.GetInt(GetKey(key), Convert.ToInt32(defaultValue));
            return (T)Enum.ToObject(typeof(T), value);
        }

        private void SaveEnum<T>(string key, T value) where T : Enum
        {
            PlayerPrefs.SetInt(GetKey(key), Convert.ToInt32(value));
        }

        private void SetSetting<T>(ref T field, T value, string settingName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;

            if (_isInitialized)
            {
                if (_enableDebugLogging)
                {
                    Debug.Log($"SettingsManager: {settingName} changed to {value}");
                }

                OnSettingsChanged?.Invoke();
            }
        }

        private void SetSetting(ref string field, string value, string settingName)
        {
            if (string.Equals(field, value, StringComparison.Ordinal))
            {
                return;
            }

            field = value;

            if (_isInitialized)
            {
                if (_enableDebugLogging)
                {
                    Debug.Log($"SettingsManager: {settingName} changed to {value}");
                }

                OnSettingsChanged?.Invoke();
            }
        }

        private int ClampFrameRate(int value)
        {
            if (value <= 30) return 30;
            if (value <= 60) return 60;
            return 120;
        }

        #endregion
    }

    /// <summary>
    /// Container for all application settings.
    /// </summary>
    [Serializable]
    public class AppSettings
    {
        // Defaults
        public const QualityTier DefaultQualityTier = QualityTier.Auto;
        public const int DefaultTargetFrameRate = 60;
        public const DistanceUnit DefaultDistanceUnit = DistanceUnit.Yards;
        public const SpeedUnit DefaultSpeedUnit = SpeedUnit.MPH;
        public const TemperatureUnit DefaultTemperatureUnit = TemperatureUnit.Fahrenheit;
        public const float DefaultTemperatureF = 70f;
        public const float DefaultElevationFt = 0f;
        public const float DefaultHumidityPct = 50f;
        public const bool DefaultWindEnabled = false;
        public const float DefaultWindSpeedMph = 0f;
        public const float DefaultWindDirectionDeg = 0f;
        public const bool DefaultAutoConnect = true;
        public const string DefaultGSProHost = "127.0.0.1";
        public const int DefaultGSProPort = 921;
        public const float DefaultMasterVolume = 1f;
        public const float DefaultEffectsVolume = 1f;

        // Graphics
        public QualityTier QualityTier = DefaultQualityTier;
        public int TargetFrameRate = DefaultTargetFrameRate;

        // Units
        public DistanceUnit DistanceUnit = DefaultDistanceUnit;
        public SpeedUnit SpeedUnit = DefaultSpeedUnit;
        public TemperatureUnit TemperatureUnit = DefaultTemperatureUnit;

        // Environment
        public float TemperatureF = DefaultTemperatureF;
        public float ElevationFt = DefaultElevationFt;
        public float HumidityPct = DefaultHumidityPct;
        public bool WindEnabled = DefaultWindEnabled;
        public float WindSpeedMph = DefaultWindSpeedMph;
        public float WindDirectionDeg = DefaultWindDirectionDeg;

        // Connection
        public bool AutoConnect = DefaultAutoConnect;
        public string GSProHost = DefaultGSProHost;
        public int GSProPort = DefaultGSProPort;

        // Audio
        public float MasterVolume = DefaultMasterVolume;
        public float EffectsVolume = DefaultEffectsVolume;
    }

    /// <summary>
    /// Quality tier for graphics settings.
    /// </summary>
    public enum QualityTier
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Auto = 3
    }

    /// <summary>
    /// Distance display unit.
    /// </summary>
    public enum DistanceUnit
    {
        Yards = 0,
        Meters = 1
    }

    /// <summary>
    /// Speed display unit.
    /// </summary>
    public enum SpeedUnit
    {
        MPH = 0,
        KPH = 1
    }

    /// <summary>
    /// Temperature display unit.
    /// </summary>
    public enum TemperatureUnit
    {
        Fahrenheit = 0,
        Celsius = 1
    }
}
