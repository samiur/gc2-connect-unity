// ABOUTME: Unit tests for DistanceMarker component.
// ABOUTME: Tests distance display, positioning, quality tier adjustments, and visibility.

using NUnit.Framework;
using UnityEngine;
using TMPro;
using OpenRange.Visualization;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class DistanceMarkerTests
    {
        private GameObject _testObject;
        private DistanceMarker _marker;
        private GameObject _postObject;
        private GameObject _signObject;
        private TextMeshPro _distanceText;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestDistanceMarker");
            _marker = _testObject.AddComponent<DistanceMarker>();

            // Create post
            _postObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _postObject.name = "Post";
            _postObject.transform.SetParent(_testObject.transform);

            // Create sign
            _signObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _signObject.name = "Sign";
            _signObject.transform.SetParent(_testObject.transform);

            // Create distance text
            var textGo = new GameObject("DistanceText");
            textGo.transform.SetParent(_testObject.transform);
            _distanceText = textGo.AddComponent<TextMeshPro>();

            // Wire up references
            _marker.SetPostTransform(_postObject.transform);
            _marker.SetSignTransform(_signObject.transform);
            _marker.SetDistanceText(_distanceText);
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
        public void DistanceMarker_InitializesWithDefaultValues()
        {
            Assert.IsTrue(_marker.IsVisible);
            Assert.AreEqual(QualityTier.High, _marker.CurrentQualityTier);
        }

        [Test]
        public void DistanceMarker_PostTransformIsSet()
        {
            Assert.IsNotNull(_marker.PostTransform);
            Assert.AreEqual(_postObject.transform, _marker.PostTransform);
        }

        [Test]
        public void DistanceMarker_SignTransformIsSet()
        {
            Assert.IsNotNull(_marker.SignTransform);
            Assert.AreEqual(_signObject.transform, _marker.SignTransform);
        }

        #endregion

        #region Distance Tests

        [Test]
        public void SetDistance_UpdatesDistanceValue()
        {
            _marker.SetDistance(150);

            Assert.AreEqual(150, _marker.Distance);
        }

        [Test]
        public void SetDistance_UpdatesDistanceText()
        {
            _marker.SetDistanceFormat("{0}");
            _marker.SetDistance(200);

            Assert.AreEqual("200", _distanceText.text);
        }

        [Test]
        public void SetDistance_FiresOnDistanceChangedEvent()
        {
            int receivedDistance = 0;
            _marker.OnDistanceChanged += (d) => receivedDistance = d;

            _marker.SetDistance(175);

            Assert.AreEqual(175, receivedDistance);
        }

        [Test]
        public void SetDistance_SameValue_DoesNotFireEvent()
        {
            _marker.SetDistance(100);
            int callCount = 0;
            _marker.OnDistanceChanged += (d) => callCount++;

            _marker.SetDistance(100);

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void SetDistanceFormat_UpdatesText()
        {
            _marker.SetDistance(150);
            _marker.SetDistanceFormat("{0} yards");

            Assert.AreEqual("150 yards", _distanceText.text);
        }

        [Test]
        public void SetUnitSuffix_AppendsToText()
        {
            _marker.SetDistanceFormat("{0}");
            _marker.SetUnitSuffix(" yd");
            _marker.SetDistance(200);

            Assert.AreEqual("200 yd", _distanceText.text);
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
        public void SetQualityTier_High_SetsStandardTextSize()
        {
            _marker.SetQualityTier(QualityTier.High);

            // Should use standard text size
            Assert.AreEqual(QualityTier.High, _marker.CurrentQualityTier);
        }

        [Test]
        public void SetQualityTier_Low_IncreasesTextSize()
        {
            float originalSize = _distanceText.fontSize;
            _marker.SetQualityTier(QualityTier.Low);

            // Low quality increases text size for readability
            Assert.AreEqual(QualityTier.Low, _marker.CurrentQualityTier);
        }

        #endregion

        #region Visibility Tests

        [Test]
        public void Show_SetsVisibleToTrue()
        {
            _marker.Hide();
            _marker.Show();

            Assert.IsTrue(_marker.IsVisible);
        }

        [Test]
        public void Hide_SetsVisibleToFalse()
        {
            _marker.Show();
            _marker.Hide();

            Assert.IsFalse(_marker.IsVisible);
        }

        [Test]
        public void Hide_HidesAllChildObjects()
        {
            _marker.Hide();

            Assert.IsFalse(_postObject.activeSelf);
            Assert.IsFalse(_signObject.activeSelf);
            Assert.IsFalse(_distanceText.gameObject.activeSelf);
        }

        [Test]
        public void Show_ShowsAllChildObjects()
        {
            _marker.Hide();
            _marker.Show();

            Assert.IsTrue(_postObject.activeSelf);
            Assert.IsTrue(_signObject.activeSelf);
            Assert.IsTrue(_distanceText.gameObject.activeSelf);
        }

        #endregion

        #region Null Safety Tests

        [Test]
        public void SetDistance_WithNullText_DoesNotThrow()
        {
            _marker.SetDistanceText(null);

            Assert.DoesNotThrow(() => _marker.SetDistance(150));
        }

        [Test]
        public void Hide_WithNullTransforms_DoesNotThrow()
        {
            _marker.SetPostTransform(null);
            _marker.SetSignTransform(null);
            _marker.SetDistanceText(null);

            Assert.DoesNotThrow(() => _marker.Hide());
        }

        [Test]
        public void Show_WithNullTransforms_DoesNotThrow()
        {
            _marker.SetPostTransform(null);
            _marker.SetSignTransform(null);
            _marker.SetDistanceText(null);

            Assert.DoesNotThrow(() => _marker.Show());
        }

        #endregion

        #region Position Tests

        [Test]
        public void Marker_CanBePositionedAt50Yards()
        {
            float expectedZ = 50 * EnvironmentManager.YardsToMeters;
            _testObject.transform.position = new Vector3(-5f, 0f, expectedZ);

            Assert.AreEqual(expectedZ, _testObject.transform.position.z, 0.01f);
        }

        [Test]
        public void Marker_CanBePositionedAt300Yards()
        {
            float expectedZ = 300 * EnvironmentManager.YardsToMeters;
            _testObject.transform.position = new Vector3(-5f, 0f, expectedZ);

            Assert.AreEqual(expectedZ, _testObject.transform.position.z, 0.01f);
        }

        #endregion
    }
}
