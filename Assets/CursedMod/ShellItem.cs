using UnityEngine;
using UnityEngine.AI;

/// <summary>Phase 2 pickup and inventory registration. Existing item IDs stay unchanged.</summary>
public static class ShellItem
{
    public const int ItemId = 12;
    public const float Duration = 10f;

    public static void Register(GameControllerScript controller)
    {
        Texture2D icon = Resources.Load<Texture2D>("CursedMod/Shell");
        if (controller.itemTextures.Length <= ItemId)
            System.Array.Resize(ref controller.itemTextures, ItemId + 1);
        controller.itemTextures[ItemId] = icon;
    }

    public static bool TryUse(GameControllerScript controller)
    {
        if (controller == null || !controller.CanUseItems) return false;
        BaldiScript baldi = controller.baldiScrpt;
        if (baldi == null || !baldi.isActiveAndEnabled) return false;
        // Phase 2 initially shows normal Baldi: only the actual cursed skin qualifies.
        CursedBaldiVisual cursed = baldi.GetComponentInChildren<CursedBaldiVisual>();
        if (cursed == null || !cursed.isActiveAndEnabled) return false;
        SpriteRenderer body = cursed.GetComponent<SpriteRenderer>();
        Sprite shell = Resources.Load<Sprite>("CursedMod/Shell");
        AudioClip sound = Resources.Load<AudioClip>("CursedMod/ShellUse");
        NavMeshAgent agent = baldi.GetComponent<NavMeshAgent>();
        if (body == null || body.sprite == null || shell == null || sound == null ||
            agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return false;

        ShellEffect effect = baldi.GetComponent<ShellEffect>();
        if (effect == null) effect = baldi.gameObject.AddComponent<ShellEffect>();
        effect.Begin(baldi, body, shell, sound);
        return true;
    }

    public static void InstallPickup(GameControllerScript controller)
    {
        if (controller == null || !CursedPhaseManager.IsTestRoomEnabled ||
            controller.gameObject.scene.name != "School") return;

        // Include collected (inactive) pickups so reinstallation cannot respawn the item.
        foreach (PickupScript existing in Resources.FindObjectsOfTypeAll<PickupScript>())
        {
            if (existing.gameObject.scene == controller.gameObject.scene &&
                existing.name == "Pickup_Shell") return;
        }

        Sprite icon = Resources.Load<Sprite>("CursedMod/Shell");
        FacultyRoomIdentity room = FacultyRoomIdentity.FindInScene(
            controller.gameObject.scene, FacultyRoomIdentity.AlarmClockRoomId);
        if (icon == null || room == null || !room.isActiveAndEnabled || room.ItemTable == null)
        {
            Debug.LogError("Shell sprite or faculty room ID 3/table reference is missing.");
            return;
        }

        // The scene stores the exact desk reference: no room names or coordinate search.
        Bounds tabletop = room.ItemTable.bounds;
        GameObject pickup = new GameObject("Pickup_Shell");
        pickup.tag = "Item";
        // Parent to the room, not the scaled desk, to preserve item size.
        pickup.transform.SetParent(room.transform, true);
        pickup.transform.position = new Vector3(tabletop.center.x, tabletop.max.y, tabletop.center.z);
        CapsuleCollider collider = pickup.AddComponent<CapsuleCollider>();
        collider.isTrigger = true;
        collider.center = Vector3.up * 1.25f;
        collider.radius = 1.25f;
        collider.height = 2.5f;
        PickupScript interaction = pickup.AddComponent<PickupScript>();
        interaction.gc = controller;
        interaction.player = controller.playerTransform;

        GameObject image = new GameObject("Shell Sprite");
        image.transform.SetParent(pickup.transform, false);
        image.transform.localPosition = Vector3.up * 1.25f;
        image.transform.localScale = Vector3.one * (2.5f / icon.bounds.size.y);
        SpriteRenderer renderer = image.AddComponent<SpriteRenderer>();
        renderer.sprite = icon;
        SpriteRenderer reference = room.ItemStyleReference;
        if (reference != null) renderer.sharedMaterial = reference.sharedMaterial;
        image.AddComponent<Billboard>();
    }
}
