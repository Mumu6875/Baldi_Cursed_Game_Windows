using UnityEngine;
using UnityEngine.AI;

public class AgentTest : MonoBehaviour
{
	private void Start()
	{
		agent = GetComponent<NavMeshAgent>(); // Define the AI Agent
		Wander(); //Start wandering
	}
	private void Update()
	{
		if (coolDown > 0f)
		{
			coolDown -= 1f * Time.deltaTime;
		}
	}
	private void FixedUpdate()
	{
		Vector3 direction = player.position - transform.position;
		RaycastHit raycastHit;
		if (Physics.Raycast(transform.position, direction, out raycastHit, float.PositiveInfinity, 3, QueryTriggerInteraction.Ignore) & raycastHit.transform.tag == "Player") //Check if its the player
		{
			db = true;
			TargetPlayer(); //Head towards the player
		}
		else
		{
			db = false;
			if (agent.velocity.magnitude <= 1f & coolDown <= 0f)
			{
				Wander(); //Just wander
			}
		}
	}
	private void Wander()
	{
		wanderer.GetNewTarget(); //Get a new target on the map
		agent.SetDestination(wanderTarget.position); //Set its destination to position of the wanderTarget
		coolDown = 1f;
	}
	private void TargetPlayer()
	{
		agent.SetDestination(player.position); //Set it's destination to the player
		coolDown = 1f;
	}
	public bool db;
	public Transform player;
	public Transform wanderTarget;
	public AILocationSelectorScript wanderer;
	public float coolDown;
	private NavMeshAgent agent;
}
