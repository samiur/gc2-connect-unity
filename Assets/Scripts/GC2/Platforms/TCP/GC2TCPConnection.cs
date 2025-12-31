// ABOUTME: Stub TCP connection for GC2 communication - used in Editor for testing.
// ABOUTME: Full implementation will come in Prompt 18.

using System;
using System.Threading.Tasks;
using UnityEngine;

namespace OpenRange.GC2.Platforms.TCP
{
    /// <summary>
    /// TCP-based GC2 connection for Editor testing and relay mode.
    /// Stub implementation - to be completed in Prompt 18.
    /// </summary>
    public class GC2TCPConnection : MonoBehaviour, IGC2Connection
    {
        private string _address = "127.0.0.1";
        private int _port = 8888;
        private bool _isConnected;

        public bool IsConnected => _isConnected;
        public GC2DeviceInfo DeviceInfo => null;

        public event Action<GC2ShotData> OnShotReceived;
        public event Action<bool> OnConnectionChanged;
        public event Action<string> OnError;

        /// <summary>
        /// Set TCP connection parameters.
        /// </summary>
        public void SetConnectionParams(string address, int port)
        {
            _address = address;
            _port = port;
        }

        public bool IsDeviceAvailable()
        {
            // Stub - always return false until implemented
            return false;
        }

        public Task<bool> ConnectAsync()
        {
            // Stub - return false until implemented
            Debug.LogWarning("GC2TCPConnection: Stub implementation - connection not available");
            return Task.FromResult(false);
        }

        public void Disconnect()
        {
            if (_isConnected)
            {
                _isConnected = false;
                OnConnectionChanged?.Invoke(false);
            }
        }

        // Suppress unused event warnings for stub
        private void OnDestroy()
        {
            OnShotReceived = null;
            OnConnectionChanged = null;
            OnError = null;
        }
    }
}
