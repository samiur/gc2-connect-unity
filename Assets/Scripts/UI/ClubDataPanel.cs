// ABOUTME: Right-side panel displaying HMT (Head Measurement Technology) club data.
// ABOUTME: Shows club speed, path, attack angle, face to target, and dynamic loft when GC2 HMT data is available.

using System;
using System.Collections;
using OpenRange.GC2;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.UI
{
    /// <summary>
    /// Direction of swing path (in-to-out vs out-to-in).
    /// </summary>
    public enum SwingPathDirection
    {
        /// <summary>Path within threshold of neutral (±0.5°).</summary>
        Neutral,
        /// <summary>Positive path - club moving from inside to outside (draw path).</summary>
        InToOut,
        /// <summary>Negative path - club moving from outside to inside (fade path).</summary>
        OutToIn
    }

    /// <summary>
    /// Direction of attack angle (ascending vs descending).
    /// </summary>
    public enum AttackAngleDirection
    {
        /// <summary>Attack angle at zero.</summary>
        Neutral,
        /// <summary>Positive attack angle - hitting up on the ball.</summary>
        Ascending,
        /// <summary>Negative attack angle - hitting down on the ball.</summary>
        Descending
    }

    /// <summary>
    /// Direction of face at impact relative to target.
    /// </summary>
    public enum FaceDirection
    {
        /// <summary>Face within threshold of square (±0.5°).</summary>
        Square,
        /// <summary>Positive face angle - face pointing right of target.</summary>
        Open,
        /// <summary>Negative face angle - face pointing left of target.</summary>
        Closed
    }

    /// <summary>
    /// The club data panel displayed on the right side of the screen.
    /// Only visible when GC2 shot data includes HMT (Head Measurement Technology) club data.
    /// </summary>
    public class ClubDataPanel : MonoBehaviour
    {
        #region Constants

        /// <summary>
        /// Threshold for considering a value as neutral (degrees).
        /// </summary>
        private const float NeutralThreshold = 0.5f;

        #endregion

        #region Serialized Fields

        [Header("Data Tiles")]
        [SerializeField] private DataTile _clubSpeedTile;
        [SerializeField] private DataTile _pathTile;
        [SerializeField] private DataTile _attackAngleTile;
        [SerializeField] private DataTile _faceToTargetTile;
        [SerializeField] private DataTile _dynamicLoftTile;

        [Header("Indicators")]
        [SerializeField] private SwingPathIndicator _swingPathIndicator;
        [SerializeField] private AttackAngleIndicator _attackAngleIndicator;

        [Header("Layout")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _headerText;

        #endregion

        #region Private Fields

        private ResponsiveLayout _responsiveLayout;
        private GC2ShotData _lastShotData;
        private Coroutine _animationCoroutine;
        private bool _isVisible;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the display is updated with new HMT data.
        /// </summary>
        public event Action<GC2ShotData> OnDisplayUpdated;

        /// <summary>
        /// Fired when the display is cleared.
        /// </summary>
        public event Action OnDisplayCleared;

        /// <summary>
        /// Fired when visibility changes.
        /// </summary>
        public event Action<bool> OnVisibilityChanged;

        #endregion

        #region Public Properties

        /// <summary>
        /// The last shot data that was displayed.
        /// </summary>
        public GC2ShotData LastShotData => _lastShotData;

        /// <summary>
        /// Whether the panel currently has HMT data displayed.
        /// </summary>
        public bool HasClubData => _lastShotData != null && _lastShotData.HasClubData;

        /// <summary>
        /// Whether the panel is currently visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// The number of data tiles in this panel.
        /// </summary>
        public int TileCount => 5;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeTiles();

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            // Start hidden
            _isVisible = false;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
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
        /// Only updates if HasClubData is true.
        /// </summary>
        /// <param name="shot">The shot data from GC2.</param>
        /// <param name="animate">Whether to animate the update.</param>
        public void UpdateDisplay(GC2ShotData shot, bool animate = true)
        {
            if (shot == null)
            {
                return;
            }

            _lastShotData = shot;

            if (shot.HasClubData)
            {
                UpdateTileValues(shot, animate);
                UpdateIndicators(shot);

                if (animate && gameObject.activeInHierarchy)
                {
                    Show();
                }

                OnDisplayUpdated?.Invoke(shot);
            }
            else
            {
                // Clear tiles when no HMT data
                ClearAllTiles();
            }
        }

        /// <summary>
        /// Clears all displayed values and hides the panel.
        /// </summary>
        public void Clear()
        {
            _lastShotData = null;
            ClearAllTiles();
            ClearIndicators();
            Hide();
            OnDisplayCleared?.Invoke();
        }

        /// <summary>
        /// Shows the panel with animation.
        /// </summary>
        public void Show()
        {
            _isVisible = true;
            gameObject.SetActive(true);
            AnimateShow();
            OnVisibilityChanged?.Invoke(true);
        }

        /// <summary>
        /// Hides the panel with animation.
        /// </summary>
        public void Hide()
        {
            _isVisible = false;
            AnimateHide(() => { });
            OnVisibilityChanged?.Invoke(false);
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

        #region Static Helper Methods

        /// <summary>
        /// Gets the swing path direction from a path angle value.
        /// </summary>
        /// <param name="path">Path angle in degrees (positive = in-to-out).</param>
        /// <returns>The path direction.</returns>
        public static SwingPathDirection GetPathDirection(float path)
        {
            if (Mathf.Abs(path) < NeutralThreshold)
            {
                return SwingPathDirection.Neutral;
            }
            return path > 0 ? SwingPathDirection.InToOut : SwingPathDirection.OutToIn;
        }

        /// <summary>
        /// Gets the attack angle direction from an attack angle value.
        /// </summary>
        /// <param name="attackAngle">Attack angle in degrees (positive = ascending).</param>
        /// <returns>The attack direction.</returns>
        public static AttackAngleDirection GetAttackDirection(float attackAngle)
        {
            if (Mathf.Approximately(attackAngle, 0f))
            {
                return AttackAngleDirection.Neutral;
            }
            return attackAngle > 0 ? AttackAngleDirection.Ascending : AttackAngleDirection.Descending;
        }

        /// <summary>
        /// Gets the face direction from a face to target angle value.
        /// </summary>
        /// <param name="faceToTarget">Face to target angle in degrees (positive = open).</param>
        /// <returns>The face direction.</returns>
        public static FaceDirection GetFaceDirection(float faceToTarget)
        {
            if (Mathf.Abs(faceToTarget) < NeutralThreshold)
            {
                return FaceDirection.Square;
            }
            return faceToTarget > 0 ? FaceDirection.Open : FaceDirection.Closed;
        }

        #endregion

        #region Internal Methods (for testing)

        /// <summary>
        /// Gets a specific tile by index (0-4).
        /// </summary>
        internal DataTile GetTile(int index)
        {
            return index switch
            {
                0 => _clubSpeedTile,
                1 => _pathTile,
                2 => _attackAngleTile,
                3 => _faceToTargetTile,
                4 => _dynamicLoftTile,
                _ => null
            };
        }

        /// <summary>
        /// Force sets tile references for testing.
        /// </summary>
        internal void SetTileReferences(
            DataTile clubSpeed, DataTile path, DataTile attackAngle,
            DataTile faceToTarget, DataTile dynamicLoft)
        {
            _clubSpeedTile = clubSpeed;
            _pathTile = path;
            _attackAngleTile = attackAngle;
            _faceToTargetTile = faceToTarget;
            _dynamicLoftTile = dynamicLoft;

            InitializeTiles();
        }

        /// <summary>
        /// Sets the responsive layout reference for testing.
        /// </summary>
        internal void SetResponsiveLayout(ResponsiveLayout layout)
        {
            _responsiveLayout = layout;
        }

        /// <summary>
        /// Sets the indicator references for testing.
        /// </summary>
        internal void SetIndicatorReferences(SwingPathIndicator pathIndicator, AttackAngleIndicator attackIndicator)
        {
            _swingPathIndicator = pathIndicator;
            _attackAngleIndicator = attackIndicator;
        }

        #endregion

        #region Private Methods

        private void InitializeTiles()
        {
            // Set labels and units for each tile
            SetupTile(_clubSpeedTile, "CLUB SPEED", "mph");
            SetupTile(_pathTile, "PATH", "deg");
            SetupTile(_attackAngleTile, "ATTACK", "deg");
            SetupTile(_faceToTargetTile, "FACE", "deg");
            SetupTile(_dynamicLoftTile, "LOFT", "deg");

            ClearAllTiles();
        }

        private void SetupTile(DataTile tile, string label, string unit)
        {
            if (tile == null) return;

            tile.Label = label;
            tile.Unit = unit;
        }

        private void UpdateTileValues(GC2ShotData shot, bool animate)
        {
            // Club Speed: 105.2 mph
            _clubSpeedTile?.SetValue(shot.ClubSpeed, "F1", animate);

            // Path: 2.5 deg (signed - positive = in-to-out)
            _pathTile?.SetValue(shot.Path, "F1", animate);

            // Attack Angle: -4.2 deg (signed - negative = descending)
            _attackAngleTile?.SetValue(shot.AttackAngle, "F1", animate);

            // Face to Target: -1.0 deg (signed - negative = closed)
            _faceToTargetTile?.SetValue(shot.FaceToTarget, "F1", animate);

            // Dynamic Loft: 14.5 deg
            _dynamicLoftTile?.SetValue(shot.DynamicLoft, "F1", animate);
        }

        private void UpdateIndicators(GC2ShotData shot)
        {
            if (_swingPathIndicator != null)
            {
                _swingPathIndicator.UpdateDisplay(shot.Path, shot.FaceToTarget);
            }

            if (_attackAngleIndicator != null)
            {
                _attackAngleIndicator.UpdateDisplay(shot.AttackAngle);
            }
        }

        private void ClearAllTiles()
        {
            _clubSpeedTile?.Clear();
            _pathTile?.Clear();
            _attackAngleTile?.Clear();
            _faceToTargetTile?.Clear();
            _dynamicLoftTile?.Clear();
        }

        private void ClearIndicators()
        {
            if (_swingPathIndicator != null)
            {
                _swingPathIndicator.Clear();
            }

            if (_attackAngleIndicator != null)
            {
                _attackAngleIndicator.Clear();
            }
        }

        private void OnLayoutChanged(ScreenCategory category)
        {
            UpdateLayout(category);
        }

        private void UpdateLayout(ScreenCategory category)
        {
            // Update font sizes for all tiles
            UpdateTileFontSizes(category);

            // Update header if present
            if (_headerText != null)
            {
                _headerText.fontSize = UITheme.GetFontSize(category, FontCategory.Header);
            }
        }

        private void UpdateTileFontSizes(ScreenCategory category)
        {
            _clubSpeedTile?.UpdateFontSize(category);
            _pathTile?.UpdateFontSize(category);
            _attackAngleTile?.UpdateFontSize(category);
            _faceToTargetTile?.UpdateFontSize(category);
            _dynamicLoftTile?.UpdateFontSize(category);
        }

        private void AnimateShow()
        {
            StopAnimation();
            _animationCoroutine = StartCoroutine(AnimateFade(1f));
        }

        private void AnimateHide(Action onComplete = null)
        {
            StopAnimation();
            _animationCoroutine = StartCoroutine(AnimateFade(0f, onComplete));
        }

        private void StopAnimation()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
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
            float duration = UITheme.Animation.PanelTransition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
            _animationCoroutine = null;
            onComplete?.Invoke();
        }

        #endregion
    }
}
