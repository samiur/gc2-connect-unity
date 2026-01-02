// ABOUTME: Editor tool for creating ClubDataPanel and indicator prefabs.
// ABOUTME: Creates properly configured UI components with TextMeshPro elements.

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using OpenRange.UI;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor tool for creating ClubDataPanel and indicator prefabs.
    /// </summary>
    public static class ClubDataPanelGenerator
    {
        private const string PrefabPath = "Assets/Prefabs/UI";
        private const string MaterialPath = "Assets/Materials/UI";

        #region Menu Items

        [MenuItem("OpenRange/Create Club Data Panel Prefab")]
        public static void CreateClubDataPanelPrefab()
        {
            EnsureDirectories();

            var panelGO = CreateClubDataPanel();

            string path = $"{PrefabPath}/ClubDataPanel.prefab";
            PrefabUtility.SaveAsPrefabAsset(panelGO, path);
            Object.DestroyImmediate(panelGO);

            AssetDatabase.Refresh();
            Debug.Log($"ClubDataPanelGenerator: Created ClubDataPanel.prefab at {path}");
        }

        [MenuItem("OpenRange/Create Swing Path Indicator Prefab")]
        public static void CreateSwingPathIndicatorPrefab()
        {
            EnsureDirectories();

            var indicatorGO = CreateSwingPathIndicator();

            string path = $"{PrefabPath}/SwingPathIndicator.prefab";
            PrefabUtility.SaveAsPrefabAsset(indicatorGO, path);
            Object.DestroyImmediate(indicatorGO);

            AssetDatabase.Refresh();
            Debug.Log($"ClubDataPanelGenerator: Created SwingPathIndicator.prefab at {path}");
        }

        [MenuItem("OpenRange/Create Attack Angle Indicator Prefab")]
        public static void CreateAttackAngleIndicatorPrefab()
        {
            EnsureDirectories();

            var indicatorGO = CreateAttackAngleIndicator();

            string path = $"{PrefabPath}/AttackAngleIndicator.prefab";
            PrefabUtility.SaveAsPrefabAsset(indicatorGO, path);
            Object.DestroyImmediate(indicatorGO);

            AssetDatabase.Refresh();
            Debug.Log($"ClubDataPanelGenerator: Created AttackAngleIndicator.prefab at {path}");
        }

        [MenuItem("OpenRange/Create All Club Data Panel Prefabs")]
        public static void CreateAllPrefabs()
        {
            CreateSwingPathIndicatorPrefab();
            CreateAttackAngleIndicatorPrefab();
            CreateClubDataPanelPrefab();
            Debug.Log("ClubDataPanelGenerator: Created all club data panel prefabs");
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
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            if (!AssetDatabase.IsValidFolder(MaterialPath))
            {
                AssetDatabase.CreateFolder("Assets/Materials", "UI");
            }
        }

        private static GameObject CreateDataTile(string name)
        {
            var tileGO = new GameObject(name);

            // RectTransform
            var rectTransform = tileGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(90, 60);

            // Background
            var bgImage = tileGO.AddComponent<Image>();
            bgImage.color = UITheme.PanelBackground;

            // CanvasGroup for animations
            var canvasGroup = tileGO.AddComponent<CanvasGroup>();

            // Layout element
            var layoutElement = tileGO.AddComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1f;
            layoutElement.minWidth = 60f;
            layoutElement.preferredHeight = 60f;

            // Create Label (top)
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(tileGO.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0, -UITheme.Padding.Tiny);
            labelRect.sizeDelta = new Vector2(0, 14);

            var labelText = labelGO.AddComponent<TextMeshProUGUI>();
            labelText.text = "LABEL";
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = UITheme.TextSecondary;
            labelText.fontSize = UITheme.FontSizeRegular.Small;
            labelText.fontStyle = FontStyles.Normal;

            // Create Value (center)
            var valueGO = new GameObject("Value");
            valueGO.transform.SetParent(tileGO.transform, false);
            var valueRect = valueGO.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0, 0.25f);
            valueRect.anchorMax = new Vector2(1, 0.8f);
            valueRect.offsetMin = new Vector2(UITheme.Padding.Tiny, 0);
            valueRect.offsetMax = new Vector2(-UITheme.Padding.Tiny, 0);

            var valueText = valueGO.AddComponent<TextMeshProUGUI>();
            valueText.text = "-";
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.color = UITheme.TextPrimary;
            valueText.fontSize = UITheme.FontSizeRegular.Large;
            valueText.fontStyle = FontStyles.Bold;
            valueText.enableAutoSizing = true;
            valueText.fontSizeMin = 10;
            valueText.fontSizeMax = UITheme.FontSizeRegular.Large;

            // Create Unit (bottom)
            var unitGO = new GameObject("Unit");
            unitGO.transform.SetParent(tileGO.transform, false);
            var unitRect = unitGO.AddComponent<RectTransform>();
            unitRect.anchorMin = new Vector2(0, 0);
            unitRect.anchorMax = new Vector2(1, 0);
            unitRect.pivot = new Vector2(0.5f, 0f);
            unitRect.anchoredPosition = new Vector2(0, UITheme.Padding.Tiny);
            unitRect.sizeDelta = new Vector2(0, 12);

            var unitText = unitGO.AddComponent<TextMeshProUGUI>();
            unitText.text = "unit";
            unitText.alignment = TextAlignmentOptions.Center;
            unitText.color = UITheme.TextSecondary;
            unitText.fontSize = UITheme.FontSizeRegular.Small;
            unitText.fontStyle = FontStyles.Normal;

            // Add DataTile component and wire up references
            var dataTile = tileGO.AddComponent<DataTile>();

            var so = new SerializedObject(dataTile);
            so.FindProperty("_labelText").objectReferenceValue = labelText;
            so.FindProperty("_valueText").objectReferenceValue = valueText;
            so.FindProperty("_unitText").objectReferenceValue = unitText;
            so.FindProperty("_backgroundImage").objectReferenceValue = bgImage;
            so.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
            so.ApplyModifiedPropertiesWithoutUndo();

            return tileGO;
        }

        private static GameObject CreateSwingPathIndicator()
        {
            var indicatorGO = new GameObject("SwingPathIndicator");

            // RectTransform
            var rectTransform = indicatorGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100, 100);

            // Background circle
            var bgImage = indicatorGO.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

            // Create path arrow
            var pathArrowGO = new GameObject("PathArrow");
            pathArrowGO.transform.SetParent(indicatorGO.transform, false);
            var pathArrowRect = pathArrowGO.AddComponent<RectTransform>();
            pathArrowRect.sizeDelta = new Vector2(60, 20);
            pathArrowRect.anchoredPosition = Vector2.zero;

            var pathArrowImage = pathArrowGO.AddComponent<Image>();
            pathArrowImage.color = SwingPathIndicator.NeutralColor;

            // Create face angle line
            var faceLineGO = new GameObject("FaceAngleLine");
            faceLineGO.transform.SetParent(indicatorGO.transform, false);
            var faceLineRect = faceLineGO.AddComponent<RectTransform>();
            faceLineRect.sizeDelta = new Vector2(4, 50);
            faceLineRect.anchoredPosition = Vector2.zero;

            var faceLineImage = faceLineGO.AddComponent<Image>();
            faceLineImage.color = UITheme.TextPrimary;

            // Create target line (horizontal reference)
            var targetLineGO = new GameObject("TargetLine");
            targetLineGO.transform.SetParent(indicatorGO.transform, false);
            var targetLineRect = targetLineGO.AddComponent<RectTransform>();
            targetLineRect.sizeDelta = new Vector2(80, 2);
            targetLineRect.anchoredPosition = Vector2.zero;

            var targetLineImage = targetLineGO.AddComponent<Image>();
            targetLineImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

            // Add SwingPathIndicator component and wire up references
            var indicator = indicatorGO.AddComponent<SwingPathIndicator>();

            var so = new SerializedObject(indicator);
            so.FindProperty("_pathArrow").objectReferenceValue = pathArrowImage;
            so.FindProperty("_faceAngleLine").objectReferenceValue = faceLineImage;
            so.ApplyModifiedPropertiesWithoutUndo();

            return indicatorGO;
        }

        private static GameObject CreateAttackAngleIndicator()
        {
            var indicatorGO = new GameObject("AttackAngleIndicator");

            // RectTransform
            var rectTransform = indicatorGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(80, 60);

            // Background
            var bgImage = indicatorGO.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

            // Create ground line (horizontal reference)
            var groundLineGO = new GameObject("GroundLine");
            groundLineGO.transform.SetParent(indicatorGO.transform, false);
            var groundLineRect = groundLineGO.AddComponent<RectTransform>();
            groundLineRect.sizeDelta = new Vector2(70, 2);
            groundLineRect.anchoredPosition = new Vector2(0, -15);

            var groundLineImage = groundLineGO.AddComponent<Image>();
            groundLineImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

            // Create angle arrow
            var arrowGO = new GameObject("AngleArrow");
            arrowGO.transform.SetParent(indicatorGO.transform, false);
            var arrowRect = arrowGO.AddComponent<RectTransform>();
            arrowRect.sizeDelta = new Vector2(50, 8);
            arrowRect.anchoredPosition = new Vector2(0, -15);
            arrowRect.pivot = new Vector2(0, 0.5f);

            var arrowImage = arrowGO.AddComponent<Image>();
            arrowImage.color = AttackAngleIndicator.NeutralColor;

            // Create ball position indicator
            var ballGO = new GameObject("BallIndicator");
            ballGO.transform.SetParent(indicatorGO.transform, false);
            var ballRect = ballGO.AddComponent<RectTransform>();
            ballRect.sizeDelta = new Vector2(10, 10);
            ballRect.anchoredPosition = new Vector2(0, -15);

            var ballImage = ballGO.AddComponent<Image>();
            ballImage.color = UITheme.TextPrimary;

            // Add AttackAngleIndicator component and wire up references
            var indicator = indicatorGO.AddComponent<AttackAngleIndicator>();

            var so = new SerializedObject(indicator);
            so.FindProperty("_angleArrow").objectReferenceValue = arrowImage;
            so.ApplyModifiedPropertiesWithoutUndo();

            return indicatorGO;
        }

        private static GameObject CreateClubDataPanel()
        {
            var panelGO = new GameObject("ClubDataPanel");

            // RectTransform - left-side anchored
            var rectTransform = panelGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0.3f);
            rectTransform.anchorMax = new Vector2(0, 0.7f);
            rectTransform.pivot = new Vector2(0, 0.5f);
            rectTransform.anchoredPosition = new Vector2(UITheme.Margin.Normal, 0);
            rectTransform.sizeDelta = new Vector2(160, 0);

            // Background
            var bgImage = panelGO.AddComponent<Image>();
            bgImage.color = new Color(
                UITheme.PanelBackground.r,
                UITheme.PanelBackground.g,
                UITheme.PanelBackground.b,
                0.9f
            );

            // CanvasGroup for animations
            var canvasGroup = panelGO.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f; // Start visible

            // Vertical Layout Group
            var layoutGroup = panelGO.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = UITheme.Padding.Small;
            layoutGroup.padding = new RectOffset(
                (int)UITheme.Padding.Small,
                (int)UITheme.Padding.Small,
                (int)UITheme.Padding.Normal,
                (int)UITheme.Padding.Normal
            );
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            // Content size fitter
            var sizeFitter = panelGO.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Create header
            var headerGO = new GameObject("Header");
            headerGO.transform.SetParent(panelGO.transform, false);
            var headerRect = headerGO.AddComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(0, 24);

            var headerLayout = headerGO.AddComponent<LayoutElement>();
            headerLayout.preferredHeight = 24;

            var headerText = headerGO.AddComponent<TextMeshProUGUI>();
            headerText.text = "CLUB DATA";
            headerText.alignment = TextAlignmentOptions.Center;
            headerText.color = UITheme.TextPrimary;
            headerText.fontSize = UITheme.FontSizeRegular.Normal;
            headerText.fontStyle = FontStyles.Bold;

            // Create tiles for HMT data
            string[] labels = { "CLUB SPEED", "PATH", "ATTACK", "FACE", "LOFT" };
            string[] units = { "mph", "deg", "deg", "deg", "deg" };
            string[] names = { "ClubSpeedTile", "PathTile", "AttackAngleTile", "FaceToTargetTile", "DynamicLoftTile" };

            DataTile[] tiles = new DataTile[5];

            for (int i = 0; i < 5; i++)
            {
                var tileGO = CreateDataTile(names[i]);
                tileGO.transform.SetParent(panelGO.transform, false);

                var tile = tileGO.GetComponent<DataTile>();
                tile.Label = labels[i];
                tile.Unit = units[i];

                tiles[i] = tile;
            }

            // Create indicators container
            var indicatorsGO = new GameObject("Indicators");
            indicatorsGO.transform.SetParent(panelGO.transform, false);
            var indicatorsRect = indicatorsGO.AddComponent<RectTransform>();
            indicatorsRect.sizeDelta = new Vector2(0, 80);

            var indicatorsLayout = indicatorsGO.AddComponent<LayoutElement>();
            indicatorsLayout.preferredHeight = 80;

            var indicatorsHLayout = indicatorsGO.AddComponent<HorizontalLayoutGroup>();
            indicatorsHLayout.spacing = UITheme.Padding.Small;
            indicatorsHLayout.childAlignment = TextAnchor.MiddleCenter;
            indicatorsHLayout.childControlWidth = true;
            indicatorsHLayout.childControlHeight = true;
            indicatorsHLayout.childForceExpandWidth = true;
            indicatorsHLayout.childForceExpandHeight = true;

            // Create swing path indicator
            var pathIndicatorGO = CreateSwingPathIndicator();
            pathIndicatorGO.transform.SetParent(indicatorsGO.transform, false);
            var pathIndicator = pathIndicatorGO.GetComponent<SwingPathIndicator>();

            // Create attack angle indicator
            var attackIndicatorGO = CreateAttackAngleIndicator();
            attackIndicatorGO.transform.SetParent(indicatorsGO.transform, false);
            var attackIndicator = attackIndicatorGO.GetComponent<AttackAngleIndicator>();

            // Add ClubDataPanel component and wire up references
            var clubDataPanel = panelGO.AddComponent<ClubDataPanel>();

            var so = new SerializedObject(clubDataPanel);
            so.FindProperty("_clubSpeedTile").objectReferenceValue = tiles[0];
            so.FindProperty("_pathTile").objectReferenceValue = tiles[1];
            so.FindProperty("_attackAngleTile").objectReferenceValue = tiles[2];
            so.FindProperty("_faceToTargetTile").objectReferenceValue = tiles[3];
            so.FindProperty("_dynamicLoftTile").objectReferenceValue = tiles[4];
            so.FindProperty("_swingPathIndicator").objectReferenceValue = pathIndicator;
            so.FindProperty("_attackAngleIndicator").objectReferenceValue = attackIndicator;
            so.FindProperty("_backgroundImage").objectReferenceValue = bgImage;
            so.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
            so.FindProperty("_headerText").objectReferenceValue = headerText;
            so.ApplyModifiedPropertiesWithoutUndo();

            return panelGO;
        }

        #endregion
    }
}
