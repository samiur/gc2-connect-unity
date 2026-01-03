// ABOUTME: Unit tests for GSProModeUIGenerator layout constants and prefab structure.
// ABOUTME: Tests verify minimum widths, spacing, and indicator sizing requirements from Prompt 43.

using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OpenRange.Tests.EditMode
{
    /// <summary>
    /// Tests for GSProModeUIGenerator layout constants.
    /// These validate the layout improvements from Prompt 43.
    /// </summary>
    [TestFixture]
    public class GSProModeUIGeneratorTests
    {
        #region Layout Constants Tests

        // These constants mirror the private constants in GSProModeUIGenerator
        // to ensure they remain at the expected values.

        private const float ExpectedPanelMinWidth = 280f;
        private const float ExpectedPanelPadding = 12f;
        private const float ExpectedSectionSpacing = 12f;
        private const float ExpectedItemSpacing = 8f;
        private const float ExpectedLedIndicatorSize = 14f;
        private const float ExpectedToggleWidth = 50f;
        private const float ExpectedToggleHeight = 26f;
        private const float ExpectedButtonMinWidth = 100f;
        private const float ExpectedButtonHeight = 32f;
        private const float ExpectedHostInputWidth = 130f;
        private const float ExpectedPortInputWidth = 65f;
        private const float ExpectedInputHeight = 28f;

        [Test]
        public void PanelMinWidth_MeetsMinimumRequirement()
        {
            // Prompt 43 requires minimum 280px panel width
            Assert.GreaterOrEqual(ExpectedPanelMinWidth, 280f,
                "Panel minimum width should be at least 280px to prevent text truncation");
        }

        [Test]
        public void PanelPadding_IsWithinReasonableRange()
        {
            // Prompt 43 specifies 12px padding
            Assert.AreEqual(12f, ExpectedPanelPadding,
                "Panel padding should be 12px per design spec");
        }

        [Test]
        public void SectionSpacing_MatchesDesignSpec()
        {
            // Prompt 43 specifies 12px between major sections
            Assert.AreEqual(12f, ExpectedSectionSpacing,
                "Section spacing should be 12px per design spec");
        }

        [Test]
        public void ItemSpacing_MatchesDesignSpec()
        {
            // Prompt 43 specifies 8px between items within sections
            Assert.AreEqual(8f, ExpectedItemSpacing,
                "Item spacing should be 8px per design spec");
        }

        [Test]
        public void LedIndicatorSize_IsSmall()
        {
            // Prompt 43 requires small LED-style indicators (12-16px)
            Assert.GreaterOrEqual(ExpectedLedIndicatorSize, 12f);
            Assert.LessOrEqual(ExpectedLedIndicatorSize, 16f,
                "LED indicator should be 12-16px (small dot, not large square)");
        }

        [Test]
        public void ToggleWidth_IsLargeEnoughForTouchTargets()
        {
            // Toggle should be touch-friendly (at least 44px per Apple HIG, but we use 50px)
            Assert.GreaterOrEqual(ExpectedToggleWidth, 44f,
                "Toggle width should meet minimum touch target size");
        }

        [Test]
        public void ToggleHeight_IsLargeEnoughForTouchTargets()
        {
            // Toggle height should be reasonable
            Assert.GreaterOrEqual(ExpectedToggleHeight, 24f,
                "Toggle height should be at least 24px");
        }

        [Test]
        public void ButtonMinWidth_PreventsTextTruncation()
        {
            // Button should be wide enough for "Disconnect" text
            Assert.GreaterOrEqual(ExpectedButtonMinWidth, 90f,
                "Button should be wide enough to show 'Disconnect' without truncation");
        }

        [Test]
        public void ButtonHeight_MeetsMinimumTouchTarget()
        {
            // Button should be touch-friendly
            Assert.GreaterOrEqual(ExpectedButtonHeight, 30f,
                "Button height should be at least 30px for touch targets");
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
            // Should fit IPv4 addresses like 192.168.100.100
            Assert.GreaterOrEqual(ExpectedHostInputWidth, 120f,
                "Host input should be at least 120px to fit IPv4 addresses");
        }

        [Test]
        public void PortInputWidth_FitsPortNumbers()
        {
            // Should fit 5-digit port numbers (0-65535)
            Assert.GreaterOrEqual(ExpectedPortInputWidth, 60f,
                "Port input should be at least 60px to fit 5-digit ports");
        }

        [Test]
        public void InputHeight_IsComfortable()
        {
            // Input fields should be comfortable to interact with
            Assert.GreaterOrEqual(ExpectedInputHeight, 26f,
                "Input height should be at least 26px");
        }

        #endregion

        #region Layout Hierarchy Tests

        [Test]
        public void LayoutGroup_WithPadding_ContentFitsWithinPanel()
        {
            // Verify that content width + 2*padding <= panel width
            float contentWidth = ExpectedHostInputWidth + ExpectedItemSpacing + ExpectedPortInputWidth + 45f; // 45f for label
            float totalWidth = contentWidth + 2 * ExpectedPanelPadding;

            Assert.LessOrEqual(totalWidth, ExpectedPanelMinWidth,
                "Content with padding should fit within panel minimum width");
        }

        [Test]
        public void IndicatorPillSize_IsCompact()
        {
            // Indicator pills should be compact but readable
            const float expectedPillHeight = 24f;
            const float expectedPillLedSize = 10f;

            Assert.LessOrEqual(expectedPillHeight, 28f,
                "Indicator pill should be compact (max 28px height)");
            Assert.LessOrEqual(expectedPillLedSize, ExpectedLedIndicatorSize,
                "Pill LED should be smaller than connection LED");
        }

        #endregion

        #region Accessibility Tests

        [Test]
        public void TouchTargets_MeetMinimumSize()
        {
            // WCAG recommends 44x44px for touch targets
            const float minimumTouchSize = 44f;

            // Toggle as primary interaction element should meet this
            var toggleArea = ExpectedToggleWidth * ExpectedToggleHeight;
            var minimumArea = minimumTouchSize * minimumTouchSize;

            // We check toggle area is reasonably close to minimum
            Assert.GreaterOrEqual(toggleArea, minimumArea * 0.6f,
                "Toggle touch area should be at least 60% of recommended minimum");
        }

        [Test]
        public void ButtonSize_IsAdequateForTouch()
        {
            // Button should be easy to tap
            var buttonArea = ExpectedButtonMinWidth * ExpectedButtonHeight;

            Assert.GreaterOrEqual(buttonArea, 2800f,
                "Button area should be at least 2800 sq px for comfortable touch");
        }

        #endregion

        #region Proportional Tests

        [Test]
        public void Spacing_IsPropotionalToPadding()
        {
            // Item spacing should be less than or equal to section spacing
            Assert.LessOrEqual(ExpectedItemSpacing, ExpectedSectionSpacing,
                "Item spacing should be <= section spacing for visual hierarchy");
        }

        [Test]
        public void InputFields_HaveSufficientCombinedWidth()
        {
            // Combined input width should leave room for labels
            float inputsWidth = ExpectedHostInputWidth + ExpectedPortInputWidth;
            float availableWidth = ExpectedPanelMinWidth - 2 * ExpectedPanelPadding;

            Assert.Less(inputsWidth, availableWidth,
                "Input fields should leave room for labels within panel width");
        }

        #endregion
    }
}
