#if UNITY_EDITOR
using System.Collections.Generic;
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
        HashSet<int> ids = new HashSet<int>();
        FacultyRoomIdentity shellRoom = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
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
            shellRoom.ItemTable.isTrigger || shellRoom.ItemStyleReference.gameObject.scene != scene)
            throw new BuildFailedException("Shell room ID 3 must reference its own active table and an item sprite.");
        Debug.Log("Faculty room IDs and Shell table validated: " + ids.Count + " rooms.");
    }
}
#endif
