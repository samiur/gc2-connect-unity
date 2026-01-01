// ABOUTME: Unit tests for TeeMat component.
// ABOUTME: Tests spawn position, dimensions, bounds checking, and configuration.

using NUnit.Framework;
using UnityEngine;
using OpenRange.Visualization;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class TeeMatTests
    {
        private GameObject _testObject;
        private TeeMat _teeMat;
        private GameObject _matSurface;
        private GameObject _spawnPoint;
        private GameObject _boundaries;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestTeeMat");
            _teeMat = _testObject.AddComponent<TeeMat>();

            // Create mat surface
            _matSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _matSurface.name = "Mat";
            _matSurface.transform.SetParent(_testObject.transform);

            // Create spawn point
            _spawnPoint = new GameObject("SpawnPoint");
            _spawnPoint.transform.SetParent(_testObject.transform);
            _spawnPoint.transform.localPosition = new Vector3(0f, 0.02f, 0f);

            // Create boundaries
            _boundaries = new GameObject("Boundaries");
            _boundaries.transform.SetParent(_testObject.transform);

            // Wire up references
            _teeMat.SetMatSurface(_matSurface.transform);
            _teeMat.SetSpawnPoint(_spawnPoint.transform);
            _teeMat.SetBoundaries(_boundaries.transform);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
        }

        #region Initialization Tests

        [Test]
        public void TeeMat_InitializesWithDefaultDimensions()
        {
            Assert.AreEqual(2f, _teeMat.MatWidth, 0.01f);
            Assert.AreEqual(3f, _teeMat.MatLength, 0.01f);
        }

        [Test]
        public void TeeMat_SpawnPointIsSet()
        {
            Assert.IsNotNull(_teeMat.SpawnPoint);
            Assert.AreEqual(_spawnPoint.transform, _teeMat.SpawnPoint);
        }

        [Test]
        public void TeeMat_MatSurfaceIsSet()
        {
            Assert.IsNotNull(_teeMat.MatSurface);
            Assert.AreEqual(_matSurface.transform, _teeMat.MatSurface);
        }

        #endregion

        #region Spawn Position Tests

        [Test]
        public void BallSpawnPosition_ReturnsSpawnPointPosition()
        {
            _spawnPoint.transform.position = new Vector3(1f, 0.02f, 2f);

            Assert.AreEqual(1f, _teeMat.BallSpawnPosition.x, 0.001f);
            Assert.AreEqual(0.02f, _teeMat.BallSpawnPosition.y, 0.001f);
            Assert.AreEqual(2f, _teeMat.BallSpawnPosition.z, 0.001f);
        }

        [Test]
        public void SetSpawnPosition_UpdatesSpawnPointLocalPosition()
        {
            _teeMat.SetSpawnPosition(new Vector3(0.5f, 0.03f, 0.5f));

            Assert.AreEqual(0.5f, _spawnPoint.transform.localPosition.x, 0.001f);
            Assert.AreEqual(0.03f, _spawnPoint.transform.localPosition.y, 0.001f);
            Assert.AreEqual(0.5f, _spawnPoint.transform.localPosition.z, 0.001f);
        }

        [Test]
        public void SetSpawnPosition_FiresOnSpawnPointChangedEvent()
        {
            Vector3 receivedPosition = Vector3.zero;
            _teeMat.OnSpawnPointChanged += (pos) => receivedPosition = pos;

            _teeMat.SetSpawnPosition(new Vector3(0.5f, 0.02f, 0.5f));

            Assert.AreEqual(_teeMat.BallSpawnPosition, receivedPosition);
        }

        [Test]
        public void BallSpawnPosition_WithNullSpawnPoint_ReturnsFallback()
        {
            _teeMat.SetSpawnPoint(null);

            Vector3 fallback = _teeMat.BallSpawnPosition;

            Assert.AreEqual(_testObject.transform.position.x, fallback.x, 0.001f);
            Assert.AreEqual(0.02f, fallback.y, 0.001f);
            Assert.AreEqual(_testObject.transform.position.z, fallback.z, 0.001f);
        }

        #endregion

        #region Dimension Tests

        [Test]
        public void SetDimensions_UpdatesWidth()
        {
            _teeMat.SetDimensions(3f, 4f);

            Assert.AreEqual(3f, _teeMat.MatWidth, 0.01f);
        }

        [Test]
        public void SetDimensions_UpdatesLength()
        {
            _teeMat.SetDimensions(3f, 4f);

            Assert.AreEqual(4f, _teeMat.MatLength, 0.01f);
        }

        #endregion

        #region Position Detection Tests

        [Test]
        public void IsPositionOnMat_CenterPosition_ReturnsTrue()
        {
            _testObject.transform.position = Vector3.zero;
            Vector3 centerPos = Vector3.zero;

            Assert.IsTrue(_teeMat.IsPositionOnMat(centerPos));
        }

        [Test]
        public void IsPositionOnMat_InsidePosition_ReturnsTrue()
        {
            _testObject.transform.position = Vector3.zero;
            Vector3 insidePos = new Vector3(0.5f, 0f, 1f); // Within 2x3 mat

            Assert.IsTrue(_teeMat.IsPositionOnMat(insidePos));
        }

        [Test]
        public void IsPositionOnMat_OutsidePosition_ReturnsFalse()
        {
            _testObject.transform.position = Vector3.zero;
            Vector3 outsidePos = new Vector3(5f, 0f, 5f); // Outside 2x3 mat

            Assert.IsFalse(_teeMat.IsPositionOnMat(outsidePos));
        }

        [Test]
        public void IsPositionOnMat_EdgePosition_ReturnsTrue()
        {
            _testObject.transform.position = Vector3.zero;
            Vector3 edgePos = new Vector3(0.9f, 0f, 1.4f); // Near edge of 2x3 mat

            Assert.IsTrue(_teeMat.IsPositionOnMat(edgePos));
        }

        #endregion

        #region Bounds Tests

        [Test]
        public void GetMatBounds_ReturnsCorrectSize()
        {
            _testObject.transform.position = Vector3.zero;
            _teeMat.SetDimensions(2f, 3f);

            Bounds bounds = _teeMat.GetMatBounds();

            Assert.AreEqual(2f, bounds.size.x, 0.01f);
            Assert.AreEqual(3f, bounds.size.z, 0.01f);
        }

        [Test]
        public void GetMatBounds_CenteredCorrectly()
        {
            _testObject.transform.position = new Vector3(10f, 0f, 20f);

            Bounds bounds = _teeMat.GetMatBounds();

            Assert.AreEqual(10f, bounds.center.x, 0.01f);
            Assert.AreEqual(20f, bounds.center.z, 0.01f);
        }

        #endregion

        #region Visibility Tests

        [Test]
        public void Show_ActivatesGameObject()
        {
            _testObject.SetActive(false);
            _teeMat.Show();

            Assert.IsTrue(_testObject.activeSelf);
        }

        [Test]
        public void Hide_DeactivatesGameObject()
        {
            _teeMat.Hide();

            Assert.IsFalse(_testObject.activeSelf);
        }

        [Test]
        public void SetShowBoundaries_True_ActivatesBoundaries()
        {
            _boundaries.SetActive(false);
            _teeMat.SetShowBoundaries(true);

            Assert.IsTrue(_boundaries.activeSelf);
        }

        [Test]
        public void SetShowBoundaries_False_DeactivatesBoundaries()
        {
            _boundaries.SetActive(true);
            _teeMat.SetShowBoundaries(false);

            Assert.IsFalse(_boundaries.activeSelf);
        }

        #endregion

        #region Color Tests

        [Test]
        public void SetMatColor_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _teeMat.SetMatColor(Color.green));
        }

        #endregion

        #region Null Safety Tests

        [Test]
        public void SetDimensions_WithNullSurface_DoesNotThrow()
        {
            _teeMat.SetMatSurface(null);

            Assert.DoesNotThrow(() => _teeMat.SetDimensions(3f, 4f));
        }

        [Test]
        public void SetSpawnPosition_WithNullSpawnPoint_CreatesNewOne()
        {
            _teeMat.SetSpawnPoint(null);

            Assert.DoesNotThrow(() => _teeMat.SetSpawnPosition(Vector3.zero));
            Assert.IsNotNull(_teeMat.SpawnPoint);
        }

        [Test]
        public void SetShowBoundaries_WithNullBoundaries_DoesNotThrow()
        {
            _teeMat.SetBoundaries(null);

            Assert.DoesNotThrow(() => _teeMat.SetShowBoundaries(true));
        }

        [Test]
        public void SetMatColor_WithNullRenderer_DoesNotThrow()
        {
            _teeMat.SetMatSurface(null);

            Assert.DoesNotThrow(() => _teeMat.SetMatColor(Color.red));
        }

        #endregion
    }
}
