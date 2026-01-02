// ABOUTME: Unit tests for ClubDataPanel UI component.
// ABOUTME: Tests HMT data display, visibility based on HasClubData, events, and animations.

using System.Collections.Generic;
using NUnit.Framework;
using OpenRange.GC2;
using OpenRange.UI;
using TMPro;
using UnityEngine;

namespace OpenRange.Tests.EditMode
{
    [TestFixture]
    public class ClubDataPanelTests
    {
        private GameObject _panelGO;
        private ClubDataPanel _panel;
        private DataTile[] _tiles;
        private GC2ShotData _testShotWithHMT;
        private GC2ShotData _testShotWithoutHMT;

        [SetUp]
        public void SetUp()
        {
            _panelGO = new GameObject("TestClubDataPanel");
            _panel = _panelGO.AddComponent<ClubDataPanel>();
            _panelGO.AddComponent<CanvasGroup>();

            // Create 5 tiles for HMT data
            _tiles = new DataTile[5];
            string[] names = { "ClubSpeed", "Path", "AttackAngle", "FaceToTarget", "DynamicLoft" };

            for (int i = 0; i < 5; i++)
            {
                _tiles[i] = CreateTestDataTile(names[i]);
            }

            // Wire up references
            _panel.SetTileReferences(
                _tiles[0], _tiles[1], _tiles[2], _tiles[3], _tiles[4]
            );

            // Create test data
            _testShotWithHMT = CreateTestShotWithHMT();
            _testShotWithoutHMT = CreateTestShotWithoutHMT();
        }

        [TearDown]
        public void TearDown()
        {
            if (_panelGO != null)
            {
                Object.DestroyImmediate(_panelGO);
            }
        }

        #region Helper Methods

