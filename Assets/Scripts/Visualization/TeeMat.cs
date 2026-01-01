// ABOUTME: Component for the tee mat area where players hit from.
// ABOUTME: Defines the ball spawn position and tee box boundaries.

using System;
using UnityEngine;

namespace OpenRange.Visualization
{
    /// <summary>
    /// Tee mat area where the ball is spawned and shots are taken from.
    /// Defines spawn position and visual boundaries.
    /// </summary>
    public class TeeMat : MonoBehaviour
    {
        [Header("Visual References")]
        [SerializeField] private Transform _matSurface;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private Transform _boundaries;
        [SerializeField] private Renderer _matRenderer;

        [Header("Dimensions")]
        [SerializeField] private float _matWidth = 2f;
        [SerializeField] private float _matLength = 3f;
        [SerializeField] private float _matHeight = 0.02f;
        [SerializeField] private float _spawnHeight = 0.02f;

        [Header("Visual Settings")]
        [SerializeField] private Color _matColor = new Color(0.18f, 0.31f, 0.18f);
        [SerializeField] private bool _showBoundaries = true;
        [SerializeField] private Color _boundaryColor = Color.white;
        [SerializeField] private float _boundaryWidth = 0.05f;

        private MaterialPropertyBlock _propertyBlock;

        /// <summary>
        /// Gets the property block, initializing lazily if needed.
        /// </summary>
        private MaterialPropertyBlock PropertyBlock
        {
            get
            {
                if (_propertyBlock == null)
                {
                    _propertyBlock = new MaterialPropertyBlock();
                }
                return _propertyBlock;
            }
        }

        /// <summary>
        /// Width of the tee mat in meters.
        /// </summary>
        public float MatWidth => _matWidth;

        /// <summary>
        /// Length of the tee mat in meters.
        /// </summary>
        public float MatLength => _matLength;

        /// <summary>
        /// The spawn point for the golf ball.
        /// </summary>
        public Transform SpawnPoint => _spawnPoint;

        /// <summary>
        /// World position where ball should spawn.
        /// </summary>
        public Vector3 BallSpawnPosition
        {
            get
            {
                if (_spawnPoint != null)
                {
                    return _spawnPoint.position;
                }
                return transform.position + new Vector3(0f, _spawnHeight, 0f);
            }
        }

        /// <summary>
        /// The mat surface transform.
        /// </summary>
        public Transform MatSurface => _matSurface;

        /// <summary>
        /// Event fired when spawn point changes.
        /// </summary>
        public event Action<Vector3> OnSpawnPointChanged;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            InitializeReferences();
            ApplyMatSettings();
        }

        /// <summary>
        /// Initialize component references if not set in inspector.
        /// </summary>
        private void InitializeReferences()
        {
            if (_matSurface == null)
            {
                _matSurface = transform.Find("Mat");
                if (_matSurface == null)
                {
                    _matSurface = transform.Find("MatSurface");
                }
            }

            if (_spawnPoint == null)
            {
                _spawnPoint = transform.Find("SpawnPoint");
            }

            if (_boundaries == null)
            {
                _boundaries = transform.Find("Boundaries");
            }

            if (_matRenderer == null && _matSurface != null)
            {
                _matRenderer = _matSurface.GetComponent<Renderer>();
            }
        }

        /// <summary>
        /// Apply mat size and visual settings.
        /// </summary>
        private void ApplyMatSettings()
        {
            if (_matSurface != null)
            {
                // Scale for a cube primitive (default size is 1x1x1)
                _matSurface.localScale = new Vector3(_matWidth, _matHeight, _matLength);
                _matSurface.localPosition = new Vector3(0f, _matHeight / 2f, 0f);
            }

            if (_spawnPoint == null)
            {
                CreateSpawnPoint();
            }
            else
            {
                _spawnPoint.localPosition = new Vector3(0f, _spawnHeight, 0f);
            }

            if (_boundaries != null)
            {
                _boundaries.gameObject.SetActive(_showBoundaries);
            }

            ApplyMatColor(_matColor);
        }

