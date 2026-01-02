// ABOUTME: GSPro-style bottom panel displaying all shot metrics.
// ABOUTME: Contains 10 DataTiles showing ball speed, direction, spin, distances, etc.

using System;
using System.Collections;
using OpenRange.GC2;
using OpenRange.Physics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.UI
{
    /// <summary>
    /// The main shot data display bar at the bottom of the screen.
    /// Displays all shot metrics in GSPro-style layout.
    /// </summary>
    public class ShotDataBar : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Data Tiles")]
        [SerializeField] private DataTile _ballSpeedTile;
        [SerializeField] private DataTile _directionTile;
        [SerializeField] private DataTile _angleTile;
        [SerializeField] private DataTile _backSpinTile;
        [SerializeField] private DataTile _sideSpinTile;
        [SerializeField] private DataTile _apexTile;
        [SerializeField] private DataTile _offlineTile;
        [SerializeField] private DataTile _carryTile;
        [SerializeField] private DataTile _runTile;
        [SerializeField] private DataTile _totalTile;

        [Header("Layout")]
        [SerializeField] private HorizontalLayoutGroup _layoutGroup;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private CanvasGroup _canvasGroup;

        #endregion

        #region Private Fields

        private ResponsiveLayout _responsiveLayout;
        private GC2ShotData _lastShotData;
        private ShotResult _lastShotResult;
        private Coroutine _showAnimationCoroutine;

        // Conversion constants
        private const float FeetToYards = 1f / 3f;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the display is updated with new data.
        /// </summary>
        public event Action<GC2ShotData, ShotResult> OnDisplayUpdated;

        /// <summary>
        /// Fired when the display is cleared.
        /// </summary>
        public event Action OnDisplayCleared;

        #endregion

        #region Public Properties

        /// <summary>
        /// The last shot data that was displayed.
        /// </summary>
        public GC2ShotData LastShotData => _lastShotData;

        /// <summary>
        /// The last shot result that was displayed.
        /// </summary>
        public ShotResult LastShotResult => _lastShotResult;

        /// <summary>
        /// Whether the bar currently has data displayed.
        /// </summary>
        public bool HasData => _lastShotData != null && _lastShotResult != null;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeTiles();

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            // Find ResponsiveLayout in parent canvas
            _responsiveLayout = GetComponentInParent<ResponsiveLayout>();
            if (_responsiveLayout != null)
            {
                _responsiveLayout.OnLayoutChanged += OnLayoutChanged;
                UpdateLayout(_responsiveLayout.CurrentCategory);
            }
        }

        private void OnEnable()
        {
            if (_responsiveLayout != null)
            {
                UpdateLayout(_responsiveLayout.CurrentCategory);
            }
        }

        private void OnDisable()
        {
            StopAnimation();
        }

        private void OnDestroy()
        {
            if (_responsiveLayout != null)
            {
                _responsiveLayout.OnLayoutChanged -= OnLayoutChanged;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the display with new shot data.
        /// </summary>
        /// <param name="shot">The shot data from GC2.</param>
        /// <param name="result">The physics simulation result.</param>
        /// <param name="animate">Whether to animate the update.</param>
        public void UpdateDisplay(GC2ShotData shot, ShotResult result, bool animate = true)
        {
            if (shot == null || result == null)
            {
                return;
            }

            _lastShotData = shot;
            _lastShotResult = result;

            UpdateTileValues(shot, result, animate);

            if (animate && gameObject.activeInHierarchy)
            {
                AnimateShow();
            }

            OnDisplayUpdated?.Invoke(shot, result);
        }

        /// <summary>
        /// Clears all displayed values.
        /// </summary>
        public void Clear()
        {
            _lastShotData = null;
            _lastShotResult = null;

            ClearAllTiles();
            OnDisplayCleared?.Invoke();
        }

        /// <summary>
        /// Shows the data bar with animation.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            AnimateShow();
        }

        /// <summary>
        /// Hides the data bar with animation.
        /// </summary>
        public void Hide()
        {
            AnimateHide(() => gameObject.SetActive(false));
        }

        /// <summary>
        /// Force updates the layout for the current screen size.
        /// </summary>
        public void RefreshLayout()
        {
            if (_responsiveLayout != null)
            {
                UpdateLayout(_responsiveLayout.CurrentCategory);
            }
            else
            {
                UpdateLayout(ScreenCategory.Regular);
            }
        }

        #endregion

        #region Internal Methods (for testing)

        /// <summary>
        /// Gets a specific tile by index (0-9).
        /// </summary>
        internal DataTile GetTile(int index)
        {
            return index switch
            {
                0 => _ballSpeedTile,
                1 => _directionTile,
                2 => _angleTile,
                3 => _backSpinTile,
                4 => _sideSpinTile,
                5 => _apexTile,
                6 => _offlineTile,
                7 => _carryTile,
                8 => _runTile,
                9 => _totalTile,
                _ => null
            };
        }

        /// <summary>
        /// Gets the tile count.
        /// </summary>
        internal int TileCount => 10;

        /// <summary>
        /// Force sets references for testing.
        /// </summary>
        internal void SetTileReferences(
            DataTile ballSpeed, DataTile direction, DataTile angle,
            DataTile backSpin, DataTile sideSpin, DataTile apex,
            DataTile offline, DataTile carry, DataTile run, DataTile total)
        {
            _ballSpeedTile = ballSpeed;
            _directionTile = direction;
            _angleTile = angle;
            _backSpinTile = backSpin;
            _sideSpinTile = sideSpin;
            _apexTile = apex;
            _offlineTile = offline;
            _carryTile = carry;
            _runTile = run;
            _totalTile = total;

            InitializeTiles();
        }

        /// <summary>
        /// Sets the responsive layout reference for testing.
        /// </summary>
        internal void SetResponsiveLayout(ResponsiveLayout layout)
        {
            _responsiveLayout = layout;
        }

        #endregion

        #region Private Methods

        private void InitializeTiles()
        {
            // Set labels and units for each tile
            SetupTile(_ballSpeedTile, "BALL SPEED", "mph");
            SetupTile(_directionTile, "DIRECTION", "deg");
            SetupTile(_angleTile, "ANGLE", "deg");
            SetupTile(_backSpinTile, "BACK SPIN", "rpm");
            SetupTile(_sideSpinTile, "SIDE SPIN", "rpm");
            SetupTile(_apexTile, "APEX", "yd");
            SetupTile(_offlineTile, "OFFLINE", "yd");
            SetupTile(_carryTile, "CARRY", "yd");
            SetupTile(_runTile, "RUN", "yd");
            SetupTile(_totalTile, "TOTAL", "yd", true);

            ClearAllTiles();
        }

        private void SetupTile(DataTile tile, string label, string unit, bool highlighted = false)
        {
            if (tile == null) return;

            tile.Label = label;
            tile.Unit = unit;
            tile.IsHighlighted = highlighted;

            if (highlighted)
            {
                tile.SetHighlightColor(UITheme.TotalRed);
            }
        }

        private void UpdateTileValues(GC2ShotData shot, ShotResult result, bool animate)
        {
            // Ball Speed: 104.5 mph
            _ballSpeedTile?.SetValue(shot.BallSpeed, "F1", animate);

            // Direction: L4.0 or R4.0 (positive = right)
            _directionTile?.SetValueWithDirection(shot.Direction, "F1", animate);

            // Angle: 24.0 deg
            _angleTile?.SetValue(shot.LaunchAngle, "F1", animate);

            // Back Spin: 4,121 rpm
            _backSpinTile?.SetValueWithThousands(shot.BackSpin, animate);

            // Side Spin: L311 or R311 (positive = fade/slice = right)
            _sideSpinTile?.SetValueWithDirectionAndThousands(shot.SideSpin, animate);

            // Apex: 30.7 yd (convert from feet)
            float apexYards = result.MaxHeight * FeetToYards;
            _apexTile?.SetValue(apexYards, "F1", animate);

            // Offline: L7.2 or R7.2 (positive = right)
            _offlineTile?.SetValueWithDirection(result.OfflineDistance, "F1", animate);

            // Carry: 150.0 yd
            _carryTile?.SetValue(result.CarryDistance, "F1", animate);

            // Run: 4.6 yd
            _runTile?.SetValue(result.RollDistance, "F1", animate);

            // Total: 154.6 yd (highlighted in red)
            _totalTile?.SetValue(result.TotalDistance, "F1", animate);
        }

        private void ClearAllTiles()
        {
            _ballSpeedTile?.Clear();
            _directionTile?.Clear();
            _angleTile?.Clear();
            _backSpinTile?.Clear();
            _sideSpinTile?.Clear();
            _apexTile?.Clear();
            _offlineTile?.Clear();
            _carryTile?.Clear();
            _runTile?.Clear();
            _totalTile?.Clear();
        }

        private void OnLayoutChanged(ScreenCategory category)
        {
            UpdateLayout(category);
        }

        private void UpdateLayout(ScreenCategory category)
        {
            // Update font sizes for all tiles
            UpdateTileFontSizes(category);

            // Adjust spacing based on screen size
            if (_layoutGroup != null)
            {
                _layoutGroup.spacing = category switch
                {
                    ScreenCategory.Compact => UITheme.Padding.Tiny,
                    ScreenCategory.Regular => UITheme.Padding.Small,
                    ScreenCategory.Large => UITheme.Padding.Normal,
                    _ => UITheme.Padding.Small
                };
            }
        }

        private void UpdateTileFontSizes(ScreenCategory category)
        {
            _ballSpeedTile?.UpdateFontSize(category);
            _directionTile?.UpdateFontSize(category);
            _angleTile?.UpdateFontSize(category);
            _backSpinTile?.UpdateFontSize(category);
            _sideSpinTile?.UpdateFontSize(category);
            _apexTile?.UpdateFontSize(category);
            _offlineTile?.UpdateFontSize(category);
            _carryTile?.UpdateFontSize(category);
            _runTile?.UpdateFontSize(category);
            _totalTile?.UpdateFontSize(category);
        }

        private void AnimateShow()
        {
            StopAnimation();
            _showAnimationCoroutine = StartCoroutine(AnimateFade(1f));
        }

        private void AnimateHide(Action onComplete = null)
        {
            StopAnimation();
            _showAnimationCoroutine = StartCoroutine(AnimateFade(0f, onComplete));
        }

        private void StopAnimation()
        {
            if (_showAnimationCoroutine != null)
            {
                StopCoroutine(_showAnimationCoroutine);
                _showAnimationCoroutine = null;
            }
        }

        private IEnumerator AnimateFade(float targetAlpha, Action onComplete = null)
        {
            if (_canvasGroup == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float startAlpha = _canvasGroup.alpha;
            float duration = UITheme.Animation.Fast;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
            _showAnimationCoroutine = null;
            onComplete?.Invoke();
        }

        #endregion
    }
}
