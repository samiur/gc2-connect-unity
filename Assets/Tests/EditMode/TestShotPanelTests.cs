// ABOUTME: Unit tests for the TestShotPanel UI component.
// ABOUTME: Tests panel visibility, preset values, GC2ShotData creation, event firing, and club data toggle.

using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpenRange.UI;
using OpenRange.GC2;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class TestShotPanelTests
    {
        private GameObject _panelGo;
        private TestShotPanel _testShotPanel;
        private CanvasGroup _canvasGroup;

        // Sliders
        private SettingSlider _ballSpeedSlider;
        private SettingSlider _launchAngleSlider;
        private SettingSlider _directionSlider;
        private SettingSlider _backSpinSlider;
        private SettingSlider _sideSpinSlider;

        // Club data
        private SettingToggle _clubDataToggle;

        // Fire button
        private Button _fireShotButton;

        [SetUp]
        public void SetUp()
        {
            _panelGo = new GameObject("TestShotPanel");

            // Create canvas group
            _canvasGroup = _panelGo.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;

            // Create sliders
            _ballSpeedSlider = CreateSlider("BallSpeedSlider");
            _launchAngleSlider = CreateSlider("LaunchAngleSlider");
            _directionSlider = CreateSlider("DirectionSlider");
            _backSpinSlider = CreateSlider("BackSpinSlider");
            _sideSpinSlider = CreateSlider("SideSpinSlider");

            // Create club data toggle
            _clubDataToggle = CreateToggle("ClubDataToggle");

            // Create fire button
            var fireBtnGo = new GameObject("FireShotButton");
            fireBtnGo.transform.SetParent(_panelGo.transform);
            _fireShotButton = fireBtnGo.AddComponent<Button>();

            // Add TestShotPanel component
            _testShotPanel = _panelGo.AddComponent<TestShotPanel>();
            _testShotPanel.SetReferences(
                canvasGroup: _canvasGroup,
                ballSpeedSlider: _ballSpeedSlider,
                launchAngleSlider: _launchAngleSlider,
                directionSlider: _directionSlider,
                backSpinSlider: _backSpinSlider,
                sideSpinSlider: _sideSpinSlider,
                clubDataToggle: _clubDataToggle,
                fireShotButton: _fireShotButton
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
        public void IsVisible_WhenCreated_ReturnsFalse()
        {
            Assert.IsFalse(_testShotPanel.IsVisible);
        }

        [Test]
        public void Show_SetsIsVisibleTrue()
        {
            _testShotPanel.Show();

            Assert.IsTrue(_testShotPanel.IsVisible);
        }

        [Test]
        public void Show_EnablesCanvasGroupInteraction()
        {
            _testShotPanel.Show();

            Assert.IsTrue(_canvasGroup.interactable);
            Assert.IsTrue(_canvasGroup.blocksRaycasts);
        }

        [Test]
        public void Hide_SetsIsVisibleFalse()
        {
            _testShotPanel.SetVisibleImmediate(true);

            _testShotPanel.Hide();

            Assert.IsFalse(_testShotPanel.IsVisible);
        }

        [Test]
        public void Toggle_WhenHidden_Shows()
        {
            _testShotPanel.SetVisibleImmediate(false);

            _testShotPanel.Toggle();

            Assert.IsTrue(_testShotPanel.IsVisible);
        }

        [Test]
        public void Toggle_WhenVisible_Hides()
        {
            _testShotPanel.SetVisibleImmediate(true);

            _testShotPanel.Toggle();

            Assert.IsFalse(_testShotPanel.IsVisible);
        }

        [Test]
        public void SetVisibleImmediate_SetsAlphaToOne()
        {
            _testShotPanel.SetVisibleImmediate(true);

            Assert.AreEqual(1f, _canvasGroup.alpha);
        }

        [Test]
        public void SetVisibleImmediate_SetsAlphaToZero()
        {
            _testShotPanel.SetVisibleImmediate(false);

            Assert.AreEqual(0f, _canvasGroup.alpha);
        }

        #endregion

        #region Visibility Event Tests

        [Test]
        public void OnVisibilityChanged_WhenShown_FiresWithTrue()
        {
            bool? receivedValue = null;
            _testShotPanel.OnVisibilityChanged += (visible) => receivedValue = visible;

            _testShotPanel.Show();

            Assert.IsTrue(receivedValue.HasValue);
            Assert.IsTrue(receivedValue.Value);
        }

        [Test]
        public void OnVisibilityChanged_WhenHidden_FiresWithFalse()
        {
            _testShotPanel.SetVisibleImmediate(true);
            bool? receivedValue = null;
            _testShotPanel.OnVisibilityChanged += (visible) => receivedValue = visible;

            _testShotPanel.Hide();

            Assert.IsTrue(receivedValue.HasValue);
            Assert.IsFalse(receivedValue.Value);
        }

        [Test]
        public void OnVisibilityChanged_DoesNotFireIfAlreadyVisible()
        {
            _testShotPanel.SetVisibleImmediate(true);
            int eventCount = 0;
            _testShotPanel.OnVisibilityChanged += (visible) => eventCount++;

            _testShotPanel.Show(); // Should not fire since already visible

            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void OnVisibilityChanged_DoesNotFireIfAlreadyHidden()
        {
            int eventCount = 0;
            _testShotPanel.OnVisibilityChanged += (visible) => eventCount++;

            _testShotPanel.Hide(); // Should not fire since already hidden

            Assert.AreEqual(0, eventCount);
        }

        #endregion

        #region Preset Value Tests

        [Test]
        public void DriverPreset_HasCorrectBallSpeed()
        {
            Assert.AreEqual(167f, TestShotPanel.Presets.Driver.BallSpeed);
        }

        [Test]
        public void DriverPreset_HasCorrectLaunchAngle()
        {
            Assert.AreEqual(10.9f, TestShotPanel.Presets.Driver.LaunchAngle);
        }

        [Test]
        public void DriverPreset_HasCorrectBackSpin()
        {
            Assert.AreEqual(2686f, TestShotPanel.Presets.Driver.BackSpin);
        }

        [Test]
        public void SevenIronPreset_HasCorrectBallSpeed()
        {
            Assert.AreEqual(120f, TestShotPanel.Presets.SevenIron.BallSpeed);
        }

        [Test]
        public void SevenIronPreset_HasCorrectLaunchAngle()
        {
            Assert.AreEqual(16.3f, TestShotPanel.Presets.SevenIron.LaunchAngle);
        }

        [Test]
        public void SevenIronPreset_HasCorrectBackSpin()
        {
            Assert.AreEqual(7097f, TestShotPanel.Presets.SevenIron.BackSpin);
        }

        [Test]
        public void WedgePreset_HasCorrectBallSpeed()
        {
            Assert.AreEqual(102f, TestShotPanel.Presets.Wedge.BallSpeed);
        }

        [Test]
        public void WedgePreset_HasCorrectLaunchAngle()
        {
            Assert.AreEqual(24.2f, TestShotPanel.Presets.Wedge.LaunchAngle);
        }

        [Test]
        public void WedgePreset_HasCorrectBackSpin()
        {
            Assert.AreEqual(9304f, TestShotPanel.Presets.Wedge.BackSpin);
        }

        [Test]
        public void HookPreset_HasNegativeSideSpin()
        {
            Assert.AreEqual(-1500f, TestShotPanel.Presets.Hook.SideSpin);
        }

        [Test]
        public void SlicePreset_HasPositiveSideSpin()
        {
            Assert.AreEqual(1500f, TestShotPanel.Presets.Slice.SideSpin);
        }

        #endregion

        #region ApplyPreset Tests

        [Test]
        public void ApplyPreset_Driver_SetsBallSpeed()
        {
            _testShotPanel.ApplyPreset(TestShotPanel.Presets.Driver);

            Assert.AreEqual(167f, _testShotPanel.BallSpeed);
        }

        [Test]
        public void ApplyPreset_Driver_SetsLaunchAngle()
        {
            _testShotPanel.ApplyPreset(TestShotPanel.Presets.Driver);

            Assert.AreEqual(10.9f, _testShotPanel.LaunchAngle);
        }

        [Test]
        public void ApplyPreset_Driver_SetsDirection()
        {
            _testShotPanel.ApplyPreset(TestShotPanel.Presets.Driver);

            Assert.AreEqual(0f, _testShotPanel.Direction);
        }

        [Test]
        public void ApplyPreset_Driver_SetsBackSpin()
        {
            _testShotPanel.ApplyPreset(TestShotPanel.Presets.Driver);

            Assert.AreEqual(2686f, _testShotPanel.BackSpin);
        }

        [Test]
        public void ApplyPreset_Driver_SetsSideSpin()
        {
            _testShotPanel.ApplyPreset(TestShotPanel.Presets.Driver);

            Assert.AreEqual(0f, _testShotPanel.SideSpin);
        }

        [Test]
        public void ApplyPreset_Hook_SetsNegativeSideSpin()
        {
            _testShotPanel.ApplyPreset(TestShotPanel.Presets.Hook);

            Assert.AreEqual(-1500f, _testShotPanel.SideSpin);
        }

        [Test]
        public void ApplyPreset_Slice_SetsPositiveSideSpin()
        {
            _testShotPanel.ApplyPreset(TestShotPanel.Presets.Slice);

            Assert.AreEqual(1500f, _testShotPanel.SideSpin);
        }

        #endregion

        #region CreateShotData Tests

        [Test]
        public void CreateShotData_ReturnsGC2ShotData()
        {
            var shotData = _testShotPanel.CreateShotData();

            Assert.IsNotNull(shotData);
            Assert.IsInstanceOf<GC2ShotData>(shotData);
        }

        [Test]
        public void CreateShotData_HasCorrectBallSpeed()
        {
            _testShotPanel.ApplyPreset(TestShotPanel.Presets.Driver);

            var shotData = _testShotPanel.CreateShotData();

            Assert.AreEqual(167f, shotData.BallSpeed);
        }

        [Test]
        public void CreateShotData_HasCorrectLaunchAngle()
        {
            _testShotPanel.ApplyPreset(TestShotPanel.Presets.SevenIron);

            var shotData = _testShotPanel.CreateShotData();

            Assert.AreEqual(16.3f, shotData.LaunchAngle);
        }

        [Test]
        public void CreateShotData_HasCorrectBackSpin()
        {
            _testShotPanel.ApplyPreset(TestShotPanel.Presets.Wedge);

            var shotData = _testShotPanel.CreateShotData();

            Assert.AreEqual(9304f, shotData.BackSpin);
        }

        [Test]
        public void CreateShotData_HasCorrectSideSpin()
        {
            _testShotPanel.ApplyPreset(TestShotPanel.Presets.Hook);

            var shotData = _testShotPanel.CreateShotData();

            Assert.AreEqual(-1500f, shotData.SideSpin);
        }

        [Test]
        public void CreateShotData_CalculatesTotalSpin()
        {
            _testShotPanel.ApplyPreset(TestShotPanel.Presets.Hook);

            var shotData = _testShotPanel.CreateShotData();

            // TotalSpin = sqrt(backSpin^2 + sideSpin^2)
            float expectedTotal = Mathf.Sqrt(3000f * 3000f + 1500f * 1500f);
            Assert.AreEqual(expectedTotal, shotData.TotalSpin, 0.01f);
        }

        [Test]
        public void CreateShotData_HasTimestamp()
        {
            var shotData = _testShotPanel.CreateShotData();

            Assert.Greater(shotData.Timestamp, 0);
        }

        [Test]
        public void CreateShotData_HasClubDataFalseByDefault()
        {
            var shotData = _testShotPanel.CreateShotData();

            Assert.IsFalse(shotData.HasClubData);
        }

        #endregion

        #region Club Data Toggle Tests

        [Test]
        public void IncludeClubData_DefaultsFalse()
        {
            Assert.IsFalse(_testShotPanel.IncludeClubData);
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnTestShotFired_WhenFireShotCalled_FiresEvent()
        {
            GC2ShotData receivedData = null;
            _testShotPanel.OnTestShotFired += (data) => receivedData = data;

            _testShotPanel.FireShot();

            Assert.IsNotNull(receivedData);
        }

        [Test]
        public void OnTestShotFired_ContainsCorrectBallSpeed()
        {
            _testShotPanel.ApplyPreset(TestShotPanel.Presets.Driver);
            GC2ShotData receivedData = null;
            _testShotPanel.OnTestShotFired += (data) => receivedData = data;

            _testShotPanel.FireShot();

            Assert.AreEqual(167f, receivedData.BallSpeed);
        }

        #endregion

        #region Slider Range Constants Tests

        [Test]
        public void MinBallSpeed_Is50()
        {
            Assert.AreEqual(50f, TestShotPanel.MinBallSpeed);
        }

        [Test]
        public void MaxBallSpeed_Is200()
        {
            Assert.AreEqual(200f, TestShotPanel.MaxBallSpeed);
        }

        [Test]
        public void MinLaunchAngle_Is0()
        {
            Assert.AreEqual(0f, TestShotPanel.MinLaunchAngle);
        }

        [Test]
        public void MaxLaunchAngle_Is45()
        {
            Assert.AreEqual(45f, TestShotPanel.MaxLaunchAngle);
        }

        [Test]
        public void MinDirection_IsNegative20()
        {
            Assert.AreEqual(-20f, TestShotPanel.MinDirection);
        }

        [Test]
        public void MaxDirection_Is20()
        {
            Assert.AreEqual(20f, TestShotPanel.MaxDirection);
        }

        [Test]
        public void MinBackSpin_Is0()
        {
            Assert.AreEqual(0f, TestShotPanel.MinBackSpin);
        }

        [Test]
        public void MaxBackSpin_Is12000()
        {
            Assert.AreEqual(12000f, TestShotPanel.MaxBackSpin);
        }

        [Test]
        public void MinSideSpin_IsNegative3000()
        {
            Assert.AreEqual(-3000f, TestShotPanel.MinSideSpin);
        }

        [Test]
        public void MaxSideSpin_Is3000()
        {
            Assert.AreEqual(3000f, TestShotPanel.MaxSideSpin);
        }

        #endregion

        #region Layout Constants Tests

        [Test]
        public void PanelWidth_Is300()
        {
            Assert.AreEqual(300f, TestShotPanel.PanelWidth);
        }

        [Test]
        public void PanelPadding_Is12()
        {
            Assert.AreEqual(12f, TestShotPanel.PanelPadding);
        }

        [Test]
        public void AnimationDuration_Is025()
        {
            Assert.AreEqual(0.25f, TestShotPanel.AnimationDuration);
        }

        [Test]
        public void FireButtonHeight_Is48()
        {
            Assert.AreEqual(48f, TestShotPanel.FireButtonHeight);
        }

        #endregion

        #region Preset Name Tests

        [Test]
        public void DriverPreset_HasCorrectName()
        {
            Assert.AreEqual("Driver", TestShotPanel.Presets.Driver.Name);
        }

        [Test]
        public void SevenIronPreset_HasCorrectName()
        {
            Assert.AreEqual("7-Iron", TestShotPanel.Presets.SevenIron.Name);
        }

        [Test]
        public void WedgePreset_HasCorrectName()
        {
            Assert.AreEqual("Wedge", TestShotPanel.Presets.Wedge.Name);
        }

        [Test]
        public void HookPreset_HasCorrectName()
        {
            Assert.AreEqual("Hook", TestShotPanel.Presets.Hook.Name);
        }

        [Test]
        public void SlicePreset_HasCorrectName()
        {
            Assert.AreEqual("Slice", TestShotPanel.Presets.Slice.Name);
        }

        #endregion

        #region Club Data Range Constants Tests

        [Test]
        public void MinClubSpeed_Is60()
        {
            Assert.AreEqual(60f, TestShotPanel.MinClubSpeed);
        }

        [Test]
        public void MaxClubSpeed_Is130()
        {
            Assert.AreEqual(130f, TestShotPanel.MaxClubSpeed);
        }

        [Test]
        public void MinAttackAngle_IsNegative10()
        {
            Assert.AreEqual(-10f, TestShotPanel.MinAttackAngle);
        }

        [Test]
        public void MaxAttackAngle_Is10()
        {
            Assert.AreEqual(10f, TestShotPanel.MaxAttackAngle);
        }

        [Test]
        public void MinDynamicLoft_Is5()
        {
            Assert.AreEqual(5f, TestShotPanel.MinDynamicLoft);
        }

        [Test]
        public void MaxDynamicLoft_Is50()
        {
            Assert.AreEqual(50f, TestShotPanel.MaxDynamicLoft);
        }

        #endregion
    }
}
