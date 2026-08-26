using UnityEngine;

public class DebugFirstPrizeScript : MonoBehaviour
{
	private void Update()
	{
		transform.position = first.position + new Vector3((float)Mathf.RoundToInt(first.forward.x), 0f, (float)Mathf.RoundToInt(first.forward.z)) * 3f;
	}
	public Transform player;
	public Transform first;
}
