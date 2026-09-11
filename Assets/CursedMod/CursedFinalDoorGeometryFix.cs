using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Moves the generated Room 99 finale door onto a REAL existing cafeteria
/// doorway. This avoids placing a door on top of an uncut Schoolhouse wall.
/// The final door itself remains the normal cloned DoorScript door.
/// </summary>
public class CursedFinalDoorGeometryFix : MonoBehaviour
{
    private const string FinaleDoorName = "Cafeteria Phase 2 Door";
    private const string RoomRootName = "Cafeteria Phase 2 Narrow Room";
    private const string RoomFloorName = "Phase 2 Narrow Room Floor";
    private const string PassageName = "Cafeteria Phase 2 Physical Doorway Passage";
    private const string DoorwayVoidName = "Cafeteria Phase 2 Doorway Void";

    private const float RoomWidth = 4.8f;
    private const float RoomHeight = 4.6f;
    private const float RoomLength = 13f;
    private const float RoomStartOffset = 0.45f;
    private const float MaxBoundaryDistance = 6f;

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
        StartCoroutine(WatchForFinaleDoor());
    }

    private IEnumerator WatchForFinaleDoor()
    {
        while (true)
        {
            GameObject finalDoor = GameObject.Find(FinaleDoorName);
            GameObject roomRoot = GameObject.Find(RoomRootName);
            GameObject roomFloor = GameObject.Find(RoomFloorName);
            GameObject passage = GameObject.Find(PassageName);
            Transform cafeteria = FindSceneTransform("Cafeteria");

            if (finalDoor != null &&
                roomRoot != null &&
                roomFloor != null &&
                cafeteria != null)
            {
                int currentId = finalDoor.GetInstanceID();
                if (currentId != fixedDoorInstanceId)
                {
                    if (RelocateToRealCafeteriaDoorway(
                        finalDoor,
                        roomRoot,
                        roomFloor,
                        passage,
                        cafeteria))
                    {
                        fixedDoorInstanceId = currentId;
                    }
                }
            }
            else if (finalDoor == null)
            {
                fixedDoorInstanceId = 0;
            }

            yield return null;
        }
    }

    private static bool RelocateToRealCafeteriaDoorway(
        GameObject finalDoor,
        GameObject roomRoot,
        GameObject roomFloor,
        GameObject passage,
        Transform cafeteria)
    {
        Bounds cafeteriaBounds;
        if (!TryGetCafeteriaLocalBounds(cafeteria, finalDoor, out cafeteriaBounds))
        {
            Debug.LogError("Could not calculate cafeteria bounds for the finale doorway.");
            return false;
        }

        float floorY;
        if (!TryGetFloorSurfaceY(roomFloor, out floorY))
        {
            Debug.LogError("Could not determine Phase 2 narrow-room floor height.");
            return false;
        }

        Transform realDoorway;
        Vector3 newOutward;
        if (!TryFindBestExistingCafeteriaDoorway(
            cafeteria,
            cafeteriaBounds,
            finalDoor,
            out realDoorway,
            out newOutward))
        {
            Debug.LogError(
                "No existing cafeteria doorway was found. " +
                "The Room 99 door was not moved onto a solid wall again.");
            return false;
        }

        Vector3 oldOutward = finalDoor.transform.forward;
        oldOutward.y = 0f;
        if (oldOutward.sqrMagnitude < 0.01f) oldOutward = Vector3.forward;
        oldOutward.Normalize();

        newOutward.y = 0f;
        if (newOutward.sqrMagnitude < 0.01f) newOutward = Vector3.forward;
        newOutward.Normalize();

        Quaternion oldFrameRotation = Quaternion.LookRotation(oldOutward, Vector3.up);
        Quaternion newFrameRotation = Quaternion.LookRotation(newOutward, Vector3.up);

        Vector3 oldFloorOrigin = new Vector3(
            finalDoor.transform.position.x,
            floorY,
            finalDoor.transform.position.z);

        Vector3 newFloorOrigin = new Vector3(
            realDoorway.position.x,
            floorY,
            realDoorway.position.z);

        DisableExistingDoorObject(realDoorway.gameObject);

        Transform roomTransform = roomRoot.transform;
        for (int i = 0; i < roomTransform.childCount; i++)
        {
            Transform child = roomTransform.GetChild(i);
            MoveBetweenDoorFrames(
                child,
                oldFloorOrigin,
                oldFrameRotation,
                newFloorOrigin,
                newFrameRotation);
        }

        finalDoor.transform.position = realDoorway.position;
        finalDoor.transform.rotation = newFrameRotation;
        AlignVisibleBottomToFloor(finalDoor, floorY);

        DoorScript[] finalDoorScripts = finalDoor.GetComponentsInChildren<DoorScript>(true);
        for (int i = 0; i < finalDoorScripts.Length; i++)
        {
            DoorScript door = finalDoorScripts[i];
            if (door == null) continue;
            door.enabled = true;
            door.UnlockDoor();
            door.openingDistance = Mathf.Max(door.openingDistance, 8f);
        }

        if (passage != null)
        {
            Collider[] passageColliders = passage.GetComponents<Collider>();
            for (int i = 0; i < passageColliders.Length; i++)
            {
                if (passageColliders[i] != null) passageColliders[i].enabled = false;
            }

            CursedDoorwayPassageTrigger oldBypass =
                passage.GetComponent<CursedDoorwayPassageTrigger>();
            if (oldBypass != null) oldBypass.enabled = false;

            CursedFinalDoorwayCollisionBypass newerBypass =
                passage.GetComponent<CursedFinalDoorwayCollisionBypass>();
            if (newerBypass != null) newerBypass.enabled = false;
        }

        GameObject doorwayVoid = GameObject.Find(DoorwayVoidName);
        if (doorwayVoid != null) doorwayVoid.SetActive(false);

        Physics.SyncTransforms();

        Debug.Log(
            "Room 99 moved onto real cafeteria doorway '" + realDoorway.name +
            "'. No fake wall opening or teleport is used.");
        return true;
    }

    private static bool TryFindBestExistingCafeteriaDoorway(
        Transform cafeteria,
        Bounds bounds,
        GameObject finalDoor,
        out Transform bestDoorway,
        out Vector3 bestOutward)
    {
        bestDoorway = null;
        bestOutward = Vector3.forward;
        float bestScore = float.MaxValue;
        HashSet<int> checkedObjects = new HashSet<int>();

        DoorScript[] normalDoors = Resources.FindObjectsOfTypeAll<DoorScript>();
        for (int i = 0; i < normalDoors.Length; i++)
        {
            DoorScript door = normalDoors[i];
            if (door == null || !door.gameObject.scene.IsValid()) continue;
            if (door.gameObject.scene != cafeteria.gameObject.scene) continue;
            if (door.transform == finalDoor.transform || door.transform.IsChildOf(finalDoor.transform)) continue;

            EvaluateDoorwayCandidate(
                cafeteria,
                bounds,
                finalDoor,
                door.transform,
                checkedObjects,
                ref bestDoorway,
                ref bestOutward,
                ref bestScore);
        }

        SwingingDoorScript[] swingingDoors = Resources.FindObjectsOfTypeAll<SwingingDoorScript>();
        for (int i = 0; i < swingingDoors.Length; i++)
        {
            SwingingDoorScript door = swingingDoors[i];
            if (door == null || !door.gameObject.scene.IsValid()) continue;
            if (door.gameObject.scene != cafeteria.gameObject.scene) continue;

            EvaluateDoorwayCandidate(
                cafeteria,
                bounds,
                finalDoor,
                door.transform,
                checkedObjects,
                ref bestDoorway,
                ref bestOutward,
                ref bestScore);
        }

        return bestDoorway != null;
    }

    private static void EvaluateDoorwayCandidate(
        Transform cafeteria,
        Bounds bounds,
        GameObject finalDoor,
        Transform candidate,
        HashSet<int> checkedObjects,
        ref Transform bestDoorway,
        ref Vector3 bestOutward,
        ref float bestScore)
    {
        if (candidate == null) return;
        int id = candidate.gameObject.GetInstanceID();
        if (!checkedObjects.Add(id)) return;

        Vector3 local = cafeteria.InverseTransformPoint(candidate.position);

        float dMaxZ = Mathf.Abs(local.z - bounds.max.z);
        float dMinZ = Mathf.Abs(local.z - bounds.min.z);
        float dMaxX = Mathf.Abs(local.x - bounds.max.x);
        float dMinX = Mathf.Abs(local.x - bounds.min.x);

        float boundaryDistance = dMaxZ;
        Vector3 localOutward = Vector3.forward;
        bool alongBoundary =
            local.x >= bounds.min.x - 2f &&
            local.x <= bounds.max.x + 2f;

        if (dMinZ < boundaryDistance)
        {
            boundaryDistance = dMinZ;
            localOutward = Vector3.back;
            alongBoundary =
                local.x >= bounds.min.x - 2f &&
                local.x <= bounds.max.x + 2f;
        }

        if (dMaxX < boundaryDistance)
        {
            boundaryDistance = dMaxX;
            localOutward = Vector3.right;
            alongBoundary =
                local.z >= bounds.min.z - 2f &&
                local.z <= bounds.max.z + 2f;
        }

        if (dMinX < boundaryDistance)
        {
            boundaryDistance = dMinX;
            localOutward = Vector3.left;
            alongBoundary =
                local.z >= bounds.min.z - 2f &&
                local.z <= bounds.max.z + 2f;
        }

        if (!alongBoundary || boundaryDistance > MaxBoundaryDistance) return;
        if (local.y < bounds.min.y - 2f || local.y > bounds.max.y + 3f) return;

        Vector3 outward = cafeteria.TransformDirection(localOutward);
        outward.y = 0f;
        if (outward.sqrMagnitude < 0.01f) return;
        outward.Normalize();

        int obstructionScore = ScoreRoomSpace(candidate.position, outward, candidate.gameObject, finalDoor);
        float oldDoorDistance = Vector3.Distance(candidate.position, finalDoor.transform.position);

        float score =
            boundaryDistance * 20f +
            obstructionScore * 30f +
            oldDoorDistance * 0.15f;

        if (score >= bestScore) return;

        bestScore = score;
        bestDoorway = candidate;
        bestOutward = outward;
    }

    private static int ScoreRoomSpace(
        Vector3 doorwayPosition,
        Vector3 outward,
        GameObject existingDoor,
        GameObject finalDoor)
    {
        Quaternion rotation = Quaternion.LookRotation(outward, Vector3.up);
        Vector3 center =
            new Vector3(doorwayPosition.x, doorwayPosition.y, doorwayPosition.z) +
            outward * (RoomStartOffset + RoomLength * 0.5f);

        Collider[] overlaps = Physics.OverlapBox(
            center,
            new Vector3(RoomWidth * 0.5f, RoomHeight * 0.5f, RoomLength * 0.5f),
            rotation,
            ~0,
            QueryTriggerInteraction.Ignore);

        int score = 0;
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider overlap = overlaps[i];
            if (overlap == null) continue;

            if (overlap.transform == existingDoor.transform ||
                overlap.transform.IsChildOf(existingDoor.transform) ||
                overlap.transform == finalDoor.transform ||
                overlap.transform.IsChildOf(finalDoor.transform))
            {
                continue;
            }

            string name = overlap.gameObject.name.ToLowerInvariant();
            if (name.Contains("floor") || name.Contains("ground")) continue;

            score += overlap.bounds.size.y >= 2.2f ? 3 : 1;
        }

        return score;
    }

    private static void DisableExistingDoorObject(GameObject doorwayObject)
    {
        if (doorwayObject == null) return;

        DoorScript[] normalDoors = doorwayObject.GetComponentsInChildren<DoorScript>(true);
        for (int i = 0; i < normalDoors.Length; i++)
        {
            if (normalDoors[i] != null) normalDoors[i].enabled = false;
        }

        SwingingDoorScript[] swingingDoors = doorwayObject.GetComponentsInChildren<SwingingDoorScript>(true);
        for (int i = 0; i < swingingDoors.Length; i++)
        {
            if (swingingDoors[i] != null) swingingDoors[i].enabled = false;
        }

        Collider[] colliders = doorwayObject.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null) colliders[i].enabled = false;
        }

        Renderer[] renderers = doorwayObject.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].enabled = false;
        }
    }

    private static void MoveBetweenDoorFrames(
        Transform target,
        Vector3 oldOrigin,
        Quaternion oldRotation,
        Vector3 newOrigin,
        Quaternion newRotation)
    {
        Quaternion oldInverse = Quaternion.Inverse(oldRotation);
        Vector3 localPosition = oldInverse * (target.position - oldOrigin);
        Quaternion localRotation = oldInverse * target.rotation;

        target.position = newOrigin + newRotation * localPosition;
        target.rotation = newRotation * localRotation;
    }

    private static void AlignVisibleBottomToFloor(GameObject door, float floorY)
    {
        Bounds bounds;
        if (!TryGetRendererBounds(door, out bounds)) return;

        float correction = floorY - bounds.min.y;
        door.transform.position += Vector3.up * correction;
    }

    private static bool TryGetCafeteriaLocalBounds(
        Transform cafeteria,
        GameObject generatedDoor,
        out Bounds localBounds)
    {
        bool initialized = false;
        localBounds = new Bounds(Vector3.zero, Vector3.zero);

        Renderer[] renderers = cafeteria.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null) continue;
            if (renderer.transform == generatedDoor.transform ||
                renderer.transform.IsChildOf(generatedDoor.transform))
            {
                continue;
            }

            EncapsulateWorldBounds(cafeteria, renderer.bounds, ref initialized, ref localBounds);
        }

        if (initialized) return true;

        Collider[] colliders = cafeteria.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || collider.isTrigger) continue;
            if (collider.transform == generatedDoor.transform ||
                collider.transform.IsChildOf(generatedDoor.transform))
            {
                continue;
            }

            EncapsulateWorldBounds(cafeteria, collider.bounds, ref initialized, ref localBounds);
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

    private static bool TryGetFloorSurfaceY(GameObject roomFloor, out float floorY)
    {
        Collider collider = roomFloor.GetComponent<Collider>();
        if (collider != null)
        {
            floorY = collider.bounds.max.y;
            return true;
        }

        Renderer renderer = roomFloor.GetComponent<Renderer>();
        if (renderer != null)
        {
            floorY = renderer.bounds.max.y;
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
            if (renderer == null || !renderer.enabled) continue;

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
}

public class CursedFinalDoorwayCollisionBypass : MonoBehaviour
{
    public void Configure(
        Transform doorTransform,
        Collider[] wallBlockers,
        float width,
        float height,
        float depth)
    {
    }
}
