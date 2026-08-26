using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BasicButtonScript : MonoBehaviour
{
	private void Start()
	{
		button = GetComponent<Button>();
		button.onClick.AddListener(new UnityAction(OpenScreen));
	}
	private void OpenScreen()
	{
		screen.SetActive(true);
	}
	private Button button;
	public GameObject screen;
}
