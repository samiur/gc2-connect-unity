// ABOUTME: Singleton manager for Bridge Mode operation (background GC2→GSPro relay).
// ABOUTME: Coordinates GC2 connection, GSPro relay, and platform-specific services.

using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using OpenRange.GC2;
using OpenRange.Network;

namespace OpenRange.Core
{
    /// <summary>
    /// Manages Bridge Mode operation for background GC2→GSPro relay.
    /// Use case: Moonlight streaming - relay shots while GSPro is streamed to device.
    /// </summary>
    public class BridgeModeManager : MonoBehaviour
    {
        /// <summary>Singleton instance.</summary>
        public static BridgeModeManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private string _gsProHost = "localhost";
        [SerializeField] private int _gsProPort = GSProClient.DefaultPort;

        [Header("State")]
        [SerializeField] private BridgeModeState _state = BridgeModeState.Disabled;

        private GSProRelay _gsProRelay;
        private IBridgeService _bridgeService;
        private BridgeModeStatistics _statistics;

        // Test shot support when GC2 not connected
        private Coroutine _testShotCoroutine;
        private int _testShotIndex;

        /// <summary>Interval between test shots when GC2 not connected (seconds).</summary>
        public const float TestShotIntervalSeconds = 15f;

        /// <summary>Whether test shots are currently being sent.</summary>
        public bool IsTestShotModeActive => _testShotCoroutine != null;

        /// <summary>Current bridge mode state.</summary>
        public BridgeModeState State => _state;

        /// <summary>GSPro host address.</summary>
        public string GSProHost
        {
            get => _gsProHost;
            set => _gsProHost = value;
        }

        /// <summary>GSPro port.</summary>
        public int GSProPort
        {
            get => _gsProPort;
            set => _gsProPort = value;
        }

        /// <summary>Whether bridge mode is currently enabled (Active or Backgrounded).</summary>
        public bool IsEnabled => _state != BridgeModeState.Disabled;

        /// <summary>Whether GSPro relay is connected.</summary>
        public bool IsGSProConnected => _gsProRelay?.IsConnected ?? false;

        /// <summary>Current bridge mode statistics.</summary>
        public BridgeModeStatistics Statistics => _statistics;

        /// <summary>Fired when bridge mode state changes.</summary>
        public event Action<BridgeModeState> OnBridgeModeStateChanged;

        /// <summary>Fired when a shot is relayed to GSPro.</summary>
        public event Action<GC2ShotData> OnShotRelayed;

        /// <summary>Fired when a shot relay fails.</summary>
        public event Action<GC2ShotData, string> OnShotRelayFailed;

        /// <summary>Fired when an error occurs.</summary>
        public event Action<string> OnError;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _statistics = new BridgeModeStatistics();

            // Load GSPro host/port from saved settings
            var settings = SettingsManager.Instance;
            if (settings != null)
            {
                _gsProHost = settings.GSProHost;
                _gsProPort = settings.GSProPort;
                Debug.Log($"BridgeModeManager: Loaded settings - host={_gsProHost}, port={_gsProPort}");
            }

            // Create platform-specific bridge service
            if (BridgeServiceFactory.IsBridgeModeSupported())
            {
                _bridgeService = BridgeServiceFactory.Create(gameObject);
                Debug.Log($"BridgeModeManager: Bridge service created: {_bridgeService?.GetType().Name ?? "null"}");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                DisableBridgeMode();
                Instance = null;
            }
        }

        private void OnApplicationPause(bool isPaused)
        {
            Debug.Log($"BridgeModeManager: OnApplicationPause(isPaused={isPaused}), state={_state}");

            if (_state == BridgeModeState.Active && isPaused)
            {
                TransitionToBackgrounded();
            }
            else if (_state == BridgeModeState.Backgrounded && !isPaused)
            {
                TransitionToActive();
            }
        }

