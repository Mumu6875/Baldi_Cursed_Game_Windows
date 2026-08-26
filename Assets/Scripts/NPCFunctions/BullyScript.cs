using UnityEngine;

public class BullyScript : MonoBehaviour
{
	private void Start()
	{
		audioDevice = GetComponent<AudioSource>(); //Get the Audio Source
		waitTime = Random.Range(60f, 120f); //Set the amount of time before the bully appears again
	}
	private void Update()
	{
		if (waitTime > 0f) //Decrease the waittime
		{
			waitTime -= Time.deltaTime;
		}
		else if (!active)
		{
			Activate(); //Activate the Bully
		}
		if (active) //If the Bully is on the map
		{
			activeTime += Time.deltaTime; //Increase active time
			if (activeTime >= 180f & (transform.position - player.position).magnitude >= 120f) //If the bully has been in the map for a long time and the player is far away
			{
				Reset(); //Reset the bully
			}
		}
		if (guilt > 0f)
		{
			guilt -= Time.deltaTime; //Decrease Bully's guilt
		}
	}
	private void FixedUpdate()
	{
		Vector3 direction = player.position - transform.position;
		RaycastHit raycastHit;
		if (Physics.Raycast(transform.position + new Vector3(0f, 4f, 0f), direction, out raycastHit, float.PositiveInfinity, 769, QueryTriggerInteraction.Ignore) & raycastHit.transform.tag == "Player" & (transform.position - player.position).magnitude <= 30f & active)
		{
			if (!spoken) // If the bully hasn't already spoken
			{
				int num = Mathf.RoundToInt(Random.Range(0f, 1f)); //Get a random number between 0 and 1
				audioDevice.PlayOneShot(aud_Taunts[num]); //Say a line in an index using num
				spoken = true; //Sets spoken to true, preventing the bully from talking again
			}
			guilt = 10f; //Makes the bully guilty for "Bullying in the halls"
		}
	}
	private void Activate()
	{
		wanderer.GetNewTargetHallway(); //Get a hallway position
		transform.position = wanderTarget.position + new Vector3(0f, 5f, 0f); // Go to the wanderTarget + 5 on the Y axis
		while ((transform.position - player.position).magnitude < 20f) // While the Bully is close to the player
		{
			wanderer.GetNewTargetHallway(); //Get a new target
			transform.position = wanderTarget.position + new Vector3(0f, 5f, 0f);// Go to the wanderTarget + 5 on the Y axis
        } //This is here to prevent the bully from spawning ontop iof the player
		active = true; //Set the bully to active
	}
	private void OnTriggerEnter(Collider other)
	{
		if (other.transform.tag == "Player") // If touching the player
		{
			if (gc.item[0] == 0 & gc.item[1] == 0 & gc.item[2] == 0) // If the player has no items
			{
				audioDevice.PlayOneShot(aud_Denied); // "What, no items? No Items? No passsssss"
			}
			else
			{
				int num = Mathf.RoundToInt(Random.Range(0f, 2f)); //Get a random item slot
				while (gc.item[num] == 0) //If the selected slot is empty
				{
					num = Mathf.RoundToInt(Random.Range(0f, 2f)); // Choose another slot
				}
				gc.LoseItem(num); // Remove the item selected
				int num2 = Mathf.RoundToInt(Random.Range(0f, 1f));
				audioDevice.PlayOneShot(aud_Thanks[num2]);
				Reset();
			}
		}
	}
	private void OnTriggerStay(Collider other)
	{
		if (other.transform.name == "Principal of the Thing" & guilt > 0f) //If touching the principal and the bully is guilty
		{
			Reset(); //Reset the bully
		}
	}
	private void Reset()
	{
		transform.position = transform.position - new Vector3(0f, 20f, 0f); // Go to X: 0, Y: 20, Z: 20
		waitTime = Random.Range(60f, 120f); //Set the amount of time before the bully appears again
		active = false; //Set active to false
		activeTime = 0f; //Reset active time
		spoken = false; //Reset spoken
	}
	public Transform player;
	public GameControllerScript gc;
	public Renderer bullyRenderer;
	public Transform wanderTarget;
	public AILocationSelectorScript wanderer;
	public float waitTime;
	public float activeTime;
	public float guilt;
	public bool active;
	public bool spoken;
	private AudioSource audioDevice;
	public AudioClip[] aud_Taunts = new AudioClip[2];
	public AudioClip[] aud_Thanks = new AudioClip[2];
	public AudioClip aud_Denied;
}
