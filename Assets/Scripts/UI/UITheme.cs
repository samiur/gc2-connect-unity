// ABOUTME: Static class containing UI theme constants for consistent styling.
// ABOUTME: Includes colors, font sizes, spacing, and animation durations.

using UnityEngine;

namespace OpenRange.UI
{
    /// <summary>
    /// Static class containing UI theme constants.
    /// Provides consistent styling across all UI components.
    /// </summary>
    public static class UITheme
    {
        #region Colors

        /// <summary>
        /// Semi-transparent dark panel background.
        /// </summary>
        public static readonly Color PanelBackground = new Color(0.102f, 0.102f, 0.180f, 0.85f); // #1a1a2e

        /// <summary>
        /// Accent green for highlights and positive values.
        /// </summary>
        public static readonly Color AccentGreen = new Color(0.176f, 0.353f, 0.153f, 1f); // #2d5a27

        /// <summary>
        /// Primary text color (white).
        /// </summary>
        public static readonly Color TextPrimary = Color.white;

        /// <summary>
        /// Secondary text color (slightly dimmed).
        /// </summary>
        public static readonly Color TextSecondary = new Color(0.8f, 0.8f, 0.8f, 1f);

        /// <summary>
        /// Red color for total distance and important highlights.
        /// </summary>
        public static readonly Color TotalRed = new Color(1f, 0.420f, 0.420f, 1f); // #ff6b6b

        /// <summary>
        /// Warning/caution yellow color.
        /// </summary>
        public static readonly Color WarningYellow = new Color(1f, 0.843f, 0f, 1f); // #ffd700

        /// <summary>
        /// Connection status colors.
        /// </summary>
        public static readonly Color StatusConnected = new Color(0.2f, 0.8f, 0.2f, 1f);
        public static readonly Color StatusConnecting = new Color(1f, 0.8f, 0.2f, 1f);
        public static readonly Color StatusDisconnected = new Color(0.8f, 0.2f, 0.2f, 1f);
        public static readonly Color StatusNoDevice = new Color(0.5f, 0.5f, 0.5f, 1f);

        /// <summary>
        /// Toast notification colors.
        /// </summary>
        public static readonly Color ToastInfo = new Color(0.2f, 0.4f, 0.8f, 0.95f);
        public static readonly Color ToastSuccess = new Color(0.2f, 0.6f, 0.2f, 0.95f);
        public static readonly Color ToastWarning = new Color(0.8f, 0.6f, 0.1f, 0.95f);
        public static readonly Color ToastError = new Color(0.8f, 0.2f, 0.2f, 0.95f);

        #endregion

        #region Font Sizes

        /// <summary>
        /// Font sizes for Compact screens (&lt;800px width).
        /// </summary>
        public static class FontSizeCompact
        {
            public const int Small = 10;
            public const int Normal = 12;
            public const int Large = 16;
            public const int Header = 20;
            public const int DataValue = 24;
        }

        /// <summary>
        /// Font sizes for Regular screens (800-1200px width).
        /// </summary>
        public static class FontSizeRegular
        {
            public const int Small = 12;
            public const int Normal = 14;
            public const int Large = 18;
            public const int Header = 24;
            public const int DataValue = 32;
        }

        /// <summary>
        /// Font sizes for Large screens (&gt;1200px width).
        /// </summary>
        public static class FontSizeLarge
        {
            public const int Small = 14;
            public const int Normal = 16;
            public const int Large = 22;
            public const int Header = 28;
            public const int DataValue = 40;
        }

