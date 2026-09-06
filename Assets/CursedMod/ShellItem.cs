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
        GameObject anchor = GameObject.Find("Pickup_AlarmClock");
        GameObject room = GameObject.Find("FacultyRoom1");
        if (icon == null || anchor == null || room == null) return;
        Transform furniture = room.transform.Find("Objects");
        if (furniture == null) return;

        // School.unity: this specific desk is in the Alarm Clock's faculty room.
        // Room-local position is stable even when Environment is moved or rotated.
        Transform table = null;
        bool clockInThisRoom = false;
        foreach (Transform child in furniture)
        {
            if (child.name != "Desk") continue;
            BoxCollider deskCollider = child.GetComponent<BoxCollider>();
            if (deskCollider == null) continue;
            Bounds bounds = deskCollider.bounds;
            Vector3 clock = anchor.transform.position;
            if (clock.x >= bounds.min.x && clock.x <= bounds.max.x &&
                clock.z >= bounds.min.z && clock.z <= bounds.max.z)
                clockInThisRoom = true;
            if ((child.localPosition - new Vector3(-4f, 1f, -10f)).sqrMagnitude < 0.001f)
                table = child;
        }
        if (!clockInThisRoom || table == null)
        {
            Debug.LogError("Shell's designated desk in the Alarm Clock faculty room was not found.");
            return;
        }

        Bounds tabletop = table.GetComponent<BoxCollider>().bounds;
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
        SpriteRenderer reference = anchor.GetComponentInChildren<SpriteRenderer>();
        if (reference != null) renderer.sharedMaterial = reference.sharedMaterial;
        image.AddComponent<Billboard>();
    }
}
