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
        /// Simulate one step of rolling with spin-dependent effects.
        /// High backspin causes additional braking and can reverse ball direction.
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

            // Improved stopped detection: both speed AND spin must be below thresholds
            if (speed < PhysicsConstants.RollStoppedSpeedThreshold &&
                backspin < PhysicsConstants.RollStoppedSpinThreshold)
            {
                return (pos, Vector3.zero, 0f, Phase.Stopped);
            }

            // Calculate base rolling deceleration
            float baseDecel = surface.RollingResistance * PhysicsConstants.GravityMs2;

            // Add spin-dependent deceleration if spin is above threshold
            float spinDecel = 0f;
            if (backspin >= PhysicsConstants.MinSpinForRollEffect)
            {
                // Spin braking: higher spin = more braking
                float spinFactor = backspin / PhysicsConstants.SpinRollBrakingBase;
                spinDecel = spinFactor * PhysicsConstants.GravityMs2 * surface.SpinBrakingMultiplier;

                // Cap spin braking to prevent unrealistic behavior
                spinDecel = Mathf.Min(spinDecel, PhysicsConstants.MaxSpinRollBraking);
            }

            // Total deceleration
            float totalDecel = baseDecel + spinDecel;

            // Ensure minimum deceleration so ball eventually stops
            totalDecel = Mathf.Max(totalDecel, 0.5f);

            // Calculate new speed
            float newSpeed = speed - totalDecel * dt;

            // Get direction for velocity calculations
            Vector3 direction = speed > 0.001f ? vel.normalized : Vector3.forward;

            // Check for spin-back conditions (high spin at very low speed)
            bool spinBack = false;
            if (backspin >= PhysicsConstants.SpinBackHighThreshold &&
                speed < PhysicsConstants.SpinBackHighVelocityThreshold &&
                speed > 0.01f)  // Must be moving to reverse
            {
                // Spin-back: reverse direction
                spinBack = true;
                newSpeed = speed * PhysicsConstants.BackwardRollVelocityFactor;
                direction = -direction;  // Reverse direction
            }
            else if (backspin >= PhysicsConstants.SpinBackThreshold &&
                     speed < PhysicsConstants.SpinBackVelocityThreshold)
            {
                // Enhanced braking - ball is checking hard
                newSpeed = Mathf.Max(0f, newSpeed * 0.5f);
            }

            // Check if stopped after deceleration
            if (newSpeed <= 0 && !spinBack)
            {
                // Final stop check with spin consideration
                if (backspin < PhysicsConstants.RollStoppedSpinThreshold)
                {
                    return (pos, Vector3.zero, 0f, Phase.Stopped);
                }
                else
                {
                    // High spin but no velocity - ball may still move due to spin
                    // This is a special case where ball is "checking" hard
                    newSpeed = 0f;
                }
            }

            // Calculate new velocity
            Vector3 newVel = direction * newSpeed;

            // Update position
            Vector3 newPos = pos + newVel * dt;
            newPos.y = 0;  // Stay on ground

            // Coupled spin decay: faster decay at slower speeds
            float speedFactor = 1f;
            if (speed > 0.1f)
            {
                speedFactor = 1f + PhysicsConstants.SpinDecaySpeedFactor / speed;
            }
            else
            {
                speedFactor = 1f + PhysicsConstants.SpinDecaySpeedFactor * 10f;  // Very fast decay when nearly stopped
            }

            // Apply spin decay with surface retention
            float baseDecay = PhysicsConstants.SpinDecayBaseRate * speedFactor * dt;
            float surfaceDecay = 1f - (1f - surface.SpinRetentionDuringRoll) * dt * 10f;  // Scale for time step
            float spinRetention = Mathf.Max(0f, 1f - baseDecay) * surfaceDecay;
            float newSpin = backspin * spinRetention;

            // If spin-back occurred, reduce spin significantly
            if (spinBack)
            {
                newSpin *= 0.3f;
            }

            // Spin below threshold becomes zero
            if (newSpin < PhysicsConstants.RollStoppedSpinThreshold)
            {
                newSpin = 0f;
            }

            // Final check: if both are zero, we're stopped
            if (newSpeed < PhysicsConstants.RollStoppedSpeedThreshold &&
                newSpin < PhysicsConstants.RollStoppedSpinThreshold)
            {
                return (pos, Vector3.zero, 0f, Phase.Stopped);
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

        /// <summary>
        /// Estimate roll distance with spin effects.
        /// Accounts for spin-dependent braking, spin-back potential, and surface properties.
        /// </summary>
        /// <param name="landingSpeedMs">Landing speed in m/s</param>
        /// <param name="landingAngleDeg">Landing angle in degrees (0 = horizontal, 90 = vertical)</param>
        /// <param name="backspin">Backspin in rpm</param>
        /// <param name="surface">Ground surface properties</param>
        /// <returns>Estimated roll distance in meters (can be negative for spin-back)</returns>
        public static float EstimateRollWithSpin(
            float landingSpeedMs, float landingAngleDeg, float backspin, GroundSurface surface)
        {
            // Calculate horizontal component of landing speed
            float horizontalSpeed = landingSpeedMs * Mathf.Cos(landingAngleDeg * Mathf.Deg2Rad);

            // Calculate post-bounce horizontal speed using similar logic to Bounce()
            float landingAngleRad = landingAngleDeg * Mathf.Deg2Rad;
            float angleFactor = 1f - Mathf.Sin(landingAngleRad) * 0.3f;

            // Spin braking factor during bounce
            float spinBrakeFactor = 1f - Mathf.Min(
                PhysicsConstants.MaxSpinBraking,
                backspin / PhysicsConstants.SpinBrakingDenominator
            );

            // Base friction reduction
            float baseFrictionReduction = 1f - surface.TangentialFriction * 0.3f;
            float frictionFactor = baseFrictionReduction * angleFactor;

            // Post-bounce speed
            float afterBounceSpeed = horizontalSpeed * frictionFactor * spinBrakeFactor;

            // Estimate post-bounce spin (approximate)
            float postBounceSpin = backspin * (0.3f + 0.4f * 0.5f) * (1f - surface.SpinAbsorption * 0.5f);

            // Calculate deceleration for roll
            // Base rolling resistance
            float baseDecel = surface.RollingResistance * PhysicsConstants.GravityMs2;

            // Spin-dependent deceleration (average over roll)
            float avgSpin = postBounceSpin * 0.6f;  // Spin decays during roll
            float spinDecel = 0f;
            if (avgSpin >= PhysicsConstants.MinSpinForRollEffect)
            {
                float spinFactor = avgSpin / PhysicsConstants.SpinRollBrakingBase;
                spinDecel = spinFactor * PhysicsConstants.GravityMs2 * surface.SpinBrakingMultiplier;
                spinDecel = Mathf.Min(spinDecel, PhysicsConstants.MaxSpinRollBraking);
            }

            float totalDecel = baseDecel + spinDecel;
            totalDecel = Mathf.Max(totalDecel, 0.5f);

            // Check for spin-back conditions
            if (postBounceSpin >= PhysicsConstants.SpinBackHighThreshold &&
                afterBounceSpeed < PhysicsConstants.SpinBackVelocityThreshold * 2f)
            {
                // Spin-back likely - estimate minimal or negative roll
                if (postBounceSpin >= 9000f && landingAngleDeg >= 50f)
                {
                    // Extreme check - may spin back
                    return -1f;  // Indicates spin-back
                }
                // Hard check - minimal roll
                return 0.5f;
            }

            // Check for hard check conditions (high spin, low speed after bounce)
            if (postBounceSpin >= PhysicsConstants.SpinBackThreshold &&
                afterBounceSpeed < PhysicsConstants.SpinBackVelocityThreshold)
            {
                // Ball checks hard - very short roll
                return 1f;
            }

            // Standard physics estimate: d = v² / (2a)
            float rollDistance = (afterBounceSpeed * afterBounceSpeed) / (2f * totalDecel);

            // Clamp to reasonable range
            return Mathf.Max(0f, rollDistance);
        }
    }
}
