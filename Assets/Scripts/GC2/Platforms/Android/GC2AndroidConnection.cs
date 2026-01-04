// ABOUTME: C# bridge for the Android native USB plugin using USB Host API.
// ABOUTME: Implements IGC2Connection via AndroidJavaObject calls to GC2Plugin.

using System;
using System.Threading.Tasks;
using OpenRange.Utilities;
using UnityEngine;

namespace OpenRange.GC2.Platforms.Android
{
#if UNITY_ANDROID
    /// <summary>
    /// Android implementation of IGC2Connection using USB Host API native plugin.
    /// Communicates with com.openrange.gc2.GC2Plugin via AndroidJavaObject and UnitySendMessage callbacks.
    /// </summary>
    public class GC2AndroidConnection : MonoBehaviour, IGC2Connection
    {
        #region Constants

        /// <summary>
        /// Fully qualified class name of the Android plugin.
        /// </summary>
        private const string PluginClassName = "com.openrange.gc2.GC2Plugin";

        #endregion

        #region Private Fields

        private AndroidJavaObject _plugin;
        private AndroidJavaObject _unityActivity;
        private bool _isConnected;
        private bool _isInitialized;
        private bool _isDisposed;
        private GC2DeviceStatus? _lastDeviceStatus;
        private GC2DeviceInfo _deviceInfo;

        #endregion

        #region IGC2Connection Properties

        /// <summary>
        /// Whether currently connected to a GC2 device.
        /// </summary>
        public bool IsConnected => _isConnected && !_isDisposed && IsNativeConnected();

        /// <summary>
        /// Information about the connected device, or null if not connected.
        /// </summary>
        public GC2DeviceInfo DeviceInfo => _isConnected ? _deviceInfo : null;

        #endregion

        #region IGC2Connection Events

        /// <summary>
        /// Fired when shot data is received from the GC2 (0H messages).
        /// </summary>
        public event Action<GC2ShotData> OnShotReceived;

        /// <summary>
        /// Fired when connection state changes.
        /// </summary>
        public event Action<bool> OnConnectionChanged;

        /// <summary>
        /// Fired when an error occurs.
        /// </summary>
        public event Action<string> OnError;

        /// <summary>
        /// Fired when device status changes (0M messages).
        /// </summary>
        public event Action<GC2DeviceStatus> OnDeviceStatusChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializePlugin();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void OnApplicationQuit()
        {
            Cleanup();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // App is going to background - may need to handle USB disconnection
                Debug.Log("GC2AndroidConnection: App paused");
            }
            else
            {
                // App is resuming - check if we need to reconnect
                Debug.Log("GC2AndroidConnection: App resumed");
            }
        }

        #endregion

        #region Plugin Lifecycle

