using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenRange.GC2
{
    /// <summary>
    /// Parser for GC2 USB protocol messages.
    /// The GC2 sends data as key=value pairs separated by newlines,
    /// with messages terminated by double newlines.
    /// </summary>
    public static class GC2Protocol
    {
        /// <summary>
        /// Parse a GC2 protocol message into structured shot data.
        /// </summary>
        /// <param name="message">Raw message string from GC2</param>
        /// <returns>Parsed shot data, or null if invalid</returns>
        public static GC2ShotData Parse(string message)
        {
            if (string.IsNullOrEmpty(message))
                return null;
            
            var values = new Dictionary<string, string>();
            
            // Parse key=value pairs
            var lines = message.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;
                
                var equalsIndex = trimmed.IndexOf('=');
                if (equalsIndex > 0)
                {
                    var key = trimmed.Substring(0, equalsIndex);
                    var value = trimmed.Substring(equalsIndex + 1);
                    values[key] = value;
                }
            }
            
            // Validate required field
            if (!values.ContainsKey("SPEED_MPH"))
            {
                Debug.LogWarning("GC2Protocol: Missing SPEED_MPH field");
                return null;
            }
            
            // Build shot data
            var shot = new GC2ShotData
            {
                ShotId = GetInt(values, "SHOT_ID", 0),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                
                // Ball data
                BallSpeed = GetFloat(values, "SPEED_MPH"),
                LaunchAngle = GetFloat(values, "ELEVATION_DEG"),
                Direction = GetFloat(values, "AZIMUTH_DEG"),
                TotalSpin = GetFloat(values, "SPIN_RPM"),
                BackSpin = GetFloat(values, "BACK_RPM"),
                SideSpin = GetFloat(values, "SIDE_RPM"),
                SpinAxis = GetFloat(values, "SPIN_AXIS_DEG"),
                
                // HMT club data
                HasClubData = GetInt(values, "HMT", 0) == 1
            };
            
            // Club data if HMT present
            if (shot.HasClubData)
            {
                shot.ClubSpeed = GetFloat(values, "CLUBSPEED_MPH");
                shot.Path = GetFloat(values, "HPATH_DEG");
                shot.AttackAngle = GetFloat(values, "VPATH_DEG");
                shot.FaceToTarget = GetFloat(values, "FACE_T_DEG");
                shot.DynamicLoft = GetFloat(values, "LOFT_DEG");
                shot.Lie = GetFloat(values, "LIE_DEG");
            }
            
            // Handle spin calculation fallback
            // If BackSpin/SideSpin are zero but TotalSpin is not, calculate from axis
            if (shot.BackSpin == 0 && shot.SideSpin == 0 && shot.TotalSpin > 0)
            {
                float axisRad = shot.SpinAxis * Mathf.Deg2Rad;
                shot.BackSpin = shot.TotalSpin * Mathf.Cos(axisRad);
                shot.SideSpin = shot.TotalSpin * Mathf.Sin(axisRad);
            }
            
            // Validate the shot
            if (!IsValidShot(shot))
            {
                Debug.LogWarning($"GC2Protocol: Invalid shot data - {shot}");
                return null;
            }
            
            return shot;
        }
        
        /// <summary>
        /// Validate shot data is within reasonable ranges.
        /// See docs/GC2_PROTOCOL.md for misread detection patterns.
        /// </summary>
        public static bool IsValidShot(GC2ShotData shot)
        {
            // Speed sanity check (per protocol: reject < 10 or > 250)
            if (shot.BallSpeed < 10 || shot.BallSpeed > 250)
                return false;

            // Launch angle sanity
            if (shot.LaunchAngle < -10 || shot.LaunchAngle > 60)
                return false;

            // Direction sanity
            if (Mathf.Abs(shot.Direction) > 45)
                return false;

            // Zero spin indicates a camera misread
            if (shot.TotalSpin == 0)
                return false;

            // 2222 pattern is a known GC2 error code for spin misread
            if (Mathf.Approximately(shot.BackSpin, 2222f))
                return false;

            // Zero spin with high speed is usually a misread
            if (shot.BallSpeed > 80 && shot.TotalSpin < 100)
                return false;

            // Spin axis must be reasonable
            if (Mathf.Abs(shot.SpinAxis) > 90)
                return false;

            return true;
        }
        
        /// <summary>
        /// Convert shot data to JSON string.
        /// </summary>
        public static string ToJson(GC2ShotData shot)
        {
            return JsonUtility.ToJson(shot);
        }
        
        /// <summary>
        /// Parse shot data from JSON string.
        /// </summary>
        public static GC2ShotData FromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<GC2ShotData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"GC2Protocol: JSON parse error - {ex.Message}");
                return null;
            }
        }
        
        #region Helper Methods
        
        private static float GetFloat(Dictionary<string, string> values, string key, float defaultValue = 0f)
        {
            if (values.TryGetValue(key, out var str))
            {
                if (float.TryParse(str, System.Globalization.NumberStyles.Float, 
                    System.Globalization.CultureInfo.InvariantCulture, out var value))
                {
                    return value;
                }
            }
            return defaultValue;
        }
        
        private static int GetInt(Dictionary<string, string> values, string key, int defaultValue = 0)
        {
            if (values.TryGetValue(key, out var str))
            {
                if (int.TryParse(str, out var value))
                {
                    return value;
                }
            }
            return defaultValue;
        }
        
        #endregion
    }
}
