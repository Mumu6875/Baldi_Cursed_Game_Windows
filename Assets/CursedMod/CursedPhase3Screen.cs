using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Phase 3 is a single password screen. It uses the password shown at the end
/// of Phase 2 and closes the application after submission or 66.6 seconds.
/// </summary>
public sealed class CursedPhase3Screen : MonoBehaviour
{
    private const float LifetimeSeconds = 66.6f;
    private static CursedPhase3Screen instance;

    private float remainingTime;
    private string enteredPassword = string.Empty;
    private Text enteredText;
    private bool quitting;

    public static void Show()
    {
        if (instance != null) return;
        GameObject root = new GameObject("Cursed Phase 3 Password Screen");
        instance = root.AddComponent<CursedPhase3Screen>();
        DontDestroyOnLoad(root);
    }

    private void Awake()
    {
        remainingTime = LifetimeSeconds;
        BuildScreen();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Update()
    {
        if (quitting) return;

        string typed = Input.inputString;
        for (int i = 0; i < typed.Length; i++)
        {
            char character = typed[i];
            if (character >= '0' && character <= '9')
            {
                AddDigit(character - '0');
            }
            else if (character == '\b')
            {
                Backspace();
            }
            else if (character == '\n' || character == '\r')
            {
                SubmitPassword();
            }
        }

        if (Input.GetKeyDown(KeyCode.Delete)) ClearPassword();
        if (quitting) return;

        remainingTime -= Time.unscaledDeltaTime;
        if (remainingTime <= 0f)
        {
            FailAndQuit("Phase 3 timeout after 66.6 seconds.");
        }
    }

    private void BuildScreen()
    {
        Texture2D backgroundTexture = Resources.Load<Texture2D>("CursedMod/Phase3Password");
        if (backgroundTexture == null)
        {
            Debug.LogError("Phase 3 password image could not be loaded.");
            FailAndQuit("Missing Phase 3 password image.");
            return;
        }

        StopEverySound();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        AudioListener.pause = true;
        Time.timeScale = 0f;

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1672f, 941f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject backgroundObject = new GameObject("Phase 3 Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        backgroundObject.transform.SetParent(transform, false);
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        Stretch(backgroundRect, Vector2.zero, Vector2.one);
        RawImage background = backgroundObject.GetComponent<RawImage>();
        background.texture = backgroundTexture;
        background.color = Color.white;
        background.raycastTarget = false;

        Text prompt = MakeTextFromPixels("Password Prompt", 620f, 480f, 1050f, 660f, 54);
        prompt.text = "ENTER PASSWORD";
        prompt.color = Color.white;

        enteredText = MakeTextFromPixels("Entered Password", 680f, 770f, 1030f, 850f, 70);
        enteredText.color = Color.white;
        RefreshEnteredText();

        CreateDigitButtonFromPixels(7, 1135f, 430f, 1197f, 497f);
        CreateDigitButtonFromPixels(8, 1207f, 430f, 1268f, 497f);
        CreateDigitButtonFromPixels(9, 1278f, 429f, 1340f, 497f);
        CreateDigitButtonFromPixels(4, 1135f, 508f, 1197f, 574f);
        CreateDigitButtonFromPixels(5, 1207f, 508f, 1269f, 574f);
        CreateDigitButtonFromPixels(6, 1279f, 508f, 1342f, 574f);
        CreateDigitButtonFromPixels(1, 1134f, 585f, 1197f, 651f);
        CreateDigitButtonFromPixels(2, 1207f, 585f, 1269f, 650f);
        CreateDigitButtonFromPixels(3, 1279f, 585f, 1343f, 650f);
        MakeClickAreaFromPixels("Clear Password", 1135f, 663f, 1197f, 730f, ClearPassword);
        CreateDigitButtonFromPixels(0, 1207f, 663f, 1270f, 730f);
        MakeClickAreaFromPixels("Backspace", 1279f, 663f, 1342f, 730f, Backspace);
        MakeClickAreaFromPixels("Submit Password", 1142f, 733f, 1331f, 927f, SubmitPassword);
    }

    private Text MakeTextFromPixels(string objectName, float left, float top, float right, float bottom, int fontSize)
    {
        return MakeText(objectName, PixelAnchorMin(left, bottom), PixelAnchorMax(right, top), fontSize);
    }

    private Text MakeText(string objectName, Vector2 anchorMin, Vector2 anchorMax, int fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
        textObject.transform.SetParent(transform, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        Stretch(rect, anchorMin, anchorMax);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 24;
        text.resizeTextMaxSize = fontSize;
        text.raycastTarget = false;
        Outline outline = textObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.08f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
        return text;
    }

    private void CreateDigitButtonFromPixels(int digit, float left, float top, float right, float bottom)
    {
        int capturedDigit = digit;
        MakeClickAreaFromPixels(
            "Digit " + digit,
            left,
            top,
            right,
            bottom,
            delegate { AddDigit(capturedDigit); });
    }

    private void MakeClickAreaFromPixels(string objectName, float left, float top, float right, float bottom, UnityAction action)
    {
        MakeClickArea(objectName, PixelAnchorMin(left, bottom), PixelAnchorMax(right, top), action);
    }

    private static Vector2 PixelAnchorMin(float left, float bottom)
    {
        return new Vector2(left / 1672f, 1f - bottom / 941f);
    }

    private static Vector2 PixelAnchorMax(float right, float top)
    {
        return new Vector2(right / 1672f, 1f - top / 941f);
    }

    private void MakeClickArea(string objectName, Vector2 anchorMin, Vector2 anchorMax, UnityAction action)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(transform, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        Stretch(rect, anchorMin, anchorMax);
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.001f);
        image.raycastTarget = true;
        Button button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;
        button.onClick.AddListener(action);
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void AddDigit(int digit)
    {
        if (quitting || enteredPassword.Length >= 4) return;
        enteredPassword += digit.ToString();
        RefreshEnteredText();
    }

    private void Backspace()
    {
        if (quitting || enteredPassword.Length == 0) return;
        enteredPassword = enteredPassword.Substring(0, enteredPassword.Length - 1);
        RefreshEnteredText();
    }

    private void ClearPassword()
    {
        if (quitting || enteredPassword.Length == 0) return;
        enteredPassword = string.Empty;
        RefreshEnteredText();
    }

    private void RefreshEnteredText()
    {
        if (enteredText == null) return;
        enteredText.text = enteredPassword.PadRight(4, '_');
    }

    private void SubmitPassword()
    {
        bool correct = enteredPassword == CursedPhaseManager.Phase3Password;
        if (correct)
        {
            CursedPhaseManager.UnlockPhase4();
            Quit("Correct Phase 3 password. Phase 4 unlocked.");
            return;
        }

        FailAndQuit("Wrong Phase 3 password. Progress reset to Phase 1.");
    }

    private void FailAndQuit(string reason)
    {
        CursedPhaseManager.ResetToPhase1();
        Quit(reason);
    }

    private static void StopEverySound()
    {
        AudioSource[] sources = Resources.FindObjectsOfTypeAll<AudioSource>();
        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i].gameObject.scene.IsValid()) sources[i].Stop();
        }
    }

    private void Quit(string reason)
    {
        if (quitting) return;
        quitting = true;
        Debug.Log(reason);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
