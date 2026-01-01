// ABOUTME: Visual marker component that displays landing position and distance information.
// ABOUTME: Shows ring graphic and distance text with fade-out animation and quality tier support.

using System;
using System.Collections;
using UnityEngine;
using TMPro;

namespace OpenRange.Visualization
{
    /// <summary>
    /// Displays a visual marker at the ball landing position.
    /// Shows a ring/target graphic with distance text that can fade out over time.
    /// Supports quality tier adjustments for visual complexity.
    /// </summary>
    public class LandingMarker : MonoBehaviour
    {
        [Header("Visual References")]
        [SerializeField] private Transform _ringTransform;
        [SerializeField] private Renderer _ringRenderer;
        [SerializeField] private TextMeshPro _carryDistanceText;
        [SerializeField] private TextMeshPro _totalDistanceText;

        [Header("Display Settings")]
        [SerializeField] private float _ringDiameter = 3f;
        [SerializeField] private float _heightOffset = 0.05f;
        [SerializeField] private bool _showTotalDistance = true;
        [SerializeField] private string _distanceFormat = "{0:F1} yd";

        [Header("Animation Settings")]
        [SerializeField] private float _fadeInDuration = 0.3f;
        [SerializeField] private float _autoHideDuration = 5f;
        [SerializeField] private float _fadeOutDuration = 1f;
        [SerializeField] private AnimationCurve _fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve _fadeOutCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        [Header("Quality Settings")]
        [SerializeField] private float _fadeOutDurationHigh = 1f;
        [SerializeField] private float _fadeOutDurationLow = 0.5f;

        private QualityTier _currentQualityTier = QualityTier.High;
        private Coroutine _animationCoroutine;
        private Color _originalRingColor = Color.white;
        private Color _originalCarryTextColor = Color.white;
        private Color _originalTotalTextColor = Color.white;
        private bool _isVisible;
        private float _carryDistance;
        private float _totalDistance;

        /// <summary>
        /// Whether the marker is currently visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// The carry distance displayed by this marker.
        /// </summary>
        public float CarryDistance => _carryDistance;

        /// <summary>
        /// The total distance displayed by this marker.
        /// </summary>
        public float TotalDistance => _totalDistance;

        /// <summary>
        /// Current quality tier affecting visual complexity.
        /// </summary>
        public QualityTier CurrentQualityTier => _currentQualityTier;

        /// <summary>
        /// The ring transform for external access.
        /// </summary>
        public Transform RingTransform => _ringTransform;

        /// <summary>
        /// Event fired when the marker finishes fading out.
        /// </summary>
        public event Action OnFadeOutComplete;

        /// <summary>
        /// Event fired when the marker is shown.
        /// </summary>
        public event Action OnShown;

        /// <summary>
        /// Event fired when the marker is hidden.
        /// </summary>
        public event Action OnHidden;

        private void Awake()
        {
            InitializeReferences();
            CacheOriginalColors();
            SetVisibility(false);
        }

        /// <summary>
        /// Initialize component references if not set in inspector.
        /// </summary>
        private void InitializeReferences()
        {
            if (_ringTransform == null)
            {
                _ringTransform = transform.Find("Ring");
            }

            if (_ringRenderer == null && _ringTransform != null)
            {
                _ringRenderer = _ringTransform.GetComponent<Renderer>();
            }

            if (_carryDistanceText == null)
            {
                var carryTextTransform = transform.Find("CarryDistanceText");
                if (carryTextTransform != null)
                {
                    _carryDistanceText = carryTextTransform.GetComponent<TextMeshPro>();
                }
            }

            if (_totalDistanceText == null)
            {
                var totalTextTransform = transform.Find("TotalDistanceText");
                if (totalTextTransform != null)
                {
                    _totalDistanceText = totalTextTransform.GetComponent<TextMeshPro>();
                }
            }
        }

        /// <summary>
        /// Cache the original colors for fade animations.
        /// </summary>
        private void CacheOriginalColors()
        {
            if (_ringRenderer != null && _ringRenderer.material != null)
            {
                _originalRingColor = _ringRenderer.material.color;
            }

            if (_carryDistanceText != null)
            {
                _originalCarryTextColor = _carryDistanceText.color;
            }

            if (_totalDistanceText != null)
            {
                _originalTotalTextColor = _totalDistanceText.color;
            }
        }

