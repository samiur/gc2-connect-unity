// ABOUTME: TCP connection for GC2 communication - used in Editor for testing and relay mode.
// ABOUTME: Supports both server mode (accept connections) and client mode (connect to host).

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenRange.Utilities;
using UnityEngine;

namespace OpenRange.GC2.Platforms.TCP
{
    /// <summary>
    /// TCP connection mode.
    /// </summary>
    public enum TCPMode
    {
        /// <summary>Listen for incoming connections (for testing)</summary>
        Server,

        /// <summary>Connect to a remote host (for relay)</summary>
        Client
    }

    /// <summary>
    /// TCP-based GC2 connection for Editor testing and relay mode.
    /// In Server mode, listens for incoming connections on the specified port.
    /// In Client mode, connects to a remote host.
    /// </summary>
    public class GC2TCPConnection : MonoBehaviour, IGC2Connection
    {
        private const int DefaultPort = 8888;
        private const int BufferSize = 4096;
        private const int ReconnectDelayMs = 2000;
        private const int MaxReconnectAttempts = 5;

        private string _address = "127.0.0.1";
        private int _port = DefaultPort;
        private TCPMode _mode = TCPMode.Server;
        private bool _isConnected;
        private bool _isDisposed;

        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cancellationTokenSource;
        private StringBuilder _messageBuffer = new StringBuilder();

        private int _reconnectAttempts;
        private bool _shouldReconnect;

        private GC2DeviceStatus? _lastDeviceStatus;

        public bool IsConnected => _isConnected;
        public GC2DeviceInfo DeviceInfo => _isConnected
            ? new GC2DeviceInfo { SerialNumber = $"TCP-{_mode}", FirmwareVersion = "1.0" }
            : null;

        public event Action<GC2ShotData> OnShotReceived;
        public event Action<bool> OnConnectionChanged;
        public event Action<string> OnError;
        public event Action<GC2DeviceStatus> OnDeviceStatusChanged;

        /// <summary>
        /// Current TCP connection mode.
        /// </summary>
        public TCPMode Mode => _mode;

        /// <summary>
        /// Current address (host for client, bind address for server).
        /// </summary>
        public string Address => _address;

        /// <summary>
        /// Current port number.
        /// </summary>
        public int Port => _port;

        /// <summary>
        /// Set TCP connection parameters.
        /// Must be called before ConnectAsync().
        /// </summary>
        public void SetConnectionParams(string address, int port)
        {
            if (_isConnected)
            {
                Debug.LogWarning("GC2TCPConnection: Cannot change params while connected");
                return;
            }

            _address = address ?? "127.0.0.1";
            _port = port > 0 ? port : DefaultPort;
        }

        /// <summary>
        /// Set the connection mode.
        /// Must be called before ConnectAsync().
        /// </summary>
        public void SetMode(TCPMode mode)
        {
            if (_isConnected)
            {
                Debug.LogWarning("GC2TCPConnection: Cannot change mode while connected");
                return;
            }

            _mode = mode;
        }

        public bool IsDeviceAvailable()
        {
            return _isConnected;
        }

        public async Task<bool> ConnectAsync()
        {
            if (_isConnected)
            {
                Debug.LogWarning("GC2TCPConnection: Already connected");
                return true;
            }

            _isDisposed = false;
            _cancellationTokenSource = new CancellationTokenSource();
            _reconnectAttempts = 0;
            _shouldReconnect = true;

            try
            {
                bool connected = _mode == TCPMode.Server
                    ? await StartServerAsync()
                    : await ConnectToHostAsync();

                if (connected)
                {
                    _isConnected = true;
                    NotifyConnectionChanged(true);
                    StartReadLoop();
                    Debug.Log($"GC2TCPConnection: Connected in {_mode} mode on port {_port}");
                }

                return connected;
            }
            catch (Exception ex)
            {
                NotifyError($"Connection failed: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            _shouldReconnect = false;
            CloseConnection();

            if (_isConnected)
            {
                _isConnected = false;
                NotifyConnectionChanged(false);
                Debug.Log("GC2TCPConnection: Disconnected");
            }
        }

        /// <summary>
        /// Send raw data to the connected client/server.
        /// Used for testing to simulate GC2 messages.
        /// </summary>
        public async Task SendDataAsync(string data)
        {
            if (!_isConnected || _stream == null || !_stream.CanWrite)
            {
                Debug.LogWarning("GC2TCPConnection: Cannot send data - not connected or stream not writable");
                return;
            }

            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                await _stream.WriteAsync(bytes, 0, bytes.Length, _cancellationTokenSource?.Token ?? CancellationToken.None);
                await _stream.FlushAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"GC2TCPConnection: Send failed - {ex.Message}");
            }
        }

        /// <summary>
        /// Send a simulated shot message.
        /// Formats the shot data as GC2 protocol and sends it.
        /// </summary>
        public async Task SendShotAsync(GC2ShotData shot)
        {
            string message = FormatShotMessage(shot);
            await SendDataAsync(message);
        }

