using UnityEngine;

/// <summary>Head overlay and spatial sound share Baldi's gameplay-time blindness timer.</summary>
public sealed class ShellEffect : MonoBehaviour
{
    private BaldiScript baldi;
    private GameObject cover;
    private AudioSource sound;
    private bool audioPaused;

    public void Begin(BaldiScript target, SpriteRenderer body, Sprite sprite, AudioClip clip)
    {
        Cleanup();
        baldi = target;
        enabled = true;
        cover = new GameObject("Shell Head Cover");
        cover.transform.SetParent(body.transform, false);
        Sprite bodySprite = body.sprite;
        // Normalized source coordinates remain correct after Unity downscales CursedBaldi.png.
        float width = bodySprite.rect.width / bodySprite.pixelsPerUnit;
        float height = bodySprite.rect.height / bodySprite.pixelsPerUnit;
        cover.transform.localPosition = new Vector3(
            (514f / 1024f - bodySprite.pivot.x / bodySprite.rect.width) * width,
            (1f - 165f / 1536f - bodySprite.pivot.y / bodySprite.rect.height) * height,
            -0.04f);
        cover.transform.localScale = new Vector3(
            width * (420f / 1024f) / sprite.bounds.size.x,
            height * (540f / 1536f) / sprite.bounds.size.y, 1f);
        SpriteRenderer renderer = cover.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = body.sharedMaterial;
        renderer.sortingLayerID = body.sortingLayerID;
        renderer.sortingOrder = body.sortingOrder + 1;

        sound = cover.AddComponent<AudioSource>();
        sound.playOnAwake = false;
        sound.clip = clip;
        sound.loop = true;
        sound.volume = 1f;
        sound.spatialBlend = 1f;
        sound.rolloffMode = AudioRolloffMode.Linear;
        sound.minDistance = 20f;
        sound.maxDistance = 250f;
        sound.dopplerLevel = 0f;
        sound.priority = 32;
        // Audible to the player; do not call Hear() with Baldi's own head position.
        baldi.ApplyShellBlindness(ShellItem.Duration);
        sound.Play();
    }

    private void Update()
    {
        if (baldi == null || !baldi.isActiveAndEnabled || !baldi.IsShellBlind || cover == null)
        {
            Cleanup();
            enabled = false;
            return;
        }
        if (Time.timeScale <= 0f && !audioPaused)
        {
            sound.Pause();
            audioPaused = true;
        }
        else if (Time.timeScale > 0f && audioPaused)
        {
            sound.UnPause();
            audioPaused = false;
        }
    }

    private void OnDisable() { Cleanup(); }

    private void Cleanup()
    {
        if (sound != null) sound.Stop();
        if (cover != null)
        {
            cover.SetActive(false);
            Destroy(cover);
        }
        if (baldi != null) baldi.ClearShellBlindness();
        sound = null;
        cover = null;
        audioPaused = false;
    }
}
