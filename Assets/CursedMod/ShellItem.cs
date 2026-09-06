using UnityEngine;
using UnityEngine.AI;

/// <summary>Phase 2 pickup and inventory registration. Existing item IDs stay unchanged.</summary>
public static class ShellItem
{
    public const int ItemId = 12;
    public const float Duration = 10f;
    public const float PickupSize = 3f;
    public const float TableClearance = 0.05f;

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
        if (controller == null || !CursedPhaseManager.IsPhase2 ||
            CursedPhaseManager.IsPhase3 || CursedPhaseManager.IsPhase4 ||
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
        Bounds pickupBounds;
        if (icon == null || icon.bounds.size.y <= 0f || controller.playerTransform == null ||
            !TryGetPickupBounds(room, out pickupBounds) || room.ItemStyleReference == null)
        {
            Debug.LogError("Shell setup failed: check sprite, player, faculty room ID 3, desk mesh and item material references.", controller);
            return;
        }

        CreatePickup(controller, room, icon, pickupBounds);
        Debug.Log("Shell installed in faculty room ID " + room.RoomId +
            " at " + pickupBounds.center + " (item ID " + ItemId + ").", controller);
    }

    // Shared by runtime installation and the scene build validator.
    public static bool TryGetPickupBounds(FacultyRoomIdentity room, out Bounds pickupBounds)
    {
        pickupBounds = new Bounds();
        if (room == null || !room.isActiveAndEnabled || room.ItemTable == null ||
            !room.ItemTable.enabled || !room.ItemTable.gameObject.activeInHierarchy ||
            room.ItemTable.isTrigger || !room.ItemTable.transform.IsChildOf(room.transform)) return false;

        Bounds table = room.ItemTable.bounds;
        float surfaceY = table.max.y;
        bool hasMesh = false;
        foreach (MeshRenderer mesh in room.ItemTable.GetComponentsInChildren<MeshRenderer>())
        {
            if (!mesh.enabled || !mesh.gameObject.activeInHierarchy) continue;
            hasMesh = true;
            surfaceY = Mathf.Max(surfaceY, mesh.bounds.max.y);
        }
        if (!hasMesh || table.size.x < PickupSize || table.size.z < PickupSize) return false;

        // The visual desk extends above its collision box. Clear both surfaces.
        pickupBounds = new Bounds(new Vector3(table.center.x,
            surfaceY + TableClearance + PickupSize * 0.5f, table.center.z),
            Vector3.one * PickupSize);
        return true;
    }

    public static GameObject CreatePickup(GameControllerScript controller, FacultyRoomIdentity room,
        Sprite icon, Bounds pickupBounds)
    {
        GameObject pickup = new GameObject("Pickup_Shell");
        pickup.tag = "Item";
        // Parent to the room, not the scaled desk, to preserve item size.
        pickup.transform.SetParent(room.transform, true);
        pickup.transform.position = new Vector3(pickupBounds.center.x, pickupBounds.min.y, pickupBounds.center.z);
        // A box gives the horizontal reticle a broad target instead of a tangent
        // at the very top of the old spherical capsule.
        BoxCollider collider = pickup.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.center = Vector3.up * (PickupSize * 0.5f);
        collider.size = Vector3.one * PickupSize;
        PickupScript interaction = pickup.AddComponent<PickupScript>();
        interaction.gc = controller;
        interaction.player = controller.playerTransform;

        GameObject image = new GameObject("Shell Sprite");
        image.transform.SetParent(pickup.transform, false);
        float imageScale = PickupSize / icon.bounds.size.y;
        image.transform.localPosition = Vector3.up * (PickupSize * 0.5f) - icon.bounds.center * imageScale;
        image.transform.localScale = Vector3.one * imageScale;
        SpriteRenderer renderer = image.AddComponent<SpriteRenderer>();
        renderer.sprite = icon;
        SpriteRenderer reference = room.ItemStyleReference;
        if (reference != null) renderer.sharedMaterial = reference.sharedMaterial;
        image.AddComponent<Billboard>();
        return pickup;
    }
}
