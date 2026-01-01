// ABOUTME: Unit tests for UITheme static class.
// ABOUTME: Verifies color values, font sizes, and theme constants.

using NUnit.Framework;
using OpenRange.UI;
using UnityEngine;

namespace OpenRange.Tests.EditMode
{
    /// <summary>
    /// Tests for UITheme static class.
    /// </summary>
    [TestFixture]
    public class UIThemeTests
    {
        #region Color Tests

        [Test]
        public void PanelBackground_HasCorrectColor()
        {
            // #1a1a2e with alpha 0.85
            var expected = new Color(0.102f, 0.102f, 0.180f, 0.85f);

            Assert.That(UITheme.PanelBackground.r, Is.EqualTo(expected.r).Within(0.01f));
            Assert.That(UITheme.PanelBackground.g, Is.EqualTo(expected.g).Within(0.01f));
            Assert.That(UITheme.PanelBackground.b, Is.EqualTo(expected.b).Within(0.01f));
            Assert.That(UITheme.PanelBackground.a, Is.EqualTo(expected.a).Within(0.01f));
        }

        [Test]
        public void AccentGreen_HasCorrectColor()
        {
            // #2d5a27
            Assert.That(UITheme.AccentGreen.r, Is.EqualTo(0.176f).Within(0.01f));
            Assert.That(UITheme.AccentGreen.g, Is.EqualTo(0.353f).Within(0.01f));
            Assert.That(UITheme.AccentGreen.b, Is.EqualTo(0.153f).Within(0.01f));
            Assert.That(UITheme.AccentGreen.a, Is.EqualTo(1f));
        }

        [Test]
        public void TextPrimary_IsWhite()
        {
            Assert.That(UITheme.TextPrimary, Is.EqualTo(Color.white));
        }

        [Test]
        public void TotalRed_HasCorrectColor()
        {
            // #ff6b6b
            Assert.That(UITheme.TotalRed.r, Is.EqualTo(1f).Within(0.01f));
            Assert.That(UITheme.TotalRed.g, Is.EqualTo(0.420f).Within(0.01f));
            Assert.That(UITheme.TotalRed.b, Is.EqualTo(0.420f).Within(0.01f));
            Assert.That(UITheme.TotalRed.a, Is.EqualTo(1f));
        }

        [Test]
        public void StatusConnected_IsGreen()
        {
            Assert.That(UITheme.StatusConnected.g, Is.GreaterThan(UITheme.StatusConnected.r));
            Assert.That(UITheme.StatusConnected.g, Is.GreaterThan(UITheme.StatusConnected.b));
        }

        [Test]
        public void StatusDisconnected_IsRed()
        {
            Assert.That(UITheme.StatusDisconnected.r, Is.GreaterThan(UITheme.StatusDisconnected.g));
            Assert.That(UITheme.StatusDisconnected.r, Is.GreaterThan(UITheme.StatusDisconnected.b));
        }

        [Test]
        public void ToastColors_AreDifferent()
        {
            Assert.That(UITheme.ToastInfo, Is.Not.EqualTo(UITheme.ToastSuccess));
            Assert.That(UITheme.ToastSuccess, Is.Not.EqualTo(UITheme.ToastWarning));
            Assert.That(UITheme.ToastWarning, Is.Not.EqualTo(UITheme.ToastError));
            Assert.That(UITheme.ToastError, Is.Not.EqualTo(UITheme.ToastInfo));
        }

        #endregion

        #region Font Size Tests

        [Test]
        public void FontSizeCompact_ValuesArePositive()
        {
            Assert.That(UITheme.FontSizeCompact.Small, Is.GreaterThan(0));
            Assert.That(UITheme.FontSizeCompact.Normal, Is.GreaterThan(0));
            Assert.That(UITheme.FontSizeCompact.Large, Is.GreaterThan(0));
            Assert.That(UITheme.FontSizeCompact.Header, Is.GreaterThan(0));
            Assert.That(UITheme.FontSizeCompact.DataValue, Is.GreaterThan(0));
        }

        [Test]
        public void FontSizeCompact_AreInAscendingOrder()
        {
            Assert.That(UITheme.FontSizeCompact.Small, Is.LessThan(UITheme.FontSizeCompact.Normal));
            Assert.That(UITheme.FontSizeCompact.Normal, Is.LessThan(UITheme.FontSizeCompact.Large));
            Assert.That(UITheme.FontSizeCompact.Large, Is.LessThan(UITheme.FontSizeCompact.Header));
            Assert.That(UITheme.FontSizeCompact.Header, Is.LessThan(UITheme.FontSizeCompact.DataValue));
        }

        [Test]
        public void FontSizeRegular_LargerThanCompact()
        {
            Assert.That(UITheme.FontSizeRegular.Small, Is.GreaterThanOrEqualTo(UITheme.FontSizeCompact.Small));
            Assert.That(UITheme.FontSizeRegular.Normal, Is.GreaterThanOrEqualTo(UITheme.FontSizeCompact.Normal));
            Assert.That(UITheme.FontSizeRegular.Large, Is.GreaterThanOrEqualTo(UITheme.FontSizeCompact.Large));
        }

        [Test]
        public void FontSizeLarge_LargerThanRegular()
        {
            Assert.That(UITheme.FontSizeLarge.Small, Is.GreaterThanOrEqualTo(UITheme.FontSizeRegular.Small));
            Assert.That(UITheme.FontSizeLarge.Normal, Is.GreaterThanOrEqualTo(UITheme.FontSizeRegular.Normal));
            Assert.That(UITheme.FontSizeLarge.Large, Is.GreaterThanOrEqualTo(UITheme.FontSizeRegular.Large));
        }

