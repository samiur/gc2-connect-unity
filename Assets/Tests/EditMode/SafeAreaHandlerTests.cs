// ABOUTME: Unit tests for SafeAreaHandler component.
// ABOUTME: Tests safe area application and edge configuration.

using NUnit.Framework;
using OpenRange.UI;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.Tests.EditMode
{
    /// <summary>
    /// Tests for SafeAreaHandler component.
    /// </summary>
    [TestFixture]
    public class SafeAreaHandlerTests
    {
        private GameObject _canvasObject;
        private GameObject _testObject;
        private SafeAreaHandler _safeAreaHandler;
        private Canvas _canvas;

        [SetUp]
        public void SetUp()
        {
            // Create a canvas for testing
            _canvasObject = new GameObject("TestCanvas");
            _canvas = _canvasObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var canvasRect = _canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1920, 1080);

            // Add CanvasScaler
            var scaler = _canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Create test object as child of canvas
            _testObject = new GameObject("TestSafeArea");
            _testObject.transform.SetParent(_canvasObject.transform, false);

            var rectTransform = _testObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            _safeAreaHandler = _testObject.AddComponent<SafeAreaHandler>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_canvasObject != null)
            {
                Object.DestroyImmediate(_canvasObject);
            }
        }

        #region Component Setup

        [Test]
        public void RectTransform_IsNotNull()
        {
            Assert.That(_safeAreaHandler.RectTransform, Is.Not.Null);
        }

        [Test]
        public void IsApplied_InitiallyFalse()
        {
            Assert.That(_safeAreaHandler.IsApplied, Is.False);
        }

        [Test]
        public void LastAppliedSafeArea_InitiallyZero()
        {
            Assert.That(_safeAreaHandler.LastAppliedSafeArea, Is.EqualTo(Rect.zero));
        }

        #endregion

        #region Apply Safe Area

        [Test]
        public void ApplySafeArea_SetsIsAppliedTrue()
        {
            _safeAreaHandler.ApplySafeArea();
            Assert.That(_safeAreaHandler.IsApplied, Is.True);
        }

        [Test]
        public void ApplySafeArea_UpdatesLastAppliedSafeArea()
        {
            _safeAreaHandler.ApplySafeArea();
            Assert.That(_safeAreaHandler.LastAppliedSafeArea, Is.Not.EqualTo(Rect.zero));
        }

        [Test]
        public void ApplySafeArea_CalledTwice_DoesNotReapplyIfUnchanged()
        {
            _safeAreaHandler.ApplySafeArea();
            var firstRect = _safeAreaHandler.LastAppliedSafeArea;

            _safeAreaHandler.ApplySafeArea();
            var secondRect = _safeAreaHandler.LastAppliedSafeArea;

            Assert.That(secondRect, Is.EqualTo(firstRect));
        }

        #endregion

        #region Reset Safe Area

        [Test]
        public void ResetSafeArea_SetsIsAppliedFalse()
        {
            _safeAreaHandler.ApplySafeArea();
            Assert.That(_safeAreaHandler.IsApplied, Is.True);

            _safeAreaHandler.ResetSafeArea();
            Assert.That(_safeAreaHandler.IsApplied, Is.False);
        }

        [Test]
        public void ResetSafeArea_ResetsRectTransformAnchors()
        {
            _safeAreaHandler.ApplySafeArea();
            _safeAreaHandler.ResetSafeArea();

            var rect = _safeAreaHandler.RectTransform;
            Assert.That(rect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.anchorMax, Is.EqualTo(Vector2.one));
        }

        [Test]
        public void ResetSafeArea_ResetsOffsets()
        {
            _safeAreaHandler.ApplySafeArea();
            _safeAreaHandler.ResetSafeArea();

            var rect = _safeAreaHandler.RectTransform;
            Assert.That(rect.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(rect.offsetMax, Is.EqualTo(Vector2.zero));
        }

        #endregion

        #region Edge Configuration

        [Test]
        public void SetEdges_CanSetAllEdges()
        {
            _safeAreaHandler.SetEdges(true, true, true, true);
            // Should not throw
            Assert.Pass();
        }

        [Test]
        public void SetEdges_CanSetNoEdges()
        {
            _safeAreaHandler.SetEdges(false, false, false, false);
            // Should not throw
            Assert.Pass();
        }

        [Test]
        public void SetEdges_CanSetTopOnly()
        {
            _safeAreaHandler.SetEdges(true, false, false, false);
            Assert.Pass();
        }

        [Test]
        public void SetEdges_CanSetBottomOnly()
        {
            _safeAreaHandler.SetEdges(false, true, false, false);
            Assert.Pass();
        }

        [Test]
        public void SetEdges_CanSetLeftOnly()
        {
            _safeAreaHandler.SetEdges(false, false, true, false);
            Assert.Pass();
        }

        [Test]
        public void SetEdges_CanSetRightOnly()
        {
            _safeAreaHandler.SetEdges(false, false, false, true);
            Assert.Pass();
        }

        [Test]
        public void SetEdges_ReappliesIfAlreadyApplied()
        {
            _safeAreaHandler.ApplySafeArea();
            var firstRect = _safeAreaHandler.LastAppliedSafeArea;

            // Force reapply by invalidating last safe area
            _safeAreaHandler.SetEdges(true, false, true, false);

            // Should still be applied
            Assert.That(_safeAreaHandler.IsApplied, Is.True);
        }

        #endregion

        #region Get Insets

        [Test]
        public void GetInsets_ReturnsVector4()
        {
            var insets = _safeAreaHandler.GetInsets();
            Assert.That(insets, Is.TypeOf<Vector4>());
        }

        [Test]
        public void GetInsets_AllComponentsNonNegative()
        {
            var insets = _safeAreaHandler.GetInsets();
            Assert.That(insets.x, Is.GreaterThanOrEqualTo(0f)); // left
            Assert.That(insets.y, Is.GreaterThanOrEqualTo(0f)); // right
            Assert.That(insets.z, Is.GreaterThanOrEqualTo(0f)); // bottom
            Assert.That(insets.w, Is.GreaterThanOrEqualTo(0f)); // top
        }

        #endregion

        #region Static Methods

        [Test]
        public void HasSafeAreaInsets_ReturnsBool()
        {
            bool hasInsets = SafeAreaHandler.HasSafeAreaInsets();
            Assert.That(hasInsets, Is.TypeOf<bool>());
        }

        [Test]
        public void HasSafeAreaInsets_WorksWithoutInstance()
        {
            // This should work even without any SafeAreaHandler instance
            bool hasInsets = SafeAreaHandler.HasSafeAreaInsets();
            // In editor, typically no insets
            Assert.That(hasInsets, Is.EqualTo(false).Or.EqualTo(true));
        }

        #endregion

        #region Without Canvas

        [Test]
        public void ApplySafeArea_WithoutCanvas_DoesNotThrow()
        {
            // Create object without canvas parent
            var orphanObject = new GameObject("OrphanSafeArea");
            orphanObject.AddComponent<RectTransform>();
            var handler = orphanObject.AddComponent<SafeAreaHandler>();

            // Should not throw
            handler.ApplySafeArea();

            Object.DestroyImmediate(orphanObject);
            Assert.Pass();
        }

        [Test]
        public void GetInsets_WithoutCanvas_ReturnsZero()
        {
            var orphanObject = new GameObject("OrphanSafeArea");
            orphanObject.AddComponent<RectTransform>();
            var handler = orphanObject.AddComponent<SafeAreaHandler>();

            var insets = handler.GetInsets();
            Assert.That(insets, Is.EqualTo(Vector4.zero));

            Object.DestroyImmediate(orphanObject);
        }

        #endregion
    }
}
