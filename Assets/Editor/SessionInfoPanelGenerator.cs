// ABOUTME: Editor tool for creating Session Info Panel, Shot History, and related UI prefabs.
// ABOUTME: Creates properly configured UI components with TextMeshPro elements and layout groups.

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using OpenRange.UI;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor tool for creating Session Info Panel and related UI prefabs.
    /// </summary>
    public static class SessionInfoPanelGenerator
    {
        private const string PrefabPath = "Assets/Prefabs/UI";

        #region Menu Items

        [MenuItem("OpenRange/Create Session Info Panel Prefab")]
        public static void CreateSessionInfoPanelPrefab()
        {
            EnsureDirectories();

            var panelGO = CreateSessionInfoPanel();

            string path = $"{PrefabPath}/SessionInfoPanel.prefab";
            PrefabUtility.SaveAsPrefabAsset(panelGO, path);
            Object.DestroyImmediate(panelGO);

            AssetDatabase.Refresh();
            Debug.Log($"SessionInfoPanelGenerator: Created SessionInfoPanel.prefab at {path}");
        }

        [MenuItem("OpenRange/Create Shot History Item Prefab")]
        public static void CreateShotHistoryItemPrefab()
        {
            EnsureDirectories();

            var itemGO = CreateShotHistoryItem();

            string path = $"{PrefabPath}/ShotHistoryItem.prefab";
            PrefabUtility.SaveAsPrefabAsset(itemGO, path);
            Object.DestroyImmediate(itemGO);

            AssetDatabase.Refresh();
            Debug.Log($"SessionInfoPanelGenerator: Created ShotHistoryItem.prefab at {path}");
        }

        [MenuItem("OpenRange/Create Shot History Panel Prefab")]
        public static void CreateShotHistoryPanelPrefab()
        {
            EnsureDirectories();

            // First create the item prefab if it doesn't exist
            var itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/ShotHistoryItem.prefab");
            if (itemPrefab == null)
            {
                CreateShotHistoryItemPrefab();
                itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/ShotHistoryItem.prefab");
            }

            var panelGO = CreateShotHistoryPanel(itemPrefab);

            string path = $"{PrefabPath}/ShotHistoryPanel.prefab";
            PrefabUtility.SaveAsPrefabAsset(panelGO, path);
            Object.DestroyImmediate(panelGO);

            AssetDatabase.Refresh();
            Debug.Log($"SessionInfoPanelGenerator: Created ShotHistoryPanel.prefab at {path}");
        }

        [MenuItem("OpenRange/Create Shot Detail Modal Prefab")]
        public static void CreateShotDetailModalPrefab()
        {
            EnsureDirectories();

            var modalGO = CreateShotDetailModal();

            string path = $"{PrefabPath}/ShotDetailModal.prefab";
            PrefabUtility.SaveAsPrefabAsset(modalGO, path);
            Object.DestroyImmediate(modalGO);

            AssetDatabase.Refresh();
            Debug.Log($"SessionInfoPanelGenerator: Created ShotDetailModal.prefab at {path}");
        }

        [MenuItem("OpenRange/Create All Session Info Prefabs")]
        public static void CreateAllPrefabs()
        {
            CreateSessionInfoPanelPrefab();
            CreateShotHistoryItemPrefab();
            CreateShotHistoryPanelPrefab();
            CreateShotDetailModalPrefab();
            Debug.Log("SessionInfoPanelGenerator: Created all session info prefabs");
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

        private static GameObject CreateSessionInfoPanel()
        {
            var panelGO = new GameObject("SessionInfoPanel");

            // RectTransform - top-left corner
            var rectTransform = panelGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(20, -60);
            rectTransform.sizeDelta = new Vector2(180, 120);

            // Background
            var bgImage = panelGO.AddComponent<Image>();
            bgImage.color = UITheme.PanelBackground;

            // CanvasGroup
            var canvasGroup = panelGO.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;

            // Make it a button for expand functionality
            var expandButton = panelGO.AddComponent<Button>();
            expandButton.transition = Selectable.Transition.ColorTint;

            // Vertical Layout
            var layoutGroup = panelGO.AddComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(12, 12, 8, 8);
            layoutGroup.spacing = 4;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            // Title row
            var titleGO = CreateLabelValueRow("TitleRow", "SESSION", "", 14, true);
            titleGO.transform.SetParent(panelGO.transform, false);

            // Session Time
            var timeRow = CreateLabelValueRow("TimeRow", "Time", "00:00:00", 12);
            timeRow.transform.SetParent(panelGO.transform, false);
            var sessionTimeText = timeRow.transform.Find("Value").GetComponent<TextMeshProUGUI>();

            // Total Shots
            var shotsRow = CreateLabelValueRow("ShotsRow", "Shots", "0", 12);
            shotsRow.transform.SetParent(panelGO.transform, false);
            var totalShotsText = shotsRow.transform.Find("Value").GetComponent<TextMeshProUGUI>();

            // Avg Speed
            var speedRow = CreateLabelValueRow("SpeedRow", "Avg Speed", "- mph", 12);
            speedRow.transform.SetParent(panelGO.transform, false);
            var avgSpeedText = speedRow.transform.Find("Value").GetComponent<TextMeshProUGUI>();

            // Longest Carry
            var carryRow = CreateLabelValueRow("CarryRow", "Best Carry", "- yd", 12);
            carryRow.transform.SetParent(panelGO.transform, false);
            var longestCarryText = carryRow.transform.Find("Value").GetComponent<TextMeshProUGUI>();

            // Add SessionInfoPanel component and wire up references
            var panel = panelGO.AddComponent<SessionInfoPanel>();

            var so = new SerializedObject(panel);
            so.FindProperty("_sessionTimeText").objectReferenceValue = sessionTimeText;
            so.FindProperty("_totalShotsText").objectReferenceValue = totalShotsText;
            so.FindProperty("_avgSpeedText").objectReferenceValue = avgSpeedText;
            so.FindProperty("_longestCarryText").objectReferenceValue = longestCarryText;
            so.FindProperty("_expandButton").objectReferenceValue = expandButton;
            so.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("_backgroundImage").objectReferenceValue = bgImage;
            so.ApplyModifiedPropertiesWithoutUndo();

            return panelGO;
        }

        private static GameObject CreateShotHistoryItem()
        {
            var itemGO = new GameObject("ShotHistoryItem");

            // RectTransform
            var rectTransform = itemGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, 50);

            // Background
            var bgImage = itemGO.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);

            // Make it clickable
            var button = itemGO.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;

            // Layout Element
            var layoutElement = itemGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 50;
            layoutElement.flexibleWidth = 1f;

            // Horizontal Layout
            var layoutGroup = itemGO.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.padding = new RectOffset(8, 8, 4, 4);
            layoutGroup.spacing = 8;
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = true;

            // Shot Number
            var shotNumGO = CreateText("ShotNumber", "#1", 14, FontStyles.Bold, new Vector2(40, 0));
            shotNumGO.transform.SetParent(itemGO.transform, false);
            var shotNumberText = shotNumGO.GetComponent<TextMeshProUGUI>();

            // Ball Speed
            var speedGO = CreateText("BallSpeed", "165 mph", 12, FontStyles.Normal, new Vector2(70, 0));
            speedGO.transform.SetParent(itemGO.transform, false);
            var ballSpeedText = speedGO.GetComponent<TextMeshProUGUI>();

            // Carry Distance
            var carryGO = CreateText("CarryDistance", "275 yd", 12, FontStyles.Normal, new Vector2(60, 0));
            carryGO.transform.SetParent(itemGO.transform, false);
            var carryDistanceText = carryGO.GetComponent<TextMeshProUGUI>();

            // Time Ago (flexible width)
            var timeGO = CreateText("TimeAgo", "Just now", 11, FontStyles.Italic, new Vector2(0, 0), true);
            timeGO.transform.SetParent(itemGO.transform, false);
            var timeAgoText = timeGO.GetComponent<TextMeshProUGUI>();
            timeAgoText.color = UITheme.TextSecondary;
            timeAgoText.alignment = TextAlignmentOptions.MidlineRight;

            // Selection Highlight (hidden by default)
            var highlightGO = new GameObject("SelectionHighlight");
            highlightGO.transform.SetParent(itemGO.transform, false);
            var highlightRect = highlightGO.AddComponent<RectTransform>();
            highlightRect.anchorMin = Vector2.zero;
            highlightRect.anchorMax = Vector2.one;
            highlightRect.offsetMin = Vector2.zero;
            highlightRect.offsetMax = Vector2.zero;
            var highlightImage = highlightGO.AddComponent<Image>();
            highlightImage.color = new Color(0.2f, 0.6f, 0.4f, 0.3f);
            highlightGO.SetActive(false);

            // Add ShotHistoryItem component and wire up references
            var item = itemGO.AddComponent<ShotHistoryItem>();

            var so = new SerializedObject(item);
            so.FindProperty("_shotNumberText").objectReferenceValue = shotNumberText;
            so.FindProperty("_ballSpeedText").objectReferenceValue = ballSpeedText;
            so.FindProperty("_carryDistanceText").objectReferenceValue = carryDistanceText;
            so.FindProperty("_timeAgoText").objectReferenceValue = timeAgoText;
            so.FindProperty("_itemButton").objectReferenceValue = button;
            so.FindProperty("_backgroundImage").objectReferenceValue = bgImage;
            so.FindProperty("_selectionHighlight").objectReferenceValue = highlightImage;
            so.ApplyModifiedPropertiesWithoutUndo();

            return itemGO;
        }

        private static GameObject CreateShotHistoryPanel(GameObject itemPrefab)
        {
            var panelGO = new GameObject("ShotHistoryPanel");

            // RectTransform - right side panel
            var rectTransform = panelGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1, 0);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(1, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(320, 0);
            rectTransform.offsetMin = new Vector2(-320, 100);
            rectTransform.offsetMax = new Vector2(0, -60);

            // Background
            var bgImage = panelGO.AddComponent<Image>();
            bgImage.color = new Color(UITheme.PanelBackground.r, UITheme.PanelBackground.g, UITheme.PanelBackground.b, 0.95f);

            // CanvasGroup
            var canvasGroup = panelGO.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // Main Vertical Layout
            var mainLayout = panelGO.AddComponent<VerticalLayoutGroup>();
            mainLayout.padding = new RectOffset(12, 12, 12, 12);
            mainLayout.spacing = 8;
            mainLayout.childAlignment = TextAnchor.UpperCenter;
            mainLayout.childControlWidth = true;
            mainLayout.childControlHeight = false;
            mainLayout.childForceExpandWidth = true;
            mainLayout.childForceExpandHeight = false;

            // Header with title and close button
            var headerGO = CreateHeader(panelGO.transform);
            var closeButton = headerGO.transform.Find("CloseButton").GetComponent<Button>();

            // Statistics summary section
            var statsSection = CreateStatisticsSection(panelGO.transform);
            var totalShotsText = statsSection.transform.Find("TotalShotsRow/Value").GetComponent<TextMeshProUGUI>();
            var avgSpeedText = statsSection.transform.Find("AvgSpeedRow/Value").GetComponent<TextMeshProUGUI>();
            var avgCarryText = statsSection.transform.Find("AvgCarryRow/Value").GetComponent<TextMeshProUGUI>();
            var longestCarryText = statsSection.transform.Find("LongestCarryRow/Value").GetComponent<TextMeshProUGUI>();

            // Scroll area for shot list
            var scrollArea = CreateScrollArea(panelGO.transform);
            var scrollRect = scrollArea.GetComponent<ScrollRect>();
            var contentContainer = scrollArea.transform.Find("Viewport/Content").GetComponent<RectTransform>();

            // Clear History button at bottom
            var clearButton = CreateClearHistoryButton(panelGO.transform);

            // Add ShotHistoryPanel component and wire up references
            var panel = panelGO.AddComponent<ShotHistoryPanel>();

            var so = new SerializedObject(panel);
            so.FindProperty("_totalShotsText").objectReferenceValue = totalShotsText;
            so.FindProperty("_avgSpeedText").objectReferenceValue = avgSpeedText;
            so.FindProperty("_avgCarryText").objectReferenceValue = avgCarryText;
            so.FindProperty("_longestCarryText").objectReferenceValue = longestCarryText;
            so.FindProperty("_scrollRect").objectReferenceValue = scrollRect;
            so.FindProperty("_contentContainer").objectReferenceValue = contentContainer;
            so.FindProperty("_closeButton").objectReferenceValue = closeButton;
            so.FindProperty("_clearHistoryButton").objectReferenceValue = clearButton;
            so.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("_backgroundImage").objectReferenceValue = bgImage;

            if (itemPrefab != null)
            {
                var itemComponent = itemPrefab.GetComponent<ShotHistoryItem>();
                so.FindProperty("_itemPrefab").objectReferenceValue = itemComponent;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            return panelGO;
        }

        private static GameObject CreateShotDetailModal()
        {
            var modalGO = new GameObject("ShotDetailModal");

            // RectTransform - full screen overlay
            var rectTransform = modalGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // Background overlay (semi-transparent)
            var overlayImage = modalGO.AddComponent<Image>();
            overlayImage.color = new Color(0, 0, 0, 0.7f);

            // CanvasGroup
            var canvasGroup = modalGO.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // Content panel (centered)
            var contentPanel = new GameObject("ContentPanel");
            contentPanel.transform.SetParent(modalGO.transform, false);
            var contentRect = contentPanel.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(400, 500);

            var contentBg = contentPanel.AddComponent<Image>();
            contentBg.color = new Color(0.12f, 0.12f, 0.18f, 0.98f);

            // Vertical Layout for content
            var contentLayout = contentPanel.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(16, 16, 16, 16);
            contentLayout.spacing = 12;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            // Header with shot number and close button
            var headerGO = CreateModalHeader(contentPanel.transform);
            var closeButton = headerGO.transform.Find("CloseButton").GetComponent<Button>();
            var shotNumberText = headerGO.transform.Find("ShotNumber").GetComponent<TextMeshProUGUI>();
            var timestampText = headerGO.transform.Find("Timestamp").GetComponent<TextMeshProUGUI>();

            // Ball Data section
            var ballSection = CreateDataSection(contentPanel.transform, "BALL DATA");
            var ballSpeedRow = CreateDataRow(ballSection.transform, "Ball Speed", "- mph");
            var directionRow = CreateDataRow(ballSection.transform, "Direction", "- deg");
            var launchRow = CreateDataRow(ballSection.transform, "Launch Angle", "- deg");
            var backSpinRow = CreateDataRow(ballSection.transform, "Back Spin", "- rpm");
            var sideSpinRow = CreateDataRow(ballSection.transform, "Side Spin", "- rpm");

            // Result Data section
            var resultSection = CreateDataSection(contentPanel.transform, "RESULT");
            var carryRow = CreateDataRow(resultSection.transform, "Carry", "- yd", true);
            var runRow = CreateDataRow(resultSection.transform, "Run", "- yd");
            var totalRow = CreateDataRow(resultSection.transform, "Total", "- yd", true);
            var apexRow = CreateDataRow(resultSection.transform, "Apex", "- yd");
            var offlineRow = CreateDataRow(resultSection.transform, "Offline", "- yd");

            // Club Data section (hidden by default)
            var clubSection = CreateDataSection(contentPanel.transform, "CLUB DATA (HMT)");
            var clubSpeedRow = CreateDataRow(clubSection.transform, "Club Speed", "- mph");
            var clubPathRow = CreateDataRow(clubSection.transform, "Path", "-");
            var attackRow = CreateDataRow(clubSection.transform, "Attack Angle", "-");
            var faceRow = CreateDataRow(clubSection.transform, "Face Angle", "-");
            var loftRow = CreateDataRow(clubSection.transform, "Dynamic Loft", "- deg");
            clubSection.SetActive(false);

            // Replay button
            var replayButton = CreateReplayButton(contentPanel.transform);

            // Add ShotDetailModal component and wire up references
            var modal = modalGO.AddComponent<ShotDetailModal>();

            var so = new SerializedObject(modal);
            so.FindProperty("_shotNumberText").objectReferenceValue = shotNumberText;
            so.FindProperty("_timestampText").objectReferenceValue = timestampText;
            so.FindProperty("_ballSpeedText").objectReferenceValue = ballSpeedRow.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_directionText").objectReferenceValue = directionRow.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_launchAngleText").objectReferenceValue = launchRow.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_backSpinText").objectReferenceValue = backSpinRow.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_sideSpinText").objectReferenceValue = sideSpinRow.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_carryText").objectReferenceValue = carryRow.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_runText").objectReferenceValue = runRow.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_totalText").objectReferenceValue = totalRow.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_apexText").objectReferenceValue = apexRow.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_offlineText").objectReferenceValue = offlineRow.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_speedDeltaText").objectReferenceValue = carryRow.transform.Find("Delta")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_carryDeltaText").objectReferenceValue = totalRow.transform.Find("Delta")?.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_clubSpeedText").objectReferenceValue = clubSpeedRow.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_clubPathText").objectReferenceValue = clubPathRow.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_attackAngleText").objectReferenceValue = attackRow.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_faceAngleText").objectReferenceValue = faceRow.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_dynamicLoftText").objectReferenceValue = loftRow.GetComponent<TextMeshProUGUI>();
            so.FindProperty("_closeButton").objectReferenceValue = closeButton;
            so.FindProperty("_replayButton").objectReferenceValue = replayButton;
            so.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("_backgroundOverlay").objectReferenceValue = overlayImage;
            so.FindProperty("_clubDataContainer").objectReferenceValue = clubSection;
            so.ApplyModifiedPropertiesWithoutUndo();

            return modalGO;
        }

        #endregion

        #region Helper Methods

        private static GameObject CreateLabelValueRow(string name, string label, string value, int fontSize, bool isHeader = false)
        {
            var rowGO = new GameObject(name);
            var rowRect = rowGO.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0, fontSize + 4);

            var rowLayout = rowGO.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;

            var layoutElement = rowGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = fontSize + 4;

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(rowGO.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            var labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = fontSize;
            labelText.color = isHeader ? UITheme.TextPrimary : UITheme.TextSecondary;
            labelText.fontStyle = isHeader ? FontStyles.Bold : FontStyles.Normal;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;

            var labelLayout = labelGO.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;

            // Value
            var valueGO = new GameObject("Value");
            valueGO.transform.SetParent(rowGO.transform, false);
            var valueRect = valueGO.AddComponent<RectTransform>();
            var valueText = valueGO.AddComponent<TextMeshProUGUI>();
            valueText.text = value;
            valueText.fontSize = fontSize;
            valueText.color = UITheme.TextPrimary;
            valueText.fontStyle = FontStyles.Bold;
            valueText.alignment = TextAlignmentOptions.MidlineRight;

            var valueLayout = valueGO.AddComponent<LayoutElement>();
            valueLayout.flexibleWidth = 1f;

            return rowGO;
        }

        private static GameObject CreateText(string name, string text, int fontSize, FontStyles style, Vector2 size, bool flexible = false)
        {
            var textGO = new GameObject(name);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.sizeDelta = size;

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = UITheme.TextPrimary;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            var layout = textGO.AddComponent<LayoutElement>();
            if (flexible)
            {
                layout.flexibleWidth = 1f;
            }
            else
            {
                layout.preferredWidth = size.x;
            }

            return textGO;
        }

        private static GameObject CreateHeader(Transform parent)
        {
            var headerGO = new GameObject("Header");
            headerGO.transform.SetParent(parent, false);
            var headerRect = headerGO.AddComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(0, 30);

            var headerLayout = headerGO.AddComponent<HorizontalLayoutGroup>();
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandHeight = true;

            var headerLayoutElement = headerGO.AddComponent<LayoutElement>();
            headerLayoutElement.preferredHeight = 30;

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(headerGO.transform, false);
            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "SHOT HISTORY";
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = UITheme.TextPrimary;

            var titleLayout = titleGO.AddComponent<LayoutElement>();
            titleLayout.flexibleWidth = 1f;

            // Close button
            var closeGO = new GameObject("CloseButton");
            closeGO.transform.SetParent(headerGO.transform, false);
            var closeRect = closeGO.AddComponent<RectTransform>();
            closeRect.sizeDelta = new Vector2(30, 30);

            var closeBg = closeGO.AddComponent<Image>();
            closeBg.color = new Color(0.3f, 0.2f, 0.2f, 0.8f);

            var closeButton = closeGO.AddComponent<Button>();

            var closeLayout = closeGO.AddComponent<LayoutElement>();
            closeLayout.preferredWidth = 30;

            // X text
            var closeTextGO = new GameObject("Text");
            closeTextGO.transform.SetParent(closeGO.transform, false);
            var closeTextRect = closeTextGO.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;
            var closeText = closeTextGO.AddComponent<TextMeshProUGUI>();
            closeText.text = "X";
            closeText.fontSize = 14;
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.color = UITheme.TextPrimary;

            return headerGO;
        }

        private static GameObject CreateStatisticsSection(Transform parent)
        {
            var sectionGO = new GameObject("StatisticsSection");
            sectionGO.transform.SetParent(parent, false);

            var sectionLayout = sectionGO.AddComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = 4;
            sectionLayout.childAlignment = TextAnchor.UpperLeft;
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = false;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;

            var sectionLayoutElement = sectionGO.AddComponent<LayoutElement>();
            sectionLayoutElement.preferredHeight = 80;

            // Stats rows
            var totalRow = CreateLabelValueRow("TotalShotsRow", "Total Shots", "0", 12);
            totalRow.transform.SetParent(sectionGO.transform, false);

            var avgSpeedRow = CreateLabelValueRow("AvgSpeedRow", "Avg Speed", "- mph", 12);
            avgSpeedRow.transform.SetParent(sectionGO.transform, false);

            var avgCarryRow = CreateLabelValueRow("AvgCarryRow", "Avg Carry", "- yd", 12);
            avgCarryRow.transform.SetParent(sectionGO.transform, false);

            var longestRow = CreateLabelValueRow("LongestCarryRow", "Longest Carry", "- yd", 12);
            longestRow.transform.SetParent(sectionGO.transform, false);

            return sectionGO;
        }

        private static GameObject CreateScrollArea(Transform parent)
        {
            var scrollGO = new GameObject("ScrollArea");
            scrollGO.transform.SetParent(parent, false);

            var scrollRect = scrollGO.AddComponent<RectTransform>();
            var scrollRectComponent = scrollGO.AddComponent<ScrollRect>();

            var scrollLayout = scrollGO.AddComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1f;

            // Viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            var viewportImage = viewportGO.AddComponent<Image>();
            viewportImage.color = Color.clear;
            viewportGO.AddComponent<Mask>().showMaskGraphic = false;

            // Content
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);

            var contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 4;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var contentFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Configure scroll rect
            scrollRectComponent.content = contentRect;
            scrollRectComponent.viewport = viewportRect;
            scrollRectComponent.horizontal = false;
            scrollRectComponent.vertical = true;
            scrollRectComponent.scrollSensitivity = 30f;

            return scrollGO;
        }

        private static Button CreateClearHistoryButton(Transform parent)
        {
            var buttonGO = new GameObject("ClearHistoryButton");
            buttonGO.transform.SetParent(parent, false);

            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(0, 36);

            var buttonBg = buttonGO.AddComponent<Image>();
            buttonBg.color = new Color(0.4f, 0.2f, 0.2f, 0.8f);

            var button = buttonGO.AddComponent<Button>();

            var buttonLayout = buttonGO.AddComponent<LayoutElement>();
            buttonLayout.preferredHeight = 36;

            // Text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "Clear History";
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Center;
            text.color = UITheme.TextPrimary;

            return button;
        }

        private static GameObject CreateModalHeader(Transform parent)
        {
            var headerGO = new GameObject("Header");
            headerGO.transform.SetParent(parent, false);
            var headerRect = headerGO.AddComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(0, 50);

            var headerLayout = headerGO.AddComponent<LayoutElement>();
            headerLayout.preferredHeight = 50;

            // Shot Number (centered)
            var shotNumGO = new GameObject("ShotNumber");
            shotNumGO.transform.SetParent(headerGO.transform, false);
            var shotNumRect = shotNumGO.AddComponent<RectTransform>();
            shotNumRect.anchorMin = new Vector2(0, 0.5f);
            shotNumRect.anchorMax = new Vector2(1, 1);
            shotNumRect.offsetMin = Vector2.zero;
            shotNumRect.offsetMax = Vector2.zero;
            var shotNumText = shotNumGO.AddComponent<TextMeshProUGUI>();
            shotNumText.text = "Shot #1";
            shotNumText.fontSize = 20;
            shotNumText.fontStyle = FontStyles.Bold;
            shotNumText.alignment = TextAlignmentOptions.Center;
            shotNumText.color = UITheme.TextPrimary;

            // Timestamp (below shot number)
            var timestampGO = new GameObject("Timestamp");
            timestampGO.transform.SetParent(headerGO.transform, false);
            var timestampRect = timestampGO.AddComponent<RectTransform>();
            timestampRect.anchorMin = new Vector2(0, 0);
            timestampRect.anchorMax = new Vector2(1, 0.5f);
            timestampRect.offsetMin = Vector2.zero;
            timestampRect.offsetMax = Vector2.zero;
            var timestampText = timestampGO.AddComponent<TextMeshProUGUI>();
            timestampText.text = "00:00:00";
            timestampText.fontSize = 12;
            timestampText.alignment = TextAlignmentOptions.Center;
            timestampText.color = UITheme.TextSecondary;

            // Close button (top-right)
            var closeGO = new GameObject("CloseButton");
            closeGO.transform.SetParent(headerGO.transform, false);
            var closeRect = closeGO.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(8, 8);
            closeRect.sizeDelta = new Vector2(30, 30);

            var closeBg = closeGO.AddComponent<Image>();
            closeBg.color = new Color(0.3f, 0.2f, 0.2f, 0.8f);

            closeGO.AddComponent<Button>();

            var closeTextGO = new GameObject("Text");
            closeTextGO.transform.SetParent(closeGO.transform, false);
            var closeTextRect = closeTextGO.AddComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;
            var closeText = closeTextGO.AddComponent<TextMeshProUGUI>();
            closeText.text = "X";
            closeText.fontSize = 14;
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.color = UITheme.TextPrimary;

            return headerGO;
        }

        private static GameObject CreateDataSection(Transform parent, string title)
        {
            var sectionGO = new GameObject(title.Replace(" ", ""));
            sectionGO.transform.SetParent(parent, false);

            var sectionLayout = sectionGO.AddComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = 4;
            sectionLayout.childAlignment = TextAnchor.UpperLeft;
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = false;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;

            var sectionLayoutElement = sectionGO.AddComponent<LayoutElement>();
            sectionLayoutElement.preferredHeight = 100;

            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(sectionGO.transform, false);
            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = title;
            titleText.fontSize = 11;
            titleText.color = UITheme.TextSecondary;
            titleText.fontStyle = FontStyles.Bold;
            var titleLayout = titleGO.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 16;

            return sectionGO;
        }

        private static GameObject CreateDataRow(Transform parent, string label, string value, bool showDelta = false)
        {
            var rowGO = new GameObject(label.Replace(" ", "") + "Row");
            rowGO.transform.SetParent(parent, false);

            var rowLayout = rowGO.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandHeight = true;

            var rowLayoutElement = rowGO.AddComponent<LayoutElement>();
            rowLayoutElement.preferredHeight = 18;

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(rowGO.transform, false);
            var labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 12;
            labelText.color = UITheme.TextSecondary;
            var labelLayout = labelGO.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;

            // Value
            var valueGO = new GameObject("Value");
            valueGO.transform.SetParent(rowGO.transform, false);
            var valueText = valueGO.AddComponent<TextMeshProUGUI>();
            valueText.text = value;
            valueText.fontSize = 12;
            valueText.fontStyle = FontStyles.Bold;
            valueText.color = UITheme.TextPrimary;
            valueText.alignment = TextAlignmentOptions.MidlineRight;
            var valueLayout = valueGO.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = 80;

            // Delta (optional)
            if (showDelta)
            {
                var deltaGO = new GameObject("Delta");
                deltaGO.transform.SetParent(rowGO.transform, false);
                var deltaText = deltaGO.AddComponent<TextMeshProUGUI>();
                deltaText.text = "";
                deltaText.fontSize = 10;
                deltaText.color = UITheme.TextSecondary;
                deltaText.alignment = TextAlignmentOptions.MidlineLeft;
                var deltaLayout = deltaGO.AddComponent<LayoutElement>();
                deltaLayout.preferredWidth = 50;
            }

            return valueGO;
        }

        private static Button CreateReplayButton(Transform parent)
        {
            var buttonGO = new GameObject("ReplayButton");
            buttonGO.transform.SetParent(parent, false);

            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(0, 44);

            var buttonBg = buttonGO.AddComponent<Image>();
            buttonBg.color = UITheme.AccentGreen;

            var button = buttonGO.AddComponent<Button>();

            var buttonLayout = buttonGO.AddComponent<LayoutElement>();
            buttonLayout.preferredHeight = 44;

            // Text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "Replay Shot";
            text.fontSize = 16;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = UITheme.TextPrimary;

            return button;
        }

        #endregion
    }
}
