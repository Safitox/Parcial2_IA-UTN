using UnityEngine;

public class FleeP2 : HunterState
{
    NavGraph.Node safe;
    float wait;

    public FleeP2(HunterNPC hunter) : base(hunter)
    {
    }

    public override void Enter()
    {
        NavGraph graph = NavGraph.Instance;
        float bestDistance = float.PositiveInfinity;
        safe = null;

        foreach (NavGraph.Node node in graph.SafeNodes)
        {
            var path = graph.FindPath(h.transform.position, node.position);
            float distance = graph.PathLength(path);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                safe = node;
            }
        }

        if (safe != null)
        {
            h.Navigate(safe.position);
            Debug.Log("Cazador detectó lobo. Huyendo a zona segura: " + safe.position);
        }
        else
        {
            Debug.Log("Cazador detectó lobo, pero no encontró una zona segura. Es pollo");
        }

        wait = 0;
    }

    public override void Update()
    {
        if (safe == null)
        {
            h.FSM.ChangeState(h.Search);
            return;
        }

        if (!NavGraph.Instance.IsSafe(h.transform.position))
        {
            if (h.FollowPath())
                h.MoveDirect(safe.position, 0.4f);

            return;
        }

        wait += Time.deltaTime;

        if (wait >= h.SafeZoneWaitTime)
        {
            if (h.GatherCount >= h.RequiredGathers)
                h.FSM.ChangeState(h.Exit);
            else
                h.FSM.ChangeState(h.Search);
        }
    }
}
