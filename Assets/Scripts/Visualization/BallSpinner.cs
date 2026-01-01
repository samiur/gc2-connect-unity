// ABOUTME: Handles golf ball rotation during flight based on spin data from GC2.
// ABOUTME: Calculates rotation from backspin and sidespin, applying decay over time.

using UnityEngine;
using OpenRange.Physics;

namespace OpenRange.Visualization
{
    /// <summary>
    /// Handles ball rotation during flight based on spin data.
    /// Calculates the rotation axis from backspin and sidespin, and applies
    /// rotation with decay over time for realistic visual spin.
    /// </summary>
    public class BallSpinner : MonoBehaviour
    {
        [Header("Spin Settings")]
        [SerializeField] private float _visualSpinMultiplier = 0.1f;
        [SerializeField] private float _spinDecayRate = 0.1f;
        [SerializeField] private bool _enableSpinVisualization = true;

        private float _backSpin;
        private float _sideSpin;
        private float _initialBackSpin;
        private float _initialSideSpin;
        private float _elapsedTime;
        private bool _isSpinning;
        private Vector3 _spinAxis;
        private float _spinRate;

        /// <summary>
        /// Whether the ball is currently spinning.
        /// </summary>
        public bool IsSpinning => _isSpinning;

        /// <summary>
        /// Current backspin in RPM (after decay).
        /// </summary>
        public float CurrentBackSpin => _backSpin;

        /// <summary>
        /// Current sidespin in RPM (after decay).
        /// </summary>
        public float CurrentSideSpin => _sideSpin;

        /// <summary>
        /// Initial backspin value before decay.
        /// </summary>
        public float InitialBackSpin => _initialBackSpin;

        /// <summary>
        /// Initial sidespin value before decay.
        /// </summary>
        public float InitialSideSpin => _initialSideSpin;

        /// <summary>
        /// The calculated spin axis in world space.
        /// </summary>
        public Vector3 SpinAxis => _spinAxis;

        /// <summary>
        /// Current spin rate in radians per second.
        /// </summary>
        public float SpinRate => _spinRate;

        /// <summary>
        /// Whether spin visualization is enabled.
        /// </summary>
        public bool IsVisualizationEnabled => _enableSpinVisualization;

        private void Update()
        {
            if (_isSpinning && _enableSpinVisualization)
            {
                UpdateRotation(Time.deltaTime);
            }
        }

        /// <summary>
        /// Initialize the spinner with backspin and sidespin values.
        /// </summary>
        /// <param name="backSpin">Backspin in RPM (positive = backspin).</param>
        /// <param name="sideSpin">Sidespin in RPM (positive = fade/slice, curves right).</param>
        public void Initialize(float backSpin, float sideSpin)
        {
            _initialBackSpin = backSpin;
            _initialSideSpin = sideSpin;
            _backSpin = backSpin;
            _sideSpin = sideSpin;
            _elapsedTime = 0f;
            _isSpinning = true;

            CalculateSpinAxis();
        }

        /// <summary>
        /// Calculate the spin axis from backspin and sidespin components.
        /// </summary>
        private void CalculateSpinAxis()
        {
            // In Unity coordinate system:
            // - Backspin rotates around the Z axis (lateral axis, perpendicular to forward)
            // - Sidespin rotates around the Y axis (vertical)
            //
            // The spin axis is the combined result of these two components.
            // For a pure backspin, the axis points along +Z (right when viewed from behind)
            // For a pure sidespin (fade), the axis tilts toward -Y

            // Convert RPM to radians per second
            float backSpinRadS = UnitConversions.RpmToRadS(_backSpin);
            float sideSpinRadS = UnitConversions.RpmToRadS(_sideSpin);

            // Spin vector components (magnitude is angular velocity)
            // Backspin contributes to Z component, sidespin to -Y (negative because
            // positive sidespin = fade = curves right = tilt axis left)
            Vector3 spinVector = new Vector3(0f, -sideSpinRadS, backSpinRadS);

            if (spinVector.sqrMagnitude > 0.0001f)
            {
                _spinRate = spinVector.magnitude;
                _spinAxis = spinVector.normalized;
            }
            else
            {
                _spinRate = 0f;
                _spinAxis = Vector3.forward;
            }
        }

        /// <summary>
        /// Get the rotation quaternion for a given elapsed time.
        /// </summary>
        /// <param name="time">Time since launch in seconds.</param>
        /// <returns>Rotation quaternion for the ball.</returns>
        public Quaternion CalculateRotation(float time)
        {
            if (!_isSpinning || _spinRate < 0.001f)
            {
                return Quaternion.identity;
            }

            // Apply decay to get effective spin at this time
            float decayFactor = Mathf.Exp(-_spinDecayRate * time);
            float effectiveSpinRate = _spinRate * decayFactor * _visualSpinMultiplier;

            // Calculate total rotation angle
            // Integral of exp(-k*t) from 0 to t is (1 - exp(-k*t)) / k
            float totalAngle;
            if (_spinDecayRate > 0.0001f)
            {
                totalAngle = _spinRate * _visualSpinMultiplier *
                             (1f - Mathf.Exp(-_spinDecayRate * time)) / _spinDecayRate;
            }
            else
            {
                // No decay, linear accumulation
                totalAngle = _spinRate * _visualSpinMultiplier * time;
            }

            // Convert to degrees
            float angleDegrees = totalAngle * Mathf.Rad2Deg;

            return Quaternion.AngleAxis(angleDegrees, _spinAxis);
        }

        /// <summary>
        /// Update ball rotation based on delta time.
        /// Call this each frame during flight animation.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update.</param>
        public void UpdateRotation(float deltaTime)
        {
            if (!_isSpinning)
            {
                return;
            }

            _elapsedTime += deltaTime;

            // Apply spin decay
            float decayFactor = Mathf.Exp(-_spinDecayRate * _elapsedTime);
            _backSpin = _initialBackSpin * decayFactor;
            _sideSpin = _initialSideSpin * decayFactor;

            // Update spin axis and rate
            CalculateSpinAxis();

            // Apply rotation
            if (_spinRate > 0.001f)
            {
                float rotationAngle = _spinRate * _visualSpinMultiplier * deltaTime * Mathf.Rad2Deg;
                transform.Rotate(_spinAxis, rotationAngle, Space.World);
            }
        }

        /// <summary>
        /// Stop the spin and reset state.
        /// </summary>
        public void Stop()
        {
            _isSpinning = false;
            _backSpin = 0f;
            _sideSpin = 0f;
            _spinRate = 0f;
            _elapsedTime = 0f;
        }

        /// <summary>
        /// Reset to initial spin values and restart.
        /// </summary>
        public void Reset()
        {
            _backSpin = _initialBackSpin;
            _sideSpin = _initialSideSpin;
            _elapsedTime = 0f;
            _isSpinning = true;
            CalculateSpinAxis();
        }

        /// <summary>
        /// Set whether spin visualization is enabled.
        /// </summary>
        /// <param name="enabled">True to enable visual rotation.</param>
        public void SetVisualizationEnabled(bool enabled)
        {
            _enableSpinVisualization = enabled;
        }

        /// <summary>
        /// Get the spin decay factor at a given time.
        /// </summary>
        /// <param name="time">Time since launch in seconds.</param>
        /// <returns>Decay factor (0 to 1).</returns>
        public float GetDecayFactor(float time)
        {
            return Mathf.Exp(-_spinDecayRate * time);
        }
    }
}
