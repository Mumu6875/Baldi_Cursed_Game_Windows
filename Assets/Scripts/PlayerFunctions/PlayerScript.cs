using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class PlayerScript : MonoBehaviour
{
	private void Start()
	{
		if (PlayerPrefs.GetInt("AnalogMove") == 1)
		{
			sensitivityActive = true;
		}
		height = transform.position.y;
		stamina = maxStamina;
		playerRotation = transform.rotation;
		mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity");
		principalBugFixer = 1;
		flipaturn = 1f;
	}
	private void Update()
	{
		transform.position = new Vector3(transform.position.x, height, transform.position.z);
		MouseMove();
		PlayerMove();
		StaminaCheck();
		GuiltCheck();
		if (cc.velocity.magnitude > 0f)
		{
			gc.LockMouse();
		}
		if (jumpRope & (transform.position - frozenPosition).magnitude >= 1f) // If the player moves, deactivate the jumprope minigame
		{
			DeactivateJumpRope();
		}
		if (sweepingFailsave > 0f)
		{
			sweepingFailsave -= Time.deltaTime;
		}
		else
		{
			sweeping = false;
			hugging = false;
		}
	}
	private void MouseMove()
	{
		playerRotation.eulerAngles = new Vector3(playerRotation.eulerAngles.x, playerRotation.eulerAngles.y, fliparoo);
		float horizontalLook = Input.GetAxis("Mouse X") * mouseSensitivity * Time.timeScale;
		playerRotation.eulerAngles = playerRotation.eulerAngles + Vector3.up * horizontalLook * flipaturn;
		transform.rotation = playerRotation;
	}
	private void PlayerMove()
	{
		Vector3 movement = Vector3.zero;
		Vector3 lateralMovement = Vector3.zero;
		float inputMagnitude;
		if (Singleton<InputManager>.Instance.GetActionKey(InputAction.MoveForward)) movement = transform.forward;
		if (Singleton<InputManager>.Instance.GetActionKey(InputAction.MoveBackward)) movement = -transform.forward;
		if (Singleton<InputManager>.Instance.GetActionKey(InputAction.MoveLeft)) lateralMovement = -transform.right;
		if (Singleton<InputManager>.Instance.GetActionKey(InputAction.MoveRight)) lateralMovement = transform.right;
		inputMagnitude = Mathf.Clamp01((movement + lateralMovement).magnitude);
		if (stamina > 0f)
		{
			if (Singleton<InputManager>.Instance.GetActionKey(InputAction.Run))
			{
				playerSpeed = runSpeed;
				sensitivity = 1f;
				if (cc.velocity.magnitude > 0.1f & !hugging & !sweeping)
				{
					ResetGuilt("running", 0.1f);
				}
			}
			else
			{
				playerSpeed = walkSpeed;
				if (sensitivityActive)
				{
					sensitivity = inputMagnitude;
				}
				else
				{
					sensitivity = 1f;
				}
			}
		}
		else
		{
			playerSpeed = walkSpeed;
			if (sensitivityActive)
			{
				sensitivity = inputMagnitude;
			}
			else
			{
				sensitivity = 1f;
			}
		}
		playerSpeed *= Time.deltaTime;
		moveDirection = (movement + lateralMovement).normalized * playerSpeed * sensitivity;
		if (!(!jumpRope & !sweeping & !hugging))
		{
			if (sweeping && !bootsActive)
			{
				moveDirection = gottaSweep.velocity * Time.deltaTime + moveDirection * 0.3f;
			}
			else if (hugging && !bootsActive)
			{
				moveDirection = (firstPrize.velocity * 1.2f * Time.deltaTime + (new Vector3(firstPrizeTransform.position.x, height, firstPrizeTransform.position.z) + new Vector3((float)Mathf.RoundToInt(firstPrizeTransform.forward.x), 0f, (float)Mathf.RoundToInt(firstPrizeTransform.forward.z)) * 3f - transform.position)) * (float)principalBugFixer;
			}
			else if (jumpRope)
			{
				moveDirection = new Vector3(0f, 0f, 0f);
			}
		}
		cc.Move(moveDirection);
	}
	private void StaminaCheck()
	{
		if (cc.velocity.magnitude > 0.1f)
		{
			if (Singleton<InputManager>.Instance.GetActionKey(InputAction.Run) & stamina > 0f)
			{
				stamina -= staminaRate * Time.deltaTime;
			}
			if (stamina < 0f & stamina > -5f)
			{
				stamina = -5f;
			}
		}
		else if (stamina < maxStamina)
		{
			stamina += staminaRate * Time.deltaTime;
		}
		staminaBar.value = stamina / maxStamina * 100f;
	}
	private void OnTriggerEnter(Collider other)
	{
		if (other.transform.name == "Baldi" & !gc.debugMode)
		{
			gameOver = true;
			RenderSettings.skybox = blackSky; //Sets the skybox black
			StartCoroutine(KeepTheHudOff()); //Hides the Hud
		}
		else if (other.transform.name == "Playtime" & !jumpRope & playtime.playCool <= 0f)
		{
			ActivateJumpRope();
		}
	}
	public IEnumerator KeepTheHudOff()
	{
		while (gameOver)
		{
			hud.enabled = false;
			jumpRopeScreen.SetActive(false);
			yield return new WaitForEndOfFrame();
		}
		yield break;
	}
	private void OnTriggerStay(Collider other)
	{
		if (other.transform.name == "Gotta Sweep")
		{
			sweeping = true;
			sweepingFailsave = 1f;
		}
		else if (other.transform.name == "1st Prize" & firstPrize.velocity.magnitude > 5f)
		{
			hugging = true;
			sweepingFailsave = 1f;
		}
	}
	private void OnTriggerExit(Collider other)
	{
		if (other.transform.name == "Office Trigger")
		{
			ResetGuilt("escape", door.lockTime);
		}
		else if (other.transform.name == "Gotta Sweep")
		{
			sweeping = false;
		}
		else if (other.transform.name == "1st Prize")
		{
			hugging = false;
		}
	}
	public void ResetGuilt(string type, float amount)
	{
		if (amount >= guilt)
		{
			guilt = amount;
			guiltType = type;
		}
	}
	private void GuiltCheck()
	{
		if (guilt > 0f)
		{
			guilt -= Time.deltaTime;
		}
	}
	public void ActivateJumpRope()
	{
		jumpRopeScreen.SetActive(true);
		jumpRope = true;
		frozenPosition = transform.position;
	}
	public void DeactivateJumpRope()
	{
		jumpRopeScreen.SetActive(false);
		jumpRope = false;
	}
	public void ActivateBoots()
	{
		bootsActive = true;
		StartCoroutine(BootTimer());
	}
	private IEnumerator BootTimer()
	{
		float time = 15f;
		while (time > 0f)
		{
			time -= Time.deltaTime;
			yield return null;
		}
		bootsActive = false;
		yield break;
	}
	public GameControllerScript gc;
	public BaldiScript baldi;
	public DoorScript door;
	public PlaytimeScript playtime;
	public bool gameOver;
	public bool jumpRope;
	public bool sweeping;
	public bool hugging;
	public bool bootsActive;
	public int principalBugFixer;
	public float sweepingFailsave;
	public float fliparoo;
	public float flipaturn;
	private Quaternion playerRotation;
	public Vector3 frozenPosition;
	private bool sensitivityActive;
	private float sensitivity;
	public float mouseSensitivity;
	public float walkSpeed;
	public float runSpeed;
	public float slowSpeed;
	public float maxStamina;
	public float staminaRate;
	public float guilt;
	public float initGuilt;
	private Vector3 moveDirection;
	private float playerSpeed;
	public float stamina;
	public CharacterController cc;
	public NavMeshAgent gottaSweep;
	public NavMeshAgent firstPrize;
	public Transform firstPrizeTransform;
	public Slider staminaBar;
	public float db;
	public string guiltType;
	public GameObject jumpRopeScreen;
	public float height;
	public Material blackSky;
	public Canvas hud;
}
