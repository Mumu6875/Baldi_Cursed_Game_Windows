using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuScript : MonoBehaviour
{
    private enum Page { Home, Story, Settings, Controls }

    private static readonly Color Overlay = new Color(0.015f, 0.02f, 0.03f, 0.92f);
    private static readonly Color Panel = new Color(0.055f, 0.065f, 0.085f, 0.98f);
    private static readonly Color Card = new Color(0.095f, 0.11f, 0.14f, 1f);
    private static readonly Color Muted = new Color(0.68f, 0.72f, 0.78f, 1f);
    private static readonly Color Yellow = new Color(1f, 0.78f, 0.08f, 1f);
    private static readonly Color Red = new Color(0.9f, 0.08f, 0.08f, 1f);

    public GameControllerScript gc;

    private TMP_FontAsset font;
    private Color accent;
    private Page currentPage;
    private GameObject homePage;
    private GameObject storyPage;
    private GameObject settingsPage;
    private GameObject controlsPage;
    private GameObject bindingOverlay;
    private TextMeshProUGUI phaseLabel;
    private TextMeshProUGUI statusLabel;
    private TextMeshProUGUI sensitivityValue;
    private Slider sensitivitySlider;
    private InputManager inputManager;
    private readonly Dictionary<InputAction, TextMeshProUGUI> bindingLabels = new Dictionary<InputAction, TextMeshProUGUI>();
    private Coroutine bindingRoutine;

    public bool IsCapturingBinding { get { return bindingRoutine != null; } }

    private void Awake()
    {
        font = FindLegacyFont();
        accent = IsExclusivePhase2() ? Red : Yellow;
        DisableLegacyInterface();
        BuildInterface();
    }

    private void OnEnable()
    {
        if (homePage == null) return;
        accent = IsExclusivePhase2() ? Red : Yellow;
        ShowPage(Page.Home);
        RefreshValues();
    }

    private void OnDisable()
    {
        CancelBinding();
    }

    public bool HandleBackRequest()
    {
        if (IsCapturingBinding) return true;
        if (currentPage != Page.Home)
        {
            ShowPage(Page.Home);
            return true;
        }
        return false;
    }

    private void DisableLegacyInterface()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }

        MenuController menuController = GetComponent<MenuController>();
        if (menuController != null) menuController.enabled = false;
        UIController uiController = GetComponent<UIController>();
        if (uiController != null) uiController.enabled = false;
    }

    private void BuildInterface()
    {
        GameObject overlay = CreateImage("Modern Pause Overlay", transform, Overlay);
        Stretch(overlay.GetComponent<RectTransform>());

        GameObject frame = CreateImage("Pause Panel", overlay.transform, Panel);
        SetAnchors(frame.GetComponent<RectTransform>(), 0.055f, 0.055f, 0.945f, 0.945f);
        AddOutline(frame, new Color(accent.r, accent.g, accent.b, 0.55f), 2f);

        GameObject header = CreateImage("Header", frame.transform, new Color(0.025f, 0.03f, 0.045f, 1f));
        SetAnchors(header.GetComponent<RectTransform>(), 0f, 0.835f, 1f, 1f);
        CreateText("Title", header.transform, "GAME PAUSED", 30f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white,
            new Vector2(0.04f, 0f), new Vector2(0.58f, 1f));
        phaseLabel = CreateText("Phase", header.transform, "", 17f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, accent,
            new Vector2(0.58f, 0f), new Vector2(0.96f, 1f));

        GameObject sidebar = CreateImage("Navigation", frame.transform, new Color(0.035f, 0.042f, 0.058f, 1f));
        SetAnchors(sidebar.GetComponent<RectTransform>(), 0f, 0f, 0.255f, 0.835f);
        AddNavigation(sidebar.transform);

        RectTransform contentRoot = CreateRect("Content", frame.transform);
        SetAnchors(contentRoot, 0.275f, 0.035f, 0.975f, 0.805f);
        homePage = BuildHomePage(contentRoot);
        storyPage = BuildStoryPage(contentRoot);
        settingsPage = BuildSettingsPage(contentRoot);
        controlsPage = BuildControlsPage(contentRoot);
        BuildBindingOverlay(overlay.transform);
        ShowPage(Page.Home);
    }

    private void AddNavigation(Transform parent)
    {
        CreateText("Nav Caption", parent, "PAUSE MENU", 13f, FontStyles.Bold, TextAlignmentOptions.Center, Muted,
            new Vector2(0.08f, 0.87f), new Vector2(0.92f, 0.98f));
        CreateButton("Resume", parent, "RESUME", new Vector2(0.09f, 0.70f), new Vector2(0.91f, 0.84f), delegate { gc.UnpauseGame(); }, true);
        CreateButton("Story", parent, "STORY", new Vector2(0.09f, 0.53f), new Vector2(0.91f, 0.67f), delegate { ShowPage(Page.Story); }, false);
        CreateButton("Settings", parent, "SETTINGS", new Vector2(0.09f, 0.36f), new Vector2(0.91f, 0.50f), delegate { ShowPage(Page.Settings); }, false);
        CreateButton("Controls", parent, "CONTROLS", new Vector2(0.09f, 0.19f), new Vector2(0.91f, 0.33f), delegate { ShowPage(Page.Controls); }, false);
        CreateButton("Main Menu", parent, "MAIN MENU", new Vector2(0.09f, 0.02f), new Vector2(0.91f, 0.16f), delegate { gc.ExitGame(); }, false);
    }

    private GameObject BuildHomePage(Transform parent)
    {
        GameObject page = CreatePage("Home Page", parent);
        CreateText("Heading", page.transform, "TAKE A BREATH", 27f, FontStyles.Bold, TextAlignmentOptions.TopLeft, Color.white,
            new Vector2(0f, 0.78f), new Vector2(1f, 1f));
        CreateText("Subtitle", page.transform, "The school is frozen while this menu is open.", 16f, FontStyles.Normal,
            TextAlignmentOptions.TopLeft, Muted, new Vector2(0f, 0.65f), new Vector2(1f, 0.83f));

        GameObject card = CreateImage("Run Status", page.transform, Card);
        SetAnchors(card.GetComponent<RectTransform>(), 0f, 0.25f, 1f, 0.62f);
        AddOutline(card, new Color(1f, 1f, 1f, 0.08f), 1f);
        CreateText("Status Caption", card.transform, "CURRENT RUN", 13f, FontStyles.Bold, TextAlignmentOptions.TopLeft, accent,
            new Vector2(0.05f, 0.63f), new Vector2(0.95f, 0.92f));
        statusLabel = CreateText("Status", card.transform, "", 21f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white,
            new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.68f));
        CreateText("Hint", page.transform, "Press ESC again to resume", 14f, FontStyles.Normal, TextAlignmentOptions.BottomRight, Muted,
            new Vector2(0f, 0f), new Vector2(1f, 0.16f));
        return page;
    }

    private GameObject BuildStoryPage(Transform parent)
    {
        GameObject page = CreatePage("Story Page", parent);
        AddPageHeader(page.transform, "STORY", "Read the current phase objective.");
        RectTransform content = CreateScrollArea("Story Scroll", page.transform, new Vector2(0f, 0f), new Vector2(1f, 0.76f));
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 14, 18);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        TextMeshProUGUI story = CreateFlowText("Story Text", content, CursedHorrorBootstrap.GetStoryTextForCurrentPhase(), 18f, Color.white);
        story.richText = true;
        return page;
    }

    private GameObject BuildSettingsPage(Transform parent)
    {
        GameObject page = CreatePage("Settings Page", parent);
        AddPageHeader(page.transform, "SETTINGS", "Changes are applied and saved immediately.");
        GameObject card = CreateImage("Sensitivity Card", page.transform, Card);
        SetAnchors(card.GetComponent<RectTransform>(), 0f, 0.34f, 1f, 0.72f);
        CreateText("Sensitivity Label", card.transform, "LOOK SENSITIVITY", 17f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft,
            Color.white, new Vector2(0.05f, 0.62f), new Vector2(0.72f, 0.93f));
        sensitivityValue = CreateText("Sensitivity Value", card.transform, "", 18f, FontStyles.Bold, TextAlignmentOptions.MidlineRight,
            accent, new Vector2(0.72f, 0.62f), new Vector2(0.95f, 0.93f));
        sensitivitySlider = CreateSlider(card.transform, new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.54f));
        sensitivitySlider.minValue = 0.1f;
        sensitivitySlider.maxValue = 10f;
        sensitivitySlider.onValueChanged.AddListener(ApplySensitivity);
        CreateButton("Reset Sensitivity", card.transform, "RESET TO 2.0", new Vector2(0.05f, 0.04f), new Vector2(0.42f, 0.25f),
            delegate { sensitivitySlider.value = 2f; }, false);
        return page;
    }

    private GameObject BuildControlsPage(Transform parent)
    {
        GameObject page = CreatePage("Controls Page", parent);
        AddPageHeader(page.transform, "CONTROLS", "Select a binding, then press a key or mouse button.");
#if UNITY_STANDALONE || UNITY_EDITOR
        RectTransform content = CreateScrollArea("Controls Scroll", page.transform, new Vector2(0f, 0.12f), new Vector2(1f, 0.75f));
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 7f;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        InputAction[] actions = { InputAction.MoveForward, InputAction.MoveBackward, InputAction.MoveLeft, InputAction.MoveRight,
            InputAction.Interact, InputAction.UseItem, InputAction.Slot0, InputAction.Slot1, InputAction.Slot2,
            InputAction.Run, InputAction.LookBehind, InputAction.PauseOrCancel };
        for (int i = 0; i < actions.Length; i++) AddBindingRow(content, actions[i]);
        CreateButton("Reset Controls", page.transform, "RESET DEFAULTS", new Vector2(0f, 0f), new Vector2(0.38f, 0.09f), ResetControls, false);
#else
        RectTransform content = CreateScrollArea("Touch Guide Scroll", page.transform, new Vector2(0f, 0f), new Vector2(1f, 0.75f));
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 14, 18);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        CreateFlowText("Touch Guide", content,
            "MOVE\nUse the left virtual joystick.\n\nLOOK AND INTERACT\nDrag the right side to look. Tap an object, notebook or door to interact.\n\nRUN\nHold the run button while moving.\n\nITEMS\nTap a slot to select it, then use the round item button.", 17f, Color.white);
