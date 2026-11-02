using UnityEngine;
using System.Collections.Generic;

public class Alien : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float _health = 10f;
    [SerializeField] private float _respawnTime = 3f;
    private float _respawnTimer;
    private bool _isDead = false;
    private bool _isCollected = false;
    public bool IsDead => _isDead;
    public bool IsCollected => _isCollected;

    [SerializeField] private float _maxSpeed = 5f;
    [SerializeField] private float _maxSteeringForce = 5f;
    [SerializeField] private float _arriveRadius = 3f;
    [SerializeField] private float _stopRadius = 0.5f;
    [SerializeField] private float _viewRadius = 5f;

    [Header("Objects Of Interest")]
    [SerializeField] private float _objectOfInterestDamage = 1f;
    [SerializeField] private float _objectOfInterestDamageInterval = 1f;
    [SerializeField] private float _distSquaredToobjectOfInterestDetection = 1.5f;
    private float _objectOfInterestDamageTimer = 0f;

    [Header("Flocking")]
    [SerializeField] private float _separationRadius = 4f;
    [SerializeField, Range(0f, 3f)] private float _separationWeight = 0.8f;
    [SerializeField, Range(0f, 3f)] private float _cohesionWeight = 1f;
    [SerializeField, Range(0f, 3f)] private float _alignmentWeight = 1f;

    private ObjectOfInterest _currentTarget;
    private readonly List<Alien> _nearbyAgents = new List<Alien>();
    private HunterNPC _nearbyHunter;
    private bool _hunterDetected;

    private Renderer[] _renderers;
    private Collider[] _colliders;
    private Vector3 _velocity;
    private float _movementHeight;
    public Vector3 Velocity => _velocity;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _colliders = GetComponentsInChildren<Collider>();
    }

    private void Start()
    {
        _movementHeight = transform.position.y;
        Vector3 randomVector = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        _velocity += randomVector.normalized * _maxSpeed;
    }

    private void Update()
    {
        if (_isDead)
        {
            ChangeVisualFeedback();

            if (_isCollected)
            {
                _respawnTimer += Time.deltaTime;

                if (_respawnTimer >= _respawnTime)
                {
                    Respawn();
                }
            }

            return;
        }

        _respawnTimer = 0f;
        DetectNearbyObjects();

        if (_hunterDetected)
        {
            _currentTarget = null;
            _objectOfInterestDamageTimer = 0f;
            _velocity += CalculateEvade(_nearbyHunter);
        }
        else
        {
            if (_currentTarget != null)
            {
                Vector3 difference = _currentTarget.transform.position - transform.position;
                float distanceSquared = difference.sqrMagnitude;
                float damageRadiusSquared = _distSquaredToobjectOfInterestDetection * _distSquaredToobjectOfInterestDetection;

                _velocity += CalculateArrive(_currentTarget.transform.position);
                _velocity += CalculateSeparation(_nearbyAgents, _separationRadius) * _separationWeight;

                if (distanceSquared < damageRadiusSquared)
                {
                    _objectOfInterestDamageTimer -= Time.deltaTime;

                    if (_objectOfInterestDamageTimer <= 0f)
                    {
                        _currentTarget.TakeDamage(_objectOfInterestDamage);
                        _objectOfInterestDamageTimer = _objectOfInterestDamageInterval;
                    }
                }
                else
                {
                    _objectOfInterestDamageTimer = 0f;
                }
            }
            else
            {
                CalculateFlocking();
            }
        }

        // Los Boids se mueven solo en X y Z.
        _velocity.y = 0f;
        _velocity = Vector3.ClampMagnitude(_velocity, _maxSpeed);
        Vector3 nextPosition = transform.position + _velocity * Time.deltaTime;
        nextPosition.y = _movementHeight;

        if (NavGraph.Instance != null)
        {
            transform.position = ConfineToHuntingArea(nextPosition);
        }
        else if (Universe.Instance != null)
        {
            transform.position = Universe.Instance.CalculateLimitPosition(nextPosition);
        }
        else
        {
            transform.position = nextPosition;
        }

        Vector3 correctedPosition = transform.position;
        correctedPosition.y = _movementHeight;
        transform.position = correctedPosition;

        if (_velocity.sqrMagnitude > 0.001f)
        {
            transform.forward = _velocity.normalized;
        }

        ChangeVisualFeedback();
    }

    private Vector3 CalculateSteering(Vector3 desired)
    {
        desired.y = 0f;
        Vector3 steering = desired - _velocity;
        steering.y = 0f;
        steering = Vector3.ClampMagnitude(steering, _maxSteeringForce);
        return steering * Time.deltaTime;
    }

    private void CalculateFlocking()
    {
        Vector3 separationSum = Vector3.zero;
        Vector3 alignmentSum = Vector3.zero;
        Vector3 cohesionPositionSum = Vector3.zero;
        int separationCount = 0;
        int flockingCount = 0;
        float separationRadiusSquared = _separationRadius * _separationRadius;
        float viewRadiusSquared = _viewRadius * _viewRadius;

        foreach (Alien agent in _nearbyAgents)
        {
            if (agent == null || agent == this || agent.IsDead)
            {
                continue;
            }

            Vector3 difference = agent.transform.position - transform.position;
            float distanceSquared = difference.sqrMagnitude;

            if (distanceSquared <= separationRadiusSquared)
            {
                separationSum += difference;
                separationCount++;
            }

            if (distanceSquared <= viewRadiusSquared)
            {
                alignmentSum += agent.Velocity;
                cohesionPositionSum += agent.transform.position;
                flockingCount++;
            }
        }

        Vector3 separationSteering = Vector3.zero;
        Vector3 alignmentSteering = Vector3.zero;
        Vector3 cohesionSteering = Vector3.zero;

        if (separationCount > 0)
        {
            Vector3 averageSeparation = separationSum / separationCount;
            separationSteering = CalculateSteering(-averageSeparation.normalized * _maxSpeed);
        }

        if (flockingCount > 0)
        {
            Vector3 averageVelocity = alignmentSum / flockingCount;
            alignmentSteering = CalculateSteering(averageVelocity.normalized * _maxSpeed);
            Vector3 centerPosition = cohesionPositionSum / flockingCount;
            cohesionSteering = CalculateSeek(centerPosition);
        }

        _velocity += separationSteering * _separationWeight;
        _velocity += alignmentSteering * _alignmentWeight;
        _velocity += cohesionSteering * _cohesionWeight;
    }

    private Vector3 CalculateSeparation(IEnumerable<Alien> agents, float radius)
    {
        Vector3 desired = Vector3.zero;
        int count = 0;

        foreach (Alien agent in agents)
        {
            if (agent == null || agent == this || agent.IsDead)
            {
                continue;
            }

            if (InRange(agent.transform.position, radius))
            {
                desired += agent.transform.position - transform.position;
                count++;
            }
        }

        if (count == 0)
        {
            return Vector3.zero;
        }

        desired /= count;
        return CalculateSteering(-desired.normalized * _maxSpeed);
    }

    private bool InRange(Vector3 position, float radius)
    {
        Vector3 difference = position - transform.position;
        return difference.sqrMagnitude <= radius * radius;
    }

    private Vector3 CalculateSeek(Vector3 targetPosition)
    {
        Vector3 desired = targetPosition - transform.position;
        desired.y = 0f;
        desired = desired.normalized * _maxSpeed;
        return CalculateSteering(desired);
    }

    private Vector3 CalculateFlee(Vector3 targetPosition)
    {
        Vector3 desired = targetPosition - transform.position;
        desired.y = 0f;
        desired = desired.normalized * _maxSpeed;
        return CalculateSteering(-desired);
    }

    private Vector3 CalculateArrive(Vector3 targetPosition)
    {
        Vector3 toTarget = targetPosition - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        if (distance < _stopRadius)
        {
            _velocity *= 0.8f;

            if (_velocity.magnitude < 0.05f)
            {
                _velocity = Vector3.zero;
            }

            return Vector3.zero;
        }

        float speed = _maxSpeed;

        if (distance < _arriveRadius)
        {
            float factor = distance / _arriveRadius;
            speed *= factor * factor;
        }

        Vector3 desired = toTarget.normalized * speed;
        return CalculateSteering(desired);
    }

    private Vector3 CalculateEvade(HunterNPC target)
    {
        return CalculateFlee(GetFuturePosition(target));
    }

    private Vector3 GetFuturePosition(HunterNPC target)
    {
        float distance = Vector3.Distance(target.transform.position, transform.position);
        float predictedTime = distance / (_maxSpeed + target.Velocity.magnitude);
        return target.transform.position + target.Velocity * predictedTime;
    }

    private Vector3 ConfineToHuntingArea(Vector3 position)
    {
        float minX = NavGraph.Position(5, 6).x - 1.8f;
        float maxX = NavGraph.Position(9, 6).x + 1.8f;
        float minZ = NavGraph.Position(5, 6).z - 1.8f;
        float maxZ = NavGraph.Position(5, 10).z + 1.8f;

        if (position.x < minX || position.x > maxX)
        {
            _velocity.x = -_velocity.x;
        }

        if (position.z < minZ || position.z > maxZ)
        {
            _velocity.z = -_velocity.z;
        }

        float x = Mathf.Clamp(position.x, minX, maxX);
        float z = Mathf.Clamp(position.z, minZ, maxZ);
        return new Vector3(x, _movementHeight, z);
    }

    public void TakeDamage(float damage)
    {
        if (_isDead)
        {
            return;
        }

        _health -= damage;

        if (_health <= 0f)
        {
            _velocity = Vector3.zero;
            _respawnTimer = 0f;
            _isDead = true;
        }
    }

    public void Collect()
    {
        if (!_isDead || _isCollected)
        {
            return;
        }

        _isCollected = true;
        _respawnTimer = 0f;
        _velocity = Vector3.zero;

        foreach (Renderer renderer in _renderers)
        {
            renderer.enabled = false;
        }

        foreach (Collider collider in _colliders)
        {
            collider.enabled = false;
        }
    }

    private void DetectNearbyObjects()
    {
        _nearbyAgents.Clear();
        _nearbyHunter = null;
        _hunterDetected = false;
        _currentTarget = null;

        float radius = Mathf.Max(_viewRadius, _separationRadius);
        Collider[] nearby = Physics.OverlapSphere(transform.position, radius, -1, QueryTriggerInteraction.Collide);
        float closestObjectDistance = _viewRadius * _viewRadius;
        float closestHunterDistance = _viewRadius * _viewRadius;

        foreach (Collider collider in nearby)
        {
            Alien agent = collider.GetComponentInParent<Alien>();

            if (agent != null && agent != this && !agent.IsDead)
            {
                if (!_nearbyAgents.Contains(agent))
                {
                    _nearbyAgents.Add(agent);
                }
            }

            HunterNPC hunter = collider.GetComponentInParent<HunterNPC>();

            if (hunter != null && !hunter.IsInactive)
            {
                Vector3 difference = hunter.transform.position - transform.position;
                float distance = difference.sqrMagnitude;

                if (distance < closestHunterDistance)
                {
                    closestHunterDistance = distance;
                    _nearbyHunter = hunter;
                    _hunterDetected = true;
                }
            }

            ObjectOfInterest interest = collider.GetComponentInParent<ObjectOfInterest>();

            if (interest != null)
            {
                Vector3 difference = interest.transform.position - transform.position;
                float distance = difference.sqrMagnitude;

                if (distance < closestObjectDistance)
                {
                    closestObjectDistance = distance;
                    _currentTarget = interest;
                }
            }
        }
    }

    private void Respawn()
    {
        _isDead = false;
        _isCollected = false;
        _health = 10f;
        _respawnTimer = 0f;
        Vector3 randomPosition;

        if (NavGraph.Instance != null && NavGraph.Instance.HuntingNodes.Count > 0)
        {
            int index = Random.Range(0, NavGraph.Instance.HuntingNodes.Count);
            randomPosition = NavGraph.Instance.HuntingNodes[index].position;
        }
        else if (Universe.Instance != null)
        {
            Vector3 position = new Vector3(Random.Range(-10f, 10f), _movementHeight, Random.Range(-10f, 10f));
            randomPosition = Universe.Instance.CalculateLimitPosition(position);
        }
        else
        {
            randomPosition = transform.position;
        }

        randomPosition.y = _movementHeight;
        transform.position = randomPosition;
        Vector3 direction = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        _velocity = direction.normalized * _maxSpeed;

        foreach (Renderer renderer in _renderers)
        {
            renderer.enabled = true;
        }

        foreach (Collider collider in _colliders)
        {
            collider.enabled = true;
        }
    }

    private void ChangeVisualFeedback()
    {
        if (_isDead)
        {
            SetColor(Color.black);
        }
        else if (_currentTarget != null)
        {
            SetColor(Color.green);
        }
        else if (_hunterDetected)
        {
            SetColor(Color.red);
        }
        else
        {
            SetColor(Color.blue);
        }
    }

    private void SetColor(Color color)
    {
        foreach (Renderer renderer in _renderers)
        {
            renderer.material.color = color;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _separationRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _viewRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _arriveRadius);
    }
}