        [Test]
        public void GetFontSize_Compact_ReturnsCompactValues()
        {
            Assert.That(UITheme.GetFontSize(ScreenCategory.Compact, FontCategory.Small),
                Is.EqualTo(UITheme.FontSizeCompact.Small));
            Assert.That(UITheme.GetFontSize(ScreenCategory.Compact, FontCategory.Normal),
                Is.EqualTo(UITheme.FontSizeCompact.Normal));
            Assert.That(UITheme.GetFontSize(ScreenCategory.Compact, FontCategory.DataValue),
                Is.EqualTo(UITheme.FontSizeCompact.DataValue));
        }

        [Test]
        public void GetFontSize_Regular_ReturnsRegularValues()
        {
            Assert.That(UITheme.GetFontSize(ScreenCategory.Regular, FontCategory.Small),
                Is.EqualTo(UITheme.FontSizeRegular.Small));
            Assert.That(UITheme.GetFontSize(ScreenCategory.Regular, FontCategory.Normal),
                Is.EqualTo(UITheme.FontSizeRegular.Normal));
            Assert.That(UITheme.GetFontSize(ScreenCategory.Regular, FontCategory.DataValue),
                Is.EqualTo(UITheme.FontSizeRegular.DataValue));
        }

        [Test]
        public void GetFontSize_Large_ReturnsLargeValues()
        {
            Assert.That(UITheme.GetFontSize(ScreenCategory.Large, FontCategory.Small),
                Is.EqualTo(UITheme.FontSizeLarge.Small));
            Assert.That(UITheme.GetFontSize(ScreenCategory.Large, FontCategory.Normal),
                Is.EqualTo(UITheme.FontSizeLarge.Normal));
            Assert.That(UITheme.GetFontSize(ScreenCategory.Large, FontCategory.DataValue),
                Is.EqualTo(UITheme.FontSizeLarge.DataValue));
        }

        #endregion

        #region Spacing Tests

        [Test]
        public void Padding_ValuesArePositive()
        {
            Assert.That(UITheme.Padding.Tiny, Is.GreaterThan(0));
            Assert.That(UITheme.Padding.Small, Is.GreaterThan(0));
            Assert.That(UITheme.Padding.Normal, Is.GreaterThan(0));
            Assert.That(UITheme.Padding.Large, Is.GreaterThan(0));
            Assert.That(UITheme.Padding.XLarge, Is.GreaterThan(0));
        }

        [Test]
        public void Padding_InAscendingOrder()
        {
            Assert.That(UITheme.Padding.Tiny, Is.LessThan(UITheme.Padding.Small));
            Assert.That(UITheme.Padding.Small, Is.LessThan(UITheme.Padding.Normal));
            Assert.That(UITheme.Padding.Normal, Is.LessThan(UITheme.Padding.Large));
            Assert.That(UITheme.Padding.Large, Is.LessThan(UITheme.Padding.XLarge));
        }

        [Test]
        public void Margin_ValuesArePositive()
        {
            Assert.That(UITheme.Margin.Tiny, Is.GreaterThan(0));
            Assert.That(UITheme.Margin.Small, Is.GreaterThan(0));
            Assert.That(UITheme.Margin.Normal, Is.GreaterThan(0));
            Assert.That(UITheme.Margin.Large, Is.GreaterThan(0));
            Assert.That(UITheme.Margin.XLarge, Is.GreaterThan(0));
        }

        [Test]
        public void BorderRadius_ValuesArePositive()
        {
            Assert.That(UITheme.BorderRadius.Small, Is.GreaterThan(0));
            Assert.That(UITheme.BorderRadius.Normal, Is.GreaterThan(0));
            Assert.That(UITheme.BorderRadius.Large, Is.GreaterThan(0));
        }

        #endregion

        #region Animation Tests

        [Test]
        public void Animation_DurationsArePositive()
        {
            Assert.That(UITheme.Animation.Fast, Is.GreaterThan(0));
            Assert.That(UITheme.Animation.Normal, Is.GreaterThan(0));
            Assert.That(UITheme.Animation.Slow, Is.GreaterThan(0));
            Assert.That(UITheme.Animation.PanelTransition, Is.GreaterThan(0));
            Assert.That(UITheme.Animation.ToastSlide, Is.GreaterThan(0));
        }

        [Test]
        public void Animation_InAscendingOrder()
        {
            Assert.That(UITheme.Animation.Fast, Is.LessThan(UITheme.Animation.Normal));
            Assert.That(UITheme.Animation.Normal, Is.LessThan(UITheme.Animation.Slow));
        }

        [Test]
        public void ToastDefaultDuration_IsReasonable()
        {
            Assert.That(UITheme.ToastDefaultDuration, Is.GreaterThanOrEqualTo(1f));
            Assert.That(UITheme.ToastDefaultDuration, Is.LessThanOrEqualTo(10f));
        }

        #endregion

        #region Layout Tests

        [Test]
        public void ReferenceResolution_Is1920x1080()
        {
            Assert.That(UITheme.ReferenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
        }

        [Test]
        public void Breakpoints_AreReasonable()
        {
            Assert.That(UITheme.Breakpoints.Compact, Is.GreaterThan(0));
            Assert.That(UITheme.Breakpoints.Regular, Is.GreaterThan(UITheme.Breakpoints.Compact));
        }

        #endregion
    }
}