        /// <summary>
        /// Enable bridge mode and connect to GSPro.
        /// Works with or without GC2 connected - when GC2 not connected, allows GSPro testing.
        /// </summary>
        /// <returns>True if enabled successfully.</returns>
        public async Task<bool> EnableBridgeModeAsync()
        {
            if (_state != BridgeModeState.Disabled)
            {
                Debug.LogWarning("BridgeModeManager: Already enabled");
                return false;
            }

            // Note: GC2 connection is NOT required for bridge mode.
            // - When GC2 connected: Uses connectedDevice foreground service type, relays real shots
            // - When GC2 not connected: Uses dataSync foreground service type, allows GSPro testing
            var gc2Connection = GameManager.Instance?.GC2Connection;
            bool isGC2Connected = gc2Connection?.IsConnected ?? false;

            Debug.Log($"BridgeModeManager: Enabling bridge mode, connecting to {_gsProHost}:{_gsProPort} (GC2 connected: {isGC2Connected})");

            // On Android, the native service handles ALL GSPro communication.
            // Unity's GSProRelay is NOT used - this prevents dual connection issues.
#if UNITY_ANDROID && !UNITY_EDITOR
            Debug.Log("BridgeModeManager: Android - native service will handle GSPro connection");

            // Start platform service - it will handle GSPro connection
            if (_bridgeService != null)
            {
                // Configure GSPro parameters for native background relay
                ConfigureBridgeServiceGSPro(isGC2Connected);
                await _bridgeService.StartAsync();
            }
            else
            {
                Debug.LogError("BridgeModeManager: No bridge service available on Android");
                OnError?.Invoke("Bridge service not available");
                return false;
            }

            // Subscribe to GC2 shots (for relaying via native service)
            if (GameManager.Instance?.GC2Connection != null)
            {
                GameManager.Instance.GC2Connection.OnShotReceived += HandleGC2Shot;
            }
#else
            // On other platforms (macOS, Editor), use Unity's GSProRelay
            // Create and connect relay
            _gsProRelay = new GSProRelay();
            _gsProRelay.OnShotRelayed += HandleShotRelayed;
            _gsProRelay.OnError += HandleRelayError;

            bool connected = await _gsProRelay.ConnectAsync(_gsProHost, _gsProPort);
            if (!connected)
            {
                Debug.LogError("BridgeModeManager: Failed to connect to GSPro");
                _gsProRelay.OnShotRelayed -= HandleShotRelayed;
                _gsProRelay.OnError -= HandleRelayError;
                _gsProRelay.Dispose();
                _gsProRelay = null;
                OnError?.Invoke("Failed to connect to GSPro");
                return false;
            }

            // Subscribe to GC2 shots
            if (GameManager.Instance?.GC2Connection != null)
            {
                GameManager.Instance.GC2Connection.OnShotReceived += HandleGC2Shot;
            }

            // Start platform service if available
            if (_bridgeService != null)
            {
                // Configure GSPro parameters for native background relay
                ConfigureBridgeServiceGSPro(isGC2Connected);
                await _bridgeService.StartAsync();
            }
#endif

            // Update state
            _statistics.StartTime = DateTime.UtcNow;
            SetState(BridgeModeState.Active);

            // Note: Test shots are now handled by the native Android service when backgrounded.
            // They should ONLY be sent when the app is in the background, GC2 is not connected,
            // and GSPro IS connected. Unity coroutines don't run when backgrounded, so the
            // native service handles this instead.

            Debug.Log("BridgeModeManager: Bridge mode enabled");
            return true;
        }

