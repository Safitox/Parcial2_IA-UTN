using UnityEngine;

public abstract class HunterP2State : HunterState
{
    protected readonly HunterNPC h;

    protected HunterP2State(HunterNPC hunter)
    {
        h = hunter;
    }

    public override void Enter()
    {
    }

    public override void Exit()
    {
    }

    protected bool Threat()
    {
        if (h.DetectWolf())
        {
            h.FSM.ChangeState(h.Flee);
            return true;
        }

        return false;
    }
}

public class SearchP2 : HunterP2State
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

public class AttackP2 : HunterP2State
{
    Alien target;

    public AttackP2(HunterNPC hunter) : base(hunter)
    {
    }

    public override void Enter()
    {
        target = null;

        if (h.CanAttack)
            target = h.NearestBoid(false);
    }

    public override void Update()
    {
        if (Threat())
            return;

        if (target == null || target.IsDead)
        {
            h.FSM.ChangeState(h.Search);
            return;
        }

        float distance = Vector3.Distance(h.transform.position, target.transform.position);

        if (distance > h.ViewRadius)
        {
            h.FSM.ChangeState(h.Search);
            return;
        }

        if (distance <= h.MeleeRadius)
        {
            target.TakeDamage(5f);
        }
        else if (distance <= h.RangeRadius)
        {
            h.Shoot(target);
        }
        else
        {
            Vector3 direction = target.transform.position - h.transform.position;
            Vector3 start = h.transform.position + Vector3.up * 0.4f;

            bool blocked = Physics.Raycast(start, direction.normalized, out RaycastHit hit, direction.magnitude, Physics.AllLayers, QueryTriggerInteraction.Ignore);

            if (blocked && hit.collider.GetComponentInParent<Alien>() != target)
            {
                h.Navigate(target.transform.position);
                h.FollowPath();
            }
            else
            {
                float prediction = distance / Mathf.Max(1f, h.Speed + target.Velocity.magnitude);
                Vector3 destination = target.transform.position + target.Velocity * prediction;
                h.MoveDirect(destination, 1f);
            }

            return;
        }

        h.ResetAttackCooldown();
        h.FSM.ChangeState(h.Search);
    }
}

public class GatherP2 : HunterP2State
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

public class FleeP2 : HunterP2State
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

public class ExitP2 : HunterP2State
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

public class DeadP2 : HunterP2State
{
    float timer;

    public DeadP2(HunterNPC hunter) : base(hunter)
    {
    }

    public override void Enter()
    {
        timer = 0;
        h.SetVisible(false);
        Debug.Log("Cazador capturado. Respawneando...");
    }

    public override void Update()
    {
        timer += Time.deltaTime;

        if (timer >= h.RespawnTime)
            h.ResetCycle();
    }
}
