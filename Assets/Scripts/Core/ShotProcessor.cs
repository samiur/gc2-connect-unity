// ABOUTME: Service that validates incoming GC2 shots and runs physics simulation.
// ABOUTME: Bridges raw GC2 data with visualization/UI via events.

using System;
using UnityEngine;
using OpenRange.GC2;
using OpenRange.Physics;

namespace OpenRange.Core
{
    /// <summary>
    /// Processes incoming shots from GC2, validates them, runs physics simulation,
    /// and fires events for visualization and UI updates.
    /// </summary>
    public class ShotProcessor : MonoBehaviour
    {
        [Header("Environmental Conditions")]
        [SerializeField] private float _temperatureF = PhysicsConstants.StdTempF;
        [SerializeField] private float _elevationFt = PhysicsConstants.StdElevationFt;
        [SerializeField] private float _humidityPct = PhysicsConstants.StdHumidityPct;
        [SerializeField] private float _windSpeedMph = 0f;
        [SerializeField] private float _windDirectionDeg = 0f;

        [Header("Processing")]
        [SerializeField] private GroundSurfaceType _groundSurface = GroundSurfaceType.Fairway;
        [SerializeField] private bool _enableDebugLogging = false;

        private AppMode _currentMode = AppMode.OpenRange;
        private IGSProClient _gsproClient;

        /// <summary>Temperature in Fahrenheit for physics simulation.</summary>
        public float TemperatureF
        {
            get => _temperatureF;
            set => _temperatureF = Mathf.Clamp(value, 20f, 120f);
        }

        /// <summary>Elevation in feet above sea level.</summary>
        public float ElevationFt
        {
            get => _elevationFt;
            set => _elevationFt = Mathf.Clamp(value, -500f, 15000f);
        }

        /// <summary>Relative humidity percentage.</summary>
        public float HumidityPct
        {
            get => _humidityPct;
            set => _humidityPct = Mathf.Clamp(value, 0f, 100f);
        }

        /// <summary>Wind speed in mph.</summary>
        public float WindSpeedMph
        {
            get => _windSpeedMph;
            set => _windSpeedMph = Mathf.Clamp(value, 0f, 50f);
        }

        /// <summary>Wind direction in degrees (0 = headwind, 90 = from right).</summary>
        public float WindDirectionDeg
        {
            get => _windDirectionDeg;
            set => _windDirectionDeg = value % 360f;
        }

        /// <summary>Current application mode.</summary>
        public AppMode CurrentMode => _currentMode;

        /// <summary>
        /// Fired when a shot is successfully processed.
        /// </summary>
        public event Action<GC2ShotData, ShotResult> OnShotProcessed;

        /// <summary>
        /// Fired when a shot is rejected during validation.
        /// </summary>
        public event Action<GC2ShotData, string> OnShotRejected;

        /// <summary>
        /// Set the application mode (OpenRange or GSPro relay).
        /// </summary>
        /// <param name="mode">The mode to set.</param>
        public void SetMode(AppMode mode)
        {
            _currentMode = mode;

            if (_enableDebugLogging)
            {
                Debug.Log($"ShotProcessor: Mode set to {mode}");
            }
        }

        /// <summary>
        /// Set the GSPro client for relay mode.
        /// </summary>
        /// <param name="client">The GSPro client to use for relaying shots.</param>
        public void SetGSProClient(IGSProClient client)
        {
            _gsproClient = client;
        }

        /// <summary>
        /// Update environmental conditions used for physics simulation.
        /// </summary>
        /// <param name="tempF">Temperature in Fahrenheit.</param>
        /// <param name="elevationFt">Elevation in feet.</param>
        /// <param name="humidityPct">Humidity percentage.</param>
        /// <param name="windSpeedMph">Wind speed in mph.</param>
        /// <param name="windDirDeg">Wind direction in degrees.</param>
        public void SetEnvironmentalConditions(
            float tempF,
            float elevationFt,
            float humidityPct = PhysicsConstants.StdHumidityPct,
            float windSpeedMph = 0f,
            float windDirDeg = 0f)
        {
            TemperatureF = tempF;
            ElevationFt = elevationFt;
            HumidityPct = humidityPct;
            WindSpeedMph = windSpeedMph;
            WindDirectionDeg = windDirDeg;

            if (_enableDebugLogging)
            {
                Debug.Log($"ShotProcessor: Conditions updated - {tempF}F, {elevationFt}ft, " +
                          $"{humidityPct}% humidity, wind {windSpeedMph}mph @ {windDirDeg}deg");
            }
        }

