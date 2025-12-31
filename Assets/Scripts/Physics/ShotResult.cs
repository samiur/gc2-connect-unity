using System.Collections.Generic;
using UnityEngine;

namespace OpenRange.Physics
{
    /// <summary>
    /// Result of a trajectory simulation.
    /// All distances in yards, heights in feet, times in seconds.
    /// </summary>
    [System.Serializable]
    public class ShotResult
    {
        /// <summary>Complete trajectory with position at each time step</summary>
        public List<TrajectoryPoint> Trajectory;
        
        /// <summary>Carry distance in yards (where ball first lands)</summary>
        public float CarryDistance;
        
        /// <summary>Total distance in yards (final resting position)</summary>
        public float TotalDistance;
        
        /// <summary>Roll distance in yards (Total - Carry)</summary>
        public float RollDistance;
        
        /// <summary>Offline distance in yards (+ = right)</summary>
        public float OfflineDistance;
        
        /// <summary>Maximum height in feet</summary>
        public float MaxHeight;
        
        /// <summary>Time to maximum height in seconds</summary>
        public float MaxHeightTime;
        
        /// <summary>Flight time to first landing in seconds</summary>
        public float FlightTime;
        
        /// <summary>Total time until ball stops in seconds</summary>
        public float TotalTime;
        
        /// <summary>Number of bounces</summary>
        public int BounceCount;
        
        /// <summary>Launch conditions used</summary>
        public LaunchData LaunchData;
        
        /// <summary>Environmental conditions used</summary>
        public Conditions Conditions;
        
        /// <summary>
        /// Get the apex (highest point) of the trajectory.
        /// </summary>
        public TrajectoryPoint GetApex()
        {
            TrajectoryPoint apex = null;
            float maxY = float.MinValue;
            
            foreach (var point in Trajectory)
            {
                if (point.Position.y > maxY)
                {
                    maxY = point.Position.y;
                    apex = point;
                }
            }
            
            return apex;
        }
        
        /// <summary>
        /// Get the landing point (first ground contact).
        /// </summary>
        public TrajectoryPoint GetLandingPoint()
        {
            for (int i = 1; i < Trajectory.Count; i++)
            {
                if (Trajectory[i].Phase != Phase.Flight)
                {
                    return Trajectory[i - 1];
                }
            }
            
            return Trajectory.Count > 0 ? Trajectory[Trajectory.Count - 1] : null;
        }
        
        public override string ToString()
        {
            return $"Carry: {CarryDistance:F1} yds, Total: {TotalDistance:F1} yds, " +
                   $"Apex: {MaxHeight:F1} ft, Flight: {FlightTime:F2}s";
        }
    }
    
    /// <summary>
    /// A single point in a trajectory.
    /// </summary>
    [System.Serializable]
    public class TrajectoryPoint
    {
        /// <summary>Time in seconds from launch</summary>
        public float Time;
        
        /// <summary>Position: X=forward (yds), Y=height (ft), Z=lateral (yds, + = right)</summary>
        public Vector3 Position;
        
        /// <summary>Current phase of flight</summary>
        public Phase Phase;
        
        public TrajectoryPoint() { }
        
        public TrajectoryPoint(float time, Vector3 position, Phase phase)
        {
            Time = time;
            Position = position;
            Phase = phase;
        }
        
        public override string ToString()
        {
            return $"t={Time:F2}s: ({Position.x:F1}, {Position.y:F1}, {Position.z:F1}) [{Phase}]";
        }
    }
    
    /// <summary>
    /// Phase of ball flight.
    /// </summary>
    public enum Phase
    {
        Flight,   // In the air
        Bounce,   // Just bounced
        Rolling,  // Rolling on ground
        Stopped   // At rest
    }
    
    /// <summary>
    /// Launch conditions for a shot.
    /// </summary>
    [System.Serializable]
    public class LaunchData
    {
        /// <summary>Ball speed in mph</summary>
        public float BallSpeed;
        
        /// <summary>Vertical launch angle in degrees</summary>
        public float VLA;
        
        /// <summary>Horizontal launch angle in degrees (+ = right)</summary>
        public float HLA;
        
        /// <summary>Backspin in rpm</summary>
        public float BackSpin;
        
        /// <summary>Sidespin in rpm (+ = fade/slice)</summary>
        public float SideSpin;
        
        public override string ToString()
        {
            return $"{BallSpeed:F1} mph, {VLA:F1}° VLA, {BackSpin:F0} rpm";
        }
    }
    
    /// <summary>
    /// Environmental conditions.
    /// </summary>
    [System.Serializable]
    public class Conditions
    {
        /// <summary>Temperature in Fahrenheit</summary>
        public float TempF = 70f;
        
        /// <summary>Elevation in feet</summary>
        public float ElevationFt = 0f;
        
        /// <summary>Relative humidity percentage</summary>
        public float HumidityPct = 50f;
        
        /// <summary>Wind speed in mph</summary>
        public float WindSpeedMph = 0f;
        
        /// <summary>Wind direction in degrees (0 = headwind)</summary>
        public float WindDirDeg = 0f;
        
        public override string ToString()
        {
            string windStr = WindSpeedMph > 0 ? $", Wind: {WindSpeedMph:F0} mph @ {WindDirDeg:F0}°" : "";
            return $"{TempF:F0}°F, {ElevationFt:F0}ft{windStr}";
        }
    }
}
