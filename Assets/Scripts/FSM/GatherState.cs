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
        _target = _agent.NearestBoid(true);
        _gatherTimer = 0f;
    }

    public override void Update()
    {
        if (_target == null || !_target.IsDead || _target.IsCollected)
        {
            _agent.FSM.ChangeState(_agent.Patrol);
            return;
        }

        Vector3 direction = _target.transform.position - _agent.transform.position;
        direction.y = 0f;
        float distanceSquared = direction.sqrMagnitude;

        if (distanceSquared > 1.5f * 1.5f)
        {
            _gatherTimer = 0f;
            _agent.transform.position = Vector3.MoveTowards(_agent.transform.position, _target.transform.position, _agent.Speed * Time.deltaTime);

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion rotation = Quaternion.LookRotation(direction);
                _agent.transform.rotation = Quaternion.RotateTowards(_agent.transform.rotation, rotation, _agent.RotationSpeed * Time.deltaTime);
            }

            return;
        }

        _gatherTimer += Time.deltaTime;

        if (_gatherTimer >= _gatherDuration)
        {
            _target.Collect();
            _target = null;
            _agent.FSM.ChangeState(_agent.Patrol);
        }
    }

    public override void Exit()
    {
    }
}
