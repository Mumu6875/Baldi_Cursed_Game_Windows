using UnityEngine;

public class DoorScript : MonoBehaviour
{
	private void Start()
	{
		myAudio = GetComponent<AudioSource>();
	}
	private void Update()
	{
		if (lockTime > 0f) // If the lock time is greater then 0, decrease lockTime
		{
			lockTime -= 1f * Time.deltaTime;
		}
		else if (bDoorLocked) //If the door is locked, unlock it
		{
			UnlockDoor();
		}
		if (openTime > 0f) // If the open time is greater then 0, decrease lockTime Decrease open time
		{
			openTime -= 1f * Time.deltaTime;
		}
		if (openTime <= 0f & bDoorOpen)
		{
			barrier.enabled = true; // Turn on collision
			invisibleBarrier.enabled = true; //Enable the invisible barrier
			bDoorOpen = false; //Set the door open status to false
			inside.material = closed; // Change one side of the door to the closed material
			outside.material = closedOutside; // Change the other side of the door to the closed material
            if (silentOpens <= 0) //If the door isn't silent
			{
				myAudio.PlayOneShot(doorClose, 1f); //Play the door close sound
			}
		}
		if (Singleton<InputManager>.Instance.GetActionKeyDown(InputAction.Interact) && Time.timeScale != 0f) //If the door is left clicked and the game isn't paused
		{
			Ray ray = Camera.main.ScreenPointToRay(new Vector3((float)(Screen.width / 2), (float)(Screen.height / 2), 0f));
			RaycastHit raycastHit;
			if (Physics.Raycast(ray, out raycastHit) && (raycastHit.collider == trigger & Vector3.Distance(player.position, transform.position) < openingDistance & !bDoorLocked))
			{
				if (baldi.isActiveAndEnabled & silentOpens <= 0)
				{
					baldi.Hear(transform.position, 1f); //If the door isn't silent, Baldi hears the door with a priority of 1.
				}
				OpenDoor();
				if (silentOpens > 0) //If the door is silent
				{
					silentOpens--; //Decrease the amount of opens the door will stay quite for.
				}
			}
		}
	}
	public void OpenDoor()
	{
		if (silentOpens <= 0 && !bDoorOpen) //Play the door sound if the door isn't silent
		{
			myAudio.PlayOneShot(doorOpen, 1f);
		}
		barrier.enabled = false; //Disable the Barrier
		invisibleBarrier.enabled = false;//Disable the invisible Barrier
		bDoorOpen = true; //Set the door open status to false
		inside.material = open; //Change one side of the door to the open material
		outside.material = openOutside; //Change the other side of the door to the open material
        openTime = 3f; //Set the open time to 3 seconds
	}
	private void OnTriggerStay(Collider other)
	{
		if (!bDoorLocked & other.CompareTag("NPC")) //Open the door if it isn't locked and it is an NPC
		{
			OpenDoor();
		}
	}
	public void LockDoor(float time) //Lock the door for a specified amount of time
	{
		bDoorLocked = true;
		lockTime = time;
	}
	public void UnlockDoor() //Unlock the door
	{
		bDoorLocked = false;
	}
	public bool DoorLocked
	{
		get
		{
			return bDoorLocked;
		}
	}
	public void SilenceDoor() //Set the amount of times the door can be open silently
	{
		silentOpens = 4;
	}
	public float openingDistance;
	public Transform player;
	public BaldiScript baldi;
	public MeshCollider barrier;
	public MeshCollider trigger;
	public MeshCollider invisibleBarrier;
	public MeshRenderer inside;
	public MeshRenderer outside;
	public AudioClip doorOpen;
	public AudioClip doorClose;
	public Material closed;
	public Material open;
	public Material closedOutside;
	public Material openOutside;
	private bool bDoorOpen;
	private bool bDoorLocked;
	public int silentOpens;
	private float openTime;
	public float lockTime;
	private AudioSource myAudio;
}
