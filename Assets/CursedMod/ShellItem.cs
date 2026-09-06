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
        if (Time.timeScale <= 0f || controller.gamePaused || controller.learningActive) return false;
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
            controller.gameObject.scene.name != "School" || GameObject.Find("Pickup_Shell") != null) return;
        Sprite icon = Resources.Load<Sprite>("CursedMod/Shell");
        GameObject anchor = GameObject.Find("Pickup_AlarmClock");
        if (icon == null || anchor == null) return;

        // Place beside the existing clock, on reachable floor and on the same side of walls.
        Vector3[] offsets = { Vector3.right * 4f, Vector3.left * 4f,
            Vector3.forward * 4f, Vector3.back * 4f };
        foreach (Vector3 offset in offsets)
        {
            NavMeshHit hit;
            // The clock sits on a table at y=4; query near the floor, not at tabletop height.
            if (!NavMesh.SamplePosition(anchor.transform.position + offset + Vector3.down * 4f,
                out hit, 3f, NavMesh.AllAreas)) continue;
            Vector3 separation = hit.position - anchor.transform.position;
            separation.y = 0f;
            if (separation.magnitude < 3.2f) continue;
            Vector3 center = hit.position + Vector3.up * 1.5f;
            if (Physics.Linecast(anchor.transform.position + Vector3.up * 1.5f, center,
                769, QueryTriggerInteraction.Ignore)) continue;
            if (Physics.CheckSphere(center, 1.25f, 769, QueryTriggerInteraction.Ignore)) continue;

            GameObject pickup = new GameObject("Pickup_Shell");
            pickup.tag = "Item";
            pickup.transform.position = hit.position;
            CapsuleCollider collider = pickup.AddComponent<CapsuleCollider>();
            collider.isTrigger = true;
            collider.center = Vector3.up * 1.5f;
            collider.radius = 1.25f;
            collider.height = 2.5f;
            PickupScript interaction = pickup.AddComponent<PickupScript>();
            interaction.gc = controller;
            interaction.player = controller.playerTransform;

            GameObject image = new GameObject("Shell Sprite");
            image.transform.SetParent(pickup.transform, false);
            image.transform.localPosition = Vector3.up * 1.5f;
            image.transform.localScale = Vector3.one * (2.5f / icon.bounds.size.y);
            SpriteRenderer renderer = image.AddComponent<SpriteRenderer>();
            renderer.sprite = icon;
            SpriteRenderer reference = anchor.GetComponentInChildren<SpriteRenderer>();
            if (reference != null) renderer.sharedMaterial = reference.sharedMaterial;
            image.AddComponent<Billboard>();
            return;
        }
        Debug.LogError("Shell pickup could not find clear floor beside the Alarm Clock.");
    }
}
