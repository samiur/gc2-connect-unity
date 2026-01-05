// ABOUTME: Unit tests for LightingController component.
// ABOUTME: Tests preset configuration, quality tier settings, and event handling.

using NUnit.Framework;
using UnityEngine;
using OpenRange.Visualization;
using QualityTier = OpenRange.Core.QualityTier;

namespace OpenRange.Tests.EditMode
{
    /// <summary>
    /// Unit tests for LightingController functionality.
    /// </summary>
    [TestFixture]
    public class LightingControllerTests
    {
        private GameObject _controllerGo;
        private LightingController _controller;
        private GameObject _lightGo;
        private Light _light;
        private GameObject _probeGo;
        private ReflectionProbe _probe;
        private Material _skyboxMaterial;

        [SetUp]
        public void SetUp()
        {
            // Create controller
            _controllerGo = new GameObject("LightingController");
            _controller = _controllerGo.AddComponent<LightingController>();

            // Create directional light
            _lightGo = new GameObject("DirectionalLight");
            _light = _lightGo.AddComponent<Light>();
            _light.type = LightType.Directional;

            // Create reflection probe
            _probeGo = new GameObject("ReflectionProbe");
            _probe = _probeGo.AddComponent<ReflectionProbe>();

            // Create skybox material (use Standard shader as fallback for testing)
            var shader = Shader.Find("Standard");
            if (shader != null)
            {
                _skyboxMaterial = new Material(shader);
            }
            else
            {
                _skyboxMaterial = new Material(Shader.Find("Unlit/Color"));
            }

            // Wire up references via SerializedObject
            var so = new UnityEditor.SerializedObject(_controller);
            so.FindProperty("_directionalLight").objectReferenceValue = _light;
            so.FindProperty("_reflectionProbe").objectReferenceValue = _probe;
            so.FindProperty("_skyboxMaterial").objectReferenceValue = _skyboxMaterial;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            if (_controllerGo != null)
            {
                Object.DestroyImmediate(_controllerGo);
            }
            if (_lightGo != null)
            {
                Object.DestroyImmediate(_lightGo);
            }
            if (_probeGo != null)
            {
                Object.DestroyImmediate(_probeGo);
            }
            if (_skyboxMaterial != null)
            {
                Object.DestroyImmediate(_skyboxMaterial);
            }
        }

        #region Initialization Tests

        [Test]
        public void Initialize_SetsIsInitializedTrue()
        {
            _controller.Initialize();

            Assert.IsTrue(_controller.IsInitialized);
        }

        [Test]
        public void Initialize_CalledTwice_DoesNotReinitialize()
        {
            _controller.Initialize();
            var firstPreset = _controller.CurrentPreset;
            _controller.Initialize();

            Assert.AreEqual(firstPreset, _controller.CurrentPreset);
        }

        [Test]
        public void Initialize_SetsDefaultPreset()
        {
            _controller.Initialize();

            Assert.AreEqual(LightingController.LightingPreset.GoldenHour, _controller.CurrentPreset);
        }

        #endregion

        #region Preset Configuration Tests

        [Test]
        public void GetPresetConfig_GoldenHour_ReturnsExpectedValues()
        {
            var config = _controller.GetPresetConfig(LightingController.LightingPreset.GoldenHour);

            Assert.AreEqual(new Color(0.1f, 0.3f, 0.8f), config.SkyTopColor);
            Assert.AreEqual(new Color(1.0f, 0.6f, 0.4f), config.SkyHorizonColor);
            Assert.AreEqual(1.2f, config.LightIntensity);
        }

        [Test]
        public void GetPresetConfig_Midday_ReturnsExpectedValues()
        {
            var config = _controller.GetPresetConfig(LightingController.LightingPreset.Midday);

            Assert.AreEqual(new Color(0.15f, 0.4f, 0.9f), config.SkyTopColor);
            Assert.AreEqual(1.5f, config.LightIntensity);
        }

