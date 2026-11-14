using UnityEngine;

public class ExitP2 : HunterState
{
    public ExitP2(HunterNPC hunter) : base(hunter)
    {
    }

    public override void Enter()
    {
        h.Navigate(NavGraph.Instance.ExitNode.position);
    }

    public override void Update()
    {
        if (Threat())
            return;

        Vector3 goal = NavGraph.Instance.ExitNode.position;
        float distance = Vector3.Distance(h.transform.position, goal);

        if (distance < 1.3f)
        {
            h.SetVisible(false);
            h.ResetCycle();
            return;
        }

        if (h.FollowPath())
            h.MoveDirect(goal, 0.8f);
    }
}
