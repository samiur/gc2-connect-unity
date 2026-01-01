// ABOUTME: Unit tests for ResponsiveLayout component.
// ABOUTME: Tests screen category detection and layout change events.

using NUnit.Framework;
using OpenRange.UI;
using UnityEngine;

namespace OpenRange.Tests.EditMode
{
    /// <summary>
    /// Tests for ResponsiveLayout component.
    /// </summary>
    [TestFixture]
    public class ResponsiveLayoutTests
    {
        private GameObject _testObject;
        private ResponsiveLayout _responsiveLayout;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestResponsiveLayout");
            _responsiveLayout = _testObject.AddComponent<ResponsiveLayout>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
        }

        #region Screen Category Detection

        [Test]
        public void CalculateScreenCategory_BelowCompact_ReturnsCompact()
        {
            var category = ResponsiveLayout.CalculateScreenCategory(640);
            Assert.That(category, Is.EqualTo(ScreenCategory.Compact));
        }

        [Test]
        public void CalculateScreenCategory_AtCompact_ReturnsCompact()
        {
            // 800 is the boundary - below it is Compact
            var category = ResponsiveLayout.CalculateScreenCategory(799);
            Assert.That(category, Is.EqualTo(ScreenCategory.Compact));
        }

        [Test]
        public void CalculateScreenCategory_AtRegularBoundary_ReturnsRegular()
        {
            var category = ResponsiveLayout.CalculateScreenCategory(800);
            Assert.That(category, Is.EqualTo(ScreenCategory.Regular));
        }

        [Test]
        public void CalculateScreenCategory_InRegularRange_ReturnsRegular()
        {
            var category = ResponsiveLayout.CalculateScreenCategory(1000);
            Assert.That(category, Is.EqualTo(ScreenCategory.Regular));
        }

        [Test]
        public void CalculateScreenCategory_BelowLargeBoundary_ReturnsRegular()
        {
            var category = ResponsiveLayout.CalculateScreenCategory(1199);
            Assert.That(category, Is.EqualTo(ScreenCategory.Regular));
        }

        [Test]
        public void CalculateScreenCategory_AtLargeBoundary_ReturnsLarge()
        {
            var category = ResponsiveLayout.CalculateScreenCategory(1200);
            Assert.That(category, Is.EqualTo(ScreenCategory.Large));
        }

        [Test]
        public void CalculateScreenCategory_AboveLarge_ReturnsLarge()
        {
            var category = ResponsiveLayout.CalculateScreenCategory(1920);
            Assert.That(category, Is.EqualTo(ScreenCategory.Large));
        }

        [Test]
        public void CalculateScreenCategory_VeryWide_ReturnsLarge()
        {
            var category = ResponsiveLayout.CalculateScreenCategory(3840);
            Assert.That(category, Is.EqualTo(ScreenCategory.Large));
        }

        #endregion

        #region Component Properties

        [Test]
        public void GetScreenCategory_ReturnsCurrentCategory()
        {
            // This will use actual screen dimensions
            var category = _responsiveLayout.GetScreenCategory();
            Assert.That(category, Is.EqualTo(ScreenCategory.Compact)
                .Or.EqualTo(ScreenCategory.Regular)
                .Or.EqualTo(ScreenCategory.Large));
        }

        [Test]
        public void GetSafeArea_ReturnsRect()
        {
            var safeArea = _responsiveLayout.GetSafeArea();
            Assert.That(safeArea.width, Is.GreaterThan(0));
            Assert.That(safeArea.height, Is.GreaterThan(0));
        }

        [Test]
        public void ScreenWidth_ReturnsPositiveValue()
        {
            Assert.That(_responsiveLayout.ScreenWidth, Is.GreaterThan(0));
        }

        [Test]
        public void ScreenHeight_ReturnsPositiveValue()
        {
            Assert.That(_responsiveLayout.ScreenHeight, Is.GreaterThan(0));
        }

