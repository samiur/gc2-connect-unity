// ABOUTME: Unit tests for EffectsManager singleton and object pooling.
// ABOUTME: Tests pool management, event subscription, and quality tier propagation.

using NUnit.Framework;
using UnityEngine;
using OpenRange.Visualization;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class EffectsManagerTests
    {
        private GameObject _testObject;
        private EffectsManager _manager;
        private GameObject _markerPrefab;
        private GameObject _effectPrefab;
        private LandingMarker _markerComponent;
        private ImpactEffect _effectComponent;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestEffectsManager");
            _manager = _testObject.AddComponent<EffectsManager>();

            // Create marker prefab
            _markerPrefab = new GameObject("MarkerPrefab");
            _markerComponent = _markerPrefab.AddComponent<LandingMarker>();

            // Create effect prefab
            _effectPrefab = new GameObject("EffectPrefab");
            _effectComponent = _effectPrefab.AddComponent<ImpactEffect>();
            var ps = _effectPrefab.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.1f;
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 10) });
            _effectComponent.SetParticleSystem(ps);

            // Wire up prefabs
            _manager.SetLandingMarkerPrefab(_markerComponent);
            _manager.SetImpactEffectPrefab(_effectComponent);

            // Disable auto-spawn for controlled testing
            _manager.SetAutoSpawnOnLanding(false);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
            if (_markerPrefab != null)
            {
                Object.DestroyImmediate(_markerPrefab);
            }
            if (_effectPrefab != null)
            {
                Object.DestroyImmediate(_effectPrefab);
            }
        }

        #region Singleton Tests

        [Test]
        public void EffectsManager_HasSingletonInstance()
        {
            // Force Awake to run
            _manager.enabled = false;
            _manager.enabled = true;

            Assert.IsNotNull(EffectsManager.Instance);
        }

        [Test]
        public void EffectsManager_InitializesWithDefaultValues()
        {
            Assert.AreEqual(QualityTier.High, _manager.CurrentQualityTier);
            Assert.AreEqual(0, _manager.ActiveMarkerCount);
            Assert.AreEqual(0, _manager.ActiveEffectCount);
        }

        #endregion

        #region Marker Spawning Tests

        [Test]
        public void SpawnLandingMarker_ReturnsMarkerInstance()
        {
            var marker = _manager.SpawnLandingMarker(Vector3.zero, 150f, 160f);

            Assert.IsNotNull(marker);
        }

        [Test]
        public void SpawnLandingMarker_IncrementsActiveMarkerCount()
        {
            _manager.SpawnLandingMarker(Vector3.zero, 150f, 160f);

            Assert.AreEqual(1, _manager.ActiveMarkerCount);
        }

        [Test]
        public void SpawnLandingMarker_PositionsMarkerCorrectly()
        {
            Vector3 position = new Vector3(100f, 0f, 50f);
            var marker = _manager.SpawnLandingMarker(position, 150f, 160f);

            Assert.AreEqual(position.x, marker.transform.position.x, 0.1f);
            Assert.AreEqual(position.z, marker.transform.position.z, 0.1f);
        }

        [Test]
        public void SpawnLandingMarker_WithNoPrefab_ReturnsNull()
        {
            _manager.SetLandingMarkerPrefab(null);

            var marker = _manager.SpawnLandingMarker(Vector3.zero, 150f, 160f);

            Assert.IsNull(marker);
        }

        [Test]
        public void SpawnLandingMarker_FiresOnMarkerSpawnedEvent()
        {
            LandingMarker spawnedMarker = null;
            _manager.OnMarkerSpawned += (m) => spawnedMarker = m;

            var marker = _manager.SpawnLandingMarker(Vector3.zero, 150f, 160f);

            Assert.IsNotNull(spawnedMarker);
            Assert.AreEqual(marker, spawnedMarker);
        }

        [Test]
        public void SpawnLandingMarker_SetsCarryDistance()
        {
            var marker = _manager.SpawnLandingMarker(Vector3.zero, 175.5f, 182.3f);

            Assert.AreEqual(175.5f, marker.CarryDistance);
        }

        [Test]
        public void SpawnLandingMarker_SetsTotalDistance()
        {
            var marker = _manager.SpawnLandingMarker(Vector3.zero, 175.5f, 182.3f);

            Assert.AreEqual(182.3f, marker.TotalDistance);
        }

        #endregion

        #region Effect Spawning Tests

        [Test]
        public void SpawnImpactEffect_ReturnsEffectInstance()
        {
            var effect = _manager.SpawnImpactEffect(Vector3.zero);

            Assert.IsNotNull(effect);
        }

        [Test]
        public void SpawnImpactEffect_IncrementsActiveEffectCount()
        {
            _manager.SpawnImpactEffect(Vector3.zero);

            Assert.AreEqual(1, _manager.ActiveEffectCount);
        }

        [Test]
        public void SpawnImpactEffect_PositionsEffectCorrectly()
        {
            Vector3 position = new Vector3(50f, 0f, 25f);
            var effect = _manager.SpawnImpactEffect(position);

            Assert.AreEqual(position, effect.transform.position);
        }

        [Test]
        public void SpawnImpactEffect_WithNoPrefab_ReturnsNull()
        {
            _manager.SetImpactEffectPrefab(null);

            var effect = _manager.SpawnImpactEffect(Vector3.zero);

            Assert.IsNull(effect);
        }

        [Test]
        public void SpawnImpactEffect_FiresOnEffectSpawnedEvent()
        {
            ImpactEffect spawnedEffect = null;
            _manager.OnEffectSpawned += (e) => spawnedEffect = e;

            var effect = _manager.SpawnImpactEffect(Vector3.zero);

            Assert.IsNotNull(spawnedEffect);
            Assert.AreEqual(effect, spawnedEffect);
        }

        [Test]
        public void SpawnBounceEffect_ReturnsEffect()
        {
            var effect = _manager.SpawnBounceEffect(Vector3.zero);

            Assert.IsNotNull(effect);
        }

        #endregion

        #region Clear Tests

        [Test]
        public void ClearAllMarkers_RemovesAllActiveMarkers()
        {
            _manager.SpawnLandingMarker(Vector3.zero, 150f, 160f);
            _manager.SpawnLandingMarker(Vector3.one, 200f, 210f);

            _manager.ClearAllMarkers();

            Assert.AreEqual(0, _manager.ActiveMarkerCount);
        }

        [Test]
        public void ClearAllEffects_RemovesAllActiveEffects()
        {
            _manager.SpawnImpactEffect(Vector3.zero);
            _manager.SpawnImpactEffect(Vector3.one);

            _manager.ClearAllEffects();

            Assert.AreEqual(0, _manager.ActiveEffectCount);
        }

        [Test]
        public void ClearAll_RemovesAllMarkersAndEffects()
        {
            _manager.SpawnLandingMarker(Vector3.zero, 150f, 160f);
            _manager.SpawnImpactEffect(Vector3.one);

            _manager.ClearAll();

            Assert.AreEqual(0, _manager.ActiveMarkerCount);
            Assert.AreEqual(0, _manager.ActiveEffectCount);
        }

        #endregion

        #region Quality Tier Tests

        [Test]
        public void SetQualityTier_UpdatesCurrentQualityTier()
        {
            _manager.SetQualityTier(QualityTier.Low);

            Assert.AreEqual(QualityTier.Low, _manager.CurrentQualityTier);
        }

        [Test]
        public void SetQualityTier_PropagatestoActiveMarkers()
        {
            var marker = _manager.SpawnLandingMarker(Vector3.zero, 150f, 160f);

            _manager.SetQualityTier(QualityTier.Low);

            Assert.AreEqual(QualityTier.Low, marker.CurrentQualityTier);
        }

        [Test]
        public void SetQualityTier_PropagatesToActiveEffects()
        {
            var effect = _manager.SpawnImpactEffect(Vector3.zero);

            _manager.SetQualityTier(QualityTier.Low);

            Assert.AreEqual(QualityTier.Low, effect.CurrentQualityTier);
        }

        [Test]
        public void SpawnLandingMarker_UsesCurrentQualityTier()
        {
            _manager.SetQualityTier(QualityTier.Medium);

            var marker = _manager.SpawnLandingMarker(Vector3.zero, 150f, 160f);

            Assert.AreEqual(QualityTier.Medium, marker.CurrentQualityTier);
        }

        [Test]
        public void SpawnImpactEffect_UsesCurrentQualityTier()
        {
            _manager.SetQualityTier(QualityTier.Medium);

            var effect = _manager.SpawnImpactEffect(Vector3.zero);

            Assert.AreEqual(QualityTier.Medium, effect.CurrentQualityTier);
        }

        #endregion

        #region Configuration Tests

        [Test]
        public void SetBallController_AcceptsNull()
        {
            Assert.DoesNotThrow(() => _manager.SetBallController(null));
        }

        [Test]
        public void SetAutoSpawnOnLanding_CanBeToggled()
        {
            Assert.DoesNotThrow(() => _manager.SetAutoSpawnOnLanding(true));
            Assert.DoesNotThrow(() => _manager.SetAutoSpawnOnLanding(false));
        }

        [Test]
        public void SetSpawnMarkerOnLanding_CanBeToggled()
        {
            Assert.DoesNotThrow(() => _manager.SetSpawnMarkerOnLanding(true));
            Assert.DoesNotThrow(() => _manager.SetSpawnMarkerOnLanding(false));
        }

        [Test]
        public void SetSpawnEffectOnLanding_CanBeToggled()
        {
            Assert.DoesNotThrow(() => _manager.SetSpawnEffectOnLanding(true));
            Assert.DoesNotThrow(() => _manager.SetSpawnEffectOnLanding(false));
        }

        #endregion

        #region List Access Tests

        [Test]
        public void GetActiveMarkers_ReturnsCopyOfList()
        {
            _manager.SpawnLandingMarker(Vector3.zero, 150f, 160f);

            var markers = _manager.GetActiveMarkers();
            markers.Clear(); // Modify the copy

            Assert.AreEqual(1, _manager.ActiveMarkerCount); // Original unchanged
        }

        [Test]
        public void GetActiveEffects_ReturnsCopyOfList()
        {
            _manager.SpawnImpactEffect(Vector3.zero);

            var effects = _manager.GetActiveEffects();
            effects.Clear(); // Modify the copy

            Assert.AreEqual(1, _manager.ActiveEffectCount); // Original unchanged
        }

        #endregion

        #region Multiple Spawn Tests

        [Test]
        public void SpawnLandingMarker_CanSpawnMultiple()
        {
            _manager.SpawnLandingMarker(Vector3.zero, 150f, 160f);
            _manager.SpawnLandingMarker(Vector3.one, 200f, 210f);
            _manager.SpawnLandingMarker(Vector3.forward, 175f, 180f);

            Assert.AreEqual(3, _manager.ActiveMarkerCount);
        }

        [Test]
        public void SpawnImpactEffect_CanSpawnMultiple()
        {
            _manager.SpawnImpactEffect(Vector3.zero);
            _manager.SpawnImpactEffect(Vector3.one);
            _manager.SpawnImpactEffect(Vector3.forward);

            Assert.AreEqual(3, _manager.ActiveEffectCount);
        }

        #endregion
    }
}
