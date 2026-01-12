// ABOUTME: Singleton controller for managing water rendering with planar reflections and quality tiers.
// ABOUTME: Integrates with QualityManager for quality tier adjustments and manages reflection cameras.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using OpenRange.Core;
using CoreQualityTier = OpenRange.Core.QualityTier;

namespace OpenRange.Visualization
{
    /// <summary>
    /// Preset configurations for water appearance.
    /// </summary>
    public enum WaterPreset
    {
        Ocean,
        Pond,
        Custom
    }

    /// <summary>
    /// Configuration settings for different water presets.
    /// </summary>
    [Serializable]
    public struct WaterPresetSettings
    {
        public Color ShallowColor;
        public Color DeepColor;
        public float DepthFadeDistance;
        public float WaveHeight;
        public float WaveSpeed;
        public Color FoamColor;
        public float FoamWidth;
        public float ReflectionStrength;

        public static WaterPresetSettings Ocean => new WaterPresetSettings
        {
            ShallowColor = new Color(0.2f, 0.6f, 0.8f, 0.8f),
            DeepColor = new Color(0.05f, 0.2f, 0.4f, 1.0f),
            DepthFadeDistance = 8.0f,
            WaveHeight = 0.3f,
            WaveSpeed = 1.0f,
            FoamColor = new Color(1.0f, 1.0f, 1.0f, 0.8f),
            FoamWidth = 0.5f,
            ReflectionStrength = 0.5f
        };

        public static WaterPresetSettings Pond => new WaterPresetSettings
        {
            ShallowColor = new Color(0.3f, 0.5f, 0.4f, 0.7f),
            DeepColor = new Color(0.1f, 0.25f, 0.2f, 1.0f),
            DepthFadeDistance = 3.0f,
            WaveHeight = 0.05f,
            WaveSpeed = 0.5f,
            FoamColor = new Color(0.9f, 0.95f, 0.9f, 0.5f),
            FoamWidth = 0.2f,
            ReflectionStrength = 0.7f
        };

        public static WaterPresetSettings GetPreset(WaterPreset preset)
        {
            switch (preset)
            {
                case WaterPreset.Ocean:
                    return Ocean;
                case WaterPreset.Pond:
                    return Pond;
                default:
                    return Ocean;
            }
        }
    }

    /// <summary>
    /// Quality tier settings for water rendering.
    /// </summary>
    [Serializable]
    public struct WaterQualitySettings
    {
        public bool EnableWaves;
        public bool EnableFoam;
        public bool EnableReflection;
        public bool EnablePlanarReflection;
        public int ReflectionTextureSize;

        public static WaterQualitySettings High => new WaterQualitySettings
        {
            EnableWaves = true,
            EnableFoam = true,
            EnableReflection = true,
            EnablePlanarReflection = true,
            ReflectionTextureSize = 512
        };

        public static WaterQualitySettings Medium => new WaterQualitySettings
        {
            EnableWaves = true,
            EnableFoam = true,
            EnableReflection = true,
            EnablePlanarReflection = false,
            ReflectionTextureSize = 256
        };

        public static WaterQualitySettings Low => new WaterQualitySettings
        {
            EnableWaves = false,
            EnableFoam = false,
            EnableReflection = false,
            EnablePlanarReflection = false,
            ReflectionTextureSize = 128
        };

        public static WaterQualitySettings GetSettings(CoreQualityTier tier)
        {
            switch (tier)
            {
                case CoreQualityTier.High:
                    return High;
                case CoreQualityTier.Medium:
                    return Medium;
                case CoreQualityTier.Low:
                    return Low;
                default:
                    return Medium;
            }
        }
    }

