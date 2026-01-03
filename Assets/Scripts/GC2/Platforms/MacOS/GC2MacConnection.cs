// ABOUTME: C# bridge for the macOS native USB plugin using libusb.
// ABOUTME: Implements IGC2Connection via DllImport calls to GC2MacPlugin.bundle.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AOT;
using OpenRange.Utilities;
using UnityEngine;

namespace OpenRange.GC2.Platforms.MacOS
{
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    /// <summary>
    /// macOS implementation of IGC2Connection using libusb native plugin.
    /// Communicates with GC2MacPlugin.bundle via DllImport and function pointer callbacks.
    /// </summary>
    public class GC2MacConnection : MonoBehaviour, IGC2Connection
    {
        #region Native Callback Delegates

        // Delegate types matching native callback signatures
        private delegate void NativeShotCallback(string jsonData);
        private delegate void NativeConnectionCallback(string connected);
        private delegate void NativeErrorCallback(string error);
        private delegate void NativeDeviceStatusCallback(string jsonData);

        #endregion

        #region Native Plugin Imports

        private const string PluginName = "GC2MacPlugin";

        [DllImport(PluginName)]
        private static extern void GC2Mac_Initialize(string callbackObject);

        [DllImport(PluginName)]
        private static extern void GC2Mac_Shutdown();

        [DllImport(PluginName)]
        private static extern bool GC2Mac_IsDeviceAvailable();

        [DllImport(PluginName)]
        private static extern bool GC2Mac_Connect();

        [DllImport(PluginName)]
        private static extern void GC2Mac_Disconnect();

        [DllImport(PluginName)]
        private static extern bool GC2Mac_IsConnected();

        [DllImport(PluginName)]
        private static extern IntPtr GC2Mac_GetDeviceSerial();

        [DllImport(PluginName)]
        private static extern IntPtr GC2Mac_GetFirmwareVersion();

        // Callback registration functions
        [DllImport(PluginName)]
        private static extern void GC2Mac_SetShotCallback(NativeShotCallback callback);

        [DllImport(PluginName)]
        private static extern void GC2Mac_SetConnectionCallback(NativeConnectionCallback callback);

        [DllImport(PluginName)]
        private static extern void GC2Mac_SetErrorCallback(NativeErrorCallback callback);

        [DllImport(PluginName)]
        private static extern void GC2Mac_SetDeviceStatusCallback(NativeDeviceStatusCallback callback);

        #endregion

        #region Static Instance for Callbacks

        // Static instance for routing native callbacks to the active connection
        private static GC2MacConnection s_instance;

        #endregion

        #region Private Fields

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

        #endregion

        #region Plugin Lifecycle

        private void InitializePlugin()
        {
            if (_isInitialized || _isDisposed)
                return;

            try
            {
                // Set static instance for callback routing
                s_instance = this;

                // Initialize native plugin
                GC2Mac_Initialize(gameObject.name);

                // Register function pointer callbacks (required for IL2CPP builds)
                GC2Mac_SetShotCallback(OnNativeShotCallbackStatic);
                GC2Mac_SetConnectionCallback(OnNativeConnectionCallbackStatic);
                GC2Mac_SetErrorCallback(OnNativeErrorCallbackStatic);
                GC2Mac_SetDeviceStatusCallback(OnNativeDeviceStatusCallbackStatic);

                _isInitialized = true;
                Debug.Log($"GC2MacConnection: Initialized with function pointer callbacks");
            }
            catch (DllNotFoundException ex)
            {
                Debug.LogError($"GC2MacConnection: Native plugin not found - {ex.Message}");
                Debug.LogError("GC2MacConnection: Make sure GC2MacPlugin.bundle is in Assets/Plugins/macOS/");
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"GC2MacConnection: Initialization failed - {ex.Message}");
                _isInitialized = false;
            }
        }

        #endregion

        #region Static Native Callbacks (IL2CPP compatible)

        // These static methods are called from native code and route to the instance
        [MonoPInvokeCallback(typeof(NativeShotCallback))]
        private static void OnNativeShotCallbackStatic(string jsonData)
        {
            if (s_instance != null && !s_instance._isDisposed)
            {
                s_instance.OnNativeShotReceived(jsonData);
            }
        }

        [MonoPInvokeCallback(typeof(NativeConnectionCallback))]
        private static void OnNativeConnectionCallbackStatic(string connected)
        {
            if (s_instance != null && !s_instance._isDisposed)
            {
                s_instance.OnNativeConnectionChanged(connected);
            }
        }

        [MonoPInvokeCallback(typeof(NativeErrorCallback))]
        private static void OnNativeErrorCallbackStatic(string error)
        {
            if (s_instance != null && !s_instance._isDisposed)
            {
                s_instance.OnNativeError(error);
            }
        }

        [MonoPInvokeCallback(typeof(NativeDeviceStatusCallback))]
        private static void OnNativeDeviceStatusCallbackStatic(string jsonData)
        {
            if (s_instance != null && !s_instance._isDisposed)
            {
                s_instance.OnNativeDeviceStatus(jsonData);
            }
        }

