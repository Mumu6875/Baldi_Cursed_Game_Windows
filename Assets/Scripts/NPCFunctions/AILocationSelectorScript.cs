using UnityEngine;

public class AILocationSelectorScript : MonoBehaviour
{
	public void GetNewTarget()
	{
		id = Mathf.RoundToInt(Random.Range(0f, 28f)); //Get a random number between 0 and 28
		transform.position = newLocation[id].position; //Set it's location to a position in a list of positions using the ID variable that just got set.
		ambience.PlayAudio(); //Play an ambience audio
	}
	public void GetNewTargetHallway()
	{
		id = Mathf.RoundToInt(Random.Range(0f, 15f)); //Get a random number between 0 and 15
		transform.position = newLocation[id].position; //Set it's location to a position in a list of positions using the ID variable that just got set.
		ambience.PlayAudio(); //Play an ambience audio
	}
	public void QuarterExclusive()
	{
		id = Mathf.RoundToInt(Random.Range(1f, 15f)); //Get a random number between 0 and 15
		transform.position = newLocation[id].position; //Set it's location to a position in a list of positions using the ID variable that just got set.
	}
	public Transform[] newLocation = new Transform[29];
	public AmbienceScript ambience;
	private int id;
}
