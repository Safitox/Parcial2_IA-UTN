using System.Collections.Generic;
using UnityEngine;

public class HunterNPC : MonoBehaviour
{
    [Header("Parcial 2 - Objetivos")]
    public int RequiredGathers = 3;
    public float RespawnTime = 5f;
    public float SafeZoneWaitTime = 4f;
    public float GatherDuration = 3f;
    public int GatherCount { get; private set; }
    public bool IsInactive
    {
        get { return FSM.CurrentState == Dead || !_visible; }
    }

    [Header("Combate")]
    [SerializeField] float _tba = 3f;
    [SerializeField] float _rangeAttackRadius = 6f;
    [SerializeField] float _meleeRadius = 2f;
    [SerializeField] float _viewRadius = 10f;
    [SerializeField] float _speed = 3.6f;
    [SerializeField] float _rotationSpeed = 250f;
    [SerializeField] Bullet _bulletPrefab;
    [SerializeField] Transform _spawnBulletPoint;
    [SerializeField] float _bulletSpeed = 20f;

    [Header("Intereses")]
    [SerializeField] ObjectOfInterest _interestPrefab;
    [SerializeField] float _spawnInterval = 8f;

    float attackTimer = 999f;
    float spawnTimer;
    Vector3 previous;
    bool _visible = true;
    AgentSensor sensor;
    VisionSensor vision;
    int lastPerceptionScan = -1;
    bool wolfVisible;
    Alien nearestAlive;
    Alien nearestDead;
    AgentFeedback feedback;
    Renderer[] renderers;
    Collider[] colliders;

    readonly List<ObjectOfInterest> spawned = new List<ObjectOfInterest>();
    readonly MoverEnGraph mover = new MoverEnGraph();
    readonly FiniteStateMachine fsm = new FiniteStateMachine();

    public FiniteStateMachine FSM
    {
        get { return fsm; }
    }
    public HunterState Search { get; private set; }
    public HunterState Attack { get; private set; }
    public HunterState Gather { get; private set; }
    public HunterState Flee { get; private set; }
    public HunterState Exit { get; private set; }
    public HunterState Dead { get; private set; }
    public Vector3 Velocity { get; private set; }
    public float Speed
    {
        get { return _speed; }
    }
    public float RotationSpeed
    {
        get { return _rotationSpeed; }
    }
    public Bullet BulletPrefab
    {
        get { return _bulletPrefab; }
    }
    public Transform SpawnBulletPoint
    {
        get { return _spawnBulletPoint; }
    }
    public float BulletSpeed
    {
        get { return _bulletSpeed; }
    }
    public float RangeRadius
    {
        get { return _rangeAttackRadius; }
    }
    public float MeleeRadius
    {
        get { return _meleeRadius; }
    }
    public float ViewRadius
    {
        get { return _viewRadius; }
    }
    public bool CanAttack
    {
        get { return attackTimer >= _tba; }
    }

    void Awake()
    {
        sensor = GetComponent<AgentSensor>();
        vision = GetComponent<VisionSensor>();
        feedback = GetComponent<AgentFeedback>();
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();
    }

    void Start()
    {
        Search = new SearchP2(this);
        Attack = new AttackP2(this);
        Gather = new GatherP2(this);
        Flee = new FleeP2(this);
        Exit = new ExitP2(this);
        Dead = new DeadP2(this);

        fsm.AddState(Search);
        fsm.AddState(Attack);
        fsm.AddState(Gather);
        fsm.AddState(Flee);
        fsm.AddState(Exit);
        fsm.AddState(Dead);

        if (NavGraph.Instance != null)
        {
            transform.position = NavGraph.Instance.SpawnNode.position + Vector3.up * 0.9f;
        }

        fsm.ChangeState(Search);
    }

    void Update()
    {
        attackTimer += Time.deltaTime;
        previous = transform.position;
        fsm.Update();
        Velocity = (transform.position - previous) / Mathf.Max(Time.deltaTime, 0.0001f);

        UpdateFeedback();

        mover.DebugDraw(Color.green);
    }

    void UpdateFeedback()
    {
        if (fsm.CurrentState == Dead)
        {
            SetColor(Color.black);
        }
        else if (fsm.CurrentState == Flee)
        {
            SetColor(Color.magenta);
        }
        else if (fsm.CurrentState == Exit)
        {
            SetColor(Color.yellow);
        }
        else if (fsm.CurrentState == Gather)
        {
            SetColor(Color.cyan);
        }
        else if (fsm.CurrentState == Attack)
        {
            SetColor(Color.red);
        }
        else
        {
            SetColor(Color.white);
        }

    }