        private void InitializePlugin()
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
                    Debug.LogError("GC2AndroidConnection: Failed to get Unity activity");
                    return;
                }

                // Get the plugin singleton
                using (var pluginClass = new AndroidJavaClass(PluginClassName))
                {
                    _plugin = pluginClass.CallStatic<AndroidJavaObject>("getInstance");
                }

                if (_plugin == null)
                {
                    Debug.LogError("GC2AndroidConnection: Failed to get plugin instance");
                    return;
                }

                // Initialize the plugin with our GameObject name for callbacks
                _plugin.Call("initialize", _unityActivity, gameObject.name);

                _isInitialized = true;
                Debug.Log($"GC2AndroidConnection: Initialized with callback object: {gameObject.name}");
            }
            catch (AndroidJavaException ex)
            {
                Debug.LogError($"GC2AndroidConnection: Java exception during initialization - {ex.Message}");
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"GC2AndroidConnection: Initialization failed - {ex.Message}");
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
                if (_isConnected && _plugin != null)
                {
                    _plugin.Call("disconnect");
                }

                if (_isInitialized && _plugin != null)
                {
                    _plugin.Call("shutdown");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GC2AndroidConnection: Cleanup error - {ex.Message}");
            }
            finally
            {
                _plugin?.Dispose();
                _unityActivity?.Dispose();
                _plugin = null;
                _unityActivity = null;
            }

            _isConnected = false;
            _isInitialized = false;
            _lastDeviceStatus = null;
            _deviceInfo = null;

            // Clear event handlers
            OnShotReceived = null;
            OnConnectionChanged = null;
            OnError = null;
            OnDeviceStatusChanged = null;
        }

        #endregion

        #region IGC2Connection Methods

        /// <summary>
        /// Check if a GC2 device is available for connection.
        /// </summary>
        public bool IsDeviceAvailable()
        {
            if (!_isInitialized || _isDisposed || _plugin == null)
                return false;

            try
            {
                return _plugin.Call<bool>("isDeviceAvailable");
            }
            catch (Exception ex)
            {
                Debug.LogError($"GC2AndroidConnection: IsDeviceAvailable failed - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Attempt to connect to the GC2 device.
        /// </summary>
        public Task<bool> ConnectAsync()
        {
            if (_isDisposed)
                return Task.FromResult(false);

            if (_isConnected)
            {
                Debug.LogWarning("GC2AndroidConnection: Already connected");
                return Task.FromResult(true);
            }

            if (!_isInitialized)
            {
                InitializePlugin();
                if (!_isInitialized)
                {
                    NotifyError("Plugin not initialized");
                    return Task.FromResult(false);
                }
            }

            try
            {
                // Pass the activity context for permission requests
                bool success = _plugin.Call<bool>("connect", _unityActivity);

                if (success)
                {
                    // Connection will be confirmed via OnNativeConnectionChanged callback
                    Debug.Log("GC2AndroidConnection: Connection initiated (waiting for permission/callback)");
                }
                else
                {
                    NotifyError("Failed to initiate connection to GC2 device");
                }

                return Task.FromResult(success);
            }
            catch (Exception ex)
            {
                Debug.LogError($"GC2AndroidConnection: Connect failed - {ex.Message}");
                NotifyError($"Connection error: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Disconnect from the GC2 device.
        /// </summary>
        public void Disconnect()
        {
            if (!_isConnected || _isDisposed || _plugin == null)
                return;

            try
            {
                _plugin.Call("disconnect");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GC2AndroidConnection: Disconnect error - {ex.Message}");
            }

            _isConnected = false;
            _lastDeviceStatus = null;
            _deviceInfo = null;
            NotifyConnectionChanged(false);
            Debug.Log("GC2AndroidConnection: Disconnected");
        }

        #endregion

        #region Native Callbacks (Called via UnitySendMessage)

        /// <summary>
        /// Called by native plugin when shot data is received.
        /// JSON format matches GC2ShotData fields.
        /// </summary>
        public void OnNativeShotReceived(string json)
        {
            if (_isDisposed || string.IsNullOrEmpty(json))
                return;

            try
            {
                var shot = ParseShotJson(json);
                if (shot != null)
                {
                    MainThreadDispatcher.Execute(() =>
                    {
                        try
                        {
                            OnShotReceived?.Invoke(shot);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"GC2AndroidConnection: Shot callback error - {ex.Message}");
                        }
                    });
                    Debug.Log($"GC2AndroidConnection: Received shot - Speed: {shot.BallSpeed:F1} mph, " +
                              $"BackSpin: {shot.BackSpin:F0} rpm, SideSpin: {shot.SideSpin:F0} rpm");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"GC2AndroidConnection: Shot parsing failed - {ex.Message}");
            }
        }

        /// <summary>
        /// Called by native plugin when connection state changes.
        /// </summary>
        public void OnNativeConnectionChanged(string connected)
        {
            if (_isDisposed)
                return;

            bool isConnected = string.Equals(connected, "true", StringComparison.OrdinalIgnoreCase);

            if (isConnected != _isConnected)
            {
                _isConnected = isConnected;

                if (isConnected)
                {
                    UpdateDeviceInfo();
                }
                else
                {
                    _deviceInfo = null;
                    _lastDeviceStatus = null;
                }

                MainThreadDispatcher.Execute(() => NotifyConnectionChanged(isConnected));
                Debug.Log($"GC2AndroidConnection: Connection state changed - {(isConnected ? "Connected" : "Disconnected")}");
            }
        }

        /// <summary>
        /// Called by native plugin when an error occurs.
        /// </summary>
        public void OnNativeError(string error)
        {
            if (_isDisposed || string.IsNullOrEmpty(error))
                return;

            MainThreadDispatcher.Execute(() => NotifyError(error));
            Debug.LogError($"GC2AndroidConnection: Native error - {error}");
        }

        /// <summary>
        /// Called by native plugin when device status changes (0M messages).
        /// JSON format: {"IsReady": bool, "BallDetected": bool, "RawFlags": int, "BallCount": int}
        /// </summary>
        public void OnNativeDeviceStatus(string json)
        {
            if (_isDisposed || string.IsNullOrEmpty(json))
                return;

            try
            {
                var status = ParseDeviceStatusJson(json);

                // Only notify if status changed
                if (!status.Equals(_lastDeviceStatus))
                {
                    _lastDeviceStatus = status;

                    MainThreadDispatcher.Execute(() =>
                    {
                        try
                        {
                            OnDeviceStatusChanged?.Invoke(status);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"GC2AndroidConnection: Status callback error - {ex.Message}");
                        }
                    });
                    Debug.Log($"GC2AndroidConnection: Device status - Ready: {status.IsReady}, Ball: {status.BallDetected}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"GC2AndroidConnection: Status parsing failed - {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private bool IsNativeConnected()
        {
            if (!_isInitialized || _isDisposed || _plugin == null)
                return false;

            try
            {
                return _plugin.Call<bool>("isConnected");
            }
            catch
            {
                return false;
            }
        }

        private void UpdateDeviceInfo()
        {
            try
            {
                var serial = _plugin?.Call<string>("getDeviceSerial");
                var firmware = _plugin?.Call<string>("getFirmwareVersion");

                _deviceInfo = new GC2DeviceInfo
                {
                    SerialNumber = serial ?? "Unknown",
                    FirmwareVersion = firmware ?? "Unknown",
                    HasHMT = false // Will be updated when HMT shot is received
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GC2AndroidConnection: Failed to get device info - {ex.Message}");
                _deviceInfo = new GC2DeviceInfo
                {
                    SerialNumber = "Unknown",
                    FirmwareVersion = "Unknown",
                    HasHMT = false
                };
            }
        }

        private void NotifyConnectionChanged(bool connected)
        {
            try
            {
                OnConnectionChanged?.Invoke(connected);
            }
            catch (Exception ex)
            {
                Debug.LogError($"GC2AndroidConnection: Connection callback error - {ex.Message}");
            }
        }

        private void NotifyError(string message)
        {
            Debug.LogError($"GC2AndroidConnection: {message}");
            try
            {
                OnError?.Invoke(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"GC2AndroidConnection: Error callback error - {ex.Message}");
            }
        }

        #endregion

        #region JSON Parsing

        /// <summary>
        /// Parse shot data JSON from native plugin.
        /// Native format matches GC2ShotData field names.
        /// </summary>
        internal static GC2ShotData ParseShotJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                // Use Unity's JsonUtility for parsing
                var wrapper = JsonUtility.FromJson<ShotJsonWrapper>(json);

                var shot = new GC2ShotData
                {
                    ShotId = wrapper.ShotId,
                    Timestamp = wrapper.Timestamp > 0 ? wrapper.Timestamp : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    BallSpeed = wrapper.BallSpeed,
                    LaunchAngle = wrapper.LaunchAngle,
                    Direction = wrapper.LaunchDirection, // Android plugin uses LaunchDirection
                    TotalSpin = wrapper.TotalSpin,
                    BackSpin = wrapper.BackSpin,
                    SideSpin = wrapper.SideSpin,
                    SpinAxis = wrapper.SpinAxis,
                    HasClubData = wrapper.HasClubData,
                    ClubSpeed = wrapper.ClubSpeed,
                    Path = wrapper.ClubPath, // Android plugin uses ClubPath
                    AttackAngle = wrapper.AttackAngle,
                    FaceToTarget = wrapper.FaceToTarget,
                    DynamicLoft = wrapper.DynamicLoft,
                    Lie = wrapper.Lie
                };

                return shot;
            }
            catch (Exception ex)
            {
                Debug.LogError($"GC2AndroidConnection: Failed to parse shot JSON - {ex.Message}");
                Debug.LogError($"GC2AndroidConnection: JSON was: {json}");
                return null;
            }
        }

        /// <summary>
        /// Parse device status JSON from native plugin.
        /// </summary>
        internal static GC2DeviceStatus ParseDeviceStatusJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return GC2DeviceStatus.Unknown;

            try
            {
                var wrapper = JsonUtility.FromJson<DeviceStatusJsonWrapper>(json);

                return new GC2DeviceStatus(
                    wrapper.RawFlags,
                    wrapper.BallCount,
                    null // Android plugin doesn't provide ball position yet
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"GC2AndroidConnection: Failed to parse status JSON - {ex.Message}");
                return GC2DeviceStatus.Unknown;
            }
        }

        /// <summary>
        /// JSON wrapper for shot data parsing.
        /// Field names match Android native plugin output (GC2Protocol.kt).
        /// </summary>
        [Serializable]
        private class ShotJsonWrapper
        {
            public int ShotId;
            public long Timestamp;
            public float BallSpeed;
            public float LaunchAngle;
            public float LaunchDirection; // Android uses LaunchDirection, not Direction
            public float TotalSpin;
            public float BackSpin;
            public float SideSpin;
            public float SpinAxis;
            public bool HasClubData;
            public float ClubSpeed;
            public float ClubPath; // Android uses ClubPath, not Path
            public float AttackAngle;
            public float FaceToTarget;
            public float DynamicLoft;
            public float Lie;
        }

        /// <summary>
        /// JSON wrapper for device status parsing.
        /// Field names match Android native plugin output.
        /// </summary>
        [Serializable]
        private class DeviceStatusJsonWrapper
        {
            public bool IsReady;
            public bool BallDetected;
            public int RawFlags;
            public int BallCount;
        }

        #endregion
    }
#endif
}
