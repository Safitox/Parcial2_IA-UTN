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

    public List<Node> Nodes = new List<Node>();
    public List<Node> SafeNodes = new List<Node>();
    public List<Node> HuntingNodes = new List<Node>();
    public Node SpawnNode;
    public Node ExitNode;
    public float connectionDistance = 4.1f;
    public bool showGraph = true;

    // N = pasillo, I = inicio, H = coto, S = zona segura, E = salida, - = pared
    private string[] map = {
        "SSSN-NNNNN-NNNN",
        "NNNN-NNNNN-NNNN",
        "NNNNNNNNNN-NNNN",
        "NNNN-NNNNNNNNNN",
        "NNNN-NNNNN-NNNN",
        "NINN---N---NNEN",
        "NNNN-HHHHH-NNNN",
        "NNNN-HHHHH-NNNN",
        "NNNNNHHHHHNNNNN",
        "NNNN-HHHHH-NNNN",
        "NNNN-HHHHH-NSSS"
    };

    void Awake()
    {
        Instance = this;
        BuildGraph();
    }

    //Convierto coordenadas de la grilla en posiciones del mundo
    public static Vector3 Position(int x, int z)
    {
        return new Vector3((x - 7) * 4f, 0, (z - 5) * 4f);
    }
    void BuildGraph()
    {
        Nodes.Clear();
        SafeNodes.Clear();
        HuntingNodes.Clear();
        SpawnNode = null;
        ExitNode = null;

        for (int z = 0; z < map.Length; z++)
        {
            for (int x = 0; x < map[z].Length; x++)
            {
                char cell = map[z][x];

                if (cell == '-')
                    continue;

                Node node = new Node();
                node.position = Position(x, z);

                switch (cell)
                {
                    case 'I':
                        node.zone = Zone.Initial;
                        SpawnNode = node;
                        break;
                    case 'H':
                        node.zone = Zone.Hunting;
                        HuntingNodes.Add(node);
                        break;
                    case 'S':
                        node.zone = Zone.Safe;
                        SafeNodes.Add(node);
                        break;
                    case 'E':
                        node.zone = Zone.Exit;
                        ExitNode = node;
                        break;
                    default:
                        node.zone = Zone.Normal;
                        break;
                }

                Nodes.Add(node);
            }
        }

        foreach (Node node in Nodes)
        {
            foreach (Node other in Nodes)
            {
                if (node == other)
                    continue;

                if (Vector3.Distance(node.position, other.position) > connectionDistance)
                    continue;

                Vector3 start = node.position + Vector3.up * 0.7f;
                Vector3 end = other.position + Vector3.up * 0.7f;

                if (!Physics.Linecast(start, end, ~0, QueryTriggerInteraction.Ignore))
                    node.neighbors.Add(other);
            }
        }
    }

    public Node Nearest(Vector3 position, bool wolf = false)
    {
        Node result = null;
        float distance = float.MaxValue;

        foreach (Node node in Nodes)
        {
            if (wolf && (node.zone == Zone.Safe || node.zone == Zone.Initial))
                continue;

            Vector3 difference = position - node.position;
            difference.y = 0;
            float newDistance = difference.sqrMagnitude;

            if (newDistance < distance)
            {
                distance = newDistance;
                result = node;
            }
        }

        return result;
    }

    public List<Node> FindPath(Vector3 origin, Vector3 destination, bool wolf = false)
    {
        Node start = Nearest(origin, wolf);
        Node goal = Nearest(destination, wolf);
        List<Node> open = new List<Node>();
        List<Node> closed = new List<Node>();
        Dictionary<Node, Node> previous = new Dictionary<Node, Node>();
        Dictionary<Node, float> costs = new Dictionary<Node, float>();

        if (start == null || goal == null)
            return open;

        open.Add(start);
        costs[start] = 0;

        while (open.Count > 0)
        {
            Node current = open[0];

            foreach (Node node in open)
            {
                float nodeCost = costs[node] + Vector3.Distance(node.position, goal.position);
                float currentCost = costs[current] + Vector3.Distance(current.position, goal.position);

                if (nodeCost < currentCost)
                    current = node;
            }

            if (current == goal)
            {
                List<Node> path = new List<Node>();
                path.Add(current);

                while (previous.ContainsKey(current))
                {
                    current = previous[current];
                    path.Add(current);
                }

                path.Reverse();
                return path;
            }

            open.Remove(current);
            closed.Add(current);

            foreach (Node next in current.neighbors)
            {
                if (wolf && (next.zone == Zone.Safe || next.zone == Zone.Initial))
                    continue;

                if (closed.Contains(next))
                    continue;

                float cost = costs[current] + Vector3.Distance(current.position, next.position);

                if (!costs.ContainsKey(next) || cost < costs[next])
                {
                    previous[next] = current;
                    costs[next] = cost;

                    if (!open.Contains(next))
                        open.Add(next);
                }
            }
        }

        return new List<Node>();
    }

    public float PathLength(List<Node> path)
    {
        if (path.Count == 0)
            return float.PositiveInfinity;

        float length = 0;

        for (int i = 1; i < path.Count; i++)
        {
            length += Vector3.Distance(path[i - 1].position, path[i].position);
        }

        return length;
    }

    public bool IsSafe(Vector3 position)
    {
        Vector3 groundPosition = new Vector3(position.x, 0, position.z);

        foreach (Node node in SafeNodes)
        {
            if (Vector3.Distance(groundPosition, node.position) < 1.9f)
                return true;
        }

        return false;
    }

    public bool InHunting(Vector3 position)
    {
        Vector3 groundPosition = new Vector3(position.x, 0, position.z);

        foreach (Node node in HuntingNodes)
        {
            if (Vector3.Distance(groundPosition, node.position) < 2.6f)
                return true;
        }

        return false;
    }

    // Muestro los nodos con Gizmos para no llenar la escena de objetos.
    void OnDrawGizmosSelected()
    {
        if (!showGraph)
            return;

        if (!Application.isPlaying)
            BuildGraph();

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
                case Zone.Exit:
                    Gizmos.color = Color.yellow;
                    break;
                case Zone.Initial:
                    Gizmos.color = Color.magenta;
                    break;
                default:
                    Gizmos.color = Color.gray;
                    break;
            }

            Gizmos.DrawSphere(node.position + Vector3.up * 0.15f, 0.25f);
            Gizmos.color = Color.blue;

            foreach (Node next in node.neighbors)
            {
                Gizmos.DrawLine(node.position + Vector3.up * 0.15f, next.position + Vector3.up * 0.15f);
            }
        }
    }
}
