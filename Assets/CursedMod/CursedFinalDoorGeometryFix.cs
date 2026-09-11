using System.Collections;
using UnityEngine;

/// <summary>
/// Cuts the Room 99 finale entrance through an actual solid cafeteria wall.
/// Existing cafeteria doorways are never reused or removed.
/// </summary>
public class CursedFinalDoorGeometryFix : MonoBehaviour
{
    private const string DoorName = "Cafeteria Phase 2 Door";
    private const string RoomName = "Cafeteria Phase 2 Narrow Room";
    private const string FloorName = "Phase 2 Narrow Room Floor";
    private const string PassageName = "Cafeteria Phase 2 Physical Doorway Passage";
    private const string VoidName = "Cafeteria Phase 2 Doorway Void";
    private const string CutRootName = "Cafeteria Phase 2 Cut Wall";

    private const float RoomWidth = 4.8f;
    private const float RoomHeight = 4.6f;
    private const float RoomLength = 13f;
    private const float RoomStartOffset = 0.45f;
    private const float OpeningWidth = 4.55f;
    private const float OpeningHeight = 4.65f;
    private const float MaxThickness = 2f;
    private const float MaxWallDistance = 7f;
    private const float BoundaryTolerance = 3f;
    private const float MinSide = 0.18f;

    private static CursedFinalDoorGeometryFix instance;
    private int fixedDoorId;

