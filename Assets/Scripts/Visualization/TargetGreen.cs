// ABOUTME: Component for target green areas on the driving range with optional flag.
// ABOUTME: Supports different sizes and quality tier adjustments for visual complexity.

using System;
using UnityEngine;

namespace OpenRange.Visualization
{
    /// <summary>
    /// Size options for target greens.
    /// </summary>
    public enum TargetGreenSize
    {
        Small = 0,
        Medium = 1,
        Large = 2
    }

    /// <summary>
    /// Target green area that players aim for.
    /// Includes circular green surface and optional flag.
    /// </summary>
    public class TargetGreen : MonoBehaviour
    {
        [Header("Visual References")]
        [SerializeField] private Transform _greenSurface;
        [SerializeField] private Transform _flagPole;
        [SerializeField] private Transform _flag;
        [SerializeField] private Renderer _greenRenderer;
        [SerializeField] private Renderer _flagRenderer;
        [SerializeField] private Renderer _poleRenderer;

        [Header("Size Settings")]
        [SerializeField] private TargetGreenSize _size = TargetGreenSize.Medium;
        [SerializeField] private float _smallDiameter = 10f;
        [SerializeField] private float _mediumDiameter = 15f;
        [SerializeField] private float _largeDiameter = 20f;
        [SerializeField] private float _flagHeight = 2.5f;

        [Header("Animation")]
        [SerializeField] private bool _animateFlag = true;
        [SerializeField] private float _flagWaveSpeed = 2f;
        [SerializeField] private float _flagWaveAmount = 15f;

        [Header("Highlight Settings")]
        [SerializeField] private Color _normalColor = new Color(0.18f, 0.35f, 0.15f);
        [SerializeField] private Color _highlightColor = new Color(0.25f, 0.45f, 0.2f);
        [SerializeField] private float _highlightDuration = 2f;

        private QualityTier _currentQualityTier = QualityTier.High;
        private bool _isHighlighted;
        private float _highlightTimer;
        private MaterialPropertyBlock _propertyBlock;
        private Quaternion _flagBaseRotation;

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
        /// Current size of the green.
        /// </summary>
        public TargetGreenSize Size => _size;

        /// <summary>
        /// Current diameter of the green in meters.
        /// </summary>
        public float Diameter => GetDiameterForSize(_size);

        /// <summary>
        /// Whether the green is currently highlighted.
        /// </summary>
        public bool IsHighlighted => _isHighlighted;

        /// <summary>
        /// Current quality tier affecting visuals.
        /// </summary>
        public QualityTier CurrentQualityTier => _currentQualityTier;

        /// <summary>
        /// The green surface transform.
        /// </summary>
        public Transform GreenSurface => _greenSurface;

        /// <summary>
        /// The flag pole transform.
        /// </summary>
        public Transform FlagPole => _flagPole;

        /// <summary>
        /// Event fired when ball lands on green.
        /// </summary>
        public event Action OnBallLanded;

        /// <summary>
        /// Event fired when highlight ends.
        /// </summary>
        public event Action OnHighlightEnded;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            InitializeReferences();
            ApplySizeSettings();

            if (_flag != null)
            {
                _flagBaseRotation = _flag.localRotation;
            }
        }

        private void Update()
        {
            if (_animateFlag && ShouldAnimateFlag())
            {
                AnimateFlag();
            }

            if (_isHighlighted)
            {
                UpdateHighlight();
            }
        }

        /// <summary>
        /// Initialize component references if not set in inspector.
        /// </summary>
        private void InitializeReferences()
        {
            if (_greenSurface == null)
            {
                _greenSurface = transform.Find("GreenSurface");
            }

            if (_flagPole == null)
            {
                _flagPole = transform.Find("FlagPole");
            }

            if (_flag == null)
            {
                _flag = transform.Find("Flag");
                if (_flag == null && _flagPole != null)
                {
                    _flag = _flagPole.Find("Flag");
                }
            }

            if (_greenRenderer == null && _greenSurface != null)
            {
                _greenRenderer = _greenSurface.GetComponent<Renderer>();
            }

            if (_flagRenderer == null && _flag != null)
            {
                _flagRenderer = _flag.GetComponent<Renderer>();
            }

            if (_poleRenderer == null && _flagPole != null)
            {
                _poleRenderer = _flagPole.GetComponent<Renderer>();
            }
        }

        /// <summary>
        /// Apply size settings to the green.
        /// </summary>
        private void ApplySizeSettings()
        {
            if (_greenSurface != null)
            {
                float diameter = GetDiameterForSize(_size);
                // Scale for a cylinder primitive (default diameter is 1)
                _greenSurface.localScale = new Vector3(diameter, 0.1f, diameter);
            }

            if (_flagPole != null)
            {
                _flagPole.localPosition = new Vector3(0f, 0f, 0f);
                _flagPole.localScale = new Vector3(0.05f, _flagHeight, 0.05f);
            }
        }

        /// <summary>
        /// Get diameter for a specific size.
        /// </summary>
        private float GetDiameterForSize(TargetGreenSize size)
        {
            return size switch
            {
                TargetGreenSize.Small => _smallDiameter,
                TargetGreenSize.Medium => _mediumDiameter,
                TargetGreenSize.Large => _largeDiameter,
                _ => _mediumDiameter
            };
        }