        /// <summary>
        /// Disable bridge mode and disconnect from GSPro.
        /// </summary>
        public void DisableBridgeMode()
        {
            if (_state == BridgeModeState.Disabled)
            {
                return;
            }

            Debug.Log("BridgeModeManager: Disabling bridge mode");

            // Stop test shot mode if active
            StopTestShotMode();

            // Unsubscribe from GC2 shots
            if (GameManager.Instance?.GC2Connection != null)
            {
                GameManager.Instance.GC2Connection.OnShotReceived -= HandleGC2Shot;
            }

            // Stop platform service
            _bridgeService?.Stop();

            // Disconnect relay
            if (_gsProRelay != null)
            {
                _gsProRelay.OnShotRelayed -= HandleShotRelayed;
                _gsProRelay.OnError -= HandleRelayError;
                _gsProRelay.Dispose();
                _gsProRelay = null;
            }

            SetState(BridgeModeState.Disabled);

            Debug.Log("BridgeModeManager: Bridge mode disabled");
        }

        /// <summary>
        /// Transition to backgrounded state.
        /// Called when app is paused/backgrounded.
        /// </summary>
        public void TransitionToBackgrounded()
        {
            if (_state != BridgeModeState.Active)
            {
                return;
            }

            Debug.Log("BridgeModeManager: Transitioning to backgrounded");

            // Notify Android service that app is backgrounded (triggers test shots if conditions met)
            NotifyBridgeServiceAppBackgrounded();

            SetState(BridgeModeState.Backgrounded);
        }

        /// <summary>
        /// Transition to active state.
        /// Called when app returns to foreground.
        /// </summary>
        public void TransitionToActive()
        {
            if (_state != BridgeModeState.Backgrounded)
            {
                return;
            }

            Debug.Log("BridgeModeManager: Transitioning to active");

            // Notify Android service that app is resumed (stops test shots)
            NotifyBridgeServiceAppResumed();

            SetState(BridgeModeState.Active);
        }

        /// <summary>
        /// Set the platform-specific bridge service.
        /// </summary>
        /// <param name="service">The bridge service to use.</param>
        public void SetBridgeService(IBridgeService service)
        {
            _bridgeService = service;
        }

