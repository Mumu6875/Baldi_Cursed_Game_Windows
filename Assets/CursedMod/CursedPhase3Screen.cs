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

        Text prompt = MakeText("Password Prompt", new Vector2(0.306f, 0.225f), new Vector2(0.620f, 0.541f), 54);
        prompt.text = "Enter the password";
        prompt.color = new Color(0.78f, 0.015f, 0.015f, 1f);

        enteredText = MakeText("Entered Password", new Vector2(0.331f, 0.010f), new Vector2(0.616f, 0.170f), 70);
        enteredText.color = new Color(0.72f, 0.01f, 0.01f, 1f);
        RefreshEnteredText();

        MakeClickArea("Clear Password Top", new Vector2(0.229f, 0.466f), new Vector2(0.281f, 0.548f), ClearPassword);
        MakeClickArea("Clear Password Middle", new Vector2(0.229f, 0.378f), new Vector2(0.281f, 0.461f), ClearPassword);
        MakeClickArea("Clear Password Bottom", new Vector2(0.229f, 0.291f), new Vector2(0.281f, 0.374f), ClearPassword);
        CreateDigitButton(7, new Vector2(0.644f, 0.474f), new Vector2(0.681f, 0.545f));
        CreateDigitButton(8, new Vector2(0.686f, 0.474f), new Vector2(0.724f, 0.545f));
        CreateDigitButton(9, new Vector2(0.729f, 0.474f), new Vector2(0.767f, 0.545f));
        CreateDigitButton(4, new Vector2(0.644f, 0.395f), new Vector2(0.681f, 0.462f));
        CreateDigitButton(5, new Vector2(0.686f, 0.395f), new Vector2(0.724f, 0.462f));
        CreateDigitButton(6, new Vector2(0.729f, 0.395f), new Vector2(0.767f, 0.462f));
        CreateDigitButton(1, new Vector2(0.644f, 0.310f), new Vector2(0.681f, 0.380f));
        CreateDigitButton(2, new Vector2(0.686f, 0.310f), new Vector2(0.724f, 0.380f));
        CreateDigitButton(3, new Vector2(0.729f, 0.310f), new Vector2(0.767f, 0.380f));
        CreateDigitButton(0, new Vector2(0.643f, 0.226f), new Vector2(0.724f, 0.296f));
        MakeClickArea("Backspace", new Vector2(0.729f, 0.226f), new Vector2(0.767f, 0.296f), Backspace);
        MakeClickArea("Submit Password", new Vector2(0.644f, 0.015f), new Vector2(0.769f, 0.190f), SubmitPassword);
    }

    private Text MakeText(string objectName, Vector2 anchorMin, Vector2 anchorMax, int fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
        textObject.transform.SetParent(transform, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        Stretch(rect, anchorMin, anchorMax);
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
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

    private void CreateDigitButton(int digit, Vector2 anchorMin, Vector2 anchorMax)
    {
        int capturedDigit = digit;
        MakeClickArea("Digit " + digit, anchorMin, anchorMax, delegate { AddDigit(capturedDigit); });
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
