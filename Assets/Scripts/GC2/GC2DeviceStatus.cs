// ABOUTME: Data structure representing GC2 device status from 0M messages.
// ABOUTME: Contains readiness state, ball detection, and ball position for GSPro integration.

using System;
using UnityEngine;

namespace OpenRange.GC2
{
    /// <summary>
    /// Device status received from GC2 0M messages.
    /// Used for GSPro integration (LaunchMonitorIsReady/LaunchMonitorBallDetected).
    /// </summary>
    [Serializable]
    public struct GC2DeviceStatus : IEquatable<GC2DeviceStatus>
    {
        /// <summary>
        /// Whether the device is ready (green light).
        /// True when FLAGS == 7 in 0M message.
        /// </summary>
        public bool IsReady;

        /// <summary>
        /// Whether a ball is detected in the tee area.
        /// True when BALLS > 0 in 0M message.
        /// </summary>
        public bool BallDetected;

        /// <summary>
        /// Ball position if detected (from BALL1 field).
        /// Null if no ball is detected.
        /// Coordinates are in mm from the GC2 sensor origin.
        /// </summary>
        public Vector3? BallPosition;

        /// <summary>
        /// The raw FLAGS value from the 0M message.
        /// FLAGS == 7 means fully ready, FLAGS == 1 means not ready.
        /// </summary>
        public int RawFlags;

        /// <summary>
        /// The number of balls detected (from BALLS field).
        /// Typically 0 or 1.
        /// </summary>
        public int BallCount;

        /// <summary>
        /// Flags value indicating the device is fully ready (green light).
        /// </summary>
        public const int FlagsReady = 7;

        /// <summary>
        /// Flags value indicating the device is not ready (red light).
        /// </summary>
        public const int FlagsNotReady = 1;

        /// <summary>
        /// Create a device status from parsed 0M message fields.
        /// </summary>
        /// <param name="flags">FLAGS field value</param>
        /// <param name="balls">BALLS field value</param>
        /// <param name="ballPosition">BALL1 position if present</param>
        public GC2DeviceStatus(int flags, int balls, Vector3? ballPosition = null)
        {
            RawFlags = flags;
            BallCount = balls;
            IsReady = flags == FlagsReady;
            BallDetected = balls > 0;
            BallPosition = ballPosition;
        }

        /// <summary>
        /// Default status when device is disconnected or status unknown.
        /// </summary>
        public static GC2DeviceStatus Unknown => new GC2DeviceStatus(0, 0, null);

        public bool Equals(GC2DeviceStatus other)
        {
            return IsReady == other.IsReady &&
                   BallDetected == other.BallDetected &&
                   RawFlags == other.RawFlags &&
                   BallCount == other.BallCount &&
                   Nullable.Equals(BallPosition, other.BallPosition);
        }

        public override bool Equals(object obj)
        {
            return obj is GC2DeviceStatus other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + IsReady.GetHashCode();
                hash = hash * 31 + BallDetected.GetHashCode();
                hash = hash * 31 + RawFlags.GetHashCode();
                hash = hash * 31 + BallCount.GetHashCode();
                hash = hash * 31 + (BallPosition?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public static bool operator ==(GC2DeviceStatus left, GC2DeviceStatus right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GC2DeviceStatus left, GC2DeviceStatus right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            string readyState = IsReady ? "Ready" : "NotReady";
            string ballState = BallDetected ? $"Ball@{BallPosition}" : "NoBall";
            return $"GC2Status: {readyState}, {ballState} (FLAGS={RawFlags}, BALLS={BallCount})";
        }
    }
}
