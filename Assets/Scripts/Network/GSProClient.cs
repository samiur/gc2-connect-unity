// ABOUTME: GSPro Open Connect API v1 TCP client for relay mode.
// ABOUTME: Sends shot data to GSPro and maintains heartbeat connection.

using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using OpenRange.Core;
using OpenRange.GC2;

namespace OpenRange.Network
{
    /// <summary>
    /// TCP client for GSPro Open Connect API v1.
    /// Sends shot data to GSPro and maintains connection with heartbeat.
    /// </summary>
    public class GSProClient : IDisposable, IGSProClient
    {
        /// <summary>Default GSPro port.</summary>
        public const int DefaultPort = 921;

        /// <summary>Heartbeat interval in milliseconds.</summary>
        public const int HeartbeatIntervalMs = 2000;

        /// <summary>Connection timeout in milliseconds.</summary>
        public const int ConnectionTimeoutMs = 5000;

        /// <summary>Timeout for shot response in milliseconds.</summary>
        public const int ShotResponseTimeoutMs = 5000;

        /// <summary>Size of receive buffer for reading responses.</summary>
        private const int ReceiveBufferSize = 4096;

        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _heartbeatCts;
        private CancellationTokenSource _reconnectCts;

        private string _host;
        private int _port;
        private int _shotNumber;
        private bool _isConnected;
        private bool _isReconnecting;

        private bool _launchMonitorIsReady = true;
        private bool _launchMonitorBallDetected;

        private readonly object _lock = new object();
        private readonly byte[] _receiveBuffer = new byte[ReceiveBufferSize];

        /// <summary>Whether the client is connected to GSPro.</summary>
        public bool IsConnected
        {
            get
            {
                lock (_lock)
                {
                    return _isConnected && _client?.Connected == true;
                }
            }
        }

        /// <summary>Whether the client is currently attempting to reconnect.</summary>
        public bool IsReconnecting
        {
            get
            {
                lock (_lock)
                {
                    return _isReconnecting;
                }
            }
        }

        /// <summary>Current shot number (increments with each shot).</summary>
        public int ShotNumber => _shotNumber;

        /// <summary>Current host address.</summary>
        public string Host => _host;

        /// <summary>Current port.</summary>
        public int Port => _port;

        /// <summary>Whether the launch monitor is ready for shots.</summary>
        public bool LaunchMonitorIsReady => _launchMonitorIsReady;

        /// <summary>Whether a ball is detected on the tee.</summary>
        public bool LaunchMonitorBallDetected => _launchMonitorBallDetected;

        /// <summary>Fired when successfully connected to GSPro.</summary>
        public event Action OnConnected;

        /// <summary>Fired when disconnected from GSPro.</summary>
        public event Action OnDisconnected;

        /// <summary>Fired when an error occurs.</summary>
        public event Action<string> OnError;

        /// <summary>Fired when a shot is sent successfully.</summary>
        public event Action<int> OnShotSent;

        /// <summary>Fired when a heartbeat is sent.</summary>
        public event Action OnHeartbeatSent;

        /// <summary>
        /// Fired when GSPro confirms a shot was received.
        /// Parameters: response code (200/201), player info (may be null).
        /// </summary>
        public event Action<int, GSProPlayerInfo> OnShotConfirmed;

        /// <summary>
        /// Fired when GSPro reports an error for a shot.
        /// Parameter: error message.
        /// </summary>
        public event Action<string> OnShotFailed;

