// ABOUTME: Main camera controller that manages camera modes and transitions.
// ABOUTME: Subscribes to BallController events for automatic mode switching during shot animation.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenRange.Visualization
{
    /// <summary>
    /// Main camera controller that manages camera modes and transitions.
    /// Automatically switches between modes based on ball flight state.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Reference")]
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _cameraTransform;

        [Header("Mode Settings")]
        [SerializeField] private CameraMode _defaultMode = CameraMode.Static;
        [SerializeField] private bool _autoSwitchModes = true;

        [Header("Transition Settings")]
        [SerializeField] private float _transitionDuration = 0.5f;
        [SerializeField] private AnimationCurve _transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Static Camera Settings")]
        [SerializeField] private Vector3 _staticPosition = new Vector3(0f, 3f, -8f);
        [SerializeField] private Vector3 _staticLookAt = new Vector3(0f, 0f, 50f);

        [Header("TopDown Camera Settings")]
        [SerializeField] private float _topDownHeight = 100f;
        [SerializeField] private Vector3 _topDownCenter = new Vector3(0f, 0f, 125f);

        [Header("Target")]
        [SerializeField] private Transform _ballTransform;
        [SerializeField] private BallController _ballController;

        private Dictionary<CameraMode, ICameraMode> _modes = new Dictionary<CameraMode, ICameraMode>();
        private ICameraMode _currentMode;
        private CameraMode _currentModeType = CameraMode.Static;
        private bool _isTransitioning;
        private float _transitionProgress;
        private Vector3 _transitionStartPosition;
        private Quaternion _transitionStartRotation;
        private Vector3 _transitionTargetPosition;
        private Quaternion _transitionTargetRotation;
        private CameraMode _transitionTargetMode;

        /// <summary>
        /// The current camera mode type.
        /// </summary>
        public CameraMode CurrentMode => _currentModeType;

        /// <summary>
        /// Whether the camera is currently transitioning between modes.
        /// </summary>
        public bool IsTransitioning => _isTransitioning;

        /// <summary>
        /// Whether auto mode switching is enabled.
        /// </summary>
        public bool AutoSwitchModes
        {
            get => _autoSwitchModes;
            set => _autoSwitchModes = value;
        }

        /// <summary>
        /// The camera being controlled.
        /// </summary>
        public Camera Camera => _camera;

        /// <summary>
        /// The camera transform.
        /// </summary>
        public Transform CameraTransform => _cameraTransform;

        /// <summary>
        /// The ball transform being tracked.
        /// </summary>
        public Transform BallTransform => _ballTransform;

        /// <summary>
        /// Static camera position preset.
        /// </summary>
        public Vector3 StaticPosition
        {
            get => _staticPosition;
            set => _staticPosition = value;
        }

        /// <summary>
        /// Static camera look-at point.
        /// </summary>
        public Vector3 StaticLookAt
        {
            get => _staticLookAt;
            set => _staticLookAt = value;
        }

        /// <summary>
        /// Top-down camera height.
        /// </summary>
        public float TopDownHeight
        {
            get => _topDownHeight;
            set => _topDownHeight = value;
        }

        /// <summary>
        /// Transition duration in seconds.
        /// </summary>
        public float TransitionDuration
        {
            get => _transitionDuration;
            set => _transitionDuration = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Fired when camera mode changes.
        /// </summary>
        public event Action<CameraMode> OnModeChanged;

        /// <summary>
        /// Fired when a transition starts.
        /// </summary>
        public event Action<CameraMode, CameraMode> OnTransitionStarted;

        /// <summary>
        /// Fired when a transition completes.
        /// </summary>
        public event Action<CameraMode> OnTransitionCompleted;

        private void Awake()
        {
            InitializeComponents();
            RegisterModes();
        }

        private void Start()
        {
            // Set initial mode
            SetMode(_defaultMode, instant: true);

            // Subscribe to ball controller events
            SubscribeToBallController();
        }

        private void OnDestroy()
        {
            UnsubscribeFromBallController();
        }

        private void Update()
        {
            if (_isTransitioning)
            {
                UpdateTransition();
            }
            else if (_currentMode != null)
            {
                _currentMode.ProcessInput();
                _currentMode.UpdateCamera(Time.deltaTime);
            }
        }

        /// <summary>
        /// Initialize component references.
        /// </summary>
        private void InitializeComponents()
        {
            if (_camera == null)
            {
                _camera = GetComponentInChildren<Camera>();
            }

            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_cameraTransform == null && _camera != null)
            {
                _cameraTransform = _camera.transform;
            }

            if (_cameraTransform == null)
            {
                _cameraTransform = transform;
            }

            if (_ballController == null)
            {
                _ballController = FindAnyObjectByType<BallController>();
            }

            if (_ballTransform == null && _ballController != null)
            {
                _ballTransform = _ballController.BallTransform;
            }
        }

        /// <summary>
        /// Register all available camera modes.
        /// </summary>
        private void RegisterModes()
        {
            // Register built-in modes
            var followCamera = GetComponent<FollowCamera>();
            if (followCamera != null)
            {
                _modes[CameraMode.Follow] = followCamera;
            }

            var orbitCamera = GetComponent<OrbitCamera>();
            if (orbitCamera != null)
            {
                _modes[CameraMode.FreeOrbit] = orbitCamera;
            }

            // Static and TopDown are handled internally by CameraController
        }

        /// <summary>
        /// Register a camera mode.
        /// </summary>
        /// <param name="mode">The mode type.</param>
        /// <param name="handler">The mode handler.</param>
        public void RegisterMode(CameraMode mode, ICameraMode handler)
        {
            _modes[mode] = handler;
        }

        /// <summary>
        /// Subscribe to BallController events for auto mode switching.
        /// </summary>
        private void SubscribeToBallController()
        {
            if (_ballController != null)
            {
                _ballController.OnFlightStarted += HandleFlightStarted;
                _ballController.OnStopped += HandleBallStopped;
            }
        }

        /// <summary>
        /// Unsubscribe from BallController events.
        /// </summary>
        private void UnsubscribeFromBallController()
        {
            if (_ballController != null)
            {
                _ballController.OnFlightStarted -= HandleFlightStarted;
                _ballController.OnStopped -= HandleBallStopped;
            }
        }

        /// <summary>
        /// Handle ball flight started - switch to follow mode.
        /// </summary>
        private void HandleFlightStarted()
        {
            if (_autoSwitchModes)
            {
                SetMode(CameraMode.Follow);
            }
        }

        /// <summary>
        /// Handle ball stopped - return to static mode.
        /// </summary>
        private void HandleBallStopped(Vector3 position)
        {
            if (_autoSwitchModes)
            {
                SetMode(CameraMode.Static);
            }
        }

        /// <summary>
        /// Set the camera mode with optional instant transition.
        /// </summary>
        /// <param name="mode">The mode to switch to.</param>
        /// <param name="instant">If true, skip transition animation.</param>
        public void SetMode(CameraMode mode, bool instant = false)
        {
            if (_currentModeType == mode && !instant)
            {
                return;
            }

            // Exit current mode
            _currentMode?.Exit();

            CameraMode previousMode = _currentModeType;
            _currentModeType = mode;

            // Get new mode handler
            _modes.TryGetValue(mode, out _currentMode);

            // Set up target position/rotation based on mode
            Vector3 targetPosition;
            Quaternion targetRotation;

            switch (mode)
            {
                case CameraMode.Static:
                    targetPosition = _staticPosition;
                    targetRotation = Quaternion.LookRotation(_staticLookAt - _staticPosition);
                    break;

                case CameraMode.TopDown:
                    targetPosition = new Vector3(_topDownCenter.x, _topDownHeight, _topDownCenter.z);
                    targetRotation = Quaternion.Euler(90f, 0f, 0f);
                    break;

                case CameraMode.Follow:
                case CameraMode.FreeOrbit:
                    if (_currentMode != null)
                    {
                        _currentMode.SetTarget(_ballTransform);
                        _currentMode.Enter(this, _cameraTransform);
                        targetPosition = _currentMode.GetTargetPosition();
                        targetRotation = _cameraTransform.rotation;
                    }
                    else
                    {
                        // Fallback if mode not registered
                        targetPosition = _staticPosition;
                        targetRotation = Quaternion.LookRotation(_staticLookAt - _staticPosition);
                    }
                    break;

                default:
                    targetPosition = _cameraTransform.position;
                    targetRotation = _cameraTransform.rotation;
                    break;
            }

            if (instant || _transitionDuration <= 0f)
            {
                // Instant switch
                _cameraTransform.position = targetPosition;
                _cameraTransform.rotation = targetRotation;

                if (_currentMode != null && mode != CameraMode.Static && mode != CameraMode.TopDown)
                {
                    _currentMode.Enter(this, _cameraTransform);
                }

                OnModeChanged?.Invoke(mode);
            }
            else
            {
                // Start transition
                StartTransition(targetPosition, targetRotation, mode, previousMode);
            }
        }

        /// <summary>
        /// Start a transition to a new camera position/rotation.
        /// </summary>
        private void StartTransition(Vector3 targetPosition, Quaternion targetRotation, CameraMode targetMode, CameraMode fromMode)
        {
            _isTransitioning = true;
            _transitionProgress = 0f;
            _transitionStartPosition = _cameraTransform.position;
            _transitionStartRotation = _cameraTransform.rotation;
            _transitionTargetPosition = targetPosition;
            _transitionTargetRotation = targetRotation;
            _transitionTargetMode = targetMode;

            OnTransitionStarted?.Invoke(fromMode, targetMode);
        }

        /// <summary>
        /// Update the transition animation.
        /// </summary>
        private void UpdateTransition()
        {
            _transitionProgress += Time.deltaTime / _transitionDuration;

            if (_transitionProgress >= 1f)
            {
                // Complete transition
                _transitionProgress = 1f;
                _cameraTransform.position = _transitionTargetPosition;
                _cameraTransform.rotation = _transitionTargetRotation;
                _isTransitioning = false;

                // Enter the new mode
                if (_currentMode != null && _transitionTargetMode != CameraMode.Static && _transitionTargetMode != CameraMode.TopDown)
                {
                    _currentMode.Enter(this, _cameraTransform);
                }

                OnTransitionCompleted?.Invoke(_transitionTargetMode);
                OnModeChanged?.Invoke(_transitionTargetMode);
            }
            else
            {
                // Interpolate
                float t = _transitionCurve.Evaluate(_transitionProgress);
                _cameraTransform.position = Vector3.Lerp(_transitionStartPosition, _transitionTargetPosition, t);
                _cameraTransform.rotation = Quaternion.Slerp(_transitionStartRotation, _transitionTargetRotation, t);
            }
        }

        /// <summary>
        /// Immediately snap to a position and look at a target.
        /// </summary>
        /// <param name="position">The camera position.</param>
        /// <param name="lookAt">The point to look at.</param>
        public void SnapTo(Vector3 position, Vector3 lookAt)
        {
            _isTransitioning = false;
            _cameraTransform.position = position;
            _cameraTransform.LookAt(lookAt);
        }

        /// <summary>
        /// Set the ball controller reference.
        /// </summary>
        public void SetBallController(BallController controller)
        {
            UnsubscribeFromBallController();
            _ballController = controller;
            _ballTransform = controller?.BallTransform;
            SubscribeToBallController();

            // Update target for current mode
            _currentMode?.SetTarget(_ballTransform);
        }

        /// <summary>
        /// Set the ball transform directly.
        /// </summary>
        public void SetBallTransform(Transform ballTransform)
        {
            _ballTransform = ballTransform;
            _currentMode?.SetTarget(_ballTransform);
        }

        /// <summary>
        /// Move the static camera position.
        /// </summary>
        public void SetStaticPosition(Vector3 position, Vector3 lookAt)
        {
            _staticPosition = position;
            _staticLookAt = lookAt;

            if (_currentModeType == CameraMode.Static && !_isTransitioning)
            {
                _cameraTransform.position = position;
                _cameraTransform.LookAt(lookAt);
            }
        }

        /// <summary>
        /// Set the camera reference.
        /// </summary>
        public void SetCamera(Camera camera)
        {
            _camera = camera;
            _cameraTransform = camera?.transform;
        }

        /// <summary>
        /// Get the current mode handler.
        /// </summary>
        public ICameraMode GetCurrentModeHandler()
        {
            return _currentMode;
        }
    }
}
