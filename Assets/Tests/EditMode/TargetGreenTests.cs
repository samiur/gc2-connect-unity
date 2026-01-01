// ABOUTME: Unit tests for TargetGreen component.
// ABOUTME: Tests size settings, highlighting, quality tier adjustments, and ball landing detection.

using NUnit.Framework;
using UnityEngine;
using OpenRange.Visualization;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class TargetGreenTests
    {
        private GameObject _testObject;
        private TargetGreen _green;
        private GameObject _surfaceObject;
        private GameObject _flagPoleObject;
        private GameObject _flagObject;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestTargetGreen");
            _green = _testObject.AddComponent<TargetGreen>();

            // Create green surface
            _surfaceObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _surfaceObject.name = "GreenSurface";
            _surfaceObject.transform.SetParent(_testObject.transform);

            // Create flag pole
            _flagPoleObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _flagPoleObject.name = "FlagPole";
            _flagPoleObject.transform.SetParent(_testObject.transform);

            // Create flag
            _flagObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _flagObject.name = "Flag";
            _flagObject.transform.SetParent(_flagPoleObject.transform);

            // Wire up references
            _green.SetGreenSurface(_surfaceObject.transform);
            _green.SetFlagPole(_flagPoleObject.transform);
            _green.SetFlag(_flagObject.transform);
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
        public void TargetGreen_InitializesWithDefaultValues()
        {
            Assert.AreEqual(TargetGreenSize.Medium, _green.Size);
            Assert.AreEqual(QualityTier.High, _green.CurrentQualityTier);
            Assert.IsFalse(_green.IsHighlighted);
        }

        [Test]
        public void TargetGreen_GreenSurfaceIsSet()
        {
            Assert.IsNotNull(_green.GreenSurface);
            Assert.AreEqual(_surfaceObject.transform, _green.GreenSurface);
        }

        [Test]
        public void TargetGreen_FlagPoleIsSet()
        {
            Assert.IsNotNull(_green.FlagPole);
            Assert.AreEqual(_flagPoleObject.transform, _green.FlagPole);
        }

        #endregion

        #region Size Tests

        [Test]
        public void SetSize_Small_UpdatesSize()
        {
            _green.SetSize(TargetGreenSize.Small);

            Assert.AreEqual(TargetGreenSize.Small, _green.Size);
        }

        [Test]
        public void SetSize_Medium_UpdatesSize()
        {
            _green.SetSize(TargetGreenSize.Medium);

            Assert.AreEqual(TargetGreenSize.Medium, _green.Size);
        }

        [Test]
        public void SetSize_Large_UpdatesSize()
        {
            _green.SetSize(TargetGreenSize.Large);

            Assert.AreEqual(TargetGreenSize.Large, _green.Size);
        }

        [Test]
        public void Diameter_Small_Returns10Meters()
        {
            _green.SetSize(TargetGreenSize.Small);

            Assert.AreEqual(10f, _green.Diameter);
        }

        [Test]
        public void Diameter_Medium_Returns15Meters()
        {
            _green.SetSize(TargetGreenSize.Medium);

            Assert.AreEqual(15f, _green.Diameter);
        }

        [Test]
        public void Diameter_Large_Returns20Meters()
        {
            _green.SetSize(TargetGreenSize.Large);

            Assert.AreEqual(20f, _green.Diameter);
        }

        #endregion

        #region Quality Tier Tests

        [Test]
        public void SetQualityTier_UpdatesCurrentQualityTier()
        {
            _green.SetQualityTier(QualityTier.Low);

            Assert.AreEqual(QualityTier.Low, _green.CurrentQualityTier);
        }

        [Test]
        public void SetQualityTier_Low_HidesFlagPole()
        {
            _green.SetQualityTier(QualityTier.Low);

            Assert.IsFalse(_flagPoleObject.activeSelf);
        }

        [Test]
        public void SetQualityTier_High_ShowsFlagPole()
        {
            _green.SetQualityTier(QualityTier.High);

            Assert.IsTrue(_flagPoleObject.activeSelf);
        }

        [Test]
        public void SetQualityTier_Medium_ShowsFlagPole()
        {
            _green.SetQualityTier(QualityTier.Medium);

            Assert.IsTrue(_flagPoleObject.activeSelf);
        }

        #endregion

        #region Highlight Tests

        [Test]
        public void Highlight_SetsIsHighlightedToTrue()
        {
            _green.Highlight(5f);

            Assert.IsTrue(_green.IsHighlighted);
        }

        [Test]
        public void EndHighlight_SetsIsHighlightedToFalse()
        {
            _green.Highlight(5f);
            _green.EndHighlight();

            Assert.IsFalse(_green.IsHighlighted);
        }

        [Test]
        public void Highlight_FiresEvent_WhenBallLands()
        {
            bool eventFired = false;
            _green.OnBallLanded += () => eventFired = true;

            // Position ball at center of green
            Vector3 centerPosition = _testObject.transform.position;
            _green.NotifyBallLanded(centerPosition);

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void EndHighlight_FiresOnHighlightEndedEvent()
        {
            bool eventFired = false;
            _green.OnHighlightEnded += () => eventFired = true;

            _green.Highlight(1f);
            _green.EndHighlight();

            Assert.IsTrue(eventFired);
        }

        #endregion

        #region Position Detection Tests

        [Test]
        public void IsPositionOnGreen_CenterPosition_ReturnsTrue()
        {
            _green.SetSize(TargetGreenSize.Medium);
            Vector3 centerPos = _testObject.transform.position;

            Assert.IsTrue(_green.IsPositionOnGreen(centerPos));
        }

        [Test]
        public void IsPositionOnGreen_EdgePosition_ReturnsTrue()
        {
            _green.SetSize(TargetGreenSize.Medium); // 15m diameter, 7.5m radius
            Vector3 edgePos = _testObject.transform.position + new Vector3(7f, 0f, 0f);

            Assert.IsTrue(_green.IsPositionOnGreen(edgePos));
        }

        [Test]
        public void IsPositionOnGreen_OutsidePosition_ReturnsFalse()
        {
            _green.SetSize(TargetGreenSize.Medium); // 15m diameter, 7.5m radius
            Vector3 outsidePos = _testObject.transform.position + new Vector3(10f, 0f, 0f);

            Assert.IsFalse(_green.IsPositionOnGreen(outsidePos));
        }

        [Test]
        public void IsPositionOnGreen_IgnoresYCoordinate()
        {
            _green.SetSize(TargetGreenSize.Medium);
            Vector3 elevatedPos = _testObject.transform.position + new Vector3(0f, 10f, 0f);

            Assert.IsTrue(_green.IsPositionOnGreen(elevatedPos));
        }

        [Test]
        public void NotifyBallLanded_InsideGreen_TriggersHighlight()
        {
            _green.SetSize(TargetGreenSize.Medium);
            Vector3 centerPos = _testObject.transform.position;

            _green.NotifyBallLanded(centerPos);

            Assert.IsTrue(_green.IsHighlighted);
        }

        [Test]
        public void NotifyBallLanded_OutsideGreen_DoesNotHighlight()
        {
            _green.SetSize(TargetGreenSize.Medium);
            Vector3 outsidePos = _testObject.transform.position + new Vector3(100f, 0f, 0f);

            _green.NotifyBallLanded(outsidePos);

            Assert.IsFalse(_green.IsHighlighted);
        }

        #endregion

        #region Visibility Tests

        [Test]
        public void Show_ActivatesGameObject()
        {
            _testObject.SetActive(false);
            _green.Show();

            Assert.IsTrue(_testObject.activeSelf);
        }

        [Test]
        public void Hide_DeactivatesGameObject()
        {
            _green.Hide();

            Assert.IsFalse(_testObject.activeSelf);
        }

        #endregion

        #region Color Tests

        [Test]
        public void SetNormalColor_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _green.SetNormalColor(Color.green));
        }

        [Test]
        public void SetHighlightColor_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _green.SetHighlightColor(Color.yellow));
        }

        #endregion

        #region Null Safety Tests

        [Test]
        public void SetSize_WithNullSurface_DoesNotThrow()
        {
            _green.SetGreenSurface(null);

            Assert.DoesNotThrow(() => _green.SetSize(TargetGreenSize.Large));
        }

        [Test]
        public void SetQualityTier_WithNullFlagPole_DoesNotThrow()
        {
            _green.SetFlagPole(null);

            Assert.DoesNotThrow(() => _green.SetQualityTier(QualityTier.Low));
        }

        [Test]
        public void Highlight_WithNullRenderer_DoesNotThrow()
        {
            _green.SetGreenSurface(null);

            Assert.DoesNotThrow(() => _green.Highlight(1f));
        }

        #endregion
    }
}