        [Test]
        public void GetPresetConfig_Overcast_ReturnsExpectedValues()
        {
            var config = _controller.GetPresetConfig(LightingController.LightingPreset.Overcast);

            Assert.AreEqual(new Color(0.4f, 0.45f, 0.5f), config.SkyTopColor);
            Assert.AreEqual(0.8f, config.LightIntensity);
        }

        [Test]
        public void GetPresetConfig_GoldenHour_HasValidSunDirection()
        {
            var config = _controller.GetPresetConfig(LightingController.LightingPreset.GoldenHour);

            Assert.AreNotEqual(Vector3.zero, config.SunDirection);
            Assert.Greater(config.SunDirection.magnitude, 0.5f);
        }

        [Test]
        public void GetPresetConfig_AllPresets_HaveValidAmbientColors()
        {
            var presets = new[]
            {
                LightingController.LightingPreset.GoldenHour,
                LightingController.LightingPreset.Midday,
                LightingController.LightingPreset.Overcast
            };

            foreach (var preset in presets)
            {
                var config = _controller.GetPresetConfig(preset);

                Assert.AreNotEqual(Color.black, config.AmbientSkyColor, $"Preset {preset} has black ambient sky");
                Assert.AreNotEqual(Color.black, config.AmbientEquatorColor, $"Preset {preset} has black ambient equator");
                Assert.AreNotEqual(Color.black, config.AmbientGroundColor, $"Preset {preset} has black ambient ground");
            }
        }

        #endregion

        #region SetPreset Tests

        [Test]
        public void SetPreset_ChangesCurrentPreset()
        {
            _controller.Initialize();
            _controller.SetPreset(LightingController.LightingPreset.Midday);

            Assert.AreEqual(LightingController.LightingPreset.Midday, _controller.CurrentPreset);
        }

