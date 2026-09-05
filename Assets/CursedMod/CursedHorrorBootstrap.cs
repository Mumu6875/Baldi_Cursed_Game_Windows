using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Installs the horror skin and atmosphere without replacing the original Baldi gameplay code.
/// This keeps the mod based on the original Baldi character and school project.
/// </summary>
public class CursedHorrorBootstrap : MonoBehaviour
{
    private const string NormalHowToPlayText =
        "Oh noes! School is out, but your friend has a problem! " +
        "He left all his noteboos in school, but doesn't have time to go get them, " +
        "because if he does he'll be late for eating pracitce. To help him out, you have to go back in " +
        "the school and find all his notebooks for him. It won't be easy though! " +
        "Baldi loves challenging his students with fun trivia problems whenever he can! " +
        "Each time you find a notebook, you'll have to answer some questions.\n\n" +
        "To win the game, you must find 7 notebooks, and then exit the school! " +
        "You have to have fun too, of course!";

    private static readonly string Phase2HowToPlayText = NormalHowToPlayText
        .Replace("your friend has a problem!", "<color=#FF0000>BALD.ENTITY has a problem!</color>")
        .Replace("because if he does he'll be late for eating pracitce.",
            "<color=#FF0000>because if he does he'll be late for ekatsim a ma I pracitce.</color>")
        .Replace("Baldi loves challenging his students with fun trivia problems whenever he can!",
            "<color=#FF0000>BALD.ENTITY loves challenging his students with fun trivia problems whenever he can!</color>")
        .Replace("and then exit the school!", "<color=#FF0000>and then get stuck in the school!</color>");

    private static CursedHorrorBootstrap instance;
    private Texture2D cursedBaldiTexture;
    private Texture2D cursedThinkPadTexture;
    private Texture2D helpMeExitTexture;
    private Texture2D phase2SchoolRulesTexture;
    private Texture2D phase2MathBlackboardTexture;
    private Sprite cursedBaldiSprite;
    private Sprite helpMeExitSprite;
    private Image dangerFlash;
    private float pulse;
    private bool horrorActive;

    public static bool HorrorActive
    {
        get { return instance != null && instance.horrorActive; }
    }

    public static string GetHowToPlayTextForCurrentPhase()
    {
        return CursedPhaseManager.IsPhase2 && !CursedPhaseManager.IsPhase3 && !CursedPhaseManager.IsPhase4
            ? Phase2HowToPlayText
            : NormalHowToPlayText;
    }

    public static void ActivateHorror()
    {
        if (instance != null) instance.ActivateHorrorInternal();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (instance != null) return;
        GameObject root = new GameObject("Cursed Horror Mod");
        instance = root.AddComponent<CursedHorrorBootstrap>();
        DontDestroyOnLoad(root);
    }

