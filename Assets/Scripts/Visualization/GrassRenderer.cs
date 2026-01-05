// ABOUTME: GPU instanced grass renderer for detailed grass near the tee area.
// ABOUTME: Uses DrawMeshInstancedIndirect for high-performance grass blade rendering with wind animation.

using System;
using System.Collections.Generic;
using UnityEngine;
using OpenRange.Core;

namespace OpenRange.Visualization
{
    /// <summary>
    /// Renders thousands of grass blades using GPU instancing.
    /// Integrates with WindController for animated grass movement.
    /// </summary>
    public class GrassRenderer : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Grass Mesh")]
        [SerializeField] private Mesh _grassBladeMesh;
        [SerializeField] private Material _grassMaterial;

        [Header("Spawn Area")]
        [SerializeField] private Vector3 _areaCenter = Vector3.zero;
        [SerializeField] private Vector2 _areaSize = new Vector2(20f, 50f); // Width x Length in meters
        [SerializeField] private float _nearDistance = 0f; // Start of grass (usually 0)
        [SerializeField] private float _farDistance = 50f; // End of grass (fade to terrain)

        [Header("Density")]
        [SerializeField] private int _bladesPerSquareMeter = 100;
        [SerializeField] private float _minBladeHeight = 0.1f;
        [SerializeField] private float _maxBladeHeight = 0.3f;
        [SerializeField] private float _minBladeWidth = 0.02f;
        [SerializeField] private float _maxBladeWidth = 0.04f;

        [Header("Variation")]
        [SerializeField] private float _positionJitter = 0.05f;
        [SerializeField] private float _rotationVariation = 30f;
        [SerializeField] private float _tiltVariation = 15f;
        [SerializeField] private Color _colorVariationMin = new Color(0.9f, 0.95f, 0.9f);
        [SerializeField] private Color _colorVariationMax = new Color(1.1f, 1.05f, 1.1f);

        [Header("LOD")]
        [SerializeField] private float _lodDistance1 = 20f; // Full density
        [SerializeField] private float _lodDistance2 = 35f; // Half density
        [SerializeField] private float _lodDistance3 = 50f; // Quarter density

        [Header("Culling")]
        [SerializeField] private bool _enableFrustumCulling = true;
        [SerializeField] private float _cullPadding = 2f;

        [Header("Quality")]
        [SerializeField] private bool _useQualityTiers = true;

        #endregion

        #region Private Fields

        private ComputeBuffer _positionBuffer;
        private ComputeBuffer _argsBuffer;
        private uint[] _args = new uint[5] { 0, 0, 0, 0, 0 };
        private List<GrassInstance> _grassInstances = new List<GrassInstance>();
        private Bounds _renderBounds;
        private bool _isInitialized;
        private int _currentInstanceCount;
        private Camera _mainCamera;

        // Shader property IDs
        private static readonly int PositionBufferID = Shader.PropertyToID("_PositionBuffer");
        private static readonly int WindDirectionID = Shader.PropertyToID("_WindDirection");
        private static readonly int WindStrengthID = Shader.PropertyToID("_WindStrength");
        private static readonly int WindSpeedID = Shader.PropertyToID("_WindSpeed");
        private static readonly int TimeID = Shader.PropertyToID("_Time");

        #endregion

        #region Structs

        /// <summary>
        /// Per-instance grass data sent to GPU.
        /// </summary>
        private struct GrassInstance
        {
            public Vector3 Position;
            public float Rotation; // Y-axis rotation in radians
            public float Height;
            public float Width;
            public float Tilt; // Forward tilt in radians
            public Color ColorTint;

            public static int Size => sizeof(float) * 10; // 3 + 1 + 1 + 1 + 1 + 4 (color as 4 floats) = 11, but we pack color
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Whether the grass renderer is initialized and ready.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Current number of grass instances being rendered.
        /// </summary>
        public int InstanceCount => _currentInstanceCount;

        /// <summary>
        /// The grass blade mesh used for rendering.
        /// </summary>
        public Mesh GrassBladeMesh
        {
            get => _grassBladeMesh;
            set
            {
                _grassBladeMesh = value;
                if (_isInitialized) UpdateArgsBuffer();
            }
        }

        /// <summary>
        /// The material used for grass rendering.
        /// </summary>
        public Material GrassMaterial
        {
            get => _grassMaterial;
            set => _grassMaterial = value;
        }

        /// <summary>
        /// Spawn area size (width x length).
        /// </summary>
        public Vector2 AreaSize
        {
            get => _areaSize;
            set
            {
                _areaSize = value;
                if (_isInitialized) RegenerateGrass();
            }
        }

