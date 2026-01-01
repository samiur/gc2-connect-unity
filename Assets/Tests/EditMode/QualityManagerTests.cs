// ABOUTME: Unit tests for the QualityManager service.
// ABOUTME: Tests quality tier detection, tier switching, settings, and dynamic adjustment.

using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using OpenRange.Core;
using URPShadowQuality = UnityEngine.Rendering.Universal.ShadowQuality;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class QualityManagerTests
    {
        private GameObject _testObject;
        private QualityManager _qualityManager;

        [SetUp]
        public void SetUp()
        {
            if (QualityManager.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(QualityManager.Instance.gameObject);
            }

            if (SettingsManager.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(SettingsManager.Instance.gameObject);
            }

            PlatformManager.ClearCache();

            _testObject = new GameObject("TestQualityManager");
            _qualityManager = _testObject.AddComponent<QualityManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testObject);
            }

            PlatformManager.ClearCache();
        }

        #region Singleton Tests

        [Test]
        public void Instance_IsSetAfterAwake()
        {
            Assert.IsNotNull(QualityManager.Instance);
        }

        [Test]
        public void Instance_ReturnsSameObjectOnMultipleCalls()
        {
            var first = QualityManager.Instance;
            var second = QualityManager.Instance;
            Assert.AreSame(first, second);
        }

        [Test]
        public void Singleton_DestroysNewInstanceIfOneExists()
        {
            var existingInstance = QualityManager.Instance;
            var secondObject = new GameObject("SecondQualityManager");
            var secondManager = secondObject.AddComponent<QualityManager>();

            Assert.AreNotSame(existingInstance, secondManager);
            Assert.AreSame(existingInstance, QualityManager.Instance);

            if (secondObject != null)
            {
                UnityEngine.Object.DestroyImmediate(secondObject);
            }
        }

        #endregion

        #region DetectOptimalTier Tests

        [Test]
        public void DetectOptimalTier_ReturnsValidTier()
        {
            _qualityManager.Initialize();
            var tier = _qualityManager.DetectOptimalTier();

            Assert.IsTrue(
                tier == QualityTier.Low ||
                tier == QualityTier.Medium ||
                tier == QualityTier.High
            );
        }

        [Test]
        public void DetectOptimalTier_ReturnsHighForEditor()
        {
#if UNITY_EDITOR
            _qualityManager.Initialize();
            var tier = _qualityManager.DetectOptimalTier();
            Assert.AreEqual(QualityTier.High, tier);
#endif
        }

        [Test]
        public void DetectOptimalTier_NeverReturnsAuto()
        {
            _qualityManager.Initialize();
            var tier = _qualityManager.DetectOptimalTier();
            Assert.AreNotEqual(QualityTier.Auto, tier);
        }

        #endregion

        #region ApplyQualityTier Tests

        [Test]
        public void ApplyQualityTier_SetsCurrentTier()
        {
            _qualityManager.Initialize();
            _qualityManager.ApplyQualityTier(QualityTier.Low);
            Assert.AreEqual(QualityTier.Low, _qualityManager.CurrentTier);
        }

        [Test]
        public void ApplyQualityTier_SetsEffectiveTier()
        {
            _qualityManager.Initialize();
            _qualityManager.ApplyQualityTier(QualityTier.High);
            Assert.AreEqual(QualityTier.High, _qualityManager.EffectiveTier);
        }

        [Test]
        public void ApplyQualityTier_WithAuto_DetectsOptimalTier()
        {
            _qualityManager.Initialize();
            _qualityManager.ApplyQualityTier(QualityTier.Auto);

            Assert.AreEqual(QualityTier.Auto, _qualityManager.CurrentTier);
            Assert.AreNotEqual(QualityTier.Auto, _qualityManager.EffectiveTier);
        }

        [Test]
        public void ApplyQualityTier_FiresOnQualityTierChangedEvent()
        {
            _qualityManager.Initialize();
            _qualityManager.ApplyQualityTier(QualityTier.Medium);

            bool eventFired = false;
            QualityTier firedTier = QualityTier.Auto;

            _qualityManager.OnQualityTierChanged += (tier) =>
            {
                eventFired = true;
                firedTier = tier;
            };

            _qualityManager.ApplyQualityTier(QualityTier.Low);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(QualityTier.Low, firedTier);
        }

        [Test]
        public void ApplyQualityTier_DoesNotFireEventWhenTierUnchanged()
        {
            _qualityManager.Initialize();
            _qualityManager.ApplyQualityTier(QualityTier.Medium);

            bool eventFired = false;
            _qualityManager.OnQualityTierChanged += (tier) => eventFired = true;

            _qualityManager.ApplyQualityTier(QualityTier.Medium);

            Assert.IsFalse(eventFired);
        }

        [Test]
        public void ApplyQualityTier_UpdatesTargetFrameRate()
        {
            _qualityManager.Initialize();

            _qualityManager.ApplyQualityTier(QualityTier.Low);
            Assert.AreEqual(30, _qualityManager.TargetFrameRate);

            _qualityManager.ApplyQualityTier(QualityTier.Medium);
            Assert.AreEqual(60, _qualityManager.TargetFrameRate);

            _qualityManager.ApplyQualityTier(QualityTier.High);
            Assert.AreEqual(120, _qualityManager.TargetFrameRate);
        }

        #endregion

        #region GetTierSettings Tests

        [Test]
        public void GetTierSettings_Low_ReturnsCorrectSettings()
        {
            _qualityManager.Initialize();
            var settings = _qualityManager.GetTierSettings(QualityTier.Low);

            Assert.AreEqual(30, settings.TargetFrameRate);
            Assert.AreEqual(URPShadowQuality.Disabled, settings.ShadowQuality);
            Assert.AreEqual(MsaaQuality.Disabled, settings.MsaaLevel);
            Assert.AreEqual(0.75f, settings.RenderScale, 0.001f);
            Assert.IsFalse(settings.PostProcessingEnabled);
        }

        [Test]
        public void GetTierSettings_Medium_ReturnsCorrectSettings()
        {
            _qualityManager.Initialize();
            var settings = _qualityManager.GetTierSettings(QualityTier.Medium);

            Assert.AreEqual(60, settings.TargetFrameRate);
            Assert.AreEqual(URPShadowQuality.HardShadows, settings.ShadowQuality);
            Assert.AreEqual(MsaaQuality._2x, settings.MsaaLevel);
            Assert.AreEqual(1.0f, settings.RenderScale, 0.001f);
            Assert.IsTrue(settings.PostProcessingEnabled);
        }

        [Test]
        public void GetTierSettings_High_ReturnsCorrectSettings()
        {
            _qualityManager.Initialize();
            var settings = _qualityManager.GetTierSettings(QualityTier.High);

            Assert.AreEqual(120, settings.TargetFrameRate);
            Assert.AreEqual(URPShadowQuality.SoftShadows, settings.ShadowQuality);
            Assert.AreEqual(MsaaQuality._4x, settings.MsaaLevel);
            Assert.AreEqual(1.0f, settings.RenderScale, 0.001f);
            Assert.IsTrue(settings.PostProcessingEnabled);
        }

        [Test]
        public void GetTierSettings_Auto_ReturnsMediumSettings()
        {
            _qualityManager.Initialize();
            var autoSettings = _qualityManager.GetTierSettings(QualityTier.Auto);
            var mediumSettings = _qualityManager.GetTierSettings(QualityTier.Medium);

            Assert.AreEqual(mediumSettings.TargetFrameRate, autoSettings.TargetFrameRate);
            Assert.AreEqual(mediumSettings.ShadowQuality, autoSettings.ShadowQuality);
        }

        #endregion

        #region Initialize Tests

        [Test]
        public void Initialize_SetsIsInitializedToTrue()
        {
            _qualityManager.Initialize();
            Assert.IsTrue(_qualityManager.IsInitialized);
        }

        [Test]
        public void Initialize_CanBeCalledMultipleTimes()
        {
            _qualityManager.Initialize();
            bool wasInitialized = _qualityManager.IsInitialized;

            Assert.DoesNotThrow(() => _qualityManager.Initialize());
            Assert.IsTrue(_qualityManager.IsInitialized);
        }

        [Test]
        public void Initialize_SetsDefaultTierToAuto()
        {
            _qualityManager.Initialize();
            Assert.AreEqual(QualityTier.Auto, _qualityManager.CurrentTier);
        }

        #endregion

        #region EnableDynamicAdjustment Tests

        [Test]
        public void EnableDynamicAdjustment_UpdatesProperty()
        {
            _qualityManager.Initialize();

            _qualityManager.EnableDynamicAdjustment(true);
            Assert.IsTrue(_qualityManager.DynamicAdjustmentEnabled);

            _qualityManager.EnableDynamicAdjustment(false);
            Assert.IsFalse(_qualityManager.DynamicAdjustmentEnabled);
        }

        [Test]
        public void EnableDynamicAdjustment_DoesNotThrow()
        {
            _qualityManager.Initialize();

            Assert.DoesNotThrow(() => _qualityManager.EnableDynamicAdjustment(true));
            Assert.DoesNotThrow(() => _qualityManager.EnableDynamicAdjustment(false));
        }

        #endregion

        #region GetAverageFPS Tests

        [Test]
        public void GetAverageFPS_ReturnsNonNegativeValue()
        {
            _qualityManager.Initialize();
            float fps = _qualityManager.GetAverageFPS();
            Assert.GreaterOrEqual(fps, 0f);
        }

        [Test]
        public void GetAverageFPS_ReturnsZeroWhenNotEnoughSamples()
        {
            _qualityManager.Initialize();
            float fps = _qualityManager.GetAverageFPS();
            Assert.AreEqual(0f, fps);
        }

        #endregion

        #region TargetFrameRate Tests

        [Test]
        public void TargetFrameRate_IsPositive()
        {
            _qualityManager.Initialize();
            Assert.Greater(_qualityManager.TargetFrameRate, 0);
        }

        [Test]
        public void TargetFrameRate_IsValidValue()
        {
            _qualityManager.Initialize();
            int fps = _qualityManager.TargetFrameRate;
            Assert.IsTrue(fps == 30 || fps == 60 || fps == 120);
        }

        #endregion

        #region EffectiveTier Tests

        [Test]
        public void EffectiveTier_NeverReturnsAuto()
        {
            _qualityManager.Initialize();
            Assert.AreNotEqual(QualityTier.Auto, _qualityManager.EffectiveTier);
        }

        [Test]
        public void EffectiveTier_MatchesCurrentTierWhenNotAuto()
        {
            _qualityManager.Initialize();
            _qualityManager.ApplyQualityTier(QualityTier.Low);

            Assert.AreEqual(_qualityManager.CurrentTier, _qualityManager.EffectiveTier);
        }

        #endregion

        #region QualityTierSettings Tests

        [Test]
        public void QualityTierSettings_TargetFrameRateIsAccessible()
        {
            var settings = new QualityTierSettings { TargetFrameRate = 60 };
            Assert.AreEqual(60, settings.TargetFrameRate);
        }

        [Test]
        public void QualityTierSettings_AllPropertiesSettable()
        {
            var settings = new QualityTierSettings
            {
                TargetFrameRate = 120,
                ShadowQuality = URPShadowQuality.SoftShadows,
                MsaaLevel = MsaaQuality._8x,
                RenderScale = 1.5f,
                PostProcessingEnabled = true
            };

            Assert.AreEqual(120, settings.TargetFrameRate);
            Assert.AreEqual(URPShadowQuality.SoftShadows, settings.ShadowQuality);
            Assert.AreEqual(MsaaQuality._8x, settings.MsaaLevel);
            Assert.AreEqual(1.5f, settings.RenderScale, 0.001f);
            Assert.IsTrue(settings.PostProcessingEnabled);
        }

        #endregion
    }
}