        /// <summary>
        /// Process an incoming shot from the GC2.
        /// </summary>
        /// <param name="shot">Raw shot data from GC2.</param>
        public void ProcessShot(GC2ShotData shot)
        {
            if (shot == null)
            {
                OnShotRejected?.Invoke(null, "Shot data is null");
                return;
            }

            // Validate the shot
            string validationError = ValidateShot(shot);
            if (!string.IsNullOrEmpty(validationError))
            {
                if (_enableDebugLogging)
                {
                    Debug.LogWarning($"ShotProcessor: Shot rejected - {validationError}");
                }

                OnShotRejected?.Invoke(shot, validationError);
                return;
            }

            // Run physics simulation
            ShotResult result = SimulateShot(shot);

            if (result == null)
            {
                OnShotRejected?.Invoke(shot, "Physics simulation failed");
                return;
            }

            if (_enableDebugLogging)
            {
                Debug.Log($"ShotProcessor: Shot processed - Carry: {result.CarryDistance:F1} yds, " +
                          $"Total: {result.TotalDistance:F1} yds, Apex: {result.MaxHeight:F1} ft");
            }

            // Handle GSPro relay mode
            if (_currentMode == AppMode.GSPro)
            {
                RelayToGSPro(shot, result);
            }

            // Fire event for visualization and UI
            OnShotProcessed?.Invoke(shot, result);
        }

        /// <summary>
        /// Validate shot data is within acceptable ranges.
        /// </summary>
        /// <param name="shot">Shot to validate.</param>
        /// <returns>Error message if invalid, null if valid.</returns>
        private string ValidateShot(GC2ShotData shot)
        {
            // Use the existing protocol validation as the first check
            if (!GC2Protocol.IsValidShot(shot))
            {
                // Provide more specific error messages
                if (shot.BallSpeed < 10 || shot.BallSpeed > 220)
                {
                    return $"Ball speed out of range: {shot.BallSpeed:F1} mph (expected 10-220)";
                }

                if (shot.LaunchAngle < -10 || shot.LaunchAngle > 60)
                {
                    return $"Launch angle out of range: {shot.LaunchAngle:F1} deg (expected -10 to 60)";
                }

                if (Mathf.Abs(shot.Direction) > 45)
                {
                    return $"Direction out of range: {shot.Direction:F1} deg (expected -45 to 45)";
                }

                if (shot.BallSpeed > 80 && shot.TotalSpin < 100)
                {
                    return $"Spin too low for speed: {shot.TotalSpin:F0} rpm at {shot.BallSpeed:F1} mph";
                }

                if (Mathf.Abs(shot.SpinAxis) > 90)
                {
                    return $"Spin axis out of range: {shot.SpinAxis:F1} deg (expected -90 to 90)";
                }

                return "Shot failed validation";
            }

            return null;
        }

        /// <summary>
        /// Run physics simulation for the shot.
        /// </summary>
        /// <param name="shot">Validated shot data.</param>
        /// <returns>Shot result with trajectory, or null on failure.</returns>
        private ShotResult SimulateShot(GC2ShotData shot)
        {
            try
            {
                GroundSurface surface = GetGroundSurface(_groundSurface);

                var simulator = new TrajectorySimulator(
                    tempF: _temperatureF,
                    elevationFt: _elevationFt,
                    humidityPct: _humidityPct,
                    windSpeedMph: _windSpeedMph,
                    windDirDeg: _windDirectionDeg,
                    surface: surface
                );

                ShotResult result = simulator.Simulate(
                    ballSpeedMph: shot.BallSpeed,
                    vlaDeg: shot.LaunchAngle,
                    hlaDeg: shot.Direction,
                    backspinRpm: shot.BackSpin,
                    sidespinRpm: shot.SideSpin
                );

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"ShotProcessor: Simulation error - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Relay shot data to GSPro client.
        /// </summary>
        /// <param name="shot">Original shot data.</param>
        /// <param name="result">Physics simulation result.</param>
        private void RelayToGSPro(GC2ShotData shot, ShotResult result)
        {
            if (_gsproClient == null)
            {
                if (_enableDebugLogging)
                {
                    Debug.LogWarning("ShotProcessor: GSPro client not set, cannot relay shot");
                }
                return;
            }

            if (!_gsproClient.IsConnected)
            {
                if (_enableDebugLogging)
                {
                    Debug.LogWarning("ShotProcessor: GSPro client not connected, cannot relay shot");
                }
                return;
            }

            try
            {
                _gsproClient.SendShot(shot);

                if (_enableDebugLogging)
                {
                    Debug.Log("ShotProcessor: Shot relayed to GSPro");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"ShotProcessor: Failed to relay shot to GSPro - {ex.Message}");
            }
        }

        /// <summary>
        /// Convert surface type enum to GroundSurface object.
        /// </summary>
        private GroundSurface GetGroundSurface(GroundSurfaceType type)
        {
            return type switch
            {
                GroundSurfaceType.Fairway => GroundSurface.Fairway,
                GroundSurfaceType.Rough => GroundSurface.Rough,
                GroundSurfaceType.Green => GroundSurface.Green,
                _ => GroundSurface.Fairway
            };
        }
    }

    /// <summary>
    /// Ground surface types for the inspector dropdown.
    /// </summary>
    public enum GroundSurfaceType
    {
        Fairway,
        Rough,
        Green
    }

    /// <summary>
    /// Interface for GSPro client (for dependency injection and testing).
    /// Will be implemented in the Network layer.
    /// </summary>
    public interface IGSProClient
    {
        /// <summary>Whether the client is connected to GSPro.</summary>
        bool IsConnected { get; }

        /// <summary>Send shot data to GSPro.</summary>
        void SendShot(GC2ShotData shot);
    }
}
