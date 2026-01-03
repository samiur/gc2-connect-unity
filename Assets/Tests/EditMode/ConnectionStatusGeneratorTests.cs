// ABOUTME: Unit tests for ConnectionStatusGenerator layout constants and prefab creation.
// ABOUTME: Validates panel sizing, button accessibility, and modal overlay configuration.

using NUnit.Framework;
using OpenRange.Editor;
using UnityEngine;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class ConnectionStatusGeneratorTests
    {
        #region Layout Constants Tests

        [Test]
        public void PanelWidth_MeetsMinimumRequirement()
        {
            // Panel should be wide enough for content
            Assert.That(ConnectionStatusGenerator.PanelWidth, Is.GreaterThanOrEqualTo(350f),
                "Panel width must be at least 350px to fit content");
        }

        [Test]
        public void PanelMinHeight_FitsAllContent()
        {
            // Calculate minimum height needed for all content rows
            float minRequiredHeight =
                ConnectionStatusGenerator.HeaderHeight +
                ConnectionStatusGenerator.StatusRowHeight +
                (ConnectionStatusGenerator.InfoRowHeight * 3) + // Device info, mode, last shot
                ConnectionStatusGenerator.SpacerHeight +
                ConnectionStatusGenerator.ButtonRowHeight +
                60f; // Padding (top and bottom ~30px each)

            Assert.That(ConnectionStatusGenerator.PanelMinHeight, Is.GreaterThanOrEqualTo(minRequiredHeight),
                "Panel min height must fit all content rows");
        }

        [Test]
        public void CloseButton_MeetsAccessibilityRequirement()
        {
            // Apple HIG and WCAG recommend minimum 44px, but 32px is often acceptable
            const float minimumTouchTarget = 32f;
            Assert.That(ConnectionStatusGenerator.CloseButtonSize, Is.GreaterThanOrEqualTo(minimumTouchTarget),
                "Close button must meet minimum touch target size (32px)");
        }

        [Test]
        public void CloseButton_ReasonableSize()
        {
            // But not so large it's unwieldy
            const float maximumReasonableSize = 60f;
            Assert.That(ConnectionStatusGenerator.CloseButtonSize, Is.LessThanOrEqualTo(maximumReasonableSize),
                "Close button shouldn't be excessively large");
        }

        [Test]
        public void ActionButtonMinWidth_FitsButtonText()
        {
            // "Disconnect" is the longest button text (~10 chars)
            // At ~10px per char + padding, need at least 100px
            const float minimumForDisconnect = 100f;
            Assert.That(ConnectionStatusGenerator.ActionButtonMinWidth, Is.GreaterThanOrEqualTo(minimumForDisconnect),
                "Button width must fit 'Disconnect' text");
        }

        [Test]
        public void ActionButtonHeight_MeetsTouchTarget()
        {
            // Buttons should be easy to tap
            const float minimumTouchTarget = 40f;
            Assert.That(ConnectionStatusGenerator.ActionButtonHeight, Is.GreaterThanOrEqualTo(minimumTouchTarget),
                "Action buttons must meet minimum touch target height");
        }

        [Test]
        public void OverlayAlpha_IsVisible()
        {
            // Overlay should be visible enough to indicate modal state
            Assert.That(ConnectionStatusGenerator.OverlayAlpha, Is.GreaterThan(0.3f),
                "Overlay should be visible");
        }

        [Test]
        public void OverlayAlpha_IsNotOpaque()
        {
            // Overlay shouldn't completely block the background
            Assert.That(ConnectionStatusGenerator.OverlayAlpha, Is.LessThan(0.9f),
                "Overlay should not be fully opaque");
        }

        #endregion

        #region Height Calculation Tests

        [Test]
        public void HeaderHeight_IsReasonable()
        {
            // Header needs room for title and close button
            Assert.That(ConnectionStatusGenerator.HeaderHeight, Is.InRange(40f, 70f),
                "Header height should be reasonable for title and close button");
        }

        [Test]
        public void StatusRowHeight_IsReasonable()
        {
            // Status row has dot + text
            Assert.That(ConnectionStatusGenerator.StatusRowHeight, Is.InRange(30f, 50f),
                "Status row height should fit dot and text");
        }

        [Test]
        public void InfoRowHeight_FitsLabelAndValue()
        {
            // Info rows have label + value stacked vertically
            Assert.That(ConnectionStatusGenerator.InfoRowHeight, Is.InRange(44f, 80f),
                "Info row height should fit stacked label and value");
        }

        [Test]
        public void ButtonRowHeight_IsAdequate()
        {
            // Button row should fit action buttons with padding
            Assert.That(ConnectionStatusGenerator.ButtonRowHeight,
                Is.GreaterThanOrEqualTo(ConnectionStatusGenerator.ActionButtonHeight),
                "Button row must be at least as tall as action buttons");
        }

        #endregion

        #region Panel Max Height Tests

        [Test]
        public void PanelMaxHeight_GreaterThanMinHeight()
        {
            Assert.That(ConnectionStatusGenerator.PanelMaxHeight,
                Is.GreaterThan(ConnectionStatusGenerator.PanelMinHeight),
                "Max height must be greater than min height");
        }

        [Test]
        public void PanelMaxHeight_ReasonableForScreen()
        {
            // Max height should leave room for other UI at 1080p
            const float maxReasonable = 800f;
            Assert.That(ConnectionStatusGenerator.PanelMaxHeight, Is.LessThanOrEqualTo(maxReasonable),
                "Panel max height should leave room for other UI");
        }

        #endregion

        #region Proportion Tests

        [Test]
        public void CloseButtonSize_ProportionalToHeader()
        {
            // Close button should fit comfortably in header
            float maxCloseSize = ConnectionStatusGenerator.HeaderHeight * 0.9f;
            Assert.That(ConnectionStatusGenerator.CloseButtonSize, Is.LessThanOrEqualTo(maxCloseSize),
                "Close button should fit within header height");
        }

        [Test]
        public void SpacerHeight_IsPositive()
        {
            // Spacer should provide visual separation
            Assert.That(ConnectionStatusGenerator.SpacerHeight, Is.GreaterThan(0f),
                "Spacer height should be positive");
        }

        #endregion
    }
}
