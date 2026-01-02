// ABOUTME: Unit tests for the SettingDropdown UI component.
// ABOUTME: Tests dropdown functionality, option management, and value binding.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using TMPro;
using OpenRange.UI;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class SettingDropdownTests
    {
        private GameObject _dropdownGo;
        private SettingDropdown _settingDropdown;
        private TMP_Dropdown _unityDropdown;
        private TextMeshProUGUI _labelText;

        [SetUp]
        public void SetUp()
        {
            _dropdownGo = new GameObject("TestSettingDropdown");

            // Create TMP_Dropdown component with required child objects
            var templateGo = new GameObject("Template");
            templateGo.transform.SetParent(_dropdownGo.transform);
            templateGo.AddComponent<RectTransform>();

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(templateGo.transform);
            viewportGo.AddComponent<RectTransform>();

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform);
            contentGo.AddComponent<RectTransform>();

            var itemGo = new GameObject("Item");
            itemGo.transform.SetParent(contentGo.transform);
            itemGo.AddComponent<RectTransform>();
            var toggle = itemGo.AddComponent<UnityEngine.UI.Toggle>();

            _unityDropdown = _dropdownGo.AddComponent<TMP_Dropdown>();

            // Create caption text (required for TMP_Dropdown)
            var captionGo = new GameObject("Caption");
            captionGo.transform.SetParent(_dropdownGo.transform);
            var captionText = captionGo.AddComponent<TextMeshProUGUI>();

            // Use reflection to set the private template field (required for initialization)
            var templateField = typeof(TMP_Dropdown).GetField("m_Template", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (templateField != null)
            {
                templateField.SetValue(_unityDropdown, templateGo.GetComponent<RectTransform>());
            }

            var captionField = typeof(TMP_Dropdown).GetField("m_CaptionText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (captionField != null)
            {
                captionField.SetValue(_unityDropdown, captionText);
            }

            var itemTextField = typeof(TMP_Dropdown).GetField("m_ItemText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            // No item text set for now

            // Create label text
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(_dropdownGo.transform);
            _labelText = labelGo.AddComponent<TextMeshProUGUI>();

            // Add SettingDropdown component
            _settingDropdown = _dropdownGo.AddComponent<SettingDropdown>();
            _settingDropdown.SetReferences(_unityDropdown, _labelText);
        }

        [TearDown]
        public void TearDown()
        {
            if (_dropdownGo != null)
            {
                UnityEngine.Object.DestroyImmediate(_dropdownGo);
            }
        }

        #region Label Tests

        [Test]
        public void Label_SetValue_UpdatesText()
        {
            _settingDropdown.Label = "Test Label";

            Assert.AreEqual("Test Label", _labelText.text);
        }

        [Test]
        public void Label_GetValue_ReturnsCurrentText()
        {
            _labelText.text = "Current Label";

            Assert.AreEqual("Current Label", _settingDropdown.Label);
        }

        #endregion

        #region Options Tests

        [Test]
        public void SetOptions_StringArray_PopulatesDropdown()
        {
            string[] options = { "Option A", "Option B", "Option C" };

            _settingDropdown.SetOptions(options);

            Assert.AreEqual(3, _unityDropdown.options.Count);
            Assert.AreEqual("Option A", _unityDropdown.options[0].text);
            Assert.AreEqual("Option B", _unityDropdown.options[1].text);
            Assert.AreEqual("Option C", _unityDropdown.options[2].text);
        }

        [Test]
        public void SetOptions_EmptyArray_ClearsDropdown()
        {
            _unityDropdown.options.Add(new TMP_Dropdown.OptionData("Existing"));
            _settingDropdown.SetOptions(new string[] { });

            Assert.AreEqual(0, _unityDropdown.options.Count);
        }

        [Test]
        public void SetOptions_List_PopulatesDropdown()
        {
            var options = new List<string> { "Option 1", "Option 2" };

            _settingDropdown.SetOptions(options);

            Assert.AreEqual(2, _unityDropdown.options.Count);
        }

        [Test]
        public void OptionCount_ReturnsCorrectCount()
        {
            _settingDropdown.SetOptions(new[] { "A", "B", "C", "D" });

            Assert.AreEqual(4, _settingDropdown.OptionCount);
        }

        [Test]
        public void GetOptionText_ReturnsCorrectText()
        {
            _settingDropdown.SetOptions(new[] { "First", "Second", "Third" });

            Assert.AreEqual("Second", _settingDropdown.GetOptionText(1));
        }

        [Test]
        public void GetOptionText_InvalidIndex_ReturnsEmpty()
        {
            _settingDropdown.SetOptions(new[] { "First" });

            Assert.AreEqual(string.Empty, _settingDropdown.GetOptionText(5));
        }

        #endregion

        #region Value Tests

        [Test]
        public void SelectedIndex_SetValue_UpdatesDropdown()
        {
            _settingDropdown.SetOptions(new[] { "A", "B", "C" });

            _settingDropdown.SelectedIndex = 2;

            Assert.AreEqual(2, _unityDropdown.value);
        }

        [Test]
        public void SelectedIndex_GetValue_ReturnsDropdownValue()
        {
            _settingDropdown.SetOptions(new[] { "A", "B", "C" });
            _unityDropdown.value = 1;

            Assert.AreEqual(1, _settingDropdown.SelectedIndex);
        }

        [Test]
        public void SelectedIndex_SetBelowZero_ClampsToZero()
        {
            _settingDropdown.SetOptions(new[] { "A", "B", "C" });

            _settingDropdown.SelectedIndex = -1;

            Assert.AreEqual(0, _settingDropdown.SelectedIndex);
        }

        [Test]
        public void SelectedIndex_SetAboveMax_ClampsToMax()
        {
            _settingDropdown.SetOptions(new[] { "A", "B", "C" });

            _settingDropdown.SelectedIndex = 10;

            Assert.AreEqual(2, _settingDropdown.SelectedIndex);
        }

        [Test]
        public void SelectedText_ReturnsCurrentOptionText()
        {
            _settingDropdown.SetOptions(new[] { "First", "Second", "Third" });
            _settingDropdown.SelectedIndex = 1;

            Assert.AreEqual("Second", _settingDropdown.SelectedText);
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnValueChanged_SelectionChanged_FiresEvent()
        {
            _settingDropdown.SetOptions(new[] { "A", "B", "C" });

            bool eventFired = false;
            int eventValue = -1;

            _settingDropdown.OnValueChanged += (value) =>
            {
                eventFired = true;
                eventValue = value;
            };

            _settingDropdown.SelectedIndex = 2;

            Assert.IsTrue(eventFired);
            Assert.AreEqual(2, eventValue);
        }

        [Test]
        public void OnValueChanged_SameValue_DoesNotFireEvent()
        {
            _settingDropdown.SetOptions(new[] { "A", "B", "C" });
            _settingDropdown.SelectedIndex = 1;

            int eventCount = 0;
            _settingDropdown.OnValueChanged += (_) => eventCount++;

            _settingDropdown.SelectedIndex = 1;

            Assert.AreEqual(0, eventCount);
        }

        #endregion

        #region SetWithoutNotify Tests

        [Test]
        public void SetWithoutNotify_ChangesValue_DoesNotFireEvent()
        {
            _settingDropdown.SetOptions(new[] { "A", "B", "C" });

            bool eventFired = false;
            _settingDropdown.OnValueChanged += (_) => eventFired = true;

            _settingDropdown.SetWithoutNotify(2);

            Assert.IsFalse(eventFired);
            Assert.AreEqual(2, _settingDropdown.SelectedIndex);
        }

        #endregion

        #region Interactable Tests

        [Test]
        public void IsInteractable_SetFalse_DisablesDropdown()
        {
            _settingDropdown.IsInteractable = false;

            Assert.IsFalse(_unityDropdown.interactable);
        }

        [Test]
        public void IsInteractable_SetTrue_EnablesDropdown()
        {
            _unityDropdown.interactable = false;
            _settingDropdown.IsInteractable = true;

            Assert.IsTrue(_unityDropdown.interactable);
        }

        #endregion

        #region Font Size Tests

        [Test]
        public void UpdateFontSize_Compact_SetsCompactFont()
        {
            _settingDropdown.UpdateFontSize(ScreenCategory.Compact);

            Assert.AreEqual(UITheme.FontSizeCompact.Normal, _labelText.fontSize);
        }

        [Test]
        public void UpdateFontSize_Regular_SetsRegularFont()
        {
            _settingDropdown.UpdateFontSize(ScreenCategory.Regular);

            Assert.AreEqual(UITheme.FontSizeRegular.Normal, _labelText.fontSize);
        }

        [Test]
        public void UpdateFontSize_Large_SetsLargeFont()
        {
            _settingDropdown.UpdateFontSize(ScreenCategory.Large);

            Assert.AreEqual(UITheme.FontSizeLarge.Normal, _labelText.fontSize);
        }

        #endregion

        #region Enum Helper Tests

        [Test]
        public void SetOptionsFromEnum_PopulatesWithEnumValues()
        {
            _settingDropdown.SetOptionsFromEnum<TestEnum>();

            Assert.AreEqual(3, _unityDropdown.options.Count);
            Assert.AreEqual("ValueA", _unityDropdown.options[0].text);
            Assert.AreEqual("ValueB", _unityDropdown.options[1].text);
            Assert.AreEqual("ValueC", _unityDropdown.options[2].text);
        }

        [Test]
        public void GetSelectedEnum_ReturnsCorrectEnumValue()
        {
            _settingDropdown.SetOptionsFromEnum<TestEnum>();
            _settingDropdown.SelectedIndex = 1;

            Assert.AreEqual(TestEnum.ValueB, _settingDropdown.GetSelectedEnum<TestEnum>());
        }

        [Test]
        public void SetSelectedEnum_SelectsCorrectOption()
        {
            _settingDropdown.SetOptionsFromEnum<TestEnum>();

            _settingDropdown.SetSelectedEnum(TestEnum.ValueC);

            Assert.AreEqual(2, _settingDropdown.SelectedIndex);
        }

        private enum TestEnum
        {
            ValueA,
            ValueB,
            ValueC
        }

        #endregion
    }
}
