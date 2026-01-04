// ABOUTME: Editor tool for creating Bridge Mode UI prefabs.
// ABOUTME: Creates BridgeModeOverlay prefab for floating status display.

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

        #endregion
    }
}
