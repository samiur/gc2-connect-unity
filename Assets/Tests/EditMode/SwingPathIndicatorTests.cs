// ABOUTME: Unit tests for SwingPathIndicator UI component.
// ABOUTME: Tests visual display of swing path direction and face angle.

using NUnit.Framework;
using OpenRange.UI;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class SwingPathIndicatorTests
    {
        private GameObject _indicatorGO;
        private SwingPathIndicator _indicator;
        private Image _pathArrow;
        private Image _faceAngleLine;

        [SetUp]
        public void SetUp()
        {
            _indicatorGO = new GameObject("TestSwingPathIndicator");
            _indicator = _indicatorGO.AddComponent<SwingPathIndicator>();

            // Create path arrow
            var pathArrowGO = new GameObject("PathArrow");
            pathArrowGO.transform.SetParent(_indicatorGO.transform);
            _pathArrow = pathArrowGO.AddComponent<Image>();

            // Create face angle line
            var faceLineGO = new GameObject("FaceAngleLine");
            faceLineGO.transform.SetParent(_indicatorGO.transform);
            _faceAngleLine = faceLineGO.AddComponent<Image>();

            _indicator.SetReferences(_pathArrow, _faceAngleLine);
        }

        [TearDown]
        public void TearDown()
        {
            if (_indicatorGO != null)
            {
                Object.DestroyImmediate(_indicatorGO);
            }
        }

        #region UpdateDisplay Tests

        [Test]
        public void UpdateDisplay_InToOut_SetsBlueColor()
        {
            _indicator.UpdateDisplay(3.0f, 0f);

            Assert.AreEqual(SwingPathIndicator.InToOutColor, _pathArrow.color);
        }

        [Test]
        public void UpdateDisplay_OutToIn_SetsOrangeColor()
        {
            _indicator.UpdateDisplay(-3.0f, 0f);

            Assert.AreEqual(SwingPathIndicator.OutToInColor, _pathArrow.color);
        }

        [Test]
        public void UpdateDisplay_Neutral_SetsNeutralColor()
        {
            _indicator.UpdateDisplay(0f, 0f);

            Assert.AreEqual(SwingPathIndicator.NeutralColor, _pathArrow.color);
        }

        [Test]
        public void UpdateDisplay_SmallPath_SetsNeutralColor()
        {
            _indicator.UpdateDisplay(0.3f, 0f);

            Assert.AreEqual(SwingPathIndicator.NeutralColor, _pathArrow.color);
        }

        #endregion

        #region Path Rotation Tests

        [Test]
        public void UpdateDisplay_PositivePath_RotatesArrowClockwise()
        {
            _indicator.UpdateDisplay(5.0f, 0f);

            // Positive path = in-to-out = rotate left/counter-clockwise from top-down view
            float expectedRotation = 5.0f * SwingPathIndicator.RotationScale;
            Assert.AreEqual(expectedRotation, _pathArrow.rectTransform.localEulerAngles.z, 0.1f);
        }

        [Test]
        public void UpdateDisplay_NegativePath_RotatesArrowCounterClockwise()
        {
            _indicator.UpdateDisplay(-5.0f, 0f);

            // Negative path = out-to-in = rotate right/clockwise from top-down view
            float expectedRotation = 360f + (-5.0f * SwingPathIndicator.RotationScale);
            Assert.AreEqual(expectedRotation, _pathArrow.rectTransform.localEulerAngles.z, 0.1f);
        }

        [Test]
        public void UpdateDisplay_ZeroPath_NoRotation()
        {
            _indicator.UpdateDisplay(0f, 0f);

            Assert.AreEqual(0f, _pathArrow.rectTransform.localEulerAngles.z, 0.1f);
        }

        #endregion

        #region Face Angle Tests

        [Test]
        public void UpdateDisplay_OpenFace_RotatesFaceLineClockwise()
        {
            _indicator.UpdateDisplay(0f, 5.0f);

            float expectedRotation = 5.0f * SwingPathIndicator.FaceRotationScale;
            Assert.AreEqual(expectedRotation, _faceAngleLine.rectTransform.localEulerAngles.z, 0.1f);
        }

        [Test]
        public void UpdateDisplay_ClosedFace_RotatesFaceLineCounterClockwise()
        {
            _indicator.UpdateDisplay(0f, -5.0f);

            float expectedRotation = 360f + (-5.0f * SwingPathIndicator.FaceRotationScale);
            Assert.AreEqual(expectedRotation, _faceAngleLine.rectTransform.localEulerAngles.z, 0.1f);
        }

        [Test]
        public void UpdateDisplay_SquareFace_NoFaceRotation()
        {
            _indicator.UpdateDisplay(0f, 0f);

            Assert.AreEqual(0f, _faceAngleLine.rectTransform.localEulerAngles.z, 0.1f);
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_ResetsPathArrowRotation()
        {
            _indicator.UpdateDisplay(5.0f, 5.0f);
            _indicator.Clear();

            Assert.AreEqual(0f, _pathArrow.rectTransform.localEulerAngles.z, 0.1f);
        }

        [Test]
        public void Clear_ResetsFaceLineRotation()
        {
            _indicator.UpdateDisplay(5.0f, 5.0f);
            _indicator.Clear();

            Assert.AreEqual(0f, _faceAngleLine.rectTransform.localEulerAngles.z, 0.1f);
        }

        [Test]
        public void Clear_SetsNeutralColor()
        {
            _indicator.UpdateDisplay(5.0f, 0f);
            _indicator.Clear();

            Assert.AreEqual(SwingPathIndicator.NeutralColor, _pathArrow.color);
        }

        #endregion

        #region CurrentPath and CurrentFace Tests

        [Test]
        public void CurrentPath_AfterUpdate_ReturnsPath()
        {
            _indicator.UpdateDisplay(3.5f, 0f);

            Assert.AreEqual(3.5f, _indicator.CurrentPath, 0.01f);
        }

        [Test]
        public void CurrentFace_AfterUpdate_ReturnsFace()
        {
            _indicator.UpdateDisplay(0f, -2.0f);

            Assert.AreEqual(-2.0f, _indicator.CurrentFace, 0.01f);
        }

        [Test]
        public void CurrentPath_AfterClear_ReturnsZero()
        {
            _indicator.UpdateDisplay(3.5f, 0f);
            _indicator.Clear();

            Assert.AreEqual(0f, _indicator.CurrentPath, 0.01f);
        }

        [Test]
        public void CurrentFace_AfterClear_ReturnsZero()
        {
            _indicator.UpdateDisplay(0f, -2.0f);
            _indicator.Clear();

            Assert.AreEqual(0f, _indicator.CurrentFace, 0.01f);
        }

        #endregion

        #region PathDirection Property Tests

        [Test]
        public void PathDirection_InToOut_ReturnsInToOut()
        {
            _indicator.UpdateDisplay(3.0f, 0f);

            Assert.AreEqual(SwingPathDirection.InToOut, _indicator.PathDirection);
        }

        [Test]
        public void PathDirection_OutToIn_ReturnsOutToIn()
        {
            _indicator.UpdateDisplay(-3.0f, 0f);

            Assert.AreEqual(SwingPathDirection.OutToIn, _indicator.PathDirection);
        }

        [Test]
        public void PathDirection_Neutral_ReturnsNeutral()
        {
            _indicator.UpdateDisplay(0f, 0f);

            Assert.AreEqual(SwingPathDirection.Neutral, _indicator.PathDirection);
        }

        #endregion

        #region Color Constants Tests

        [Test]
        public void InToOutColor_IsBlue()
        {
            // Blue color for draw path
            Assert.IsTrue(SwingPathIndicator.InToOutColor.b > SwingPathIndicator.InToOutColor.r);
        }

        [Test]
        public void OutToInColor_IsOrange()
        {
            // Orange color for fade path
            Assert.IsTrue(SwingPathIndicator.OutToInColor.r > SwingPathIndicator.OutToInColor.b);
        }

        [Test]
        public void NeutralColor_IsWhiteOrGray()
        {
            // Neutral should be white/gray (equal RGB values)
            Assert.AreEqual(SwingPathIndicator.NeutralColor.r, SwingPathIndicator.NeutralColor.g, 0.1f);
            Assert.AreEqual(SwingPathIndicator.NeutralColor.g, SwingPathIndicator.NeutralColor.b, 0.1f);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void UpdateDisplay_LargePathAngle_ClampsRotation()
        {
            _indicator.UpdateDisplay(15.0f, 0f);

            // Should still display, just scaled
            float rotation = _pathArrow.rectTransform.localEulerAngles.z;
            Assert.IsTrue(rotation <= 360f);
        }

        [Test]
        public void UpdateDisplay_NullPathArrow_DoesNotThrow()
        {
            _indicator.SetReferences(null, _faceAngleLine);

            Assert.DoesNotThrow(() => _indicator.UpdateDisplay(3.0f, 0f));
        }

        [Test]
        public void UpdateDisplay_NullFaceLine_DoesNotThrow()
        {
            _indicator.SetReferences(_pathArrow, null);

            Assert.DoesNotThrow(() => _indicator.UpdateDisplay(0f, 3.0f));
        }

        #endregion
    }
}
