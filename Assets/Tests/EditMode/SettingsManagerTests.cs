// ABOUTME: Unit tests for the SettingsManager service.
// ABOUTME: Tests settings persistence, default values, clamping, events, and reset functionality.

using System;
using NUnit.Framework;
using UnityEngine;
using OpenRange.Core;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class SettingsManagerTests
    {
        private GameObject _testObject;
        private SettingsManager _settingsManager;

        [SetUp]
        public void SetUp()
        {
            // Clear any existing singleton
            if (SettingsManager.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(SettingsManager.Instance.gameObject);
            }

            // Clear PlayerPrefs for test isolation
            ClearTestPlayerPrefs();

            _testObject = new GameObject("TestSettingsManager");
            _settingsManager = _testObject.AddComponent<SettingsManager>();

            // Manually call LoadSettings since Awake sets up the singleton
            _settingsManager.LoadSettings();
        }

        [TearDown]
        public void TearDown()
        {
            ClearTestPlayerPrefs();

            if (_testObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testObject);
            }
        }

        private void ClearTestPlayerPrefs()
        {
            string[] keys = {
                "OpenRange_QualityTier", "OpenRange_TargetFrameRate",
                "OpenRange_DistanceUnit", "OpenRange_SpeedUnit", "OpenRange_TemperatureUnit",
                "OpenRange_TemperatureF", "OpenRange_ElevationFt", "OpenRange_HumidityPct",
                "OpenRange_WindEnabled", "OpenRange_WindSpeedMph", "OpenRange_WindDirectionDeg",
                "OpenRange_AutoConnect", "OpenRange_GSProHost", "OpenRange_GSProPort",
                "OpenRange_MasterVolume", "OpenRange_EffectsVolume"
            };

            foreach (var key in keys)
            {
                PlayerPrefs.DeleteKey(key);
            }

            PlayerPrefs.Save();
        }

        #region Initialization Tests

        [Test]
        public void LoadSettings_SetsIsInitializedToTrue()
        {
            // Assert
            Assert.IsTrue(_settingsManager.IsInitialized);
        }

        [Test]
        public void LoadSettings_CreatesSettingsObject()
        {
            // Assert
            Assert.IsNotNull(_settingsManager.Settings);
        }

        #endregion

        #region Default Values Tests

        [Test]
        public void DefaultValues_QualityTier_IsAuto()
        {
            Assert.AreEqual(QualityTier.Auto, _settingsManager.QualityTier);
        }

        [Test]
        public void DefaultValues_TargetFrameRate_Is60()
        {
            Assert.AreEqual(60, _settingsManager.TargetFrameRate);
        }

        [Test]
        public void DefaultValues_DistanceUnit_IsYards()
        {
            Assert.AreEqual(DistanceUnit.Yards, _settingsManager.DistanceUnit);
        }

        [Test]
        public void DefaultValues_SpeedUnit_IsMPH()
        {
            Assert.AreEqual(SpeedUnit.MPH, _settingsManager.SpeedUnit);
        }

        [Test]
        public void DefaultValues_TemperatureUnit_IsFahrenheit()
        {
            Assert.AreEqual(TemperatureUnit.Fahrenheit, _settingsManager.TemperatureUnit);
        }

        [Test]
        public void DefaultValues_TemperatureF_Is70()
        {
            Assert.AreEqual(70f, _settingsManager.TemperatureF, 0.01f);
        }

        [Test]
        public void DefaultValues_ElevationFt_Is0()
        {
            Assert.AreEqual(0f, _settingsManager.ElevationFt, 0.01f);
        }

        [Test]
        public void DefaultValues_HumidityPct_Is50()
        {
            Assert.AreEqual(50f, _settingsManager.HumidityPct, 0.01f);
        }

        [Test]
        public void DefaultValues_WindEnabled_IsFalse()
        {
            Assert.IsFalse(_settingsManager.WindEnabled);
        }

        [Test]
        public void DefaultValues_WindSpeedMph_Is0()
        {
            Assert.AreEqual(0f, _settingsManager.WindSpeedMph, 0.01f);
        }

        [Test]
        public void DefaultValues_WindDirectionDeg_Is0()
        {
            Assert.AreEqual(0f, _settingsManager.WindDirectionDeg, 0.01f);
        }

        [Test]
        public void DefaultValues_AutoConnect_IsTrue()
        {
            Assert.IsTrue(_settingsManager.AutoConnect);
        }

        [Test]
        public void DefaultValues_GSProHost_Is127001()
        {
            Assert.AreEqual("127.0.0.1", _settingsManager.GSProHost);
        }

        [Test]
        public void DefaultValues_GSProPort_Is921()
        {
            Assert.AreEqual(921, _settingsManager.GSProPort);
        }

        [Test]
        public void DefaultValues_MasterVolume_Is1()
        {
            Assert.AreEqual(1f, _settingsManager.MasterVolume, 0.01f);
        }

        [Test]
        public void DefaultValues_EffectsVolume_Is1()
        {
            Assert.AreEqual(1f, _settingsManager.EffectsVolume, 0.01f);
        }

        #endregion

        #region Settings Persistence Tests

        [Test]
        public void SaveAndLoad_QualityTier_Persists()
        {
            // Arrange
            _settingsManager.QualityTier = QualityTier.High;
            _settingsManager.SaveSettings();

            // Act
            _settingsManager.LoadSettings();

            // Assert
            Assert.AreEqual(QualityTier.High, _settingsManager.QualityTier);
        }

        [Test]
        public void SaveAndLoad_TargetFrameRate_Persists()
        {
            // Arrange
            _settingsManager.TargetFrameRate = 120;
            _settingsManager.SaveSettings();

            // Act
            _settingsManager.LoadSettings();

            // Assert
            Assert.AreEqual(120, _settingsManager.TargetFrameRate);
        }

        [Test]
        public void SaveAndLoad_DistanceUnit_Persists()
        {
            // Arrange
            _settingsManager.DistanceUnit = DistanceUnit.Meters;
            _settingsManager.SaveSettings();

            // Act
            _settingsManager.LoadSettings();

            // Assert
            Assert.AreEqual(DistanceUnit.Meters, _settingsManager.DistanceUnit);
        }

        [Test]
        public void SaveAndLoad_TemperatureF_Persists()
        {
            // Arrange
            _settingsManager.TemperatureF = 85f;
            _settingsManager.SaveSettings();

            // Act
            _settingsManager.LoadSettings();

            // Assert
            Assert.AreEqual(85f, _settingsManager.TemperatureF, 0.01f);
        }

        [Test]
        public void SaveAndLoad_WindEnabled_Persists()
        {
            // Arrange
            _settingsManager.WindEnabled = true;
            _settingsManager.SaveSettings();

            // Act
            _settingsManager.LoadSettings();

            // Assert
            Assert.IsTrue(_settingsManager.WindEnabled);
        }

        [Test]
        public void SaveAndLoad_GSProHost_Persists()
        {
            // Arrange
            _settingsManager.GSProHost = "192.168.1.100";
            _settingsManager.SaveSettings();

            // Act
            _settingsManager.LoadSettings();

            // Assert
            Assert.AreEqual("192.168.1.100", _settingsManager.GSProHost);
        }

        [Test]
        public void SaveAndLoad_MasterVolume_Persists()
        {
            // Arrange
            _settingsManager.MasterVolume = 0.75f;
            _settingsManager.SaveSettings();

            // Act
            _settingsManager.LoadSettings();

            // Assert
            Assert.AreEqual(0.75f, _settingsManager.MasterVolume, 0.01f);
        }

        #endregion

        #region Value Clamping Tests

        [Test]
        public void TemperatureF_ClampsToMinimum()
        {
            // Act
            _settingsManager.TemperatureF = 20f;

            // Assert
            Assert.AreEqual(40f, _settingsManager.TemperatureF, 0.01f);
        }

        [Test]
        public void TemperatureF_ClampsToMaximum()
        {
            // Act
            _settingsManager.TemperatureF = 120f;

            // Assert
            Assert.AreEqual(100f, _settingsManager.TemperatureF, 0.01f);
        }

        [Test]
        public void ElevationFt_ClampsToMinimum()
        {
            // Act
            _settingsManager.ElevationFt = -100f;

            // Assert
            Assert.AreEqual(0f, _settingsManager.ElevationFt, 0.01f);
        }

        [Test]
        public void ElevationFt_ClampsToMaximum()
        {
            // Act
            _settingsManager.ElevationFt = 10000f;

            // Assert
            Assert.AreEqual(8000f, _settingsManager.ElevationFt, 0.01f);
        }

        [Test]
        public void HumidityPct_ClampsToMinimum()
        {
            // Act
            _settingsManager.HumidityPct = -10f;

            // Assert
            Assert.AreEqual(0f, _settingsManager.HumidityPct, 0.01f);
        }

        [Test]
        public void HumidityPct_ClampsToMaximum()
        {
            // Act
            _settingsManager.HumidityPct = 150f;

            // Assert
            Assert.AreEqual(100f, _settingsManager.HumidityPct, 0.01f);
        }

        [Test]
        public void WindSpeedMph_ClampsToMinimum()
        {
            // Act
            _settingsManager.WindSpeedMph = -5f;

            // Assert
            Assert.AreEqual(0f, _settingsManager.WindSpeedMph, 0.01f);
        }

        [Test]
        public void WindSpeedMph_ClampsToMaximum()
        {
            // Act
            _settingsManager.WindSpeedMph = 50f;

            // Assert
            Assert.AreEqual(30f, _settingsManager.WindSpeedMph, 0.01f);
        }

        [Test]
        public void WindDirectionDeg_WrapsAround()
        {
            // Act
            _settingsManager.WindDirectionDeg = 400f;

            // Assert - 400 % 360 = 40
            Assert.AreEqual(40f, _settingsManager.WindDirectionDeg, 0.01f);
        }

        [Test]
        public void WindDirectionDeg_WrapsNegative()
        {
            // Act
            _settingsManager.WindDirectionDeg = -90f;

            // Assert - Mathf.Repeat handles this as 270
            Assert.AreEqual(270f, _settingsManager.WindDirectionDeg, 0.01f);
        }

        [Test]
        public void GSProPort_ClampsToMinimum()
        {
            // Act
            _settingsManager.GSProPort = 0;

            // Assert
            Assert.AreEqual(1, _settingsManager.GSProPort);
        }

        [Test]
        public void GSProPort_ClampsToMaximum()
        {
            // Act
            _settingsManager.GSProPort = 100000;

            // Assert
            Assert.AreEqual(65535, _settingsManager.GSProPort);
        }

        [Test]
        public void MasterVolume_ClampsTo01()
        {
            // Act
            _settingsManager.MasterVolume = 1.5f;

            // Assert
            Assert.AreEqual(1f, _settingsManager.MasterVolume, 0.01f);

            // Act
            _settingsManager.MasterVolume = -0.5f;

            // Assert
            Assert.AreEqual(0f, _settingsManager.MasterVolume, 0.01f);
        }

        [Test]
        public void TargetFrameRate_ClampsTo30()
        {
            // Act
            _settingsManager.TargetFrameRate = 15;

            // Assert
            Assert.AreEqual(30, _settingsManager.TargetFrameRate);
        }

        [Test]
        public void TargetFrameRate_ClampsTo60()
        {
            // Act
            _settingsManager.TargetFrameRate = 45;

            // Assert
            Assert.AreEqual(60, _settingsManager.TargetFrameRate);
        }

        [Test]
        public void TargetFrameRate_ClampsTo120()
        {
            // Act
            _settingsManager.TargetFrameRate = 144;

            // Assert
            Assert.AreEqual(120, _settingsManager.TargetFrameRate);
        }

        [Test]
        public void GSProHost_NullDefaultsTo127001()
        {
            // Act
            _settingsManager.GSProHost = null;

            // Assert
            Assert.AreEqual("127.0.0.1", _settingsManager.GSProHost);
        }

        #endregion

        #region OnSettingsChanged Event Tests

        [Test]
        public void OnSettingsChanged_FiresWhenQualityTierChanges()
        {
            // Arrange
            bool eventFired = false;
            _settingsManager.OnSettingsChanged += () => eventFired = true;

            // Act
            _settingsManager.QualityTier = QualityTier.Low;

            // Assert
            Assert.IsTrue(eventFired);
        }

        [Test]
        public void OnSettingsChanged_FiresWhenTemperatureFChanges()
        {
            // Arrange
            bool eventFired = false;
            _settingsManager.OnSettingsChanged += () => eventFired = true;

            // Act
            _settingsManager.TemperatureF = 85f;

            // Assert
            Assert.IsTrue(eventFired);
        }

        [Test]
        public void OnSettingsChanged_DoesNotFireWhenValueSame()
        {
            // Arrange
            bool eventFired = false;
            _settingsManager.OnSettingsChanged += () => eventFired = true;

            // Act - Set to same value
            _settingsManager.TemperatureF = 70f;

            // Assert
            Assert.IsFalse(eventFired);
        }

        [Test]
        public void OnSettingsChanged_FiresOnResetToDefaults()
        {
            // Arrange
            _settingsManager.TemperatureF = 90f;
            bool eventFired = false;
            _settingsManager.OnSettingsChanged += () => eventFired = true;

            // Act
            _settingsManager.ResetToDefaults();

            // Assert
            Assert.IsTrue(eventFired);
        }

        #endregion

        #region ResetToDefaults Tests

        [Test]
        public void ResetToDefaults_RestoresGraphicsSettings()
        {
            // Arrange
            _settingsManager.QualityTier = QualityTier.Low;
            _settingsManager.TargetFrameRate = 30;

            // Act
            _settingsManager.ResetToDefaults();

            // Assert
            Assert.AreEqual(QualityTier.Auto, _settingsManager.QualityTier);
            Assert.AreEqual(60, _settingsManager.TargetFrameRate);
        }

        [Test]
        public void ResetToDefaults_RestoresUnitSettings()
        {
            // Arrange
            _settingsManager.DistanceUnit = DistanceUnit.Meters;
            _settingsManager.SpeedUnit = SpeedUnit.KPH;
            _settingsManager.TemperatureUnit = TemperatureUnit.Celsius;

            // Act
            _settingsManager.ResetToDefaults();

            // Assert
            Assert.AreEqual(DistanceUnit.Yards, _settingsManager.DistanceUnit);
            Assert.AreEqual(SpeedUnit.MPH, _settingsManager.SpeedUnit);
            Assert.AreEqual(TemperatureUnit.Fahrenheit, _settingsManager.TemperatureUnit);
        }

        [Test]
        public void ResetToDefaults_RestoresEnvironmentSettings()
        {
            // Arrange
            _settingsManager.TemperatureF = 90f;
            _settingsManager.ElevationFt = 5000f;
            _settingsManager.HumidityPct = 80f;
            _settingsManager.WindEnabled = true;
            _settingsManager.WindSpeedMph = 15f;
            _settingsManager.WindDirectionDeg = 180f;

            // Act
            _settingsManager.ResetToDefaults();

            // Assert
            Assert.AreEqual(70f, _settingsManager.TemperatureF, 0.01f);
            Assert.AreEqual(0f, _settingsManager.ElevationFt, 0.01f);
            Assert.AreEqual(50f, _settingsManager.HumidityPct, 0.01f);
            Assert.IsFalse(_settingsManager.WindEnabled);
            Assert.AreEqual(0f, _settingsManager.WindSpeedMph, 0.01f);
            Assert.AreEqual(0f, _settingsManager.WindDirectionDeg, 0.01f);
        }

        [Test]
        public void ResetToDefaults_RestoresConnectionSettings()
        {
            // Arrange
            _settingsManager.AutoConnect = false;
            _settingsManager.GSProHost = "192.168.1.100";
            _settingsManager.GSProPort = 9210;

            // Act
            _settingsManager.ResetToDefaults();

            // Assert
            Assert.IsTrue(_settingsManager.AutoConnect);
            Assert.AreEqual("127.0.0.1", _settingsManager.GSProHost);
            Assert.AreEqual(921, _settingsManager.GSProPort);
        }

        [Test]
        public void ResetToDefaults_RestoresAudioSettings()
        {
            // Arrange
            _settingsManager.MasterVolume = 0.5f;
            _settingsManager.EffectsVolume = 0.3f;

            // Act
            _settingsManager.ResetToDefaults();

            // Assert
            Assert.AreEqual(1f, _settingsManager.MasterVolume, 0.01f);
            Assert.AreEqual(1f, _settingsManager.EffectsVolume, 0.01f);
        }

        [Test]
        public void ResetToDefaults_PersistsDefaultValues()
        {
            // Arrange
            _settingsManager.TemperatureF = 90f;
            _settingsManager.SaveSettings();

            // Act
            _settingsManager.ResetToDefaults();
            _settingsManager.LoadSettings();

            // Assert - Should load defaults since ResetToDefaults saves
            Assert.AreEqual(70f, _settingsManager.TemperatureF, 0.01f);
        }

        #endregion

        #region GetSettingsCopy Tests

        [Test]
        public void GetSettingsCopy_ReturnsCorrectValues()
        {
            // Arrange
            _settingsManager.QualityTier = QualityTier.High;
            _settingsManager.TemperatureF = 85f;
            _settingsManager.GSProHost = "192.168.1.1";

            // Act
            var copy = _settingsManager.GetSettingsCopy();

            // Assert
            Assert.AreEqual(QualityTier.High, copy.QualityTier);
            Assert.AreEqual(85f, copy.TemperatureF, 0.01f);
            Assert.AreEqual("192.168.1.1", copy.GSProHost);
        }

        [Test]
        public void GetSettingsCopy_IsIndependentCopy()
        {
            // Arrange
            var copy = _settingsManager.GetSettingsCopy();

            // Act - Modify the copy
            copy.TemperatureF = 100f;

            // Assert - Original should be unchanged
            Assert.AreEqual(70f, _settingsManager.TemperatureF, 0.01f);
        }

        #endregion

        #region ClearPersistedSettings Tests

        [Test]
        public void ClearPersistedSettings_RemovesFromPlayerPrefs()
        {
            // Arrange
            _settingsManager.TemperatureF = 90f;
            _settingsManager.SaveSettings();
            Assert.IsTrue(PlayerPrefs.HasKey("OpenRange_TemperatureF"));

            // Act
            _settingsManager.ClearPersistedSettings();

            // Assert
            Assert.IsFalse(PlayerPrefs.HasKey("OpenRange_TemperatureF"));
        }

        #endregion

        #region AppSettings Tests

        [Test]
        public void AppSettings_HasCorrectDefaultConstants()
        {
            Assert.AreEqual(QualityTier.Auto, AppSettings.DefaultQualityTier);
            Assert.AreEqual(60, AppSettings.DefaultTargetFrameRate);
            Assert.AreEqual(DistanceUnit.Yards, AppSettings.DefaultDistanceUnit);
            Assert.AreEqual(SpeedUnit.MPH, AppSettings.DefaultSpeedUnit);
            Assert.AreEqual(TemperatureUnit.Fahrenheit, AppSettings.DefaultTemperatureUnit);
            Assert.AreEqual(70f, AppSettings.DefaultTemperatureF, 0.01f);
            Assert.AreEqual(0f, AppSettings.DefaultElevationFt, 0.01f);
            Assert.AreEqual(50f, AppSettings.DefaultHumidityPct, 0.01f);
            Assert.IsFalse(AppSettings.DefaultWindEnabled);
            Assert.AreEqual(0f, AppSettings.DefaultWindSpeedMph, 0.01f);
            Assert.AreEqual(0f, AppSettings.DefaultWindDirectionDeg, 0.01f);
            Assert.IsTrue(AppSettings.DefaultAutoConnect);
            Assert.AreEqual("127.0.0.1", AppSettings.DefaultGSProHost);
            Assert.AreEqual(921, AppSettings.DefaultGSProPort);
            Assert.AreEqual(1f, AppSettings.DefaultMasterVolume, 0.01f);
            Assert.AreEqual(1f, AppSettings.DefaultEffectsVolume, 0.01f);
        }

        #endregion

        #region Enum Tests

        [Test]
        public void QualityTier_HasCorrectValues()
        {
            Assert.AreEqual(0, (int)QualityTier.Low);
            Assert.AreEqual(1, (int)QualityTier.Medium);
            Assert.AreEqual(2, (int)QualityTier.High);
            Assert.AreEqual(3, (int)QualityTier.Auto);
        }

        [Test]
        public void DistanceUnit_HasCorrectValues()
        {
            Assert.AreEqual(0, (int)DistanceUnit.Yards);
            Assert.AreEqual(1, (int)DistanceUnit.Meters);
        }

        [Test]
        public void SpeedUnit_HasCorrectValues()
        {
            Assert.AreEqual(0, (int)SpeedUnit.MPH);
            Assert.AreEqual(1, (int)SpeedUnit.KPH);
        }

        [Test]
        public void TemperatureUnit_HasCorrectValues()
        {
            Assert.AreEqual(0, (int)TemperatureUnit.Fahrenheit);
            Assert.AreEqual(1, (int)TemperatureUnit.Celsius);
        }

        #endregion
    }
}
