using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Replaces the old 31718 test entities with two harmless wandering cursed
/// figures and mounts the generated laboratory poster on the back wall.
/// </summary>
public sealed class CursedTestRoomSetup : MonoBehaviour
{
    private static CursedTestRoomSetup instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (instance != null) return;
        GameObject host = new GameObject("Cursed Test Room Setup");
        instance = host.AddComponent<CursedTestRoomSetup>();
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
        if (scene.name == "TestRoom")
        {
            if (!CursedPhaseManager.IsTestRoomEnabled)
            {
                SceneManager.LoadSceneAsync("MainMenu");
                return;
            }

            StartCoroutine(ConfigureAfterSceneStart(scene));
            StartCoroutine(QuitToPhase1AfterDelay(scene));
        }
    }

    private IEnumerator QuitToPhase1AfterDelay(Scene scene)
    {
        yield return new WaitForSecondsRealtime(66.6f);
        if (!scene.isLoaded) yield break;

        // Persist the reset before Windows closes so the next launch starts
        // from Phase 1 and cannot reopen the 31718 route.
        CursedPhaseManager.ResetToPhase1();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator ConfigureAfterSceneStart(Scene scene)
    {
        // Let the scene finish Start() before removing its legacy NPC container.
        yield return null;
        if (!scene.isLoaded) yield break;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && roots[i].name == "NPC")
            {
                Destroy(roots[i]);
            }
        }

        Texture2D cursedTexture = Resources.Load<Texture2D>("CursedMod/CursedBaldi");
        if (cursedTexture != null)
        {
            cursedTexture.wrapMode = TextureWrapMode.Clamp;
            Sprite cursedSprite = Sprite.Create(
                cursedTexture,
                new Rect(0f, 0f, cursedTexture.width, cursedTexture.height),
                new Vector2(0.5f, 0.5344603f),
                256f);
            cursedSprite.name = "Harmless Test Entity Sprite";

            CreateHarmlessEntity("Harmless Cursed Entity 1", new Vector3(-6f, 0f, -2f), cursedSprite);
            CreateHarmlessEntity("Harmless Cursed Entity 2", new Vector3(5f, 0f, 3f), cursedSprite);
        }
        else
        {
            Debug.LogError("TestRoom could not load the cursed entity texture.");
        }

        InstallPoster();
    }

    private static void CreateHarmlessEntity(string objectName, Vector3 preferredPosition, Sprite sprite)
    {
        GameObject entity = new GameObject(objectName, typeof(NavMeshAgent), typeof(CursedHarmlessWanderer));
        NavMeshAgent agent = entity.GetComponent<NavMeshAgent>();
        agent.speed = 1.8f;
        agent.acceleration = 7f;
        agent.angularSpeed = 120f;
        agent.radius = 0.32f;
        agent.height = 2.56f;
        agent.stoppingDistance = 0.15f;
        agent.autoBraking = true;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(preferredPosition, out hit, 12f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
        else
        {
            entity.transform.position = preferredPosition;
            agent.enabled = false;
        }

        GameObject visual = new GameObject("Entity Visual", typeof(SpriteRenderer), typeof(Billboard));
        visual.transform.SetParent(entity.transform, false);
        SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.white;

        // Match the gameplay reference using the actual imported sprite size.
        float scaleX = CursedBaldiSizing.ReferenceBodyWidthPixels / 100f * 3.2f /
            CursedBaldiSizing.BodyWidthUnits(sprite);
        float scaleY = CursedBaldiSizing.CanonicalVisibleWorldHeight /
            CursedBaldiSizing.VisibleHeightUnits(sprite);
        visual.transform.localScale = new Vector3(scaleX, scaleY, 1f);

        float cursedFootOffset =
            (sprite.bounds.min.y + CursedBaldiSizing.BottomPaddingUnits(sprite)) * scaleY;
        visual.transform.localPosition = Vector3.up * -cursedFootOffset;
    }

    private static void InstallPoster()
    {
        Texture2D posterTexture = Resources.Load<Texture2D>("CursedMod/TestRoomEntityPoster");
        if (posterTexture == null)
        {
            Debug.LogError("TestRoom laboratory poster could not be loaded.");
            return;
        }

        posterTexture.wrapMode = TextureWrapMode.Clamp;
        posterTexture.filterMode = FilterMode.Bilinear;

        GameObject poster = GameObject.CreatePrimitive(PrimitiveType.Quad);
        poster.name = "BALD.ENTITY Laboratory Poster";
        poster.transform.position = new Vector3(2f, 2.65f, -8.56f);
        // Unity's Quad front normal is local -Z. Rotate it so the front faces
        // +Z, toward the TestRoom interior instead of into the back wall.
        poster.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        poster.transform.localScale = new Vector3(6f, 6f * posterTexture.height / posterTexture.width, 1f);

        Collider posterCollider = poster.GetComponent<Collider>();
        if (posterCollider != null) Destroy(posterCollider);

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Texture");
        Material material = new Material(shader);
        material.name = "TestRoom Laboratory Poster Material";
        material.mainTexture = posterTexture;
        material.color = Color.white;
        poster.GetComponent<MeshRenderer>().material = material;
    }
}

/// <summary>
/// Random NavMesh wandering only. It has no player target, collision damage,
/// jumpscare, audio, item interaction or game-over behavior.
/// </summary>
public sealed class CursedHarmlessWanderer : MonoBehaviour
{
    private NavMeshAgent agent;
    private float destinationTimer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        destinationTimer -= Time.deltaTime;
        if (destinationTimer <= 0f || (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f))
        {
            PickDestination();
        }
    }

    private void PickDestination()
    {
        destinationTimer = Random.Range(2.5f, 5f);
        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle * 9f;
            Vector3 candidate = transform.position + new Vector3(offset.x, 0f, offset.y);
            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidate, out hit, 3f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
    }
}
