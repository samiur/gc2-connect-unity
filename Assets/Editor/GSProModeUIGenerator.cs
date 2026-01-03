// ABOUTME: Editor tool for creating GSPro Mode UI prefab.
// ABOUTME: Generates mode toggle, connection status, and device readiness indicators.

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using OpenRange.UI;

namespace OpenRange.Editor
{
    /// <summary>
    /// Editor tool for creating GSPro Mode UI prefab.
    /// </summary>
    public static class GSProModeUIGenerator
    {
        private const string PrefabPath = "Assets/Prefabs/UI/GSProModeUI.prefab";
        private const string PrefabFolder = "Assets/Prefabs/UI";

        [MenuItem("OpenRange/Create GSPro Mode UI Prefab")]
        public static void CreatePrefab()
        {
            EnsureFolderExists(PrefabFolder);

            var root = CreateRootObject();

            try
            {
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"Created prefab: {PrefabPath}");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            AssetDatabase.Refresh();
        }

        private static GameObject CreateRootObject()
        {
            var root = new GameObject("GSProModeUI");
            var rectTransform = root.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(320f, 280f);

            var component = root.AddComponent<GSProModeUI>();

            var background = root.AddComponent<Image>();
            background.color = new Color(0.1f, 0.1f, 0.2f, 0.9f);

            var verticalLayout = root.AddComponent<VerticalLayoutGroup>();
            verticalLayout.padding = new RectOffset(16, 16, 16, 16);
            verticalLayout.spacing = 12f;
            verticalLayout.childAlignment = TextAnchor.UpperCenter;
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = false;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;

            var fitter = root.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Create child elements
            var header = CreateHeader(root.transform);
            var modeRow = CreateModeToggleRow(root.transform);
            var connectionRow = CreateConnectionRow(root.transform);
            var readinessRow = CreateReadinessRow(root.transform);
            var configPanel = CreateConfigPanel(root.transform);

            // Wire up references
            WireReferences(component, modeRow, connectionRow, readinessRow, configPanel);

            return root;
        }

        private static GameObject CreateHeader(Transform parent)
        {
            var header = new GameObject("Header");
            header.transform.SetParent(parent, false);

            var text = header.AddComponent<TextMeshProUGUI>();
            text.text = "GSPro Mode";
            text.fontSize = 20;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;

            var layout = header.AddComponent<LayoutElement>();
            layout.preferredHeight = 30f;

            return header;
        }

        private static ModeToggleRow CreateModeToggleRow(Transform parent)
        {
            var row = new GameObject("ModeRow");
            row.transform.SetParent(parent, false);

            var horizontal = row.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 10f;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = false;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;

            var layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = 36f;

            // Mode label
            var labelObj = new GameObject("ModeLabel");
            labelObj.transform.SetParent(row.transform, false);

            var label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = "Open Range Mode";
            label.fontSize = 16;
            label.color = Color.white;

            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 180f;

            // Toggle
            var toggleObj = new GameObject("ModeToggle");
            toggleObj.transform.SetParent(row.transform, false);

            var toggleBg = new GameObject("Background");
            toggleBg.transform.SetParent(toggleObj.transform, false);
            var bgImage = toggleBg.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f);
            var bgRect = toggleBg.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(50f, 26f);

            var checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(toggleBg.transform, false);
            var checkImage = checkmark.AddComponent<Image>();
            checkImage.color = new Color(0.2f, 0.7f, 0.2f);
            var checkRect = checkmark.GetComponent<RectTransform>();
            checkRect.sizeDelta = new Vector2(20f, 20f);

            var toggle = toggleObj.AddComponent<Toggle>();
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = false;

            var toggleLayout = toggleObj.AddComponent<LayoutElement>();
            toggleLayout.preferredWidth = 50f;
            toggleLayout.preferredHeight = 26f;

            return new ModeToggleRow { Toggle = toggle, Label = label };
        }

