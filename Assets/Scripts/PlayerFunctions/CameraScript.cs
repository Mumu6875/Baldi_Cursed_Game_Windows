using UnityEngine;

public class CameraScript : MonoBehaviour
{
	private void Start()
	{
		offset = transform.position - player.transform.position; //Defines the offset
	}
	private void Update()
	{
		if (ps.jumpRope) //If the player is jump roping
		{
			velocity -= gravity * Time.deltaTime; //Decrease the velocity using gravity
			jumpHeight += velocity * Time.deltaTime; //Increase the jump height based on the velocity
			if (jumpHeight <= 0f) //When the player is on the floor, prevent the player from falling through.
			{
				jumpHeight = 0f;
				if (Singleton<InputManager>.Instance.GetActionKey(InputAction.Jump))
				{
					velocity = initVelocity; //Start the jump
				}
			}
			jumpHeightV3 = new Vector3(0f, jumpHeight, 0f); //Turn the float into a vector
		}
		else if (Singleton<InputManager>.Instance.GetActionKey(InputAction.LookBehind))
		{
			lookBehind = 180; //Look behind you
		}
		else
		{
			lookBehind = 0; //Don't look behind you
		}
	}
	private void LateUpdate()
	{
		transform.position = player.transform.position + offset; //Teleport to the player, then move based on the offset vector(if all other statements fail)
		if (!ps.gameOver & !ps.jumpRope)
		{
			transform.position = player.transform.position + offset; //Teleport to the player, then move based on the offset vector
			transform.rotation = player.transform.rotation * Quaternion.Euler(0f, (float)lookBehind, 0f); //Rotate based on player direction + lookbehind
		}
		else if (ps.gameOver)
		{
			transform.position = baldi.transform.position + baldi.transform.forward * BaldiOffset.z + new Vector3(0f, BaldiOffset.y, 0f);//Puts the camera in front of Baldi
			transform.LookAt(new Vector3(baldi.position.x, baldi.position.y + BaldiOffset.y, baldi.position.z));//Makes the player look at baldi with an offset so the camera doesn't look at the feet
		}
		else if (ps.jumpRope)
		{
			transform.position = player.transform.position + offset + jumpHeightV3; //Apply the jump rope vector onto the normal offset
			transform.rotation = player.transform.rotation; //Rotate based on player direction
		}
	}
	public GameObject player;
	public PlayerScript ps;
	public Transform baldi;
	public Vector3 BaldiOffset;
	public float initVelocity;
	public float velocity;
	public float gravity;
	private int lookBehind;
	public Vector3 offset;
	public float jumpHeight;
	public Vector3 jumpHeightV3;
}