        /// <summary>
        /// Show the marker at the specified position with distance information.
        /// </summary>
        /// <param name="position">World position for the marker.</param>
        /// <param name="carryDistance">Carry distance in yards.</param>
        /// <param name="totalDistance">Total distance in yards (including roll).</param>
        /// <param name="autoHide">Whether to automatically hide after duration.</param>
        public void Show(Vector3 position, float carryDistance, float totalDistance, bool autoHide = true)
        {
            StopAnimationCoroutine();

            _carryDistance = carryDistance;
            _totalDistance = totalDistance;

            // Position the marker at landing position with height offset
            transform.position = new Vector3(position.x, position.y + _heightOffset, position.z);

            // Update distance text
            UpdateDistanceText();

            // Start fade in animation
            _animationCoroutine = StartCoroutine(FadeInCoroutine(autoHide));

            OnShown?.Invoke();
        }

        /// <summary>
        /// Hide the marker immediately without animation.
        /// </summary>
        public void Hide()
        {
            StopAnimationCoroutine();
            SetVisibility(false);
            RestoreOriginalColors();
            _isVisible = false;
            OnHidden?.Invoke();
        }

        /// <summary>
        /// Fade out the marker over a duration.
        /// </summary>
        /// <param name="duration">Duration of the fade. If 0, uses quality-based duration.</param>
        public void FadeOut(float duration = 0f)
        {
            if (!_isVisible)
            {
                return;
            }

            StopAnimationCoroutine();

            float actualDuration = duration > 0f ? duration : GetFadeOutDuration();
            _animationCoroutine = StartCoroutine(FadeOutCoroutine(actualDuration));
        }

        /// <summary>
        /// Update the displayed distance text.
        /// </summary>
        private void UpdateDistanceText()
        {
            if (_carryDistanceText != null)
            {
                _carryDistanceText.text = string.Format(_distanceFormat, _carryDistance);
            }

            if (_totalDistanceText != null)
            {
                bool showTotal = _showTotalDistance && ShouldShowTotalDistance();
                _totalDistanceText.gameObject.SetActive(showTotal);

                if (showTotal)
                {
                    float rollDistance = _totalDistance - _carryDistance;
                    _totalDistanceText.text = string.Format(_distanceFormat, _totalDistance);
                }
            }
        }

        /// <summary>
        /// Determine whether to show total distance based on quality tier.
        /// </summary>
        private bool ShouldShowTotalDistance()
        {
            // On low quality, don't show total distance to reduce visual clutter
            return _currentQualityTier != QualityTier.Low;
        }

        /// <summary>
        /// Get the fade out duration based on quality tier.
        /// </summary>
        private float GetFadeOutDuration()
        {
            return _currentQualityTier switch
            {
                QualityTier.High => _fadeOutDurationHigh,
                QualityTier.Medium => (_fadeOutDurationHigh + _fadeOutDurationLow) / 2f,
                QualityTier.Low => _fadeOutDurationLow,
                _ => _fadeOutDuration
            };
        }

        /// <summary>
        /// Coroutine for fade in animation.
        /// </summary>
        private IEnumerator FadeInCoroutine(bool autoHide)
        {
            SetVisibility(true);
            _isVisible = true;

            float elapsed = 0f;

            while (elapsed < _fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _fadeInDuration;
                float alpha = _fadeInCurve.Evaluate(t);

                ApplyAlpha(alpha);
                yield return null;
            }

            ApplyAlpha(1f);

            if (autoHide && _autoHideDuration > 0f)
            {
                yield return new WaitForSeconds(_autoHideDuration);
                _animationCoroutine = StartCoroutine(FadeOutCoroutine(GetFadeOutDuration()));
            }
            else
            {
                _animationCoroutine = null;
            }
        }

        /// <summary>
        /// Coroutine for fade out animation.
        /// </summary>
        private IEnumerator FadeOutCoroutine(float duration)
        {
            if (duration <= 0f)
            {
                Hide();
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float alpha = _fadeOutCurve.Evaluate(t);

                ApplyAlpha(alpha);
                yield return null;
            }

            ApplyAlpha(0f);
            SetVisibility(false);
            RestoreOriginalColors();
            _isVisible = false;
            _animationCoroutine = null;

            OnFadeOutComplete?.Invoke();
            OnHidden?.Invoke();
        }

