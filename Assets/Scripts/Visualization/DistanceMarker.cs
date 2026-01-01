// ABOUTME: Component for distance marker signs placed at yardage intervals on the driving range.
// ABOUTME: Displays distance text and supports quality tier adjustments for visibility.

using System;
using UnityEngine;
using TMPro;

namespace OpenRange.Visualization
{
    /// <summary>
    /// Visual marker showing distance from the tee position.
    /// Displays a sign post with distance text.
    /// </summary>
    public class DistanceMarker : MonoBehaviour
    {
        [Header("Visual References")]
        [SerializeField] private Transform _postTransform;
        [SerializeField] private Transform _signTransform;
        [SerializeField] private TextMeshPro _distanceText;
        [SerializeField] private Renderer _signRenderer;
        [SerializeField] private Renderer _postRenderer;

        [Header("Display Settings")]
        [SerializeField] private int _distance = 100;
        [SerializeField] private string _distanceFormat = "{0}";
        [SerializeField] private string _unitSuffix = "";
        [SerializeField] private float _textSize = 3f;

        [Header("Quality Settings")]
        [SerializeField] private float _fadeDistanceHigh = 400f;
        [SerializeField] private float _fadeDistanceMedium = 300f;
        [SerializeField] private float _fadeDistanceLow = 150f;

        private QualityTier _currentQualityTier = QualityTier.High;
        private bool _isVisible = true;
        private MaterialPropertyBlock _propertyBlock;
        private Camera _mainCamera;

        /// <summary>
        /// Gets the property block, initializing lazily if needed.
        /// </summary>
        private MaterialPropertyBlock PropertyBlock
        {
            get
            {
                if (_propertyBlock == null)
                {
                    _propertyBlock = new MaterialPropertyBlock();
                }
                return _propertyBlock;
            }
        }

        /// <summary>
        /// Distance value displayed by this marker in yards.
        /// </summary>
        public int Distance => _distance;

        /// <summary>
        /// Whether the marker is currently visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Current quality tier affecting visuals.
        /// </summary>
        public QualityTier CurrentQualityTier => _currentQualityTier;

        /// <summary>
        /// The post transform.
        /// </summary>
        public Transform PostTransform => _postTransform;

        /// <summary>
        /// The sign transform.
        /// </summary>
        public Transform SignTransform => _signTransform;

