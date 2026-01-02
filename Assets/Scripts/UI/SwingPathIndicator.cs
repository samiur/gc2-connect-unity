// ABOUTME: Visual indicator showing swing path direction from a top-down view.
// ABOUTME: Displays club path arrow (in-to-out vs out-to-in) and face angle line.

using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.UI
{
    /// <summary>
    /// Visual diagram showing swing path from a top-down view.
    /// Shows club path direction (in-to-out vs out-to-in) with an arrow,
    /// and face angle relative to target with a line overlay.
    /// </summary>
    public class SwingPathIndicator : MonoBehaviour
    {
        #region Constants

        /// <summary>
        /// Blue color for in-to-out (draw) path.
        /// </summary>
        public static readonly Color InToOutColor = new Color(0.2f, 0.4f, 0.9f, 1f);

        /// <summary>
        /// Orange color for out-to-in (fade) path.
        /// </summary>
        public static readonly Color OutToInColor = new Color(1f, 0.5f, 0.1f, 1f);

        /// <summary>
        /// White/gray color for neutral path.
        /// </summary>
        public static readonly Color NeutralColor = new Color(0.8f, 0.8f, 0.8f, 1f);

        /// <summary>
        /// Threshold for considering path as neutral (degrees).
        /// </summary>
        private const float NeutralThreshold = 0.5f;

        /// <summary>
        /// Scale factor to convert path degrees to visual rotation.
        /// Amplifies small angles for better visibility.
        /// </summary>
        public const float RotationScale = 3f;

        /// <summary>
        /// Scale factor to convert face angle degrees to visual rotation.
        /// </summary>
        public const float FaceRotationScale = 3f;

        #endregion

        #region Serialized Fields

        [SerializeField] private Image _pathArrow;
        [SerializeField] private Image _faceAngleLine;

        [Header("Settings")]
        [SerializeField] private float _maxRotation = 45f;

        #endregion

        #region Private Fields

        private float _currentPath;
        private float _currentFace;

        #endregion

        #region Public Properties

        /// <summary>
        /// The current path angle value.
        /// </summary>
        public float CurrentPath => _currentPath;

        /// <summary>
        /// The current face angle value.
        /// </summary>
        public float CurrentFace => _currentFace;

        /// <summary>
        /// The current path direction based on the path angle.
        /// </summary>
        public SwingPathDirection PathDirection => ClubDataPanel.GetPathDirection(_currentPath);

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the display with new path and face angle values.
        /// </summary>
        /// <param name="path">Path angle in degrees (positive = in-to-out).</param>
        /// <param name="faceToTarget">Face angle in degrees (positive = open).</param>
        public void UpdateDisplay(float path, float faceToTarget)
        {
            _currentPath = path;
            _currentFace = faceToTarget;

            UpdatePathArrow(path);
            UpdateFaceAngleLine(faceToTarget);
        }

        /// <summary>
        /// Clears the indicator, resetting to neutral state.
        /// </summary>
        public void Clear()
        {
            _currentPath = 0f;
            _currentFace = 0f;

            if (_pathArrow != null)
            {
                _pathArrow.rectTransform.localEulerAngles = Vector3.zero;
                _pathArrow.color = NeutralColor;
            }

            if (_faceAngleLine != null)
            {
                _faceAngleLine.rectTransform.localEulerAngles = Vector3.zero;
            }
        }

        #endregion

        #region Internal Methods (for testing)

        /// <summary>
        /// Sets the image references for testing.
        /// </summary>
        internal void SetReferences(Image pathArrow, Image faceAngleLine)
        {
            _pathArrow = pathArrow;
            _faceAngleLine = faceAngleLine;
        }

        #endregion

        #region Private Methods

        private void UpdatePathArrow(float path)
        {
            if (_pathArrow == null) return;

            // Calculate rotation (scaled for visibility)
            float rotation = path * RotationScale;
            rotation = Mathf.Clamp(rotation, -_maxRotation, _maxRotation);

            _pathArrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, rotation);

            // Set color based on direction
            _pathArrow.color = GetColorForPath(path);
        }

        private void UpdateFaceAngleLine(float faceToTarget)
        {
            if (_faceAngleLine == null) return;

            // Calculate rotation (scaled for visibility)
            float rotation = faceToTarget * FaceRotationScale;
            rotation = Mathf.Clamp(rotation, -_maxRotation, _maxRotation);

            _faceAngleLine.rectTransform.localEulerAngles = new Vector3(0f, 0f, rotation);
        }

        private Color GetColorForPath(float path)
        {
            if (Mathf.Abs(path) < NeutralThreshold)
            {
                return NeutralColor;
            }

            return path > 0 ? InToOutColor : OutToInColor;
        }

        #endregion
    }
}
