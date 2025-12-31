// ABOUTME: Golf ball trajectory simulator using RK4 numerical integration.
// ABOUTME: Based on Professor Alan Nathan's trajectory model with Magnus effect and drag.

using System.Collections.Generic;
using UnityEngine;

namespace OpenRange.Physics
{
    /// <summary>
    /// Golf ball trajectory simulator using RK4 integration.
    /// Based on Professor Alan Nathan's trajectory model.
    /// </summary>
    public class TrajectorySimulator
    {
        private float _tempF;
        private float _elevationFt;
        private float _humidityPct;
        private float _pressureInHg;
        private float _windSpeedMph;
        private float _windDirDeg;
        private GroundSurface _surface;
        private float _airDensity;
        private float _dt;

        /// <summary>
        /// Create a trajectory simulator with environmental conditions.
        /// </summary>
        public TrajectorySimulator(
            float tempF = PhysicsConstants.StdTempF,
            float elevationFt = PhysicsConstants.StdElevationFt,
            float humidityPct = PhysicsConstants.StdHumidityPct,
            float pressureInHg = PhysicsConstants.StdPressureInHg,
            float windSpeedMph = 0f,
            float windDirDeg = 0f,
            GroundSurface surface = null,
            float dt = PhysicsConstants.Dt)
        {
            _tempF = tempF;
            _elevationFt = elevationFt;
            _humidityPct = humidityPct;
            _pressureInHg = pressureInHg;
            _windSpeedMph = windSpeedMph;
            _windDirDeg = windDirDeg;
            _surface = surface ?? GroundSurface.Fairway;
            _dt = dt;

            _airDensity = Aerodynamics.CalculateAirDensity(
                _tempF, _elevationFt, _humidityPct, _pressureInHg
            );
        }

        /// <summary>
        /// Update environmental conditions.
        /// </summary>
        public void SetConditions(float tempF, float elevationFt, float humidityPct = 50f,
                                  float windSpeedMph = 0f, float windDirDeg = 0f)
        {
            _tempF = tempF;
            _elevationFt = elevationFt;
            _humidityPct = humidityPct;
            _windSpeedMph = windSpeedMph;
            _windDirDeg = windDirDeg;

            _airDensity = Aerodynamics.CalculateAirDensity(
                _tempF, _elevationFt, _humidityPct, _pressureInHg
            );
        }

        /// <summary>
        /// Set ground surface for bounce/roll physics.
        /// </summary>
        public void SetSurface(GroundSurface surface)
        {
            _surface = surface ?? GroundSurface.Fairway;
        }