        private static ConnectionRow CreateConnectionRow(Transform parent)
        {
            var row = new GameObject("ConnectionRow");
            row.transform.SetParent(parent, false);

            var horizontal = row.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 8f;
            horizontal.childAlignment = TextAnchor.MiddleLeft;
            horizontal.childControlWidth = false;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;

            var layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = 36f;

            // Indicator
            var indicatorObj = new GameObject("Indicator");
            indicatorObj.transform.SetParent(row.transform, false);
            var indicator = indicatorObj.AddComponent<Image>();
            indicator.color = new Color(0.8f, 0.2f, 0.2f);
            var indicatorLayout = indicatorObj.AddComponent<LayoutElement>();
            indicatorLayout.preferredWidth = 16f;
            indicatorLayout.preferredHeight = 16f;

            // Text
            var textObj = new GameObject("ConnectionText");
            textObj.transform.SetParent(row.transform, false);
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Disconnected";
            text.fontSize = 14;
            text.color = Color.white;
            var textLayout = textObj.AddComponent<LayoutElement>();
            textLayout.preferredWidth = 120f;

            // Button
            var buttonObj = new GameObject("ConnectButton");
            buttonObj.transform.SetParent(row.transform, false);
            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.4f, 0.6f);
            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            var buttonLayout = buttonObj.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 90f;
            buttonLayout.preferredHeight = 30f;

            var buttonTextObj = new GameObject("Text");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            var buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = "Connect";
            buttonText.fontSize = 14;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            var buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.sizeDelta = Vector2.zero;

