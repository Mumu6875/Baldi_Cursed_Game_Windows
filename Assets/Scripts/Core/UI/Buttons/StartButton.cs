using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButton : MonoBehaviour
{
	public void StartGame()
	{
		SceneManager.LoadSceneAsync(LoadScene);
	}
	public string LoadScene;
}
