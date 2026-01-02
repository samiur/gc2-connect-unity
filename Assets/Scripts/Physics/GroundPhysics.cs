// ABOUTME: Ground interaction physics for bounce and roll with spin effects.
// ABOUTME: Implements Penner's velocity-dependent COR and spin-dependent braking.

using UnityEngine;

namespace OpenRange.Physics
{
    /// <summary>
    /// Result of a bounce calculation including spin state.
    /// </summary>
    public struct BounceResult
    {
        /// <summary>Position after bounce</summary>
        public Vector3 Position;

        /// <summary>Velocity after bounce</summary>
        public Vector3 Velocity;

        /// <summary>Remaining backspin after bounce (rpm)</summary>
        public float Spin;

        /// <summary>True if spin reversal (check/spin-back) occurred</summary>
        public bool SpinReversed;
    }

    /// <summary>
    /// Ground interaction physics for bounce and roll.
    /// Implements spin-dependent effects and velocity-dependent COR based on Penner's model.
    /// </summary>
    public static class GroundPhysics
    {
        /// <summary>
        /// Calculate velocity-dependent COR using Penner's formula.
        /// e = 0.510 - 0.0375*v + 0.000903*v² (clamped to valid range)
        /// </summary>
        /// <param name="normalVelocity">Normal component of impact velocity (m/s, positive)</param>
        /// <param name="surface">Ground surface properties</param>
        /// <returns>Coefficient of restitution for the bounce</returns>
        public static float CalculateVelocityDependentCOR(float normalVelocity, GroundSurface surface)
        {
            float v = Mathf.Abs(normalVelocity);

            // Penner's formula: e = A - B*v + C*v²
            float cor = PhysicsConstants.PennerCOR_A
                      - PhysicsConstants.PennerCOR_B * v
                      + PhysicsConstants.PennerCOR_C * v * v;

            // Apply surface multiplier
            cor *= surface.CORMultiplier;

            // Clamp to valid range
            return Mathf.Clamp(cor, PhysicsConstants.MinCOR, PhysicsConstants.MaxCOR);
        }

        /// <summary>
        /// Calculate bounce result with full spin-dependent effects.
        /// </summary>
        /// <param name="pos">Impact position</param>
        /// <param name="vel">Impact velocity</param>
        /// <param name="backspin">Backspin in rpm</param>
        /// <param name="landingAngleDeg">Landing angle in degrees (0 = horizontal, 90 = vertical)</param>
        /// <param name="surface">Ground surface properties</param>
        /// <returns>Complete bounce result with position, velocity, spin, and reversal flag</returns>
        public static BounceResult Bounce(
            Vector3 pos, Vector3 vel, float backspin, float landingAngleDeg, GroundSurface surface)
        {
            Vector3 normal = Vector3.up;

            // Decompose velocity into normal and tangential components
            float vn = Vector3.Dot(vel, normal);  // Normal component (negative when hitting ground)
            Vector3 vt = vel - normal * vn;       // Tangential component
            float vtMagnitude = vt.magnitude;

            // Calculate velocity-dependent COR (using absolute normal velocity)
            float cor = CalculateVelocityDependentCOR(-vn, surface);

            // Apply COR to normal component (bounce height)
            float vnNew = -vn * cor;

            // Calculate landing angle effect on friction
            // Steeper angles increase friction effect (reduce velocity retention)
            float landingAngleRad = landingAngleDeg * Mathf.Deg2Rad;
            float angleFactor = 1f - Mathf.Sin(landingAngleRad) * 0.3f;

            // Calculate spin-dependent braking factor
            // High backspin reduces horizontal velocity after bounce
            float spinBrakeFactor = 1f - Mathf.Min(
                PhysicsConstants.MaxSpinBraking,
                backspin / PhysicsConstants.SpinBrakingDenominator
            );

            // Apply tangential friction with angle and spin effects
            float baseFrictionReduction = 1f - surface.TangentialFriction * 0.3f;
            float frictionFactor = baseFrictionReduction * angleFactor;

            // Combine all horizontal velocity reductions
            Vector3 vtNew = vt * frictionFactor * spinBrakeFactor;

            // Check for spin reversal conditions
            bool spinReversed = false;
            if (backspin >= PhysicsConstants.SpinReversalThreshold &&
                landingAngleDeg >= PhysicsConstants.LandingAngleThresholdDeg &&
                vtMagnitude < PhysicsConstants.SpinReversalVelocityThreshold)
            {
                // Spin reversal - ball checks hard or spins back
                spinReversed = true;

                // Calculate reversal strength based on spin and angle
                float reversalStrength = Mathf.Min(1f,
                    (backspin - PhysicsConstants.SpinReversalThreshold) / 5000f *
                    (landingAngleDeg - PhysicsConstants.LandingAngleThresholdDeg) / 20f
                );

                // Apply reversal: reduce or reverse horizontal velocity
                vtNew *= (1f - reversalStrength * 1.5f);

                // If strong reversal, can actually reverse direction
                if (reversalStrength > 0.7f && vtMagnitude > 0.5f)
                {
                    vtNew = -vtNew.normalized * 0.5f; // Spin-back effect
                }
            }

            // Combine velocity components
            Vector3 newVel = vtNew + normal * vnNew;

            // Calculate post-bounce spin
            // Spin reduces based on COR (higher bounce = more spin retained) and surface absorption
            float spinRetention = (0.3f + 0.4f * cor) * (1f - surface.SpinAbsorption * 0.5f);
            float newSpin = backspin * spinRetention;

            // If spin reversed, spin decays more
            if (spinReversed)
            {
                newSpin *= 0.5f;
            }

            // Ensure ball is above ground
            Vector3 newPos = new Vector3(pos.x, 0.001f, pos.z);

            return new BounceResult
            {
                Position = newPos,
                Velocity = newVel,
                Spin = newSpin,
                SpinReversed = spinReversed
            };
        }

