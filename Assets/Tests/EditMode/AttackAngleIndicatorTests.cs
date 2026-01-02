// ABOUTME: Unit tests for AttackAngleIndicator UI component.
// ABOUTME: Tests visual display of attack angle direction (ascending vs descending).

using NUnit.Framework;
using OpenRange.UI;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class AttackAngleIndicatorTests
    {
        private GameObject _indicatorGO;
        private AttackAngleIndicator _indicator;
        private Image _angleArrow;

        [SetUp]
        public void SetUp()
        {
            _indicatorGO = new GameObject("TestAttackAngleIndicator");
            _indicator = _indicatorGO.AddComponent<AttackAngleIndicator>();

            // Create angle arrow
            var arrowGO = new GameObject("AngleArrow");
            arrowGO.transform.SetParent(_indicatorGO.transform);
            _angleArrow = arrowGO.AddComponent<Image>();

            _indicator.SetReferences(_angleArrow);
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
        public void UpdateDisplay_Ascending_SetsAscendingColor()
        {
            _indicator.UpdateDisplay(4.0f);

            Assert.AreEqual(AttackAngleIndicator.AscendingColor, _angleArrow.color);
        }

        [Test]
        public void UpdateDisplay_Descending_SetsDescendingColor()
        {
            _indicator.UpdateDisplay(-5.0f);

            Assert.AreEqual(AttackAngleIndicator.DescendingColor, _angleArrow.color);
        }

        [Test]
        public void UpdateDisplay_Neutral_SetsNeutralColor()
        {
            _indicator.UpdateDisplay(0f);

            Assert.AreEqual(AttackAngleIndicator.NeutralColor, _angleArrow.color);
        }

        #endregion

        #region Rotation Tests

        [Test]
        public void UpdateDisplay_PositiveAngle_RotatesArrowUp()
        {
            _indicator.UpdateDisplay(5.0f);

            // Positive attack angle = ascending = arrow points upward
            float expectedRotation = 5.0f * AttackAngleIndicator.RotationScale;
            Assert.AreEqual(expectedRotation, _angleArrow.rectTransform.localEulerAngles.z, 0.1f);
        }

        [Test]
        public void UpdateDisplay_NegativeAngle_RotatesArrowDown()
        {
            _indicator.UpdateDisplay(-5.0f);

            // Negative attack angle = descending = arrow points downward
            float expectedRotation = 360f + (-5.0f * AttackAngleIndicator.RotationScale);
            Assert.AreEqual(expectedRotation, _angleArrow.rectTransform.localEulerAngles.z, 0.1f);
        }

        [Test]
        public void UpdateDisplay_ZeroAngle_NoRotation()
        {
            _indicator.UpdateDisplay(0f);

            Assert.AreEqual(0f, _angleArrow.rectTransform.localEulerAngles.z, 0.1f);
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_ResetsArrowRotation()
        {
            _indicator.UpdateDisplay(5.0f);
            _indicator.Clear();

            Assert.AreEqual(0f, _angleArrow.rectTransform.localEulerAngles.z, 0.1f);
        }

        [Test]
        public void Clear_SetsNeutralColor()
        {
            _indicator.UpdateDisplay(5.0f);
            _indicator.Clear();

            Assert.AreEqual(AttackAngleIndicator.NeutralColor, _angleArrow.color);
        }

        #endregion

        #region CurrentAngle Tests

        [Test]
        public void CurrentAngle_AfterUpdate_ReturnsAngle()
        {
            _indicator.UpdateDisplay(-4.2f);

            Assert.AreEqual(-4.2f, _indicator.CurrentAngle, 0.01f);
        }

        [Test]
        public void CurrentAngle_AfterClear_ReturnsZero()
        {
            _indicator.UpdateDisplay(-4.2f);
            _indicator.Clear();

            Assert.AreEqual(0f, _indicator.CurrentAngle, 0.01f);
        }

        #endregion

        #region AttackDirection Property Tests

        [Test]
        public void AttackDirection_Ascending_ReturnsAscending()
        {
            _indicator.UpdateDisplay(3.0f);

            Assert.AreEqual(AttackAngleDirection.Ascending, _indicator.AttackDirection);
        }

        [Test]
        public void AttackDirection_Descending_ReturnsDescending()
        {
            _indicator.UpdateDisplay(-5.0f);

            Assert.AreEqual(AttackAngleDirection.Descending, _indicator.AttackDirection);
        }

        [Test]
        public void AttackDirection_Neutral_ReturnsNeutral()
        {
            _indicator.UpdateDisplay(0f);

            Assert.AreEqual(AttackAngleDirection.Neutral, _indicator.AttackDirection);
        }

        #endregion

        #region Color Constants Tests

        [Test]
        public void AscendingColor_IsGreenish()
        {
            // Ascending (hitting up) should be a greenish color
            Assert.IsTrue(AttackAngleIndicator.AscendingColor.g > 0.3f);
        }

        [Test]
        public void DescendingColor_IsBlueish()
        {
            // Descending (hitting down) should be a blueish/purple color
            Assert.IsTrue(AttackAngleIndicator.DescendingColor.b > 0.3f);
        }

        [Test]
        public void NeutralColor_IsWhiteOrGray()
        {
            // Neutral should be white/gray (equal RGB values)
            Assert.AreEqual(AttackAngleIndicator.NeutralColor.r, AttackAngleIndicator.NeutralColor.g, 0.1f);
            Assert.AreEqual(AttackAngleIndicator.NeutralColor.g, AttackAngleIndicator.NeutralColor.b, 0.1f);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void UpdateDisplay_LargeAngle_ClampsRotation()
        {
            _indicator.UpdateDisplay(20.0f);

            // Should still display, just clamped
            float rotation = _angleArrow.rectTransform.localEulerAngles.z;
            Assert.IsTrue(rotation <= 360f);
        }

        [Test]
        public void UpdateDisplay_NullArrow_DoesNotThrow()
        {
            _indicator.SetReferences(null);

            Assert.DoesNotThrow(() => _indicator.UpdateDisplay(5.0f));
        }

        #endregion

        #region Typical Values Tests

        [Test]
        public void UpdateDisplay_TypicalDriver_DisplaysCorrectly()
        {
            // Driver typically has positive attack angle (hitting up)
            _indicator.UpdateDisplay(4.0f);

            Assert.AreEqual(AttackAngleDirection.Ascending, _indicator.AttackDirection);
            Assert.AreEqual(AttackAngleIndicator.AscendingColor, _angleArrow.color);
        }

        [Test]
        public void UpdateDisplay_TypicalIron_DisplaysCorrectly()
        {
            // Irons typically have negative attack angle (hitting down)
            _indicator.UpdateDisplay(-5.5f);

            Assert.AreEqual(AttackAngleDirection.Descending, _indicator.AttackDirection);
            Assert.AreEqual(AttackAngleIndicator.DescendingColor, _angleArrow.color);
        }

        [Test]
        public void UpdateDisplay_SteepWedge_DisplaysCorrectly()
        {
            // Wedges typically have steep negative attack angle
            _indicator.UpdateDisplay(-8.0f);

            Assert.AreEqual(AttackAngleDirection.Descending, _indicator.AttackDirection);
            Assert.AreEqual(AttackAngleIndicator.DescendingColor, _angleArrow.color);
        }

        #endregion
    }
}