    /// <summary>
    /// Controls water rendering features including planar reflections and quality settings.
    /// </summary>
    public class WaterController : MonoBehaviour
    {
        // Shader property IDs for performance
        private static readonly int ShallowColorId = Shader.PropertyToID("_ShallowColor");
        private static readonly int DeepColorId = Shader.PropertyToID("_DeepColor");
        private static readonly int DepthFadeDistanceId = Shader.PropertyToID("_DepthFadeDistance");
        private static readonly int WaveHeightId = Shader.PropertyToID("_WaveHeight");
        private static readonly int WaveSpeedId = Shader.PropertyToID("_WaveSpeed");
        private static readonly int FoamColorId = Shader.PropertyToID("_FoamColor");
        private static readonly int FoamWidthId = Shader.PropertyToID("_FoamWidth");
        private static readonly int ReflectionStrengthId = Shader.PropertyToID("_ReflectionStrength");
        private static readonly int ReflectionTexId = Shader.PropertyToID("_ReflectionTex");

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static WaterController Instance { get; private set; }

        [Header("Water Settings")]
        [SerializeField] private WaterPreset _currentPreset = WaterPreset.Ocean;
        [SerializeField] private Material _waterMaterial;

        [Header("Reflection Settings")]
        [SerializeField] private LayerMask _reflectionLayers = -1;
        [SerializeField] private float _reflectionClipPlaneOffset = 0.07f;

        [Header("Quality Settings")]
        [SerializeField] private bool _waterEnabled = true;

        private Camera _reflectionCamera;
        private RenderTexture _reflectionTexture;
        private CoreQualityTier _currentQualityTier = CoreQualityTier.High;
        private WaterQualitySettings _qualitySettings;
        private WaterPresetSettings _presetSettings;
        private bool _isInitialized;
        private Transform _waterPlaneTransform;
        private List<Renderer> _waterRenderers = new List<Renderer>();

