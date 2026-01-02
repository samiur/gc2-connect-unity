// ABOUTME: Visual indicator showing attack angle from a side view.
// ABOUTME: Displays arrow indicating ascending (up) vs descending (down) attack angle.

using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.UI
{
    /// <summary>
    /// Visual diagram showing attack angle from a side view.
    /// Shows whether the club is hitting up (ascending) or down (descending) on the ball.
    /// </summary>
    public class AttackAngleIndicator : MonoBehaviour
    {
        #region Constants

        /// <summary>
        /// Green color for ascending (hitting up) attack angle.
        /// Typically seen with driver shots.
        /// </summary>
        public static readonly Color AscendingColor = new Color(0.2f, 0.8f, 0.4f, 1f);

        /// <summary>
        /// Blue/purple color for descending (hitting down) attack angle.
        /// Typically seen with iron and wedge shots.
        /// </summary>
        public static readonly Color DescendingColor = new Color(0.4f, 0.3f, 0.9f, 1f);

        /// <summary>
        /// White/gray color for neutral attack angle.
        /// </summary>
        public static readonly Color NeutralColor = new Color(0.8f, 0.8f, 0.8f, 1f);

        /// <summary>
        /// Scale factor to convert attack angle degrees to visual rotation.
        /// Amplifies small angles for better visibility.
        /// </summary>
        public const float RotationScale = 3f;

        #endregion

        #region Serialized Fields

        [SerializeField] private Image _angleArrow;

        [Header("Settings")]
        [SerializeField] private float _maxRotation = 45f;

        #endregion

        #region Private Fields

        private float _currentAngle;

        #endregion

        #region Public Properties

        /// <summary>
        /// The current attack angle value.
        /// </summary>
        public float CurrentAngle => _currentAngle;

        /// <summary>
        /// The current attack direction based on the attack angle.
        /// </summary>
        public AttackAngleDirection AttackDirection => ClubDataPanel.GetAttackDirection(_currentAngle);

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the display with a new attack angle value.
        /// </summary>
        /// <param name="attackAngle">Attack angle in degrees (positive = ascending, negative = descending).</param>
        public void UpdateDisplay(float attackAngle)
        {
            _currentAngle = attackAngle;

            UpdateArrow(attackAngle);
        }

        /// <summary>
        /// Clears the indicator, resetting to neutral state.
        /// </summary>
        public void Clear()
        {
            _currentAngle = 0f;

            if (_angleArrow != null)
            {
                _angleArrow.rectTransform.localEulerAngles = Vector3.zero;
                _angleArrow.color = NeutralColor;
            }
        }

        #endregion

        #region Internal Methods (for testing)

        /// <summary>
        /// Sets the image references for testing.
        /// </summary>
        internal void SetReferences(Image angleArrow)
        {
            _angleArrow = angleArrow;
        }

        #endregion

        #region Private Methods

        private void UpdateArrow(float attackAngle)
        {
            if (_angleArrow == null) return;

            // Calculate rotation (scaled for visibility)
            // Positive angle = ascending = arrow points up
            // Negative angle = descending = arrow points down
            float rotation = attackAngle * RotationScale;
            rotation = Mathf.Clamp(rotation, -_maxRotation, _maxRotation);

            _angleArrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, rotation);

            // Set color based on direction
            _angleArrow.color = GetColorForAngle(attackAngle);
        }

        private Color GetColorForAngle(float attackAngle)
        {
            if (Mathf.Approximately(attackAngle, 0f))
            {
                return NeutralColor;
            }

            return attackAngle > 0 ? AscendingColor : DescendingColor;
        }

        #endregion
    }
}
