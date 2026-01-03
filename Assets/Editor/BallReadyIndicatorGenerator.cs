// ABOUTME: Editor tool for creating the Ball Ready Indicator prefab.
// ABOUTME: Creates a prominent UI indicator showing device ready and ball detected status.

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using OpenRange.UI;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor tool for creating the Ball Ready Indicator prefab.
    /// </summary>
    public static class BallReadyIndicatorGenerator
    {
        private const string PrefabPath = "Assets/Prefabs/UI";

        #region Menu Items

        [MenuItem("OpenRange/Create Ball Ready Indicator Prefab")]
        public static void CreateBallReadyIndicatorPrefab()
        {
            EnsureDirectories();

            var indicatorGO = CreateBallReadyIndicator();

            string path = $"{PrefabPath}/BallReadyIndicator.prefab";
            PrefabUtility.SaveAsPrefabAsset(indicatorGO, path);
            Object.DestroyImmediate(indicatorGO);

            AssetDatabase.Refresh();
            Debug.Log($"BallReadyIndicatorGenerator: Created BallReadyIndicator.prefab at {path}");
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

        private static GameObject CreateBallReadyIndicator()
        {
            var indicatorGO = new GameObject("BallReadyIndicator");

            // RectTransform - top-center, below connection status
            var rectTransform = indicatorGO.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -60f); // Below connection status
            rectTransform.sizeDelta = new Vector2(200f, 60f);

            // Background panel
            var bgImage = indicatorGO.AddComponent<Image>();
            bgImage.color = UITheme.PanelBackground;

            // CanvasGroup for animations
            var canvasGroup = indicatorGO.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;

            // Horizontal layout
            var layoutGroup = indicatorGO.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = UITheme.Padding.Normal;
            layoutGroup.padding = new RectOffset(
                (int)UITheme.Padding.Normal,
                (int)UITheme.Padding.Normal,
                (int)UITheme.Padding.Small,
                (int)UITheme.Padding.Small
            );
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = true;

            // Create status icon (circle)
            var iconGO = new GameObject("StatusIcon");
            iconGO.transform.SetParent(indicatorGO.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(24f, 24f);
            var iconLayout = iconGO.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 24f;
            iconLayout.preferredHeight = 24f;
            iconLayout.minWidth = 24f;
            iconLayout.minHeight = 24f;

            var iconImage = iconGO.AddComponent<Image>();
            iconImage.color = BallReadyIndicator.DisconnectedColor;
            // Use a circular sprite if available, or just keep it square for now

            // Create status text
            var textGO = new GameObject("StatusText");
            textGO.transform.SetParent(indicatorGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            var textLayout = textGO.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 1f;
            textLayout.minWidth = 100f;

            var statusText = textGO.AddComponent<TextMeshProUGUI>();
            statusText.text = "Connect GC2";
            statusText.alignment = TextAlignmentOptions.MidlineLeft;
            statusText.color = UITheme.TextPrimary;
            statusText.fontSize = UITheme.FontSizeRegular.Large;
            statusText.fontStyle = FontStyles.Normal;

            // Add BallReadyIndicator component and wire up references
            var indicator = indicatorGO.AddComponent<BallReadyIndicator>();

            var so = new SerializedObject(indicator);
            so.FindProperty("_statusIcon").objectReferenceValue = iconImage;
            so.FindProperty("_statusText").objectReferenceValue = statusText;
            so.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
            so.ApplyModifiedPropertiesWithoutUndo();

            return indicatorGO;
        }

        #endregion
    }
}
