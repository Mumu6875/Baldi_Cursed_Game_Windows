using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Handles the Phase 2 final-exit sequence. The fourth exit is sealed, a real
/// in-game DoorScript door is cloned onto a cafeteria wall, and the player
/// physically walks through that door into a short narrow room.
/// </summary>
public class CursedFinalExitSequence : MonoBehaviour
{
    private const float RoomWidth = 4.8f;
    private const float RoomHeight = 4.6f;
    private const float RoomLength = 13f;
    private const float RoomStartOffset = 0.45f;
    private const float Phase3TriggerDistance = 7.5f;

    private const float DoorwayWidth = 4.5f;
    private const float DoorwayHeight = 5.4f;
    private const float DoorwayDepth = 3.0f;

    private static CursedFinalExitSequence instance;

    private bool sequenceActive;
    private Canvas overlayCanvas;
    private Image fadeImage;
    private Text messageText;
    private GameObject cafeteriaDoor;
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

        if (!BuildCafeteriaDoorAndRoom())
        {
            Debug.LogError("Phase 2 cafeteria door / narrow room could not be created.");
            yield break;
        }

        Debug.Log("Final exit locked. Real cafeteria DoorScript entrance is active.");
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

    private bool BuildCafeteriaDoorAndRoom()
    {
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
        ChooseFreeCafeteriaWall(
            cafeteria,
            localBounds,
            out doorLocalPosition,
            out outwardLocalDirection);

        Vector3 doorWorldPosition = cafeteria.TransformPoint(doorLocalPosition);
        Vector3 outward = cafeteria.TransformDirection(outwardLocalDirection);
        outward.y = 0f;
        if (outward.sqrMagnitude < 0.01f) outward = Vector3.forward;
        outward.Normalize();

        Quaternion rotation = Quaternion.LookRotation(outward, Vector3.up);

        if (!CloneExistingDoor(cafeteria, doorWorldPosition, rotation))
        {
            Debug.LogError("No existing DoorScript could be cloned for the cafeteria finale door.");
            return false;
        }

        float floorY = cafeteria.TransformPoint(new Vector3(0f, localBounds.min.y, 0f)).y;
        Vector3 floorBase = new Vector3(doorWorldPosition.x, floorY, doorWorldPosition.z);

        BuildDoorwayPassage(cafeteria, doorWorldPosition, outward, rotation, floorY);
        BuildNarrowRoom(floorBase, outward, rotation);

        Debug.Log("Phase 2 cafeteria door uses the normal DoorScript and opens into a physical narrow room.");
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

    private static void EncapsulateWorldBounds(
        Transform root,
        Bounds worldBounds,
        ref bool initialized,
        ref Bounds localBounds)
    {
        Vector3 center = worldBounds.center;
        Vector3 extents = worldBounds.extents;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 worldPoint =
                        center + Vector3.Scale(extents, new Vector3(x, y, z));
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
        bestLocalPosition =
            new Vector3(bounds.center.x, bounds.min.y + 2.8f, bounds.max.z);
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

                int score = ScoreRoomSpace(
                    cafeteria,
                    localPosition,
                    localOutward);

                if (score >= bestScore) continue;

                bestScore = score;
                bestLocalPosition = localPosition;
                bestOutwardLocal = localOutward;
            }
        }
    }

    private static int ScoreRoomSpace(
        Transform cafeteria,
        Vector3 localDoor,
        Vector3 localOutward)
    {
        Vector3 doorWorld = cafeteria.TransformPoint(localDoor);
        Vector3 outward = cafeteria.TransformDirection(localOutward);
        outward.y = 0f;
        if (outward.sqrMagnitude < 0.01f) outward = Vector3.forward;
        outward.Normalize();

        Quaternion rotation = Quaternion.LookRotation(outward, Vector3.up);
        Vector3 center =
            doorWorld + outward * (RoomStartOffset + RoomLength * 0.5f);

        Collider[] overlaps = Physics.OverlapBox(
            center,
            new Vector3(
                RoomWidth * 0.5f,
                RoomHeight * 0.5f,
                RoomLength * 0.5f),
            rotation,
            ~0,
            QueryTriggerInteraction.Ignore);

        int score = 0;

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider overlap = overlaps[i];
            if (overlap == null) continue;
            if (overlap.transform == cafeteria ||
                overlap.transform.IsChildOf(cafeteria))
            {
                continue;
            }

            string objectName = overlap.gameObject.name.ToLowerInvariant();
            if (objectName.Contains("floor") ||
                objectName.Contains("ground"))
            {
                continue;
            }

            score++;
        }

        return score;
    }

    private bool CloneExistingDoor(
        Transform cafeteria,
        Vector3 worldPosition,
        Quaternion worldRotation)
    {
        DoorScript[] doors = Resources.FindObjectsOfTypeAll<DoorScript>();
        DoorScript template = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < doors.Length; i++)
        {
            DoorScript candidate = doors[i];
            if (candidate == null || !candidate.gameObject.scene.IsValid()) continue;
            if (candidate.gameObject.scene != cafeteria.gameObject.scene) continue;

            float distance =
                (candidate.transform.position - worldPosition).sqrMagnitude;

            if (distance >= bestDistance) continue;

            bestDistance = distance;
            template = candidate;
        }

        if (template == null) return false;

        // Clone the complete normal door GameObject. Its DoorScript, materials,
        // audio clips, barrier, invisibleBarrier and trigger references are all
        // cloned exactly as Unity clones any other in-scene door.
        cafeteriaDoor =
            Instantiate(template.gameObject, worldPosition, worldRotation);

        cafeteriaDoor.name = "Cafeteria Phase 2 Door";
        cafeteriaDoor.transform.SetParent(cafeteria, true);

        DoorScript[] clonedDoorScripts =
            cafeteriaDoor.GetComponentsInChildren<DoorScript>(true);

        for (int i = 0; i < clonedDoorScripts.Length; i++)
        {
            DoorScript clonedDoor = clonedDoorScripts[i];
            if (clonedDoor == null) continue;

            // Do not replace or emulate DoorScript. Keep the original script
            // enabled and only guarantee that this newly spawned finale door
            // starts unlocked.
            clonedDoor.enabled = true;
            clonedDoor.UnlockDoor();
        }

        return clonedDoorScripts.Length > 0;
    }

    private void BuildDoorwayPassage(
        Transform cafeteria,
        Vector3 doorWorldPosition,
        Vector3 outward,
        Quaternion rotation,
        float floorY)
    {
        Collider[] blockers =
            FindDoorwayBlockingColliders(cafeteria, doorWorldPosition, rotation);

        GameObject passage =
            new GameObject("Cafeteria Phase 2 Physical Doorway Passage");

        passage.transform.position =
            new Vector3(
                doorWorldPosition.x,
                floorY + DoorwayHeight * 0.5f,
                doorWorldPosition.z);

        passage.transform.rotation = rotation;

        BoxCollider trigger = passage.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size =
            new Vector3(DoorwayWidth, DoorwayHeight, DoorwayDepth);

        CursedDoorwayPassageTrigger passageScript =
            passage.AddComponent<CursedDoorwayPassageTrigger>();

        passageScript.blockingColliders = blockers;

        // A very thin, non-colliding dark opening hides any cafeteria wall
        // renderer sitting immediately behind the cloned door. The real door
        // itself stays in front and still supplies all normal open/close visuals.
        Material voidMaterial = CreateMaterial(Color.black);
        Vector3 voidPosition =
            doorWorldPosition +
            outward * 0.08f +
            Vector3.down * 0.05f;

        GameObject doorwayVoid = CreateOrientedCube(
            "Cafeteria Phase 2 Doorway Void",
            null,
            voidPosition,
            new Vector3(DoorwayWidth - 0.25f, 4.3f, 0.05f),
            rotation,
            voidMaterial);

        Collider voidCollider = doorwayVoid.GetComponent<Collider>();
        if (voidCollider != null) Destroy(voidCollider);
    }

    private static Collider[] FindDoorwayBlockingColliders(
        Transform cafeteria,
        Vector3 doorWorldPosition,
        Quaternion rotation)
    {
        Collider[] overlaps = Physics.OverlapBox(
            doorWorldPosition,
            new Vector3(
                DoorwayWidth * 0.5f,
                DoorwayHeight * 0.5f,
                DoorwayDepth * 0.5f),
            rotation,
            ~0,
            QueryTriggerInteraction.Ignore);

        List<Collider> blockers = new List<Collider>();

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider candidate = overlaps[i];
            if (candidate == null ||
                !candidate.enabled ||
                candidate.isTrigger)
            {
                continue;
            }

            if (instance != null &&
                instance.cafeteriaDoor != null &&
                (candidate.transform == instance.cafeteriaDoor.transform ||
                 candidate.transform.IsChildOf(instance.cafeteriaDoor.transform)))
            {
                continue;
            }

            // Only bypass cafeteria boundary geometry. Do not make unrelated
            // props, NPCs or world objects non-solid.
            if (candidate.transform != cafeteria &&
                !candidate.transform.IsChildOf(cafeteria))
            {
                continue;
            }

            string lowerName = candidate.gameObject.name.ToLowerInvariant();

            if (lowerName.Contains("floor") ||
                lowerName.Contains("ground") ||
                lowerName.Contains("door"))
            {
                continue;
            }

            if (candidate.GetComponentInParent<DoorScript>() != null) continue;
            if (candidate.bounds.size.y < 2.2f) continue;

            blockers.Add(candidate);
        }

        return blockers.ToArray();
    }

    private void BuildNarrowRoom(
        Vector3 floorBase,
        Vector3 outward,
        Quaternion rotation)
    {
        GameObject root =
            new GameObject("Cafeteria Phase 2 Narrow Room");

        Material wallMaterial =
            CreateMaterial(new Color(0.055f, 0.055f, 0.065f, 1f));

        Material floorMaterial =
            CreateMaterial(new Color(0.012f, 0.012f, 0.016f, 1f));

        Vector3 right = rotation * Vector3.right;
        float centerDistance =
            RoomStartOffset + RoomLength * 0.5f;

        Vector3 roomCenter =
            floorBase + outward * centerDistance;

        CreateOrientedCube(
            "Phase 2 Narrow Room Floor",
            root.transform,
            roomCenter - Vector3.up * 0.15f,
            new Vector3(RoomWidth, 0.3f, RoomLength),
            rotation,
            floorMaterial);

        CreateOrientedCube(
            "Phase 2 Narrow Room Ceiling",
            root.transform,
            roomCenter + Vector3.up * RoomHeight,
            new Vector3(RoomWidth, 0.3f, RoomLength),
            rotation,
            floorMaterial);

        CreateOrientedCube(
            "Phase 2 Narrow Room Left Wall",
            root.transform,
            roomCenter -
                right * (RoomWidth * 0.5f) +
                Vector3.up * (RoomHeight * 0.5f),
            new Vector3(0.24f, RoomHeight, RoomLength),
            rotation,
            wallMaterial);

        CreateOrientedCube(
            "Phase 2 Narrow Room Right Wall",
            root.transform,
            roomCenter +
                right * (RoomWidth * 0.5f) +
                Vector3.up * (RoomHeight * 0.5f),
            new Vector3(0.24f, RoomHeight, RoomLength),
            rotation,
            wallMaterial);

        Vector3 farEnd =
            floorBase + outward * (RoomStartOffset + RoomLength);

        CreateOrientedCube(
            "Phase 2 Narrow Room End Wall",
            root.transform,
            farEnd + Vector3.up * (RoomHeight * 0.5f),
            new Vector3(RoomWidth, RoomHeight, 0.24f),
            rotation,
            wallMaterial);

        GameObject entryTrigger =
            new GameObject("Phase 2 Narrow Room Entry Trigger");

        entryTrigger.transform.SetParent(root.transform, true);
        entryTrigger.transform.position =
            floorBase +
            outward * 1.65f +
            Vector3.up * 1.7f;
        entryTrigger.transform.rotation = rotation;

        BoxCollider entryBox =
            entryTrigger.AddComponent<BoxCollider>();

        entryBox.isTrigger = true;
        entryBox.size =
            new Vector3(RoomWidth - 0.5f, 3.4f, 1.4f);

        entryTrigger.AddComponent<CursedPhysicalRoomEntryTrigger>();

        GameObject endTrigger =
            new GameObject("Phase 2 Narrow Room Phase 3 Trigger");

        endTrigger.transform.SetParent(root.transform, true);
        endTrigger.transform.position =
            floorBase +
            outward * Phase3TriggerDistance +
            Vector3.up * 1.7f;
        endTrigger.transform.rotation = rotation;

        BoxCollider trigger = endTrigger.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size =
            new Vector3(RoomWidth - 0.5f, 3.4f, 1.4f);

        endTrigger.AddComponent<CursedMazeEndTrigger>();
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
        GameObject cube =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        cube.name = objectName;

        if (parent != null)
        {
            cube.transform.SetParent(parent, true);
        }

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

        CursedPhase3Screen.Show();
    }

    private void ShowPhase2CompletionScreen()
    {
        if (completionVisible) return;

        completionVisible = true;
        completionCode = GenerateFourDigitCode();
        CursedPhaseManager.UnlockPhase3(completionCode);

        Texture2D completionTexture =
            Resources.Load<Texture2D>(
                "CursedMod/Phase2Completion");

        if (completionTexture == null)
        {
            Debug.LogError(
                "Phase 2 completion image could not be loaded. Opening Phase 3 directly.");
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
                "Continue To Phase 3",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage),
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

        RawImage background =
            screen.GetComponent<RawImage>();

        background.texture = completionTexture;
        background.color = Color.white;
        background.raycastTarget = true;

        Button continueButton =
            screen.GetComponent<Button>();

        continueButton.transition =
            Selectable.Transition.None;
        continueButton.targetGraphic = background;
        continueButton.onClick.AddListener(
            ContinueToPhase3);

        GameObject codeObject =
            new GameObject(
                "Random Four Digit Code",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text),
                typeof(Outline));

        codeObject.transform.SetParent(
            screen.transform,
            false);

        RectTransform codeRect =
            codeObject.GetComponent<RectTransform>();

        codeRect.anchorMin = new Vector2(0.50f, 0.395f);
        codeRect.anchorMax = new Vector2(0.812f, 0.751f);
        codeRect.offsetMin = Vector2.zero;
        codeRect.offsetMax = Vector2.zero;

        Text codeText = codeObject.GetComponent<Text>();
        codeText.text = completionCode;
        codeText.font =
            Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf");
        codeText.fontSize = 132;
        codeText.fontStyle = FontStyle.Bold;
        codeText.alignment = TextAnchor.MiddleCenter;
        codeText.color =
            new Color(0.055f, 0.012f, 0.012f, 1f);
        codeText.resizeTextForBestFit = true;
        codeText.resizeTextMinSize = 72;
        codeText.resizeTextMaxSize = 140;
        codeText.raycastTarget = false;

        Outline outline =
            codeObject.GetComponent<Outline>();

        outline.effectColor =
            new Color(0.48f, 0f, 0f, 0.92f);
        outline.effectDistance =
            new Vector2(3f, -3f);

        Debug.Log(
            "Phase 2 complete. Phase 3 password saved: " +
            completionCode);
    }

    private void ContinueToPhase3()
    {
        completionVisible = false;

        GameObject completionCanvas =
            GameObject.Find("Phase 2 Completion Canvas");

        if (completionCanvas != null)
        {
            Destroy(completionCanvas);
        }

        CursedPhase3Screen.Show();
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

/// <summary>
/// The cafeteria boundary may contain a solid wall collider where the runtime
/// finale door is placed. While the player is inside the doorway volume, ignore
/// only those cafeteria wall colliders. The cloned DoorScript's own barrier and
/// invisibleBarrier are never ignored, so a closed door still blocks movement.
/// </summary>
public class CursedDoorwayPassageTrigger : MonoBehaviour
{
    public Collider[] blockingColliders;

    private Collider activePlayerCollider;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerScript player =
            other.GetComponent<PlayerScript>();

        if (player == null)
        {
            player =
                other.GetComponentInParent<PlayerScript>();
        }

        if (player == null) return;

        activePlayerCollider =
            player.cc != null
                ? (Collider)player.cc
                : other;

        SetIgnored(activePlayerCollider, true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Collider playerCollider =
            activePlayerCollider != null
                ? activePlayerCollider
                : other;

        SetIgnored(playerCollider, false);
        activePlayerCollider = null;
    }

    private void OnDisable()
    {
        if (activePlayerCollider == null) return;

        SetIgnored(activePlayerCollider, false);
        activePlayerCollider = null;
    }

    private void SetIgnored(
        Collider playerCollider,
        bool ignored)
    {
        if (playerCollider == null ||
            blockingColliders == null)
        {
            return;
        }

        for (int i = 0;
             i < blockingColliders.Length;
             i++)
        {
            Collider wall = blockingColliders[i];
            if (wall == null) continue;

            Physics.IgnoreCollision(
                playerCollider,
                wall,
                ignored);
        }
    }
}
