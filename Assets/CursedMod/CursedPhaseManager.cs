using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Persists the four-stage horror flow between Windows launches.
/// Phase 3 unlocks Phase 4 only when its saved password is entered correctly.
/// </summary>
public static class CursedPhaseManager
{
    // Versioned so updating from the earlier test build starts the revised
    // Phase 1 flow once instead of inheriting its already-unlocked Phase 2.
    private const string Phase2Key = "CursedHorrorPhase2Unlocked_v2";
    private const string Phase3Key = "CursedHorrorPhase3Unlocked_v1";
    private const string Phase3PasswordKey = "CursedHorrorPhase3Password_v1";
    private const string Phase4Key = "CursedHorrorPhase4Unlocked_v1";
    private static bool warningVisible;

    public static bool IsPhase2
    {
        get { return PlayerPrefs.GetInt(Phase2Key, 0) == 1; }
    }

    public static bool IsPhase3
    {
        get { return PlayerPrefs.GetInt(Phase3Key, 0) == 1; }
    }

    public static string Phase3Password
    {
        get { return PlayerPrefs.GetString(Phase3PasswordKey, "0000"); }
    }

    public static bool IsPhase4
    {
        get { return PlayerPrefs.GetInt(Phase4Key, 0) == 1; }
    }

    /// <summary>
    /// The 31718 route belongs exclusively to Phase 2. Later phases can retain
    /// the Phase 2 preference while their own screens are active.
    /// </summary>
    public static bool IsTestRoomEnabled
    {
        get { return IsPhase2 && !IsPhase3 && !IsPhase4; }
    }

    public static void UnlockPhase3(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length != 4 || !IsFourDigitPassword(password))
        {
            Debug.LogError("Phase 3 password must contain exactly four digits.");
            return;
        }
        PlayerPrefs.SetString(Phase3PasswordKey, password);
        PlayerPrefs.SetInt(Phase3Key, 1);
        PlayerPrefs.Save();
    }

    private static bool IsFourDigitPassword(string password)
    {
        for (int i = 0; i < password.Length; i++)
        {
            if (password[i] < '0' || password[i] > '9') return false;
        }
        return true;
    }

    public static void UnlockPhase4()
    {
        PlayerPrefs.SetInt(Phase4Key, 1);
        PlayerPrefs.DeleteKey(Phase2Key);
        PlayerPrefs.DeleteKey(Phase3Key);
        PlayerPrefs.DeleteKey(Phase3PasswordKey);
        PlayerPrefs.Save();
    }

    public static void ResetToPhase1()
    {
        warningVisible = false;
        PlayerPrefs.DeleteKey(Phase2Key);
        PlayerPrefs.DeleteKey(Phase3Key);
        PlayerPrefs.DeleteKey(Phase3PasswordKey);
        PlayerPrefs.DeleteKey(Phase4Key);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Returns true when normal answer processing must stop for the warning.
    /// </summary>
    public static bool HandleSecondNotebookFinalAnswer()
    {
        if (IsPhase2)
        {
            CursedHorrorBootstrap.ActivateHorror();
            return false;
        }

        return ShowPiracyWarning();
    }

    public static bool HandleFirstNotebookWrongAnswer()
    {
        if (IsPhase2)
        {
            CursedHorrorBootstrap.ActivateHorror();
            return false;
        }
        return ShowPiracyWarning();
    }

    private static bool ShowPiracyWarning()
    {
        if (warningVisible) return true;
        warningVisible = true;

        Texture2D warningTexture = Resources.Load<Texture2D>("CursedMod/PiracyWarningPhase1");
        if (warningTexture == null)
        {
            Debug.LogError("Phase 1 warning texture could not be loaded.");
            warningVisible = false;
            return false;
        }

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        GameObject canvasObject = new GameObject("Phase 1 Piracy Warning", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32750;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1672f, 941f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject screen = new GameObject("Click To Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(Button));
        screen.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = screen.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        RawImage image = screen.GetComponent<RawImage>();
        image.texture = warningTexture;
        image.color = Color.white;
        image.raycastTarget = true;

        Button button = screen.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;
        button.onClick.AddListener(UnlockPhase2AndQuit);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        AudioListener.pause = true;
        Time.timeScale = 0f;
        return true;
    }

    private static void UnlockPhase2AndQuit()
    {
        PlayerPrefs.SetInt(Phase2Key, 1);
        PlayerPrefs.Save();
        Application.Quit();
    }
}
