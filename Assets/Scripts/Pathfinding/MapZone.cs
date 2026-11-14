using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class MapZone : MonoBehaviour
{
    public NavGraph.Zone zone;
    [SerializeField] BoxCollider area;

    void Awake()
    {
        if (area == null)
        {
            area = GetComponent<BoxCollider>();
        }
    }

    public bool Contains(Vector3 position)
    {
        if (area == null)
        {
            area = GetComponent<BoxCollider>();
        }

        Vector3 point = transform.InverseTransformPoint(position) - area.center;
        Vector3 half = area.size * 0.5f;
        return Mathf.Abs(point.x) <= half.x && Mathf.Abs(point.z) <= half.z;
    }

    public Vector3 Confine(Vector3 position, ref Vector3 velocity)
    {
        if (area == null)
        {
            area = GetComponent<BoxCollider>();
        }

        Vector3 point = transform.InverseTransformPoint(position) - area.center;
        Vector3 localVelocity = transform.InverseTransformVector(velocity);
        float halfX = Mathf.Max(0f, area.size.x * 0.5f - 0.2f);
        float halfZ = Mathf.Max(0f, area.size.z * 0.5f - 0.2f);
        float x = Mathf.Clamp(point.x, -halfX, halfX);
        float z = Mathf.Clamp(point.z, -halfZ, halfZ);

        if (x != point.x)
        {
            localVelocity.x = -localVelocity.x;
        }

        if (z != point.z)
        {
            localVelocity.z = -localVelocity.z;
        }

        velocity = transform.TransformVector(localVelocity);
        return transform.TransformPoint(new Vector3(x, point.y, z) + area.center);
    }

    void OnDrawGizmosSelected()
    {
        BoxCollider box = GetComponent<BoxCollider>();

        if (box == null)
        {
            return;
        }

        switch (zone)
        {
            case NavGraph.Zone.Initial:
                Gizmos.color = Color.magenta;
                break;
            case NavGraph.Zone.Hunting:
                Gizmos.color = Color.green;
                break;
            case NavGraph.Zone.Safe:
                Gizmos.color = Color.cyan;
                break;
            case NavGraph.Zone.Exit:
                Gizmos.color = Color.yellow;
                break;
            default:
                Gizmos.color = Color.gray;
                break;
        }

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
        Gizmos.matrix = oldMatrix;
    }
}
