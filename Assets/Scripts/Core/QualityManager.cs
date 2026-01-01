// ABOUTME: Manages graphics quality tiers and URP asset switching based on platform capabilities.
// ABOUTME: Provides auto-detection, dynamic adjustment, and per-tier settings for optimal performance.

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using URPShadowQuality = UnityEngine.Rendering.Universal.ShadowQuality;

namespace OpenRange.Core
{
    /// <summary>
    /// Manages graphics quality tiers and applies appropriate settings.
    /// Singleton that persists across scenes.
    /// </summary>
    public class QualityManager : MonoBehaviour
    {
        private const int FrameSampleSize = 60;
        private const float DynamicAdjustmentInterval = 1f;
        private const float DowngradeThreshold = 0.8f;
        private const float DowngradeTimeRequired = 5f;

        public static QualityManager Instance { get; private set; }

        [Header("URP Assets")]
        [Tooltip("Low quality URP asset for low-end devices.")]
        [SerializeField] private UniversalRenderPipelineAsset _lowQualityAsset;

        [Tooltip("Medium quality URP asset for mid-range devices.")]
        [SerializeField] private UniversalRenderPipelineAsset _mediumQualityAsset;

        [Tooltip("High quality URP asset for high-end devices.")]
        [SerializeField] private UniversalRenderPipelineAsset _highQualityAsset;

        [Header("Settings")]
        [SerializeField] private bool _enableDynamicAdjustment = true;
        [SerializeField] private bool _enableDebugLogging = false;

        private QualityTier _currentTier = QualityTier.Auto;
        private QualityTier _effectiveTier = QualityTier.Medium;
        private int _targetFrameRate = 60;
        private bool _isInitialized;

        private float[] _frameTimes;
        private int _frameTimeIndex;
        private float _timeUnderTarget;
        private Coroutine _dynamicAdjustmentCoroutine;

        /// <summary>
        /// The current quality tier setting (may be Auto).
        /// </summary>
        public QualityTier CurrentTier => _currentTier;

        /// <summary>
        /// The effective quality tier being applied (never Auto).
        /// </summary>
        public QualityTier EffectiveTier => _effectiveTier;

        /// <summary>
        /// The current target frame rate.
        /// </summary>
        public int TargetFrameRate => _targetFrameRate;

        /// <summary>
        /// Whether the manager has been initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Whether dynamic adjustment is enabled.
        /// </summary>
        public bool DynamicAdjustmentEnabled => _enableDynamicAdjustment;

        /// <summary>
        /// Fired when the quality tier changes.
        /// </summary>
        public event Action<QualityTier> OnQualityTierChanged;

