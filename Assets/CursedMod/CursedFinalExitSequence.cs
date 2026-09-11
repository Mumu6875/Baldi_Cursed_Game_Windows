using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Handles the Phase 2 final-exit sequence. The fourth exit is sealed, a real
/// in-game door is cloned onto a free cafeteria wall, and that door leads to a
/// short straight corridor that finishes Phase 2.
/// </summary>
public class CursedFinalExitSequence : MonoBehaviour
{
    private const float CorridorWidth = 5.6f;
    private const float CorridorHeight = 4.6f;
    private const float CorridorLength = 30f;
    private const float CorridorStartOffset = 1.15f;

    private static CursedFinalExitSequence instance;

    private bool sequenceActive;
    private Canvas overlayCanvas;
    private Image fadeImage;
    private Text messageText;
    private GameObject cafeteriaDoor;
    private BoxCollider cafeteriaDoorTrigger;
    private CursedRoom99Portal cafeteriaDoorPortal;
    private GameControllerScript activeController;
    private bool completionVisible;
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

        instance.StartCoroutine(instance.Begin(exit, playerCollider, gc));
        return true;
    }

    private IEnumerator Begin(ExitTriggerScript exit, Collider playerCollider, GameControllerScript gc)
    {
        sequenceActive = true;
        activeController = gc;

        DisableAllFinalExitTriggers();
        SealFinalExit(exit, playerCollider);
        BuildOverlay();

        if (messageText != null) messageText.text = string.Empty;
        if (gc.notebookCount != null) gc.notebookCount.text = "Find the door.";

        Vector3 corridorSpawn;
        Quaternion corridorFacing;
        if (!BuildCafeteriaDoorAndCorridor(out corridorSpawn, out corridorFacing))
        {
            Debug.LogError("Phase 2 corridor could not be created.");
            yield break;
        }

        ActivateCafeteriaDoorPortal(corridorSpawn, corridorFacing);
    }

    private void SealFinalExit(ExitTriggerScript exit, Collider playerCollider)
    {
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

        MovePlayerBackInside(exit.transform, playerCollider);
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

    private static void MovePlayerBackInside(Transform exitTransform, Collider playerCollider)
    {
        PlayerScript player = playerCollider.GetComponent<PlayerScript>();
        if (player == null) player = playerCollider.GetComponentInParent<PlayerScript>();
        if (player == null) return;

        Vector3 exitPosition = exitTransform.position;
        Vector3 forward = exitTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 optionA = exitPosition + forward * 2.8f;
        Vector3 optionB = exitPosition - forward * 2.8f;
        float aDistance = optionA.x * optionA.x + optionA.z * optionA.z;
        float bDistance = optionB.x * optionB.x + optionB.z * optionB.z;
        Vector3 target = aDistance <= bDistance ? optionA : optionB;
        target.y = player.height;

        CharacterController controller = player.cc;
        if (controller != null) controller.enabled = false;
        player.transform.position = target;
        if (controller != null) controller.enabled = true;
    }

    private static void DisableAllFinalExitTriggers()
    {
        ExitTriggerScript[] exits = Resources.FindObjectsOfTypeAll<ExitTriggerScript>();
        for (int i = 0; i < exits.Length; i++)
        {
            if (exits[i] != null && exits[i].gameObject.scene.IsValid())
            {
                exits[i].enabled = false;
            }
        }
    }

    private bool BuildCafeteriaDoorAndCorridor(out Vector3 corridorSpawn, out Quaternion corridorFacing)
    {
        corridorSpawn = Vector3.zero;
        corridorFacing = Quaternion.identity;

        Transform cafeteria = FindSceneTransform("Cafeteria");
        if (cafeteria == null)
        {
            Debug.LogError("Cafeteria root was not found.");
            return false;
        }

        Bounds localBounds;
        if (!TryGetLocalBounds(cafeteria, out localBounds))
        {
            Debug.LogError("Could not determine cafeteria bounds.");
            return false;
        }

        Vector3 doorLocalPosition;
        Vector3 outwardLocalDirection;
        ChooseFreeCafeteriaWall(cafeteria, localBounds, out doorLocalPosition, out outwardLocalDirection);

        Vector3 doorWorldPosition = cafeteria.TransformPoint(doorLocalPosition);
        Vector3 outwardWorldDirection = cafeteria.TransformDirection(outwardLocalDirection);
        outwardWorldDirection.y = 0f;
        if (outwardWorldDirection.sqrMagnitude < 0.01f) outwardWorldDirection = Vector3.forward;
        outwardWorldDirection.Normalize();

        corridorFacing = Quaternion.LookRotation(outwardWorldDirection, Vector3.up);

        if (!CloneExistingDoor(cafeteria, doorWorldPosition, corridorFacing))
        {
            Debug.LogError("No existing DoorScript could be cloned for the cafeteria Phase 2 door.");
            return false;
        }

        float floorY = cafeteria.TransformPoint(new Vector3(0f, localBounds.min.y, 0f)).y;
        Vector3 floorBase = new Vector3(doorWorldPosition.x, floorY, doorWorldPosition.z);

        BuildStraightCorridor(floorBase, outwardWorldDirection, corridorFacing, out corridorSpawn);
        BuildPortalTrigger(doorWorldPosition, outwardWorldDirection, corridorFacing);

        Debug.Log("Phase 2 cafeteria door placed on the least-obstructed wall and connected to a straight corridor.");
        return true;
    }

    private static bool TryGetLocalBounds(Transform root, out Bounds localBounds)
    {
        bool initialized = false;
        localBounds = new Bounds(Vector3.zero, Vector3.zero);

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null) continue;
            EncapsulateWorldBounds(root, renderer.bounds, ref initialized, ref localBounds);
        }

        if (initialized) return true;

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || collider.isTrigger) continue;
            EncapsulateWorldBounds(root, collider.bounds, ref initialized, ref localBounds);
        }

        return initialized;
    }

    private static void EncapsulateWorldBounds(Transform root, Bounds worldBounds, ref bool initialized, ref Bounds localBounds)
    {
        Vector3 center = worldBounds.center;
        Vector3 extents = worldBounds.extents;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 worldPoint = center + Vector3.Scale(extents, new Vector3(x, y, z));
                    Vector3 localPoint = root.InverseTransformPoint(worldPoint);

                    if (!initialized)
                    {
                        localBounds = new Bounds(localPoint, Vector3.zero);
                        initialized = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(localPoint);
                    }
                }
            }
        }
    }

    private static void ChooseFreeCafeteriaWall(
        Transform cafeteria,
        Bounds bounds,
        out Vector3 bestLocalPosition,
        out Vector3 bestOutwardLocal)
    {
        bestLocalPosition = new Vector3(bounds.center.x, bounds.min.y + 2.8f, bounds.max.z);
        bestOutwardLocal = Vector3.forward;
        int bestScore = int.MaxValue;

        float[] along = { 0.28f, 0.5f, 0.72f };

        for (int side = 0; side < 4; side++)
        {
            for (int i = 0; i < along.Length; i++)
            {
                float t = along[i];
                Vector3 localPosition;
                Vector3 localOutward;

                if (side == 0)
                {
                    localPosition = new Vector3(
                        Mathf.Lerp(bounds.min.x, bounds.max.x, t),
                        bounds.min.y + 2.8f,
                        bounds.max.z);
                    localOutward = Vector3.forward;
                }
                else if (side == 1)
                {
                    localPosition = new Vector3(
                        Mathf.Lerp(bounds.min.x, bounds.max.x, t),
                        bounds.min.y + 2.8f,
                        bounds.min.z);
                    localOutward = Vector3.back;
                }
                else if (side == 2)
                {
                    localPosition = new Vector3(
                        bounds.max.x,
                        bounds.min.y + 2.8f,
                        Mathf.Lerp(bounds.min.z, bounds.max.z, t));
                    localOutward = Vector3.right;
                }
                else
                {
                    localPosition = new Vector3(
                        bounds.min.x,
                        bounds.min.y + 2.8f,
                        Mathf.Lerp(bounds.min.z, bounds.max.z, t));
                    localOutward = Vector3.left;
                }

                int score = ScoreCorridorSpace(cafeteria, localPosition, localOutward);
                if (score >= bestScore) continue;

                bestScore = score;
                bestLocalPosition = localPosition;
                bestOutwardLocal = localOutward;
            }
        }
    }

    private static int ScoreCorridorSpace(Transform cafeteria, Vector3 localDoor, Vector3 localOutward)
    {
        Vector3 doorWorld = cafeteria.TransformPoint(localDoor);
        Vector3 outwardWorld = cafeteria.TransformDirection(localOutward);
        outwardWorld.y = 0f;
        if (outwardWorld.sqrMagnitude < 0.01f) outwardWorld = Vector3.forward;
        outwardWorld.Normalize();

        Quaternion rotation = Quaternion.LookRotation(outwardWorld, Vector3.up);
        Vector3 center = doorWorld + outwardWorld * (CorridorStartOffset + CorridorLength * 0.5f);

        Collider[] overlaps = Physics.OverlapBox(
            center,
            new Vector3(CorridorWidth * 0.5f, CorridorHeight * 0.5f, CorridorLength * 0.5f),
            rotation,
            ~0,
            QueryTriggerInteraction.Ignore);

        int score = 0;
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider overlap = overlaps[i];
            if (overlap == null) continue;
            if (overlap.transform == cafeteria || overlap.transform.IsChildOf(cafeteria)) continue;

            string objectName = overlap.gameObject.name.ToLowerInvariant();
            if (objectName.Contains("floor") || objectName.Contains("ground")) continue;

            score++;
        }

        return score;
    }

    private bool CloneExistingDoor(Transform cafeteria, Vector3 worldPosition, Quaternion worldRotation)
    {
        DoorScript[] doors = Resources.FindObjectsOfTypeAll<DoorScript>();
        DoorScript template = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < doors.Length; i++)
        {
            DoorScript candidate = doors[i];
            if (candidate == null || !candidate.gameObject.scene.IsValid()) continue;

            float distance = (candidate.transform.position - worldPosition).sqrMagnitude;
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            template = candidate;
        }

        if (template == null) return false;

        cafeteriaDoor = Instantiate(template.gameObject, worldPosition, worldRotation);
        cafeteriaDoor.name = "Cafeteria Phase 2 Door";
        cafeteriaDoor.transform.SetParent(cafeteria, true);

        DoorScript[] clonedDoorScripts = cafeteriaDoor.GetComponentsInChildren<DoorScript>(true);
        for (int i = 0; i < clonedDoorScripts.Length; i++)
        {
            clonedDoorScripts[i].enabled = false;
        }

        Collider[] clonedColliders = cafeteriaDoor.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < clonedColliders.Length; i++)
        {
            clonedColliders[i].enabled = false;
        }

        return true;
    }

    private void BuildStraightCorridor(
        Vector3 floorBase,
        Vector3 outward,
        Quaternion rotation,
        out Vector3 corridorSpawn)
    {
        GameObject root = new GameObject("Cafeteria Phase 2 Straight Corridor");

        Material wallMaterial = CreateMaterial(new Color(0.055f, 0.055f, 0.065f, 1f));
        Material floorMaterial = CreateMaterial(new Color(0.012f, 0.012f, 0.016f, 1f));

        Vector3 right = rotation * Vector3.right;
        float centerDistance = CorridorStartOffset + CorridorLength * 0.5f;
        Vector3 corridorCenter = floorBase + outward * centerDistance;

        CreateOrientedCube(
            "Phase 2 Corridor Floor",
            root.transform,
            corridorCenter - Vector3.up * 0.15f,
            new Vector3(CorridorWidth, 0.3f, CorridorLength),
            rotation,
            floorMaterial);

        CreateOrientedCube(
            "Phase 2 Corridor Ceiling",
            root.transform,
            corridorCenter + Vector3.up * CorridorHeight,
            new Vector3(CorridorWidth, 0.3f, CorridorLength),
            rotation,
            floorMaterial);

        CreateOrientedCube(
            "Phase 2 Corridor Left Wall",
            root.transform,
            corridorCenter - right * (CorridorWidth * 0.5f) + Vector3.up * (CorridorHeight * 0.5f),
            new Vector3(0.24f, CorridorHeight, CorridorLength),
            rotation,
            wallMaterial);

        CreateOrientedCube(
            "Phase 2 Corridor Right Wall",
            root.transform,
            corridorCenter + right * (CorridorWidth * 0.5f) + Vector3.up * (CorridorHeight * 0.5f),
            new Vector3(0.24f, CorridorHeight, CorridorLength),
            rotation,
            wallMaterial);

        Vector3 farEnd = floorBase + outward * (CorridorStartOffset + CorridorLength);
        CreateOrientedCube(
            "Phase 2 Corridor End Wall",
            root.transform,
            farEnd + Vector3.up * (CorridorHeight * 0.5f),
            new Vector3(CorridorWidth, CorridorHeight, 0.24f),
            rotation,
            wallMaterial);

        Vector3 triggerPosition = farEnd - outward * 1.25f + Vector3.up * 1.7f;
        GameObject endTrigger = new GameObject("Phase 2 Corridor End Trigger");
        endTrigger.transform.SetParent(root.transform, true);
        endTrigger.transform.position = triggerPosition;
        endTrigger.transform.rotation = rotation;

        BoxCollider trigger = endTrigger.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(CorridorWidth - 0.5f, 3.4f, 1.8f);
        endTrigger.AddComponent<CursedMazeEndTrigger>();

        corridorSpawn = floorBase + outward * (CorridorStartOffset + 1.2f) + Vector3.up * 0.45f;
    }

    private void BuildPortalTrigger(Vector3 doorWorldPosition, Vector3 outward, Quaternion rotation)
    {
        GameObject portalObject = new GameObject("Cafeteria Phase 2 Door Portal");
        portalObject.transform.position = doorWorldPosition - outward * 0.85f;
        portalObject.transform.rotation = rotation;

        cafeteriaDoorTrigger = portalObject.AddComponent<BoxCollider>();
        cafeteriaDoorTrigger.isTrigger = true;
        cafeteriaDoorTrigger.size = new Vector3(4.5f, 5.4f, 1.5f);
        cafeteriaDoorTrigger.enabled = false;

        cafeteriaDoorPortal = portalObject.AddComponent<CursedRoom99Portal>();
    }

    private void ActivateCafeteriaDoorPortal(Vector3 corridorSpawn, Quaternion corridorFacing)
    {
        if (cafeteriaDoor == null || cafeteriaDoorTrigger == null || cafeteriaDoorPortal == null)
        {
            Debug.LogError("Cafeteria Phase 2 door portal could not be activated.");
            return;
        }

        cafeteriaDoorPortal.corridorSpawn = corridorSpawn;
        cafeteriaDoorPortal.corridorFacing = corridorFacing;
        cafeteriaDoorTrigger.enabled = true;

        Debug.Log("Final exit locked. Cafeteria Phase 2 door is active.");
    }

    private static Transform FindSceneTransform(string objectName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current == null || !current.gameObject.scene.IsValid()) continue;
            if (current.name == objectName) return current;
        }

        return null;
    }

    private static GameObject CreateOrientedCube(
        string objectName,
        Transform parent,
        Vector3 position,
        Vector3 scale,
        Quaternion rotation,
        Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = objectName;
        cube.transform.SetParent(parent, true);
        cube.transform.position = position;
        cube.transform.rotation = rotation;
        cube.transform.localScale = scale;

        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null) renderer.material = material;
        return cube;
    }

    private static Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Diffuse");

        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    private void BuildOverlay()
    {
        GameObject canvasObject = new GameObject("Phase 2 Exit Overlay", typeof(Canvas), typeof(CanvasScaler));
        overlayCanvas = canvasObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 32765;

        GameObject fade = new GameObject("Fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fade.transform.SetParent(canvasObject.transform, false);

        RectTransform fadeRect = fade.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;

        fadeImage = fade.GetComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;

        GameObject message = new GameObject("Message", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        message.transform.SetParent(canvasObject.transform, false);

        RectTransform textRect = message.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.35f);
        textRect.anchorMax = new Vector2(0.9f, 0.65f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        messageText = message.GetComponent<Text>();
        messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
            float alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
            yield return null;
        }
    }

    public void FinishPhase2()
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(FinishPhase2Routine());
    }

    private IEnumerator FinishPhase2Routine()
    {
        if (messageText != null) messageText.text = string.Empty;
        if (fadeImage != null) fadeImage.color = new Color(0f, 0f, 0f, 0f);

        yield return Fade(0f, 1f, 0.65f);
        yield return new WaitForSecondsRealtime(0.25f);

        if (CursedPhaseManager.IsPhase2)
        {
            ShowPhase2CompletionScreen();
            yield break;
        }

        CursedPhase3Screen.Show();
    }

    private void ShowPhase2CompletionScreen()
    {
        if (completionVisible) return;

        completionVisible = true;
        completionCode = GenerateFourDigitCode();
        CursedPhaseManager.UnlockPhase3(completionCode);

        Texture2D completionTexture = Resources.Load<Texture2D>("CursedMod/Phase2Completion");
        if (completionTexture == null)
        {
            Debug.LogError("Phase 2 completion image could not be loaded. Opening Phase 3 directly.");
            ContinueToPhase3();
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
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

#if UNITY_ANDROID || UNITY_IOS
        CursedMobileInput.Hide();
#else
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
#endif

        AudioListener.pause = true;
        Time.timeScale = 0f;

        GameObject canvasObject = new GameObject(
            "Phase 2 Completion Canvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32766;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1672f, 941f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject screen = new GameObject(
            "Continue To Phase 3",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage),
            typeof(Button));

        screen.transform.SetParent(canvasObject.transform, false);

        RectTransform screenRect = screen.GetComponent<RectTransform>();
        screenRect.anchorMin = Vector2.zero;
        screenRect.anchorMax = Vector2.one;
        screenRect.offsetMin = Vector2.zero;
        screenRect.offsetMax = Vector2.zero;

        RawImage background = screen.GetComponent<RawImage>();
        background.texture = completionTexture;
        background.color = Color.white;
        background.raycastTarget = true;

        Button continueButton = screen.GetComponent<Button>();
        continueButton.transition = Selectable.Transition.None;
        continueButton.targetGraphic = background;
        continueButton.onClick.AddListener(ContinueToPhase3);

        GameObject codeObject = new GameObject(
            "Random Four Digit Code",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text),
            typeof(Outline));

        codeObject.transform.SetParent(screen.transform, false);

        RectTransform codeRect = codeObject.GetComponent<RectTransform>();
        codeRect.anchorMin = new Vector2(0.50f, 0.395f);
        codeRect.anchorMax = new Vector2(0.812f, 0.751f);
        codeRect.offsetMin = Vector2.zero;
        codeRect.offsetMax = Vector2.zero;

        Text codeText = codeObject.GetComponent<Text>();
        codeText.text = completionCode;
        codeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        codeText.fontSize = 132;
        codeText.fontStyle = FontStyle.Bold;
        codeText.alignment = TextAnchor.MiddleCenter;
        codeText.color = new Color(0.055f, 0.012f, 0.012f, 1f);
        codeText.resizeTextForBestFit = true;
        codeText.resizeTextMinSize = 72;
        codeText.resizeTextMaxSize = 140;
        codeText.raycastTarget = false;

        Outline outline = codeObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.48f, 0f, 0f, 0.92f);
        outline.effectDistance = new Vector2(3f, -3f);

        Debug.Log("Phase 2 complete. Phase 3 password saved: " + completionCode);
    }

    private void ContinueToPhase3()
    {
        completionVisible = false;

        GameObject completionCanvas = GameObject.Find("Phase 2 Completion Canvas");
        if (completionCanvas != null) Destroy(completionCanvas);

        CursedPhase3Screen.Show();
    }

    private static string GenerateFourDigitCode()
    {
        return UnityEngine.Random.Range(0, 10000).ToString("D4");
    }

    public static void FinishPhase2FromCorridor()
    {
        if (instance != null) instance.FinishPhase2();
    }

    public static void EnterCorridor()
    {
        if (instance == null || instance.activeController == null) return;

        GameControllerScript gc = instance.activeController;
        SetInactive(gc.baldiTutor);
        SetInactive(gc.baldi);
        SetInactive(gc.principal);
        SetInactive(gc.crafters);
        SetInactive(gc.playtime);
        SetInactive(gc.gottaSweep);
        SetInactive(gc.bully);
        SetInactive(gc.firstPrize);
        SetInactive(gc.TestEnemy);

        if (gc.schoolMusic != null) gc.schoolMusic.Stop();
        if (gc.learnMusic != null) gc.learnMusic.Stop();
    }

    private static void SetInactive(GameObject target)
    {
        if (target != null) target.SetActive(false);
    }
}

public class CursedMazeEndTrigger : MonoBehaviour
{
    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;

        triggered = true;
        CursedFinalExitSequence.FinishPhase2FromCorridor();
    }
}

public class CursedRoom99Portal : MonoBehaviour
{
    public Vector3 corridorSpawn;
    public Quaternion corridorFacing;

    private bool entered;

    private void OnTriggerEnter(Collider other)
    {
        if (entered || !other.CompareTag("Player")) return;

        PlayerScript player = other.GetComponent<PlayerScript>();
        if (player == null) player = other.GetComponentInParent<PlayerScript>();
        if (player == null) return;

        entered = true;
        CursedFinalExitSequence.EnterCorridor();

        CharacterController controller = player.cc;
        if (controller != null) controller.enabled = false;

        player.height = corridorSpawn.y;
        player.transform.position = corridorSpawn;
        player.transform.rotation = corridorFacing;

        if (controller != null) controller.enabled = true;
    }
}
