using UnityEngine;

public class AlarmClockScript : MonoBehaviour
{
	private void Start()
	{
		timeLeft = 30f;
		lifeSpan = 35f;
	}
	private void Update()
	{
		if (timeLeft >= 0f) //If the time is greater then 0
		{
			timeLeft -= Time.deltaTime; //Decrease the time variable
		}
		else if (!rang) // If it has not been rang
		{
			Alarm(); // Start the alarm function
		}
		if (lifeSpan >= 0f) //If the time left in the lifespan is greater then 0
		{
			lifeSpan -= Time.deltaTime; //Decrease the time variable
		}
		else
		{
			Destroy(gameObject, 0f); //Otherwise, if time is less then 0, destroy the alarm clock
		}
	}
	private void Alarm()
	{
		rang = true;
		if (baldi.isActiveAndEnabled) baldi.Hear(transform.position, 8f); //Baldi is told to go to this location, with a priority of 10(above most sounds)
		audioDevice.clip = ring;
		audioDevice.loop = false; // Tells the audio not to loop
		audioDevice.Play(); //Play the audio
	}
	public float timeLeft;
	private float lifeSpan;
	private bool rang;
	public BaldiScript baldi;
	public AudioClip ring;
	public AudioSource audioDevice;
}
