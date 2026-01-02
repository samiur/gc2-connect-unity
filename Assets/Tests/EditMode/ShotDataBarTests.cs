// ABOUTME: Unit tests for ShotDataBar UI component.
// ABOUTME: Tests display update, tile values, events, and responsive layout.

using System.Collections.Generic;
using NUnit.Framework;
using OpenRange.GC2;
using OpenRange.Physics;
using OpenRange.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class ShotDataBarTests
    {
        private GameObject _barGO;
        private ShotDataBar _bar;
        private DataTile[] _tiles;
        private GC2ShotData _testShot;
        private ShotResult _testResult;

        [SetUp]
        public void SetUp()
        {
            _barGO = new GameObject("TestShotDataBar");
            _bar = _barGO.AddComponent<ShotDataBar>();
            _barGO.AddComponent<CanvasGroup>();

            // Create 10 tiles
            _tiles = new DataTile[10];
            string[] names = { "BallSpeed", "Direction", "Angle", "BackSpin", "SideSpin",
                              "Apex", "Offline", "Carry", "Run", "Total" };

            for (int i = 0; i < 10; i++)
            {
                _tiles[i] = CreateTestDataTile(names[i]);
            }

            // Wire up references
            _bar.SetTileReferences(
                _tiles[0], _tiles[1], _tiles[2], _tiles[3], _tiles[4],
                _tiles[5], _tiles[6], _tiles[7], _tiles[8], _tiles[9]
            );

            // Create test data
            _testShot = CreateTestShotData();
            _testResult = CreateTestShotResult();
        }

        [TearDown]
        public void TearDown()
        {
            if (_barGO != null)
            {
                Object.DestroyImmediate(_barGO);
            }
        }

        #region Helper Methods

        private DataTile CreateTestDataTile(string name)
        {
            var tileGO = new GameObject(name);
            tileGO.transform.SetParent(_barGO.transform);
            var tile = tileGO.AddComponent<DataTile>();
            tileGO.AddComponent<CanvasGroup>();

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(tileGO.transform);
            var labelText = labelGO.AddComponent<TextMeshProUGUI>();

            var valueGO = new GameObject("Value");
            valueGO.transform.SetParent(tileGO.transform);
            var valueText = valueGO.AddComponent<TextMeshProUGUI>();

            var unitGO = new GameObject("Unit");
            unitGO.transform.SetParent(tileGO.transform);
            var unitText = unitGO.AddComponent<TextMeshProUGUI>();

            tile.SetReferences(labelText, valueText, unitText);

            return tile;
        }

        private GC2ShotData CreateTestShotData()
        {
            return new GC2ShotData
            {
                ShotId = 1,
                Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                BallSpeed = 104.5f,
                LaunchAngle = 24.0f,
                Direction = -4.0f, // 4 degrees left
                TotalSpin = 4500f,
                BackSpin = 4121f,
                SideSpin = -311f, // 311 rpm draw (left)
                SpinAxis = 5f,
                HasClubData = false
            };
        }

        private ShotResult CreateTestShotResult()
        {
            return new ShotResult
            {
                CarryDistance = 150.0f,
                TotalDistance = 154.6f,
                RollDistance = 4.6f,
                OfflineDistance = -7.2f, // 7.2 yards left
                MaxHeight = 92.1f, // in feet
                FlightTime = 5.5f,
                TotalTime = 7.0f,
                BounceCount = 2,
                Trajectory = new List<TrajectoryPoint>()
            };
        }

        #endregion

        #region UpdateDisplay Tests

        [Test]
        public void UpdateDisplay_SetsAllTileValues()
        {
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            Assert.IsTrue(_tiles[0].HasValue, "BallSpeed should have value");
            Assert.IsTrue(_tiles[1].HasValue, "Direction should have value");
            Assert.IsTrue(_tiles[2].HasValue, "Angle should have value");
            Assert.IsTrue(_tiles[3].HasValue, "BackSpin should have value");
            Assert.IsTrue(_tiles[4].HasValue, "SideSpin should have value");
            Assert.IsTrue(_tiles[5].HasValue, "Apex should have value");
            Assert.IsTrue(_tiles[6].HasValue, "Offline should have value");
            Assert.IsTrue(_tiles[7].HasValue, "Carry should have value");
            Assert.IsTrue(_tiles[8].HasValue, "Run should have value");
            Assert.IsTrue(_tiles[9].HasValue, "Total should have value");
        }

        [Test]
        public void UpdateDisplay_BallSpeed_ShowsCorrectValue()
        {
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            Assert.AreEqual("104.5", _tiles[0].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_Direction_ShowsLeftPrefix()
        {
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            Assert.AreEqual("L4.0", _tiles[1].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_Direction_RightValue_ShowsRightPrefix()
        {
            _testShot.Direction = 5.5f;
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            Assert.AreEqual("R5.5", _tiles[1].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_LaunchAngle_ShowsCorrectValue()
        {
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            Assert.AreEqual("24.0", _tiles[2].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_BackSpin_ShowsThousandsSeparator()
        {
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            Assert.AreEqual("4,121", _tiles[3].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_SideSpin_ShowsLeftPrefix()
        {
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            Assert.AreEqual("L311", _tiles[4].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_SideSpin_FadeSlice_ShowsRightPrefix()
        {
            _testShot.SideSpin = 500f;
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            Assert.AreEqual("R500", _tiles[4].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_Apex_ConvertsFromFeetToYards()
        {
            // 92.1 feet = 30.7 yards
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            Assert.AreEqual("30.7", _tiles[5].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_Offline_ShowsLeftPrefix()
        {
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            Assert.AreEqual("L7.2", _tiles[6].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_Offline_RightValue_ShowsRightPrefix()
        {
            _testResult.OfflineDistance = 10.5f;
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            Assert.AreEqual("R10.5", _tiles[6].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_Carry_ShowsCorrectValue()
        {
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            Assert.AreEqual("150.0", _tiles[7].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_Run_ShowsCorrectValue()
        {
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            Assert.AreEqual("4.6", _tiles[8].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_Total_ShowsCorrectValue()
        {
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            Assert.AreEqual("154.6", _tiles[9].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_Total_IsHighlighted()
        {
            Assert.IsTrue(_tiles[9].IsHighlighted);
        }

        [Test]
        public void UpdateDisplay_StoresLastShotData()
        {
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            Assert.AreEqual(_testShot, _bar.LastShotData);
        }

        [Test]
        public void UpdateDisplay_StoresLastShotResult()
        {
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            Assert.AreEqual(_testResult, _bar.LastShotResult);
        }

        [Test]
        public void UpdateDisplay_NullShot_DoesNothing()
        {
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);
            _bar.UpdateDisplay(null, _testResult, animate: false);

            // Should still have previous data
            Assert.AreEqual(_testShot, _bar.LastShotData);
        }

        [Test]
        public void UpdateDisplay_NullResult_DoesNothing()
        {
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);
            _bar.UpdateDisplay(_testShot, null, animate: false);

            // Should still have previous data
            Assert.AreEqual(_testResult, _bar.LastShotResult);
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_ResetsAllTiles()
        {
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);
            _bar.Clear();

            for (int i = 0; i < 10; i++)
            {
                Assert.IsFalse(_tiles[i].HasValue, $"Tile {i} should not have value after clear");
            }
        }

        [Test]
        public void Clear_ResetsLastShotData()
        {
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);
            _bar.Clear();

            Assert.IsNull(_bar.LastShotData);
        }

        [Test]
        public void Clear_ResetsLastShotResult()
        {
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);
            _bar.Clear();

            Assert.IsNull(_bar.LastShotResult);
        }

        [Test]
        public void Clear_SetsHasDataFalse()
        {
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);
            Assert.IsTrue(_bar.HasData);

            _bar.Clear();
            Assert.IsFalse(_bar.HasData);
        }

        #endregion

        #region HasData Tests

        [Test]
        public void HasData_InitiallyFalse()
        {
            Assert.IsFalse(_bar.HasData);
        }

        [Test]
        public void HasData_TrueAfterUpdate()
        {
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            Assert.IsTrue(_bar.HasData);
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnDisplayUpdated_FiresOnUpdate()
        {
            bool eventFired = false;
            GC2ShotData receivedShot = null;
            ShotResult receivedResult = null;

            _bar.OnDisplayUpdated += (shot, result) =>
            {
                eventFired = true;
                receivedShot = shot;
                receivedResult = result;
            };

            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(_testShot, receivedShot);
            Assert.AreEqual(_testResult, receivedResult);
        }

        [Test]
        public void OnDisplayCleared_FiresOnClear()
        {
            bool eventFired = false;
            _bar.OnDisplayCleared += () => eventFired = true;

            _bar.UpdateDisplay(_testShot, _testResult, animate: false);
            _bar.Clear();

            Assert.IsTrue(eventFired);
        }

        #endregion

        #region Tile Access Tests

        [Test]
        public void TileCount_ReturnsTen()
        {
            Assert.AreEqual(10, _bar.TileCount);
        }

        [Test]
        public void GetTile_ValidIndex_ReturnsTile()
        {
            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(_tiles[i], _bar.GetTile(i));
            }
        }

        [Test]
        public void GetTile_InvalidIndex_ReturnsNull()
        {
            Assert.IsNull(_bar.GetTile(-1));
            Assert.IsNull(_bar.GetTile(10));
            Assert.IsNull(_bar.GetTile(100));
        }

        #endregion

        #region Label Tests

        [Test]
        public void Tiles_HaveCorrectLabels()
        {
            string[] expectedLabels = { "BALL SPEED", "DIRECTION", "ANGLE", "BACK SPIN", "SIDE SPIN",
                                       "APEX", "OFFLINE", "CARRY", "RUN", "TOTAL" };

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(expectedLabels[i], _tiles[i].Label, $"Tile {i} should have label '{expectedLabels[i]}'");
            }
        }

        [Test]
        public void Tiles_HaveCorrectUnits()
        {
            string[] expectedUnits = { "mph", "deg", "deg", "rpm", "rpm", "yd", "yd", "yd", "yd", "yd" };

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(expectedUnits[i], _tiles[i].Unit, $"Tile {i} should have unit '{expectedUnits[i]}'");
            }
        }

        #endregion

        #region Edge Cases

        [Test]
        public void UpdateDisplay_ZeroValues_DisplaysCorrectly()
        {
            _testShot.BallSpeed = 0f;
            _testShot.Direction = 0f;
            _testShot.LaunchAngle = 0f;
            _testShot.BackSpin = 0f;
            _testShot.SideSpin = 0f;
            _testResult.MaxHeight = 0f;
            _testResult.OfflineDistance = 0f;
            _testResult.CarryDistance = 0f;
            _testResult.RollDistance = 0f;
            _testResult.TotalDistance = 0f;

            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            Assert.AreEqual("0.0", _tiles[0].GetDisplayedValue()); // BallSpeed
            Assert.AreEqual("0", _tiles[1].GetDisplayedValue()); // Direction (below threshold)
            Assert.AreEqual("0.0", _tiles[2].GetDisplayedValue()); // Angle
            Assert.AreEqual("0", _tiles[3].GetDisplayedValue()); // BackSpin
            Assert.AreEqual("0", _tiles[4].GetDisplayedValue()); // SideSpin (below threshold)
        }

        [Test]
        public void UpdateDisplay_LargeValues_DisplaysCorrectly()
        {
            _testShot.BallSpeed = 200f;
            _testShot.BackSpin = 15000f;
            _testResult.CarryDistance = 350f;
            _testResult.TotalDistance = 380f;

            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            Assert.AreEqual("200.0", _tiles[0].GetDisplayedValue());
            Assert.AreEqual("15,000", _tiles[3].GetDisplayedValue());
            Assert.AreEqual("350.0", _tiles[7].GetDisplayedValue());
            Assert.AreEqual("380.0", _tiles[9].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_Driver_Shot_DisplaysCorrectly()
        {
            // Typical driver shot
            _testShot.BallSpeed = 167f;
            _testShot.LaunchAngle = 10.9f;
            _testShot.Direction = 2.5f;
            _testShot.BackSpin = 2686f;
            _testShot.SideSpin = 500f;
            _testResult.CarryDistance = 275f;
            _testResult.TotalDistance = 295f;
            _testResult.RollDistance = 20f;
            _testResult.MaxHeight = 90f;
            _testResult.OfflineDistance = 15f;

            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            Assert.AreEqual("167.0", _tiles[0].GetDisplayedValue());
            Assert.AreEqual("R2.5", _tiles[1].GetDisplayedValue());
            Assert.AreEqual("10.9", _tiles[2].GetDisplayedValue());
            Assert.AreEqual("2,686", _tiles[3].GetDisplayedValue());
            Assert.AreEqual("R500", _tiles[4].GetDisplayedValue());
            Assert.AreEqual("30.0", _tiles[5].GetDisplayedValue()); // 90 ft = 30 yd
            Assert.AreEqual("R15.0", _tiles[6].GetDisplayedValue());
            Assert.AreEqual("275.0", _tiles[7].GetDisplayedValue());
            Assert.AreEqual("20.0", _tiles[8].GetDisplayedValue());
            Assert.AreEqual("295.0", _tiles[9].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_Wedge_Shot_DisplaysCorrectly()
        {
            // Typical wedge shot
            _testShot.BallSpeed = 102f;
            _testShot.LaunchAngle = 24.2f;
            _testShot.Direction = -1.5f;
            _testShot.BackSpin = 9304f;
            _testShot.SideSpin = -200f;
            _testResult.CarryDistance = 136f;
            _testResult.TotalDistance = 138f;
            _testResult.RollDistance = 2f;
            _testResult.MaxHeight = 105f;
            _testResult.OfflineDistance = -3f;

            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            Assert.AreEqual("102.0", _tiles[0].GetDisplayedValue());
            Assert.AreEqual("L1.5", _tiles[1].GetDisplayedValue());
            Assert.AreEqual("24.2", _tiles[2].GetDisplayedValue());
            Assert.AreEqual("9,304", _tiles[3].GetDisplayedValue());
            Assert.AreEqual("L200", _tiles[4].GetDisplayedValue());
            Assert.AreEqual("35.0", _tiles[5].GetDisplayedValue()); // 105 ft = 35 yd
            Assert.AreEqual("L3.0", _tiles[6].GetDisplayedValue());
            Assert.AreEqual("136.0", _tiles[7].GetDisplayedValue());
            Assert.AreEqual("2.0", _tiles[8].GetDisplayedValue());
            Assert.AreEqual("138.0", _tiles[9].GetDisplayedValue());
        }

        #endregion

        #region Multiple Updates Tests

        [Test]
        public void UpdateDisplay_MultipleUpdates_OverwritesPreviousValues()
        {
            _bar.UpdateDisplay(_testShot, _testResult, animate: false);

            var newShot = new GC2ShotData { BallSpeed = 200f, LaunchAngle = 15f };
            var newResult = new ShotResult { CarryDistance = 300f, TotalDistance = 320f };

            _bar.UpdateDisplay(newShot, newResult, animate: false);

            Assert.AreEqual("200.0", _tiles[0].GetDisplayedValue());
            Assert.AreEqual("300.0", _tiles[7].GetDisplayedValue());
            Assert.AreEqual("320.0", _tiles[9].GetDisplayedValue());
        }

        #endregion
    }
}
