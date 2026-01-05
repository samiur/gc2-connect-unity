// ABOUTME: Controls scene lighting and skybox based on quality tier and visual presets.
// ABOUTME: Manages directional light, ambient settings, reflection probes, and skybox material.

using System;
using UnityEngine;
using UnityEngine.Rendering;
using OpenRange.Core;
using CoreQualityTier = OpenRange.Core.QualityTier;

namespace OpenRange.Visualization
{
    /// <summary>
    /// Controls scene lighting configuration based on quality tier and visual presets.
    /// Manages skybox, directional light, ambient settings, and reflection probes.
    /// </summary>
    public class LightingController : MonoBehaviour
    {
        /// <summary>
        /// Visual preset for scene lighting.
        /// </summary>
        public enum LightingPreset
        {
            /// <summary>Warm golden hour lighting.</summary>
            GoldenHour,

            /// <summary>Bright midday lighting.</summary>
            Midday,

            /// <summary>Soft overcast lighting.</summary>
            Overcast
        }

        [Header("References")]
        [SerializeField] private Light _directionalLight;
        [SerializeField] private ReflectionProbe _reflectionProbe;
        [SerializeField] private Material _skyboxMaterial;

        [Header("Settings")]
        [SerializeField] private LightingPreset _currentPreset = LightingPreset.GoldenHour;

        private CoreQualityTier _currentQualityTier = CoreQualityTier.High;
        private bool _isInitialized;

        /// <summary>
        /// The current lighting preset.
        /// </summary>
        public LightingPreset CurrentPreset => _currentPreset;

        /// <summary>
        /// The current quality tier affecting lighting.
        /// </summary>
        public CoreQualityTier CurrentQualityTier => _currentQualityTier;

        /// <summary>
        /// Whether the controller has been initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// The directional light reference.
        /// </summary>
        public Light DirectionalLight => _directionalLight;

        /// <summary>
        /// The reflection probe reference.
        /// </summary>
        public ReflectionProbe ReflectionProbe => _reflectionProbe;

        /// <summary>
        /// The skybox material reference.
        /// </summary>
        public Material SkyboxMaterial => _skyboxMaterial;

        /// <summary>
        /// Fired when the lighting preset changes.
        /// </summary>
        public event Action<LightingPreset> OnPresetChanged;

        /// <summary>
        /// Fired when the quality tier changes.
        /// </summary>
        public event Action<CoreQualityTier> OnQualityTierChanged;

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            UnsubscribeFromQualityManager();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the lighting controller.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            // Subscribe to quality manager if available
            if (QualityManager.Instance != null)
            {
                _currentQualityTier = QualityManager.Instance.EffectiveTier;
                QualityManager.Instance.OnQualityTierChanged += HandleQualityTierChanged;
            }

            ApplyPreset(_currentPreset);
            ApplyQualitySettings(_currentQualityTier);

            _isInitialized = true;
        }

        /// <summary>
        /// Sets and applies a lighting preset.
        /// </summary>
        /// <param name="preset">The preset to apply.</param>
        public void SetPreset(LightingPreset preset)
        {
            if (_currentPreset == preset)
            {
                return;
            }

            _currentPreset = preset;
            ApplyPreset(preset);
            OnPresetChanged?.Invoke(preset);
        }

        /// <summary>
        /// Sets and applies a quality tier for lighting.
        /// </summary>
        /// <param name="tier">The quality tier to apply.</param>
        public void SetQualityTier(CoreQualityTier tier)
        {
            if (_currentQualityTier == tier)
            {
                return;
            }

            _currentQualityTier = tier;
            ApplyQualitySettings(tier);
            OnQualityTierChanged?.Invoke(tier);
        }

