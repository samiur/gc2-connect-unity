// ABOUTME: Singleton manager for spawning and pooling landing effects and markers.
// ABOUTME: Subscribes to BallController events and manages quality tier for all effects.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenRange.Visualization
{
    /// <summary>
    /// Manages spawning and pooling of landing effects and markers.
    /// Subscribes to BallController events to automatically spawn effects on landing.
    /// Implements object pooling for performance optimization.
    /// </summary>
    public class EffectsManager : MonoBehaviour
    {
        [Header("Prefab References")]
        [SerializeField] private LandingMarker _landingMarkerPrefab;
        [SerializeField] private ImpactEffect _impactEffectPrefab;

        [Header("Pool Settings")]
        [SerializeField] private int _markerPoolSize = 5;
        [SerializeField] private int _effectPoolSize = 10;

        [Header("Integration")]
        [SerializeField] private BallController _ballController;

        [Header("Behavior")]
        [SerializeField] private bool _autoSpawnOnLanding = true;
        [SerializeField] private bool _spawnMarkerOnLanding = true;
        [SerializeField] private bool _spawnEffectOnLanding = true;

        private QualityTier _currentQualityTier = QualityTier.High;
        private Queue<LandingMarker> _markerPool = new Queue<LandingMarker>();
        private Queue<ImpactEffect> _effectPool = new Queue<ImpactEffect>();
        private List<LandingMarker> _activeMarkers = new List<LandingMarker>();
        private List<ImpactEffect> _activeEffects = new List<ImpactEffect>();

        private float _lastCarryDistance;
        private float _lastTotalDistance;

        private static EffectsManager _instance;

        /// <summary>
        /// Singleton instance of the EffectsManager.
        /// </summary>
        public static EffectsManager Instance => _instance;

        /// <summary>
        /// Current quality tier affecting all spawned effects.
        /// </summary>
        public QualityTier CurrentQualityTier => _currentQualityTier;

        /// <summary>
        /// Number of markers currently active (not pooled).
        /// </summary>
        public int ActiveMarkerCount => _activeMarkers.Count;

        /// <summary>
        /// Number of effects currently active (not pooled).
        /// </summary>
        public int ActiveEffectCount => _activeEffects.Count;

        /// <summary>
        /// Number of markers available in the pool.
        /// </summary>
        public int PooledMarkerCount => _markerPool.Count;

        /// <summary>
        /// Number of effects available in the pool.
        /// </summary>
        public int PooledEffectCount => _effectPool.Count;

        /// <summary>
        /// Event fired when a landing marker is spawned.
        /// </summary>
        public event Action<LandingMarker> OnMarkerSpawned;

        /// <summary>
        /// Event fired when an impact effect is spawned.
        /// </summary>
        public event Action<ImpactEffect> OnEffectSpawned;

        private void Awake()
        {
            SetupSingleton();
            InitializePools();
        }

        private void OnEnable()
        {
            SubscribeToBallController();
        }

        private void OnDisable()
        {
            UnsubscribeFromBallController();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Set up the singleton instance.
        /// </summary>
        private void SetupSingleton()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("EffectsManager: Multiple instances detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        /// <summary>
        /// Initialize the object pools.
        /// </summary>
        private void InitializePools()
        {
            // Pre-populate marker pool
            if (_landingMarkerPrefab != null)
            {
                for (int i = 0; i < _markerPoolSize; i++)
                {
                    var marker = CreateMarkerInstance();
                    marker.gameObject.SetActive(false);
                    _markerPool.Enqueue(marker);
                }
            }

            // Pre-populate effect pool
            if (_impactEffectPrefab != null)
            {
                for (int i = 0; i < _effectPoolSize; i++)
                {
                    var effect = CreateEffectInstance();
                    effect.gameObject.SetActive(false);
                    _effectPool.Enqueue(effect);
                }
            }
        }

        /// <summary>
        /// Create a new marker instance.
        /// </summary>
        private LandingMarker CreateMarkerInstance()
        {
            var marker = Instantiate(_landingMarkerPrefab, transform);
            marker.SetQualityTier(_currentQualityTier);
            marker.OnFadeOutComplete += () => ReturnMarkerToPool(marker);
            return marker;
        }

        /// <summary>
        /// Create a new effect instance.
        /// </summary>
        private ImpactEffect CreateEffectInstance()
        {
            var effect = Instantiate(_impactEffectPrefab, transform);
            effect.SetQualityTier(_currentQualityTier);
            effect.OnEffectComplete += ReturnEffectToPool;
            return effect;
        }

        /// <summary>
        /// Subscribe to BallController events.
        /// </summary>
        private void SubscribeToBallController()
        {
            if (_ballController == null)
            {
                _ballController = FindAnyObjectByType<BallController>();
            }

            if (_ballController != null)
            {
                _ballController.OnLanded += HandleBallLanded;
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
                _ballController.OnLanded -= HandleBallLanded;
                _ballController.OnStopped -= HandleBallStopped;
            }
        }

        /// <summary>
        /// Handle ball landing event.
        /// </summary>
        private void HandleBallLanded(Vector3 position)
        {
            if (!_autoSpawnOnLanding)
            {
                return;
            }

            // Store carry distance for marker
            if (_ballController?.CurrentShot != null)
            {
                _lastCarryDistance = _ballController.CurrentShot.CarryDistance;
                _lastTotalDistance = _ballController.CurrentShot.TotalDistance;
            }

            // Spawn impact effect
            if (_spawnEffectOnLanding)
            {
                float velocity = EstimateLandingVelocity();
                SpawnImpactEffect(position, velocity);
            }
        }

        /// <summary>
        /// Handle ball stopped event.
        /// </summary>
        private void HandleBallStopped(Vector3 position)
        {
            if (!_autoSpawnOnLanding || !_spawnMarkerOnLanding)
            {
                return;
            }

            // Spawn marker at final position with distances
            SpawnLandingMarker(position, _lastCarryDistance, _lastTotalDistance);
        }

        /// <summary>
        /// Estimate landing velocity from current shot.
        /// </summary>
        private float EstimateLandingVelocity()
        {
            // Approximate landing velocity based on ball speed and flight time
            if (_ballController?.CurrentShot != null)
            {
                var shot = _ballController.CurrentShot;
                // Landing velocity is roughly 40-60% of initial speed
                return shot.LaunchData?.BallSpeed * 0.5f * 0.44704f ?? 20f; // Convert mph to m/s
            }
            return 20f; // Default moderate velocity
        }

        /// <summary>
        /// Spawn a landing marker at the specified position.
        /// </summary>
        /// <param name="position">World position for the marker.</param>
        /// <param name="carryDistance">Carry distance in yards.</param>
        /// <param name="totalDistance">Total distance in yards.</param>
        /// <param name="autoHide">Whether to automatically hide after duration.</param>
        /// <returns>The spawned marker, or null if no prefab assigned.</returns>
        public LandingMarker SpawnLandingMarker(Vector3 position, float carryDistance, float totalDistance, bool autoHide = true)
        {
            if (_landingMarkerPrefab == null)
            {
                Debug.LogWarning("EffectsManager: Landing marker prefab not assigned");
                return null;
            }

            // Get from pool or create new
            LandingMarker marker;
            if (_markerPool.Count > 0)
            {
                marker = _markerPool.Dequeue();
                marker.gameObject.SetActive(true);
            }
            else
            {
                marker = CreateMarkerInstance();
            }

            marker.SetQualityTier(_currentQualityTier);
            marker.Show(position, carryDistance, totalDistance, autoHide);

            _activeMarkers.Add(marker);
            OnMarkerSpawned?.Invoke(marker);

            return marker;
        }

        /// <summary>
        /// Spawn an impact effect at the specified position.
        /// </summary>
        /// <param name="position">World position for the effect.</param>
        /// <param name="velocity">Landing velocity to scale the effect.</param>
        /// <returns>The spawned effect, or null if no prefab assigned.</returns>
        public ImpactEffect SpawnImpactEffect(Vector3 position, float velocity = 0f)
        {
            if (_impactEffectPrefab == null)
            {
                Debug.LogWarning("EffectsManager: Impact effect prefab not assigned");
                return null;
            }

            // Get from pool or create new
            ImpactEffect effect;
            if (_effectPool.Count > 0)
            {
                effect = _effectPool.Dequeue();
                effect.gameObject.SetActive(true);
            }
            else
            {
                effect = CreateEffectInstance();
            }

            effect.SetQualityTier(_currentQualityTier);
            effect.PlayAtPosition(position, velocity);

            _activeEffects.Add(effect);
            OnEffectSpawned?.Invoke(effect);

            return effect;
        }

        /// <summary>
        /// Spawn a bounce effect (smaller impact effect).
        /// </summary>
        /// <param name="position">World position for the effect.</param>
        /// <returns>The spawned effect, or null if no prefab assigned.</returns>
        public ImpactEffect SpawnBounceEffect(Vector3 position)
        {
            // Bounce effects use lower velocity for smaller particles
            return SpawnImpactEffect(position, 10f);
        }

        /// <summary>
        /// Return a marker to the pool.
        /// </summary>
        private void ReturnMarkerToPool(LandingMarker marker)
        {
            if (marker == null)
            {
                return;
            }

            _activeMarkers.Remove(marker);
            marker.gameObject.SetActive(false);
            _markerPool.Enqueue(marker);
        }

        /// <summary>
        /// Return an effect to the pool.
        /// </summary>
        private void ReturnEffectToPool(ImpactEffect effect)
        {
            if (effect == null)
            {
                return;
            }

            _activeEffects.Remove(effect);
            effect.gameObject.SetActive(false);
            _effectPool.Enqueue(effect);
        }

        /// <summary>
        /// Clear all active markers immediately.
        /// </summary>
        public void ClearAllMarkers()
        {
            foreach (var marker in _activeMarkers.ToArray())
            {
                marker.Hide();
                ReturnMarkerToPool(marker);
            }
            _activeMarkers.Clear();
        }

        /// <summary>
        /// Clear all active effects immediately.
        /// </summary>
        public void ClearAllEffects()
        {
            foreach (var effect in _activeEffects.ToArray())
            {
                effect.Stop();
                ReturnEffectToPool(effect);
            }
            _activeEffects.Clear();
        }

        /// <summary>
        /// Clear all active markers and effects.
        /// </summary>
        public void ClearAll()
        {
            ClearAllMarkers();
            ClearAllEffects();
        }

        /// <summary>
        /// Set the quality tier for all effects.
        /// </summary>
        /// <param name="tier">The quality tier to apply.</param>
        public void SetQualityTier(QualityTier tier)
        {
            _currentQualityTier = tier;

            // Update active markers
            foreach (var marker in _activeMarkers)
            {
                marker.SetQualityTier(tier);
            }

            // Update active effects
            foreach (var effect in _activeEffects)
            {
                effect.SetQualityTier(tier);
            }

            // Update pooled markers
            foreach (var marker in _markerPool)
            {
                marker.SetQualityTier(tier);
            }

            // Update pooled effects
            foreach (var effect in _effectPool)
            {
                effect.SetQualityTier(tier);
            }
        }

        /// <summary>
        /// Set the BallController reference.
        /// </summary>
        public void SetBallController(BallController controller)
        {
            UnsubscribeFromBallController();
            _ballController = controller;
            SubscribeToBallController();
        }

        /// <summary>
        /// Set the landing marker prefab.
        /// </summary>
        public void SetLandingMarkerPrefab(LandingMarker prefab)
        {
            _landingMarkerPrefab = prefab;
        }

        /// <summary>
        /// Set the impact effect prefab.
        /// </summary>
        public void SetImpactEffectPrefab(ImpactEffect prefab)
        {
            _impactEffectPrefab = prefab;
        }

        /// <summary>
        /// Set whether to auto-spawn effects on landing.
        /// </summary>
        public void SetAutoSpawnOnLanding(bool autoSpawn)
        {
            _autoSpawnOnLanding = autoSpawn;
        }

        /// <summary>
        /// Set whether to spawn markers on landing.
        /// </summary>
        public void SetSpawnMarkerOnLanding(bool spawn)
        {
            _spawnMarkerOnLanding = spawn;
        }

        /// <summary>
        /// Set whether to spawn effects on landing.
        /// </summary>
        public void SetSpawnEffectOnLanding(bool spawn)
        {
            _spawnEffectOnLanding = spawn;
        }

        /// <summary>
        /// Get the list of active markers (for testing).
        /// </summary>
        public List<LandingMarker> GetActiveMarkers()
        {
            return new List<LandingMarker>(_activeMarkers);
        }

        /// <summary>
        /// Get the list of active effects (for testing).
        /// </summary>
        public List<ImpactEffect> GetActiveEffects()
        {
            return new List<ImpactEffect>(_activeEffects);
        }
    }
}
