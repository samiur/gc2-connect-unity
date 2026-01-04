// ABOUTME: Editor tool that generates SettingsPanel prefab and related UI elements.
// ABOUTME: Creates prefabs for SettingToggle, SettingSlider, SettingDropdown, and the main SettingsPanel.

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpenRange.UI;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor utility to generate Settings Panel prefabs.
    /// </summary>
    public static class SettingsPanelGenerator
    {
        private const string PrefabsPath = "Assets/Prefabs/UI";
        private const string MaterialsPath = "Assets/Materials/UI";

        #region Dropdown Layout Constants

        /// <summary>
        /// Height of the dropdown template (expanded area showing options).
        /// </summary>
        public const float DropdownTemplateHeight = 180f;

        /// <summary>
        /// Height of each dropdown item row.
        /// Must be tall enough to show full text without truncation.
        /// </summary>
        public const float DropdownItemHeight = 36f;

        /// <summary>
        /// Sorting order for dropdown template Canvas.
        /// Higher than normal UI ensures dropdown renders above other elements.
        /// </summary>
        public const int DropdownSortingOrder = 100;

        /// <summary>
        /// Minimum touch target size for accessibility (Apple HIG).
        /// </summary>
        public const float MinTouchTargetSize = 44f;

        /// <summary>
        /// Width of the scrollbar in settings panel.
        /// </summary>
        public const float ScrollbarWidth = 12f;

        #endregion

        [MenuItem("OpenRange/Create Setting Toggle Prefab", priority = 340)]
        public static void CreateSettingTogglePrefab()
        {
            EnsureDirectoriesExist();

            var toggleGo = new GameObject("SettingToggle");
            toggleGo.AddComponent<RectTransform>();

            // Background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(toggleGo.transform);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(1, 0.5f);
            bgRect.anchorMax = new Vector2(1, 0.5f);
            bgRect.pivot = new Vector2(1, 0.5f);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(60, 30);

            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

            // Checkmark/Handle
            var checkGo = new GameObject("Checkmark");
            checkGo.transform.SetParent(bgGo.transform);
            var checkRect = checkGo.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0, 0);
            checkRect.anchorMax = new Vector2(0.5f, 1);
            checkRect.offsetMin = new Vector2(2, 2);
            checkRect.offsetMax = new Vector2(-2, -2);

            var checkImage = checkGo.AddComponent<Image>();
            checkImage.color = UITheme.AccentGreen;

            // Toggle component
            var toggle = toggleGo.AddComponent<Toggle>();
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = false;

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(toggleGo.transform);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = new Vector2(-70, 0);

            var labelText = labelGo.AddComponent<TextMeshProUGUI>();
            labelText.text = "Toggle Label";
            labelText.fontSize = UITheme.FontSizeRegular.Normal;
            labelText.color = UITheme.TextPrimary;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;

            // SettingToggle component
            var settingToggle = toggleGo.AddComponent<SettingToggle>();

            // Wire up references using SerializedObject
            var so = new SerializedObject(settingToggle);
            so.FindProperty("_toggle").objectReferenceValue = toggle;
            so.FindProperty("_labelText").objectReferenceValue = labelText;
            so.FindProperty("_backgroundImage").objectReferenceValue = bgImage;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Set RectTransform
            var toggleRect = toggleGo.GetComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(200, 40);

            // Save prefab
            string prefabPath = $"{PrefabsPath}/SettingToggle.prefab";
            PrefabUtility.SaveAsPrefabAsset(toggleGo, prefabPath);
            Object.DestroyImmediate(toggleGo);

            Debug.Log($"SettingsPanelGenerator: Created SettingToggle prefab at {prefabPath}");
        }

        [MenuItem("OpenRange/Create Setting Slider Prefab", priority = 341)]
        public static void CreateSettingSliderPrefab()
        {
            EnsureDirectoriesExist();

            var sliderGo = new GameObject("SettingSlider");
            var sliderRect = sliderGo.AddComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(300, 60);

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(sliderGo.transform);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.6f);
            labelRect.anchorMax = new Vector2(0.7f, 1);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var labelText = labelGo.AddComponent<TextMeshProUGUI>();
            labelText.text = "Slider Label";
            labelText.fontSize = UITheme.FontSizeRegular.Normal;
            labelText.color = UITheme.TextPrimary;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;

            // Value text
            var valueGo = new GameObject("Value");
            valueGo.transform.SetParent(sliderGo.transform);
            var valueRect = valueGo.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.7f, 0.6f);
            valueRect.anchorMax = new Vector2(1, 1);
            valueRect.offsetMin = Vector2.zero;
            valueRect.offsetMax = Vector2.zero;

            var valueText = valueGo.AddComponent<TextMeshProUGUI>();
            valueText.text = "0";
            valueText.fontSize = UITheme.FontSizeRegular.Normal;
            valueText.color = UITheme.TextPrimary;
            valueText.alignment = TextAlignmentOptions.MidlineRight;

            // Slider container
            var sliderContainerGo = new GameObject("SliderContainer");
            sliderContainerGo.transform.SetParent(sliderGo.transform);
            var sliderContainerRect = sliderContainerGo.AddComponent<RectTransform>();
            sliderContainerRect.anchorMin = new Vector2(0, 0);
            sliderContainerRect.anchorMax = new Vector2(1, 0.5f);
            sliderContainerRect.offsetMin = Vector2.zero;
            sliderContainerRect.offsetMax = Vector2.zero;

            // Background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(sliderContainerGo.transform);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.25f);
            bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

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
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var fillImage = fillGo.AddComponent<Image>();
            fillImage.color = UITheme.AccentGreen;

            // Handle slide area
            var handleAreaGo = new GameObject("Handle Slide Area");
            handleAreaGo.transform.SetParent(sliderContainerGo.transform);
            var handleAreaRect = handleAreaGo.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);

            // Handle
            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(handleAreaGo.transform);
            var handleRect = handleGo.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);

            var handleImage = handleGo.AddComponent<Image>();
            handleImage.color = Color.white;

            // Slider component
            var slider = sliderContainerGo.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.value = 50;

            // SettingSlider component
            var settingSlider = sliderGo.AddComponent<SettingSlider>();

            // Wire up references
            var so = new SerializedObject(settingSlider);
            so.FindProperty("_slider").objectReferenceValue = slider;
            so.FindProperty("_labelText").objectReferenceValue = labelText;
            so.FindProperty("_valueText").objectReferenceValue = valueText;
            so.FindProperty("_backgroundImage").objectReferenceValue = bgImage;
            so.FindProperty("_fillImage").objectReferenceValue = fillImage;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save prefab
            string prefabPath = $"{PrefabsPath}/SettingSlider.prefab";
            PrefabUtility.SaveAsPrefabAsset(sliderGo, prefabPath);
            Object.DestroyImmediate(sliderGo);

            Debug.Log($"SettingsPanelGenerator: Created SettingSlider prefab at {prefabPath}");
        }

        [MenuItem("OpenRange/Create Setting Dropdown Prefab", priority = 342)]
        public static void CreateSettingDropdownPrefab()
        {
            EnsureDirectoriesExist();

            var dropdownGo = new GameObject("SettingDropdown");
            var dropdownRect = dropdownGo.AddComponent<RectTransform>();
            dropdownRect.sizeDelta = new Vector2(250, 60);

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(dropdownGo.transform);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(0.4f, 1);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var labelText = labelGo.AddComponent<TextMeshProUGUI>();
            labelText.text = "Dropdown";
            labelText.fontSize = UITheme.FontSizeRegular.Normal;
            labelText.color = UITheme.TextPrimary;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;

            // Dropdown container
            var containerGo = new GameObject("DropdownContainer");
            containerGo.transform.SetParent(dropdownGo.transform);
            var containerRect = containerGo.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.4f, 0);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.offsetMin = new Vector2(10, 0);
            containerRect.offsetMax = Vector2.zero;

            var containerImage = containerGo.AddComponent<Image>();
            containerImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Caption text
            var captionGo = new GameObject("Caption");
            captionGo.transform.SetParent(containerGo.transform);
            var captionRect = captionGo.AddComponent<RectTransform>();
            captionRect.anchorMin = Vector2.zero;
            captionRect.anchorMax = Vector2.one;
            captionRect.offsetMin = new Vector2(10, 0);
            captionRect.offsetMax = new Vector2(-30, 0);

            var captionText = captionGo.AddComponent<TextMeshProUGUI>();
            captionText.text = "Option";
            captionText.fontSize = UITheme.FontSizeRegular.Normal;
            captionText.color = UITheme.TextPrimary;
            captionText.alignment = TextAlignmentOptions.MidlineLeft;

            // Arrow
            var arrowGo = new GameObject("Arrow");
            arrowGo.transform.SetParent(containerGo.transform);
            var arrowRect = arrowGo.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0);
            arrowRect.anchorMax = new Vector2(1, 1);
            arrowRect.pivot = new Vector2(1, 0.5f);
            arrowRect.sizeDelta = new Vector2(25, 0);
            arrowRect.anchoredPosition = new Vector2(-5, 0);

            var arrowText = arrowGo.AddComponent<TextMeshProUGUI>();
            arrowText.text = "▼";
            arrowText.fontSize = 14;
            arrowText.color = UITheme.TextPrimary;
            arrowText.alignment = TextAlignmentOptions.Center;

            // Template
            var templateGo = new GameObject("Template");
            templateGo.transform.SetParent(containerGo.transform);
            var templateRect = templateGo.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.anchoredPosition = Vector2.zero;
            templateRect.sizeDelta = new Vector2(0, DropdownTemplateHeight);

            var templateImage = templateGo.AddComponent<Image>();
            templateImage.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            // Add Canvas for z-order override - ensures dropdown renders above other UI elements
            var templateCanvas = templateGo.AddComponent<Canvas>();
            templateCanvas.overrideSorting = true;
            templateCanvas.sortingOrder = DropdownSortingOrder;
            templateGo.AddComponent<GraphicRaycaster>();

            var scrollRect = templateGo.AddComponent<ScrollRect>();
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // Viewport
            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(templateGo.transform);
            var viewportRect = viewportGo.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            var viewportMask = viewportGo.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false; // Hide the mask image so dropdown bg shows through
            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = Color.white; // Required for mask but hidden

            // Content - needs proper layout for items
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, DropdownItemHeight);

            // Wire up ScrollRect
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            // Item - anchor to stretch full width
            var itemGo = new GameObject("Item");
            itemGo.transform.SetParent(contentGo.transform);
            var itemRect = itemGo.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 0.5f);
            itemRect.anchorMax = new Vector2(1, 0.5f);
            itemRect.pivot = new Vector2(0.5f, 0.5f);
            itemRect.anchoredPosition = Vector2.zero;
            itemRect.sizeDelta = new Vector2(0, DropdownItemHeight);

            var itemToggle = itemGo.AddComponent<Toggle>();

            // Item background
            var itemBgGo = new GameObject("Item Background");
            itemBgGo.transform.SetParent(itemGo.transform);
            var itemBgRect = itemBgGo.AddComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.offsetMin = Vector2.zero;
            itemBgRect.offsetMax = Vector2.zero;

            var itemBgImage = itemBgGo.AddComponent<Image>();
            itemBgImage.color = new Color(0.25f, 0.25f, 0.35f, 1f);

            // Item checkmark
            var itemCheckGo = new GameObject("Item Checkmark");
            itemCheckGo.transform.SetParent(itemGo.transform);
            var itemCheckRect = itemCheckGo.AddComponent<RectTransform>();
            itemCheckRect.anchorMin = new Vector2(0, 0.5f);
            itemCheckRect.anchorMax = new Vector2(0, 0.5f);
            itemCheckRect.pivot = new Vector2(0, 0.5f);
            itemCheckRect.sizeDelta = new Vector2(16, 16);
            itemCheckRect.anchoredPosition = new Vector2(8, 0);

            // Use Unity's built-in Checkmark sprite for proper visual
            var itemCheckImage = itemCheckGo.AddComponent<Image>();
            itemCheckImage.sprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
            itemCheckImage.color = UITheme.AccentGreen;
            itemCheckImage.preserveAspect = true;

            // Item label
            var itemLabelGo = new GameObject("Item Label");
            itemLabelGo.transform.SetParent(itemGo.transform);
            var itemLabelRect = itemLabelGo.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(25, 0);
            itemLabelRect.offsetMax = Vector2.zero;

            var itemLabelText = itemLabelGo.AddComponent<TextMeshProUGUI>();
            itemLabelText.text = "Option";
            itemLabelText.fontSize = UITheme.FontSizeRegular.Normal;
            itemLabelText.color = UITheme.TextPrimary;
            itemLabelText.alignment = TextAlignmentOptions.MidlineLeft;

            itemToggle.targetGraphic = itemBgImage;
            itemToggle.graphic = itemCheckImage;
            itemToggle.isOn = true;

            // Deactivate template
            templateGo.SetActive(false);

            // TMP_Dropdown component
            var dropdown = containerGo.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = containerImage;
            dropdown.template = templateRect;
            dropdown.captionText = captionText;
            dropdown.itemText = itemLabelText;

            // SettingDropdown component
            var settingDropdown = dropdownGo.AddComponent<SettingDropdown>();

            // Wire up references
            var so = new SerializedObject(settingDropdown);
            so.FindProperty("_dropdown").objectReferenceValue = dropdown;
            so.FindProperty("_labelText").objectReferenceValue = labelText;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save prefab
            string prefabPath = $"{PrefabsPath}/SettingDropdown.prefab";
            PrefabUtility.SaveAsPrefabAsset(dropdownGo, prefabPath);
            Object.DestroyImmediate(dropdownGo);

            Debug.Log($"SettingsPanelGenerator: Created SettingDropdown prefab at {prefabPath}");
        }

        [MenuItem("OpenRange/Create Settings Panel Prefab", priority = 343)]
        public static void CreateSettingsPanelPrefab()
        {
            EnsureDirectoriesExist();

            // Load prefabs
            var togglePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/SettingToggle.prefab");
            var sliderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/SettingSlider.prefab");
            var dropdownPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/SettingDropdown.prefab");

            if (togglePrefab == null || sliderPrefab == null || dropdownPrefab == null)
            {
                Debug.LogError("SettingsPanelGenerator: Missing prefabs. Run 'Create Setting Toggle/Slider/Dropdown Prefab' first.");
                return;
            }

            var panelGo = new GameObject("SettingsPanel");
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(500, 700);

            // Canvas Group
            var canvasGroup = panelGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // Background
            var bgImage = panelGo.AddComponent<Image>();
            bgImage.color = UITheme.PanelBackground;

            // Header
            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(panelGo.transform);
            var headerRect = headerGo.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, 60);

            var headerText = headerGo.AddComponent<TextMeshProUGUI>();
            headerText.text = "Settings";
            headerText.fontSize = UITheme.FontSizeRegular.Header;
            headerText.color = UITheme.TextPrimary;
            headerText.alignment = TextAlignmentOptions.Center;
            headerText.fontStyle = FontStyles.Bold;

            // Close button
            var closeBtn = CreateButton(panelGo.transform, "CloseButton", "×");
            var closeBtnRect = closeBtn.GetComponent<RectTransform>();
            closeBtnRect.anchorMin = new Vector2(1, 1);
            closeBtnRect.anchorMax = new Vector2(1, 1);
            closeBtnRect.pivot = new Vector2(1, 1);
            closeBtnRect.anchoredPosition = new Vector2(-10, -10);
            closeBtnRect.sizeDelta = new Vector2(40, 40);
            var closeBtnText = closeBtn.GetComponentInChildren<TextMeshProUGUI>();
            closeBtnText.fontSize = 28;

            // Scroll View
            var scrollGo = new GameObject("ScrollView");
            scrollGo.transform.SetParent(panelGo.transform);
            var scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(20, 70);
            scrollRect.offsetMax = new Vector2(-20, -70);

            var scrollView = scrollGo.AddComponent<ScrollRect>();
            scrollView.horizontal = false;
            scrollView.vertical = true;

            // Viewport - leave room for scrollbar on right
            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(scrollGo.transform);
            var viewportRect = viewportGo.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(-ScrollbarWidth, 0); // Room for scrollbar

            viewportGo.AddComponent<Mask>();
            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.color = new Color(1, 1, 1, 0.01f);

            scrollView.viewport = viewportRect;

            // Scrollbar
            var scrollbarGo = new GameObject("Scrollbar");
            scrollbarGo.transform.SetParent(scrollGo.transform);
            var scrollbarRect = scrollbarGo.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1, 0);
            scrollbarRect.anchorMax = new Vector2(1, 1);
            scrollbarRect.pivot = new Vector2(1, 0.5f);
            scrollbarRect.anchoredPosition = Vector2.zero;
            scrollbarRect.sizeDelta = new Vector2(ScrollbarWidth, 0);

            var scrollbarBg = scrollbarGo.AddComponent<Image>();
            scrollbarBg.color = new Color(0.1f, 0.1f, 0.12f, 0.6f);

            // Scrollbar sliding area
            var slidingAreaGo = new GameObject("Sliding Area");
            slidingAreaGo.transform.SetParent(scrollbarGo.transform);
            var slidingRect = slidingAreaGo.AddComponent<RectTransform>();
            slidingRect.anchorMin = Vector2.zero;
            slidingRect.anchorMax = Vector2.one;
            slidingRect.offsetMin = new Vector2(2, 2);
            slidingRect.offsetMax = new Vector2(-2, -2);

            // Handle
            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(slidingAreaGo.transform);
            var handleRect = handleGo.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = new Vector2(1, 0.3f); // Initial size
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;

            var handleImage = handleGo.AddComponent<Image>();
            handleImage.color = new Color(0.4f, 0.4f, 0.45f, 0.8f);

            var scrollbar = scrollbarGo.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            scrollView.verticalScrollbar = scrollbar;
            scrollView.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            // Content
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 850);

            var verticalLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            verticalLayout.spacing = 15;
            verticalLayout.padding = new RectOffset(10, 10, 10, 10);
            verticalLayout.childAlignment = TextAnchor.UpperCenter;
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = false;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;

            var contentSizeFitter = contentGo.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollView.content = contentRect;

            // Create sections
            SettingDropdown qualityDropdown = null;
            SettingDropdown frameRateDropdown = null;
            SettingDropdown distanceUnitDropdown = null;
            SettingDropdown speedUnitDropdown = null;
            SettingDropdown tempUnitDropdown = null;
            SettingSlider temperatureSlider = null;
            SettingSlider elevationSlider = null;
            SettingSlider humiditySlider = null;
            SettingToggle windEnabledToggle = null;
            SettingSlider windSpeedSlider = null;
            SettingSlider windDirectionSlider = null;
            SettingToggle autoConnectToggle = null;
            SettingSlider masterVolumeSlider = null;
            SettingSlider effectsVolumeSlider = null;

            // Graphics Section
            CreateSectionHeader(contentGo.transform, "Graphics");
            qualityDropdown = InstantiateDropdown(dropdownPrefab, contentGo.transform, "Quality");
            frameRateDropdown = InstantiateDropdown(dropdownPrefab, contentGo.transform, "Frame Rate");

            // Units Section
            CreateSectionHeader(contentGo.transform, "Units");
            distanceUnitDropdown = InstantiateDropdown(dropdownPrefab, contentGo.transform, "Distance");
            speedUnitDropdown = InstantiateDropdown(dropdownPrefab, contentGo.transform, "Speed");
            tempUnitDropdown = InstantiateDropdown(dropdownPrefab, contentGo.transform, "Temperature");

            // Environment Section
            CreateSectionHeader(contentGo.transform, "Environment");
            temperatureSlider = InstantiateSlider(sliderPrefab, contentGo.transform, "Temperature");
            elevationSlider = InstantiateSlider(sliderPrefab, contentGo.transform, "Elevation");
            humiditySlider = InstantiateSlider(sliderPrefab, contentGo.transform, "Humidity");
            windEnabledToggle = InstantiateToggle(togglePrefab, contentGo.transform, "Wind Enabled");
            windSpeedSlider = InstantiateSlider(sliderPrefab, contentGo.transform, "Wind Speed");
            windDirectionSlider = InstantiateSlider(sliderPrefab, contentGo.transform, "Wind Direction");

            // Connection Section
            CreateSectionHeader(contentGo.transform, "Connection");
            autoConnectToggle = InstantiateToggle(togglePrefab, contentGo.transform, "Auto-Connect");

            // Audio Section
            CreateSectionHeader(contentGo.transform, "Audio");
            masterVolumeSlider = InstantiateSlider(sliderPrefab, contentGo.transform, "Master Volume");
            effectsVolumeSlider = InstantiateSlider(sliderPrefab, contentGo.transform, "Effects Volume");

            // Reset Button
            var resetBtn = CreateButton(panelGo.transform, "ResetButton", "Reset to Defaults");
            var resetBtnRect = resetBtn.GetComponent<RectTransform>();
            resetBtnRect.anchorMin = new Vector2(0.5f, 0);
            resetBtnRect.anchorMax = new Vector2(0.5f, 0);
            resetBtnRect.pivot = new Vector2(0.5f, 0);
            resetBtnRect.anchoredPosition = new Vector2(0, 15);
            resetBtnRect.sizeDelta = new Vector2(200, 40);

            // SettingsPanel component
            var settingsPanel = panelGo.AddComponent<SettingsPanel>();

            // Wire up references
            var so = new SerializedObject(settingsPanel);
            so.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("_closeButton").objectReferenceValue = closeBtn.GetComponent<Button>();
            so.FindProperty("_resetButton").objectReferenceValue = resetBtn.GetComponent<Button>();
            so.FindProperty("_qualityDropdown").objectReferenceValue = qualityDropdown;
            so.FindProperty("_frameRateDropdown").objectReferenceValue = frameRateDropdown;
            so.FindProperty("_distanceUnitDropdown").objectReferenceValue = distanceUnitDropdown;
            so.FindProperty("_speedUnitDropdown").objectReferenceValue = speedUnitDropdown;
            so.FindProperty("_tempUnitDropdown").objectReferenceValue = tempUnitDropdown;
            so.FindProperty("_temperatureSlider").objectReferenceValue = temperatureSlider;
            so.FindProperty("_elevationSlider").objectReferenceValue = elevationSlider;
            so.FindProperty("_humiditySlider").objectReferenceValue = humiditySlider;
            so.FindProperty("_windEnabledToggle").objectReferenceValue = windEnabledToggle;
            so.FindProperty("_windSpeedSlider").objectReferenceValue = windSpeedSlider;
            so.FindProperty("_windDirectionSlider").objectReferenceValue = windDirectionSlider;
            so.FindProperty("_autoConnectToggle").objectReferenceValue = autoConnectToggle;
            so.FindProperty("_masterVolumeSlider").objectReferenceValue = masterVolumeSlider;
            so.FindProperty("_effectsVolumeSlider").objectReferenceValue = effectsVolumeSlider;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Save prefab
            string prefabPath = $"{PrefabsPath}/SettingsPanel.prefab";
            PrefabUtility.SaveAsPrefabAsset(panelGo, prefabPath);
            Object.DestroyImmediate(panelGo);

            Debug.Log($"SettingsPanelGenerator: Created SettingsPanel prefab at {prefabPath}");
        }

        [MenuItem("OpenRange/Create All Settings Panel Prefabs", priority = 344)]
        public static void CreateAllPrefabs()
        {
            CreateSettingTogglePrefab();
            CreateSettingSliderPrefab();
            CreateSettingDropdownPrefab();
            CreateSettingsPanelPrefab();

            AssetDatabase.Refresh();
            Debug.Log("SettingsPanelGenerator: All Settings Panel prefabs created");
        }

        #region Helper Methods

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

            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            if (!AssetDatabase.IsValidFolder(MaterialsPath))
            {
                AssetDatabase.CreateFolder("Assets/Materials", "UI");
            }
        }

        private static GameObject CreateButton(Transform parent, string name, string text)
        {
            var buttonGo = new GameObject(name);
            buttonGo.transform.SetParent(parent);

            var rectTransform = buttonGo.AddComponent<RectTransform>();
            rectTransform.localScale = Vector3.one;

            var image = buttonGo.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.35f, 0.9f);

            var button = buttonGo.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.3f, 0.3f, 0.35f, 0.9f);
            colors.highlightedColor = new Color(0.4f, 0.4f, 0.45f, 0.9f);
            colors.pressedColor = new Color(0.25f, 0.25f, 0.3f, 0.9f);
            button.colors = colors;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(buttonGo.transform);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmpText = textGo.AddComponent<TextMeshProUGUI>();
            tmpText.text = text;
            tmpText.fontSize = UITheme.FontSizeRegular.Normal;
            tmpText.color = UITheme.TextPrimary;
            tmpText.alignment = TextAlignmentOptions.Center;

            return buttonGo;
        }

        private static void CreateSectionHeader(Transform parent, string title)
        {
            var headerGo = new GameObject($"Section_{title}");
            headerGo.transform.SetParent(parent);

            var rectTransform = headerGo.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, 30);

            var layoutElement = headerGo.AddComponent<LayoutElement>();
            layoutElement.minHeight = 30;
            layoutElement.preferredHeight = 30;

            var text = headerGo.AddComponent<TextMeshProUGUI>();
            text.text = title;
            text.fontSize = UITheme.FontSizeRegular.Large;
            text.color = UITheme.AccentGreen;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.MidlineLeft;
        }

        private static SettingDropdown InstantiateDropdown(GameObject prefab, Transform parent, string label)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = $"Dropdown_{label.Replace(" ", "")}";
            go.transform.SetParent(parent);

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.minHeight = 60;
            layoutElement.preferredHeight = 60;

            var settingDropdown = go.GetComponent<SettingDropdown>();
            settingDropdown.Label = label;

            return settingDropdown;
        }

        private static SettingSlider InstantiateSlider(GameObject prefab, Transform parent, string label)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = $"Slider_{label.Replace(" ", "")}";
            go.transform.SetParent(parent);

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.minHeight = 60;
            layoutElement.preferredHeight = 60;

            var settingSlider = go.GetComponent<SettingSlider>();
            settingSlider.Label = label;

            return settingSlider;
        }

        private static SettingToggle InstantiateToggle(GameObject prefab, Transform parent, string label)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = $"Toggle_{label.Replace(" ", "").Replace("-", "")}";
            go.transform.SetParent(parent);

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.minHeight = 40;
            layoutElement.preferredHeight = 40;

            var settingToggle = go.GetComponent<SettingToggle>();
            settingToggle.Label = label;

            return settingToggle;
        }

        #endregion
    }
}
