using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Handles the Phase 2 final-exit sequence. The fourth exit is sealed, then
/// the Room 99 doorway and narrow room already serialized in School.unity are
/// revealed. No cafeteria wall/door search or runtime room construction occurs.
/// </summary>
public class CursedFinalExitSequence : MonoBehaviour
{
    private static CursedFinalExitSequence instance;

    private bool sequenceActive;
    private Canvas overlayCanvas;
    private Image fadeImage;
    private Text messageText;
    private GameControllerScript activeController;
    private bool completionVisible;
    private bool phase2CompletionHandled;
    private string completionCode;

    public static void EnsureInstalled()
    {
        if (instance != null) return;

        GameObject host = new GameObject("Cursed Final Exit Sequence");
        instance = host.AddComponent<CursedFinalExitSequence>();
    }

    public static bool TryStart(ExitTriggerScript exit, Collider playerCollider, GameControllerScript gc)
    {
        EnsureInstalled();
        if (instance.sequenceActive || exit == null || playerCollider == null || gc == null) return false;

        instance.StartCoroutine(instance.Begin(exit, gc));
        return true;
    }

    private IEnumerator Begin(ExitTriggerScript exit, GameControllerScript gc)
    {
        sequenceActive = true;
        activeController = gc;

        DisableAllFinalExitTriggers();
        SealFinalExit(exit);
        BuildOverlay();

        if (messageText != null) messageText.text = string.Empty;
        if (gc.notebookCount != null) gc.notebookCount.text = "Find the door.";

        if (!ActivateRoom99SceneGeometry())
        {
            Debug.LogError("Serialized Room 99 cafeteria geometry could not be activated.");
            yield break;
        }

        yield return null;
    }

    private void SealFinalExit(ExitTriggerScript exit)
    {
        // The first three finale exits are sealed with EntranceScript.Lower().
        // Do the same for exit four, but do not teleport the player anywhere:
        // the final sequence now starts from NearExitTriggerScript while the
        // player is still safely on the school side.
        EntranceScript entrance = FindNearestEntrance(exit.transform);
        if (entrance != null)
        {
            entrance.Lower();
        }

        Collider source = exit.GetComponent<Collider>();
        Bounds exitBounds = source != null
            ? source.bounds
            : new Bounds(exit.transform.position, new Vector3(4f, 5f, 1.2f));

        GameObject blocker = new GameObject("Final Exit Safety Blocker");
        blocker.transform.position = exitBounds.center;

        BoxCollider box = blocker.AddComponent<BoxCollider>();
        box.size = new Vector3(
            Mathf.Max(1.6f, exitBounds.size.x),
            Mathf.Max(4.5f, exitBounds.size.y),
            Mathf.Max(1.6f, exitBounds.size.z));
    }

    private static EntranceScript FindNearestEntrance(Transform exitTransform)
    {
        EntranceScript direct = exitTransform.GetComponentInParent<EntranceScript>();
        if (direct != null) return direct;

        EntranceScript[] entrances = Resources.FindObjectsOfTypeAll<EntranceScript>();
        EntranceScript nearest = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < entrances.Length; i++)
        {
            EntranceScript candidate = entrances[i];
            if (candidate == null || !candidate.gameObject.scene.IsValid()) continue;
            if (candidate.gameObject.scene != exitTransform.gameObject.scene) continue;

            float distance = (candidate.transform.position - exitTransform.position).sqrMagnitude;
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            nearest = candidate;
        }

