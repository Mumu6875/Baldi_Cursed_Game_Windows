using UnityEngine;

public class NearExitTriggerScript : MonoBehaviour
{
    private bool reached;

    private void OnTriggerEnter(Collider other)
    {
        if (reached || gc == null || !gc.finaleMode || !other.CompareTag("Player"))
        {
            return;
        }

        // Preserve the original behavior for exits 1-3.
        if (gc.exitsReached < 3)
        {
            reached = true;
            gc.ExitReached();
            if (es != null) es.Lower();
            if (gc.baldiScrpt != null && gc.baldiScrpt.isActiveAndEnabled)
            {
                gc.baldiScrpt.Hear(transform.position, 8f);
            }
            return;
        }

        // Exit 4 must be intercepted on the SCHOOL side. If we wait for the
        // outer ExitTriggerScript, the player has already crossed the map edge.
        ExitTriggerScript finalExit = FindClosestExitTrigger();
        if (finalExit != null && CursedFinalExitSequence.TryStart(finalExit, other, gc))
        {
            reached = true;
        }
    }

    private ExitTriggerScript FindClosestExitTrigger()
    {
        ExitTriggerScript[] exits = Resources.FindObjectsOfTypeAll<ExitTriggerScript>();
        ExitTriggerScript closest = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < exits.Length; i++)
        {
            ExitTriggerScript candidate = exits[i];
            if (candidate == null || !candidate.gameObject.scene.IsValid()) continue;
            if (candidate.gameObject.scene != gameObject.scene) continue;
            if (candidate.gc != gc) continue;

            float distance = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distance >= bestDistance) continue;

            bestDistance = distance;
            closest = candidate;
        }

        return closest;
    }

    public GameControllerScript gc;
    public EntranceScript es;
}
