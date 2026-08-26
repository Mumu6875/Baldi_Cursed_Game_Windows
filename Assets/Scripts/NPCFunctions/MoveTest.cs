using UnityEngine;

public class MoveTest : MonoBehaviour
{
	private void Update()
	{
		transform.position = transform.position + new Vector3(0.1f, 0f, 0f);
	}
}
