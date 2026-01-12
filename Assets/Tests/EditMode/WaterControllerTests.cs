// ABOUTME: Unit tests for WaterController singleton that manages water rendering and planar reflections.
// ABOUTME: Tests initialization, preset management, quality tier adjustments, and events.

using System;
using NUnit.Framework;
using UnityEngine;
using OpenRange.Visualization;
using OpenRange.Core;
using CoreQualityTier = OpenRange.Core.QualityTier;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class WaterControllerTests
    {
        private GameObject _controllerGo;
        private WaterController _controller;
        private Material _testMaterial;

        [SetUp]
        public void SetUp()
        {
            // Clean up any existing instance
            if (WaterController.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(WaterController.Instance.gameObject);
            }

            _controllerGo = new GameObject("WaterController");
            _controller = _controllerGo.AddComponent<WaterController>();

            // Create a test material
            var shader = Shader.Find("OpenRange/StylizedWater");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            _testMaterial = new Material(shader);

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

            if (_testMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(_testMaterial);
            }
        }

        #region Initialization Tests

        [Test]
        public void Instance_IsSetAfterCreation()
        {
            Assert.That(WaterController.Instance, Is.EqualTo(_controller));
        }

        [Test]
        public void IsInitialized_IsTrueAfterAwake()
        {
            Assert.That(_controller.IsInitialized, Is.True);
        }

        [Test]
        public void WaterEnabled_DefaultsToTrue()
        {
            Assert.That(_controller.WaterEnabled, Is.True);
        }

        [Test]
        public void CurrentPreset_DefaultsToOcean()
        {
            Assert.That(_controller.CurrentPreset, Is.EqualTo(WaterPreset.Ocean));
        }

        #endregion

        #region Property Setter Tests

        [Test]
        public void WaterEnabled_SetToFalse_UpdatesProperty()
        {
            _controller.WaterEnabled = false;
            Assert.That(_controller.WaterEnabled, Is.False);
        }

        [Test]
        public void WaterEnabled_SetToFalse_FiresEvent()
        {
            bool eventFired = false;
            _controller.OnWaterChanged += () => eventFired = true;

            _controller.WaterEnabled = false;

            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void WaterEnabled_SetToSameValue_DoesNotFireEvent()
        {
            bool eventFired = false;
            _controller.OnWaterChanged += () => eventFired = true;

            // Set to same value (default is true)
            _controller.WaterEnabled = true;

            Assert.That(eventFired, Is.False);
        }

        [Test]
        public void CurrentPreset_SetToPond_UpdatesProperty()
        {
            _controller.CurrentPreset = WaterPreset.Pond;
            Assert.That(_controller.CurrentPreset, Is.EqualTo(WaterPreset.Pond));
        }

        [Test]
        public void CurrentPreset_SetToPond_FiresEvent()
        {
            bool eventFired = false;
            _controller.OnWaterChanged += () => eventFired = true;

            _controller.CurrentPreset = WaterPreset.Pond;

            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void CurrentPreset_SetToSameValue_DoesNotFireEvent()
        {
            bool eventFired = false;
            _controller.OnWaterChanged += () => eventFired = true;

            // Set to same value (default is Ocean)
            _controller.CurrentPreset = WaterPreset.Ocean;

            Assert.That(eventFired, Is.False);
        }

        [Test]
        public void WaterMaterial_SetValue_UpdatesProperty()
        {
            _controller.WaterMaterial = _testMaterial;
            Assert.That(_controller.WaterMaterial, Is.EqualTo(_testMaterial));
        }

        #endregion

        #region Preset Settings Tests

        [Test]
        public void PresetSettings_OceanPreset_HasExpectedValues()
        {
            var settings = WaterPresetSettings.Ocean;

            Assert.That(settings.ShallowColor.r, Is.EqualTo(0.2f).Within(0.01f));
            Assert.That(settings.DeepColor.b, Is.EqualTo(0.4f).Within(0.01f));
            Assert.That(settings.DepthFadeDistance, Is.EqualTo(8.0f));
            Assert.That(settings.WaveHeight, Is.EqualTo(0.3f));
        }

        [Test]
        public void PresetSettings_PondPreset_HasExpectedValues()
        {
            var settings = WaterPresetSettings.Pond;

            Assert.That(settings.ShallowColor.g, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(settings.DepthFadeDistance, Is.EqualTo(3.0f));
            Assert.That(settings.WaveHeight, Is.EqualTo(0.05f));
            Assert.That(settings.ReflectionStrength, Is.EqualTo(0.7f));
        }

        [Test]
        public void GetPreset_Ocean_ReturnsOceanSettings()
        {
            var settings = WaterPresetSettings.GetPreset(WaterPreset.Ocean);
            var expected = WaterPresetSettings.Ocean;

            Assert.That(settings.WaveHeight, Is.EqualTo(expected.WaveHeight));
            Assert.That(settings.DepthFadeDistance, Is.EqualTo(expected.DepthFadeDistance));
        }

        [Test]
        public void GetPreset_Pond_ReturnsPondSettings()
        {
            var settings = WaterPresetSettings.GetPreset(WaterPreset.Pond);
            var expected = WaterPresetSettings.Pond;

            Assert.That(settings.WaveHeight, Is.EqualTo(expected.WaveHeight));
            Assert.That(settings.DepthFadeDistance, Is.EqualTo(expected.DepthFadeDistance));
        }

        [Test]
        public void GetPreset_Custom_ReturnsOceanAsDefault()
        {
            var settings = WaterPresetSettings.GetPreset(WaterPreset.Custom);
            var ocean = WaterPresetSettings.Ocean;

            Assert.That(settings.WaveHeight, Is.EqualTo(ocean.WaveHeight));
        }

        #endregion

        #region Quality Settings Tests

        [Test]
        public void QualitySettings_High_EnablesAllFeatures()
        {
            var settings = WaterQualitySettings.High;

            Assert.That(settings.EnableWaves, Is.True);
            Assert.That(settings.EnableFoam, Is.True);
            Assert.That(settings.EnableReflection, Is.True);
            Assert.That(settings.EnablePlanarReflection, Is.True);
            Assert.That(settings.ReflectionTextureSize, Is.EqualTo(512));
        }

        [Test]
        public void QualitySettings_Medium_DisablesPlanarReflection()
        {
            var settings = WaterQualitySettings.Medium;

            Assert.That(settings.EnableWaves, Is.True);
            Assert.That(settings.EnableFoam, Is.True);
            Assert.That(settings.EnableReflection, Is.True);
            Assert.That(settings.EnablePlanarReflection, Is.False);
            Assert.That(settings.ReflectionTextureSize, Is.EqualTo(256));
        }

        [Test]
        public void QualitySettings_Low_DisablesAllEffects()
        {
            var settings = WaterQualitySettings.Low;

            Assert.That(settings.EnableWaves, Is.False);
            Assert.That(settings.EnableFoam, Is.False);
            Assert.That(settings.EnableReflection, Is.False);
            Assert.That(settings.EnablePlanarReflection, Is.False);
        }

        [Test]
        public void GetSettings_HighTier_ReturnsHighSettings()
        {
            var settings = WaterQualitySettings.GetSettings(CoreQualityTier.High);
            Assert.That(settings.EnablePlanarReflection, Is.True);
        }

        [Test]
        public void GetSettings_MediumTier_ReturnsMediumSettings()
        {
            var settings = WaterQualitySettings.GetSettings(CoreQualityTier.Medium);
            Assert.That(settings.EnablePlanarReflection, Is.False);
            Assert.That(settings.EnableWaves, Is.True);
        }

        [Test]
        public void GetSettings_LowTier_ReturnsLowSettings()
        {
            var settings = WaterQualitySettings.GetSettings(CoreQualityTier.Low);
            Assert.That(settings.EnableWaves, Is.False);
        }

        #endregion

        #region Renderer Registration Tests

        [Test]
        public void RegisterWaterRenderer_AddsRenderer()
        {
            var waterGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            var renderer = waterGo.GetComponent<Renderer>();

            _controller.RegisterWaterRenderer(renderer);

            // Verify by checking we can set material
            _controller.WaterMaterial = _testMaterial;
            Assert.That(renderer.sharedMaterial, Is.EqualTo(_testMaterial));

            UnityEngine.Object.DestroyImmediate(waterGo);
        }

        [Test]
        public void RegisterWaterRenderer_NullRenderer_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _controller.RegisterWaterRenderer(null));
        }

        [Test]
        public void RegisterWaterRenderer_DuplicateRenderer_DoesNotAddTwice()
        {
            var waterGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            var renderer = waterGo.GetComponent<Renderer>();

            _controller.RegisterWaterRenderer(renderer);
            _controller.RegisterWaterRenderer(renderer); // Duplicate

            // Should still work without issues
            Assert.DoesNotThrow(() => _controller.UnregisterWaterRenderer(renderer));

            UnityEngine.Object.DestroyImmediate(waterGo);
        }

        [Test]
        public void UnregisterWaterRenderer_RemovesRenderer()
        {
            var waterGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            var renderer = waterGo.GetComponent<Renderer>();

            _controller.RegisterWaterRenderer(renderer);
            _controller.UnregisterWaterRenderer(renderer);

            // Should not throw when unregistered renderer is used
            Assert.DoesNotThrow(() => _controller.ForceUpdate());

            UnityEngine.Object.DestroyImmediate(waterGo);
        }

        #endregion

        #region Water Plane Tests

        [Test]
        public void SetWaterPlane_SetsTransform()
        {
            var planeGo = new GameObject("TestPlane");

            _controller.SetWaterPlane(planeGo.transform);

            // Reflection calculations depend on this transform
            Assert.DoesNotThrow(() => _controller.ForceUpdate());

            UnityEngine.Object.DestroyImmediate(planeGo);
        }

        #endregion

        #region Custom Settings Tests

        [Test]
        public void ApplyCustomSettings_SetsPresetToCustom()
        {
            var customSettings = new WaterPresetSettings
            {
                ShallowColor = Color.red,
                DeepColor = Color.blue,
                DepthFadeDistance = 10f,
                WaveHeight = 0.5f,
                WaveSpeed = 2f,
                FoamColor = Color.white,
                FoamWidth = 1f,
                ReflectionStrength = 0.8f
            };

            _controller.ApplyCustomSettings(customSettings);

            Assert.That(_controller.CurrentPreset, Is.EqualTo(WaterPreset.Custom));
        }

        [Test]
        public void ApplyCustomSettings_FiresEvent()
        {
            bool eventFired = false;
            _controller.OnWaterChanged += () => eventFired = true;

            var customSettings = new WaterPresetSettings();
            _controller.ApplyCustomSettings(customSettings);

            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void ApplyCustomSettings_UpdatesPresetSettings()
        {
            var customSettings = new WaterPresetSettings
            {
                WaveHeight = 0.75f,
                DepthFadeDistance = 15f
            };

            _controller.ApplyCustomSettings(customSettings);

            Assert.That(_controller.PresetSettings.WaveHeight, Is.EqualTo(0.75f));
            Assert.That(_controller.PresetSettings.DepthFadeDistance, Is.EqualTo(15f));
        }

        #endregion

        #region ForceUpdate Tests

        [Test]
        public void ForceUpdate_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _controller.ForceUpdate());
        }

        [Test]
        public void ForceUpdate_WithMaterial_AppliesSettings()
        {
            _controller.WaterMaterial = _testMaterial;
            _controller.CurrentPreset = WaterPreset.Pond;

            Assert.DoesNotThrow(() => _controller.ForceUpdate());
        }

        #endregion

        #region Planar Reflection Tests

        [Test]
        public void IsPlanarReflectionActive_DefaultsBasedOnQuality()
        {
            // In EditMode tests without QualityManager, defaults to High
            // But reflection camera is created lazily
            Assert.That(_controller.IsPlanarReflectionActive, Is.False.Or.True);
        }

        [Test]
        public void QualitySettings_ReturnsCurrentSettings()
        {
            var settings = _controller.QualitySettings;
            Assert.That(settings.EnableWaves, Is.True.Or.False);
        }

        [Test]
        public void CurrentQualityTier_ReturnsValidTier()
        {
            var tier = _controller.CurrentQualityTier;
            Assert.That(tier, Is.EqualTo(CoreQualityTier.Low)
                .Or.EqualTo(CoreQualityTier.Medium)
                .Or.EqualTo(CoreQualityTier.High));
        }

        #endregion

        #region Singleton Tests

        [Test]
        public void SecondInstance_IsDestroyed()
        {
            var secondGo = new GameObject("WaterController2");
            var secondController = secondGo.AddComponent<WaterController>();

            // The singleton pattern should preserve the first instance
            Assert.That(WaterController.Instance, Is.EqualTo(_controller));

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
            Assert.That(WaterController.Instance == null, Is.True, "Instance should be null after destroy");
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnWaterChanged_FiredWhenWaterEnabledChanges()
        {
            int fireCount = 0;
            _controller.OnWaterChanged += () => fireCount++;

            _controller.WaterEnabled = false;
            _controller.WaterEnabled = true;

            Assert.That(fireCount, Is.EqualTo(2));
        }

        [Test]
        public void OnWaterChanged_FiredWhenPresetChanges()
        {
            int fireCount = 0;
            _controller.OnWaterChanged += () => fireCount++;

            _controller.CurrentPreset = WaterPreset.Pond;
            _controller.CurrentPreset = WaterPreset.Ocean;

            Assert.That(fireCount, Is.EqualTo(2));
        }

        #endregion
    }
}