        /// <summary>
        /// Grass density in blades per square meter.
        /// </summary>
        public int BladesPerSquareMeter
        {
            get => _bladesPerSquareMeter;
            set
            {
                _bladesPerSquareMeter = Mathf.Clamp(value, 1, 500);
                if (_isInitialized) RegenerateGrass();
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            if (_isInitialized)
            {
                UpdateArgsBuffer();
            }
        }

        private void OnDisable()
        {
            // Buffers are released in OnDestroy
        }

        private void OnDestroy()
        {
            ReleaseBuffers();
        }

        private void Update()
        {
            if (!_isInitialized || _grassMaterial == null || _grassBladeMesh == null)
                return;

            UpdateWindParameters();
            RenderGrass();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the grass renderer.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // Apply quality tier settings
            if (_useQualityTiers)
            {
                ApplyQualitySettings();
            }

            // Generate grass instances
            GenerateGrassInstances();

            // Create GPU buffers
            CreateBuffers();

            _isInitialized = true;
            Debug.Log($"GrassRenderer: Initialized with {_currentInstanceCount} grass blades");
        }

        /// <summary>
        /// Regenerate grass with current settings.
        /// </summary>
        public void RegenerateGrass()
        {
            ReleaseBuffers();
            _grassInstances.Clear();
            _isInitialized = false;
            Initialize();
        }

        private void ApplyQualitySettings()
        {
            var qualityManager = QualityManager.Instance;
            var tier = qualityManager != null ? qualityManager.CurrentTier : Core.QualityTier.High;

            switch (tier)
            {
                case Core.QualityTier.Low:
                    _bladesPerSquareMeter = Mathf.Min(_bladesPerSquareMeter, 25);
                    _lodDistance1 = 10f;
                    _lodDistance2 = 20f;
                    _lodDistance3 = 30f;
                    break;

                case Core.QualityTier.Medium:
                    _bladesPerSquareMeter = Mathf.Min(_bladesPerSquareMeter, 50);
                    _lodDistance1 = 15f;
                    _lodDistance2 = 30f;
                    _lodDistance3 = 45f;
                    break;

                case Core.QualityTier.High:
                    // Use configured values
                    break;
            }
        }

        #endregion

        #region Grass Generation

        private void GenerateGrassInstances()
        {
            _grassInstances.Clear();

            float halfWidth = _areaSize.x * 0.5f;
            float halfLength = _areaSize.y * 0.5f;

            // Calculate total area and instance count
            float area = _areaSize.x * _areaSize.y;
            int totalBlades = Mathf.RoundToInt(area * _bladesPerSquareMeter);

            // Generate random positions within the area
            for (int i = 0; i < totalBlades; i++)
            {
                float x = UnityEngine.Random.Range(-halfWidth, halfWidth);
                float z = UnityEngine.Random.Range(_nearDistance, _farDistance);

                // Add position jitter
                x += UnityEngine.Random.Range(-_positionJitter, _positionJitter);
                z += UnityEngine.Random.Range(-_positionJitter, _positionJitter);

                Vector3 worldPos = _areaCenter + new Vector3(x, 0f, z);

                // Sample terrain height (if terrain exists)
                float y = SampleTerrainHeight(worldPos);
                worldPos.y = y;

                // Create instance with variation
                var instance = new GrassInstance
                {
                    Position = worldPos,
                    Rotation = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad,
                    Height = UnityEngine.Random.Range(_minBladeHeight, _maxBladeHeight),
                    Width = UnityEngine.Random.Range(_minBladeWidth, _maxBladeWidth),
                    Tilt = UnityEngine.Random.Range(-_tiltVariation, _tiltVariation) * Mathf.Deg2Rad,
                    ColorTint = Color.Lerp(_colorVariationMin, _colorVariationMax, UnityEngine.Random.value)
                };

                _grassInstances.Add(instance);
            }

            _currentInstanceCount = _grassInstances.Count;

            // Calculate render bounds
            _renderBounds = new Bounds(
                _areaCenter + new Vector3(0, _maxBladeHeight * 0.5f, (_nearDistance + _farDistance) * 0.5f),
                new Vector3(_areaSize.x + _cullPadding * 2, _maxBladeHeight * 2, _farDistance - _nearDistance + _cullPadding * 2)
            );
        }

        private float SampleTerrainHeight(Vector3 position)
        {
            // Raycast down to find terrain height
            if (UnityEngine.Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
            {
                return hit.point.y;
            }
            return 0f;
        }

        #endregion

        #region GPU Buffers

        private void CreateBuffers()
        {
            if (_grassInstances.Count == 0) return;

            // Create position buffer (simplified for now - using Vector4 for position + rotation)
            int stride = sizeof(float) * 4; // x, y, z, rotation
            _positionBuffer = new ComputeBuffer(_grassInstances.Count, stride);

            // Pack instance data
            Vector4[] packedData = new Vector4[_grassInstances.Count];
            for (int i = 0; i < _grassInstances.Count; i++)
            {
                var inst = _grassInstances[i];
                packedData[i] = new Vector4(inst.Position.x, inst.Position.y, inst.Position.z, inst.Rotation);
            }
            _positionBuffer.SetData(packedData);

            // Create args buffer for DrawMeshInstancedIndirect
            _argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            UpdateArgsBuffer();
        }

        private void UpdateArgsBuffer()
        {
            if (_grassBladeMesh == null || _argsBuffer == null) return;

            _args[0] = _grassBladeMesh.GetIndexCount(0);
            _args[1] = (uint)_currentInstanceCount;
            _args[2] = _grassBladeMesh.GetIndexStart(0);
            _args[3] = _grassBladeMesh.GetBaseVertex(0);
            _args[4] = 0;

            _argsBuffer.SetData(_args);
        }

        private void ReleaseBuffers()
        {
            _positionBuffer?.Release();
            _positionBuffer = null;

            _argsBuffer?.Release();
            _argsBuffer = null;
        }

        #endregion

        #region Rendering

        private void UpdateWindParameters()
        {
            if (_grassMaterial == null) return;

            var windController = WindController.Instance;
            if (windController != null && windController.WindEnabled)
            {
                Vector3 windDir = windController.GetWindDirectionVector();
                _grassMaterial.SetVector(WindDirectionID, new Vector4(windDir.x, windDir.y, windDir.z, 0));
                _grassMaterial.SetFloat(WindStrengthID, windController.GetEffectiveStrength());
                _grassMaterial.SetFloat(WindSpeedID, windController.WindSpeed);
            }
            else
            {
                _grassMaterial.SetVector(WindDirectionID, Vector4.zero);
                _grassMaterial.SetFloat(WindStrengthID, 0f);
                _grassMaterial.SetFloat(WindSpeedID, 0f);
            }
        }

        private void RenderGrass()
        {
            if (_positionBuffer == null || _argsBuffer == null) return;

            // Frustum culling check
            if (_enableFrustumCulling && _mainCamera != null)
            {
                Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);
                if (!GeometryUtility.TestPlanesAABB(frustumPlanes, _renderBounds))
                {
                    return; // Skip rendering if outside frustum
                }
            }

            // Set buffer on material
            _grassMaterial.SetBuffer(PositionBufferID, _positionBuffer);

            // Draw instanced
            Graphics.DrawMeshInstancedIndirect(
                _grassBladeMesh,
                0,
                _grassMaterial,
                _renderBounds,
                _argsBuffer,
                0,
                null,
                UnityEngine.Rendering.ShadowCastingMode.Off,
                false
            );
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set the spawn area.
        /// </summary>
        public void SetArea(Vector3 center, Vector2 size, float nearDist, float farDist)
        {
            _areaCenter = center;
            _areaSize = size;
            _nearDistance = nearDist;
            _farDistance = farDist;

            if (_isInitialized) RegenerateGrass();
        }

        /// <summary>
        /// Force regeneration with new density.
        /// </summary>
        public void SetDensity(int bladesPerSquareMeter)
        {
            _bladesPerSquareMeter = Mathf.Clamp(bladesPerSquareMeter, 1, 500);
            if (_isInitialized) RegenerateGrass();
        }

        #endregion

        #region Editor Support

        private void OnDrawGizmosSelected()
        {
            // Draw spawn area
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
            Vector3 center = _areaCenter + new Vector3(0, 0.1f, (_nearDistance + _farDistance) * 0.5f);
            Vector3 size = new Vector3(_areaSize.x, 0.2f, _farDistance - _nearDistance);
            Gizmos.DrawCube(center, size);

            // Draw wireframe
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 1f);
            Gizmos.DrawWireCube(center, size);

            // Draw LOD distances
            Gizmos.color = Color.yellow;
            DrawCircle(_areaCenter, _lodDistance1, 32);
            Gizmos.color = Color.orange;
            DrawCircle(_areaCenter, _lodDistance2, 32);
            Gizmos.color = Color.red;
            DrawCircle(_areaCenter, _lodDistance3, 32);
        }

        private void DrawCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep * Mathf.Deg2Rad;
                float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

                Vector3 p1 = center + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * radius;
                Vector3 p2 = center + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * radius;

                Gizmos.DrawLine(p1, p2);
            }
        }

        #endregion
    }
}
