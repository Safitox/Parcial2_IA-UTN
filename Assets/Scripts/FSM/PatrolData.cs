using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PatrolData
{
    public List<Transform> waypoints;
    public Transform transform;
    public float waypointCheckDistance; 
}