// ABOUTME: Renders golf ball trajectory as a visible line using LineRenderer.
// ABOUTME: Supports actual and predicted paths with quality tier adjustments.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenRange.Physics;

namespace OpenRange.Visualization
{
    /// <summary>
    /// Renders the ball flight trajectory as a visible line.
    /// Supports showing actual flight path, predicted path, or both.
    /// Integrates with the quality tier system for performance optimization.
    /// </summary>
    public class TrajectoryRenderer : MonoBehaviour
    {
        /// <summary>
        /// Display mode for trajectory visualization.
        /// </summary>
        public enum DisplayMode
        {
            /// <summary>Show only the actual flight path.</summary>
            Actual,
            /// <summary>Show only the predicted path (before shot lands).</summary>
            Predicted,
            /// <summary>Show both predicted and actual paths.</summary>
            Both
        }

        [Header("Line Renderer References")]
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private LineRenderer _predictionLineRenderer;

        [Header("Display Settings")]
        [SerializeField] private DisplayMode _displayMode = DisplayMode.Actual;
        [SerializeField] private float _lineWidthStart = 0.05f;
        [SerializeField] private float _lineWidthEnd = 0.01f;

        [Header("Color Settings")]
        [SerializeField] private Gradient _actualColorGradient;
        [SerializeField] private Gradient _predictionColorGradient;

        [Header("Quality Settings")]
        [SerializeField] private int _vertexCountHigh = 100;
        [SerializeField] private int _vertexCountMedium = 50;
        [SerializeField] private int _vertexCountLow = 20;

        [Header("Coordinate Conversion")]
        [SerializeField] private float _yardsToUnits = 0.9144f;
        [SerializeField] private float _feetToUnits = 0.3048f;
        [SerializeField] private Vector3 _originOffset = Vector3.zero;

        private QualityTier _currentQualityTier = QualityTier.High;
        private ShotResult _currentShot;
        private Coroutine _fadeCoroutine;
        private bool _isVisible;
        private bool _isPredictionVisible;

        /// <summary>
        /// Current display mode.
        /// </summary>
        public DisplayMode CurrentDisplayMode
        {
            get => _displayMode;
            set => _displayMode = value;
        }

        /// <summary>
        /// Whether the trajectory line is currently visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Whether the prediction line is currently visible.
        /// </summary>
        public bool IsPredictionVisible => _isPredictionVisible;

        /// <summary>
        /// Current quality tier affecting vertex count.
        /// </summary>
        public QualityTier CurrentQualityTier => _currentQualityTier;

        /// <summary>
        /// The main line renderer reference.
        /// </summary>
        public LineRenderer LineRenderer => _lineRenderer;

        /// <summary>
        /// The prediction line renderer reference.
        /// </summary>
        public LineRenderer PredictionLineRenderer => _predictionLineRenderer;

        /// <summary>
        /// Gets the current vertex count based on quality tier.
        /// </summary>
        public int CurrentVertexCount
        {
            get
            {
                return _currentQualityTier switch
                {
                    QualityTier.High => _vertexCountHigh,
                    QualityTier.Medium => _vertexCountMedium,
                    QualityTier.Low => _vertexCountLow,
                    _ => _vertexCountMedium
                };
            }
        }

        /// <summary>
        /// The origin offset for coordinate conversion.
        /// </summary>
        public Vector3 OriginOffset
        {
            get => _originOffset;
            set => _originOffset = value;
        }

        private void Awake()
        {
            InitializeComponents();
            InitializeDefaultGradients();
        }

        /// <summary>
        /// Initialize component references if not set in inspector.
        /// </summary>
        private void InitializeComponents()
        {
            if (_lineRenderer == null)
            {
                _lineRenderer = GetComponent<LineRenderer>();
            }

            // Hide lines by default
            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = false;
            }