        /// <summary>
        /// Fired when dynamic adjustment triggers a downgrade.
        /// </summary>
        public event Action<QualityTier, QualityTier> OnDynamicDowngrade;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                StopDynamicAdjustment();
            }
        }

        private void Update()
        {
            if (_enableDynamicAdjustment && _isInitialized && _frameTimes != null)
            {
                RecordFrameTime();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the quality manager and applies the initial quality tier.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _frameTimes = new float[FrameSampleSize];
            _frameTimeIndex = 0;

            var settingsManager = SettingsManager.Instance;
            if (settingsManager != null && settingsManager.IsInitialized)
            {
                _currentTier = settingsManager.QualityTier;
            }
            else
            {
                _currentTier = QualityTier.Auto;
            }

            if (_currentTier == QualityTier.Auto)
            {
                _effectiveTier = DetectOptimalTier();
            }
            else
            {
                _effectiveTier = _currentTier;
            }

            ApplyTierSettings(_effectiveTier);
            _isInitialized = true;

            if (_enableDynamicAdjustment)
            {
                StartDynamicAdjustment();
            }

            if (_enableDebugLogging)
            {
                Debug.Log($"QualityManager: Initialized with tier {_effectiveTier} (setting: {_currentTier})");
            }
        }

        /// <summary>
        /// Detects the optimal quality tier for the current platform.
        /// </summary>
        /// <returns>The recommended quality tier.</returns>
        public QualityTier DetectOptimalTier()
        {
            var platform = PlatformManager.CurrentPlatform;

            switch (platform)
            {
                case PlatformManager.Platform.Mac:
                    return PlatformManager.IsAppleSilicon() ? QualityTier.High : QualityTier.Medium;

                case PlatformManager.Platform.iPad:
                    return DetectIPadTier();

                case PlatformManager.Platform.Android:
                    return DetectAndroidTier();

                case PlatformManager.Platform.Windows:
                    return DetectWindowsTier();

                case PlatformManager.Platform.Editor:
                    return QualityTier.High;

                default:
                    return QualityTier.Medium;
            }
        }

        /// <summary>
        /// Sets and applies a quality tier.
        /// </summary>
        /// <param name="tier">The quality tier to apply.</param>
        public void ApplyQualityTier(QualityTier tier)
        {
            var previousTier = _currentTier;
            _currentTier = tier;

            if (tier == QualityTier.Auto)
            {
                _effectiveTier = DetectOptimalTier();
            }
            else
            {
                _effectiveTier = tier;
            }

            ApplyTierSettings(_effectiveTier);

            if (_enableDebugLogging)
            {
                Debug.Log($"QualityManager: Applied tier {_effectiveTier} (setting: {_currentTier})");
            }

            if (previousTier != _currentTier)
            {
                OnQualityTierChanged?.Invoke(_currentTier);
            }
        }

        /// <summary>
        /// Enables or disables dynamic frame rate adjustment.
        /// </summary>
        /// <param name="enable">Whether to enable dynamic adjustment.</param>
        public void EnableDynamicAdjustment(bool enable)
        {
            if (_enableDynamicAdjustment == enable)
            {
                return;
            }

            _enableDynamicAdjustment = enable;

            if (enable && _isInitialized)
            {
                StartDynamicAdjustment();
            }
            else
            {
                StopDynamicAdjustment();
            }

            if (_enableDebugLogging)
            {
                Debug.Log($"QualityManager: Dynamic adjustment {(enable ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// Gets the current average FPS over the sample window.
        /// </summary>
        /// <returns>Average FPS, or 0 if not enough samples.</returns>
        public float GetAverageFPS()
        {
            if (_frameTimes == null || _frameTimeIndex < FrameSampleSize)
            {
                return 0f;
            }

            float totalTime = 0f;
            for (int i = 0; i < FrameSampleSize; i++)
            {
                totalTime += _frameTimes[i];
            }

            return totalTime > 0f ? FrameSampleSize / totalTime : 0f;
        }

        /// <summary>
        /// Gets quality settings for a specific tier.
        /// </summary>
        /// <param name="tier">The tier to get settings for.</param>
        /// <returns>The tier settings.</returns>
        public QualityTierSettings GetTierSettings(QualityTier tier)
        {
            return tier switch
            {
                QualityTier.Low => new QualityTierSettings
                {
                    TargetFrameRate = 30,
                    ShadowQuality = URPShadowQuality.Disabled,
                    MsaaLevel = MsaaQuality.Disabled,
                    RenderScale = 0.75f,
                    PostProcessingEnabled = false
                },
                QualityTier.Medium => new QualityTierSettings
                {
                    TargetFrameRate = 60,
                    ShadowQuality = URPShadowQuality.HardShadows,
                    MsaaLevel = MsaaQuality._2x,
                    RenderScale = 1.0f,
                    PostProcessingEnabled = true
                },
                QualityTier.High => new QualityTierSettings
                {
                    TargetFrameRate = 120,
                    ShadowQuality = URPShadowQuality.SoftShadows,
                    MsaaLevel = MsaaQuality._4x,
                    RenderScale = 1.0f,
                    PostProcessingEnabled = true
                },
                _ => GetTierSettings(QualityTier.Medium)
            };
        }

        #endregion

        #region Private Methods

        private QualityTier DetectIPadTier()
        {
            if (PlatformManager.IsAppleSilicon())
            {
                int memory = SystemInfo.systemMemorySize;
                return memory >= 8000 ? QualityTier.High : QualityTier.Medium;
            }

            return QualityTier.Medium;
        }

        private QualityTier DetectAndroidTier()
        {
            int gpuMemory = SystemInfo.graphicsMemorySize;

            if (gpuMemory >= 4000)
            {
                return QualityTier.High;
            }

            if (gpuMemory >= 2000)
            {
                return QualityTier.Medium;
            }

            return QualityTier.Low;
        }

        private QualityTier DetectWindowsTier()
        {
            int gpuMemory = SystemInfo.graphicsMemorySize;

            if (gpuMemory >= 4000)
            {
                return QualityTier.High;
            }

            if (gpuMemory >= 2000)
            {
                return QualityTier.Medium;
            }

            return QualityTier.Low;
        }

        private void ApplyTierSettings(QualityTier tier)
        {
            var settings = GetTierSettings(tier);
            _targetFrameRate = settings.TargetFrameRate;

            Application.targetFrameRate = _targetFrameRate;

            // Skip URP asset switching in batch mode (headless servers, CI)
            // to avoid crashes when graphics are unavailable
            if (Application.isBatchMode)
            {
                if (_enableDebugLogging)
                {
                    Debug.Log("QualityManager: Skipping URP asset switch in batch mode");
                }
                return;
            }

            try
            {
                var urpAsset = GetURPAssetForTier(tier);
                if (urpAsset != null && GraphicsSettings.currentRenderPipeline != urpAsset)
                {
                    QualitySettings.renderPipeline = urpAsset;

                    if (_enableDebugLogging)
                    {
                        Debug.Log($"QualityManager: Switched to URP asset for tier {tier}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"QualityManager: Failed to switch URP asset: {ex.Message}");
            }
        }

        private UniversalRenderPipelineAsset GetURPAssetForTier(QualityTier tier)
        {
            return tier switch
            {
                QualityTier.Low => _lowQualityAsset,
                QualityTier.Medium => _mediumQualityAsset,
                QualityTier.High => _highQualityAsset,
                _ => _mediumQualityAsset
            };
        }

        private void RecordFrameTime()
        {
            _frameTimes[_frameTimeIndex % FrameSampleSize] = Time.unscaledDeltaTime;
            _frameTimeIndex++;
        }

        private void StartDynamicAdjustment()
        {
            if (_dynamicAdjustmentCoroutine != null)
            {
                return;
            }

            _timeUnderTarget = 0f;
            _dynamicAdjustmentCoroutine = StartCoroutine(DynamicAdjustmentLoop());
        }

        private void StopDynamicAdjustment()
        {
            if (_dynamicAdjustmentCoroutine != null)
            {
                StopCoroutine(_dynamicAdjustmentCoroutine);
                _dynamicAdjustmentCoroutine = null;
            }
        }

        private IEnumerator DynamicAdjustmentLoop()
        {
            var wait = new WaitForSecondsRealtime(DynamicAdjustmentInterval);

            while (_enableDynamicAdjustment)
            {
                yield return wait;

                if (_frameTimeIndex < FrameSampleSize)
                {
                    continue;
                }

                float avgFps = GetAverageFPS();
                float targetThreshold = _targetFrameRate * DowngradeThreshold;

                if (avgFps < targetThreshold)
                {
                    _timeUnderTarget += DynamicAdjustmentInterval;

                    if (_timeUnderTarget >= DowngradeTimeRequired)
                    {
                        TryDowngrade();
                        _timeUnderTarget = 0f;
                    }
                }
                else
                {
                    _timeUnderTarget = 0f;
                }
            }
        }

        private void TryDowngrade()
        {
            if (_effectiveTier == QualityTier.Low)
            {
                return;
            }

            var previousTier = _effectiveTier;
            _effectiveTier = _effectiveTier == QualityTier.High ? QualityTier.Medium : QualityTier.Low;

            ApplyTierSettings(_effectiveTier);

            if (_enableDebugLogging)
            {
                Debug.Log($"QualityManager: Dynamic downgrade from {previousTier} to {_effectiveTier}");
            }

            OnDynamicDowngrade?.Invoke(previousTier, _effectiveTier);
        }

        #endregion
    }

    /// <summary>
    /// Settings for a specific quality tier.
    /// </summary>
    public struct QualityTierSettings
    {
        /// <summary>Target frame rate for this tier.</summary>
        public int TargetFrameRate;

        /// <summary>Shadow quality level.</summary>
        public URPShadowQuality ShadowQuality;

        /// <summary>MSAA quality level.</summary>
        public MsaaQuality MsaaLevel;

        /// <summary>Render scale (0.5 to 1.0).</summary>
        public float RenderScale;

        /// <summary>Whether post-processing is enabled.</summary>
        public bool PostProcessingEnabled;
    }
}
