// ABOUTME: Particle effect component for ball landing impact visualization.
// ABOUTME: Spawns dust particles scaled by velocity with quality tier support.

using System;
using UnityEngine;

namespace OpenRange.Visualization
{
    /// <summary>
    /// Manages particle effects for ball landing impact.
    /// Scales particle intensity based on landing velocity and quality tier.
    /// Automatically cleans up after particle system completes.
    /// </summary>
    public class ImpactEffect : MonoBehaviour
    {
        [Header("Particle System")]
        [SerializeField] private ParticleSystem _particleSystem;

        [Header("Velocity Scaling")]
        [SerializeField] private float _minVelocity = 5f;
        [SerializeField] private float _maxVelocity = 50f;
        [SerializeField] private float _minEmissionMultiplier = 0.5f;
        [SerializeField] private float _maxEmissionMultiplier = 1.5f;

        [Header("Quality Settings")]
        [SerializeField] private int _particleCountHigh = 30;
        [SerializeField] private int _particleCountMedium = 20;
        [SerializeField] private int _particleCountLow = 10;

        [Header("Behavior")]
        [SerializeField] private bool _autoDestroy = false;
        [SerializeField] private bool _returnToPool = true;

        private QualityTier _currentQualityTier = QualityTier.High;
        private ParticleSystem.MainModule _mainModule;
        private ParticleSystem.EmissionModule _emissionModule;
        private bool _isPlaying;
        private int _baseParticleCount;

        /// <summary>
        /// Whether the effect is currently playing.
        /// </summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// Current quality tier affecting particle count.
        /// </summary>
        public QualityTier CurrentQualityTier => _currentQualityTier;

        /// <summary>
        /// The particle system reference.
        /// </summary>
        public ParticleSystem ParticleSystemRef => _particleSystem;

        /// <summary>
        /// Event fired when the effect completes.
        /// </summary>
        public event Action<ImpactEffect> OnEffectComplete;

        private void Awake()
        {
            InitializeReferences();
            CacheModules();
        }

        private void OnEnable()
        {
            // Reset state when re-enabled from pool
            _isPlaying = false;
        }

        /// <summary>
        /// Initialize component references if not set in inspector.
        /// </summary>
        private void InitializeReferences()
        {
            if (_particleSystem == null)
            {
                _particleSystem = GetComponent<ParticleSystem>();
            }

            if (_particleSystem == null)
            {
                _particleSystem = GetComponentInChildren<ParticleSystem>();
            }
        }

        /// <summary>
        /// Cache particle system modules for performance.
        /// </summary>
        private void CacheModules()
        {
            if (_particleSystem != null)
            {
                _mainModule = _particleSystem.main;
                _emissionModule = _particleSystem.emission;

                // Store base burst count
                if (_emissionModule.burstCount > 0)
                {
                    var burst = _emissionModule.GetBurst(0);
                    _baseParticleCount = (int)burst.count.constant;
                }
                else
                {
                    _baseParticleCount = _particleCountHigh;
                }
            }
        }

        /// <summary>
        /// Play the impact effect with optional velocity scaling.
        /// </summary>
        /// <param name="velocity">Landing velocity to scale the effect.</param>
        public void Play(float velocity = 0f)
        {
            if (_particleSystem == null)
            {
                Debug.LogWarning("ImpactEffect: No particle system assigned");
                return;
            }

            // Apply quality tier particle count
            ApplyQualitySettings();

            // Apply velocity scaling
            if (velocity > 0f)
            {
                ApplyVelocityScaling(velocity);
            }

            // Play the particle system
            _particleSystem.Clear();
            _particleSystem.Play();
            _isPlaying = true;

            // Start monitoring for completion
            StartCoroutine(WaitForCompletion());
        }

        /// <summary>
        /// Play the effect at a specific position.
        /// </summary>
        /// <param name="position">World position to spawn the effect.</param>
        /// <param name="velocity">Landing velocity to scale the effect.</param>
        public void PlayAtPosition(Vector3 position, float velocity = 0f)
        {
            transform.position = position;
            Play(velocity);
        }

