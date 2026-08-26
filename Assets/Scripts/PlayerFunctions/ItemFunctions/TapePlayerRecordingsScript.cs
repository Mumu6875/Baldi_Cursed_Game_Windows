using UnityEngine;

public class TapePlayerRecordingsScript : MonoBehaviour
{
	private void Start()
	{
		audioDevice = GetComponent<AudioSource>();
	}
	public void Play()
	{
		sprite.sprite = closedSprite;
		audVal = Mathf.RoundToInt(UnityEngine.Random.Range(0f, 4f));
		audioDevice.PlayOneShot(this.recordings[audVal]);
		if (baldi.isActiveAndEnabled) baldi.Hear(transform.position, 4f);
	}
	public Sprite closedSprite;
	public SpriteRenderer sprite;
	private int audVal;
	public AudioClip[] recordings = new AudioClip[5];
	public BaldiScript baldi;
	private AudioSource audioDevice;
}
