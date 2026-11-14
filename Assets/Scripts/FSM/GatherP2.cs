using UnityEngine;

public class GatherP2 : HunterState
{
    Alien target;
    float timer;

    public GatherP2(HunterNPC hunter) : base(hunter)
    {
    }

    public override void Enter()
    {
        target = h.NearestBoid(true);
        timer = 0;

        if (target != null)
            h.Navigate(target.transform.position);
    }

    public override void Update()
    {
        if (Threat())
            return;

        if (target == null || !target.IsDead || target.IsCollected)
        {
            h.FSM.ChangeState(h.Search);
            return;
        }

        float distance = Vector3.Distance(h.transform.position, target.transform.position);

        if (distance > 1.5f)
        {
            timer = 0;

            if (!h.FollowPath())
                return;

            h.MoveDirect(target.transform.position, 1.2f);
            return;
        }

        timer += Time.deltaTime;

        if (timer < h.GatherDuration)
            return;

        target.Collect();
        h.AddGather();

        if (h.GatherCount >= h.RequiredGathers)
            h.FSM.ChangeState(h.Exit);
        else
            h.FSM.ChangeState(h.Search);
    }
}
