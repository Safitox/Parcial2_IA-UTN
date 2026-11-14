using System.Collections.Generic;
using UnityEngine;

public class WolfAgent : MonoBehaviour
{
    public enum Mode
    {
        Patrol,
        Pursue,
        Investigate
    }

    [SerializeField] float speed = 2f;
    [SerializeField] float viewDistance = 12f;
    [SerializeField] float viewAngle = 110f;
    [SerializeField] float captureRadius = 1.5f;
    [SerializeField] float searchDuration = 5f;
    [SerializeField] float alertDistance = 100f;
    [SerializeField] string wolfName = "Lobito";

    public Transform[] patrolPoints;
    public Mode CurrentMode { get; private set; }

    MoverEnGraph mover = new MoverEnGraph();
    List<Vector3> patrol = new List<Vector3>();
    HunterNPC trackedHunter;
    int waypoint;
    float searchTimer;
    bool searchingArea;
    float howlCooldown;
    float repathTimer;
    Vector3 lastKnown;
    AgentFeedback feedback;
    AgentSensor sensor;
    VisionSensor vision;
    int lastVisionScan = -1;
    HunterNPC visibleCache;
    [SerializeField] float spawnClearance = 12f;

    void Start()
    {
        sensor = GetComponent<AgentSensor>();
        vision = GetComponent<VisionSensor>();
        feedback = GetComponent<AgentFeedback>();

        if (patrolPoints != null)
        {
            foreach (Transform point in patrolPoints)
            {
                if (point != null)
                {
                    patrol.Add(point.position);
                }
            }
        }

        ReturnToPatrol();

        if (patrol.Count == 0)
        {
            Debug.LogWarning(name + " no tiene waypoints asignados.");
        }
    }

    HunterNPC FindVisibleHunter()
    {
        if (NavGraph.Instance == null)
        {
            return null;
        }

        int count = sensor.Scan(viewDistance);
        if (sensor.ScanCount == lastVisionScan)
        {
            if (visibleCache == null || visibleCache.IsInactive) return null;
            if (NavGraph.Instance.IsSafe(visibleCache.transform.position)) return null;
            return visibleCache;
        }

        lastVisionScan = sensor.ScanCount;
        visibleCache = null;

        for (int i = 0; i < count; i++)
        {
            Collider item = sensor.GetCollider(i);
            if (item == null) continue;
            HunterNPC hunter = item.GetComponentInParent<HunterNPC>();

            if (hunter == null || hunter.IsInactive)
            {
                continue;
            }

            if (NavGraph.Instance.IsSafe(hunter.transform.position))
            {
                continue;
            }

            if (NavGraph.Instance.SpawnNode != null && Vector3.Distance(hunter.transform.position, NavGraph.Instance.SpawnNode.position) < spawnClearance)
            {
                continue;
            }

            if (vision.CanSee(hunter.transform.position, viewDistance, viewAngle))
            {
                visibleCache = hunter;
                return hunter;
            }
        }

        return null;
    }

    public bool CanSeeHunter()
    {
        return FindVisibleHunter() != null;
    }

