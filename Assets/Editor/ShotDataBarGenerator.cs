// ABOUTME: Editor tool for creating ShotDataBar and DataTile prefabs.
// ABOUTME: Creates properly configured UI components with TextMeshPro elements.

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using OpenRange.UI;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor tool for creating ShotDataBar and DataTile prefabs.
    /// </summary>
    public static class ShotDataBarGenerator
    {
        private const string PrefabPath = "Assets/Prefabs/UI";

        #region Menu Items

        [MenuItem("OpenRange/Create Data Tile Prefab")]
        public static void CreateDataTilePrefab()
        {
            EnsureDirectories();

            var tileGO = CreateDataTile("DataTile");

            string path = $"{PrefabPath}/DataTile.prefab";
            PrefabUtility.SaveAsPrefabAsset(tileGO, path);
            Object.DestroyImmediate(tileGO);

            AssetDatabase.Refresh();
            Debug.Log($"ShotDataBarGenerator: Created DataTile.prefab at {path}");
        }

        [MenuItem("OpenRange/Create Shot Data Bar Prefab")]
        public static void CreateShotDataBarPrefab()
        {
            EnsureDirectories();

            var barGO = CreateShotDataBar();

            string path = $"{PrefabPath}/ShotDataBar.prefab";
            PrefabUtility.SaveAsPrefabAsset(barGO, path);
            Object.DestroyImmediate(barGO);

            AssetDatabase.Refresh();
            Debug.Log($"ShotDataBarGenerator: Created ShotDataBar.prefab at {path}");
        }

        [MenuItem("OpenRange/Create All Shot Data Bar Prefabs")]
        public static void CreateAllPrefabs()
        {
            CreateDataTilePrefab();
            CreateShotDataBarPrefab();
            Debug.Log("ShotDataBarGenerator: Created all shot data bar prefabs");
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

        private static GameObject CreateDataTile(string name)
        {
            var tileGO = new GameObject(name);

            // RectTransform
            var rectTransform = tileGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100, 80);

            // Background
            var bgImage = tileGO.AddComponent<Image>();
            bgImage.color = UITheme.PanelBackground;

            // CanvasGroup for animations
            var canvasGroup = tileGO.AddComponent<CanvasGroup>();

            // Layout element
            var layoutElement = tileGO.AddComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1f;
            layoutElement.minWidth = 60f;

            // Create Label (top)
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(tileGO.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0, -UITheme.Padding.Tiny);
            labelRect.sizeDelta = new Vector2(0, 16);

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
            valueRect.anchorMin = new Vector2(0, 0.3f);
            valueRect.anchorMax = new Vector2(1, 0.8f);
            valueRect.offsetMin = new Vector2(UITheme.Padding.Tiny, 0);
            valueRect.offsetMax = new Vector2(-UITheme.Padding.Tiny, 0);

            var valueText = valueGO.AddComponent<TextMeshProUGUI>();
            valueText.text = "-";
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.color = UITheme.TextPrimary;
            valueText.fontSize = UITheme.FontSizeRegular.DataValue;
            valueText.fontStyle = FontStyles.Bold;
            valueText.enableAutoSizing = true;
            valueText.fontSizeMin = 12;
            valueText.fontSizeMax = UITheme.FontSizeRegular.DataValue;

            // Create Unit (bottom)
            var unitGO = new GameObject("Unit");
            unitGO.transform.SetParent(tileGO.transform, false);
            var unitRect = unitGO.AddComponent<RectTransform>();
            unitRect.anchorMin = new Vector2(0, 0);
            unitRect.anchorMax = new Vector2(1, 0);
            unitRect.pivot = new Vector2(0.5f, 0f);
            unitRect.anchoredPosition = new Vector2(0, UITheme.Padding.Tiny);
            unitRect.sizeDelta = new Vector2(0, 14);

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

        private static GameObject CreateShotDataBar()
        {
            var barGO = new GameObject("ShotDataBar");

            // RectTransform - bottom anchored, full width
            var rectTransform = barGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 0);
            rectTransform.pivot = new Vector2(0.5f, 0);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(0, 100);

            // Background
            var bgImage = barGO.AddComponent<Image>();
            bgImage.color = new Color(
                UITheme.PanelBackground.r,
                UITheme.PanelBackground.g,
                UITheme.PanelBackground.b,
                0.9f
            );

            // CanvasGroup for animations
            var canvasGroup = barGO.AddComponent<CanvasGroup>();

            // Horizontal Layout Group
            var layoutGroup = barGO.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = UITheme.Padding.Small;
            layoutGroup.padding = new RectOffset(
                (int)UITheme.Padding.Normal,
                (int)UITheme.Padding.Normal,
                (int)UITheme.Padding.Small,
                (int)UITheme.Padding.Small
            );
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = true;

            // Create all 10 data tiles
            string[] labels = { "BALL SPEED", "DIRECTION", "ANGLE", "BACK SPIN", "SIDE SPIN",
                               "APEX", "OFFLINE", "CARRY", "RUN", "TOTAL" };
            string[] units = { "mph", "deg", "deg", "rpm", "rpm", "yd", "yd", "yd", "yd", "yd" };
            string[] names = { "BallSpeedTile", "DirectionTile", "AngleTile", "BackSpinTile", "SideSpinTile",
                              "ApexTile", "OfflineTile", "CarryTile", "RunTile", "TotalTile" };

            DataTile[] tiles = new DataTile[10];

            for (int i = 0; i < 10; i++)
            {
                var tileGO = CreateDataTile(names[i]);
                tileGO.transform.SetParent(barGO.transform, false);

                var tile = tileGO.GetComponent<DataTile>();
                tile.Label = labels[i];
                tile.Unit = units[i];

                // Highlight Total tile in red
                if (i == 9)
                {
                    tile.IsHighlighted = true;
                    tile.SetHighlightColor(UITheme.TotalRed);
                }

                tiles[i] = tile;
            }

            // Add ShotDataBar component and wire up references
            var shotDataBar = barGO.AddComponent<ShotDataBar>();

            var so = new SerializedObject(shotDataBar);
            so.FindProperty("_ballSpeedTile").objectReferenceValue = tiles[0];
            so.FindProperty("_directionTile").objectReferenceValue = tiles[1];
            so.FindProperty("_angleTile").objectReferenceValue = tiles[2];
            so.FindProperty("_backSpinTile").objectReferenceValue = tiles[3];
            so.FindProperty("_sideSpinTile").objectReferenceValue = tiles[4];
            so.FindProperty("_apexTile").objectReferenceValue = tiles[5];
            so.FindProperty("_offlineTile").objectReferenceValue = tiles[6];
            so.FindProperty("_carryTile").objectReferenceValue = tiles[7];
            so.FindProperty("_runTile").objectReferenceValue = tiles[8];
            so.FindProperty("_totalTile").objectReferenceValue = tiles[9];
            so.FindProperty("_layoutGroup").objectReferenceValue = layoutGroup;
            so.FindProperty("_backgroundImage").objectReferenceValue = bgImage;
            so.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
            so.ApplyModifiedPropertiesWithoutUndo();

            return barGO;
        }

        #endregion
    }
}