        /// <summary>
        /// Reset statistics counters.
        /// </summary>
        public void ResetStatistics()
        {
            _statistics.Reset();
            if (IsEnabled)
            {
                _statistics.StartTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Relay a shot to GSPro.
        /// Called internally when a shot is received from GC2.
        /// </summary>
        /// <param name="shot">The shot data to relay.</param>
        public void RelayShotToGSPro(GC2ShotData shot)
        {
            if (_state == BridgeModeState.Disabled)
            {
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            // On Android, use native service to send shots (it handles all GSPro communication)
            var androidService = _bridgeService as OpenRange.GC2.Platforms.Android.AndroidBridgeService;
            if (androidService != null)
            {
                if (androidService.SendShot(shot))
                {
                    // Statistics updated via OnBridgeShotRelayed callback from native
                    return;
                }
                else
                {
                    _statistics.ShotsRejected++;
                    OnShotRelayFailed?.Invoke(shot, "Failed to send via native service");
                    return;
                }
            }
            else
            {
                _statistics.ShotsRejected++;
                OnShotRelayFailed?.Invoke(shot, "Android bridge service not available");
                return;
            }
#else
            // On other platforms, use Unity's GSProRelay
            if (_gsProRelay == null || !_gsProRelay.IsConnected)
            {
                _statistics.ShotsRejected++;
                OnShotRelayFailed?.Invoke(shot, "GSPro not connected");
                return;
            }

            _gsProRelay.RelayShot(shot);
#endif
        }

        /// <summary>
        /// Update device status for GSPro heartbeat.
        /// </summary>
        /// <param name="isReady">Whether GC2 is ready.</param>
        /// <param name="ballDetected">Whether ball is detected.</param>
        public void UpdateDeviceStatus(bool isReady, bool ballDetected)
        {
            _gsProRelay?.UpdateDeviceStatus(isReady, ballDetected);
        }

        private void SetState(BridgeModeState newState)
        {
            if (_state != newState)
            {
                var oldState = _state;
                _state = newState;
                Debug.Log($"BridgeModeManager: State changed from {oldState} to {newState}");
                OnBridgeModeStateChanged?.Invoke(newState);
            }
        }

        private void HandleGC2Shot(GC2ShotData shot)
        {
            RelayShotToGSPro(shot);
        }

        private void HandleShotRelayed(GC2ShotData shot)
        {
            _statistics.ShotsRelayed++;
            OnShotRelayed?.Invoke(shot);
        }

        private void HandleRelayError(string error)
        {
            _statistics.ShotsRejected++;
            OnError?.Invoke(error);
        }

        /// <summary>
        /// Configures the platform bridge service with GSPro connection parameters.
        /// On Android, enables test shot mode when GC2 is not connected.
        /// </summary>
        private void ConfigureBridgeServiceGSPro(bool isGC2Connected)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // On Android, configure the native service to handle GSPro connection and test shots
            var androidService = _bridgeService as OpenRange.GC2.Platforms.Android.AndroidBridgeService;
            if (androidService != null)
            {
                // Enable test shot mode when GC2 is not connected
                // Test shots will be sent by the native service when the app is backgrounded
                bool testShotMode = !isGC2Connected;
                androidService.ConfigureGSPro(_gsProHost, _gsProPort, testShotMode);
                Debug.Log($"BridgeModeManager: Configured Android service - testShotMode={testShotMode}");
            }
#endif
        }

        /// <summary>
        /// Notifies the Android bridge service that the app has gone to background.
        /// </summary>
        private void NotifyBridgeServiceAppBackgrounded()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Debug.Log($"BridgeModeManager: NotifyBridgeServiceAppBackgrounded - bridgeService={_bridgeService?.GetType().Name ?? "null"}");
            var androidService = _bridgeService as OpenRange.GC2.Platforms.Android.AndroidBridgeService;
            if (androidService != null)
            {
                androidService.NotifyAppBackgrounded();
            }
            else
            {
                Debug.LogWarning("BridgeModeManager: Could not cast to AndroidBridgeService");
            }
#endif
        }

        /// <summary>
        /// Notifies the Android bridge service that the app has returned to foreground.
        /// </summary>
        private void NotifyBridgeServiceAppResumed()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Debug.Log($"BridgeModeManager: NotifyBridgeServiceAppResumed - bridgeService={_bridgeService?.GetType().Name ?? "null"}");
            var androidService = _bridgeService as OpenRange.GC2.Platforms.Android.AndroidBridgeService;
            if (androidService != null)
            {
                androidService.NotifyAppResumed();
            }
            else
            {
                Debug.LogWarning("BridgeModeManager: Could not cast to AndroidBridgeService");
            }
#endif
        }

        #region Test Shot Mode (When GC2 Not Connected)

        /// <summary>
        /// Test shot presets for GSPro testing without GC2.
        /// Cycles through Driver, 7-Iron, and Wedge shots.
        /// </summary>
        private static readonly GC2ShotData[] TestShotPresets = new[]
        {
            // Driver - 167 mph, 10.9° launch, 2686 rpm backspin
            new GC2ShotData
            {
                ShotId = 0,
                BallSpeed = 167f,
                LaunchAngle = 10.9f,
                Direction = 0f,
                TotalSpin = 2686f,
                BackSpin = 2686f,
                SideSpin = 0f,
                SpinAxis = 0f,
                Timestamp = 0
            },
            // 7-Iron - 120 mph, 16.3° launch, 7097 rpm backspin
            new GC2ShotData
            {
                ShotId = 0,
                BallSpeed = 120f,
                LaunchAngle = 16.3f,
                Direction = 0f,
                TotalSpin = 7097f,
                BackSpin = 7097f,
                SideSpin = 0f,
                SpinAxis = 0f,
                Timestamp = 0
            },
            // Pitching Wedge - 102 mph, 24.2° launch, 9304 rpm backspin
            new GC2ShotData
            {
                ShotId = 0,
                BallSpeed = 102f,
                LaunchAngle = 24.2f,
                Direction = 0f,
                TotalSpin = 9304f,
                BackSpin = 9304f,
                SideSpin = 0f,
                SpinAxis = 0f,
                Timestamp = 0
            }
        };

