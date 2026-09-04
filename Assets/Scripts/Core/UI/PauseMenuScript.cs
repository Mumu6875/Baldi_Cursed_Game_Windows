using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuScript : MonoBehaviour
{
    private enum Page { Story, Controls }

    private static readonly Color Overlay = new Color(0f, 0f, 0f, 0.72f);
    private static readonly Color Paper = new Color(0.94f, 0.93f, 0.86f, 0.98f);
    private static readonly Color Ink = new Color(0.08f, 0.08f, 0.20f, 1f);
    private static readonly Color Purple = new Color(0.40f, 0.38f, 0.72f, 1f);
    private static readonly Color Blue = new Color(0.08f, 0.30f, 0.85f, 1f);
    private static readonly Color Green = new Color(0.10f, 0.82f, 0.13f, 1f);
    private static readonly Color Red = new Color(0.92f, 0.06f, 0.10f, 1f);

    public GameControllerScript gc;

    private TMP_FontAsset font;
    private GameObject storyPage;
    private GameObject controlsPage;
    private GameObject bindingOverlay;
    private GameObject baldiYesButton;
    private GameObject baldiNoButton;
    private TextMeshProUGUI storyText;
    private TextMeshProUGUI sensitivityValue;
    private Slider sensitivitySlider;
    private InputManager inputManager;
    private readonly Dictionary<InputAction, TextMeshProUGUI> bindingLabels = new Dictionary<InputAction, TextMeshProUGUI>();
    private Coroutine bindingRoutine;

    public bool IsCapturingBinding { get { return bindingRoutine != null; } }

    private void Awake()
    {
        font = FindLegacyFont();
        CaptureAndDisableLegacyInterface();
        BuildInterface();
    }

    private void OnEnable()
    {
        if (storyPage == null) return;
        ShowPage(Page.Story);
        RefreshValues();
    }

    private void OnDisable()
    {
        CancelBinding();
    }

    // Pausing again must never resume the game. Only Baldi's No head resumes.
    public bool HandleBackRequest()
    {
        return true;
    }

    private void CaptureAndDisableLegacyInterface()
    {
        Transform oldButtons = transform.Find("PauseButtons");
        if (oldButtons != null)
        {
            Transform yes = oldButtons.Find("BaldiNodButton");
            Transform no = oldButtons.Find("BaldiShakeButton");
            if (yes != null) baldiYesButton = yes.gameObject;
            if (no != null) baldiNoButton = no.gameObject;
        }

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
        GameObject overlay = CreateImage("Simple Pause Overlay", transform, Overlay);
        Stretch(overlay.GetComponent<RectTransform>());

        GameObject paper = CreateImage("Pause Paper", overlay.transform, Paper);
        SetAnchors(paper.GetComponent<RectTransform>(), 0.12f, 0.055f, 0.88f, 0.945f);
        AddOutline(paper, new Color(0.08f, 0.08f, 0.12f, 0.8f), 3f);

        CreateText("Sensitivity Label", paper.transform, "SENSITIVITY", 24f, FontStyles.Bold,
            TextAlignmentOptions.Center, Ink, new Vector2(0.08f, 0.86f), new Vector2(0.72f, 0.98f));
        sensitivityValue = CreateText("Sensitivity Value", paper.transform, "", 18f, FontStyles.Bold,
            TextAlignmentOptions.Center, Blue, new Vector2(0.74f, 0.86f), new Vector2(0.92f, 0.98f));
        sensitivitySlider = CreateSlider(paper.transform, new Vector2(0.10f, 0.77f), new Vector2(0.90f, 0.86f));
        sensitivitySlider.minValue = 0.1f;
        sensitivitySlider.maxValue = 10f;
        sensitivitySlider.onValueChanged.AddListener(ApplySensitivity);

        CreateButton("Controls Tab", paper.transform, "CONTROLS", new Vector2(0.10f, 0.66f), new Vector2(0.47f, 0.75f),
            delegate { ShowPage(Page.Controls); }, Purple);
        CreateButton("Story Tab", paper.transform, "STORY", new Vector2(0.53f, 0.66f), new Vector2(0.90f, 0.75f),
            delegate { ShowPage(Page.Story); }, Purple);

        RectTransform content = CreateRect("Content", paper.transform);
        SetAnchors(content, 0.08f, 0.24f, 0.92f, 0.63f);
        storyPage = BuildStoryPage(content);
        controlsPage = BuildControlsPage(content);

        BuildBaldiChoices(paper.transform);
        BuildBindingOverlay(overlay.transform);
        ShowPage(Page.Story);
    }

    private GameObject BuildStoryPage(Transform parent)
    {
        GameObject page = CreatePage("Story Page", parent);
        RectTransform content = CreateScrollArea("Story Scroll", page.transform);
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 12, 16);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        storyText = CreateFlowText("Story Text", content, CursedHorrorBootstrap.GetHowToPlayTextForCurrentPhase(), 17f, Ink);
        storyText.richText = true;
        return page;
    }

    private GameObject BuildControlsPage(Transform parent)
    {
        GameObject page = CreatePage("Controls Page", parent);
        RectTransform content = CreateScrollArea("Controls Scroll", page.transform);
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 9, 10);
        layout.spacing = 6f;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