        /// <summary>
        /// Stop the effect immediately.
        /// </summary>
        public void Stop()
        {
            if (_particleSystem != null)
            {
                _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            _isPlaying = false;
        }

        /// <summary>
        /// Apply velocity scaling to particle emission.
        /// </summary>
        private void ApplyVelocityScaling(float velocity)
        {
            if (_particleSystem == null)
            {
                return;
            }

            // Clamp velocity to range
            float normalizedVelocity = Mathf.InverseLerp(_minVelocity, _maxVelocity, velocity);

            // Calculate emission multiplier
            float emissionMultiplier = Mathf.Lerp(_minEmissionMultiplier, _maxEmissionMultiplier, normalizedVelocity);

            // Get current particle count based on quality
            int qualityParticleCount = GetParticleCountForQuality();

            // Apply scaled particle count
            int scaledCount = Mathf.RoundToInt(qualityParticleCount * emissionMultiplier);

            if (_emissionModule.burstCount > 0)
            {
                var burst = _emissionModule.GetBurst(0);
                burst.count = scaledCount;
                _emissionModule.SetBurst(0, burst);
            }

            // Scale start size based on velocity
            float sizeMultiplier = Mathf.Lerp(0.8f, 1.2f, normalizedVelocity);
            var main = _particleSystem.main;
            main.startSizeMultiplier = sizeMultiplier;
        }

        /// <summary>
        /// Apply quality tier settings to particle system.
        /// </summary>
        private void ApplyQualitySettings()
        {
            if (_particleSystem == null)
            {
                return;
            }

            int particleCount = GetParticleCountForQuality();

            if (_emissionModule.burstCount > 0)
            {
                var burst = _emissionModule.GetBurst(0);
                burst.count = particleCount;
                _emissionModule.SetBurst(0, burst);
            }
        }

        /// <summary>
        /// Get the particle count for the current quality tier.
        /// </summary>
        private int GetParticleCountForQuality()
        {
            return _currentQualityTier switch
            {
                QualityTier.High => _particleCountHigh,
                QualityTier.Medium => _particleCountMedium,
                QualityTier.Low => _particleCountLow,
                _ => _particleCountMedium
            };
        }

        /// <summary>
        /// Wait for the particle system to complete.
        /// </summary>
        private System.Collections.IEnumerator WaitForCompletion()
        {
            if (_particleSystem == null)
            {
                yield break;
            }

            // Wait for particles to finish
            while (_particleSystem.isPlaying || _particleSystem.particleCount > 0)
            {
                yield return null;
            }

            _isPlaying = false;

            // Fire completion event
            OnEffectComplete?.Invoke(this);

            // Handle cleanup
            if (_autoDestroy)
            {
                Destroy(gameObject);
            }
            else if (_returnToPool)
            {
                // Pool will handle this via the event
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Set the quality tier which affects particle count.
        /// </summary>
        /// <param name="tier">The quality tier to apply.</param>
        public void SetQualityTier(QualityTier tier)
        {
            _currentQualityTier = tier;
        }

        /// <summary>
        /// Set the particle system reference (for testing).
        /// </summary>
        public void SetParticleSystem(ParticleSystem ps)
        {
            _particleSystem = ps;
            if (ps != null)
            {
                CacheModules();
            }
        }

        /// <summary>
        /// Set whether to auto-destroy after completion.
        /// </summary>
        public void SetAutoDestroy(bool autoDestroy)
        {
            _autoDestroy = autoDestroy;
        }

        /// <summary>
        /// Set whether to return to pool after completion.
        /// </summary>
        public void SetReturnToPool(bool returnToPool)
        {
            _returnToPool = returnToPool;
        }

        /// <summary>
        /// Set the velocity scaling range.
        /// </summary>
        public void SetVelocityRange(float min, float max)
        {
            _minVelocity = min;
            _maxVelocity = max;
        }

        /// <summary>
        /// Get the current particle count for the burst.
        /// </summary>
        public int GetCurrentParticleCount()
        {
            if (_particleSystem == null || _emissionModule.burstCount == 0)
            {
                return 0;
            }

            var burst = _emissionModule.GetBurst(0);
            return (int)burst.count.constant;
        }
    }
}
