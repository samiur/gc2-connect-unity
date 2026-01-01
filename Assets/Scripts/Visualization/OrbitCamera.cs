// ABOUTME: Camera mode for user-controlled orbit around a target point.
// ABOUTME: Supports touch controls (pinch zoom, two-finger rotate) and mouse controls (scroll, drag).

using UnityEngine;

namespace OpenRange.Visualization
{
    /// <summary>
    /// Camera mode for user-controlled orbit around a target point.
    /// Supports both touch and mouse input for rotation and zoom.
    /// </summary>
    public class OrbitCamera : MonoBehaviour, ICameraMode
    {
        [Header("Orbit Settings")]
        [SerializeField] private Vector3 _orbitCenter = new Vector3(0f, 0f, 125f);
        [SerializeField] private float _defaultDistance = 50f;
        [SerializeField] private float _minDistance = 10f;
        [SerializeField] private float _maxDistance = 200f;

        [Header("Rotation Limits")]
        [SerializeField] private float _minPitch = 5f;
        [SerializeField] private float _maxPitch = 85f;

        [Header("Mouse Controls")]
        [SerializeField] private float _mouseSensitivity = 3f;
        [SerializeField] private float _scrollSensitivity = 10f;
        [SerializeField] private int _rotateMouseButton = 1; // Right mouse button

        [Header("Touch Controls")]
        [SerializeField] private float _touchRotateSensitivity = 0.5f;
        [SerializeField] private float _pinchSensitivity = 0.1f;

        [Header("Smoothing")]
        [SerializeField] private float _rotationSmoothing = 10f;
        [SerializeField] private float _zoomSmoothing = 10f;

        [Header("Ground Avoidance")]
        [SerializeField] private float _groundClearance = 1f;

        private CameraController _controller;
        private Transform _cameraTransform;
        private Transform _target;
        private bool _isActive;

        private float _currentYaw;
        private float _currentPitch = 30f;
        private float _currentDistance;
        private float _targetYaw;
        private float _targetPitch = 30f;
        private float _targetDistance;

        // Touch tracking
        private Vector2 _lastTouchMidpoint;
        private float _lastTouchDistance;
        private int _activeTouchCount;

        /// <summary>
        /// The type of this camera mode.
        /// </summary>
        public CameraMode ModeType => CameraMode.FreeOrbit;

        /// <summary>
        /// Whether this mode is currently active.
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// The orbit center point.
        /// </summary>
        public Vector3 OrbitCenter
        {
            get => _orbitCenter;
            set => _orbitCenter = value;
        }

        /// <summary>
        /// Current distance from orbit center.
        /// </summary>
        public float CurrentDistance => _currentDistance;

        /// <summary>
        /// Current yaw angle in degrees.
        /// </summary>
        public float CurrentYaw => _currentYaw;

        /// <summary>
        /// Current pitch angle in degrees.
        /// </summary>
        public float CurrentPitch => _currentPitch;

        /// <summary>
        /// Minimum orbit distance.
        /// </summary>
        public float MinDistance
        {
            get => _minDistance;
            set => _minDistance = Mathf.Max(1f, value);
        }

        /// <summary>
        /// Maximum orbit distance.
        /// </summary>
        public float MaxDistance
        {
            get => _maxDistance;
            set => _maxDistance = Mathf.Max(_minDistance + 1f, value);
        }

        private void Awake()
        {
            _currentDistance = _defaultDistance;
            _targetDistance = _defaultDistance;
        }

        /// <summary>
        /// Enter this camera mode.
        /// </summary>
        public void Enter(CameraController controller, Transform camera)
        {
            _controller = controller;
            _cameraTransform = camera;
            _isActive = true;

            // Initialize angles from current camera position
            if (_cameraTransform != null)
            {
                Vector3 offset = _cameraTransform.position - GetOrbitCenter();
                _currentDistance = offset.magnitude;
                _targetDistance = _currentDistance;

                if (_currentDistance > 0.01f)
                {
                    _currentYaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
                    _currentPitch = Mathf.Asin(Mathf.Clamp(offset.y / _currentDistance, -1f, 1f)) * Mathf.Rad2Deg;
                }

                _targetYaw = _currentYaw;
                _targetPitch = _currentPitch;
            }

            _activeTouchCount = 0;
        }

        /// <summary>
        /// Exit this camera mode.
        /// </summary>
        public void Exit()
        {
            _isActive = false;
        }

        /// <summary>
        /// Update camera position based on orbit parameters.
        /// </summary>
        public void UpdateCamera(float deltaTime)
        {
            if (!_isActive || _cameraTransform == null)
            {
                return;
            }

            // Smooth toward target values
            _currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, deltaTime * _rotationSmoothing);
            _currentPitch = Mathf.Lerp(_currentPitch, _targetPitch, deltaTime * _rotationSmoothing);
            _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, deltaTime * _zoomSmoothing);

            // Calculate camera position
            Vector3 center = GetOrbitCenter();
            Vector3 position = CalculateOrbitPosition(center, _currentYaw, _currentPitch, _currentDistance);

