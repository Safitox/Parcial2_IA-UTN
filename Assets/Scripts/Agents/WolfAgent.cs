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
    Renderer display;

    void Start()
    {
        display = GetComponent<Renderer>();

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

    // Devuelve el Cazador solo si realmente está dentro del campo de visión.
    HunterNPC FindVisibleHunter()
    {
        if (NavGraph.Instance == null)
        {
            return null;
        }

        Collider[] nearby = Physics.OverlapSphere(transform.position, viewDistance, -1, QueryTriggerInteraction.Ignore);

        foreach (Collider item in nearby)
        {
            HunterNPC hunter = item.GetComponentInParent<HunterNPC>();

            if (hunter == null || hunter.IsInactive)
            {
                continue;
            }

            if (NavGraph.Instance.IsSafe(hunter.transform.position))
            {
                continue;
            }

            Vector3 direction = hunter.transform.position - transform.position;
            direction.y = 0f;
            float distance = direction.magnitude;

            if (distance > viewDistance || distance < 0.001f)
            {
                continue;
            }

            if (Vector3.Angle(transform.forward, direction) > viewAngle / 2f)
            {
                continue;
            }

            if (HasLineOfSight(hunter))
            {
                return hunter;
            }
        }

        return null;
    }

    bool HasLineOfSight(HunterNPC hunter)
    {
        Vector3 origin = transform.position + Vector3.up * 0.4f;
        Vector3 destination = hunter.transform.position + Vector3.up * 0.4f;
        Vector3 direction = destination - origin;
        float distance = direction.magnitude;

        RaycastHit[] hits = Physics.RaycastAll(origin, direction.normalized, distance, -1, QueryTriggerInteraction.Ignore);
        float nearestDistance = float.MaxValue;
        Collider nearestCollider = null;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<WolfAgent>() == this)
            {
                continue;
            }

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearestCollider = hit.collider;
            }
        }

        if (nearestCollider == null)
        {
            return true;
        }

        return nearestCollider.GetComponentInParent<HunterNPC>() == hunter;
    }

    public bool CanSeeHunter()
    {
        return FindVisibleHunter() != null;
    }

    void SendAlert()
    {
        Collider[] nearby = Physics.OverlapSphere(transform.position, alertDistance, -1, QueryTriggerInteraction.Ignore);
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

        // La alerta transmite una posición, no una referencia permanente al Cazador.
        trackedHunter = null;
        lastKnown = position;
        searchTimer = searchDuration;
        searchingArea = false;
        repathTimer = 0f;
        CurrentMode = Mode.Investigate;

        if (!mover.Set(transform.position, lastKnown, true))
        {
            ReturnToPatrol();
        }
    }

    void Update()
    {
        if (NavGraph.Instance == null)
        {
            return;
        }

        howlCooldown -= Time.deltaTime;
        repathTimer -= Time.deltaTime;

        // Solo se conserva esta referencia durante una persecución directa.
        // Se usa para cancelar la persecución cuando el objetivo desaparece o está a salvo.
        if (trackedHunter != null)
        {
            if (trackedHunter.IsInactive || NavGraph.Instance.IsSafe(trackedHunter.transform.position))
            {
                ReturnToPatrol();
            }
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

            if (!mover.Set(transform.position, lastKnown, true))
            {
                ReturnToPatrol();
            }
        }

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

        UpdateFeedback();
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
            if (node.zone == NavGraph.Zone.Safe || node.zone == NavGraph.Zone.Initial)
            {
                continue;
            }

            float distance = Vector3.Distance(node.position, lastKnown);

            if (distance > 12f || distance < 2f)
            {
                continue;
            }

            List<NavGraph.Node> path = NavGraph.Instance.FindPath(transform.position, node.position, true);

            if (path.Count > 1)
            {
                nearby.Add(node);
            }
        }

        if (nearby.Count == 0)
        {
            ReturnToPatrol();
            return;
        }

        int selected = Random.Range(0, nearby.Count);
        mover.Set(transform.position, nearby[selected].position, true);
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

        if (display != null)
        {
            switch (CurrentMode)
            {
                case Mode.Pursue:
                    display.material.color = Color.red;
                    break;

                case Mode.Investigate:
                    display.material.color = Color.yellow;
                    break;

                default:
                    display.material.color = new Color(0.6f, 0.2f, 0.1f);
                    break;
            }
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
