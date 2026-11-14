using UnityEngine;

public class VisionSensor : MonoBehaviour
{
    [SerializeField] LayerMask obstacles = 1 << 8;
    [SerializeField] float eyeHeight = 0.4f;

    public bool CanSee(Vector3 position, float viewDistance, float viewAngle = 360f)
    {
        if (!InRange(position, viewDistance))
        {
            return false;
        }

        if (!InViewAngle(position, viewAngle))
        {
            return false;
        }

        return InLineOfSight(position);
    }

    public bool InRange(Vector3 position, float viewDistance)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0f;
        float distance = direction.magnitude;

        return distance <= viewDistance && distance >= 0.001f;
    }

    public bool InViewAngle(Vector3 position, float viewAngle)
    {
        if (viewAngle >= 360f)
        {
            return true;
        }

        Vector3 direction = position - transform.position;
        direction.y = 0f;
        float angle = Vector3.Angle(transform.forward, direction);

        return angle <= viewAngle * 0.5f;
    }

    public bool InLineOfSight(Vector3 position)
    {
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 destination = position + Vector3.up * eyeHeight;

        return !Physics.Linecast(origin, destination, obstacles, QueryTriggerInteraction.Ignore);
    }
}