        /// <summary>
        /// Send a simulated device status message.
        /// </summary>
        public async Task SendDeviceStatusAsync(bool isReady, bool ballDetected, Vector3? ballPosition = null)
        {
            string message = FormatStatusMessage(isReady, ballDetected, ballPosition);
            await SendDataAsync(message);
        }

        #region Server Mode

        private async Task<bool> StartServerAsync()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();
                Debug.Log($"GC2TCPConnection: Server listening on port {_port}");

                // Wait for a client to connect
                _client = await _listener.AcceptTcpClientAsync();
                _stream = _client.GetStream();
                Debug.Log($"GC2TCPConnection: Client connected from {((IPEndPoint)_client.Client.RemoteEndPoint).Address}");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"GC2TCPConnection: Server start failed - {ex.Message}");
                _listener?.Stop();
                _listener = null;
                return false;
            }
        }

        #endregion

        #region Client Mode

        private async Task<bool> ConnectToHostAsync()
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(_address, _port);
                _stream = _client.GetStream();
                Debug.Log($"GC2TCPConnection: Connected to {_address}:{_port}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"GC2TCPConnection: Client connect failed - {ex.Message}");
                _client?.Close();
                _client = null;
                return false;
            }
        }

        #endregion

        #region Read Loop

        private void StartReadLoop()
        {
            Task.Run(async () => await ReadLoopAsync(), _cancellationTokenSource.Token);
        }

        private async Task ReadLoopAsync()
        {
            byte[] buffer = new byte[BufferSize];

            try
            {
                while (!_cancellationTokenSource.IsCancellationRequested && _stream != null && _client != null && _client.Connected)
                {
                    int bytesRead;
                    try
                    {
                        bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    if (bytesRead == 0)
                    {
                        // Connection closed
                        Debug.Log("GC2TCPConnection: Connection closed by remote host");
                        break;
                    }

                    string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    ProcessReceivedData(data);
                }
            }
            catch (Exception ex) when (!_cancellationTokenSource.IsCancellationRequested)
            {
                Debug.LogError($"GC2TCPConnection: Read error - {ex.Message}");
            }
            finally
            {
                if (!_isDisposed && _shouldReconnect)
                {
                    MainThreadDispatcher.Execute(HandleDisconnection);
                }
            }
        }

        private void ProcessReceivedData(string data)
        {
            _messageBuffer.Append(data);
            string bufferContent = _messageBuffer.ToString();

            // Look for complete messages (terminated by \n\t or just \n for simple messages)
            while (true)
            {
                // Check for message terminator (\n\t)
                int terminatorIndex = bufferContent.IndexOf("\n\t", StringComparison.Ordinal);
                int newlineIndex = bufferContent.IndexOf('\n');

                if (terminatorIndex >= 0)
                {
                    // Complete message with terminator
                    string message = bufferContent.Substring(0, terminatorIndex);
                    bufferContent = bufferContent.Substring(terminatorIndex + 2);
                    _messageBuffer.Clear();
                    _messageBuffer.Append(bufferContent);
                    ProcessMessage(message);
                }
                else if (newlineIndex >= 0 && bufferContent.Contains("="))
                {
                    // Check if we have a complete message (has required fields)
                    // Look for double newline or end of meaningful data
                    int doubleNewline = bufferContent.IndexOf("\n\n", StringComparison.Ordinal);
                    if (doubleNewline >= 0)
                    {
                        string message = bufferContent.Substring(0, doubleNewline);
                        bufferContent = bufferContent.Substring(doubleNewline + 2);
                        _messageBuffer.Clear();
                        _messageBuffer.Append(bufferContent);
                        ProcessMessage(message);
                    }
                    else
                    {
                        // Wait for more data
                        break;
                    }
                }
                else
                {
                    // No complete message yet
                    break;
                }
            }
        }

        private void ProcessMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            var messageType = GC2Protocol.GetMessageType(message);

            switch (messageType)
            {
                case GC2MessageType.Shot:
                    var shot = GC2Protocol.Parse(message);
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
                                Debug.LogError($"GC2TCPConnection: Shot callback error - {ex.Message}");
                            }
                        });
                        Debug.Log($"GC2TCPConnection: Received shot - {shot.BallSpeed:F1} mph");
                    }
                    break;

                case GC2MessageType.DeviceStatus:
                    var status = GC2Protocol.ParseDeviceStatus(message);
                    if (status.HasValue && !status.Equals(_lastDeviceStatus))
                    {
                        _lastDeviceStatus = status;
                        MainThreadDispatcher.Execute(() =>
                        {
                            try
                            {
                                OnDeviceStatusChanged?.Invoke(status.Value);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"GC2TCPConnection: Status callback error - {ex.Message}");
                            }
                        });
                        Debug.Log($"GC2TCPConnection: Device status - Ready: {status.Value.IsReady}, Ball: {status.Value.BallDetected}");
                    }
                    break;
            }
        }

        #endregion

        #region Reconnection

        private void HandleDisconnection()
        {
            if (_isDisposed || !_shouldReconnect)
                return;

            bool wasConnected = _isConnected;
            _isConnected = false;

            if (wasConnected)
            {
                NotifyConnectionChanged(false);
            }

            CloseClientConnection();

            // Attempt reconnection
            if (_mode == TCPMode.Client && _reconnectAttempts < MaxReconnectAttempts)
            {
                _reconnectAttempts++;
                int delay = ReconnectDelayMs * _reconnectAttempts;
                Debug.Log($"GC2TCPConnection: Reconnecting in {delay}ms (attempt {_reconnectAttempts}/{MaxReconnectAttempts})");
                Invoke(nameof(AttemptReconnect), delay / 1000f);
            }
            else if (_mode == TCPMode.Server)
            {
                // In server mode, wait for new client connection
                Debug.Log("GC2TCPConnection: Waiting for new client connection...");
                Task.Run(async () =>
                {
                    try
                    {
                        if (_listener != null)
                        {
                            _client = await _listener.AcceptTcpClientAsync();
                            _stream = _client.GetStream();
                            MainThreadDispatcher.Execute(() =>
                            {
                                _isConnected = true;
                                NotifyConnectionChanged(true);
                                StartReadLoop();
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"GC2TCPConnection: Accept failed - {ex.Message}");
                    }
                });
            }
        }

        private void AttemptReconnect()
        {
            if (_isDisposed || !_shouldReconnect)
                return;

            _ = ConnectAsync();
        }

        #endregion

        #region Cleanup

        private void CloseConnection()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            CloseClientConnection();

            _listener?.Stop();
            _listener = null;

            _messageBuffer.Clear();
            _lastDeviceStatus = null;
        }

        private void CloseClientConnection()
        {
            try
            {
                _stream?.Close();
                _stream = null;
            }
            catch { }

            try
            {
                _client?.Close();
                _client = null;
            }
            catch { }
        }

        private void OnDestroy()
        {
            _isDisposed = true;
            _shouldReconnect = false;
            CloseConnection();

            OnShotReceived = null;
            OnConnectionChanged = null;
            OnError = null;
            OnDeviceStatusChanged = null;
        }

        private void OnApplicationQuit()
        {
            _isDisposed = true;
            _shouldReconnect = false;
            CloseConnection();
        }

        #endregion

        #region Notification Helpers

        private void NotifyConnectionChanged(bool connected)
        {
            MainThreadDispatcher.Execute(() =>
            {
                try
                {
                    OnConnectionChanged?.Invoke(connected);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"GC2TCPConnection: Connection callback error - {ex.Message}");
                }
            });
        }

        private void NotifyError(string message)
        {
            Debug.LogError($"GC2TCPConnection: {message}");
            MainThreadDispatcher.Execute(() =>
            {
                try
                {
                    OnError?.Invoke(message);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"GC2TCPConnection: Error callback error - {ex.Message}");
                }
            });
        }

        #endregion

        #region Message Formatting

        /// <summary>
        /// Format a shot as a GC2 protocol message.
        /// </summary>
        internal static string FormatShotMessage(GC2ShotData shot)
        {
            var sb = new StringBuilder();
            sb.AppendLine("0H");
            sb.AppendLine($"SHOT_ID={shot.ShotId}");
            sb.AppendLine($"SPEED_MPH={shot.BallSpeed:F2}");
            sb.AppendLine($"ELEVATION_DEG={shot.LaunchAngle:F2}");
            sb.AppendLine($"AZIMUTH_DEG={shot.Direction:F2}");
            sb.AppendLine($"SPIN_RPM={shot.TotalSpin:F0}");
            sb.AppendLine($"BACK_RPM={shot.BackSpin:F0}");
            sb.AppendLine($"SIDE_RPM={shot.SideSpin:F0}");
            sb.AppendLine($"SPIN_AXIS_DEG={shot.SpinAxis:F2}");

            if (shot.HasClubData)
            {
                sb.AppendLine("HMT=1");
                sb.AppendLine($"CLUBSPEED_MPH={shot.ClubSpeed:F2}");
                sb.AppendLine($"HPATH_DEG={shot.Path:F2}");
                sb.AppendLine($"VPATH_DEG={shot.AttackAngle:F2}");
                sb.AppendLine($"FACE_T_DEG={shot.FaceToTarget:F2}");
                sb.AppendLine($"LOFT_DEG={shot.DynamicLoft:F2}");
            }

            sb.Append("\t"); // Message terminator
            return sb.ToString();
        }

        /// <summary>
        /// Format a device status as a GC2 protocol message.
        /// </summary>
        internal static string FormatStatusMessage(bool isReady, bool ballDetected, Vector3? ballPosition = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("0M");
            sb.AppendLine($"FLAGS={(isReady ? 7 : 1)}");
            sb.AppendLine($"BALLS={(ballDetected ? 1 : 0)}");

            if (ballPosition.HasValue)
            {
                var pos = ballPosition.Value;
                sb.AppendLine($"BALL1={pos.x:F1},{pos.y:F1},{pos.z:F1}");
            }

            sb.Append("\t"); // Message terminator
            return sb.ToString();
        }

        #endregion
    }
}
