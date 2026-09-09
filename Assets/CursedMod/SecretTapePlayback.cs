using UnityEngine;

// Entering the secret room plays the loaded cassette once per scene visit.
[RequireComponent(typeof(AudioSource))]
public sealed class SecretTapePlayback : MonoBehaviour
{
    private AudioSource source;
    private bool played;
    private bool paused;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
    }

    private void OnTriggerEnter(Collider other) { TryPlay(other); }
    private void OnTriggerStay(Collider other) { TryPlay(other); }

    private void TryPlay(Collider other)
    {
        if (played || Time.timeScale <= 0f) return;
        if (other.GetComponentInParent<PlayerScript>() == null) return;
        if (source.clip == null)
        {
            Debug.LogError("Secret tape recording is missing.", this);
            enabled = false;
            return;
        }
        played = true;
        source.Play();
    }

    private void Update()
    {
        if (!played) return;
        if (Time.timeScale <= 0f && source.isPlaying)
        {
            source.Pause();
            paused = true;
        }
        else if (Time.timeScale > 0f && paused)
        {
            source.UnPause();
            paused = false;
        }
    }
}