        /// <summary>
        /// Whether water rendering is enabled.
        /// </summary>
        public bool WaterEnabled
        {
            get => _waterEnabled;
            set
            {
                if (_waterEnabled != value)
                {
                    _waterEnabled = value;
                    UpdateWaterState();
                    OnWaterChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Current water preset.
        /// </summary>
        public WaterPreset CurrentPreset
        {
            get => _currentPreset;
            set
            {
                if (_currentPreset != value)
                {
                    _currentPreset = value;
                    _presetSettings = WaterPresetSettings.GetPreset(value);
                    ApplyPresetToMaterial();
                    OnWaterChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// Current water material.
        /// </summary>
        public Material WaterMaterial
        {
            get => _waterMaterial;
            set
            {
                if (_waterMaterial != value)
                {
                    _waterMaterial = value;
                    ApplyMaterialToRenderers();
                    ApplyPresetToMaterial();
                    ApplyQualitySettings();
                }
            }
        }

        /// <summary>
        /// Current quality tier.
        /// </summary>
        public CoreQualityTier CurrentQualityTier => _currentQualityTier;

        /// <summary>
        /// Current quality settings.
        /// </summary>
        public WaterQualitySettings QualitySettings => _qualitySettings;

        /// <summary>
        /// Current preset settings.
        /// </summary>
        public WaterPresetSettings PresetSettings => _presetSettings;

        /// <summary>
        /// Whether the controller is initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Whether planar reflection is currently active.
        /// </summary>
        public bool IsPlanarReflectionActive => _reflectionCamera != null && _reflectionCamera.enabled;

        /// <summary>
        /// Fired when any water parameter changes.
        /// </summary>
        public event Action OnWaterChanged;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Initialize();
        }

        private void LateUpdate()
        {
            if (!_isInitialized || !_waterEnabled)
            {
                return;
            }

            // Update planar reflection if enabled
            if (_qualitySettings.EnablePlanarReflection && _reflectionCamera != null)
            {
                UpdatePlanarReflection();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                UnsubscribeFromManagers();
                CleanupReflectionResources();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the water controller.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _presetSettings = WaterPresetSettings.GetPreset(_currentPreset);

            // Subscribe to QualityManager
            if (QualityManager.Instance != null)
            {
                _currentQualityTier = QualityManager.Instance.EffectiveTier;
                QualityManager.Instance.OnQualityTierChanged += HandleQualityTierChanged;
            }

            _qualitySettings = WaterQualitySettings.GetSettings(_currentQualityTier);

            ApplyPresetToMaterial();
            ApplyQualitySettings();
            SetupPlanarReflection();

            _isInitialized = true;
        }

        /// <summary>
        /// Registers a water renderer to be managed by this controller.
        /// </summary>
        /// <param name="renderer">The water renderer to register.</param>
        public void RegisterWaterRenderer(Renderer renderer)
        {
            if (renderer != null && !_waterRenderers.Contains(renderer))
            {
                _waterRenderers.Add(renderer);

                if (_waterPlaneTransform == null)
                {
                    _waterPlaneTransform = renderer.transform;
                }

                // Apply current material
                if (_waterMaterial != null)
                {
                    renderer.sharedMaterial = _waterMaterial;
                }
            }
        }

        /// <summary>
        /// Unregisters a water renderer.
        /// </summary>
        /// <param name="renderer">The water renderer to unregister.</param>
        public void UnregisterWaterRenderer(Renderer renderer)
        {
            _waterRenderers.Remove(renderer);

            if (_waterPlaneTransform == renderer?.transform)
            {
                _waterPlaneTransform = _waterRenderers.Count > 0 ? _waterRenderers[0].transform : null;
            }
        }

        /// <summary>
        /// Sets the water plane transform for reflection calculations.
        /// </summary>
        /// <param name="planeTransform">The transform representing the water surface.</param>
        public void SetWaterPlane(Transform planeTransform)
        {
            _waterPlaneTransform = planeTransform;
        }

        /// <summary>
        /// Forces an immediate update of all water settings.
        /// </summary>
        public void ForceUpdate()
        {
            ApplyPresetToMaterial();
            ApplyQualitySettings();

            if (_qualitySettings.EnablePlanarReflection)
            {
                SetupPlanarReflection();
            }
        }

        /// <summary>
        /// Applies a custom preset settings to the water.
        /// </summary>
        /// <param name="settings">The custom settings to apply.</param>
        public void ApplyCustomSettings(WaterPresetSettings settings)
        {
            _currentPreset = WaterPreset.Custom;
            _presetSettings = settings;
            ApplyPresetToMaterial();
            OnWaterChanged?.Invoke();
        }

        /// <summary>
        /// Forces initialization of the singleton instance.
        /// Used in EditMode tests where Awake() is not called.
        /// </summary>
        internal void ForceInitializeSingleton()
        {
            Instance = this;
            Initialize();
        }

        #endregion

        #region Private Methods

        private void UpdateWaterState()
        {
            foreach (var renderer in _waterRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = _waterEnabled;
                }
            }

            if (_reflectionCamera != null)
            {
                _reflectionCamera.enabled = _waterEnabled && _qualitySettings.EnablePlanarReflection;
            }
        }

        private void ApplyMaterialToRenderers()
        {
            if (_waterMaterial == null)
            {
                return;
            }

            foreach (var renderer in _waterRenderers)
            {
                if (renderer != null)
                {
                    renderer.sharedMaterial = _waterMaterial;
                }
            }
        }

        private void ApplyPresetToMaterial()
        {
            if (_waterMaterial == null)
            {
                return;
            }

            _waterMaterial.SetColor(ShallowColorId, _presetSettings.ShallowColor);
            _waterMaterial.SetColor(DeepColorId, _presetSettings.DeepColor);
            _waterMaterial.SetFloat(DepthFadeDistanceId, _presetSettings.DepthFadeDistance);
            _waterMaterial.SetFloat(WaveHeightId, _presetSettings.WaveHeight);
            _waterMaterial.SetFloat(WaveSpeedId, _presetSettings.WaveSpeed);
            _waterMaterial.SetColor(FoamColorId, _presetSettings.FoamColor);
            _waterMaterial.SetFloat(FoamWidthId, _presetSettings.FoamWidth);
            _waterMaterial.SetFloat(ReflectionStrengthId, _presetSettings.ReflectionStrength);
        }

        private void ApplyQualitySettings()
        {
            if (_waterMaterial == null)
            {
                return;
            }

            // Set shader keywords based on quality settings
            SetKeyword(_waterMaterial, "_ENABLEWAVES_ON", _qualitySettings.EnableWaves);
            SetKeyword(_waterMaterial, "_ENABLEFOAM_ON", _qualitySettings.EnableFoam);
            SetKeyword(_waterMaterial, "_ENABLEREFLECTION_ON", _qualitySettings.EnableReflection);

            // Update reflection camera state
            if (_reflectionCamera != null)
            {
                _reflectionCamera.enabled = _waterEnabled && _qualitySettings.EnablePlanarReflection;
            }

            // Resize reflection texture if needed
            if (_qualitySettings.EnablePlanarReflection && _reflectionTexture != null)
            {
                if (_reflectionTexture.width != _qualitySettings.ReflectionTextureSize)
                {
                    ResizeReflectionTexture();
                }
            }
        }

        private void SetKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }

        private void SetupPlanarReflection()
        {
            if (!_qualitySettings.EnablePlanarReflection)
            {
                CleanupReflectionResources();
                return;
            }

            // Create reflection camera if needed
            if (_reflectionCamera == null)
            {
                var reflectionGo = new GameObject("WaterReflectionCamera");
                reflectionGo.transform.SetParent(transform);
                _reflectionCamera = reflectionGo.AddComponent<Camera>();
                _reflectionCamera.enabled = false;
                _reflectionCamera.cullingMask = _reflectionLayers;

                // Copy settings from main camera
                var mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    _reflectionCamera.clearFlags = mainCamera.clearFlags;
                    _reflectionCamera.backgroundColor = mainCamera.backgroundColor;
                    _reflectionCamera.farClipPlane = mainCamera.farClipPlane;
                    _reflectionCamera.nearClipPlane = mainCamera.nearClipPlane;
                    _reflectionCamera.fieldOfView = mainCamera.fieldOfView;

                    // Add URP camera data if needed
                    var mainCameraData = mainCamera.GetComponent<UniversalAdditionalCameraData>();
                    if (mainCameraData != null)
                    {
                        var reflectionCameraData = _reflectionCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                        reflectionCameraData.renderShadows = false;
                        reflectionCameraData.requiresColorTexture = false;
                        reflectionCameraData.requiresDepthTexture = false;
                    }
                }
            }

            // Create reflection texture if needed
            if (_reflectionTexture == null || _reflectionTexture.width != _qualitySettings.ReflectionTextureSize)
            {
                CreateReflectionTexture();
            }

            _reflectionCamera.targetTexture = _reflectionTexture;
            _reflectionCamera.enabled = _waterEnabled;
        }

        private void CreateReflectionTexture()
        {
            if (_reflectionTexture != null)
            {
                _reflectionTexture.Release();
                DestroyImmediate(_reflectionTexture);
            }

            int size = _qualitySettings.ReflectionTextureSize;
            _reflectionTexture = new RenderTexture(size, size, 16, RenderTextureFormat.ARGB32);
            _reflectionTexture.name = "WaterReflection";
            _reflectionTexture.filterMode = FilterMode.Bilinear;
            _reflectionTexture.Create();

            if (_waterMaterial != null)
            {
                _waterMaterial.SetTexture(ReflectionTexId, _reflectionTexture);
            }
        }

        private void ResizeReflectionTexture()
        {
            CreateReflectionTexture();
            if (_reflectionCamera != null)
            {
                _reflectionCamera.targetTexture = _reflectionTexture;
            }
        }

        private void UpdatePlanarReflection()
        {
            if (_reflectionCamera == null || Camera.main == null || _waterPlaneTransform == null)
            {
                return;
            }

            var mainCamera = Camera.main;
            var waterPos = _waterPlaneTransform.position;
            var waterNormal = _waterPlaneTransform.up;

            // Calculate reflection matrix
            float d = -Vector3.Dot(waterNormal, waterPos) - _reflectionClipPlaneOffset;
            Vector4 reflectionPlane = new Vector4(waterNormal.x, waterNormal.y, waterNormal.z, d);

            Matrix4x4 reflectionMatrix = CalculateReflectionMatrix(reflectionPlane);

            // Update reflection camera position and rotation
            _reflectionCamera.worldToCameraMatrix = mainCamera.worldToCameraMatrix * reflectionMatrix;

            // Calculate oblique projection matrix for proper clipping
            Vector4 clipPlane = CameraSpacePlane(_reflectionCamera, waterPos, waterNormal, 1.0f);
            _reflectionCamera.projectionMatrix = mainCamera.CalculateObliqueMatrix(clipPlane);

            // Render
            _reflectionCamera.Render();
        }

        private Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
        {
            Matrix4x4 reflectionMat = Matrix4x4.identity;

            reflectionMat.m00 = 1 - 2 * plane.x * plane.x;
            reflectionMat.m01 = -2 * plane.x * plane.y;
            reflectionMat.m02 = -2 * plane.x * plane.z;
            reflectionMat.m03 = -2 * plane.x * plane.w;

            reflectionMat.m10 = -2 * plane.y * plane.x;
            reflectionMat.m11 = 1 - 2 * plane.y * plane.y;
            reflectionMat.m12 = -2 * plane.y * plane.z;
            reflectionMat.m13 = -2 * plane.y * plane.w;

            reflectionMat.m20 = -2 * plane.z * plane.x;
            reflectionMat.m21 = -2 * plane.z * plane.y;
            reflectionMat.m22 = 1 - 2 * plane.z * plane.z;
            reflectionMat.m23 = -2 * plane.z * plane.w;

            reflectionMat.m30 = 0;
            reflectionMat.m31 = 0;
            reflectionMat.m32 = 0;
            reflectionMat.m33 = 1;

            return reflectionMat;
        }

        private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
        {
            Vector3 offsetPos = pos + normal * _reflectionClipPlaneOffset;
            Matrix4x4 m = cam.worldToCameraMatrix;
            Vector3 cpos = m.MultiplyPoint(offsetPos);
            Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
        }

        private void CleanupReflectionResources()
        {
            if (_reflectionCamera != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_reflectionCamera.gameObject);
                }
                else
                {
                    DestroyImmediate(_reflectionCamera.gameObject);
                }
                _reflectionCamera = null;
            }

            if (_reflectionTexture != null)
            {
                _reflectionTexture.Release();
                if (Application.isPlaying)
                {
                    Destroy(_reflectionTexture);
                }
                else
                {
                    DestroyImmediate(_reflectionTexture);
                }
                _reflectionTexture = null;
            }
        }

        private void HandleQualityTierChanged(CoreQualityTier tier)
        {
            if (_currentQualityTier == tier)
            {
                return;
            }

            _currentQualityTier = tier;
            _qualitySettings = WaterQualitySettings.GetSettings(tier);
            ApplyQualitySettings();

            if (_qualitySettings.EnablePlanarReflection)
            {
                SetupPlanarReflection();
            }
            else
            {
                CleanupReflectionResources();
            }

            OnWaterChanged?.Invoke();
        }

        private void UnsubscribeFromManagers()
        {
            if (QualityManager.Instance != null)
            {
                QualityManager.Instance.OnQualityTierChanged -= HandleQualityTierChanged;
            }
        }

        #endregion
    }
}
