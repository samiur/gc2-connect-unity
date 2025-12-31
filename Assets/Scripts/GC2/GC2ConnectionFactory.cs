using UnityEngine;

namespace OpenRange.GC2
{
    /// <summary>
    /// Factory for creating platform-specific GC2 connection implementations.
    /// </summary>
    public static class GC2ConnectionFactory
    {
        /// <summary>
        /// Create a GC2 connection appropriate for the current platform.
        /// </summary>
        /// <param name="host">GameObject to attach the connection component to</param>
        /// <returns>Platform-specific IGC2Connection implementation</returns>
        public static IGC2Connection Create(GameObject host)
        {
#if UNITY_EDITOR
            // Use TCP in editor for testing with GC2 Connect Desktop
            Debug.Log("GC2ConnectionFactory: Using TCP connection (Editor)");
            return host.AddComponent<Platforms.TCP.GC2TCPConnection>();
            
#elif UNITY_STANDALONE_OSX
            Debug.Log("GC2ConnectionFactory: Using macOS USB connection");
            return host.AddComponent<Platforms.MacOS.GC2MacConnection>();
            
#elif UNITY_IOS
            Debug.Log("GC2ConnectionFactory: Using iPad DriverKit connection");
            return host.AddComponent<Platforms.iOS.GC2iPadConnection>();
            
#elif UNITY_ANDROID
            Debug.Log("GC2ConnectionFactory: Using Android USB Host connection");
            return host.AddComponent<Platforms.Android.GC2AndroidConnection>();
            
#else
            // Fallback to TCP
            Debug.Log("GC2ConnectionFactory: Using TCP connection (Fallback)");
            return host.AddComponent<Platforms.TCP.GC2TCPConnection>();
#endif
        }
        
        /// <summary>
        /// Create a TCP-based GC2 connection for testing or relay mode.
        /// </summary>
        /// <param name="host">GameObject to attach the connection component to</param>
        /// <param name="address">Host address (default: 127.0.0.1)</param>
        /// <param name="port">Port number (default: 8888)</param>
        /// <returns>TCP-based IGC2Connection</returns>
        public static IGC2Connection CreateTCP(GameObject host, string address = "127.0.0.1", int port = 8888)
        {
            var connection = host.AddComponent<Platforms.TCP.GC2TCPConnection>();
            connection.SetConnectionParams(address, port);
            return connection;
        }
    }
}