            if (_predictionLineRenderer != null)
            {
                _predictionLineRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Initialize default color gradients if not set.
        /// </summary>
        private void InitializeDefaultGradients()
        {
            if (_actualColorGradient == null || _actualColorGradient.colorKeys.Length == 0)
            {
                _actualColorGradient = CreateDefaultActualGradient();
            }

            if (_predictionColorGradient == null || _predictionColorGradient.colorKeys.Length == 0)
            {
                _predictionColorGradient = CreateDefaultPredictionGradient();
            }
        }

        /// <summary>
        /// Create the default gradient for actual trajectory (white to cyan).
        /// </summary>
        private Gradient CreateDefaultActualGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.5f, 1f, 1f), 0.5f),
                    new GradientColorKey(Color.cyan, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.8f),
                    new GradientAlphaKey(0.5f, 1f)
                }
            );
            return gradient;
        }

        /// <summary>
        /// Create the default gradient for prediction trajectory (yellow-orange).
        /// </summary>
        private Gradient CreateDefaultPredictionGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(Color.yellow, 0f),
                    new GradientColorKey(new Color(1f, 0.5f, 0f), 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.8f, 0f),
                    new GradientAlphaKey(0.3f, 1f)
                }
            );
            return gradient;
        }

        /// <summary>
        /// Show the actual trajectory from shot result.
        /// </summary>
        /// <param name="result">The shot result containing trajectory data.</param>
        public void ShowTrajectory(ShotResult result)
        {
            if (result == null || result.Trajectory == null || result.Trajectory.Count == 0)
            {
                Debug.LogWarning("TrajectoryRenderer: Cannot show trajectory with null or empty data");
                return;
            }

            // Cancel any fade in progress
            StopFadeCoroutine();

            _currentShot = result;

            if (_lineRenderer != null)
            {
                var positions = SampleTrajectoryPoints(result.Trajectory);
                ApplyPositionsToLineRenderer(_lineRenderer, positions, _actualColorGradient);
                _lineRenderer.enabled = true;
                _isVisible = true;
            }
        }

        /// <summary>
        /// Show predicted trajectory path (different visual style).
        /// </summary>
        /// <param name="result">The shot result containing predicted trajectory.</param>
        public void ShowPrediction(ShotResult result)
        {
            if (result == null || result.Trajectory == null || result.Trajectory.Count == 0)
            {
                Debug.LogWarning("TrajectoryRenderer: Cannot show prediction with null or empty data");
                return;
            }

            _currentShot = result;

            // Use prediction line renderer if available, otherwise use main line
            var lineRenderer = _predictionLineRenderer != null ? _predictionLineRenderer : _lineRenderer;

            if (lineRenderer != null)
            {
                var positions = SampleTrajectoryPoints(result.Trajectory);
                ApplyPositionsToLineRenderer(lineRenderer, positions, _predictionColorGradient);
                lineRenderer.enabled = true;

                if (_predictionLineRenderer != null)
                {
                    _isPredictionVisible = true;
                }
                else
                {
                    _isVisible = true;
                }
            }
        }

        /// <summary>
        /// Hide the trajectory immediately.
        /// </summary>
        public void Hide()
        {
            StopFadeCoroutine();

            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = false;
            }

            if (_predictionLineRenderer != null)
            {
                _predictionLineRenderer.enabled = false;
            }

            _isVisible = false;
            _isPredictionVisible = false;
        }

        /// <summary>
        /// Hide only the prediction line.
        /// </summary>
        public void HidePrediction()
        {
            if (_predictionLineRenderer != null)
            {
                _predictionLineRenderer.enabled = false;
            }

            _isPredictionVisible = false;
        }

        /// <summary>
        /// Fade out the trajectory over a duration.
        /// </summary>
        /// <param name="duration">Duration of the fade in seconds.</param>
        public void FadeOut(float duration)
        {
            if (!_isVisible && !_isPredictionVisible)
            {
                return;
            }

            StopFadeCoroutine();
            _fadeCoroutine = StartCoroutine(FadeOutCoroutine(duration));
        }

        /// <summary>
        /// Coroutine to fade out the trajectory line.
        /// </summary>
        private IEnumerator FadeOutCoroutine(float duration)
        {
            if (duration <= 0f)
            {
                Hide();
                yield break;
            }

            float elapsed = 0f;
            float startAlpha = 1f;

            // Store original gradients
            Gradient originalActualGradient = _lineRenderer != null ? CloneGradient(_lineRenderer.colorGradient) : null;
            Gradient originalPredictionGradient = _predictionLineRenderer != null ? CloneGradient(_predictionLineRenderer.colorGradient) : null;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float alpha = Mathf.Lerp(startAlpha, 0f, t);

                // Apply faded gradient to main line
                if (_lineRenderer != null && _isVisible && originalActualGradient != null)
                {
                    var fadedGradient = CreateFadedGradient(originalActualGradient, alpha);
                    _lineRenderer.colorGradient = fadedGradient;
                }

                // Apply faded gradient to prediction line
                if (_predictionLineRenderer != null && _isPredictionVisible && originalPredictionGradient != null)
                {
                    var fadedGradient = CreateFadedGradient(originalPredictionGradient, alpha);
                    _predictionLineRenderer.colorGradient = fadedGradient;
                }

                yield return null;
            }

            Hide();

            // Restore original gradients after hiding
            if (_lineRenderer != null && originalActualGradient != null)
            {
                _lineRenderer.colorGradient = originalActualGradient;
            }

            if (_predictionLineRenderer != null && originalPredictionGradient != null)
            {
                _predictionLineRenderer.colorGradient = originalPredictionGradient;
            }

            _fadeCoroutine = null;
        }

        /// <summary>
        /// Clone a gradient.
        /// </summary>
        private Gradient CloneGradient(Gradient original)
        {
            var clone = new Gradient();
            clone.SetKeys(original.colorKeys, original.alphaKeys);
            return clone;
        }

        /// <summary>
        /// Create a gradient with adjusted alpha.
        /// </summary>
        private Gradient CreateFadedGradient(Gradient original, float alphaMultiplier)
        {
            var faded = new Gradient();

            var alphaKeys = new GradientAlphaKey[original.alphaKeys.Length];
            for (int i = 0; i < original.alphaKeys.Length; i++)
            {
                alphaKeys[i] = new GradientAlphaKey(
                    original.alphaKeys[i].alpha * alphaMultiplier,
                    original.alphaKeys[i].time
                );
            }

            faded.SetKeys(original.colorKeys, alphaKeys);
            return faded;
        }

        /// <summary>
        /// Stop any fade coroutine in progress.
        /// </summary>
        private void StopFadeCoroutine()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
        }

        /// <summary>
        /// Set the quality tier which affects vertex count.
        /// </summary>
        /// <param name="tier">The quality tier to apply.</param>
        public void SetQualityTier(QualityTier tier)
        {
            _currentQualityTier = tier;

            // Re-render current trajectory if visible
            if (_isVisible && _currentShot != null)
            {
                ShowTrajectory(_currentShot);
            }
        }

        /// <summary>
        /// Sample trajectory points to create a smooth line with target vertex count.
        /// </summary>
        private Vector3[] SampleTrajectoryPoints(List<TrajectoryPoint> trajectory)
        {
            int targetCount = CurrentVertexCount;

            if (trajectory.Count <= targetCount)
            {
                // No need to resample, use all points
                return ConvertToWorldPositions(trajectory);
            }

            // Resample to target count
            var positions = new Vector3[targetCount];
            float step = (float)(trajectory.Count - 1) / (targetCount - 1);

            for (int i = 0; i < targetCount; i++)
            {
                float index = i * step;
                int lowIndex = Mathf.FloorToInt(index);
                int highIndex = Mathf.CeilToInt(index);
                highIndex = Mathf.Min(highIndex, trajectory.Count - 1);

                if (lowIndex == highIndex)
                {
                    positions[i] = TrajectoryPointToWorldPosition(trajectory[lowIndex]);
                }
                else
                {
                    float t = index - lowIndex;
                    Vector3 lowPos = TrajectoryPointToWorldPosition(trajectory[lowIndex]);
                    Vector3 highPos = TrajectoryPointToWorldPosition(trajectory[highIndex]);
                    positions[i] = Vector3.Lerp(lowPos, highPos, t);
                }
            }

            return positions;
        }

        /// <summary>
        /// Convert all trajectory points to world positions.
        /// </summary>
        private Vector3[] ConvertToWorldPositions(List<TrajectoryPoint> trajectory)
        {
            var positions = new Vector3[trajectory.Count];
            for (int i = 0; i < trajectory.Count; i++)
            {
                positions[i] = TrajectoryPointToWorldPosition(trajectory[i]);
            }
            return positions;
        }

        /// <summary>
        /// Convert a trajectory point to world position.
        /// Trajectory uses: X = forward (yards), Y = height (feet), Z = lateral (yards)
        /// Unity uses: X = right (lateral), Y = up (height), Z = forward (distance)
        /// </summary>
        private Vector3 TrajectoryPointToWorldPosition(TrajectoryPoint point)
        {
            // Swap X/Z: Physics X (forward) → Unity Z, Physics Z (lateral) → Unity X
            return new Vector3(
                point.Position.z * _yardsToUnits + _originOffset.x,
                point.Position.y * _feetToUnits + _originOffset.y,
                point.Position.x * _yardsToUnits + _originOffset.z
            );
        }

        /// <summary>
        /// Apply positions to a line renderer.
        /// </summary>
        private void ApplyPositionsToLineRenderer(LineRenderer lineRenderer, Vector3[] positions, Gradient colorGradient)
        {
            lineRenderer.positionCount = positions.Length;
            lineRenderer.SetPositions(positions);
            lineRenderer.startWidth = _lineWidthStart;
            lineRenderer.endWidth = _lineWidthEnd;
            lineRenderer.colorGradient = colorGradient;
        }

        /// <summary>
        /// Set the actual color gradient.
        /// </summary>
        public void SetActualColorGradient(Gradient gradient)
        {
            _actualColorGradient = gradient;
        }

        /// <summary>
        /// Set the prediction color gradient.
        /// </summary>
        public void SetPredictionColorGradient(Gradient gradient)
        {
            _predictionColorGradient = gradient;
        }

        /// <summary>
        /// Set the line renderer reference.
        /// </summary>
        public void SetLineRenderer(LineRenderer lineRenderer)
        {
            _lineRenderer = lineRenderer;
        }

        /// <summary>
        /// Set the prediction line renderer reference.
        /// </summary>
        public void SetPredictionLineRenderer(LineRenderer lineRenderer)
        {
            _predictionLineRenderer = lineRenderer;
        }

        /// <summary>
        /// Get the world position at a normalized progress (0-1) along the trajectory.
        /// </summary>
        /// <param name="progress">Normalized progress (0 = start, 1 = end).</param>
        /// <returns>World position at that point.</returns>
        public Vector3 GetPositionAtProgress(float progress)
        {
            if (_currentShot == null || _currentShot.Trajectory == null || _currentShot.Trajectory.Count == 0)
            {
                return _originOffset;
            }

            var trajectory = _currentShot.Trajectory;
            progress = Mathf.Clamp01(progress);

            float index = progress * (trajectory.Count - 1);
            int lowIndex = Mathf.FloorToInt(index);
            int highIndex = Mathf.CeilToInt(index);
            highIndex = Mathf.Min(highIndex, trajectory.Count - 1);

            if (lowIndex == highIndex)
            {
                return TrajectoryPointToWorldPosition(trajectory[lowIndex]);
            }

            float t = index - lowIndex;
            Vector3 lowPos = TrajectoryPointToWorldPosition(trajectory[lowIndex]);
            Vector3 highPos = TrajectoryPointToWorldPosition(trajectory[highIndex]);
            return Vector3.Lerp(lowPos, highPos, t);
        }
    }
}
