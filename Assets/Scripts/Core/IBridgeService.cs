// ABOUTME: Interface for platform-specific background service implementations.
// ABOUTME: Abstracts Android foreground service, iOS workarounds, and mock for testing.

using System;
using System.Threading.Tasks;
using OpenRange.GC2;

namespace OpenRange.Core
{
    /// <summary>
    /// Interface for platform-specific background service implementations.
    /// Implementations exist for Android (foreground service), iOS (PiP workaround),
    /// and mock (for testing/editor).
    /// </summary>
    public interface IBridgeService
    {
        /// <summary>
        /// Whether the service is currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Fired when the service successfully starts.
        /// </summary>
        event Action OnStarted;

        /// <summary>
        /// Fired when the service stops.
        /// </summary>
        event Action OnStopped;

        /// <summary>
        /// Fired when a shot is successfully relayed to GSPro.
        /// </summary>
        event Action<GC2ShotData> OnShotRelayed;

        /// <summary>
        /// Fired when an error occurs in the service.
        /// </summary>
        event Action<string> OnError;

        /// <summary>
        /// Start the background service.
        /// </summary>
        /// <returns>True if service started successfully.</returns>
        Task<bool> StartAsync();

        /// <summary>
        /// Stop the background service.
        /// </summary>
        void Stop();
    }

    /// <summary>
    /// Bridge mode state.
    /// </summary>
    public enum BridgeModeState
    {
        /// <summary>Bridge mode is disabled (normal app mode).</summary>
        Disabled,

        /// <summary>Bridge mode is enabled and app is in foreground.</summary>
        Active,

        /// <summary>Bridge mode is enabled and app is backgrounded.</summary>
        Backgrounded
    }

    /// <summary>
    /// Statistics for bridge mode operation.
    /// </summary>
    public struct BridgeModeStatistics
    {
        /// <summary>Number of shots successfully relayed to GSPro.</summary>
        public int ShotsRelayed;

        /// <summary>Number of shots rejected (validation failed or GSPro error).</summary>
        public int ShotsRejected;

        /// <summary>Time when bridge mode was enabled.</summary>
        public DateTime? StartTime;

        /// <summary>Total duration bridge mode has been active.</summary>
        public TimeSpan TotalDuration => StartTime.HasValue
            ? DateTime.UtcNow - StartTime.Value
            : TimeSpan.Zero;

        /// <summary>
        /// Reset all statistics.
        /// </summary>
        public void Reset()
        {
            ShotsRelayed = 0;
            ShotsRejected = 0;
            StartTime = null;
        }
    }
}
