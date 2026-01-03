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

        // Layout constants - sizing for right-side panel
        // Panel width calculation: padding(10*2) + toggle(44) + spacing(4) + label(~100) = 168
        // With button row: padding(10*2) + LED(10) + spacing(4) + text(~70) + spacing(4) + button(70) = 178
        // Using 200px panel width with tighter spacing
        private const float PanelWidth = 200f;
        private const float PanelPadding = 8f;
        private const float SectionSpacing = 2f;  // Minimal spacing between rows
        private const float ItemSpacing = 4f;     // Tighter horizontal spacing
        private const float LedIndicatorSize = 10f;
        private const float ToggleWidth = 44f;
        private const float ToggleHeight = 22f;
        private const float ButtonWidth = 70f;  // Fits "Connect" text
        private const float ButtonHeight = 22f;
        private const float HostInputWidth = 100f;
        private const float PortInputWidth = 50f;
        private const float InputHeight = 20f;
        private const float LabelWidth = 35f;
        private const float RowHeight = 22f;  // Reduced from 26f
        private const float SmallFontSize = 10f;  // Reduced from 11f for compact layout
        private const float TitleFontSize = 12f;  // Reduced from 14f

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
            rectTransform.sizeDelta = new Vector2(PanelWidth, 200f);

            var component = root.AddComponent<GSProModeUI>();

            var background = root.AddComponent<Image>();
            background.color = new Color(0.12f, 0.12f, 0.18f, 0.95f);

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
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layoutElement = root.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = PanelWidth;

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
            text.fontSize = TitleFontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;

            var layout = header.AddComponent<LayoutElement>();
            layout.preferredHeight = 16f;  // Reduced from 20f

            return header;
        }

        private static ModeToggleRow CreateModeToggleRow(Transform parent)
        {
            var row = new GameObject("ModeRow");
            row.transform.SetParent(parent, false);

            var horizontal = row.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = ItemSpacing;
            horizontal.childAlignment = TextAnchor.MiddleLeft;
            horizontal.childControlWidth = true;   // Enable width control so children are constrained
            horizontal.childControlHeight = false;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;

            var layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = RowHeight;

            // Toggle on left (fixed size)
            var toggleObj = CreateToggle(row.transform);

            // Mode label on right
            var labelObj = new GameObject("ModeLabel");
            labelObj.transform.SetParent(row.transform, false);

            var label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = "Open Range";
            label.fontSize = SmallFontSize;
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

            // Background track (dark when off)
            var toggleBg = new GameObject("Background");
            toggleBg.transform.SetParent(toggleObj.transform, false);
            var bgImage = toggleBg.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.35f);
            var bgRect = toggleBg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Checkmark area (green highlight when on)
            var checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(toggleBg.transform, false);
            var checkImage = checkmark.AddComponent<Image>();
            checkImage.color = new Color(0.2f, 0.7f, 0.3f);
            var checkRect = checkmark.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.5f, 0.1f);
            checkRect.anchorMax = new Vector2(0.95f, 0.9f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;

            var toggle = toggleObj.AddComponent<Toggle>();
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = false;

            // Fixed size layout element - critical to prevent stretching
            var toggleLayout = toggleObj.AddComponent<LayoutElement>();
            toggleLayout.minWidth = ToggleWidth;
            toggleLayout.minHeight = ToggleHeight;
            toggleLayout.preferredWidth = ToggleWidth;
            toggleLayout.preferredHeight = ToggleHeight;
            toggleLayout.flexibleWidth = 0f;
            toggleLayout.flexibleHeight = 0f;

            return (toggle, toggleObj);
        }

        private static ConnectionRow CreateConnectionRow(Transform parent)
        {
            var row = new GameObject("ConnectionRow");
            row.transform.SetParent(parent, false);

            var horizontal = row.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = ItemSpacing;
            horizontal.childAlignment = TextAnchor.MiddleLeft;
            horizontal.childControlWidth = true;   // Enable width control so children are constrained
            horizontal.childControlHeight = false;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;

            var layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = RowHeight;

            // LED Indicator (small fixed-size circle)
            var indicatorObj = new GameObject("Indicator");
            indicatorObj.transform.SetParent(row.transform, false);
            var indicator = indicatorObj.AddComponent<Image>();
            indicator.color = new Color(0.8f, 0.2f, 0.2f);
            var indicatorRect = indicatorObj.GetComponent<RectTransform>();
            indicatorRect.sizeDelta = new Vector2(LedIndicatorSize, LedIndicatorSize);
            var indicatorLayout = indicatorObj.AddComponent<LayoutElement>();
            indicatorLayout.minWidth = LedIndicatorSize;
            indicatorLayout.minHeight = LedIndicatorSize;
            indicatorLayout.preferredWidth = LedIndicatorSize;
            indicatorLayout.preferredHeight = LedIndicatorSize;
            indicatorLayout.flexibleWidth = 0f;
            indicatorLayout.flexibleHeight = 0f;

            // Connection status text
            var textObj = new GameObject("ConnectionText");
            textObj.transform.SetParent(row.transform, false);
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Disconnected";
            text.fontSize = SmallFontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            var textLayout = textObj.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 1f;
            textLayout.preferredHeight = RowHeight;

            // Connect/Disconnect button (fixed size)
            var buttonObj = new GameObject("ConnectButton");
            buttonObj.transform.SetParent(row.transform, false);
            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.25f, 0.5f, 0.7f);
            var buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);  // 70x22
            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            var buttonLayout = buttonObj.AddComponent<LayoutElement>();
            buttonLayout.minWidth = ButtonWidth;
            buttonLayout.minHeight = ButtonHeight;
            buttonLayout.preferredWidth = ButtonWidth;
            buttonLayout.preferredHeight = ButtonHeight;
            buttonLayout.flexibleWidth = 0f;
            buttonLayout.flexibleHeight = 0f;

            var buttonTextObj = new GameObject("Text");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            var buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = "Connect";
            buttonText.fontSize = SmallFontSize;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            var buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;

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
            horizontal.spacing = ItemSpacing;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = true;   // Enable width control
            horizontal.childControlHeight = false;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;

            var layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = 20f;  // Reduced from 22f

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
            var pillRect = pill.GetComponent<RectTransform>();
            pillRect.sizeDelta = new Vector2(75f, 18f);  // Reduced from 85x20

            var pillLayout = pill.AddComponent<LayoutElement>();
            pillLayout.minWidth = 70f;
            pillLayout.minHeight = 18f;
            pillLayout.preferredWidth = 75f;
            pillLayout.preferredHeight = 18f;
            pillLayout.flexibleWidth = 0f;
            pillLayout.flexibleHeight = 0f;

            var horizontal = pill.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 3f;  // Reduced from 4f
            horizontal.padding = new RectOffset(4, 4, 2, 2);  // Reduced from 6,6,2,2
            horizontal.childAlignment = TextAnchor.MiddleLeft;
            horizontal.childControlWidth = true;   // Enable width control
            horizontal.childControlHeight = false;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;

            // LED indicator (small fixed-size circle)
            var indicatorObj = new GameObject("LED");
            indicatorObj.transform.SetParent(pill.transform, false);
            var indicator = indicatorObj.AddComponent<Image>();
            indicator.color = new Color(0.5f, 0.5f, 0.5f);
            var indicatorRect = indicatorObj.GetComponent<RectTransform>();
            indicatorRect.sizeDelta = new Vector2(8f, 8f);
            var indicatorLayout = indicatorObj.AddComponent<LayoutElement>();
            indicatorLayout.minWidth = 8f;
            indicatorLayout.minHeight = 8f;
            indicatorLayout.preferredWidth = 8f;
            indicatorLayout.preferredHeight = 8f;
            indicatorLayout.flexibleWidth = 0f;
            indicatorLayout.flexibleHeight = 0f;

            // Status text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(pill.transform, false);
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = defaultText;
            text.fontSize = 9f;  // Reduced from 10f
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            var textLayout = textObj.AddComponent<LayoutElement>();
            textLayout.preferredWidth = 48f;  // Reduced from 55f
            textLayout.preferredHeight = 14f;  // Reduced from 16f
            textLayout.flexibleWidth = 1f;

            return (indicator, text);
        }

        private static ConfigPanel CreateConfigPanel(Transform parent)
        {
            var panel = new GameObject("ConfigPanel");
            panel.transform.SetParent(parent, false);

            var vertical = panel.AddComponent<VerticalLayoutGroup>();
            vertical.spacing = 2f;  // Reduced from 4f
            vertical.childAlignment = TextAnchor.UpperLeft;
            vertical.childControlWidth = true;
            vertical.childControlHeight = false;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;

            var layout = panel.AddComponent<LayoutElement>();
            layout.preferredHeight = 44f;  // Reduced from 52f

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
            horizontal.spacing = 4f;
            horizontal.childAlignment = TextAnchor.MiddleLeft;
            horizontal.childControlWidth = true;   // Enable width control
            horizontal.childControlHeight = false;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;

            var layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = InputHeight;

            // Label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(row.transform, false);
            var labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = SmallFontSize;
            labelText.color = new Color(0.75f, 0.75f, 0.8f);
            labelText.alignment = TextAlignmentOptions.Left;
            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.minWidth = LabelWidth;
            labelLayout.preferredWidth = LabelWidth;
            labelLayout.preferredHeight = InputHeight;

            // Input field
            var inputObj = new GameObject("InputField");
            inputObj.transform.SetParent(row.transform, false);

            var inputBg = inputObj.AddComponent<Image>();
            inputBg.color = new Color(0.18f, 0.18f, 0.22f);

            var inputLayout = inputObj.AddComponent<LayoutElement>();
            inputLayout.minWidth = inputWidth;
            inputLayout.minHeight = InputHeight;
            inputLayout.preferredWidth = inputWidth;
            inputLayout.preferredHeight = InputHeight;
            inputLayout.flexibleWidth = 1f;

            var textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputObj.transform, false);
            var textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = new Vector2(0.05f, 0.1f);
            textAreaRect.anchorMax = new Vector2(0.95f, 0.9f);
            textAreaRect.offsetMin = Vector2.zero;
            textAreaRect.offsetMax = Vector2.zero;

            var textComponent = new GameObject("Text");
            textComponent.transform.SetParent(textArea.transform, false);
            var text = textComponent.AddComponent<TextMeshProUGUI>();
            text.fontSize = SmallFontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            var textRect = textComponent.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(textArea.transform, false);
            var placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
            placeholderText.text = defaultValue;
            placeholderText.fontSize = SmallFontSize;
            placeholderText.color = new Color(0.5f, 0.5f, 0.55f);
            placeholderText.fontStyle = FontStyles.Italic;
            placeholderText.alignment = TextAlignmentOptions.Left;
            var placeholderRect = placeholder.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;

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
