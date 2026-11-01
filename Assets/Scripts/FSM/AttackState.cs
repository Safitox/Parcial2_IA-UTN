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
        Alien target = GetClosestBoid();

        // Si no encuentra un objetivo, vuelve a Patrol y no se reinicia el tiempo de ataque.

        if (target == null)
        {
            _agent.FSM.ChangeState(_agent.Patrol);
            return;
        }

        float distSqr = (
            target.transform.position -
            _agent.transform.position
        ).sqrMagnitude;

        // Si el objetivo salió del rango de visión, vuelve a Patrol sin reiniciar el tiempo de ataqeu

        if (distSqr > _agent.ViewRadius * _agent.ViewRadius)
        {
            _agent.FSM.ChangeState(_agent.Patrol);
            return;
        }

        Vector3 direction =
            target.transform.position -
            _agent.transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            _agent.transform.rotation =
                Quaternion.RotateTowards(
                    _agent.transform.rotation,
                    targetRotation,
                    _agent.RotationSpeed * Time.deltaTime
                );
        }

        // Prioridad 1: Melee a las piñas
        if (distSqr <= _agent.MeleeRadius * _agent.MeleeRadius)
        {
            target.TakeDamage(5f);

            _agent.ResetAttackCooldown();
            _agent.FSM.ChangeState(_agent.Patrol);
            return;
        }

        // Prioridad 2: ataque a distancia a los tiros
        if (distSqr <= _agent.RangeRadius * _agent.RangeRadius)
        {
            Shoot(target);

            _agent.ResetAttackCooldown();
            _agent.FSM.ChangeState(_agent.Patrol);
            return;
        }

        // Fuera de ambos rangos: sale a perseguir
        _agent.transform.position = Vector3.MoveTowards(
            _agent.transform.position,
            target.transform.position,
            _agent.Speed * Time.deltaTime
        );
    }

    private Alien GetClosestBoid()
    {
        Alien closest = null;
        float minDistSqr =
            _agent.ViewRadius * _agent.ViewRadius;

        foreach (Alien boid in Alien.AllAgents)
        {
            if (boid == null || boid.IsDead)
                continue;

            float distSqr = (
                boid.transform.position -
                _agent.transform.position
            ).sqrMagnitude;

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

    }

    private void Shoot(Alien target)
    {
        Vector3 dir = (target.transform.position - _agent.transform.position).normalized;

        GameObject projGO = GameObject.Instantiate(
            _agent.BulletPrefab,
            _agent.SpawnBulletPoint.position,
            Quaternion.LookRotation(dir)
        );

        Bullet proj = projGO.GetComponent<Bullet>();
        proj.Init(dir, _agent.BulletSpeed, 10f);
    }
}