        private DataTile CreateTestDataTile(string name)
        {
            var tileGO = new GameObject(name);
            tileGO.transform.SetParent(_panelGO.transform);
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

        private GC2ShotData CreateTestShotWithHMT()
        {
            return new GC2ShotData
            {
                ShotId = 1,
                Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                BallSpeed = 150f,
                LaunchAngle = 12.5f,
                Direction = 2.0f,
                TotalSpin = 3000f,
                BackSpin = 2800f,
                SideSpin = 400f,
                SpinAxis = 8f,
                HasClubData = true,
                ClubSpeed = 105.2f,
                Path = 2.5f,            // In-to-out
                AttackAngle = -4.2f,    // Down (descending)
                FaceToTarget = -1.0f,   // Closed
                DynamicLoft = 14.5f,
                Lie = 0f
            };
        }

        private GC2ShotData CreateTestShotWithoutHMT()
        {
            return new GC2ShotData
            {
                ShotId = 2,
                Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                BallSpeed = 150f,
                LaunchAngle = 12.5f,
                Direction = 2.0f,
                TotalSpin = 3000f,
                BackSpin = 2800f,
                SideSpin = 400f,
                SpinAxis = 8f,
                HasClubData = false
            };
        }

        #endregion

        #region UpdateDisplay Tests

        [Test]
        public void UpdateDisplay_WithHMT_SetsAllTileValues()
        {
            _panel.UpdateDisplay(_testShotWithHMT, animate: false);

            Assert.IsTrue(_tiles[0].HasValue, "ClubSpeed should have value");
            Assert.IsTrue(_tiles[1].HasValue, "Path should have value");
            Assert.IsTrue(_tiles[2].HasValue, "AttackAngle should have value");
            Assert.IsTrue(_tiles[3].HasValue, "FaceToTarget should have value");
            Assert.IsTrue(_tiles[4].HasValue, "DynamicLoft should have value");
        }

        [Test]
        public void UpdateDisplay_WithHMT_ClubSpeed_ShowsCorrectValue()
        {
            _panel.UpdateDisplay(_testShotWithHMT, animate: false);

            Assert.AreEqual("105.2", _tiles[0].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_WithHMT_Path_InToOut_ShowsPositiveValue()
        {
            _panel.UpdateDisplay(_testShotWithHMT, animate: false);

            // Positive path = in-to-out
            Assert.AreEqual("2.5", _tiles[1].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_WithHMT_Path_OutToIn_ShowsNegativeValue()
        {
            _testShotWithHMT.Path = -3.5f;
            _panel.UpdateDisplay(_testShotWithHMT, animate: false);

            Assert.AreEqual("-3.5", _tiles[1].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_WithHMT_AttackAngle_Descending_ShowsNegativeValue()
        {
            _panel.UpdateDisplay(_testShotWithHMT, animate: false);

            // Negative = descending (down)
            Assert.AreEqual("-4.2", _tiles[2].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_WithHMT_AttackAngle_Ascending_ShowsPositiveValue()
        {
            _testShotWithHMT.AttackAngle = 3.0f;
            _panel.UpdateDisplay(_testShotWithHMT, animate: false);

            Assert.AreEqual("3.0", _tiles[2].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_WithHMT_FaceToTarget_Closed_ShowsNegativeValue()
        {
            _panel.UpdateDisplay(_testShotWithHMT, animate: false);

            // Negative = closed
            Assert.AreEqual("-1.0", _tiles[3].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_WithHMT_FaceToTarget_Open_ShowsPositiveValue()
        {
            _testShotWithHMT.FaceToTarget = 2.5f;
            _panel.UpdateDisplay(_testShotWithHMT, animate: false);

            Assert.AreEqual("2.5", _tiles[3].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_WithHMT_DynamicLoft_ShowsCorrectValue()
        {
            _panel.UpdateDisplay(_testShotWithHMT, animate: false);

            Assert.AreEqual("14.5", _tiles[4].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_WithoutHMT_DoesNotUpdateTiles()
        {
            _panel.UpdateDisplay(_testShotWithoutHMT, animate: false);

            Assert.IsFalse(_tiles[0].HasValue, "ClubSpeed should not have value");
            Assert.IsFalse(_tiles[1].HasValue, "Path should not have value");
            Assert.IsFalse(_tiles[2].HasValue, "AttackAngle should not have value");
            Assert.IsFalse(_tiles[3].HasValue, "FaceToTarget should not have value");
            Assert.IsFalse(_tiles[4].HasValue, "DynamicLoft should not have value");
        }

        [Test]
        public void UpdateDisplay_NullShot_DoesNothing()
        {
            _panel.UpdateDisplay(_testShotWithHMT, animate: false);
            _panel.UpdateDisplay(null, animate: false);

            // Should still have previous data
            Assert.AreEqual(_testShotWithHMT, _panel.LastShotData);
        }

        [Test]
        public void UpdateDisplay_StoresLastShotData()
        {
            _panel.UpdateDisplay(_testShotWithHMT, animate: false);

            Assert.AreEqual(_testShotWithHMT, _panel.LastShotData);
        }

        #endregion

        #region HasClubData Property Tests

        [Test]
        public void HasClubData_InitiallyFalse()
        {
            Assert.IsFalse(_panel.HasClubData);
        }

        [Test]
        public void HasClubData_TrueAfterHMTUpdate()
        {
            _panel.UpdateDisplay(_testShotWithHMT, animate: false);

            Assert.IsTrue(_panel.HasClubData);
        }

        [Test]
        public void HasClubData_FalseAfterNonHMTUpdate()
        {
            _panel.UpdateDisplay(_testShotWithHMT, animate: false);
            _panel.UpdateDisplay(_testShotWithoutHMT, animate: false);

            Assert.IsFalse(_panel.HasClubData);
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_ResetsAllTiles()
        {
            _panel.UpdateDisplay(_testShotWithHMT, animate: false);
            _panel.Clear();

            for (int i = 0; i < 5; i++)
            {
                Assert.IsFalse(_tiles[i].HasValue, $"Tile {i} should not have value after clear");
            }
        }

        [Test]
        public void Clear_ResetsLastShotData()
        {
            _panel.UpdateDisplay(_testShotWithHMT, animate: false);
            _panel.Clear();

            Assert.IsNull(_panel.LastShotData);
        }

        [Test]
        public void Clear_SetsHasClubDataFalse()
        {
            _panel.UpdateDisplay(_testShotWithHMT, animate: false);
            Assert.IsTrue(_panel.HasClubData);

            _panel.Clear();
            Assert.IsFalse(_panel.HasClubData);
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnDisplayUpdated_FiresOnHMTUpdate()
        {
            bool eventFired = false;
            GC2ShotData receivedShot = null;

            _panel.OnDisplayUpdated += (shot) =>
            {
                eventFired = true;
                receivedShot = shot;
            };

            _panel.UpdateDisplay(_testShotWithHMT, animate: false);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(_testShotWithHMT, receivedShot);
        }

        [Test]
        public void OnDisplayUpdated_DoesNotFireWithoutHMT()
        {
            bool eventFired = false;
            _panel.OnDisplayUpdated += (shot) => eventFired = true;

            _panel.UpdateDisplay(_testShotWithoutHMT, animate: false);

            Assert.IsFalse(eventFired);
        }

        [Test]
        public void OnDisplayCleared_FiresOnClear()
        {
            bool eventFired = false;
            _panel.OnDisplayCleared += () => eventFired = true;

            _panel.UpdateDisplay(_testShotWithHMT, animate: false);
            _panel.Clear();

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void OnVisibilityChanged_FiresOnShow()
        {
            bool eventFired = false;
            bool receivedVisibility = false;

            _panel.OnVisibilityChanged += (visible) =>
            {
                eventFired = true;
                receivedVisibility = visible;
            };

            _panel.Show();

            Assert.IsTrue(eventFired);
            Assert.IsTrue(receivedVisibility);
        }

        [Test]
        public void OnVisibilityChanged_FiresOnHide()
        {
            bool eventFired = false;
            bool receivedVisibility = true;

            _panel.OnVisibilityChanged += (visible) =>
            {
                eventFired = true;
                receivedVisibility = visible;
            };

            _panel.Show();
            _panel.Hide();

            Assert.IsTrue(eventFired);
            Assert.IsFalse(receivedVisibility);
        }

        #endregion

        #region Tile Access Tests

        [Test]
        public void TileCount_ReturnsFive()
        {
            Assert.AreEqual(5, _panel.TileCount);
        }

        [Test]
        public void GetTile_ValidIndex_ReturnsTile()
        {
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(_tiles[i], _panel.GetTile(i));
            }
        }

        [Test]
        public void GetTile_InvalidIndex_ReturnsNull()
        {
            Assert.IsNull(_panel.GetTile(-1));
            Assert.IsNull(_panel.GetTile(5));
            Assert.IsNull(_panel.GetTile(100));
        }

        #endregion

        #region Label Tests

        [Test]
        public void Tiles_HaveCorrectLabels()
        {
            string[] expectedLabels = { "CLUB SPEED", "PATH", "ATTACK", "FACE", "LOFT" };

            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(expectedLabels[i], _tiles[i].Label, $"Tile {i} should have label '{expectedLabels[i]}'");
            }
        }

        [Test]
        public void Tiles_HaveCorrectUnits()
        {
            string[] expectedUnits = { "mph", "deg", "deg", "deg", "deg" };

            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(expectedUnits[i], _tiles[i].Unit, $"Tile {i} should have unit '{expectedUnits[i]}'");
            }
        }

        #endregion

        #region Visibility Tests

        [Test]
        public void IsVisible_InitiallyFalse()
        {
            Assert.IsFalse(_panel.IsVisible);
        }

        [Test]
        public void Show_SetsIsVisibleTrue()
        {
            _panel.Show();

            Assert.IsTrue(_panel.IsVisible);
        }

        [Test]
        public void Hide_SetsIsVisibleFalse()
        {
            _panel.Show();
            _panel.Hide();

            Assert.IsFalse(_panel.IsVisible);
        }

        #endregion

        #region Path Direction Tests

        [Test]
        public void GetPathDirection_Positive_ReturnsInToOut()
        {
            Assert.AreEqual(SwingPathDirection.InToOut, ClubDataPanel.GetPathDirection(2.5f));
        }

        [Test]
        public void GetPathDirection_Negative_ReturnsOutToIn()
        {
            Assert.AreEqual(SwingPathDirection.OutToIn, ClubDataPanel.GetPathDirection(-2.5f));
        }

        [Test]
        public void GetPathDirection_Zero_ReturnsNeutral()
        {
            Assert.AreEqual(SwingPathDirection.Neutral, ClubDataPanel.GetPathDirection(0f));
        }

        [Test]
        public void GetPathDirection_SmallPositive_ReturnsNeutral()
        {
            Assert.AreEqual(SwingPathDirection.Neutral, ClubDataPanel.GetPathDirection(0.3f));
        }

        [Test]
        public void GetPathDirection_SmallNegative_ReturnsNeutral()
        {
            Assert.AreEqual(SwingPathDirection.Neutral, ClubDataPanel.GetPathDirection(-0.3f));
        }

        #endregion

        #region Attack Angle Direction Tests

        [Test]
        public void GetAttackDirection_Positive_ReturnsAscending()
        {
            Assert.AreEqual(AttackAngleDirection.Ascending, ClubDataPanel.GetAttackDirection(3.0f));
        }

        [Test]
        public void GetAttackDirection_Negative_ReturnsDescending()
        {
            Assert.AreEqual(AttackAngleDirection.Descending, ClubDataPanel.GetAttackDirection(-4.2f));
        }

        [Test]
        public void GetAttackDirection_Zero_ReturnsNeutral()
        {
            Assert.AreEqual(AttackAngleDirection.Neutral, ClubDataPanel.GetAttackDirection(0f));
        }

        #endregion

        #region Face Direction Tests

        [Test]
        public void GetFaceDirection_Positive_ReturnsOpen()
        {
            Assert.AreEqual(FaceDirection.Open, ClubDataPanel.GetFaceDirection(2.5f));
        }

        [Test]
        public void GetFaceDirection_Negative_ReturnsClosed()
        {
            Assert.AreEqual(FaceDirection.Closed, ClubDataPanel.GetFaceDirection(-1.0f));
        }

        [Test]
        public void GetFaceDirection_Zero_ReturnsSquare()
        {
            Assert.AreEqual(FaceDirection.Square, ClubDataPanel.GetFaceDirection(0f));
        }

        [Test]
        public void GetFaceDirection_SmallPositive_ReturnsSquare()
        {
            Assert.AreEqual(FaceDirection.Square, ClubDataPanel.GetFaceDirection(0.3f));
        }

        #endregion

        #region Typical Shot Tests

        [Test]
        public void UpdateDisplay_TypicalDriver_DisplaysCorrectly()
        {
            var shot = new GC2ShotData
            {
                HasClubData = true,
                ClubSpeed = 112.5f,
                Path = 3.5f,
                AttackAngle = 4.0f,   // Ascending for driver
                FaceToTarget = 1.0f,  // Slightly open
                DynamicLoft = 12.5f
            };

            _panel.UpdateDisplay(shot, animate: false);

            Assert.AreEqual("112.5", _tiles[0].GetDisplayedValue());
            Assert.AreEqual("3.5", _tiles[1].GetDisplayedValue());
            Assert.AreEqual("4.0", _tiles[2].GetDisplayedValue());
            Assert.AreEqual("1.0", _tiles[3].GetDisplayedValue());
            Assert.AreEqual("12.5", _tiles[4].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_TypicalIron_DisplaysCorrectly()
        {
            var shot = new GC2ShotData
            {
                HasClubData = true,
                ClubSpeed = 85.0f,
                Path = 0f,            // Neutral
                AttackAngle = -5.5f,  // Descending for iron
                FaceToTarget = -0.5f, // Slightly closed
                DynamicLoft = 22.0f
            };

            _panel.UpdateDisplay(shot, animate: false);

            Assert.AreEqual("85.0", _tiles[0].GetDisplayedValue());
            Assert.AreEqual("0.0", _tiles[1].GetDisplayedValue());
            Assert.AreEqual("-5.5", _tiles[2].GetDisplayedValue());
            Assert.AreEqual("-0.5", _tiles[3].GetDisplayedValue());
            Assert.AreEqual("22.0", _tiles[4].GetDisplayedValue());
        }

        #endregion

        #region Multiple Updates Tests

        [Test]
        public void UpdateDisplay_MultipleHMTUpdates_OverwritesPreviousValues()
        {
            _panel.UpdateDisplay(_testShotWithHMT, animate: false);

            var newShot = new GC2ShotData
            {
                HasClubData = true,
                ClubSpeed = 110f,
                Path = -2f,
                AttackAngle = 2f,
                FaceToTarget = 3f,
                DynamicLoft = 18f
            };

            _panel.UpdateDisplay(newShot, animate: false);

            Assert.AreEqual("110.0", _tiles[0].GetDisplayedValue());
            Assert.AreEqual("-2.0", _tiles[1].GetDisplayedValue());
            Assert.AreEqual("2.0", _tiles[2].GetDisplayedValue());
            Assert.AreEqual("3.0", _tiles[3].GetDisplayedValue());
            Assert.AreEqual("18.0", _tiles[4].GetDisplayedValue());
        }

        [Test]
        public void UpdateDisplay_HMTThenNoHMT_ClearsTiles()
        {
            _panel.UpdateDisplay(_testShotWithHMT, animate: false);
            _panel.UpdateDisplay(_testShotWithoutHMT, animate: false);

            for (int i = 0; i < 5; i++)
            {
                Assert.IsFalse(_tiles[i].HasValue, $"Tile {i} should not have value after non-HMT update");
            }
        }

        #endregion
    }
}
