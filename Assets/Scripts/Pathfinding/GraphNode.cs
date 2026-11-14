using UnityEngine;
public partial class GraphNode : MonoBehaviour
{
    public NodeZone zone;
    void OnDrawGizmos()
    {
        switch (zone)
        {
            case NodeZone.Hunting:
                Gizmos.color = Color.green;
                break;

            case NodeZone.Safe:
                Gizmos.color = Color.cyan;
                break;

            case NodeZone.Exit:
                Gizmos.color = Color.yellow;
                break;

            default:
                Gizmos.color = Color.gray;
                break;
        }

        Gizmos.DrawSphere(transform.position, 0.28f);
    }
}
