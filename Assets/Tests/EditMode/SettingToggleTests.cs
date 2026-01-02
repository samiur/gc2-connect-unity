// ABOUTME: Unit tests for the SettingToggle UI component.
// ABOUTME: Tests toggle functionality, value binding, and visual state changes.

using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpenRange.UI;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class SettingToggleTests
    {
        private GameObject _toggleGo;
        private SettingToggle _toggle;
        private Toggle _unityToggle;
        private TextMeshProUGUI _labelText;
        private Image _backgroundImage;

        [SetUp]
        public void SetUp()
        {
            _toggleGo = new GameObject("TestSettingToggle");

            // Create Toggle component
            _unityToggle = _toggleGo.AddComponent<Toggle>();
            _unityToggle.isOn = false;

            // Create label text
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(_toggleGo.transform);
            _labelText = labelGo.AddComponent<TextMeshProUGUI>();

            // Create background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(_toggleGo.transform);
            _backgroundImage = bgGo.AddComponent<Image>();

            // Add SettingToggle component
            _toggle = _toggleGo.AddComponent<SettingToggle>();
            _toggle.SetReferences(_unityToggle, _labelText, _backgroundImage);
        }

        [TearDown]
        public void TearDown()
        {
            if (_toggleGo != null)
            {
                UnityEngine.Object.DestroyImmediate(_toggleGo);
            }
        }

        #region Label Tests

        [Test]
        public void Label_SetValue_UpdatesText()
        {
            _toggle.Label = "Test Label";

            Assert.AreEqual("Test Label", _labelText.text);
        }

        [Test]
        public void Label_GetValue_ReturnsCurrentText()
        {
            _labelText.text = "Current Label";

            Assert.AreEqual("Current Label", _toggle.Label);
        }

        #endregion

        #region Value Tests

        [Test]
        public void IsOn_SetTrue_UpdatesToggle()
        {
            _toggle.IsOn = true;

            Assert.IsTrue(_unityToggle.isOn);
        }

        [Test]
        public void IsOn_SetFalse_UpdatesToggle()
        {
            _unityToggle.isOn = true;
            _toggle.IsOn = false;

            Assert.IsFalse(_unityToggle.isOn);
        }

        [Test]
        public void IsOn_GetValue_ReturnsToggleState()
        {
            _unityToggle.isOn = true;

            Assert.IsTrue(_toggle.IsOn);
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnValueChanged_ToggledOn_FiresEvent()
        {
            bool eventFired = false;
            bool eventValue = false;

            _toggle.OnValueChanged += (value) =>
            {
                eventFired = true;
                eventValue = value;
            };

            _toggle.IsOn = true;

            Assert.IsTrue(eventFired);
            Assert.IsTrue(eventValue);
        }

        [Test]
        public void OnValueChanged_ToggledOff_FiresEvent()
        {
            _toggle.IsOn = true;

            bool eventFired = false;
            bool eventValue = true;

            _toggle.OnValueChanged += (value) =>
            {
                eventFired = true;
                eventValue = value;
            };

            _toggle.IsOn = false;

            Assert.IsTrue(eventFired);
            Assert.IsFalse(eventValue);
        }

        [Test]
        public void OnValueChanged_SameValue_DoesNotFireEvent()
        {
            _toggle.IsOn = true;
            int eventCount = 0;

            _toggle.OnValueChanged += (_) => eventCount++;

            _toggle.IsOn = true;

            Assert.AreEqual(0, eventCount);
        }

        #endregion

        #region Interactable Tests

        [Test]
        public void IsInteractable_SetFalse_DisablesToggle()
        {
            _toggle.IsInteractable = false;

            Assert.IsFalse(_unityToggle.interactable);
        }

        [Test]
        public void IsInteractable_SetTrue_EnablesToggle()
        {
            _unityToggle.interactable = false;
            _toggle.IsInteractable = true;

            Assert.IsTrue(_unityToggle.interactable);
        }

        [Test]
        public void IsInteractable_GetValue_ReturnsToggleInteractable()
        {
            _unityToggle.interactable = false;

            Assert.IsFalse(_toggle.IsInteractable);
        }

        #endregion

        #region SetWithoutNotify Tests

        [Test]
        public void SetWithoutNotify_ChangesValue_DoesNotFireEvent()
        {
            bool eventFired = false;
            _toggle.OnValueChanged += (_) => eventFired = true;

            _toggle.SetWithoutNotify(true);

            Assert.IsFalse(eventFired);
            Assert.IsTrue(_toggle.IsOn);
        }

        [Test]
        public void SetWithoutNotify_True_UpdatesToggle()
        {
            _toggle.SetWithoutNotify(true);

            Assert.IsTrue(_unityToggle.isOn);
        }

        [Test]
        public void SetWithoutNotify_False_UpdatesToggle()
        {
            _unityToggle.isOn = true;
            _toggle.SetWithoutNotify(false);

            Assert.IsFalse(_unityToggle.isOn);
        }

        #endregion

        #region Font Size Tests

        [Test]
        public void UpdateFontSize_Compact_SetsSmallFont()
        {
            _toggle.UpdateFontSize(ScreenCategory.Compact);

            Assert.AreEqual(UITheme.FontSizeCompact.Normal, _labelText.fontSize);
        }

        [Test]
        public void UpdateFontSize_Regular_SetsNormalFont()
        {
            _toggle.UpdateFontSize(ScreenCategory.Regular);

            Assert.AreEqual(UITheme.FontSizeRegular.Normal, _labelText.fontSize);
        }

        [Test]
        public void UpdateFontSize_Large_SetsLargeFont()
        {
            _toggle.UpdateFontSize(ScreenCategory.Large);

            Assert.AreEqual(UITheme.FontSizeLarge.Normal, _labelText.fontSize);
        }

        #endregion
    }
}