        [Test]
        public void SafeArea_ReturnsValidRect()
        {
            var safeArea = _responsiveLayout.SafeArea;
            Assert.That(safeArea.width, Is.GreaterThanOrEqualTo(0));
            Assert.That(safeArea.height, Is.GreaterThanOrEqualTo(0));
        }

        #endregion

        #region Orientation Detection

        [Test]
        public void IsPortrait_Or_IsLandscape_OneIsTrue()
        {
            // At least one should be true
            bool portrait = _responsiveLayout.IsPortrait();
            bool landscape = _responsiveLayout.IsLandscape();

            // Both can be true if width == height, but at least landscape should be
            // since width >= height makes landscape true
            Assert.That(portrait || landscape, Is.True);
        }

        [Test]
        public void IsLandscape_TrueWhenWidthGreaterThanHeight()
        {
            // Test static calculation
            bool landscape = Screen.width >= Screen.height;
            Assert.That(_responsiveLayout.IsLandscape(), Is.EqualTo(landscape));
        }

        #endregion

        #region Font Size Retrieval

        [Test]
        public void GetFontSize_ReturnsPositiveValue()
        {
            int fontSize = _responsiveLayout.GetFontSize(FontCategory.Normal);
            Assert.That(fontSize, Is.GreaterThan(0));
        }

        [Test]
        public void GetFontSize_Small_ReturnsSmallerThanNormal()
        {
            int small = _responsiveLayout.GetFontSize(FontCategory.Small);
            int normal = _responsiveLayout.GetFontSize(FontCategory.Normal);
            Assert.That(small, Is.LessThan(normal));
        }

        [Test]
        public void GetFontSize_DataValue_ReturnsLargestSize()
        {
            int normal = _responsiveLayout.GetFontSize(FontCategory.Normal);
            int dataValue = _responsiveLayout.GetFontSize(FontCategory.DataValue);
            Assert.That(dataValue, Is.GreaterThan(normal));
        }

        #endregion

        #region Diagonal Inches

        [Test]
        public void GetDiagonalInches_ReturnsValueOrNegativeOne()
        {
            float diagonal = _responsiveLayout.GetDiagonalInches();
            // Either returns a positive diagonal or -1 if DPI unavailable
            Assert.That(diagonal, Is.EqualTo(-1f).Or.GreaterThan(0f));
        }

        #endregion

        #region Force Update

        [Test]
        public void ForceLayoutUpdate_FiresEvent()
        {
            bool eventFired = false;
            _responsiveLayout.OnLayoutChanged += (category) => eventFired = true;

            _responsiveLayout.ForceLayoutUpdate();

            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void ForceLayoutUpdate_UpdatesSafeAreaEvent()
        {
            bool eventFired = false;
            _responsiveLayout.OnSafeAreaChanged += (rect) => eventFired = true;

            _responsiveLayout.ForceLayoutUpdate();

            Assert.That(eventFired, Is.True);
        }

        #endregion

        #region Events

        [Test]
        public void OnLayoutChanged_CanSubscribe()
        {
            ScreenCategory receivedCategory = ScreenCategory.Compact;
            _responsiveLayout.OnLayoutChanged += (category) => receivedCategory = category;

            _responsiveLayout.ForceLayoutUpdate();

            // Just verify it doesn't throw
            Assert.Pass();
        }

        [Test]
        public void OnOrientationChanged_CanSubscribe()
        {
            _responsiveLayout.OnOrientationChanged += (orientation) => { };
            // Just verify subscription works
            Assert.Pass();
        }

        [Test]
        public void OnSafeAreaChanged_CanSubscribe()
        {
            Rect receivedRect = Rect.zero;
            _responsiveLayout.OnSafeAreaChanged += (rect) => receivedRect = rect;

            _responsiveLayout.ForceLayoutUpdate();

            // Safe area should have some dimensions
            Assert.That(receivedRect.width, Is.GreaterThanOrEqualTo(0));
        }

        #endregion
    }
}
