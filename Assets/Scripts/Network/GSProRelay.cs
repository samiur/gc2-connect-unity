// ABOUTME: Lightweight GSPro relay for Bridge Mode operation.
// ABOUTME: Forwards GC2 shot data to GSPro without physics simulation.

using System;
using System.Threading.Tasks;
using UnityEngine;
using OpenRange.GC2;

namespace OpenRange.Network
{
    /// <summary>
    /// Lightweight GSPro relay for Bridge Mode.
    /// Forwards GC2 shot data directly to GSPro without running physics simulation.
    /// Uses existing GSProClient infrastructure internally.
    /// </summary>
    public class GSProRelay : IDisposable
    {
        private GSProClient _client;
        private bool _isDisposed;

        private bool _launchMonitorIsReady = true;
        private bool _launchMonitorBallDetected;

        /// <summary>Whether connected to GSPro.</summary>
        public bool IsConnected => _client?.IsConnected ?? false;

        /// <summary>Current host address.</summary>
        public string Host => _client?.Host;

        /// <summary>Current port.</summary>
        public int Port => _client?.Port ?? 0;

        /// <summary>Number of shots relayed.</summary>
        public int ShotCount { get; private set; }

        /// <summary>Fired when a shot is successfully relayed.</summary>
        public event Action<GC2ShotData> OnShotRelayed;

        /// <summary>Fired when an error occurs.</summary>
        public event Action<string> OnError;

        /// <summary>Fired when connected to GSPro.</summary>
        public event Action OnConnected;

        /// <summary>Fired when disconnected from GSPro.</summary>
        public event Action OnDisconnected;

        /// <summary>
        /// Create a new GSProRelay instance.
        /// </summary>
        public GSProRelay()
        {
            _client = new GSProClient();
            _client.OnConnected += HandleConnected;
            _client.OnDisconnected += HandleDisconnected;
            _client.OnError += HandleError;
            _client.OnShotSent += HandleShotSent;
        }

        /// <summary>
        /// Connect to GSPro.
        /// </summary>
        /// <param name="host">GSPro host address.</param>
        /// <param name="port">GSPro port (default 921).</param>
        /// <returns>True if connected successfully.</returns>
        public async Task<bool> ConnectAsync(string host, int port = GSProClient.DefaultPort)
        {
            if (_isDisposed)
            {
                OnError?.Invoke("Relay has been disposed");
                return false;
            }

            return await _client.ConnectAsync(host, port);
        }

        /// <summary>
        /// Disconnect from GSPro.
        /// </summary>
        public void Disconnect()
        {
            _client?.Disconnect();
        }

        /// <summary>
        /// Relay shot data to GSPro without physics simulation.
        /// </summary>
        /// <param name="shot">GC2 shot data to relay.</param>
        public void RelayShot(GC2ShotData shot)
        {
            if (shot == null)
            {
                OnError?.Invoke("Shot data is null");
                return;
            }

            if (!IsConnected)
            {
                OnError?.Invoke("Not connected to GSPro");
                return;
            }

            // Send directly to GSPro - no physics simulation
            _client.SendShot(shot);
        }

        /// <summary>
        /// Update device status for GSPro heartbeat.
        /// </summary>
        /// <param name="isReady">Whether the launch monitor is ready.</param>
        /// <param name="ballDetected">Whether a ball is detected on tee.</param>
        public void UpdateDeviceStatus(bool isReady, bool ballDetected)
        {
            _launchMonitorIsReady = isReady;
            _launchMonitorBallDetected = ballDetected;
            _client?.UpdateReadyState(isReady, ballDetected);
        }

        /// <summary>
        /// Dispose of the relay and clean up resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (_client != null)
            {
                _client.OnConnected -= HandleConnected;
                _client.OnDisconnected -= HandleDisconnected;
                _client.OnError -= HandleError;
                _client.OnShotSent -= HandleShotSent;
                _client.Dispose();
                _client = null;
            }
        }

        private void HandleConnected()
        {
            Debug.Log("GSProRelay: Connected to GSPro");
            OnConnected?.Invoke();
        }

        private void HandleDisconnected()
        {
            Debug.Log("GSProRelay: Disconnected from GSPro");
            OnDisconnected?.Invoke();
        }

        private void HandleError(string error)
        {
            Debug.LogWarning($"GSProRelay: Error - {error}");
            OnError?.Invoke(error);
        }

        private void HandleShotSent(int shotNumber)
        {
            ShotCount++;
            Debug.Log($"GSProRelay: Shot #{shotNumber} relayed (total: {ShotCount})");
        }

        /// <summary>
        /// Notify that a shot was relayed.
        /// Called internally after successful send.
        /// </summary>
        /// <param name="shot">The shot that was relayed.</param>
        internal void NotifyShotRelayed(GC2ShotData shot)
        {
            OnShotRelayed?.Invoke(shot);
        }
    }
}
