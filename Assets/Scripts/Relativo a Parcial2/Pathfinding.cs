using System.Collections.Generic;
using UnityEngine;

public static class Pathfinding
{
    public static List<NavGraph.Node> FindPath(NavGraph graph, Vector3 origin, Vector3 destination, bool wolf)
    {
        List<NavGraph.Node> empty = new List<NavGraph.Node>();
        NavGraph.Node start = graph.Nearest(origin, wolf);
        NavGraph.Node goal = graph.Nearest(destination, wolf);

        if (start == null || goal == null)
        {
            return empty;
        }

        PriorityQueue<NavGraph.Node> open = new PriorityQueue<NavGraph.Node>();
        HashSet<NavGraph.Node> closed = new HashSet<NavGraph.Node>();
        Dictionary<NavGraph.Node, float> cost = new Dictionary<NavGraph.Node, float>();
        Dictionary<NavGraph.Node, NavGraph.Node> previous = new Dictionary<NavGraph.Node, NavGraph.Node>();

        cost[start] = 0f;
        open.Enqueue(start, GetHeuristic(start, goal));

        while (open.Count > 0)
        {
            NavGraph.Node current = open.Dequeue();

            if (closed.Contains(current))
            {
                continue;
            }

            if (current == goal)
            {
                return Reconstruct(previous, current);
            }

            closed.Add(current);

            foreach (NavGraph.Node next in current.neighbors)
            {
                if (wolf && (next.zone == NavGraph.Zone.Safe || next.zone == NavGraph.Zone.Initial))
                {
                    continue;
                }

                if (closed.Contains(next))
                {
                    continue;
                }

                float newCost = cost[current] + Vector3.Distance(current.position, next.position);

                if (cost.ContainsKey(next) && newCost >= cost[next])
                {
                    continue;
                }

                cost[next] = newCost;
                previous[next] = current;
                float priority = newCost + GetHeuristic(next, goal);
                open.Enqueue(next, priority);
            }
        }

        return empty;
    }

    static float GetHeuristic(NavGraph.Node from, NavGraph.Node to)
    {
        return Vector3.Distance(from.position, to.position);
    }

    static List<NavGraph.Node> Reconstruct(Dictionary<NavGraph.Node, NavGraph.Node> previous, NavGraph.Node current)
    {
        List<NavGraph.Node> result = new List<NavGraph.Node>();
        result.Add(current);

        while (previous.ContainsKey(current))
        {
            current = previous[current];
            result.Add(current);
        }

        result.Reverse();
        return result;
    }
}
