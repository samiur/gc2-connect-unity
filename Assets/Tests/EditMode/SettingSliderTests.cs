// ABOUTME: Unit tests for the SettingSlider UI component.
// ABOUTME: Tests slider functionality, value binding, range, and formatting.

using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpenRange.UI;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class SettingSliderTests
    {
        private GameObject _sliderGo;
        private SettingSlider _settingSlider;
        private Slider _unitySlider;
        private TextMeshProUGUI _labelText;
        private TextMeshProUGUI _valueText;

        [SetUp]
        public void SetUp()
        {
            _sliderGo = new GameObject("TestSettingSlider");

            // Create Slider component
            _unitySlider = _sliderGo.AddComponent<Slider>();
            _unitySlider.minValue = 0f;
            _unitySlider.maxValue = 100f;
            _unitySlider.value = 50f;

            // Create label text
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(_sliderGo.transform);
            _labelText = labelGo.AddComponent<TextMeshProUGUI>();

            // Create value text
            var valueGo = new GameObject("Value");
            valueGo.transform.SetParent(_sliderGo.transform);
            _valueText = valueGo.AddComponent<TextMeshProUGUI>();

            // Add SettingSlider component
            _settingSlider = _sliderGo.AddComponent<SettingSlider>();
            _settingSlider.SetReferences(_unitySlider, _labelText, _valueText);
        }

        [TearDown]
        public void TearDown()
        {
            if (_sliderGo != null)
            {
                UnityEngine.Object.DestroyImmediate(_sliderGo);
            }
        }

        #region Label Tests

        [Test]
        public void Label_SetValue_UpdatesText()
        {
            _settingSlider.Label = "Test Label";

            Assert.AreEqual("Test Label", _labelText.text);
        }

        [Test]
        public void Label_GetValue_ReturnsCurrentText()
        {
            _labelText.text = "Current Label";

            Assert.AreEqual("Current Label", _settingSlider.Label);
        }

        #endregion

        #region Suffix Tests

        [Test]
        public void Suffix_SetValue_UpdatesValueDisplay()
        {
            _settingSlider.Suffix = " mph";
            _settingSlider.SetRange(0, 100);
            _settingSlider.Value = 60;

            Assert.IsTrue(_valueText.text.EndsWith(" mph"));
        }

        [Test]
        public void Suffix_Empty_ShowsValueOnly()
        {
            _settingSlider.Suffix = "";
            _settingSlider.SetRange(0, 100);
            _settingSlider.Value = 60;

            Assert.AreEqual("60", _valueText.text);
        }

        #endregion

        #region Value Tests

        [Test]
        public void Value_SetValue_UpdatesSlider()
        {
            _settingSlider.SetRange(0, 100);
            _settingSlider.Value = 75;

            Assert.AreEqual(75f, _unitySlider.value, 0.01f);
        }

        [Test]
        public void Value_GetValue_ReturnsSliderValue()
        {
            _unitySlider.value = 42;

            Assert.AreEqual(42f, _settingSlider.Value, 0.01f);
        }

        [Test]
        public void Value_SetBelowMin_ClampsToMin()
        {
            _settingSlider.SetRange(10, 100);
            _settingSlider.Value = 5;

            Assert.AreEqual(10f, _settingSlider.Value, 0.01f);
        }

        [Test]
        public void Value_SetAboveMax_ClampsToMax()
        {
            _settingSlider.SetRange(0, 50);
            _settingSlider.Value = 75;

            Assert.AreEqual(50f, _settingSlider.Value, 0.01f);
        }

        #endregion

        #region Range Tests

        [Test]
        public void SetRange_UpdatesMinMax()
        {
            _settingSlider.SetRange(20, 80);

            Assert.AreEqual(20f, _unitySlider.minValue, 0.01f);
            Assert.AreEqual(80f, _unitySlider.maxValue, 0.01f);
        }

        [Test]
        public void SetRange_MinGreaterThanMax_SwapsValues()
        {
            _settingSlider.SetRange(100, 50);

            Assert.AreEqual(50f, _unitySlider.minValue, 0.01f);
            Assert.AreEqual(100f, _unitySlider.maxValue, 0.01f);
        }

        [Test]
        public void MinValue_ReturnsSliderMin()
        {
            _unitySlider.minValue = 15;

            Assert.AreEqual(15f, _settingSlider.MinValue, 0.01f);
        }

        [Test]
        public void MaxValue_ReturnsSliderMax()
        {
            _unitySlider.maxValue = 85;

            Assert.AreEqual(85f, _settingSlider.MaxValue, 0.01f);
        }

        #endregion

        #region WholeNumbers Tests

        [Test]
        public void WholeNumbers_SetTrue_EnablesOnSlider()
        {
            _settingSlider.WholeNumbers = true;

            Assert.IsTrue(_unitySlider.wholeNumbers);
        }

        [Test]
        public void WholeNumbers_SetFalse_DisablesOnSlider()
        {
            _unitySlider.wholeNumbers = true;
            _settingSlider.WholeNumbers = false;

            Assert.IsFalse(_unitySlider.wholeNumbers);
        }

        [Test]
        public void WholeNumbers_True_FormatsWithoutDecimals()
        {
            _settingSlider.WholeNumbers = true;
            _settingSlider.SetRange(0, 100);
            _settingSlider.Value = 50;

            Assert.AreEqual("50", _valueText.text);
        }

        #endregion

        #region Format Tests

        [Test]
        public void Format_F0_ShowsNoDecimals()
        {
            _settingSlider.Format = "F0";
            _settingSlider.SetRange(0, 100);
            _settingSlider.Value = 50.7f;

            Assert.AreEqual("51", _valueText.text);
        }

        [Test]
        public void Format_F1_ShowsOneDecimal()
        {
            _settingSlider.Format = "F1";
            _settingSlider.SetRange(0, 100);
            _settingSlider.Value = 50.75f;

            Assert.AreEqual("50.8", _valueText.text);
        }

        [Test]
        public void Format_F2_ShowsTwoDecimals()
        {
            _settingSlider.Format = "F2";
            _settingSlider.SetRange(0, 1);
            _settingSlider.Value = 0.5f;

            Assert.AreEqual("0.50", _valueText.text);
        }

        [Test]
        public void Format_Percent_ShowsPercentage()
        {
            _settingSlider.Format = "P0";
            _settingSlider.SetRange(0, 1);
            _settingSlider.Value = 0.5f;

            // Note: P0 format shows as "50 %" or "50%" depending on locale (value * 100)
            Assert.IsTrue(_valueText.text.Contains("50") && _valueText.text.Contains("%"),
                $"Expected percentage format but got: {_valueText.text}");
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnValueChanged_SliderMoved_FiresEvent()
        {
            bool eventFired = false;
            float eventValue = 0;

            _settingSlider.OnValueChanged += (value) =>
            {
                eventFired = true;
                eventValue = value;
            };

            _settingSlider.Value = 75;

            Assert.IsTrue(eventFired);
            Assert.AreEqual(75f, eventValue, 0.01f);
        }

        [Test]
        public void OnValueChanged_SameValue_DoesNotFireEvent()
        {
            _settingSlider.SetRange(0, 100);
            _settingSlider.Value = 50;

            int eventCount = 0;
            _settingSlider.OnValueChanged += (_) => eventCount++;

            _settingSlider.Value = 50;

            Assert.AreEqual(0, eventCount);
        }

        #endregion

        #region SetWithoutNotify Tests

        [Test]
        public void SetWithoutNotify_ChangesValue_DoesNotFireEvent()
        {
            bool eventFired = false;
            _settingSlider.OnValueChanged += (_) => eventFired = true;

            _settingSlider.SetWithoutNotify(75);

            Assert.IsFalse(eventFired);
            Assert.AreEqual(75f, _settingSlider.Value, 0.01f);
        }

        [Test]
        public void SetWithoutNotify_UpdatesValueDisplay()
        {
            _settingSlider.SetRange(0, 100);
            _settingSlider.Format = "F0";
            _settingSlider.SetWithoutNotify(75);

            Assert.AreEqual("75", _valueText.text);
        }

        #endregion

        #region Interactable Tests

        [Test]
        public void IsInteractable_SetFalse_DisablesSlider()
        {
            _settingSlider.IsInteractable = false;

            Assert.IsFalse(_unitySlider.interactable);
        }

        [Test]
        public void IsInteractable_SetTrue_EnablesSlider()
        {
            _unitySlider.interactable = false;
            _settingSlider.IsInteractable = true;

            Assert.IsTrue(_unitySlider.interactable);
        }

        #endregion

        #region Font Size Tests

        [Test]
        public void UpdateFontSize_Compact_SetsCompactFonts()
        {
            _settingSlider.UpdateFontSize(ScreenCategory.Compact);

            Assert.AreEqual(UITheme.FontSizeCompact.Normal, _labelText.fontSize);
            Assert.AreEqual(UITheme.FontSizeCompact.Normal, _valueText.fontSize);
        }

        [Test]
        public void UpdateFontSize_Regular_SetsRegularFonts()
        {
            _settingSlider.UpdateFontSize(ScreenCategory.Regular);

            Assert.AreEqual(UITheme.FontSizeRegular.Normal, _labelText.fontSize);
            Assert.AreEqual(UITheme.FontSizeRegular.Normal, _valueText.fontSize);
        }

        [Test]
        public void UpdateFontSize_Large_SetsLargeFonts()
        {
            _settingSlider.UpdateFontSize(ScreenCategory.Large);

            Assert.AreEqual(UITheme.FontSizeLarge.Normal, _labelText.fontSize);
            Assert.AreEqual(UITheme.FontSizeLarge.Normal, _valueText.fontSize);
        }

        #endregion

        #region NormalizedValue Tests

        [Test]
        public void NormalizedValue_AtMin_ReturnsZero()
        {
            _settingSlider.SetRange(0, 100);
            _settingSlider.Value = 0;

            Assert.AreEqual(0f, _settingSlider.NormalizedValue, 0.01f);
        }

        [Test]
        public void NormalizedValue_AtMax_ReturnsOne()
        {
            _settingSlider.SetRange(0, 100);
            _settingSlider.Value = 100;

            Assert.AreEqual(1f, _settingSlider.NormalizedValue, 0.01f);
        }

        [Test]
        public void NormalizedValue_AtMidpoint_ReturnsHalf()
        {
            _settingSlider.SetRange(0, 100);
            _settingSlider.Value = 50;

            Assert.AreEqual(0.5f, _settingSlider.NormalizedValue, 0.01f);
        }

        [Test]
        public void NormalizedValue_Set_UpdatesValue()
        {
            _settingSlider.SetRange(0, 100);
            _settingSlider.NormalizedValue = 0.75f;

            Assert.AreEqual(75f, _settingSlider.Value, 0.01f);
        }

        #endregion
    }
}