        /// <summary>
        /// Check if flag animation should run.
        /// </summary>
        private bool ShouldAnimateFlag()
        {
            return _currentQualityTier != QualityTier.Low && _flag != null;
        }

        /// <summary>
        /// Animate the flag waving.
        /// </summary>
        private void AnimateFlag()
        {
            if (_flag == null)
            {
                return;
            }

            float angle = Mathf.Sin(Time.time * _flagWaveSpeed) * _flagWaveAmount;
            _flag.localRotation = _flagBaseRotation * Quaternion.Euler(0f, 0f, angle);
        }

        /// <summary>
        /// Update highlight effect.
        /// </summary>
        private void UpdateHighlight()
        {
            _highlightTimer -= Time.deltaTime;

            if (_highlightTimer <= 0f)
            {
                EndHighlight();
            }
        }

        /// <summary>
        /// Set the size of the green.
        /// </summary>
        /// <param name="size">The size to apply.</param>
        public void SetSize(TargetGreenSize size)
        {
            _size = size;
            ApplySizeSettings();
        }

        /// <summary>
        /// Set the quality tier for visual adjustments.
        /// </summary>
        /// <param name="tier">The quality tier to apply.</param>
        public void SetQualityTier(QualityTier tier)
        {
            _currentQualityTier = tier;

            // On low quality, hide flag and disable animation
            if (_flagPole != null)
            {
                _flagPole.gameObject.SetActive(tier != QualityTier.Low);
            }

            // Adjust animation based on tier
            if (tier == QualityTier.Medium)
            {
                // Static flag on medium
                _animateFlag = false;
                if (_flag != null)
                {
                    _flag.localRotation = _flagBaseRotation;
                }
            }
            else if (tier == QualityTier.High)
            {
                _animateFlag = true;
            }
        }

        /// <summary>
        /// Highlight the green (e.g., when ball lands on it).
        /// </summary>
        /// <param name="duration">Duration of highlight, or -1 for default.</param>
        public void Highlight(float duration = -1f)
        {
            _isHighlighted = true;
            _highlightTimer = duration > 0f ? duration : _highlightDuration;

            ApplyGreenColor(_highlightColor);
        }

        /// <summary>
        /// End the highlight effect.
        /// </summary>
        public void EndHighlight()
        {
            _isHighlighted = false;
            _highlightTimer = 0f;

            ApplyGreenColor(_normalColor);
            OnHighlightEnded?.Invoke();
        }

        /// <summary>
        /// Apply a color to the green surface.
        /// </summary>
        private void ApplyGreenColor(Color color)
        {
            if (_greenRenderer == null)
            {
                return;
            }

            _greenRenderer.GetPropertyBlock(PropertyBlock);
            PropertyBlock.SetColor("_BaseColor", color);
            PropertyBlock.SetColor("_Color", color);
            _greenRenderer.SetPropertyBlock(PropertyBlock);
        }

        /// <summary>
        /// Check if a position is on the green.
        /// </summary>
        /// <param name="position">World position to check.</param>
        /// <returns>True if position is within the green radius.</returns>
        public bool IsPositionOnGreen(Vector3 position)
        {
            // Project to horizontal plane
            Vector3 greenCenter = transform.position;
            greenCenter.y = 0f;
            Vector3 checkPos = position;
            checkPos.y = 0f;

            float distance = Vector3.Distance(greenCenter, checkPos);
            return distance <= (Diameter / 2f);
        }

        /// <summary>
        /// Notify that a ball has landed on the green.
        /// </summary>
        /// <param name="position">Landing position.</param>
        public void NotifyBallLanded(Vector3 position)
        {
            if (IsPositionOnGreen(position))
            {
                Highlight();
                OnBallLanded?.Invoke();
            }
        }

        /// <summary>
        /// Set the green surface transform (for testing).
        /// </summary>
        public void SetGreenSurface(Transform surface)
        {
            _greenSurface = surface;
            if (surface != null)
            {
                _greenRenderer = surface.GetComponent<Renderer>();
            }
        }

        /// <summary>
        /// Set the flag pole transform (for testing).
        /// </summary>
        public void SetFlagPole(Transform pole)
        {
            _flagPole = pole;
            if (pole != null)
            {
                _poleRenderer = pole.GetComponent<Renderer>();
            }
        }

        /// <summary>
        /// Set the flag transform (for testing).
        /// </summary>
        public void SetFlag(Transform flag)
        {
            _flag = flag;
            if (flag != null)
            {
                _flagRenderer = flag.GetComponent<Renderer>();
                _flagBaseRotation = flag.localRotation;
            }
        }

        /// <summary>
        /// Set the normal color.
        /// </summary>
        public void SetNormalColor(Color color)
        {
            _normalColor = color;
            if (!_isHighlighted)
            {
                ApplyGreenColor(_normalColor);
            }
        }

        /// <summary>
        /// Set the highlight color.
        /// </summary>
        public void SetHighlightColor(Color color)
        {
            _highlightColor = color;
            if (_isHighlighted)
            {
                ApplyGreenColor(_highlightColor);
            }
        }

        /// <summary>
        /// Show the green.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hide the green.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
