using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitTriggerScript : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		if (gc.notebooks >= 7 & other.tag == "Player")
		{
			// The all-wrong ending takes priority over the Room 99 sequence.
			if (gc.failedNotebooks >= 7)
			{
				SceneManager.LoadScene(SecretScene);
				return;
			}

			// The fourth/final exit now opens the Room 99 horror sequence instead
			// of immediately loading the normal results scene.
			if (gc.finaleMode && gc.exitsReached >= 3 &&
				CursedFinalExitSequence.TryStart(this, other, gc))
			{
				return;
			}

			SceneManager.LoadScene(ResultsScene); //Go to the win screen
		}
	}
	public GameControllerScript gc;
	public string ResultsScene;
	public string SecretScene;
}