        /// <summary>
        /// Simulate full trajectory from launch conditions.
        /// </summary>
        /// <param name="ballSpeedMph">Ball speed in mph</param>
        /// <param name="vlaDeg">Vertical launch angle in degrees</param>
        /// <param name="hlaDeg">Horizontal launch angle in degrees (+ = right)</param>
        /// <param name="backspinRpm">Backspin in rpm</param>
        /// <param name="sidespinRpm">Sidespin in rpm (+ = fade/slice)</param>
        /// <returns>Complete shot result with trajectory</returns>
        public ShotResult Simulate(
            float ballSpeedMph,
            float vlaDeg,
            float hlaDeg,
            float backspinRpm,
            float sidespinRpm)
        {
            // Convert to SI units
            float speedMs = UnitConversions.MphToMs(ballSpeedMph);
            float vlaRad = vlaDeg * Mathf.Deg2Rad;
            float hlaRad = hlaDeg * Mathf.Deg2Rad;

            // Initial velocity vector
            // X = forward (toward target), Y = up, Z = lateral (+ = right)
            Vector3 vel = new Vector3(
                speedMs * Mathf.Cos(vlaRad) * Mathf.Cos(hlaRad),
                speedMs * Mathf.Sin(vlaRad),
                speedMs * Mathf.Cos(vlaRad) * Mathf.Sin(hlaRad)
            );

            // State variables
            Vector3 pos = Vector3.zero;
            float spinBack = backspinRpm;
            float spinSide = sidespinRpm;
            float t = 0f;
            Phase phase = Phase.Flight;

            // Results
            var trajectory = new List<TrajectoryPoint>();
            float maxHeight = 0f;
            float maxHeightTime = 0f;
            float landingTime = 0f;
            Vector3 landingPos = Vector3.zero;
            int bounceCount = 0;

            // Add initial point
            trajectory.Add(new TrajectoryPoint(0, Vector3.zero, Phase.Flight));

            // Simulation loop
            int iter = 0;
            int sampleRate = Mathf.Max(1, PhysicsConstants.MaxIterations / PhysicsConstants.MaxTrajectoryPoints);

            while (t < PhysicsConstants.MaxTime &&
                   phase != Phase.Stopped &&
                   iter < PhysicsConstants.MaxIterations)
            {
                iter++;

                if (phase == Phase.Flight || phase == Phase.Bounce)
                {
                    // RK4 integration step
                    (pos, vel) = RK4Step(pos, vel, spinBack, spinSide);
                    t += _dt;

                    // Spin decay
                    float decay = 1f - PhysicsConstants.SpinDecayRate * _dt;
                    spinBack *= decay;
                    spinSide *= decay;

                    // Track maximum height
                    if (pos.y > maxHeight)
                    {
                        maxHeight = pos.y;
                        maxHeightTime = t;
                    }

                    // Ground contact check
                    if (pos.y <= 0 && t > 0.1f)
                    {
                        // Record first landing
                        if (landingTime == 0)
                        {
                            landingTime = t;
                            landingPos = pos;
                        }

                        float speed = vel.magnitude;
                        if (speed > 2f && bounceCount < PhysicsConstants.MaxBounces)
                        {
                            // Bounce
                            (pos, vel, spinBack) = GroundPhysics.Bounce(pos, vel, spinBack, _surface);
                            phase = Phase.Bounce;
                            bounceCount++;
                        }
                        else
                        {
                            // Start rolling
                            pos.y = 0;
                            vel.y = 0;
                            phase = Phase.Rolling;
                        }
                    }
                }
                else if (phase == Phase.Rolling)
                {
                    // Roll simulation
                    (pos, vel, spinBack, phase) = GroundPhysics.RollStep(pos, vel, spinBack, _surface, _dt);
                    t += _dt;
                }

                // Sample trajectory points
                if (iter % sampleRate == 0 || phase == Phase.Stopped)
                {
                    trajectory.Add(new TrajectoryPoint(
                        t,
                        new Vector3(
                            UnitConversions.MetersToYards(pos.x),
                            UnitConversions.MetersToFeet(Mathf.Max(0, pos.y)),
                            UnitConversions.MetersToYards(pos.z)
                        ),
                        phase
                    ));
                }
            }

            // Calculate final results
            float finalX = UnitConversions.MetersToYards(pos.x);
            float finalZ = UnitConversions.MetersToYards(pos.z);
            float totalDistance = Mathf.Sqrt(finalX * finalX + finalZ * finalZ);

            // Calculate carry distance (at first landing)
            float carryX = UnitConversions.MetersToYards(landingPos.x);
            float carryZ = UnitConversions.MetersToYards(landingPos.z);
            float carryDistance = Mathf.Sqrt(carryX * carryX + carryZ * carryZ);

            return new ShotResult
            {
                Trajectory = trajectory,
                CarryDistance = carryDistance,
                TotalDistance = totalDistance,
                RollDistance = totalDistance - carryDistance,
                OfflineDistance = finalZ,
                MaxHeight = UnitConversions.MetersToFeet(maxHeight),
                MaxHeightTime = maxHeightTime,
                FlightTime = landingTime,
                TotalTime = t,
                BounceCount = bounceCount,

                LaunchData = new LaunchData
                {
                    BallSpeed = ballSpeedMph,
                    VLA = vlaDeg,
                    HLA = hlaDeg,
                    BackSpin = backspinRpm,
                    SideSpin = sidespinRpm
                },

                Conditions = new Conditions
                {
                    TempF = _tempF,
                    ElevationFt = _elevationFt,
                    HumidityPct = _humidityPct,
                    WindSpeedMph = _windSpeedMph,
                    WindDirDeg = _windDirDeg
                }
            };
        }

        /// <summary>
        /// RK4 integration step.
        /// </summary>
        private (Vector3 newPos, Vector3 newVel) RK4Step(
            Vector3 pos, Vector3 vel, float spinBack, float spinSide)
        {
            float dt = _dt;

            // k1
            Vector3 a1 = CalculateAcceleration(pos, vel, spinBack, spinSide);
            Vector3 k1_pos = vel;
            Vector3 k1_vel = a1;

            // k2
            Vector3 pos2 = pos + k1_pos * (dt / 2f);
            Vector3 vel2 = vel + k1_vel * (dt / 2f);
            Vector3 a2 = CalculateAcceleration(pos2, vel2, spinBack, spinSide);
            Vector3 k2_pos = vel2;
            Vector3 k2_vel = a2;

            // k3
            Vector3 pos3 = pos + k2_pos * (dt / 2f);
            Vector3 vel3 = vel + k2_vel * (dt / 2f);
            Vector3 a3 = CalculateAcceleration(pos3, vel3, spinBack, spinSide);
            Vector3 k3_pos = vel3;
            Vector3 k3_vel = a3;

            // k4
            Vector3 pos4 = pos + k3_pos * dt;
            Vector3 vel4 = vel + k3_vel * dt;
            Vector3 a4 = CalculateAcceleration(pos4, vel4, spinBack, spinSide);
            Vector3 k4_pos = vel4;
            Vector3 k4_vel = a4;

            // Combine
            Vector3 newPos = pos + (k1_pos + 2*k2_pos + 2*k3_pos + k4_pos) * (dt / 6f);
            Vector3 newVel = vel + (k1_vel + 2*k2_vel + 2*k3_vel + k4_vel) * (dt / 6f);

            return (newPos, newVel);
        }