#if UNITY_STANDALONE || UNITY_EDITOR
        InputAction[] actions =
        {
            InputAction.MoveForward, InputAction.MoveBackward, InputAction.MoveLeft, InputAction.MoveRight,
            InputAction.Interact, InputAction.UseItem, InputAction.Slot0, InputAction.Slot1, InputAction.Slot2,
            InputAction.Run, InputAction.LookBehind, InputAction.PauseOrCancel
        };
        for (int i = 0; i < actions.Length; i++) AddBindingRow(content, actions[i]);
#else
        CreateFlowText("Touch Guide", content,
            "MOVE - Left joystick\nLOOK - Drag the screen\nINTERACT - Tap the screen\nRUN - Hold the run button\nLOOK BEHIND - Hold the eye button\nITEMS - Select a slot and tap the item button",
            17f, Ink);
#endif
        return page;
    }

    private void BuildBaldiChoices(Transform parent)
    {
        if (baldiYesButton == null || baldiNoButton == null)
        {
            Debug.LogError("The original Baldi Yes/No pause heads could not be found.");
            return;
        }

        PlaceBaldiButton(baldiYesButton, parent, new Vector2(0.35f, 0.115f));
        PlaceBaldiButton(baldiNoButton, parent, new Vector2(0.65f, 0.115f));
        CreateText("Yes Label", parent, "YES", 14f, FontStyles.Bold, TextAlignmentOptions.Center,
            Red, new Vector2(0.24f, 0.185f), new Vector2(0.46f, 0.235f));
        CreateText("No Label", parent, "NO", 14f, FontStyles.Bold, TextAlignmentOptions.Center,
            Green, new Vector2(0.54f, 0.185f), new Vector2(0.76f, 0.235f));
    }

    private static void PlaceBaldiButton(GameObject button, Transform parent, Vector2 anchor)
    {
        button.transform.SetParent(parent, false);
        button.SetActive(true);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(94f, 94f);
        Image image = button.GetComponent<Image>();
        if (image != null) image.preserveAspect = true;
    }

    private void ShowPage(Page page)
    {
        storyPage.SetActive(page == Page.Story);
        controlsPage.SetActive(page == Page.Controls);
        RefreshValues();
    }

    private void RefreshValues()
    {
        float sensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
        if (sensitivitySlider != null) sensitivitySlider.SetValueWithoutNotify(Mathf.Clamp(sensitivity, 0.1f, 10f));
        if (sensitivityValue != null) sensitivityValue.text = sensitivity.ToString("0.0");
        if (storyText != null) storyText.text = CursedHorrorBootstrap.GetHowToPlayTextForCurrentPhase();
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

    private void AddBindingRow(Transform parent, InputAction action)
    {
        GameObject row = CreateImage(action + " Row", parent, new Color(0.82f, 0.81f, 0.92f, 0.8f));
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 44f;
        HorizontalLayoutGroup group = row.AddComponent<HorizontalLayoutGroup>();
        group.padding = new RectOffset(10, 8, 5, 5);
        group.spacing = 8f;
        group.childAlignment = TextAnchor.MiddleLeft;
        group.childControlHeight = true;
        group.childControlWidth = false;

        TextMeshProUGUI actionLabel = CreateLayoutText("Action", row.transform, ActionName(action), 14f, Ink, 145f);
        actionLabel.fontStyle = FontStyles.Bold;
        Button bindingButton = CreateLayoutButton("Binding", row.transform, "", 175f);
        TextMeshProUGUI bindingLabel = bindingButton.GetComponentInChildren<TextMeshProUGUI>();
        bindingLabels[action] = bindingLabel;
        InputAction selectedAction = action;
        bindingButton.onClick.AddListener(delegate { BeginBinding(selectedAction); });
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
                inputManager.KeyboardMapping[action] = new InputBinding(key, KeyCode.None);
                inputManager.Save();
                break;
            }
            yield return null;
        }

        bindingRoutine = null;
        bindingOverlay.SetActive(false);
        RefreshBindings();
    }

    private static bool TryReadKey(out KeyCode key)
    {
        foreach (KeyCode candidate in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (candidate == KeyCode.None || candidate == KeyCode.Escape ||
                candidate == KeyCode.Delete || candidate == KeyCode.Backspace) continue;
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

    private void RefreshBindings()
    {
        if (inputManager == null) return;
        foreach (KeyValuePair<InputAction, TextMeshProUGUI> pair in bindingLabels)
        {
            InputBinding binding = inputManager.Mappings[pair.Key];
            KeyCode visibleKey = binding.primaryKey != KeyCode.None ? binding.primaryKey : binding.secondaryKey;
            pair.Value.text = visibleKey == KeyCode.None
                ? "UNBOUND"
                : inputManager.KeyCodeToDisplayString(visibleKey).ToUpperInvariant();
        }
    }

    private void BuildBindingOverlay(Transform parent)
    {
        bindingOverlay = CreateImage("Binding Capture", parent, new Color(0f, 0f, 0f, 0.94f));
        Stretch(bindingOverlay.GetComponent<RectTransform>());
        CreateText("Capture Title", bindingOverlay.transform, "PRESS A KEY", 30f, FontStyles.Bold,
            TextAlignmentOptions.Center, Color.white, new Vector2(0.10f, 0.48f), new Vector2(0.90f, 0.68f));
        CreateText("Capture Hint", bindingOverlay.transform, "ESC CANCELS  -  DELETE CLEARS", 14f, FontStyles.Normal,
            TextAlignmentOptions.Center, Color.white, new Vector2(0.10f, 0.36f), new Vector2(0.90f, 0.50f));
        bindingOverlay.SetActive(false);
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
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
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
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
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
        text.raycastTarget = false;
        return text;
    }

    private Button CreateButton(string name, Transform parent, string label, Vector2 min, Vector2 max,
        UnityEngine.Events.UnityAction click, Color color)
    {
        GameObject obj = CreateImage(name, parent, color);
        SetAnchors(obj.GetComponent<RectTransform>(), min.x, min.y, max.x, max.y);
        Button button = obj.AddComponent<Button>();
        ConfigureButton(button);
        button.onClick.AddListener(click);
        CreateText("Label", obj.transform, label, 16f, FontStyles.Bold, TextAlignmentOptions.Center,
            Color.white, Vector2.zero, Vector2.one);
        return button;
    }

    private Button CreateLayoutButton(string name, Transform parent, string label, float width)
    {
        GameObject obj = CreateImage(name, parent, Purple);
        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.minWidth = width;
        Button button = obj.AddComponent<Button>();
        ConfigureButton(button);
        CreateText("Label", obj.transform, label, 13f, FontStyles.Bold, TextAlignmentOptions.Center,
            Color.white, Vector2.zero, Vector2.one);
        return button;
    }

    private static void ConfigureButton(Button button)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.82f);
        colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
    }

    private Slider CreateSlider(Transform parent, Vector2 min, Vector2 max)
    {
        GameObject root = new GameObject("Sensitivity Slider", typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent, false);
        SetAnchors(root.GetComponent<RectTransform>(), min.x, min.y, max.x, max.y);

        GameObject track = CreateImage("Background", root.transform, Color.red);
        SetAnchors(track.GetComponent<RectTransform>(), 0f, 0.25f, 1f, 0.75f);
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(root.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        SetAnchors(fillAreaRect, 0f, 0.25f, 1f, 0.75f);
        fillAreaRect.anchoredPosition = new Vector2(-5f, 0f);
        fillAreaRect.sizeDelta = new Vector2(-20f, 0f);
        GameObject fill = CreateImage("Fill", fillArea.transform, Color.green);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        Stretch(fillRect);
        fillRect.sizeDelta = new Vector2(10f, 0f);
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(root.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        Stretch(handleAreaRect);
        handleAreaRect.sizeDelta = new Vector2(-20f, 0f);
        GameObject handle = CreateImage("Handle", handleArea.transform, Color.white);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20f, 20f);
        handleRect.localScale = new Vector3(1.5f, 1.5f, 1f);
        Image handleImage = handle.GetComponent<Image>();
        handleImage.sprite = Resources.Load<Sprite>("CursedMod/SensitivityStick");
        handleImage.preserveAspect = true;

        Slider slider = root.GetComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private RectTransform CreateScrollArea(string name, Transform parent)
    {
        GameObject root = CreateImage(name, parent, new Color(0.78f, 0.77f, 0.90f, 0.48f));
        Stretch(root.GetComponent<RectTransform>());
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
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i].font != null) return labels[i].font;
        }
        return TMP_Settings.defaultFontAsset;
    }
}
