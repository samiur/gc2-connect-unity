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

        #region Ground Physics Constants (Penner Model)

        /// <summary>Base COR coefficient in Penner's formula: e = A - B*v + C*v²</summary>
        public const float PennerCOR_A = 0.510f;

        /// <summary>Linear velocity coefficient in Penner's formula</summary>
        public const float PennerCOR_B = 0.0375f;

        /// <summary>Quadratic velocity coefficient in Penner's formula</summary>
        public const float PennerCOR_C = 0.000903f;

        /// <summary>Minimum COR for stability (very high impact velocity)</summary>
        public const float MinCOR = 0.15f;

        /// <summary>Maximum COR for stability (very low impact velocity)</summary>
        public const float MaxCOR = 0.65f;

        /// <summary>Backspin RPM threshold for spin reversal to be possible</summary>
        public const float SpinReversalThreshold = 7000f;

        /// <summary>Landing angle threshold (degrees) for enhanced spin effects</summary>
        public const float LandingAngleThresholdDeg = 40f;

        /// <summary>Denominator for spin braking factor: brake = 1 - min(0.8, backspin/SpinBrakingDenominator)</summary>
        public const float SpinBrakingDenominator = 12000f;

        /// <summary>Maximum spin braking effect (0.8 = can reduce to 20% of original)</summary>
        public const float MaxSpinBraking = 0.8f;

        /// <summary>Minimum horizontal velocity (m/s) for spin reversal check</summary>
        public const float SpinReversalVelocityThreshold = 5f;

        #endregion

        #region Drag Coefficient Table (Nathan Model)

        /// <summary>
        /// Drag coefficient vs Reynolds number (×10⁵).
        /// Format: (Reynolds×10⁵, Cd)
        /// From Nathan's TrajectoryCalculatorGolf-v2.xlsx Cd-Cl sheet.
        /// Shows classic "drag crisis" where dimples cause early turbulent transition.
        /// </summary>
        public static readonly Vector2[] CdTable = new Vector2[]
        {
            new Vector2(0.375f, 1.945f),  // ~30 mph - subcritical (high drag)
            new Vector2(0.500f, 1.945f),  // ~40 mph
            new Vector2(0.625f, 1.492f),  // ~50 mph - transition begins
            new Vector2(0.750f, 1.039f),  // ~60 mph
            new Vector2(0.875f, 0.586f),  // ~70 mph
            new Vector2(1.000f, 0.132f),  // ~80 mph - supercritical (low drag)
            new Vector2(1.250f, 0.132f),  // ~100 mph
            new Vector2(1.500f, 0.132f),  // ~120 mph
            new Vector2(2.000f, 0.132f),  // ~160 mph
        };

        /// <summary>Drag coefficient for supercritical flow (high speed)</summary>
        public const float CdSupercritical = 0.132f;

        /// <summary>Spin-dependent drag coefficient (calibrated from libgolf)</summary>
        public const float CdSpin = 0.15f;

        /// <summary>Low-speed drag coefficient (from libgolf)</summary>
        public const float CdLow = 0.500f;

        /// <summary>High-speed drag coefficient (calibrated from libgolf)</summary>
        public const float CdHigh = 0.212f;

        /// <summary>Low Reynolds threshold for drag transition (×10⁵)</summary>
        public const float ReLow = 0.5f;

        /// <summary>High Reynolds threshold for drag transition (×10⁵)</summary>
        public const float ReHigh = 1.0f;

        #endregion

        #region Lift Coefficient Table (Nathan Model)

        /// <summary>
        /// Lift coefficient vs spin factor (S = ωr/V).
        /// Format: (SpinFactor, Cl)
        /// From Nathan's TrajectoryCalculatorGolf-v2.xlsx Cd-Cl sheet column F.
        /// </summary>
        public static readonly Vector2[] ClTable = new Vector2[]
        {
            new Vector2(0.00f, 0.000f),
            new Vector2(0.05f, 0.069f),
            new Vector2(0.10f, 0.091f),
            new Vector2(0.15f, 0.107f),
            new Vector2(0.20f, 0.121f),
            new Vector2(0.25f, 0.132f),
            new Vector2(0.30f, 0.142f),
            new Vector2(0.35f, 0.151f),
            new Vector2(0.40f, 0.159f),
            new Vector2(0.45f, 0.167f),
            new Vector2(0.50f, 0.174f),
            new Vector2(0.55f, 0.181f),
        };

        /// <summary>Maximum lift coefficient (from libgolf)</summary>
        public const float ClMax = 0.305f;

        /// <summary>Lift coefficient linear term (from libgolf quadratic formula)</summary>
        public const float ClLinear = 1.990f;

        /// <summary>Lift coefficient quadratic term (from libgolf quadratic formula)</summary>
        public const float ClQuadratic = -3.250f;

        /// <summary>Spin factor threshold for lift cap</summary>
        public const float ClSpinThreshold = 0.30f;

        #endregion
    }

    /// <summary>
    /// Ground surface properties for bounce and roll physics.
    /// </summary>
    [System.Serializable]
    public class GroundSurface
    {
        public string Name;

        /// <summary>Coefficient of Restitution (bounciness) - base value before velocity adjustment</summary>
        public float COR;

        /// <summary>Rolling resistance coefficient</summary>
        public float RollingResistance;

        /// <summary>Friction coefficient</summary>
        public float Friction;

        /// <summary>Tangential friction for bounce calculations (separate from rolling)</summary>
        public float TangentialFriction;

        /// <summary>How much spin is absorbed on impact (0 = none, 1 = all)</summary>
        public float SpinAbsorption;

        /// <summary>Multiplier applied to velocity-dependent COR (soft surfaces lower this)</summary>
        public float CORMultiplier;

        // Pre-defined surfaces
        public static readonly GroundSurface Fairway = new GroundSurface
        {
            Name = "Fairway",
            COR = 0.60f,
            RollingResistance = 0.10f,
            Friction = 0.50f,
            TangentialFriction = 0.70f,
            SpinAbsorption = 0.60f,
            CORMultiplier = 1.0f
        };

        public static readonly GroundSurface Rough = new GroundSurface
        {
            Name = "Rough",
            COR = 0.30f,
            RollingResistance = 0.30f,
            Friction = 0.70f,
            TangentialFriction = 0.90f,
            SpinAbsorption = 0.80f,
            CORMultiplier = 0.50f
        };

        public static readonly GroundSurface Green = new GroundSurface
        {
            Name = "Green",
            COR = 0.40f,
            RollingResistance = 0.05f,
            Friction = 0.30f,
            TangentialFriction = 0.80f,
            SpinAbsorption = 0.70f,
            CORMultiplier = 0.85f
        };
    }
}
