// ABOUTME: Animates golf ball flight from trajectory data produced by ShotProcessor.
// ABOUTME: Provides playback controls, events for flight phases, and position interpolation.

using System;
using System.Collections;
using UnityEngine;
using OpenRange.Physics;

namespace OpenRange.Visualization
{
    /// <summary>
    /// Animates golf ball flight from trajectory data.
    /// Interpolates ball position through TrajectoryPoints and provides
    /// playback controls, events for flight phases, and spin visualization.
    /// </summary>
    public class BallController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _ballTransform;
        [SerializeField] private BallVisuals _ballVisuals;
        [SerializeField] private BallSpinner _ballSpinner;

        [Header("Tee Position")]
        [SerializeField] private Vector3 _teePosition = Vector3.zero;

        [Header("Animation Settings")]
        [SerializeField] private float _playbackSpeed = 1f;
        [SerializeField] private TimeScale _timeScale = TimeScale.Realtime;

        [Header("Coordinate Conversion")]
        [SerializeField] private float _yardsToUnits = 0.9144f;
        [SerializeField] private float _feetToUnits = 0.3048f;

        private ShotResult _currentShot;
        private Coroutine _animationCoroutine;
        private AnimationPhase _currentPhase = AnimationPhase.Idle;
        private float _progress;
        private bool _isPaused;
        private int _currentPointIndex;
        private float _apexHeight;
        private bool _apexReached;
        private bool _landingTriggered;

        /// <summary>
        /// Whether an animation is currently running.
        /// </summary>
        public bool IsAnimating => _animationCoroutine != null && !_isPaused;

        /// <summary>
        /// Current animation phase.
        /// </summary>
        public AnimationPhase CurrentPhase => _currentPhase;

        /// <summary>
        /// Animation progress from 0 to 1.
        /// </summary>
        public float Progress => _progress;

        /// <summary>
        /// Whether the animation is paused.
        /// </summary>
        public bool IsPaused => _isPaused;

        /// <summary>
        /// Playback speed multiplier (1.0 = realtime).
        /// </summary>
        public float PlaybackSpeed
        {
            get => _playbackSpeed;
            set => _playbackSpeed = Mathf.Max(0.1f, value);
        }

        /// <summary>
        /// Current time scale mode.
        /// </summary>
        public TimeScale CurrentTimeScale
        {
            get => _timeScale;
            set => _timeScale = value;
        }

        /// <summary>
        /// The current shot result being animated.
        /// </summary>
        public ShotResult CurrentShot => _currentShot;

        /// <summary>
        /// The tee position (start position for the ball).
        /// </summary>
        public Vector3 TeePosition
        {
            get => _teePosition;
            set => _teePosition = value;
        }

        /// <summary>
        /// The ball transform being animated.
        /// </summary>
        public Transform BallTransform => _ballTransform;

        /// <summary>
        /// Fired when ball flight animation starts.
        /// </summary>
        public event Action OnFlightStarted;

        /// <summary>
        /// Fired when the ball reaches its apex (maximum height).
        /// </summary>
        public event Action<float> OnApexReached;

        /// <summary>
        /// Fired when the ball first contacts the ground.
        /// </summary>
        public event Action<Vector3> OnLanded;

        /// <summary>
        /// Fired when the ball transitions to rolling phase.
        /// </summary>
        public event Action OnRollStarted;

        /// <summary>
        /// Fired when the ball comes to rest.
        /// </summary>
        public event Action<Vector3> OnStopped;

        /// <summary>
        /// Fired when animation phase changes.
        /// </summary>
        public event Action<AnimationPhase> OnPhaseChanged;

        private void Awake()
        {
            InitializeReferences();
        }

        /// <summary>
        /// Initialize component references if not set in inspector.
        /// </summary>
        private void InitializeReferences()
        {
            if (_ballTransform == null)
            {
                _ballTransform = transform;
            }

            if (_ballVisuals == null)
            {
                _ballVisuals = GetComponent<BallVisuals>();
            }

            if (_ballSpinner == null)
            {
                _ballSpinner = GetComponent<BallSpinner>();
            }
        }

