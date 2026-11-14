using UnityEngine;

public class AgentSensor : MonoBehaviour
{
    [SerializeField] float interval = 0.2f;
    [SerializeField] LayerMask detectionLayers = 1 << 9;

    Collider[] nearby = new Collider[64];
    int count;
    float nextScan;
    public int ScanCount { get; private set; }

    public int Scan(float radius, bool includeTriggers = false)
    {
        if (Time.time < nextScan) return count;
        nextScan = Time.time + interval;
        QueryTriggerInteraction triggers = QueryTriggerInteraction.Ignore;
        if (includeTriggers) triggers = QueryTriggerInteraction.Collide;
        count = Physics.OverlapSphereNonAlloc(transform.position, radius, nearby, detectionLayers, triggers);
        ScanCount++;
        return count;
    }

    public Collider GetCollider(int index)
    {
        return nearby[index];
    }
}
