using UnityEngine;

public class QuarterSpawnScript : MonoBehaviour
{
	private void Start()
	{
		wanderer.QuarterExclusive();
		transform.position = location.position + Vector3.up * 4f;
	}
	public AILocationSelectorScript wanderer;
	public Transform location;
}
