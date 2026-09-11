using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Final-pass geometry correction for the runtime-generated cafeteria finale door.
/// The finale door itself still uses the normal DoorScript. This component only
/// fixes runtime placement and removes unrelated scene-wall collision from the
/// small doorway aperture while the player is actually near that doorway.
/// </summary>
public class CursedFinalDoorGeometryFix : MonoBehaviour
{
    private const string FinaleDoorName = "Cafeteria Phase 2 Door";
    private const string RoomFloorName = "Phase 2 Narrow Room Floor";
    private const string PassageName = "Cafeteria Phase 2 Physical Doorway Passage";
    private const string DoorwayVoidName = "Cafeteria Phase 2 Doorway Void";

    private const float DoorwayWidth = 4.5f;
    private const float DoorwayHeight = 5.4f;
    private const float DoorwayDepth = 5.6f;
    private const float PassageInsideOffset = 0.75f;

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

        // This helper trigger must never steal DoorScript's interaction ray.
        passage.layer = ignoreRaycastLayer;

        Vector3 outward = door.transform.forward;
        outward.y = 0f;
        if (outward.sqrMagnitude < 0.01f) outward = Vector3.forward;
        outward.Normalize();

        passage.transform.position =
            new Vector3(
                door.transform.position.x,
                floorY + DoorwayHeight * 0.5f,
                door.transform.position.z)
            - outward * PassageInsideOffset;
        passage.transform.rotation = Quaternion.LookRotation(outward, Vector3.up);

        BoxCollider passageCollider = passage.GetComponent<BoxCollider>();
        if (passageCollider != null)
        {
            passageCollider.isTrigger = true;
            passageCollider.size = new Vector3(DoorwayWidth, DoorwayHeight, DoorwayDepth);
        }

        // The previous implementation only accepted colliders parented under
        // Cafeteria. Schoolhouse wall colliders may live under another scene root,
        // so that filter could miss the real wall completely.
        Collider[] blockers = FindDoorwayBlockers(door, floorY, outward);

        // Disable the older trigger-event-only bypass. A proximity watcher below
        // is more reliable because it applies before the CharacterController hits
        // the wall, even if an OnTriggerEnter event is missed.
        CursedDoorwayPassageTrigger legacyPassage =
            passage.GetComponent<CursedDoorwayPassageTrigger>();
        if (legacyPassage != null)
        {
            legacyPassage.blockingColliders = new Collider[0];
            legacyPassage.enabled = false;
        }

        CursedFinalDoorwayCollisionBypass bypass =
            passage.GetComponent<CursedFinalDoorwayCollisionBypass>();
        if (bypass == null)
        {
            bypass = passage.AddComponent<CursedFinalDoorwayCollisionBypass>();
        }

        bypass.Configure(door.transform, blockers, DoorwayWidth, DoorwayHeight, DoorwayDepth);

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
            "Finale doorway fixed: " + blockers.Length +
            " scene-wall blocker(s) detected around the physical doorway.");
    }

    private static Collider[] FindDoorwayBlockers(
        GameObject door,
        float floorY,
        Vector3 outward)
    {
        Quaternion rotation = Quaternion.LookRotation(outward, Vector3.up);
        Vector3 center =
            new Vector3(
                door.transform.position.x,
                floorY + DoorwayHeight * 0.5f,
                door.transform.position.z)
            - outward * PassageInsideOffset;

        Collider[] overlaps = Physics.OverlapBox(
            center,
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
            if (candidate == null || !candidate.enabled || candidate.isTrigger) continue;

            if (candidate.transform == door.transform ||
                candidate.transform.IsChildOf(door.transform))
            {
                continue;
            }

            // Never bypass gameplay characters, normal doors or moving agents.
            if (candidate.GetComponentInParent<PlayerScript>() != null) continue;
            if (candidate.GetComponentInParent<DoorScript>() != null) continue;
            if (candidate.GetComponentInParent<NavMeshAgent>() != null) continue;

            string lowerName = candidate.gameObject.name.ToLowerInvariant();

            // Generated room geometry must remain solid, and horizontal surfaces
            // are not the wall that is blocking the doorway.
            if (lowerName.Contains("phase 2 narrow room") ||
                lowerName.Contains("physical doorway") ||
                lowerName.Contains("doorway void") ||
                lowerName.Contains("final exit safety blocker") ||
                lowerName.Contains("floor") ||
                lowerName.Contains("ground") ||
                lowerName.Contains("ceiling"))
            {
                continue;
            }

            // The obstruction we are after is wall-height geometry. This avoids
            // ignoring chairs, tables and small props that happen to be nearby.
            if (candidate.bounds.size.y < 2.2f) continue;

            blockers.Add(candidate);
        }

        return blockers.ToArray();
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
}

/// <summary>
/// Ignores only the detected scene-wall colliders while the player's
/// CharacterController is physically close to the finale doorway. The real
/// DoorScript barrier is intentionally NOT in this list, so a closed door still
/// blocks the player exactly like every normal Schoolhouse door.
/// </summary>
public class CursedFinalDoorwayCollisionBypass : MonoBehaviour
{
    private Transform door;
    private Collider[] blockers;
    private PlayerScript player;
    private float doorwayWidth;
    private float doorwayHeight;
    private float doorwayDepth;
    private bool collisionsIgnored;

    public void Configure(
        Transform doorTransform,
        Collider[] wallBlockers,
        float width,
        float height,
        float depth)
    {
        door = doorTransform;
        blockers = wallBlockers ?? new Collider[0];
        doorwayWidth = width;
        doorwayHeight = height;
        doorwayDepth = depth;
        player = FindFirstObjectByType<PlayerScript>();
        RefreshState();
    }

    private void Update()
    {
        RefreshState();
    }

    private void OnDisable()
    {
        SetIgnored(false);
    }

    private void OnDestroy()
    {
        SetIgnored(false);
    }

    private void RefreshState()
    {
        if (door == null) return;

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerScript>();
            if (player == null) return;
        }

        Vector3 local = door.InverseTransformPoint(player.transform.position);

        // Slight padding starts the bypass before the CharacterController can
        // contact the wall and keeps it active until the player is clearly
        // through the doorway on the other side.
        bool nearDoorway =
            Mathf.Abs(local.x) <= doorwayWidth * 0.5f + 0.8f &&
            Mathf.Abs(local.z) <= doorwayDepth * 0.5f + 1.0f &&
            Mathf.Abs(local.y) <= doorwayHeight + 1.0f;

        SetIgnored(nearDoorway);
    }

    private void SetIgnored(bool ignored)
    {
        if (collisionsIgnored == ignored) return;
        if (player == null || player.cc == null)
        {
            collisionsIgnored = ignored;
            return;
        }

        for (int i = 0; i < blockers.Length; i++)
        {
            Collider blocker = blockers[i];
            if (blocker == null) continue;
            Physics.IgnoreCollision(player.cc, blocker, ignored);
        }

        collisionsIgnored = ignored;
    }
}