            return new ConnectionRow
            {
                Indicator = indicator,
                Text = text,
                Button = button,
                ButtonText = buttonText
            };
        }

        private static ReadinessRow CreateReadinessRow(Transform parent)
        {
            var row = new GameObject("ReadinessRow");
            row.transform.SetParent(parent, false);

            var horizontal = row.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 16f;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = false;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;

            var layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = 36f;

            // Ready indicator
            var readyGroup = CreateIndicatorGroup(row.transform, "Ready", "Not Ready");

            // Ball indicator
            var ballGroup = CreateIndicatorGroup(row.transform, "Ball", "No Ball");

            return new ReadinessRow
            {
                ReadyIndicator = readyGroup.Indicator,
                ReadyText = readyGroup.Text,
                BallIndicator = ballGroup.Indicator,
                BallText = ballGroup.Text
            };
        }

        private static (Image Indicator, TextMeshProUGUI Text) CreateIndicatorGroup(
            Transform parent, string label, string defaultText)
        {
            var group = new GameObject(label + "Group");
            group.transform.SetParent(parent, false);

            var horizontal = group.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 4f;
            horizontal.childAlignment = TextAnchor.MiddleLeft;
            horizontal.childControlWidth = false;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;

            var layout = group.AddComponent<LayoutElement>();
            layout.preferredWidth = 100f;

            // Indicator
            var indicatorObj = new GameObject("Indicator");
            indicatorObj.transform.SetParent(group.transform, false);
            var indicator = indicatorObj.AddComponent<Image>();
            indicator.color = new Color(0.5f, 0.5f, 0.5f);
            var indicatorLayout = indicatorObj.AddComponent<LayoutElement>();
            indicatorLayout.preferredWidth = 12f;
            indicatorLayout.preferredHeight = 12f;

            // Text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(group.transform, false);
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = defaultText;
            text.fontSize = 12;
            text.color = Color.white;
            var textLayout = textObj.AddComponent<LayoutElement>();
            textLayout.preferredWidth = 80f;

            return (indicator, text);
        }

        private static ConfigPanel CreateConfigPanel(Transform parent)
        {
            var panel = new GameObject("ConfigPanel");
            panel.transform.SetParent(parent, false);

            var vertical = panel.AddComponent<VerticalLayoutGroup>();
            vertical.spacing = 8f;
            vertical.childAlignment = TextAnchor.UpperCenter;
            vertical.childControlWidth = true;
            vertical.childControlHeight = false;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;

            var layout = panel.AddComponent<LayoutElement>();
            layout.preferredHeight = 80f;

            // Host row
            var hostRow = CreateInputRow(panel.transform, "Host:", "127.0.0.1");
            var hostInput = hostRow.GetComponentInChildren<TMP_InputField>();

            // Port row
            var portRow = CreateInputRow(panel.transform, "Port:", "921");
            var portInput = portRow.GetComponentInChildren<TMP_InputField>();

            return new ConfigPanel
            {
                Panel = panel,
                HostInput = hostInput,
                PortInput = portInput
            };
        }

        private static GameObject CreateInputRow(Transform parent, string label, string defaultValue)
        {
            var row = new GameObject(label.Replace(":", "") + "Row");
            row.transform.SetParent(parent, false);

            var horizontal = row.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 8f;
            horizontal.childAlignment = TextAnchor.MiddleLeft;
            horizontal.childControlWidth = false;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;

            var layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = 30f;

            // Label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(row.transform, false);
            var labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 14;
            labelText.color = Color.white;
            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 50f;

            // Input field
            var inputObj = new GameObject("InputField");
            inputObj.transform.SetParent(row.transform, false);

            var inputBg = inputObj.AddComponent<Image>();
            inputBg.color = new Color(0.2f, 0.2f, 0.25f);

            var inputLayout = inputObj.AddComponent<LayoutElement>();
            inputLayout.preferredWidth = 200f;
            inputLayout.preferredHeight = 26f;

            var textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputObj.transform, false);
            var textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = new Vector2(0.02f, 0.1f);
            textAreaRect.anchorMax = new Vector2(0.98f, 0.9f);
            textAreaRect.sizeDelta = Vector2.zero;

            var textComponent = new GameObject("Text");
            textComponent.transform.SetParent(textArea.transform, false);
            var text = textComponent.AddComponent<TextMeshProUGUI>();
            text.fontSize = 14;
            text.color = Color.white;
            var textRect = textComponent.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(textArea.transform, false);
            var placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
            placeholderText.text = defaultValue;
            placeholderText.fontSize = 14;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f);
            placeholderText.fontStyle = FontStyles.Italic;
            var placeholderRect = placeholder.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;

            var inputField = inputObj.AddComponent<TMP_InputField>();
            inputField.textViewport = textAreaRect;
            inputField.textComponent = text;
            inputField.placeholder = placeholderText;
            inputField.text = defaultValue;

            return row;
        }

        private static void WireReferences(
            GSProModeUI component,
            ModeToggleRow modeRow,
            ConnectionRow connectionRow,
            ReadinessRow readinessRow,
            ConfigPanel configPanel)
        {
            var so = new SerializedObject(component);

            so.FindProperty("_modeToggle").objectReferenceValue = modeRow.Toggle;
            so.FindProperty("_modeLabel").objectReferenceValue = modeRow.Label;
            so.FindProperty("_connectionIndicator").objectReferenceValue = connectionRow.Indicator;
            so.FindProperty("_connectionText").objectReferenceValue = connectionRow.Text;
            so.FindProperty("_connectButton").objectReferenceValue = connectionRow.Button;
            so.FindProperty("_connectButtonText").objectReferenceValue = connectionRow.ButtonText;
            so.FindProperty("_readyIndicator").objectReferenceValue = readinessRow.ReadyIndicator;
            so.FindProperty("_readyText").objectReferenceValue = readinessRow.ReadyText;
            so.FindProperty("_ballIndicator").objectReferenceValue = readinessRow.BallIndicator;
            so.FindProperty("_ballText").objectReferenceValue = readinessRow.BallText;
            so.FindProperty("_hostInput").objectReferenceValue = configPanel.HostInput;
            so.FindProperty("_portInput").objectReferenceValue = configPanel.PortInput;
            so.FindProperty("_configPanel").objectReferenceValue = configPanel.Panel;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolderExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parts = path.Split('/');
                var current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    var next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    current = next;
                }
            }
        }

        private struct ModeToggleRow
        {
            public Toggle Toggle;
            public TextMeshProUGUI Label;
        }

        private struct ConnectionRow
        {
            public Image Indicator;
            public TextMeshProUGUI Text;
            public Button Button;
            public TextMeshProUGUI ButtonText;
        }

        private struct ReadinessRow
        {
            public Image ReadyIndicator;
            public TextMeshProUGUI ReadyText;
            public Image BallIndicator;
            public TextMeshProUGUI BallText;
        }

        private struct ConfigPanel
        {
            public GameObject Panel;
            public TMP_InputField HostInput;
            public TMP_InputField PortInput;
        }
    }
}
