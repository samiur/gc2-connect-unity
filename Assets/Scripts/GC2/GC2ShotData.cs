using System;

namespace OpenRange.GC2
{
    /// <summary>
    /// Shot data received from the GC2 launch monitor.
    /// Contains ball data (always present) and club data (HMT only).
    /// </summary>
    [Serializable]
    public class GC2ShotData
    {
        // Metadata
        public int ShotId;
        public long Timestamp;
        
        // Ball data (always present)
        /// <summary>Ball speed in mph</summary>
        public float BallSpeed;
        
        /// <summary>Vertical launch angle in degrees (up from ground)</summary>
        public float LaunchAngle;
        
        /// <summary>Horizontal launch angle in degrees (+ = right)</summary>
        public float Direction;
        
        /// <summary>Total spin rate in rpm</summary>
        public float TotalSpin;
        
        /// <summary>Backspin component in rpm</summary>
        public float BackSpin;
        
        /// <summary>Sidespin component in rpm (+ = fade/slice)</summary>
        public float SideSpin;
        
        /// <summary>Spin axis tilt in degrees (+ = fade/slice)</summary>
        public float SpinAxis;
        
        // Club data (HMT only)
        /// <summary>Whether club data is present (HMT required)</summary>
        public bool HasClubData;
        
        /// <summary>Club head speed in mph</summary>
        public float ClubSpeed;
        
        /// <summary>Club path in degrees (+ = in-to-out)</summary>
        public float Path;
        
        /// <summary>Attack angle in degrees (+ = up)</summary>
        public float AttackAngle;
        
        /// <summary>Face to target angle in degrees (+ = open)</summary>
        public float FaceToTarget;
        
        /// <summary>Dynamic loft at impact in degrees</summary>
        public float DynamicLoft;
        
        /// <summary>Lie angle in degrees</summary>
        public float Lie;
        
        /// <summary>
        /// Create a copy of this shot data.
        /// </summary>
        public GC2ShotData Clone()
        {
            return (GC2ShotData)MemberwiseClone();
        }
        
        public override string ToString()
        {
            return $"Shot #{ShotId}: {BallSpeed:F1} mph, {LaunchAngle:F1}°, {TotalSpin:F0} rpm";
        }
    }
    
    /// <summary>
    /// Information about a connected GC2 device.
    /// </summary>
    [Serializable]
    public class GC2DeviceInfo
    {
        public string SerialNumber;
        public string FirmwareVersion;
        public bool HasHMT;
        
        public override string ToString()
        {
            return $"GC2 {SerialNumber} (HMT: {HasHMT})";
        }
    }
}
