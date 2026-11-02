using UnityEngine;

public class PatrolState : HunterState
{
    private HunterNPC _agent;
    private PatrolData _data;
    private int _currentIndex = 0;

    public PatrolState(PatrolData data, HunterNPC agent)
    {
        _data = data;
        _agent = agent;
    }

    public override void Enter()
    {
    }

    public override void Update()
    {
        Alien deadBoid = _agent.NearestBoid(true);
        Alien aliveBoid = _agent.NearestBoid(false);

        if (deadBoid != null)
        {
            _agent.FSM.ChangeState(_agent.Gather);
            return;
        }

        if (aliveBoid != null && _agent.CanAttack)
        {
            _agent.FSM.ChangeState(_agent.Attack);
            return;
        }

        _agent.SpawnObject();

        if (_data.waypoints.Count == 0)
        {
            return;
        }

        Transform nextWaypoint = _data.waypoints[_currentIndex];

        if (Vector3.Distance(nextWaypoint.position, _data.transform.position) <= _data.waypointCheckDistance)
        {
            _currentIndex++;

            if (_currentIndex >= _data.waypoints.Count)
            {
                _currentIndex = 0;
            }

            nextWaypoint = _data.waypoints[_currentIndex];
        }

        Vector3 direction = nextWaypoint.position - _data.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            _data.transform.position += direction.normalized * _agent.Speed * Time.deltaTime;
            _data.transform.forward = direction.normalized;
        }
    }

    public override void Exit()
    {
    }
}