        [Test]
        public void SetPreset_SamePreset_DoesNotFireEvent()
        {
            _controller.Initialize();
            int eventCount = 0;
            _controller.OnPresetChanged += (_) => eventCount++;

            _controller.SetPreset(LightingController.LightingPreset.GoldenHour);

            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void SetPreset_DifferentPreset_FiresEvent()
        {
            _controller.Initialize();
            LightingController.LightingPreset? receivedPreset = null;
            _controller.OnPresetChanged += (preset) => receivedPreset = preset;

            _controller.SetPreset(LightingController.LightingPreset.Overcast);

            Assert.AreEqual(LightingController.LightingPreset.Overcast, receivedPreset);
        }

        [Test]
        public void SetPreset_UpdatesLightIntensity()
        {
            _controller.Initialize();
            var middayConfig = _controller.GetPresetConfig(LightingController.LightingPreset.Midday);

            _controller.SetPreset(LightingController.LightingPreset.Midday);

            Assert.AreEqual(middayConfig.LightIntensity, _light.intensity, 0.01f);
        }

        [Test]
        public void SetPreset_UpdatesLightColor()
        {
            _controller.Initialize();
            var overcastConfig = _controller.GetPresetConfig(LightingController.LightingPreset.Overcast);

            _controller.SetPreset(LightingController.LightingPreset.Overcast);

            Assert.AreEqual(overcastConfig.LightColor.r, _light.color.r, 0.01f);
            Assert.AreEqual(overcastConfig.LightColor.g, _light.color.g, 0.01f);
            Assert.AreEqual(overcastConfig.LightColor.b, _light.color.b, 0.01f);
        }

        #endregion

        #region Quality Settings Tests

        [Test]
        public void GetQualitySettings_Low_DisablesClouds()
        {
            var settings = _controller.GetQualitySettings(QualityTier.Low);

            Assert.IsFalse(settings.EnableClouds);
        }

        [Test]
        public void GetQualitySettings_Low_DisablesShadows()
        {
            var settings = _controller.GetQualitySettings(QualityTier.Low);

            Assert.IsFalse(settings.EnableShadows);
            Assert.AreEqual(LightShadows.None, settings.ShadowType);
        }

        [Test]
        public void GetQualitySettings_Low_DisablesReflectionProbe()
        {
            var settings = _controller.GetQualitySettings(QualityTier.Low);

            Assert.IsFalse(settings.EnableReflectionProbe);
        }

        [Test]
        public void GetQualitySettings_Low_UsesSolidColorSkybox()
        {
            var settings = _controller.GetQualitySettings(QualityTier.Low);

            Assert.IsTrue(settings.UseSolidColorSkybox);
        }

        [Test]
        public void GetQualitySettings_Medium_EnablesClouds()
        {
            var settings = _controller.GetQualitySettings(QualityTier.Medium);

            Assert.IsTrue(settings.EnableClouds);
        }

        [Test]
        public void GetQualitySettings_Medium_UsesHardShadows()
        {
            var settings = _controller.GetQualitySettings(QualityTier.Medium);

            Assert.IsTrue(settings.EnableShadows);
            Assert.AreEqual(LightShadows.Hard, settings.ShadowType);
        }

        [Test]
        public void GetQualitySettings_Medium_DisablesReflectionProbe()
        {
            var settings = _controller.GetQualitySettings(QualityTier.Medium);

            Assert.IsFalse(settings.EnableReflectionProbe);
        }

        [Test]
        public void GetQualitySettings_High_EnablesAllFeatures()
        {
            var settings = _controller.GetQualitySettings(QualityTier.High);

            Assert.IsTrue(settings.EnableClouds);
            Assert.IsTrue(settings.EnableSun);
            Assert.IsTrue(settings.EnableShadows);
            Assert.IsTrue(settings.EnableReflectionProbe);
        }

        [Test]
        public void GetQualitySettings_High_UsesSoftShadows()
        {
            var settings = _controller.GetQualitySettings(QualityTier.High);

            Assert.AreEqual(LightShadows.Soft, settings.ShadowType);
        }

        [Test]
        public void GetQualitySettings_High_DoesNotUseSolidColorSkybox()
        {
            var settings = _controller.GetQualitySettings(QualityTier.High);

            Assert.IsFalse(settings.UseSolidColorSkybox);
        }

        #endregion

        #region SetQualityTier Tests

        [Test]
        public void SetQualityTier_ChangesCurrentQualityTier()
        {
            _controller.Initialize();
            _controller.SetQualityTier(QualityTier.Low);

            Assert.AreEqual(QualityTier.Low, _controller.CurrentQualityTier);
        }

        [Test]
        public void SetQualityTier_SameTier_DoesNotFireEvent()
        {
            _controller.Initialize();
            int eventCount = 0;
            _controller.OnQualityTierChanged += (_) => eventCount++;

            // Default tier is High for this test
            _controller.SetQualityTier(_controller.CurrentQualityTier);

            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void SetQualityTier_DifferentTier_FiresEvent()
        {
            _controller.Initialize();
            QualityTier? receivedTier = null;
            _controller.OnQualityTierChanged += (tier) => receivedTier = tier;

            _controller.SetQualityTier(QualityTier.Low);

            Assert.AreEqual(QualityTier.Low, receivedTier);
        }

        [Test]
        public void SetQualityTier_Low_DisablesReflectionProbe()
        {
            _controller.Initialize();
            _probeGo.SetActive(true);

            _controller.SetQualityTier(QualityTier.Low);

            Assert.IsFalse(_probeGo.activeSelf);
        }

        [Test]
        public void SetQualityTier_High_EnablesReflectionProbe()
        {
            _controller.Initialize();
            // First set to Low to change state (default is High)
            _controller.SetQualityTier(QualityTier.Low);
            Assert.IsFalse(_probeGo.activeSelf);

            // Now change to High and verify probe is enabled
            _controller.SetQualityTier(QualityTier.High);

            Assert.IsTrue(_probeGo.activeSelf);
        }

        [Test]
        public void SetQualityTier_Low_SetsShadowsToNone()
        {
            _controller.Initialize();

            _controller.SetQualityTier(QualityTier.Low);

            Assert.AreEqual(LightShadows.None, _light.shadows);
        }

        [Test]
        public void SetQualityTier_High_SetsShadowsToSoft()
        {
            _controller.Initialize();

            _controller.SetQualityTier(QualityTier.High);

            Assert.AreEqual(LightShadows.Soft, _light.shadows);
        }

        #endregion

        #region Reference Tests

        [Test]
        public void DirectionalLight_ReturnsAssignedLight()
        {
            Assert.AreEqual(_light, _controller.DirectionalLight);
        }

        [Test]
        public void ReflectionProbe_ReturnsAssignedProbe()
        {
            Assert.AreEqual(_probe, _controller.ReflectionProbe);
        }

        [Test]
        public void SkyboxMaterial_ReturnsAssignedMaterial()
        {
            Assert.AreEqual(_skyboxMaterial, _controller.SkyboxMaterial);
        }

        [Test]
        public void Initialize_WithNullLight_DoesNotThrow()
        {
            var so = new UnityEditor.SerializedObject(_controller);
            so.FindProperty("_directionalLight").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.DoesNotThrow(() => _controller.Initialize());
        }

        [Test]
        public void Initialize_WithNullProbe_DoesNotThrow()
        {
            var so = new UnityEditor.SerializedObject(_controller);
            so.FindProperty("_reflectionProbe").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.DoesNotThrow(() => _controller.Initialize());
        }

        [Test]
        public void Initialize_WithNullMaterial_DoesNotThrow()
        {
            var so = new UnityEditor.SerializedObject(_controller);
            so.FindProperty("_skyboxMaterial").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.DoesNotThrow(() => _controller.Initialize());
        }

        #endregion

        #region Edge Cases

        [Test]
        public void SetPreset_BeforeInitialize_StillWorks()
        {
            Assert.DoesNotThrow(() => _controller.SetPreset(LightingController.LightingPreset.Midday));
            Assert.AreEqual(LightingController.LightingPreset.Midday, _controller.CurrentPreset);
        }

        [Test]
        public void SetQualityTier_BeforeInitialize_StillWorks()
        {
            Assert.DoesNotThrow(() => _controller.SetQualityTier(QualityTier.Low));
            Assert.AreEqual(QualityTier.Low, _controller.CurrentQualityTier);
        }

        [Test]
        public void RefreshReflectionProbe_WithActiveProbe_DoesNotThrow()
        {
            _controller.Initialize();
            _probeGo.SetActive(true);

            Assert.DoesNotThrow(() => _controller.RefreshReflectionProbe());
        }

        [Test]
        public void RefreshReflectionProbe_WithInactiveProbe_DoesNotThrow()
        {
            _controller.Initialize();
            _probeGo.SetActive(false);

            Assert.DoesNotThrow(() => _controller.RefreshReflectionProbe());
        }

        [Test]
        public void RefreshReflectionProbe_WithNullProbe_DoesNotThrow()
        {
            var so = new UnityEditor.SerializedObject(_controller);
            so.FindProperty("_reflectionProbe").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
            _controller.Initialize();

            Assert.DoesNotThrow(() => _controller.RefreshReflectionProbe());
        }

        #endregion

        #region Preset Config Validation

        [Test]
        public void AllPresetConfigs_HavePositiveLightIntensity()
        {
            var presets = new[]
            {
                LightingController.LightingPreset.GoldenHour,
                LightingController.LightingPreset.Midday,
                LightingController.LightingPreset.Overcast
            };

            foreach (var preset in presets)
            {
                var config = _controller.GetPresetConfig(preset);
                Assert.Greater(config.LightIntensity, 0f, $"Preset {preset} has non-positive light intensity");
            }
        }

        [Test]
        public void AllPresetConfigs_HaveNormalizedSunDirection()
        {
            var presets = new[]
            {
                LightingController.LightingPreset.GoldenHour,
                LightingController.LightingPreset.Midday,
                LightingController.LightingPreset.Overcast
            };

            foreach (var preset in presets)
            {
                var config = _controller.GetPresetConfig(preset);
                var magnitude = config.SunDirection.magnitude;
                Assert.Greater(magnitude, 0.5f, $"Preset {preset} has near-zero sun direction");
                Assert.Less(magnitude, 2f, $"Preset {preset} has unexpectedly large sun direction");
            }
        }

        #endregion
    }
}