            // Apply ground avoidance
            position = ApplyGroundAvoidance(position);

            // Update camera
            _cameraTransform.position = position;
            _cameraTransform.LookAt(center);
        }

        /// <summary>
        /// Process user input for orbit control.
        /// </summary>
        public void ProcessInput()
        {
            if (!_isActive)
            {
                return;
            }

            // Handle mouse input
            ProcessMouseInput();

            // Handle touch input
            ProcessTouchInput();
        }

        /// <summary>
        /// Process mouse input for orbit control.
        /// </summary>
        private void ProcessMouseInput()
        {
            // Scroll to zoom
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _targetDistance -= scroll * _scrollSensitivity;
                _targetDistance = Mathf.Clamp(_targetDistance, _minDistance, _maxDistance);
            }

            // Right-drag to rotate
            if (Input.GetMouseButton(_rotateMouseButton))
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");

                _targetYaw += mouseX * _mouseSensitivity;
                _targetPitch -= mouseY * _mouseSensitivity;
                _targetPitch = Mathf.Clamp(_targetPitch, _minPitch, _maxPitch);
            }
        }

        /// <summary>
        /// Process touch input for orbit control.
        /// </summary>
        private void ProcessTouchInput()
        {
            int touchCount = Input.touchCount;

            if (touchCount == 2)
            {
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);

                // Calculate midpoint and distance
                Vector2 midpoint = (touch0.position + touch1.position) * 0.5f;
                float distance = Vector2.Distance(touch0.position, touch1.position);

                if (_activeTouchCount == 2)
                {
                    // Rotate based on midpoint delta
                    Vector2 midpointDelta = midpoint - _lastTouchMidpoint;
                    _targetYaw += midpointDelta.x * _touchRotateSensitivity;
                    _targetPitch -= midpointDelta.y * _touchRotateSensitivity;
                    _targetPitch = Mathf.Clamp(_targetPitch, _minPitch, _maxPitch);

                    // Zoom based on pinch
                    float pinchDelta = distance - _lastTouchDistance;
                    _targetDistance -= pinchDelta * _pinchSensitivity;
                    _targetDistance = Mathf.Clamp(_targetDistance, _minDistance, _maxDistance);
                }

                _lastTouchMidpoint = midpoint;
                _lastTouchDistance = distance;
                _activeTouchCount = 2;
            }
            else
            {
                _activeTouchCount = touchCount;
            }
        }

        /// <summary>
        /// Calculate position on orbit sphere.
        /// </summary>
        private Vector3 CalculateOrbitPosition(Vector3 center, float yaw, float pitch, float distance)
        {
            float yawRad = yaw * Mathf.Deg2Rad;
            float pitchRad = pitch * Mathf.Deg2Rad;

            float x = Mathf.Sin(yawRad) * Mathf.Cos(pitchRad);
            float y = Mathf.Sin(pitchRad);
            float z = Mathf.Cos(yawRad) * Mathf.Cos(pitchRad);

            return center + new Vector3(x, y, z) * distance;
        }

        /// <summary>
        /// Apply ground avoidance to camera position.
        /// </summary>
        private Vector3 ApplyGroundAvoidance(Vector3 position)
        {
            if (position.y < _groundClearance)
            {
                position.y = _groundClearance;
            }

            return position;
        }

        /// <summary>
        /// Get the orbit center (target position or configured center).
        /// </summary>
        private Vector3 GetOrbitCenter()
        {
            if (_target != null)
            {
                return _target.position;
            }

            return _orbitCenter;
        }

        /// <summary>
        /// Set the target to orbit around.
        /// </summary>
        public void SetTarget(Transform target)
        {
            _target = target;
        }

        /// <summary>
        /// Get the target position (orbit center).
        /// </summary>
        public Vector3 GetTargetPosition()
        {
            Vector3 center = GetOrbitCenter();
            return CalculateOrbitPosition(center, _targetYaw, _targetPitch, _targetDistance);
        }

        /// <summary>
        /// Set orbit parameters directly.
        /// </summary>
        /// <param name="yaw">Yaw angle in degrees.</param>
        /// <param name="pitch">Pitch angle in degrees.</param>
        /// <param name="distance">Distance from center.</param>
        /// <param name="instant">If true, snap to values; otherwise smooth.</param>
        public void SetOrbitParameters(float yaw, float pitch, float distance, bool instant = false)
        {
            _targetYaw = yaw;
            _targetPitch = Mathf.Clamp(pitch, _minPitch, _maxPitch);
            _targetDistance = Mathf.Clamp(distance, _minDistance, _maxDistance);

            if (instant)
            {
                _currentYaw = _targetYaw;
                _currentPitch = _targetPitch;
                _currentDistance = _targetDistance;
            }
        }

        /// <summary>
        /// Reset to default orbit parameters.
        /// </summary>
        public void ResetToDefault()
        {
            _targetYaw = 0f;
            _targetPitch = 30f;
            _targetDistance = _defaultDistance;
        }
    }
}
