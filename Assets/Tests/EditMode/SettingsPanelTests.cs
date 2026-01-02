// ABOUTME: Unit tests for the SettingsPanel UI component.
// ABOUTME: Tests panel visibility, settings binding, and section management.

using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpenRange.UI;
using OpenRange.Core;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class SettingsPanelTests
    {
        private GameObject _panelGo;
        private SettingsPanel _settingsPanel;
        private CanvasGroup _canvasGroup;
        private Button _closeButton;
        private Button _resetButton;

        // Setting controls
        private SettingDropdown _qualityDropdown;
        private SettingDropdown _frameRateDropdown;
        private SettingDropdown _distanceUnitDropdown;
        private SettingDropdown _speedUnitDropdown;
        private SettingDropdown _tempUnitDropdown;
        private SettingSlider _temperatureSlider;
        private SettingSlider _elevationSlider;
        private SettingSlider _humiditySlider;
        private SettingToggle _windEnabledToggle;
        private SettingSlider _windSpeedSlider;
        private SettingSlider _windDirectionSlider;
        private SettingToggle _autoConnectToggle;
        private SettingSlider _masterVolumeSlider;
        private SettingSlider _effectsVolumeSlider;

        [SetUp]
        public void SetUp()
        {
            _panelGo = new GameObject("TestSettingsPanel");

            // Create canvas group
            _canvasGroup = _panelGo.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;

            // Create close button
            var closeBtnGo = new GameObject("CloseButton");
            closeBtnGo.transform.SetParent(_panelGo.transform);
            _closeButton = closeBtnGo.AddComponent<Button>();

            // Create reset button
            var resetBtnGo = new GameObject("ResetButton");
            resetBtnGo.transform.SetParent(_panelGo.transform);
            _resetButton = resetBtnGo.AddComponent<Button>();

            // Create setting controls
            _qualityDropdown = CreateDropdown("QualityDropdown");
            _frameRateDropdown = CreateDropdown("FrameRateDropdown");
            _distanceUnitDropdown = CreateDropdown("DistanceUnitDropdown");
            _speedUnitDropdown = CreateDropdown("SpeedUnitDropdown");
            _tempUnitDropdown = CreateDropdown("TempUnitDropdown");
            _temperatureSlider = CreateSlider("TemperatureSlider");
            _elevationSlider = CreateSlider("ElevationSlider");
            _humiditySlider = CreateSlider("HumiditySlider");
            _windEnabledToggle = CreateToggle("WindEnabledToggle");
            _windSpeedSlider = CreateSlider("WindSpeedSlider");
            _windDirectionSlider = CreateSlider("WindDirectionSlider");
            _autoConnectToggle = CreateToggle("AutoConnectToggle");
            _masterVolumeSlider = CreateSlider("MasterVolumeSlider");
            _effectsVolumeSlider = CreateSlider("EffectsVolumeSlider");

            // Add SettingsPanel component
            _settingsPanel = _panelGo.AddComponent<SettingsPanel>();
            _settingsPanel.SetReferences(
                canvasGroup: _canvasGroup,
                closeButton: _closeButton,
                resetButton: _resetButton,
                qualityDropdown: _qualityDropdown,
                frameRateDropdown: _frameRateDropdown,
                distanceUnitDropdown: _distanceUnitDropdown,
                speedUnitDropdown: _speedUnitDropdown,
                tempUnitDropdown: _tempUnitDropdown,
                temperatureSlider: _temperatureSlider,
                elevationSlider: _elevationSlider,
                humiditySlider: _humiditySlider,
                windEnabledToggle: _windEnabledToggle,
                windSpeedSlider: _windSpeedSlider,
                windDirectionSlider: _windDirectionSlider,
                autoConnectToggle: _autoConnectToggle,
                masterVolumeSlider: _masterVolumeSlider,
                effectsVolumeSlider: _effectsVolumeSlider
            );
        }

        [TearDown]
        public void TearDown()
        {
            if (_panelGo != null)
            {
                UnityEngine.Object.DestroyImmediate(_panelGo);
            }
        }

        #region Helper Methods

        private SettingDropdown CreateDropdown(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_panelGo.transform);

            // Create TMP_Dropdown with required structure
            var templateGo = new GameObject("Template");
            templateGo.transform.SetParent(go.transform);
            templateGo.AddComponent<RectTransform>();

            var dropdown = go.AddComponent<TMP_Dropdown>();
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform);
            var labelText = labelGo.AddComponent<TextMeshProUGUI>();

            var settingDropdown = go.AddComponent<SettingDropdown>();
            settingDropdown.SetReferences(dropdown, labelText);

            return settingDropdown;
        }

        private SettingSlider CreateSlider(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_panelGo.transform);

            var slider = go.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 100;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform);
            var labelText = labelGo.AddComponent<TextMeshProUGUI>();

            var valueGo = new GameObject("Value");
            valueGo.transform.SetParent(go.transform);
            var valueText = valueGo.AddComponent<TextMeshProUGUI>();

            var settingSlider = go.AddComponent<SettingSlider>();
            settingSlider.SetReferences(slider, labelText, valueText);

            return settingSlider;
        }

        private SettingToggle CreateToggle(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_panelGo.transform);

            var toggle = go.AddComponent<Toggle>();

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform);
            var labelText = labelGo.AddComponent<TextMeshProUGUI>();

            var settingToggle = go.AddComponent<SettingToggle>();
            settingToggle.SetReferences(toggle, labelText);

            return settingToggle;
        }

        #endregion

        #region Visibility Tests

        [Test]
        public void IsVisible_WhenAlphaOne_ReturnsTrue()
        {
            _canvasGroup.alpha = 1f;

            Assert.IsTrue(_settingsPanel.IsVisible);
        }

        [Test]
        public void IsVisible_WhenAlphaZero_ReturnsFalse()
        {
            _canvasGroup.alpha = 0f;

            Assert.IsFalse(_settingsPanel.IsVisible);
        }

        [Test]
        public void Show_SetsAlphaToOne()
        {
            _canvasGroup.alpha = 0f;

            _settingsPanel.Show();

            Assert.AreEqual(1f, _canvasGroup.alpha);
        }

        [Test]
        public void Show_EnablesInteraction()
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            _settingsPanel.Show();

            Assert.IsTrue(_canvasGroup.interactable);
            Assert.IsTrue(_canvasGroup.blocksRaycasts);
        }

        [Test]
        public void Hide_SetsAlphaToZero()
        {
            _canvasGroup.alpha = 1f;

            _settingsPanel.Hide();

            Assert.AreEqual(0f, _canvasGroup.alpha);
        }

        [Test]
        public void Hide_DisablesInteraction()
        {
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            _settingsPanel.Hide();

            Assert.IsFalse(_canvasGroup.interactable);
            Assert.IsFalse(_canvasGroup.blocksRaycasts);
        }

        [Test]
        public void Toggle_WhenHidden_Shows()
        {
            _canvasGroup.alpha = 0f;

            _settingsPanel.Toggle();

            Assert.AreEqual(1f, _canvasGroup.alpha);
        }

        [Test]
        public void Toggle_WhenVisible_Hides()
        {
            _canvasGroup.alpha = 1f;

            _settingsPanel.Toggle();

            Assert.AreEqual(0f, _canvasGroup.alpha);
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnPanelShown_WhenShown_FiresEvent()
        {
            bool eventFired = false;
            _settingsPanel.OnPanelShown += () => eventFired = true;

            _settingsPanel.Show();

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void OnPanelHidden_WhenHidden_FiresEvent()
        {
            _settingsPanel.Show();

            bool eventFired = false;
            _settingsPanel.OnPanelHidden += () => eventFired = true;

            _settingsPanel.Hide();

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void OnResetClicked_WhenReset_FiresEvent()
        {
            bool eventFired = false;
            _settingsPanel.OnResetClicked += () => eventFired = true;

            // Simulate reset button click
            _settingsPanel.SimulateResetClick();

            Assert.IsTrue(eventFired);
        }

        #endregion

        #region Control Initialization Tests

        [Test]
        public void QualityDropdown_IsNotNull()
        {
            Assert.IsNotNull(_settingsPanel.QualityDropdown);
        }

        [Test]
        public void FrameRateDropdown_IsNotNull()
        {
            Assert.IsNotNull(_settingsPanel.FrameRateDropdown);
        }

        [Test]
        public void DistanceUnitDropdown_IsNotNull()
        {
            Assert.IsNotNull(_settingsPanel.DistanceUnitDropdown);
        }

        [Test]
        public void SpeedUnitDropdown_IsNotNull()
        {
            Assert.IsNotNull(_settingsPanel.SpeedUnitDropdown);
        }

        [Test]
        public void TemperatureSlider_IsNotNull()
        {
            Assert.IsNotNull(_settingsPanel.TemperatureSlider);
        }

        [Test]
        public void ElevationSlider_IsNotNull()
        {
            Assert.IsNotNull(_settingsPanel.ElevationSlider);
        }

        [Test]
        public void HumiditySlider_IsNotNull()
        {
            Assert.IsNotNull(_settingsPanel.HumiditySlider);
        }

        [Test]
        public void WindEnabledToggle_IsNotNull()
        {
            Assert.IsNotNull(_settingsPanel.WindEnabledToggle);
        }

        [Test]
        public void WindSpeedSlider_IsNotNull()
        {
            Assert.IsNotNull(_settingsPanel.WindSpeedSlider);
        }

        [Test]
        public void WindDirectionSlider_IsNotNull()
        {
            Assert.IsNotNull(_settingsPanel.WindDirectionSlider);
        }

        [Test]
        public void AutoConnectToggle_IsNotNull()
        {
            Assert.IsNotNull(_settingsPanel.AutoConnectToggle);
        }

        [Test]
        public void MasterVolumeSlider_IsNotNull()
        {
            Assert.IsNotNull(_settingsPanel.MasterVolumeSlider);
        }

        [Test]
        public void EffectsVolumeSlider_IsNotNull()
        {
            Assert.IsNotNull(_settingsPanel.EffectsVolumeSlider);
        }

        #endregion

        #region Wind Control State Tests

        [Test]
        public void WindEnabled_WhenFalse_DisablesWindSpeedSlider()
        {
            _windEnabledToggle.IsOn = false;

            _settingsPanel.UpdateWindControlStates();

            Assert.IsFalse(_windSpeedSlider.IsInteractable);
        }

        [Test]
        public void WindEnabled_WhenFalse_DisablesWindDirectionSlider()
        {
            _windEnabledToggle.IsOn = false;

            _settingsPanel.UpdateWindControlStates();

            Assert.IsFalse(_windDirectionSlider.IsInteractable);
        }

        [Test]
        public void WindEnabled_WhenTrue_EnablesWindSpeedSlider()
        {
            _windEnabledToggle.IsOn = true;

            _settingsPanel.UpdateWindControlStates();

            Assert.IsTrue(_windSpeedSlider.IsInteractable);
        }

        [Test]
        public void WindEnabled_WhenTrue_EnablesWindDirectionSlider()
        {
            _windEnabledToggle.IsOn = true;

            _settingsPanel.UpdateWindControlStates();

            Assert.IsTrue(_windDirectionSlider.IsInteractable);
        }

        #endregion
    }
}