        /// <summary>
        /// Apply alpha to all visual components.
        /// </summary>
        private void ApplyAlpha(float alpha)
        {
            if (_ringRenderer != null && _ringRenderer.material != null)
            {
                Color color = _originalRingColor;
                color.a = _originalRingColor.a * alpha;
                _ringRenderer.material.color = color;
            }

            if (_carryDistanceText != null)
            {
                Color color = _originalCarryTextColor;
                color.a = _originalCarryTextColor.a * alpha;
                _carryDistanceText.color = color;
            }

            if (_totalDistanceText != null)
            {
                Color color = _originalTotalTextColor;
                color.a = _originalTotalTextColor.a * alpha;
                _totalDistanceText.color = color;
            }
        }

        /// <summary>
        /// Restore original colors after fade animation.
        /// </summary>
        private void RestoreOriginalColors()
        {
            if (_ringRenderer != null && _ringRenderer.material != null)
            {
                _ringRenderer.material.color = _originalRingColor;
            }

            if (_carryDistanceText != null)
            {
                _carryDistanceText.color = _originalCarryTextColor;
            }

            if (_totalDistanceText != null)
            {
                _totalDistanceText.color = _originalTotalTextColor;
            }
        }

        /// <summary>
        /// Set visibility of all child objects.
        /// </summary>
        private void SetVisibility(bool visible)
        {
            if (_ringTransform != null)
            {
                _ringTransform.gameObject.SetActive(visible);
            }

            if (_carryDistanceText != null)
            {
                _carryDistanceText.gameObject.SetActive(visible);
            }

            if (_totalDistanceText != null && _showTotalDistance && ShouldShowTotalDistance())
            {
                _totalDistanceText.gameObject.SetActive(visible);
            }
            else if (_totalDistanceText != null)
            {
                _totalDistanceText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Stop any running animation coroutine.
        /// </summary>
        private void StopAnimationCoroutine()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }
        }

        /// <summary>
        /// Set the quality tier which affects visual complexity.
        /// </summary>
        /// <param name="tier">The quality tier to apply.</param>
        public void SetQualityTier(QualityTier tier)
        {
            _currentQualityTier = tier;

            // Update total distance visibility based on quality
            if (_totalDistanceText != null && _isVisible)
            {
                bool showTotal = _showTotalDistance && ShouldShowTotalDistance();
                _totalDistanceText.gameObject.SetActive(showTotal);
            }
        }

        /// <summary>
        /// Set the ring renderer reference (for testing).
        /// </summary>
        public void SetRingRenderer(Renderer renderer)
        {
            _ringRenderer = renderer;
            if (renderer != null && renderer.material != null)
            {
                _originalRingColor = renderer.material.color;
            }
        }

        /// <summary>
        /// Set the carry distance text reference (for testing).
        /// </summary>
        public void SetCarryDistanceText(TextMeshPro text)
        {
            _carryDistanceText = text;
            if (text != null)
            {
                _originalCarryTextColor = text.color;
            }
        }

        /// <summary>
        /// Set the total distance text reference (for testing).
        /// </summary>
        public void SetTotalDistanceText(TextMeshPro text)
        {
            _totalDistanceText = text;
            if (text != null)
            {
                _originalTotalTextColor = text.color;
            }
        }

        /// <summary>
        /// Set the ring transform reference (for testing).
        /// </summary>
        public void SetRingTransform(Transform ringTransform)
        {
            _ringTransform = ringTransform;
        }

        /// <summary>
        /// Set whether to show total distance.
        /// </summary>
        public void SetShowTotalDistance(bool show)
        {
            _showTotalDistance = show;
        }

        /// <summary>
        /// Set the distance format string.
        /// </summary>
        public void SetDistanceFormat(string format)
        {
            _distanceFormat = format;
        }

        /// <summary>
        /// Set the auto hide duration.
        /// </summary>
        public void SetAutoHideDuration(float duration)
        {
            _autoHideDuration = duration;
        }

        /// <summary>
        /// Set the fade in duration.
        /// </summary>
        public void SetFadeInDuration(float duration)
        {
            _fadeInDuration = duration;
        }

        /// <summary>
        /// Set the height offset.
        /// </summary>
        public void SetHeightOffset(float offset)
        {
            _heightOffset = offset;
        }
    }
}
