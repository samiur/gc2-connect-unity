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

            // Create persistent managers container
            var managersGo = new GameObject("Managers");

            // Add GameManager with child services
            var gameManagerGo = new GameObject("GameManager");
            gameManagerGo.transform.SetParent(managersGo.transform);
            var gameManager = gameManagerGo.AddComponent<Core.GameManager>();

            // Add ShotProcessor as child
            var shotProcessorGo = new GameObject("ShotProcessor");
            shotProcessorGo.transform.SetParent(gameManagerGo.transform);
            var shotProcessor = shotProcessorGo.AddComponent<Core.ShotProcessor>();

            // Add SessionManager as child
            var sessionManagerGo = new GameObject("SessionManager");
            sessionManagerGo.transform.SetParent(gameManagerGo.transform);
            var sessionManager = sessionManagerGo.AddComponent<Core.SessionManager>();

            // Wire up references using SerializedObject
            var gameManagerSo = new SerializedObject(gameManager);
            gameManagerSo.FindProperty("_shotProcessor").objectReferenceValue = shotProcessor;
            gameManagerSo.FindProperty("_sessionManager").objectReferenceValue = sessionManager;
            gameManagerSo.ApplyModifiedPropertiesWithoutUndo();

            // Add SettingsManager
            var settingsManagerGo = new GameObject("SettingsManager");
            settingsManagerGo.transform.SetParent(managersGo.transform);
            settingsManagerGo.AddComponent<Core.SettingsManager>();

            // Wire SettingsManager reference
            var settingsManager = settingsManagerGo.GetComponent<Core.SettingsManager>();
            gameManagerSo.FindProperty("_settingsManager").objectReferenceValue = settingsManager;
            gameManagerSo.ApplyModifiedPropertiesWithoutUndo();

            // Add MainThreadDispatcher
            var dispatcherGo = new GameObject("MainThreadDispatcher");
            dispatcherGo.transform.SetParent(managersGo.transform);
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

            // Main Camera
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0, 5, -10);
            cameraGo.transform.rotation = Quaternion.Euler(15, 0, 0);
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraGo.AddComponent<AudioListener>();

            // Directional Light (Sun)
            var lightGo = new GameObject("Directional Light");
            lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.84f);
            light.intensity = 1.0f;
            light.shadows = LightShadows.Soft;

            // Ground Plane (temporary)
            var groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundGo.name = "Ground";
            groundGo.transform.position = Vector3.zero;
            groundGo.transform.localScale = new Vector3(50, 1, 50);
            var groundRenderer = groundGo.GetComponent<MeshRenderer>();
            // Set a default green color for grass
            groundRenderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            groundRenderer.sharedMaterial.color = new Color(0.2f, 0.5f, 0.2f);

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

            // Add MarinaSceneController
            var controllerGo = new GameObject("MarinaSceneController");
            var controller = controllerGo.AddComponent<MarinaSceneController>();

            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("_backButton").objectReferenceValue = backBtn.GetComponent<Button>();
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
        }

        private static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.SaveScene(scene, path);
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
