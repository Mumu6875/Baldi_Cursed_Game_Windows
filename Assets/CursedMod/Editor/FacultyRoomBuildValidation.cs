#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Validate the serialized IDs and table reference in the scene being built.</summary>
public sealed class FacultyRoomBuildValidation : IProcessSceneWithReport
{
    public int callbackOrder { get { return -900; } }

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (scene.name != "School") return;
        ValidateScene(scene);
    }

    [MenuItem("Cursed Baldi/Validate Shell Placement In Open School")]
    public static void ValidateOpenSchool()
    {
        Scene scene = SceneManager.GetSceneByName("School");
        if (!scene.IsValid() || !scene.isLoaded)
            throw new BuildFailedException("Open the School scene before validating Shell placement.");
        ValidateScene(scene);
    }

    private static void ValidateScene(Scene scene)
    {
        HashSet<int> ids = new HashSet<int>();
        FacultyRoomIdentity shellRoom = null;
        GameControllerScript controller = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (GameControllerScript candidate in root.GetComponentsInChildren<GameControllerScript>(true))
            {
                if (controller != null) throw new BuildFailedException("School must have one game controller.");
                controller = candidate;
            }
            foreach (FacultyRoomIdentity room in root.GetComponentsInChildren<FacultyRoomIdentity>(true))
            {
                if (room.RoomId <= 0 || !ids.Add(room.RoomId))
                    throw new BuildFailedException("Missing or duplicate faculty room ID: " + room.RoomId);
                if (room.RoomId == FacultyRoomIdentity.AlarmClockRoomId) shellRoom = room;
            }
            // Each direct child of the faculty-room group must have its own identity.
            foreach (Transform group in root.GetComponentsInChildren<Transform>(true))
            {
                // HallDoors also contains a FacultyRooms group; only Rooms contains actual rooms.
                if (group.name != "FacultyRooms" || group.parent == null || group.parent.name != "Rooms") continue;
                foreach (Transform child in group)
                {
                    if (child.GetComponent<FacultyRoomIdentity>() == null)
                        throw new BuildFailedException("Faculty room has no identity: " + child.name);
                }
            }
        }
        if (shellRoom == null || shellRoom.ItemTable == null || shellRoom.ItemStyleReference == null ||
            !shellRoom.ItemTable.transform.IsChildOf(shellRoom.transform) ||
            !shellRoom.ItemTable.enabled || !shellRoom.ItemTable.gameObject.activeInHierarchy ||
            shellRoom.ItemTable.isTrigger || shellRoom.ItemStyleReference.gameObject.scene != scene ||
            shellRoom.ItemStyleReference.sharedMaterial == null)
            throw new BuildFailedException("Shell room ID 3 must reference its own active table and an item sprite.");

        Bounds expected;
        if (controller == null || controller.playerCamera == null || controller.playerTransform == null ||
            !ShellItem.TryGetPickupBounds(shellRoom, out expected))
            throw new BuildFailedException("Shell needs a valid player/camera and a visible desk wide enough for the pickup.");

        float eyeY = controller.playerCamera.transform.position.y;
        if (eyeY <= expected.min.y + 0.25f || eyeY >= expected.max.y - 0.25f)
            throw new BuildFailedException("Shell interaction volume must intersect the player's horizontal reticle with clearance.");

        Sprite icon = Resources.Load<Sprite>("CursedMod/Shell");
        if (icon == null || icon.bounds.size.y <= 0f)
            throw new BuildFailedException("Shell sprite cannot be loaded for placement validation.");

        // Exercise the actual runtime factory, then remove the temporary object
        // so builds still contain exactly one Phase-2-only runtime pickup.
        GameObject probe = null;
        try
        {
            probe = ShellItem.CreatePickup(controller, shellRoom, icon, expected);
            Physics.SyncTransforms();
            BoxCollider collider = probe.GetComponent<BoxCollider>();
            SpriteRenderer image = probe.GetComponentInChildren<SpriteRenderer>();
            if (collider == null || image == null || !image.enabled || image.sprite != icon ||
                !probe.activeInHierarchy || image.bounds.min.y < expected.min.y - 0.01f ||
                image.bounds.size.x > shellRoom.ItemTable.bounds.size.x ||
                image.bounds.size.x > shellRoom.ItemTable.bounds.size.z)
                throw new BuildFailedException("Shell runtime visual must be active, above the desk and fit on its surface.");

            foreach (Vector3 direction in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
            {
                Vector3 origin = expected.center + direction * 4f;
                origin.y = eyeY;
                RaycastHit hit;
                if (!collider.Raycast(new Ray(origin, -direction), out hit, 8f))
                    throw new BuildFailedException("Shell cannot be targeted at camera height from " + direction);
            }
        }
        finally
        {
            if (probe != null) Object.DestroyImmediate(probe);
        }
        Debug.Log("Faculty room IDs and Shell table validated: " + ids.Count + " rooms.");
        Debug.Log("Shell runtime factory, visible tabletop placement and four horizontal interaction rays validated.");
    }
}
#endif
