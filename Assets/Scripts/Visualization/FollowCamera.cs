// ABOUTME: Camera mode that follows the ball during flight with smooth damping.
// ABOUTME: Tracks behind the ball with look-ahead based on velocity and rises with apex.

using UnityEngine;

namespace OpenRange.Visualization
{
    /// <summary>
    /// Camera mode that follows the ball during flight.
    /// Tracks behind the ball with configurable offset and damping.
    /// </summary>
    public class FollowCamera : MonoBehaviour, ICameraMode
    {
        [Header("Follow Settings")]
        [SerializeField] private Vector3 _offset = new Vector3(0f, 5f, -15f);
        [SerializeField] private float _followDamping = 5f;
        [SerializeField] private float _lookDamping = 8f;

        [Header("Height Tracking")]
        [SerializeField] private float _minHeight = 2f;
        [SerializeField] private float _heightMultiplier = 0.5f;
        [SerializeField] private float _maxHeight = 50f;

        [Header("Look Ahead")]
        [SerializeField] private float _lookAheadDistance = 10f;
        [SerializeField] private float _velocitySmoothing = 0.3f;

        [Header("Ground Avoidance")]
        [SerializeField] private float _groundClearance = 1f;
        [SerializeField] private LayerMask _groundLayer = ~0;

        private CameraController _controller;
        private Transform _cameraTransform;
        private Transform _target;
        private Vector3 _currentVelocity;
        private Vector3 _smoothedVelocity;
        private Vector3 _lastTargetPosition;
        private bool _isActive;

        /// <summary>
        /// The type of this camera mode.
        /// </summary>
        public CameraMode ModeType => CameraMode.Follow;

        /// <summary>
        /// Whether this mode is currently active.
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// The follow offset from the ball.
        /// </summary>
        public Vector3 Offset
        {
            get => _offset;
            set => _offset = value;
        }

        /// <summary>
        /// The follow damping (higher = smoother, slower response).
        /// </summary>
        public float FollowDamping
        {
            get => _followDamping;
            set => _followDamping = Mathf.Max(0.1f, value);
        }

        /// <summary>
        /// The look damping for rotation.
        /// </summary>
        public float LookDamping
        {
            get => _lookDamping;
            set => _lookDamping = Mathf.Max(0.1f, value);
        }

        /// <summary>
        /// Minimum camera height above ground.
        /// </summary>
        public float MinHeight
        {
            get => _minHeight;
            set => _minHeight = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Look ahead distance based on velocity.
        /// </summary>
        public float LookAheadDistance
        {
            get => _lookAheadDistance;
            set => _lookAheadDistance = value;
        }

        /// <summary>
        /// Enter this camera mode.
        /// </summary>
        public void Enter(CameraController controller, Transform camera)
        {
            _controller = controller;
            _cameraTransform = camera;
            _isActive = true;
            _currentVelocity = Vector3.zero;
            _smoothedVelocity = Vector3.zero;

            if (_target != null)
            {
                _lastTargetPosition = _target.position;
            }
        }

        /// <summary>
        /// Exit this camera mode.
        /// </summary>
        public void Exit()
        {
            _isActive = false;
        }

        /// <summary>
        /// Update camera position and rotation.
        /// </summary>
        public void UpdateCamera(float deltaTime)
        {
            if (!_isActive || _target == null || _cameraTransform == null)
            {
                return;
            }

            // Calculate target velocity
            Vector3 targetPosition = _target.position;
            Vector3 velocity = (targetPosition - _lastTargetPosition) / Mathf.Max(deltaTime, 0.001f);
            _lastTargetPosition = targetPosition;

            // Smooth velocity for look-ahead
            _smoothedVelocity = Vector3.Lerp(_smoothedVelocity, velocity, _velocitySmoothing);

            // Calculate desired camera position
            Vector3 desiredPosition = CalculateDesiredPosition(targetPosition);

            // Smoothly move camera
            _cameraTransform.position = Vector3.SmoothDamp(
                _cameraTransform.position,
                desiredPosition,
                ref _currentVelocity,
                1f / _followDamping
            );

            // Ensure minimum height
            ApplyGroundAvoidance();

            // Look at ball with look-ahead
            Vector3 lookTarget = targetPosition + _smoothedVelocity.normalized * _lookAheadDistance;
            Quaternion desiredRotation = Quaternion.LookRotation(lookTarget - _cameraTransform.position);
            _cameraTransform.rotation = Quaternion.Slerp(
                _cameraTransform.rotation,
                desiredRotation,
                deltaTime * _lookDamping
            );
        }

        /// <summary>
        /// Calculate the desired camera position based on target.
        /// </summary>
        private Vector3 CalculateDesiredPosition(Vector3 targetPosition)
        {
            // Base offset behind and above the ball
            Vector3 offset = _offset;

            // Increase height based on ball height (for apex tracking)
            float additionalHeight = targetPosition.y * _heightMultiplier;
            offset.y = Mathf.Clamp(_offset.y + additionalHeight, _minHeight, _maxHeight);

            // Calculate direction from ball velocity or default to forward
            Vector3 direction;
            if (_smoothedVelocity.sqrMagnitude > 0.01f)
            {
                // Position behind the ball's movement direction
                Vector3 flatVelocity = new Vector3(_smoothedVelocity.x, 0f, _smoothedVelocity.z);
                if (flatVelocity.sqrMagnitude > 0.01f)
                {
                    direction = -flatVelocity.normalized;
                }
                else
                {
                    direction = -Vector3.forward;
                }
            }
            else
            {
                direction = -Vector3.forward;
            }

            // Calculate final position
            Vector3 desiredPosition = targetPosition + direction * Mathf.Abs(offset.z) + Vector3.up * offset.y;

            return desiredPosition;
        }

        /// <summary>
        /// Ensure camera doesn't go below ground.
        /// </summary>
        private void ApplyGroundAvoidance()
        {
            Vector3 position = _cameraTransform.position;

            // Raycast down to find ground
            if (UnityEngine.Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f, _groundLayer))
            {
                float minY = hit.point.y + _groundClearance;
                if (position.y < minY)
                {
                    position.y = minY;
                    _cameraTransform.position = position;
                }
            }
            else
            {
                // No ground found, just ensure minimum height
                if (position.y < _minHeight)
                {
                    position.y = _minHeight;
                    _cameraTransform.position = position;
                }
            }
        }

        /// <summary>
        /// Process input (none for follow mode).
        /// </summary>
        public void ProcessInput()
        {
            // Follow mode doesn't process user input
        }

        /// <summary>
        /// Set the target to follow.
        /// </summary>
        public void SetTarget(Transform target)
        {
            _target = target;
            if (target != null)
            {
                _lastTargetPosition = target.position;
            }
        }

        /// <summary>
        /// Get the current target position.
        /// </summary>
        public Vector3 GetTargetPosition()
        {
            if (_target == null)
            {
                return _cameraTransform != null ? _cameraTransform.position : Vector3.zero;
            }

            return CalculateDesiredPosition(_target.position);
        }

        /// <summary>
        /// Immediately snap to the ideal follow position.
        /// </summary>
        public void SnapToTarget()
        {
            if (_target == null || _cameraTransform == null)
            {
                return;
            }

            _cameraTransform.position = CalculateDesiredPosition(_target.position);
            _cameraTransform.LookAt(_target.position);
            _currentVelocity = Vector3.zero;
            _smoothedVelocity = Vector3.zero;
        }
    }
}