        /// <summary>
        /// Gets preset configuration values.
        /// </summary>
        /// <param name="preset">The preset to get configuration for.</param>
        /// <returns>The preset configuration.</returns>
        public LightingPresetConfig GetPresetConfig(LightingPreset preset)
        {
            return preset switch
            {
                LightingPreset.GoldenHour => new LightingPresetConfig
                {
                    SkyTopColor = new Color(0.1f, 0.3f, 0.8f),
                    SkyHorizonColor = new Color(1.0f, 0.6f, 0.4f),
                    SunColor = new Color(1.5f, 1.4f, 1.0f),
                    SunDirection = new Vector3(0.3f, 0.4f, -0.8f),
                    LightColor = new Color(1.0f, 0.95f, 0.85f),
                    LightIntensity = 1.2f,
                    AmbientSkyColor = new Color(0.4f, 0.5f, 0.7f),
                    AmbientEquatorColor = new Color(0.6f, 0.5f, 0.4f),
                    AmbientGroundColor = new Color(0.3f, 0.25f, 0.2f)
                },
                LightingPreset.Midday => new LightingPresetConfig
                {
                    SkyTopColor = new Color(0.15f, 0.4f, 0.9f),
                    SkyHorizonColor = new Color(0.7f, 0.85f, 1.0f),
                    SunColor = new Color(1.2f, 1.2f, 1.0f),
                    SunDirection = new Vector3(0.1f, 0.9f, -0.3f),
                    LightColor = new Color(1.0f, 1.0f, 0.95f),
                    LightIntensity = 1.5f,
                    AmbientSkyColor = new Color(0.5f, 0.6f, 0.8f),
                    AmbientEquatorColor = new Color(0.6f, 0.65f, 0.7f),
                    AmbientGroundColor = new Color(0.35f, 0.3f, 0.25f)
                },
                LightingPreset.Overcast => new LightingPresetConfig
                {
                    SkyTopColor = new Color(0.4f, 0.45f, 0.5f),
                    SkyHorizonColor = new Color(0.6f, 0.65f, 0.7f),
                    SunColor = new Color(0.8f, 0.8f, 0.8f),
                    SunDirection = new Vector3(0.2f, 0.6f, -0.5f),
                    LightColor = new Color(0.9f, 0.9f, 0.95f),
                    LightIntensity = 0.8f,
                    AmbientSkyColor = new Color(0.5f, 0.55f, 0.6f),
                    AmbientEquatorColor = new Color(0.5f, 0.5f, 0.55f),
                    AmbientGroundColor = new Color(0.3f, 0.3f, 0.32f)
                },
                _ => GetPresetConfig(LightingPreset.GoldenHour)
            };
        }

        /// <summary>
        /// Gets quality-specific lighting settings.
        /// </summary>
        /// <param name="tier">The quality tier.</param>
        /// <returns>The quality lighting settings.</returns>
        public QualityLightingSettings GetQualitySettings(CoreQualityTier tier)
        {
            return tier switch
            {
                CoreQualityTier.Low => new QualityLightingSettings
                {
                    EnableClouds = false,
                    EnableSun = true,
                    EnableShadows = false,
                    EnableReflectionProbe = false,
                    ShadowType = LightShadows.None,
                    UseSolidColorSkybox = true
                },
                CoreQualityTier.Medium => new QualityLightingSettings
                {
                    EnableClouds = true,
                    EnableSun = true,
                    EnableShadows = true,
                    EnableReflectionProbe = false,
                    ShadowType = LightShadows.Hard,
                    UseSolidColorSkybox = false
                },
                CoreQualityTier.High => new QualityLightingSettings
                {
                    EnableClouds = true,
                    EnableSun = true,
                    EnableShadows = true,
                    EnableReflectionProbe = true,
                    ShadowType = LightShadows.Soft,
                    UseSolidColorSkybox = false
                },
                _ => GetQualitySettings(CoreQualityTier.Medium)
            };
        }

        /// <summary>
        /// Forces the reflection probe to re-bake.
        /// </summary>
        public void RefreshReflectionProbe()
        {
            if (_reflectionProbe != null && _reflectionProbe.gameObject.activeInHierarchy)
            {
                _reflectionProbe.RenderProbe();
            }
        }

        #endregion

        #region Private Methods

