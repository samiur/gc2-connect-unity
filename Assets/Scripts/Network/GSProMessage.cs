// ABOUTME: GSPro Open Connect API v1 message classes for JSON serialization.
// ABOUTME: Defines shot data, club data, and options per GSPro protocol spec.

using System;
using Newtonsoft.Json;

namespace OpenRange.Network
{
    /// <summary>
    /// Root message for GSPro Open Connect API v1.
    /// Contains ball data, optional club data, and shot options.
    /// </summary>
    [Serializable]
    public class GSProMessage
    {
        /// <summary>Identifier for the launch monitor.</summary>
        [JsonProperty("DeviceID")]
        public string DeviceID { get; set; } = "GC2 Connect Unity";

        /// <summary>Units for distance values ("Yards" or "Meters").</summary>
        [JsonProperty("Units")]
        public string Units { get; set; } = "Yards";

        /// <summary>Sequential shot number (1-based, 0 for heartbeat).</summary>
        [JsonProperty("ShotNumber")]
        public int ShotNumber { get; set; }

        /// <summary>API version (always "1" for v1 API).</summary>
        [JsonProperty("APIversion")]
        public string APIversion { get; set; } = "1";

        /// <summary>Ball flight data.</summary>
        [JsonProperty("BallData")]
        public GSProBallData BallData { get; set; } = new GSProBallData();

        /// <summary>Club data (HMT only, null if not present).</summary>
        [JsonProperty("ClubData", NullValueHandling = NullValueHandling.Ignore)]
        public GSProClubData ClubData { get; set; }

        /// <summary>Metadata about the shot/message.</summary>
        [JsonProperty("ShotDataOptions")]
        public GSProShotOptions ShotDataOptions { get; set; } = new GSProShotOptions();

        /// <summary>
        /// Create a heartbeat message with device status.
        /// </summary>
        /// <param name="isReady">Whether the launch monitor is ready.</param>
        /// <param name="ballDetected">Whether a ball is detected on tee.</param>
        /// <returns>A heartbeat message.</returns>
        public static GSProMessage CreateHeartbeat(bool isReady = true, bool ballDetected = false)
        {
            return new GSProMessage
            {
                ShotNumber = 0,
                BallData = new GSProBallData(),
                ShotDataOptions = new GSProShotOptions
                {
                    ContainsBallData = false,
                    ContainsClubData = false,
                    LaunchMonitorIsReady = isReady,
                    LaunchMonitorBallDetected = ballDetected,
                    IsHeartBeat = true
                }
            };
        }

        /// <summary>
        /// Serialize to JSON string with newline terminator.
        /// </summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.None);
        }
    }

    /// <summary>
    /// Ball flight data for GSPro.
    /// </summary>
    [Serializable]
    public class GSProBallData
    {
        /// <summary>Ball speed in mph.</summary>
        [JsonProperty("Speed")]
        public float Speed { get; set; }

        /// <summary>Spin axis tilt in degrees (+ = fade/slice).</summary>
        [JsonProperty("SpinAxis")]
        public float SpinAxis { get; set; }

        /// <summary>Total spin rate in rpm.</summary>
        [JsonProperty("TotalSpin")]
        public float TotalSpin { get; set; }

        /// <summary>Backspin component in rpm.</summary>
        [JsonProperty("BackSpin")]
        public float BackSpin { get; set; }

        /// <summary>Sidespin component in rpm (+ = fade/slice).</summary>
        [JsonProperty("SideSpin")]
        public float SideSpin { get; set; }

        /// <summary>Horizontal launch angle in degrees (+ = right).</summary>
        [JsonProperty("HLA")]
        public float HLA { get; set; }

        /// <summary>Vertical launch angle in degrees (up from ground).</summary>
        [JsonProperty("VLA")]
        public float VLA { get; set; }
    }

    /// <summary>
    /// Club data for GSPro (HMT only).
    /// </summary>
    [Serializable]
    public class GSProClubData
    {
        /// <summary>Club head speed in mph.</summary>
        [JsonProperty("Speed")]
        public float Speed { get; set; }

        /// <summary>Attack angle in degrees (+ = up).</summary>
        [JsonProperty("AngleOfAttack")]
        public float AngleOfAttack { get; set; }

        /// <summary>Face angle at impact in degrees (+ = open).</summary>
        [JsonProperty("FaceToTarget")]
        public float FaceToTarget { get; set; }

        /// <summary>Lie angle in degrees.</summary>
        [JsonProperty("Lie")]
        public float Lie { get; set; }

        /// <summary>Dynamic loft at impact in degrees.</summary>
        [JsonProperty("Loft")]
        public float Loft { get; set; }

        /// <summary>Club path in degrees (+ = in-to-out).</summary>
        [JsonProperty("Path")]
        public float Path { get; set; }

        /// <summary>Speed at moment of impact in mph.</summary>
        [JsonProperty("SpeedAtImpact")]
        public float SpeedAtImpact { get; set; }

        /// <summary>Impact point vertical offset in inches.</summary>
        [JsonProperty("VerticalFaceImpact")]
        public float VerticalFaceImpact { get; set; }

        /// <summary>Impact point horizontal offset in inches.</summary>
        [JsonProperty("HorizontalFaceImpact")]
        public float HorizontalFaceImpact { get; set; }

        /// <summary>Face closure rate in deg/sec.</summary>
        [JsonProperty("ClosureRate")]
        public float ClosureRate { get; set; }
    }

    /// <summary>
    /// Shot data options/metadata for GSPro.
    /// </summary>
    [Serializable]
    public class GSProShotOptions
    {
        /// <summary>Whether BallData contains valid data.</summary>
        [JsonProperty("ContainsBallData")]
        public bool ContainsBallData { get; set; }

        /// <summary>Whether ClubData contains valid data.</summary>
        [JsonProperty("ContainsClubData")]
        public bool ContainsClubData { get; set; }

        /// <summary>Whether the launch monitor is ready for shots.</summary>
        [JsonProperty("LaunchMonitorIsReady")]
        public bool LaunchMonitorIsReady { get; set; } = true;

        /// <summary>Whether a ball is detected on the tee.</summary>
        [JsonProperty("LaunchMonitorBallDetected")]
        public bool LaunchMonitorBallDetected { get; set; }

        /// <summary>Whether this is a heartbeat message (no shot data).</summary>
        [JsonProperty("IsHeartBeat")]
        public bool IsHeartBeat { get; set; }
    }
}
