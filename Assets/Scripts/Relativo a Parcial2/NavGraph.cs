using System.Collections.Generic;
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
    [SerializeField] LayerMask obstacles = 1 << 8;
    [SerializeField] bool showGraph = true;

    [Header("Zonas (coordenadas locales X/Z)")]
    [SerializeField] Vector2 huntingCenter = new Vector2(0f, 12f);
    [SerializeField] Vector2 huntingSize = new Vector2(20f, 20f);
    [SerializeField] Vector2 initialCenter = new Vector2(-24f, 0f);
    [SerializeField] Vector2 exitCenter = new Vector2(24f, 0f);
    [SerializeField] Vector2 safeCenterA = new Vector2(-24f, -20f);
    [SerializeField] Vector2 safeCenterB = new Vector2(24f, 20f);
    [SerializeField] Vector2 safeSize = new Vector2(12f, 5f);

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
                Vector3 check = position + Vector3.up * obstacleHeight;

                if (Physics.CheckBox(check, new Vector3(cellSize * 0.16f, 0.35f, cellSize * 0.16f), Quaternion.identity, obstacles, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                Node node = new Node();
                node.position = position;
                node.zone = GetZone(local);
                Nodes.Add(node);

                if (node.zone == Zone.Hunting) HuntingNodes.Add(node);
                if (node.zone == Zone.Safe) SafeNodes.Add(node);
                if (node.zone == Zone.Initial) SpawnNode = node;
                if (node.zone == Zone.Exit) ExitNode = node;
            }
        }

        if (Application.isPlaying && (SpawnNode == null || ExitNode == null || HuntingNodes.Count == 0 || SafeNodes.Count < 2))
        {
            Debug.LogError("Grafo: revisar zonas de inicio, coto, salida y zonas seguras.");
        }

        for (int i = 0; i < Nodes.Count; i++)
        {
            for (int j = i + 1; j < Nodes.Count; j++)
            {
                Node first = Nodes[i];
                Node second = Nodes[j];
                float distance = Vector3.Distance(first.position, second.position);

                if (distance > cellSize * 1.05f) continue;

                Vector3 start = first.position + Vector3.up * obstacleHeight;
                Vector3 end = second.position + Vector3.up * obstacleHeight;

                if (!Physics.Linecast(start, end, obstacles, QueryTriggerInteraction.Ignore))
                {
                    first.neighbors.Add(second);
                    second.neighbors.Add(first);
                }
            }
        }
    }

    Zone GetZone(Vector3 local)
    {
        Vector2 point = new Vector2(local.x, local.z);
        if (InArea(point, initialCenter, Vector2.one * cellSize)) return Zone.Initial;
        if (InArea(point, exitCenter, Vector2.one * cellSize)) return Zone.Exit;
        if (InArea(point, safeCenterA, safeSize) || InArea(point, safeCenterB, safeSize)) return Zone.Safe;
        if (InArea(point, huntingCenter, huntingSize)) return Zone.Hunting;
        return Zone.Normal;
    }

    bool InArea(Vector2 point, Vector2 center, Vector2 size)
    {
        return Mathf.Abs(point.x - center.x) <= size.x * 0.5f && Mathf.Abs(point.y - center.y) <= size.y * 0.5f;
    }

    public bool IsSafe(Vector3 position)
    {
        Vector3 local = transform.InverseTransformPoint(position);
        Vector2 point = new Vector2(local.x, local.z);
        return InArea(point, safeCenterA, safeSize) || InArea(point, safeCenterB, safeSize);
    }

    public bool InHunting(Vector3 position)
    {
        Vector3 local = transform.InverseTransformPoint(position);
        return InArea(new Vector2(local.x, local.z), huntingCenter, huntingSize);
    }

    public Vector3 ConfineToHunting(Vector3 position, ref Vector3 velocity)
    {
        Vector3 local = transform.InverseTransformPoint(position);
        float x = Mathf.Clamp(local.x, huntingCenter.x - huntingSize.x * 0.5f + 0.2f, huntingCenter.x + huntingSize.x * 0.5f - 0.2f);
        float z = Mathf.Clamp(local.z, huntingCenter.y - huntingSize.y * 0.5f + 0.2f, huntingCenter.y + huntingSize.y * 0.5f - 0.2f);
        if (x != local.x) velocity.x = -velocity.x;
        if (z != local.z) velocity.z = -velocity.z;
        return transform.TransformPoint(new Vector3(x, local.y, z));
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
                case Zone.Hunting: Gizmos.color = Color.green; break;
                case Zone.Safe: Gizmos.color = Color.cyan; break;
                case Zone.Initial: Gizmos.color = Color.magenta; break;
                case Zone.Exit: Gizmos.color = Color.yellow; break;
                default: Gizmos.color = Color.gray; break;
            }

            Gizmos.DrawSphere(node.position + Vector3.up * 0.15f, 0.23f);
            Gizmos.color = Color.blue;
            foreach (Node next in node.neighbors) Gizmos.DrawLine(node.position + Vector3.up * 0.15f, next.position + Vector3.up * 0.15f);
        }
    }
}