        return nearest;
    }

    private static void DisableAllFinalExitTriggers()
    {
        ExitTriggerScript[] exits = Resources.FindObjectsOfTypeAll<ExitTriggerScript>();
        for (int i = 0; i < exits.Length; i++)
        {
            ExitTriggerScript current = exits[i];
            if (current != null && current.gameObject.scene.IsValid())
            {
                current.enabled = false;
            }
        }

        NearExitTriggerScript[] nearExits = Resources.FindObjectsOfTypeAll<NearExitTriggerScript>();
        for (int i = 0; i < nearExits.Length; i++)
        {
            NearExitTriggerScript current = nearExits[i];
            if (current != null && current.gameObject.scene.IsValid())
            {
                current.enabled = false;
            }
        }
    }

    private bool ActivateRoom99SceneGeometry()
    {
        Transform wallCover = FindSceneTransform("Room99 Wall Cover");
        Transform doorAssembly = FindSceneTransform("Room99 Door Assembly");
        Transform room = FindSceneTransform("Room99 Narrow Room");

        if (wallCover == null || doorAssembly == null || room == null)
        {
            Debug.LogError(
                "Room 99 scene geometry is incomplete. Expected Room99 Wall Cover, " +
                "Room99 Door Assembly and Room99 Narrow Room in School.unity.");
            return false;
        }

        // School.unity owns the geometry. The solid cafeteria wall is present
        // during normal play; the finale only swaps that cover for the already
        // serialized real DoorScript doorway and narrow room.
        wallCover.gameObject.SetActive(false);
        doorAssembly.gameObject.SetActive(true);
        room.gameObject.SetActive(true);

        DoorScript[] doors =
            doorAssembly.GetComponentsInChildren<DoorScript>(true);

        if (doors.Length == 0)
        {
            room.gameObject.SetActive(false);
            doorAssembly.gameObject.SetActive(false);
            wallCover.gameObject.SetActive(true);
            Debug.LogError("Room99 Door Assembly contains no DoorScript.");
            return false;
        }

        for (int i = 0; i < doors.Length; i++)
        {
            DoorScript door = doors[i];
            if (door == null) continue;

            door.enabled = true;
            door.openingDistance = Mathf.Max(door.openingDistance, 15f);
            door.UnlockDoor();
        }

        InstallRoom99Triggers(room);
        Physics.SyncTransforms();
        return true;
    }

    private static void InstallRoom99Triggers(Transform room)
    {
        if (FindDirectChild(room, "Room99 Entry Trigger") == null)
        {
            GameObject entryTrigger =
                new GameObject("Room99 Entry Trigger");

            entryTrigger.transform.SetParent(room, false);
            entryTrigger.transform.localPosition =
                new Vector3(0f, 1.7f, 1.65f);

            BoxCollider entryBox =
                entryTrigger.AddComponent<BoxCollider>();

            entryBox.isTrigger = true;
            entryBox.size = new Vector3(4.5f, 3.4f, 1.4f);
            entryTrigger.AddComponent<CursedPhysicalRoomEntryTrigger>();
        }

        if (FindDirectChild(room, "Room99 Phase 3 Trigger") == null)
        {
            GameObject endTrigger =
                new GameObject("Room99 Phase 3 Trigger");

            endTrigger.transform.SetParent(room, false);
            endTrigger.transform.localPosition =
                new Vector3(0f, 1.7f, 7.5f);

            BoxCollider endBox =
                endTrigger.AddComponent<BoxCollider>();

            endBox.isTrigger = true;
            endBox.size = new Vector3(4.5f, 3.4f, 1.4f);
            endTrigger.AddComponent<CursedMazeEndTrigger>();
        }
    }

    private static Transform FindDirectChild(
        Transform parent,
        string objectName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && child.name == objectName)
            {
                return child;
            }
        }

        return null;
    }

    private static Transform FindSceneTransform(string objectName)
    {
        Transform[] transforms =
            Resources.FindObjectsOfTypeAll<Transform>();

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current == null || !current.gameObject.scene.IsValid()) continue;
            if (current.name == objectName) return current;
        }

        return null;
    }

    private void BuildOverlay()
    {
        GameObject canvasObject =
            new GameObject(
                "Phase 2 Exit Overlay",
                typeof(Canvas),
                typeof(CanvasScaler));

        overlayCanvas = canvasObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 32765;

        GameObject fade =
            new GameObject(
                "Fade",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

        fade.transform.SetParent(canvasObject.transform, false);

        RectTransform fadeRect = fade.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;

        fadeImage = fade.GetComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;

        GameObject message =
            new GameObject(
                "Message",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));

        message.transform.SetParent(canvasObject.transform, false);

        RectTransform textRect =
            message.GetComponent<RectTransform>();

        textRect.anchorMin = new Vector2(0.1f, 0.35f);
        textRect.anchorMax = new Vector2(0.9f, 0.65f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        messageText = message.GetComponent<Text>();
        messageText.font =
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        messageText.fontSize = 42;
        messageText.alignment = TextAnchor.MiddleCenter;
        messageText.color = Color.white;
        messageText.raycastTarget = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeImage == null) yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float alpha =
                Mathf.Lerp(
                    from,
                    to,
                    Mathf.Clamp01(elapsed / duration));

            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;

            yield return null;
        }
    }

    public void FinishPhase2()
    {
        if (!gameObject.activeInHierarchy || completionVisible) return;
        StartCoroutine(FinishPhase2Routine());
    }

    private IEnumerator FinishPhase2Routine()
    {
        if (messageText != null) messageText.text = string.Empty;
        if (fadeImage != null)
        {
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
        }

        yield return Fade(0f, 1f, 0.65f);
        yield return new WaitForSecondsRealtime(0.25f);

        if (CursedPhaseManager.IsPhase2)
        {
            ShowPhase2CompletionScreen();
            yield break;
        }

        Debug.LogWarning(
            "Phase 2 completion was requested outside Phase 2. Ignoring request.");
    }

    private void ShowPhase2CompletionScreen()
    {
        if (completionVisible) return;

        completionVisible = true;
        completionCode = GenerateFourDigitCode();

        Texture2D thinkPadTexture =
            Resources.Load<Texture2D>(
                "CursedMod/CursedThinkPad");

        if (thinkPadTexture == null)
        {
            Debug.LogError(
                "Cursed Think Pad image could not be loaded. Phase 3 remains locked.");
            completionVisible = false;
            return;
        }

        if (overlayCanvas != null)
        {
            Destroy(overlayCanvas.gameObject);
            overlayCanvas = null;
            fadeImage = null;
            messageText = null;
        }

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
        }

