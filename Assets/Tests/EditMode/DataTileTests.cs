// ABOUTME: Unit tests for DataTile UI component.
// ABOUTME: Tests value formatting, direction prefixes, highlighting, and responsive sizing.

using System.Collections.Generic;
using NUnit.Framework;
using OpenRange.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class DataTileTests
    {
        private GameObject _tileGO;
        private DataTile _tile;
        private TextMeshProUGUI _labelText;
        private TextMeshProUGUI _valueText;
        private TextMeshProUGUI _unitText;
        private Image _background;
        private CanvasGroup _canvasGroup;

        [SetUp]
        public void SetUp()
        {
            _tileGO = new GameObject("TestDataTile");
            _tile = _tileGO.AddComponent<DataTile>();
            _canvasGroup = _tileGO.AddComponent<CanvasGroup>();

            // Create text components
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(_tileGO.transform);
            _labelText = labelGO.AddComponent<TextMeshProUGUI>();

            var valueGO = new GameObject("Value");
            valueGO.transform.SetParent(_tileGO.transform);
            _valueText = valueGO.AddComponent<TextMeshProUGUI>();

            var unitGO = new GameObject("Unit");
            unitGO.transform.SetParent(_tileGO.transform);
            _unitText = unitGO.AddComponent<TextMeshProUGUI>();

            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(_tileGO.transform);
            _background = bgGO.AddComponent<Image>();

            // Wire up references
            _tile.SetReferences(_labelText, _valueText, _unitText, _background, _canvasGroup);
        }

        [TearDown]
        public void TearDown()
        {
            if (_tileGO != null)
            {
                Object.DestroyImmediate(_tileGO);
            }
        }

        #region Label and Unit Tests

        [Test]
        public void Label_SetValue_UpdatesText()
        {
            _tile.Label = "BALL SPEED";

            Assert.AreEqual("BALL SPEED", _labelText.text);
        }

        [Test]
        public void Label_GetValue_ReturnsCurrentText()
        {
            _labelText.text = "DIRECTION";

            Assert.AreEqual("DIRECTION", _tile.Label);
        }

        [Test]
        public void Unit_SetValue_UpdatesText()
        {
            _tile.Unit = "mph";

            Assert.AreEqual("mph", _unitText.text);
        }

        [Test]
        public void Unit_GetValue_ReturnsCurrentText()
        {
            _unitText.text = "yd";

            Assert.AreEqual("yd", _tile.Unit);
        }

        #endregion

        #region SetValue Tests

        [Test]
        public void SetValue_WithF1Format_ShowsOneDecimal()
        {
            _tile.SetValue(104.56f, "F1", animate: false);

            Assert.AreEqual("104.6", _tile.GetDisplayedValue());
        }

        [Test]
        public void SetValue_WithF0Format_ShowsNoDecimals()
        {
            _tile.SetValue(104.56f, "F0", animate: false);

            Assert.AreEqual("105", _tile.GetDisplayedValue());
        }

        [Test]
        public void SetValue_UpdatesCurrentValue()
        {
            _tile.SetValue(50.5f, "F1", animate: false);

            Assert.AreEqual(50.5f, _tile.CurrentValue, 0.01f);
        }

        [Test]
        public void SetValue_SetsHasValue()
        {
            Assert.IsFalse(_tile.HasValue);

            _tile.SetValue(100f, "F1", animate: false);

            Assert.IsTrue(_tile.HasValue);
        }

        [Test]
        public void SetValue_Zero_DisplaysZero()
        {
            _tile.SetValue(0f, "F1", animate: false);

            Assert.AreEqual("0.0", _tile.GetDisplayedValue());
        }

        [Test]
        public void SetValue_Negative_DisplaysNegative()
        {
            _tile.SetValue(-5.5f, "F1", animate: false);

            Assert.AreEqual("-5.5", _tile.GetDisplayedValue());
        }

        #endregion

        #region SetValueWithDirection Tests

        [Test]
        public void SetValueWithDirection_PositiveValue_ShowsRPrefix()
        {
            _tile.SetValueWithDirection(4.5f, "F1", animate: false);

            Assert.AreEqual("R4.5", _tile.GetDisplayedValue());
        }

        [Test]
        public void SetValueWithDirection_NegativeValue_ShowsLPrefix()
        {
            _tile.SetValueWithDirection(-4.5f, "F1", animate: false);

            Assert.AreEqual("L4.5", _tile.GetDisplayedValue());
        }

        [Test]
        public void SetValueWithDirection_VerySmallValue_ShowsZero()
        {
            _tile.SetValueWithDirection(0.05f, "F1", animate: false);

            Assert.AreEqual("0", _tile.GetDisplayedValue());
        }

        [Test]
        public void SetValueWithDirection_ExactlyZero_ShowsZero()
        {
            _tile.SetValueWithDirection(0f, "F1", animate: false);

            Assert.AreEqual("0", _tile.GetDisplayedValue());
        }

        [Test]
        public void SetValueWithDirection_LargePositive_ShowsRPrefix()
        {
            _tile.SetValueWithDirection(150.3f, "F1", animate: false);

            Assert.AreEqual("R150.3", _tile.GetDisplayedValue());
        }

        [Test]
        public void SetValueWithDirection_LargeNegative_ShowsLPrefix()
        {
            _tile.SetValueWithDirection(-150.3f, "F1", animate: false);

            Assert.AreEqual("L150.3", _tile.GetDisplayedValue());
        }

        #endregion

        #region SetValueWithThousands Tests

        [Test]
        public void SetValueWithThousands_LargeNumber_HasCommas()
        {
            _tile.SetValueWithThousands(4121f, animate: false);

            Assert.AreEqual("4,121", _tile.GetDisplayedValue());
        }

        [Test]
        public void SetValueWithThousands_SmallNumber_NoCommas()
        {
            _tile.SetValueWithThousands(500f, animate: false);

            Assert.AreEqual("500", _tile.GetDisplayedValue());
        }

        [Test]
        public void SetValueWithThousands_VeryLarge_MultipleCommas()
        {
            _tile.SetValueWithThousands(12345f, animate: false);

            Assert.AreEqual("12,345", _tile.GetDisplayedValue());
        }

        [Test]
        public void SetValueWithThousands_Zero_ShowsZero()
        {
            _tile.SetValueWithThousands(0f, animate: false);

            Assert.AreEqual("0", _tile.GetDisplayedValue());
        }

        #endregion

        #region SetValueWithDirectionAndThousands Tests

        [Test]
        public void SetValueWithDirectionAndThousands_PositiveLarge_ShowsRWithCommas()
        {
            _tile.SetValueWithDirectionAndThousands(1234f, animate: false);

            Assert.AreEqual("R1,234", _tile.GetDisplayedValue());
        }

        [Test]
        public void SetValueWithDirectionAndThousands_NegativeLarge_ShowsLWithCommas()
        {
            _tile.SetValueWithDirectionAndThousands(-1234f, animate: false);

            Assert.AreEqual("L1,234", _tile.GetDisplayedValue());
        }

        [Test]
        public void SetValueWithDirectionAndThousands_SmallValue_ShowsZero()
        {
            _tile.SetValueWithDirectionAndThousands(5f, animate: false);

            Assert.AreEqual("0", _tile.GetDisplayedValue());
        }

        [Test]
        public void SetValueWithDirectionAndThousands_PositiveSmall_ShowsR()
        {
            _tile.SetValueWithDirectionAndThousands(311f, animate: false);

            Assert.AreEqual("R311", _tile.GetDisplayedValue());
        }

        #endregion

        #region SetText Tests

        [Test]
        public void SetText_SetsExactText()
        {
            _tile.SetText("Custom", animate: false);

            Assert.AreEqual("Custom", _tile.GetDisplayedValue());
        }

        [Test]
        public void SetText_SetsHasValue()
        {
            Assert.IsFalse(_tile.HasValue);

            _tile.SetText("Test", animate: false);

            Assert.IsTrue(_tile.HasValue);
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_ResetsValue()
        {
            _tile.SetValue(100f, "F1", animate: false);
            _tile.Clear();

            Assert.AreEqual("-", _tile.GetDisplayedValue());
        }

        [Test]
        public void Clear_ResetsHasValue()
        {
            _tile.SetValue(100f, "F1", animate: false);
            _tile.Clear();

            Assert.IsFalse(_tile.HasValue);
        }

        [Test]
        public void Clear_ResetsCurrentValue()
        {
            _tile.SetValue(100f, "F1", animate: false);
            _tile.Clear();

            Assert.AreEqual(0f, _tile.CurrentValue);
        }

        #endregion

        #region Highlighting Tests

        [Test]
        public void IsHighlighted_SetTrue_ChangesValueColor()
        {
            _tile.SetHighlightColor(UITheme.TotalRed);
            _tile.IsHighlighted = true;

            Assert.AreEqual(UITheme.TotalRed, _tile.GetValueColor());
        }

        [Test]
        public void IsHighlighted_SetFalse_UsesNormalColor()
        {
            _tile.SetNormalColor(UITheme.TextPrimary);
            _tile.IsHighlighted = true;
            _tile.IsHighlighted = false;

            Assert.AreEqual(UITheme.TextPrimary, _tile.GetValueColor());
        }

        [Test]
        public void SetHighlightColor_ChangesHighlightedColor()
        {
            Color customColor = Color.cyan;
            _tile.SetHighlightColor(customColor);
            _tile.IsHighlighted = true;

            Assert.AreEqual(customColor, _tile.GetValueColor());
        }

        [Test]
        public void SetNormalColor_ChangesNonHighlightedColor()
        {
            Color customColor = Color.yellow;
            _tile.SetNormalColor(customColor);
            _tile.IsHighlighted = false;

            Assert.AreEqual(customColor, _tile.GetValueColor());
        }

        #endregion

        #region UpdateFontSize Tests

        [Test]
        public void UpdateFontSize_Compact_SetsSmallFonts()
        {
            _tile.UpdateFontSize(ScreenCategory.Compact);

            Assert.AreEqual(UITheme.FontSizeCompact.Small, _labelText.fontSize);
            Assert.AreEqual(UITheme.FontSizeCompact.DataValue, _valueText.fontSize);
            Assert.AreEqual(UITheme.FontSizeCompact.Small, _unitText.fontSize);
        }

        [Test]
        public void UpdateFontSize_Regular_SetsMediumFonts()
        {
            _tile.UpdateFontSize(ScreenCategory.Regular);

            Assert.AreEqual(UITheme.FontSizeRegular.Small, _labelText.fontSize);
            Assert.AreEqual(UITheme.FontSizeRegular.DataValue, _valueText.fontSize);
            Assert.AreEqual(UITheme.FontSizeRegular.Small, _unitText.fontSize);
        }

        [Test]
        public void UpdateFontSize_Large_SetsLargeFonts()
        {
            _tile.UpdateFontSize(ScreenCategory.Large);

            Assert.AreEqual(UITheme.FontSizeLarge.Small, _labelText.fontSize);
            Assert.AreEqual(UITheme.FontSizeLarge.DataValue, _valueText.fontSize);
            Assert.AreEqual(UITheme.FontSizeLarge.Small, _unitText.fontSize);
        }

        #endregion

        #region Edge Cases

        [Test]
        public void SetValue_WithNullReferences_DoesNotThrow()
        {
            var emptyTile = new GameObject("Empty").AddComponent<DataTile>();

            Assert.DoesNotThrow(() => emptyTile.SetValue(100f, "F1", animate: false));

            Object.DestroyImmediate(emptyTile.gameObject);
        }

        [Test]
        public void Label_WithNullReference_ReturnsEmpty()
        {
            var emptyTile = new GameObject("Empty").AddComponent<DataTile>();

            Assert.AreEqual(string.Empty, emptyTile.Label);

            Object.DestroyImmediate(emptyTile.gameObject);
        }

        [Test]
        public void Unit_WithNullReference_ReturnsEmpty()
        {
            var emptyTile = new GameObject("Empty").AddComponent<DataTile>();

            Assert.AreEqual(string.Empty, emptyTile.Unit);

            Object.DestroyImmediate(emptyTile.gameObject);
        }

        [Test]
        public void SetValueWithDirection_F0Format_NoDecimals()
        {
            _tile.SetValueWithDirection(4f, "F0", animate: false);

            Assert.AreEqual("R4", _tile.GetDisplayedValue());
        }

        [Test]
        public void SetValue_VeryLargeNumber_DisplaysCorrectly()
        {
            _tile.SetValue(9999.9f, "F1", animate: false);

            Assert.AreEqual("9999.9", _tile.GetDisplayedValue());
        }

        #endregion
    }
}
