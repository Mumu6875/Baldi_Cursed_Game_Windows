using UnityEngine;

public class NotebookScript : MonoBehaviour
{
	private void Start()
	{
		up = true;
	}
	private void Update()
	{
		if (Singleton<InputManager>.Instance.GetActionKey(InputAction.Interact) && Time.timeScale != 0f)
		{
			Ray ray = Camera.main.ScreenPointToRay(new Vector3((float)(Screen.width / 2), (float)(Screen.height / 2), 0f));
			RaycastHit raycastHit;
			if (Physics.Raycast(ray, out raycastHit) && (raycastHit.transform.tag == "Notebook" & Vector3.Distance(player.position, transform.position) < openingDistance))
			{
				transform.position = new Vector3(transform.position.x, -20f, transform.position.z);
				up = false;
				respawnTime = 120f;
				gc.CollectNotebook();
				GameObject gameObject = Instantiate<GameObject>(learningGame);
				gameObject.GetComponent<MathGameScript>().gc = gc;
				gameObject.GetComponent<MathGameScript>().baldiScript = bsc;
				gameObject.GetComponent<MathGameScript>().playerPosition = player.position;
			}
		}
	}
	public float openingDistance;
	public GameControllerScript gc;
	public BaldiScript bsc;
	public float respawnTime;
	public bool up;
	public Transform player;
	public GameObject learningGame;
	public AudioSource audioDevice;
}