    void SendAlert()
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, alertDistance, 1 << 9, QueryTriggerInteraction.Ignore);
        HashSet<WolfAgent> alerted = new HashSet<WolfAgent>();

        foreach (Collider item in nearby)
        {
            WolfAgent other = item.GetComponentInParent<WolfAgent>();

            if (other == null || other == this || alerted.Contains(other))
            {
                continue;
            }

            alerted.Add(other);
            other.HearAlert(lastKnown);
        }

        Debug.Log("LOBO " + wolfName + ": AULLIDO -> " + lastKnown);
    }

    public void HearAlert(Vector3 position)
    {
        if (NavGraph.Instance == null || NavGraph.Instance.IsSafe(position))
        {
            return;
        }

        if (NavGraph.Instance.SpawnNode != null && Vector3.Distance(position, NavGraph.Instance.SpawnNode.position) < spawnClearance)
        {
            return;
        }

        trackedHunter = null;
        lastKnown = position;
        searchTimer = searchDuration;
        searchingArea = false;
        repathTimer = 0f;
        CurrentMode = Mode.Investigate;
        Debug.Log(name + " recibio alerta: " + lastKnown);

        if (!mover.Set(transform.position, lastKnown, true))
        {
            ReturnToPatrol();
        }
    }

    void Update()
    {
        if (NavGraph.Instance == null) return;
        howlCooldown -= Time.deltaTime;
        repathTimer -= Time.deltaTime;
        CheckHunter();
        UpdateMovement();
        UpdateFeedback();
    }

    void CheckHunter()
    {
        if (trackedHunter != null && (trackedHunter.IsInactive || NavGraph.Instance.IsSafe(trackedHunter.transform.position)))
        {
            ReturnToPatrol();
        }

        HunterNPC visibleHunter = FindVisibleHunter();
        if (visibleHunter != null)
        {
            ChaseHunter(visibleHunter);
        }
        else if (CurrentMode == Mode.Pursue)
        {
            trackedHunter = null;
            CurrentMode = Mode.Investigate;
            searchTimer = searchDuration;
            searchingArea = false;
            if (!mover.Set(transform.position, lastKnown, true)) ReturnToPatrol();
        }
    }

    void UpdateMovement()
    {
        switch (CurrentMode)
        {
            case Mode.Patrol:
                UpdatePatrol();
                break;

            case Mode.Pursue:
                mover.Move(transform, speed);
                break;

            case Mode.Investigate:
                UpdateInvestigation();
                break;
        }
    }

    void ChaseHunter(HunterNPC hunter)
    {
        trackedHunter = hunter;
        lastKnown = hunter.transform.position;

        if (CurrentMode != Mode.Pursue && howlCooldown <= 0f)
        {
            howlCooldown = 3f;
            SendAlert();
        }

        CurrentMode = Mode.Pursue;

        if (repathTimer <= 0f)
        {
            mover.Set(transform.position, lastKnown, true);
            repathTimer = 0.45f;
        }

        if (Vector3.Distance(transform.position, hunter.transform.position) < captureRadius)
        {
            hunter.CapturedByWolf();
        }
    }

    void UpdatePatrol()
    {
        if (patrol.Count == 0)
        {
            return;
        }

        if (mover.Move(transform, speed))
        {
            waypoint++;

            if (waypoint >= patrol.Count)
            {
                waypoint = 0;
            }

            mover.Set(transform.position, patrol[waypoint], true);
        }
    }

    void UpdateInvestigation()
    {
        bool reached = mover.Move(transform, speed);

        if (!searchingArea)
        {
            if (reached)
            {
                searchingArea = true;
                ChooseSearchPoint();
            }

            return;
        }

        searchTimer -= Time.deltaTime;

        if (searchTimer <= 0f)
        {
            ReturnToPatrol();
        }
        else if (reached)
        {
            ChooseSearchPoint();
        }
    }

    void ChooseSearchPoint()
    {
        List<NavGraph.Node> nearby = new List<NavGraph.Node>();

        foreach (NavGraph.Node node in NavGraph.Instance.Nodes)
        {
            if (node.zone == NavGraph.Zone.Safe || node.zone == NavGraph.Zone.Initial) continue;
            if (NavGraph.Instance.SpawnNode != null && Vector3.Distance(node.position, NavGraph.Instance.SpawnNode.position) < spawnClearance) continue;
            float distance = Vector3.Distance(node.position, lastKnown);
            if (distance >= 2f && distance <= 12f) nearby.Add(node);
        }

        while (nearby.Count > 0)
        {
            int selected = Random.Range(0, nearby.Count);
            Vector3 destination = nearby[selected].position;
            if (mover.Set(transform.position, destination, true)) return;
            nearby.RemoveAt(selected);
        }

        ReturnToPatrol();
    }

    void ReturnToPatrol()
    {
        CurrentMode = Mode.Patrol;
        trackedHunter = null;
        searchingArea = false;
        lastKnown = Vector3.zero;

        if (patrol.Count > 0)
        {
            mover.Set(transform.position, patrol[waypoint], true);
        }
    }

    void UpdateFeedback()
    {
        if (CurrentMode == Mode.Patrol)
        {
            mover.DebugDraw(Color.gray);
        }
        else
        {
            mover.DebugDraw(Color.red);
        }

        switch (CurrentMode)
        {
            case Mode.Pursue:
                feedback.SetColor(Color.red);
                break;

            case Mode.Investigate:
                feedback.SetColor(Color.yellow);
                break;

            default:
                feedback.SetColor(new Color(0.6f, 0.2f, 0.1f));
                break;
        }

        if (CurrentMode != Mode.Patrol)
        {
            Debug.DrawLine(transform.position + Vector3.up, lastKnown + Vector3.up, Color.magenta);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Vector3 left = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;

        Gizmos.DrawRay(transform.position, left * viewDistance);
        Gizmos.DrawRay(transform.position, right * viewDistance);

        if (CurrentMode != Mode.Patrol)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(lastKnown, 0.3f);
        }
    }
}
