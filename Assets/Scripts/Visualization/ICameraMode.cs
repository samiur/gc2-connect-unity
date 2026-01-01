// ABOUTME: Interface for camera mode implementations.
// ABOUTME: Defines the contract for different camera behaviors (Follow, Orbit, Static, TopDown).

using UnityEngine;

namespace OpenRange.Visualization
{
    /// <summary>
    /// Interface for camera mode implementations.
    /// Each camera mode handles its own update logic and input processing.
    /// </summary>
    public interface ICameraMode
    {
        /// <summary>
        /// The type of this camera mode.
        /// </summary>
        CameraMode ModeType { get; }

        /// <summary>
        /// Called when this mode becomes active.
        /// Use for initialization and setup.
        /// </summary>
        /// <param name="controller">The camera controller managing this mode.</param>
        /// <param name="camera">The camera transform to control.</param>
        void Enter(CameraController controller, Transform camera);

        /// <summary>
        /// Called when this mode becomes inactive.
        /// Use for cleanup.
        /// </summary>
        void Exit();

        /// <summary>
        /// Called every frame while this mode is active.
        /// Handle camera positioning and rotation.
        /// </summary>
        /// <param name="deltaTime">Time since last update.</param>
        void UpdateCamera(float deltaTime);

        /// <summary>
        /// Process input for this camera mode.
        /// Called every frame to handle touch/mouse input.
        /// </summary>
        void ProcessInput();

        /// <summary>
        /// Set the target to follow or focus on.
        /// </summary>
        /// <param name="target">The target transform.</param>
        void SetTarget(Transform target);

        /// <summary>
        /// Get the current target position this mode is focusing on.
        /// </summary>
        /// <returns>World position of the focus point.</returns>
        Vector3 GetTargetPosition();
    }

    /// <summary>
    /// Camera mode types.
    /// </summary>
    public enum CameraMode
    {
        /// <summary>Fixed position, rotates to track target.</summary>
        Static,
        /// <summary>Tracks behind ball during flight.</summary>
        Follow,
        /// <summary>Overhead view for dispersion.</summary>
        TopDown,
        /// <summary>User-controlled orbit around range.</summary>
        FreeOrbit
    }
}
