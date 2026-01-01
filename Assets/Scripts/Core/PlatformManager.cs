// ABOUTME: Static utility class for platform detection and device capability queries.
// ABOUTME: Provides platform type, screen category, USB support, and Apple Silicon detection.

using UnityEngine;

namespace OpenRange.Core
{
    /// <summary>
    /// Static utility class for platform detection and device capability queries.
    /// </summary>
    public static class PlatformManager
    {
        /// <summary>
        /// Target platform for the application.
        /// </summary>
        public enum Platform
        {
            Mac,
            iPad,
            Android,
            Windows,
            Editor
        }

        private static Platform? _cachedPlatform;
        private static ScreenCategory? _cachedScreenCategory;
        private static float? _cachedDiagonalInches;
        private static bool? _cachedIsAppleSilicon;

        /// <summary>
        /// The current runtime platform.
        /// </summary>
        public static Platform CurrentPlatform
        {
            get
            {
                if (_cachedPlatform.HasValue)
                {
                    return _cachedPlatform.Value;
                }

                _cachedPlatform = DetectPlatform();
                return _cachedPlatform.Value;
            }
        }

        /// <summary>
        /// Whether the current platform is a desktop (Mac or Windows).
        /// </summary>
        public static bool IsDesktop => CurrentPlatform == Platform.Mac ||
                                         CurrentPlatform == Platform.Windows ||
                                         CurrentPlatform == Platform.Editor;

        /// <summary>
        /// Whether the current platform is mobile (iPad or Android).
        /// </summary>
        public static bool IsMobile => CurrentPlatform == Platform.iPad ||
                                        CurrentPlatform == Platform.Android;

        /// <summary>
        /// Whether the current device is a tablet (iPad or Android tablet).
        /// </summary>
        public static bool IsTablet
        {
            get
            {
                if (CurrentPlatform == Platform.iPad)
                {
                    return true;
                }

                if (CurrentPlatform == Platform.Android)
                {
                    return GetDiagonalInches() >= 7f;
                }

                return false;
            }
        }

        /// <summary>
        /// The screen category based on diagonal size.
        /// </summary>
        public static ScreenCategory ScreenCategoryValue
        {
            get
            {
                if (_cachedScreenCategory.HasValue)
                {
                    return _cachedScreenCategory.Value;
                }

                _cachedScreenCategory = DetermineScreenCategory();
                return _cachedScreenCategory.Value;
            }
        }

        /// <summary>
        /// Whether the device supports USB Host mode for GC2 connection.
        /// </summary>
        public static bool HasUSBHostSupport
        {
            get
            {
#if UNITY_EDITOR
                return true;
#elif UNITY_STANDALONE_OSX
                return true;
#elif UNITY_IOS
                return SupportsDriverKit;
#elif UNITY_ANDROID
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// The device model string from SystemInfo.
        /// </summary>
        public static string DeviceModel => SystemInfo.deviceModel;

        /// <summary>
        /// Whether the device is an M1+ iPad that supports DriverKit USB.
        /// </summary>
        public static bool SupportsDriverKit
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return IsAppleSilicon() && SystemInfo.systemMemorySize >= 8000;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// The device name from SystemInfo.
        /// </summary>
        public static string DeviceName => SystemInfo.deviceName;

        /// <summary>
        /// The graphics device name.
        /// </summary>
        public static string GraphicsDeviceName => SystemInfo.graphicsDeviceName;

        /// <summary>
        /// System memory size in MB.
        /// </summary>
        public static int SystemMemoryMB => SystemInfo.systemMemorySize;

        /// <summary>
        /// Graphics memory size in MB.
        /// </summary>
        public static int GraphicsMemoryMB => SystemInfo.graphicsMemorySize;

        /// <summary>
        /// Calculates the screen diagonal in inches.
        /// </summary>
        /// <returns>Screen diagonal in inches, or 0 if DPI is unavailable.</returns>
        public static float GetDiagonalInches()
        {
            if (_cachedDiagonalInches.HasValue)
            {
                return _cachedDiagonalInches.Value;
            }

            float dpi = Screen.dpi;

            if (dpi <= 0f)
            {
                _cachedDiagonalInches = 0f;
                return 0f;
            }

            float widthInches = Screen.width / dpi;
            float heightInches = Screen.height / dpi;
            float diagonal = Mathf.Sqrt(widthInches * widthInches + heightInches * heightInches);

            _cachedDiagonalInches = diagonal;
            return diagonal;
        }

        /// <summary>
        /// Detects whether the device uses Apple Silicon (M1, M2, etc.).
        /// </summary>
        /// <returns>True if the device is Apple Silicon.</returns>
        public static bool IsAppleSilicon()
        {
            if (_cachedIsAppleSilicon.HasValue)
            {
                return _cachedIsAppleSilicon.Value;
            }

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            string graphicsDevice = SystemInfo.graphicsDeviceName.ToLowerInvariant();
            bool isSilicon = graphicsDevice.Contains("apple") ||
                             graphicsDevice.Contains("m1") ||
                             graphicsDevice.Contains("m2") ||
                             graphicsDevice.Contains("m3") ||
                             graphicsDevice.Contains("m4");
            _cachedIsAppleSilicon = isSilicon;
            return isSilicon;
#elif UNITY_IOS
            string deviceModel = SystemInfo.deviceModel.ToLowerInvariant();
            bool hasM = deviceModel.Contains("ipad13") ||
                        deviceModel.Contains("ipad14") ||
                        deviceModel.Contains("ipad15") ||
                        deviceModel.Contains("ipad16");
            _cachedIsAppleSilicon = hasM;
            return hasM;
#else
            _cachedIsAppleSilicon = false;
            return false;
#endif
        }

        /// <summary>
        /// Clears cached platform detection values. Useful for testing.
        /// </summary>
        public static void ClearCache()
        {
            _cachedPlatform = null;
            _cachedScreenCategory = null;
            _cachedDiagonalInches = null;
            _cachedIsAppleSilicon = null;
        }

        private static Platform DetectPlatform()
        {
#if UNITY_EDITOR
            return Platform.Editor;
#elif UNITY_STANDALONE_OSX
            return Platform.Mac;
#elif UNITY_IOS
            return Platform.iPad;
#elif UNITY_ANDROID
            return Platform.Android;
#elif UNITY_STANDALONE_WIN
            return Platform.Windows;
#else
            return Platform.Editor;
#endif
        }

        private static ScreenCategory DetermineScreenCategory()
        {
            float diagonal = GetDiagonalInches();

            if (diagonal <= 0f)
            {
                return ScreenCategory.Regular;
            }

            if (diagonal < 7f)
            {
                return ScreenCategory.Compact;
            }

            if (diagonal < 13f)
            {
                return ScreenCategory.Regular;
            }

            return ScreenCategory.Large;
        }
    }

    /// <summary>
    /// Screen size category for responsive layout.
    /// </summary>
    public enum ScreenCategory
    {
        /// <summary>Screens less than 7 inches diagonal (phones, small tablets).</summary>
        Compact,

        /// <summary>Screens 7-13 inches diagonal (most tablets).</summary>
        Regular,

        /// <summary>Screens larger than 13 inches diagonal (large tablets, desktops).</summary>
        Large
    }
}