#endif
        return page;
    }

    private void AddBindingRow(Transform parent, InputAction action)
    {
        GameObject row = CreateImage(action + " Row", parent, Card);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 48f;
        HorizontalLayoutGroup group = row.AddComponent<HorizontalLayoutGroup>();
        group.padding = new RectOffset(12, 10, 6, 6);
        group.spacing = 10f;
        group.childAlignment = TextAnchor.MiddleLeft;
        group.childControlHeight = true;
        group.childControlWidth = false;

        TextMeshProUGUI name = CreateLayoutText("Action", row.transform, ActionName(action), 15f, Color.white, 125f);
        name.fontStyle = FontStyles.Bold;
        Button bindingButton = CreateLayoutButton("Binding", row.transform, "", 165f);
        TextMeshProUGUI label = bindingButton.GetComponentInChildren<TextMeshProUGUI>();
        bindingLabels[action] = label;
        InputAction capturedAction = action;
        bindingButton.onClick.AddListener(delegate { BeginBinding(capturedAction); });
    }

    private void BuildBindingOverlay(Transform parent)
    {
        bindingOverlay = CreateImage("Binding Capture", parent, new Color(0f, 0f, 0f, 0.94f));
        Stretch(bindingOverlay.GetComponent<RectTransform>());
        CreateText("Capture Title", bindingOverlay.transform, "PRESS A KEY", 30f, FontStyles.Bold, TextAlignmentOptions.Center,
            Color.white, new Vector2(0.1f, 0.50f), new Vector2(0.9f, 0.68f));
        CreateText("Capture Hint", bindingOverlay.transform, "ESC cancels  |  DELETE clears", 15f, FontStyles.Normal,
            TextAlignmentOptions.Center, Muted, new Vector2(0.1f, 0.38f), new Vector2(0.9f, 0.52f));
        bindingOverlay.SetActive(false);
    }

    private void ShowPage(Page page)
    {
        currentPage = page;
        homePage.SetActive(page == Page.Home);
        storyPage.SetActive(page == Page.Story);
        settingsPage.SetActive(page == Page.Settings);
        controlsPage.SetActive(page == Page.Controls);
        RefreshValues();
    }

    private void RefreshValues()
    {
        string phase = IsExclusivePhase2() ? "PHASE 2" : (CursedPhaseManager.IsPhase3 ? "PHASE 3" : (CursedPhaseManager.IsPhase4 ? "PHASE 4" : "PHASE 1"));
        if (phaseLabel != null)
        {
            phaseLabel.text = phase;
            phaseLabel.color = accent;
        }
        if (statusLabel != null) statusLabel.text = phase + "\n" + (gc != null ? gc.notebooks : 0) + " / 7 NOTEBOOKS";
        float sensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
        if (sensitivitySlider != null) sensitivitySlider.SetValueWithoutNotify(Mathf.Clamp(sensitivity, 0.1f, 10f));
        if (sensitivityValue != null) sensitivityValue.text = sensitivity.ToString("0.0");
#if UNITY_STANDALONE || UNITY_EDITOR
        inputManager = Singleton<InputManager>.Instance;
        RefreshBindings();
#endif
    }

    private void ApplySensitivity(float value)
    {
        value = Mathf.Clamp(value, 0.1f, 10f);
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        PlayerPrefs.Save();
        if (gc != null && gc.player != null) gc.player.mouseSensitivity = value;
        if (sensitivityValue != null) sensitivityValue.text = value.ToString("0.0");
    }

    private void BeginBinding(InputAction action)
    {
        if (bindingRoutine != null) StopCoroutine(bindingRoutine);
        bindingOverlay.SetActive(true);
        bindingRoutine = StartCoroutine(CaptureBinding(action));
    }

    private IEnumerator CaptureBinding(InputAction action)
    {
        yield return null;
        float deadline = Time.realtimeSinceStartup + 8f;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) break;
            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
            {
                inputManager.ClearPrimaryKey(action);
                inputManager.ClearSecondaryKey(action);
                inputManager.Save();
                break;
            }
            KeyCode key;
            if (TryReadKey(out key))
            {
                InputBinding binding = inputManager.Mappings[action];
                binding.primaryKey = key;
                binding.secondaryKey = KeyCode.None;
                inputManager.KeyboardMapping[action] = binding;
                inputManager.Save();
                break;
            }
            yield return null;
        }
        bindingRoutine = null;
        bindingOverlay.SetActive(false);
        RefreshBindings();
    }

    private bool TryReadKey(out KeyCode key)
    {
        foreach (KeyCode candidate in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (candidate == KeyCode.None || candidate == KeyCode.Escape || candidate == KeyCode.Delete || candidate == KeyCode.Backspace) continue;
            if (Input.GetKeyDown(candidate))
            {
                key = candidate;
                return true;
            }
        }
        key = KeyCode.None;
        return false;
    }

    private void CancelBinding()
    {
        if (bindingRoutine != null) StopCoroutine(bindingRoutine);
        bindingRoutine = null;
        if (bindingOverlay != null) bindingOverlay.SetActive(false);
    }

    private void ResetControls()
    {
        inputManager = Singleton<InputManager>.Instance;
        inputManager.SetDefaults();
        RefreshBindings();
    }

    private void RefreshBindings()
    {
        if (inputManager == null) return;
        foreach (KeyValuePair<InputAction, TextMeshProUGUI> pair in bindingLabels)
        {
            InputBinding binding = inputManager.Mappings[pair.Key];
            KeyCode visibleKey = binding.primaryKey != KeyCode.None ? binding.primaryKey : binding.secondaryKey;
            pair.Value.text = visibleKey == KeyCode.None ? "UNBOUND" : inputManager.KeyCodeToDisplayString(visibleKey).ToUpperInvariant();
        }
    }

    private static string ActionName(InputAction action)
    {
        switch (action)
        {
            case InputAction.MoveForward: return "MOVE FORWARD";
            case InputAction.MoveBackward: return "MOVE BACK";
            case InputAction.MoveLeft: return "MOVE LEFT";
            case InputAction.MoveRight: return "MOVE RIGHT";
            case InputAction.Interact: return "INTERACT";
            case InputAction.UseItem: return "USE ITEM";
            case InputAction.Slot0: return "ITEM SLOT 1";
            case InputAction.Slot1: return "ITEM SLOT 2";
            case InputAction.Slot2: return "ITEM SLOT 3";
            case InputAction.Run: return "RUN";
            case InputAction.LookBehind: return "LOOK BEHIND";
            case InputAction.PauseOrCancel: return "PAUSE";
            default: return action.ToString().ToUpperInvariant();
        }
    }

    private void AddPageHeader(Transform parent, string title, string subtitle)
    {
        CreateText(title, parent, title, 25f, FontStyles.Bold, TextAlignmentOptions.TopLeft, Color.white,
            new Vector2(0f, 0.82f), new Vector2(1f, 1f));
        CreateText("Subtitle", parent, subtitle, 14f, FontStyles.Normal, TextAlignmentOptions.TopLeft, Muted,
            new Vector2(0f, 0.73f), new Vector2(1f, 0.86f));
    }

    private GameObject CreatePage(string name, Transform parent)
    {
        GameObject page = new GameObject(name, typeof(RectTransform));
        page.transform.SetParent(parent, false);
        Stretch(page.GetComponent<RectTransform>());
        return page;
    }

    private GameObject CreateImage(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().color = color;
        return obj;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string value, float size, FontStyles style,
        TextAlignmentOptions alignment, Color color, Vector2 min, Vector2 max)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        SetAnchors(obj.GetComponent<RectTransform>(), min.x, min.y, max.x, max.y);
        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = true;
        return text;
    }

    private TextMeshProUGUI CreateFlowText(string name, Transform parent, string value, float size, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableWordWrapping = true;
        return text;
    }

    private TextMeshProUGUI CreateLayoutText(string name, Transform parent, string value, float size, Color color, float width)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        obj.transform.SetParent(parent, false);
        LayoutElement layout = obj.GetComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.minWidth = width;
        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        return text;
    }

    private Button CreateButton(string name, Transform parent, string label, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction click, bool primary)
    {
        GameObject obj = CreateImage(name, parent, primary ? accent : Card);
        SetAnchors(obj.GetComponent<RectTransform>(), min.x, min.y, max.x, max.y);
        Button button = obj.AddComponent<Button>();
        ConfigureButton(button, primary);
        button.onClick.AddListener(click);
        CreateText("Label", obj.transform, label, 16f, FontStyles.Bold, TextAlignmentOptions.Center, primary ? Color.black : Color.white,
            Vector2.zero, Vector2.one);
        return button;
    }

    private Button CreateLayoutButton(string name, Transform parent, string label, float width)
    {
        GameObject obj = CreateImage(name, parent, new Color(0.14f, 0.16f, 0.2f, 1f));
        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.minWidth = width;
        Button button = obj.AddComponent<Button>();
        ConfigureButton(button, false);
        CreateText("Label", obj.transform, label, 13f, FontStyles.Bold, TextAlignmentOptions.Center, accent, Vector2.zero, Vector2.one);
        return button;
    }

    private void ConfigureButton(Button button, bool primary)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = primary ? new Color(1f, 0.92f, 0.65f, 1f) : new Color(1.25f, 1.25f, 1.25f, 1f);
        colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
    }

    private Slider CreateSlider(Transform parent, Vector2 min, Vector2 max)
    {
        GameObject root = new GameObject("Sensitivity Slider", typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent, false);
        SetAnchors(root.GetComponent<RectTransform>(), min.x, min.y, max.x, max.y);
        GameObject background = CreateImage("Track", root.transform, new Color(0.025f, 0.03f, 0.04f, 1f));
        SetAnchors(background.GetComponent<RectTransform>(), 0f, 0.38f, 1f, 0.62f);
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(root.transform, false);
        SetAnchors(fillArea.GetComponent<RectTransform>(), 0f, 0.38f, 1f, 0.62f);
        GameObject fill = CreateImage("Fill", fillArea.transform, accent);
        Stretch(fill.GetComponent<RectTransform>());
        GameObject handleArea = new GameObject("Handle Area", typeof(RectTransform));
        handleArea.transform.SetParent(root.transform, false);
        Stretch(handleArea.GetComponent<RectTransform>());
        GameObject handle = CreateImage("Handle", handleArea.transform, Color.white);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(18f, 26f);
        Slider slider = root.GetComponent<Slider>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private RectTransform CreateScrollArea(string name, Transform parent, Vector2 min, Vector2 max)
    {
        GameObject root = CreateImage(name, parent, Card);
        SetAnchors(root.GetComponent<RectTransform>(), min.x, min.y, max.x, max.y);
        ScrollRect scroll = root.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.scrollSensitivity = 24f;
        GameObject viewport = CreateImage("Viewport", root.transform, Color.white);
        Stretch(viewport.GetComponent<RectTransform>());
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        RectTransform content = CreateRect("Content", viewport.transform);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = content;
        return content;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<RectTransform>();
    }

    private static void AddOutline(GameObject obj, Color color, float distance)
    {
        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(distance, -distance);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private TMP_FontAsset FindLegacyFont()
    {
        TextMeshProUGUI[] labels = GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < labels.Length; i++) if (labels[i].font != null) return labels[i].font;
        return TMP_Settings.defaultFontAsset;
    }

    private static bool IsExclusivePhase2()
    {
        return CursedPhaseManager.IsPhase2 && !CursedPhaseManager.IsPhase3 && !CursedPhaseManager.IsPhase4;
    }
}
