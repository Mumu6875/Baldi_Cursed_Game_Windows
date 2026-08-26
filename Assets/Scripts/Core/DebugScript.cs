using System.Collections;
using UnityEngine;
using TMPro;

public class DebugScript : MonoBehaviour
{
	private void OnEnable() => Application.logMessageReceived += LogError;

	private void OnDisable() => Application.logMessageReceived -= LogError;

	private void LogError(string logString, string stackTrace, LogType type)
	{
		if (type == LogType.Error || type == LogType.Exception)
		{
			errorText.text = logString + "\n" + stackTrace + "\n";
			if (!errorSource.isPlaying)
			{
				errorSource.Play();
			}
			if (logDissappearCoroutine != null)
			{
				StopCoroutine(logDissappearCoroutine);
			}
			logDissappearCoroutine = StartCoroutine(LogDissappear());
		}
	}
	private IEnumerator LogDissappear()
	{
		yield return new WaitForSeconds(10f);
		float elapsed = 0f;
		Color color = errorText.color;
		color.a = 1f;
		errorText.color = color;

		while (elapsed < 1f)
		{
			elapsed += Time.deltaTime;
			color.a = Mathf.Lerp(1f, 0f, elapsed / 1f);
			errorText.color = color;
			yield return null;
		}

		color.a = 0f;
		errorText.color = color;
		logDissappearCoroutine = null;
		yield break;
	}
		private void Start()
	{
		if (limitFramerate)
		{
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = framerate;
		}
	}
	public void Update()
	{
		if (Time.deltaTime != 0f)
		{
			deltaTime += (Time.deltaTime - this.deltaTime) * 0.1f;
			float f = 1f / this.deltaTime;
			fpsText.text = Mathf.Ceil(f).ToString() + " :FPS";
		}
		foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
		{
			if (Input.GetKeyDown(keyCode))
			{
				keyText.text = keyCode.ToString() + " :Input";
				break; // Only show the first key detected this frame
			}
		}
	}
	public bool limitFramerate;
	public int framerate;
	public TextMeshProUGUI fpsText;
	public float deltaTime;
	[SerializeField] private TextMeshProUGUI keyText;
	[SerializeField] private TMP_Text errorText;
	[SerializeField] private AudioSource errorSource;
	private Coroutine logDissappearCoroutine;
}
