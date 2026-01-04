// ABOUTME: C# bridge for Android foreground service implementing IBridgeService.
// ABOUTME: Manages GC2BridgeService lifecycle via AndroidJavaObject calls.

using System;
using System.Threading.Tasks;
using OpenRange.Core;
using OpenRange.Utilities;
using UnityEngine;

namespace OpenRange.GC2.Platforms.Android
{
#if UNITY_ANDROID
    /// <summary>
    /// Android implementation of IBridgeService using foreground service.
    /// Communicates with com.openrange.gc2.GC2BridgeService via AndroidJavaObject.
    /// </summary>
    public class AndroidBridgeService : MonoBehaviour, IBridgeService
    {
        #region Constants

        /// <summary>
        /// Fully qualified class name of the Android service.
        /// </summary>
        private const string ServiceClassName = "com.openrange.gc2.GC2BridgeService";

        /// <summary>
        /// Action to start the service.
        /// </summary>
        private const string ActionStart = "com.openrange.gc2.action.START_BRIDGE";

        /// <summary>
        /// Action to stop the service.
        /// </summary>
        private const string ActionStop = "com.openrange.gc2.action.STOP_BRIDGE";

        /// <summary>
        /// Action to update notification.
        /// </summary>
        private const string ActionUpdate = "com.openrange.gc2.action.UPDATE_NOTIFICATION";

        /// <summary>
        /// Extra key for shots relayed count.
        /// </summary>
        private const string ExtraShotsRelayed = "shots_relayed";

        /// <summary>
        /// Extra key for GC2 connection status.
        /// </summary>
        private const string ExtraIsConnected = "is_connected";

        /// <summary>
        /// Extra key for GSPro connection status.
        /// </summary>
        private const string ExtraGSProConnected = "gspro_connected";

        #endregion

        #region Private Fields

        private AndroidJavaObject _unityActivity;
        private bool _isRunning;
        private bool _isInitialized;
        private bool _isDisposed;
        private int _shotsRelayed;
        private bool _isGC2Connected;
        private bool _isGSProConnected;

        #endregion

        #region IBridgeService Properties

        /// <summary>
        /// Whether the service is currently running.
        /// </summary>
        public bool IsRunning => _isRunning && !_isDisposed;

        #endregion

        #region IBridgeService Events

        /// <summary>
        /// Fired when the service successfully starts.
        /// </summary>
        public event Action OnStarted;

        /// <summary>
        /// Fired when the service stops.
        /// </summary>
        public event Action OnStopped;

        /// <summary>
        /// Fired when a shot is successfully relayed to GSPro.
        /// </summary>
        public event Action<GC2ShotData> OnShotRelayed;

        /// <summary>
        /// Fired when an error occurs in the service.
        /// </summary>
        public event Action<string> OnError;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void OnApplicationQuit()
        {
            // Ensure service is stopped when app quits
            if (_isRunning)
            {
                Stop();
            }
            Cleanup();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            if (_isInitialized || _isDisposed)
                return;

            try
            {
                // Get the Unity activity
                using (var unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    _unityActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
                }

                if (_unityActivity == null)
                {
                    Debug.LogError("AndroidBridgeService: Failed to get Unity activity");
                    return;
                }

                _isInitialized = true;
                Debug.Log($"AndroidBridgeService: Initialized with callback object: {gameObject.name}");
            }
            catch (AndroidJavaException ex)
            {
                Debug.LogError($"AndroidBridgeService: Java exception during initialization - {ex.Message}");
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"AndroidBridgeService: Initialization failed - {ex.Message}");
                _isInitialized = false;
            }
        }

        private void Cleanup()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            try
            {
                if (_isRunning)
                {
                    StopServiceInternal();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"AndroidBridgeService: Cleanup error - {ex.Message}");
            }
            finally
            {
                _unityActivity?.Dispose();
                _unityActivity = null;
            }

            _isRunning = false;
            _isInitialized = false;

