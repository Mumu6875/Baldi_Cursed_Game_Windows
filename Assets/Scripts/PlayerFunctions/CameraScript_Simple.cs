using UnityEngine;

public class CameraScript_Simple : MonoBehaviour
{
	private void Start()
	{
		offset = transform.position - player.transform.position;
	}
	private void LateUpdate()
	{
		transform.position = player.transform.position + offset;
		transform.rotation = player.transform.rotation * Quaternion.Euler(0f, (float)lookBehind, 0f);
	}
	public GameObject player;
	private int lookBehind;
	private Vector3 offset;
}
