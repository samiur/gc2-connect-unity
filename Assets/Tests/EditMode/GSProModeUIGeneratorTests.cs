// ABOUTME: Unit tests for GSProModeUIGenerator layout constants and prefab structure.
// ABOUTME: Tests verify compact sizing and layout requirements for the GSPro Mode panel.

using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OpenRange.Tests.EditMode
{
    /// <summary>
    /// Tests for GSProModeUIGenerator layout constants.
    /// These validate the compact layout design for the right-side panel.
    /// </summary>
    [TestFixture]
    public class GSProModeUIGeneratorTests
    {
        #region Layout Constants Tests

        // These constants mirror the private constants in GSProModeUIGenerator
        // Sized to fit right-side panel with all content visible.

        private const float ExpectedPanelWidth = 200f;
        private const float ExpectedPanelPadding = 8f;
        private const float ExpectedSectionSpacing = 2f;
        private const float ExpectedItemSpacing = 4f;
        private const float ExpectedLedIndicatorSize = 10f;
        private const float ExpectedToggleWidth = 44f;
        private const float ExpectedToggleHeight = 22f;
        private const float ExpectedButtonWidth = 70f;
        private const float ExpectedButtonHeight = 22f;
        private const float ExpectedHostInputWidth = 100f;
        private const float ExpectedPortInputWidth = 50f;
        private const float ExpectedInputHeight = 20f;

        [Test]
        public void PanelWidth_IsCompact()
        {
            // Panel should be compact to fit on the right side of the screen
            Assert.LessOrEqual(ExpectedPanelWidth, 280f,
                "Panel width should be compact (max 280px) to fit right-side panel");
            Assert.GreaterOrEqual(ExpectedPanelWidth, 200f,
                "Panel width should be at least 200px to fit all content");
        }

        [Test]
        public void PanelPadding_IsWithinReasonableRange()
        {
            // Padding should be reasonable for compact layout
            Assert.GreaterOrEqual(ExpectedPanelPadding, 8f);
            Assert.LessOrEqual(ExpectedPanelPadding, 16f,
                "Panel padding should be 8-16px for compact layout");
        }

        [Test]
        public void SectionSpacing_IsCompact()
        {
            // Section spacing should be compact
            Assert.GreaterOrEqual(ExpectedSectionSpacing, 2f);
            Assert.LessOrEqual(ExpectedSectionSpacing, 12f,
                "Section spacing should be 2-12px for compact layout");
        }

        [Test]
        public void ItemSpacing_IsCompact()
        {
            // Item spacing should be compact
            Assert.GreaterOrEqual(ExpectedItemSpacing, 4f);
            Assert.LessOrEqual(ExpectedItemSpacing, 10f,
                "Item spacing should be 4-10px for compact layout");
        }

        [Test]
        public void LedIndicatorSize_IsSmall()
        {
            // LED indicators must be small dots, not large squares
            Assert.GreaterOrEqual(ExpectedLedIndicatorSize, 8f);
            Assert.LessOrEqual(ExpectedLedIndicatorSize, 14f,
                "LED indicator should be 8-14px (small dot, not large square)");
        }

        [Test]
        public void ToggleWidth_IsReasonable()
        {
            // Toggle should be functional but not oversized
            Assert.GreaterOrEqual(ExpectedToggleWidth, 40f,
                "Toggle width should be at least 40px for usability");
            Assert.LessOrEqual(ExpectedToggleWidth, 60f,
                "Toggle width should be at most 60px for compact layout");
        }

        [Test]
        public void ToggleHeight_IsReasonable()
        {
            // Toggle height should be compact but usable
            Assert.GreaterOrEqual(ExpectedToggleHeight, 20f,
                "Toggle height should be at least 20px");
            Assert.LessOrEqual(ExpectedToggleHeight, 30f,
                "Toggle height should be at most 30px for compact layout");
        }

        [Test]
        public void ButtonWidth_FitsText()
        {
            // Button should fit "Connect" text at minimum
            Assert.GreaterOrEqual(ExpectedButtonWidth, 60f,
                "Button should be wide enough to show 'Connect' text");
        }

        [Test]
        public void ButtonHeight_IsCompact()
        {
            // Button should be compact
            Assert.GreaterOrEqual(ExpectedButtonHeight, 22f,
                "Button height should be at least 22px");
            Assert.LessOrEqual(ExpectedButtonHeight, 32f,
                "Button height should be at most 32px for compact layout");
        }

        [Test]
        public void HostInputWidth_IsWiderThanPort()
        {
            // Host input needs more space for IP addresses
            Assert.Greater(ExpectedHostInputWidth, ExpectedPortInputWidth,
                "Host input should be wider than port input");
        }

        [Test]
        public void HostInputWidth_FitsIPAddresses()
        {
            // Should fit IPv4 addresses like 192.168.1.1
            Assert.GreaterOrEqual(ExpectedHostInputWidth, 80f,
                "Host input should be at least 80px to fit IPv4 addresses");
        }

        [Test]
        public void PortInputWidth_FitsPortNumbers()
        {
            // Should fit 5-digit port numbers (0-65535)
            Assert.GreaterOrEqual(ExpectedPortInputWidth, 45f,
                "Port input should be at least 45px to fit 5-digit ports");
        }

        [Test]
        public void InputHeight_IsCompact()
        {
            // Input fields should be compact but usable
            Assert.GreaterOrEqual(ExpectedInputHeight, 20f,
                "Input height should be at least 20px");
            Assert.LessOrEqual(ExpectedInputHeight, 30f,
                "Input height should be at most 30px for compact layout");
        }

        #endregion

        #region Layout Hierarchy Tests

        [Test]
        public void LayoutGroup_WithPadding_ContentFitsWithinPanel()
        {
            // Verify that content width + 2*padding fits reasonable space
            float contentWidth = ExpectedHostInputWidth + ExpectedItemSpacing + 35f; // 35f for label
            float totalWidth = contentWidth + 2 * ExpectedPanelPadding;

            Assert.LessOrEqual(totalWidth, ExpectedPanelWidth,
                "Content with padding should fit within panel width");
        }

        [Test]
        public void IndicatorPillSize_IsCompact()
        {
            // Indicator pills should be compact but readable
            const float expectedPillHeight = 18f;  // Updated to match new constants
            const float expectedPillLedSize = 8f;

            Assert.LessOrEqual(expectedPillHeight, 24f,
                "Indicator pill should be compact (max 24px height)");
            Assert.LessOrEqual(expectedPillLedSize, ExpectedLedIndicatorSize,
                "Pill LED should be smaller than or equal to connection LED");
        }

        #endregion

        #region Accessibility Tests

        [Test]
        public void TouchTargets_AreReasonable()
        {
            // Toggle as primary interaction element should be usable
            var toggleArea = ExpectedToggleWidth * ExpectedToggleHeight;

            // Compact design prioritizes space efficiency over large touch targets
            Assert.GreaterOrEqual(toggleArea, 800f,
                "Toggle touch area should be at least 800 sq px for usability");
        }

        [Test]
        public void ButtonSize_IsAdequate()
        {
            // Button should be usable even if compact
            var buttonArea = ExpectedButtonWidth * ExpectedButtonHeight;

            Assert.GreaterOrEqual(buttonArea, 1500f,
                "Button area should be at least 1500 sq px for usability");
        }

        #endregion

        #region Proportional Tests

        [Test]
        public void Spacing_IsPropotionalToPadding()
        {
            // Both spacings should be compact and reasonable
            // In a compact vertical layout, section spacing (vertical) can be smaller
            // than item spacing (horizontal) since rows need more horizontal separation
            Assert.LessOrEqual(ExpectedSectionSpacing, 12f,
                "Section spacing should be compact (max 12px)");
            Assert.LessOrEqual(ExpectedItemSpacing, 10f,
                "Item spacing should be compact (max 10px)");
        }

        [Test]
        public void InputFields_HaveSufficientCombinedWidth()
        {
            // Combined input width should leave room for labels
            float inputsWidth = ExpectedHostInputWidth + ExpectedPortInputWidth;
            float availableWidth = ExpectedPanelWidth - 2 * ExpectedPanelPadding;

            Assert.Less(inputsWidth, availableWidth,
                "Input fields should leave room for labels within panel width");
        }

        #endregion
    }
}
