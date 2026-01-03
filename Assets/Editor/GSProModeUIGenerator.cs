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

        // Layout constants
        private const float PanelMinWidth = 280f;
        private const float PanelPadding = 12f;
        private const float SectionSpacing = 12f;
        private const float ItemSpacing = 8f;
        private const float LedIndicatorSize = 14f;
        private const float ToggleWidth = 50f;
        private const float ToggleHeight = 26f;
        private const float ButtonMinWidth = 100f;
        private const float ButtonHeight = 32f;
        private const float HostInputWidth = 130f;
        private const float PortInputWidth = 65f;
        private const float InputHeight = 28f;
        private const float LabelWidth = 45f;
        private const float RowHeight = 32f;

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
            rectTransform.sizeDelta = new Vector2(PanelMinWidth, 320f);

            var component = root.AddComponent<GSProModeUI>();

            var background = root.AddComponent<Image>();
            background.color = new Color(0.1f, 0.1f, 0.2f, 0.92f);

            var verticalLayout = root.AddComponent<VerticalLayoutGroup>();
            verticalLayout.padding = new RectOffset(
                (int)PanelPadding, (int)PanelPadding,
                (int)PanelPadding, (int)PanelPadding);
            verticalLayout.spacing = SectionSpacing;
            verticalLayout.childAlignment = TextAnchor.UpperCenter;
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = false;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;

            var fitter = root.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var minSize = root.AddComponent<LayoutElement>();
            minSize.minWidth = PanelMinWidth;

            // Create child elements
            var header = CreateHeader(root.transform);
            var modeRow = CreateModeToggleRow(root.transform);
            var divider1 = CreateDivider(root.transform);
            var connectionRow = CreateConnectionRow(root.transform);
            var readinessRow = CreateReadinessRow(root.transform);
            var divider2 = CreateDivider(root.transform);
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
            text.fontSize = 18;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;

            var layout = header.AddComponent<LayoutElement>();
            layout.preferredHeight = 26f;

            return header;
        }

        private static GameObject CreateDivider(Transform parent)
        {
            var divider = new GameObject("Divider");
            divider.transform.SetParent(parent, false);

            var image = divider.AddComponent<Image>();
            image.color = new Color(0.3f, 0.3f, 0.4f, 0.6f);

            var layout = divider.AddComponent<LayoutElement>();
            layout.preferredHeight = 1f;
            layout.flexibleWidth = 1f;

            return divider;
        }

        private static ModeToggleRow CreateModeToggleRow(Transform parent)
        {
            var row = new GameObject("ModeRow");
            row.transform.SetParent(parent, false);

            var horizontal = row.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = ItemSpacing;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = false;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;
            horizontal.padding = new RectOffset(0, 0, 2, 2);

            var layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = RowHeight;

            // Toggle on left
            var toggleObj = CreateToggle(row.transform);

            // Mode label on right (shows current mode status)
            var labelObj = new GameObject("ModeLabel");
            labelObj.transform.SetParent(row.transform, false);

            var label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = "Open Range Mode";
            label.fontSize = 14;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Left;

            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
            labelLayout.preferredHeight = RowHeight;

            return new ModeToggleRow { Toggle = toggleObj.Toggle, Label = label };
        }

        private static (Toggle Toggle, GameObject Object) CreateToggle(Transform parent)
        {
            var toggleObj = new GameObject("ModeToggle");
            toggleObj.transform.SetParent(parent, false);

            var toggleRect = toggleObj.AddComponent<RectTransform>();
            toggleRect.sizeDelta = new Vector2(ToggleWidth, ToggleHeight);

            // Background track
            var toggleBg = new GameObject("Background");
            toggleBg.transform.SetParent(toggleObj.transform, false);
            var bgImage = toggleBg.AddComponent<Image>();
            bgImage.color = new Color(0.25f, 0.25f, 0.3f);
            var bgRect = toggleBg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Add rounded corners effect (simulated with padding)
            bgRect.offsetMin = new Vector2(2f, 2f);
            bgRect.offsetMax = new Vector2(-2f, -2f);

            // Checkmark/knob area
            var checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(toggleBg.transform, false);
            var checkImage = checkmark.AddComponent<Image>();
            checkImage.color = new Color(0.2f, 0.7f, 0.3f);
            var checkRect = checkmark.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.5f, 0f);
            checkRect.anchorMax = new Vector2(1f, 1f);
            checkRect.sizeDelta = Vector2.zero;
            checkRect.offsetMin = new Vector2(2f, 2f);
            checkRect.offsetMax = new Vector2(-2f, -2f);

            var toggle = toggleObj.AddComponent<Toggle>();
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = false;

            var toggleLayout = toggleObj.AddComponent<LayoutElement>();
            toggleLayout.preferredWidth = ToggleWidth;
            toggleLayout.preferredHeight = ToggleHeight;
            toggleLayout.minWidth = ToggleWidth;

            return (toggle, toggleObj);
        }

        private static ConnectionRow CreateConnectionRow(Transform parent)
        {
            var row = new GameObject("ConnectionRow");
            row.transform.SetParent(parent, false);

            var horizontal = row.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = ItemSpacing;
            horizontal.childAlignment = TextAnchor.MiddleLeft;
            horizontal.childControlWidth = false;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;

            var layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = RowHeight;

            // LED Indicator (small circle)
            var indicatorObj = new GameObject("Indicator");
            indicatorObj.transform.SetParent(row.transform, false);
            var indicator = indicatorObj.AddComponent<Image>();
            indicator.color = new Color(0.8f, 0.2f, 0.2f);
            var indicatorLayout = indicatorObj.AddComponent<LayoutElement>();
            indicatorLayout.preferredWidth = LedIndicatorSize;
            indicatorLayout.preferredHeight = LedIndicatorSize;
            indicatorLayout.minWidth = LedIndicatorSize;

            // Connection status text
            var textObj = new GameObject("ConnectionText");
            textObj.transform.SetParent(row.transform, false);
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Disconnected";
            text.fontSize = 13;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            var textLayout = textObj.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 1f;

            // Connect/Disconnect button
            var buttonObj = new GameObject("ConnectButton");
            buttonObj.transform.SetParent(row.transform, false);
            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.45f, 0.65f);
            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            var buttonLayout = buttonObj.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = ButtonMinWidth;
            buttonLayout.preferredHeight = ButtonHeight;
            buttonLayout.minWidth = ButtonMinWidth;

            var buttonTextObj = new GameObject("Text");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            var buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = "Connect";
            buttonText.fontSize = 13;
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
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = true;
            horizontal.childForceExpandHeight = false;

            var layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = 28f;

            // Ready indicator pill
            var readyGroup = CreateIndicatorPill(row.transform, "Ready", "Not Ready");

            // Ball indicator pill
            var ballGroup = CreateIndicatorPill(row.transform, "Ball", "No Ball");

            return new ReadinessRow
            {
                ReadyIndicator = readyGroup.Indicator,
                ReadyText = readyGroup.Text,
                BallIndicator = ballGroup.Indicator,
                BallText = ballGroup.Text
            };
        }

        private static (Image Indicator, TextMeshProUGUI Text) CreateIndicatorPill(
            Transform parent, string label, string defaultText)
        {
            var pill = new GameObject(label + "Pill");
            pill.transform.SetParent(parent, false);

            // Pill background
            var bgImage = pill.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);

            var pillLayout = pill.AddComponent<LayoutElement>();
            pillLayout.preferredHeight = 24f;
            pillLayout.flexibleWidth = 1f;

            var horizontal = pill.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 6f;
            horizontal.padding = new RectOffset(8, 10, 3, 3);
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = false;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;

            // LED indicator (small circle)
            var indicatorObj = new GameObject("LED");
            indicatorObj.transform.SetParent(pill.transform, false);
            var indicator = indicatorObj.AddComponent<Image>();
            indicator.color = new Color(0.5f, 0.5f, 0.5f);
            var indicatorLayout = indicatorObj.AddComponent<LayoutElement>();
            indicatorLayout.preferredWidth = 10f;
            indicatorLayout.preferredHeight = 10f;
            indicatorLayout.minWidth = 10f;

            // Status text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(pill.transform, false);
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = defaultText;
            text.fontSize = 11;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            var textLayout = textObj.AddComponent<LayoutElement>();
            textLayout.preferredWidth = 60f;

            return (indicator, text);
        }

        private static ConfigPanel CreateConfigPanel(Transform parent)
        {
            var panel = new GameObject("ConfigPanel");
            panel.transform.SetParent(parent, false);

            var vertical = panel.AddComponent<VerticalLayoutGroup>();
            vertical.spacing = ItemSpacing;
            vertical.childAlignment = TextAnchor.UpperCenter;
            vertical.childControlWidth = true;
            vertical.childControlHeight = false;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;

            var layout = panel.AddComponent<LayoutElement>();
            layout.preferredHeight = 72f;

            // Host row
            var hostRow = CreateInputRow(panel.transform, "Host:", "127.0.0.1", HostInputWidth);
            var hostInput = hostRow.GetComponentInChildren<TMP_InputField>();

            // Port row
            var portRow = CreateInputRow(panel.transform, "Port:", "921", PortInputWidth);
            var portInput = portRow.GetComponentInChildren<TMP_InputField>();

            return new ConfigPanel
            {
                Panel = panel,
                HostInput = hostInput,
                PortInput = portInput
            };
        }

        private static GameObject CreateInputRow(Transform parent, string label, string defaultValue, float inputWidth)
        {
            var row = new GameObject(label.Replace(":", "") + "Row");
            row.transform.SetParent(parent, false);

            var horizontal = row.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = ItemSpacing;
            horizontal.childAlignment = TextAnchor.MiddleLeft;
            horizontal.childControlWidth = false;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = false;

            var layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = InputHeight;

            // Label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(row.transform, false);
            var labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 13;
            labelText.color = new Color(0.85f, 0.85f, 0.85f);
            labelText.alignment = TextAlignmentOptions.Left;
            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = LabelWidth;
            labelLayout.minWidth = LabelWidth;

            // Input field
            var inputObj = new GameObject("InputField");
            inputObj.transform.SetParent(row.transform, false);

            var inputBg = inputObj.AddComponent<Image>();
            inputBg.color = new Color(0.15f, 0.15f, 0.2f);

            var inputLayout = inputObj.AddComponent<LayoutElement>();
            inputLayout.preferredWidth = inputWidth;
            inputLayout.preferredHeight = InputHeight;
            inputLayout.minWidth = inputWidth;

            var textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputObj.transform, false);
            var textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = new Vector2(0.04f, 0.1f);
            textAreaRect.anchorMax = new Vector2(0.96f, 0.9f);
            textAreaRect.sizeDelta = Vector2.zero;

            var textComponent = new GameObject("Text");
            textComponent.transform.SetParent(textArea.transform, false);
            var text = textComponent.AddComponent<TextMeshProUGUI>();
            text.fontSize = 13;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            var textRect = textComponent.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(textArea.transform, false);
            var placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
            placeholderText.text = defaultValue;
            placeholderText.fontSize = 13;
            placeholderText.color = new Color(0.5f, 0.5f, 0.55f);
            placeholderText.fontStyle = FontStyles.Italic;
            placeholderText.alignment = TextAlignmentOptions.Left;
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
