using System.Collections;
using UnityEngine;

public class DefaultSettingsScript : MonoBehaviour
{
	private void Start()
	{
		if (!PlayerPrefs.HasKey("OptionsSet"))
		{
			options.SetActive(true);
			StartCoroutine(CloseOptions());
			canvas.enabled = false;
		}
	}
	public IEnumerator CloseOptions()
	{
		yield return new WaitForEndOfFrame();
		canvas.enabled = true;
		options.SetActive(false);
		yield break;
	}
	public GameObject options;
	public Canvas canvas;
}
