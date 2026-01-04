// ABOUTME: Editor tool for creating Bridge Mode UI prefabs.
// ABOUTME: Creates BridgeModeOverlay (floating status) and BridgeModeToggle (settings panel) prefabs.

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using OpenRange.UI;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor tool for creating Bridge Mode UI prefabs.
    /// </summary>
    public static class BridgeModeUIGenerator
    {
        private const string PrefabPath = "Assets/Prefabs/UI";

        #region Menu Items

        [MenuItem("OpenRange/Create Bridge Mode Overlay Prefab")]
        public static void CreateBridgeModeOverlayPrefab()
        {
            EnsureDirectories();

            var overlayGO = CreateBridgeModeOverlay();

            string path = $"{PrefabPath}/BridgeModeOverlay.prefab";
            PrefabUtility.SaveAsPrefabAsset(overlayGO, path);
            Object.DestroyImmediate(overlayGO);

            AssetDatabase.Refresh();
            Debug.Log($"BridgeModeUIGenerator: Created BridgeModeOverlay.prefab at {path}");
        }

        [MenuItem("OpenRange/Create Bridge Mode Toggle Prefab")]
        public static void CreateBridgeModeTogglePrefab()
        {
            EnsureDirectories();

            var toggleGO = CreateBridgeModeToggle();

            string path = $"{PrefabPath}/BridgeModeToggle.prefab";
            PrefabUtility.SaveAsPrefabAsset(toggleGO, path);
            Object.DestroyImmediate(toggleGO);

            AssetDatabase.Refresh();
            Debug.Log($"BridgeModeUIGenerator: Created BridgeModeToggle.prefab at {path}");
        }

        [MenuItem("OpenRange/Create All Bridge Mode UI Prefabs")]
        public static void CreateAllPrefabs()
        {
            CreateBridgeModeOverlayPrefab();
            CreateBridgeModeTogglePrefab();
            Debug.Log("BridgeModeUIGenerator: Created all Bridge Mode UI prefabs");
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

        private static GameObject CreateBridgeModeOverlay()
        {
            var overlayGO = new GameObject("BridgeModeOverlay");

            // RectTransform - floating, bottom-right corner by default
            var rectTransform = overlayGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(1f, 0f);
            rectTransform.anchoredPosition = new Vector2(-20f, 20f);
            rectTransform.sizeDelta = new Vector2(180f, 70f);

            // Background panel
            var bgImage = overlayGO.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            // CanvasGroup for animations and visibility
            var canvasGroup = overlayGO.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f; // Start hidden

            // Main vertical layout
            var mainLayout = overlayGO.AddComponent<VerticalLayoutGroup>();
            mainLayout.spacing = 4f;
            mainLayout.padding = new RectOffset(10, 10, 8, 8);
            mainLayout.childAlignment = TextAnchor.UpperLeft;
            mainLayout.childControlWidth = true;
            mainLayout.childControlHeight = true;
            mainLayout.childForceExpandWidth = true;
            mainLayout.childForceExpandHeight = false;

            // --- Compact Header Row ---
            var headerRowGO = new GameObject("HeaderRow");
            headerRowGO.transform.SetParent(overlayGO.transform, false);
            var headerRect = headerRowGO.AddComponent<RectTransform>();
            var headerLayout = headerRowGO.AddComponent<LayoutElement>();
            headerLayout.preferredHeight = 24f;

            var headerHLayout = headerRowGO.AddComponent<HorizontalLayoutGroup>();
            headerHLayout.spacing = 6f;
            headerHLayout.childAlignment = TextAnchor.MiddleLeft;
            headerHLayout.childControlWidth = false;
            headerHLayout.childControlHeight = true;
            headerHLayout.childForceExpandWidth = false;
            headerHLayout.childForceExpandHeight = true;

            // Status icon (LED indicator)
            var iconGO = new GameObject("StatusIcon");
            iconGO.transform.SetParent(headerRowGO.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(12f, 12f);
            var iconLayoutElem = iconGO.AddComponent<LayoutElement>();
            iconLayoutElem.preferredWidth = 12f;
            iconLayoutElem.preferredHeight = 12f;
            iconLayoutElem.minWidth = 12f;
            iconLayoutElem.minHeight = 12f;

            var statusIcon = iconGO.AddComponent<Image>();
            statusIcon.color = BridgeModeOverlay.DisconnectedColor;

            // Status text
            var statusTextGO = new GameObject("StatusText");
            statusTextGO.transform.SetParent(headerRowGO.transform, false);
            var statusTextLayout = statusTextGO.AddComponent<LayoutElement>();
            statusTextLayout.flexibleWidth = 1f;
            statusTextLayout.minWidth = 80f;

            var statusText = statusTextGO.AddComponent<TextMeshProUGUI>();
            statusText.text = "Disconnected";
            statusText.alignment = TextAlignmentOptions.MidlineLeft;
            statusText.color = UITheme.TextPrimary;
            statusText.fontSize = 12f;
            statusText.raycastTarget = false;

            // --- Shots Relayed Row ---
            var shotsRowGO = new GameObject("ShotsRow");
            shotsRowGO.transform.SetParent(overlayGO.transform, false);
            var shotsLayout = shotsRowGO.AddComponent<LayoutElement>();
            shotsLayout.preferredHeight = 20f;

            var shotsRelayedText = shotsRowGO.AddComponent<TextMeshProUGUI>();
            shotsRelayedText.text = "0 shots";
            shotsRelayedText.alignment = TextAlignmentOptions.MidlineLeft;
            shotsRelayedText.color = UITheme.TextSecondary;
            shotsRelayedText.fontSize = 11f;
            shotsRelayedText.raycastTarget = false;

            // --- Expanded Panel (hidden by default) ---
            var expandedPanelGO = new GameObject("ExpandedPanel");
            expandedPanelGO.transform.SetParent(overlayGO.transform, false);
            var expandedRect = expandedPanelGO.AddComponent<RectTransform>();
            var expandedLayout = expandedPanelGO.AddComponent<LayoutElement>();
            expandedLayout.preferredHeight = 40f;
            var expandedCanvasGroup = expandedPanelGO.AddComponent<CanvasGroup>();
            expandedPanelGO.SetActive(false);

            var expandedVLayout = expandedPanelGO.AddComponent<VerticalLayoutGroup>();
            expandedVLayout.spacing = 4f;
            expandedVLayout.childAlignment = TextAnchor.UpperLeft;
            expandedVLayout.childControlWidth = true;
            expandedVLayout.childControlHeight = true;
            expandedVLayout.childForceExpandWidth = true;
            expandedVLayout.childForceExpandHeight = false;

            // Duration text
            var durationGO = new GameObject("DurationText");
            durationGO.transform.SetParent(expandedPanelGO.transform, false);
            var durationLayout = durationGO.AddComponent<LayoutElement>();
            durationLayout.preferredHeight = 16f;

            var durationText = durationGO.AddComponent<TextMeshProUGUI>();
            durationText.text = "0m 00s";
            durationText.alignment = TextAlignmentOptions.MidlineLeft;
            durationText.color = UITheme.TextSecondary;
            durationText.fontSize = 10f;
            durationText.raycastTarget = false;

            // Disable button
            var disableButtonGO = new GameObject("DisableButton");
            disableButtonGO.transform.SetParent(expandedPanelGO.transform, false);
            var disableButtonRect = disableButtonGO.AddComponent<RectTransform>();
            disableButtonRect.sizeDelta = new Vector2(60f, 20f);
            var disableButtonLayout = disableButtonGO.AddComponent<LayoutElement>();
            disableButtonLayout.preferredWidth = 60f;
            disableButtonLayout.preferredHeight = 20f;

            var disableButtonImage = disableButtonGO.AddComponent<Image>();
            disableButtonImage.color = new Color(0.5f, 0.2f, 0.2f, 1f);

            var disableButton = disableButtonGO.AddComponent<Button>();
            disableButton.targetGraphic = disableButtonImage;

            var disableButtonTextGO = new GameObject("Text");
            disableButtonTextGO.transform.SetParent(disableButtonGO.transform, false);
            var disableButtonTextRect = disableButtonTextGO.AddComponent<RectTransform>();
            disableButtonTextRect.anchorMin = Vector2.zero;
            disableButtonTextRect.anchorMax = Vector2.one;
            disableButtonTextRect.sizeDelta = Vector2.zero;

            var disableButtonText = disableButtonTextGO.AddComponent<TextMeshProUGUI>();
            disableButtonText.text = "Disable";
            disableButtonText.alignment = TextAlignmentOptions.Center;
            disableButtonText.color = Color.white;
            disableButtonText.fontSize = 10f;
            disableButtonText.raycastTarget = false;

            // Add BridgeModeOverlay component and wire up references
            var overlay = overlayGO.AddComponent<BridgeModeOverlay>();

            var so = new SerializedObject(overlay);
            so.FindProperty("_statusIcon").objectReferenceValue = statusIcon;
            so.FindProperty("_statusText").objectReferenceValue = statusText;
            so.FindProperty("_shotsRelayedText").objectReferenceValue = shotsRelayedText;
            so.FindProperty("_expandedPanel").objectReferenceValue = expandedPanelGO;
            so.FindProperty("_disableButton").objectReferenceValue = disableButton;
            so.FindProperty("_durationText").objectReferenceValue = durationText;
            so.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
            so.ApplyModifiedPropertiesWithoutUndo();

            return overlayGO;
        }

        private static GameObject CreateBridgeModeToggle()
        {
            var toggleGO = new GameObject("BridgeModeToggle");

            // RectTransform
            var rectTransform = toggleGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(300f, 200f);

            // Main vertical layout
            var mainLayout = toggleGO.AddComponent<VerticalLayoutGroup>();
            mainLayout.spacing = 10f;
            mainLayout.padding = new RectOffset(12, 12, 12, 12);
            mainLayout.childAlignment = TextAnchor.UpperLeft;
            mainLayout.childControlWidth = true;
            mainLayout.childControlHeight = true;
            mainLayout.childForceExpandWidth = true;
            mainLayout.childForceExpandHeight = false;

            // --- Header Row (Title + Toggle) ---
            var headerRowGO = new GameObject("HeaderRow");
            headerRowGO.transform.SetParent(toggleGO.transform, false);
            var headerLayout = headerRowGO.AddComponent<LayoutElement>();
            headerLayout.preferredHeight = 30f;

            var headerHLayout = headerRowGO.AddComponent<HorizontalLayoutGroup>();
            headerHLayout.spacing = 10f;
            headerHLayout.childAlignment = TextAnchor.MiddleLeft;
            headerHLayout.childControlWidth = false;
            headerHLayout.childControlHeight = true;
            headerHLayout.childForceExpandWidth = false;
            headerHLayout.childForceExpandHeight = true;

            // Title text
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(headerRowGO.transform, false);
            var titleLayout = titleGO.AddComponent<LayoutElement>();
            titleLayout.flexibleWidth = 1f;
            titleLayout.minWidth = 150f;

            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "Bridge Mode";
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            titleText.color = UITheme.TextPrimary;
            titleText.fontSize = 16f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.raycastTarget = false;

            // Enable toggle
            var enableToggleGO = CreateToggle("EnableToggle");
            enableToggleGO.transform.SetParent(headerRowGO.transform, false);
            var enableToggle = enableToggleGO.GetComponent<Toggle>();

            // --- Description ---
            var descGO = new GameObject("Description");
            descGO.transform.SetParent(toggleGO.transform, false);
            var descLayout = descGO.AddComponent<LayoutElement>();
            descLayout.preferredHeight = 32f;

            var descText = descGO.AddComponent<TextMeshProUGUI>();
            descText.text = "Relay GC2 shots to GSPro in background.\nFor use with Moonlight streaming.";
            descText.alignment = TextAlignmentOptions.TopLeft;
            descText.color = UITheme.TextSecondary;
            descText.fontSize = 11f;
            descText.raycastTarget = false;

            // --- Battery Warning ---
            var batteryWarningGO = new GameObject("BatteryWarning");
            batteryWarningGO.transform.SetParent(toggleGO.transform, false);
            var batteryLayout = batteryWarningGO.AddComponent<LayoutElement>();
            batteryLayout.preferredHeight = 20f;

            var batteryText = batteryWarningGO.AddComponent<TextMeshProUGUI>();
            batteryText.text = "⚠ Uses more battery while active";
            batteryText.alignment = TextAlignmentOptions.MidlineLeft;
            batteryText.color = new Color(1f, 0.8f, 0.2f, 1f);
            batteryText.fontSize = 10f;
            batteryText.raycastTarget = false;
            batteryWarningGO.SetActive(false);

            // --- Config Panel (shown when enabled) ---
            var configPanelGO = new GameObject("ConfigPanel");
            configPanelGO.transform.SetParent(toggleGO.transform, false);
            var configLayout = configPanelGO.AddComponent<LayoutElement>();
            configLayout.preferredHeight = 100f;

            var configCanvasGroup = configPanelGO.AddComponent<CanvasGroup>();
            configCanvasGroup.alpha = 0f;
            configCanvasGroup.interactable = false;
            configCanvasGroup.blocksRaycasts = false;

            var configVLayout = configPanelGO.AddComponent<VerticalLayoutGroup>();
            configVLayout.spacing = 8f;
            configVLayout.childAlignment = TextAnchor.UpperLeft;
            configVLayout.childControlWidth = true;
            configVLayout.childControlHeight = true;
            configVLayout.childForceExpandWidth = true;
            configVLayout.childForceExpandHeight = false;

            // Host input row
            var hostRowGO = CreateInputRow("HostRow", "GSPro Host:", "localhost", out var hostInput);
            hostRowGO.transform.SetParent(configPanelGO.transform, false);

            // Port input row
            var portRowGO = CreateInputRow("PortRow", "Port:", "921", out var portInput);
            portRowGO.transform.SetParent(configPanelGO.transform, false);

            // Status row
            var statusRowGO = new GameObject("StatusRow");
            statusRowGO.transform.SetParent(configPanelGO.transform, false);
            var statusRowLayout = statusRowGO.AddComponent<LayoutElement>();
            statusRowLayout.preferredHeight = 20f;

            var statusRowHLayout = statusRowGO.AddComponent<HorizontalLayoutGroup>();
            statusRowHLayout.spacing = 8f;
            statusRowHLayout.childAlignment = TextAnchor.MiddleLeft;
            statusRowHLayout.childControlWidth = false;
            statusRowHLayout.childControlHeight = true;
            statusRowHLayout.childForceExpandWidth = false;
            statusRowHLayout.childForceExpandHeight = true;

            var statusLabelGO = new GameObject("Label");
            statusLabelGO.transform.SetParent(statusRowGO.transform, false);
            var statusLabelLayout = statusLabelGO.AddComponent<LayoutElement>();
            statusLabelLayout.preferredWidth = 50f;
            var statusLabel = statusLabelGO.AddComponent<TextMeshProUGUI>();
            statusLabel.text = "Status:";
            statusLabel.alignment = TextAlignmentOptions.MidlineLeft;
            statusLabel.color = UITheme.TextSecondary;
            statusLabel.fontSize = 11f;
            statusLabel.raycastTarget = false;

            var statusValueGO = new GameObject("StatusText");
            statusValueGO.transform.SetParent(statusRowGO.transform, false);
            var statusValueLayout = statusValueGO.AddComponent<LayoutElement>();
            statusValueLayout.flexibleWidth = 1f;
            var statusText = statusValueGO.AddComponent<TextMeshProUGUI>();
            statusText.text = "Disconnected";
            statusText.alignment = TextAlignmentOptions.MidlineLeft;
            statusText.color = BridgeModeToggle.DisconnectedColor;
            statusText.fontSize = 11f;
            statusText.raycastTarget = false;

            // Connect button
            var connectButtonGO = new GameObject("ConnectButton");
            connectButtonGO.transform.SetParent(configPanelGO.transform, false);
            var connectButtonRect = connectButtonGO.AddComponent<RectTransform>();
            var connectButtonLayout = connectButtonGO.AddComponent<LayoutElement>();
            connectButtonLayout.preferredHeight = 28f;

            var connectButtonImage = connectButtonGO.AddComponent<Image>();
            connectButtonImage.color = UITheme.AccentGreen;

            var connectButton = connectButtonGO.AddComponent<Button>();
            connectButton.targetGraphic = connectButtonImage;

            var connectButtonTextGO = new GameObject("Text");
            connectButtonTextGO.transform.SetParent(connectButtonGO.transform, false);
            var connectButtonTextRect = connectButtonTextGO.AddComponent<RectTransform>();
            connectButtonTextRect.anchorMin = Vector2.zero;
            connectButtonTextRect.anchorMax = Vector2.one;
            connectButtonTextRect.sizeDelta = Vector2.zero;

            var connectButtonText = connectButtonTextGO.AddComponent<TextMeshProUGUI>();
            connectButtonText.text = "Connect";
            connectButtonText.alignment = TextAlignmentOptions.Center;
            connectButtonText.color = Color.white;
            connectButtonText.fontSize = 13f;
            connectButtonText.fontStyle = FontStyles.Bold;
            connectButtonText.raycastTarget = false;

            // Add BridgeModeToggle component and wire up references
            var bridgeModeToggle = toggleGO.AddComponent<BridgeModeToggle>();

            var so = new SerializedObject(bridgeModeToggle);
            so.FindProperty("_enableToggle").objectReferenceValue = enableToggle;
            so.FindProperty("_titleText").objectReferenceValue = titleText;
            so.FindProperty("_descriptionText").objectReferenceValue = descText;
            so.FindProperty("_configPanel").objectReferenceValue = configCanvasGroup;
            so.FindProperty("_hostInput").objectReferenceValue = hostInput;
            so.FindProperty("_portInput").objectReferenceValue = portInput;
            so.FindProperty("_connectButton").objectReferenceValue = connectButton;
            so.FindProperty("_connectButtonText").objectReferenceValue = connectButtonText;
            so.FindProperty("_statusText").objectReferenceValue = statusText;
            so.FindProperty("_batteryWarningText").objectReferenceValue = batteryText;
            so.ApplyModifiedPropertiesWithoutUndo();

            return toggleGO;
        }

        private static GameObject CreateToggle(string name)
        {
            var toggleGO = new GameObject(name);
            var toggleRect = toggleGO.AddComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(50f, 26f);
            var toggleLayout = toggleGO.AddComponent<LayoutElement>();
            toggleLayout.preferredWidth = 50f;
            toggleLayout.preferredHeight = 26f;

            // Background
            var bgImage = toggleGO.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            var toggle = toggleGO.AddComponent<Toggle>();
            toggle.targetGraphic = bgImage;

            // Checkmark
            var checkGO = new GameObject("Checkmark");
            checkGO.transform.SetParent(toggleGO.transform, false);
            var checkRect = checkGO.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.5f, 0f);
            checkRect.anchorMax = new Vector2(1f, 1f);
            checkRect.sizeDelta = Vector2.zero;

            var checkImage = checkGO.AddComponent<Image>();
            checkImage.color = UITheme.AccentGreen;

            toggle.graphic = checkImage;
            toggle.isOn = false;

            return toggleGO;
        }

        private static GameObject CreateInputRow(string name, string label, string defaultValue, out TMP_InputField inputField)
        {
            var rowGO = new GameObject(name);
            var rowLayout = rowGO.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 24f;

            var rowHLayout = rowGO.AddComponent<HorizontalLayoutGroup>();
            rowHLayout.spacing = 8f;
            rowHLayout.childAlignment = TextAnchor.MiddleLeft;
            rowHLayout.childControlWidth = false;
            rowHLayout.childControlHeight = true;
            rowHLayout.childForceExpandWidth = false;
            rowHLayout.childForceExpandHeight = true;

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(rowGO.transform, false);
            var labelLayout = labelGO.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 70f;

            var labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.color = UITheme.TextSecondary;
            labelText.fontSize = 11f;
            labelText.raycastTarget = false;

            // Input field
            var inputGO = new GameObject("Input");
            inputGO.transform.SetParent(rowGO.transform, false);
            var inputRect = inputGO.AddComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(120f, 22f);
            var inputLayout = inputGO.AddComponent<LayoutElement>();
            inputLayout.preferredWidth = 120f;
            inputLayout.preferredHeight = 22f;

            var inputBg = inputGO.AddComponent<Image>();
            inputBg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            inputField = inputGO.AddComponent<TMP_InputField>();
            inputField.text = defaultValue;

            // Text area
            var textAreaGO = new GameObject("Text Area");
            textAreaGO.transform.SetParent(inputGO.transform, false);
            var textAreaRect = textAreaGO.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(6f, 2f);
            textAreaRect.offsetMax = new Vector2(-6f, -2f);

            var inputText = textAreaGO.AddComponent<TextMeshProUGUI>();
            inputText.text = defaultValue;
            inputText.alignment = TextAlignmentOptions.MidlineLeft;
            inputText.color = UITheme.TextPrimary;
            inputText.fontSize = 11f;

            inputField.textComponent = inputText;
            inputField.textViewport = textAreaRect;

            return rowGO;
        }

        #endregion
    }
}
