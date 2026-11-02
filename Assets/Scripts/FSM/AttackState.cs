using UnityEngine;

public class AttackState : HunterState
{
    private HunterNPC _agent;

    public AttackState(HunterNPC agent)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        Debug.Log("Atacando");
    }

    public override void Update()
    {
        Alien target = _agent.NearestBoid(false);

        if (target == null)
        {
            _agent.FSM.ChangeState(_agent.Patrol);
            return;
        }

        Vector3 direction = target.transform.position - _agent.transform.position;
        direction.y = 0f;
        float distanceSquared = direction.sqrMagnitude;

        if (distanceSquared > _agent.ViewRadius * _agent.ViewRadius)
        {
            _agent.FSM.ChangeState(_agent.Patrol);
            return;
        }

        if (distanceSquared > 0.01f)
        {
            Quaternion rotation = Quaternion.LookRotation(direction);
            _agent.transform.rotation = Quaternion.RotateTowards(_agent.transform.rotation, rotation, _agent.RotationSpeed * Time.deltaTime);
        }

        if (distanceSquared <= _agent.MeleeRadius * _agent.MeleeRadius)
        {
            target.TakeDamage(5f);
            _agent.ResetAttackCooldown();
            _agent.FSM.ChangeState(_agent.Patrol);
            return;
        }

        if (distanceSquared <= _agent.RangeRadius * _agent.RangeRadius)
        {
            _agent.Shoot(target);
            _agent.ResetAttackCooldown();
            _agent.FSM.ChangeState(_agent.Patrol);
            return;
        }

        _agent.transform.position = Vector3.MoveTowards(_agent.transform.position, target.transform.position, _agent.Speed * Time.deltaTime);
    }

    public override void Exit()
    {
    }
}
