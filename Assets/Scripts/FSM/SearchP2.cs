using UnityEngine;

public class SearchP2 : HunterState
{
    float nextDestination;

    public SearchP2(HunterNPC hunter) : base(hunter)
    {
    }

    public override void Enter()
    {
        nextDestination = 0;
        h.Navigate(NavGraph.Instance.HuntingNodes[0].position);
    }

    public override void Update()
    {
        if (Threat())
            return;

        if (h.GatherCount >= h.RequiredGathers)
        {
            h.FSM.ChangeState(h.Exit);
            return;
        }

        NavGraph graph = NavGraph.Instance;

        if (graph.InHunting(h.transform.position))
        {
            h.SpawnObject();

            if (h.NearestBoid(true) != null)
            {
                h.FSM.ChangeState(h.Gather);
                return;
            }

            if (h.CanAttack && h.NearestBoid(false) != null)
            {
                h.FSM.ChangeState(h.Attack);
                return;
            }

            if (h.FollowPath() || Time.time >= nextDestination)
            {
                int index = Random.Range(0, graph.HuntingNodes.Count);
                h.Navigate(graph.HuntingNodes[index].position);
                nextDestination = Time.time + 7f;
            }
        }
        else if (h.FollowPath())
        {
            int index = Random.Range(0, graph.HuntingNodes.Count);
            h.Navigate(graph.HuntingNodes[index].position);
        }
    }
}
