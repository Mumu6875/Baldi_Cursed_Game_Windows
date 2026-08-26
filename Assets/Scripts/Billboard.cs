using UnityEngine;

public class Billboard : MonoBehaviour
{
	private void Start()
	{
		m_Camera = Camera.main;
	}
	private void LateUpdate()
	{
		transform.LookAt(transform.position + m_Camera.transform.rotation * Vector3.forward); // Look towards the player
	}
	private Camera m_Camera;
}
