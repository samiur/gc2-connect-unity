// ABOUTME: Editor tool for creating Connection Status UI prefabs.
// ABOUTME: Creates ConnectionStatus indicator, ConnectionPanel modal, and ConnectionToast prefabs.

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using OpenRange.UI;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor tool for creating Connection Status UI prefabs.
    /// </summary>
    public static class ConnectionStatusGenerator
    {
        private const string PrefabPath = "Assets/Prefabs/UI";

        #region Layout Constants

        // Panel sizing
        public const float PanelWidth = 400f;
        public const float PanelMinHeight = 420f; // Minimum height to fit all content
        public const float PanelMaxHeight = 600f; // Maximum height before scrolling

        // Close button (minimum 32x32 for touch targets)
        public const float CloseButtonSize = 36f;

        // Row heights
        public const float HeaderHeight = 50f;
        public const float StatusRowHeight = 36f;
        public const float InfoRowHeight = 56f;
        public const float ButtonRowHeight = 50f;
        public const float SpacerHeight = 16f;

        // Button sizing
        public const float ActionButtonMinWidth = 100f;
        public const float ActionButtonHeight = 44f;

        // Modal overlay
        public const float OverlayAlpha = 0.6f;

        #endregion

        #region Menu Items

        [MenuItem("OpenRange/Create Connection Status Prefab")]
        public static void CreateConnectionStatusPrefab()
        {
            EnsureDirectories();

            var statusGO = CreateConnectionStatus();

            string path = $"{PrefabPath}/ConnectionStatus.prefab";
            PrefabUtility.SaveAsPrefabAsset(statusGO, path);
            Object.DestroyImmediate(statusGO);

            AssetDatabase.Refresh();
            Debug.Log($"ConnectionStatusGenerator: Created ConnectionStatus.prefab at {path}");
        }

        [MenuItem("OpenRange/Create Connection Panel Prefab")]
        public static void CreateConnectionPanelPrefab()
        {
            EnsureDirectories();

            var panelGO = CreateConnectionPanel();

            string path = $"{PrefabPath}/ConnectionPanel.prefab";
            PrefabUtility.SaveAsPrefabAsset(panelGO, path);
            Object.DestroyImmediate(panelGO);

            AssetDatabase.Refresh();
            Debug.Log($"ConnectionStatusGenerator: Created ConnectionPanel.prefab at {path}");
        }

        [MenuItem("OpenRange/Create All Connection Status Prefabs")]
        public static void CreateAllPrefabs()
        {
            CreateConnectionStatusPrefab();
            CreateConnectionPanelPrefab();
            Debug.Log("ConnectionStatusGenerator: Created all connection status prefabs");
        }

        #endregion

        #region Private Methods

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
        }

        private static GameObject CreateConnectionStatus()
        {
            var statusGO = new GameObject("ConnectionStatus");

            // RectTransform - top-right corner
            var rectTransform = statusGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(1, 1);
            rectTransform.anchoredPosition = new Vector2(-UITheme.Padding.Normal, -UITheme.Padding.Normal);
            rectTransform.sizeDelta = new Vector2(180, 40);

            // Background
            var bgImage = statusGO.AddComponent<Image>();
            bgImage.color = UITheme.PanelBackground;

            // CanvasGroup for animations
            var canvasGroup = statusGO.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;

            // Button for click handling
            var button = statusGO.AddComponent<Button>();
            button.targetGraphic = bgImage;
            button.transition = Selectable.Transition.ColorTint;

            // Horizontal layout
            var layoutGroup = statusGO.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = UITheme.Padding.Small;
            layoutGroup.padding = new RectOffset(
                (int)UITheme.Padding.Small,
                (int)UITheme.Padding.Small,
                (int)UITheme.Padding.Tiny,
                (int)UITheme.Padding.Tiny
            );
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = true;

            // Create status dot
            var dotGO = new GameObject("StatusDot");
            dotGO.transform.SetParent(statusGO.transform, false);
            var dotRect = dotGO.AddComponent<RectTransform>();
            dotRect.sizeDelta = new Vector2(12, 12);
            var dotLayout = dotGO.AddComponent<LayoutElement>();
            dotLayout.preferredWidth = 12;
            dotLayout.minWidth = 12;

            var dotImage = dotGO.AddComponent<Image>();
            dotImage.color = UITheme.StatusDisconnected;
            // Make it circular by using a circular sprite or just keeping it square for now

            // Create status text
            var textGO = new GameObject("StatusText");
            textGO.transform.SetParent(statusGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            var textLayout = textGO.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 1;
            textLayout.minWidth = 100;

            var statusText = textGO.AddComponent<TextMeshProUGUI>();
            statusText.text = "Disconnected";
            statusText.alignment = TextAlignmentOptions.MidlineLeft;
            statusText.color = UITheme.TextPrimary;
            statusText.fontSize = UITheme.FontSizeRegular.Normal;
            statusText.fontStyle = FontStyles.Normal;

            // Add ConnectionStatusUI component and wire up references
            var connectionStatus = statusGO.AddComponent<ConnectionStatusUI>();

            var so = new SerializedObject(connectionStatus);
            so.FindProperty("_statusDotImage").objectReferenceValue = dotImage;
            so.FindProperty("_statusText").objectReferenceValue = statusText;
            so.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("_clickButton").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();

            return statusGO;
        }

        private static GameObject CreateConnectionPanel()
        {
            // Create root container that includes modal overlay
            var rootGO = new GameObject("ConnectionPanel");

            // Root RectTransform - full screen for overlay
            var rootRect = rootGO.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // CanvasGroup for animations (on root so overlay fades with panel)
            var canvasGroup = rootGO.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // Modal overlay (semi-transparent background that blocks interaction)
            var overlayGO = new GameObject("ModalOverlay");
            overlayGO.transform.SetParent(rootGO.transform, false);
            var overlayRect = overlayGO.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            var overlayImage = overlayGO.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, OverlayAlpha);

            // Make overlay block raycasts
            overlayImage.raycastTarget = true;

            // Panel container - centered modal
            var panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(rootGO.transform, false);

            var rectTransform = panelGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(PanelWidth, PanelMinHeight);

            // Panel background
            var bgImage = panelGO.AddComponent<Image>();
            bgImage.color = new Color(
                UITheme.PanelBackground.r,
                UITheme.PanelBackground.g,
                UITheme.PanelBackground.b,
                0.98f
            );

            // Vertical layout for panel content
            var layoutGroup = panelGO.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = UITheme.Padding.Normal;
            layoutGroup.padding = new RectOffset(
                (int)UITheme.Padding.Large,
                (int)UITheme.Padding.Large,
                (int)UITheme.Padding.Large,
                (int)UITheme.Padding.Large
            );
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            // Create header with close button
            var headerGO = CreateHeader(panelGO.transform);
            var closeButton = headerGO.GetComponentInChildren<Button>();

            // Create status row (dot + text)
            var statusRowGO = CreateStatusRow(panelGO.transform);
            var statusDot = statusRowGO.transform.Find("StatusDot").GetComponent<Image>();
            var statusText = statusRowGO.transform.Find("StatusText").GetComponent<TextMeshProUGUI>();

            // Create device info section
            var deviceInfoText = CreateInfoRow(panelGO.transform, "Device Info:", "Serial: N/A\nFirmware: N/A");

            // Create connection mode row
            var modeText = CreateInfoRow(panelGO.transform, "Connection Mode:", "USB");

            // Create last shot row
            var lastShotText = CreateInfoRow(panelGO.transform, "Last Shot:", "No shots yet");

            // Create spacer to push buttons to bottom
            var spacerGO = new GameObject("Spacer");
            spacerGO.transform.SetParent(panelGO.transform, false);
            spacerGO.AddComponent<RectTransform>();
            var spacerLayout = spacerGO.AddComponent<LayoutElement>();
            spacerLayout.flexibleHeight = 1f;
            spacerLayout.minHeight = SpacerHeight;

            // Create buttons row
            var (connectButton, disconnectButton, retryButton) = CreateButtonsRow(panelGO.transform);

            // Add ConnectionPanel component to root and wire up references
            var connectionPanel = rootGO.AddComponent<ConnectionPanel>();

            var so = new SerializedObject(connectionPanel);
            so.FindProperty("_statusDotImage").objectReferenceValue = statusDot;
            so.FindProperty("_statusText").objectReferenceValue = statusText;
            so.FindProperty("_deviceInfoText").objectReferenceValue = deviceInfoText;
            so.FindProperty("_modeText").objectReferenceValue = modeText;
            so.FindProperty("_lastShotText").objectReferenceValue = lastShotText;
            so.FindProperty("_connectButton").objectReferenceValue = connectButton;
            so.FindProperty("_disconnectButton").objectReferenceValue = disconnectButton;
            so.FindProperty("_retryButton").objectReferenceValue = retryButton;
            so.FindProperty("_closeButton").objectReferenceValue = closeButton;
            so.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
            so.ApplyModifiedPropertiesWithoutUndo();

            return rootGO;
        }

        private static GameObject CreateHeader(Transform parent)
        {
            var headerGO = new GameObject("Header");
            headerGO.transform.SetParent(parent, false);

            var headerRect = headerGO.AddComponent<RectTransform>();
            var headerLayout = headerGO.AddComponent<LayoutElement>();
            headerLayout.preferredHeight = HeaderHeight;
            headerLayout.minHeight = HeaderHeight;

            // Horizontal layout for title + close button
            var headerLayoutGroup = headerGO.AddComponent<HorizontalLayoutGroup>();
            headerLayoutGroup.spacing = UITheme.Padding.Small;
            headerLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            headerLayoutGroup.childControlWidth = true;
            headerLayoutGroup.childControlHeight = true;
            headerLayoutGroup.childForceExpandWidth = false;
            headerLayoutGroup.childForceExpandHeight = false;

            // Title text
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(headerGO.transform, false);
            var titleLayout = titleGO.AddComponent<LayoutElement>();
            titleLayout.flexibleWidth = 1;
            titleLayout.preferredHeight = HeaderHeight;

            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "CONNECTION STATUS";
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            titleText.color = UITheme.TextPrimary;
            titleText.fontSize = UITheme.FontSizeRegular.Header;
            titleText.fontStyle = FontStyles.Bold;

            // Close button - minimum 32x32 for accessibility
            var closeGO = new GameObject("CloseButton");
            closeGO.transform.SetParent(headerGO.transform, false);
            var closeRect = closeGO.AddComponent<RectTransform>();
            closeRect.sizeDelta = new Vector2(CloseButtonSize, CloseButtonSize);
            var closeLayout = closeGO.AddComponent<LayoutElement>();
            closeLayout.preferredWidth = CloseButtonSize;
            closeLayout.preferredHeight = CloseButtonSize;
            closeLayout.minWidth = CloseButtonSize;
            closeLayout.minHeight = CloseButtonSize;
            closeLayout.flexibleWidth = 0;
            closeLayout.flexibleHeight = 0;

            var closeImage = closeGO.AddComponent<Image>();
            closeImage.color = new Color(0.4f, 0.4f, 0.4f, 1f); // Darker background for visibility

            var closeButton = closeGO.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            var colors = closeButton.colors;
            colors.highlightedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            colors.pressedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            closeButton.colors = colors;

            // X text inside close button - larger for visibility
            var xGO = new GameObject("X");
            xGO.transform.SetParent(closeGO.transform, false);
            var xRect = xGO.AddComponent<RectTransform>();
            xRect.anchorMin = Vector2.zero;
            xRect.anchorMax = Vector2.one;
            xRect.offsetMin = Vector2.zero;
            xRect.offsetMax = Vector2.zero;

            var xText = xGO.AddComponent<TextMeshProUGUI>();
            xText.text = "✕";
            xText.alignment = TextAlignmentOptions.Center;
            xText.color = UITheme.TextPrimary;
            xText.fontSize = 22; // Larger for visibility

            return headerGO;
        }

        private static GameObject CreateStatusRow(Transform parent)
        {
            var rowGO = new GameObject("StatusRow");
            rowGO.transform.SetParent(parent, false);

            var rowLayout = rowGO.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = StatusRowHeight;
            rowLayout.minHeight = StatusRowHeight;

            var rowLayoutGroup = rowGO.AddComponent<HorizontalLayoutGroup>();
            rowLayoutGroup.spacing = UITheme.Padding.Normal;
            rowLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
            rowLayoutGroup.childControlWidth = false;
            rowLayoutGroup.childControlHeight = true;
            rowLayoutGroup.childForceExpandWidth = false;
            rowLayoutGroup.childForceExpandHeight = false;

            // Status dot - larger for visibility
            var dotGO = new GameObject("StatusDot");
            dotGO.transform.SetParent(rowGO.transform, false);
            var dotRect = dotGO.AddComponent<RectTransform>();
            dotRect.sizeDelta = new Vector2(18, 18);
            var dotLayout = dotGO.AddComponent<LayoutElement>();
            dotLayout.preferredWidth = 18;
            dotLayout.preferredHeight = 18;
            dotLayout.minWidth = 18;
            dotLayout.minHeight = 18;

            var dotImage = dotGO.AddComponent<Image>();
            dotImage.color = UITheme.StatusDisconnected;

            // Status text
            var textGO = new GameObject("StatusText");
            textGO.transform.SetParent(rowGO.transform, false);
            var textLayout = textGO.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 1;
            textLayout.preferredHeight = StatusRowHeight;

            var statusText = textGO.AddComponent<TextMeshProUGUI>();
            statusText.text = "Disconnected";
            statusText.alignment = TextAlignmentOptions.MidlineLeft;
            statusText.color = UITheme.TextPrimary;
            statusText.fontSize = UITheme.FontSizeRegular.Large;
            statusText.fontStyle = FontStyles.Bold;

            return rowGO;
        }

        private static TextMeshProUGUI CreateInfoRow(Transform parent, string label, string value)
        {
            var rowGO = new GameObject(label.Replace(":", "").Replace(" ", ""));
            rowGO.transform.SetParent(parent, false);

            var rowLayout = rowGO.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = InfoRowHeight;
            rowLayout.minHeight = 44f;

            var rowLayoutGroup = rowGO.AddComponent<VerticalLayoutGroup>();
            rowLayoutGroup.spacing = 4f;
            rowLayoutGroup.childAlignment = TextAnchor.UpperLeft;
            rowLayoutGroup.childControlWidth = true;
            rowLayoutGroup.childControlHeight = true;
            rowLayoutGroup.childForceExpandWidth = true;
            rowLayoutGroup.childForceExpandHeight = false;

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(rowGO.transform, false);
            var labelLayout = labelGO.AddComponent<LayoutElement>();
            labelLayout.preferredHeight = 18f;
            labelLayout.minHeight = 18f;

            var labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.alignment = TextAlignmentOptions.TopLeft;
            labelText.color = UITheme.TextSecondary;
            labelText.fontSize = UITheme.FontSizeRegular.Small;

            // Value
            var valueGO = new GameObject("Value");
            valueGO.transform.SetParent(rowGO.transform, false);
            var valueLayout = valueGO.AddComponent<LayoutElement>();
            valueLayout.preferredHeight = 28f;
            valueLayout.minHeight = 22f;
            valueLayout.flexibleHeight = 1;

            var valueText = valueGO.AddComponent<TextMeshProUGUI>();
            valueText.text = value;
            valueText.alignment = TextAlignmentOptions.TopLeft;
            valueText.color = UITheme.TextPrimary;
            valueText.fontSize = UITheme.FontSizeRegular.Normal;

            return valueText;
        }

        private static (Button connect, Button disconnect, Button retry) CreateButtonsRow(Transform parent)
        {
            var rowGO = new GameObject("ButtonsRow");
            rowGO.transform.SetParent(parent, false);

            var rowLayout = rowGO.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = ButtonRowHeight;
            rowLayout.minHeight = ButtonRowHeight;

            var rowLayoutGroup = rowGO.AddComponent<HorizontalLayoutGroup>();
            rowLayoutGroup.spacing = UITheme.Padding.Normal;
            rowLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            rowLayoutGroup.childControlWidth = true;
            rowLayoutGroup.childControlHeight = true;
            rowLayoutGroup.childForceExpandWidth = true;
            rowLayoutGroup.childForceExpandHeight = false;

            // Connect button
            var connectButton = CreateButton(rowGO.transform, "Connect", UITheme.AccentGreen);

            // Disconnect button
            var disconnectButton = CreateButton(rowGO.transform, "Disconnect", UITheme.StatusDisconnected);
            disconnectButton.gameObject.SetActive(false);

            // Retry button
            var retryButton = CreateButton(rowGO.transform, "Retry", UITheme.ToastWarning);
            retryButton.gameObject.SetActive(false);

            return (connectButton, disconnectButton, retryButton);
        }

        private static Button CreateButton(Transform parent, string text, Color color)
        {
            var buttonGO = new GameObject(text + "Button");
            buttonGO.transform.SetParent(parent, false);

            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(ActionButtonMinWidth, ActionButtonHeight);

            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = color;

            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.transition = Selectable.Transition.ColorTint;

            var buttonLayout = buttonGO.AddComponent<LayoutElement>();
            buttonLayout.minWidth = ActionButtonMinWidth;
            buttonLayout.minHeight = ActionButtonHeight;
            buttonLayout.preferredHeight = ActionButtonHeight;
            buttonLayout.flexibleWidth = 1;

            // Button text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var buttonText = textGO.AddComponent<TextMeshProUGUI>();
            buttonText.text = text;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = UITheme.TextPrimary;
            buttonText.fontSize = UITheme.FontSizeRegular.Normal;
            buttonText.fontStyle = FontStyles.Bold;

            return button;
        }

        #endregion
    }
}