        /// <summary>
        /// Start animating a shot result.
        /// </summary>
        /// <param name="result">The shot result containing trajectory data.</param>
        public void PlayShot(ShotResult result)
        {
            if (result == null || result.Trajectory == null || result.Trajectory.Count == 0)
            {
                Debug.LogWarning("BallController: Cannot play shot with null or empty trajectory");
                return;
            }

            // Stop any existing animation
            StopAnimation();

            _currentShot = result;
            _progress = 0f;
            _apexReached = false;
            _landingTriggered = false;
            _apexHeight = result.MaxHeight;

            // Initialize ball position
            if (_ballTransform != null)
            {
                _ballTransform.position = _teePosition;
            }

            // Initialize spinner with shot spin data
            if (_ballSpinner != null && result.LaunchData != null)
            {
                _ballSpinner.Initialize(result.LaunchData.BackSpin, result.LaunchData.SideSpin);
            }

            // Enable trail
            if (_ballVisuals != null)
            {
                _ballVisuals.ClearTrail();
                _ballVisuals.SetTrailEnabled(true);
            }

            // Start animation based on time scale
            if (_timeScale == TimeScale.Instant)
            {
                SkipToEnd();
            }
            else
            {
                _animationCoroutine = StartCoroutine(AnimateTrajectory());
            }
        }

        /// <summary>
        /// Skip to the end of the current animation.
        /// </summary>
        public void SkipToEnd()
        {
            if (_currentShot == null || _currentShot.Trajectory.Count == 0)
            {
                return;
            }

            // Stop coroutine if running
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }

            // Get final position
            var lastPoint = _currentShot.Trajectory[_currentShot.Trajectory.Count - 1];
            Vector3 finalPosition = TrajectoryPointToWorldPosition(lastPoint);

            if (_ballTransform != null)
            {
                _ballTransform.position = finalPosition;
            }
            _progress = 1f;

            // Stop spinner
            if (_ballSpinner != null)
            {
                _ballSpinner.Stop();
            }

            // Disable trail emitting but keep visible
            if (_ballVisuals != null)
            {
                _ballVisuals.SetTrailEnabled(false);
            }

            // Fire events
            if (!_apexReached)
            {
                OnApexReached?.Invoke(_currentShot.MaxHeight);
            }

            if (!_landingTriggered)
            {
                var landingPoint = _currentShot.GetLandingPoint();
                if (landingPoint != null)
                {
                    OnLanded?.Invoke(TrajectoryPointToWorldPosition(landingPoint));
                }
            }

