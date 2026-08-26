using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameControllerScript : MonoBehaviour
{
	private void Start()
	{
		cullingMask = playerCamera.cullingMask; // Changes cullingMask in the Camera
		audioDevice = GetComponent<AudioSource>(); //Get the Audio Source
		schoolMusic.Play(); //Play the school music
		LockMouse(); //Prevent the mouse from moving
		UpdateNotebookCount(); //Update the notebook count
		itemSelected = 0; //Set selection to item slot 0(the first item slot)
		gameOverDelay = 0.5f;
	}
	private void Update()
	{
		if (!learningActive)
		{
			if (Singleton<InputManager>.Instance.GetActionKeyDown(InputAction.PauseOrCancel) && !player.gameOver)
			{
				if (!gamePaused)
				{
					PauseGame();
				}
				else
				{
					UnpauseGame();
				}
			}
			
			if (Input.GetKeyDown(KeyCode.Y) & gamePaused)
			{
				ExitGame();
			}
			else if (Input.GetKeyDown(KeyCode.N) & gamePaused)
			{
				UnpauseGame();
			}

			if (!gamePaused & Time.timeScale != 1f)
			{
				Time.timeScale = 1f;
			}

			if (Singleton<InputManager>.Instance.GetActionKeyDown(InputAction.UseItem) && Time.timeScale != 0f)
			{
				UseItem();
			}

			if (Input.GetAxis("Mouse ScrollWheel") > 0f && Time.timeScale != 0f)
			{
				DecreaseItemSelection();
			}
			else if (Input.GetAxis("Mouse ScrollWheel") < 0f && Time.timeScale != 0f)
			{
				IncreaseItemSelection();
			}

			if (Time.timeScale != 0f)
			{
				if (Singleton<InputManager>.Instance.GetActionKeyDown(InputAction.Slot0))
				{
					itemSelected = 0;
					UpdateItemSelection();
				}
				else if (Singleton<InputManager>.Instance.GetActionKeyDown(InputAction.Slot1))
				{
					itemSelected = 1;
					UpdateItemSelection();
				}
				else if (Singleton<InputManager>.Instance.GetActionKeyDown(InputAction.Slot2))
				{
					itemSelected = 2;
					UpdateItemSelection();
				}
			}
		}
		else
		{
			if (Time.timeScale != 0f)
			{
				Time.timeScale = 0f;
			}
		}

		if (player.stamina < 0f & !warning.activeSelf)
		{
			warning.SetActive(true); //Set the warning text to be visible
		}
		else if (player.stamina > 0f & warning.activeSelf)
		{
			warning.SetActive(false); //Set the warning text to be invisible
		}

		if (player.gameOver)
		{
			Time.timeScale = 0f;
			gameOverDelay -= Time.unscaledDeltaTime * 0.5f;
			playerCamera.farClipPlane = gameOverDelay * 400f; //Set camera farClip 
			audioDevice.PlayOneShot(aud_buzz);

			if (gameOverDelay <= 0f)
			{
				Time.timeScale = 1f;
				SceneManager.LoadScene(GameOverScene);
			}
		}

		if (finaleMode && !audioDevice.isPlaying && exitsReached == 3)
		{
			audioDevice.clip = aud_MachineLoop;
			audioDevice.loop = true;
			audioDevice.Play();
		}
	}
	private void UpdateNotebookCount()
	{
		notebookCount.text = notebooks.ToString() + "/7 Notebooks";

		if (notebooks == 7)
		{
			ActivateFinaleMode();
		}
	}
	public void CollectNotebook()
	{
		notebooks++;
		UpdateNotebookCount();
	}
	public void LockMouse()
	{
		if (!learningActive)
		{
			cursorController.LockCursor(); //Prevent the cursor from moving
			mouseLocked = true;
			reticle.SetActive(true);
		}
	}
	public void UnlockMouse()
	{
		cursorController.UnlockCursor(); //Allow the cursor to move
		mouseLocked = false;
		reticle.SetActive(false);
	}
	public void PauseGame()
	{
		if (!learningActive)
		{
			{
				UnlockMouse();
			}
			Time.timeScale = 0f;
			gamePaused = true;
			pauseMenu.SetActive(true);
		}
	}
	public void ExitGame()
	{
		Time.timeScale = 1f;
		SceneManager.LoadScene(ExitGameScene);
	}
	public void UnpauseGame()
	{
		Time.timeScale = 1f;
		gamePaused = false;
		pauseMenu.SetActive(false);
		LockMouse();
	}
	public void ActivateSpoopMode()
	{
		spoopMode = true; //Tells the game its time for spoop
		entrance_0.Lower(); //Lowers all the exits
		entrance_1.Lower();
		entrance_2.Lower();
		entrance_3.Lower();
		baldiTutor.SetActive(false); //Turns off Baldi(The one that you see at the start of the game)
		baldi.SetActive(true); //Turns on Baldi
        principal.SetActive(true); //Turns on Principal
        crafters.SetActive(true); //Turns on Crafters
        playtime.SetActive(true); //Turns on Playtime
        gottaSweep.SetActive(true); //Turns on Gotta Sweep
        bully.SetActive(true); //Turns on Bully
        firstPrize.SetActive(true); //Turns on First-Prize
		//TestEnemy.SetActive(true); //Turns on Test-Enemy (Bonus)
		audioDevice.PlayOneShot(aud_Hang); //Plays the hang sound
		learnMusic.Stop(); //Stop all the music
		schoolMusic.Stop();
	}
	private void ActivateFinaleMode()
	{
		finaleMode = true;
		entrance_0.Raise(); //Raise all the enterances(make them appear)
		entrance_1.Raise();
		entrance_2.Raise();
		entrance_3.Raise();
	}
	public void GetAngry(float value) //Make Baldi get angry
	{
		if (!spoopMode)
		{
			ActivateSpoopMode();
		}
		baldiScrpt.GetAngry(value);
	}
	public void ActivateLearningGame()
	{
		//camera.cullingMask = 0; //Sets the cullingMask to nothing
		learningActive = true;
		UnlockMouse(); //Unlock the mouse
		tutorBaldi.Stop(); //Make tutor Baldi stop talking
		if (!spoopMode) //If the player hasn't gotten a question wrong
		{
			schoolMusic.Stop(); //Start playing the learn music
			learnMusic.Play();
		}
	}
	public void DeactivateLearningGame(GameObject subject)
	{
		playerCamera.cullingMask = cullingMask; //Sets the cullingMask to Everything
		learningActive = false;
		Destroy(subject);
		LockMouse(); //Prevent the mouse from moving
		if (player.stamina < 100f) //Reset Stamina
		{
			player.stamina = 100f;
		}
		if (!spoopMode) //If it isn't spoop mode, play the school music
		{
			schoolMusic.Play();
			learnMusic.Stop();
		}
		if (notebooks == 1 & !spoopMode) // If this is the players first notebook and they didn't get any questions wrong, reward them with a quarter
		{
			quarter.SetActive(true);
			tutorBaldi.PlayOneShot(aud_Prize);
		}
		else if (notebooks == 7) // Plays the all 7 notebook sound
		{
			audioDevice.PlayOneShot(aud_AllNotebooks, 0.8f);
		}
	}
	private void IncreaseItemSelection()
	{
		itemSelected++;
		if (itemSelected > 2)
		{
			itemSelected = 0;
		}
		itemSelect.anchoredPosition = new Vector3(itemSelectOffset[itemSelected], (itemSelectPosition)); //Moves the item selector background(the red rectangle)
		UpdateItemName();
	}
	private void DecreaseItemSelection()
	{
		itemSelected--;
		if (itemSelected < 0)
		{
			itemSelected = 2;
		}
		itemSelect.anchoredPosition = new Vector3(itemSelectOffset[itemSelected], (itemSelectPosition)); //Moves the item selector background(the red rectangle)
		UpdateItemName();
	}
	private void UpdateItemSelection()
	{
		itemSelect.anchoredPosition = new Vector3(itemSelectOffset[itemSelected], (itemSelectPosition)); //Moves the item selector background(the red rectangle)
		UpdateItemName();
	}
	public void CollectItem(int item_ID)
	{
		if (item[0] == 0)
		{
			item[0] = item_ID; //Set the item slot to the Item_ID provided
			itemSlot[0].texture = itemTextures[item_ID]; //Set the item slot's texture to a texture in a list of textures based on the Item_ID
		}
		else if (item[1] == 0)
		{
			item[1] = item_ID; //Set the item slot to the Item_ID provided
            itemSlot[1].texture = itemTextures[item_ID]; //Set the item slot's texture to a texture in a list of textures based on the Item_ID
        }
		else if (item[2] == 0)
		{
			item[2] = item_ID; //Set the item slot to the Item_ID provided
            itemSlot[2].texture = itemTextures[item_ID]; //Set the item slot's texture to a texture in a list of textures based on the Item_ID
        }
		else //This one overwrites the currently selected slot when your inventory is full
		{
			item[itemSelected] = item_ID;
			itemSlot[itemSelected].texture = itemTextures[item_ID];
		}
		UpdateItemName();
	}
	private void UseItem()
	{
		if (item[itemSelected] != 0)
		{
			if (item[itemSelected] == 1)
			{
				player.stamina = player.maxStamina * 2f;
				ResetItem();
				//player.ResetGuilt("food", 3f);
			}
			else if (item[itemSelected] == 2)
			{
				Ray ray = Camera.main.ScreenPointToRay(new Vector3((float)(Screen.width / 2), (float)(Screen.height / 2), 0f));
				RaycastHit raycastHit;
				if (Physics.Raycast(ray, out raycastHit) && (raycastHit.collider.tag == "SwingingDoor" & Vector3.Distance(playerTransform.position, raycastHit.transform.position) <= 10f))
				{
					raycastHit.collider.gameObject.GetComponent<SwingingDoorScript>().LockDoor(15f);
					ResetItem();
				}
			}
			else if (item[itemSelected] == 3)
			{
				Ray ray2 = Camera.main.ScreenPointToRay(new Vector3((float)(Screen.width / 2), (float)(Screen.height / 2), 0f));
				RaycastHit raycastHit2;
				if (Physics.Raycast(ray2, out raycastHit2) && (raycastHit2.collider.tag == "Door" & Vector3.Distance(playerTransform.position, raycastHit2.transform.position) <= 10f))
				{
					DoorScript component = raycastHit2.collider.gameObject.GetComponent<DoorScript>();
					if (component.DoorLocked)
					{
						component.UnlockDoor();
						component.OpenDoor();
						ResetItem();
					}
				}
			}
			else if (item[itemSelected] == 4)
			{
				Instantiate<GameObject>(bsodaSpray, playerTransform.position, cameraTransform.rotation);
				ResetItem();
				player.ResetGuilt("drink", 1f);
				audioDevice.PlayOneShot(aud_Soda);
			}
			else if (item[itemSelected] == 5)
			{
				Ray ray3 = Camera.main.ScreenPointToRay(new Vector3((float)(Screen.width / 2), (float)(Screen.height / 2), 0f));
				RaycastHit raycastHit3;
				if (Physics.Raycast(ray3, out raycastHit3))
				{
					if (raycastHit3.collider.name == "BSODAMachine" & Vector3.Distance(playerTransform.position, raycastHit3.transform.position) <= 10f)
					{
						ResetItem();
						CollectItem(4);
					}
					else if (raycastHit3.collider.name == "ZestyMachine" & Vector3.Distance(playerTransform.position, raycastHit3.transform.position) <= 10f)
					{
						ResetItem();
						CollectItem(1);
					}
					else if (raycastHit3.collider.name == "PayPhone" & Vector3.Distance(playerTransform.position, raycastHit3.transform.position) <= 10f)
					{
						raycastHit3.collider.gameObject.GetComponent<TapePlayerScript>().Play();
						ResetItem();
					}
				}
			}
			else if (item[itemSelected] == 6)
			{
				Ray ray4 = Camera.main.ScreenPointToRay(new Vector3((float)(Screen.width / 2), (float)(Screen.height / 2), 0f));
				RaycastHit raycastHit4;
				if (Physics.Raycast(ray4, out raycastHit4) && (raycastHit4.collider.name == "TapePlayer" & Vector3.Distance(playerTransform.position, raycastHit4.transform.position) <= 10f))
				{
					raycastHit4.collider.gameObject.GetComponent<TapePlayerScript>().Play();
					ResetItem();
				}
			}
			//else if (item[itemSelected] == 6) //For the old tape recordings function
			//{
				//Ray ray4 = Camera.main.ScreenPointToRay(new Vector3((float)(Screen.width / 2), (float)(Screen.height / 2), 0f));
				//RaycastHit raycastHit4;
				//if (Physics.Raycast(ray4, out raycastHit4) && (raycastHit4.collider.name == "TapePlayer" & Vector3.Distance(playerTransform.position, raycastHit4.transform.position) <= 10f))
				//{
					//raycastHit4.collider.gameObject.GetComponent<TapePlayerRecordingsScript>().Play();
					//ResetItem();
				//}
			//}
			else if (item[itemSelected] == 7)
			{
				GameObject gameObject = Instantiate<GameObject>(alarmClock, playerTransform.position, cameraTransform.rotation);
				gameObject.GetComponent<AlarmClockScript>().baldi = baldiScrpt;
				ResetItem();
			}
			else if (item[itemSelected] == 8)
			{
				Ray ray5 = Camera.main.ScreenPointToRay(new Vector3((float)(Screen.width / 2), (float)(Screen.height / 2), 0f));
				RaycastHit raycastHit5;
				if (Physics.Raycast(ray5, out raycastHit5) && (raycastHit5.collider.tag == "Door" & Vector3.Distance(playerTransform.position, raycastHit5.transform.position) <= 10f))
				{
					raycastHit5.collider.gameObject.GetComponent<DoorScript>().SilenceDoor();
					ResetItem();
					audioDevice.PlayOneShot(aud_Spray);
				}
			}
			else if (item[itemSelected] == 9)
			{
				Ray ray6 = Camera.main.ScreenPointToRay(new Vector3((float)(Screen.width / 2), (float)(Screen.height / 2), 0f));
				RaycastHit raycastHit6;
				if (player.jumpRope)
				{
					player.DeactivateJumpRope();
					playtimeScript.Disappoint();
					ResetItem();
				}
				else if (Physics.Raycast(ray6, out raycastHit6) && raycastHit6.collider.name == "1st Prize")
				{
					firstPrizeScript.GoCrazy();
					ResetItem();
				}
			}
			else if (item[itemSelected] == 10)
			{
				player.ActivateBoots();
				StartCoroutine(BootAnimation());
				ResetItem();
			}
			else if (item[itemSelected] == 11)
			{
				StartCoroutine(Teleporter());
				ResetItem();
			}
		}
	}
	private IEnumerator BootAnimation()
	{
		float time = 15f;
		float height = 375f;
		Vector3 position = default(Vector3);
		boots.gameObject.SetActive(true);
		while (height > -375f)
		{
			height -= 375f * Time.deltaTime;
			time -= Time.deltaTime;
			position = boots.localPosition;
			position.y = height;
			boots.localPosition = position;
			yield return null;
		}
		position = boots.localPosition;
		position.y = -375f;
		boots.localPosition = position;
		boots.gameObject.SetActive(false);
		while (time > 0f)
		{
			time -= Time.deltaTime;
			yield return null;
		}
		boots.gameObject.SetActive(true);
		while (height < 375f)
		{
			height += 375f * Time.deltaTime;
			position = boots.localPosition;
			position.y = height;
			boots.localPosition = position;
			yield return null;
		}
		position = boots.localPosition;
		position.y = 375f;
		boots.localPosition = position;
		boots.gameObject.SetActive(false);
		yield break;
	}
	private IEnumerator Teleporter()
	{
		playerCharacter.enabled = false;
		playerCollider.enabled = false;
		int teleports = Random.Range(12, 16);
		int teleportCount = 0;
		float baseTime = 0.2f;
		float currentTime = baseTime;
		float increaseFactor = 1.1f;
		while (teleportCount < teleports)
		{
			currentTime -= Time.deltaTime;
			if (currentTime < 0f)
			{
				Teleport();
				teleportCount++;
				baseTime *= increaseFactor;
				currentTime = baseTime;
			}
			player.height = 4f;
			yield return null;
		}
		playerCharacter.enabled = true;
		playerCollider.enabled = true;
		yield break;
	}
	private void Teleport()
	{
		AILocationSelector.GetNewTarget();
		player.transform.position = AILocationSelector.transform.position + Vector3.up * player.height;
		audioDevice.PlayOneShot(aud_Teleport);
	}
	private void ResetItem()
	{
		item[itemSelected] = 0;
		itemSlot[itemSelected].texture = itemTextures[0];
		UpdateItemName();
	}
	public void LoseItem(int id)
	{
		item[id] = 0;
		itemSlot[id].texture = itemTextures[0];
		UpdateItemName();
	}
	private void UpdateItemName()
	{
		itemText.text = itemNames[item[itemSelected]];
	}
	public void ExitReached()
	{
		exitsReached++;
		if (exitsReached == 1)
		{
			RenderSettings.ambientLight = Color.red; //Make everything red
			//RenderSettings.fog = true;
			audioDevice.PlayOneShot(aud_Switch, 0.8f);
			audioDevice.clip = aud_MachineQuiet;
			audioDevice.loop = true;
			audioDevice.Play(); //start playing the weird sound
		}
		if (exitsReached == 2) //Play a sound
		{
			audioDevice.volume = 0.8f;
			audioDevice.clip = aud_MachineStart;
			audioDevice.loop = true;
			audioDevice.Play();
		}
		if (exitsReached == 3) //Play a even louder sound
		{
			audioDevice.clip = aud_MachineRev;
			audioDevice.loop = false;
			audioDevice.Play();
		}
	}
	public CursorControllerScript cursorController;
	public PlayerScript player;
	public Transform playerTransform;
	public CharacterController playerCharacter;
	public Collider playerCollider;
	public AILocationSelectorScript AILocationSelector;
	public Transform cameraTransform;
	public Camera playerCamera;
    private int cullingMask;
	public EntranceScript entrance_0;
	public EntranceScript entrance_1;
	public EntranceScript entrance_2;
	public EntranceScript entrance_3;
	public GameObject baldiTutor;
	public GameObject baldi;
	public BaldiScript baldiScrpt;
	public AudioClip aud_Prize;
	public AudioClip aud_PrizeMobile;
	public AudioClip aud_AllNotebooks;
	public AudioClip aud_Teleport;
	public GameObject principal;
	public GameObject crafters;
	public GameObject playtime;
	public PlaytimeScript playtimeScript;
	public GameObject gottaSweep;
	public GameObject bully;
	public GameObject firstPrize;
	public GameObject TestEnemy;
	public FirstPrizeScript firstPrizeScript;
	public GameObject quarter;
	public AudioSource tutorBaldi;
	public RectTransform boots;
	public int notebooks;
	public int failedNotebooks;
	public bool spoopMode;
	public bool finaleMode;
	public bool debugMode;
	public bool mouseLocked;
	public int exitsReached;
	public int itemSelected;
	public int[] item = new int[3];
	public RawImage[] itemSlot = new RawImage[3];
	private string[] itemNames = new string[]
	{
		"Nothing",
		"Energy flavored Zesty Bar",
		"Yellow Door Lock",
		"Principal's Keys",
		"BSODA",
		"Quarter",
		"Baldi Anti Hearing and Disorienting Tape",
		"Alarm Clock",
		"WD-NoSquee (Door Type)",
		"Safety Scissors",
		"Big Ol' Boots",
		"Teleportation Teleporter"
	};
	public TMP_Text itemText;
	public Texture[] itemTextures = new Texture[10];
	public GameObject bsodaSpray;
	public GameObject alarmClock;
	public TMP_Text notebookCount;
	public GameObject pauseMenu;
	public string ExitGameScene;
	public GameObject warning;
	public GameObject reticle;
	public RectTransform itemSelect;
	public int[] itemSelectOffset;
	public int itemSelectPosition;
	private bool gamePaused;
	private bool learningActive;
	private float gameOverDelay;
	public string GameOverScene;
	private AudioSource audioDevice;
	public AudioClip aud_Soda;
	public AudioClip aud_Spray;
	public AudioClip aud_buzz;
	public AudioClip aud_Hang;
	public AudioClip aud_MachineQuiet;
	public AudioClip aud_MachineStart;
	public AudioClip aud_MachineRev;
	public AudioClip aud_MachineLoop;
	public AudioClip aud_Switch;
	public AudioSource schoolMusic;
	public AudioSource learnMusic;
}