    private sealed class WallHit
    {
        public Collider collider;
        public Renderer renderer;
        public Bounds bounds;
        public Vector3 outward;
        public Vector3 span;
        public Vector3 doorPosition;
        public float spanLength;
        public float thickness;
        public float score;
    }

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
        StartCoroutine(Watch());
    }

    private IEnumerator Watch()
    {
        while (true)
        {
            GameObject door = GameObject.Find(DoorName);
            GameObject room = GameObject.Find(RoomName);
            GameObject floor = GameObject.Find(FloorName);
            GameObject passage = GameObject.Find(PassageName);
            Transform cafeteria = FindSceneTransform("Cafeteria");

            if (door != null && room != null && floor != null && cafeteria != null)
            {
                int id = door.GetInstanceID();
                if (id != fixedDoorId && Apply(door, room, floor, passage, cafeteria))
                {
                    fixedDoorId = id;
                }
            }
            else if (door == null)
            {
                fixedDoorId = 0;
            }

            yield return null;
        }
    }

    private static bool Apply(GameObject door, GameObject room, GameObject roomFloor, GameObject passage, Transform cafeteria)
    {
        float floorY;
        if (!TryGetFloorY(roomFloor, out floorY)) return false;

        Bounds cafeteriaBounds;
        if (!TryGetLocalBounds(cafeteria, door, out cafeteriaBounds)) return false;

        Vector3 oldOutward = Horizontal(door.transform.forward);
        if (oldOutward.sqrMagnitude < 0.01f) oldOutward = Vector3.forward;
        oldOutward.Normalize();

        WallHit wall;
        if (!TryFindWall(cafeteria, cafeteriaBounds, door, floorY, oldOutward, out wall))
        {
            Debug.LogError("No suitable SOLID cafeteria wall was found for Room 99. Existing cafeteria doorways were intentionally left untouched.");
            return false;
        }

        Quaternion oldRot = Quaternion.LookRotation(oldOutward, Vector3.up);
        Quaternion newRot = Quaternion.LookRotation(wall.outward, Vector3.up);
        Vector3 oldOrigin = new Vector3(door.transform.position.x, floorY, door.transform.position.z);
        Vector3 newOrigin = new Vector3(wall.doorPosition.x, floorY, wall.doorPosition.z);

        if (!CarveWall(wall, floorY, cafeteria)) return false;

        Transform roomTransform = room.transform;
        for (int i = 0; i < roomTransform.childCount; i++)
            MoveFrame(roomTransform.GetChild(i), oldOrigin, oldRot, newOrigin, newRot);

        door.transform.position = wall.doorPosition;
        door.transform.rotation = newRot;
        AlignDoorBottom(door, floorY);

        DoorScript[] scripts = door.GetComponentsInChildren<DoorScript>(true);
        for (int i = 0; i < scripts.Length; i++)
        {
            if (scripts[i] == null) continue;
            scripts[i].enabled = true;
            scripts[i].UnlockDoor();
            scripts[i].openingDistance = Mathf.Max(scripts[i].openingDistance, 8f);
        }

        DisableFakeOpening(passage);
        Physics.SyncTransforms();
        Debug.Log("Room 99 carved through solid cafeteria wall '" + wall.collider.gameObject.name + "'. No existing doorway was consumed.");
        return true;
    }

    private static bool TryFindWall(Transform cafeteria, Bounds cafeteriaBounds, GameObject finalDoor, float floorY, Vector3 preferredOutward, out WallHit best)
    {
        best = null;
        Collider[] colliders = cafeteria.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider c = colliders[i];
            if (!IsWallLike(c, finalDoor)) continue;
            Bounds b = c.bounds;
            if (b.size.y < OpeningHeight + 0.1f) continue;

            bool thinX = b.size.x <= b.size.z;
            float thickness = thinX ? b.size.x : b.size.z;
            float spanLength = thinX ? b.size.z : b.size.x;
            if (thickness <= 0.02f || thickness > MaxThickness) continue;
            if (spanLength < OpeningWidth + MinSide * 2f) continue;

            float boundaryDistance = DistanceToCafeteriaBoundary(cafeteria, cafeteriaBounds, b.center);
            if (boundaryDistance > BoundaryTolerance) continue;

            Renderer renderer = FindMatchingRenderer(c, b);
            if (renderer == null) continue;

            Vector3 normal = thinX ? Vector3.right : Vector3.forward;
            Vector3 span = thinX ? Vector3.forward : Vector3.right;
            if (Vector3.Dot(normal, preferredOutward) < 0f) normal = -normal;

            Vector3 delta = finalDoor.transform.position - b.center;
            float planeDistance = Mathf.Abs(Vector3.Dot(delta, normal));
            if (planeDistance > MaxWallDistance) continue;

            float halfSpan = spanLength * 0.5f;
            float openingHalf = OpeningWidth * 0.5f;
            float rawOffset = Vector3.Dot(delta, span);
            float maxOffset = Mathf.Max(0f, halfSpan - openingHalf - MinSide);
            float offset = Mathf.Clamp(rawOffset, -maxOffset, maxOffset);

            if (floorY < b.min.y - 0.8f || floorY > b.min.y + 1.5f) continue;
            if (floorY + OpeningHeight > b.max.y + 0.15f) continue;

            Vector3 doorway = b.center + span * offset;
            doorway.y = floorY + OpeningHeight * 0.5f;

            int obstruction = ScoreRoomSpace(doorway, normal, c, finalDoor);
            float score = planeDistance * 8f + Mathf.Abs(rawOffset - offset) * 6f + thickness * 2f + boundaryDistance * 12f + obstruction * 28f;
            if (best != null && score >= best.score) continue;

            best = new WallHit
            {
                collider = c,
                renderer = renderer,
                bounds = b,
                outward = normal,
                span = span,
                doorPosition = doorway,
                spanLength = spanLength,
                thickness = thickness,
                score = score
            };
        }
        return best != null;
    }

    private static bool IsWallLike(Collider c, GameObject finalDoor)
    {
        if (c == null || !c.enabled || c.isTrigger || !c.gameObject.scene.IsValid()) return false;
        if (c.transform == finalDoor.transform || c.transform.IsChildOf(finalDoor.transform)) return false;
        if (c.GetComponentInParent<DoorScript>() != null) return false;
        if (c.GetComponentInParent<SwingingDoorScript>() != null) return false;

        string n = c.gameObject.name.ToLowerInvariant();
        if (n.Contains("floor") || n.Contains("ground") || n.Contains("ceiling") || n.Contains("door") || n.Contains("table") || n.Contains("chair") || n.Contains("desk") || n.Contains("trigger")) return false;
        return true;
    }

    private static Renderer FindMatchingRenderer(Collider c, Bounds cb)
    {
        Renderer direct = c.GetComponent<Renderer>();
        if (RendererMatches(direct, cb)) return direct;
        Renderer[] renderers = c.GetComponentsInChildren<Renderer>(true);
        Renderer best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (!RendererMatches(r, cb)) continue;
            float d = (r.bounds.center - cb.center).sqrMagnitude;
            if (d >= bestDistance) continue;
            bestDistance = d;
            best = r;
        }
        return best;
    }

    private static bool RendererMatches(Renderer r, Bounds cb)
    {
        if (r == null || !r.enabled || r.GetComponentInParent<DoorScript>() != null) return false;
        Bounds rb = r.bounds;
        if (rb.size.y < 2.5f || !rb.Intersects(cb)) return false;
        float cSpan = Mathf.Max(cb.size.x, cb.size.z);
        float rSpan = Mathf.Max(rb.size.x, rb.size.z);
        if (rSpan > cSpan * 2.5f + 1f) return false;
        if (rb.size.y > cb.size.y * 2f + 1f) return false;
        return Horizontal(rb.center - cb.center).magnitude <= cSpan * 0.75f + 1f;
    }

    private static int ScoreRoomSpace(Vector3 doorway, Vector3 outward, Collider selectedWall, GameObject finalDoor)
    {
        Quaternion rot = Quaternion.LookRotation(outward, Vector3.up);
        Vector3 floorBase = doorway - Vector3.up * (OpeningHeight * 0.5f);
        Vector3 center = floorBase + Vector3.up * (RoomHeight * 0.5f) + outward * (RoomStartOffset + RoomLength * 0.5f);
        Collider[] overlaps = Physics.OverlapBox(center, new Vector3(RoomWidth * 0.5f, RoomHeight * 0.5f, RoomLength * 0.5f), rot, ~0, QueryTriggerInteraction.Ignore);
        int score = 0;
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider o = overlaps[i];
            if (o == null || o == selectedWall) continue;
            if (o.transform == finalDoor.transform || o.transform.IsChildOf(finalDoor.transform)) continue;
            string n = o.gameObject.name.ToLowerInvariant();
            if (n.Contains("floor") || n.Contains("ground") || n.Contains("phase 2 narrow room")) continue;
            score += o.bounds.size.y >= 2.2f ? 3 : 1;
        }
        return score;
    }

    private static bool CarveWall(WallHit wall, float floorY, Transform cafeteria)
    {
        Bounds b = wall.bounds;
        float halfSpan = wall.spanLength * 0.5f;
        float openingHalf = OpeningWidth * 0.5f;
        float offset = Vector3.Dot(wall.doorPosition - b.center, wall.span);
        float openingLeft = offset - openingHalf;
        float openingRight = offset + openingHalf;
        float leftLength = openingLeft + halfSpan;
        float rightLength = halfSpan - openingRight;
        if (leftLength < MinSide || rightLength < MinSide) return false;

        float wallTop = b.max.y;
        float openingTop = Mathf.Min(wallTop, floorY + OpeningHeight);
        float lintelHeight = wallTop - openingTop;
        Quaternion rot = Quaternion.LookRotation(wall.outward, Vector3.up);
        Material material = FirstMaterial(wall.renderer);
        int layer = wall.collider.gameObject.layer;

        GameObject oldRoot = GameObject.Find(CutRootName);
        if (oldRoot != null) Destroy(oldRoot);
        GameObject root = new GameObject(CutRootName);
        root.transform.SetParent(cafeteria, true);

        float centerY = b.min.y + b.size.y * 0.5f;
        CreatePiece("Final Door Wall - Left", root.transform, b.center + wall.span * (-halfSpan + leftLength * 0.5f) + Vector3.up * (centerY - b.center.y), new Vector3(leftLength, b.size.y, wall.thickness), rot, material, layer);
        CreatePiece("Final Door Wall - Right", root.transform, b.center + wall.span * (openingRight + rightLength * 0.5f) + Vector3.up * (centerY - b.center.y), new Vector3(rightLength, b.size.y, wall.thickness), rot, material, layer);

        if (lintelHeight > 0.05f)
            CreatePiece("Final Door Wall - Lintel", root.transform, b.center + wall.span * offset + Vector3.up * ((openingTop + lintelHeight * 0.5f) - b.center.y), new Vector3(OpeningWidth, lintelHeight, wall.thickness), rot, material, layer);

        wall.collider.enabled = false;
        wall.renderer.enabled = false;
        return true;
    }

    private static void CreatePiece(string name, Transform parent, Vector3 position, Vector3 scale, Quaternion rotation, Material material, int layer)
    {
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = name;
        piece.transform.SetParent(parent, true);
        piece.transform.position = position;
        piece.transform.rotation = rotation;
        piece.transform.localScale = scale;
        piece.layer = layer;
        Renderer r = piece.GetComponent<Renderer>();
        if (r != null && material != null) r.sharedMaterial = material;
    }

    private static Material FirstMaterial(Renderer r)
    {
        if (r == null) return null;
        Material[] materials = r.sharedMaterials;
        for (int i = 0; i < materials.Length; i++) if (materials[i] != null) return materials[i];
        return r.sharedMaterial;
    }

    private static void DisableFakeOpening(GameObject passage)
    {
        if (passage != null)
        {
            Collider[] colliders = passage.GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++) if (colliders[i] != null) colliders[i].enabled = false;
            CursedDoorwayPassageTrigger oldBypass = passage.GetComponent<CursedDoorwayPassageTrigger>();
            if (oldBypass != null) oldBypass.enabled = false;
            CursedFinalDoorwayCollisionBypass newBypass = passage.GetComponent<CursedFinalDoorwayCollisionBypass>();
            if (newBypass != null) newBypass.enabled = false;
        }
        GameObject doorwayVoid = GameObject.Find(VoidName);
        if (doorwayVoid != null) doorwayVoid.SetActive(false);
    }

    private static void MoveFrame(Transform target, Vector3 oldOrigin, Quaternion oldRotation, Vector3 newOrigin, Quaternion newRotation)
    {
        Quaternion inv = Quaternion.Inverse(oldRotation);
        Vector3 localPosition = inv * (target.position - oldOrigin);
        Quaternion localRotation = inv * target.rotation;
        target.position = newOrigin + newRotation * localPosition;
        target.rotation = newRotation * localRotation;
    }

    private static void AlignDoorBottom(GameObject door, float floorY)
    {
        Bounds b;
        if (!TryGetRendererBounds(door, out b)) return;
        door.transform.position += Vector3.up * (floorY - b.min.y);
    }

    private static float DistanceToCafeteriaBoundary(Transform cafeteria, Bounds bounds, Vector3 world)
    {
        Vector3 p = cafeteria.InverseTransformPoint(world);
        return Mathf.Min(Mathf.Min(Mathf.Abs(p.x - bounds.min.x), Mathf.Abs(p.x - bounds.max.x)), Mathf.Min(Mathf.Abs(p.z - bounds.min.z), Mathf.Abs(p.z - bounds.max.z)));
    }

    private static bool TryGetFloorY(GameObject floor, out float y)
    {
        Collider c = floor.GetComponent<Collider>();
        if (c != null) { y = c.bounds.max.y; return true; }
        Renderer r = floor.GetComponent<Renderer>();
        if (r != null) { y = r.bounds.max.y; return true; }
        y = 0f;
        return false;
    }

    private static bool TryGetLocalBounds(Transform root, GameObject ignored, out Bounds localBounds)
    {
        bool initialized = false;
        localBounds = new Bounds(Vector3.zero, Vector3.zero);
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || r.transform == ignored.transform || r.transform.IsChildOf(ignored.transform)) continue;
            string n = r.gameObject.name.ToLowerInvariant();
            if (n.Contains("phase 2 narrow room") || n.Contains("cafeteria phase 2")) continue;
            Encapsulate(root, r.bounds, ref initialized, ref localBounds);
        }
        return initialized;
    }

    private static void Encapsulate(Transform root, Bounds world, ref bool initialized, ref Bounds local)
    {
        Vector3 c = world.center;
        Vector3 e = world.extents;
        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            Vector3 p = root.InverseTransformPoint(c + Vector3.Scale(e, new Vector3(x, y, z)));
            if (!initialized) { local = new Bounds(p, Vector3.zero); initialized = true; }
            else local.Encapsulate(p);
        }
    }

    private static bool TryGetRendererBounds(GameObject root, out Bounds b)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool initialized = false;
        b = new Bounds(root.transform.position, Vector3.zero);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || !r.enabled) continue;
            if (!initialized) { b = r.bounds; initialized = true; }
            else b.Encapsulate(r.bounds);
        }
        return initialized;
    }

    private static Vector3 Horizontal(Vector3 v) { v.y = 0f; return v; }

    private static Transform FindSceneTransform(string objectName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform t = transforms[i];
            if (t != null && t.gameObject.scene.IsValid() && t.name == objectName) return t;
        }
        return null;
    }
}

// Kept so old runtime references remain loadable. The solid-wall cut does not require a collision-bypass component anymore.
public class CursedFinalDoorwayCollisionBypass : MonoBehaviour
{
    public void Configure(Transform doorTransform, Collider[] wallBlockers, float width, float height, float depth) { }
}
