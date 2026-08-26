using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
	public void OnEnable()
	{
		uc.firstButton = firstButton;
		uc.dummyButtonPC = dummyButtonPC;
		uc.dummyButtonElse = dummyButtonElse;
		uc.SwitchMenu();
	}
	public UIController uc;
	public Selectable firstButton;
	public Selectable dummyButtonPC;
	public Selectable dummyButtonElse;
	public GameObject back;
}
