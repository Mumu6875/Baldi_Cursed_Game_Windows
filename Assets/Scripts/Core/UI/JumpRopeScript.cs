using TMPro;
using UnityEngine;

public class JumpRopeScript : MonoBehaviour
{
	private void OnEnable()
	{
		jumpDelay = 1f;
		ropeHit = true;
		jumpStarted = false;
		jumps = 0;
		jumpCount.text = 0 + "/5";
		cs.jumpHeight = 0f;
		playtime.audioDevice.PlayOneShot(playtime.aud_ReadyGo);
	}
	private void Update()
	{
		if (jumpDelay > 0f) //Decrease jumpDelay countdown
		{
			jumpDelay -= Time.deltaTime;
		}
		else if (!jumpStarted) //If the jump hasn't started
		{
			jumpStarted = true; //Start the jump
			ropePosition = 1f; //Set the rope position to 1f
			rope.SetTrigger("ActivateJumpRope"); //Activate the jumprope
			ropeHit = false;
		}
		if (ropePosition > 0f)
		{
			ropePosition -= Time.deltaTime;
		}
		else if (!ropeHit) //If the player has not tried to hit the rope
		{
			RopeHit();
		}
	}
	private void RopeHit()
	{
		ropeHit = true; //Set ropehit to true
		if (cs.jumpHeight <= 0.2f)
		{
			Fail(); //Fail
		}
		else
		{
			Success(); //Succeed
		}
		jumpStarted = false;
	}
	private void Success()
	{
		playtime.audioDevice.Stop(); //Stop all of the lines playtime is currently speaking
		playtime.audioDevice.PlayOneShot(playtime.aud_Numbers[jumps]);
		jumps++;
		jumpCount.text = jumps + "/5";
		jumpDelay = 0.5f;
		if (jumps >= 5) //If players complete the minigame
		{
			playtime.audioDevice.Stop(); //Stop playtime from talking
			playtime.audioDevice.PlayOneShot(playtime.aud_Congrats);
			ps.DeactivateJumpRope(); //Deactivate the jumprope
		}
	}
	private void Fail()
	{
		jumps = 0; //Reset jumps
		jumpCount.text = jumps + "/5";
		jumpDelay = 2f; //Set the jump delay to 2 seconds to allow playtime to finish her line before the rope starts again
		playtime.audioDevice.PlayOneShot(playtime.aud_Oops);
	}
	public TMP_Text jumpCount;
	public Animator rope;
	public CameraScript cs;
	public PlayerScript ps;
	public PlaytimeScript playtime;
	public int jumps;
	public float jumpDelay;
	public float ropePosition;
	public bool ropeHit;
	public bool jumpStarted;
}
