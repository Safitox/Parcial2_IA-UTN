using System.Collections.Generic;
using UnityEngine;

public class MoverEnGraph
{
    public readonly List<NavGraph.Node> Path = new List<NavGraph.Node>();
    int index;

    public void Clear()
    {
        Path.Clear();
        index = 0;
    }

    public bool Set(Vector3 origin, Vector3 target, bool wolf = false)
    {
        Clear();

        if (NavGraph.Instance == null)
        {
            return false;
        }

        List<NavGraph.Node> newPath = NavGraph.Instance.FindPath(origin, target, wolf);
        Path.AddRange(newPath);

        if (Path.Count > 1)
        {
            index = 1;
        }
        else
        {
            index = 0;
        }

        return Path.Count > 0;
    }

    public bool Move(Transform agent, float speed, float turnSpeed = 360f)
    {
        if (index >= Path.Count)
        {
            return true;
        }

        Vector3 goal = Path[index].position;
        goal.y = agent.position.y;

        Vector3 direction = goal - agent.position;

        if (direction.sqrMagnitude < 0.16f)
        {
            index++;
            return index >= Path.Count;
        }

        agent.position = Vector3.MoveTowards(agent.position, goal, speed * Time.deltaTime);

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion rotation = Quaternion.LookRotation(direction);
            agent.rotation = Quaternion.RotateTowards(agent.rotation, rotation, turnSpeed * Time.deltaTime);
        }

        return false;
    }

    public void DebugDraw(Color color)
    {
        for (int i = 1; i < Path.Count; i++)
        {
            Vector3 start = Path[i - 1].position + Vector3.up;
            Vector3 end = Path[i].position + Vector3.up;
            Debug.DrawLine(start, end, color);
        }
    }
}
