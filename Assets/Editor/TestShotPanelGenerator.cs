// ABOUTME: Editor tool that generates TestShotPanel prefab for runtime test shots.
// ABOUTME: Creates a left-side slide-out panel with presets, sliders, and fire button.

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpenRange.UI;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor utility to generate TestShotPanel prefab.
    /// </summary>
    public static class TestShotPanelGenerator
    {
        private const string PrefabsPath = "Assets/Prefabs/UI";

        #region Layout Constants

        /// <summary>Panel width.</summary>
        public const float PanelWidth = 280f;

        /// <summary>Panel padding.</summary>
        public const float PanelPadding = 12f;

        /// <summary>Section spacing.</summary>
        public const float SectionSpacing = 8f;

        /// <summary>Item spacing.</summary>
        public const float ItemSpacing = 4f;

        /// <summary>Preset button height.</summary>
        public const float PresetButtonHeight = 32f;

        /// <summary>Fire button height.</summary>
        public const float FireButtonHeight = 44f;

        /// <summary>Slider height.</summary>
        public const float SliderHeight = 44f;

        /// <summary>Toggle height.</summary>
        public const float ToggleHeight = 32f;

        /// <summary>Section header height.</summary>
        public const float HeaderHeight = 20f;

        /// <summary>Close button size.</summary>
        public const float CloseButtonSize = 28f;

        /// <summary>Button minimum width.</summary>
        public const float ButtonMinWidth = 60f;

        #endregion

        /// <summary>
        /// Alias for CreateTestShotPanelPrefab() used by SceneGenerator.
        /// </summary>
        public static void CreatePrefab()
        {
            CreateTestShotPanelPrefab();
        }

        [MenuItem("OpenRange/Create Test Shot Panel Prefab", priority = 355)]
        public static void CreateTestShotPanelPrefab()
        {
            EnsureDirectoriesExist();

            // Create root panel
            var panelGo = new GameObject("TestShotPanel");
            var panelRect = panelGo.AddComponent<RectTransform>();

            // Anchor to left side, stretch vertically
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 0.5f);
            panelRect.sizeDelta = new Vector2(PanelWidth, 0);
            panelRect.anchoredPosition = Vector2.zero;

            // Background
            var bgImage = panelGo.AddComponent<Image>();
            bgImage.color = UITheme.PanelBackground;

            // CanvasGroup for fade animation
            var canvasGroup = panelGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // Content container with ScrollRect
            var scrollViewGo = CreateScrollView(panelGo.transform);
            var contentGo = scrollViewGo.transform.Find("Viewport/Content").gameObject;

            // Add VerticalLayoutGroup to content - CRITICAL: childControlHeight = true
            var contentLayout = contentGo.GetComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset((int)PanelPadding, (int)PanelPadding, (int)PanelPadding, (int)PanelPadding);
            contentLayout.spacing = SectionSpacing;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;  // MUST be true to prevent overlapping
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            // Add ContentSizeFitter to content
            var contentFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Create sections
            var headerSection = CreateHeaderSection(contentGo.transform);
            var presetsSection = CreatePresetsSection(contentGo.transform);
            var ballDataSection = CreateBallDataSection(contentGo.transform);
            var spinDataSection = CreateSpinDataSection(contentGo.transform);
            var clubDataToggle = CreateClubDataToggle(contentGo.transform);
            var clubDataSection = CreateClubDataSection(contentGo.transform);
            var actionsSection = CreateActionsSection(contentGo.transform);
            var statusSection = CreateStatusSection(contentGo.transform);

            // Close button (overlays in top-right corner)
            var closeButton = CreateCloseButton(panelGo.transform);

            // Add TestShotPanel component and wire references
            var testShotPanel = panelGo.AddComponent<TestShotPanel>();
            var so = new SerializedObject(testShotPanel);

            so.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("_panelRect").objectReferenceValue = panelRect;

            // Preset buttons
            so.FindProperty("_driverButton").objectReferenceValue = presetsSection.driverButton;
            so.FindProperty("_sevenIronButton").objectReferenceValue = presetsSection.sevenIronButton;
            so.FindProperty("_wedgeButton").objectReferenceValue = presetsSection.wedgeButton;
            so.FindProperty("_hookButton").objectReferenceValue = presetsSection.hookButton;
            so.FindProperty("_sliceButton").objectReferenceValue = presetsSection.sliceButton;

            // Ball data sliders
            so.FindProperty("_ballSpeedSlider").objectReferenceValue = ballDataSection.ballSpeedSlider;
            so.FindProperty("_launchAngleSlider").objectReferenceValue = ballDataSection.launchAngleSlider;
            so.FindProperty("_directionSlider").objectReferenceValue = ballDataSection.directionSlider;

            // Spin data sliders
            so.FindProperty("_backSpinSlider").objectReferenceValue = spinDataSection.backSpinSlider;
            so.FindProperty("_sideSpinSlider").objectReferenceValue = spinDataSection.sideSpinSlider;

            // Club data
            so.FindProperty("_clubDataToggle").objectReferenceValue = clubDataToggle;
            so.FindProperty("_clubDataSection").objectReferenceValue = clubDataSection.sectionGo;
            so.FindProperty("_clubSpeedSlider").objectReferenceValue = clubDataSection.clubSpeedSlider;
            so.FindProperty("_attackAngleSlider").objectReferenceValue = clubDataSection.attackAngleSlider;
            so.FindProperty("_faceToTargetSlider").objectReferenceValue = clubDataSection.faceToTargetSlider;
            so.FindProperty("_pathSlider").objectReferenceValue = clubDataSection.pathSlider;
            so.FindProperty("_dynamicLoftSlider").objectReferenceValue = clubDataSection.dynamicLoftSlider;

            // Action buttons
            so.FindProperty("_fireShotButton").objectReferenceValue = actionsSection.fireShotButton;
            so.FindProperty("_resetBallButton").objectReferenceValue = actionsSection.resetBallButton;
            so.FindProperty("_closeButton").objectReferenceValue = closeButton;

            // Status
            so.FindProperty("_statusText").objectReferenceValue = statusSection;

            so.ApplyModifiedPropertiesWithoutUndo();

            // Save prefab
            string prefabPath = $"{PrefabsPath}/TestShotPanel.prefab";
            PrefabUtility.SaveAsPrefabAsset(panelGo, prefabPath);
            Object.DestroyImmediate(panelGo);

            Debug.Log($"TestShotPanelGenerator: Created TestShotPanel prefab at {prefabPath}");
        }

        #region Section Creation Methods

        private static GameObject CreateScrollView(Transform parent)
        {
            var scrollViewGo = new GameObject("ScrollView");
            scrollViewGo.transform.SetParent(parent);
            var scrollRect = scrollViewGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            var scrollRectComp = scrollViewGo.AddComponent<ScrollRect>();
            scrollRectComp.horizontal = false;
            scrollRectComp.vertical = true;
            scrollRectComp.movementType = ScrollRect.MovementType.Elastic;
            scrollRectComp.elasticity = 0.1f;
            scrollRectComp.scrollSensitivity = 20f;

            // Viewport
            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollViewGo.transform);
            var viewportRect = viewportGo.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(-8, 0); // Leave room for scrollbar

            var viewportMask = viewportGo.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = Color.white;

            // Content
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);

            contentGo.AddComponent<VerticalLayoutGroup>();

            // Wire up ScrollRect
            scrollRectComp.viewport = viewportRect;
            scrollRectComp.content = contentRect;

            // Scrollbar
            var scrollbarGo = new GameObject("Scrollbar");
            scrollbarGo.transform.SetParent(scrollViewGo.transform);
            var scrollbarRect = scrollbarGo.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.pivot = new Vector2(1, 0.5f);
            scrollbarRect.sizeDelta = new Vector2(6, 0);
            scrollbarRect.anchoredPosition = Vector2.zero;

            var scrollbarImage = scrollbarGo.AddComponent<Image>();
            scrollbarImage.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);

            var scrollbar = scrollbarGo.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            // Handle
            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(scrollbarGo.transform);
            var handleRect = handleGo.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            var handleImage = handleGo.AddComponent<Image>();
            handleImage.color = new Color(0.5f, 0.5f, 0.5f, 0.6f);

            scrollbar.targetGraphic = handleImage;
            scrollbar.handleRect = handleRect;
            scrollRectComp.verticalScrollbar = scrollbar;
            scrollRectComp.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

            return scrollViewGo;
        }

        private static GameObject CreateHeaderSection(Transform parent)
        {
            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(parent);
            var headerRect = headerGo.AddComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(0, HeaderHeight);

            var headerLayout = headerGo.AddComponent<LayoutElement>();
            headerLayout.minHeight = HeaderHeight;
            headerLayout.preferredHeight = HeaderHeight;
            headerLayout.flexibleHeight = 0f;

            var headerText = headerGo.AddComponent<TextMeshProUGUI>();
            headerText.text = "Test Shot";
            headerText.fontSize = 18f;
            headerText.fontStyle = FontStyles.Bold;
            headerText.color = UITheme.TextPrimary;
            headerText.alignment = TextAlignmentOptions.MidlineLeft;
            headerText.raycastTarget = false;

            return headerGo;
        }

        private static (Button driverButton, Button sevenIronButton, Button wedgeButton,
            Button hookButton, Button sliceButton) CreatePresetsSection(Transform parent)
        {
            // Section container
            var sectionGo = new GameObject("PresetsSection");
            sectionGo.transform.SetParent(parent);
            sectionGo.AddComponent<RectTransform>();

            var sectionLayout = sectionGo.AddComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = ItemSpacing;
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = true;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;

            var sectionLayoutElem = sectionGo.AddComponent<LayoutElement>();
            sectionLayoutElem.flexibleHeight = 0f;

            // Section header
            CreateSectionHeader(sectionGo.transform, "Quick Presets");

            // Row 1: Driver, 7-Iron, Wedge
            var row1Go = new GameObject("Row1");
            row1Go.transform.SetParent(sectionGo.transform);
            row1Go.AddComponent<RectTransform>();

            var row1Layout = row1Go.AddComponent<HorizontalLayoutGroup>();
            row1Layout.spacing = ItemSpacing;
            row1Layout.childControlWidth = true;
            row1Layout.childControlHeight = true;
            row1Layout.childForceExpandWidth = true;
            row1Layout.childForceExpandHeight = false;

            var row1LayoutElem = row1Go.AddComponent<LayoutElement>();
            row1LayoutElem.minHeight = PresetButtonHeight;
            row1LayoutElem.preferredHeight = PresetButtonHeight;
            row1LayoutElem.flexibleHeight = 0f;

            var driverButton = CreatePresetButton(row1Go.transform, "Driver");
            var sevenIronButton = CreatePresetButton(row1Go.transform, "7-Iron");
            var wedgeButton = CreatePresetButton(row1Go.transform, "Wedge");

            // Row 2: Hook, Slice
            var row2Go = new GameObject("Row2");
            row2Go.transform.SetParent(sectionGo.transform);
            row2Go.AddComponent<RectTransform>();

            var row2Layout = row2Go.AddComponent<HorizontalLayoutGroup>();
            row2Layout.spacing = ItemSpacing;
            row2Layout.childControlWidth = true;
            row2Layout.childControlHeight = true;
            row2Layout.childForceExpandWidth = true;
            row2Layout.childForceExpandHeight = false;

            var row2LayoutElem = row2Go.AddComponent<LayoutElement>();
            row2LayoutElem.minHeight = PresetButtonHeight;
            row2LayoutElem.preferredHeight = PresetButtonHeight;
            row2LayoutElem.flexibleHeight = 0f;

            var hookButton = CreatePresetButton(row2Go.transform, "Hook");
            var sliceButton = CreatePresetButton(row2Go.transform, "Slice");

            // Add spacer to fill remaining space
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(row2Go.transform);
            spacer.AddComponent<RectTransform>();
            var spacerLayout = spacer.AddComponent<LayoutElement>();
            spacerLayout.flexibleWidth = 1f;

            return (driverButton, sevenIronButton, wedgeButton, hookButton, sliceButton);
        }

        private static Button CreatePresetButton(Transform parent, string text)
        {
            var buttonGo = new GameObject(text + "Button");
            buttonGo.transform.SetParent(parent);
            var buttonRect = buttonGo.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(ButtonMinWidth, PresetButtonHeight);

            var buttonLayout = buttonGo.AddComponent<LayoutElement>();
            buttonLayout.minWidth = ButtonMinWidth;
            buttonLayout.minHeight = PresetButtonHeight;
            buttonLayout.preferredHeight = PresetButtonHeight;
            buttonLayout.flexibleWidth = 1f;
            buttonLayout.flexibleHeight = 0f;

            var buttonImage = buttonGo.AddComponent<Image>();
            buttonImage.color = new Color(0.25f, 0.25f, 0.30f, 0.9f);
            buttonImage.raycastTarget = true;  // Ensure button receives clicks

            var button = buttonGo.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            var colors = button.colors;
            colors.normalColor = new Color(0.25f, 0.25f, 0.30f, 0.9f);
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.40f, 0.9f);
            colors.pressedColor = new Color(0.20f, 0.20f, 0.25f, 0.9f);
            button.colors = colors;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(buttonGo.transform);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var textComp = textGo.AddComponent<TextMeshProUGUI>();
            textComp.text = text;
            textComp.fontSize = 13f;
            textComp.color = UITheme.TextPrimary;
            textComp.alignment = TextAlignmentOptions.Center;
            textComp.raycastTarget = false;

            return button;
        }

        private static (SettingSlider ballSpeedSlider, SettingSlider launchAngleSlider,
            SettingSlider directionSlider) CreateBallDataSection(Transform parent)
        {
            // Section container
            var sectionGo = new GameObject("BallDataSection");
            sectionGo.transform.SetParent(parent);
            sectionGo.AddComponent<RectTransform>();

            var sectionLayout = sectionGo.AddComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = ItemSpacing;
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = true;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;

            var sectionLayoutElem = sectionGo.AddComponent<LayoutElement>();
            sectionLayoutElem.flexibleHeight = 0f;

            // Section header
            CreateSectionHeader(sectionGo.transform, "Ball Data");

            // Sliders
            var ballSpeedSlider = CreateSettingSlider(sectionGo.transform, "Ball Speed", " mph", 50, 200, 150, true);
            var launchAngleSlider = CreateSettingSlider(sectionGo.transform, "Launch Angle", "°", 0, 45, 12, false);
            var directionSlider = CreateSettingSlider(sectionGo.transform, "Direction", "°", -20, 20, 0, false);

            return (ballSpeedSlider, launchAngleSlider, directionSlider);
        }

        private static (SettingSlider backSpinSlider, SettingSlider sideSpinSlider)
            CreateSpinDataSection(Transform parent)
        {
            // Section container
            var sectionGo = new GameObject("SpinDataSection");
            sectionGo.transform.SetParent(parent);
            sectionGo.AddComponent<RectTransform>();

            var sectionLayout = sectionGo.AddComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = ItemSpacing;
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = true;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;

            var sectionLayoutElem = sectionGo.AddComponent<LayoutElement>();
            sectionLayoutElem.flexibleHeight = 0f;

            // Section header
            CreateSectionHeader(sectionGo.transform, "Spin Data");

            // Sliders
            var backSpinSlider = CreateSettingSlider(sectionGo.transform, "Back Spin", " rpm", 0, 12000, 3000, true);
            var sideSpinSlider = CreateSettingSlider(sectionGo.transform, "Side Spin", " rpm", -3000, 3000, 0, true);

            return (backSpinSlider, sideSpinSlider);
        }

        private static SettingToggle CreateClubDataToggle(Transform parent)
        {
            var toggleGo = new GameObject("ClubDataToggle");
            toggleGo.transform.SetParent(parent);
            var toggleRect = toggleGo.AddComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(0, ToggleHeight);

            var toggleLayoutElem = toggleGo.AddComponent<LayoutElement>();
            toggleLayoutElem.minHeight = ToggleHeight;
            toggleLayoutElem.preferredHeight = ToggleHeight;
            toggleLayoutElem.flexibleHeight = 0f;

            // Horizontal layout for label + toggle
            var hLayout = toggleGo.AddComponent<HorizontalLayoutGroup>();
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(toggleGo.transform);
            var labelRect = labelGo.AddComponent<RectTransform>();

            var labelLayoutElem = labelGo.AddComponent<LayoutElement>();
            labelLayoutElem.flexibleWidth = 1f;

            var labelText = labelGo.AddComponent<TextMeshProUGUI>();
            labelText.text = "Include Club Data";
            labelText.fontSize = 13f;
            labelText.color = UITheme.TextPrimary;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.raycastTarget = false;

            // Toggle background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(toggleGo.transform);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(44, 24);

            var bgLayoutElem = bgGo.AddComponent<LayoutElement>();
            bgLayoutElem.minWidth = 44;
            bgLayoutElem.minHeight = 24;
            bgLayoutElem.preferredWidth = 44;
            bgLayoutElem.preferredHeight = 24;

            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

            // Checkmark
            var checkGo = new GameObject("Checkmark");
            checkGo.transform.SetParent(bgGo.transform);
            var checkRect = checkGo.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0, 0);
            checkRect.anchorMax = new Vector2(0.5f, 1);
            checkRect.offsetMin = new Vector2(2, 2);
            checkRect.offsetMax = new Vector2(-2, -2);

            var checkImage = checkGo.AddComponent<Image>();
            checkImage.color = UITheme.AccentGreen;

            // Toggle component on root
            var toggle = toggleGo.AddComponent<Toggle>();
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = false;

            // SettingToggle component
            var settingToggle = toggleGo.AddComponent<SettingToggle>();

            var so = new SerializedObject(settingToggle);
            so.FindProperty("_toggle").objectReferenceValue = toggle;
            so.FindProperty("_labelText").objectReferenceValue = labelText;
            so.FindProperty("_backgroundImage").objectReferenceValue = bgImage;
            so.ApplyModifiedPropertiesWithoutUndo();

            return settingToggle;
        }

        private static (GameObject sectionGo, SettingSlider clubSpeedSlider, SettingSlider attackAngleSlider,
            SettingSlider faceToTargetSlider, SettingSlider pathSlider, SettingSlider dynamicLoftSlider)
            CreateClubDataSection(Transform parent)
        {
            // Section container (starts hidden)
            var sectionGo = new GameObject("ClubDataSection");
            sectionGo.transform.SetParent(parent);
            sectionGo.AddComponent<RectTransform>();
            sectionGo.SetActive(false); // Hidden by default

            var sectionLayout = sectionGo.AddComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = ItemSpacing;
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = true;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;

            var sectionLayoutElem = sectionGo.AddComponent<LayoutElement>();
            sectionLayoutElem.flexibleHeight = 0f;

            // Section header
            CreateSectionHeader(sectionGo.transform, "Club Data (HMT)");

            // Sliders
            var clubSpeedSlider = CreateSettingSlider(sectionGo.transform, "Club Speed", " mph", 60, 130, 95, true);
            var attackAngleSlider = CreateSettingSlider(sectionGo.transform, "Attack Angle", "°", -10, 10, 0, false);
            var faceToTargetSlider = CreateSettingSlider(sectionGo.transform, "Face to Target", "°", -10, 10, 0, false);
            var pathSlider = CreateSettingSlider(sectionGo.transform, "Path", "°", -15, 15, 0, false);
            var dynamicLoftSlider = CreateSettingSlider(sectionGo.transform, "Dynamic Loft", "°", 5, 50, 15, false);

            return (sectionGo, clubSpeedSlider, attackAngleSlider, faceToTargetSlider, pathSlider, dynamicLoftSlider);
        }

        private static (Button fireShotButton, Button resetBallButton) CreateActionsSection(Transform parent)
        {
            // Section container
            var sectionGo = new GameObject("ActionsSection");
            sectionGo.transform.SetParent(parent);
            sectionGo.AddComponent<RectTransform>();

            var sectionLayout = sectionGo.AddComponent<VerticalLayoutGroup>();
            sectionLayout.spacing = ItemSpacing;
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = true;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;

            var sectionLayoutElem = sectionGo.AddComponent<LayoutElement>();
            sectionLayoutElem.flexibleHeight = 0f;

            // Fire Shot button (large, green)
            var fireShotGo = new GameObject("FireShotButton");
            fireShotGo.transform.SetParent(sectionGo.transform);
            var fireShotRect = fireShotGo.AddComponent<RectTransform>();
            fireShotRect.sizeDelta = new Vector2(0, FireButtonHeight);

            var fireLayout = fireShotGo.AddComponent<LayoutElement>();
            fireLayout.minHeight = FireButtonHeight;
            fireLayout.preferredHeight = FireButtonHeight;
            fireLayout.flexibleHeight = 0f;

            var fireImage = fireShotGo.AddComponent<Image>();
            fireImage.color = new Color(0.2f, 0.5f, 0.2f, 1f); // Green

            var fireButton = fireShotGo.AddComponent<Button>();
            fireButton.targetGraphic = fireImage;

            var fireColors = fireButton.colors;
            fireColors.normalColor = new Color(0.2f, 0.5f, 0.2f, 1f);
            fireColors.highlightedColor = new Color(0.3f, 0.6f, 0.3f, 1f);
            fireColors.pressedColor = new Color(0.15f, 0.4f, 0.15f, 1f);
            fireButton.colors = fireColors;

            var fireTextGo = new GameObject("Text");
            fireTextGo.transform.SetParent(fireShotGo.transform);
            var fireTextRect = fireTextGo.AddComponent<RectTransform>();
            fireTextRect.anchorMin = Vector2.zero;
            fireTextRect.anchorMax = Vector2.one;
            fireTextRect.offsetMin = Vector2.zero;
            fireTextRect.offsetMax = Vector2.zero;

            var fireText = fireTextGo.AddComponent<TextMeshProUGUI>();
            fireText.text = "FIRE SHOT";
            fireText.fontSize = 16f;
            fireText.fontStyle = FontStyles.Bold;
            fireText.color = Color.white;
            fireText.alignment = TextAlignmentOptions.Center;
            fireText.raycastTarget = false;

            // Reset Ball button
            var resetGo = new GameObject("ResetBallButton");
            resetGo.transform.SetParent(sectionGo.transform);
            var resetRect = resetGo.AddComponent<RectTransform>();
            resetRect.sizeDelta = new Vector2(0, PresetButtonHeight);

            var resetLayout = resetGo.AddComponent<LayoutElement>();
            resetLayout.minHeight = PresetButtonHeight;
            resetLayout.preferredHeight = PresetButtonHeight;
            resetLayout.flexibleHeight = 0f;

            var resetImage = resetGo.AddComponent<Image>();
            resetImage.color = new Color(0.3f, 0.3f, 0.3f, 0.9f);

            var resetButton = resetGo.AddComponent<Button>();
            resetButton.targetGraphic = resetImage;

            var resetTextGo = new GameObject("Text");
            resetTextGo.transform.SetParent(resetGo.transform);
            var resetTextRect = resetTextGo.AddComponent<RectTransform>();
            resetTextRect.anchorMin = Vector2.zero;
            resetTextRect.anchorMax = Vector2.one;
            resetTextRect.offsetMin = Vector2.zero;
            resetTextRect.offsetMax = Vector2.zero;

            var resetText = resetTextGo.AddComponent<TextMeshProUGUI>();
            resetText.text = "Reset Ball";
            resetText.fontSize = 13f;
            resetText.color = UITheme.TextPrimary;
            resetText.alignment = TextAlignmentOptions.Center;
            resetText.raycastTarget = false;

            return (fireButton, resetButton);
        }

        private static TextMeshProUGUI CreateStatusSection(Transform parent)
        {
            var statusGo = new GameObject("StatusSection");
            statusGo.transform.SetParent(parent);
            var statusRect = statusGo.AddComponent<RectTransform>();
            statusRect.sizeDelta = new Vector2(0, 20f);

            var statusLayout = statusGo.AddComponent<LayoutElement>();
            statusLayout.minHeight = 20f;
            statusLayout.preferredHeight = 20f;
            statusLayout.flexibleHeight = 0f;

            var statusText = statusGo.AddComponent<TextMeshProUGUI>();
            statusText.text = "Ready";
            statusText.fontSize = 11f;
            statusText.color = UITheme.TextSecondary;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.fontStyle = FontStyles.Italic;
            statusText.raycastTarget = false;

            return statusText;
        }

        private static Button CreateCloseButton(Transform parent)
        {
            var closeGo = new GameObject("CloseButton");
            closeGo.transform.SetParent(parent);
            var closeRect = closeGo.AddComponent<RectTransform>();

            // Top-right corner
            closeRect.anchorMin = new Vector2(1, 1);
            closeRect.anchorMax = new Vector2(1, 1);
            closeRect.pivot = new Vector2(1, 1);
            closeRect.anchoredPosition = new Vector2(-6, -6);
            closeRect.sizeDelta = new Vector2(CloseButtonSize, CloseButtonSize);

            var closeImage = closeGo.AddComponent<Image>();
            closeImage.color = new Color(0.4f, 0.4f, 0.4f, 0.9f);

            var closeButton = closeGo.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;

            // X text
            var xTextGo = new GameObject("XText");
            xTextGo.transform.SetParent(closeGo.transform);
            var xTextRect = xTextGo.AddComponent<RectTransform>();
            xTextRect.anchorMin = Vector2.zero;
            xTextRect.anchorMax = Vector2.one;
            xTextRect.offsetMin = Vector2.zero;
            xTextRect.offsetMax = Vector2.zero;

            var xText = xTextGo.AddComponent<TextMeshProUGUI>();
            xText.text = "X";
            xText.fontSize = 14f;
            xText.fontStyle = FontStyles.Bold;
            xText.color = UITheme.TextPrimary;
            xText.alignment = TextAlignmentOptions.Center;
            xText.raycastTarget = false;

            return closeButton;
        }

        #endregion

        #region Helper Methods

        private static void CreateSectionHeader(Transform parent, string text)
        {
            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(parent);
            var headerRect = headerGo.AddComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(0, 18f);

            var headerLayout = headerGo.AddComponent<LayoutElement>();
            headerLayout.minHeight = 18f;
            headerLayout.preferredHeight = 18f;
            headerLayout.flexibleHeight = 0f;

            var headerText = headerGo.AddComponent<TextMeshProUGUI>();
            headerText.text = text;
            headerText.fontSize = 12f;
            headerText.fontStyle = FontStyles.Bold;
            headerText.color = UITheme.TextSecondary;
            headerText.alignment = TextAlignmentOptions.MidlineLeft;
            headerText.raycastTarget = false;
        }

        private static SettingSlider CreateSettingSlider(Transform parent, string label, string suffix,
            float min, float max, float defaultValue, bool wholeNumbers)
        {
            var sliderGo = new GameObject(label.Replace(" ", "") + "Slider");
            sliderGo.transform.SetParent(parent);
            var sliderRect = sliderGo.AddComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(0, SliderHeight);

            var sliderLayoutElem = sliderGo.AddComponent<LayoutElement>();
            sliderLayoutElem.minHeight = SliderHeight;
            sliderLayoutElem.preferredHeight = SliderHeight;
            sliderLayoutElem.flexibleHeight = 0f;

            // Vertical layout for label row + slider
            var vLayout = sliderGo.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 2f;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.padding = new RectOffset(0, 0, 0, 0);

            // Label row
            var labelRowGo = new GameObject("LabelRow");
            labelRowGo.transform.SetParent(sliderGo.transform);
            var labelRowRect = labelRowGo.AddComponent<RectTransform>();

            var labelRowLayout = labelRowGo.AddComponent<HorizontalLayoutGroup>();
            labelRowLayout.childControlWidth = true;
            labelRowLayout.childControlHeight = true;
            labelRowLayout.childForceExpandWidth = false;
            labelRowLayout.childForceExpandHeight = false;

            var labelRowLayoutElem = labelRowGo.AddComponent<LayoutElement>();
            labelRowLayoutElem.minHeight = 18f;
            labelRowLayoutElem.preferredHeight = 18f;
            labelRowLayoutElem.flexibleHeight = 0f;

            // Label text
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(labelRowGo.transform);
            labelGo.AddComponent<RectTransform>();

            var labelLayoutElem = labelGo.AddComponent<LayoutElement>();
            labelLayoutElem.flexibleWidth = 1f;

            var labelText = labelGo.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 11f;
            labelText.color = UITheme.TextSecondary;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.raycastTarget = false;

            // Value text
            var valueGo = new GameObject("Value");
            valueGo.transform.SetParent(labelRowGo.transform);
            valueGo.AddComponent<RectTransform>();

            var valueLayoutElem = valueGo.AddComponent<LayoutElement>();
            valueLayoutElem.minWidth = 70f;

            var valueText = valueGo.AddComponent<TextMeshProUGUI>();
            valueText.text = (wholeNumbers ? defaultValue.ToString("F0") : defaultValue.ToString("F1")) + suffix;
            valueText.fontSize = 11f;
            valueText.color = UITheme.TextPrimary;
            valueText.alignment = TextAlignmentOptions.MidlineRight;
            valueText.raycastTarget = false;

            // Slider container
            var sliderContainerGo = new GameObject("SliderContainer");
            sliderContainerGo.transform.SetParent(sliderGo.transform);
            var sliderContainerRect = sliderContainerGo.AddComponent<RectTransform>();

            var sliderContainerLayoutElem = sliderContainerGo.AddComponent<LayoutElement>();
            sliderContainerLayoutElem.minHeight = 20f;
            sliderContainerLayoutElem.preferredHeight = 20f;
            sliderContainerLayoutElem.flexibleHeight = 0f;

            // Slider background
            var sliderBgImage = sliderContainerGo.AddComponent<Image>();
            sliderBgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Fill area
            var fillAreaGo = new GameObject("Fill Area");
            fillAreaGo.transform.SetParent(sliderContainerGo.transform);
            var fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);

            // Fill
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform);
            var fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0, 1);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var fillImage = fillGo.AddComponent<Image>();
            fillImage.color = UITheme.AccentGreen;

            // Handle area
            var handleAreaGo = new GameObject("Handle Slide Area");
            handleAreaGo.transform.SetParent(sliderContainerGo.transform);
            var handleAreaRect = handleAreaGo.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(8, 0);
            handleAreaRect.offsetMax = new Vector2(-8, 0);

            // Handle
            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(handleAreaGo.transform);
            var handleRect = handleGo.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(16, 0);
            handleRect.anchorMin = new Vector2(0, 0);
            handleRect.anchorMax = new Vector2(0, 1);

            var handleImage = handleGo.AddComponent<Image>();
            handleImage.color = Color.white;

            // Slider component on container
            var slider = sliderContainerGo.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = defaultValue;
            slider.wholeNumbers = wholeNumbers;
            slider.direction = Slider.Direction.LeftToRight;

            // SettingSlider component on root
            var settingSlider = sliderGo.AddComponent<SettingSlider>();

            var so = new SerializedObject(settingSlider);
            so.FindProperty("_slider").objectReferenceValue = slider;
            so.FindProperty("_labelText").objectReferenceValue = labelText;
            so.FindProperty("_valueText").objectReferenceValue = valueText;
            so.FindProperty("_backgroundImage").objectReferenceValue = sliderBgImage;
            so.FindProperty("_fillImage").objectReferenceValue = fillImage;
            so.ApplyModifiedPropertiesWithoutUndo();

            return settingSlider;
        }

        private static void EnsureDirectoriesExist()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            if (!AssetDatabase.IsValidFolder(PrefabsPath))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }
        }

        #endregion
    }
}
