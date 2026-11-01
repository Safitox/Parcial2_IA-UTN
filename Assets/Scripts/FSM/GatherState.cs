using UnityEngine;

public class GatherState : HunterState
{
    private HunterNPC _agent;
    private Alien _target;
    private float _gatherTimer;
    private float _gatherDuration = 5f;
    public GatherState(HunterNPC agent)
    {
        _agent = agent;
    }

    public override void Enter()
    {
        _target = GetClosestDeadBoid();
        _gatherTimer = 0;
    }

    public override void Update()
    {
        if (_target == null || !_target.IsDead) 
        {   _agent.FSM.ChangeState(_agent.Patrol); 
            return; 
        }

        float distSqr = (_target.transform.position - _agent.transform.position).sqrMagnitude;

        if (distSqr > 1.5f * 1.5f)
        {
            _gatherTimer = 0f;

            _agent.transform.position = Vector3.MoveTowards(
                _agent.transform.position,
                _target.transform.position,
                _agent.Speed * Time.deltaTime
            );

            Vector3 direction = _target.transform.position - _agent.transform.position;
            direction.y = 0;

            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                _agent.transform.rotation = Quaternion.RotateTowards(
                    _agent.transform.rotation,
                    targetRotation,
                    _agent.RotationSpeed * Time.deltaTime
                );
            }
        }
        else
        {
            _gatherTimer += Time.deltaTime;

            if (_gatherTimer >= _gatherDuration)
            {
                _target.Collect();
                _target = null;

                _agent.FSM.ChangeState(_agent.Patrol);
            }
        }
    }

    private Alien GetClosestDeadBoid()
    {
        Alien closest = null;

        float minDistSqr = _agent.ViewRadius * _agent.ViewRadius;

        foreach (Alien boid in Alien.AllAgents)
        {
            if (boid == null)
                continue;

            if (!boid.IsDead || boid.IsCollected)
                continue;

            float distSqr = (boid.transform.position - _agent.transform.position).sqrMagnitude;

            if (distSqr < minDistSqr)
            {
                minDistSqr = distSqr;
                closest = boid;
            }
        }
        return closest;
    }

    public override void Exit()
    {
        return;
    }
}