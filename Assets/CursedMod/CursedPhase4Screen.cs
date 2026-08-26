using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Phase 4 is a single final screen. Clicking anywhere closes the application.
/// </summary>
public sealed class CursedPhase4Screen : MonoBehaviour
{
    private static CursedPhase4Screen instance;
    private bool quitting;

    public static void Show()
    {
        if (instance != null) return;
        GameObject root = new GameObject("Cursed Phase 4 Final Screen");
        instance = root.AddComponent<CursedPhase4Screen>();
        DontDestroyOnLoad(root);
    }

    private void Awake()
    {
        BuildScreen();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void BuildScreen()
    {
        Texture2D backgroundTexture = Resources.Load<Texture2D>("CursedMod/Phase4Final");
        if (backgroundTexture == null)
        {
            Debug.LogError("Phase 4 final image could not be loaded.");
            Quit();
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

        GameObject screenObject = new GameObject("Phase 4 Click To Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(Button));
        screenObject.transform.SetParent(transform, false);
        RectTransform screenRect = screenObject.GetComponent<RectTransform>();
        Stretch(screenRect, Vector2.zero, Vector2.one);
        RawImage background = screenObject.GetComponent<RawImage>();
        background.texture = backgroundTexture;
        background.color = Color.white;
        background.raycastTarget = true;
        Button closeButton = screenObject.GetComponent<Button>();
        closeButton.transition = Selectable.Transition.None;
        closeButton.targetGraphic = background;
        closeButton.onClick.AddListener(Quit);

        GameObject textObject = new GameObject("Phase 4 Message", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
        textObject.transform.SetParent(transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        Stretch(textRect, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.61f));
        Text text = textObject.GetComponent<Text>();
        text.text = "You were just a mistake.";
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 82;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 32;
        text.resizeTextMaxSize = 82;
        text.color = new Color(0.82f, 0.01f, 0.01f, 1f);
        text.raycastTarget = false;
        Outline outline = textObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.05f, 0f, 0f, 0.95f);
        outline.effectDistance = new Vector2(3f, -3f);
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void StopEverySound()
    {
        AudioSource[] sources = Resources.FindObjectsOfTypeAll<AudioSource>();
        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i].gameObject.scene.IsValid()) sources[i].Stop();
        }
    }

    private void Quit()
    {
        if (quitting) return;
        quitting = true;
        Debug.Log("Phase 4 screen clicked. Closing application.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
