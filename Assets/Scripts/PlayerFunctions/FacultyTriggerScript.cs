using UnityEngine;

public class FacultyTriggerScript : MonoBehaviour
{
	private void Start()
	{
		hitBox = GetComponent<BoxCollider>();
	}
	private void OnTriggerStay(Collider other)
	{
		if (other.gameObject.CompareTag("Player")) //If it is a player
		{
			ps.ResetGuilt("faculty", 1f); //Make the player guilty of entering school faculty for 1 second
		}
	}
	public PlayerScript ps;
	private BoxCollider hitBox;
}