        private void Cleanup()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            try
            {
                // Clear native callbacks before shutdown
                if (_isInitialized)
                {
                    GC2Mac_SetShotCallback(null);
                    GC2Mac_SetConnectionCallback(null);
                    GC2Mac_SetErrorCallback(null);
                    GC2Mac_SetDeviceStatusCallback(null);
                }

                if (_isConnected)
                {
                    GC2Mac_Disconnect();
                }

                if (_isInitialized)
                {
                    GC2Mac_Shutdown();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GC2MacConnection: Cleanup error - {ex.Message}");
            }

            _isConnected = false;
            _isInitialized = false;
            _lastDeviceStatus = null;
            _deviceInfo = null;

            // Clear static instance
            if (s_instance == this)
            {
                s_instance = null;
            }

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
            if (!_isInitialized || _isDisposed)
                return false;

            try
            {
                return GC2Mac_IsDeviceAvailable();
            }
            catch (Exception ex)
            {
                Debug.LogError($"GC2MacConnection: IsDeviceAvailable failed - {ex.Message}");
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
                Debug.LogWarning("GC2MacConnection: Already connected");
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
                bool success = GC2Mac_Connect();

                if (success)
                {
                    _isConnected = true;
                    UpdateDeviceInfo();
                    NotifyConnectionChanged(true);
                    Debug.Log("GC2MacConnection: Connected to GC2 device");
                }
                else
                {
                    NotifyError("Failed to connect to GC2 device");
                }

                return Task.FromResult(success);
            }
            catch (Exception ex)
            {
                Debug.LogError($"GC2MacConnection: Connect failed - {ex.Message}");
                NotifyError($"Connection error: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Disconnect from the GC2 device.
        /// </summary>
        public void Disconnect()
        {
            if (!_isConnected || _isDisposed)
                return;

            try
            {
                GC2Mac_Disconnect();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GC2MacConnection: Disconnect error - {ex.Message}");
            }

            _isConnected = false;
            _lastDeviceStatus = null;
            _deviceInfo = null;
            NotifyConnectionChanged(false);
            Debug.Log("GC2MacConnection: Disconnected");
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
                            Debug.LogError($"GC2MacConnection: Shot callback error - {ex.Message}");
                        }
                    });
                    Debug.Log($"GC2MacConnection: Received shot - {shot.BallSpeed:F1} mph");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"GC2MacConnection: Shot parsing failed - {ex.Message}");
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
                Debug.Log($"GC2MacConnection: Connection state changed - {(isConnected ? "Connected" : "Disconnected")}");
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
            Debug.LogError($"GC2MacConnection: Native error - {error}");
        }

        /// <summary>
        /// Called by native plugin when device status changes (0M messages).
        /// JSON format: {"isReady": bool, "ballDetected": bool, "rawFlags": int, "ballCount": int}
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
                            Debug.LogError($"GC2MacConnection: Status callback error - {ex.Message}");
                        }
                    });
                    Debug.Log($"GC2MacConnection: Device status - Ready: {status.IsReady}, Ball: {status.BallDetected}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"GC2MacConnection: Status parsing failed - {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private bool IsNativeConnected()
        {
            if (!_isInitialized || _isDisposed)
                return false;

            try
            {
                return GC2Mac_IsConnected();
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
                var serialPtr = GC2Mac_GetDeviceSerial();
                var firmwarePtr = GC2Mac_GetFirmwareVersion();

                _deviceInfo = new GC2DeviceInfo
                {
                    SerialNumber = serialPtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(serialPtr) : "Unknown",
                    FirmwareVersion = firmwarePtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(firmwarePtr) : "Unknown",
                    HasHMT = false // Will be updated when HMT shot is received
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GC2MacConnection: Failed to get device info - {ex.Message}");
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
                Debug.LogError($"GC2MacConnection: Connection callback error - {ex.Message}");
            }
        }

        private void NotifyError(string message)
        {
            Debug.LogError($"GC2MacConnection: {message}");
            try
            {
                OnError?.Invoke(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"GC2MacConnection: Error callback error - {ex.Message}");
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
                    Direction = wrapper.Direction,
                    TotalSpin = wrapper.TotalSpin,
                    BackSpin = wrapper.BackSpin,
                    SideSpin = wrapper.SideSpin,
                    SpinAxis = wrapper.SpinAxis,
                    HasClubData = wrapper.HasClubData,
                    ClubSpeed = wrapper.ClubSpeed,
                    Path = wrapper.Path,
                    AttackAngle = wrapper.AttackAngle,
                    FaceToTarget = wrapper.FaceToTarget,
                    DynamicLoft = wrapper.DynamicLoft,
                    Lie = wrapper.Lie
                };

                return shot;
            }
            catch (Exception ex)
            {
                Debug.LogError($"GC2MacConnection: Failed to parse shot JSON - {ex.Message}");
                Debug.LogError($"GC2MacConnection: JSON was: {json}");
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
                    wrapper.rawFlags,
                    wrapper.ballCount,
                    wrapper.hasBallPosition ? new Vector3(wrapper.ballX, wrapper.ballY, wrapper.ballZ) : (Vector3?)null
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"GC2MacConnection: Failed to parse status JSON - {ex.Message}");
                return GC2DeviceStatus.Unknown;
            }
        }

        /// <summary>
        /// JSON wrapper for shot data parsing.
        /// Field names match native plugin output.
        /// </summary>
        [Serializable]
        private class ShotJsonWrapper
        {
            public int ShotId;
            public long Timestamp;
            public float BallSpeed;
            public float LaunchAngle;
            public float Direction;
            public float TotalSpin;
            public float BackSpin;
            public float SideSpin;
            public float SpinAxis;
            public bool HasClubData;
            public float ClubSpeed;
            public float Path;
            public float AttackAngle;
            public float FaceToTarget;
            public float DynamicLoft;
            public float Lie;
        }

        /// <summary>
        /// JSON wrapper for device status parsing.
        /// </summary>
        [Serializable]
        private class DeviceStatusJsonWrapper
        {
            public bool isReady;
            public bool ballDetected;
            public int rawFlags;
            public int ballCount;
            public bool hasBallPosition;
            public float ballX;
            public float ballY;
            public float ballZ;
        }

        #endregion
    }
#endif
}
