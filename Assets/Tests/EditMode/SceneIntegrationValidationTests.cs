// ABOUTME: Validates that all prefabs referenced in SceneGenerator exist and are properly wired.
// ABOUTME: Prevents missing prefab/component issues by catching them in CI before they reach builds.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace OpenRange.Tests.EditMode
{
    /// <summary>
    /// Tests that validate scene integration - ensuring all prefabs exist and are properly wired.
    /// These tests catch issues like missing prefabs or unconnected serialized fields before builds.
    /// </summary>
    [TestFixture]
    public class SceneIntegrationValidationTests
    {
        // All prefabs that SceneGenerator references and expects to exist
        private static readonly string[] RequiredPrefabs = new[]
        {
            // Ball
            "Assets/Prefabs/Ball/GolfBall.prefab",

            // Camera
            "Assets/Prefabs/Camera/CameraRig.prefab",

            // Effects
            "Assets/Prefabs/Effects/LandingDust.prefab",
            "Assets/Prefabs/Effects/LandingMarker.prefab",
            "Assets/Prefabs/Effects/TrajectoryLine.prefab",

            // Environment
            "Assets/Prefabs/Environment/DistanceMarker.prefab",
            "Assets/Prefabs/Environment/TargetGreen.prefab",
            "Assets/Prefabs/Environment/TeeMat.prefab",

            // UI - Main components
            "Assets/Prefabs/UI/BallReadyIndicator.prefab",
            "Assets/Prefabs/UI/ClubDataPanel.prefab",
            "Assets/Prefabs/UI/ConnectionPanel.prefab",
            "Assets/Prefabs/UI/ConnectionStatus.prefab",
            "Assets/Prefabs/UI/GSProModeUI.prefab",
            "Assets/Prefabs/UI/SessionInfoPanel.prefab",
            "Assets/Prefabs/UI/SettingsPanel.prefab",
            "Assets/Prefabs/UI/ShotDataBar.prefab",
            "Assets/Prefabs/UI/ShotDetailModal.prefab",
            "Assets/Prefabs/UI/ShotHistoryPanel.prefab",

            // UI - Sub-components that need to exist for wiring
            "Assets/Prefabs/UI/ShotHistoryItem.prefab",
            "Assets/Prefabs/UI/Toast.prefab",
        };

        // Prefabs that are sub-components (embedded in parent prefabs, don't need direct scene instantiation)
        private static readonly string[] SubComponentPrefabs = new[]
        {
            "Assets/Prefabs/UI/AttackAngleIndicator.prefab",
            "Assets/Prefabs/UI/SwingPathIndicator.prefab",
            "Assets/Prefabs/UI/DataTile.prefab",
            "Assets/Prefabs/UI/SettingToggle.prefab",
            "Assets/Prefabs/UI/SettingSlider.prefab",
            "Assets/Prefabs/UI/SettingDropdown.prefab",
            "Assets/Prefabs/UI/UICanvas.prefab",
            "Assets/Prefabs/Ball/BallTrail.prefab",
        };

        [Test]
        public void AllRequiredPrefabs_Exist()
        {
            var missingPrefabs = new List<string>();

            foreach (var prefabPath in RequiredPrefabs)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    missingPrefabs.Add(prefabPath);
                }
            }

            Assert.IsEmpty(missingPrefabs,
                $"Missing required prefabs that SceneGenerator expects:\n" +
                $"  - {string.Join("\n  - ", missingPrefabs)}\n\n" +
                $"Run the corresponding 'OpenRange > Create...' menu items to generate them.");
        }

        [Test]
        public void AllSubComponentPrefabs_Exist()
        {
            var missingPrefabs = new List<string>();

            foreach (var prefabPath in SubComponentPrefabs)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    missingPrefabs.Add(prefabPath);
                }
            }

            Assert.IsEmpty(missingPrefabs,
                $"Missing sub-component prefabs:\n" +
                $"  - {string.Join("\n  - ", missingPrefabs)}\n\n" +
                $"These are embedded in parent prefabs but should still exist.");
        }

        [Test]
        public void SceneGenerator_ReferencesAllExpectedPrefabs()
        {
            // Read SceneGenerator.cs and extract all prefab paths
            var sceneGeneratorPath = "Assets/Editor/SceneGenerator.cs";
            Assert.IsTrue(File.Exists(sceneGeneratorPath), "SceneGenerator.cs should exist");

            var content = File.ReadAllText(sceneGeneratorPath);
            var referencedPrefabs = new HashSet<string>();

            // Extract all prefab path strings
            var matches = System.Text.RegularExpressions.Regex.Matches(
                content,
                @"Assets/Prefabs/[^""]+\.prefab");

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                referencedPrefabs.Add(match.Value);
            }

            // Verify all required prefabs are referenced
            var unreferencedPrefabs = RequiredPrefabs
                .Where(p => !referencedPrefabs.Contains(p))
                .ToList();

            Assert.IsEmpty(unreferencedPrefabs,
                $"Prefabs in RequiredPrefabs list but not referenced in SceneGenerator:\n" +
                $"  - {string.Join("\n  - ", unreferencedPrefabs)}\n\n" +
                $"Either add them to SceneGenerator or remove from RequiredPrefabs.");
        }

        [Test]
        public void GolfBallPrefab_HasRequiredComponents()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ball/GolfBall.prefab");
            Assert.IsNotNull(prefab, "GolfBall.prefab should exist");

            var ballController = prefab.GetComponent<Visualization.BallController>();
            Assert.IsNotNull(ballController, "GolfBall should have BallController component");

            var ballVisuals = prefab.GetComponent<Visualization.BallVisuals>();
            Assert.IsNotNull(ballVisuals, "GolfBall should have BallVisuals component");

            var ballSpinner = prefab.GetComponent<Visualization.BallSpinner>();
            Assert.IsNotNull(ballSpinner, "GolfBall should have BallSpinner component");
        }

        [Test]
        public void CameraRigPrefab_HasRequiredComponents()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Camera/CameraRig.prefab");
            Assert.IsNotNull(prefab, "CameraRig.prefab should exist");

            var cameraController = prefab.GetComponent<Visualization.CameraController>();
            Assert.IsNotNull(cameraController, "CameraRig should have CameraController component");

            var camera = prefab.GetComponentInChildren<Camera>();
            Assert.IsNotNull(camera, "CameraRig should have a Camera in children");
        }

        [Test]
        public void ClubDataPanel_HasIndicatorChildren()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/ClubDataPanel.prefab");
            Assert.IsNotNull(prefab, "ClubDataPanel.prefab should exist");

            var clubDataPanel = prefab.GetComponent<UI.ClubDataPanel>();
            Assert.IsNotNull(clubDataPanel, "ClubDataPanel should have ClubDataPanel component");

            // Check that indicator references are set via SerializedObject
            var so = new SerializedObject(clubDataPanel);

            var swingPathProp = so.FindProperty("_swingPathIndicator");
            Assert.IsNotNull(swingPathProp, "ClubDataPanel should have _swingPathIndicator field");

            var attackAngleProp = so.FindProperty("_attackAngleIndicator");
            Assert.IsNotNull(attackAngleProp, "ClubDataPanel should have _attackAngleIndicator field");
        }

        [Test]
        public void ShotDataBar_HasDataTileChildren()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/ShotDataBar.prefab");
            Assert.IsNotNull(prefab, "ShotDataBar.prefab should exist");

            var shotDataBar = prefab.GetComponent<UI.ShotDataBar>();
            Assert.IsNotNull(shotDataBar, "ShotDataBar should have ShotDataBar component");

            // Should have multiple DataTile children
            var dataTiles = prefab.GetComponentsInChildren<UI.DataTile>();
            Assert.IsTrue(dataTiles.Length >= 5, $"ShotDataBar should have at least 5 DataTile children, found {dataTiles.Length}");
        }

        [Test]
        public void ShotHistoryItemPrefab_HasRequiredComponent()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/ShotHistoryItem.prefab");
            Assert.IsNotNull(prefab, "ShotHistoryItem.prefab should exist");

            var item = prefab.GetComponent<UI.ShotHistoryItem>();
            Assert.IsNotNull(item, "ShotHistoryItem.prefab should have ShotHistoryItem component");
        }

        [Test]
        public void ToastPrefab_HasRequiredComponent()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Toast.prefab");
            Assert.IsNotNull(prefab, "Toast.prefab should exist");

            var toast = prefab.GetComponent<UI.Toast>();
            Assert.IsNotNull(toast, "Toast.prefab should have Toast component");
        }

        [Test]
        public void BallReadyIndicatorPrefab_HasRequiredComponent()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/BallReadyIndicator.prefab");
            Assert.IsNotNull(prefab, "BallReadyIndicator.prefab should exist");

            var indicator = prefab.GetComponent<UI.BallReadyIndicator>();
            Assert.IsNotNull(indicator, "BallReadyIndicator.prefab should have BallReadyIndicator component");
        }

        [Test]
        public void GSProModeUIPrefab_HasRequiredComponent()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/GSProModeUI.prefab");
            Assert.IsNotNull(prefab, "GSProModeUI.prefab should exist");

            var ui = prefab.GetComponent<UI.GSProModeUI>();
            Assert.IsNotNull(ui, "GSProModeUI.prefab should have GSProModeUI component");
        }

        [Test]
        public void SceneGenerator_CreatesUIManager()
        {
            // Verify SceneGenerator code creates UIManager
            var sceneGeneratorPath = "Assets/Editor/SceneGenerator.cs";
            var content = File.ReadAllText(sceneGeneratorPath);

            Assert.IsTrue(content.Contains("AddComponent<UIManager>()"),
                "SceneGenerator should create UIManager component in Marina scene");

            Assert.IsTrue(content.Contains("_toastPrefab"),
                "SceneGenerator should wire _toastPrefab to UIManager");

            Assert.IsTrue(content.Contains("_toastContainer"),
                "SceneGenerator should wire _toastContainer to UIManager");
        }

        [Test]
        public void SceneGenerator_WiresShotHistoryItemPrefab()
        {
            // Verify SceneGenerator wires ShotHistoryItem prefab to ShotHistoryPanel
            var sceneGeneratorPath = "Assets/Editor/SceneGenerator.cs";
            var content = File.ReadAllText(sceneGeneratorPath);

            Assert.IsTrue(content.Contains("ShotHistoryItem.prefab"),
                "SceneGenerator should load ShotHistoryItem.prefab");

            Assert.IsTrue(content.Contains("_itemPrefab"),
                "SceneGenerator should wire _itemPrefab property");
        }

        [Test]
        public void MarinaSceneController_HasAllExpectedSerializedFields()
        {
            // Get all serialized fields from MarinaSceneController
            var controllerType = typeof(UI.MarinaSceneController);
            var fields = controllerType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.GetCustomAttribute<SerializeField>() != null)
                .Select(f => f.Name)
                .ToList();

            // Expected fields that must exist
            var expectedFields = new[]
            {
                "_backButton",
                "_settingsButton",
                "_shotDataBar",
                "_clubDataPanel",
                "_sessionInfoPanel",
                "_shotHistoryPanel",
                "_shotDetailModal",
                "_connectionStatusUI",
                "_connectionPanel",
                "_ballReadyIndicator",
                "_gsProModeUI",
                "_settingsPanel",
                "_ballController",
                "_trajectoryRenderer"
            };

            var missingFields = expectedFields.Where(f => !fields.Contains(f)).ToList();

            Assert.IsEmpty(missingFields,
                $"MarinaSceneController is missing serialized fields:\n" +
                $"  - {string.Join("\n  - ", missingFields)}");
        }

        [Test]
        public void AllPrefabDirectories_Exist()
        {
            var requiredDirectories = new[]
            {
                "Assets/Prefabs/Ball",
                "Assets/Prefabs/Camera",
                "Assets/Prefabs/Effects",
                "Assets/Prefabs/Environment",
                "Assets/Prefabs/UI"
            };

            var missingDirs = requiredDirectories
                .Where(d => !AssetDatabase.IsValidFolder(d))
                .ToList();

            Assert.IsEmpty(missingDirs,
                $"Missing prefab directories:\n  - {string.Join("\n  - ", missingDirs)}");
        }

        [Test]
        public void NoPrefabsInRequiredList_AreMissing()
        {
            // This is the main integration test that CI should catch
            var allPrefabPaths = RequiredPrefabs.Concat(SubComponentPrefabs).ToList();
            var missingCount = allPrefabPaths.Count(p => AssetDatabase.LoadAssetAtPath<GameObject>(p) == null);

            Assert.AreEqual(0, missingCount,
                $"{missingCount} prefab(s) are missing. Run 'make test' locally and check " +
                $"AllRequiredPrefabs_Exist and AllSubComponentPrefabs_Exist tests for details.");
        }
    }
}
