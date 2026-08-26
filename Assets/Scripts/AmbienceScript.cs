using UnityEngine;

public class AmbienceScript : MonoBehaviour
{
	public void PlayAudio()
	{
		int num = Mathf.RoundToInt(Random.Range(0f, 49f)); 
		if (!audioDevice.isPlaying & num == 0) // If it is not currently playing an audio device, and num is equal to 0 (1/50 chance)
		{
			transform.position = aiLocation.position; // Go to the location of the AILocation object
			int num2 = Mathf.RoundToInt(Random.Range(0f, (float)(sounds.Length - 1))); // Choose a random number for playing a sound
			audioDevice.PlayOneShot(sounds[num2]); // Play the sound
		}
	}
	public Transform aiLocation;
	public AudioClip[] sounds;
	public AudioSource audioDevice;
}