        /// <summary>
        /// Event fired when distance changes.
        /// </summary>
        public event Action<int> OnDistanceChanged;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            InitializeReferences();
            UpdateDistanceText();
        }

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            UpdateVisibilityBasedOnDistance();
        }

        /// <summary>
        /// Initialize component references if not set in inspector.
        /// </summary>
        private void InitializeReferences()
        {
            if (_postTransform == null)
            {
                _postTransform = transform.Find("Post");
            }

            if (_signTransform == null)
            {
                _signTransform = transform.Find("Sign");
            }

            if (_distanceText == null)
            {
                var textTransform = transform.Find("DistanceText");
                if (textTransform != null)
                {
                    _distanceText = textTransform.GetComponent<TextMeshPro>();
                }
            }

            if (_signRenderer == null && _signTransform != null)
            {
                _signRenderer = _signTransform.GetComponent<Renderer>();
            }

            if (_postRenderer == null && _postTransform != null)
            {
                _postRenderer = _postTransform.GetComponent<Renderer>();
            }
        }

        /// <summary>
        /// Update visibility based on camera distance.
        /// </summary>
        private void UpdateVisibilityBasedOnDistance()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null)
                {
                    return;
                }
            }

            float distance = Vector3.Distance(_mainCamera.transform.position, transform.position);
            float fadeDistance = GetFadeDistanceForTier(_currentQualityTier);

            // Fade out based on distance
            if (distance > fadeDistance)
            {
                if (_isVisible)
                {
                    SetVisibility(false);
                }
            }
            else
            {
                if (!_isVisible)
                {
                    SetVisibility(true);
                }

                // Apply distance-based alpha
                float fadeStart = fadeDistance * 0.7f;
                if (distance > fadeStart)
                {
                    float alpha = 1f - ((distance - fadeStart) / (fadeDistance - fadeStart));
                    ApplyAlpha(Mathf.Clamp01(alpha));
                }
                else
                {
                    ApplyAlpha(1f);
                }
            }
        }

        /// <summary>
        /// Get fade distance for a quality tier.
        /// </summary>
        private float GetFadeDistanceForTier(QualityTier tier)
        {
            return tier switch
            {
                QualityTier.High => _fadeDistanceHigh,
                QualityTier.Medium => _fadeDistanceMedium,
                QualityTier.Low => _fadeDistanceLow,
                _ => _fadeDistanceMedium
            };
        }

        /// <summary>
        /// Apply alpha to all visual components.
        /// </summary>
        private void ApplyAlpha(float alpha)
        {
            if (_distanceText != null)
            {
                var color = _distanceText.color;
                color.a = alpha;
                _distanceText.color = color;
            }

            // Apply alpha to renderers using property blocks
            if (_signRenderer != null)
            {
                _signRenderer.GetPropertyBlock(PropertyBlock);
                var color = _signRenderer.sharedMaterial != null
                    ? _signRenderer.sharedMaterial.color
                    : Color.white;
                color.a = alpha;
                PropertyBlock.SetColor("_BaseColor", color);
                PropertyBlock.SetColor("_Color", color);
                _signRenderer.SetPropertyBlock(PropertyBlock);
            }

            if (_postRenderer != null)
            {
                _postRenderer.GetPropertyBlock(PropertyBlock);
                var color = _postRenderer.sharedMaterial != null
                    ? _postRenderer.sharedMaterial.color
                    : new Color(0.55f, 0.27f, 0.07f); // Wood color
                color.a = alpha;
                PropertyBlock.SetColor("_BaseColor", color);
                PropertyBlock.SetColor("_Color", color);
                _postRenderer.SetPropertyBlock(PropertyBlock);
            }
        }

        /// <summary>
        /// Set visibility of all child objects.
        /// </summary>
        private void SetVisibility(bool visible)
        {
            _isVisible = visible;

            if (_postTransform != null)
            {
                _postTransform.gameObject.SetActive(visible);
            }

            if (_signTransform != null)
            {
                _signTransform.gameObject.SetActive(visible);
            }

            if (_distanceText != null)
            {
                _distanceText.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Update the displayed distance text.
        /// </summary>
        private void UpdateDistanceText()
        {
            if (_distanceText != null)
            {
                string text = string.Format(_distanceFormat, _distance);
                if (!string.IsNullOrEmpty(_unitSuffix))
                {
                    text += _unitSuffix;
                }
                _distanceText.text = text;
            }
        }

        /// <summary>
        /// Set the distance value displayed by this marker.
        /// </summary>
        /// <param name="distanceYards">Distance in yards.</param>
        public void SetDistance(int distanceYards)
        {
            if (_distance == distanceYards)
            {
                return;
            }

            _distance = distanceYards;
            UpdateDistanceText();
            OnDistanceChanged?.Invoke(_distance);
        }

        /// <summary>
        /// Set the quality tier for visual adjustments.
        /// </summary>
        /// <param name="tier">The quality tier to apply.</param>
        public void SetQualityTier(QualityTier tier)
        {
            _currentQualityTier = tier;

            // Apply tier-specific adjustments
            switch (tier)
            {
                case QualityTier.Low:
                    // Simplified visuals on low quality
                    if (_distanceText != null)
                    {
                        _distanceText.fontSize = _textSize * 1.2f; // Larger text for readability
                    }
                    break;
                case QualityTier.Medium:
                case QualityTier.High:
                default:
                    if (_distanceText != null)
                    {
                        _distanceText.fontSize = _textSize;
                    }
                    break;
            }
        }

        /// <summary>
        /// Set the distance format string.
        /// </summary>
        public void SetDistanceFormat(string format)
        {
            _distanceFormat = format;
            UpdateDistanceText();
        }

        /// <summary>
        /// Set the unit suffix.
        /// </summary>
        public void SetUnitSuffix(string suffix)
        {
            _unitSuffix = suffix;
            UpdateDistanceText();
        }

        /// <summary>
        /// Set the post transform reference (for testing).
        /// </summary>
        public void SetPostTransform(Transform post)
        {
            _postTransform = post;
            if (post != null)
            {
                _postRenderer = post.GetComponent<Renderer>();
            }
        }

        /// <summary>
        /// Set the sign transform reference (for testing).
        /// </summary>
        public void SetSignTransform(Transform sign)
        {
            _signTransform = sign;
            if (sign != null)
            {
                _signRenderer = sign.GetComponent<Renderer>();
            }
        }

        /// <summary>
        /// Set the distance text reference (for testing).
        /// </summary>
        public void SetDistanceText(TextMeshPro text)
        {
            _distanceText = text;
        }

        /// <summary>
        /// Show the marker.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            SetVisibility(true);
            ApplyAlpha(1f);
        }

        /// <summary>
        /// Hide the marker.
        /// </summary>
        public void Hide()
        {
            SetVisibility(false);
        }

        /// <summary>
        /// Force update visibility check.
        /// </summary>
        public void ForceVisibilityUpdate()
        {
            UpdateVisibilityBasedOnDistance();
        }
    }
}
