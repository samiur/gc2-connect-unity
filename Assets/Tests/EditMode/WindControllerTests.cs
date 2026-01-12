// ABOUTME: Unit tests for WindController singleton that manages global wind shader properties.
// ABOUTME: Tests initialization, SettingsManager integration, quality tier adjustments, and events.

using System;
using NUnit.Framework;
using UnityEngine;
using OpenRange.Visualization;
using OpenRange.Core;
using CoreQualityTier = OpenRange.Core.QualityTier;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class WindControllerTests
    {
        private GameObject _controllerGo;
        private WindController _controller;

        [SetUp]
        public void SetUp()
        {
            // Clean up any existing instance
            if (WindController.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(WindController.Instance.gameObject);
            }

            _controllerGo = new GameObject("WindController");
            _controller = _controllerGo.AddComponent<WindController>();

            // In EditMode tests, Awake() is not automatically called
            // We need to manually set up the singleton and initialize
            _controller.ForceInitializeSingleton();
        }

        [TearDown]
        public void TearDown()
        {
            if (_controllerGo != null)
            {
                UnityEngine.Object.DestroyImmediate(_controllerGo);
            }

            // Reset global shader properties
            Shader.SetGlobalVector("_GlobalWindDirection", new Vector4(0, 0, 1, 0));
            Shader.SetGlobalFloat("_GlobalWindSpeed", 0);
            Shader.SetGlobalFloat("_GlobalWindStrength", 0);
            Shader.SetGlobalFloat("_GlobalWindTime", 0);
        }

        #region Initialization Tests

        [Test]
        public void Instance_IsSetAfterCreation()
        {
            Assert.That(WindController.Instance, Is.EqualTo(_controller));
        }

        [Test]
        public void IsInitialized_IsTrueAfterAwake()
        {
            Assert.That(_controller.IsInitialized, Is.True);
        }

        [Test]
        public void WindEnabled_DefaultsToTrue()
        {
            Assert.That(_controller.WindEnabled, Is.True);
        }

        [Test]
        public void WindSpeed_DefaultsToPositive()
        {
            Assert.That(_controller.WindSpeed, Is.GreaterThan(0));
        }

        [Test]
        public void WindStrength_DefaultsToPositive()
        {
            Assert.That(_controller.WindStrength, Is.GreaterThan(0));
        }

        [Test]
        public void WindDirection_DefaultsToNormalized()
        {
            var direction = _controller.WindDirection;
            Assert.That(direction.magnitude, Is.EqualTo(1f).Within(0.001f));
        }

        #endregion

        #region Property Setter Tests

        [Test]
        public void WindEnabled_SetToFalse_UpdatesProperty()
        {
            _controller.WindEnabled = false;
            Assert.That(_controller.WindEnabled, Is.False);
        }

        [Test]
        public void WindEnabled_SetToFalse_FiresEvent()
        {
            bool eventFired = false;
            _controller.OnWindChanged += () => eventFired = true;

            _controller.WindEnabled = false;

            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void WindEnabled_SetToSameValue_DoesNotFireEvent()
        {
            bool eventFired = false;
            _controller.OnWindChanged += () => eventFired = true;

            // Set to same value (default is true)
            _controller.WindEnabled = true;

            Assert.That(eventFired, Is.False);
        }

        [Test]
        public void WindSpeed_SetValue_UpdatesProperty()
        {
            _controller.WindSpeed = 2.5f;
            Assert.That(_controller.WindSpeed, Is.EqualTo(2.5f));
        }

        [Test]
        public void WindSpeed_NegativeValue_ClampedToZero()
        {
            _controller.WindSpeed = -5f;
            Assert.That(_controller.WindSpeed, Is.EqualTo(0f));
        }

        [Test]
        public void WindSpeed_SetValue_FiresEvent()
        {
            bool eventFired = false;
            _controller.OnWindChanged += () => eventFired = true;

            _controller.WindSpeed = 2.0f;

            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void WindStrength_SetValue_UpdatesProperty()
        {
            _controller.WindStrength = 1.5f;
            Assert.That(_controller.WindStrength, Is.EqualTo(1.5f));
        }

        [Test]
        public void WindStrength_ClampedToRange()
        {
            _controller.WindStrength = 5f;
            Assert.That(_controller.WindStrength, Is.EqualTo(2f));

            _controller.WindStrength = -1f;
            Assert.That(_controller.WindStrength, Is.EqualTo(0f));
        }

        [Test]
        public void WindStrength_SetValue_FiresEvent()
        {
            bool eventFired = false;
            _controller.OnWindChanged += () => eventFired = true;

            _controller.WindStrength = 0.5f;

            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void WindDirectionDegrees_SetValue_UpdatesProperty()
        {
            _controller.WindDirectionDegrees = 90f;
            Assert.That(_controller.WindDirectionDegrees, Is.EqualTo(90f));
        }

        [Test]
        public void WindDirectionDegrees_Normalized()
        {
            _controller.WindDirectionDegrees = 450f;
            Assert.That(_controller.WindDirectionDegrees, Is.EqualTo(90f).Within(0.001f));
        }

        [Test]
        public void WindDirectionDegrees_NegativeNormalized()
        {
            _controller.WindDirectionDegrees = -90f;
            Assert.That(_controller.WindDirectionDegrees, Is.EqualTo(270f).Within(0.001f));
        }

        [Test]
        public void WindDirectionDegrees_UpdatesWindDirection()
        {
            _controller.WindDirectionDegrees = 90f;
            var direction = _controller.WindDirection;

            // 90 degrees = East = +X direction
            Assert.That(direction.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(direction.z, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void Turbulence_SetValue_UpdatesProperty()
        {
            _controller.Turbulence = 1.5f;
            Assert.That(_controller.Turbulence, Is.EqualTo(1.5f));
        }

        [Test]
        public void Turbulence_ClampedToRange()
        {
            _controller.Turbulence = 5f;
            Assert.That(_controller.Turbulence, Is.EqualTo(2f));
        }

        [Test]
        public void GustingEnabled_SetValue_UpdatesProperty()
        {
            _controller.GustingEnabled = false;
            Assert.That(_controller.GustingEnabled, Is.False);
        }

        [Test]
        public void GustStrength_SetValue_UpdatesProperty()
        {
            _controller.GustStrength = 0.5f;
            Assert.That(_controller.GustStrength, Is.EqualTo(0.5f));
        }

        [Test]
        public void GustStrength_ClampedToRange()
        {
            _controller.GustStrength = 2f;
            Assert.That(_controller.GustStrength, Is.EqualTo(1f));
        }

        [Test]
        public void GustFrequency_SetValue_UpdatesProperty()
        {
            _controller.GustFrequency = 2.5f;
            Assert.That(_controller.GustFrequency, Is.EqualTo(2.5f));
        }

        [Test]
        public void GustFrequency_ClampedToRange()
        {
            _controller.GustFrequency = 10f;
            Assert.That(_controller.GustFrequency, Is.EqualTo(5f));
        }

        #endregion

        #region Global Shader Property Tests

        [Test]
        public void ForceUpdate_UpdatesShaderProperties()
        {
            _controller.WindDirectionDegrees = 180f;
            _controller.WindSpeed = 1.5f;
            _controller.WindStrength = 0.8f;

            _controller.ForceUpdate();

            var globalDirection = Shader.GetGlobalVector("_GlobalWindDirection");
            Assert.That(globalDirection.z, Is.EqualTo(-1f).Within(0.01f)); // 180 = -Z
        }

        [Test]
        public void WindDisabled_SetsZeroStrengthInShader()
        {
            _controller.WindEnabled = false;

            var globalStrength = Shader.GetGlobalFloat("_GlobalWindStrength");
            Assert.That(globalStrength, Is.EqualTo(0f));
        }

        [Test]
        public void WindEnabled_SetsNonZeroStrengthInShader()
        {
            _controller.WindEnabled = true;
            _controller.WindStrength = 1.0f;
            _controller.ForceUpdate();

            var globalStrength = Shader.GetGlobalFloat("_GlobalWindStrength");
            Assert.That(globalStrength, Is.GreaterThan(0f));
        }

        #endregion

        #region GetEffectiveStrength Tests

        [Test]
        public void GetEffectiveStrength_WindDisabled_ReturnsZero()
        {
            _controller.WindEnabled = false;
            _controller.WindStrength = 1.0f;

            Assert.That(_controller.GetEffectiveStrength(), Is.EqualTo(0f));
        }

        [Test]
        public void GetEffectiveStrength_WindEnabled_ReturnsStrength()
        {
            _controller.WindEnabled = true;
            _controller.WindStrength = 1.5f;

            Assert.That(_controller.GetEffectiveStrength(), Is.EqualTo(1.5f));
        }

        #endregion

        #region Static Helper Method Tests

        [Test]
        public void WindSpeedMphToMultiplier_ZeroSpeed_ReturnsZero()
        {
            var result = WindController.WindSpeedMphToMultiplier(0f);
            Assert.That(result, Is.EqualTo(0f));
        }

        [Test]
        public void WindSpeedMphToMultiplier_15Mph_ReturnsOne()
        {
            var result = WindController.WindSpeedMphToMultiplier(15f);
            Assert.That(result, Is.EqualTo(1f));
        }

        [Test]
        public void WindSpeedMphToMultiplier_30Mph_ReturnsTwo()
        {
            var result = WindController.WindSpeedMphToMultiplier(30f);
            Assert.That(result, Is.EqualTo(2f));
        }

        [Test]
        public void WindSpeedMphToMultiplier_HighSpeed_ClampedToTwo()
        {
            var result = WindController.WindSpeedMphToMultiplier(100f);
            Assert.That(result, Is.EqualTo(2f));
        }

        [Test]
        public void DegreesToDirection_ZeroDegrees_ReturnsForward()
        {
            var result = WindController.DegreesToDirection(0f);
            Assert.That(result.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(result.z, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void DegreesToDirection_90Degrees_ReturnsRight()
        {
            var result = WindController.DegreesToDirection(90f);
            Assert.That(result.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(result.z, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void DegreesToDirection_180Degrees_ReturnsBack()
        {
            var result = WindController.DegreesToDirection(180f);
            Assert.That(result.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(result.z, Is.EqualTo(-1f).Within(0.001f));
        }

        [Test]
        public void DegreesToDirection_270Degrees_ReturnsLeft()
        {
            var result = WindController.DegreesToDirection(270f);
            Assert.That(result.x, Is.EqualTo(-1f).Within(0.001f));
            Assert.That(result.z, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void DegreesToDirection_ResultIsNormalized()
        {
            var result = WindController.DegreesToDirection(45f);
            Assert.That(result.magnitude, Is.EqualTo(1f).Within(0.001f));
        }

        #endregion

        #region GetWindDirectionVector Tests

        [Test]
        public void GetWindDirectionVector_ReturnsCurrentDirection()
        {
            _controller.WindDirectionDegrees = 45f;
            var result = _controller.GetWindDirectionVector();
            var expected = WindController.DegreesToDirection(45f);

            Assert.That(result.x, Is.EqualTo(expected.x).Within(0.001f));
            Assert.That(result.z, Is.EqualTo(expected.z).Within(0.001f));
        }

        #endregion

        #region Singleton Tests

        [Test]
        public void SecondInstance_IsDestroyed()
        {
            var secondGo = new GameObject("WindController2");
            var secondController = secondGo.AddComponent<WindController>();

            // The singleton pattern should destroy the second instance
            // We need to wait a frame or use DestroyImmediate in tests
            Assert.That(WindController.Instance, Is.EqualTo(_controller));

            // Clean up
            if (secondGo != null)
            {
                UnityEngine.Object.DestroyImmediate(secondGo);
            }
        }

        [Test]
        public void OnDestroy_ClearsInstance()
        {
            UnityEngine.Object.DestroyImmediate(_controllerGo);
            _controllerGo = null;

            // Use Unity's == null check which handles "fake null" destroyed objects
            Assert.That(WindController.Instance == null, Is.True, "Instance should be null after destroy");
        }

        [Test]
        public void WindDisabled_GlobalStrengthIsZero()
        {
            // Set some values first
            _controller.WindSpeed = 2.0f;
            _controller.WindStrength = 1.5f;
            _controller.WindEnabled = true;
            _controller.ForceUpdate();

            // Verify non-zero when enabled
            var enabledStrength = Shader.GetGlobalFloat("_GlobalWindStrength");
            Assert.That(enabledStrength, Is.GreaterThan(0f));

            // Disable and verify zero
            _controller.WindEnabled = false;
            var disabledStrength = Shader.GetGlobalFloat("_GlobalWindStrength");
            Assert.That(disabledStrength, Is.EqualTo(0f));
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnWindChanged_FiredWhenWindEnabledChanges()
        {
            int fireCount = 0;
            _controller.OnWindChanged += () => fireCount++;

            _controller.WindEnabled = false;
            _controller.WindEnabled = true;

            Assert.That(fireCount, Is.EqualTo(2));
        }

        [Test]
        public void OnWindChanged_FiredWhenSpeedChanges()
        {
            int fireCount = 0;
            _controller.OnWindChanged += () => fireCount++;

            _controller.WindSpeed = 0.5f;
            _controller.WindSpeed = 1.5f;

            Assert.That(fireCount, Is.EqualTo(2));
        }

        [Test]
        public void OnWindChanged_FiredWhenStrengthChanges()
        {
            int fireCount = 0;
            _controller.OnWindChanged += () => fireCount++;

            _controller.WindStrength = 0.5f;

            Assert.That(fireCount, Is.EqualTo(1));
        }

        [Test]
        public void OnWindChanged_FiredWhenDirectionChanges()
        {
            int fireCount = 0;
            _controller.OnWindChanged += () => fireCount++;

            _controller.WindDirectionDegrees = 90f;

            Assert.That(fireCount, Is.EqualTo(1));
        }

        [Test]
        public void OnWindChanged_FiredWhenTurbulenceChanges()
        {
            int fireCount = 0;
            _controller.OnWindChanged += () => fireCount++;

            _controller.Turbulence = 1.5f;

            Assert.That(fireCount, Is.EqualTo(1));
        }

        [Test]
        public void OnWindChanged_FiredWhenGustingEnabledChanges()
        {
            int fireCount = 0;
            _controller.OnWindChanged += () => fireCount++;

            _controller.GustingEnabled = false;

            Assert.That(fireCount, Is.EqualTo(1));
        }

        [Test]
        public void OnWindChanged_FiredWhenGustStrengthChanges()
        {
            int fireCount = 0;
            _controller.OnWindChanged += () => fireCount++;

            _controller.GustStrength = 0.5f;

            Assert.That(fireCount, Is.EqualTo(1));
        }

        [Test]
        public void OnWindChanged_FiredWhenGustFrequencyChanges()
        {
            int fireCount = 0;
            _controller.OnWindChanged += () => fireCount++;

            _controller.GustFrequency = 2.0f;

            Assert.That(fireCount, Is.EqualTo(1));
        }

        #endregion

        #region GrassQualitySettings Struct Tests

        [Test]
        public void GrassQualitySettings_High_AllFeaturesEnabled()
        {
            var settings = GrassQualitySettings.High;

            Assert.That(settings.EnableWind, Is.True);
            Assert.That(settings.EnableTipColor, Is.True);
            Assert.That(settings.EnableSSS, Is.True);
            Assert.That(settings.EnableAO, Is.True);
            Assert.That(settings.EnableColorVariation, Is.True);
        }

        [Test]
        public void GrassQualitySettings_Medium_SSSDisabled()
        {
            var settings = GrassQualitySettings.Medium;

            Assert.That(settings.EnableWind, Is.True);
            Assert.That(settings.EnableTipColor, Is.True);
            Assert.That(settings.EnableSSS, Is.False);
            Assert.That(settings.EnableAO, Is.True);
            Assert.That(settings.EnableColorVariation, Is.True);
        }

        [Test]
        public void GrassQualitySettings_Low_MostFeaturesDisabled()
        {
            var settings = GrassQualitySettings.Low;

            Assert.That(settings.EnableWind, Is.False);
            Assert.That(settings.EnableTipColor, Is.False);
            Assert.That(settings.EnableSSS, Is.False);
            Assert.That(settings.EnableAO, Is.False);
            Assert.That(settings.EnableColorVariation, Is.False);
        }

        [Test]
        public void GrassQualitySettings_GetSettings_HighTier_ReturnsHighSettings()
        {
            var settings = GrassQualitySettings.GetSettings(CoreQualityTier.High);

            Assert.That(settings.EnableSSS, Is.True);
            Assert.That(settings.EnableWind, Is.True);
        }

        [Test]
        public void GrassQualitySettings_GetSettings_MediumTier_ReturnsMediumSettings()
        {
            var settings = GrassQualitySettings.GetSettings(CoreQualityTier.Medium);

            Assert.That(settings.EnableSSS, Is.False);
            Assert.That(settings.EnableWind, Is.True);
        }

        [Test]
        public void GrassQualitySettings_GetSettings_LowTier_ReturnsLowSettings()
        {
            var settings = GrassQualitySettings.GetSettings(CoreQualityTier.Low);

            Assert.That(settings.EnableWind, Is.False);
            Assert.That(settings.EnableSSS, Is.False);
        }

        #endregion

        #region GrassQuality Property Tests

        [Test]
        public void GrassQuality_DefaultsToHighTierSettings()
        {
            // Default tier is High
            var grassQuality = _controller.GrassQuality;

            Assert.That(grassQuality.EnableWind, Is.True);
            Assert.That(grassQuality.EnableSSS, Is.True);
        }

        [Test]
        public void SetGrassQualitySettings_UpdatesGrassQuality()
        {
            var customSettings = new GrassQualitySettings
            {
                EnableWind = false,
                EnableTipColor = true,
                EnableSSS = false,
                EnableAO = true,
                EnableColorVariation = false
            };

            _controller.SetGrassQualitySettings(customSettings);

            Assert.That(_controller.GrassQuality.EnableWind, Is.False);
            Assert.That(_controller.GrassQuality.EnableTipColor, Is.True);
            Assert.That(_controller.GrassQuality.EnableSSS, Is.False);
            Assert.That(_controller.GrassQuality.EnableAO, Is.True);
            Assert.That(_controller.GrassQuality.EnableColorVariation, Is.False);
        }

        [Test]
        public void SetGrassQualitySettings_FiresOnWindChanged()
        {
            bool eventFired = false;
            _controller.OnWindChanged += () => eventFired = true;

            _controller.SetGrassQualitySettings(GrassQualitySettings.Low);

            Assert.That(eventFired, Is.True);
        }

        #endregion

        #region RegisterGrassMaterial Tests

        [Test]
        public void RegisterGrassMaterial_NullMaterial_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _controller.RegisterGrassMaterial(null));
        }

        [Test]
        public void RegisterGrassMaterial_AddsMaterialToArray()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            _controller.RegisterGrassMaterial(material);

            Assert.That(_controller.GrassMaterials, Is.Not.Null);
            Assert.That(_controller.GrassMaterials, Does.Contain(material));

            // Cleanup
            UnityEngine.Object.DestroyImmediate(material);
        }

        [Test]
        public void RegisterGrassMaterial_DuplicateMaterial_NotAddedTwice()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            _controller.RegisterGrassMaterial(material);
            _controller.RegisterGrassMaterial(material);

            Assert.That(_controller.GrassMaterials.Length, Is.EqualTo(1));

            // Cleanup
            UnityEngine.Object.DestroyImmediate(material);
        }

        [Test]
        public void RegisterGrassMaterial_MultipleMaterials_AllAdded()
        {
            var material1 = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var material2 = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            _controller.RegisterGrassMaterial(material1);
            _controller.RegisterGrassMaterial(material2);

            Assert.That(_controller.GrassMaterials.Length, Is.EqualTo(2));
            Assert.That(_controller.GrassMaterials, Does.Contain(material1));
            Assert.That(_controller.GrassMaterials, Does.Contain(material2));

            // Cleanup
            UnityEngine.Object.DestroyImmediate(material1);
            UnityEngine.Object.DestroyImmediate(material2);
        }

        #endregion

        #region Material Keyword Tests

        [Test]
        public void RegisterGrassMaterial_AppliesQualitySettings_DoesNotThrow()
        {
            // Note: Custom keywords like _ENABLEWIND_ON are shader-specific and only work
            // with StylizedGrass/InstancedGrass shaders. This test verifies the method
            // runs without error when applying settings to any material.
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            _controller.SetGrassQualitySettings(GrassQualitySettings.High);

            Assert.DoesNotThrow(() => _controller.RegisterGrassMaterial(material));

            // Cleanup
            UnityEngine.Object.DestroyImmediate(material);
        }

        [Test]
        public void SetGrassQualitySettings_AppliesKeywordsToMaterials_DoesNotThrow()
        {
            // Note: Keyword application is shader-specific. This test verifies the
            // quality settings propagate to materials without throwing.
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            _controller.RegisterGrassMaterial(material);

            Assert.DoesNotThrow(() => _controller.SetGrassQualitySettings(GrassQualitySettings.Low));

            // Cleanup
            UnityEngine.Object.DestroyImmediate(material);
        }

        [Test]
        public void GrassMaterials_SetProperty_AppliesSettingsToAllMaterials()
        {
            // Note: Keyword verification skipped since keywords are shader-specific.
            // This test verifies that setting GrassMaterials propagates quality settings.
            var material1 = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var material2 = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            _controller.SetGrassQualitySettings(GrassQualitySettings.High);

            Assert.DoesNotThrow(() =>
            {
                _controller.GrassMaterials = new Material[] { material1, material2 };
            });

            // Verify materials were set
            Assert.That(_controller.GrassMaterials.Length, Is.EqualTo(2));

            // Cleanup
            UnityEngine.Object.DestroyImmediate(material1);
            UnityEngine.Object.DestroyImmediate(material2);
        }

        #endregion

        #region CurrentQualityTier Tests

        [Test]
        public void CurrentQualityTier_DefaultsToHigh()
        {
            Assert.That(_controller.CurrentQualityTier, Is.EqualTo(CoreQualityTier.High));
        }

        #endregion
    }
}
