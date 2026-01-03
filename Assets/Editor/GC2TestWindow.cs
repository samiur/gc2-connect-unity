// ABOUTME: Editor window for testing TCP connections without GC2 hardware.
// ABOUTME: Provides controls for server/client mode, connection status, and sending test messages.

using UnityEditor;
using UnityEngine;
using OpenRange.GC2;
using OpenRange.GC2.Platforms.TCP;
using System.Threading.Tasks;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor window for testing TCP connections without GC2 hardware.
    /// Allows connecting as server or client and sending test shots/status messages.
    /// </summary>
    public class GC2TestWindow : EditorWindow
    {
        // Connection settings
        private TCPMode _mode = TCPMode.Server;
        private string _host = "127.0.0.1";
        private int _port = 8888;

        // Connection state
        private GC2TCPConnection _connection;
        private GC2TCPListener _listener;
        private bool _isConnecting;

        // Shot parameters
        private float _ballSpeed = 150f;
        private float _launchAngle = 12f;
        private float _azimuth = 0f;
        private float _backSpin = 3000f;
        private float _sideSpin = 0f;

        // Status parameters
        private bool _deviceReady = true;
        private bool _ballDetected = true;
        private Vector3 _ballPosition = new Vector3(0, 0, 100);

        // Preset selection
        private int _selectedPreset = 0;
        private readonly string[] _presetNames = { "Custom", "Driver", "7-Iron", "Wedge", "Hook", "Slice" };

        // Foldout states
        private bool _showConnection = true;
        private bool _showShotParams = true;
        private bool _showDeviceStatus = false;
        private bool _showLog = true;

        // Log
        private string _logText = "";
        private Vector2 _logScrollPos;
        private const int MaxLogLines = 100;

        [MenuItem("OpenRange/GC2 Test Window", priority = 201)]
        public static void ShowWindow()
        {
            var window = GetWindow<GC2TestWindow>("GC2 Test");
            window.minSize = new Vector2(350, 500);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            Cleanup();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            if (_connection != null)
            {
                _connection.Disconnect();
                if (_connection != null && _connection.gameObject != null)
                {
                    DestroyImmediate(_connection.gameObject);
                }
                _connection = null;
            }

            _listener?.Dispose();
            _listener = null;
        }

        private void OnEditorUpdate()
        {
            // Repaint if connection state might have changed
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("GC2 TCP Test Window", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Test TCP connections for GC2 communication. Use Server mode to accept connections, " +
                "or Client mode to connect to a remote host.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            DrawConnectionSection();
            EditorGUILayout.Space(5);
            DrawShotSection();
            EditorGUILayout.Space(5);
            DrawDeviceStatusSection();
            EditorGUILayout.Space(5);
            DrawLogSection();
        }

        private void DrawConnectionSection()
        {
            _showConnection = EditorGUILayout.Foldout(_showConnection, "Connection", true);
            if (!_showConnection) return;

            EditorGUI.indentLevel++;

            bool isConnected = _connection?.IsConnected ?? false;
            bool hasClient = _listener?.HasClient ?? false;

            // Status display
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Status:", GUILayout.Width(80));

            string statusText;
            Color statusColor;
            if (_isConnecting)
            {
                statusText = "Connecting...";
                statusColor = Color.yellow;
            }
            else if (isConnected)
            {
                statusText = "Connected";
                statusColor = Color.green;
            }
            else if (_listener?.IsRunning ?? false)
            {
                statusText = hasClient ? "Client Connected" : "Listening...";
                statusColor = hasClient ? Color.green : Color.yellow;
            }
            else
            {
                statusText = "Disconnected";
                statusColor = Color.red;
            }

            var originalColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField(statusText, EditorStyles.boldLabel);
            GUI.color = originalColor;
            EditorGUILayout.EndHorizontal();

            // Mode selection
            GUI.enabled = !isConnected && !(_listener?.IsRunning ?? false);
            _mode = (TCPMode)EditorGUILayout.EnumPopup("Mode", _mode);

            // Connection parameters
            if (_mode == TCPMode.Client)
            {
                _host = EditorGUILayout.TextField("Host", _host);
            }
            _port = EditorGUILayout.IntField("Port", _port);
            GUI.enabled = true;

            EditorGUILayout.Space(5);

            // Connect/Disconnect buttons
            EditorGUILayout.BeginHorizontal();

            if (!isConnected && !(_listener?.IsRunning ?? false))
            {
                GUI.enabled = !_isConnecting;
                if (GUILayout.Button(_isConnecting ? "Connecting..." : "Connect", GUILayout.Height(25)))
                {
                    Connect();
                }
                GUI.enabled = true;
            }
            else
            {
                GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
                if (GUILayout.Button("Disconnect", GUILayout.Height(25)))
                {
                    Disconnect();
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        private void DrawShotSection()
        {
            _showShotParams = EditorGUILayout.Foldout(_showShotParams, "Shot Parameters", true);
            if (!_showShotParams) return;

            EditorGUI.indentLevel++;

            // Preset selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Preset:", GUILayout.Width(50));
            int newPreset = EditorGUILayout.Popup(_selectedPreset, _presetNames);
            if (newPreset != _selectedPreset)
            {
                _selectedPreset = newPreset;
                ApplyPreset(newPreset);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            _ballSpeed = EditorGUILayout.Slider("Ball Speed (mph)", _ballSpeed, 50f, 200f);
            _launchAngle = EditorGUILayout.Slider("Launch Angle (°)", _launchAngle, 0f, 45f);
            _azimuth = EditorGUILayout.Slider("Azimuth (°)", _azimuth, -20f, 20f);
            _backSpin = EditorGUILayout.Slider("Back Spin (rpm)", _backSpin, 500f, 12000f);
            _sideSpin = EditorGUILayout.Slider("Side Spin (rpm)", _sideSpin, -3000f, 3000f);

            EditorGUILayout.Space(5);

            // Quick fire buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Driver", GUILayout.Height(25)))
            {
                ApplyPreset(1);
                SendTestShot();
            }
            if (GUILayout.Button("7-Iron", GUILayout.Height(25)))
            {
                ApplyPreset(2);
                SendTestShot();
            }
            if (GUILayout.Button("Wedge", GUILayout.Height(25)))
            {
                ApplyPreset(3);
                SendTestShot();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            // Send shot button
            bool canSend = (_connection?.IsConnected ?? false) || (_listener?.HasClient ?? false);
            GUI.enabled = canSend;
            GUI.backgroundColor = canSend ? new Color(0.2f, 0.8f, 0.2f) : Color.gray;
            if (GUILayout.Button("Send Test Shot", GUILayout.Height(30)))
            {
                SendTestShot();
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            EditorGUI.indentLevel--;
        }

        private void DrawDeviceStatusSection()
        {
            _showDeviceStatus = EditorGUILayout.Foldout(_showDeviceStatus, "Device Status", true);
            if (!_showDeviceStatus) return;

            EditorGUI.indentLevel++;

            _deviceReady = EditorGUILayout.Toggle("Device Ready (FLAGS=7)", _deviceReady);
            _ballDetected = EditorGUILayout.Toggle("Ball Detected (BALLS>0)", _ballDetected);
            _ballPosition = EditorGUILayout.Vector3Field("Ball Position (mm)", _ballPosition);

            EditorGUILayout.Space(3);

            bool canSend = (_connection?.IsConnected ?? false) || (_listener?.HasClient ?? false);
            GUI.enabled = canSend;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Send Status", GUILayout.Height(25)))
            {
                SendDeviceStatus();
            }
            if (GUILayout.Button("Ready + Ball", GUILayout.Height(25)))
            {
                _deviceReady = true;
                _ballDetected = true;
                SendDeviceStatus();
            }
            if (GUILayout.Button("Not Ready", GUILayout.Height(25)))
            {
                _deviceReady = false;
                _ballDetected = false;
                SendDeviceStatus();
            }
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            EditorGUI.indentLevel--;
        }

        private void DrawLogSection()
        {
            _showLog = EditorGUILayout.Foldout(_showLog, "Log", true);
            if (!_showLog) return;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                _logText = "";
            }
            EditorGUILayout.EndHorizontal();

            _logScrollPos = EditorGUILayout.BeginScrollView(_logScrollPos, GUILayout.Height(100));
            EditorGUILayout.TextArea(_logText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void ApplyPreset(int preset)
        {
            switch (preset)
            {
                case 1: // Driver
                    _ballSpeed = 167f;
                    _launchAngle = 10.9f;
                    _azimuth = 0f;
                    _backSpin = 2686f;
                    _sideSpin = 0f;
                    break;

                case 2: // 7-Iron
                    _ballSpeed = 120f;
                    _launchAngle = 16.3f;
                    _azimuth = 0f;
                    _backSpin = 7097f;
                    _sideSpin = 0f;
                    break;

                case 3: // Wedge
                    _ballSpeed = 102f;
                    _launchAngle = 24.2f;
                    _azimuth = 0f;
                    _backSpin = 9304f;
                    _sideSpin = 0f;
                    break;

                case 4: // Hook
                    _ballSpeed = 150f;
                    _launchAngle = 12f;
                    _azimuth = 0f;
                    _backSpin = 3000f;
                    _sideSpin = -1500f;
                    break;

                case 5: // Slice
                    _ballSpeed = 150f;
                    _launchAngle = 12f;
                    _azimuth = 0f;
                    _backSpin = 3000f;
                    _sideSpin = 1500f;
                    break;
            }
        }

        private async void Connect()
        {
            _isConnecting = true;
            Log($"Connecting in {_mode} mode on port {_port}...");

            try
            {
                if (_mode == TCPMode.Server)
                {
                    // Use the listener for server mode
                    _listener = new GC2TCPListener(_port);
                    _listener.OnClientConnected += () => Log("Client connected");
                    _listener.OnClientDisconnected += () => Log("Client disconnected");
                    _listener.OnDataReceived += msg => Log($"Received: {msg.Substring(0, Mathf.Min(50, msg.Length))}...");
                    _listener.OnError += err => Log($"Error: {err}");

                    if (_listener.Start())
                    {
                        Log($"Server listening on port {_port}");
                    }
                    else
                    {
                        Log("Failed to start server");
                    }
                }
                else
                {
                    // Client mode - create connection component
                    var go = new GameObject("GC2TCPConnection (Editor)");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    _connection = go.AddComponent<GC2TCPConnection>();

                    _connection.SetMode(TCPMode.Client);
                    _connection.SetConnectionParams(_host, _port);

                    _connection.OnConnectionChanged += connected =>
                    {
                        Log(connected ? "Connected" : "Disconnected");
                    };
                    _connection.OnShotReceived += shot =>
                    {
                        Log($"Received shot: {shot.BallSpeed:F1} mph");
                    };
                    _connection.OnDeviceStatusChanged += status =>
                    {
                        Log($"Status: Ready={status.IsReady}, Ball={status.BallDetected}");
                    };
                    _connection.OnError += err => Log($"Error: {err}");

                    bool success = await _connection.ConnectAsync();
                    if (!success)
                    {
                        Log($"Failed to connect to {_host}:{_port}");
                        DestroyImmediate(_connection.gameObject);
                        _connection = null;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log($"Connect error: {ex.Message}");
            }
            finally
            {
                _isConnecting = false;
                Repaint();
            }
        }

        private void Disconnect()
        {
            Log("Disconnecting...");

            if (_connection != null)
            {
                _connection.Disconnect();
                if (_connection?.gameObject != null)
                {
                    DestroyImmediate(_connection.gameObject);
                }
                _connection = null;
            }

            if (_listener != null)
            {
                _listener.Stop();
                _listener.Dispose();
                _listener = null;
            }

            Log("Disconnected");
            Repaint();
        }

        private async void SendTestShot()
        {
            var shot = new GC2ShotData
            {
                BallSpeed = _ballSpeed,
                LaunchAngle = _launchAngle,
                Direction = _azimuth,
                TotalSpin = Mathf.Sqrt(_backSpin * _backSpin + _sideSpin * _sideSpin),
                BackSpin = _backSpin,
                SideSpin = _sideSpin,
                Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                HasClubData = false
            };

            Log($"Sending shot: {_ballSpeed:F1} mph, {_launchAngle:F1}°, {_backSpin:F0} rpm");

            if (_listener?.HasClient ?? false)
            {
                bool success = await _listener.SendShotAsync(shot);
                if (success)
                {
                    Log("Shot sent successfully");
                }
            }
            else if (_connection?.IsConnected ?? false)
            {
                await _connection.SendShotAsync(shot);
                Log("Shot sent successfully");
            }
            else
            {
                Log("Error: No connection available");
            }
        }

        private async void SendDeviceStatus()
        {
            Log($"Sending status: Ready={_deviceReady}, Ball={_ballDetected}");

            Vector3? pos = _ballDetected ? (Vector3?)_ballPosition : null;

            if (_listener?.HasClient ?? false)
            {
                bool success = await _listener.SendDeviceStatusAsync(_deviceReady, _ballDetected, pos);
                if (success)
                {
                    Log("Status sent successfully");
                }
            }
            else if (_connection?.IsConnected ?? false)
            {
                await _connection.SendDeviceStatusAsync(_deviceReady, _ballDetected, pos);
                Log("Status sent successfully");
            }
            else
            {
                Log("Error: No connection available");
            }
        }

        private void Log(string message)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            _logText = $"[{timestamp}] {message}\n" + _logText;

            // Trim log if too long
            string[] lines = _logText.Split('\n');
            if (lines.Length > MaxLogLines)
            {
                _logText = string.Join("\n", lines, 0, MaxLogLines);
            }

            Repaint();
        }
    }
}