            // Clear event handlers
            OnStarted = null;
            OnStopped = null;
            OnShotRelayed = null;
            OnError = null;
        }

        #endregion

        #region IBridgeService Methods

        /// <summary>
        /// Start the background service.
        /// </summary>
        /// <returns>True if service started successfully.</returns>
        public Task<bool> StartAsync()
        {
            if (_isDisposed)
            {
                Debug.LogError("AndroidBridgeService: Cannot start - service is disposed");
                return Task.FromResult(false);
            }

            if (_isRunning)
            {
                Debug.LogWarning("AndroidBridgeService: Service already running");
                return Task.FromResult(true);
            }

            if (!_isInitialized)
            {
                Initialize();
                if (!_isInitialized)
                {
                    NotifyError("Service not initialized");
                    return Task.FromResult(false);
                }
            }

            try
            {
                StartServiceInternal();
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"AndroidBridgeService: StartAsync failed - {ex.Message}");
                NotifyError($"Start error: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Stop the background service.
        /// </summary>
        public void Stop()
        {
            if (!_isRunning || _isDisposed)
                return;

            try
            {
                StopServiceInternal();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"AndroidBridgeService: Stop error - {ex.Message}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the service notification with current status.
        /// </summary>
        /// <param name="shotsRelayed">Number of shots relayed</param>
        /// <param name="isGC2Connected">Whether GC2 is connected</param>
        /// <param name="isGSProConnected">Whether GSPro is connected</param>
        public void UpdateNotification(int shotsRelayed, bool isGC2Connected, bool isGSProConnected)
        {
            if (!_isRunning || _isDisposed || _unityActivity == null)
                return;

            _shotsRelayed = shotsRelayed;
            _isGC2Connected = isGC2Connected;
            _isGSProConnected = isGSProConnected;

            try
            {
                using (var intent = CreateServiceIntent(ActionUpdate))
                {
                    intent.Call<AndroidJavaObject>("putExtra", ExtraShotsRelayed, shotsRelayed);
                    intent.Call<AndroidJavaObject>("putExtra", ExtraIsConnected, isGC2Connected);
                    intent.Call<AndroidJavaObject>("putExtra", ExtraGSProConnected, isGSProConnected);
                    _unityActivity.Call("startService", intent);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"AndroidBridgeService: UpdateNotification failed - {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the current shots relayed count.
        /// </summary>
        public int GetShotsRelayed() => _shotsRelayed;

        #endregion

        #region Native Callbacks (Called via UnitySendMessage)

        /// <summary>
        /// Called by native service when it starts.
        /// </summary>
        public void OnBridgeServiceStarted(string unused)
        {
            if (_isDisposed)
                return;

            _isRunning = true;
            MainThreadDispatcher.Execute(() =>
            {
                try
                {
                    OnStarted?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"AndroidBridgeService: Started callback error - {ex.Message}");
                }
            });
            Debug.Log("AndroidBridgeService: Service started");
        }

        /// <summary>
        /// Called by native service when it stops.
        /// </summary>
        public void OnBridgeServiceStopped(string unused)
        {
            if (_isDisposed)
                return;

            _isRunning = false;
            MainThreadDispatcher.Execute(() =>
            {
                try
                {
                    OnStopped?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"AndroidBridgeService: Stopped callback error - {ex.Message}");
                }
            });
            Debug.Log("AndroidBridgeService: Service stopped");
        }

        /// <summary>
        /// Called by native service when a shot is relayed.
        /// </summary>
        public void OnBridgeShotRelayed(string shotCountStr)
        {
            if (_isDisposed)
                return;

            if (int.TryParse(shotCountStr, out int shotCount))
            {
                _shotsRelayed = shotCount;
            }

            Debug.Log($"AndroidBridgeService: Shot relayed, count: {_shotsRelayed}");
        }

        /// <summary>
        /// Called by native service when an error occurs.
        /// </summary>
        public void OnBridgeError(string error)
        {
            if (_isDisposed || string.IsNullOrEmpty(error))
                return;

            MainThreadDispatcher.Execute(() => NotifyError(error));
            Debug.LogError($"AndroidBridgeService: Native error - {error}");
        }

        #endregion

        #region Private Methods

        private void StartServiceInternal()
        {
            using (var intent = CreateServiceIntent(ActionStart))
            {
                intent.Call<AndroidJavaObject>("putExtra", ExtraShotsRelayed, _shotsRelayed);
                intent.Call<AndroidJavaObject>("putExtra", ExtraIsConnected, _isGC2Connected);
                intent.Call<AndroidJavaObject>("putExtra", ExtraGSProConnected, _isGSProConnected);

                // Use startForegroundService for Android 8.0+
                if (GetAndroidApiLevel() >= 26)
                {
                    _unityActivity.Call<AndroidJavaObject>("startForegroundService", intent);
                }
                else
                {
                    _unityActivity.Call<AndroidJavaObject>("startService", intent);
                }
            }

            // Set callback object name on service
            SetServiceCallbackObject();

            Debug.Log("AndroidBridgeService: Service start requested");
        }

        private void StopServiceInternal()
        {
            using (var intent = CreateServiceIntent(ActionStop))
            {
                _unityActivity.Call("startService", intent);
            }

            _isRunning = false;
            Debug.Log("AndroidBridgeService: Service stop requested");
        }

        private void SetServiceCallbackObject()
        {
            try
            {
                using (var serviceClass = new AndroidJavaClass(ServiceClassName))
                {
                    var serviceInstance = serviceClass.CallStatic<AndroidJavaObject>("getInstance");
                    if (serviceInstance != null)
                    {
                        serviceInstance.Call("setUnityCallbackObject", gameObject.name);
                        serviceInstance.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                // Service may not be started yet, which is fine
                Debug.LogWarning($"AndroidBridgeService: Failed to set callback object - {ex.Message}");
            }
        }

        private AndroidJavaObject CreateServiceIntent(string action)
        {
            using (var serviceClass = new AndroidJavaClass(ServiceClassName))
            {
                var intent = new AndroidJavaObject(
                    "android.content.Intent",
                    _unityActivity,
                    serviceClass.Call<AndroidJavaObject>("getClass")
                );
                intent.Call<AndroidJavaObject>("setAction", action);
                return intent;
            }
        }

        private int GetAndroidApiLevel()
        {
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    return version.GetStatic<int>("SDK_INT");
                }
            }
            catch
            {
                return 21; // Default to API 21
            }
        }

        private void NotifyError(string message)
        {
            Debug.LogError($"AndroidBridgeService: {message}");
            try
            {
                OnError?.Invoke(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"AndroidBridgeService: Error callback error - {ex.Message}");
            }
        }

        #endregion

        #region Testing Support

        /// <summary>
        /// Force initialize for testing.
        /// </summary>
        internal void ForceInitialize()
        {
            _isInitialized = true;
            _isDisposed = false;
        }

        /// <summary>
        /// Force set running state for testing.
        /// </summary>
        internal void ForceSetRunning(bool running)
        {
            _isRunning = running;
        }

        /// <summary>
        /// Get internal state for testing.
        /// </summary>
        internal bool IsInitialized => _isInitialized;

        /// <summary>
        /// Get disposed state for testing.
        /// </summary>
        internal bool IsDisposed => _isDisposed;

        #endregion
    }
#else
    /// <summary>
    /// Stub implementation for non-Android platforms.
    /// </summary>
    public class AndroidBridgeService : MonoBehaviour, IBridgeService
    {
        public bool IsRunning => false;
        public event Action OnStarted;
        public event Action OnStopped;
        public event Action<GC2ShotData> OnShotRelayed;
        public event Action<string> OnError;

        public Task<bool> StartAsync()
        {
            Debug.LogWarning("AndroidBridgeService: Not available on this platform");
            return Task.FromResult(false);
        }

        public void Stop()
        {
            Debug.LogWarning("AndroidBridgeService: Not available on this platform");
        }

        public void UpdateNotification(int shotsRelayed, bool isGC2Connected, bool isGSProConnected)
        {
            // No-op on non-Android
        }

        public int GetShotsRelayed() => 0;

        // Suppress unused event warnings
        private void SuppressWarnings()
        {
            OnStarted?.Invoke();
            OnStopped?.Invoke();
            OnShotRelayed?.Invoke(null);
            OnError?.Invoke(null);
        }
    }
#endif
}
