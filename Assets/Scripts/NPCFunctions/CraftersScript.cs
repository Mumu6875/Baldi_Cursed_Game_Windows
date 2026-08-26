using UnityEngine;
using UnityEngine.AI;

public class CraftersScript : MonoBehaviour
{
	private void Start()
	{
		agent = GetComponent<NavMeshAgent>(); // Defines the nav mesh agent
		audioDevice = GetComponent<AudioSource>(); //Gets the audio source
		sprite.SetActive(false); // Set arts and crafters sprite to be invisible
	}
	private void Update()
	{
		if (forceShowTime > 0f)
		{
			forceShowTime -= Time.deltaTime;
		}
		if (gettingAngry) //If arts is getting agry
		{
			anger += Time.deltaTime; // Increase anger
			if (anger >= 1f & !angry) //If anger is greater then 1 and arts isn't angry
			{
				angry = true; // Get angry
				audioDevice.PlayOneShot(aud_Intro); // Do the woooosoh sound
				spriteImage.sprite = angrySprite; // Switch to the angry sprite
			}
		}
		else if (anger > 0f) // If anger is greater then 0, decrease.
		{
			anger -= Time.deltaTime;
		}
		if (!angry) // If not angry
		{
			if (((transform.position - agent.destination).magnitude <= 20f & (transform.position - player.position).magnitude >= 60f) || forceShowTime > 0f) //If close to the player and force showtime is less then 0
			{
				sprite.SetActive(true); // Become visible
			}
			else
			{
				sprite.SetActive(false); // Become invisible
			}
		}
		else
		{
			agent.speed = agent.speed + 60f * Time.deltaTime; // Increase the speed
			TargetPlayer(); // Target the player
			if (!audioDevice.isPlaying) //If the sound is not already playing
			{
				audioDevice.PlayOneShot(aud_Loop); //Play the full wooooosh sound
			}
		}
	}
	private void FixedUpdate()
	{
		if (gc.notebooks >= 7) // If the player has more then 7 notebooks
		{
			Vector3 direction = player.position - transform.position;
			RaycastHit raycastHit;
			if (Physics.Raycast(transform.position + Vector3.up * 2f, direction, out raycastHit, float.PositiveInfinity, 769, QueryTriggerInteraction.Ignore) & raycastHit.transform.tag == "Player" & craftersRenderer.isVisible & sprite.activeSelf) // If Arts is Visible, and active and sees the player
			{
				gettingAngry = true; // Start to get angry
			}
			else
			{
				gettingAngry = false; // Stop getting angry
			}
		}
	}
	public void GiveLocation(Vector3 location, bool flee)
	{
		if (!angry && agent.isActiveAndEnabled)
		{
			agent.SetDestination(location);
			if (flee)
			{
				forceShowTime = 3f; // Make arts appear in 3 seconds
			}
		}
	}
	private void TargetPlayer()
	{
		agent.SetDestination(player.position); // Set destination to the player
	}
	private void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player" & angry) // If arts is angry and is touching the player
		{
			cc.enabled = false;
			player.position = new Vector3(playerTeleportPoint.position.x, 4f, playerTeleportPoint.position.z); // Teleport the player to Player_WarpPoint
			baldiAgent.Warp(baldiTeleportPoint.position); // Teleport Baldi to Baldi_WarpPoint
			cc.enabled = true;
			gameObject.SetActive(false); // Despawn Arts And Crafters
		}
	}
	public bool db;
	public bool angry;
	public bool gettingAngry;
	public float anger;
	private float forceShowTime;
	public Transform player;
	public CharacterController cc;
	public Transform playerCamera;
	public Transform baldiTeleportPoint;
	public Transform playerTeleportPoint;
	public NavMeshAgent baldiAgent;
	public GameObject sprite;
	public GameControllerScript gc;
	[SerializeField]
	private NavMeshAgent agent;
	public Renderer craftersRenderer;
	public SpriteRenderer spriteImage;
	public Sprite angrySprite;
	private AudioSource audioDevice;
	public AudioClip aud_Intro;
	public AudioClip aud_Loop;
}
