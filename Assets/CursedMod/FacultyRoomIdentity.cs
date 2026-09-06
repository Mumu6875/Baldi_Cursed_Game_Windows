using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Authored room IDs are saved in the scene, never assigned by discovery order.</summary>
[DisallowMultipleComponent]
public sealed class FacultyRoomIdentity : MonoBehaviour
{
    public const int AlarmClockRoomId = 3;

    [SerializeField] private int roomId;
    [SerializeField] private BoxCollider itemTable;
    [SerializeField] private SpriteRenderer itemStyleReference;

    public int RoomId { get { return roomId; } }
    public BoxCollider ItemTable { get { return itemTable; } }
    public SpriteRenderer ItemStyleReference { get { return itemStyleReference; } }

    public static FacultyRoomIdentity FindInScene(Scene scene, int id)
    {
        FacultyRoomIdentity found = null;
        foreach (FacultyRoomIdentity candidate in Resources.FindObjectsOfTypeAll<FacultyRoomIdentity>())
        {
            if (candidate.gameObject.scene != scene || candidate.RoomId != id) continue;
            if (found != null)
            {
                Debug.LogError("Duplicate faculty room ID in " + scene.name + ": " + id);
                return null;
            }
            found = candidate;
        }
        return found;
    }
}