    private void Awake()
    {
        cursedBaldiTexture = Resources.Load<Texture2D>("CursedMod/CursedBaldi");
        cursedThinkPadTexture = Resources.Load<Texture2D>("CursedMod/CursedThinkPad");
        helpMeExitTexture = Resources.Load<Texture2D>("CursedMod/HelpMeExitSign");
        phase2SchoolRulesTexture = Resources.Load<Texture2D>("CursedMod/SchoolRulesPosterPhase2");
        phase2MathBlackboardTexture = Resources.Load<Texture2D>("CursedMod/MathBlackboardPhase2");
        if (cursedBaldiTexture != null)
        {
            // Account for the different transparent bottom padding so the
            // cursed feet use the exact same ground line as original Baldi.
            cursedBaldiSprite = Sprite.Create(cursedBaldiTexture, new Rect(0f, 0f, cursedBaldiTexture.width, cursedBaldiTexture.height), new Vector2(0.5f, 0.5344603f), 256f);
            cursedBaldiSprite.name = "Cursed Baldi Runtime Sprite";
        }
        if (helpMeExitTexture != null)
        {
            // Match the original ExitSign.png import: centered pivot and 100 pixels per unit.
            helpMeExitSprite = Sprite.Create(helpMeExitTexture, new Rect(0f, 0f, helpMeExitTexture.width, helpMeExitTexture.height), new Vector2(0.5f, 0.5f), 100f);
            helpMeExitSprite.name = "Phase 2 Help Me Exit Sign";
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (CursedPhaseManager.IsPhase4)
        {
            horrorActive = false;
            RemoveDangerOverlay();
            CursedPhase4Screen.Show();
            return;
        }

        if (CursedPhaseManager.IsPhase3)
        {
            horrorActive = false;
            RemoveDangerOverlay();
            CursedPhase3Screen.Show();
            return;
        }

        if (scene.name == "MainMenu" || scene.name == "Warning")
        {
            horrorActive = false;
            RemoveDangerOverlay();
        }
        ApplyPhase2MusicSpeed(scene);
        StartCoroutine(PatchSceneAfterActivation(scene));
    }

    private IEnumerator PatchSceneAfterActivation(Scene scene)
    {
        yield return null;

        // Repeat after one frame as a safety net for objects instantiated by Start().
        ApplyPhase2MusicSpeed(scene);
        PatchPhase2HowToPlayText(scene);

        bool gameplay = FindFirstObjectByType<PlayerScript>() != null || FindFirstObjectByType<PlayerMovement>() != null;
        if (gameplay)
        {
            CursedFinalExitSequence.EnsureInstalled();
            if (CursedPhaseManager.IsPhase2)
            {
                PatchPhase2SceneVisuals();
            }
            if (horrorActive)
            {
                PatchBaldiVisuals();
                PatchThinkPad();
                InstallAtmosphere();
                InstallDangerOverlay();
            }
        }
        if (scene.name == "GameOver" && CursedPhaseManager.IsPhase2)
        {
            InstallGameOverImage();
        }
    }

    private void PatchPhase2SceneVisuals()
    {
        HidePhase2ExteriorGarden();
        PatchExitSigns();
        PatchPhase2SchoolRulesPosters();
        PatchPhase2MathBlackboards();
    }

    private static void HidePhase2ExteriorGarden()
    {
        if (!CursedPhaseManager.IsPhase2) return;

        Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer.gameObject.scene.name != "School") continue;
            if (!HasAncestorNamed(renderer.transform, "Entrances")) continue;

            string objectName = renderer.gameObject.name;
            if (objectName != "Grass" && objectName != "Fence" && objectName != "PlantSprite") continue;

            // Keep ground/boundary colliders and the entrance's exit triggers active.
            renderer.enabled = false;
        }
    }

    private static void PatchPhase2HowToPlayText(Scene scene)
    {
        if (scene.name != "MainMenu" ||
            !CursedPhaseManager.IsPhase2 ||
            CursedPhaseManager.IsPhase3 ||
            CursedPhaseManager.IsPhase4)
        {
            return;
        }

        TMP_FontAsset storyFont = null;
        TextMeshProUGUI[] existingTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        for (int i = 0; i < existingTexts.Length; i++)
        {
            TextMeshProUGUI candidate = existingTexts[i];
            if (!candidate.gameObject.scene.IsValid() || candidate.gameObject.scene != scene) continue;
            if (!string.IsNullOrEmpty(candidate.text) && candidate.text.StartsWith("Story:") && candidate.font != null)
            {
                storyFont = candidate.font;
                break;
            }
        }

        int patched = 0;
        RawImage[] images = Resources.FindObjectsOfTypeAll<RawImage>();
        for (int i = 0; i < images.Length; i++)
        {
            RawImage storyImage = images[i];
            if (!storyImage.gameObject.scene.IsValid() || storyImage.gameObject.scene != scene) continue;
            if (storyImage.gameObject.name != "StoryImage" || !HasAncestorNamed(storyImage.transform, "Story")) continue;
            if (storyImage.transform.parent.Find("Phase 2 Story Panel") != null) continue;

            GameObject panel = new GameObject("Phase 2 Story Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(storyImage.transform.parent, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            CopyRect(storyImage.rectTransform, panelRect);
            panel.transform.SetSiblingIndex(storyImage.transform.GetSiblingIndex() + 1);
            Image background = panel.GetComponent<Image>();
            background.color = Color.white;
            background.raycastTarget = false;

            GameObject textObject = new GameObject("Phase 2 Story Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panel.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18f, 54f);
            textRect.offsetMax = new Vector2(-18f, -14f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            if (storyFont != null) text.font = storyFont;
            text.text = GetHowToPlayTextForCurrentPhase();
            text.richText = true;
            text.color = Color.black;
            text.fontSize = 18f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = 18f;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            patched++;
        }

        if (patched == 0)
        {
            Debug.LogWarning("The visible Phase 2 How to Play StoryText was not found in MainMenu.");
            return;
        }

        Debug.Log("Phase 2 cursed Story applied to the visible How to Play panel: " + patched);
    }

    private static void CopyRect(RectTransform source, RectTransform destination)
    {
        destination.anchorMin = source.anchorMin;
        destination.anchorMax = source.anchorMax;
        destination.pivot = source.pivot;
        destination.anchoredPosition = source.anchoredPosition;
        destination.sizeDelta = source.sizeDelta;
        destination.localRotation = source.localRotation;
        destination.localScale = source.localScale;
    }

    private static bool HasAncestorNamed(Transform child, string ancestorName)
    {
        Transform current = child.parent;
        while (current != null)
        {
            if (current.gameObject.name == ancestorName) return true;
            current = current.parent;
        }
        return false;
    }

    private void PatchExitSigns()
    {
        if (helpMeExitSprite == null)
        {
            Debug.LogError("Phase 2 HELP ME exit sign texture could not be loaded.");
            return;
        }

        int patched = 0;
        SpriteRenderer[] renderers = Resources.FindObjectsOfTypeAll<SpriteRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (!renderer.gameObject.scene.IsValid()) continue;
            if (renderer.gameObject.name != "ExitSignSprite") continue;
            renderer.sprite = helpMeExitSprite;
            patched++;
        }
        Debug.Log("Phase 2 HELP ME exit signs applied: " + patched);
    }

    private void PatchPhase2SchoolRulesPosters()
    {
        if (phase2SchoolRulesTexture == null)
        {
            Debug.LogError("Phase 2 school rules poster texture could not be loaded.");
            return;
        }

        int patched = 0;
        Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (!renderer.gameObject.scene.IsValid()) continue;

            Material[] sharedMaterials = renderer.sharedMaterials;
            bool containsSchoolRulesPoster = false;
            for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
            {
                Material material = sharedMaterials[materialIndex];
                if (material == null) continue;
                if (material.name.StartsWith("SchoolRulesPoster") ||
                    (material.mainTexture != null && material.mainTexture.name == "SchoolRulesPoster"))
                {
                    containsSchoolRulesPoster = true;
                    break;
                }
            }
            if (!containsSchoolRulesPoster) continue;

            Material[] materials = renderer.materials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null) continue;
                if (!material.name.StartsWith("SchoolRulesPoster") &&
                    (material.mainTexture == null || material.mainTexture.name != "SchoolRulesPoster"))
                {
                    continue;
                }

                material.mainTexture = phase2SchoolRulesTexture;
                patched++;
            }
            renderer.materials = materials;
        }

        Debug.Log("Phase 2 school rules posters applied: " + patched);
    }

    private void PatchPhase2MathBlackboards()
    {
        if (phase2MathBlackboardTexture == null)
        {
            Debug.LogError("Phase 2 Math blackboard texture could not be loaded.");
            return;
        }

        int patched = 0;
        Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>();
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];
            if (!renderer.gameObject.scene.IsValid()) continue;

            Material[] sharedMaterials = renderer.sharedMaterials;
            bool containsMathBlackboard = false;
            for (int materialIndex = 0; materialIndex < sharedMaterials.Length; materialIndex++)
            {
                if (!IsMathBlackboardMaterial(sharedMaterials[materialIndex])) continue;
                containsMathBlackboard = true;
                break;
            }
            if (!containsMathBlackboard) continue;

            Material[] materials = renderer.materials;
            int materialCount = Mathf.Min(sharedMaterials.Length, materials.Length);
            for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
            {
                if (!IsMathBlackboardMaterial(sharedMaterials[materialIndex])) continue;
                materials[materialIndex].mainTexture = phase2MathBlackboardTexture;
                patched++;
            }
            renderer.materials = materials;
        }

        Debug.Log("Phase 2 cursed Math blackboards applied: " + patched);
    }

    private static bool IsMathBlackboardMaterial(Material material)
    {
        return material != null &&
               material.name == "Math" &&
               material.mainTexture != null &&
               material.mainTexture.name == "Math";
    }

    private static void ApplyPhase2MusicSpeed(Scene scene)
    {
        if (!CursedPhaseManager.IsPhase2) return;

        if (scene.name == "MainMenu")
        {
            AudioSource[] menuSources = Resources.FindObjectsOfTypeAll<AudioSource>();
            for (int i = 0; i < menuSources.Length; i++)
            {
                AudioSource source = menuSources[i];
                if (!source.gameObject.scene.IsValid() || source.gameObject.scene != scene) continue;
                if (source.clip != null && source.clip.name == "mus_Intro")
                {
                    source.pitch = 0.5f;
                }
            }
        }

        GameControllerScript controller = FindFirstObjectByType<GameControllerScript>();
        if (controller != null)
        {
            // schoolMusic is heard when gameplay begins; learnMusic is the
            // You Can Think Pad background track.
            if (controller.schoolMusic != null) controller.schoolMusic.pitch = 0.5f;
            if (controller.learnMusic != null) controller.learnMusic.pitch = 0.5f;
        }
    }

    private void ActivateHorrorInternal()
    {
        if (horrorActive) return;
        horrorActive = true;
        PatchBaldiVisuals();
        PatchThinkPad();
        InstallAtmosphere();
        InstallDangerOverlay();
    }

    private void PatchBaldiVisuals()
    {
        if (cursedBaldiSprite == null) return;
        SpriteRenderer[] renderers = Resources.FindObjectsOfTypeAll<SpriteRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (!renderer.gameObject.scene.IsValid()) continue;
            if (!ContainsBaldiName(renderer.transform)) continue;
            if (renderer.GetComponent<CursedBaldiVisual>() != null) continue;

            CursedBaldiVisual visual = renderer.gameObject.AddComponent<CursedBaldiVisual>();
            visual.Apply(renderer, cursedBaldiSprite);
        }

        BaldiScript[] baldis = Resources.FindObjectsOfTypeAll<BaldiScript>();
        for (int i = 0; i < baldis.Length; i++)
        {
            if (!baldis[i].gameObject.scene.IsValid()) continue;
            baldis[i].speed *= 1.12f;
            baldis[i].baldiSpeedScale *= 1.08f;
            baldis[i].baseTime = Mathf.Min(baldis[i].baseTime, 2.65f);
        }
    }

    private static bool ContainsBaldiName(Transform target)
    {
        Transform current = target;
        while (current != null)
        {
            if (current.name.ToLowerInvariant().Contains("baldi")) return true;
            current = current.parent;
        }
        return false;
    }

    private void PatchThinkPad()
    {
        MathGameScript[] mathGames = Resources.FindObjectsOfTypeAll<MathGameScript>();
        for (int i = 0; i < mathGames.Length; i++)
        {
            MathGameScript math = mathGames[i];
            if (!math.gameObject.scene.IsValid()) continue;
            CursedThinkPadInstaller.ApplyTo(math);
        }
    }

    private void InstallAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.010f;
        RenderSettings.fogColor = new Color(0.075f, 0.018f, 0.018f, 1f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.30f, 0.17f, 0.16f, 1f);

        Camera camera = Camera.main;
        if (camera != null && camera.GetComponent<CursedFlickerLight>() == null)
        {
            Light light = camera.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 18f;
            light.intensity = 1.05f;
            light.color = new Color(1f, 0.62f, 0.52f);
            camera.gameObject.AddComponent<CursedFlickerLight>().lightSource = light;
        }
    }

    private void InstallDangerOverlay()
    {
        if (dangerFlash != null) Destroy(dangerFlash.gameObject.transform.root.gameObject);
        GameObject canvasObject = new GameObject("Cursed Danger Canvas", typeof(Canvas), typeof(CanvasScaler));
        DontDestroyOnLoad(canvasObject);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 31000;

        GameObject flashObject = new GameObject("Danger Pulse", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        flashObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = flashObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        dangerFlash = flashObject.GetComponent<Image>();
        dangerFlash.color = new Color(0.35f, 0f, 0f, 0f);
        dangerFlash.raycastTarget = false;
    }

    private void RemoveDangerOverlay()
    {
        if (dangerFlash == null) return;
        GameObject root = dangerFlash.transform.root.gameObject;
        dangerFlash = null;
        Destroy(root);
    }

    private void Update()
    {
        if (dangerFlash == null) return;
        BaldiScript baldi = FindFirstObjectByType<BaldiScript>();
        PlayerScript player = FindFirstObjectByType<PlayerScript>();
        float targetAlpha = 0f;
        if (baldi != null && player != null && baldi.gameObject.activeInHierarchy)
        {
            float distance = Vector3.Distance(baldi.transform.position, player.transform.position);
            float danger = 1f - Mathf.Clamp01((distance - 2f) / 24f);
            pulse += Time.unscaledDeltaTime * Mathf.Lerp(2f, 8f, danger);
            targetAlpha = danger * (0.055f + Mathf.Abs(Mathf.Sin(pulse)) * 0.12f);
        }
        Color color = dangerFlash.color;
        color.a = Mathf.Lerp(color.a, targetAlpha, Time.unscaledDeltaTime * 4f);
        dangerFlash.color = color;
    }

    private void InstallGameOverImage()
    {
        if (cursedBaldiTexture == null) return;
        GameObject canvasObject = new GameObject("Cursed Baldi Jumpscare", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;

        GameObject background = new GameObject("Black", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        background.transform.SetParent(canvasObject.transform, false);
        Stretch(background.GetComponent<RectTransform>());
        background.GetComponent<Image>().color = Color.black;

        GameObject face = new GameObject("Cursed Baldi", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        face.transform.SetParent(canvasObject.transform, false);
        RectTransform faceRect = face.GetComponent<RectTransform>();
        faceRect.anchorMin = new Vector2(0.16f, -0.1f);
        faceRect.anchorMax = new Vector2(0.84f, 1.1f);
        faceRect.offsetMin = Vector2.zero;
        faceRect.offsetMax = Vector2.zero;
        RawImage raw = face.GetComponent<RawImage>();
        raw.texture = cursedBaldiTexture;
        raw.raycastTarget = false;
        face.AddComponent<CursedJumpscarePulse>();
        StartCoroutine(QuitAfterPhase2Jumpscare());
    }

    private IEnumerator QuitAfterPhase2Jumpscare()
    {
        // Keep the jumpscare visible before closing the Phase 2 game session.
        yield return new WaitForSecondsRealtime(3f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

public static class CursedThinkPadInstaller
{
    public static void ApplyTo(MathGameScript math)
    {
        if (math == null) return;
        // Keep Phase 2 notebooks normal until runtime horror activation: either
        // a wrong first-notebook answer or the second notebook's final answer.
        if (!CursedHorrorBootstrap.HorrorActive) return;
        Texture2D texture = Resources.Load<Texture2D>("CursedMod/CursedThinkPad");
        if (texture == null) return;
        GameObject root = math.mathGame != null ? math.mathGame : math.gameObject;
        if (root.transform.Find("Cursed Think Pad Skin") != null) return;

        // The cursed artwork already contains its own ENTER ANSWER label.
        // Remove only the stock placeholder layer while preserving the live
        // TMP input text that displays the player's numeric answer.
        if (math.playerAnswer != null && math.playerAnswer.placeholder != null)
        {
            math.playerAnswer.placeholder.gameObject.SetActive(false);
        }

        // The TMP input field itself also owns the stock white background.
        // Disable only that graphic in horror mode; keep the input field and
        // its live answer text active so entered numbers remain visible.
        if (math.playerAnswer != null)
        {
            Image stockAnswerBackground = math.playerAnswer.GetComponent<Image>();
            if (stockAnswerBackground != null) stockAnswerBackground.enabled = false;
        }

        // The stock YCTP image is opaque around its transparent display cutouts.
        // Hide only that background graphic; its keypad children remain active.
        Transform stockThinkPad = root.transform.Find("YCTP");
        if (stockThinkPad != null)
        {
            RawImage stockBackground = stockThinkPad.GetComponent<RawImage>();
            if (stockBackground != null) stockBackground.enabled = false;
            Transform stockButtons = stockThinkPad.Find("Buttons");
            if (stockButtons != null) stockButtons.gameObject.SetActive(false);
        }

        GameObject skin = new GameObject("Cursed Think Pad Skin", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        skin.transform.SetParent(root.transform, false);
        // Insert the cursed background immediately before the live result layer.
        // Questions, result marks, answer text and buttons then render above it.
        int foregroundIndex = 1;
        if (math.results != null && math.results.Length > 0 && math.results[0] != null)
        {
            Transform resultLayer = math.results[0].transform.parent;
            if (resultLayer != null && resultLayer.parent == root.transform)
            {
                foregroundIndex = resultLayer.GetSiblingIndex();
            }
        }
        skin.transform.SetSiblingIndex(Mathf.Clamp(foregroundIndex, 0, root.transform.childCount - 1));
        RectTransform rect = skin.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        RawImage image = skin.GetComponent<RawImage>();
        image.texture = texture;
        image.color = new Color(0.82f, 0.82f, 0.82f, 1f);
        image.raycastTarget = false;

        AlignResultMarks(math, root.transform);

        // Build fresh hit regions from the exact normalized pixel bounds in the
        // 1536x1024 cursed artwork. These scale together with the skin on every
        // aspect ratio and bypass the differently spaced stock keypad entirely.
        GameObject controls = new GameObject("Cursed Think Pad Controls", typeof(RectTransform));
        controls.transform.SetParent(root.transform, false);
        RectTransform controlsRect = controls.GetComponent<RectTransform>();
        controlsRect.anchorMin = Vector2.zero;
        controlsRect.anchorMax = Vector2.one;
        controlsRect.offsetMin = Vector2.zero;
        controlsRect.offsetMax = Vector2.zero;
        controls.transform.SetAsLastSibling();

        CreateKey(controls.transform, "7", new Vector2(0.7507f, 0.7861f), new Vector2(0.8125f, 0.8896f), math, 7, false);
        CreateKey(controls.transform, "8", new Vector2(0.8223f, 0.7852f), new Vector2(0.8841f, 0.8877f), math, 8, false);
        CreateKey(controls.transform, "9", new Vector2(0.8913f, 0.7871f), new Vector2(0.9518f, 0.8867f), math, 9, false);
        CreateKey(controls.transform, "4", new Vector2(0.7500f, 0.6689f), new Vector2(0.8125f, 0.7725f), math, 4, false);
        CreateKey(controls.transform, "5", new Vector2(0.8216f, 0.6699f), new Vector2(0.8828f, 0.7705f), math, 5, false);
        CreateKey(controls.transform, "6", new Vector2(0.8919f, 0.6709f), new Vector2(0.9518f, 0.7705f), math, 6, false);
        CreateKey(controls.transform, "1", new Vector2(0.7520f, 0.5537f), new Vector2(0.8118f, 0.6543f), math, 1, false);
        CreateKey(controls.transform, "2", new Vector2(0.8216f, 0.5537f), new Vector2(0.8835f, 0.6543f), math, 2, false);
        CreateKey(controls.transform, "3", new Vector2(0.8906f, 0.5527f), new Vector2(0.9518f, 0.6543f), math, 3, false);
        CreateKey(controls.transform, "0", new Vector2(0.7513f, 0.4346f), new Vector2(0.8828f, 0.5400f), math, 0, false);
        CreateKey(controls.transform, "Minus", new Vector2(0.8919f, 0.4307f), new Vector2(0.9518f, 0.5381f), math, -1, false);
        CreateKey(controls.transform, "OK", new Vector2(0.7565f, 0.1152f), new Vector2(0.9303f, 0.3838f), math, 0, true);
    }

    private static void AlignResultMarks(MathGameScript math, Transform root)
    {
        if (math.results == null || math.results.Length == 0) return;

        // The stock result marks were positioned for the original Think Pad.
        // Reparent them to a full-screen layer and anchor their centres to the
        // three green status windows painted into the 1536x1024 cursed skin.
        Vector2[] markAnchors =
        {
            new Vector2(0.1123f, 0.8369f),
            new Vector2(0.1123f, 0.6802f),
            new Vector2(0.1123f, 0.5269f)
        };

        GameObject layer = new GameObject("Cursed Result Marks", typeof(RectTransform));
        layer.transform.SetParent(root, false);
        RectTransform layerRect = layer.GetComponent<RectTransform>();
        layerRect.anchorMin = Vector2.zero;
        layerRect.anchorMax = Vector2.one;
        layerRect.offsetMin = Vector2.zero;
        layerRect.offsetMax = Vector2.zero;
        layer.transform.SetAsLastSibling();

        int count = Mathf.Min(math.results.Length, markAnchors.Length);
        for (int i = 0; i < count; i++)
        {
            RawImage result = math.results[i];
            if (result == null) continue;

            RectTransform resultRect = result.rectTransform;
            resultRect.SetParent(layerRect, false);
            resultRect.anchorMin = markAnchors[i];
            resultRect.anchorMax = markAnchors[i];
            resultRect.pivot = new Vector2(0.5f, 0.5f);
            resultRect.anchoredPosition = Vector2.zero;
            resultRect.sizeDelta = new Vector2(53f, 53f);
            resultRect.localRotation = Quaternion.identity;
            resultRect.localScale = Vector3.one;
            result.raycastTarget = false;
        }
    }

    private static void CreateKey(Transform parent, string keyName, Vector2 anchorMin, Vector2 anchorMax, MathGameScript math, int value, bool submit)
    {
        GameObject key = new GameObject("Cursed Key " + keyName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        key.transform.SetParent(parent, false);
        RectTransform rect = key.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image hitArea = key.GetComponent<Image>();
        hitArea.color = new Color(1f, 1f, 1f, 0f);
        hitArea.raycastTarget = true;

        Button button = key.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        if (submit)
        {
            button.onClick.AddListener(delegate { math.OKButton(); });
        }
        else
        {
            button.onClick.AddListener(delegate { math.ButtonPress(value); });
        }
    }
}

public static class CursedBaldiSizing
{
    // Use the neutral Slap0000 body, excluding the ruler and extended arms.
    // Torso alpha spans: normal x=20..62 at y=120; cursed x=386..638 at y=650.
    public const float ReferenceBodyWidthPixels = 43f;
    public const float ReferenceHeightPixels = 232f;
    public const float CursedBodyWidthUnits = 253f / 256f;
    public const float CanonicalVisibleWorldWidth =
        ReferenceBodyWidthPixels / 100f * 3.2f / CursedBodyWidthUnits * CursedVisibleWidthUnits;
    public const float CanonicalVisibleWorldHeight = ReferenceHeightPixels / 100f * 3.2f;

    // CursedBaldi.png has a 434x1460 opaque region at 256 PPU, with 28
    // transparent pixels below its feet.
    public const float CursedVisibleWidthUnits = 434f / 256f;
    public const float CursedVisibleHeightUnits = 1460f / 256f;
    public const float CursedBottomPaddingUnits = 28f / 256f;
    public const float CanonicalWorldScaleX = CanonicalVisibleWorldWidth / CursedVisibleWidthUnits;
    public const float CanonicalWorldScaleY = CanonicalVisibleWorldHeight / CursedVisibleHeightUnits;

    // Use the imported sprite dimensions: Unity can resize NPOT source images
    // or downsample them for a platform. Source-pixel constants alone are unsafe.
    public static float BodyWidthUnits(Sprite sprite)
    {
        return sprite.rect.width / sprite.pixelsPerUnit * (253f / 1024f);
    }

    public static float VisibleHeightUnits(Sprite sprite)
    {
        return sprite.rect.height / sprite.pixelsPerUnit * (1460f / 1536f);
    }

    public static float BottomPaddingUnits(Sprite sprite)
    {
        return sprite.rect.height / sprite.pixelsPerUnit * (28f / 1536f);
    }

    public static void GetSlapOpaqueMetrics(Sprite original, out float widthPixels, out float heightPixels, out float bottomPaddingPixels)
    {
        widthPixels = 71f;
        heightPixels = 251f;
        bottomPaddingPixels = 2f;
        if (original == null) return;

        switch (original.name)
        {
            case "Baldi_Slap0000": widthPixels = 102f; heightPixels = 232f; break;
            case "Baldi_Slap0006": widthPixels = 94f; heightPixels = 232f; break;
            case "Baldi_Slap0012": widthPixels = 75f; heightPixels = 232f; break;
            case "Baldi_Slap0018": widthPixels = 71f; heightPixels = 243f; break;
            case "Baldi_Slap0024": widthPixels = 71f; heightPixels = 251f; break;
        }
    }
}

public class CursedBaldiVisual : MonoBehaviour
{
    private SpriteRenderer target;
    private Sprite cursedSprite;

    public void Apply(SpriteRenderer renderer, Sprite sprite)
    {
        target = renderer;
        cursedSprite = sprite;
        Sprite original = target.sprite;
        Vector3 originalLossyScale = transform.lossyScale;
        float originalScaleX = Mathf.Abs(originalLossyScale.x);
        float originalScaleY = Mathf.Abs(originalLossyScale.y);

        float visibleWidthPixels;
        float visibleHeightPixels;
        float bottomPaddingPixels;
        CursedBaldiSizing.GetSlapOpaqueMetrics(original, out visibleWidthPixels, out visibleHeightPixels, out bottomPaddingPixels);

        float originalPixelsPerUnit = original != null ? original.pixelsPerUnit : 100f;
        // Animation frames change the ruler/arm bounds. Size the cursed body
        // against a fixed neutral pose instead of whichever frame is playing.
        float originalVisibleWorldWidth = CursedBaldiSizing.ReferenceBodyWidthPixels / originalPixelsPerUnit * originalScaleX;
        float originalVisibleWorldHeight = CursedBaldiSizing.ReferenceHeightPixels / originalPixelsPerUnit * originalScaleY;
        float desiredWorldScaleX = originalVisibleWorldWidth / CursedBaldiSizing.BodyWidthUnits(cursedSprite);
        float desiredWorldScaleY = originalVisibleWorldHeight / CursedBaldiSizing.VisibleHeightUnits(cursedSprite);

        Debug.Log("Cursed Baldi sizing: imported sprite=" + cursedSprite.rect.width + "x" +
            cursedSprite.rect.height + ", target body width=" + originalVisibleWorldWidth +
            ", target height=" + originalVisibleWorldHeight);

        float originalFootWorldY = transform.position.y;
        if (original != null)
        {
            float originalPivotFraction = original.pivot.y / original.rect.height;
            float originalFullWorldHeight = original.rect.height / originalPixelsPerUnit * originalScaleY;
            float originalBottomPaddingWorld = bottomPaddingPixels / originalPixelsPerUnit * originalScaleY;
            originalFootWorldY = transform.position.y - originalPivotFraction * originalFullWorldHeight + originalBottomPaddingWorld;
        }

        Vector3 localScale = transform.localScale;
        if (originalScaleX > 0.0001f) localScale.x *= desiredWorldScaleX / originalScaleX;
        if (originalScaleY > 0.0001f) localScale.y *= desiredWorldScaleY / originalScaleY;
        transform.localScale = localScale;

        // Keep the first opaque foot pixel at exactly the same world height as
        // the normal sprite while changing width and height independently.
        float cursedFootOffset = (cursedSprite.bounds.min.y + CursedBaldiSizing.BottomPaddingUnits(cursedSprite)) * desiredWorldScaleY;
        Vector3 desiredWorldPosition = transform.position;
        desiredWorldPosition.y = originalFootWorldY - cursedFootOffset;
        if (transform.parent != null)
        {
            transform.localPosition = transform.parent.InverseTransformPoint(desiredWorldPosition);
        }
        else
        {
            transform.position = desiredWorldPosition;
        }

        Animator animator = GetComponent<Animator>();
        if (animator != null) animator.enabled = false;
        target.sprite = cursedSprite;
        target.color = Color.white;
    }

    private void LateUpdate()
    {
        if (target != null && cursedSprite != null) target.sprite = cursedSprite;
    }
}

public class CursedFlickerLight : MonoBehaviour
{
    public Light lightSource;
    private float baseIntensity;
    private float nextDrop;

    private void Start()
    {
        if (lightSource != null) baseIntensity = lightSource.intensity;
    }

    private void Update()
    {
        if (lightSource == null) return;
        if (Time.unscaledTime >= nextDrop)
        {
            nextDrop = Time.unscaledTime + Random.Range(0.035f, 0.22f);
            lightSource.intensity = Random.value < 0.12f ? baseIntensity * Random.Range(0.05f, 0.3f) : baseIntensity * Random.Range(0.82f, 1.12f);
        }
    }
}

public class CursedJumpscarePulse : MonoBehaviour
{
    private RectTransform rect;
    private float time;

    private void Start() { rect = GetComponent<RectTransform>(); }
    private void Update()
    {
        time += Time.unscaledDeltaTime;
        if (rect != null)
        {
            float scale = 1f + Mathf.Sin(time * 28f) * 0.025f + Mathf.Clamp01(time) * 0.28f;
            rect.localScale = new Vector3(scale, scale, 1f);
            rect.anchoredPosition = Random.insideUnitCircle * Mathf.Lerp(2f, 16f, Mathf.Clamp01(time));
        }
    }
}

/// <summary>
/// Briefly transforms the visible Think Pad portrait into Cursed Baldi after
/// the first Phase 2 wrong answer. The overlay belongs to the current MathGame
/// instance, so it is destroyed when that notebook closes. Later notebooks
/// keep using MathGameScript's existing spoop-mode portrait hiding behavior.
/// </summary>
public sealed class CursedBaldiPortraitTransition : MonoBehaviour
{
    private const float Duration = 2.4f;
    private static readonly Rect CursedPortraitCrop = new Rect(0.19f, 0.58f, 0.62f, 0.42f);

    private Image normalPortrait;
    private RawImage cursedPortrait;
    private RectTransform cursedRect;
    private Color normalColor;
    private Coroutine transitionRoutine;

    public static void Play(Animator baldiFeed)
    {
        if (baldiFeed == null || !baldiFeed.gameObject.activeInHierarchy) return;

        CursedBaldiPortraitTransition transition = baldiFeed.GetComponent<CursedBaldiPortraitTransition>();
        if (transition == null)
        {
            transition = baldiFeed.gameObject.AddComponent<CursedBaldiPortraitTransition>();
        }
        transition.Begin();
    }

    private void Begin()
    {
        if (transitionRoutine != null || cursedPortrait != null) return;

        normalPortrait = GetComponent<Image>();
        Texture2D cursedTexture = Resources.Load<Texture2D>("CursedMod/CursedBaldi");
        if (normalPortrait == null || cursedTexture == null) return;

        normalColor = normalPortrait.color;

        GameObject portraitObject = new GameObject(
            "Cursed Baldi Portrait Transition",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(RawImage));
        portraitObject.transform.SetParent(transform, false);
        portraitObject.transform.SetAsLastSibling();

        cursedRect = portraitObject.GetComponent<RectTransform>();
        cursedRect.anchorMin = Vector2.zero;
        cursedRect.anchorMax = Vector2.one;
        cursedRect.offsetMin = Vector2.zero;
        cursedRect.offsetMax = Vector2.zero;
        cursedRect.pivot = new Vector2(0.5f, 0.5f);

        cursedPortrait = portraitObject.GetComponent<RawImage>();
        cursedPortrait.texture = cursedTexture;
        cursedPortrait.uvRect = CursedPortraitCrop;
        cursedPortrait.raycastTarget = false;
        cursedPortrait.color = new Color(1f, 1f, 1f, 0f);

        transitionRoutine = StartCoroutine(TransformPortrait());
    }

    private IEnumerator TransformPortrait()
    {
        float elapsed = 0f;
        while (elapsed < Duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / Duration);
            float reveal = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.08f, 0.88f, progress));

            // Early frames deliberately drop out for a corrupted, unstable
            // transition. The final portion is stable and fully readable.
            float flicker = 1f;
            if (progress < 0.82f && Random.value < Mathf.Lerp(0.32f, 0.08f, progress))
            {
                flicker = Random.Range(0.18f, 0.68f);
            }

            float redTint = Mathf.Lerp(1f, 0.78f, progress);
            float otherTint = Mathf.Lerp(1f, 0.52f, progress);
            cursedPortrait.color = new Color(redTint, otherTint, otherTint, reveal * flicker);

            float shake = Mathf.Lerp(1.5f, 7f, reveal) * (1f - Mathf.SmoothStep(0.78f, 1f, progress));
            cursedRect.anchoredPosition = Random.insideUnitCircle * shake;
            float pulse = 1.08f - 0.08f * reveal + Mathf.Sin(elapsed * 31f) * 0.018f * (1f - progress);
            cursedRect.localScale = new Vector3(pulse, pulse, 1f);

            float normalBrightness = Mathf.Lerp(1f, 0.28f, reveal);
            normalPortrait.color = new Color(
                normalColor.r * normalBrightness,
                normalColor.g * normalBrightness,
                normalColor.b * normalBrightness,
                normalColor.a);

            yield return null;
        }

        cursedRect.anchoredPosition = Vector2.zero;
        cursedRect.localScale = Vector3.one;
        cursedPortrait.color = Color.white;
        normalPortrait.color = normalColor;
        transitionRoutine = null;
    }

    private void OnDestroy()
    {
        if (normalPortrait != null) normalPortrait.color = normalColor;
    }
}
