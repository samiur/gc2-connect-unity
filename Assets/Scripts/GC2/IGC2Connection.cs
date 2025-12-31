using System;
using System.Threading.Tasks;

namespace OpenRange.GC2
{
    /// <summary>
    /// Platform-agnostic interface for GC2 launch monitor communication.
    /// Implementations exist for macOS (libusb), iPad (DriverKit), and Android (USB Host).
    /// </summary>
    public interface IGC2Connection
    {
        /// <summary>
        /// Whether currently connected to a GC2 device.
        /// </summary>
        bool IsConnected { get; }
        
        /// <summary>
        /// Information about the connected device, or null if not connected.
        /// </summary>
        GC2DeviceInfo DeviceInfo { get; }
        
        /// <summary>
        /// Fired when shot data is received from the GC2.
        /// </summary>
        event Action<GC2ShotData> OnShotReceived;
        
        /// <summary>
        /// Fired when connection state changes.
        /// </summary>
        event Action<bool> OnConnectionChanged;
        
        /// <summary>
        /// Fired when an error occurs.
        /// </summary>
        event Action<string> OnError;
        
        /// <summary>
        /// Check if a GC2 device is available for connection.
        /// Does not require the device to be connected.
        /// </summary>
        bool IsDeviceAvailable();
        
        /// <summary>
        /// Attempt to connect to the GC2 device.
        /// </summary>
        /// <returns>True if connection successful</returns>
        Task<bool> ConnectAsync();
        
        /// <summary>
        /// Disconnect from the GC2 device.
        /// </summary>
        void Disconnect();
    }
}
