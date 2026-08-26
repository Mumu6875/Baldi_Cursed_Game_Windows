using UnityEngine;

public class Script : MonoBehaviour
{
	private void Update()
	{
		if (!audioDevice.isPlaying & played)
		{
			Application.Quit();
			Debug.Log("The game is closed.");
		}
	}
	private void OnTriggerEnter(Collider other)
	{
		if (other.name == "Player" & !played)
		{
			audioDevice.Play();
			played = true;
		}
	}
	public AudioSource audioDevice;
	private bool played;
}
