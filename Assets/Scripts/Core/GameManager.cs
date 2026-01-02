using System;
using UnityEngine;
using OpenRange.GC2;

namespace OpenRange.Core
{
    /// <summary>
    /// Main application controller.
    /// Manages app lifecycle, mode switching, and high-level coordination.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private ShotProcessor _shotProcessor;
        [SerializeField] private SessionManager _sessionManager;
        [SerializeField] private SettingsManager _settingsManager;

        [Header("State")]
        [SerializeField] private AppMode _currentMode = AppMode.OpenRange;
        [SerializeField] private ConnectionState _connectionState = ConnectionState.Disconnected;

        private IGC2Connection _gc2Connection;

        public AppMode CurrentMode => _currentMode;
        public ConnectionState ConnectionState => _connectionState;
        public IGC2Connection GC2Connection => _gc2Connection;
        public ShotProcessor ShotProcessor => _shotProcessor;
        public SessionManager SessionManager => _sessionManager;

        public event Action<AppMode> OnModeChanged;
        public event Action<ConnectionState> OnConnectionStateChanged;

        private void Awake()
        {
            // Singleton setup
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Ensure references
            if (_shotProcessor == null)
                _shotProcessor = FindObjectOfType<ShotProcessor>();
            if (_sessionManager == null)
                _sessionManager = FindObjectOfType<SessionManager>();
            if (_settingsManager == null)
                _settingsManager = FindObjectOfType<SettingsManager>();
        }

        private void Start()
        {
            InitializeGC2Connection();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                CleanupGC2Connection();
            }
        }

        #region GC2 Connection

        private void InitializeGC2Connection()
        {
            // Create platform-specific connection
            _gc2Connection = GC2ConnectionFactory.Create(gameObject);

            // Subscribe to events
            _gc2Connection.OnShotReceived += HandleShotReceived;
            _gc2Connection.OnConnectionChanged += HandleConnectionChanged;
            _gc2Connection.OnError += HandleConnectionError;

            // Auto-connect if device available
            if (_gc2Connection.IsDeviceAvailable())
            {
                ConnectToGC2();
            }
            else
            {
                SetConnectionState(ConnectionState.DeviceNotFound);
            }
        }

        private void CleanupGC2Connection()
        {
            if (_gc2Connection != null)
            {
                _gc2Connection.OnShotReceived -= HandleShotReceived;
                _gc2Connection.OnConnectionChanged -= HandleConnectionChanged;
                _gc2Connection.OnError -= HandleConnectionError;
                _gc2Connection.Disconnect();
            }
        }

        public async void ConnectToGC2()
        {
            SetConnectionState(ConnectionState.Connecting);

            bool success = await _gc2Connection.ConnectAsync();

            if (success)
            {
                SetConnectionState(ConnectionState.Connected);
                Debug.Log("GameManager: Connected to GC2");
            }
            else
            {
                SetConnectionState(ConnectionState.Failed);
                Debug.LogWarning("GameManager: Failed to connect to GC2");
            }
        }

        public void DisconnectFromGC2()
        {
            _gc2Connection?.Disconnect();
            SetConnectionState(ConnectionState.Disconnected);
        }

        private void HandleShotReceived(GC2ShotData shot)
        {
            Debug.Log($"GameManager: Shot received - {shot}");
            _shotProcessor?.ProcessShot(shot);
        }

        private void HandleConnectionChanged(bool connected)
        {
            SetConnectionState(connected ? ConnectionState.Connected : ConnectionState.Disconnected);
        }

        private void HandleConnectionError(string error)
        {
            Debug.LogError($"GameManager: GC2 error - {error}");
            SetConnectionState(ConnectionState.Failed);
        }

        private void SetConnectionState(ConnectionState state)
        {
            if (_connectionState != state)
            {
                _connectionState = state;
                OnConnectionStateChanged?.Invoke(state);
            }
        }

        #endregion

        #region Mode Switching

        public void SetMode(AppMode mode)
        {
            if (_currentMode != mode)
            {
                _currentMode = mode;
                _shotProcessor?.SetMode(mode);
                OnModeChanged?.Invoke(mode);
                Debug.Log($"GameManager: Mode changed to {mode}");
            }
        }

        public void ToggleMode()
        {
            SetMode(_currentMode == AppMode.OpenRange ? AppMode.GSPro : AppMode.OpenRange);
        }

        #endregion

        #region Session Control

        public void StartNewSession()
        {
            _sessionManager?.StartNewSession();
        }

        public void EndSession()
        {
            _sessionManager?.EndSession();
        }

        #endregion

        #region Test Shot (Editor/Demo)

        [ContextMenu("Fire Test Shot")]
        public void FireTestShot()
        {
            var testShot = new GC2ShotData
            {
                ShotId = UnityEngine.Random.Range(1, 1000),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                BallSpeed = UnityEngine.Random.Range(140f, 170f),
                LaunchAngle = UnityEngine.Random.Range(10f, 15f),
                Direction = UnityEngine.Random.Range(-5f, 5f),
                TotalSpin = UnityEngine.Random.Range(2500f, 3500f),
                BackSpin = UnityEngine.Random.Range(2400f, 3400f),
                SideSpin = UnityEngine.Random.Range(-500f, 500f),
                SpinAxis = UnityEngine.Random.Range(-10f, 10f)
            };

            HandleShotReceived(testShot);
        }

        #endregion
    }

    /// <summary>
    /// Application mode.
    /// </summary>
    public enum AppMode
    {
        /// <summary>Local driving range visualization</summary>
        OpenRange,

        /// <summary>Relay to GSPro</summary>
        GSPro
    }

    /// <summary>
    /// GC2 connection state.
    /// </summary>
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        DeviceNotFound,
        Failed
    }
}