        /// <summary>
        /// Connect to GSPro.
        /// </summary>
        /// <param name="host">Host address.</param>
        /// <param name="port">Port number (default 921).</param>
        /// <returns>True if connection successful.</returns>
        public async Task<bool> ConnectAsync(string host, int port = DefaultPort)
        {
            if (string.IsNullOrEmpty(host))
            {
                OnError?.Invoke("Host cannot be empty");
                return false;
            }

            _host = host;
            _port = port;

            try
            {
                Disconnect();

                _client = new TcpClient();
                _client.NoDelay = true;
                _client.ReceiveTimeout = ConnectionTimeoutMs;
                _client.SendTimeout = ConnectionTimeoutMs;

                using var cts = new CancellationTokenSource(ConnectionTimeoutMs);
                await _client.ConnectAsync(host, port);

                if (!_client.Connected)
                {
                    OnError?.Invoke($"Failed to connect to {host}:{port}");
                    return false;
                }

                _stream = _client.GetStream();

                lock (_lock)
                {
                    _isConnected = true;
                    _isReconnecting = false;
                    _shotNumber = 0;
                }

                StartHeartbeat();

                Debug.Log($"GSProClient: Connected to {host}:{port}");
                OnConnected?.Invoke();
                return true;
            }
            catch (SocketException ex)
            {
                var message = ex.SocketErrorCode switch
                {
                    SocketError.ConnectionRefused => $"Connection refused - is GSPro running with Open Connect enabled on port {port}?",
                    SocketError.TimedOut => $"Connection timed out to {host}:{port}",
                    SocketError.HostNotFound => $"Host not found: {host}",
                    SocketError.NetworkUnreachable => "Network unreachable",
                    _ => $"Socket error: {ex.Message}"
                };
                OnError?.Invoke(message);
                return false;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Connection error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disconnect from GSPro.
        /// </summary>
        public void Disconnect()
        {
            Debug.Log("GSProClient: Disconnect() called");

            StopHeartbeat();
            StopReconnect();

            bool wasConnected;
            lock (_lock)
            {
                wasConnected = _isConnected;
                _isConnected = false;
            }

            Debug.Log($"GSProClient: wasConnected={wasConnected}");

            // Set linger to 0 BEFORE closing anything - sends RST instead of FIN
            try
            {
                if (_client?.Client != null && _client.Client.Connected)
                {
                    Debug.Log("GSProClient: Setting linger=0 for immediate RST...");
                    _client.Client.LingerState = new System.Net.Sockets.LingerOption(true, 0);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GSProClient: Error setting linger: {ex.Message}");
            }

            // Flush stream to ensure any pending data is sent
            try
            {
                if (_stream != null && _stream.CanWrite)
                {
                    Debug.Log("GSProClient: Flushing stream...");
                    _stream.Flush();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GSProClient: Error flushing stream: {ex.Message}");
            }

            // Close TcpClient (this also closes the stream and socket)
            if (_client != null)
            {
                try
                {
                    Debug.Log("GSProClient: Closing TcpClient...");
                    _client.Close();
                    Debug.Log("GSProClient: TcpClient closed");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"GSProClient: Error closing client: {ex.Message}");
                }
            }

            _stream = null;
            _client = null;

            // Notify listeners if we were connected
            if (wasConnected)
            {
                Debug.Log("GSProClient: Firing OnDisconnected event");
                OnDisconnected?.Invoke();
            }

            Debug.Log("GSProClient: Disconnect complete");
        }

        /// <summary>
        /// Send shot data to GSPro.
        /// </summary>
        /// <param name="shot">GC2 shot data to send.</param>
        public void SendShot(GC2ShotData shot)
        {
            if (shot == null)
            {
                OnError?.Invoke("Shot data is null");
                return;
            }

            _ = SendShotAsync(shot);
        }

        /// <summary>
        /// Send shot data to GSPro asynchronously.
        /// </summary>
        /// <param name="shot">GC2 shot data to send.</param>
        public async Task SendShotAsync(GC2ShotData shot)
        {
            if (!IsConnected)
            {
                OnError?.Invoke("Not connected to GSPro");
                return;
            }

            _shotNumber++;
            int currentShotNumber = _shotNumber;

            var message = CreateShotMessage(shot, currentShotNumber);

            // Debug: Log input shot data and output JSON
            Debug.Log($"GSProClient: Input shot - BackSpin: {shot.BackSpin:F0}, SideSpin: {shot.SideSpin:F0}, TotalSpin: {shot.TotalSpin:F0}");
            Debug.Log($"GSProClient: Output JSON: {message.ToJson()}");

            try
            {
                // Clear any stale data from receive buffer before sending
                ClearReceiveBuffer();

                await SendMessageAsync(message);
                Debug.Log($"GSProClient: Shot #{currentShotNumber} sent to GSPro");
                OnShotSent?.Invoke(currentShotNumber);

                // Await response from GSPro (shot messages get responses)
                var response = await ReadShotResponseAsync();

                if (response != null)
                {
                    if (response.IsSuccess)
                    {
                        Debug.Log($"GSProClient: Shot #{currentShotNumber} confirmed by GSPro (code: {response.Code})");
                        OnShotConfirmed?.Invoke(response.Code, response.Player);
                    }
                    else
                    {
                        var errorMsg = $"GSPro returned error {response.Code}: {response.Message}";
                        Debug.LogWarning($"GSProClient: {errorMsg}");
                        OnShotFailed?.Invoke(errorMsg);
                    }
                }
                else
                {
                    // No response received (timeout or parse error)
                    // This is not necessarily an error - some GSPro versions may not respond
                    Debug.Log($"GSProClient: No response received for shot #{currentShotNumber}");
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Failed to send shot: {ex.Message}");
                HandleDisconnection();
            }
        }

        /// <summary>
        /// Update device readiness state (from 0M messages).
        /// This is used in heartbeat messages to inform GSPro of device status.
        /// </summary>
        /// <param name="isReady">Whether the launch monitor is ready.</param>
        /// <param name="ballDetected">Whether a ball is detected on tee.</param>
        public void UpdateReadyState(bool isReady, bool ballDetected)
        {
            _launchMonitorIsReady = isReady;
            _launchMonitorBallDetected = ballDetected;
        }

        /// <summary>
        /// Create a GSPro message from GC2 shot data.
        /// </summary>
        /// <param name="shot">GC2 shot data.</param>
        /// <param name="shotNumber">Shot number.</param>
        /// <returns>GSPro message.</returns>
        public GSProMessage CreateShotMessage(GC2ShotData shot, int shotNumber)
        {
            var message = new GSProMessage
            {
                ShotNumber = shotNumber,
                BallData = new GSProBallData
                {
                    Speed = shot.BallSpeed,
                    SpinAxis = shot.SpinAxis,
                    TotalSpin = shot.TotalSpin,
                    BackSpin = shot.BackSpin,
                    SideSpin = shot.SideSpin,
                    HLA = shot.Direction,
                    VLA = shot.LaunchAngle
                },
                ShotDataOptions = new GSProShotOptions
                {
                    ContainsBallData = true,
                    ContainsClubData = shot.HasClubData,
                    LaunchMonitorIsReady = _launchMonitorIsReady,
                    LaunchMonitorBallDetected = _launchMonitorBallDetected,
                    IsHeartBeat = false
                }
            };

            if (shot.HasClubData)
            {
                message.ClubData = new GSProClubData
                {
                    Speed = shot.ClubSpeed,
                    AngleOfAttack = shot.AttackAngle,
                    FaceToTarget = shot.FaceToTarget,
                    Lie = shot.Lie,
                    Loft = shot.DynamicLoft,
                    Path = shot.Path
                };
            }

            return message;
        }

        /// <summary>
        /// Create a heartbeat message.
        /// </summary>
        /// <returns>GSPro heartbeat message.</returns>
        public GSProMessage CreateHeartbeatMessage()
        {
            return GSProMessage.CreateHeartbeat(_launchMonitorIsReady, _launchMonitorBallDetected);
        }

        /// <summary>
        /// Clear any pending data from the receive buffer.
        /// This prevents stale responses from causing parsing issues.
        /// </summary>
        internal void ClearReceiveBuffer()
        {
            if (_stream == null || !_stream.CanRead)
                return;

            try
            {
                // Read and discard any available bytes
                while (_stream.DataAvailable)
                {
                    _stream.Read(_receiveBuffer, 0, _receiveBuffer.Length);
                }
            }
            catch
            {
                // Ignore errors during buffer clearing
            }
        }

        /// <summary>
        /// Read and parse the response to a shot message from GSPro.
        /// </summary>
        /// <returns>Parsed response, or null if timeout or parse error.</returns>
        private async Task<GSProResponse> ReadShotResponseAsync()
        {
            if (_stream == null || !_stream.CanRead)
                return null;

            try
            {
                // Set read timeout for shot response
                _stream.ReadTimeout = ShotResponseTimeoutMs;

                // Use async read with cancellation
                using var cts = new CancellationTokenSource(ShotResponseTimeoutMs);
                var readTask = _stream.ReadAsync(_receiveBuffer, 0, _receiveBuffer.Length, cts.Token);

                int bytesRead = await readTask;

                if (bytesRead > 0)
                {
                    // Parse the first JSON object from the response
                    var response = GSProResponse.ParseFirstObject(_receiveBuffer, bytesRead);
                    return response;
                }
            }
            catch (OperationCanceledException)
            {
                // Timeout - not necessarily an error
                Debug.Log("GSProClient: Shot response timeout");
            }
            catch (System.IO.IOException)
            {
                // Read timeout or connection issue
                Debug.Log("GSProClient: Shot response read timeout");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GSProClient: Error reading shot response - {ex.Message}");
            }

            return null;
        }

        private void StartHeartbeat()
        {
            StopHeartbeat();
            _heartbeatCts = new CancellationTokenSource();
            _ = HeartbeatLoopAsync(_heartbeatCts.Token);
        }

        private void StopHeartbeat()
        {
            try
            {
                _heartbeatCts?.Cancel();
                _heartbeatCts?.Dispose();
            }
            catch
            {
                // Ignore
            }
            _heartbeatCts = null;
        }

        private async Task HeartbeatLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && IsConnected)
            {
                try
                {
                    await Task.Delay(HeartbeatIntervalMs, ct);

                    if (ct.IsCancellationRequested || !IsConnected)
                        break;

                    var message = CreateHeartbeatMessage();
                    await SendMessageAsync(message);
                    OnHeartbeatSent?.Invoke();

                    // Note: Do NOT wait for heartbeat response - GSPro doesn't send one
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"GSProClient: Heartbeat error - {ex.Message}");
                    HandleDisconnection();
                    break;
                }
            }
        }

        private async Task SendMessageAsync(GSProMessage message)
        {
            if (_stream == null || !_stream.CanWrite)
            {
                throw new InvalidOperationException("Stream is not writable");
            }

            var json = message.ToJson();
            var bytes = Encoding.UTF8.GetBytes(json);

            await _stream.WriteAsync(bytes, 0, bytes.Length);
            await _stream.FlushAsync();
        }

        private void HandleDisconnection()
        {
            bool wasConnected;
            lock (_lock)
            {
                wasConnected = _isConnected;
                _isConnected = false;
            }

            if (wasConnected)
            {
                Debug.Log("GSProClient: Disconnected from GSPro");
                OnDisconnected?.Invoke();
                StartReconnect();
            }
        }

        private void StartReconnect()
        {
            lock (_lock)
            {
                if (_isReconnecting)
                    return;
                _isReconnecting = true;
            }

            StopReconnect();
            _reconnectCts = new CancellationTokenSource();
            _ = ReconnectLoopAsync(_reconnectCts.Token);
        }

        private void StopReconnect()
        {
            try
            {
                _reconnectCts?.Cancel();
                _reconnectCts?.Dispose();
            }
            catch
            {
                // Ignore
            }
            _reconnectCts = null;
        }

        private async Task ReconnectLoopAsync(CancellationToken ct)
        {
            int[] delays = { 1000, 2000, 5000, 10000 };
            int attempt = 0;
            const int maxAttempts = 10;

            while (!ct.IsCancellationRequested && attempt < maxAttempts)
            {
                int delay = delays[Math.Min(attempt, delays.Length - 1)];
                Debug.Log($"GSProClient: Reconnecting in {delay}ms (attempt {attempt + 1}/{maxAttempts})...");

                try
                {
                    await Task.Delay(delay, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (ct.IsCancellationRequested)
                    break;

                bool success = await ConnectAsync(_host, _port);
                if (success)
                {
                    Debug.Log("GSProClient: Reconnected successfully");
                    return;
                }

                attempt++;
            }

            lock (_lock)
            {
                _isReconnecting = false;
            }

            if (!ct.IsCancellationRequested)
            {
                OnError?.Invoke($"Failed to reconnect after {maxAttempts} attempts");
            }
        }

        /// <summary>
        /// Dispose of the client and close connections.
        /// </summary>
        public void Dispose()
        {
            Disconnect();
        }
    }
}
