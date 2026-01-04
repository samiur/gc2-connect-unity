// ABOUTME: Factory for creating platform-specific bridge service implementations.
// ABOUTME: Returns AndroidBridgeService on Android, null (no-op) on other platforms.

using UnityEngine;

namespace OpenRange.Core
{
    /// <summary>
    /// Factory for creating platform-specific IBridgeService implementations.
    /// Bridge services enable background operation for GC2→GSPro relay.
    /// </summary>
    public static class BridgeServiceFactory
    {
        /// <summary>
        /// Create a bridge service appropriate for the current platform.
        /// </summary>
        /// <param name="host">GameObject to attach the service component to.</param>
        /// <returns>Platform-specific IBridgeService, or null if not supported.</returns>
        public static IBridgeService Create(GameObject host)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Debug.Log("BridgeServiceFactory: Creating Android foreground service");
            return host.AddComponent<GC2.Platforms.Android.AndroidBridgeService>();

#elif UNITY_IOS && !UNITY_EDITOR
            // iOS doesn't support true background services the same way
            // Background audio or location updates would be needed
            Debug.Log("BridgeServiceFactory: iOS bridge service not implemented");
            return null;

#else
            // Desktop/Editor doesn't need a foreground service
            Debug.Log("BridgeServiceFactory: No bridge service needed on this platform");
            return null;
#endif
        }

        /// <summary>
        /// Check if the current platform supports bridge mode.
        /// </summary>
        /// <returns>True if bridge mode is supported.</returns>
        public static bool IsBridgeModeSupported()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }
    }
}
