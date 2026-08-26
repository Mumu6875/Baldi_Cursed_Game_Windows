using UnityEngine;
using UnityEngine.AI;

public class BaldiScript : MonoBehaviour
{
	private void Start()
	{
		baldiAudio = GetComponent<AudioSource>(); //Get The Baldi Audio Source(Used mostly for the slap sound)
		AudioClip replacementSlap = Resources.Load<AudioClip>("CursedMod/BaldiRulerLoud");
		if (replacementSlap != null)
		{
			slap = replacementSlap;
		}
		else
		{
			Debug.LogError("Replacement Baldi ruler sound could not be loaded.");
		}
		if (baldiAudio != null)
		{
			// The supplied recording is peak-normalized in the imported asset.
			// Extend its 3D reach so the ruler can be heard across long hallways.
			baldiAudio.volume = 1f;
			baldiAudio.spatialBlend = 1f;
			baldiAudio.minDistance = Mathf.Max(baldiAudio.minDistance, 12f);
			baldiAudio.maxDistance = Mathf.Max(baldiAudio.maxDistance, 500f);
		}
		agent = GetComponent<NavMeshAgent>(); //Get the Nav Mesh Agent
		timeToMove = baseTime; //Sets timeToMove to baseTime
		Wander(); //Start wandering
		if (PlayerPrefs.GetInt("Rumble") == 1)
		{
			rumble = true;
		}
	}
	private void Update()
	{
		if (timeToMove > 0f) //If timeToMove is greater then 0, decrease it
		{
			timeToMove -= 1f * Time.deltaTime;
		}
		else
		{
			Move(); //Start moving
		}
		if (coolDown > 0f) //If coolDown is greater then 0, decrease it
		{
			coolDown -= 1f * Time.deltaTime;
		}
		if (baldiTempAnger > 0f) //Slowly decrease Baldi's temporary anger over time.
		{
			baldiTempAnger -= 0.02f * Time.deltaTime;
		}
		else
		{
			baldiTempAnger = 0f; //Cap its lowest value at 0
		}
		if (antiHearingTime > 0f) //Decrease antiHearingTime, then when it runs out stop the effects of the antiHearing tape
		{
			antiHearingTime -= Time.deltaTime;
		}
		else
		{
			antiHearing = false;
		}
	}
	private void FixedUpdate()
	{
		if (moveFrames > 0f) //Move for a certain amount of frames, and then stop moving.(Ruler slapping)
		{
			moveFrames -= 1f;
			agent.speed = speed;
		}
		else
		{
			agent.speed = 0f;
		}
		Vector3 direction = player.position - transform.position; 
		RaycastHit raycastHit;
		if (Physics.Raycast(transform.position + Vector3.up * 2f, direction, out raycastHit, float.PositiveInfinity, 769, QueryTriggerInteraction.Ignore) & raycastHit.transform.tag == "Player") //Create a raycast, if the raycast hits the player, Baldi can see the player
		{
			db = true;
			TargetPlayer(); //Start attacking the player
		}
		else
		{
			db = false;
		}
	}
	private void Wander()
	{
		wanderer.GetNewTarget(); //Get a new location
		agent.SetDestination(wanderTarget.position); //Head towards the position of the wanderTarget object
		coolDown = 1f; //Set the cooldown
		currentPriority = 0f;
	}
	public void TargetPlayer()
	{
		agent.SetDestination(player.position); //Target the player
		coolDown = 1f;
		currentPriority = 0f;
	}
	private void Move()
	{
		if (transform.position == previous & coolDown < 0f) // If Baldi reached his destination, start wandering
		{
			Wander();
		}
		moveFrames = 10f;
		timeToMove = baldiWait - baldiTempAnger;
		previous = transform.position; // Set previous to Baldi's current location
		baldiAudio.PlayOneShot(slap); //Play the slap sound
		baldiAnimator.SetTrigger("slap"); // Play the slap animation
		if (rumble)
		{
			float num = Vector3.Distance(transform.position, player.position);
			if (num < vibrationDistance)
			{
				float motorLevel = 1f - num / vibrationDistance;
			}
		}
	}
	public void GetAngry(float value)
	{
		baldiAnger += value; // Increase Baldi's anger by the value provided
		if (baldiAnger < 0.5f) //Cap Baldi anger at a minimum of 0.5
		{
			baldiAnger = 0.5f;
		}
		baldiWait = -3f * baldiAnger / (baldiAnger + 2f / baldiSpeedScale) + 3f; //Some formula I don't understand.
	}
	public void GetTempAngry(float value)
	{
		baldiTempAnger += value; //Increase Baldi's Temporary Anger
	}
	public void Hear(Vector3 soundLocation, float priority)
	{
		if (!antiHearing && priority >= currentPriority) //If anti-hearing is not active and the priority is greater then the priority of the current sound
		{
			agent.SetDestination(soundLocation); //Go to that sound
			currentPriority = priority; //Set the current priority to the priority
		}
	}
	public void ActivateAntiHearing(float t)
	{
		Wander(); //Start wandering
		antiHearing = true; //Set the antihearing variable to true for other scripts
		antiHearingTime = t; //Set the time the tape's effect on baldi will last
	}
	public bool db;
	public float baseTime;
	public float speed;
	public float timeToMove;
	public float baldiAnger;
	public float baldiTempAnger;
	public float baldiWait;
	public float baldiSpeedScale;
	private float moveFrames;
	private float currentPriority;
	public bool antiHearing;
	public float antiHearingTime;
	public float vibrationDistance;
	public float angerRate;
	public float angerRateRate;
	public float angerFrequency;
	public float timeToAnger;
	public Transform player;
	public Transform wanderTarget;
	public AILocationSelectorScript wanderer;
	private AudioSource baldiAudio;
	public AudioClip slap;
	public Animator baldiAnimator;
	public float coolDown;
	private Vector3 previous;
	private bool rumble;
	private NavMeshAgent agent;
}
