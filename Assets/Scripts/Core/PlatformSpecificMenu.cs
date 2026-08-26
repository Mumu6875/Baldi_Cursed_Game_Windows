using UnityEngine;

public class PlatformSpecificMenu : MonoBehaviour
{
	private void Start()
	{
		if (pC != null) pC.SetActive(true);
		if (mobile != null) mobile.SetActive(false);
	}
	public GameObject pC;
	public GameObject mobile;
}
