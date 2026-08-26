using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BackButtonScript : MonoBehaviour
{
	private void Start()
	{
		button = GetComponent<Button>();
		button.onClick.AddListener(new UnityAction(CloseScreen));
	}
	private void CloseScreen()
	{
		screen.SetActive(false);
	}
	private Button button;
	public GameObject screen;
}
