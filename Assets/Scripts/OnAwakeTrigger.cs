using UnityEngine;
using UnityEngine.Events;

public class OnAwakeTrigger : MonoBehaviour
{
	private void OnEnable()
	{
		OnEnableEvent.Invoke();
	}
	public UnityEvent OnEnableEvent;
}
