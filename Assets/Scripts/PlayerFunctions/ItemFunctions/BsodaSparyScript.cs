using UnityEngine;

public class BsodaSparyScript : MonoBehaviour
{
	private void Start()
	{
		rb = GetComponent<Rigidbody>(); //Get the RigidBody
		rb.linearVelocity = transform.forward * speed; //Move forward
		lifeSpan = 30f; //Set the lifespan
	}
	private void Update()
	{
		rb.linearVelocity = transform.forward * speed; //Move forward
		lifeSpan -= Time.deltaTime; // Decrease the lifespan variable
		if (lifeSpan < 0f) //When the lifespan timer ends, destroy the BSODA
		{
			Destroy(gameObject, 0f);
		}
	}
	public float speed;
	private float lifeSpan;
	private Rigidbody rb;
}