        /// <summary>
        /// Calculate acceleration from all forces.
        /// </summary>
        private Vector3 CalculateAcceleration(
            Vector3 pos, Vector3 vel, float spinBack, float spinSide)
        {
            // Get wind at current height
            Vector3 wind = GetWindAtHeight(pos.y);
            Vector3 relVel = vel - wind;
            float speed = relVel.magnitude;

            // If nearly stationary, just gravity
            if (speed < 0.1f)
            {
                return new Vector3(0, -PhysicsConstants.GravityMs2, 0);
            }

            // Aerodynamic coefficients
            float Re = Aerodynamics.CalculateReynolds(speed, _airDensity);
            float Cd = Aerodynamics.GetDragCoefficient(Re);

            float omega = UnitConversions.RpmToRadS(
                Mathf.Sqrt(spinBack * spinBack + spinSide * spinSide)
            );
            float S = (omega * PhysicsConstants.BallRadiusM) / speed;
            float Cl = Aerodynamics.GetLiftCoefficient(S);

            // Dynamic pressure
            float q = 0.5f * _airDensity * speed * speed;

            // Drag force (opposes motion)
            float dragMag = q * Cd * PhysicsConstants.BallAreaM2;
            Vector3 dragDir = -relVel.normalized;
            Vector3 dragForce = dragDir * dragMag;

            // Magnus force (perpendicular to velocity and spin)
            // Backspin axis is along Z (lateral), sidespin axis is along Y (vertical)
            // Negative sign on sidespin so positive sidespin (fade/slice) curves right
            float omegaBack = UnitConversions.RpmToRadS(spinBack);
            float omegaSide = UnitConversions.RpmToRadS(spinSide);
            Vector3 spinVec = new Vector3(0, -omegaSide, omegaBack);
            Vector3 magnusDir = Vector3.Cross(spinVec, relVel).normalized;

            // Handle NaN from zero cross product
            if (float.IsNaN(magnusDir.x))
            {
                magnusDir = Vector3.zero;
            }

            float magnusMag = q * Cl * PhysicsConstants.BallAreaM2;
            Vector3 magnusForce = magnusDir * magnusMag;

            // Gravity force
            Vector3 gravityForce = new Vector3(
                0,
                -PhysicsConstants.BallMassKg * PhysicsConstants.GravityMs2,
                0
            );

            // Total force and acceleration
            Vector3 totalForce = gravityForce + dragForce + magnusForce;
            Vector3 accel = totalForce / PhysicsConstants.BallMassKg;

            // Safety check for NaN
            if (float.IsNaN(accel.x) || float.IsNaN(accel.y) || float.IsNaN(accel.z))
            {
                return new Vector3(0, -PhysicsConstants.GravityMs2, 0);
            }

            return accel;
        }

        /// <summary>
        /// Get wind velocity at a given height (logarithmic profile).
        /// </summary>
        private Vector3 GetWindAtHeight(float heightM)
        {
            if (_windSpeedMph < 0.1f) return Vector3.zero;

            float heightFt = UnitConversions.MetersToFeet(heightM);

            // Logarithmic wind profile
            const float z0 = 0.01f;        // Roughness length (short grass)
            const float refHeight = 10f;   // Reference height in feet

            float factor = 1f;
            if (heightFt > 0.1f)
            {
                factor = Mathf.Log(heightFt / z0) / Mathf.Log(refHeight / z0);
                factor = Mathf.Clamp(factor, 0f, 2f);
            }

            float windSpeedMs = UnitConversions.MphToMs(_windSpeedMph * factor);
            float windDirRad = _windDirDeg * Mathf.Deg2Rad;

            // Wind direction: 0° = headwind (from target), 90° = from right
            return new Vector3(
                -windSpeedMs * Mathf.Cos(windDirRad),
                0f,
                windSpeedMs * Mathf.Sin(windDirRad)
            );
        }
    }
}
