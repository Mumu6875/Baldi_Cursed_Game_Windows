using UnityEngine;

public class CraftersTriggerScript : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Player")
		{
			crafters.GiveLocation(goTarget.position, false);
		}
	}
	private void OnTriggerExit(Collider other)
	{
		if (other.tag == "Player")
		{
			crafters.GiveLocation(fleeTarget.position, true);
		}
	}
	public Transform goTarget;
	public Transform fleeTarget;
	public CraftersScript crafters;
}
