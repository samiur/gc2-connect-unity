// ABOUTME: Manages environment state, distance markers, target greens, and quality tier adjustments.
// ABOUTME: Singleton that handles environment setup and dynamic quality changes for the Marina scene.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenRange.Visualization
{
    /// <summary>
    /// Manages the driving range environment including distance markers,
    /// target greens, tee mat, and quality tier adjustments.
    /// </summary>
    public class EnvironmentManager : MonoBehaviour
    {
        /// <summary>
        /// Conversion factor from yards to Unity units (meters).
        /// </summary>
        public const float YardsToMeters = 0.9144f;

        /// <summary>
        /// Standard distance marker intervals in yards.
        /// </summary>
        public static readonly int[] StandardMarkerDistances = { 50, 100, 150, 200, 250, 300 };

        [Header("Prefab References")]
        [SerializeField] private DistanceMarker _distanceMarkerPrefab;
        [SerializeField] private TargetGreen _targetGreenPrefab;
        [SerializeField] private TeeMat _teeMatPrefab;

        [Header("Environment References")]
        [SerializeField] private Transform _groundPlane;
        [SerializeField] private Light _sunLight;

        [Header("Marker Configuration")]
        [SerializeField] private float _markerSpacing = 5f;
        [SerializeField] private bool _showDistanceMarkers = true;
        [SerializeField] private bool _showTargetGreens = true;

        [Header("Quality Settings")]
        [SerializeField] private float _drawDistanceHigh = 500f;
        [SerializeField] private float _drawDistanceMedium = 350f;
        [SerializeField] private float _drawDistanceLow = 200f;
        [SerializeField] private bool _enableReflectionsHigh = true;
        [SerializeField] private bool _enableReflectionsMedium = false;

        private QualityTier _currentQualityTier = QualityTier.High;
        private List<DistanceMarker> _distanceMarkers = new List<DistanceMarker>();
        private List<TargetGreen> _targetGreens = new List<TargetGreen>();
        private TeeMat _teeMat;
        private bool _isInitialized;

        private static EnvironmentManager _instance;

        /// <summary>
        /// Singleton instance of the EnvironmentManager.
        /// </summary>
        public static EnvironmentManager Instance => _instance;

        /// <summary>
        /// Current quality tier affecting environment visuals.
        /// </summary>
        public QualityTier CurrentQualityTier => _currentQualityTier;

        /// <summary>
        /// Whether the environment has been initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// All distance markers in the scene.
        /// </summary>
        public IReadOnlyList<DistanceMarker> DistanceMarkers => _distanceMarkers.AsReadOnly();

        /// <summary>
        /// All target greens in the scene.
        /// </summary>
        public IReadOnlyList<TargetGreen> TargetGreens => _targetGreens.AsReadOnly();

        /// <summary>
        /// The tee mat in the scene.
        /// </summary>
        public TeeMat TeeMat => _teeMat;

        /// <summary>
        /// Current draw distance based on quality tier.
        /// </summary>
        public float CurrentDrawDistance => GetDrawDistanceForTier(_currentQualityTier);

        /// <summary>
        /// Whether reflections are enabled for current quality tier.
        /// </summary>
        public bool ReflectionsEnabled => AreReflectionsEnabledForTier(_currentQualityTier);

        /// <summary>
        /// Event fired when quality tier changes.
        /// </summary>
        public event Action<QualityTier> OnQualityTierChanged;

        /// <summary>
        /// Event fired when environment setup completes.
        /// </summary>
        public event Action OnEnvironmentReady;

        private void Awake()
        {
            SetupSingleton();
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
            // Use Unity's overloaded == to handle destroyed objects correctly
            if (_instance == null || ReferenceEquals(_instance, null) || !_instance)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Debug.LogWarning("EnvironmentManager: Multiple instances detected. Destroying duplicate.");
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
            }
        }

        /// <summary>
        /// Initialize the environment with distance markers and greens.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            FindExistingEnvironmentObjects();
            ApplyQualitySettings();
            _isInitialized = true;

            OnEnvironmentReady?.Invoke();
        }

        /// <summary>
        /// Find existing environment objects in the scene.
        /// </summary>
        private void FindExistingEnvironmentObjects()
        {
            // Find existing distance markers
            _distanceMarkers.Clear();
            var existingMarkers = FindObjectsByType<DistanceMarker>(FindObjectsSortMode.None);
            _distanceMarkers.AddRange(existingMarkers);

            // Find existing target greens
            _targetGreens.Clear();
            var existingGreens = FindObjectsByType<TargetGreen>(FindObjectsSortMode.None);
            _targetGreens.AddRange(existingGreens);

            // Find existing tee mat
            _teeMat = FindAnyObjectByType<TeeMat>();

            // Find ground plane if not set
            if (_groundPlane == null)
            {
                var ground = GameObject.Find("Ground");
                if (ground != null)
                {
                    _groundPlane = ground.transform;
                }
            }

            // Find sun light if not set
            if (_sunLight == null)
            {
                var lightGo = GameObject.Find("Directional Light");
                if (lightGo != null)
                {
                    _sunLight = lightGo.GetComponent<Light>();
                }
            }
        }

        /// <summary>
        /// Spawn distance markers at standard intervals.
        /// </summary>
        /// <param name="distances">Array of distances in yards, or null for standard distances.</param>
        public void SpawnDistanceMarkers(int[] distances = null)
        {
            if (_distanceMarkerPrefab == null)
            {
                Debug.LogWarning("EnvironmentManager: Distance marker prefab not assigned");
                return;
            }

            distances ??= StandardMarkerDistances;

            foreach (int distance in distances)
            {
                SpawnDistanceMarker(distance);
            }
        }

        /// <summary>
        /// Spawn a single distance marker at a specific distance.
        /// </summary>
        /// <param name="distanceYards">Distance in yards from tee.</param>
        /// <returns>The spawned distance marker.</returns>
        public DistanceMarker SpawnDistanceMarker(int distanceYards)
        {
            if (_distanceMarkerPrefab == null)
            {
                Debug.LogWarning("EnvironmentManager: Distance marker prefab not assigned");
                return null;
            }

            float zPosition = distanceYards * YardsToMeters;
            Vector3 position = new Vector3(-_markerSpacing, 0f, zPosition);

            var marker = Instantiate(_distanceMarkerPrefab, position, Quaternion.identity, transform);
            marker.SetDistance(distanceYards);
            marker.SetQualityTier(_currentQualityTier);
            _distanceMarkers.Add(marker);

            return marker;
        }

        /// <summary>
        /// Spawn target greens at specified distances.
        /// </summary>
        /// <param name="distances">Array of distances in yards.</param>
        /// <param name="sizes">Corresponding sizes for each green.</param>
        public void SpawnTargetGreens(int[] distances, TargetGreenSize[] sizes = null)
        {
            if (_targetGreenPrefab == null)
            {
                Debug.LogWarning("EnvironmentManager: Target green prefab not assigned");
                return;
            }

            for (int i = 0; i < distances.Length; i++)
            {
                var size = sizes != null && i < sizes.Length ? sizes[i] : TargetGreenSize.Medium;
                SpawnTargetGreen(distances[i], size);
            }
        }

        /// <summary>
        /// Spawn a single target green at a specific distance.
        /// </summary>
        /// <param name="distanceYards">Distance in yards from tee.</param>
        /// <param name="size">Size of the green.</param>
        /// <returns>The spawned target green.</returns>
        public TargetGreen SpawnTargetGreen(int distanceYards, TargetGreenSize size = TargetGreenSize.Medium)
        {
            if (_targetGreenPrefab == null)
            {
                Debug.LogWarning("EnvironmentManager: Target green prefab not assigned");
                return null;
            }

            float zPosition = distanceYards * YardsToMeters;
            Vector3 position = new Vector3(0f, 0.01f, zPosition); // Slightly above ground

            var green = Instantiate(_targetGreenPrefab, position, Quaternion.identity, transform);
            green.SetSize(size);
            green.SetQualityTier(_currentQualityTier);
            _targetGreens.Add(green);

            return green;
        }

        /// <summary>
        /// Spawn the tee mat at the origin.
        /// </summary>
        /// <returns>The spawned tee mat.</returns>
        public TeeMat SpawnTeeMat()
        {
            if (_teeMatPrefab == null)
            {
                Debug.LogWarning("EnvironmentManager: Tee mat prefab not assigned");
                return null;
            }

            if (_teeMat != null)
            {
                Debug.LogWarning("EnvironmentManager: Tee mat already exists");
                return _teeMat;
            }

            _teeMat = Instantiate(_teeMatPrefab, Vector3.zero, Quaternion.identity, transform);
            return _teeMat;
        }

        /// <summary>
        /// Set the quality tier for all environment elements.
        /// </summary>
        /// <param name="tier">The quality tier to apply.</param>
        public void SetQualityTier(QualityTier tier)
        {
            if (_currentQualityTier == tier)
            {
                return;
            }

            _currentQualityTier = tier;
            ApplyQualitySettings();

            // Update all distance markers
            foreach (var marker in _distanceMarkers)
            {
                if (marker != null)
                {
                    marker.SetQualityTier(tier);
                }
            }

            // Update all target greens
            foreach (var green in _targetGreens)
            {
                if (green != null)
                {
                    green.SetQualityTier(tier);
                }
            }

            OnQualityTierChanged?.Invoke(tier);
        }

        /// <summary>
        /// Apply quality-dependent settings to the environment.
        /// </summary>
        private void ApplyQualitySettings()
        {
            // Adjust camera draw distance based on tier
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.farClipPlane = GetDrawDistanceForTier(_currentQualityTier);
            }

            // Adjust shadow settings based on tier
            if (_sunLight != null)
            {
                switch (_currentQualityTier)
                {
                    case QualityTier.High:
                        _sunLight.shadows = LightShadows.Soft;
                        break;
                    case QualityTier.Medium:
                        _sunLight.shadows = LightShadows.Hard;
                        break;
                    case QualityTier.Low:
                        _sunLight.shadows = LightShadows.None;
                        break;
                }
            }
        }

        /// <summary>
        /// Get draw distance for a quality tier.
        /// </summary>
        /// <param name="tier">The quality tier.</param>
        /// <returns>Draw distance in meters.</returns>
        public float GetDrawDistanceForTier(QualityTier tier)
        {
            return tier switch
            {
                QualityTier.High => _drawDistanceHigh,
                QualityTier.Medium => _drawDistanceMedium,
                QualityTier.Low => _drawDistanceLow,
                _ => _drawDistanceMedium
            };
        }

        /// <summary>
        /// Check if reflections are enabled for a quality tier.
        /// </summary>
        /// <param name="tier">The quality tier.</param>
        /// <returns>True if reflections are enabled.</returns>
        public bool AreReflectionsEnabledForTier(QualityTier tier)
        {
            return tier switch
            {
                QualityTier.High => _enableReflectionsHigh,
                QualityTier.Medium => _enableReflectionsMedium,
                QualityTier.Low => false,
                _ => false
            };
        }

        /// <summary>
        /// Clear all distance markers.
        /// </summary>
        public void ClearDistanceMarkers()
        {
            foreach (var marker in _distanceMarkers)
            {
                if (marker != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(marker.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(marker.gameObject);
                    }
                }
            }
            _distanceMarkers.Clear();
        }

        /// <summary>
        /// Clear all target greens.
        /// </summary>
        public void ClearTargetGreens()
        {
            foreach (var green in _targetGreens)
            {
                if (green != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(green.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(green.gameObject);
                    }
                }
            }
            _targetGreens.Clear();
        }

        /// <summary>
        /// Clear all environment objects (markers, greens, tee mat).
        /// </summary>
        public void ClearAll()
        {
            ClearDistanceMarkers();
            ClearTargetGreens();

            if (_teeMat != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_teeMat.gameObject);
                }
                else
                {
                    DestroyImmediate(_teeMat.gameObject);
                }
                _teeMat = null;
            }
        }

        /// <summary>
        /// Set the distance marker prefab.
        /// </summary>
        public void SetDistanceMarkerPrefab(DistanceMarker prefab)
        {
            _distanceMarkerPrefab = prefab;
        }

        /// <summary>
        /// Set the target green prefab.
        /// </summary>
        public void SetTargetGreenPrefab(TargetGreen prefab)
        {
            _targetGreenPrefab = prefab;
        }

        /// <summary>
        /// Set the tee mat prefab.
        /// </summary>
        public void SetTeeMatPrefab(TeeMat prefab)
        {
            _teeMatPrefab = prefab;
        }

        /// <summary>
        /// Set whether to show distance markers.
        /// </summary>
        public void SetShowDistanceMarkers(bool show)
        {
            _showDistanceMarkers = show;
            foreach (var marker in _distanceMarkers)
            {
                if (marker != null)
                {
                    marker.gameObject.SetActive(show);
                }
            }
        }

        /// <summary>
        /// Set whether to show target greens.
        /// </summary>
        public void SetShowTargetGreens(bool show)
        {
            _showTargetGreens = show;
            foreach (var green in _targetGreens)
            {
                if (green != null)
                {
                    green.gameObject.SetActive(show);
                }
            }
        }

        /// <summary>
        /// Get distance marker at a specific distance.
        /// </summary>
        /// <param name="distanceYards">Distance in yards.</param>
        /// <returns>The marker at that distance, or null if not found.</returns>
        public DistanceMarker GetMarkerAtDistance(int distanceYards)
        {
            foreach (var marker in _distanceMarkers)
            {
                if (marker != null && marker.Distance == distanceYards)
                {
                    return marker;
                }
            }
            return null;
        }

        /// <summary>
        /// Convert yards to Unity units (meters).
        /// </summary>
        /// <param name="yards">Distance in yards.</param>
        /// <returns>Distance in Unity units (meters).</returns>
        public static float ConvertYardsToUnits(float yards)
        {
            return yards * YardsToMeters;
        }

        /// <summary>
        /// Convert Unity units (meters) to yards.
        /// </summary>
        /// <param name="units">Distance in Unity units (meters).</param>
        /// <returns>Distance in yards.</returns>
        public static float ConvertUnitsToYards(float units)
        {
            return units / YardsToMeters;
        }

        /// <summary>
        /// Force initialization of singleton (for testing).
        /// </summary>
        public void ForceInitializeSingleton()
        {
            SetupSingleton();
        }
    }
}