        /// <summary>
        /// Create a spawn point if one doesn't exist.
        /// </summary>
        private void CreateSpawnPoint()
        {
            var spawnGo = new GameObject("SpawnPoint");
            spawnGo.transform.SetParent(transform);
            spawnGo.transform.localPosition = new Vector3(0f, _spawnHeight, 0f);
            spawnGo.transform.localRotation = Quaternion.identity;
            _spawnPoint = spawnGo.transform;
        }

        /// <summary>
        /// Apply color to the mat surface.
        /// </summary>
        private void ApplyMatColor(Color color)
        {
            if (_matRenderer == null)
            {
                return;
            }

            _matRenderer.GetPropertyBlock(PropertyBlock);
            PropertyBlock.SetColor("_BaseColor", color);
            PropertyBlock.SetColor("_Color", color);
            _matRenderer.SetPropertyBlock(PropertyBlock);
        }

        /// <summary>
        /// Set the mat dimensions.
        /// </summary>
        /// <param name="width">Width in meters.</param>
        /// <param name="length">Length in meters.</param>
        public void SetDimensions(float width, float length)
        {
            _matWidth = width;
            _matLength = length;
            ApplyMatSettings();
        }

        /// <summary>
        /// Set the mat color.
        /// </summary>
        /// <param name="color">Color to apply.</param>
        public void SetMatColor(Color color)
        {
            _matColor = color;
            ApplyMatColor(color);
        }

        /// <summary>
        /// Set the spawn point position.
        /// </summary>
        /// <param name="localPosition">Local position relative to tee mat.</param>
        public void SetSpawnPosition(Vector3 localPosition)
        {
            if (_spawnPoint == null)
            {
                CreateSpawnPoint();
            }

            _spawnPoint.localPosition = localPosition;
            OnSpawnPointChanged?.Invoke(BallSpawnPosition);
        }

        /// <summary>
        /// Set whether to show boundaries.
        /// </summary>
        /// <param name="show">Whether to show boundaries.</param>
        public void SetShowBoundaries(bool show)
        {
            _showBoundaries = show;
            if (_boundaries != null)
            {
                _boundaries.gameObject.SetActive(show);
            }
        }

        /// <summary>
        /// Check if a position is on the tee mat.
        /// </summary>
        /// <param name="position">World position to check.</param>
        /// <returns>True if position is within tee mat bounds.</returns>
        public bool IsPositionOnMat(Vector3 position)
        {
            Vector3 localPos = transform.InverseTransformPoint(position);

            float halfWidth = _matWidth / 2f;
            float halfLength = _matLength / 2f;

            return localPos.x >= -halfWidth && localPos.x <= halfWidth &&
                   localPos.z >= -halfLength && localPos.z <= halfLength;
        }

        /// <summary>
        /// Get the bounds of the tee mat.
        /// </summary>
        /// <returns>Bounds of the mat in world space.</returns>
        public Bounds GetMatBounds()
        {
            Vector3 size = new Vector3(_matWidth, _matHeight, _matLength);
            Vector3 center = transform.position + new Vector3(0f, _matHeight / 2f, 0f);
            return new Bounds(center, size);
        }

        /// <summary>
        /// Set the mat surface transform (for testing).
        /// </summary>
        public void SetMatSurface(Transform surface)
        {
            _matSurface = surface;
            if (surface != null)
            {
                _matRenderer = surface.GetComponent<Renderer>();
            }
        }

        /// <summary>
        /// Set the spawn point transform (for testing).
        /// </summary>
        public void SetSpawnPoint(Transform spawnPoint)
        {
            _spawnPoint = spawnPoint;
        }

        /// <summary>
        /// Set the boundaries transform (for testing).
        /// </summary>
        public void SetBoundaries(Transform boundaries)
        {
            _boundaries = boundaries;
        }

        /// <summary>
        /// Show the tee mat.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hide the tee mat.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw tee mat bounds
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Vector3 center = transform.position + new Vector3(0f, _matHeight / 2f, 0f);
            Vector3 size = new Vector3(_matWidth, _matHeight, _matLength);
            Gizmos.DrawCube(center, size);

            // Draw spawn point
            Gizmos.color = Color.yellow;
            Vector3 spawnPos = _spawnPoint != null
                ? _spawnPoint.position
                : transform.position + new Vector3(0f, _spawnHeight, 0f);
            Gizmos.DrawWireSphere(spawnPos, 0.05f);
        }
#endif
    }
}
