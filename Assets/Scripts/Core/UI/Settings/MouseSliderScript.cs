using UnityEngine;
using UnityEngine.UI;

public class MouseSliderScript : MonoBehaviour
{
	private const string MouseSensitivityKey = "MouseSensitivity";
	private const float DefaultMouseSensitivity = 2f;

	private void Start()
	{
		if (!PlayerPrefs.HasKey(MouseSensitivityKey))
		{
			PlayerPrefs.SetFloat(MouseSensitivityKey, DefaultMouseSensitivity);
		}
		slider.value = Mathf.Clamp(PlayerPrefs.GetFloat(MouseSensitivityKey, DefaultMouseSensitivity), slider.minValue, slider.maxValue);
	}
	private void Update()
	{
		PlayerPrefs.SetFloat(MouseSensitivityKey, slider.value);
	}
	public Slider slider;
}
