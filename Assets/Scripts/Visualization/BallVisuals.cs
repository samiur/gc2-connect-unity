// ABOUTME: Component that manages golf ball visual state including trail and spin visualization.
// ABOUTME: Attached to GolfBall prefab and controlled by BallController during animation.

using System;
using UnityEngine;

namespace OpenRange.Visualization
{
    /// <summary>
    /// Manages visual state of the golf ball including trail renderer and spin visualization.
    /// This component is attached to the GolfBall prefab and provides methods for
    /// controlling visual elements during flight animation.
    /// </summary>
    public class BallVisuals : MonoBehaviour
    {
        [Header("Trail Settings")]
        [SerializeField] private TrailRenderer _trailRenderer;
        [SerializeField] private bool _trailEnabledByDefault = true;

        [Header("Spin Visualization")]
        [SerializeField] private Transform _spinIndicator;
        [SerializeField] private bool _showSpinByDefault = false;
        [SerializeField] private float _spinVisualizationScale = 1f;

        [Header("Quality Settings")]
        [SerializeField] private int _trailVerticesHigh = 30;
        [SerializeField] private int _trailVerticesLow = 10;
        [SerializeField] private float _trailTimeHigh = 1.5f;
        [SerializeField] private float _trailTimeLow = 0.8f;

        private QualityTier _currentQualityTier = QualityTier.High;
        private Vector3 _currentSpinAxis;
        private float _currentSpinRpm;
        private bool _isSpinActive;

        /// <summary>
        /// Whether the trail renderer is currently enabled.
        /// </summary>
        public bool IsTrailEnabled => _trailRenderer != null && _trailRenderer.enabled;

        /// <summary>
        /// Whether spin visualization is currently active.
        /// </summary>
        public bool IsSpinVisualizationActive => _isSpinActive;

        /// <summary>
        /// Current quality tier affecting visual complexity.
        /// </summary>
        public QualityTier CurrentQualityTier => _currentQualityTier;

        /// <summary>
        /// The trail renderer component reference.
        /// </summary>
        public TrailRenderer TrailRenderer => _trailRenderer;

        /// <summary>
        /// The spin indicator transform reference.
        /// </summary>
        public Transform SpinIndicator => _spinIndicator;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            ApplyDefaultState();
        }

        private void Update()
        {
            if (_isSpinActive && _spinIndicator != null)
            {
                UpdateSpinRotation();
            }
        }

        /// <summary>
        /// Initialize component references if not set in inspector.
        /// </summary>
        private void InitializeComponents()
        {
            if (_trailRenderer == null)
            {
                _trailRenderer = GetComponentInChildren<TrailRenderer>();
            }

            if (_spinIndicator == null)
            {
                _spinIndicator = transform.Find("SpinIndicator");
            }
        }

        /// <summary>
        /// Apply default visual state.
        /// </summary>
        private void ApplyDefaultState()
        {
            SetTrailEnabled(_trailEnabledByDefault);

            if (_spinIndicator != null)
            {
                _spinIndicator.gameObject.SetActive(_showSpinByDefault);
            }
        }

        /// <summary>
        /// Enable or disable the trail renderer.
        /// </summary>
        /// <param name="enabled">Whether to enable the trail.</param>
        public void SetTrailEnabled(bool enabled)
        {
            if (_trailRenderer != null)
            {
                _trailRenderer.enabled = enabled;
                _trailRenderer.emitting = enabled;
            }
        }

        /// <summary>
        /// Set up spin visualization with axis and speed.
        /// </summary>
        /// <param name="spinAxis">The axis of rotation (normalized world direction).</param>
        /// <param name="rpm">Spin rate in rotations per minute.</param>
        public void SetSpinVisualization(Vector3 spinAxis, float rpm)
        {
            _currentSpinAxis = spinAxis.normalized;
            _currentSpinRpm = rpm;
            _isSpinActive = true;

            if (_spinIndicator != null)
            {
                _spinIndicator.gameObject.SetActive(true);

                // Orient the indicator along the spin axis
                if (_currentSpinAxis.sqrMagnitude > 0.001f)
                {
                    _spinIndicator.rotation = Quaternion.LookRotation(_currentSpinAxis);
                }
            }
        }

        /// <summary>
        /// Stop spin visualization.
        /// </summary>
        public void StopSpinVisualization()
        {
            _isSpinActive = false;
            _currentSpinRpm = 0f;

            if (_spinIndicator != null)
            {
                _spinIndicator.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Update the spin indicator rotation based on current spin rate.
        /// </summary>
        private void UpdateSpinRotation()
        {
            if (_spinIndicator == null || _currentSpinRpm <= 0f)
            {
                return;
            }

            // Convert RPM to degrees per second
            float degreesPerSecond = (_currentSpinRpm / 60f) * 360f * _spinVisualizationScale;

            // Rotate around the spin axis
            _spinIndicator.Rotate(_currentSpinAxis, degreesPerSecond * Time.deltaTime, Space.World);
        }

        /// <summary>
        /// Reset all visuals to their initial state.
        /// </summary>
        public void ResetVisuals()
        {
            // Clear trail
            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
                SetTrailEnabled(_trailEnabledByDefault);
            }

            // Stop spin
            StopSpinVisualization();

            // Reset spin indicator rotation
            if (_spinIndicator != null)
            {
                _spinIndicator.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Set the quality tier which affects trail complexity.
        /// </summary>
        /// <param name="tier">The quality tier to apply.</param>
        public void SetQualityTier(QualityTier tier)
        {
            _currentQualityTier = tier;
            ApplyQualitySettings();
        }

        /// <summary>
        /// Apply quality settings to visual components.
        /// </summary>
        private void ApplyQualitySettings()
        {
            if (_trailRenderer == null)
            {
                return;
            }

            switch (_currentQualityTier)
            {
                case QualityTier.High:
                    _trailRenderer.numCornerVertices = _trailVerticesHigh;
                    _trailRenderer.numCapVertices = _trailVerticesHigh / 2;
                    _trailRenderer.time = _trailTimeHigh;
                    break;

                case QualityTier.Medium:
                    int mediumVertices = (_trailVerticesHigh + _trailVerticesLow) / 2;
                    _trailRenderer.numCornerVertices = mediumVertices;
                    _trailRenderer.numCapVertices = mediumVertices / 2;
                    _trailRenderer.time = (_trailTimeHigh + _trailTimeLow) / 2f;
                    break;

                case QualityTier.Low:
                    _trailRenderer.numCornerVertices = _trailVerticesLow;
                    _trailRenderer.numCapVertices = _trailVerticesLow / 2;
                    _trailRenderer.time = _trailTimeLow;
                    break;
            }
        }

        /// <summary>
        /// Clear the trail immediately.
        /// </summary>
        public void ClearTrail()
        {
            if (_trailRenderer != null)
            {
                _trailRenderer.Clear();
            }
        }

        /// <summary>
        /// Get the current spin rate in RPM.
        /// </summary>
        /// <returns>Current spin rate.</returns>
        public float GetCurrentSpinRpm() => _currentSpinRpm;

        /// <summary>
        /// Get the current spin axis.
        /// </summary>
        /// <returns>Current spin axis direction.</returns>
        public Vector3 GetCurrentSpinAxis() => _currentSpinAxis;
    }

    /// <summary>
    /// Quality tiers for visual complexity.
    /// </summary>
    public enum QualityTier
    {
        Low,
        Medium,
        High
    }
}
