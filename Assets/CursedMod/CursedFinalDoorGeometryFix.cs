using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Final-pass geometry correction for the runtime-generated cafeteria finale door.
/// The finale door itself still uses the normal DoorScript. This component only
/// fixes runtime placement / doorway geometry after CursedFinalExitSequence has
/// spawned the door and narrow room.
/// </summary>
public class CursedFinalDoorGeometryFix : MonoBehaviour
{
    private const string FinaleDoorName = "Cafeteria Phase 2 Door";
    private const string RoomFloorName = "Phase 2 Narrow Room Floor";
    private const string PassageName = "Cafeteria Phase 2 Physical Doorway Passage";
    private const string DoorwayVoidName = "Cafeteria Phase 2 Doorway Void";

    private const float DoorwayWidth = 4.5f;
    private const float DoorwayHeight = 5.4f;
    private const float DoorwayDepth = 4.6f;
    private const float PassageInsideOffset = 0.65f;

    private static CursedFinalDoorGeometryFix instance;
    private int fixedDoorInstanceId;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (instance != null) return;

        GameObject host = new GameObject("Cursed Final Door Geometry Fix");
        instance = host.AddComponent<CursedFinalDoorGeometryFix>();
        DontDestroyOnLoad(host);
    }

    private void OnEnable()
    {
        StartCoroutine(WatchForGeneratedDoor());
    }

    private IEnumerator WatchForGeneratedDoor()
    {
        WaitForSecondsRealtime delay = new WaitForSecondsRealtime(0.1f);

        while (true)
        {
            GameObject door = GameObject.Find(FinaleDoorName);
            GameObject roomFloor = GameObject.Find(RoomFloorName);
            GameObject passage = GameObject.Find(PassageName);

            if (door != null && roomFloor != null && passage != null)
            {
                int currentId = door.GetInstanceID();
                if (currentId != fixedDoorInstanceId)
                {
                    FixGeneratedDoor(door, roomFloor, passage);
                    fixedDoorInstanceId = currentId;
                }
            }
            else if (door == null)
            {
                fixedDoorInstanceId = 0;
            }

            yield return delay;
        }
    }

    private static void FixGeneratedDoor(
        GameObject door,
        GameObject roomFloor,
        GameObject passage)
    {
        float floorY;
        if (!TryGetFloorSurfaceY(roomFloor, out floorY))
        {
            Debug.LogError("Finale door fix could not determine the narrow-room floor height.");
            return;
        }

        // The old placement used a hard-coded +2.8 Y offset. Normal school
        // doors do not guarantee that their root pivot sits at that height.
        // Align the actual visible bottom of the cloned door to the real floor.
        Bounds visualBounds;
        if (TryGetRendererBounds(door, out visualBounds))
        {
            float verticalCorrection = floorY - visualBounds.min.y;
            door.transform.position += Vector3.up * verticalCorrection;
            Physics.SyncTransforms();
        }

        DoorScript[] doorScripts = door.GetComponentsInChildren<DoorScript>(true);
        for (int i = 0; i < doorScripts.Length; i++)
        {
            DoorScript doorScript = doorScripts[i];
            if (doorScript == null) continue;

            doorScript.enabled = true;
            doorScript.UnlockDoor();
            doorScript.openingDistance = Mathf.Max(doorScript.openingDistance, 8f);
        }

        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycastLayer < 0) ignoreRaycastLayer = 2;

        // Critical: this helper trigger sits in front of the real DoorScript
        // trigger. If it participates in raycasts, DoorScript.Update() hits this
        // collider first and refuses to open because the hit collider is not its
        // own trigger. Keep it as a physical trigger, but exclude it from raycasts.
        passage.layer = ignoreRaycastLayer;

        Vector3 outward = door.transform.forward;
        outward.y = 0f;
        if (outward.sqrMagnitude < 0.01f) outward = Vector3.forward;
        outward.Normalize();

        passage.transform.position =
            new Vector3(door.transform.position.x, floorY + DoorwayHeight * 0.5f, door.transform.position.z)
            - outward * PassageInsideOffset;
        passage.transform.rotation = Quaternion.LookRotation(outward, Vector3.up);

        BoxCollider passageCollider = passage.GetComponent<BoxCollider>();
        if (passageCollider != null)
        {
            passageCollider.isTrigger = true;
            passageCollider.size = new Vector3(DoorwayWidth, DoorwayHeight, DoorwayDepth);
        }

        Transform cafeteria = door.transform.parent;
        Collider[] blockers = FindDoorwayBlockers(door, cafeteria, floorY, outward);

        // DoorScript uses Physics.Raycast without a custom mask. Any cafeteria
        // wall collider directly behind/in front of the runtime door can steal
        // that ray before it reaches DoorScript.trigger. Ignore those wall
        // colliders only for raycasts; their normal physical collision remains.
        for (int i = 0; i < blockers.Length; i++)
        {
            Collider blocker = blockers[i];
            if (blocker == null) continue;
            blocker.gameObject.layer = ignoreRaycastLayer;
        }

        CursedDoorwayPassageTrigger passageScript =
            passage.GetComponent<CursedDoorwayPassageTrigger>();
        if (passageScript != null)
        {
            passageScript.blockingColliders = blockers;
        }

        GameObject doorwayVoid = GameObject.Find(DoorwayVoidName);
        if (doorwayVoid != null)
        {
            Vector3 voidPosition = doorwayVoid.transform.position;
            voidPosition.y = floorY + doorwayVoid.transform.lossyScale.y * 0.5f;
            doorwayVoid.transform.position = voidPosition;

            Collider voidCollider = doorwayVoid.GetComponent<Collider>();
            if (voidCollider != null) voidCollider.enabled = false;
            doorwayVoid.layer = ignoreRaycastLayer;
        }

        Physics.SyncTransforms();
        Debug.Log(
            "Finale door geometry fixed: visual bottom aligned to floor, " +
            "doorway trigger removed from raycasts, and wall blockers bypassed locally.");
    }

    private static bool TryGetFloorSurfaceY(GameObject roomFloor, out float floorY)
    {
        Collider floorCollider = roomFloor.GetComponent<Collider>();
        if (floorCollider != null)
        {
            floorY = floorCollider.bounds.max.y;
            return true;
        }

        Renderer floorRenderer = roomFloor.GetComponent<Renderer>();
        if (floorRenderer != null)
        {
            floorY = floorRenderer.bounds.max.y;
            return true;
        }

        floorY = 0f;
        return false;
    }

    private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool initialized = false;
        bounds = new Bounds(root.transform.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null) continue;

            if (!initialized)
            {
                bounds = renderer.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return initialized;
    }

    private static Collider[] FindDoorwayBlockers(
        GameObject door,
        Transform cafeteria,
        float floorY,
        Vector3 outward)
    {
        if (cafeteria == null) return new Collider[0];

        Quaternion rotation = Quaternion.LookRotation(outward, Vector3.up);
        Vector3 center =
            new Vector3(door.transform.position.x, floorY + DoorwayHeight * 0.5f, door.transform.position.z)
            - outward * PassageInsideOffset;

        Collider[] overlaps = Physics.OverlapBox(
            center,
            new Vector3(DoorwayWidth * 0.5f, DoorwayHeight * 0.5f, DoorwayDepth * 0.5f),
            rotation,
            ~0,
            QueryTriggerInteraction.Ignore);

        List<Collider> blockers = new List<Collider>();

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider candidate = overlaps[i];
            if (candidate == null || !candidate.enabled || candidate.isTrigger) continue;
            if (candidate.transform == door.transform || candidate.transform.IsChildOf(door.transform)) continue;

            if (candidate.transform != cafeteria && !candidate.transform.IsChildOf(cafeteria))
            {
                continue;
            }

            if (candidate.GetComponentInParent<DoorScript>() != null) continue;

            string lowerName = candidate.gameObject.name.ToLowerInvariant();
            if (lowerName.Contains("floor") || lowerName.Contains("ground") || lowerName.Contains("door"))
            {
                continue;
            }

            if (candidate.bounds.size.y < 2.2f) continue;

            blockers.Add(candidate);
        }

        return blockers.ToArray();
    }
}
