using UnityEngine;

public class ClickableTest : MonoBehaviour
{
	private void Start()
	{
		GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

		if (playerObject != null)
		{
			player = playerObject.transform;
		}
	}
	private void Update()
	{
		if (Input.GetMouseButtonDown(0)) //Left click
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit raycastHit;
			if (Physics.Raycast(ray, out raycastHit) && (raycastHit.transform.tag == "Notebook" & Vector3.Distance(player.position, transform.position) < 10f)) // If you are looking at a notebook
			{
				test.PlayOneShot(testHit);
				gameObject.SetActive(false); //Disable the notebook
			}
		}
	}
	private Transform player;
	public AudioSource test;
	public AudioClip testHit;
}
