using UnityEngine;

namespace OpenRange.Physics
{
    /// <summary>
    /// Ground interaction physics for bounce and roll.
    /// </summary>
    public static class GroundPhysics
    {
        /// <summary>
        /// Calculate bounce result when ball hits ground.
        /// </summary>
        /// <param name="pos">Impact position</param>
        /// <param name="vel">Impact velocity</param>
        /// <param name="backspin">Backspin in rpm</param>
        /// <param name="surface">Ground surface properties</param>
        /// <returns>New position, velocity, and spin after bounce</returns>
        public static (Vector3 pos, Vector3 vel, float spin) Bounce(
            Vector3 pos, Vector3 vel, float backspin, GroundSurface surface)
        {
            Vector3 normal = Vector3.up;
            
            // Decompose velocity into normal and tangential components
            float vn = Vector3.Dot(vel, normal);  // Normal component (negative when hitting ground)
            Vector3 vt = vel - normal * vn;       // Tangential component
            
            // Apply coefficient of restitution to normal component
            float vnNew = -vn * surface.COR;
            
            // Apply friction to tangential component
            // Friction reduces forward velocity on impact
            float frictionFactor = 1f - surface.Friction * 0.3f;
            Vector3 vtNew = vt * frictionFactor;
            
            // Combine components
            Vector3 newVel = vtNew + normal * vnNew;
            
            // Spin reduction on bounce
            // Impact absorbs some rotational energy
            float spinReduction = 0.7f - (surface.Friction * 0.1f);
            float newSpin = backspin * spinReduction;
            
            // Ensure ball is above ground
            Vector3 newPos = new Vector3(pos.x, 0.001f, pos.z);
            
            return (newPos, newVel, newSpin);
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
