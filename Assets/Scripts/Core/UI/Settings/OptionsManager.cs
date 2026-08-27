using UnityEngine;
using UnityEngine.UI;

public class OptionsManager : MonoBehaviour
{
	private const string MouseSensitivityKey = "MouseSensitivity";
	private const float DefaultMouseSensitivity = 2f;

	private void Awake()
	{
		// These legacy options have no supported PC implementation in this mod.
		// Hide them instead of presenting toggles that cannot affect gameplay.
		if (rumble != null)
		{
			rumble.gameObject.SetActive(false);
		}
		if (analog != null)
		{
			analog.gameObject.SetActive(false);
		}
	}

	private void Start()
	{
		if (!PlayerPrefs.HasKey(MouseSensitivityKey))
		{
			PlayerPrefs.SetFloat(MouseSensitivityKey, DefaultMouseSensitivity);
		}
		slider.value = Mathf.Clamp(PlayerPrefs.GetFloat(MouseSensitivityKey, DefaultMouseSensitivity), slider.minValue, slider.maxValue);

		PlayerPrefs.SetInt("Rumble", 0);
		PlayerPrefs.SetInt("AnalogMove", 0);
		PlayerPrefs.Save();
	}
	private void Update()
	{
		PlayerPrefs.SetFloat(MouseSensitivityKey, slider.value);
	}
	public Slider slider;
	public Toggle rumble;
	public Toggle analog;
}
