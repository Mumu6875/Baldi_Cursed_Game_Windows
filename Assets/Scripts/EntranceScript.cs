using UnityEngine;

public class EntranceScript : MonoBehaviour
{
	public void Lower()
	{
		transform.position = transform.position - new Vector3(0f, 10f, 0f);
		if (gc.finaleMode)
		{
			wall.material = map;
		}
	}
	public void Raise()
	{
		transform.position = transform.position + new Vector3(0f, 10f, 0f);
	}
	public GameControllerScript gc;
	public Material map;
	public MeshRenderer wall;
}
