using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps Baldi (including the cursed Baldi variants) as the game's only NPC.
/// Objects are disabled instead of destroyed so the original scene's serialized
/// references remain valid and cannot cause MissingReferenceExceptions.
/// </summary>
public sealed class BaldiOnlyNpcCleanup : MonoBehaviour
{
    private static readonly HashSet<string> RemovedCharacterNames = new HashSet<string>
    {
        "Playtime",
        "PlaytimeSprite",
        "Principal of the Thing",
        "PrincipalSprite",
        "1st Prize",
        "Arts and Crafters",
        "Gotta Sweep",
        "Its a Bully",
        "BullySprite"
    };

    private static BaldiOnlyNpcCleanup instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (instance != null) return;

        GameObject host = new GameObject("Baldi Only NPC Cleanup");
        instance = host.AddComponent<BaldiOnlyNpcCleanup>();
        DontDestroyOnLoad(host);
    }

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SuppressNonBaldiCharacters(scene);
        StartCoroutine(SuppressAfterSceneStart(scene));
    }

    private IEnumerator SuppressAfterSceneStart(Scene scene)
    {
        // A second pass catches anything instantiated by another component's Start().
        yield return null;
        if (scene.isLoaded) SuppressNonBaldiCharacters(scene);
    }

    private static void SuppressNonBaldiCharacters(Scene scene)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];
            if (target == null || target.gameObject.scene != scene) continue;
            if (!RemovedCharacterNames.Contains(target.name)) continue;

            DisableCharacter(target.gameObject);
        }

        // Arts and Crafters uses separate hallway triggers. Remove their behavior
        // as well so no invisible NPC-related interaction remains in the school.
        CraftersTriggerScript[] craftersTriggers = Resources.FindObjectsOfTypeAll<CraftersTriggerScript>();
        for (int i = 0; i < craftersTriggers.Length; i++)
        {
            CraftersTriggerScript trigger = craftersTriggers[i];
            if (trigger != null && trigger.gameObject.scene == scene)
            {
                DisableCharacter(trigger.gameObject);
            }
        }
    }

    private static void DisableCharacter(GameObject character)
    {
        Behaviour[] behaviours = character.GetComponentsInChildren<Behaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null) behaviours[i].enabled = false;
        }

        Renderer[] renderers = character.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].enabled = false;
        }

        Collider[] colliders = character.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null) colliders[i].enabled = false;
        }

        Rigidbody[] rigidbodies = character.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody body = rigidbodies[i];
            if (body == null) continue;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.detectCollisions = false;
            body.isKinematic = true;
        }

        NavMeshAgent[] agents = character.GetComponentsInChildren<NavMeshAgent>(true);
        for (int i = 0; i < agents.Length; i++)
        {
            if (agents[i] != null) agents[i].enabled = false;
        }

        character.SetActive(false);
    }
}
