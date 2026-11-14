using UnityEngine;

public class AttackP2 : HunterState
{
    Alien target;

    public AttackP2(HunterNPC hunter) : base(hunter)
    {
    }

    public override void Enter()
    {
        target = null;
        if (h.CanAttack) target = h.NearestBoid(false);
    }

    public override void Update()
    {
        if (Threat()) return;

        if (target == null || target.IsDead)
        {
            h.FSM.ChangeState(h.Search);
            return;
        }

        float distance = Vector3.Distance(h.transform.position, target.transform.position);

        if (distance > h.ViewRadius || !h.CanSeeTarget(target.transform.position))
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
            float prediction = distance / Mathf.Max(1f, h.Speed + target.Velocity.magnitude);
            Vector3 destination = target.transform.position + target.Velocity * prediction;
            h.MoveDirect(destination, 1f);
            return;
        }

        h.ResetAttackCooldown();
        h.FSM.ChangeState(h.Search);
    }
}
