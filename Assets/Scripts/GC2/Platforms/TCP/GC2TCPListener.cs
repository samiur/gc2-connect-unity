// ABOUTME: Simple TCP server for testing GC2 communication without hardware.
// ABOUTME: Accepts connections and provides methods to send simulated shot/status data.

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace OpenRange.GC2.Platforms.TCP
{
    /// <summary>
    /// Simple TCP server for testing GC2 communication.
    /// Listens for client connections and can send simulated shot/status messages.
    /// This is a standalone utility class (not a MonoBehaviour) for flexibility.
    /// </summary>
    public class GC2TCPListener : IDisposable
    {
        private const int DefaultPort = 8888;
        private const int BufferSize = 4096;

        private int _port;
        private TcpListener _listener;
        private TcpClient _connectedClient;
        private NetworkStream _stream;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning;
        private bool _isDisposed;

        private int _nextShotId = 1;

        /// <summary>
        /// Whether the server is currently running.
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Whether a client is currently connected.
        /// </summary>
        public bool HasClient => _connectedClient?.Connected ?? false;

        /// <summary>
        /// The port the server is listening on.
        /// </summary>
        public int Port => _port;

        /// <summary>
        /// Fired when a client connects.
        /// </summary>
        public event Action OnClientConnected;

        /// <summary>
        /// Fired when a client disconnects.
        /// </summary>
        public event Action OnClientDisconnected;

        /// <summary>
        /// Fired when data is received from the client.
        /// </summary>
        public event Action<string> OnDataReceived;

        /// <summary>
        /// Fired when an error occurs.
        /// </summary>
        public event Action<string> OnError;

        /// <summary>
        /// Create a new TCP listener on the specified port.
        /// </summary>
        /// <param name="port">Port to listen on (default 8888)</param>
        public GC2TCPListener(int port = DefaultPort)
        {
            _port = port > 0 ? port : DefaultPort;
        }

        /// <summary>
        /// Start listening for connections.
        /// </summary>
        /// <returns>True if server started successfully</returns>
        public bool Start()
        {
            if (_isRunning)
            {
                Debug.LogWarning("GC2TCPListener: Already running");
                return true;
            }

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();
                _isRunning = true;

                Debug.Log($"GC2TCPListener: Listening on port {_port}");

                // Start accepting connections
                Task.Run(AcceptClientsAsync, _cancellationTokenSource.Token);

                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Failed to start server: {ex.Message}");
                Debug.LogError($"GC2TCPListener: Start failed - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stop the server and close all connections.
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
                return;

            _isRunning = false;
            _cancellationTokenSource?.Cancel();

            CloseClient();

            try
            {
                _listener?.Stop();
            }
            catch { }

            _listener = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            Debug.Log("GC2TCPListener: Stopped");
        }

        /// <summary>
        /// Send a simulated shot to the connected client.
        /// </summary>
        public async Task<bool> SendShotAsync(GC2ShotData shot)
        {
            if (shot.ShotId == 0)
            {
                shot.ShotId = _nextShotId++;
            }

            string message = GC2TCPConnection.FormatShotMessage(shot);
            return await SendDataAsync(message);
        }

        /// <summary>
        /// Send a simulated shot with parameters.
        /// </summary>
        public async Task<bool> SendShotAsync(
            float ballSpeed,
            float launchAngle,
            float direction,
            float backSpin,
            float sideSpin)
        {
            var shot = new GC2ShotData
            {
                ShotId = _nextShotId++,
                BallSpeed = ballSpeed,
                LaunchAngle = launchAngle,
                Direction = direction,
                TotalSpin = Mathf.Sqrt(backSpin * backSpin + sideSpin * sideSpin),
                BackSpin = backSpin,
                SideSpin = sideSpin,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                HasClubData = false
            };

            return await SendShotAsync(shot);
        }

        /// <summary>
        /// Send a simulated device status to the connected client.
        /// </summary>
        public async Task<bool> SendDeviceStatusAsync(bool isReady, bool ballDetected, Vector3? ballPosition = null)
        {
            string message = GC2TCPConnection.FormatStatusMessage(isReady, ballDetected, ballPosition);
            return await SendDataAsync(message);
        }

        /// <summary>
        /// Send raw data to the connected client.
        /// </summary>
        public async Task<bool> SendDataAsync(string data)
        {
            if (!HasClient || _stream == null || !_stream.CanWrite)
            {
                Debug.LogWarning("GC2TCPListener: No client connected");
                return false;
            }

            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(data);
                await _stream.WriteAsync(bytes, 0, bytes.Length, _cancellationTokenSource?.Token ?? CancellationToken.None);
                await _stream.FlushAsync();
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Send failed: {ex.Message}");
                Debug.LogError($"GC2TCPListener: Send failed - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disconnect the current client.
        /// </summary>
        public void DisconnectClient()
        {
            CloseClient();
            OnClientDisconnected?.Invoke();
        }

        private async Task AcceptClientsAsync()
        {
            try
            {
                while (_isRunning && !_cancellationTokenSource.IsCancellationRequested)
                {
                    Debug.Log("GC2TCPListener: Waiting for client connection...");
                    var client = await _listener.AcceptTcpClientAsync();

                    // Only accept one client at a time
                    if (HasClient)
                    {
                        Debug.Log("GC2TCPListener: Rejecting additional client (already have one connected)");
                        client.Close();
                        continue;
                    }

                    _connectedClient = client;
                    _stream = client.GetStream();

                    var remoteEndpoint = (IPEndPoint)client.Client.RemoteEndPoint;
                    Debug.Log($"GC2TCPListener: Client connected from {remoteEndpoint.Address}:{remoteEndpoint.Port}");

                    OnClientConnected?.Invoke();

                    // Start reading from client
                    await ReadFromClientAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex) when (_isRunning)
            {
                OnError?.Invoke($"Accept error: {ex.Message}");
                Debug.LogError($"GC2TCPListener: Accept error - {ex.Message}");
            }
        }

        private async Task ReadFromClientAsync()
        {
            byte[] buffer = new byte[BufferSize];
            StringBuilder messageBuffer = new StringBuilder();

            try
            {
                while (_connectedClient?.Connected ?? false)
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
                        Debug.Log("GC2TCPListener: Client disconnected");
                        break;
                    }

                    string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    messageBuffer.Append(data);

                    // Process complete messages
                    string content = messageBuffer.ToString();
                    int terminatorIndex;
                    while ((terminatorIndex = content.IndexOf("\n\t", StringComparison.Ordinal)) >= 0)
                    {
                        string message = content.Substring(0, terminatorIndex);
                        content = content.Substring(terminatorIndex + 2);
                        messageBuffer.Clear();
                        messageBuffer.Append(content);

                        OnDataReceived?.Invoke(message);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"GC2TCPListener: Read error - {ex.Message}");
            }
            finally
            {
                CloseClient();
                OnClientDisconnected?.Invoke();
            }
        }

        private void CloseClient()
        {
            try
            {
                _stream?.Close();
            }
            catch { }

            try
            {
                _connectedClient?.Close();
            }
            catch { }

            _stream = null;
            _connectedClient = null;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            Stop();

            OnClientConnected = null;
            OnClientDisconnected = null;
            OnDataReceived = null;
            OnError = null;
        }
    }
}
