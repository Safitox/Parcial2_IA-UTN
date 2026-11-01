using System.Collections.Generic;
using UnityEngine;

public class WolfAgent : MonoBehaviour
{
    public static readonly List<WolfAgent> All = new List<WolfAgent>();

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
    [SerializeField] string wolfName = "Lobito";

    public Transform[] patrolPoints;
    public Mode CurrentMode { get; private set; }

    MoverEnGraph mover = new MoverEnGraph();
    List<Vector3> patrol = new List<Vector3>();
    int waypoint;
    float searchTimer;
    bool searchingArea;
    float howlCooldown;
    float repathTimer;
    Vector3 lastKnown;
    Renderer display;

    void OnEnable()
    {
        All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    void Start()
    {
        display = GetComponent<Renderer>();

        foreach (Transform point in patrolPoints)
        {
            if (point != null)
            {
                patrol.Add(point.position);
            }
        }

        CurrentMode = Mode.Patrol;

        if (patrol.Count > 0)
        {
            mover.Set(transform.position, patrol[0], true);
        }
        else
        {
            Debug.Log(name + " no tiene waypoints asignados.");
        }
    }

    public bool CanSeeHunter()
    {
        HunterNPC hunter = HunterNPC.Instance;

        if (hunter == null || hunter.IsInactive)
        {
            return false;
        }

        if (NavGraph.Instance.IsSafe(hunter.transform.position))
        {
            return false;
        }

        Vector3 direction = hunter.transform.position - transform.position;
        direction.y = 0;
        float distance = direction.magnitude;

        if (distance > viewDistance || distance < 0.001f)
        {
            return false;
        }

        if (Vector3.Angle(transform.forward, direction) > viewAngle / 2f)
        {
            return false;
        }

        Vector3 origin = transform.position + Vector3.up * 0.4f;
        RaycastHit hit;

        if (Physics.Raycast(origin, direction.normalized, out hit, distance, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<HunterNPC>() != null;
        }

        return true;
    }

    public void HearAlert(Vector3 position)
    {
        if (NavGraph.Instance.IsSafe(position))
        {
            return;
        }

        lastKnown = position;
        searchTimer = searchDuration;
        searchingArea = false;
        repathTimer = 0;
        CurrentMode = Mode.Investigate;
        if (!mover.Set(transform.position, lastKnown, true))
        {
            ReturnToPatrol();
        }
    }

    void Update()
    {
        HunterNPC hunter = HunterNPC.Instance;

        if (hunter == null || NavGraph.Instance == null)
        {
            return;
        }

        howlCooldown -= Time.deltaTime;
        repathTimer -= Time.deltaTime;

        if (hunter.IsInactive || NavGraph.Instance.IsSafe(hunter.transform.position))
        {
            if (CurrentMode != Mode.Patrol)
            {
                ReturnToPatrol();
            }
        }
        else if (CanSeeHunter())
        {
            lastKnown = hunter.transform.position;

            if (CurrentMode != Mode.Pursue && howlCooldown <= 0)
            {
                howlCooldown = 3f;
                Debug.Log("LOBO " + wolfName + ": AULLIDO -> " + lastKnown);

                foreach (WolfAgent other in All)
                {
                    if (other != this)
                    {
                        other.HearAlert(lastKnown);
                    }
                }
            }

            CurrentMode = Mode.Pursue;

            if (repathTimer <= 0)
            {
                mover.Set(transform.position, lastKnown, true);
                repathTimer = 0.45f;
            }

            if (Vector3.Distance(transform.position, hunter.transform.position) < captureRadius)
            {
                hunter.CapturedByWolf();
            }
        }
        else if (CurrentMode == Mode.Pursue)
        {
            CurrentMode = Mode.Investigate;
            searchTimer = searchDuration;
            searchingArea = false;
            if (!mover.Set(transform.position, lastKnown, true))
            {
                ReturnToPatrol();
            }
        }

        if (CurrentMode == Mode.Patrol)
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
        else
        {
            bool reached = mover.Move(transform, speed);

            if (CurrentMode == Mode.Investigate)
            {
                if (!searchingArea)
                {
                    if (reached)
                    {
                        searchingArea = true;
                        ChooseSearchPoint();
                    }
                }
                else
                {
                    searchTimer -= Time.deltaTime;

                    if (searchTimer <= 0)
                    {
                        ReturnToPatrol();
                    }
                    else if (reached)
                    {
                        ChooseSearchPoint();
                    }
                }
            }
        }

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

        Debug.DrawLine(transform.position + Vector3.up, lastKnown + Vector3.up, Color.magenta);
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
        searchingArea = false;
        lastKnown = Vector3.zero;

        if (patrol.Count > 0)
        {
            mover.Set(transform.position, patrol[waypoint], true);
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

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(lastKnown, 0.3f);
    }
}
