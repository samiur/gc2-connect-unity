// ABOUTME: Unit tests for EnvironmentManager singleton.
// ABOUTME: Tests quality tier handling, unit conversion, and environment management.

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using OpenRange.Visualization;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class EnvironmentManagerTests
    {
        private GameObject _testObject;
        private EnvironmentManager _manager;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestEnvironmentManager");
            _manager = _testObject.AddComponent<EnvironmentManager>();
            _manager.ForceInitializeSingleton();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
            // Clear static singleton reference for test isolation
            ClearSingleton();
        }

        /// <summary>
        /// Clear the static singleton reference using reflection (for test isolation).
        /// </summary>
        private void ClearSingleton()
        {
            var field = typeof(EnvironmentManager).GetField("_instance",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(null, null);
        }

        #region Initialization Tests

        [Test]
        public void EnvironmentManager_InitializesWithDefaultValues()
        {
            Assert.AreEqual(QualityTier.High, _manager.CurrentQualityTier);
            Assert.IsFalse(_manager.IsInitialized);
        }

        [Test]
        public void Initialize_SetsIsInitializedToTrue()
        {
            _manager.Initialize();

            Assert.IsTrue(_manager.IsInitialized);
        }

        [Test]
        public void Initialize_FiresOnEnvironmentReadyEvent()
        {
            bool eventFired = false;
            _manager.OnEnvironmentReady += () => eventFired = true;

            _manager.Initialize();

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void Initialize_CalledTwice_OnlyInitializesOnce()
        {
            int callCount = 0;
            _manager.OnEnvironmentReady += () => callCount++;

            _manager.Initialize();
            _manager.Initialize();

            Assert.AreEqual(1, callCount);
        }

        #endregion

        #region Singleton Tests

        [Test]
        public void Instance_ReturnsSingletonInstance()
        {
            Assert.AreEqual(_manager, EnvironmentManager.Instance);
        }

        [Test]
        public void ForceInitializeSingleton_SetsInstance()
        {
            var newManagerGo = new GameObject("NewManager");
            var newManager = newManagerGo.AddComponent<EnvironmentManager>();
            newManager.ForceInitializeSingleton();

            // The first one should still be the instance since it was initialized first
            Assert.AreEqual(_manager, EnvironmentManager.Instance);

            // The duplicate manager's gameObject may have been destroyed by the singleton logic
            // Only destroy if it still exists
            if (newManagerGo != null)
            {
                Object.DestroyImmediate(newManagerGo);
            }
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
        public void SetQualityTier_FiresOnQualityTierChangedEvent()
        {
            QualityTier receivedTier = QualityTier.High;
            _manager.OnQualityTierChanged += (tier) => receivedTier = tier;

            _manager.SetQualityTier(QualityTier.Low);

            Assert.AreEqual(QualityTier.Low, receivedTier);
        }

        [Test]
        public void SetQualityTier_SameValue_DoesNotFireEvent()
        {
            _manager.SetQualityTier(QualityTier.High); // Set initial value
            int callCount = 0;
            _manager.OnQualityTierChanged += (tier) => callCount++;

            _manager.SetQualityTier(QualityTier.High);

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void GetDrawDistanceForTier_High_Returns500()
        {
            float distance = _manager.GetDrawDistanceForTier(QualityTier.High);

            Assert.AreEqual(500f, distance);
        }

        [Test]
        public void GetDrawDistanceForTier_Medium_Returns350()
        {
            float distance = _manager.GetDrawDistanceForTier(QualityTier.Medium);

            Assert.AreEqual(350f, distance);
        }

        [Test]
        public void GetDrawDistanceForTier_Low_Returns200()
        {
            float distance = _manager.GetDrawDistanceForTier(QualityTier.Low);

            Assert.AreEqual(200f, distance);
        }

        [Test]
        public void AreReflectionsEnabledForTier_High_ReturnsTrue()
        {
            bool enabled = _manager.AreReflectionsEnabledForTier(QualityTier.High);

            Assert.IsTrue(enabled);
        }

        [Test]
        public void AreReflectionsEnabledForTier_Low_ReturnsFalse()
        {
            bool enabled = _manager.AreReflectionsEnabledForTier(QualityTier.Low);

            Assert.IsFalse(enabled);
        }

        [Test]
        public void CurrentDrawDistance_ReflectsCurrentTier()
        {
            _manager.SetQualityTier(QualityTier.Medium);

            Assert.AreEqual(350f, _manager.CurrentDrawDistance);
        }

        [Test]
        public void ReflectionsEnabled_ReflectsCurrentTier()
        {
            _manager.SetQualityTier(QualityTier.Low);

            Assert.IsFalse(_manager.ReflectionsEnabled);
        }

        #endregion

        #region Unit Conversion Tests

        [Test]
        public void YardsToMeters_HasCorrectValue()
        {
            Assert.AreEqual(0.9144f, EnvironmentManager.YardsToMeters, 0.0001f);
        }

        [Test]
        public void ConvertYardsToUnits_100Yards_Returns91Point44Meters()
        {
            float meters = EnvironmentManager.ConvertYardsToUnits(100f);

            Assert.AreEqual(91.44f, meters, 0.01f);
        }

        [Test]
        public void ConvertYardsToUnits_300Yards_Returns274Point32Meters()
        {
            float meters = EnvironmentManager.ConvertYardsToUnits(300f);

            Assert.AreEqual(274.32f, meters, 0.01f);
        }

        [Test]
        public void ConvertUnitsToYards_91Point44Meters_Returns100Yards()
        {
            float yards = EnvironmentManager.ConvertUnitsToYards(91.44f);

            Assert.AreEqual(100f, yards, 0.01f);
        }

        [Test]
        public void ConvertYardsToUnits_AndBack_ReturnsOriginal()
        {
            float originalYards = 175f;
            float meters = EnvironmentManager.ConvertYardsToUnits(originalYards);
            float convertedYards = EnvironmentManager.ConvertUnitsToYards(meters);

            Assert.AreEqual(originalYards, convertedYards, 0.001f);
        }

        #endregion

        #region Standard Marker Distances Tests

        [Test]
        public void StandardMarkerDistances_HasCorrectCount()
        {
            Assert.AreEqual(6, EnvironmentManager.StandardMarkerDistances.Length);
        }

        [Test]
        public void StandardMarkerDistances_StartsAt50()
        {
            Assert.AreEqual(50, EnvironmentManager.StandardMarkerDistances[0]);
        }

        [Test]
        public void StandardMarkerDistances_EndsAt300()
        {
            int lastIndex = EnvironmentManager.StandardMarkerDistances.Length - 1;
            Assert.AreEqual(300, EnvironmentManager.StandardMarkerDistances[lastIndex]);
        }

        [Test]
        public void StandardMarkerDistances_Has50YardIntervals()
        {
            int[] expected = { 50, 100, 150, 200, 250, 300 };
            Assert.AreEqual(expected, EnvironmentManager.StandardMarkerDistances);
        }

        #endregion

        #region Marker Management Tests

        [Test]
        public void DistanceMarkers_InitializesEmpty()
        {
            Assert.AreEqual(0, _manager.DistanceMarkers.Count);
        }

        [Test]
        public void TargetGreens_InitializesEmpty()
        {
            Assert.AreEqual(0, _manager.TargetGreens.Count);
        }

        [Test]
        public void TeeMat_InitializesAsNull()
        {
            Assert.IsNull(_manager.TeeMat);
        }

        [Test]
        public void GetMarkerAtDistance_WithNoMarkers_ReturnsNull()
        {
            var marker = _manager.GetMarkerAtDistance(100);

            Assert.IsNull(marker);
        }

        [Test]
        public void SpawnDistanceMarker_WithoutPrefab_LogsWarning()
        {
            // No prefab set
            LogAssert.Expect(LogType.Warning, "EnvironmentManager: Distance marker prefab not assigned");

            _manager.SpawnDistanceMarker(100);
        }

        [Test]
        public void SpawnTargetGreen_WithoutPrefab_LogsWarning()
        {
            // No prefab set
            LogAssert.Expect(LogType.Warning, "EnvironmentManager: Target green prefab not assigned");

            _manager.SpawnTargetGreen(100);
        }

        [Test]
        public void SpawnTeeMat_WithoutPrefab_LogsWarning()
        {
            // No prefab set
            LogAssert.Expect(LogType.Warning, "EnvironmentManager: Tee mat prefab not assigned");

            _manager.SpawnTeeMat();
        }

        [Test]
        public void ClearDistanceMarkers_WithNoMarkers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _manager.ClearDistanceMarkers());
        }

        [Test]
        public void ClearTargetGreens_WithNoGreens_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _manager.ClearTargetGreens());
        }

        [Test]
        public void ClearAll_WithNoElements_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _manager.ClearAll());
        }

        #endregion

        #region Visibility Settings Tests

        [Test]
        public void SetShowDistanceMarkers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _manager.SetShowDistanceMarkers(false));
        }

        [Test]
        public void SetShowTargetGreens_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _manager.SetShowTargetGreens(false));
        }

        #endregion

        #region Prefab Setter Tests

        [Test]
        public void SetDistanceMarkerPrefab_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _manager.SetDistanceMarkerPrefab(null));
        }

        [Test]
        public void SetTargetGreenPrefab_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _manager.SetTargetGreenPrefab(null));
        }

        [Test]
        public void SetTeeMatPrefab_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _manager.SetTeeMatPrefab(null));
        }

        #endregion
    }
}
