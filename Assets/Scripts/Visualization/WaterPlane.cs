// ABOUTME: Component for water plane GameObjects that auto-registers with WaterController.
// ABOUTME: Handles registration/unregistration lifecycle and provides water surface reference.

using UnityEngine;

namespace OpenRange.Visualization
{
    /// <summary>
    /// Marks a GameObject as a water surface and handles registration with WaterController.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class WaterPlane : MonoBehaviour
    {
        private Renderer _renderer;
        private bool _isRegistered;

        /// <summary>
        /// The renderer for this water plane.
        /// </summary>
        public Renderer Renderer => _renderer;

        /// <summary>
        /// Whether this water plane is registered with the controller.
        /// </summary>
        public bool IsRegistered => _isRegistered;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
        }

        private void OnEnable()
        {
            RegisterWithController();
        }

        private void OnDisable()
        {
            UnregisterFromController();
        }

        private void OnDestroy()
        {
            UnregisterFromController();
        }

        /// <summary>
        /// Registers this water plane with the WaterController.
        /// </summary>
        public void RegisterWithController()
        {
            if (_isRegistered)
            {
                return;
            }

            if (WaterController.Instance != null && _renderer != null)
            {
                WaterController.Instance.RegisterWaterRenderer(_renderer);
                WaterController.Instance.SetWaterPlane(transform);
                _isRegistered = true;
            }
        }

        /// <summary>
        /// Unregisters this water plane from the WaterController.
        /// </summary>
        public void UnregisterFromController()
        {
            if (!_isRegistered)
            {
                return;
            }

            if (WaterController.Instance != null && _renderer != null)
            {
                WaterController.Instance.UnregisterWaterRenderer(_renderer);
                _isRegistered = false;
            }
        }

        /// <summary>
        /// Forces registration with the controller.
        /// Used when the controller is initialized after this plane.
        /// </summary>
        public void ForceRegister()
        {
            _isRegistered = false;
            RegisterWithController();
        }

        /// <summary>
        /// Forces initialization for EditMode tests where Awake() is not called.
        /// </summary>
        internal void ForceInitialize()
        {
            _renderer = GetComponent<Renderer>();
        }
    }
}