        /// <summary>
        /// Start sending periodic test shots to GSPro.
        /// Called automatically when bridge mode is enabled and GC2 is not connected.
        /// </summary>
        public void StartTestShotMode()
        {
            if (_testShotCoroutine != null)
            {
                Debug.Log("BridgeModeManager: Test shot mode already active");
                return;
            }

            if (_state == BridgeModeState.Disabled)
            {
                Debug.LogWarning("BridgeModeManager: Cannot start test shot mode - bridge mode not enabled");
                return;
            }

            Debug.Log($"BridgeModeManager: Starting test shot mode (interval: {TestShotIntervalSeconds}s)");
            _testShotIndex = 0;
            _testShotCoroutine = StartCoroutine(TestShotCoroutine());
        }

        /// <summary>
        /// Stop sending periodic test shots.
        /// Called automatically when GC2 connects or bridge mode is disabled.
        /// </summary>
        public void StopTestShotMode()
        {
            if (_testShotCoroutine == null)
            {
                return;
            }

            Debug.Log("BridgeModeManager: Stopping test shot mode");
            StopCoroutine(_testShotCoroutine);
            _testShotCoroutine = null;
        }

        private IEnumerator TestShotCoroutine()
        {
            // Send first shot immediately
            SendTestShot();

            while (true)
            {
                yield return new WaitForSeconds(TestShotIntervalSeconds);

                // Check if we should stop (GC2 connected or bridge mode disabled)
                var gc2Connection = GameManager.Instance?.GC2Connection;
                if (gc2Connection?.IsConnected == true)
                {
                    Debug.Log("BridgeModeManager: GC2 connected, stopping test shot mode");
                    _testShotCoroutine = null;
                    yield break;
                }

                if (_state == BridgeModeState.Disabled)
                {
                    Debug.Log("BridgeModeManager: Bridge mode disabled, stopping test shot mode");
                    _testShotCoroutine = null;
                    yield break;
                }

                SendTestShot();
            }
        }

        private void SendTestShot()
        {
            if (_gsProRelay == null || !_gsProRelay.IsConnected)
            {
                Debug.LogWarning("BridgeModeManager: Cannot send test shot - GSPro not connected");
                return;
            }

            // Get the next preset and cycle
            var preset = TestShotPresets[_testShotIndex];
            _testShotIndex = (_testShotIndex + 1) % TestShotPresets.Length;

            // Create a copy with updated shot ID and timestamp
            var testShot = new GC2ShotData
            {
                ShotId = _statistics.ShotsRelayed + 1,
                BallSpeed = preset.BallSpeed,
                LaunchAngle = preset.LaunchAngle,
                Direction = preset.Direction,
                TotalSpin = preset.TotalSpin,
                BackSpin = preset.BackSpin,
                SideSpin = preset.SideSpin,
                SpinAxis = preset.SpinAxis,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            string[] clubNames = { "Driver", "7-Iron", "PW" };
            string clubName = clubNames[(_testShotIndex + TestShotPresets.Length - 1) % TestShotPresets.Length];

            Debug.Log($"BridgeModeManager: Sending test shot #{testShot.ShotId} ({clubName}) - {testShot.BallSpeed} mph");

            _gsProRelay.RelayShot(testShot);
        }

        #endregion

        #region Testing Support

        /// <summary>
        /// Force initialize singleton for testing.
        /// </summary>
        internal void ForceInitializeSingleton()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        /// <summary>
        /// Force set state for testing.
        /// </summary>
        internal void ForceSetState(BridgeModeState state)
        {
            _state = state;
        }

        /// <summary>
        /// Get the current GSProRelay for testing.
        /// </summary>
        internal GSProRelay GetGSProRelay() => _gsProRelay;

        /// <summary>
        /// Set statistics for testing.
        /// </summary>
        internal void SetStatistics(BridgeModeStatistics stats)
        {
            _statistics = stats;
        }

        #endregion
    }
}
