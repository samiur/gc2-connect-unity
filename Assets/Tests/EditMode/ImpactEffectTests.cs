// ABOUTME: Unit tests for ImpactEffect particle component.
// ABOUTME: Tests particle playback, velocity scaling, and quality tier support.

using NUnit.Framework;
using UnityEngine;
using OpenRange.Visualization;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class ImpactEffectTests
    {
        private GameObject _testObject;
        private ImpactEffect _effect;
        private ParticleSystem _particleSystem;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestImpactEffect");
            _effect = _testObject.AddComponent<ImpactEffect>();

            // Create particle system
            _particleSystem = _testObject.AddComponent<ParticleSystem>();

            // Configure particle system for testing
            var main = _particleSystem.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.1f;
            main.startLifetime = 0.1f;

            var emission = _particleSystem.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 30)
            });

            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // Wire up reference
            _effect.SetParticleSystem(_particleSystem);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
        }

        #region Initialization Tests

        [Test]
        public void ImpactEffect_InitializesWithDefaultValues()
        {
            Assert.IsFalse(_effect.IsPlaying);
            Assert.AreEqual(QualityTier.High, _effect.CurrentQualityTier);
        }

        [Test]
        public void ImpactEffect_HasParticleSystemReference()
        {
            Assert.IsNotNull(_effect.ParticleSystemRef);
            Assert.AreEqual(_particleSystem, _effect.ParticleSystemRef);
        }

        #endregion

        #region Play Tests

        [Test]
        public void Play_SetsIsPlayingToTrue()
        {
            _effect.Play();

            Assert.IsTrue(_effect.IsPlaying);
        }

        [Test]
        public void Play_StartsParticleSystem()
        {
            _effect.Play();

            Assert.IsTrue(_particleSystem.isPlaying);
        }

        [Test]
        public void PlayAtPosition_MovesEffectToPosition()
        {
            Vector3 targetPos = new Vector3(10f, 0f, 20f);

            _effect.PlayAtPosition(targetPos);

            Assert.AreEqual(targetPos, _testObject.transform.position);
        }

        [Test]
        public void Stop_SetsIsPlayingToFalse()
        {
            _effect.Play();
            _effect.Stop();

            Assert.IsFalse(_effect.IsPlaying);
        }

        [Test]
        public void Stop_StopsParticleSystem()
        {
            _effect.Play();
            _effect.Stop();

            Assert.IsFalse(_particleSystem.isPlaying);
        }

        #endregion

        #region Quality Tier Tests

        [Test]
        public void SetQualityTier_UpdatesCurrentQualityTier()
        {
            _effect.SetQualityTier(QualityTier.Low);

            Assert.AreEqual(QualityTier.Low, _effect.CurrentQualityTier);
        }

        [Test]
        public void Play_WithHighQuality_Uses30Particles()
        {
            _effect.SetQualityTier(QualityTier.High);
            _effect.Play();

            Assert.AreEqual(30, _effect.GetCurrentParticleCount());
        }

        [Test]
        public void Play_WithMediumQuality_Uses20Particles()
        {
            _effect.SetQualityTier(QualityTier.Medium);
            _effect.Play();

            Assert.AreEqual(20, _effect.GetCurrentParticleCount());
        }

        [Test]
        public void Play_WithLowQuality_Uses10Particles()
        {
            _effect.SetQualityTier(QualityTier.Low);
            _effect.Play();

            Assert.AreEqual(10, _effect.GetCurrentParticleCount());
        }

        #endregion

        #region Velocity Scaling Tests

        [Test]
        public void Play_WithHighVelocity_IncreasesParticleCount()
        {
            _effect.SetQualityTier(QualityTier.High);
            _effect.SetVelocityRange(5f, 50f);

            // Play with max velocity
            _effect.Play(50f);

            // Should be scaled up (30 * 1.5 = 45)
            Assert.Greater(_effect.GetCurrentParticleCount(), 30);
        }

        [Test]
        public void Play_WithLowVelocity_DecreasesParticleCount()
        {
            _effect.SetQualityTier(QualityTier.High);
            _effect.SetVelocityRange(5f, 50f);

            // Play with min velocity
            _effect.Play(5f);

            // Should be scaled down (30 * 0.5 = 15)
            Assert.Less(_effect.GetCurrentParticleCount(), 30);
        }

        [Test]
        public void Play_WithMidVelocity_UsesNominalParticleCount()
        {
            _effect.SetQualityTier(QualityTier.High);
            _effect.SetVelocityRange(5f, 50f);

            // Play with mid velocity
            float midVelocity = (5f + 50f) / 2f;
            _effect.Play(midVelocity);

            // Should be close to base count (30 * 1.0 = 30)
            int count = _effect.GetCurrentParticleCount();
            Assert.GreaterOrEqual(count, 25);
            Assert.LessOrEqual(count, 35);
        }

        #endregion

        #region Configuration Tests

        [Test]
        public void SetAutoDestroy_CanBeToggled()
        {
            Assert.DoesNotThrow(() => _effect.SetAutoDestroy(true));
            Assert.DoesNotThrow(() => _effect.SetAutoDestroy(false));
        }

        [Test]
        public void SetReturnToPool_CanBeToggled()
        {
            Assert.DoesNotThrow(() => _effect.SetReturnToPool(true));
            Assert.DoesNotThrow(() => _effect.SetReturnToPool(false));
        }

        [Test]
        public void SetVelocityRange_AcceptsValidRange()
        {
            Assert.DoesNotThrow(() => _effect.SetVelocityRange(10f, 100f));
        }

        #endregion

        #region Null Safety Tests

        [Test]
        public void Play_WithNullParticleSystem_LogsWarning()
        {
            _effect.SetParticleSystem(null);

            LogAssert.Expect(LogType.Warning, "ImpactEffect: No particle system assigned");
            _effect.Play();
        }

        [Test]
        public void Stop_WithNullParticleSystem_DoesNotThrow()
        {
            _effect.SetParticleSystem(null);

            Assert.DoesNotThrow(() => _effect.Stop());
        }

        [Test]
        public void GetCurrentParticleCount_WithNullSystem_ReturnsZero()
        {
            _effect.SetParticleSystem(null);

            Assert.AreEqual(0, _effect.GetCurrentParticleCount());
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnEffectComplete_CanBeSubscribed()
        {
            bool eventCalled = false;
            _effect.OnEffectComplete += (e) => eventCalled = true;

            // Event is called when particles complete - we can't easily test this
            // in EditMode without PlayMode coroutine support
            Assert.IsFalse(eventCalled); // Just verify no exception
        }

        #endregion
    }
}
