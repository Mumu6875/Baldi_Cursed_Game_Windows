using UnityEngine;

public class YouWonScript : MonoBehaviour
{
	private void Start()
	{
		delay = 10f;
	}
	private void Update()
	{
		delay -= Time.deltaTime;
		if (delay <= 0f)
		{
			Application.Quit();
		}
	}
	private float delay;
}
