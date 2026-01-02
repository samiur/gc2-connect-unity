// ABOUTME: Editor tool that generates the initial scene structure for the application.
// ABOUTME: Creates Bootstrap, MainMenu, and Marina scenes with proper GameObjects.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;
using OpenRange.UI;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor utility to generate the initial scene structure for the application.
    /// </summary>
    public static class SceneGenerator
    {
        private const string ScenesPath = "Assets/Scenes";
        private const string RangesPath = "Assets/Scenes/Ranges";
        private const string MaterialsPath = "Assets/Materials/Environment";

        [MenuItem("OpenRange/Generate All Scenes", priority = 100)]
        public static void GenerateAllScenes()
        {
            if (!EditorUtility.DisplayDialog(
                "Generate Scenes",
                "This will create Bootstrap.unity, MainMenu.unity, and Ranges/Marina.unity scenes. Existing scenes will be overwritten. Continue?",
                "Generate",
                "Cancel"))
            {
                return;
            }

            EnsureDirectoriesExist();

            GenerateBootstrapScene();
            GenerateMainMenuScene();
            GenerateMarinaScene();
            UpdateBuildSettings();

            AssetDatabase.Refresh();
            Debug.Log("SceneGenerator: All scenes generated successfully!");
        }

        [MenuItem("OpenRange/Generate Bootstrap Scene", priority = 101)]
        public static void GenerateBootstrapScene()
        {
            EnsureDirectoriesExist();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Add GameManager as ROOT object (DontDestroyOnLoad requires root)
            var gameManagerGo = new GameObject("GameManager");
            var gameManager = gameManagerGo.AddComponent<Core.GameManager>();

            // Add ShotProcessor as child of GameManager
            var shotProcessorGo = new GameObject("ShotProcessor");
            shotProcessorGo.transform.SetParent(gameManagerGo.transform);
            var shotProcessor = shotProcessorGo.AddComponent<Core.ShotProcessor>();

            // Add SessionManager as child of GameManager
            var sessionManagerGo = new GameObject("SessionManager");
            sessionManagerGo.transform.SetParent(gameManagerGo.transform);
            var sessionManager = sessionManagerGo.AddComponent<Core.SessionManager>();

            // Add SettingsManager as child of GameManager
            var settingsManagerGo = new GameObject("SettingsManager");
            settingsManagerGo.transform.SetParent(gameManagerGo.transform);
            var settingsManager = settingsManagerGo.AddComponent<Core.SettingsManager>();

            // Wire up references using SerializedObject
            var gameManagerSo = new SerializedObject(gameManager);
            gameManagerSo.FindProperty("_shotProcessor").objectReferenceValue = shotProcessor;
            gameManagerSo.FindProperty("_sessionManager").objectReferenceValue = sessionManager;
            gameManagerSo.FindProperty("_settingsManager").objectReferenceValue = settingsManager;
            gameManagerSo.ApplyModifiedPropertiesWithoutUndo();

            // Add MainThreadDispatcher as ROOT object (has its own DontDestroyOnLoad)
            var dispatcherGo = new GameObject("MainThreadDispatcher");
            dispatcherGo.AddComponent<Utilities.MainThreadDispatcher>();

            // Add EventSystem
            CreateEventSystem();

            // Add BootstrapLoader
            var bootstrapGo = new GameObject("BootstrapLoader");
            var bootstrapLoader = bootstrapGo.AddComponent<Core.BootstrapLoader>();

            // Wire up bootstrap references
            var bootstrapSo = new SerializedObject(bootstrapLoader);
            bootstrapSo.FindProperty("_gameManager").objectReferenceValue = gameManager;
            bootstrapSo.FindProperty("_settingsManager").objectReferenceValue = settingsManager;
            bootstrapSo.ApplyModifiedPropertiesWithoutUndo();

            SaveScene(scene, $"{ScenesPath}/Bootstrap.unity");
            Debug.Log("SceneGenerator: Bootstrap scene created");
        }

        [MenuItem("OpenRange/Generate MainMenu Scene", priority = 102)]
        public static void GenerateMainMenuScene()
        {
            EnsureDirectoriesExist();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Main Camera
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            cameraGo.AddComponent<AudioListener>();

            // Event System
            CreateEventSystem();

            // UI Canvas
            var canvasGo = CreateUICanvas("Canvas");

            // Title Text
            var titleGo = CreateTextElement(canvasGo.transform, "Title", "GC2 Open Range");
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.7f);
            titleRect.anchorMax = new Vector2(0.5f, 0.7f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(600, 100);
            var titleText = titleGo.GetComponent<TextMeshProUGUI>();
            titleText.fontSize = 72;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;

            // Open Range Button
            var openRangeBtn = CreateButton(canvasGo.transform, "OpenRangeButton", "Open Range");
            var openRangeRect = openRangeBtn.GetComponent<RectTransform>();
            openRangeRect.anchorMin = new Vector2(0.5f, 0.4f);
            openRangeRect.anchorMax = new Vector2(0.5f, 0.4f);
            openRangeRect.anchoredPosition = Vector2.zero;
            openRangeRect.sizeDelta = new Vector2(300, 60);

            // Add MainMenuController
            var controllerGo = new GameObject("MainMenuController");
            var controller = controllerGo.AddComponent<MainMenuController>();

            // Wire up button click
            var button = openRangeBtn.GetComponent<Button>();
            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("_openRangeButton").objectReferenceValue = button;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            // Connection Status Panel (placeholder)
            var statusPanelGo = new GameObject("ConnectionStatusPanel");
            statusPanelGo.transform.SetParent(canvasGo.transform);
            var statusRect = statusPanelGo.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(1, 1);
            statusRect.anchorMax = new Vector2(1, 1);
            statusRect.pivot = new Vector2(1, 1);
            statusRect.anchoredPosition = new Vector2(-20, -20);
            statusRect.sizeDelta = new Vector2(200, 40);

            var statusBg = statusPanelGo.AddComponent<Image>();
            statusBg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            var statusTextGo = CreateTextElement(statusPanelGo.transform, "StatusText", "Disconnected");
            var statusTextRect = statusTextGo.GetComponent<RectTransform>();
            statusTextRect.anchorMin = Vector2.zero;
            statusTextRect.anchorMax = Vector2.one;
            statusTextRect.offsetMin = new Vector2(10, 5);
            statusTextRect.offsetMax = new Vector2(-10, -5);
            var statusText = statusTextGo.GetComponent<TextMeshProUGUI>();
            statusText.fontSize = 18;
            statusText.alignment = TextAlignmentOptions.MidlineRight;
            statusText.color = Color.gray;

            // Settings Button (placeholder, disabled)
            var settingsBtn = CreateButton(canvasGo.transform, "SettingsButton", "Settings");
            var settingsRect = settingsBtn.GetComponent<RectTransform>();
            settingsRect.anchorMin = new Vector2(0.5f, 0.25f);
            settingsRect.anchorMax = new Vector2(0.5f, 0.25f);
            settingsRect.anchoredPosition = Vector2.zero;
            settingsRect.sizeDelta = new Vector2(200, 50);
            settingsBtn.GetComponent<Button>().interactable = false;
            var settingsColors = settingsBtn.GetComponent<Button>().colors;
            settingsColors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            settingsBtn.GetComponent<Button>().colors = settingsColors;

            SaveScene(scene, $"{ScenesPath}/MainMenu.unity");
            Debug.Log("SceneGenerator: MainMenu scene created");
        }

        [MenuItem("OpenRange/Generate Marina Scene", priority = 103)]
        public static void GenerateMarinaScene()
        {
            EnsureDirectoriesExist();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Load and instantiate CameraRig prefab (replaces manual camera creation)
            Visualization.CameraController cameraController = null;
            var cameraRigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Camera/CameraRig.prefab");
            if (cameraRigPrefab != null)
            {
                var cameraRig = PrefabUtility.InstantiatePrefab(cameraRigPrefab) as GameObject;
                cameraRig.transform.position = Vector3.zero;
                cameraController = cameraRig.GetComponent<Visualization.CameraController>();
                Debug.Log("SceneGenerator: Added CameraRig prefab to scene");
            }
            else
            {
                // Fallback: Create basic camera if prefab doesn't exist
                Debug.LogWarning("SceneGenerator: CameraRig.prefab not found, creating basic camera. Run 'OpenRange > Create Camera Rig Prefab' first.");
                var cameraGo = new GameObject("Main Camera");
                cameraGo.tag = "MainCamera";
                cameraGo.transform.position = new Vector3(0, 5, -10);
                cameraGo.transform.rotation = Quaternion.Euler(15, 0, 0);
                var camera = cameraGo.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.Skybox;
                cameraGo.AddComponent<AudioListener>();
            }

            // Directional Light (Sun)
            var lightGo = new GameObject("Directional Light");
            lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.84f);
            light.intensity = 1.0f;
            light.shadows = LightShadows.Soft;

            // Ground Plane
            var groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundGo.name = "Ground";
            groundGo.transform.position = Vector3.zero;
            groundGo.transform.localScale = new Vector3(50, 1, 50);
            var groundRenderer = groundGo.GetComponent<MeshRenderer>();

            // Create and save a proper grass material
            var grassMaterial = CreateGrassMaterial();
            if (grassMaterial != null)
            {
                groundRenderer.sharedMaterial = grassMaterial;
                Debug.Log($"SceneGenerator: Assigned grass material to ground. Material shader: {grassMaterial.shader?.name ?? "NULL"}");
            }
            else
            {
                Debug.LogError("SceneGenerator: Failed to create grass material!");
            }

            // Load and instantiate GolfBall prefab
            Visualization.BallController ballController = null;
            var golfBallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ball/GolfBall.prefab");
            if (golfBallPrefab != null)
            {
                var golfBall = PrefabUtility.InstantiatePrefab(golfBallPrefab) as GameObject;
                golfBall.transform.position = new Vector3(0f, 0.02f, 0f); // Slightly above ground (ball radius)
                ballController = golfBall.GetComponent<Visualization.BallController>();
                Debug.Log("SceneGenerator: Added GolfBall prefab to scene");

                // Wire CameraController to BallController for auto-follow
                if (cameraController != null && ballController != null)
                {
                    var cameraSo = new SerializedObject(cameraController);
                    cameraSo.FindProperty("_ballController").objectReferenceValue = ballController;
                    cameraSo.FindProperty("_ballTransform").objectReferenceValue = golfBall.transform;
                    cameraSo.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("SceneGenerator: Wired CameraController to BallController");
                }
            }
            else
            {
                Debug.LogWarning("SceneGenerator: GolfBall.prefab not found. Run 'OpenRange > Create Golf Ball Prefab' first.");
            }

            // Load and instantiate TrajectoryLine prefab
            Visualization.TrajectoryRenderer trajectoryRenderer = null;
            var trajectoryLinePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/TrajectoryLine.prefab");
            if (trajectoryLinePrefab != null)
            {
                var trajectoryLine = PrefabUtility.InstantiatePrefab(trajectoryLinePrefab) as GameObject;
                trajectoryLine.transform.position = Vector3.zero;
                trajectoryRenderer = trajectoryLine.GetComponent<Visualization.TrajectoryRenderer>();
                Debug.Log("SceneGenerator: Added TrajectoryLine prefab to scene");
            }
            else
            {
                Debug.LogWarning("SceneGenerator: TrajectoryLine.prefab not found. Run 'OpenRange > Create Trajectory Line Prefab' first.");
            }

            // Create EffectsManager with prefab references
            var effectsManagerGo = new GameObject("EffectsManager");
            var effectsManager = effectsManagerGo.AddComponent<Visualization.EffectsManager>();

            // Load landing effects prefabs
            var landingMarkerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/LandingMarker.prefab");
            var landingDustPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/LandingDust.prefab");

            if (landingMarkerPrefab != null || landingDustPrefab != null)
            {
                var effectsSo = new SerializedObject(effectsManager);
                if (landingMarkerPrefab != null)
                {
                    var markerComponent = landingMarkerPrefab.GetComponent<Visualization.LandingMarker>();
                    effectsSo.FindProperty("_landingMarkerPrefab").objectReferenceValue = markerComponent;
                    Debug.Log("SceneGenerator: Configured LandingMarker prefab on EffectsManager");
                }
                if (landingDustPrefab != null)
                {
                    var effectComponent = landingDustPrefab.GetComponent<Visualization.ImpactEffect>();
                    effectsSo.FindProperty("_impactEffectPrefab").objectReferenceValue = effectComponent;
                    Debug.Log("SceneGenerator: Configured LandingDust prefab on EffectsManager");
                }
                effectsSo.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning("SceneGenerator: Landing effect prefabs not found. Run 'OpenRange > Create All Landing Effects' first.");
            }

            // Create EnvironmentManager with prefab references
            var envManagerGo = new GameObject("EnvironmentManager");
            var envManager = envManagerGo.AddComponent<Visualization.EnvironmentManager>();

            // Load environment prefabs
            var distanceMarkerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/DistanceMarker.prefab");
            var targetGreenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/TargetGreen.prefab");
            var teeMatPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/TeeMat.prefab");

            var envSo = new SerializedObject(envManager);
            envSo.FindProperty("_groundPlane").objectReferenceValue = groundGo.transform;
            envSo.FindProperty("_sunLight").objectReferenceValue = light;

            if (distanceMarkerPrefab != null)
            {
                var markerComponent = distanceMarkerPrefab.GetComponent<Visualization.DistanceMarker>();
                envSo.FindProperty("_distanceMarkerPrefab").objectReferenceValue = markerComponent;
                Debug.Log("SceneGenerator: Configured DistanceMarker prefab on EnvironmentManager");
            }
            if (targetGreenPrefab != null)
            {
                var greenComponent = targetGreenPrefab.GetComponent<Visualization.TargetGreen>();
                envSo.FindProperty("_targetGreenPrefab").objectReferenceValue = greenComponent;
                Debug.Log("SceneGenerator: Configured TargetGreen prefab on EnvironmentManager");
            }
            if (teeMatPrefab != null)
            {
                var teeMatComponent = teeMatPrefab.GetComponent<Visualization.TeeMat>();
                envSo.FindProperty("_teeMatPrefab").objectReferenceValue = teeMatComponent;
                Debug.Log("SceneGenerator: Configured TeeMat prefab on EnvironmentManager");
            }
            envSo.ApplyModifiedPropertiesWithoutUndo();

            // Instantiate Tee Mat at origin
            if (teeMatPrefab != null)
            {
                var teeMat = PrefabUtility.InstantiatePrefab(teeMatPrefab) as GameObject;
                teeMat.transform.position = Vector3.zero;
                Debug.Log("SceneGenerator: Added TeeMat to scene");
            }
            else
            {
                Debug.LogWarning("SceneGenerator: TeeMat.prefab not found. Run 'OpenRange > Create All Environment Prefabs' first.");
            }

            // Instantiate Distance Markers at standard intervals
            if (distanceMarkerPrefab != null)
            {
                int[] distances = { 50, 100, 150, 200, 250, 300 };
                foreach (int distance in distances)
                {
                    var marker = PrefabUtility.InstantiatePrefab(distanceMarkerPrefab) as GameObject;
                    float zPos = distance * Visualization.EnvironmentManager.YardsToMeters;
                    marker.transform.position = new Vector3(-5f, 0f, zPos);

                    // Set distance value
                    var markerComponent = marker.GetComponent<Visualization.DistanceMarker>();
                    if (markerComponent != null)
                    {
                        var markerSo = new SerializedObject(markerComponent);
                        markerSo.FindProperty("_distance").intValue = distance;
                        markerSo.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                Debug.Log("SceneGenerator: Added distance markers to scene");
            }
            else
            {
                Debug.LogWarning("SceneGenerator: DistanceMarker.prefab not found. Run 'OpenRange > Create All Environment Prefabs' first.");
            }

            // Instantiate Target Greens at key distances
            if (targetGreenPrefab != null)
            {
                int[] greenDistances = { 100, 150, 200, 250 };
                foreach (int distance in greenDistances)
                {
                    var green = PrefabUtility.InstantiatePrefab(targetGreenPrefab) as GameObject;
                    float zPos = distance * Visualization.EnvironmentManager.YardsToMeters;
                    green.transform.position = new Vector3(0f, 0.01f, zPos);
                }
                Debug.Log("SceneGenerator: Added target greens to scene");
            }
            else
            {
                Debug.LogWarning("SceneGenerator: TargetGreen.prefab not found. Run 'OpenRange > Create All Environment Prefabs' first.");
            }

            // Event System
            CreateEventSystem();

            // UI Canvas (placeholder)
            var canvasGo = CreateUICanvas("UICanvas");

            // Placeholder text for the range
            var placeholderGo = CreateTextElement(canvasGo.transform, "PlaceholderText", "Marina Driving Range");
            var placeholderRect = placeholderGo.GetComponent<RectTransform>();
            placeholderRect.anchorMin = new Vector2(0.5f, 0.9f);
            placeholderRect.anchorMax = new Vector2(0.5f, 0.9f);
            placeholderRect.anchoredPosition = Vector2.zero;
            placeholderRect.sizeDelta = new Vector2(400, 50);
            var placeholderText = placeholderGo.GetComponent<TextMeshProUGUI>();
            placeholderText.fontSize = 32;
            placeholderText.alignment = TextAlignmentOptions.Center;

            // Back to Menu button
            var backBtn = CreateButton(canvasGo.transform, "BackButton", "< Back");
            var backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0, 1);
            backRect.anchorMax = new Vector2(0, 1);
            backRect.pivot = new Vector2(0, 1);
            backRect.anchoredPosition = new Vector2(20, -20);
            backRect.sizeDelta = new Vector2(100, 40);

            // Shot Data Bar (bottom panel)
            ShotDataBar shotDataBar = null;
            var shotDataBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/ShotDataBar.prefab");
            if (shotDataBarPrefab != null)
            {
                var shotDataBarGo = (GameObject)PrefabUtility.InstantiatePrefab(shotDataBarPrefab);
                shotDataBarGo.name = "ShotDataBar";
                shotDataBarGo.transform.SetParent(canvasGo.transform, false);

                // Position at bottom of screen
                var shotDataBarRect = shotDataBarGo.GetComponent<RectTransform>();
                shotDataBarRect.anchorMin = new Vector2(0, 0);
                shotDataBarRect.anchorMax = new Vector2(1, 0);
                shotDataBarRect.pivot = new Vector2(0.5f, 0);
                shotDataBarRect.anchoredPosition = Vector2.zero;
                shotDataBarRect.sizeDelta = new Vector2(0, 80);

                shotDataBar = shotDataBarGo.GetComponent<ShotDataBar>();
                Debug.Log("SceneGenerator: Added ShotDataBar to scene");
            }
            else
            {
                Debug.LogWarning("SceneGenerator: ShotDataBar.prefab not found. Run 'OpenRange > Create All Shot Data Bar Prefabs' first.");
            }

            // Club Data Panel (left side panel for HMT data)
            ClubDataPanel clubDataPanel = null;
            var clubDataPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/ClubDataPanel.prefab");
            if (clubDataPanelPrefab != null)
            {
                var clubDataPanelGo = (GameObject)PrefabUtility.InstantiatePrefab(clubDataPanelPrefab);
                clubDataPanelGo.name = "ClubDataPanel";
                clubDataPanelGo.transform.SetParent(canvasGo.transform, false);

                // Position at left side of screen
                var clubDataPanelRect = clubDataPanelGo.GetComponent<RectTransform>();
                clubDataPanelRect.anchorMin = new Vector2(0, 0.5f);
                clubDataPanelRect.anchorMax = new Vector2(0, 0.5f);
                clubDataPanelRect.pivot = new Vector2(0, 0.5f);
                clubDataPanelRect.anchoredPosition = new Vector2(20, 0);
                clubDataPanelRect.sizeDelta = new Vector2(200, 400);

                clubDataPanel = clubDataPanelGo.GetComponent<ClubDataPanel>();
                Debug.Log("SceneGenerator: Added ClubDataPanel to scene");
            }
            else
            {
                Debug.LogWarning("SceneGenerator: ClubDataPanel.prefab not found. Run 'OpenRange > Create All Club Data Panel Prefabs' first.");
            }

            // Add MarinaSceneController
            var controllerGo = new GameObject("MarinaSceneController");
            var controller = controllerGo.AddComponent<MarinaSceneController>();

            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("_backButton").objectReferenceValue = backBtn.GetComponent<Button>();
            controllerSo.FindProperty("_shotDataBar").objectReferenceValue = shotDataBar;
            controllerSo.FindProperty("_clubDataPanel").objectReferenceValue = clubDataPanel;
            controllerSo.FindProperty("_ballController").objectReferenceValue = ballController;
            controllerSo.FindProperty("_trajectoryRenderer").objectReferenceValue = trajectoryRenderer;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            SaveScene(scene, $"{RangesPath}/Marina.unity");
            Debug.Log("SceneGenerator: Marina scene created");
        }

        [MenuItem("OpenRange/Update Build Settings", priority = 200)]
        public static void UpdateBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene($"{ScenesPath}/Bootstrap.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/MainMenu.unity", true),
                new EditorBuildSettingsScene($"{RangesPath}/Marina.unity", true)
            };

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("SceneGenerator: Build settings updated with scene order");
        }

        private static void EnsureDirectoriesExist()
        {
            if (!AssetDatabase.IsValidFolder(ScenesPath))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }
            if (!AssetDatabase.IsValidFolder(RangesPath))
            {
                AssetDatabase.CreateFolder(ScenesPath, "Ranges");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            if (!AssetDatabase.IsValidFolder(MaterialsPath))
            {
                AssetDatabase.CreateFolder("Assets/Materials", "Environment");
            }
        }

        private static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.SaveScene(scene, path);
        }

        /// <summary>
        /// Creates and saves a grass material for the ground plane.
        /// </summary>
        private static Material CreateGrassMaterial()
        {
            string materialPath = $"{MaterialsPath}/Grass.mat";

            // Check if material already exists - return it if valid
            var existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existingMaterial != null)
            {
                Debug.Log($"SceneGenerator: Using existing grass material with shader: {existingMaterial.shader?.name}");
                return existingMaterial;
            }

            // Find URP Lit shader (same pattern as GolfBallPrefabGenerator which works)
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null)
            {
                Debug.LogWarning("URP Lit shader not found, falling back to Standard shader");
                urpLitShader = Shader.Find("Standard");
            }

            if (urpLitShader == null)
            {
                Debug.LogError("SceneGenerator: No valid shader found!");
                return null;
            }

            Debug.Log($"SceneGenerator: Using shader '{urpLitShader.name}' for grass material");

            var material = new Material(urpLitShader);
            material.name = "Grass";

            // Set grass-like green color
            material.SetColor("_BaseColor", new Color(0.2f, 0.5f, 0.15f, 1f));
            material.SetFloat("_Smoothness", 0.1f);
            material.SetFloat("_Metallic", 0f);

            // Save the material as an asset (same pattern as GolfBallPrefabGenerator)
            AssetDatabase.CreateAsset(material, materialPath);
            Debug.Log($"SceneGenerator: Created grass material at {materialPath}");

            return material;
        }

        private static void CreateEventSystem()
        {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        private static GameObject CreateUICanvas(string name)
        {
            var canvasGo = new GameObject(name);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            return canvasGo;
        }

        private static GameObject CreateTextElement(Transform parent, string name, string text)
        {
            var textGo = new GameObject(name);
            textGo.transform.SetParent(parent);

            var rectTransform = textGo.AddComponent<RectTransform>();
            rectTransform.localScale = Vector3.one;

            var tmpText = textGo.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.color = Color.white;
            tmpText.fontSize = 24;

            return textGo;
        }

        private static GameObject CreateButton(Transform parent, string name, string text)
        {
            var buttonGo = new GameObject(name);
            buttonGo.transform.SetParent(parent);

            var rectTransform = buttonGo.AddComponent<RectTransform>();
            rectTransform.localScale = Vector3.one;

            var image = buttonGo.AddComponent<Image>();
            image.color = new Color(0.2f, 0.4f, 0.2f);

            var button = buttonGo.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.4f, 0.2f);
            colors.highlightedColor = new Color(0.3f, 0.5f, 0.3f);
            colors.pressedColor = new Color(0.15f, 0.3f, 0.15f);
            button.colors = colors;

            var textGo = CreateTextElement(buttonGo.transform, "Text", text);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var tmpText = textGo.GetComponent<TextMeshProUGUI>();
            tmpText.alignment = TextAlignmentOptions.Center;

            return buttonGo;
        }
    }
}