    void UpdatePerception()
    {
        int count = sensor.Scan(_viewRadius);
        if (sensor.ScanCount == lastPerceptionScan) return;
        lastPerceptionScan = sensor.ScanCount;
        wolfVisible = false;
        nearestAlive = null;
        nearestDead = null;
        float aliveDistance = _viewRadius * _viewRadius;
        float deadDistance = aliveDistance;

        for (int i = 0; i < count; i++)
        {
            Collider item = sensor.GetCollider(i);
            if (item == null) continue;

            WolfAgent wolf = item.GetComponentInParent<WolfAgent>();
            if (wolf != null && CanSee(item.bounds.center, wolf.transform)) wolfVisible = true;

            Alien boid = item.GetComponentInParent<Alien>();
            if (boid == null || boid.IsCollected) continue;
            Vector3 difference = boid.transform.position - transform.position;
            float distance = difference.sqrMagnitude;
            if (!CanSee(item.bounds.center, boid.transform)) continue;

            if (boid.IsDead && distance < deadDistance)
            {
                nearestDead = boid;
                deadDistance = distance;
            }
            else if (!boid.IsDead && distance < aliveDistance)
            {
                nearestAlive = boid;
                aliveDistance = distance;
            }
        }
    }

    public bool DetectWolf()
    {
        UpdatePerception();
        return wolfVisible;
    }

    bool CanSee(Vector3 targetPosition, Transform target)
    {
        return vision.CanSee(targetPosition - Vector3.up * 0.4f, _viewRadius);
    }

    public bool CanSeeTarget(Vector3 position)
    {
        return vision.CanSee(position, _viewRadius);
    }

    public void CapturedByWolf()
    {
        if (fsm.CurrentState != Dead)
        {
            fsm.ChangeState(Dead);
        }
    }

    public void ResetCycle()
    {
        GatherCount = 0;
        attackTimer = 999f;
        SetVisible(true);
        transform.position = NavGraph.Instance.SpawnNode.position + Vector3.up * 0.9f;
        fsm.ChangeState(Search);
    }

    public void SetVisible(bool visible)
    {
        _visible = visible;

        foreach (Renderer item in renderers)
        {
            if (item != null)
            {
                item.enabled = visible;
            }
        }

        foreach (Collider item in colliders)
        {
            if (item != null)
            {
                item.enabled = visible;
            }
        }
    }

    public void AddGather()
    {
        GatherCount++;
        Debug.Log($"Cazador: {GatherCount}/{RequiredGathers} recolecciones");
    }

    public void ResetAttackCooldown()
    {
        attackTimer = 0f;
    }

    public void SpawnObject()
    {
        spawnTimer += Time.deltaTime;
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            if (spawned[i] == null)
            {
                spawned.RemoveAt(i);
            }
        }

        if (spawnTimer < _spawnInterval || spawned.Count >= 5 || _interestPrefab == null)
        {
            return;
        }

        List<NavGraph.Node> nodes = NavGraph.Instance.HuntingNodes;

        if (nodes.Count == 0)
        {
            return;
        }

        spawnTimer = 0f;
        int index = Random.Range(0, nodes.Count);
        Vector3 position = nodes[index].position;
        ObjectOfInterest interest = Instantiate(_interestPrefab, position, Quaternion.identity);
        spawned.Add(interest);
    }

    public Alien NearestBoid(bool dead)
    {
        UpdatePerception();
        if (dead) return nearestDead;
        return nearestAlive;
    }

    public void Navigate(Vector3 destination)
    {
        mover.Set(transform.position, destination);
    }

    public bool FollowPath()
    {
        return mover.Move(transform, _speed, _rotationSpeed);
    }

    public bool MoveDirect(Vector3 destination, float stopping = 0.8f)
    {
        Vector3 direction = destination - transform.position;
        direction.y = 0f;

        if (direction.magnitude <= stopping)
        {
            return true;
        }

        float distance = Mathf.Min(_speed * Time.deltaTime, direction.magnitude);
        transform.position += direction.normalized * distance;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), _rotationSpeed * Time.deltaTime);
        return false;
    }

    public void Shoot(Alien target)
    {
        if (_bulletPrefab == null || _spawnBulletPoint == null)
        {
            target.TakeDamage(10f);
            return;
        }

        Vector3 direction = (target.transform.position - _spawnBulletPoint.position).normalized;
        Bullet bullet = Instantiate(_bulletPrefab, _spawnBulletPoint.position, Quaternion.LookRotation(direction));
        bullet.Init(direction, _bulletSpeed, 10f);
    }

    public void SetColor(Color color)
    {
        feedback.SetColor(color);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, _viewRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _meleeRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _rangeAttackRadius);
    }
}