        private void ApplyPreset(LightingPreset preset)
        {
            var config = GetPresetConfig(preset);

            // Apply skybox material settings
            if (_skyboxMaterial != null)
            {
                _skyboxMaterial.SetColor("_TopColor", config.SkyTopColor);
                _skyboxMaterial.SetColor("_HorizonColor", config.SkyHorizonColor);
                _skyboxMaterial.SetColor("_SunColor", config.SunColor);
                _skyboxMaterial.SetVector("_SunDirection", new Vector4(
                    config.SunDirection.x,
                    config.SunDirection.y,
                    config.SunDirection.z,
                    0f
                ));
            }

            // Apply directional light settings
            if (_directionalLight != null)
            {
                _directionalLight.color = config.LightColor;
                _directionalLight.intensity = config.LightIntensity;
                _directionalLight.transform.rotation = Quaternion.LookRotation(-config.SunDirection);
            }

            // Apply ambient settings
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = config.AmbientSkyColor;
            RenderSettings.ambientEquatorColor = config.AmbientEquatorColor;
            RenderSettings.ambientGroundColor = config.AmbientGroundColor;

            // Apply skybox
            if (_skyboxMaterial != null)
            {
                RenderSettings.skybox = _skyboxMaterial;
            }
        }

        private void ApplyQualitySettings(CoreQualityTier tier)
        {
            var settings = GetQualitySettings(tier);

            // Apply skybox quality settings
            if (_skyboxMaterial != null)
            {
                _skyboxMaterial.SetFloat("_EnableClouds", settings.EnableClouds ? 1f : 0f);
                _skyboxMaterial.SetFloat("_EnableSun", settings.EnableSun ? 1f : 0f);

                // Enable/disable shader keywords
                if (settings.EnableClouds)
                {
                    _skyboxMaterial.EnableKeyword("_ENABLECLOUDS_ON");
                }
                else
                {
                    _skyboxMaterial.DisableKeyword("_ENABLECLOUDS_ON");
                }

                if (settings.EnableSun)
                {
                    _skyboxMaterial.EnableKeyword("_ENABLESUN_ON");
                }
                else
                {
                    _skyboxMaterial.DisableKeyword("_ENABLESUN_ON");
                }

                // For low quality, use simplified rendering
                if (settings.UseSolidColorSkybox)
                {
                    _skyboxMaterial.SetFloat("_CloudDensity", 0f);
                }
            }

            // Apply shadow settings
            if (_directionalLight != null)
            {
                _directionalLight.shadows = settings.ShadowType;
            }

            // Apply reflection probe settings
            if (_reflectionProbe != null)
            {
                _reflectionProbe.gameObject.SetActive(settings.EnableReflectionProbe);
            }
        }

        private void HandleQualityTierChanged(CoreQualityTier tier)
        {
            SetQualityTier(tier);
        }

        private void UnsubscribeFromQualityManager()
        {
            if (QualityManager.Instance != null)
            {
                QualityManager.Instance.OnQualityTierChanged -= HandleQualityTierChanged;
            }
        }

        #endregion
    }

    /// <summary>
    /// Configuration values for a lighting preset.
    /// </summary>
    public struct LightingPresetConfig
    {
        /// <summary>Sky gradient top color.</summary>
        public Color SkyTopColor;

        /// <summary>Sky gradient horizon color.</summary>
        public Color SkyHorizonColor;

        /// <summary>Sun color (HDR).</summary>
        public Color SunColor;

        /// <summary>Sun direction vector.</summary>
        public Vector3 SunDirection;

        /// <summary>Directional light color.</summary>
        public Color LightColor;

        /// <summary>Directional light intensity.</summary>
        public float LightIntensity;

        /// <summary>Ambient sky color.</summary>
        public Color AmbientSkyColor;

        /// <summary>Ambient equator color.</summary>
        public Color AmbientEquatorColor;

        /// <summary>Ambient ground color.</summary>
        public Color AmbientGroundColor;
    }

    /// <summary>
    /// Quality-specific lighting settings.
    /// </summary>
    public struct QualityLightingSettings
    {
        /// <summary>Whether clouds are enabled in the skybox.</summary>
        public bool EnableClouds;

        /// <summary>Whether the sun is enabled in the skybox.</summary>
        public bool EnableSun;

        /// <summary>Whether shadows are enabled.</summary>
        public bool EnableShadows;

        /// <summary>Whether the reflection probe is enabled.</summary>
        public bool EnableReflectionProbe;

        /// <summary>The shadow type for the directional light.</summary>
        public LightShadows ShadowType;

        /// <summary>Whether to use a simplified solid color skybox.</summary>
        public bool UseSolidColorSkybox;
    }
}