        /// <summary>
        /// Get font size for a category based on screen size.
        /// </summary>
        public static int GetFontSize(ScreenCategory category, FontCategory fontCategory)
        {
            return category switch
            {
                ScreenCategory.Compact => fontCategory switch
                {
                    FontCategory.Small => FontSizeCompact.Small,
                    FontCategory.Normal => FontSizeCompact.Normal,
                    FontCategory.Large => FontSizeCompact.Large,
                    FontCategory.Header => FontSizeCompact.Header,
                    FontCategory.DataValue => FontSizeCompact.DataValue,
                    _ => FontSizeCompact.Normal
                },
                ScreenCategory.Regular => fontCategory switch
                {
                    FontCategory.Small => FontSizeRegular.Small,
                    FontCategory.Normal => FontSizeRegular.Normal,
                    FontCategory.Large => FontSizeRegular.Large,
                    FontCategory.Header => FontSizeRegular.Header,
                    FontCategory.DataValue => FontSizeRegular.DataValue,
                    _ => FontSizeRegular.Normal
                },
                ScreenCategory.Large => fontCategory switch
                {
                    FontCategory.Small => FontSizeLarge.Small,
                    FontCategory.Normal => FontSizeLarge.Normal,
                    FontCategory.Large => FontSizeLarge.Large,
                    FontCategory.Header => FontSizeLarge.Header,
                    FontCategory.DataValue => FontSizeLarge.DataValue,
                    _ => FontSizeLarge.Normal
                },
                _ => FontSizeRegular.Normal
            };
        }

        #endregion

        #region Spacing

        /// <summary>
        /// Standard padding values.
        /// </summary>
        public static class Padding
        {
            public const float Tiny = 4f;
            public const float Small = 8f;
            public const float Normal = 12f;
            public const float Large = 16f;
            public const float XLarge = 24f;
        }

        /// <summary>
        /// Standard margin values.
        /// </summary>
        public static class Margin
        {
            public const float Tiny = 4f;
            public const float Small = 8f;
            public const float Normal = 12f;
            public const float Large = 16f;
            public const float XLarge = 24f;
        }

        /// <summary>
        /// Standard border radius values.
        /// </summary>
        public static class BorderRadius
        {
            public const float Small = 4f;
            public const float Normal = 8f;
            public const float Large = 12f;
        }

        #endregion

        #region Animation

        /// <summary>
        /// Animation duration constants.
        /// </summary>
        public static class Animation
        {
            public const float Fast = 0.15f;
            public const float Normal = 0.25f;
            public const float Slow = 0.4f;
            public const float PanelTransition = 0.3f;
            public const float ToastSlide = 0.2f;
            public const float DataHighlight = 0.5f;
        }

        /// <summary>
        /// Default toast display duration in seconds.
        /// </summary>
        public const float ToastDefaultDuration = 3f;

        #endregion

        #region Layout

        /// <summary>
        /// Reference resolution for Canvas Scaler.
        /// </summary>
        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        /// <summary>
        /// Screen width breakpoints for responsive layout.
        /// </summary>
        public static class Breakpoints
        {
            public const float Compact = 800f;
            public const float Regular = 1200f;
        }

        #endregion
    }

    /// <summary>
    /// Screen size categories for responsive layout.
    /// </summary>
    public enum ScreenCategory
    {
        /// <summary>Small screens, typically phones or small tablets (&lt;800px).</summary>
        Compact,
        /// <summary>Medium screens, typical tablets (800-1200px).</summary>
        Regular,
        /// <summary>Large screens, desktops and large tablets (&gt;1200px).</summary>
        Large
    }

    /// <summary>
    /// Font size categories for theming.
    /// </summary>
    public enum FontCategory
    {
        /// <summary>Small text like labels and captions.</summary>
        Small,
        /// <summary>Normal body text.</summary>
        Normal,
        /// <summary>Larger emphasized text.</summary>
        Large,
        /// <summary>Section headers.</summary>
        Header,
        /// <summary>Large data values in shot display.</summary>
        DataValue
    }

    /// <summary>
    /// Toast notification types.
    /// </summary>
    public enum ToastType
    {
        /// <summary>Informational message.</summary>
        Info,
        /// <summary>Success/positive message.</summary>
        Success,
        /// <summary>Warning message.</summary>
        Warning,
        /// <summary>Error message.</summary>
        Error
    }
}
