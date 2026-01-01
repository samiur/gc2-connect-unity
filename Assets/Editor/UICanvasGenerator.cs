// ABOUTME: Editor tool for creating UI prefabs (UICanvas, Toast).
// ABOUTME: Creates properly configured canvas with responsive layout and safe area handling.

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using OpenRange.UI;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor tool for creating UI prefabs.
    /// </summary>
    public static class UICanvasGenerator
    {
        private const string PrefabPath = "Assets/Prefabs/UI";
        private const string MaterialPath = "Assets/Materials/UI";

        [MenuItem("OpenRange/Create UI Canvas Prefab")]
        public static void CreateUICanvasPrefab()
        {
            EnsureDirectories();

            // Create canvas
            var canvasGO = CreateCanvas();

            // Save as prefab
            string path = $"{PrefabPath}/UICanvas.prefab";
            PrefabUtility.SaveAsPrefabAsset(canvasGO, path);
            Object.DestroyImmediate(canvasGO);

            AssetDatabase.Refresh();
            Debug.Log($"UICanvasGenerator: Created UICanvas.prefab at {path}");
        }

        [MenuItem("OpenRange/Create Toast Prefab")]
        public static void CreateToastPrefab()
        {
            EnsureDirectories();

            // Create toast
            var toastGO = CreateToast();

            // Save as prefab
            string path = $"{PrefabPath}/Toast.prefab";
            PrefabUtility.SaveAsPrefabAsset(toastGO, path);
            Object.DestroyImmediate(toastGO);

            AssetDatabase.Refresh();
            Debug.Log($"UICanvasGenerator: Created Toast.prefab at {path}");
        }

        [MenuItem("OpenRange/Create All UI Prefabs")]
        public static void CreateAllUIPrefabs()
        {
            CreateUICanvasPrefab();
            CreateToastPrefab();
            Debug.Log("UICanvasGenerator: Created all UI prefabs");
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            if (!AssetDatabase.IsValidFolder(PrefabPath))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            if (!AssetDatabase.IsValidFolder(MaterialPath))
            {
                AssetDatabase.CreateFolder("Assets/Materials", "UI");
            }
        }

        private static GameObject CreateCanvas()
        {
            // Create Canvas
            var canvasGO = new GameObject("UICanvas");

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = UITheme.ReferenceResolution;
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // Add ResponsiveLayout
            canvasGO.AddComponent<ResponsiveLayout>();

            // Create SafeArea container
            var safeAreaGO = new GameObject("SafeArea");
            safeAreaGO.transform.SetParent(canvasGO.transform, false);

            var safeAreaRect = safeAreaGO.AddComponent<RectTransform>();
            safeAreaRect.anchorMin = Vector2.zero;
            safeAreaRect.anchorMax = Vector2.one;
            safeAreaRect.offsetMin = Vector2.zero;
            safeAreaRect.offsetMax = Vector2.zero;

            safeAreaGO.AddComponent<SafeAreaHandler>();

            // Create panel containers inside safe area
            CreatePanelContainer(safeAreaGO, "TopPanel", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, -100), new Vector2(0, 0));

            CreatePanelContainer(safeAreaGO, "BottomPanel", new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0, 0), new Vector2(0, 120));

            CreatePanelContainer(safeAreaGO, "RightPanel", new Vector2(1, 0), new Vector2(1, 1),
                new Vector2(-300, 0), new Vector2(0, 0));

            CreatePanelContainer(safeAreaGO, "LeftPanel", new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(0, 0), new Vector2(300, 0));

            CreatePanelContainer(safeAreaGO, "CenterPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-200, -150), new Vector2(200, 150));

            // Create overlay container for modals
            var overlaysGO = new GameObject("Overlays");
            overlaysGO.transform.SetParent(safeAreaGO.transform, false);
            var overlaysRect = overlaysGO.AddComponent<RectTransform>();
            overlaysRect.anchorMin = Vector2.zero;
            overlaysRect.anchorMax = Vector2.one;
            overlaysRect.offsetMin = Vector2.zero;
            overlaysRect.offsetMax = Vector2.zero;

            // Create toast container (outside safe area at top)
            var toastContainerGO = new GameObject("ToastContainer");
            toastContainerGO.transform.SetParent(canvasGO.transform, false);
            var toastRect = toastContainerGO.AddComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.5f, 1f);
            toastRect.anchorMax = new Vector2(0.5f, 1f);
            toastRect.pivot = new Vector2(0.5f, 1f);
            toastRect.anchoredPosition = new Vector2(0, -20);
            toastRect.sizeDelta = new Vector2(400, 200);

            var toastLayout = toastContainerGO.AddComponent<VerticalLayoutGroup>();
            toastLayout.spacing = 8;
            toastLayout.childAlignment = TextAnchor.UpperCenter;
            toastLayout.childControlWidth = true;
            toastLayout.childControlHeight = false;
            toastLayout.childForceExpandWidth = false;
            toastLayout.childForceExpandHeight = false;

            // Add UIManager
            var uiManager = canvasGO.AddComponent<UIManager>();

            // Wire up references via SerializedObject
            var so = new SerializedObject(uiManager);
            so.FindProperty("_panelContainer").objectReferenceValue = safeAreaGO.transform;
            so.FindProperty("_toastContainer").objectReferenceValue = toastContainerGO.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            return canvasGO;
        }

        private static void CreatePanelContainer(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var panelGO = new GameObject(name);
            panelGO.transform.SetParent(parent.transform, false);

            var rect = panelGO.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            // Add CanvasGroup for fade animations
            panelGO.AddComponent<CanvasGroup>();

            // Start hidden
            panelGO.SetActive(false);
        }

        private static GameObject CreateToast()
        {
            var toastGO = new GameObject("Toast");

            var rectTransform = toastGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(400, 60);

            // Background image
            var bgImage = toastGO.AddComponent<Image>();
            bgImage.color = UITheme.ToastInfo;

            // Add rounded corners if possible (requires sprite)
            // For now, use a simple rectangle

            // Add layout element
            var layoutElement = toastGO.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 400;
            layoutElement.preferredHeight = 60;
            layoutElement.flexibleWidth = 0;

            // Create icon placeholder (left side)
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(toastGO.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.5f);
            iconRect.anchorMax = new Vector2(0, 0.5f);
            iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.anchoredPosition = new Vector2(16, 0);
            iconRect.sizeDelta = new Vector2(24, 24);

            var iconImage = iconGO.AddComponent<Image>();
            iconImage.color = UITheme.TextPrimary;

            // Create text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(toastGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(52, 8);
            textRect.offsetMax = new Vector2(-16, -8);

            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "Toast message";
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = UITheme.TextPrimary;
            text.fontSize = 16;
            text.fontStyle = FontStyles.Normal;

            // Add CanvasGroup for fade
            var canvasGroup = toastGO.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // Add Toast component
            toastGO.AddComponent<Toast>();

            return toastGO;
        }
    }
}
