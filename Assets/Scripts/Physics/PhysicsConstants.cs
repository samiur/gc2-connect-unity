// ABOUTME: Physical constants for golf ball trajectory simulation.
// ABOUTME: Contains USGA ball specs, atmosphere constants, Cd/Cl tables from Nathan model.

using UnityEngine;

namespace OpenRange.Physics
{
    /// <summary>
    /// Physical constants for golf ball trajectory simulation.
    /// Based on USGA specifications and WSU aerodynamics research.
    /// </summary>
    public static class PhysicsConstants
    {
        #region Ball Properties (USGA Specifications)

        /// <summary>Ball mass in kg (1.620 oz maximum)</summary>
        public const float BallMassKg = 0.04593f;

        /// <summary>Ball diameter in meters (1.680 inches minimum)</summary>
        public const float BallDiameterM = 0.04267f;

        /// <summary>Ball radius in meters</summary>
        public const float BallRadiusM = 0.021335f;

        /// <summary>Ball cross-sectional area in m²</summary>
        public static readonly float BallAreaM2 = Mathf.PI * BallRadiusM * BallRadiusM;

        #endregion

        #region Standard Atmosphere

        /// <summary>Standard temperature in Fahrenheit</summary>
        public const float StdTempF = 70f;

        /// <summary>Standard elevation in feet (sea level)</summary>
        public const float StdElevationFt = 0f;

        /// <summary>Standard humidity percentage</summary>
        public const float StdHumidityPct = 50f;

        /// <summary>Standard pressure in inches of mercury</summary>
        public const float StdPressureInHg = 29.92f;

        /// <summary>Standard air density in kg/m³</summary>
        public const float StdAirDensity = 1.194f;

        #endregion

        #region Physical Constants

        /// <summary>Gravitational acceleration in m/s²</summary>
        public const float GravityMs2 = 9.81f;

        /// <summary>Spin decay rate per second</summary>
        public const float SpinDecayRate = 0.01f;

        /// <summary>Kinematic viscosity of air in m²/s</summary>
        public const float KinematicViscosity = 1.5e-5f;

        #endregion

        #region Simulation Parameters

        /// <summary>Time step for integration in seconds</summary>
        public const float Dt = 0.01f;

        /// <summary>Maximum simulation time in seconds</summary>
        public const float MaxTime = 30f;

        /// <summary>Maximum integration iterations</summary>
        public const int MaxIterations = 3000;

        /// <summary>Maximum trajectory points to store</summary>
        public const int MaxTrajectoryPoints = 600;

        /// <summary>Velocity threshold for "stopped" state in m/s</summary>
        public const float StoppedThreshold = 0.1f;

        /// <summary>Maximum number of bounces to simulate</summary>
        public const int MaxBounces = 5;

        #endregion

        #region Drag Coefficient Table (Nathan Model)

        /// <summary>
        /// Drag coefficient vs Reynolds number (×10⁵).
        /// Format: (Reynolds×10⁵, Cd)
        /// Based on Nathan model with golf ball dimple effects.
        /// </summary>
        public static readonly Vector2[] CdTable = new Vector2[]
        {
            new Vector2(0.375f, 0.400f),  // ~30 mph
            new Vector2(0.500f, 0.370f),  // ~40 mph
            new Vector2(0.625f, 0.340f),  // ~50 mph
            new Vector2(0.750f, 0.300f),  // ~60 mph
            new Vector2(0.875f, 0.260f),  // ~70 mph
            new Vector2(1.000f, 0.230f),  // ~80 mph
            new Vector2(1.250f, 0.215f),  // ~100 mph
            new Vector2(1.500f, 0.205f),  // ~120 mph
            new Vector2(2.000f, 0.195f),  // ~160 mph
        };

        /// <summary>Drag coefficient for supercritical flow (high speed)</summary>
        public const float CdSupercritical = 0.195f;

        #endregion

        #region Lift Coefficient Table (Nathan Model)

        /// <summary>
        /// Lift coefficient vs spin factor (S = ωr/V).
        /// Format: (SpinFactor, Cl)
        /// Based on Nathan model with diminishing returns at high spin factors.
        /// </summary>
        public static readonly Vector2[] ClTable = new Vector2[]
        {
            new Vector2(0.00f, 0.000f),
            new Vector2(0.05f, 0.090f),
            new Vector2(0.10f, 0.130f),
            new Vector2(0.15f, 0.155f),
            new Vector2(0.20f, 0.172f),
            new Vector2(0.25f, 0.183f),
            new Vector2(0.30f, 0.190f),
            new Vector2(0.35f, 0.195f),
            new Vector2(0.40f, 0.199f),
            new Vector2(0.45f, 0.202f),
            new Vector2(0.50f, 0.204f),
            new Vector2(0.55f, 0.205f),
        };

        /// <summary>Maximum lift coefficient</summary>
        public const float ClMax = 0.205f;

        #endregion
    }

    /// <summary>
    /// Ground surface properties for bounce and roll physics.
    /// </summary>
    [System.Serializable]
    public class GroundSurface
    {
        public string Name;

        /// <summary>Coefficient of Restitution (bounciness)</summary>
        public float COR;

        /// <summary>Rolling resistance coefficient</summary>
        public float RollingResistance;

        /// <summary>Friction coefficient</summary>
        public float Friction;

        // Pre-defined surfaces
        public static readonly GroundSurface Fairway = new GroundSurface
        {
            Name = "Fairway",
            COR = 0.60f,
            RollingResistance = 0.10f,
            Friction = 0.50f
        };

        public static readonly GroundSurface Rough = new GroundSurface
        {
            Name = "Rough",
            COR = 0.30f,
            RollingResistance = 0.30f,
            Friction = 0.70f
        };

        public static readonly GroundSurface Green = new GroundSurface
        {
            Name = "Green",
            COR = 0.40f,
            RollingResistance = 0.05f,
            Friction = 0.30f
        };
    }
}
