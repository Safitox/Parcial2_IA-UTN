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

    public override void Update()
    {
        DetectBoids(
            out Alien deadBoid,
            out Alien aliveBoid
        );

        // Gather tiene prioridad sobre Attack.
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

        Transform nextWaypoint = _data.waypoints[_currentIndex];
        if (Vector3.Distance(nextWaypoint.position, _data.transform.position) <= _data.waypointCheckDistance)
        {
            _currentIndex = _currentIndex + 1 < _data.waypoints.Count ? _currentIndex + 1 : 0;
            nextWaypoint = _data.waypoints[_currentIndex];
        }

        Vector3 dir = nextWaypoint.position - _data.transform.position;

        _data.transform.position += _agent.Speed * Time.deltaTime * dir.normalized;
        _data.transform.forward = dir;
    }

    public override void Exit()
    {

    }

    private void DetectBoids(out Alien closestDeadBoid, out Alien closestAliveBoid)
    {
        closestDeadBoid = null;
        closestAliveBoid = null;

        float viewRadiusSqr = _agent.ViewRadius * _agent.ViewRadius;

        float closestDeadDistanceSqr = viewRadiusSqr;
        float closestAliveDistanceSqr = viewRadiusSqr;

        foreach (Alien boid in Alien.AllAgents)
        {
            if (boid == null)
                continue;

            float distanceSqr = (boid.transform.position - _agent.transform.position).sqrMagnitude;

            if (distanceSqr > viewRadiusSqr)
                continue;

            if (boid.IsDead)
            {
                if (boid.IsCollected)
                    continue;

                if (distanceSqr < closestDeadDistanceSqr)
                {
                    closestDeadDistanceSqr = distanceSqr;
                    closestDeadBoid = boid;
                }
            }
            else
            {
                if (distanceSqr < closestAliveDistanceSqr)
                {
                    closestAliveDistanceSqr = distanceSqr;
                    closestAliveBoid = boid;
                }
            }
        }
    }

    public override void Enter()
    {

    }
}
