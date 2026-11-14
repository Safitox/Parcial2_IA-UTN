using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class NavGraph : MonoBehaviour
{
    public static NavGraph Instance;

    public enum Zone { Normal, Initial, Hunting, Safe, Exit }

    public class Node
    {
        public Vector3 position;
        public Zone zone;
        public List<Node> neighbors = new List<Node>();
    }

    [Header("Grilla")]
    [SerializeField] int columns = 15;
    [SerializeField] int rows = 11;
    [SerializeField] float cellSize = 4f;
    [SerializeField] float obstacleHeight = 0.9f;
    [SerializeField] float agentRadius = 0.45f;
    [SerializeField] LayerMask obstacles = 1 << 8;
    [SerializeField] bool showGraph = true;

    [Header("Zonas de la escena")]
    [SerializeField] MapZone[] zones;

    public List<Node> Nodes = new List<Node>();
    public List<Node> SafeNodes = new List<Node>();
    public List<Node> HuntingNodes = new List<Node>();
    public Node SpawnNode;
    public Node ExitNode;



    void Awake()
    {
        Instance = this;
        BuildGraph();
    }

    public void BuildGraph()
    {
        Physics.SyncTransforms();
        Nodes.Clear();
        SafeNodes.Clear();
        HuntingNodes.Clear();
        SpawnNode = null;
        ExitNode = null;

        for (int z = 0; z < rows; z++)
        {
            for (int x = 0; x < columns; x++)
            {
                Vector3 local = new Vector3((x - (columns - 1) * 0.5f) * cellSize, 0f, (z - (rows - 1) * 0.5f) * cellSize);
                Vector3 position = transform.TransformPoint(local);
                Vector3 bottom = position + Vector3.up * agentRadius;
                Vector3 top = position + Vector3.up * Mathf.Max(obstacleHeight, agentRadius);

                if (Physics.CheckCapsule(bottom, top, agentRadius, obstacles, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                Node node = new Node();
                node.position = position;
                node.zone = GetZone(position);
                Nodes.Add(node);

                if (node.zone == Zone.Hunting) HuntingNodes.Add(node);
                if (node.zone == Zone.Safe) SafeNodes.Add(node);
                if (node.zone == Zone.Initial) SpawnNode = node;
                if (node.zone == Zone.Exit) ExitNode = node;
            }
        }

        if (Application.isPlaying && (SpawnNode == null || ExitNode == null || HuntingNodes.Count == 0 || SafeNodes.Count < 2))
        {
            Debug.LogError("Grafo: revisar las zonas asignadas en el Inspector.");
        }

        for (int i = 0; i < Nodes.Count; i++)
        {
            for (int j = i + 1; j < Nodes.Count; j++)
            {
                Node first = Nodes[i];
                Node second = Nodes[j];
                float distance = Vector3.Distance(first.position, second.position);

                if (distance > cellSize * 1.05f) continue;

                if (CanMove(first.position, second.position, agentRadius))
                {
                    first.neighbors.Add(second);
                    second.neighbors.Add(first);
                }
            }
        }
    }

    public bool CanMove(Vector3 origin, Vector3 destination, float radius = 0.4f)
    {
        Vector3 direction = destination - origin;
        direction.y = 0f;
        float distance = direction.magnitude;

        Vector3 bottom = origin + Vector3.up * radius;
        Vector3 top = origin + Vector3.up * Mathf.Max(obstacleHeight, radius);
        Vector3 endBottom = destination + Vector3.up * radius;
        Vector3 endTop = destination + Vector3.up * Mathf.Max(obstacleHeight, radius);

        if (Physics.CheckCapsule(bottom, top, radius, obstacles, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (Physics.CheckCapsule(endBottom, endTop, radius, obstacles, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        if (distance <= 0.001f)
        {
            return true;
        }

        RaycastHit hit;
        bool blocked = Physics.CapsuleCast(bottom, top, radius, direction.normalized, out hit, distance, obstacles, QueryTriggerInteraction.Ignore);
        return !blocked;
    }

    Zone GetZone(Vector3 position)
    {
        if (InsideZone(position, Zone.Initial)) return Zone.Initial;
        if (InsideZone(position, Zone.Exit)) return Zone.Exit;
        if (InsideZone(position, Zone.Safe)) return Zone.Safe;
        if (InsideZone(position, Zone.Hunting)) return Zone.Hunting;
        return Zone.Normal;
    }

    bool InsideZone(Vector3 position, Zone type)
    {
        if (zones == null) return false;

        foreach (MapZone area in zones)
        {
            if (area != null && area.zone == type && area.Contains(position))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsSafe(Vector3 position)
    {
        return InsideZone(position, Zone.Safe);
    }

    public bool InHunting(Vector3 position)
    {
        return InsideZone(position, Zone.Hunting);
    }

    public Vector3 ConfineToHunting(Vector3 position, ref Vector3 velocity)
    {
        if (zones == null) return position;

        foreach (MapZone area in zones)
        {
            if (area != null && area.zone == Zone.Hunting)
            {
                return area.Confine(position, ref velocity);
            }
        }

        return position;
    }

    public Node Nearest(Vector3 position, bool wolf = false)
    {
        Node nearest = null;
        float distance = float.MaxValue;

        foreach (Node node in Nodes)
        {
            if (wolf && (node.zone == Zone.Safe || node.zone == Zone.Initial)) continue;
            Vector3 difference = node.position - position;
            difference.y = 0f;
            if (difference.sqrMagnitude >= distance) continue;
            distance = difference.sqrMagnitude;
            nearest = node;
        }

        return nearest;
    }

    public List<Node> FindPath(Vector3 origin, Vector3 destination, bool wolf = false)
    {
        return Pathfinding.FindPath(this, origin, destination, wolf);
    }

    public float PathLength(List<Node> path)
    {
        if (path == null || path.Count == 0) return float.PositiveInfinity;
        float length = 0f;
        for (int i = 1; i < path.Count; i++) length += Vector3.Distance(path[i - 1].position, path[i].position);
        return length;
    }

    void OnDrawGizmosSelected()
    {
        if (!showGraph) return;
        if (!Application.isPlaying) BuildGraph();

        foreach (Node node in Nodes)
        {
            switch (node.zone)
            {
                case Zone.Hunting:
                    Gizmos.color = Color.green;
                    break;
                case Zone.Safe:
                    Gizmos.color = Color.cyan;
                    break;
                case Zone.Initial:
                    Gizmos.color = Color.magenta;
                    break;
                case Zone.Exit:
                    Gizmos.color = Color.yellow;
                    break;
                default:
                    Gizmos.color = Color.gray;
                    break;
            }

            Gizmos.DrawSphere(node.position + Vector3.up * 0.15f, 0.23f);
            Gizmos.color = Color.blue;

            foreach (Node next in node.neighbors)
            {
                Gizmos.DrawLine(node.position + Vector3.up * 0.15f, next.position + Vector3.up * 0.15f);
            }
        }
    }
}