        /// <summary>
        /// Calculate bounce result when ball hits ground (backward compatible signature).
        /// Uses a default landing angle calculated from velocity.
        /// </summary>
        /// <param name="pos">Impact position</param>
        /// <param name="vel">Impact velocity</param>
        /// <param name="backspin">Backspin in rpm</param>
        /// <param name="surface">Ground surface properties</param>
        /// <returns>New position, velocity, and spin after bounce</returns>
        public static (Vector3 pos, Vector3 vel, float spin) Bounce(
            Vector3 pos, Vector3 vel, float backspin, GroundSurface surface)
        {
            // Calculate landing angle from velocity
            float horizontalSpeed = Mathf.Sqrt(vel.x * vel.x + vel.z * vel.z);
            float landingAngleDeg = Mathf.Atan2(-vel.y, horizontalSpeed) * Mathf.Rad2Deg;
            landingAngleDeg = Mathf.Clamp(landingAngleDeg, 0f, 90f);

            // Call the full implementation
            var result = Bounce(pos, vel, backspin, landingAngleDeg, surface);

            return (result.Position, result.Velocity, result.Spin);
        }

        /// <summary>
        /// Simulate one step of rolling.
        /// </summary>
        /// <param name="pos">Current position</param>
        /// <param name="vel">Current velocity</param>
        /// <param name="backspin">Current backspin in rpm</param>
        /// <param name="surface">Ground surface properties</param>
        /// <param name="dt">Time step</param>
        /// <returns>New position, velocity, spin, and phase</returns>
        public static (Vector3 pos, Vector3 vel, float spin, Phase phase) RollStep(
            Vector3 pos, Vector3 vel, float backspin, GroundSurface surface, float dt)
        {
            // Get horizontal speed
            vel.y = 0;  // Ensure on ground
            float speed = vel.magnitude;

            // Check if stopped
            if (speed < PhysicsConstants.StoppedThreshold)
            {
                return (pos, Vector3.zero, 0f, Phase.Stopped);
            }

            // Rolling deceleration from friction
            float decel = surface.RollingResistance * PhysicsConstants.GravityMs2;

            // Ensure minimum deceleration (so ball eventually stops)
            decel = Mathf.Max(decel, 0.5f);

            // Calculate new speed
            float newSpeed = speed - decel * dt;

            if (newSpeed <= 0)
            {
                return (pos, Vector3.zero, 0f, Phase.Stopped);
            }

            // Update velocity (same direction, new magnitude)
            Vector3 direction = vel.normalized;
            Vector3 newVel = direction * newSpeed;

            // Update position
            Vector3 newPos = pos + newVel * dt;
            newPos.y = 0;  // Stay on ground

            // Spin decay during roll
            float spinDecay = 1f - 0.15f * dt;
            float newSpin = backspin * spinDecay;

            // Small spin threshold
            if (Mathf.Abs(newSpin) < 10f)
            {
                newSpin = 0f;
            }

            return (newPos, newVel, newSpin, Phase.Rolling);
        }

        /// <summary>
        /// Estimate roll distance for a given landing speed and surface.
        /// </summary>
        /// <param name="landingSpeedMs">Landing speed in m/s</param>
        /// <param name="landingAngleDeg">Landing angle in degrees</param>
        /// <param name="surface">Ground surface</param>
        /// <returns>Estimated roll distance in meters</returns>
        public static float EstimateRollDistance(float landingSpeedMs, float landingAngleDeg, GroundSurface surface)
        {
            // Simple physics-based estimate
            // v² = v0² + 2ad, solving for d when v=0
            // d = v0² / (2a) where a = friction * g

            float horizontalSpeed = landingSpeedMs * Mathf.Cos(landingAngleDeg * Mathf.Deg2Rad);
            float afterBounce = horizontalSpeed * (1f - surface.Friction * 0.3f);
            float decel = surface.RollingResistance * PhysicsConstants.GravityMs2;

            if (decel < 0.1f) decel = 0.1f;

            return (afterBounce * afterBounce) / (2f * decel);
        }
    }
}