            SetPhase(AnimationPhase.Stopped);
            OnStopped?.Invoke(finalPosition);
        }

        /// <summary>
        /// Reset the ball to the tee position.
        /// </summary>
        public void Reset()
        {
            StopAnimation();

            if (_ballTransform != null)
            {
                _ballTransform.position = _teePosition;
                _ballTransform.rotation = Quaternion.identity;
            }
            _currentShot = null;
            _progress = 0f;
            _isPaused = false;
            _currentPointIndex = 0;
            _apexReached = false;
            _landingTriggered = false;

            // Reset visuals
            if (_ballVisuals != null)
            {
                _ballVisuals.ResetVisuals();
            }

            // Reset spinner
            if (_ballSpinner != null)
            {
                _ballSpinner.Stop();
            }

            SetPhase(AnimationPhase.Idle);
        }

        /// <summary>
        /// Pause the current animation.
        /// </summary>
        public void Pause()
        {
            if (_animationCoroutine != null)
            {
                _isPaused = true;
            }
        }

        /// <summary>
        /// Resume a paused animation.
        /// </summary>
        public void Resume()
        {
            _isPaused = false;
        }

        /// <summary>
        /// Stop the current animation completely.
        /// </summary>
        private void StopAnimation()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }

            _isPaused = false;
        }

        /// <summary>
        /// Main animation coroutine that interpolates through trajectory points.
        /// </summary>
        private IEnumerator AnimateTrajectory()
        {
            SetPhase(AnimationPhase.Flight);
            OnFlightStarted?.Invoke();

            var trajectory = _currentShot.Trajectory;
            float totalTime = _currentShot.TotalTime;

            // Get the actual playback speed
            float speedMultiplier = GetEffectivePlaybackSpeed();

            float animationTime = 0f;
            int pointIndex = 0;

            while (pointIndex < trajectory.Count - 1)
            {
                // Handle pause
                while (_isPaused)
                {
                    yield return null;
                }

                // Update speed multiplier in case it changed
                speedMultiplier = GetEffectivePlaybackSpeed();

                // Get current and next points
                var currentPoint = trajectory[pointIndex];
                var nextPoint = trajectory[pointIndex + 1];

                // Calculate interpolation factor
                float segmentDuration = nextPoint.Time - currentPoint.Time;
                float segmentProgress = 0f;

                while (segmentProgress < 1f && !_isPaused)
                {
                    // Update speed in case it changed during animation
                    speedMultiplier = GetEffectivePlaybackSpeed();

                    float deltaTime = Time.deltaTime * speedMultiplier;
                    animationTime += deltaTime;
                    _progress = Mathf.Clamp01(animationTime / totalTime);

                    if (segmentDuration > 0)
                    {
                        segmentProgress += deltaTime / segmentDuration;
                        segmentProgress = Mathf.Clamp01(segmentProgress);
                    }
                    else
                    {
                        segmentProgress = 1f;
                    }

                    // Interpolate position
                    Vector3 currentPos = TrajectoryPointToWorldPosition(currentPoint);
                    Vector3 nextPos = TrajectoryPointToWorldPosition(nextPoint);
                    if (_ballTransform != null)
                    {
                        _ballTransform.position = Vector3.Lerp(currentPos, nextPos, segmentProgress);
                    }

                    // Check for apex
                    CheckApex(currentPoint, nextPoint);

                    // Check for phase changes
                    UpdatePhaseFromTrajectory(nextPoint);

                    yield return null;
                }

                pointIndex++;
                _currentPointIndex = pointIndex;
            }

            // Ensure final position is exact
            if (trajectory.Count > 0 && _ballTransform != null)
            {
                var lastPoint = trajectory[trajectory.Count - 1];
                _ballTransform.position = TrajectoryPointToWorldPosition(lastPoint);
            }

            _progress = 1f;

            // Stop spinner
            if (_ballSpinner != null)
            {
                _ballSpinner.Stop();
            }

            // Disable trail
            if (_ballVisuals != null)
            {
                _ballVisuals.SetTrailEnabled(false);
            }

            SetPhase(AnimationPhase.Stopped);
            OnStopped?.Invoke(_ballTransform != null ? _ballTransform.position : _teePosition);

            _animationCoroutine = null;
        }

        /// <summary>
        /// Get the effective playback speed based on time scale setting.
        /// </summary>
        private float GetEffectivePlaybackSpeed()
        {
            return _timeScale switch
            {
                TimeScale.Realtime => _playbackSpeed,
                TimeScale.Fast => _playbackSpeed * 3f,
                TimeScale.Instant => float.MaxValue,
                _ => _playbackSpeed
            };
        }

        /// <summary>
        /// Convert a trajectory point to world position.
        /// Trajectory uses: X = forward (yards), Y = height (feet), Z = lateral (yards)
        /// </summary>
        private Vector3 TrajectoryPointToWorldPosition(TrajectoryPoint point)
        {
            // Convert from trajectory coordinates (yards/feet) to Unity units (meters)
            return new Vector3(
                point.Position.x * _yardsToUnits + _teePosition.x,
                point.Position.y * _feetToUnits + _teePosition.y,
                point.Position.z * _yardsToUnits + _teePosition.z
            );
        }

        /// <summary>
        /// Check if apex has been reached between two trajectory points.
        /// </summary>
        private void CheckApex(TrajectoryPoint current, TrajectoryPoint next)
        {
            if (_apexReached)
            {
                return;
            }

            // Apex is when height starts decreasing
            if (current.Position.y >= _apexHeight * 0.99f && next.Position.y < current.Position.y)
            {
                _apexReached = true;
                OnApexReached?.Invoke(_apexHeight);
            }
        }

        /// <summary>
        /// Update animation phase based on trajectory point phase.
        /// </summary>
        private void UpdatePhaseFromTrajectory(TrajectoryPoint point)
        {
            AnimationPhase newPhase = point.Phase switch
            {
                Phase.Flight => AnimationPhase.Flight,
                Phase.Bounce => AnimationPhase.Bounce,
                Phase.Rolling => AnimationPhase.Rolling,
                Phase.Stopped => AnimationPhase.Stopped,
                _ => _currentPhase
            };

            // Check for landing event
            if (!_landingTriggered && (point.Phase == Phase.Bounce || point.Phase == Phase.Rolling))
            {
                _landingTriggered = true;
                OnLanded?.Invoke(_ballTransform != null ? _ballTransform.position : _teePosition);
            }

            // Check for roll start event
            if (_currentPhase != AnimationPhase.Rolling && newPhase == AnimationPhase.Rolling)
            {
                OnRollStarted?.Invoke();
            }

            if (newPhase != _currentPhase)
            {
                SetPhase(newPhase);
            }
        }

        /// <summary>
        /// Set the current animation phase and fire event.
        /// </summary>
        private void SetPhase(AnimationPhase phase)
        {
            if (_currentPhase != phase)
            {
                _currentPhase = phase;
                OnPhaseChanged?.Invoke(phase);
            }
        }

        /// <summary>
        /// Get the ball position at a specific time in the trajectory.
        /// </summary>
        /// <param name="time">Time since launch in seconds.</param>
        /// <returns>World position at that time.</returns>
        public Vector3 GetPositionAtTime(float time)
        {
            if (_currentShot == null || _currentShot.Trajectory.Count == 0)
            {
                return _teePosition;
            }

            var trajectory = _currentShot.Trajectory;

            // Find the two points surrounding this time
            for (int i = 0; i < trajectory.Count - 1; i++)
            {
                if (time >= trajectory[i].Time && time <= trajectory[i + 1].Time)
                {
                    float t = (time - trajectory[i].Time) / (trajectory[i + 1].Time - trajectory[i].Time);
                    Vector3 pos1 = TrajectoryPointToWorldPosition(trajectory[i]);
                    Vector3 pos2 = TrajectoryPointToWorldPosition(trajectory[i + 1]);
                    return Vector3.Lerp(pos1, pos2, t);
                }
            }

            // Beyond trajectory, return last point
            return TrajectoryPointToWorldPosition(trajectory[trajectory.Count - 1]);
        }

        /// <summary>
        /// Set the BallVisuals reference.
        /// </summary>
        public void SetBallVisuals(BallVisuals visuals)
        {
            _ballVisuals = visuals;
        }

        /// <summary>
        /// Set the BallSpinner reference.
        /// </summary>
        public void SetBallSpinner(BallSpinner spinner)
        {
            _ballSpinner = spinner;
        }

        /// <summary>
        /// Set the ball transform reference.
        /// </summary>
        public void SetBallTransform(Transform ballTransform)
        {
            _ballTransform = ballTransform;
        }
    }

    /// <summary>
    /// Animation phase for ball flight.
    /// </summary>
    public enum AnimationPhase
    {
        /// <summary>No animation active.</summary>
        Idle,
        /// <summary>Ball is in flight.</summary>
        Flight,
        /// <summary>Ball is bouncing.</summary>
        Bounce,
        /// <summary>Ball is rolling.</summary>
        Rolling,
        /// <summary>Ball has stopped.</summary>
        Stopped
    }

    /// <summary>
    /// Time scale options for playback.
    /// </summary>
    public enum TimeScale
    {
        /// <summary>Play at real time (affected by PlaybackSpeed).</summary>
        Realtime,
        /// <summary>Play faster (3x base speed).</summary>
        Fast,
        /// <summary>Skip to end immediately.</summary>
        Instant
    }
}