#if UNITY_ANDROID || UNITY_IOS
        CursedMobileInput.Hide();
#else
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
#endif

        AudioListener.pause = true;
        Time.timeScale = 0f;

        GameObject canvasObject =
            new GameObject(
                "Phase 2 Completion Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32766;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution =
            new Vector2(1672f, 941f);
        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject screen =
            new GameObject(
                "Phase 2 Password Screen",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));

        screen.transform.SetParent(
            canvasObject.transform,
            false);

        RectTransform screenRect =
            screen.GetComponent<RectTransform>();

        screenRect.anchorMin = Vector2.zero;
        screenRect.anchorMax = Vector2.one;
        screenRect.offsetMin = Vector2.zero;
        screenRect.offsetMax = Vector2.zero;

        Image background =
            screen.GetComponent<Image>();

        background.color = Color.black;
        background.raycastTarget = true;

        Button continueButton =
            screen.GetComponent<Button>();

        continueButton.transition =
            Selectable.Transition.None;
        continueButton.targetGraphic = background;
        continueButton.onClick.AddListener(
            CompletePhase2AndQuit);

        GameObject thinkPadObject =
            new GameObject(
                "Cursed Think Pad Password Display",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage),
                typeof(AspectRatioFitter));

        thinkPadObject.transform.SetParent(
            screen.transform,
            false);

        RectTransform thinkPadRect =
            thinkPadObject.GetComponent<RectTransform>();

        thinkPadRect.anchorMin = Vector2.zero;
        thinkPadRect.anchorMax = Vector2.one;
        thinkPadRect.offsetMin = Vector2.zero;
        thinkPadRect.offsetMax = Vector2.zero;

        RawImage thinkPadImage =
            thinkPadObject.GetComponent<RawImage>();

        thinkPadImage.texture = thinkPadTexture;
        thinkPadImage.color = Color.white;
        thinkPadImage.raycastTarget = false;

        AspectRatioFitter thinkPadFitter =
            thinkPadObject.GetComponent<AspectRatioFitter>();

        thinkPadFitter.aspectMode =
            AspectRatioFitter.AspectMode.FitInParent;
        thinkPadFitter.aspectRatio = 1448f / 1086f;

        GameObject codeObject =
            new GameObject(
                "Random Four Digit Code",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));

        codeObject.transform.SetParent(
            thinkPadObject.transform,
            false);

        RectTransform codeRect =
            codeObject.GetComponent<RectTransform>();

        codeRect.anchorMin =
            new Vector2(462f / 1448f, 1f - 570f / 1086f);
        codeRect.anchorMax =
            new Vector2(997f / 1448f, 1f - 333f / 1086f);
        codeRect.offsetMin = Vector2.zero;
        codeRect.offsetMax = Vector2.zero;

        TextMeshProUGUI codeText =
            codeObject.GetComponent<TextMeshProUGUI>();

        codeText.text = completionCode;
        codeText.font = TMP_Settings.defaultFontAsset;
        codeText.fontSize = 160f;
        codeText.fontStyle = FontStyles.Bold;
        codeText.alignment = TextAlignmentOptions.Center;
        codeText.color = Color.white;
        codeText.enableAutoSizing = true;
        codeText.fontSizeMin = 72f;
        codeText.fontSizeMax = 180f;
        codeText.textWrappingMode = TextWrappingModes.NoWrap;
        codeText.raycastTarget = false;

    }

    private void CompletePhase2AndQuit()
    {
        if (phase2CompletionHandled) return;
        phase2CompletionHandled = true;

        CursedPhaseManager.UnlockPhase3(completionCode);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static string GenerateFourDigitCode()
    {
        return UnityEngine.Random.Range(0, 10000)
            .ToString("D4");
    }

    public static void FinishPhase2FromCorridor()
    {
        if (instance != null)
        {
            instance.FinishPhase2();
        }
    }

    public static void EnterCorridor()
    {
        if (instance == null ||
            instance.activeController == null)
        {
            return;
        }

        GameControllerScript gc =
            instance.activeController;

        SetInactive(gc.baldiTutor);
        SetInactive(gc.baldi);
        SetInactive(gc.principal);
        SetInactive(gc.crafters);
        SetInactive(gc.playtime);
        SetInactive(gc.gottaSweep);
        SetInactive(gc.bully);
        SetInactive(gc.firstPrize);
        SetInactive(gc.TestEnemy);

        if (gc.schoolMusic != null)
        {
            gc.schoolMusic.Stop();
        }

        if (gc.learnMusic != null)
        {
            gc.learnMusic.Stop();
        }
    }

    private static void SetInactive(GameObject target)
    {
        if (target != null)
        {
            target.SetActive(false);
        }
    }
}

public class CursedMazeEndTrigger : MonoBehaviour
{
    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered ||
            !other.CompareTag("Player"))
        {
            return;
        }

        triggered = true;
        CursedFinalExitSequence.FinishPhase2FromCorridor();
    }
}

/// <summary>
/// Starts the special-room state only after the player has physically walked
/// through the normal DoorScript door. It does not move or teleport the player.
/// </summary>
public class CursedPhysicalRoomEntryTrigger : MonoBehaviour
{
    private bool entered;

    private void OnTriggerEnter(Collider other)
    {
        if (entered ||
            !other.CompareTag("Player"))
        {
            return;
        }

        entered = true;
        CursedFinalExitSequence.EnterCorridor();
    }
}
