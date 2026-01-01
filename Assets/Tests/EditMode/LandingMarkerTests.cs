// ABOUTME: Unit tests for LandingMarker visual component.
// ABOUTME: Tests positioning, distance display, fade animations, and quality tier support.

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TMPro;
using OpenRange.Visualization;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class LandingMarkerTests
    {
        private GameObject _testObject;
        private LandingMarker _marker;
        private GameObject _ringObject;
        private Renderer _ringRenderer;
        private TextMeshPro _carryText;
        private TextMeshPro _totalText;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestLandingMarker");
            _marker = _testObject.AddComponent<LandingMarker>();

            // Create ring object
            _ringObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _ringObject.name = "Ring";
            _ringObject.transform.SetParent(_testObject.transform);
            _ringRenderer = _ringObject.GetComponent<Renderer>();

            // Create carry distance text
            var carryTextGo = new GameObject("CarryDistanceText");
            carryTextGo.transform.SetParent(_testObject.transform);
            _carryText = carryTextGo.AddComponent<TextMeshPro>();

            // Create total distance text
            var totalTextGo = new GameObject("TotalDistanceText");
            totalTextGo.transform.SetParent(_testObject.transform);
            _totalText = totalTextGo.AddComponent<TextMeshPro>();

            // Wire up references
            _marker.SetRingTransform(_ringObject.transform);
            _marker.SetRingRenderer(_ringRenderer);
            _marker.SetCarryDistanceText(_carryText);
            _marker.SetTotalDistanceText(_totalText);
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
        public void LandingMarker_InitializesWithDefaultValues()
        {
            Assert.IsFalse(_marker.IsVisible);
            Assert.AreEqual(0f, _marker.CarryDistance);
            Assert.AreEqual(0f, _marker.TotalDistance);
            Assert.AreEqual(QualityTier.High, _marker.CurrentQualityTier);
        }

        [Test]
        public void LandingMarker_RingTransformIsSet()
        {
            Assert.IsNotNull(_marker.RingTransform);
            Assert.AreEqual(_ringObject.transform, _marker.RingTransform);
        }

        #endregion

        #region Show/Hide Tests

        [Test]
        public void Show_SetsVisibleToTrue()
        {
            _marker.SetAutoHideDuration(0f); // Disable auto-hide for test
            _marker.SetFadeInDuration(0f); // Instant fade

            _marker.Show(Vector3.zero, 150f, 160f, autoHide: false);

            Assert.IsTrue(_marker.IsVisible);
        }

        [Test]
        public void Show_SetsCarryDistance()
        {
            _marker.SetAutoHideDuration(0f);
            _marker.SetFadeInDuration(0f);

            _marker.Show(Vector3.zero, 175.5f, 182.3f, autoHide: false);

            Assert.AreEqual(175.5f, _marker.CarryDistance);
        }

        [Test]
        public void Show_SetsTotalDistance()
        {
            _marker.SetAutoHideDuration(0f);
            _marker.SetFadeInDuration(0f);

            _marker.Show(Vector3.zero, 175.5f, 182.3f, autoHide: false);

            Assert.AreEqual(182.3f, _marker.TotalDistance);
        }

        [Test]
        public void Show_PositionsMarkerAtSpecifiedLocation()
        {
            _marker.SetAutoHideDuration(0f);
            _marker.SetFadeInDuration(0f);
            _marker.SetHeightOffset(0.05f);

            Vector3 landingPos = new Vector3(100f, 0f, 50f);
            _marker.Show(landingPos, 150f, 160f, autoHide: false);

            Assert.AreEqual(landingPos.x, _testObject.transform.position.x, 0.001f);
            Assert.AreEqual(0.05f, _testObject.transform.position.y, 0.001f); // Height offset
            Assert.AreEqual(landingPos.z, _testObject.transform.position.z, 0.001f);
        }

        [Test]
        public void Hide_SetsVisibleToFalse()
        {
            _marker.SetAutoHideDuration(0f);
            _marker.SetFadeInDuration(0f);

            _marker.Show(Vector3.zero, 150f, 160f, autoHide: false);
            _marker.Hide();

            Assert.IsFalse(_marker.IsVisible);
        }

        [Test]
        public void Hide_FiresOnHiddenEvent()
        {
            bool eventFired = false;
            _marker.OnHidden += () => eventFired = true;

            _marker.SetAutoHideDuration(0f);
            _marker.SetFadeInDuration(0f);
            _marker.Show(Vector3.zero, 150f, 160f, autoHide: false);
            _marker.Hide();

            Assert.IsTrue(eventFired);
        }

        #endregion

        #region Distance Formatting Tests

        [Test]
        public void Show_FormatsCarryDistanceWithOneDecimal()
        {
            _marker.SetAutoHideDuration(0f);
            _marker.SetFadeInDuration(0f);
            _marker.SetDistanceFormat("{0:F1} yd");

            _marker.Show(Vector3.zero, 175.567f, 182.3f, autoHide: false);

            Assert.AreEqual("175.6 yd", _carryText.text);
        }

        [Test]
        public void Show_CanUseCustomDistanceFormat()
        {
            _marker.SetAutoHideDuration(0f);
            _marker.SetFadeInDuration(0f);
            _marker.SetDistanceFormat("{0:F0} yards");

            _marker.Show(Vector3.zero, 175.567f, 182.3f, autoHide: false);

            Assert.AreEqual("176 yards", _carryText.text);
        }

        #endregion

        #region Quality Tier Tests

        [Test]
        public void SetQualityTier_UpdatesCurrentQualityTier()
        {
            _marker.SetQualityTier(QualityTier.Low);

            Assert.AreEqual(QualityTier.Low, _marker.CurrentQualityTier);
        }

        [Test]
        public void SetQualityTier_ToLow_HidesTotalDistance()
        {
            _marker.SetShowTotalDistance(true);
            _marker.SetAutoHideDuration(0f);
            _marker.SetFadeInDuration(0f);

            _marker.Show(Vector3.zero, 150f, 160f, autoHide: false);
            _marker.SetQualityTier(QualityTier.Low);

            // On low quality, total distance should be hidden
            Assert.IsFalse(_totalText.gameObject.activeSelf);
        }

        [Test]
        public void SetQualityTier_ToHigh_ShowsTotalDistance()
        {
            _marker.SetShowTotalDistance(true);
            _marker.SetAutoHideDuration(0f);
            _marker.SetFadeInDuration(0f);

            _marker.SetQualityTier(QualityTier.High);
            _marker.Show(Vector3.zero, 150f, 160f, autoHide: false);

            Assert.IsTrue(_totalText.gameObject.activeSelf);
        }

        [Test]
        public void SetQualityTier_ToMedium_ShowsTotalDistance()
        {
            _marker.SetShowTotalDistance(true);
            _marker.SetAutoHideDuration(0f);
            _marker.SetFadeInDuration(0f);

            _marker.SetQualityTier(QualityTier.Medium);
            _marker.Show(Vector3.zero, 150f, 160f, autoHide: false);

            Assert.IsTrue(_totalText.gameObject.activeSelf);
        }

        #endregion

        #region Event Tests

        [Test]
        public void Show_FiresOnShownEvent()
        {
            bool eventFired = false;
            _marker.OnShown += () => eventFired = true;

            _marker.SetAutoHideDuration(0f);
            _marker.SetFadeInDuration(0f);
            _marker.Show(Vector3.zero, 150f, 160f, autoHide: false);

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void FadeOut_WithZeroDuration_HidesImmediately()
        {
            _marker.SetAutoHideDuration(0f);
            _marker.SetFadeInDuration(0f);

            _marker.Show(Vector3.zero, 150f, 160f, autoHide: false);
            _marker.FadeOut(0f);

            Assert.IsFalse(_marker.IsVisible);
        }

        #endregion

        #region Null Safety Tests

        [Test]
        public void Show_WithNullRingRenderer_DoesNotThrow()
        {
            _marker.SetRingRenderer(null);

            Assert.DoesNotThrow(() =>
            {
                _marker.SetFadeInDuration(0f);
                _marker.Show(Vector3.zero, 150f, 160f, autoHide: false);
            });
        }

        [Test]
        public void Show_WithNullCarryText_DoesNotThrow()
        {
            _marker.SetCarryDistanceText(null);

            Assert.DoesNotThrow(() =>
            {
                _marker.SetFadeInDuration(0f);
                _marker.Show(Vector3.zero, 150f, 160f, autoHide: false);
            });
        }

        [Test]
        public void Show_WithNullTotalText_DoesNotThrow()
        {
            _marker.SetTotalDistanceText(null);

            Assert.DoesNotThrow(() =>
            {
                _marker.SetFadeInDuration(0f);
                _marker.Show(Vector3.zero, 150f, 160f, autoHide: false);
            });
        }

        [Test]
        public void Hide_WhenNotVisible_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _marker.Hide());
        }

        [Test]
        public void FadeOut_WhenNotVisible_DoesNothing()
        {
            Assert.DoesNotThrow(() => _marker.FadeOut(1f));
            Assert.IsFalse(_marker.IsVisible);
        }

        #endregion

        #region Configuration Tests

        [Test]
        public void SetShowTotalDistance_False_HidesTotalText()
        {
            _marker.SetShowTotalDistance(false);
            _marker.SetAutoHideDuration(0f);
            _marker.SetFadeInDuration(0f);

            _marker.Show(Vector3.zero, 150f, 160f, autoHide: false);

            Assert.IsFalse(_totalText.gameObject.activeSelf);
        }

        [Test]
        public void SetHeightOffset_AffectsMarkerPosition()
        {
            _marker.SetHeightOffset(0.1f);
            _marker.SetAutoHideDuration(0f);
            _marker.SetFadeInDuration(0f);

            _marker.Show(new Vector3(0f, 0f, 0f), 150f, 160f, autoHide: false);

            Assert.AreEqual(0.1f, _testObject.transform.position.y, 0.001f);
        }

        #endregion
    }
}
